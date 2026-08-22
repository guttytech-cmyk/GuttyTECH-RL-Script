using System.Runtime.InteropServices;
using System.Text;

namespace GuttyRL;

/// <summary>
/// Resolve o INI/saves do RL sem cair no OneDrive nem noutro perfil Windows.
/// Caso MENĐONÇA: OneDrive no perfil atual (gusta) + perfil Windows antigo
/// (Gustavo) com Documentos local. v25.0.13 pinava o perfil errado e o restore
/// nunca migrava o OneDrive do jogo real.
/// </summary>
internal static class RlPathResolver
{
    private static readonly Guid FolderIdDocuments = new("FDD39AD0-238F-46AF-ADB4-6C85480369C7");
    private const uint KfFlagDefaultPath = 0x00000400;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHGetKnownFolderPath(ref Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SHSetKnownFolderPath(ref Guid rfid, uint dwFlags, IntPtr hToken, string pszPath);

    [DllImport("ole32.dll")]
    private static extern void CoTaskMemFree(IntPtr ptr);

    private static string PinFile => Path.Combine(AppMeta.GuttyDir, "rl-ini.path");

    public static bool IsOneDrivePath(string? path) => RlPathPolicy.IsOneDrivePath(path);

    public static string? ResolveIni()
    {
        string me = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string? ov = Environment.GetEnvironmentVariable("GUTTYRL_INI");
        if (!string.IsNullOrWhiteSpace(ov) && File.Exists(ov))
            return ov;

        string? pinned = ReadPinned();
        if (pinned is not null && !RlPathPolicy.IsPinnedUsable(pinned, me))
        {
            AppMeta.Log("Pin ignorado: " + pinned);
            try { File.Delete(PinFile); } catch { }
            pinned = null;
        }

        if (pinned is not null && File.Exists(pinned))
            return pinned;

        var ranked = RankCandidates();
        foreach (string ini in ranked)
        {
            string? dir = Path.GetDirectoryName(ini);
            if (dir is not null && Directory.Exists(dir))
            {
                Pin(ini);
                AppMeta.Log("INI escolhido: " + ini);
                return ini;
            }
        }

        return ranked.FirstOrDefault();
    }

    /// <summary>
    /// Se o INI do usuario atual estiver no OneDrive: copia para Documentos
    /// local e desfaz o redirecionamento. Nao mexe em perfil Windows alheio
    /// nem desfaz Documentos sem uma fonte OneDrive deste usuario.
    /// </summary>
    public static string RelocateOffOneDriveIfNeeded(string iniPath)
    {
        string me = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(iniPath) || !RlPathPolicy.NeedsRelocation(iniPath, me))
            return iniPath;

        string? localDocs = ResolveLocalDocumentsDir();
        if (string.IsNullOrWhiteSpace(localDocs)
            || RlPathPolicy.IsOneDrivePath(localDocs)
            || !RlPathPolicy.IsUnderProfile(localDocs, me))
        {
            AppMeta.Log("OneDrive: nao achei Documentos local para migrar (" + localDocs + ").");
            return iniPath;
        }

        try { Directory.CreateDirectory(localDocs); } catch { }

        string destRl = Path.Combine(localDocs, "My Games", "Rocket League");
        string destIni = Path.Combine(destRl, "TAGame", "Config", "TASystemSettings.ini");

        string? srcIni = RlPathPolicy.ChooseCopySourceIni(iniPath, EnumerateCurrentUserInis(), me);
        if (!RlPathPolicy.CanRelocateOffOneDrive(srcIni, me))
        {
            AppMeta.Log("OneDrive: relocate recusado (fonte nao e o OneDrive deste usuario).");
            return iniPath;
        }

        string? srcRl = TryGetRocketLeagueRoot(srcIni!);
        if (srcRl is not null
            && Directory.Exists(srcRl)
            && !SamePath(srcRl, destRl))
        {
            AppMeta.Log("OneDrive: a copiar RL -> " + destRl + " (fonte " + srcRl + ")");
            CopyTree(srcRl, destRl);
        }

        bool kf = TryUnredirectDocuments(localDocs);
        AppMeta.Log(kf
            ? "OneDrive: Documentos do Windows apontam agora para " + localDocs
            : "OneDrive: nao consegui desfazer o redirecionamento (pasta copiada na mesma).");

        if (File.Exists(destIni) || Directory.Exists(Path.GetDirectoryName(destIni)!))
        {
            Pin(destIni);
            return destIni;
        }

        return iniPath;
    }

    public static string DescribeKnownFolders()
    {
        var sb = new StringBuilder();
        sb.AppendLine("UserProfile=" + Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        sb.AppendLine("MyDocuments(atual)=" + Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        sb.AppendLine("Documents(default)=" + (GetDefaultDocumentsPath() ?? "(n/d)"));
        sb.AppendLine("Documents(local dest)=" + (ResolveLocalDocumentsDir() ?? "(n/d)"));
        sb.AppendLine("Pinned=" + (ReadPinned() ?? "(nenhum)"));
        int i = 0;
        foreach (string ini in RankCandidates())
            sb.AppendLine($"cand[{i++}]=" + ini);
        return sb.ToString();
    }

    private static IEnumerable<string> RankCandidates()
    {
        string me = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var scored = new List<(int Score, DateTime Mtime, string Ini)>();

        foreach (string root in CandidateDocumentRoots(currentUserOnly: true))
            Consider(root, me, scored);

        // Outro perfil Windows so se o atual nao tiver NADA (nem OneDrive).
        if (RlPathPolicy.ShouldScanOtherProfiles(scored.Select(x => x.Ini)))
        {
            try
            {
                string? usersRoot = Path.GetDirectoryName(me);
                if (usersRoot is not null)
                {
                    foreach (string u in Directory.GetDirectories(usersRoot))
                    {
                        if (u.Equals(me, StringComparison.OrdinalIgnoreCase)) continue;
                        foreach (string root in CandidateDocumentRootsFor(u))
                            Consider(root, me, scored);
                    }
                }
            }
            catch { }
        }

        return scored
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Mtime)
            .Select(x => x.Ini)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static void Consider(string docsRoot, string myProfile, List<(int, DateTime, string)> scored)
    {
        string configDir = Path.Combine(docsRoot, @"My Games\Rocket League\TAGame\Config");
        if (!Directory.Exists(configDir)) return;
        string ini = Path.Combine(configDir, "TASystemSettings.ini");
        int score = RlPathPolicy.Score(ini, myProfile);

        DateTime mtime = DateTime.MinValue;
        try
        {
            if (File.Exists(ini)) mtime = File.GetLastWriteTimeUtc(ini);
            string? epic = Path.Combine(docsRoot, @"My Games\Rocket League\TAGame\SaveDataEpic\DBE_Production");
            if (Directory.Exists(epic))
            {
                var big = Directory.EnumerateFiles(epic, "*.save")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.Length)
                    .FirstOrDefault();
                if (big is not null)
                {
                    if (big.LastWriteTimeUtc > mtime) mtime = big.LastWriteTimeUtc;
                    if (big.Length >= SaveRecovery.SoftGarageMinBytes) score += 20;
                }
            }
        }
        catch { }

        scored.Add((score, mtime, ini));
    }

    private static IEnumerable<string> CandidateDocumentRoots(bool currentUserOnly)
    {
        string me = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        foreach (string r in CandidateDocumentRootsFor(me))
            yield return r;

        string? def = GetDefaultDocumentsPath();
        if (!string.IsNullOrWhiteSpace(def))
            yield return def;

        string? cur = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (!string.IsNullOrWhiteSpace(cur))
            yield return cur;

        if (!currentUserOnly) yield break;
    }

    private static IEnumerable<string> CandidateDocumentRootsFor(string userProfile)
    {
        yield return Path.Combine(userProfile, "Documents");
        yield return Path.Combine(userProfile, "Documentos");
        // OneDrive por ultimo — so para detectar/migrar, nao para preferir.
        yield return Path.Combine(userProfile, "OneDrive", "Documents");
        yield return Path.Combine(userProfile, "OneDrive", "Documentos");
        yield return Path.Combine(userProfile, "OneDrive - Personal", "Documents");
        yield return Path.Combine(userProfile, "OneDrive - Pessoal", "Documents");
        yield return Path.Combine(userProfile, "OneDrive - Pessoal", "Documentos");
        string[] extra = Array.Empty<string>();
        try
        {
            extra = Directory.GetDirectories(userProfile, "OneDrive*");
        }
        catch { }

        foreach (var d in extra)
        {
            yield return Path.Combine(d, "Documents");
            yield return Path.Combine(d, "Documentos");
        }
    }

    public static string? GetDefaultDocumentsPath()
    {
        string? raw = GetKnownFolderDefaultRaw();
        return string.IsNullOrWhiteSpace(raw) ? LocalDocumentsGuess() : raw;
    }

    private static string? GetKnownFolderDefaultRaw()
    {
        Guid id = FolderIdDocuments;
        if (SHGetKnownFolderPath(ref id, KfFlagDefaultPath, IntPtr.Zero, out IntPtr p) != 0 || p == IntPtr.Zero)
            return null;
        try
        {
            string? s = Marshal.PtrToStringUni(p);
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }
        finally
        {
            CoTaskMemFree(p);
        }
    }

    private static string? LocalDocumentsGuess()
    {
        string up = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return ResolveLocalDocumentsDir() ?? Path.Combine(up, "Documents");
    }

    private static string? ResolveLocalDocumentsDir()
    {
        string me = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var candidates = new List<string>();
        string? def = GetKnownFolderDefaultRaw();
        if (!string.IsNullOrWhiteSpace(def)) candidates.Add(PhysicalOrSelf(def));
        candidates.Add(PhysicalOrSelf(Path.Combine(me, "Documents")));
        candidates.Add(PhysicalOrSelf(Path.Combine(me, "Documentos")));
        return RlPathPolicy.PickLocalDocuments(me, candidates);
    }

    private static IEnumerable<string> EnumerateCurrentUserInis()
    {
        string me = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        foreach (string root in CandidateDocumentRootsFor(me))
        {
            string configDir = Path.Combine(root, @"My Games\Rocket League\TAGame\Config");
            if (!Directory.Exists(configDir)) continue;
            yield return Path.Combine(configDir, "TASystemSettings.ini");
        }
    }

    private static string PhysicalOrSelf(string path)
    {
        try
        {
            var di = new DirectoryInfo(path);
            if (di.Exists)
            {
                FileSystemInfo? target = di.ResolveLinkTarget(returnFinalTarget: true);
                if (target is not null) return target.FullName;
            }
        }
        catch { }

        return path;
    }

    private static bool SamePath(string a, string b)
    {
        try
        {
            return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static bool TryUnredirectDocuments(string localDocs)
    {
        try
        {
            Guid id = FolderIdDocuments;
            int hr = SHSetKnownFolderPath(ref id, 0, IntPtr.Zero, localDocs);
            if (hr != 0)
            {
                AppMeta.Log("SHSetKnownFolderPath falhou HRESULT=" + hr);
                return false;
            }

            string key = @"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders";
            using var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(key, writable: true);
            k?.SetValue("Personal", localDocs, Microsoft.Win32.RegistryValueKind.ExpandString);
            return true;
        }
        catch (Exception ex)
        {
            AppMeta.Log("Unredirect Documents: " + ex.Message);
            return false;
        }
    }

    private static string? TryGetRocketLeagueRoot(string iniPath)
    {
        // ...\My Games\Rocket League\TAGame\Config\file.ini
        try
        {
            string? config = Path.GetDirectoryName(iniPath);
            string? tagame = Path.GetDirectoryName(config);
            return Path.GetDirectoryName(tagame);
        }
        catch { return null; }
    }

    private static void CopyTree(string src, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (string dir in Directory.EnumerateDirectories(src, "*", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(src, dir);
            Directory.CreateDirectory(Path.Combine(dest, rel));
        }

        foreach (string file in Directory.EnumerateFiles(src, "*", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(src, file);
            string target = Path.Combine(dest, rel);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                var srcFi = new FileInfo(file);
                var dstFi = new FileInfo(target);
                if (dstFi.Exists && dstFi.Length >= srcFi.Length && dstFi.LastWriteTimeUtc >= srcFi.LastWriteTimeUtc)
                    continue;
                File.Copy(file, target, overwrite: true);
            }
            catch (Exception ex)
            {
                AppMeta.Log("CopyTree skip " + rel + ": " + ex.Message);
            }
        }
    }

    private static string? ReadPinned()
    {
        try
        {
            if (!File.Exists(PinFile)) return null;
            string p = File.ReadAllText(PinFile).Trim();
            return p.Length == 0 ? null : p;
        }
        catch { return null; }
    }

    public static void Pin(string iniPath)
    {
        string me = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!RlPathPolicy.IsPinnedUsable(iniPath, me))
        {
            AppMeta.Log("Pin recusado: " + iniPath);
            return;
        }

        try
        {
            Directory.CreateDirectory(AppMeta.GuttyDir);
            File.WriteAllText(PinFile, iniPath);
        }
        catch { }
    }
}
