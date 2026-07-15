using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace GuttyRL;

/// <summary>Protege o startup: valida plataforma, captura crashes e evita fechar sem mensagem.</summary>
[SupportedOSPlatform("windows")]
internal static class StartupGuard
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int MessageBoxW(nint hWnd, string text, string caption, uint type);

    public static void Install()
    {
        try { Directory.CreateDirectory(AppMeta.GuttyDir); } catch { }

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                ReportFatal("Erro inesperado no GuttyRL.", ex);
            else
                ReportFatal("Erro inesperado no GuttyRL.", new Exception(e.ExceptionObject?.ToString() ?? "desconhecido"));
        };

        try
        {
            if (!OperatingSystem.IsWindows())
            {
                ReportFatal(
                    "Este GuttyRL e exclusivo para Windows.\n\n" +
                    $"SO detectado: {Environment.OSVersion}",
                    null);
                Environment.Exit(3);
            }

            if (RuntimeInformation.ProcessArchitecture != Architecture.X64)
            {
                ReportFatal(
                    "Este GuttyRL e so para Windows 64-bit (x64).\n\n" +
                    $"Seu processador: {RuntimeInformation.ProcessArchitecture}\n\n" +
                    "Use um PC Windows x64 ou o GuttyRL.bat (modo pasta) se disponivel.",
                    null);
                Environment.Exit(3);
            }
        }
        catch { }

        WarnIfRunningFromTemp();
        AppMeta.Log($"Startup {AppMeta.Version} | {Environment.ProcessPath ?? "(exe)"}");
    }

    public static int Run(Func<int> main)
    {
        try { return main(); }
        catch (Exception ex)
        {
            ReportFatal("O GuttyRL encontrou um erro e nao pode continuar.", ex);
            return 99;
        }
    }

    public static void WaitForUser(string hint = "Pressione qualquer tecla para fechar...")
    {
        try
        {
            Console.WriteLine();
            Console.WriteLine(hint);
            Console.ReadKey(true);
            return;
        }
        catch { }

        try { MessageBoxW(0, hint, "GUTTYTECH - GuttyRL", 0x30); } catch { }
        try { Thread.Sleep(15000); } catch { }
    }

    public static void ReportFatal(string title, Exception? ex)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {title}");
        if (ex is not null)
        {
            sb.AppendLine(ex.GetType().FullName);
            sb.AppendLine(ex.Message);
            sb.AppendLine(ex.StackTrace);
        }
        sb.AppendLine($"OS: {Environment.OSVersion}");
        sb.AppendLine($"Arch: {RuntimeInformation.ProcessArchitecture}");
        sb.AppendLine($"Runtime: {RuntimeInformation.FrameworkDescription}");
        sb.AppendLine($"Exe: {Environment.ProcessPath ?? "(desconhecido)"}");
        sb.AppendLine();

        try { File.AppendAllText(AppMeta.CrashLog, sb.ToString()); } catch { }

        try
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine();
            Console.WriteLine(title);
            if (ex is not null) Console.WriteLine(ex.Message);
            Console.WriteLine();
            Console.WriteLine("Log salvo em:");
            Console.WriteLine(AppMeta.CrashLog);
            Console.WriteLine();
            Console.WriteLine("Se abriu e fechou na hora sem ver nada:");
            Console.WriteLine("  1) Extraia o .exe do ZIP antes de rodar (nao rode de dentro do ZIP).");
            Console.WriteLine("  2) Atualize o Windows (Configuracoes > Windows Update).");
            Console.WriteLine("  3) Libere o .exe no antivirus / Acesso Controlado a Pastas.");
            Console.WriteLine("  4) Abra o Rocket League 1x para criar o TASystemSettings.ini.");
            WaitForUser();
        }
        catch
        {
            try
            {
                MessageBoxW(0,
                    title + "\n\n" + (ex?.Message ?? "") + "\n\nLog: " + AppMeta.CrashLog,
                    "GUTTYTECH - GuttyRL",
                    0x10);
            }
            catch { }
        }
    }

    private static void WarnIfRunningFromTemp()
    {
        string? exe = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exe)) return;

        string full = Path.GetFullPath(exe);
        if (!full.Contains(@"\Temp\", StringComparison.OrdinalIgnoreCase)
            && !full.Contains(@"\INetCache\", StringComparison.OrdinalIgnoreCase)
            && !full.Contains(@"\AppData\Local\Temp\", StringComparison.OrdinalIgnoreCase))
            return;

        AppMeta.Log("AVISO: exe parece estar em pasta temporaria (ZIP/edge). Extraia antes de usar.");
    }
}
