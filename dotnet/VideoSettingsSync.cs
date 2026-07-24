using System.Diagnostics;

namespace GuttyRL;

/// <summary>Backup + patch seguro do .save (Epic e Steam) — so video/FPS.
/// Nao apaga save nem RLSettingsData.</summary>
internal static class VideoSettingsSync
{
    /// <summary>Repara VideoOptions esparso sem UI (arranque / pos-jogo).</summary>
    public static bool HealIfNeeded(string iniPath, string mode)
    {
        try
        {
            if (GetRl().Length > 0) return false;
            return SyncVideoSave(iniPath, mode, interactive: false);
        }
        catch (Exception ex)
        {
            AppMeta.Log("Heal video falhou: " + ex.Message);
            return false;
        }
    }

    public static bool SyncVideoSave(string iniPath, string mode, bool interactive, Action<int, int, string>? progress = null)
    {
        // Nunca Prompt aqui (bloqueava com "aperte Enter" no meio da barra).
        if (!EnsureGameClosed(interactive)) return false;

        string? tagame = Path.GetDirectoryName(Path.GetDirectoryName(iniPath));
        if (tagame is null) return false;

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
            string tag = saveDir.Contains("SaveDataEpic", StringComparison.OrdinalIgnoreCase) ? "Epic" : "Steam";
            progress?.Invoke(0, 1, tag);
            BackupSaves(saveDir);
            if (SaveVideoPatcher.PatchSaveDirectory(saveDir, mode, progress))
                anyOk = true;
            else
                AppMeta.Log("Patch parcial/falhou em: " + saveDir);
        }

        if (!anyDir)
        {
            AppMeta.Log("Nenhum SaveDataEpic/SaveData encontrado; menu in-game nao sincronizado.");
            return true;
        }

        return anyOk;
    }

    /// <summary>Fecha o RL sem perguntar S/N (evita travar a barra pedindo Enter).</summary>
    private static bool EnsureGameClosed(bool interactive)
    {
        var procs = GetRl();
        if (procs.Length == 0) return true;

        AppMeta.Log("RL ainda aberto no sync — encerrando automaticamente.");
        foreach (var p in procs)
        {
            try { p.Kill(); } catch { }
        }
        Thread.Sleep(1500);
        if (GetRl().Length == 0) return true;

        if (interactive)
            AppMeta.Log("Nao consegui fechar o RL; sync abortado.");
        return false;
    }

    private static bool BackupSaves(string saveDir)
    {
        try
        {
            if (!Directory.Exists(saveDir)) return true;

            string dest = Path.Combine(AppMeta.BackupDir, "SaveDataEpic");
            Directory.CreateDirectory(dest);
            string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            var files = Directory.EnumerateFiles(saveDir, "*.save")
                .Select(f => new FileInfo(f))
                .Where(f => f.Length <= 1_200_000)
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .Take(4);

            foreach (var fi in files)
            {
                string backup = Path.Combine(dest, $"{ts}_{fi.Name}");
                if (!File.Exists(backup))
                    fi.CopyTo(backup, false);
            }

            AppMeta.Log($"Backup save ({ts}) dos recentes leves.");
            return true;
        }
        catch (Exception ex)
        {
            AppMeta.Log("Falha no backup save Epic: " + ex.Message);
            return false;
        }
    }

    private static Process[] GetRl()
    {
        try { return Process.GetProcessesByName("RocketLeague"); }
        catch { return Array.Empty<Process>(); }
    }
}
