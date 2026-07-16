namespace GuttyQuickChat;

internal static class AppMeta
{
    public const string Version = "1.4.0";

    public static string GuttyDir =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "GuttyTECH", "QuickChat");

    public static string ConfigPath => Path.Combine(GuttyDir, "config.json");
    public static string LogFile => Path.Combine(GuttyDir, "log.txt");

    public static string TAInputIni =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            @"My Games\Rocket League\TAGame\Config\TAInput.ini");

    public static void Log(string msg)
    {
        try
        {
            Directory.CreateDirectory(GuttyDir);
            File.AppendAllText(LogFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}{Environment.NewLine}");
        }
        catch { }
    }
}
