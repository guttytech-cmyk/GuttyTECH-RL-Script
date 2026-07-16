using System.Text.Json;
using System.Text.Json.Serialization;

namespace GuttyQuickChat;

internal sealed class QuickChatConfig
{
    public string Version { get; set; } = AppMeta.Version;
    public int TypingDelayMs { get; set; } = 0;
    public int SendCooldownMs { get; set; } = 150;
    public string TeamChatModifier { get; set; } = "LeftControl";
    public bool SwallowKeys { get; set; } = true;
    public bool RequireRocketLeagueFocus { get; set; } = true;
    public bool ReadBindingsFromGame { get; set; } = false;
    public ChatBindings Bindings { get; set; } = new();
    public Dictionary<string, string> DirectBinds { get; set; } = new();
    public Dictionary<string, ChatCategory> Categories { get; set; } = new();

    public static QuickChatConfig CreateDefault()
    {
        var cfg = new QuickChatConfig
        {
            Categories = new Dictionary<string, ChatCategory>
            {
                ["1"] = new()
                {
                    Label = "Tático",
                    Phrases = new Dictionary<string, string>
                    {
                        ["1"] = "Eu vou na bola!",
                        ["2"] = "to indo meu mano",
                        ["3"] = "Toma!",
                        ["4"] = "Centro!"
                    }
                },
                ["2"] = new()
                {
                    Label = "Informação",
                    Phrases = new Dictionary<string, string>
                    {
                        ["1"] = "Defendendo!",
                        ["2"] = "Preciso de boost!",
                        ["3"] = "Tomando o gol!",
                        ["4"] = "Um pra mim!"
                    }
                },
                ["3"] = new()
                {
                    Label = "Elogios",
                    Phrases = new Dictionary<string, string>
                    {
                        ["1"] = "Boa!",
                        ["2"] = "Ótimo passe!",
                        ["3"] = "Que jogada!",
                        ["4"] = "Salvou!"
                    }
                },
                ["4"] = new()
                {
                    Label = "Reações",
                    Phrases = new Dictionary<string, string>
                    {
                        ["1"] = "Desculpa!",
                        ["2"] = "Noooo!",
                        ["3"] = "Calma aí!",
                        ["4"] = "F!"
                    }
                }
            }
        };
        cfg.EnsureDirectBinds();
        return cfg;
    }

    public static QuickChatConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            var cfg = CreateDefault();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(cfg, QuickChatJsonContext.Default.QuickChatConfig));
            AppMeta.Log($"Config criado: {path}");
            return cfg;
        }

        var json = File.ReadAllText(path);
        var cfg2 = JsonSerializer.Deserialize(json, QuickChatJsonContext.Default.QuickChatConfig) ?? CreateDefault();
        cfg2.EnsureDirectBinds();
        return cfg2;
    }

    public void EnsureDirectBinds()
    {
        if (DirectBinds.Count > 0)
            return;
        RebuildDirectBinds1to9();
    }

    public void RebuildDirectBinds1to9()
    {
        DirectBinds.Clear();
        var slots = new[] { "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8", "D9" };
        var i = 0;
        foreach (var catKey in Categories.Keys.OrderBy(static k => k, StringComparer.Ordinal))
        {
            if (!Categories.TryGetValue(catKey, out var cat))
                continue;
            foreach (var phraseKey in cat.Phrases.Keys.OrderBy(static k => k, StringComparer.Ordinal))
            {
                if (i >= slots.Length)
                    return;
                if (cat.Phrases.TryGetValue(phraseKey, out var phrase))
                    DirectBinds[slots[i++]] = phrase;
            }
        }
    }

    public void ApplyBindings(TAInputBindings detected)
    {
        if (!ReadBindingsFromGame)
            return;

        if (!string.IsNullOrEmpty(detected.PcChat))
            Bindings.Chat = detected.PcChat;
        if (!string.IsNullOrEmpty(detected.PcTeamChat))
            Bindings.TeamChat = detected.PcTeamChat;
    }
}

internal sealed class ChatBindings
{
    public string Chat { get; set; } = "T";
    public string TeamChat { get; set; } = "Y";
}

internal sealed class ChatCategory
{
    public string Label { get; set; } = "";
    public Dictionary<string, string> Phrases { get; set; } = new();
}

[JsonSerializable(typeof(QuickChatConfig))]
[JsonSerializable(typeof(ChatBindings))]
[JsonSerializable(typeof(ChatCategory))]
[JsonSerializable(typeof(Dictionary<string, ChatCategory>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class QuickChatJsonContext : JsonSerializerContext;
