namespace GuttyQuickChat;

internal sealed class TAInputBindings
{
    public string? PcChat { get; set; }
    public string? PcTeamChat { get; set; }
}

internal static class TAInputReader
{
    public static TAInputBindings Read(string iniPath)
    {
        var result = new TAInputBindings();
        if (!File.Exists(iniPath))
            return result;

        foreach (var raw in File.ReadLines(iniPath))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('['))
                continue;

            var eq = line.IndexOf('=');
            if (eq <= 0)
                continue;

            var key = line[..eq].Trim();
            var value = line[(eq + 1)..].Trim();
            if (value.Length == 0)
                continue;

            switch (key)
            {
                case "PCBindings.Chat":
                    result.PcChat = NormalizeKey(value);
                    break;
                case "PCBindings.TeamChat":
                    result.PcTeamChat = NormalizeKey(value);
                    break;
            }
        }

        return result;
    }

    private static string NormalizeKey(string ueKey) => ueKey switch
    {
        "One" => "D1",
        "Two" => "D2",
        "Three" => "D3",
        "Four" => "D4",
        "Five" => "D5",
        "Six" => "D6",
        "Seven" => "D7",
        "Eight" => "D8",
        "Nine" => "D9",
        _ => ueKey.Length == 1 ? ueKey.ToUpperInvariant() : ueKey
    };
}
