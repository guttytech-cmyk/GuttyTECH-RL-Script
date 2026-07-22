using System.Text;

namespace GuttyRL;

/// <summary>Forca otimizacoes de FPS nas secoes derivadas [SystemSettings*] do CRIADOR,
/// sem mexer em [SystemSettings] (onde o usuario ajusta visual no jogo).</summary>
internal static class CriadorForce
{
    // Sombras dinamicas (DynamicLights/Shadows/Composite) ficam no template principal —
    // nao forcar OFF nos perfis derivados (senao o menu liga e o engine nao desenha sombra).
    private static readonly Dictionary<string, string> PerfKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SHSecondaryLighting"] = "False",
        ["MotionBlur"] = "False",
        ["MotionBlurPause"] = "False",
        ["MotionBlurSkinning"] = "0",
        ["DepthOfField"] = "False",
        ["AmbientOcclusion"] = "False",
        ["Bloom"] = "False",
        ["bAllowLightShafts"] = "False",
        ["LensFlares"] = "False",
        ["FogVolumes"] = "False",
        ["Distortion"] = "False",
        ["FilteredDistortion"] = "False",
        ["DropParticleDistortion"] = "False",
        ["AllowRadialBlur"] = "False",
        ["bAllowD3D9MSAA"] = "False",
        ["bAllowTemporalAA"] = "False",
        ["bAllowPostprocessMLAA"] = "False",
        ["MobileFXAAQuality"] = "0",
        ["MobileEnableMSAA"] = "False",
        ["MobileModShadows"] = "False",
        ["MobileFog"] = "False",
        ["MobileSpecular"] = "False",
        ["MobileLightShaftScale"] = "0",
        ["MobileLightShaftFirstPass"] = "0",
        ["MobileLightShaftSecondPass"] = "0",
    };

    public static string Apply(string iniText)
    {
        var sb = new StringBuilder();
        bool inChild = false;

        foreach (var raw in iniText.Replace("\r\n", "\n").Split('\n'))
        {
            string line = raw;
            if (line.StartsWith('['))
            {
                inChild = IsChildSystemSettings(line);
                sb.Append(line).Append("\r\n");
                continue;
            }

            if (!inChild || string.IsNullOrWhiteSpace(line) || line.StartsWith(';'))
            {
                sb.Append(line).Append("\r\n");
                continue;
            }

            int eq = line.IndexOf('=');
            if (eq <= 0)
            {
                sb.Append(line).Append("\r\n");
                continue;
            }

            string key = line[..eq];
            if (PerfKeys.TryGetValue(key, out string? val))
            {
                sb.Append(key).Append('=').Append(val).Append("\r\n");
                continue;
            }

            sb.Append(line).Append("\r\n");
        }

        return sb.ToString();
    }

    private static bool IsChildSystemSettings(string header)
    {
        if (!header.StartsWith("[SystemSettings", StringComparison.OrdinalIgnoreCase))
            return false;
        return !header.Equals("[SystemSettings]", StringComparison.OrdinalIgnoreCase);
    }
}
