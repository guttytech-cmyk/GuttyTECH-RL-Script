using System.Text;
using System.Text.RegularExpressions;

namespace GuttyRL;

/// <summary>
/// Sonda a pasta real do Rocket League (Epic/Steam/custom) para EAC e o exe.
/// Sem isto o zip so dizia "setup nao encontrado" e escondia o caminho.
/// </summary>
internal static class GameInstallProbe
{
    public enum InstallVerdict
    {
        Ok,
        Incomplete,
        Missing,
    }

    public sealed class Fs
    {
        public Fs(
            Func<string, bool> dirExists,
            Func<string, bool> fileExists,
            Func<string, string?> readText,
            Func<string, string, IEnumerable<string>> enumerateFiles)
        {
            DirExists = dirExists;
            FileExists = fileExists;
            ReadText = readText;
            EnumerateFiles = enumerateFiles;
        }

        public Func<string, bool> DirExists { get; set; }
        public Func<string, bool> FileExists { get; set; }
        public Func<string, string?> ReadText { get; set; }
        public Func<string, string, IEnumerable<string>> EnumerateFiles { get; set; }
    }

    public sealed record RootHit(
        string Path,
        string Source,
        bool DirectoryExists,
        bool HasRocketLeagueExe,
        bool HasEacSetup);

    public sealed record Report(
        InstallVerdict Verdict,
        string? RocketLeagueExe,
        string? EacSetupPath,
        IReadOnlyList<RootHit> Roots,
        IReadOnlyList<string> Notes);

    public static string EacSetupPath(string root) =>
        Path.Combine(root, "Binaries", "Win64", "EasyAntiCheat", "EasyAntiCheat_EOS_Setup.exe");

    public static string AltEacSetupPath(string root) =>
        Path.Combine(root, "EasyAntiCheat", "EasyAntiCheat_EOS_Setup.exe");

    public static string RocketLeagueExePath(string root) =>
        Path.Combine(root, "Binaries", "Win64", "RocketLeague.exe");

    public static string AltRocketLeagueExePath(string root) =>
        Path.Combine(root, "RocketLeague.exe");

    public static Fs LiveFs() => new(
        Directory.Exists,
        File.Exists,
        p =>
        {
            try { return File.Exists(p) ? File.ReadAllText(p) : null; }
            catch { return null; }
        },
        (dir, pattern) =>
        {
            try
            {
                return Directory.Exists(dir)
                    ? Directory.EnumerateFiles(dir, pattern)
                    : Array.Empty<string>();
            }
            catch { return Array.Empty<string>(); }
        });

    private static readonly object LiveGate = new();
    private static Report? _liveCache;
    private static DateTime _liveCacheUtc;

    public static Report ScanLive()
    {
        lock (LiveGate)
        {
            if (_liveCache is not null && DateTime.UtcNow - _liveCacheUtc < TimeSpan.FromSeconds(3))
                return _liveCache;
            Report report = ScanLiveUncached();
            _liveCache = report;
            _liveCacheUtc = DateTime.UtcNow;
            return report;
        }
    }

    private static Report ScanLiveUncached()
    {
        var extra = new List<string>
        {
            @"C:\Program Files\Epic Games\rocketleague",
            @"C:\Program Files (x86)\Steam\steamapps\common\rocketleague",
            @"D:\Program Files\Epic Games\rocketleague",
            @"D:\SteamLibrary\steamapps\common\rocketleague",
            @"E:\SteamLibrary\steamapps\common\rocketleague",
        };

        try
        {
            foreach (string drive in Environment.GetLogicalDrives())
                extra.Add(Path.Combine(drive, "Program Files", "Epic Games", "rocketleague"));
        }
        catch { }

        extra.AddRange(TryUninstallLocations());
        return Scan(LiveFs(), Environment.ProcessPath, extra);
    }

    public static Report Scan(Fs fs, string? processPath, IEnumerable<string>? extraRoots = null)
    {
        var candidates = new List<(string Path, string Source)>();
        var notes = new List<string>();

        void Add(string? path, string source)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            candidates.Add((Normalize(path), source));
        }

        if (!string.IsNullOrWhiteSpace(processPath))
        {
            string? dir = Path.GetDirectoryName(processPath);
            for (int i = 0; i < 5 && !string.IsNullOrWhiteSpace(dir); i++)
            {
                Add(dir, i == 0 ? "exe-dir" : "exe-parent");
                dir = Path.GetDirectoryName(dir);
            }
        }

        if (extraRoots is not null)
        {
            foreach (string root in extraRoots)
                Add(root, "candidate");
        }

        string manifestDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Epic", "EpicGamesLauncher", "Data", "Manifests");
        if (fs.DirExists(manifestDir))
        {
            foreach (string item in fs.EnumerateFiles(manifestDir, "*.item"))
            {
                string? json = fs.ReadText(item);
                if (string.IsNullOrWhiteSpace(json) || !LooksLikeRocketLeague(json))
                    continue;

                string? loc = ParseInstallLocation(json);
                if (IsIncompleteInstall(json))
                    notes.Add("Epic bIsIncompleteInstall=true loc=" + (loc ?? Path.GetFileName(item)));
                if (loc is null)
                    notes.Add("Epic manifest RL sem InstallLocation: " + Path.GetFileName(item));
                else
                {
                    Add(loc, "epic-manifest");
                    notes.Add("Epic InstallLocation: " + loc + " existe=" + fs.DirExists(Normalize(loc)));
                }
            }
        }
        else
            notes.Add("Epic Manifests ausente: " + manifestDir);

        string launcherDat = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Epic", "UnrealEngineLauncher", "LauncherInstalled.dat");
        string? launcherJson = fs.ReadText(launcherDat);
        if (launcherJson is not null)
        {
            foreach (string loc in ParseInstallLocationsNearRocketLeague(launcherJson))
            {
                Add(loc, "epic-launcher");
                notes.Add("LauncherInstalled: " + loc + " existe=" + fs.DirExists(Normalize(loc)));
            }
        }

        foreach (string steamRoot in SteamRootGuesses())
        {
            foreach (string vdf in new[]
                     {
                         Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf"),
                         Path.Combine(steamRoot, "config", "libraryfolders.vdf"),
                     })
            {
                string? text = fs.ReadText(vdf);
                if (string.IsNullOrWhiteSpace(text)) continue;
                foreach (string lib in ParseSteamLibraryPaths(text))
                    Add(Path.Combine(lib, "steamapps", "common", "rocketleague"), "steam-library");
            }
        }

        var hits = new List<RootHit>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach ((string path, string source) in candidates)
        {
            if (!seen.Add(path)) continue;
            hits.Add(Classify(path, source, fs));
        }

        string? rlExe = hits.Select(h => ResolveRocketLeagueExe(h, fs)).FirstOrDefault(p => p is not null);
        string? eac = hits.Select(h => ResolveEacSetup(h, fs)).FirstOrDefault(p => p is not null);

        InstallVerdict verdict;
        if (eac is not null && rlExe is not null)
            verdict = InstallVerdict.Ok;
        else if (eac is not null || rlExe is not null || hits.Any(h => MeaningfulIncomplete(h)))
            verdict = InstallVerdict.Incomplete;
        else
            verdict = InstallVerdict.Missing;

        return new Report(verdict, rlExe, eac, hits, notes);
    }

    public static RootHit Classify(string root, string source, Fs fs)
    {
        bool dir = fs.DirExists(root);
        bool exe = fs.FileExists(RocketLeagueExePath(root)) || fs.FileExists(AltRocketLeagueExePath(root));
        bool eac = fs.FileExists(EacSetupPath(root)) || fs.FileExists(AltEacSetupPath(root));
        return new RootHit(root, source, dir, exe, eac);
    }

    public static bool LooksLikeRocketLeague(string json)
    {
        if (json.IndexOf("rocketleague", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (json.IndexOf("Rocket League", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        return Regex.IsMatch(json, "\"AppName\"\\s*:\\s*\"Sugar\"", RegexOptions.IgnoreCase);
    }

    public static bool IsIncompleteInstall(string json) =>
        Regex.IsMatch(json, "\"bIsIncompleteInstall\"\\s*:\\s*true", RegexOptions.IgnoreCase);

    public static string? ParseInstallLocation(string json)
    {
        Match m = Regex.Match(json, "\"InstallLocation\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase);
        if (!m.Success) return null;
        string loc = Unescape(m.Groups[1].Value);
        return string.IsNullOrWhiteSpace(loc) ? null : loc;
    }

    public static IReadOnlyList<string> ParseInstallLocationsNearRocketLeague(string json)
    {
        var list = new List<string>();
        foreach (Match m in Regex.Matches(json, "\"InstallLocation\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase))
        {
            int start = Math.Max(0, m.Index - 320);
            int len = Math.Min(json.Length - start, m.Length + 640);
            if (!LooksLikeRocketLeague(json.Substring(start, len)))
                continue;
            string loc = Unescape(m.Groups[1].Value);
            if (!string.IsNullOrWhiteSpace(loc))
                list.Add(loc);
        }

        return list;
    }

    public static IReadOnlyList<string> ParseSteamLibraryPaths(string vdf)
    {
        var list = new List<string>();
        foreach (Match m in Regex.Matches(vdf, "\"path\"\\s+\"([^\"]+)\""))
        {
            string path = Unescape(m.Groups[1].Value);
            if (!string.IsNullOrWhiteSpace(path))
                list.Add(path);
        }

        return list;
    }

    public static string SuggestedAction(Report report) => report.Verdict switch
    {
        InstallVerdict.Missing =>
            "Epic/Steam → Verificar arquivos (RL nao instalado). Nao use Recuperar Boot.",
        InstallVerdict.Incomplete =>
            "Epic/Steam → Verificar arquivos (instalacao incompleta / EAC ausente).",
        _ => "",
    };

    public static string DiagnosticLine(Report report) => report.Verdict switch
    {
        InstallVerdict.Ok =>
            "RL: instalado — " + (report.RocketLeagueExe ?? report.EacSetupPath),
        InstallVerdict.Incomplete =>
            "RL: INSTALACAO INCOMPLETA — exe="
            + (report.RocketLeagueExe ?? "(nao)") + " eac="
            + (report.EacSetupPath ?? "(nao)"),
        _ => "RL: NAO INSTALADO — pasta do jogo/EAC ausente.",
    };

    public static string FormatText(Report report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Verdict: " + VerdictLabel(report.Verdict));
        sb.AppendLine("RocketLeague.exe: " + (report.RocketLeagueExe ?? "(nao encontrado)"));
        sb.AppendLine("EAC setup: " + (report.EacSetupPath ?? "(nao encontrado)"));
        sb.AppendLine();
        sb.AppendLine("Candidatos:");
        if (report.Roots.Count == 0)
            sb.AppendLine("  (nenhum)");
        else
        {
            foreach (RootHit hit in report.Roots)
            {
                sb.AppendLine(
                    $"  [{hit.Source}] {hit.Path} dir={hit.DirectoryExists} exe={hit.HasRocketLeagueExe} eac={hit.HasEacSetup}");
            }
        }

        if (report.Notes.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Notas:");
            foreach (string note in report.Notes)
                sb.AppendLine("  - " + note);
        }

        return sb.ToString();
    }

    public static string VerdictLabel(InstallVerdict verdict) => verdict switch
    {
        InstallVerdict.Ok => "OK",
        InstallVerdict.Incomplete => "INCOMPLETO",
        _ => "NAO_INSTALADO",
    };

    private static bool MeaningfulIncomplete(RootHit hit)
    {
        if (!hit.DirectoryExists)
            return false;
        if (hit.Source is "epic-manifest" or "epic-launcher" or "steam-library" or "candidate" or "registry")
            return true;
        string name = Path.GetFileName(hit.Path);
        return name.IndexOf("rocket", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string? ResolveRocketLeagueExe(RootHit hit, Fs fs)
    {
        if (!hit.HasRocketLeagueExe) return null;
        string primary = RocketLeagueExePath(hit.Path);
        if (fs.FileExists(primary)) return primary;
        string alt = AltRocketLeagueExePath(hit.Path);
        return fs.FileExists(alt) ? alt : primary;
    }

    private static string? ResolveEacSetup(RootHit hit, Fs fs)
    {
        if (!hit.HasEacSetup) return null;
        string primary = EacSetupPath(hit.Path);
        if (fs.FileExists(primary)) return primary;
        string alt = AltEacSetupPath(hit.Path);
        return fs.FileExists(alt) ? alt : primary;
    }

    private static string Normalize(string path) =>
        Unescape(path).Replace('/', '\\').Trim().TrimEnd('\\');

    private static string Unescape(string value) =>
        value.Replace(@"\\", @"\", StringComparison.Ordinal);

    private static IEnumerable<string> SteamRootGuesses()
    {
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam");
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam");
        yield return @"D:\Steam";
        yield return @"E:\Steam";
    }

    private static IEnumerable<string> TryUninstallLocations()
    {
        var found = new List<string>();
        string[] keys =
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
        };

        foreach (string keyPath in keys)
        {
            try
            {
                using var root = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(keyPath);
                if (root is null) continue;
                foreach (string name in root.GetSubKeyNames())
                {
                    try
                    {
                        using var sub = root.OpenSubKey(name);
                        if (sub is null) continue;
                        string? display = sub.GetValue("DisplayName") as string;
                        if (string.IsNullOrWhiteSpace(display)
                            || display.IndexOf("Rocket League", StringComparison.OrdinalIgnoreCase) < 0)
                            continue;
                        string? loc = sub.GetValue("InstallLocation") as string;
                        if (!string.IsNullOrWhiteSpace(loc))
                            found.Add(loc);
                    }
                    catch { }
                }
            }
            catch { }
        }

        return found;
    }
}
