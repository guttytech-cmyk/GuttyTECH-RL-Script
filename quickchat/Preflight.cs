namespace GuttyQuickChat;

internal static class Preflight
{
    public static void Run(QuickChatConfig config)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  Config : {AppMeta.ConfigPath}");
        Console.WriteLine($"  Log    : {AppMeta.LogFile}");
        Console.ResetColor();

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("  Atalhos (1 tecla = 1 frase):");
        Console.ResetColor();
        foreach (var (key, phrase) in config.DirectBinds.OrderBy(static kv => kv.Key, StringComparer.OrdinalIgnoreCase))
        {
            Console.Write("    ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"{DescribeKey(key),-6}");
            Console.ResetColor();
            Console.WriteLine($" {phrase}");
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  Chat partida: {config.Bindings.Chat}  |  Chat time: {config.Bindings.TeamChat} (Ctrl+tecla)");
        Console.ResetColor();
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("  COMO USAR:");
        Console.ResetColor();
        Console.WriteLine("  1. Minimize esta janela e clique no Rocket League.");
        Console.WriteLine("  2. Pressione UMA tecla: 1 a 9  -> chat GERAL (T)");
        Console.WriteLine("  3. Segure Ctrl + tecla         -> chat do TIME (Y)");
        Console.WriteLine("  [!] Desative Quick Chat nativo (teclas 1-4) nas configs do RL.");
        Console.WriteLine();
    }

    private static string DescribeKey(string key) => key switch
    {
        "One" or "D1" => "1",
        "Two" or "D2" => "2",
        "Three" or "D3" => "3",
        "Four" or "D4" => "4",
        "Five" or "D5" => "5",
        "Six" or "D6" => "6",
        "Seven" or "D7" => "7",
        "Eight" or "D8" => "8",
        "Nine" or "D9" => "9",
        _ => key
    };
}
