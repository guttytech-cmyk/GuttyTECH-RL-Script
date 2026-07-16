using System.Diagnostics;

namespace GuttyRL;

/// <summary>RL Epic: menu de video vem do .save (VideoSettingsSavePC) + RLSettingsData na nuvem.
/// COMPLETO patcha o save (sem apagar) e purga so RLSettingsData.</summary>
internal static class VideoSettingsSync
{
    public static bool SyncForCompleto(string iniPath, bool interactive)
    {
        if (!CheckGameClosed(interactive)) return false;

        string? tagame = Path.GetDirectoryName(Path.GetDirectoryName(iniPath));
        if (tagame is null) return false;
        string saveDir = Path.Combine(tagame, "SaveDataEpic", "DBE_Production");

        bool ok = true;
        ok &= BackupSaves(saveDir);
        // Patch .save desativado (v22.3.30): corrompia boot em varios clientes.
        // ok &= SaveVideoPatcher.PatchSaveDirectory(saveDir);
        // ok &= EnsurePrimarySave(saveDir);
        ok &= PurgeRlSettingsDataOnly();
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

    private static bool EnsurePrimarySave(string saveDir)
    {
        try
        {
            if (!Directory.Exists(saveDir)) return true;

            var saves = Directory.EnumerateFiles(saveDir, "*.save")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .ToList();

            if (saves.Count == 0) return true;

            // Prefer main save without _N suffix when multiple exist.
            var primary = saves.FirstOrDefault(f => !RegexSuffix().IsMatch(f.Name)) ?? saves[0];
            string mainPath = Path.Combine(saveDir, StripNumericSuffix(primary.Name));

            if (!string.Equals(primary.FullName, mainPath, StringComparison.OrdinalIgnoreCase))
                File.Copy(primary.FullName, mainPath, true);

            return true;
        }
        catch (Exception ex)
        {
            AppMeta.Log("Falha ao garantir save principal: " + ex.Message);
            return false;
        }
    }

    private static System.Text.RegularExpressions.Regex RegexSuffix() =>
        new(@"_\d+\.save$", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

    private static string StripNumericSuffix(string fileName)
    {
        var m = RegexSuffix().Match(fileName);
        return m.Success ? fileName.Replace(m.Value, ".save", StringComparison.OrdinalIgnoreCase) : fileName;
    }

    private static bool PurgeRlSettingsDataOnly()
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

            AppMeta.Log($"RLSettingsData purgado ({n} arquivo(s)). RLSaveData preservado.");
            return true;
        }
        catch (Exception ex)
        {
            AppMeta.Log("Falha ao purgar RLSettingsData: " + ex.Message);
            return false;
        }
    }

    private static Process[] GetRl()
    { try { return Process.GetProcessesByName("RocketLeague"); } catch { return Array.Empty<Process>(); } }

    private static bool IsYes(string? s) => string.Equals(s?.Trim(), "S", StringComparison.OrdinalIgnoreCase);
}
