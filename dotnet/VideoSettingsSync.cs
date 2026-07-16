using System.Diagnostics;

namespace GuttyRL;

/// <summary>Backup + patch seguro do .save Epic (so video/FPS). Nao apaga save nem RLSettingsData.</summary>
internal static class VideoSettingsSync
{
    public static bool SyncVideoSave(string iniPath, string mode, bool interactive)
    {
        if (!CheckGameClosed(interactive)) return false;

        string? tagame = Path.GetDirectoryName(Path.GetDirectoryName(iniPath));
        if (tagame is null) return false;
        string saveDir = Path.Combine(tagame, "SaveDataEpic", "DBE_Production");

        bool ok = true;
        ok &= BackupSaves(saveDir);
        ok &= SaveVideoPatcher.PatchSaveDirectory(saveDir, mode);
        return ok;
    }

    private static bool CheckGameClosed(bool interactive)
    {
        if (GetRl().Length == 0) return true;
        if (!interactive) return false;
        Ui.Prompt("Feche o Rocket League para sincronizar o menu. Fechar agora? (S/N)");
        if (!IsYes(Console.ReadLine())) return false;
        foreach (var p in GetRl()) { try { p.Kill(); } catch { } }
        Thread.Sleep(1500);
        return GetRl().Length == 0;
    }

    private static bool BackupSaves(string saveDir)
    {
        try
        {
            if (!Directory.Exists(saveDir)) return true;

            string dest = Path.Combine(AppMeta.BackupDir, "SaveDataEpic");
            Directory.CreateDirectory(dest);
            string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            foreach (var f in Directory.EnumerateFiles(saveDir, "*.save"))
            {
                string name = Path.GetFileName(f);
                string backup = Path.Combine(dest, $"{ts}_{name}");
                if (!File.Exists(backup))
                    File.Copy(f, backup, false);
            }

            AppMeta.Log($"Backup save Epic em SaveDataEpic/{ts}_*.save");
            return true;
        }
        catch (Exception ex)
        {
            AppMeta.Log("Falha no backup save Epic: " + ex.Message);
            return false;
        }
    }

    private static Process[] GetRl()
    { try { return Process.GetProcessesByName("RocketLeague"); } catch { return Array.Empty<Process>(); } }

    private static bool IsYes(string? s) => string.Equals(s?.Trim(), "S", StringComparison.OrdinalIgnoreCase);
}
