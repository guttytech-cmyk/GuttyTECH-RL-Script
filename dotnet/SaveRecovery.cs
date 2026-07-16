using System.Text.RegularExpressions;

namespace GuttyRL;

/// <summary>Restaura .save Epic e purga RLSettingsData — REMOVER nao tocava nisso (boot travado).</summary>
internal static class SaveRecovery
{
    private static readonly Regex BackupName = new(
        @"^\d{8}_\d{6}_(.+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string? SaveDirFromIni(string iniPath)
    {
        string? tagame = Path.GetDirectoryName(Path.GetDirectoryName(iniPath));
        if (tagame is null) return null;
        return Path.Combine(tagame, "SaveDataEpic", "DBE_Production");
    }

    public static bool RestoreEpicSave(string iniPath, bool preferNewest = false)
    {
        string? saveDir = SaveDirFromIni(iniPath);
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
                AppMeta.Log($"Save restaurado: {g.Key} <- {pick.Name}");
            }
            return true;
        }
        catch (Exception ex)
        {
            AppMeta.Log("Falha ao restaurar save Epic: " + ex.Message);
            return false;
        }
    }

    public static bool RestoreLatestBackup(string iniPath) => RestoreEpicSave(iniPath, preferNewest: true);

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

    public static bool FullRecovery(string iniPath) =>
        RestoreEpicSave(iniPath) & PurgeRlSettingsData();
}
