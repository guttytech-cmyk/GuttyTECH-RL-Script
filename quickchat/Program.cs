using System.Runtime.InteropServices;
using System.Text.Json;

namespace GuttyQuickChat;

internal static class Program
{
    [DllImport("user32.dll")]
    private static extern bool GetMessage(out Msg lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref Msg lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref Msg lpMsg);

    [StructLayout(LayoutKind.Sequential)]
    private struct Msg
    {
        public IntPtr Hwnd;
        public uint Message;
        public IntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public int PtX;
        public int PtY;
    }

    private static int Main(string[] args)
    {
        Console.Title = $"GUTTY QuickChat {AppMeta.Version}";
        PrintBanner();

        var ownsMutex = false;
        using var singleInstance = new Mutex(true, "GuttyTECH.QuickChat.SingleInstance", out ownsMutex);

        if (!ownsMutex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  [X] GuttyQuickChat ja esta rodando. Feche a outra janela.");
            Console.ResetColor();
            return 1;
        }

        try
        {
            Directory.CreateDirectory(AppMeta.GuttyDir);
            var config = QuickChatConfig.Load(AppMeta.ConfigPath);
            MigrateConfig(config);
            var detected = TAInputReader.Read(AppMeta.TAInputIni);
            config.ApplyBindings(detected);
            Preflight.Run(config);

            using var engine = new ChatEngine(config);
            AppMeta.Log("GUTTY QuickChat iniciado.");

            if (args.Any(a => a.Equals("--test", StringComparison.OrdinalIgnoreCase)))
                engine.SendTestPhrase();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  [+] Macro ATIVO. Uma tecla = uma frase. Ctrl+C para sair.");
            Console.ResetColor();
            Console.WriteLine("  [!] MINIMIZE esta janela antes de jogar.");
            Console.WriteLine();

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                AppMeta.Log("Encerrado pelo usuario.");
                Environment.Exit(0);
            };

            while (GetMessage(out var msg, IntPtr.Zero, 0, 0))
            {
                _ = TranslateMessage(ref msg);
                _ = DispatchMessage(ref msg);
            }

            return 0;
        }
        catch (Exception ex)
        {
            AppMeta.Log($"CRASH: {ex}");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [X] Erro: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine("  Pressione Enter para sair.");
            Console.ReadLine();
            return 1;
        }
    }

    private static void MigrateConfig(QuickChatConfig config)
    {
        var needsRebuild = config.DirectBinds.Count == 0
            || config.DirectBinds.Keys.Any(static k => k.StartsWith('F') || k.StartsWith("Num", StringComparison.OrdinalIgnoreCase));

        if (needsRebuild)
            config.RebuildDirectBinds1to9();

        config.TypingDelayMs = 0;
        config.ReadBindingsFromGame = false;

        var changed = needsRebuild;
        if (config.SendCooldownMs > 200)
        {
            config.SendCooldownMs = 150;
            changed = true;
        }

        if (changed)
        {
            File.WriteAllText(AppMeta.ConfigPath,
                JsonSerializer.Serialize(config, QuickChatJsonContext.Default.QuickChatConfig));
            AppMeta.Log("Config migrado para v1.4.0.");
        }
    }

    private static void PrintBanner()
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(@"
  ██████╗ ██╗   ██╗████████╗████████╗██╗   ██╗
 ██╔════╝ ██║   ██║╚══██╔══╝╚══██╔══╝╚██╗ ██╔╝
 ██║  ███╗██║   ██║   ██║      ██║    ╚████╔╝ 
 ██║   ██║██║   ██║   ██║      ██║     ╚██╔╝  
 ╚██████╔╝╚██████╔╝   ██║      ██║      ██║   
  ╚═════╝  ╚═════╝    ╚═╝      ╚═╝      ╚═╝   
        QUICKCHAT  1-TECLA  v1.4.0");
        Console.ResetColor();
        Console.WriteLine();
    }
}
