using System.Text;

namespace GuttyRL;

/// <summary>Forca otimizacoes de FPS nas secoes [SystemSettings*] do CRIADOR.
/// FPS ilimitado e boot-safe em TODAS as secoes (incluindo a principal).
/// Extra FPS "invisivel" (Apex/foliage/tessellation/MSAA) tambem no main —
/// sem potato de textura/sombra (isso fica no COMPLETO).</summary>
internal static class CriadorForce
{
    // Boot + FPS + cortes sem impacto visual relevante — TODAS as secoes.
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
        ["AllowDynamicResolution"] = "False",
        ["ScreenPercentage"] = "100.000000",
        ["UpscaleScreenPercentage"] = "True",
        ["MinimumScreenScale"] = "100.000000",

        // Ganho FPS validado no COMPLETO, sem batata visual:
        ["MaxMultiSamples"] = "0",
        ["bAllowD3D9MSAA"] = "False",
        ["bAllowTemporalAA"] = "False",
        ["bAllowPostprocessMLAA"] = "False",
        ["MobileFXAAQuality"] = "0",
        ["MobileEnableMSAA"] = "False",
        // BlurSamples=0 crasha; 1 = aprovado (mesmo do COMPLETO).
        ["MaxFilterBlurSampleCount"] = "1",
        ["AllowApexCloth"] = "False",
        ["ApexLODResourceBudget"] = "0.000000",
        ["ApexGRBEnable"] = "False",
        ["ApexDestructionMaxChunkIslandCount"] = "0",
        ["ApexDestructionMaxShapeCount"] = "0",
        ["bAllowFracturedDamage"] = "False",
        ["NumFracturedPartsScale"] = "0.000000",
        ["FractureDirectSpawnChanceScale"] = "0.000000",
        ["FractureRadialSpawnChanceScale"] = "0.000000",
        ["FractureCullDistanceScale"] = "0.000000",
        ["SpeedTreeLeaves"] = "False",
        ["SpeedTreeFronds"] = "False",
        ["FoliageDrawRadiusMultiplier"] = "0.000000",
        ["TessellationAdaptivePixelsPerTriangle"] = "4096.000000",
        ["SceneCaptureStreamingMultiplier"] = "0.000000",
        ["AllowSubsurfaceScattering"] = "False",
        ["bAllowSeparateTranslucency"] = "False",
        ["HighPrecisionGBuffers"] = "False",
        ["TemporalAA_MinDepth"] = "0.000000",
        ["TemporalAA_StartDepthVelocityScale"] = "0.000000",
        ["AllowRadialBlur"] = "False",
        // Cantos/laterais: sombra/indicador da bola (v22.3). Nao forcar False.
        ["UnbatchedDecals"] = "True",
        ["DynamicDecals"] = "True",
        ["DecalCullDistanceScale"] = "1.000000",
        ["bEnableForegroundShadowsOnWorld"] = "True",
    };

    // Efeitos pesados so nas secoes derivadas — a principal fica para o visual do criador
    // (sombras dinamicas / materiais HQ / texturas ficam no template).
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
        ["DropParticleDistortion"] = "True",
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
        ["MobileBumpOffset"] = "False",
        ["MobileNormalMapping"] = "False",
        ["MobileEnvMapping"] = "False",
        ["MobileRimLighting"] = "False",
        ["MobileColorBlending"] = "False",
        ["MobileVertexMovement"] = "False",
        ["MobilePostProcessBlurAmount"] = "0.000000",
        ["MobileLightShaftScale"] = "0",
        ["MobileLightShaftFirstPass"] = "0",
        ["MobileLightShaftSecondPass"] = "0",
        ["FullEffectIntensity"] = "False",
        ["UseHighQualityBloom"] = "False",
        ["bUseTranslucentArenaShaders"] = "False",
        ["AllowImageReflectionShadowing"] = "False",
    };

    public static string Apply(string iniText)
    {
        var sb = new StringBuilder();
        bool inSs = false;
        bool inChild = false;
        var pendingEverywhere = new HashSet<string>(EverywhereKeys.Keys, StringComparer.OrdinalIgnoreCase);
        var pendingChild = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var managedSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void FlushPending()
        {
            foreach (string pendingKey in pendingEverywhere)
                sb.Append(pendingKey).Append('=').Append(EverywhereKeys[pendingKey]).Append("\r\n");
            pendingEverywhere.Clear();

            foreach (string pendingKey in pendingChild)
                sb.Append(pendingKey).Append('=').Append(ChildPerfKeys[pendingKey]).Append("\r\n");
            pendingChild.Clear();
        }

        foreach (var raw in iniText.Replace("\r\n", "\n").Split('\n'))
        {
            string line = raw;
            if (line.StartsWith('['))
            {
                if (inSs)
                    FlushPending();

                inSs = line.StartsWith("[SystemSettings", StringComparison.OrdinalIgnoreCase);
                inChild = IsChildSystemSettings(line);
                managedSeen.Clear();
                pendingEverywhere = inSs
                    ? new HashSet<string>(EverywhereKeys.Keys, StringComparer.OrdinalIgnoreCase)
                    : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                pendingChild = inChild
                    ? new HashSet<string>(ChildPerfKeys.Keys, StringComparer.OrdinalIgnoreCase)
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
                if (!managedSeen.Add(key))
                    continue;
                sb.Append(key).Append('=').Append(everywhereVal).Append("\r\n");
                pendingEverywhere.Remove(key);
                continue;
            }

            if (inChild && ChildPerfKeys.TryGetValue(key, out string? val))
            {
                if (!managedSeen.Add(key))
                    continue;
                sb.Append(key).Append('=').Append(val).Append("\r\n");
                pendingChild.Remove(key);
                continue;
            }

            sb.Append(line).Append("\r\n");
        }

        if (inSs)
            FlushPending();

        return IniHygiene.DeduplicateSectionKeys(sb.ToString().TrimEnd('\r', '\n') + "\r\n");
    }

    private static bool IsChildSystemSettings(string header)
    {
        if (!header.StartsWith("[SystemSettings", StringComparison.OrdinalIgnoreCase))
            return false;
        return !header.Equals("[SystemSettings]", StringComparison.OrdinalIgnoreCase);
    }
}
