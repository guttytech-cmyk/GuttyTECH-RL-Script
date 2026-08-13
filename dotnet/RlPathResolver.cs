using System.Runtime.InteropServices;
using System.Text;

namespace GuttyRL;

/// <summary>
/// Resolve o INI/saves do RL sem cair no OneDrive nem noutro perfil Windows.
/// Caso MENĐONÇA: OneDrive desinstalado mas Documentos ainda redirecionado;
/// RESTAURAR PRESETS recriava OneDrive\Documentos e copiava saves para Steam.
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

    public static bool IsOneDrivePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        return path.IndexOf("OneDrive", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static string? ResolveIni()
    {
        string? ov = Environment.GetEnvironmentVariable("GUTTYRL_INI");
        if (!string.IsNullOrWhiteSpace(ov) && File.Exists(ov))
            return ov;

        string? pinned = ReadPinned();
        if (pinned is not null && File.Exists(pinned) && !IsOneDrivePath(pinned))
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

        if (pinned is not null && File.Exists(pinned))
            return pinned;

        return ranked.FirstOrDefault();
    }

    /// <summary>
    /// Se o INI/saves estiverem no OneDrive: copia para Documentos local,
    /// desfaz o redirecionamento do Windows e devolve o novo caminho.
    /// </summary>
    public static string RelocateOffOneDriveIfNeeded(string iniPath)
    {
        if (string.IsNullOrWhiteSpace(iniPath) || !IsOneDrivePath(iniPath))
            return iniPath;

        string? localDocs = GetDefaultDocumentsPath() ?? LocalDocumentsGuess();
        if (string.IsNullOrWhiteSpace(localDocs))
        {
            AppMeta.Log("OneDrive: nao achei Documentos local para migrar.");
            return iniPath;
        }

        try { Directory.CreateDirectory(localDocs); } catch { }

        string? srcRl = TryGetRocketLeagueRoot(iniPath);
        string destRl = Path.Combine(localDocs, "My Games", "Rocket League");
        string destIni = Path.Combine(destRl, "TAGame", "Config", "TASystemSettings.ini");

        if (srcRl is not null && Directory.Exists(srcRl))
        {
            AppMeta.Log("OneDrive: a copiar RL -> " + destRl);
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

        // Outro perfil Windows so se o atual nao tiver nada local.
        bool hasLocalMine = scored.Any(x => !IsOneDrivePath(x.Ini));
        if (!hasLocalMine)
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
        int score = 0;
        if (!IsOneDrivePath(ini)) score += 100;
        else score -= 80;
        if (ini.StartsWith(myProfile, StringComparison.OrdinalIgnoreCase)) score += 40;
        else score -= 50;

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
        Guid id = FolderIdDocuments;
        if (SHGetKnownFolderPath(ref id, KfFlagDefaultPath, IntPtr.Zero, out IntPtr p) != 0 || p == IntPtr.Zero)
            return LocalDocumentsGuess();
        try
        {
            string? s = Marshal.PtrToStringUni(p);
            return string.IsNullOrWhiteSpace(s) ? LocalDocumentsGuess() : s;
        }
        finally
        {
            CoTaskMemFree(p);
        }
    }

    private static string? LocalDocumentsGuess()
    {
        string up = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string docs = Path.Combine(up, "Documents");
        string docsPt = Path.Combine(up, "Documentos");
        if (Directory.Exists(docs) && !IsOneDrivePath(docs)) return docs;
        if (Directory.Exists(docsPt) && !IsOneDrivePath(docsPt)) return docsPt;
        return docs;
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
        try
        {
            Directory.CreateDirectory(AppMeta.GuttyDir);
            File.WriteAllText(PinFile, iniPath);
        }
        catch { }
    }
}
