using System.Text;

namespace GuttyRL;

/// <summary>Forca otimizacoes de FPS nas secoes [SystemSettings*] do CRIADOR.
/// FPS ilimitado e boot-safe em TODAS as secoes (incluindo a principal).
/// Efeitos pesados so nas secoes derivadas — a principal fica para o visual do criador.</summary>
internal static class CriadorForce
{
    // Boot + FPS: forcar em TODAS as secoes SystemSettings* (menu/engine leem isto).
    private static readonly Dictionary<string, string> EverywhereKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["OnlyStreamInTextures"] = "False",
        ["WaitForGPU"] = "False",
        ["UncappedFramerate"] = "True",
        ["bSmoothFrameRate"] = "False",
        ["CustomFPS"] = "0",
        ["UseVsync"] = "False",
        ["OneFrameThreadLag"] = "True",
        ["AllowPerFrameSleep"] = "True",
        ["AllowPerFrameYield"] = "True",
    };

    // Sombras dinamicas (DynamicLights/Shadows/Composite) ficam no template principal —
    // nao forcar OFF nos perfis derivados (senao o menu liga e o engine nao desenha sombra).
    private static readonly Dictionary<string, string> ChildPerfKeys = new(StringComparer.OrdinalIgnoreCase)
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
        ["MobileHeightFog"] = "False",
        ["MobileMinimizeFogShaders"] = "TRUE",
        ["MobileSpecular"] = "False",
        ["MobileLightShaftScale"] = "0",
        ["MobileLightShaftFirstPass"] = "0",
        ["MobileLightShaftSecondPass"] = "0",
    };

    public static string Apply(string iniText)
    {
        var sb = new StringBuilder();
        bool inSs = false;
        bool inChild = false;
        var pendingEverywhere = new HashSet<string>(EverywhereKeys.Keys, StringComparer.OrdinalIgnoreCase);

        foreach (var raw in iniText.Replace("\r\n", "\n").Split('\n'))
        {
            string line = raw;
            if (line.StartsWith('['))
            {
                if (inSs && pendingEverywhere.Count > 0)
                {
                    foreach (string pendingKey in pendingEverywhere)
                        sb.Append(pendingKey).Append('=').Append(EverywhereKeys[pendingKey]).Append("\r\n");
                }

                inSs = line.StartsWith("[SystemSettings", StringComparison.OrdinalIgnoreCase);
                inChild = IsChildSystemSettings(line);
                pendingEverywhere = inSs
                    ? new HashSet<string>(EverywhereKeys.Keys, StringComparer.OrdinalIgnoreCase)
                    : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                sb.Append(line).Append("\r\n");
                continue;
            }

            if (!inSs || string.IsNullOrWhiteSpace(line) || line.StartsWith(';'))
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
            if (EverywhereKeys.TryGetValue(key, out string? everywhereVal))
            {
                sb.Append(key).Append('=').Append(everywhereVal).Append("\r\n");
                pendingEverywhere.Remove(key);
                continue;
            }

            if (inChild && ChildPerfKeys.TryGetValue(key, out string? val))
            {
                sb.Append(key).Append('=').Append(val).Append("\r\n");
                continue;
            }

            sb.Append(line).Append("\r\n");
        }

        if (inSs && pendingEverywhere.Count > 0)
        {
            foreach (string pendingKey in pendingEverywhere)
                sb.Append(pendingKey).Append('=').Append(EverywhereKeys[pendingKey]).Append("\r\n");
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
