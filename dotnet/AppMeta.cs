namespace GuttyRL;

/// <summary>Metadados e caminhos persistentes — fonte unica de versao/pastas.</summary>
internal static class AppMeta
{
    public const string Version = "v22.3.20";

    public static string GuttyDir =>
        Path.Combine(
            Environment.GetEnvironmentVariable("GUTTYRL_HOME") is { Length: > 0 } home
                ? home
                : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "GuttyTECH", "RL-Optimizer-v22");

    public static string BackupDir => Path.Combine(GuttyDir, "Backups");
    public static string LogFile => Path.Combine(GuttyDir, "log.txt");
    public static string CrashLog => Path.Combine(GuttyDir, "crash.log");
    public static string OrigBackup => Path.Combine(BackupDir, "TASystemSettings.original.ini");

    public const string IniRelative = @"My Games\Rocket League\TAGame\Config\TASystemSettings.ini";

    public static void Log(string msg)
    {
        try { File.AppendAllText(LogFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}{Environment.NewLine}"); } catch { }
    }
}
