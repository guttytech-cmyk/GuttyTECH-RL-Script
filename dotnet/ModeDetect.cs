namespace GuttyRL;

/// <summary>
/// Deteta COMPLETO/CRIADOR mesmo quando o RL apaga GuttyTechMode= do INI.
/// Ordem: marcador INI → fingerprint das keys → tag local do Gutty.
/// </summary>
internal static class ModeDetect
{
    private static string TagPath => Path.Combine(AppMeta.GuttyDir, "applied-mode.tag");

    public static string? Detect(string? iniPath)
    {
        string? fromIni = DetectFromIni(iniPath);
        if (fromIni is not null)
        {
            Persist(fromIni);
            return fromIni;
        }

        string? fromTag = ReadTag();
        if (fromTag is "COMPLETO" or "CRIADOR")
            return fromTag;

        return null;
    }

    public static void Persist(string mode)
    {
        if (mode is not ("COMPLETO" or "CRIADOR"))
            return;
        try
        {
            Directory.CreateDirectory(AppMeta.GuttyDir);
            File.WriteAllText(TagPath, mode + Environment.NewLine);
        }
        catch { }
    }

    public static void Clear()
    {
        try
        {
            if (File.Exists(TagPath))
                File.Delete(TagPath);
        }
        catch { }
    }

    private static string? ReadTag()
    {
        try
        {
            if (!File.Exists(TagPath)) return null;
            string t = File.ReadAllText(TagPath).Trim().ToUpperInvariant();
            return t is "COMPLETO" or "CRIADOR" ? t : null;
        }
        catch { return null; }
    }

    private static string? DetectFromIni(string? iniPath)
    {
        if (string.IsNullOrWhiteSpace(iniPath) || !File.Exists(iniPath))
            return null;

        string text;
        try { text = File.ReadAllText(iniPath); }
        catch { return null; }

        if (text.Contains("GuttyTechMode=COMPLETO", StringComparison.OrdinalIgnoreCase)
            || text.Contains("GUTTYTECH-RL-OPTIMIZER=COMPLETO", StringComparison.Ordinal))
            return "COMPLETO";

        if (text.Contains("GuttyTechMode=CRIADOR", StringComparison.OrdinalIgnoreCase)
            || text.Contains("GUTTYTECH-RL-OPTIMIZER=CRIADOR", StringComparison.Ordinal))
            return "CRIADOR";

        var main = ReadMainSystemSettings(text);
        if (LooksLikeCompleto(main, text))
            return "COMPLETO";
        if (LooksLikeCriador(main, text))
            return "CRIADOR";

        return null;
    }

    /// <summary>Assinatura COMPLETO que sobrevive ao soft-rewrite do RL (TG pode afrouxar).</summary>
    private static bool LooksLikeCompleto(Dictionary<string, string> main, string fullText)
    {
        int score = 0;
        if (Eq(main, "DynamicShadows", "False")) score++;
        if (Eq(main, "DynamicLights", "False")) score++;
        if (Eq(main, "MaxShadowResolution", "1")) score++;
        if (Eq(main, "UncappedFramerate", "True")) score++;
        if (Eq(main, "MaxFilterBlurSampleCount", "1") || Eq(main, "MaxFilterBlurSampleCount", "2")) score++;
        if (Eq(main, "bAllowLightShafts", "False")) score++;

        // Potato textures: forte, mas o RL pode afrouxar a meio da sessao.
        bool potatoTg = fullText.IndexOf("MaxLODSize=2", StringComparison.OrdinalIgnoreCase) >= 0
                        && fullText.IndexOf("LODBias=100", StringComparison.OrdinalIgnoreCase) >= 0;
        if (potatoTg) score += 2;

        // Precisa do nucleo potato (sombras/lights) + pacing; 5+ evita falso positivo stock.
        return score >= 5
               && Eq(main, "DynamicShadows", "False")
               && Eq(main, "DynamicLights", "False");
    }

    /// <summary>CRIADOR: FPS cuts sem potato de sombra/textura.</summary>
    private static bool LooksLikeCriador(Dictionary<string, string> main, string fullText)
    {
        if (!Eq(main, "UncappedFramerate", "True")) return false;
        if (!(Eq(main, "MaxFilterBlurSampleCount", "1") || Eq(main, "MaxFilterBlurSampleCount", "2"))) return false;
        if (!Eq(main, "UseVsync", "False")) return false;

        // Nao e COMPLETO potato.
        if (Eq(main, "DynamicShadows", "False") && Eq(main, "DynamicLights", "False")
            && Eq(main, "MaxShadowResolution", "1"))
            return false;

        bool apexOff = Eq(main, "AllowApexCloth", "False")
                       || (main.TryGetValue("ApexLODResourceBudget", out string? apex)
                           && apex.StartsWith("0", StringComparison.Ordinal));
        bool aaOff = Eq(main, "bAllowTemporalAA", "False") || Eq(main, "MaxMultiSamples", "0");
        bool foliageOff = Eq(main, "SpeedTreeLeaves", "False") || Eq(main, "FoliageDrawRadiusMultiplier", "0.000000")
                          || fullText.Contains("FoliageDrawRadiusMultiplier=0", StringComparison.OrdinalIgnoreCase);

        int score = 0;
        if (apexOff) score++;
        if (aaOff) score++;
        if (foliageOff) score++;
        if (Eq(main, "bSmoothFrameRate", "False")) score++;

        return score >= 3;
    }

    private static bool Eq(Dictionary<string, string> map, string key, string expected) =>
        map.TryGetValue(key, out string? got) && got.Equals(expected, StringComparison.OrdinalIgnoreCase);

    private static Dictionary<string, string> ReadMainSystemSettings(string iniText)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        bool inMain = false;
        foreach (string raw in iniText.Replace("\r\n", "\n").Split('\n'))
        {
            if (raw.StartsWith('['))
            {
                inMain = raw.Equals("[SystemSettings]", StringComparison.OrdinalIgnoreCase);
                continue;
            }
            if (!inMain) continue;
            int eq = raw.IndexOf('=');
            if (eq <= 0) continue;
            map[raw[..eq].Trim()] = raw[(eq + 1)..].Trim();
        }
        return map;
    }
}
