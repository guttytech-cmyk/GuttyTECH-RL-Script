using System.Text.RegularExpressions;

namespace GuttyRL;

/// <summary>Restaura .save Epic/Steam e purga RLSettingsData — REMOVER nao tocava nisso (boot travado).</summary>
internal static class SaveRecovery
{
    private static readonly Regex BackupName = new(
        @"^\d{8}_\d{6}_(.+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string? SaveDirFromIni(string iniPath, bool epic = true)
    {
        if (string.IsNullOrWhiteSpace(iniPath)) return null;
        string? tagame = Path.GetDirectoryName(Path.GetDirectoryName(iniPath));
        if (tagame is null) return null;
        return Path.Combine(tagame, epic ? "SaveDataEpic" : "SaveData", "DBE_Production");
    }

    public static bool RestoreEpicSave(string iniPath, bool preferNewest = false) =>
        RestoreInto(SaveDirFromIni(iniPath, epic: true), preferNewest);

    public static bool RestoreSteamSave(string iniPath, bool preferNewest = false) =>
        RestoreInto(SaveDirFromIni(iniPath, epic: false), preferNewest);

    public static bool RestoreLatestBackup(string iniPath) =>
        RestoreEpicSave(iniPath, preferNewest: true) | RestoreSteamSave(iniPath, preferNewest: true);

    private static bool RestoreInto(string? saveDir, bool preferNewest)
    {
        if (saveDir is null) return false;

        string backupRoot = Path.Combine(AppMeta.BackupDir, "SaveDataEpic");
        if (!Directory.Exists(backupRoot))
            return preferNewest ? false : QuarantineSaves(saveDir);

        var groups = Directory.EnumerateFiles(backupRoot, "*.save")
            .Select(f => new FileInfo(f))
            .Select(f =>
            {
                var m = BackupName.Match(f.Name);
                return m.Success ? (Orig: m.Groups[1].Value, File: f) : default;
            })
            .Where(x => x.Orig is not null)
            .GroupBy(x => x.Orig!, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (groups.Count == 0)
            return preferNewest ? false : QuarantineSaves(saveDir);

        if (!Directory.Exists(saveDir) && preferNewest)
            return false;

        try
        {
            Directory.CreateDirectory(saveDir);
            foreach (var g in groups)
            {
                var pick = preferNewest
                    ? g.OrderByDescending(x => x.File.LastWriteTimeUtc).First().File
                    : g.OrderBy(x => x.File.LastWriteTimeUtc).First().File;
                string dest = Path.Combine(saveDir, g.Key);
                File.Copy(pick.FullName, dest, true);
                AppMeta.Log($"Save restaurado: {g.Key} <- {pick.Name} ({Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(saveDir)))})");
            }
            return true;
        }
        catch (Exception ex)
        {
            AppMeta.Log("Falha ao restaurar save: " + ex.Message);
            return false;
        }
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
        bool epic = RestoreEpicSave(iniPath);
        bool steam = RestoreSteamSave(iniPath);
        bool purge = PurgeRlSettingsData();
        // OK se pelo menos um store restaurou OU nao havia pasta; purge sempre.
        bool anyStore = Directory.Exists(SaveDirFromIni(iniPath, true) ?? "")
            || Directory.Exists(SaveDirFromIni(iniPath, false) ?? "");
        if (!anyStore) return purge;
        return (epic || steam) && purge;
    }
}
