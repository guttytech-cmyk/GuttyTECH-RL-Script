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
        if (CompletoForce.HasDrift(forcedC))
        {
            Console.WriteLine("[X] COMPLETO+Force ainda com drift apos Apply: "
                              + string.Join("; ", CompletoForce.DescribeDrift(forcedC).Take(5)));
            fails++;
        }
        fails += CheckFile("CRIADOR+Force", forcedR, CriadorPostForceRules);
        fails += CheckDuplicateKeys("CRIADOR", Templates.Criador, "SystemSettings", "MobileMinimizeFogShaders");
        fails += CheckExclusiveDisplay("COMPLETO", Templates.Completo);
        fails += CheckExclusiveDisplay("CRIADOR", Templates.Criador);

        return fails == 0 ? 0 : 1;
    }

    private static readonly Rule[] CompletoRules =
    {
        new("SystemSettings*", "DetailMode", "^[12]$", "DetailMode deve ser 0"),
        new("SystemSettings*", "bAllowLightShafts", "^(?i)true$", "raios de luz ligados"),
        new("SystemSettings*", "MobileEnableMSAA", "^(?i)true$", "MSAA mobile ligado"),
        new("SystemSettings*", "AllowImageReflections", "^(?i)true$", "reflexos ligados"),
        new("SystemSettingsMobile", "DynamicDecals", "^(?i)true$", "decals dinamicos ligados no mobile"),
        new("SystemSettings*", "MobileFog", "^(?i)true$", "nevoa/clima ligado"),
        new("SystemSettings*", "OnlyStreamInTextures", "^(?i)true$", "boot hang (OnlyStreamInTextures)"),
        new("SystemSettings*", "WaitForGPU", "^(?i)true$", "boot hang (WaitForGPU)"),
        new("SystemSettings*", "OneFrameThreadLag", "^(?i)false$", "frame pacing perigoso"),
        new("SystemSettings*", "AllowPerFrameSleep", "^(?i)false$", "frame pacing perigoso"),
        new("SystemSettings*", "AllowPerFrameYield", "^(?i)false$", "frame pacing perigoso"),
        new("SystemSettings*", "UncappedFramerate", "^(?i)false$", "FPS capped no COMPLETO"),
        new("SystemSettings*", "bSmoothFrameRate", "^(?i)true$", "smooth framerate ligado"),
        new("SystemSettings", "ScreenPercentage", "^(?!100(\\.0+)?$).*$", "ScreenPercentage fora de 100 (borda preta)"),
    };

    private static readonly Rule[] CriadorRules =
    {
        new("SystemSettings", "DynamicDecals", "^(?i)false$", "marcas de pneu desligadas"),
        new("SystemSettings", "AllowImageReflections", "^(?i)false$", "reflexos desligados no perfil criador"),
        new("SystemSettings", "bAllowHighQualityMaterials", "^(?i)false$", "materiais HQ desligados"),
        new("SystemSettings", "DynamicShadows", "^(?i)false$", "sombras dinamicas desligadas no criador"),
        new("SystemSettings", "DynamicLights", "^(?i)false$", "luzes dinamicas desligadas (sombras precisam delas)"),
        new("SystemSettings", "MotionBlur", "^(?i)true$", "motion blur ligado"),
        new("SystemSettings", "MobileFog", "^(?i)true$", "efeitos de clima ligados"),
        new("SystemSettings", "UncappedFramerate", "^(?i)false$", "FPS capped no template"),
        new("SystemSettings*", "OnlyStreamInTextures", "^(?i)true$", "boot hang (OnlyStreamInTextures)"),
        new("SystemSettings*", "WaitForGPU", "^(?i)true$", "boot hang (WaitForGPU)"),
    };

    private static readonly Rule[] CompletoPostForceRules =
    {
        new("SystemSettings*", "bAllowLightShafts", "^(?i)true$", "pos-force ainda com light shafts"),
        new("SystemSettings*", "MobileEnableMSAA", "^(?i)true$", "pos-force ainda com MSAA"),
        new("SystemSettings*", "OnlyStreamInTextures", "^(?i)true$", "pos-force OnlyStreamInTextures"),
        new("SystemSettings*", "WaitForGPU", "^(?i)true$", "pos-force WaitForGPU"),
        new("SystemSettings*", "ParticleLODBias", "^(?!100$).*$", "pos-force ParticleLODBias != 100"),
        new("SystemSettings*", "bUseTranslucentArenaShaders", "^(?i)true$", "pos-force shaders HQ"),
        new("SystemSettings*", "MobileFog", "^(?i)true$", "pos-force MobileFog"),
        new("SystemSettings", "ScreenPercentage", "^(?!100(\\.0+)?$).*$", "pos-force ScreenPercentage"),
        new("SystemSettings*", "TEXTUREGROUP_*", "MaxLODSize=(?!2(?:,|\\)))", "textura acima de 2x2"),
        new("SystemSettings*", "MaxShadowResolution", "^(?!1$).*$", "shadow map acima de 1x1"),
        new("SystemSettings*", "MaxFilterBlurSampleCount", "^(?!1$).*$", "BlurSamples deve ficar em 1 (0 crasha)"),
        new("SystemSettings*", "DynamicLights", "^(?i)true$", "luzes dinamicas ligadas"),
        new("SystemSettings*", "DynamicShadows", "^(?i)true$", "sombras dinamicas ligadas"),
        new("SystemSettings", "DynamicDecals", "^(?i)false$", "indicador/sombra da bola (DynamicDecals) off"),
        new("SystemSettings", "UnbatchedDecals", "^(?i)false$", "sombra da bola nos cantos (UnbatchedDecals) off"),
        new("SystemSettings", "bEnableForegroundShadowsOnWorld", "^(?i)false$", "foreground shadow da bola off"),
        new("SystemSettings*", "ApexLODResourceBudget", "^(?!0(?:\\.0+)?$).*$", "APEX budget diferente de zero"),
        new("SystemSettings*", "MobileNormalMapping", "^(?i)true$", "normal mapping mobile ligado"),
    };

    private static readonly Rule[] CriadorPostForceRules =
    {
        new("SystemSettingsIPhone*", "bAllowLightShafts", "^(?i)true$", "filhos ainda com light shafts"),
        new("SystemSettingsIPad*", "bAllowLightShafts", "^(?i)true$", "filhos ainda com light shafts"),
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

    private static int CheckDuplicateKeys(string name, string ini, string sectionName, string keyName)
    {
        string section = "";
        int count = 0;
        foreach (var raw in ini.Replace("\r\n", "\n").Split('\n'))
        {
            if (raw.StartsWith('[') && raw.EndsWith(']'))
            {
                section = raw[1..^1];
                continue;
            }
            if (!section.Equals(sectionName, StringComparison.OrdinalIgnoreCase)) continue;
            int eq = raw.IndexOf('=');
            if (eq <= 0) continue;
            if (raw[..eq].Equals(keyName, StringComparison.OrdinalIgnoreCase))
                count++;
        }
        if (count > 1)
        {
            Console.WriteLine($"[X] {name} [{sectionName}] {keyName} duplicado {count}x (last-wins)");
            return 1;
        }
        return 0;
    }

    private static int CheckExclusiveDisplay(string name, string ini)
    {
        string section = "";
        string? fs = null, bl = null;
        foreach (var raw in ini.Replace("\r\n", "\n").Split('\n'))
        {
            if (raw.StartsWith('[') && raw.EndsWith(']'))
            {
                section = raw[1..^1];
                continue;
            }
            if (!section.Equals("SystemSettings", StringComparison.OrdinalIgnoreCase)) continue;
            int eq = raw.IndexOf('=');
            if (eq <= 0) continue;
            string key = raw[..eq];
            string val = raw[(eq + 1)..];
            if (key.Equals("Fullscreen", StringComparison.OrdinalIgnoreCase)) fs = val;
            if (key.Equals("Borderless", StringComparison.OrdinalIgnoreCase)) bl = val;
        }
        if (string.Equals(fs, "True", StringComparison.OrdinalIgnoreCase)
            && string.Equals(bl, "True", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[X] {name} Fullscreen=True e Borderless=True ao mesmo tempo");
            return 1;
        }
        return 0;
    }

    private static bool SectionMatch(string section, string pattern) =>
        pattern.EndsWith('*')
            ? section.StartsWith(pattern[..^1], StringComparison.OrdinalIgnoreCase)
            : section.Equals(pattern, StringComparison.OrdinalIgnoreCase);

    private static bool WildcardMatch(string text, string pattern) =>
        pattern.EndsWith('*') && text.StartsWith(pattern[..^1], StringComparison.OrdinalIgnoreCase);
}
