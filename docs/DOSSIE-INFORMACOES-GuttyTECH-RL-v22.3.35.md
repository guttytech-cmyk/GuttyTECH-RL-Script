# DOSSIÊ DE INFORMAÇÕES — GuttyTECH Rocket League INI Optimizer
## Versão atual: v22.3.35 (2026-07-21) · Nome de código: TESSERACT

> Documento de **factos** para o roteirista criar o roteiro.  
> **Não é roteiro.** Não tem ganchos, timestamps nem CTAs de YouTube.  
> Fonte: código do repo, CHANGELOG, README, DESCRICAO, conversas de desenvolvimento até a release v22.3.35.

**Release:** https://github.com/guttytech-cmyk/GuttyTECH-RL-Script/releases/tag/v22.3.35  
**Artefacto cliente:** `GuttyTECH_RL.exe` (~90 MB, .NET 9 single-file self-contained)  
**Repo:** https://github.com/guttytech-cmyk/GuttyTECH-RL-Script

---

## 1. O que é o produto (estado atual)

- Otimizador do ficheiro `TASystemSettings.ini` do Rocket League (Unreal Engine 3).
- No modo **COMPLETO**, também sincroniza o menu de vídeo Epic (ficheiro `.save`) para as opções aparecerem em **High Performance**.
- **Não** mexe em Windows, registo, rede, HPET, TCP, `bcdedit`, nem PowerShell para aplicar.
- Compatível com Easy Anti-Cheat (EAC). Não é cheat; só config de gráficos + sync de menu.

### Caminhos importantes no PC do cliente

| O quê | Onde |
|--------|------|
| INI do jogo | `%USERPROFILE%\Documents\My Games\Rocket League\TAGame\Config\TASystemSettings.ini` (também OneDrive / outros perfis) |
| Save Epic | `...\TAGame\SaveDataEpic\DBE_Production\*.save` |
| Backups GuttyTECH | `%USERPROFILE%\GuttyTECH\RL-Optimizer-v22\Backups\` |
| Original (1ª vez) | `...\Backups\TASystemSettings.original.ini` |
| Runtime Python embutido | `%USERPROFILE%\GuttyTECH\RL-Optimizer-v22\runtime\{versão}\` |
| Log | `...\RL-Optimizer-v22\log.txt` |
| Crash log | `...\RL-Optimizer-v22\crash.log` |

### Menu do exe (v22.3.34+)

| Tecla | Nome no UI | O que faz |
|:---:|:---|:---|
| 1 | COMPLETO | FPS máximo, gráfico “batata”; força potato no INI + sync menu Epic High Performance |
| 2 | CRIADOR DE CONTEUDO | Otimiza sem destruir o visual (streamers) |
| 3 | REMOVER | Restaura só o INI (preserva presets/garagem do carro) |
| 4 | COMANDO DE INICIALIZACAO | Copia `-nomovie -NOSPLASH -high` (Steam/Epic) |
| 5 | CORRIGIR ERROS | Hub: Permissões \| Recuperar boot \| Tudo |
| 6 | RESTAURAR PRESETS | Copia backup save Epic mais recente → SaveDataEpic |
| 7 | SAIR | Fecha |

Submenu **CORRIGIR ERROS**:
1. PERMISSOES — destrava INI / pasta (Defender Acesso Controlado a Pastas)
2. RECUPERAR BOOT — INI padrão + save Epic (quando o jogo não abre)
3. TUDO — permissões + recuperar boot
4. VOLTAR

### Como o utilizador usa

1. Fechar Rocket League.
2. Abrir `GuttyTECH_RL.exe` (SmartScreen na 1ª vez: Mais informações → Executar assim mesmo).
3. Escolher modo.
4. COMPLETO: menu de vídeo Epic deve ficar High Performance sozinho.
5. CRIADOR: ainda precisa alinhar Opções → Vídeo 1× (Alta Qualidade, etc.).

---

## 2. Evolução: do .bat (v21) ao .exe (v22)

### Era v21 — `RL_GUTTYTECH_v21.x.bat` (legado em `legacy/`)

Problemas reais que a v22 foi feita para resolver:

| Problema v21 | Efeito no utilizador |
|---|---|
| Dependia de **PowerShell** | Falhava com ExecutionPolicy / antivirus |
| Bug `$env:RL_TARGET` sem definir | Às vezes **não aplicava nada** |
| Regex PowerShell em `TEXTUREGROUP` | Quebrava blocos de textura |
| Usava **bcdedit** | Secure Boot / BitLocker / chave de recuperação |
| Mexia TCP / rede / registo system-wide | Risco e efeitos fora do jogo |
| Forçava **1920×1080 + Fullscreen** e trancava read-only | **Tela preta** / crash em monitores diferentes |
| Backups em `%TEMP%` | Voláteis, sumiam |
| `attrib +r` agressivo | Jogo não gravava resolução / configs |

### Era v22 — `GuttyTECH_RL.exe` (.NET 9)

| Melhoria | Detalhe |
|---|---|
| Sem PowerShell para aplicar | Copia template nativo / reescreve INI inteiro |
| Sem bcdedit / TCP / HPET | Só INI (+ patch .save no COMPLETO) |
| Preserva ResX/ResY/Fullscreen/Borderless | Sem forçar 1080p |
| Backup persistente em `%USERPROFILE%\GuttyTECH\` | Com timestamp |
| Unlock de ficheiro read-only | Sem admin na maioria; eleva só se falhar |
| UI console animada | Banner, steps, painel concluído |
| Depois: Python+nixwrap **embutidos** no exe | Cliente baixa só o .exe (~90 MB) |

Arquivo legado mantido só para referência: `legacy/RL_GUTTYTECH_v21.5.bat`.

---

## 3. Linha do tempo — erros corrigidos e coisas acrescentadas

### Fundação v22.0–v22.3 (jun/2026)

- **v22.0:** app .NET 9 single-file; modos COMPLETO / CRIADOR / REMOVER; UI; preservação de resolução; detecção INI legado v21; build `build_exe.bat`.
- **v22.1:** COMPLETO mais agressivo — `MaxDrawDistanceScale=0`, `OnlyStreamInTextures=True` (mais tarde revertido por hang), LODBias 100, etc.
- **v22.2:** Launch Options helper (menu 4); comando `-nomovie -NOSPLASH -high`; documentação de placebos (`-NoVSync`, `-nolog`, `-USEALLAVAILABLECORES`, etc.).
- **v22.3:** CRIADOR — indicador da bola nas laterais (`UnbatchedDecals=True`, `bEnableForegroundShadowsOnWorld=True`); exe renomeado `GuttyTECH_RL.exe`.

### Hotfixes operacionais (v22.3.1–v22.3.3)

- **v22.3.1:** crash “abre e fecha na hora” (startup .NET / SmartScreen / pasta).
- **v22.3.2:** auto-desbloqueio pasta Defender + guia Acesso Controlado a Pastas.
- **v22.3.3:** remove checklist longo de vídeo pós-conclusão (UI).

### Sync menu Epic + indicador bola (v22.3.19–v22.3.20)

- **v22.3.19:** COMPLETO — indicador circular sob a bola (`DynamicDecals=True`, `DecalCullDistanceScale=1.0`); patch `.save` para menu vídeo; EffectIntensity Low; CRIADOR — SpeedTree leaves/fronds de volta; `CompletoForce` / `CriadorForce` / `VideoSettingsSync` / `IniAudit`.
- **v22.3.20:** Python 3.11 + nixwrap embutidos no exe (sem tools.zip no PC do cliente).

### Guerra ao boot / loading / tela preta (v22.3.21–v22.3.30)

Ciclo de tentativa/erro documentado (importante para honestidade no vídeo):

| Versão | O que tentámos | Resultado |
|---|---|---|
| .21 | WaitForGPU + CompletoForce restrito (loading infinito) | Não resolveu; revertido |
| .22 | `OnlyStreamInTextures=False` (hang DX11) | Fix parcial |
| .23 | Reverter para estado .20 | Problema de loading persistia em alguns PCs |
| .24 | Upscale on; COMPLETO INI gravável; preservar escala 3D | Baixar render quality sem janela minúscula |
| .25 | WaitForGPU=False (stock) | — |
| .26 | WaitForGPU=True | Estabilidade |
| .27 | FramePacingForce em todas as secções | Anti tela preta em partida |
| .28 | OnlyStreamInTextures=False; FramePacing só na secção principal | Boot travado |
| .29 | **Remove** FramePacingForce (crashava boot); WaitForGPU=False | Boot volta |
| .30 | Patch `.save` **desligado** (causava jogo não abrir); REMOVER restaura save Epic | Boot / recover |

### Presets / EOS / menu (v22.3.31–v22.3.34)

- **.31:** COMPLETO deixa de apagar `RLSettingsData` → deixava de desconectar Epic Online Services.
- **.32:** COMPLETO/CRIADOR mexem **só no INI** (preserva garagem); nasce `RESTAURAR-PRESETS`.
- **.33:** Clima/FPS/raios a regressar no menu — `MobileFog` no CRIADOR + chaves corrompidas preservadas; VideoLockedKeys; patch .save reativado com cuidado.
- **.34:** Menu CORRIGIR ERROS (hub) + RESTAURAR PRESETS no [6].

### Fix desta conversa — v22.3.35 (2026-07-21)

**Problema reportado pelo utilizador (com screenshots do menu RL):**
- Em BASIC SETTINGS, **Render Quality** aparecia **High Quality**.
- Em ADVANCED, Texture já estava **High Performance**, World **Performance**, Particle **High Quality**.
- Pessoas mudavam Render Quality de High Quality → High Performance e aparecia **borda preta**.
- Pedido: todas as opções em High Performance para não confundirem e não mexerem.

**Causa raiz encontrada no código:**
- Em `tools/patch_save_video.py`, o loop fazia `for option_id, value in COMPLETO_OPTIONS` onde `COMPLETO_OPTIONS` era lista de **dicts**.
- Em Python, iterar um dict unpacka as **keys** → sempre `("Id", "Value")`.
- Resultado: gravava lixo `{"Id":"Id","Value":"Value"}` no `.save` em vez de RenderQuality/ParticleDetail/etc.
- Por isso o menu Epic não sincronizava qualidade; Render Quality ficava High Quality.

**Correções aplicadas:**
1. Loop corrigido: `for item in COMPLETO_OPTIONS: upsert(item["Id"], item["Value"])`.
2. Sanitização remove entradas corrompidas `Id`/`Value`.
3. Valores COMPLETO no save:
   - RenderQuality = HighPerformance
   - RenderDetail = Performance
   - TextureDetail = TexturesLow (UI: High Performance)
   - ParticleDetail = HighPerformance
   - WorldDetail = HighPerformance
   - AntiAlias = 0
4. `Value` serializado como NameProperty (FName), igual ao Id.
5. Anti borda preta no INI via CompletoForce:
   - `ScreenPercentage=100`
   - `UpscaleScreenPercentage=True`
   - `MinimumScreenScale=100`
6. COMPLETO deixa de preservar escala antiga do utilizador (que podia ser 1.0 e abrir caminho à borda).

**Commit / release:** `a1bf265` · tag `v22.3.35` · exe na release GitHub.

---

## 4. O que cada modo faz (resumo técnico)

### COMPLETO (FPS máximo)

- Texturas 16×16, LODBias 15, filtro Point, MaxAnisotropy 0.
- DetailMode 0; ParticleLODBias/SkeletalMeshLODBias 100; MaxDrawDistanceScale 0.
- Materiais HQ OFF; reflexos OFF; HDR OFF; Apex Cloth OFF; SpeedTree OFF.
- Pós-processo OFF: motion blur, DoF, AO, bloom, light shafts, lens flares, fog, distorção.
- Sombras dinâmicas OFF; resoluções de sombra no mínimo (16).
- FPS uncapped (`UncappedFramerate=True`, `bSmoothFrameRate=False`, `UseVsync=False`).
- `WaitForGPU=False` (valor stock atual após os reverts de boot).
- `OnlyStreamInTextures=False` (hang de loading se True em alguns PCs).
- Decals dinâmicos ON (marcas de pneu / indicador bola onde aplicável).
- Escala 3D forçada a 100% + upscale (v22.3.35).
- Patch `.save`: menu High Performance + EffectIntensity EI_Low + MaxFPS 10000 + weather/light shafts off.

### CRIADOR (visual + FPS)

- Texturas até 1024, Aniso 16, DetailMode 2, materiais HQ ON, reflexos ON, HDR ON, Apex Cloth ON, SpeedTree ON.
- Sombras dinâmicas OFF (maior ganho sem matar o carro na câmara).
- Indicador bola laterais: UnbatchedDecals + ForegroundShadowsOnWorld ON.
- Pós-processo pesado OFF (blur/DoF/AO/bloom…).
- INI **gravável** / preserva escolhas do utilizador em chaves de vídeo ao reaplicar.
- **Não** força High Performance no save (utilizador ajusta Alta Qualidade 1×).

### REMOVER

- Restaura backup original ou stock; destrava read-only.
- Preserva presets da garagem (não apaga save Epic neste modo).

---

## 5. CompletoForce — chaves forçadas em TODAS as secções `[SystemSettings*]`

Além do template, o código C# reescreve estas chaves em todo o perfil derivado (para o menu/jogo não “vazar” qualidade alta):

DetailMode=0, ParticleLODBias=100, SkeletalMeshLODBias=100, MaxDrawDistanceScale=0, MaxAnisotropy=0, MaxMultiSamples=0, FullEffectIntensity=False, bAllowHighQualityMaterials=False, bUseTranslucentArenaShaders=False, AmbientOcclusion/DepthOfField/Bloom/bAllowLightShafts/LensFlares/DynamicShadows/… = False (lista completa em `dotnet/CompletoForce.cs`), MobileFog=False, MobileEnableMSAA=False, FloatingPointRenderTargets=False, e desde v22.3.35: ScreenPercentage=100, UpscaleScreenPercentage=True, MinimumScreenScale=100. Todos os TEXTUREGROUP_* → MaxLODSize=16, LODBias=15, Point.

---

## 6. Patch do .save Epic (COMPLETO) — opções atuais

Ficheiro: `tools/patch_save_video.py`

Flags vídeo: bShowLightShafts=False, bShowWeatherFX=False, bUncappedFramerate=True, bVsync=False, MaxFPS=10000.

VideoOptions:
- RenderQuality=HighPerformance
- RenderDetail=Performance
- TextureDetail=TexturesLow
- ParticleDetail=HighPerformance
- WorldDetail=HighPerformance
- AntiAlias=0

Gameplay: EffectIntensity=EI_Low  
Câmara (COMPLETO): bUseBallIndicator=True

---

## 7. Launch options (menu 4)

Comando copiado (Steam = Epic):

```
-nomovie -NOSPLASH -high
```

| Flag | Efeito real no RL |
|---|---|
| -nomovie | Pula intros — boot mais rápido |
| -NOSPLASH | Pula splash |
| -high | Prioridade Alta no Windows (tirar se stutter/áudio) |

Placebos / no-op (não usar como “ganho de FPS”): `-NoVSync`, `-nolog`, `-NoSteamVR`, `-no-stereo-rendering`, `-USEALLAVAILABLECORES`, `-malloc=system`.

Opcional manual: `-NoForceFeedback` (mata rumble).

**Nota de produto:** launch options quase não mudam FPS; o ganho real é INI + Opções → Vídeo.

---

## 8. Problemas que o suporte ainda deve conhecer

1. SmartScreen na 1ª execução do exe.
2. Não correr de dentro do ZIP — extrair primeiro.
3. Abrir RL 1× se ainda não existir `TASystemSettings.ini`.
4. Acesso Controlado a Pastas (Defender) bloqueia gravação em Documents.
5. Fechar RL antes de aplicar (jogo sobrescreve INI ao fechar).
6. “LIMITE DE CHAMADA” / EOS desconectado — muitas vezes limite Epic por abrir/fechar muito; não é o INI em si (e .31 parou de apagar RLSettingsData no COMPLETO).
7. Presets sumiram → menu [6] RESTAURAR PRESETS.
8. Jogo não abre → [5] CORRIGIR ERROS → Recuperar boot.

---

## 9. Comparação rápida COMPLETO vs CRIADOR (valores)

| Item | CRIADOR | COMPLETO |
|---|:---:|:---:|
| Texturas | até 1024, Aniso | 16px, Point |
| MaxAnisotropy | 16 | 0 |
| DetailMode | 2 | 0 |
| Materiais HQ | ON | OFF |
| Reflexos | ON | OFF |
| HDR | ON | OFF |
| Apex Cloth | ON | OFF |
| SpeedTree | ON | OFF |
| Sombras dinâmicas | OFF | OFF |
| Indicador bola laterais | ON | parcial (decals dinâmicos) |
| Menu Epic quality sync | não força HP | High Performance (v22.3.35) |
| Escala 3D | 100% tipicamente | forçada 100% + upscale |

---

## 10. Código INI atual

Segue o conteúdo integral dos templates oficiais da v22.3.35.  
Estes ficheiros vivem em `templates/` e são embutidos no exe via `dotnet/Templates.cs`.


---

## 10.1 Template COMPLETO — ficheiro completo (	emplates/INI_COMPLETO.txt)

```ini
;GUTTYTECH-RL-OPTIMIZER=COMPLETO;v22.3.35
[SystemSettings]
UseDirectSound=True
StaticDecals=False
DynamicDecals=True
UnbatchedDecals=False
DecalCullDistanceScale=1.000000
DynamicLights=False
DynamicShadows=False
LightEnvironmentShadows=False
CompositeDynamicLights=False
SHSecondaryLighting=False
DirectionalLightmaps=False
MotionBlur=False
MotionBlurPause=False
MotionBlurSkinning=0
DepthOfField=False
AmbientOcclusion=False
Bloom=False
bAllowLightShafts=False
Distortion=False
FilteredDistortion=False
DropParticleDistortion=False
bAllowDownsampledTranslucency=False
SpeedTreeLeaves=False
SpeedTreeFronds=False
OnlyStreamInTextures=False
LensFlares=False
FogVolumes=False
FloatingPointRenderTargets=False
OneFrameThreadLag=True
WaitForGPU=False
UseVsync=False
CustomFPS=0
UpscaleScreenPercentage=True
UpscaleTargetFramerateDocked=60.000000
UpscaleTargetFramerateUndocked=60.000000
MinimumScreenScale=100.000000
AllowDynamicResolution=False
ZCullSaveRestore=False
AdaptiveZcull=False
BinnerTileCache=False
BinnerTileResX=64
BinnerTileResY=64
Fullscreen=True
AllowOpenGL=False
AllowRadialBlur=False
AllowSubsurfaceScattering=False
AllowImageReflections=False
AllowImageReflectionShadowing=False
bAllowSeparateTranslucency=False
bAllowPostprocessMLAA=False
bAllowHighQualityMaterials=False
bUseTranslucentArenaShaders=False
MaxFilterBlurSampleCount=2
SkeletalMeshLODBias=100
ParticleLODBias=100
DetailMode=0
MaxDrawDistanceScale=0
ShadowFilterQualityBias=0
MaxAnisotropy=0
MaxMultiSamples=0
bAllowD3D9MSAA=False
bAllowTemporalAA=False
TemporalAA_MinDepth=0.000000
TemporalAA_StartDepthVelocityScale=0.000000
MinShadowResolution=16
MinPreShadowResolution=8
MaxShadowResolution=16
MobileShadowTextureResolution=16
MaxWholeSceneDominantShadowResolution=16
ShadowFadeResolution=1
PreShadowFadeResolution=1
ShadowFadeExponent=0.250000
ResX=1920
ResY=1080
AutoDetectDesktopResolution=False
Borderless=False
AllowApexCloth=False
ScreenPercentage=100.000000
SceneCaptureStreamingMultiplier=0.000000
ShadowTexelsPerPixel=0.000000
PreShadowResolutionFactor=0.500000
bEnableBranchingPCFShadows=False
bAllowHardwareShadowFiltering=False
TessellationAdaptivePixelsPerTriangle=0.000000
bEnableForegroundShadowsOnWorld=False
bEnableForegroundSelfShadowing=False
bAllowWholeSceneDominantShadows=False
bUseConservativeShadowBounds=False
ShadowFilterRadius=0.000000
ShadowDepthBias=1.000000
PerObjectShadowTransition=0.000000
PerSceneShadowTransition=0.000000
CSMSplitPenumbraScale=0.000000
CSMSplitSoftTransitionDistanceScale=0.000000
CSMSplitDepthBiasScale=0.000000
CSMMinimumFOV=40.000000
CSMFOVRoundFactor=4.000000
UnbuiltWholeSceneDynamicShadowRadius=0.000000
UnbuiltNumWholeSceneDynamicShadowCascades=1
WholeSceneShadowUnbuiltInteractionThreshold=0
bAllowFracturedDamage=False
NumFracturedPartsScale=0.000000
FractureDirectSpawnChanceScale=0.000000
FractureRadialSpawnChanceScale=0.000000
FractureCullDistanceScale=0.000000
bForceCPUAccessToGPUSkinVerts=false
bDisableSkeletalInstanceWeights=False
HighPrecisionGBuffers=False
AllowSecondaryDisplays=False
SecondaryDisplayMaximumWidth=1280
SecondaryDisplayMaximumHeight=720
AllowPerFrameSleep=True
AllowPerFrameYield=True
MobileFeatureLevel=0
MobileFog=False
MobileHeightFog=False
MobileSpecular=False
MobileBumpOffset=True
MobileNormalMapping=True
MobileEnvMapping=True
MobileRimLighting=True
MobileColorBlending=True
MobileColorGrading=False
MobileVertexMovement=True
MobileOcclusionQueries=False
MobileGlobalGammaCorrection=False
MobileAllowGammaCorrectionWorldOverride=False
MobileAllowDepthPrePass=False
MobileGfxGammaCorrection=False
MobileLODBias=15.5
MobileBoneCount=75
MobileBoneWeightCount=2
MobileUsePreprocessedShaders=True
MobileFlashRedForUncachedShaders=False
MobileWarmUpPreprocessedShaders=True
MobileCachePreprocessedShaders=False
MobileProfilePreprocessedShaders=False
MobileUseCPreprocessorOnShaders=True
MobileLoadCPreprocessedShaders=True
MobileSharePixelShaders=True
MobileShareVertexShaders=True
MobileShareShaderPrograms=True
MobileEnableMSAA=False
MobileContentScaleFactor=1.0
MobileVertexScratchBufferSize=150
MobileIndexScratchBufferSize=10
MobileLightShaftScale=0
MobileLightShaftFirstPass=0
MobileLightShaftSecondPass=0
MobileModShadows=False
MobileTiltShift=False
MobileMaxMemory=300
MobilePostProcessBlurAmount=32.0
bMobileUsingHighResolutionTiming=True
MobileTiltShiftPosition=0.5
MobileTiltShiftFocusWidth=0.3
MobileTiltShiftTransitionWidth=0.5
MobileMaxShadowRange=500.0
MobileBloomTint=(R=1.0,G=0.75,B=0.0,A=1.0)
MobileClearDepthBetweenDPG=False
MobileSceneDepthResolveForShadows=FALSE
MobileLandscapeLODBias=15
MobileUseShaderGroupForStartupObjects=FALSE
MobileMinimizeFogShaders=TRUE
MobileFXAAQuality=0
ApexLODResourceBudget=1000000000000000000000.0
ApexDestructionMaxChunkIslandCount=0
ApexDestructionMaxShapeCount=0
ApexDestructionMaxChunkSeparationLOD=1.0
ApexDestructionMaxActorCreatesPerFrame=-1
ApexDestructionMaxFracturesProcessedPerFrame=-1
ApexDestructionSortByBenefit=True
ApexGRBEnable=False
ApexGRBGPUMemSceneSize=128
ApexGRBGPUMemTempDataSize=128
ApexGRBMeshCellSize=7.5
ApexGRBNonPenSolverPosIterCount=9;
ApexGRBFrictionSolverPosIterCount=3;
ApexGRBFrictionSolverVelIterCount=3;
ApexGRBSkinWidth=0.025
ApexGRBMaxLinearAcceleration=1000000.0
bEnableParallelAPEXClothingFetch=False
bApexClothingAsyncFetchResults=False
ApexClothingAvgSimFrequencyWindow=60
ApexClothingAllowAsyncCooking=False
ApexClothingAllowApexWorkBetweenSubsteps=FALSE
TEXTUREGROUP_World=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_WorldNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_WorldSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_Character=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_CharacterNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_CharacterSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_Weapon=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_WeaponNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_WeaponSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_Vehicle=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_VehicleNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_VehicleSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_Cinematic=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_Effects=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_EffectsNotFiltered=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_Skybox=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_UI=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_Lightmap=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_Shadowmap=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,NumStreamedMips=3,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_RenderTarget=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_MobileFlattened=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_ProcBuilding_Face=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_ProcBuilding_LightMap=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_Terrain_Heightmap=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_Terrain_Weightmap=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_ImageBasedReflection=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_Blur5)
TEXTUREGROUP_Bokeh=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_Pitch=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
FullEffectIntensity=False
bAllowBetterModulatedShadows=FALSE
UncappedFramerate=True
bSmoothFrameRate=False
TEXTUREGROUP_ColorLookupTable=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)

[SystemSettingsBucket1]
BasedOn=SystemSettings

[SystemSettingsBucket2]
BasedOn=SystemSettings

[SystemSettingsBucket3]
BasedOn=SystemSettings

[SystemSettingsBucket4]
BasedOn=SystemSettings

[SystemSettingsBucket5]
BasedOn=SystemSettings

[SystemSettingsScreenshot]
BasedOn=SystemSettings
MaxAnisotropy=0
ShadowFilterQualityBias=0
MinShadowResolution=16
ShadowFadeResolution=1
MinPreShadowResolution=8
PreShadowFadeResolution=1
ShadowTexelsPerPixel=0.000000
PreShadowResolutionFactor=0.5
MaxShadowResolution=16
MaxWholeSceneDominantShadowResolution=16
CompositeDynamicLights=False
TEXTUREGROUP_World=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_WorldNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_WorldSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_Character=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_CharacterNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_CharacterSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_Weapon=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_WeaponNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_WeaponSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_Vehicle=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_VehicleNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_VehicleSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_Cinematic=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_Effects=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_EffectsNotFiltered=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_Skybox=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_UI=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_Lightmap=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_Shadowmap=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_RenderTarget=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_MobileFlattened=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_ProcBuilding_Face=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_ProcBuilding_LightMap=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_Terrain_Heightmap=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_Terrain_Weightmap=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)

[SystemSettingsEditor]
BasedOn=SystemSettings
ResX=1280
ResY=720

[SystemSettingsSplitScreen2]
BasedOn=SystemSettings
bAllowWholeSceneDominantShadows=False
bAllowLightShafts=False
DetailMode=0

[SystemSettingsMobile]
BasedOn=SystemSettings
Fullscreen=True
DirectionalLightmaps=False
DynamicLights=False
SHSecondaryLighting=False
StaticDecals=False
DynamicDecals=False
UnbatchedDecals=False
MotionBlur=False
MotionBlurPause=False
DepthOfField=False
AmbientOcclusion=False
Bloom=False
Distortion=False
FilteredDistortion=False
DropParticleDistortion=False
FloatingPointRenderTargets=False
MaxAnisotropy=0
bAllowLightShafts=False
MobileModShadows=False
MobileClearDepthBetweenDPG=False
MaxFilterBlurSampleCount=2
DynamicShadows=False
MobileMaxMemory=300
MobileLandscapeLODBias=15
AllowRadialBlur=False

[SystemSettingsMobilePreviewer]
BasedOn=SystemSettingsMobile
Fullscreen=False

[SystemSettingsMobileTextureBias]
BasedOn=SystemSettingsMobile
TEXTUREGROUP_World=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_WorldNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_WorldSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_Character=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_CharacterNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_CharacterSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_Weapon=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_WeaponNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_WeaponSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_Vehicle=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_VehicleNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_VehicleSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_Cinematic=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_Effects=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_EffectsNotFiltered=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_Skybox=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_UI=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_Lightmap=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_Shadowmap=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,NumStreamedMips=3)
TEXTUREGROUP_RenderTarget=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_MobileFlattened=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_ProcBuilding_Face=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_ProcBuilding_LightMap=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_Terrain_Heightmap=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)
TEXTUREGROUP_Terrain_Weightmap=(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point)

[SystemSettingsAndroid]
BasedOn=SystemSettingsMobileTextureBias

[SystemSettingsAndroid_Performance1_MemoryLow]
BasedOn=SystemSettingsMobileTextureBias
MobileFeatureLevel=1
MobileFog=False
MobileSpecular=False
MobileBumpOffset=False
MobileNormalMapping=False
MobileEnvMapping=False
MobileRimLighting=False
MobileContentScaleFactor=0.9375

[SystemSettingsAndroid_Performance2_MemoryLow]
BasedOn=SystemSettingsMobileTextureBias
MobileBumpOffset=False
MobileNormalMapping=False
MobileContentScaleFactor=0.9375

[SystemSettingsAndroid_Performance1_Memory1024]
BasedOn=SystemSettingsMobile
MobileFeatureLevel=1
MobileFog=False
MobileSpecular=False
MobileBumpOffset=False
MobileNormalMapping=False
MobileEnvMapping=False
MobileRimLighting=False
MobileContentScaleFactor=0.9375

[SystemSettingsAndroid_Performance2_Memory1024]
BasedOn=SystemSettingsMobile
MobileBumpOffset=False
MobileNormalMapping=False
MobileContentScaleFactor=0.9375

[SystemSettingsFlash]
BasedOn=SystemSettingsMobileTextureBias
MotionBlur=False
MotionBlurPause=False
DepthOfField=False
AmbientOcclusion=False
Bloom=False
Distortion=False
FilteredDistortion=False
bAllowLightShafts=False
MobileModShadows=False
DynamicShadows=False
MobileClearDepthBetweenDPG=True
DirectionalLightmaps=False
MobileHeightFog=False

[SystemSettingsIPhone]
BasedOn=SystemSettingsMobileTextureBias
bMobileUsingHighResolutionTiming=False

[SystemSettingsIPhone3GS]
BasedOn=SystemSettingsMobileTextureBias
LensFlares=False
DetailMode=0
MobileEnableMSAA=False
MobileMaxMemory=100
bMobileUsingHighResolutionTiming=False
MobileLandscapeLODBias=15

[SystemSettingsIPhone4]
BasedOn=SystemSettingsMobile
MobileContentScaleFactor=2.0
LensFlares=False
bMobileUsingHighResolutionTiming=False
MobileLandscapeLODBias=15

[SystemSettingsIPhone4S]
BasedOn=SystemSettingsMobile
MobileEnableMSAA=False
bAllowLightShafts=False
MobileModShadows=False
DynamicShadows=False
ShadowDepthBias=0.025
MobileContentScaleFactor=2.0
MaxShadowResolution=16
MobileShadowTextureResolution=16

[SystemSettingsIPhone5]
BasedOn=SystemSettingsMobile
MobileEnableMSAA=False
bAllowLightShafts=False
MobileModShadows=False
DynamicShadows=False
ShadowDepthBias=0.025
MobileContentScaleFactor=2.0
MaxShadowResolution=16
MobileShadowTextureResolution=16
AllowRadialBlur=False

[SystemSettingsIPodTouch4]
BasedOn=SystemSettingsMobileTextureBias
MobileContentScaleFactor=2.0
LensFlares=False
MobileMaxMemory=100
bMobileUsingHighResolutionTiming=False
MobileLandscapeLODBias=15

[SystemSettingsIPodTouch5]
BasedOn=SystemSettingsMobile
MobileEnableMSAA=False
bAllowLightShafts=False
MobileModShadows=False
DynamicShadows=False
ShadowDepthBias=0.025
MobileContentScaleFactor=2.0
MaxShadowResolution=16
MobileShadowTextureResolution=16

[SystemSettingsIPad]
BasedOn=SystemSettingsMobileTextureBias
MobileFeatureLevel=1
MobileFog=False
MobileSpecular=False
MobileBumpOffset=False
MobileNormalMapping=False
MobileEnvMapping=False
MobileRimLighting=False
MobileMaxMemory=100
bMobileUsingHighResolutionTiming=False
MobileLandscapeLODBias=15
MobileContentScaleFactor=0.9375

[SystemSettingsIPad2]
BasedOn=SystemSettingsMobile
MobileEnableMSAA=False
bAllowLightShafts=False
MobileModShadows=False
DynamicShadows=False
ShadowDepthBias=0.016
MobileContentScaleFactor=1.0
MaxShadowResolution=16
MobileShadowTextureResolution=16

[SystemSettingsIPad3]
BasedOn=SystemSettingsMobile
MobileEnableMSAA=False
bAllowLightShafts=False
MobileModShadows=False
DynamicShadows=False
ShadowDepthBias=0.016
MobileContentScaleFactor=1.40625
MaxShadowResolution=16
MobileShadowTextureResolution=16
MobileMaxMemory=500

[SystemSettingsIPad4]
BasedOn=SystemSettingsMobile
MobileEnableMSAA=False
bAllowLightShafts=False
MobileModShadows=False
DynamicShadows=False
ShadowDepthBias=0.016
MobileContentScaleFactor=2.0
MaxShadowResolution=16
MobileShadowTextureResolution=16
MobileMaxMemory=500
AllowRadialBlur=False

[SystemSettingsIPadMini]
BasedOn=SystemSettingsMobile
MobileEnableMSAA=False
bAllowLightShafts=False
MobileModShadows=False
DynamicShadows=False
ShadowDepthBias=0.016
MobileContentScaleFactor=1.0
MaxShadowResolution=16
MobileShadowTextureResolution=16

[SystemSettingsIPad2_Detail]
BasedOn=SystemSettingsIPad2

[Configuration]

[SystemSettingsTexturesDerp]
BasedOn=SystemSettings
TEXTUREGROUP_Character=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_CharacterNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_CharacterSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Effects=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_LightAndShadowMap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_RenderTarget=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Skybox=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_UI=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Vehicle=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_VehicleNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_VehicleSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Weapon=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_WeaponNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_WeaponSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_World=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_WorldNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_WorldSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Cinematic=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_EffectsNotFiltered=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Lightmap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Shadowmap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_MobileFlattened=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_ProcBuilding_Face=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_ProcBuilding_LightMap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Terrain_Heightmap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Terrain_Weightmap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_ImageBasedReflection=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Bokeh=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_ColorLookupTable=(MinLODSize=1,MaxLODSize=16,LODBias=15)

[SystemSettingsTexturesLow]
BasedOn=SystemSettings
TEXTUREGROUP_Character=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_CharacterNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_CharacterSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Effects=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_LightAndShadowMap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_RenderTarget=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Skybox=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_UI=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Vehicle=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_VehicleNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_VehicleSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Weapon=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_WeaponNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_WeaponSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_World=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_WorldNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_WorldSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Cinematic=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_EffectsNotFiltered=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Lightmap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Shadowmap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_MobileFlattened=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_ProcBuilding_Face=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_ProcBuilding_LightMap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Terrain_Heightmap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Terrain_Weightmap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_ImageBasedReflection=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Bokeh=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_ColorLookupTable=(MinLODSize=1,MaxLODSize=16,LODBias=15)

[SystemSettingsTexturesMedium]
BasedOn=SystemSettings
TEXTUREGROUP_Character=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_CharacterNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_CharacterSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Effects=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_LightAndShadowMap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_RenderTarget=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Skybox=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_UI=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Vehicle=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_VehicleNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_VehicleSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Weapon=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_WeaponNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_WeaponSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_World=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_WorldNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_WorldSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Cinematic=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_EffectsNotFiltered=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Lightmap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Shadowmap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_MobileFlattened=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_ProcBuilding_Face=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_ProcBuilding_LightMap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Terrain_Heightmap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Terrain_Weightmap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_ImageBasedReflection=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Bokeh=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_ColorLookupTable=(MinLODSize=1,MaxLODSize=16,LODBias=15)

[SystemSettingsTexturesHigh]
BasedOn=SystemSettings
TEXTUREGROUP_Character=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_CharacterNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_CharacterSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Effects=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_LightAndShadowMap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_RenderTarget=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Skybox=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_UI=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Vehicle=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_VehicleNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_VehicleSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Weapon=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_WeaponNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_WeaponSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_World=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_WorldNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_WorldSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Cinematic=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_EffectsNotFiltered=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Lightmap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Shadowmap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_MobileFlattened=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_ProcBuilding_Face=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_ProcBuilding_LightMap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Terrain_Heightmap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Terrain_Weightmap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_ImageBasedReflection=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Bokeh=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_ColorLookupTable=(MinLODSize=1,MaxLODSize=16,LODBias=15)

[SystemSettingsTexturesHigher]
BasedOn=SystemSettings
TEXTUREGROUP_Character=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_CharacterNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_CharacterSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Effects=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_LightAndShadowMap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_RenderTarget=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Skybox=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_UI=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Vehicle=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_VehicleNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_VehicleSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Weapon=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_WeaponNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_WeaponSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_World=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_WorldNormalMap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_WorldSpecular=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Cinematic=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_EffectsNotFiltered=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Lightmap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Shadowmap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_MobileFlattened=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_ProcBuilding_Face=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_ProcBuilding_LightMap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Terrain_Heightmap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Terrain_Weightmap=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_ImageBasedReflection=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_Bokeh=(MinLODSize=1,MaxLODSize=16,LODBias=15)
TEXTUREGROUP_ColorLookupTable=(MinLODSize=1,MaxLODSize=16,LODBias=15)

[SystemSettingsProfileDetailLow]
BasedOn=SystemSettings
DetailMode=0
AmbientOcclusion=False
DepthOfField=False
Bloom=False
bAllowLightShafts=False
LensFlares=False
DynamicShadows=False
MotionBlur=False

[SystemSettingsProfileDetailHigh]
BasedOn=SystemSettings
DetailMode=0
AmbientOcclusion=False
DepthOfField=False
Bloom=False
bAllowLightShafts=False
LensFlares=False
DynamicShadows=False
MotionBlur=False

[IniVersion]
0=1772301531.000000
1=1772301531.000000


```


---

## 10.2 Template CRIADOR — ficheiro completo (	emplates/INI_CRIADOR.txt)

```ini
;GUTTYTECH-RL-OPTIMIZER=CRIADOR;v22.3.35
[SystemSettings]
UseDirectSound=True
StaticDecals=False
DynamicDecals=True
UnbatchedDecals=True
DecalCullDistanceScale=1.000000
DynamicLights=False
DynamicShadows=False
LightEnvironmentShadows=False
CompositeDynamicLights=False
SHSecondaryLighting=False
DirectionalLightmaps=False
MotionBlur=False
MotionBlurPause=False
MotionBlurSkinning=0
DepthOfField=False
AmbientOcclusion=False
Bloom=False
bAllowLightShafts=False
Distortion=False
FilteredDistortion=False
DropParticleDistortion=False
bAllowDownsampledTranslucency=False
SpeedTreeLeaves=True
SpeedTreeFronds=True
OnlyStreamInTextures=False
LensFlares=False
FogVolumes=False
FloatingPointRenderTargets=True
OneFrameThreadLag=True
WaitForGPU=False
UseVsync=False
UpscaleScreenPercentage=True
UpscaleTargetFramerateDocked=60.000000
UpscaleTargetFramerateUndocked=60.000000
MinimumScreenScale=100.000000
AllowDynamicResolution=False
ZCullSaveRestore=False
AdaptiveZcull=False
BinnerTileCache=False
BinnerTileResX=64
BinnerTileResY=64
Fullscreen=True
AllowOpenGL=False
AllowRadialBlur=False
AllowSubsurfaceScattering=False
AllowImageReflections=True
AllowImageReflectionShadowing=True
bAllowSeparateTranslucency=False
bAllowPostprocessMLAA=False
bAllowHighQualityMaterials=True
bUseTranslucentArenaShaders=True
MaxFilterBlurSampleCount=2
SkeletalMeshLODBias=0
ParticleLODBias=0
DetailMode=2
MaxDrawDistanceScale=1
ShadowFilterQualityBias=0
MaxAnisotropy=16
MaxMultiSamples=0
bAllowD3D9MSAA=False
bAllowTemporalAA=False
TemporalAA_MinDepth=500.000000
TemporalAA_StartDepthVelocityScale=100.000000
MinShadowResolution=16
MinPreShadowResolution=8
MaxShadowResolution=16
MobileShadowTextureResolution=1120
MaxWholeSceneDominantShadowResolution=16
ShadowFadeResolution=32
PreShadowFadeResolution=16
ShadowFadeExponent=0.250000
ResX=1920
ResY=1080
AutoDetectDesktopResolution=False
Borderless=False
AllowApexCloth=true
ScreenPercentage=100.000000
SceneCaptureStreamingMultiplier=0.000000
ShadowTexelsPerPixel=0.000000
PreShadowResolutionFactor=0.500000
bEnableBranchingPCFShadows=False
bAllowHardwareShadowFiltering=False
TessellationAdaptivePixelsPerTriangle=0.000000
bEnableForegroundShadowsOnWorld=True
bEnableForegroundSelfShadowing=False
bAllowWholeSceneDominantShadows=False
bUseConservativeShadowBounds=False
ShadowFilterRadius=2.000000
ShadowDepthBias=0.012000
PerObjectShadowTransition=60.000000
PerSceneShadowTransition=600.000000
CSMSplitPenumbraScale=0.000000
CSMSplitSoftTransitionDistanceScale=4.000000
CSMSplitDepthBiasScale=0.500000
CSMMinimumFOV=40.000000
CSMFOVRoundFactor=4.000000
UnbuiltWholeSceneDynamicShadowRadius=20000.000000
UnbuiltNumWholeSceneDynamicShadowCascades=3
WholeSceneShadowUnbuiltInteractionThreshold=50
bAllowFracturedDamage=False
NumFracturedPartsScale=0.000000
FractureDirectSpawnChanceScale=0.000000
FractureRadialSpawnChanceScale=0.000000
FractureCullDistanceScale=0.000000
bForceCPUAccessToGPUSkinVerts=false
bDisableSkeletalInstanceWeights=false
HighPrecisionGBuffers=False
AllowSecondaryDisplays=False
SecondaryDisplayMaximumWidth=1280
SecondaryDisplayMaximumHeight=720
AllowPerFrameSleep=True
AllowPerFrameYield=True
MobileFeatureLevel=0
MobileFog=False
MobileHeightFog=False
MobileSpecular=True
MobileBumpOffset=True
MobileNormalMapping=True
MobileEnvMapping=True
MobileRimLighting=True
MobileColorBlending=True
MobileColorGrading=False
MobileVertexMovement=True
MobileOcclusionQueries=False
MobileGlobalGammaCorrection=False
MobileAllowGammaCorrectionWorldOverride=False
MobileAllowDepthPrePass=False
MobileGfxGammaCorrection=False
MobileLODBias=-0.5
MobileBoneCount=75
MobileBoneWeightCount=2
MobileUsePreprocessedShaders=True
MobileFlashRedForUncachedShaders=False
MobileWarmUpPreprocessedShaders=True
MobileCachePreprocessedShaders=False
MobileProfilePreprocessedShaders=False
MobileUseCPreprocessorOnShaders=True
MobileLoadCPreprocessedShaders=True
MobileSharePixelShaders=True
MobileShareVertexShaders=True
MobileShareShaderPrograms=True
MobileEnableMSAA=False
MobileContentScaleFactor=1.0
MobileVertexScratchBufferSize=150
MobileIndexScratchBufferSize=10
MobileLightShaftScale=0
MobileLightShaftFirstPass=0
MobileLightShaftSecondPass=0
MobileModShadows=False
MobileMinimizeFogShaders=TRUE
MobileTiltShift=False
MobileMaxMemory=300
MobilePostProcessBlurAmount=32.0
bMobileUsingHighResolutionTiming=True
MobileTiltShiftPosition=0.5
MobileTiltShiftFocusWidth=0.3
MobileTiltShiftTransitionWidth=0.5
MobileMaxShadowRange=500.0
MobileBloomTint=(R=1.0,G=0.75,B=0.0,A=1.0)
MobileClearDepthBetweenDPG=False
MobileSceneDepthResolveForShadows=TRUE
MobileLandscapeLodBias=0
MobileUseShaderGroupForStartupObjects=FALSE
MobileMinimizeFogShaders=FALSE
MobileFXAAQuality=0
ApexLODResourceBudget=1000000000000000000000.0
ApexDestructionMaxChunkIslandCount=0
ApexDestructionMaxShapeCount=0
ApexDestructionMaxChunkSeparationLOD=1.0
ApexDestructionMaxActorCreatesPerFrame=-1
ApexDestructionMaxFracturesProcessedPerFrame=-1
ApexDestructionSortByBenefit=True
ApexGRBEnable=false
ApexGRBGPUMemSceneSize=128
ApexGRBGPUMemTempDataSize=128
ApexGRBMeshCellSize=7.5
ApexGRBNonPenSolverPosIterCount=9;
ApexGRBFrictionSolverPosIterCount=3;
ApexGRBFrictionSolverVelIterCount=3;
ApexGRBSkinWidth=0.025
ApexGRBMaxLinearAcceleration=1000000.0
bEnableParallelAPEXClothingFetch=True
bApexClothingAsyncFetchResults=False
ApexClothingAvgSimFrequencyWindow=60
ApexClothingAllowAsyncCooking=True
ApexClothingAllowApexWorkBetweenSubsteps=FALSE
TEXTUREGROUP_World=(MinLODSize=1,MaxLODSize=1024,LODBias=0,MinMagFilter=Aniso,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_WorldNormalMap=(MinLODSize=1,MaxLODSize=512,LODBias=0,MinMagFilter=Aniso,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_WorldSpecular=(MinLODSize=1,MaxLODSize=512,LODBias=0,MinMagFilter=Aniso,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_Character=(MinLODSize=1,MaxLODSize=1024,LODBias=0,MinMagFilter=Aniso,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_CharacterNormalMap=(MinLODSize=1,MaxLODSize=512,LODBias=0,MinMagFilter=Aniso,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_CharacterSpecular=(MinLODSize=1,MaxLODSize=512,LODBias=0,MinMagFilter=Aniso,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_Weapon=(MinLODSize=1,MaxLODSize=512,LODBias=0,MinMagFilter=Aniso,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_WeaponNormalMap=(MinLODSize=1,MaxLODSize=256,LODBias=0,MinMagFilter=Aniso,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_WeaponSpecular=(MinLODSize=1,MaxLODSize=256,LODBias=0,MinMagFilter=Aniso,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_Vehicle=(MinLODSize=1,MaxLODSize=1024,LODBias=0,MinMagFilter=Aniso,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_VehicleNormalMap=(MinLODSize=1,MaxLODSize=1024,LODBias=0,MinMagFilter=Aniso,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_VehicleSpecular=(MinLODSize=1,MaxLODSize=1024,LODBias=0,MinMagFilter=Aniso,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_Cinematic=(MinLODSize=1,MaxLODSize=1024,LODBias=0,MinMagFilter=Aniso,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_Effects=(MinLODSize=1,MaxLODSize=512,LODBias=0,MinMagFilter=Aniso,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_EffectsNotFiltered=(MinLODSize=1,MaxLODSize=512,LODBias=0,MinMagFilter=Aniso,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_Skybox=(MinLODSize=1,MaxLODSize=512,LODBias=0,MinMagFilter=Aniso,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_UI=(MinLODSize=1,MaxLODSize=1024,LODBias=0,MinMagFilter=Aniso,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_Lightmap=(MinLODSize=1,MaxLODSize=512,LODBias=0,MinMagFilter=Aniso,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_Shadowmap=(MinLODSize=1,MaxLODSize=512,LODBias=0,MinMagFilter=Aniso,MipFilter=Point,NumStreamedMips=3,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_RenderTarget=(MinLODSize=1,MaxLODSize=2048,LODBias=0,MinMagFilter=Aniso,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_MobileFlattened=(MinLODSize=1,MaxLODSize=512,LODBias=0,MinMagFilter=Aniso,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_ProcBuilding_Face=(MinLODSize=1,MaxLODSize=512,LODBias=0,MinMagFilter=Aniso,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_ProcBuilding_LightMap=(MinLODSize=1,MaxLODSize=512,LODBias=0,MinMagFilter=Aniso,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_Terrain_Heightmap=(MinLODSize=1,MaxLODSize=512,LODBias=0,MinMagFilter=Aniso,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_Terrain_Weightmap=(MinLODSize=1,MaxLODSize=512,LODBias=0,MinMagFilter=Aniso,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_ImageBasedReflection=(MinLODSize=1,MaxLODSize=512,LODBias=0,MinMagFilter=Aniso,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
TEXTUREGROUP_Bokeh=(MinLODSize=1,MaxLODSize=512,LODBias=0,MinMagFilter=Aniso,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
FullEffectIntensity=False
bAllowBetterModulatedShadows=FALSE
UncappedFramerate=True
bSmoothFrameRate=False
TEXTUREGROUP_ColorLookupTable=(MinLODSize=1,MaxLODSize=512,LODBias=0,MinMagFilter=Aniso,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)
CustomFPS=0
TEXTUREGROUP_Pitch=(MinLODSize=1,MaxLODSize=2048,LODBias=0,MinMagFilter=Aniso,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)

[SystemSettingsBucket1]
BasedOn=SystemSettings

[SystemSettingsBucket2]
BasedOn=SystemSettings

[SystemSettingsBucket3]
BasedOn=SystemSettings

[SystemSettingsBucket4]
BasedOn=SystemSettings

[SystemSettingsBucket5]
BasedOn=SystemSettings

[SystemSettingsScreenshot]
BasedOn=SystemSettings
MaxAnisotropy=16
ShadowFilterQualityBias=1
MinShadowResolution=16
ShadowFadeResolution=1
MinPreShadowResolution=16
PreShadowFadeResolution=1
ShadowTexelsPerPixel=4.0f
PreShadowResolutionFactor=1.0
MaxShadowResolution=4096
MaxWholeSceneDominantShadowResolution=4096
CompositeDynamicLights=FALSE
TEXTUREGROUP_World=(MinLODSize=1,MaxLODSize=4096,LODBias=-1000,MinMagFilter=aniso,MipFilter=linear)
TEXTUREGROUP_WorldNormalMap=(MinLODSize=1,MaxLODSize=4096,LODBias=-1000,MinMagFilter=aniso,MipFilter=linear)
TEXTUREGROUP_WorldSpecular=(MinLODSize=1,MaxLODSize=4096,LODBias=-1000,MinMagFilter=aniso,MipFilter=linear)
TEXTUREGROUP_Character=(MinLODSize=1,MaxLODSize=4096,LODBias=-1000,MinMagFilter=aniso,MipFilter=linear)
TEXTUREGROUP_CharacterNormalMap=(MinLODSize=1,MaxLODSize=4096,LODBias=-1000,MinMagFilter=aniso,MipFilter=linear)
TEXTUREGROUP_CharacterSpecular=(MinLODSize=1,MaxLODSize=4096,LODBias=-1000,MinMagFilter=aniso,MipFilter=linear)
TEXTUREGROUP_Weapon=(MinLODSize=1,MaxLODSize=4096,LODBias=-1000,MinMagFilter=aniso,MipFilter=linear)
TEXTUREGROUP_WeaponNormalMap=(MinLODSize=1,MaxLODSize=4096,LODBias=-1000,MinMagFilter=aniso,MipFilter=linear)
TEXTUREGROUP_WeaponSpecular=(MinLODSize=1,MaxLODSize=4096,LODBias=-1000,MinMagFilter=aniso,MipFilter=linear)
TEXTUREGROUP_Vehicle=(MinLODSize=1,MaxLODSize=4096,LODBias=-1000,MinMagFilter=aniso,MipFilter=linear)
TEXTUREGROUP_VehicleNormalMap=(MinLODSize=1,MaxLODSize=4096,LODBias=-1000,MinMagFilter=aniso,MipFilter=linear)
TEXTUREGROUP_VehicleSpecular=(MinLODSize=1,MaxLODSize=4096,LODBias=-1000,MinMagFilter=aniso,MipFilter=linear)
TEXTUREGROUP_Cinematic=(MinLODSize=1,MaxLODSize=4096,LODBias=-1000,MinMagFilter=aniso,MipFilter=linear)
TEXTUREGROUP_Effects=(MinLODSize=1,MaxLODSize=4096,LODBias=-1000,MinMagFilter=linear,MipFilter=linear)
TEXTUREGROUP_EffectsNotFiltered=(MinLODSize=1,MaxLODSize=4096,LODBias=-1000,MinMagFilter=aniso,MipFilter=linear)
TEXTUREGROUP_Skybox=(MinLODSize=1,MaxLODSize=4096,LODBias=-1000,MinMagFilter=aniso,MipFilter=linear)
TEXTUREGROUP_UI=(MinLODSize=1,MaxLODSize=4096,LODBias=-1000,MinMagFilter=aniso,MipFilter=linear)
TEXTUREGROUP_Lightmap=(MinLODSize=1,MaxLODSize=4096,LODBias=-1000,MinMagFilter=aniso,MipFilter=linear)
TEXTUREGROUP_Shadowmap=(MinLODSize=1,MaxLODSize=4096,LODBias=-1000,MinMagFilter=aniso,MipFilter=linear)
TEXTUREGROUP_RenderTarget=(MinLODSize=1,MaxLODSize=4096,LODBias=-1000,MinMagFilter=aniso,MipFilter=linear)
TEXTUREGROUP_MobileFlattened=(MinLODSize=1,MaxLODSize=4096,LODBias=-1000,MinMagFilter=aniso,MipFilter=linear)
TEXTUREGROUP_ProcBuilding_Face=(MinLODSize=1,MaxLODSize=4096,LODBias=-1000,MinMagFilter=aniso,MipFilter=linear)
TEXTUREGROUP_ProcBuilding_LightMap=(MinLODSize=1,MaxLODSize=4096,LODBias=-1000,MinMagFilter=aniso,MipFilter=linear)
TEXTUREGROUP_Terrain_Heightmap=(MinLODSize=1,MaxLODSize=4096,LODBias=-1000,MinMagFilter=aniso,MipFilter=linear)
TEXTUREGROUP_Terrain_Weightmap=(MinLODSize=1,MaxLODSize=4096,LODBias=-1000,MinMagFilter=aniso,MipFilter=linear)

[SystemSettingsEditor]
BasedOn=SystemSettings
ResX=1280
ResY=720

[SystemSettingsSplitScreen2]
BasedOn=SystemSettings
bAllowWholeSceneDominantShadows=False
bAllowLightShafts=False
DetailMode=0

[SystemSettingsMobile]
BasedOn=SystemSettings
Fullscreen=True
DirectionalLightmaps=False
DynamicLights=False
SHSecondaryLighting=False
StaticDecals=False
DynamicDecals=False
UnbatchedDecals=False
MotionBlur=FALSE
MotionBlurPause=FALSE
DepthOfField=FALSE
AmbientOcclusion=FALSE
Bloom=FALSE
Distortion=FALSE
FilteredDistortion=FALSE
DropParticleDistortion=False
FloatingPointRenderTargets=FALSE
MaxAnisotropy=2
bAllowLightShafts=FALSE
MobileModShadows=False
MobileClearDepthBetweenDPG=False
MaxFilterBlurSampleCount=4
DynamicShadows=False
MobileMaxMemory=300
MobileLandscapeLodBias=0
AllowRadialBlur=False

[SystemSettingsMobilePreviewer]
BasedOn=SystemSettingsMobile
Fullscreen=False

[SystemSettingsMobileTextureBias]
BasedOn=SystemSettingsMobile
TEXTUREGROUP_World=(MinLODSize=1,MaxLODSize=4096,LODBias=1,MinMagFilter=aniso,MipFilter=point)
TEXTUREGROUP_WorldNormalMap=(MinLODSize=1,MaxLODSize=4096,LODBias=1,MinMagFilter=aniso,MipFilter=point)
TEXTUREGROUP_WorldSpecular=(MinLODSize=1,MaxLODSize=4096,LODBias=1,MinMagFilter=aniso,MipFilter=point)
TEXTUREGROUP_Character=(MinLODSize=1,MaxLODSize=4096,LODBias=1,MinMagFilter=aniso,MipFilter=point)
TEXTUREGROUP_CharacterNormalMap=(MinLODSize=1,MaxLODSize=4096,LODBias=1,MinMagFilter=aniso,MipFilter=point)
TEXTUREGROUP_CharacterSpecular=(MinLODSize=1,MaxLODSize=4096,LODBias=1,MinMagFilter=aniso,MipFilter=point)
TEXTUREGROUP_Weapon=(MinLODSize=1,MaxLODSize=4096,LODBias=1,MinMagFilter=aniso,MipFilter=point)
TEXTUREGROUP_WeaponNormalMap=(MinLODSize=1,MaxLODSize=4096,LODBias=1,MinMagFilter=aniso,MipFilter=point)
TEXTUREGROUP_WeaponSpecular=(MinLODSize=1,MaxLODSize=4096,LODBias=1,MinMagFilter=aniso,MipFilter=point)
TEXTUREGROUP_Vehicle=(MinLODSize=1,MaxLODSize=4096,LODBias=1,MinMagFilter=aniso,MipFilter=point)
TEXTUREGROUP_VehicleNormalMap=(MinLODSize=1,MaxLODSize=4096,LODBias=1,MinMagFilter=aniso,MipFilter=point)
TEXTUREGROUP_VehicleSpecular=(MinLODSize=1,MaxLODSize=4096,LODBias=1,MinMagFilter=aniso,MipFilter=point)
TEXTUREGROUP_Cinematic=(MinLODSize=1,MaxLODSize=4096,LODBias=1,MinMagFilter=aniso,MipFilter=point)
TEXTUREGROUP_Effects=(MinLODSize=1,MaxLODSize=4096,LODBias=1,MinMagFilter=linear,MipFilter=point)
TEXTUREGROUP_EffectsNotFiltered=(MinLODSize=1,MaxLODSize=4096,LODBias=1,MinMagFilter=aniso,MipFilter=point)
TEXTUREGROUP_Skybox=(MinLODSize=1,MaxLODSize=4096,LODBias=1,MinMagFilter=aniso,MipFilter=point)
TEXTUREGROUP_UI=(MinLODSize=1,MaxLODSize=4096,LODBias=0,MinMagFilter=aniso,MipFilter=point)
TEXTUREGROUP_Lightmap=(MinLODSize=1,MaxLODSize=4096,LODBias=1,MinMagFilter=aniso,MipFilter=point)
TEXTUREGROUP_Shadowmap=(MinLODSize=1,MaxLODSize=4096,LODBias=1,MinMagFilter=aniso,MipFilter=point,NumStreamedMips=3)
TEXTUREGROUP_RenderTarget=(MinLODSize=1,MaxLODSize=4096,LODBias=1,MinMagFilter=aniso,MipFilter=point)
TEXTUREGROUP_MobileFlattened=(MinLODSize=1,MaxLODSize=4096,LODBias=1,MinMagFilter=aniso,MipFilter=point)
TEXTUREGROUP_ProcBuilding_Face=(MinLODSize=1,MaxLODSize=1024,LODBias=1,MinMagFilter=aniso,MipFilter=point)
TEXTUREGROUP_ProcBuilding_LightMap=(MinLODSize=1,MaxLODSize=256,LODBias=1,MinMagFilter=aniso,MipFilter=point)
TEXTUREGROUP_Terrain_Heightmap=(MinLODSize=1,MaxLODSize=4096,LODBias=1,MinMagFilter=aniso,MipFilter=point)
TEXTUREGROUP_Terrain_Weightmap=(MinLODSize=1,MaxLODSize=4096,LODBias=1,MinMagFilter=aniso,MipFilter=point)

[SystemSettingsAndroid]
BasedOn=SystemSettingsMobileTextureBias

[SystemSettingsAndroid_Performance1_MemoryLow]
BasedOn=SystemSettingsMobileTextureBias
MobileFeatureLevel=1
MobileFog=False
MobileSpecular=False
MobileBumpOffset=False
MobileNormalMapping=False
MobileEnvMapping=False
MobileRimLighting=False
MobileContentScaleFactor=0.9375

[SystemSettingsAndroid_Performance2_MemoryLow]
BasedOn=SystemSettingsMobileTextureBias
MobileBumpOffset=False
MobileNormalMapping=False
MobileContentScaleFactor=0.9375

[SystemSettingsAndroid_Performance1_Memory1024]
BasedOn=SystemSettingsMobile
MobileFeatureLevel=1
MobileFog=False
MobileSpecular=False
MobileBumpOffset=False
MobileNormalMapping=False
MobileEnvMapping=False
MobileRimLighting=False
MobileContentScaleFactor=0.9375

[SystemSettingsAndroid_Performance2_Memory1024]
BasedOn=SystemSettingsMobile
MobileBumpOffset=False
MobileNormalMapping=False
MobileContentScaleFactor=0.9375

[SystemSettingsFlash]
BasedOn=SystemSettingsMobileTextureBias
MotionBlur=FALSE
MotionBlurPause=FALSE
DepthOfField=FALSE
AmbientOcclusion=FALSE
Bloom=FALSE
Distortion=FALSE
FilteredDistortion=FALSE
bAllowLightShafts=FALSE
MobileModShadows=True
DynamicShadows=True
MobileClearDepthBetweenDPG=True
DirectionalLightmaps=False
MobileHeightFog=False

[SystemSettingsIPhone]
BasedOn=SystemSettingsMobileTextureBias
bMobileUsingHighResolutionTiming=False

[SystemSettingsIPhone3GS]
BasedOn=SystemSettingsMobileTextureBias
LensFlares=False
DetailMode=0
MobileEnableMSAA=False
MobileMaxMemory=100
bMobileUsingHighResolutionTiming=False
MobileLandscapeLodBias=2

[SystemSettingsIPhone4]
BasedOn=SystemSettingsMobile
MobileContentScaleFactor=2.0
LensFlares=False
bMobileUsingHighResolutionTiming=False
MobileLandscapeLodBias=1

[SystemSettingsIPhone4S]
BasedOn=SystemSettingsMobile
MobileEnableMSAA=False
bAllowLightShafts=False
MobileModShadows=True
DynamicShadows=False
ShadowDepthBias=0.025
MobileContentScaleFactor=2.0
MaxShadowResolution=256
MobileShadowTextureResolution=256

[SystemSettingsIPhone5]
BasedOn=SystemSettingsMobile
MobileEnableMSAA=False
bAllowLightShafts=False
MobileModShadows=True
DynamicShadows=False
ShadowDepthBias=0.025
MobileContentScaleFactor=2.0
MaxShadowResolution=256
MobileShadowTextureResolution=1024
AllowRadialBlur=True

[SystemSettingsIPodTouch4]
BasedOn=SystemSettingsMobileTextureBias
MobileContentScaleFactor=2.0
LensFlares=False
MobileMaxMemory=100
bMobileUsingHighResolutionTiming=False
MobileLandscapeLodBias=2

[SystemSettingsIPodTouch5]
BasedOn=SystemSettingsMobile
MobileEnableMSAA=False
bAllowLightShafts=False
MobileModShadows=True
DynamicShadows=False
ShadowDepthBias=0.025
MobileContentScaleFactor=2.0
MaxShadowResolution=256
MobileShadowTextureResolution=256

[SystemSettingsIPad]
BasedOn=SystemSettingsMobileTextureBias
MobileFeatureLevel=1
MobileFog=False
MobileSpecular=False
MobileBumpOffset=False
MobileNormalMapping=False
MobileEnvMapping=False
MobileRimLighting=False
MobileMaxMemory=100
bMobileUsingHighResolutionTiming=False
MobileLandscapeLodBias=1
MobileContentScaleFactor=0.9375

[SystemSettingsIPad2]
BasedOn=SystemSettingsMobile
MobileEnableMSAA=False
bAllowLightShafts=False
MobileModShadows=True
DynamicShadows=False
ShadowDepthBias=0.016
MobileContentScaleFactor=1.0
MaxShadowResolution=256
MobileShadowTextureResolution=256

[SystemSettingsIPad3]
BasedOn=SystemSettingsMobile
MobileEnableMSAA=False
bAllowLightShafts=False
MobileModShadows=True
DynamicShadows=True
ShadowDepthBias=0.016
MobileContentScaleFactor=1.40625
MaxShadowResolution=256
MobileShadowTextureResolution=256
MobileMaxMemory=500

[SystemSettingsIPad4]
BasedOn=SystemSettingsMobile
MobileEnableMSAA=False
bAllowLightShafts=False
MobileModShadows=True
DynamicShadows=True
ShadowDepthBias=0.016
MobileContentScaleFactor=2.0
MaxShadowResolution=512
MobileShadowTextureResolution=512
MobileMaxMemory=500
AllowRadialBlur=True

[SystemSettingsIPadMini]
BasedOn=SystemSettingsMobile
MobileEnableMSAA=False
bAllowLightShafts=False
MobileModShadows=True
DynamicShadows=False
ShadowDepthBias=0.016
MobileContentScaleFactor=1.0
MaxShadowResolution=256
MobileShadowTextureResolution=256

[SystemSettingsIPad2_Detail]
BasedOn=SystemSettingsIPad2

[Configuration]

[SystemSettingsTexturesDerp]
BasedOn=SystemSettings
TEXTUREGROUP_Character=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_CharacterNormalMap=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_CharacterSpecular=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_Effects=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_LightAndShadowMap=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_RenderTarget=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_Skybox=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_UI=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_Vehicle=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_VehicleNormalMap=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_VehicleSpecular=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_Weapon=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_WeaponNormalMap=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_WeaponSpecular=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_World=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_WorldNormalMap=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_WorldSpecular=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_Cinematic=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_EffectsNotFiltered=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_Lightmap=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_Shadowmap=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_MobileFlattened=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_ProcBuilding_Face=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_ProcBuilding_LightMap=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_Terrain_Heightmap=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_Terrain_Weightmap=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_ImageBasedReflection=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_Bokeh=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_ColorLookupTable=(MinLODSize=1,MaxLODSize=512,LODBias=0)

[SystemSettingsTexturesLow]
BasedOn=SystemSettings
TEXTUREGROUP_Character=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_CharacterNormalMap=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_CharacterSpecular=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_Effects=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_LightAndShadowMap=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_RenderTarget=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_Skybox=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_UI=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_Vehicle=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_VehicleNormalMap=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_VehicleSpecular=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_Weapon=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_WeaponNormalMap=(MinLODSize=1,MaxLODSize=256,LODBias=0)
TEXTUREGROUP_WeaponSpecular=(MinLODSize=1,MaxLODSize=256,LODBias=0)
TEXTUREGROUP_World=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_WorldNormalMap=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_WorldSpecular=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_Cinematic=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_EffectsNotFiltered=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_Lightmap=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_Shadowmap=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_MobileFlattened=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_ProcBuilding_Face=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_ProcBuilding_LightMap=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_Terrain_Heightmap=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_Terrain_Weightmap=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_ImageBasedReflection=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_Bokeh=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_ColorLookupTable=(MinLODSize=1,MaxLODSize=512,LODBias=0)

[SystemSettingsTexturesMedium]
BasedOn=SystemSettings
TEXTUREGROUP_Character=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_CharacterNormalMap=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_CharacterSpecular=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_Effects=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_LightAndShadowMap=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_RenderTarget=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_Skybox=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_UI=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_Vehicle=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_VehicleNormalMap=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_VehicleSpecular=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_Weapon=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_WeaponNormalMap=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_WeaponSpecular=(MinLODSize=1,MaxLODSize=256,LODBias=0)
TEXTUREGROUP_World=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_WorldNormalMap=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_WorldSpecular=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_Cinematic=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_EffectsNotFiltered=(MinLODSize=1,MaxLODSize=512,LODBias=0)
TEXTUREGROUP_Lightmap=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_Shadowmap=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_MobileFlattened=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_ProcBuilding_Face=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_ProcBuilding_LightMap=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_Terrain_Heightmap=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_Terrain_Weightmap=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_ImageBasedReflection=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_Bokeh=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_ColorLookupTable=(MinLODSize=1,MaxLODSize=1024,LODBias=0)

[SystemSettingsTexturesHigh]
BasedOn=SystemSettings
TEXTUREGROUP_Character=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_CharacterNormalMap=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_CharacterSpecular=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_Effects=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_LightAndShadowMap=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_RenderTarget=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_Skybox=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_UI=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_Vehicle=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_VehicleNormalMap=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_VehicleSpecular=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_Weapon=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_WeaponNormalMap=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_WeaponSpecular=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_World=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_WorldNormalMap=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_WorldSpecular=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_Cinematic=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_EffectsNotFiltered=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_Lightmap=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_Shadowmap=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_MobileFlattened=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_ProcBuilding_Face=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_ProcBuilding_LightMap=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_Terrain_Heightmap=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_Terrain_Weightmap=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_ImageBasedReflection=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_Bokeh=(MinLODSize=1,MaxLODSize=1024,LODBias=0)
TEXTUREGROUP_ColorLookupTable=(MinLODSize=1,MaxLODSize=1024,LODBias=0)

[SystemSettingsTexturesHigher]
BasedOn=SystemSettings
TEXTUREGROUP_Character=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_CharacterNormalMap=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_CharacterSpecular=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_Effects=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_LightAndShadowMap=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_RenderTarget=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_Skybox=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_UI=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_Vehicle=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_VehicleNormalMap=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_VehicleSpecular=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_Weapon=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_WeaponNormalMap=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_WeaponSpecular=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_World=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_WorldNormalMap=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_WorldSpecular=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_Cinematic=(MinLODSize=1,MaxLODSize=4096,LODBias=0)
TEXTUREGROUP_EffectsNotFiltered=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_Lightmap=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_Shadowmap=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_MobileFlattened=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_ProcBuilding_Face=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_ProcBuilding_LightMap=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_Terrain_Heightmap=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_Terrain_Weightmap=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_ImageBasedReflection=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_Bokeh=(MinLODSize=1,MaxLODSize=2048,LODBias=0)
TEXTUREGROUP_ColorLookupTable=(MinLODSize=1,MaxLODSize=2048,LODBias=0)

[SystemSettingsProfileDetailLow]
BasedOn=SystemSettings
DetailMode=0
AmbientOcclusion=False
DepthOfField=False
Bloom=False
bAllowLightShafts=False
LensFlares=False
DynamicShadows=False
MotionBlur=False

[SystemSettingsProfileDetailHigh]
BasedOn=SystemSettings
DetailMode=2
AmbientOcclusion=False
DepthOfField=False
Bloom=False
bAllowLightShafts=False
LensFlares=False
DynamicShadows=False
MotionBlur=False

[IniVersion]
0=1769378095.000000
1=1769378095.000000

```


---

## 11. Ficheiros de código relevantes (para o roteirista saber o que existe)

| Ficheiro | Função |
|---|---|
| `dotnet/Program.cs` | Menu, apply, REMOVER, CORRIGIR, RESTAURAR PRESETS |
| `dotnet/CompletoForce.cs` | Força potato + escala 100% em todas as secções |
| `dotnet/CriadorForce.cs` | Força otimizações só em secções filhas |
| `dotnet/VideoSettingsSync.cs` | Backup + chama patch do .save |
| `dotnet/SaveVideoPatcher.cs` | Invoca Python embutido |
| `tools/patch_save_video.py` | Patch VideoOptions / EffectIntensity |
| `tools/save_codec.py` | Serialização UE3 (NameProperty Id/Value) |
| `templates/INI_COMPLETO.txt` | Template potato |
| `templates/INI_CRIADOR.txt` | Template criador |
| `templates/INI_STOCK_REFERENCE.txt` | Stock de referência |
| `legacy/RL_GUTTYTECH_v21.5.bat` | Script antigo (não usar) |
| `CHANGELOG.md` | Histórico oficial de versões |
| `README.txt` / `DESCRICAO.md` | Docs (DESCRICAO pode ter menu/tamanho ligeiramente desatualizados) |

---

## 12. Frase de marca (opcional no vídeo)

> "Você vai otimizar o jogo ou vai continuar sofrendo por culpa da engine burra do jogo?"

Paleta: fundo `#0A0A0A`, CTA `#E50A0A`.

---

*Fim do dossiê v22.3.35 — só factos. O roteirista monta o roteiro a partir daqui.*
