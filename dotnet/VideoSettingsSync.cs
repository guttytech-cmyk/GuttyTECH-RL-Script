using System.Diagnostics;

namespace GuttyRL;

/// <summary>Backup + patch seguro do .save (Epic e Steam) — so video/FPS.
/// Nao apaga save nem RLSettingsData.</summary>
internal static class VideoSettingsSync
{
    public static bool SyncVideoSave(string iniPath, string mode, bool interactive)
    {
        if (!CheckGameClosed(interactive)) return false;

        string? tagame = Path.GetDirectoryName(Path.GetDirectoryName(iniPath));
        if (tagame is null) return false;

        // Epic + Steam — o menu in-game vem do save ativo.
        string[] saveDirs =
        {
            Path.Combine(tagame, "SaveDataEpic", "DBE_Production"),
            Path.Combine(tagame, "SaveData", "DBE_Production"),
        };

        bool anyDir = false;
        bool anyOk = false;
        foreach (string saveDir in saveDirs)
        {
            if (!Directory.Exists(saveDir)) continue;
            anyDir = true;
            BackupSaves(saveDir);
            if (SaveVideoPatcher.PatchSaveDirectory(saveDir, mode))
                anyOk = true;
            else
                AppMeta.Log("Patch parcial/falhou em: " + saveDir);
        }

        if (!anyDir)
        {
            AppMeta.Log("Nenhum SaveDataEpic/SaveData encontrado; menu in-game nao sincronizado.");
            return true; // INI ja aplicado; save pode ainda nao existir.
        }

        return anyOk;
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
