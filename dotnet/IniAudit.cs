using System.Text.RegularExpressions;

namespace GuttyRL;

/// <summary>Validacao estatica dos templates (rodar: GuttyTECH_RL.exe AUDIT).</summary>
internal static class IniAudit
{
    private sealed record Rule(string? SectionLike, string KeyLike, string ForbiddenPattern, string Message);

    public static int Run()
    {
        int fails = 0;
        fails += CheckFile("COMPLETO", Templates.Completo, CompletoRules);
        fails += CheckFile("CRIADOR", Templates.Criador, CriadorRules);

        string forcedC = CompletoForce.Apply(Templates.Completo);
        string forcedR = CriadorForce.Apply(Templates.Criador);
        fails += CheckFile("COMPLETO+Force", forcedC, CompletoPostForceRules);
        fails += CheckFile("CRIADOR+Force", forcedR, CriadorPostForceRules);

        return fails == 0 ? 0 : 1;
    }

    private static readonly Rule[] CompletoRules =
    {
        new("SystemSettings*", "DetailMode", "^[12]$", "DetailMode deve ser 0"),
        new("SystemSettings*", "bAllowLightShafts", "^(?i)true$", "raios de luz ligados"),
        new("SystemSettings*", "MobileEnableMSAA", "^(?i)true$", "MSAA mobile ligado"),
        new("SystemSettings*", "AllowImageReflections", "^(?i)true$", "reflexos ligados"),
        new("SystemSettingsMobile", "DynamicDecals", "^(?i)true$", "decals dinamicos ligados no mobile"),
        new("SystemSettings", "MobileFog", "^(?i)true$", "nevoa/clima ligado"),
    };

    private static readonly Rule[] CriadorRules =
    {
        new("SystemSettings", "DynamicDecals", "^(?i)false$", "marcas de pneu desligadas"),
        new("SystemSettings", "AllowImageReflections", "^(?i)false$", "reflexos desligados no perfil criador"),
        new("SystemSettings", "bAllowHighQualityMaterials", "^(?i)false$", "materiais HQ desligados"),
        new("SystemSettings", "DynamicShadows", "^(?i)true$", "sombras dinamicas ligadas"),
        new("SystemSettings", "MotionBlur", "^(?i)true$", "motion blur ligado"),
        new("SystemSettings", "MobileFog", "^(?i)true$", "efeitos de clima ligados"),
        new("SystemSettings", "UncappedFramerate", "^(?i)false$", "FPS capped no template"),
    };

    private static readonly Rule[] CompletoPostForceRules =
    {
        new("SystemSettings*", "bAllowLightShafts", "^(?i)true$", "pos-force ainda com light shafts"),
        new("SystemSettings*", "MobileEnableMSAA", "^(?i)true$", "pos-force ainda com MSAA"),
    };

    private static readonly Rule[] CriadorPostForceRules =
    {
        new("SystemSettingsIPhone*", "bAllowLightShafts", "^(?i)true$", "filhos ainda com light shafts"),
        new("SystemSettingsIPad*", "bAllowLightShafts", "^(?i)true$", "filhos ainda com light shafts"),
        new("SystemSettingsFlash", "DynamicShadows", "^(?i)true$", "Flash ainda com sombras"),
    };

    private static int CheckFile(string name, string ini, Rule[] rules)
    {
        int fails = 0;
        string section = "";
        foreach (var raw in ini.Replace("\r\n", "\n").Split('\n'))
        {
            if (raw.StartsWith('[') && raw.EndsWith(']'))
            {
                section = raw[1..^1];
                continue;
            }
            int eq = raw.IndexOf('=');
            if (eq <= 0) continue;
            string key = raw[..eq];
            string val = raw[(eq + 1)..];

            foreach (var rule in rules)
            {
                if (rule.SectionLike != null && !SectionMatch(section, rule.SectionLike)) continue;
                if (!key.Equals(rule.KeyLike, StringComparison.OrdinalIgnoreCase)
                    && !WildcardMatch(key, rule.KeyLike)) continue;
                if (Regex.IsMatch(val, rule.ForbiddenPattern))
                {
                    Console.WriteLine($"[X] {name} [{section}] {key}={val} ({rule.Message})");
                    fails++;
                }
            }
        }
        return fails;
    }

    private static bool SectionMatch(string section, string pattern) =>
        pattern.EndsWith('*')
            ? section.StartsWith(pattern[..^1], StringComparison.OrdinalIgnoreCase)
            : section.Equals(pattern, StringComparison.OrdinalIgnoreCase);

    private static bool WildcardMatch(string text, string pattern) =>
        pattern.EndsWith('*') && text.StartsWith(pattern[..^1], StringComparison.OrdinalIgnoreCase);
}
