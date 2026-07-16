using System.Text;

namespace GuttyRL;

/// <summary>Forca valores potato em TODAS as secoes SystemSettings* do COMPLETO
/// (o jogo le perfis derivados e o menu in-game reflete chaves espalhadas).</summary>
internal static class CompletoForce
{
    private const string PotatoTextureGroup =
        "(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)";

    private static readonly Dictionary<string, string> Keys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["DetailMode"] = "0",
        ["ParticleLODBias"] = "100",
        ["SkeletalMeshLODBias"] = "100",
        ["MaxDrawDistanceScale"] = "0",
        ["MaxAnisotropy"] = "0",
        ["MaxMultiSamples"] = "0",
        ["FullEffectIntensity"] = "False",
        ["bAllowHighQualityMaterials"] = "False",
        ["bUseTranslucentArenaShaders"] = "False",
        ["AmbientOcclusion"] = "False",
        ["DepthOfField"] = "False",
        ["Bloom"] = "False",
        ["bAllowLightShafts"] = "False",
        ["LensFlares"] = "False",
        ["DynamicShadows"] = "False",
        ["LightEnvironmentShadows"] = "False",
        ["CompositeDynamicLights"] = "False",
        ["DynamicLights"] = "False",
        ["SHSecondaryLighting"] = "False",
        ["MotionBlur"] = "False",
        ["MotionBlurPause"] = "False",
        ["MotionBlurSkinning"] = "0",
        ["FogVolumes"] = "False",
        ["SpeedTreeLeaves"] = "False",
        ["SpeedTreeFronds"] = "False",
        ["bAllowD3D9MSAA"] = "False",
        ["bAllowTemporalAA"] = "False",
        ["bAllowPostprocessMLAA"] = "False",
        ["MobileFXAAQuality"] = "0",
        ["Distortion"] = "False",
        ["FilteredDistortion"] = "False",
        ["DropParticleDistortion"] = "False",
        ["AllowRadialBlur"] = "False",
        ["AllowSubsurfaceScattering"] = "False",
        ["AllowImageReflections"] = "False",
        ["AllowImageReflectionShadowing"] = "False",
        ["AllowApexCloth"] = "False",
        ["bAllowSeparateTranslucency"] = "False",
        ["FloatingPointRenderTargets"] = "False",
        ["MobileFog"] = "False",
        ["MobileSpecular"] = "False",
        ["MobileEnableMSAA"] = "False",
        ["MobileModShadows"] = "False",
        ["MobileMinimizeFogShaders"] = "TRUE",
        ["MobileLightShaftScale"] = "0",
        ["MobileLightShaftFirstPass"] = "0",
        ["MobileLightShaftSecondPass"] = "0",
        ["TemporalAA_MinDepth"] = "0.000000",
        ["TemporalAA_StartDepthVelocityScale"] = "0.000000",
    };

    public static string Apply(string iniText)
    {
        var sb = new StringBuilder();
        bool forceSection = false;

        foreach (var raw in iniText.Replace("\r\n", "\n").Split('\n'))
        {
            string line = raw;
            if (line.StartsWith('['))
            {
                forceSection = IsForceSection(line);
                sb.Append(line).Append("\r\n");
                continue;
            }

            if (!forceSection || string.IsNullOrWhiteSpace(line) || line.StartsWith(';'))
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
            if (key.StartsWith("TEXTUREGROUP_", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(key).Append('=').Append(PotatoTextureGroup).Append("\r\n");
                continue;
            }

            if (Keys.TryGetValue(key, out string? val))
            {
                sb.Append(key).Append('=').Append(val).Append("\r\n");
                continue;
            }

            sb.Append(line).Append("\r\n");
        }

        return sb.ToString();
    }

    private static bool IsForceSection(string header) =>
        header.StartsWith("[SystemSettings", StringComparison.OrdinalIgnoreCase);
}
