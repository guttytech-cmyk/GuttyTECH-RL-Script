using System.Text;

namespace GuttyRL;

/// <summary>Forca o potato maximo VALIDADO no boot (bisect 2026-08-04).
/// Killer confirmado: MaxFilterBlurSampleCount=0 → crash KERNELBASE.
/// BlurSamples=1 aprovado em teste (2026-08-07); 0 proibido.</summary>
internal static class CompletoForce
{
    // Validado: MaxLODSize=2 / LODBias=100 abre e fica estavel ~45s+.
    private const string PotatoTextureGroup =
        "(MinLODSize=1,MaxLODSize=2,LODBias=100,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)";

    private static readonly string[] TextureGroups =
    {
        "TEXTUREGROUP_World",
        "TEXTUREGROUP_WorldNormalMap",
        "TEXTUREGROUP_WorldSpecular",
        "TEXTUREGROUP_Character",
        "TEXTUREGROUP_CharacterNormalMap",
        "TEXTUREGROUP_CharacterSpecular",
        "TEXTUREGROUP_Weapon",
        "TEXTUREGROUP_WeaponNormalMap",
        "TEXTUREGROUP_WeaponSpecular",
        "TEXTUREGROUP_Vehicle",
        "TEXTUREGROUP_VehicleNormalMap",
        "TEXTUREGROUP_VehicleSpecular",
        "TEXTUREGROUP_Cinematic",
        "TEXTUREGROUP_Effects",
        "TEXTUREGROUP_EffectsNotFiltered",
        "TEXTUREGROUP_Skybox",
        "TEXTUREGROUP_UI",
        "TEXTUREGROUP_Lightmap",
        "TEXTUREGROUP_LightAndShadowMap",
        "TEXTUREGROUP_Shadowmap",
        "TEXTUREGROUP_RenderTarget",
        "TEXTUREGROUP_MobileFlattened",
        "TEXTUREGROUP_ProcBuilding_Face",
        "TEXTUREGROUP_ProcBuilding_LightMap",
        "TEXTUREGROUP_Terrain_Heightmap",
        "TEXTUREGROUP_Terrain_Weightmap",
        "TEXTUREGROUP_ImageBasedReflection",
        "TEXTUREGROUP_Bokeh",
        "TEXTUREGROUP_Pitch",
        "TEXTUREGROUP_ColorLookupTable",
    };

    private static readonly Dictionary<string, string> Keys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["DetailMode"] = "0",
        ["ParticleLODBias"] = "100",
        ["SkeletalMeshLODBias"] = "100",
        ["MaxDrawDistanceScale"] = "0",
        ["FoliageDrawRadiusMultiplier"] = "0.000000",
        ["StaticDecals"] = "False",
        // Indicador/sombra da bola (chao + cantos): DynamicDecals + UnbatchedDecals.
        // Unbatched=False apaga a sombra nos corners — regressao do extreme potato.
        ["DynamicDecals"] = "True",
        ["UnbatchedDecals"] = "True",
        ["DecalCullDistanceScale"] = "1.000000",

        ["Trilinear"] = "False",
        ["MaxAnisotropy"] = "0",
        ["MaxMultiSamples"] = "0",
        // NAO baixar para 0 — crash KERNELBASE. 1 = aprovado (era 2).
        ["MaxFilterBlurSampleCount"] = "1",
        ["FullEffectIntensity"] = "False",
        ["bAllowHighQualityMaterials"] = "False",
        ["bUseTranslucentArenaShaders"] = "False",
        ["bAllowDownsampledTranslucency"] = "False",
        ["UseHighQualityBloom"] = "False",
        ["AmbientOcclusion"] = "False",
        ["DepthOfField"] = "False",
        ["Bloom"] = "False",
        ["bAllowLightShafts"] = "False",
        ["LensFlares"] = "False",
        ["DynamicLights"] = "False",
        ["DynamicShadows"] = "False",
        ["LightEnvironmentShadows"] = "False",
        ["CompositeDynamicLights"] = "False",
        ["SHSecondaryLighting"] = "False",
        ["DirectionalLightmaps"] = "False",
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
        ["DropParticleDistortion"] = "True",
        ["AllowRadialBlur"] = "False",
        ["AllowSubsurfaceScattering"] = "False",
        ["AllowImageReflections"] = "False",
        ["AllowImageReflectionShadowing"] = "False",
        ["AllowApexCloth"] = "False",
        ["bAllowSeparateTranslucency"] = "False",
        ["FloatingPointRenderTargets"] = "False",
        ["HighPrecisionGBuffers"] = "False",
        ["MobileEnableMSAA"] = "False",
        ["MobileModShadows"] = "False",
        ["MobileMinimizeFogShaders"] = "TRUE",
        ["MobileLightShaftScale"] = "0",
        ["MobileLightShaftFirstPass"] = "0",
        ["MobileLightShaftSecondPass"] = "0",
        ["TemporalAA_MinDepth"] = "0.000000",
        ["TemporalAA_StartDepthVelocityScale"] = "0.000000",

        ["MinShadowResolution"] = "1",
        ["MinPreShadowResolution"] = "1",
        ["MaxShadowResolution"] = "1",
        ["MobileShadowTextureResolution"] = "1",
        ["MaxWholeSceneDominantShadowResolution"] = "1",
        ["ShadowFadeResolution"] = "1",
        ["PreShadowFadeResolution"] = "1",
        ["ShadowTexelsPerPixel"] = "0.250000",
        ["PreShadowResolutionFactor"] = "0.250000",
        ["bAllowWholeSceneDominantShadows"] = "False",
        ["bEnableForegroundShadowsOnWorld"] = "True",
        ["bEnableForegroundSelfShadowing"] = "False",
        ["UnbuiltWholeSceneDynamicShadowRadius"] = "1.000000",
        ["UnbuiltNumWholeSceneDynamicShadowCascades"] = "1",
        ["WholeSceneShadowUnbuiltInteractionThreshold"] = "0",

        ["TessellationAdaptivePixelsPerTriangle"] = "4096.000000",
        ["bAllowFracturedDamage"] = "False",
        ["NumFracturedPartsScale"] = "0.000000",
        ["FractureDirectSpawnChanceScale"] = "0.000000",
        ["FractureRadialSpawnChanceScale"] = "0.000000",
        ["FractureCullDistanceScale"] = "0.000000",
        ["ApexLODResourceBudget"] = "0.000000",
        ["ApexDestructionMaxChunkIslandCount"] = "0",
        ["ApexDestructionMaxShapeCount"] = "0",
        ["ApexGRBEnable"] = "False",

        ["MobileFog"] = "False",
        ["MobileHeightFog"] = "False",
        ["MobileSpecular"] = "False",
        ["MobileBumpOffset"] = "False",
        ["MobileNormalMapping"] = "False",
        ["MobileEnvMapping"] = "False",
        ["MobileRimLighting"] = "False",
        ["MobileColorBlending"] = "False",
        ["MobileVertexMovement"] = "False",
        ["MobileLODBias"] = "100.000000",
        ["MobileLandscapeLODBias"] = "100",
        ["MobilePostProcessBlurAmount"] = "0.000000",
        ["MobileMaxShadowRange"] = "0.000000",

        ["ScreenPercentage"] = "100.000000",
        ["UpscaleScreenPercentage"] = "True",
        ["MinimumScreenScale"] = "100.000000",
        ["UncappedFramerate"] = "True",
        ["bSmoothFrameRate"] = "False",
        ["CustomFPS"] = "0",
        ["OnlyStreamInTextures"] = "False",
        ["WaitForGPU"] = "False",
        ["OneFrameThreadLag"] = "True",
        ["AllowPerFrameSleep"] = "True",
        ["AllowPerFrameYield"] = "True",
        ["UseVsync"] = "False",
        ["AllowDynamicResolution"] = "False",
        ["SceneCaptureStreamingMultiplier"] = "0.000000",
    };

    public static string Apply(string iniText)
    {
        var sb = new StringBuilder();
        bool forceSection = false;
        bool mainSection = false;
        HashSet<string>? pendingKeys = null;
        HashSet<string>? pendingTextureGroups = null;
        var managedSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void FlushSectionContract()
        {
            if (pendingKeys is not null)
            {
                foreach (string key in pendingKeys)
                    sb.Append(key).Append('=').Append(Keys[key]).Append("\r\n");
                pendingKeys.Clear();
            }

            if (pendingTextureGroups is not null)
            {
                foreach (string group in pendingTextureGroups)
                    sb.Append(group).Append('=').Append(PotatoTextureGroup).Append("\r\n");
                pendingTextureGroups.Clear();
            }
        }

        string normalized = iniText.Replace("\r\n", "\n").TrimEnd('\r', '\n');
        foreach (var raw in normalized.Split('\n'))
        {
            string line = raw;
            if (line.StartsWith('['))
            {
                FlushSectionContract();
                forceSection = IsForceSection(line);
                mainSection = line.Equals("[SystemSettings]", StringComparison.OrdinalIgnoreCase);
                bool textureContractSection = mainSection
                    || line.StartsWith("[SystemSettingsTextures", StringComparison.OrdinalIgnoreCase)
                    || line.Equals("[SystemSettingsScreenshot]", StringComparison.OrdinalIgnoreCase)
                    || line.Equals("[SystemSettingsMobileTextureBias]", StringComparison.OrdinalIgnoreCase);
                managedSeen.Clear();
                pendingKeys = mainSection
                    ? new HashSet<string>(Keys.Keys, StringComparer.OrdinalIgnoreCase)
                    : null;
                pendingTextureGroups = textureContractSection
                    ? new HashSet<string>(TextureGroups, StringComparer.OrdinalIgnoreCase)
                    : null;
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
                if (!managedSeen.Add(key))
                    continue;
                sb.Append(key).Append('=').Append(PotatoTextureGroup).Append("\r\n");
                pendingTextureGroups?.Remove(key);
                continue;
            }

            if (Keys.TryGetValue(key, out string? val))
            {
                if (!managedSeen.Add(key))
                    continue;
                sb.Append(key).Append('=').Append(val).Append("\r\n");
                pendingKeys?.Remove(key);
                continue;
            }

            sb.Append(line).Append("\r\n");
        }

        FlushSectionContract();
        return IniHygiene.DeduplicateSectionKeys(sb.ToString().TrimEnd('\r', '\n') + "\r\n");
    }

    /// <summary>
    /// True se o SystemSettings principal afrouxou face ao contrato validado.
    /// O RL reescreve sobretudo TEXTUREGROUP_* (ex.: MaxLOD 128/LODBias 2) em sessao.
    /// </summary>
    public static bool HasDrift(string iniText) => DescribeDrift(iniText).Count > 0;

    public static List<string> DescribeDrift(string iniText)
    {
        var drift = new List<string>();
        var main = ReadMainSystemSettings(iniText);

        void Expect(string key, string expected, StringComparison cmp = StringComparison.OrdinalIgnoreCase)
        {
            if (!main.TryGetValue(key, out string? got)
                || !got.Equals(expected, cmp))
            {
                drift.Add($"{key}={got ?? "(ausente)"} (esperado {expected})");
            }
        }

        Expect("MaxShadowResolution", "1");
        Expect("MaxFilterBlurSampleCount", "1");
        Expect("DynamicLights", "False");
        Expect("DynamicShadows", "False");
        Expect("DynamicDecals", "True");
        Expect("UnbatchedDecals", "True");
        Expect("DecalCullDistanceScale", "1.000000");
        Expect("bEnableForegroundShadowsOnWorld", "True");
        Expect("UncappedFramerate", "True");
        Expect("bAllowLightShafts", "False");
        Expect("MobileNormalMapping", "False");

        if (!main.TryGetValue("ApexLODResourceBudget", out string? apex)
            || !apex.StartsWith("0", StringComparison.Ordinal))
        {
            drift.Add($"ApexLODResourceBudget={apex ?? "(ausente)"} (esperado 0)");
        }

        foreach (string tg in new[]
                 {
                     "TEXTUREGROUP_World", "TEXTUREGROUP_Vehicle", "TEXTUREGROUP_Pitch",
                     "TEXTUREGROUP_Character", "TEXTUREGROUP_UI",
                 })
        {
            if (!main.TryGetValue(tg, out string? val)
                || val.IndexOf("MaxLODSize=2", StringComparison.OrdinalIgnoreCase) < 0
                || val.IndexOf("LODBias=100", StringComparison.OrdinalIgnoreCase) < 0)
            {
                drift.Add($"{tg} afrouxado ({val ?? "ausente"})");
            }
        }

        return drift;
    }

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
            map[raw[..eq]] = raw[(eq + 1)..];
        }
        return map;
    }

    private static bool IsForceSection(string header) =>
        header.StartsWith("[SystemSettings", StringComparison.OrdinalIgnoreCase);
}
