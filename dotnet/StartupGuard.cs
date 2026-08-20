using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace GuttyRL;

/// <summary>Protege o startup: valida plataforma, captura crashes e evita fechar sem mensagem.</summary>
[SupportedOSPlatform("windows")]
internal static class StartupGuard
{
    public static void Install()
    {
        try { Directory.CreateDirectory(AppMeta.GuttyDir); } catch { }

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                if (CrashFilter.IsHarmlessShutdownNativeUnload(ex))
                {
                    AppMeta.Log("Shutdown nativo ignorado: " + ex.GetType().Name + " " + ex.Message);
                    return;
                }

                ReportFatal("Erro inesperado no GuttyRL.", ex);
            }
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

        RequireAdministratorOrExit();

        WarnIfRunningFromTemp();
        AppMeta.Log($"Startup {AppMeta.Version} | {Environment.ProcessPath ?? "(exe)"}");
    }

    /// <summary>
    /// O app.manifest pede requireAdministrator (UAC). Se mesmo assim nao estiver elevado
    /// (manifesto perdido, bypass, etc.), bloqueia com erro visivel e sai.
    /// </summary>
    private static void RequireAdministratorOrExit()
    {
        if (ElevationService.IsAdministrator())
            return;

        const string title = "ADMINISTRADOR OBRIGATÓRIO";
        const string body =
            "O GuttyTECH RL Optimizer precisa de permissões de administrador.\n\n" +
            "1) Feche esta janela\n" +
            "2) Clique com o direito no GuttyTECH_RL.exe\n" +
            "3) Escolha Executar como administrador\n" +
            "4) No UAC, clique Sim\n\n" +
            "Sem admin o otimizador nao pode corrigir EAC, permissoes nem o INI com seguranca.";

        AppMeta.Log("ABORT: processo sem elevacao de administrador.");
        try
        {
            System.Windows.MessageBox.Show(
                body,
                "GUTTYTECH · " + title,
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        catch { }

        try { FatalDialogService.TryShow(title, body, AppMeta.CrashLog); } catch { }

        if (!ConsoleWindowService.IsHidden)
        {
            try
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("[X] " + title);
                Console.Error.WriteLine(body);
                Console.Error.WriteLine();
            }
            catch { }
        }

        Environment.Exit(5);
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

        if (ConsoleWindowService.IsHidden)
            FatalDialogService.TryShow("AÇÃO NECESSÁRIA", hint, AppMeta.CrashLog);
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

        if (CrashFilter.IsHarmlessShutdownNativeUnload(ex))
            return;

        string body = (ex?.Message ?? "Falha de inicialização.")
            + Environment.NewLine + Environment.NewLine
            + "Log: " + AppMeta.CrashLog;

        if (ex is DllNotFoundException or BadImageFormatException)
        {
            body =
                "Falta uma biblioteca nativa do Windows/WPF (" + (ex.Message) + ")."
                + Environment.NewLine + Environment.NewLine
                + "1) Baixe de novo o GuttyTECH_RL.exe do GitHub (~142+ MB)."
                + Environment.NewLine
                + "2) Guarde em Downloads ou Desktop (nao rode do Edge/temp)."
                + Environment.NewLine
                + "3) Instale o Visual C++ 2015-2022 x64 (vc_redist)."
                + Environment.NewLine + Environment.NewLine
                + "Log: " + AppMeta.CrashLog;
        }

        // Sempre tenta MessageBox nativo — em WinExe o console nao aparece.
        try
        {
            System.Windows.MessageBox.Show(
                body,
                "GUTTYTECH · ERRO",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        catch { }

        try
        {
            if (FatalDialogService.TryShow(title, ex?.Message ?? "Falha de inicialização.", AppMeta.CrashLog))
                return;
        }
        catch { }

        if (!ConsoleWindowService.IsHidden)
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
                Console.WriteLine();
                Console.WriteLine(title);
                if (ex is not null) Console.WriteLine(ex.Message);
                Console.WriteLine(AppMeta.CrashLog);
                WaitForUser();
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
