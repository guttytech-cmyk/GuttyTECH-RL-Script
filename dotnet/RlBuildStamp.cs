using System.Diagnostics;

namespace GuttyRL;

/// <summary>
/// Versao do RocketLeague.exe. Se o patch da season mudar o binario,
/// o watcher reaplica o modo sozinho — sem popup.
/// </summary>
internal static class RlBuildStamp
{
    public static string TagPath => Path.Combine(AppMeta.GuttyDir, "rl-build.tag");

    /// <summary>Primeira vez (sem tag) nao reaplica. So quando a versao gravada difere.</summary>
    public static bool ShouldReapply(string? stored, string? current)
    {
        if (string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(stored))
            return false;
        return !string.Equals(stored.Trim(), current.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public static string? ReadInstalled()
    {
        try
        {
            GameInstallProbe.Report report = GameInstallProbe.ScanLive();
            string? exe = report.RocketLeagueExe;
            if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
                return null;

            FileVersionInfo info = FileVersionInfo.GetVersionInfo(exe);
            string ver = string.IsNullOrWhiteSpace(info.FileVersion)
                ? (info.ProductVersion ?? "")
                : info.FileVersion;
            if (!string.IsNullOrWhiteSpace(ver))
                return ver.Trim();

            var fi = new FileInfo(exe);
            return fi.Length.ToString() + "@" + fi.LastWriteTimeUtc.Ticks.ToString();
        }
        catch (Exception ex)
        {
            AppMeta.Log("RL build: " + ex.Message);
            return null;
        }
    }

    public static string? ReadStored()
    {
        try
        {
            if (!File.Exists(TagPath)) return null;
            string raw = File.ReadAllText(TagPath).Trim();
            return raw.Length == 0 ? null : raw;
        }
        catch
        {
            return null;
        }
    }

    public static void Remember(string version)
    {
        if (string.IsNullOrWhiteSpace(version)) return;
        try
        {
            Directory.CreateDirectory(AppMeta.GuttyDir);
            File.WriteAllText(TagPath, version.Trim());
        }
        catch (Exception ex)
        {
            AppMeta.Log("RL build gravar: " + ex.Message);
        }
    }

    public static void RememberInstalled()
    {
        string? current = ReadInstalled();
        if (!string.IsNullOrWhiteSpace(current))
            Remember(current);
    }

    /// <summary>
    /// True se o exe mudou desde a ultima reaplicacao. Nao grava a tag —
    /// quem chama grava so depois do heal, pra repetir se falhar.
    /// </summary>
    public static bool TryDetectUpdate(out string detail)
    {
        detail = "";
        string? current = ReadInstalled();
        if (string.IsNullOrWhiteSpace(current))
            return false;

        string? stored = ReadStored();
        if (string.IsNullOrWhiteSpace(stored))
        {
            Remember(current);
            return false;
        }

        if (!ShouldReapply(stored, current))
            return false;

        detail = stored + " -> " + current;
        return true;
    }
}
