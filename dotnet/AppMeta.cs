namespace GuttyRL;

/// <summary>Metadados e caminhos persistentes — fonte unica de versao/pastas.</summary>
internal static class AppMeta
{
    public const string Version = "v25.0.8";

    public const string GitHubOwner = "guttytech-cmyk";
    public const string GitHubRepo = "GuttyTECH-RL-Script";
    public static string GitHubReleasesLatestApi =>
        $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";
    public static string GitHubReleasesPage =>
        $"https://github.com/{GitHubOwner}/{GitHubRepo}/releases";

    public static string GuttyDir =>
        Path.Combine(
            Environment.GetEnvironmentVariable("GUTTYRL_HOME") is { Length: > 0 } home
                ? home
                : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "GuttyTECH", "RL-Optimizer-v22"); // pasta estável (backups/orig) — nao muda com major

    public static string BackupDir => Path.Combine(GuttyDir, "Backups");
    public static string LogFile => Path.Combine(GuttyDir, "log.txt");
    public static string CrashLog => Path.Combine(GuttyDir, "crash.log");
    public static string OrigBackup => Path.Combine(BackupDir, "TASystemSettings.original.ini");
    public static string UpdateDismissedFile => Path.Combine(GuttyDir, "update-dismissed.tag");

    public const string IniRelative = @"My Games\Rocket League\TAGame\Config\TASystemSettings.ini";

    public static void Log(string msg)
    {
        try { File.AppendAllText(LogFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}{Environment.NewLine}"); } catch { }
    }
}
