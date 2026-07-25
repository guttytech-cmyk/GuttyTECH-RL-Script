using System.Text.RegularExpressions;

namespace GuttyRL;

/// <summary>Backup/restauro de saves Epic/Steam (presets/garagem) + purge RLSettingsData.</summary>
internal static class SaveRecovery
{
    private static readonly Regex BackupName = new(
        @"^\d{8}_\d{6}_(.+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Saves de garagem/presets sao tipicamente &gt;1.5MB; video-only fica abaixo.</summary>
    public const long GarageMinBytes = 1_500_000;
    public const long GarageMaxBytes = 12_000_000;

    public static string BackupRoot => Path.Combine(AppMeta.BackupDir, "SaveDataEpic");
    public static string PresetsRoot => Path.Combine(AppMeta.BackupDir, "Presets");

    public static string? SaveDirFromIni(string iniPath, bool epic = true)
    {
        if (string.IsNullOrWhiteSpace(iniPath)) return null;
        string? tagame = Path.GetDirectoryName(Path.GetDirectoryName(iniPath));
        if (tagame is null) return null;
        return Path.Combine(tagame, epic ? "SaveDataEpic" : "SaveData", "DBE_Production");
    }

    public static bool RestoreEpicSave(string iniPath, bool preferNewest = false) =>
        RestoreInto(SaveDirFromIni(iniPath, epic: true), preferNewest, preferGarage: preferNewest);

    public static bool RestoreSteamSave(string iniPath, bool preferNewest = false) =>
        RestoreInto(SaveDirFromIni(iniPath, epic: false), preferNewest, preferGarage: preferNewest);

    /// <summary>RESTAURAR PRESETS: prioriza saves grandes (garagem) de todos os backups.</summary>
    public static bool RestoreLatestBackup(string iniPath) =>
        RestorePresets(iniPath, out _);

    public static bool RestorePresets(string iniPath, out string summary)
    {
        var parts = new List<string>();
        // Snapshot do que ainda esta live (pode ser a unica copia grande)
        int snapped = SnapshotLiveGarage(iniPath);
        if (snapped > 0) parts.Add($"snapshot live={snapped}");

        bool epic = RestoreInto(SaveDirFromIni(iniPath, epic: true), preferNewest: true, preferGarage: true, parts);
        bool steam = RestoreInto(SaveDirFromIni(iniPath, epic: false), preferNewest: true, preferGarage: true, parts);
        bool purge = PurgeRlSettingsData();
        if (purge) parts.Add("cache limpo");

        summary = parts.Count > 0 ? string.Join("; ", parts) : "sem backups";
        AppMeta.Log("RESTAURAR-PRESETS: " + summary);
        return epic || steam;
    }

    /// <summary>Copia saves de garagem (grandes) — so file copy, sem decrypt.</summary>
    public static int BackupGaragePresets(string? iniPath)
    {
        if (iniPath is null) return 0;
        int n = 0;
        n += BackupGarageFromDir(SaveDirFromIni(iniPath, epic: true));
        n += BackupGarageFromDir(SaveDirFromIni(iniPath, epic: false));
        return n;
    }

    private static int SnapshotLiveGarage(string iniPath)
    {
        int n = 0;
        n += BackupGarageFromDir(SaveDirFromIni(iniPath, epic: true));
        n += BackupGarageFromDir(SaveDirFromIni(iniPath, epic: false));
        return n;
    }

    private static int BackupGarageFromDir(string? saveDir)
    {
        try
        {
            if (saveDir is null || !Directory.Exists(saveDir)) return 0;

            Directory.CreateDirectory(BackupRoot);
            Directory.CreateDirectory(PresetsRoot);
            string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            var heavy = Directory.EnumerateFiles(saveDir, "*.save")
                .Select(f => new FileInfo(f))
                .Where(f => f.Length >= GarageMinBytes && f.Length <= GarageMaxBytes)
                .OrderByDescending(f => f.Length)
                .ThenByDescending(f => f.LastWriteTimeUtc)
                .Take(8)
                .ToList();

            int n = 0;
            foreach (var fi in heavy)
            {
                string name = $"{ts}_{fi.Name}";
                string destA = Path.Combine(BackupRoot, name);
                string destB = Path.Combine(PresetsRoot, name);
                if (!File.Exists(destA))
                {
                    fi.CopyTo(destA, false);
                    n++;
                }
                if (!File.Exists(destB))
                {
                    try { fi.CopyTo(destB, false); } catch { }
                }
            }

            if (n > 0)
                AppMeta.Log($"Backup garagem/presets: {n} save(s) grandes ({ts}).");
            return n;
        }
        catch (Exception ex)
        {
            AppMeta.Log("BackupGarage: " + ex.Message);
            return 0;
        }
    }

    public static (int files, int garage, long bytes) CountBackups()
    {
        int files = 0, garage = 0;
        long bytes = 0;
        foreach (string root in EnumerateBackupRoots())
        {
            if (!Directory.Exists(root)) continue;
            foreach (var f in Directory.EnumerateFiles(root, "*.save", SearchOption.AllDirectories))
            {
                try
                {
                    var fi = new FileInfo(f);
                    files++;
                    bytes += fi.Length;
                    if (fi.Length >= GarageMinBytes) garage++;
                }
                catch { }
            }
        }
        return (files, garage, bytes);
    }

    private static IEnumerable<string> EnumerateBackupRoots()
    {
        yield return BackupRoot;
        yield return PresetsRoot;
        string q = Path.Combine(AppMeta.BackupDir, "Quarantine");
        if (Directory.Exists(q))
            yield return q;
    }

    private static bool RestoreInto(string? saveDir, bool preferNewest, bool preferGarage, List<string>? parts = null)
    {
        if (saveDir is null) return false;

        var groups = CollectBackupGroups();
        if (groups.Count == 0)
            return preferNewest ? false : QuarantineSaves(saveDir);

        if (!Directory.Exists(saveDir) && preferNewest)
            return false;

        try
        {
            Directory.CreateDirectory(saveDir);
            int restored = 0;
            long bytes = 0;
            int garageHits = 0;
            string tag = saveDir.Contains("SaveDataEpic", StringComparison.OrdinalIgnoreCase) ? "Epic" : "Steam";

            foreach (var g in groups)
            {
                FileInfo pick = preferGarage
                    ? PickGaragePreferred(g)
                    : preferNewest
                        ? g.OrderByDescending(x => x.File.LastWriteTimeUtc).First().File
                        : g.OrderBy(x => x.File.LastWriteTimeUtc).First().File;

                string dest = Path.Combine(saveDir, g.Key);
                File.Copy(pick.FullName, dest, true);
                restored++;
                bytes += pick.Length;
                if (pick.Length >= GarageMinBytes) garageHits++;
                AppMeta.Log($"Save restaurado: {g.Key} <- {pick.Name} ({pick.Length / 1024}KB, {tag})");
            }

            parts?.Add($"{tag}:{restored} ficheiros ({bytes / 1024}KB, {garageHits} garagem)");
            return restored > 0;
        }
        catch (Exception ex)
        {
            AppMeta.Log("Falha ao restaurar save: " + ex.Message);
            return false;
        }
    }

    private static FileInfo PickGaragePreferred(IGrouping<string, (string Orig, FileInfo File)> g)
    {
        // 1) Maior save de garagem (>=1.5MB), mais recente
        var big = g.Where(x => x.File.Length >= GarageMinBytes)
            .OrderByDescending(x => x.File.LastWriteTimeUtc)
            .ThenByDescending(x => x.File.Length)
            .Select(x => x.File)
            .FirstOrDefault();
        if (big is not null) return big;

        // 2) Sem garagem no backup: o MAIOR disponivel (melhor que o mais novo pequenino)
        return g.OrderByDescending(x => x.File.Length)
            .ThenByDescending(x => x.File.LastWriteTimeUtc)
            .First().File;
    }

    private static List<IGrouping<string, (string Orig, FileInfo File)>> CollectBackupGroups()
    {
        var all = new List<(string Orig, FileInfo File)>();
        foreach (string root in EnumerateBackupRoots())
        {
            if (!Directory.Exists(root)) continue;
            IEnumerable<string> files = Directory.EnumerateFiles(root, "*.save", SearchOption.AllDirectories);
            foreach (string path in files)
            {
                var fi = new FileInfo(path);
                string name = fi.Name;
                var m = BackupName.Match(name);
                string orig = m.Success ? m.Groups[1].Value : name; // quarentena sem prefixo ts
                all.Add((orig, fi));
            }
        }

        return all
            .GroupBy(x => x.Orig, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static bool QuarantineSaves(string saveDir)
    {
        try
        {
            if (!Directory.Exists(saveDir)) return true;

            var saves = Directory.EnumerateFiles(saveDir, "*.save").ToList();
            if (saves.Count == 0) return true;

            string q = Path.Combine(AppMeta.BackupDir, "Quarantine",
                DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(q);

            foreach (var f in saves)
            {
                string dest = Path.Combine(q, Path.GetFileName(f));
                if (File.Exists(dest)) File.Delete(dest);
                File.Move(f, dest);
            }

            AppMeta.Log($"Saves movidos para quarentena ({saves.Count}): {q}");
            return true;
        }
        catch (Exception ex)
        {
            AppMeta.Log("Falha na quarentena de saves: " + ex.Message);
            return false;
        }
    }

    public static bool PurgeRlSettingsData()
    {
        try
        {
            string root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Rocket League", "datastorage");
            if (!Directory.Exists(root)) return true;

            int n = 0;
            foreach (var f in Directory.EnumerateFiles(root, "RLSettingsData", SearchOption.AllDirectories))
            {
                try { File.SetAttributes(f, FileAttributes.Normal); File.Delete(f); n++; } catch { }
            }

            AppMeta.Log($"RLSettingsData purgado ({n} arquivo(s)).");
            return true;
        }
        catch (Exception ex)
        {
            AppMeta.Log("Falha ao purgar RLSettingsData: " + ex.Message);
            return false;
        }
    }

    /// <summary>Ultimo recurso: save mais antigo (stock-ish) Epic+Steam + purge cache.</summary>
    public static bool FullRecovery(string iniPath)
    {
        bool epic = RestoreInto(SaveDirFromIni(iniPath, epic: true), preferNewest: false, preferGarage: false);
        bool steam = RestoreInto(SaveDirFromIni(iniPath, epic: false), preferNewest: false, preferGarage: false);
        bool purge = PurgeRlSettingsData();
        bool anyStore = Directory.Exists(SaveDirFromIni(iniPath, true) ?? "")
            || Directory.Exists(SaveDirFromIni(iniPath, false) ?? "");
        if (!anyStore) return purge;
        return (epic || steam) && purge;
    }
}
