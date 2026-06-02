@echo off
:: FORCA O KERNEL A LER ACENTOS (UTF-8) PARA NAO BUGAR EM PASTAS COMO "Usuário"
chcp 65001 >nul
setlocal EnableExtensions EnableDelayedExpansion
color 0C
title GUTTYTECH - RL ENGINE NUKER V21.0 (PROJECT TESSERACT FINAL)

:: ============================================================================
:: ELEVACAO DE PRIVILEGIO (RING 0)
:: ============================================================================
>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"
if '%errorlevel%' NEQ '0' (
    echo [+] GUTTYTECH: AUTORIZACAO DE KERNEL MAXIMA REQUERIDA...
    echo Set UAC = CreateObject^("Shell.Application"^) > "%temp%\getadmin.vbs"
    echo UAC.ShellExecute "%~s0", "", "", "runas", 1 >> "%temp%\getadmin.vbs"
    "%temp%\getadmin.vbs"
    exit /B
)
if exist "%temp%\getadmin.vbs" ( del "%temp%\getadmin.vbs" )

echo +=======================================================+
echo ^| GUTTYTECH RL NUKER v21.0 - PROJECT TESSERACT FINAL   ^|
echo ^| "O jogo acabou. Lobotomizando o Windows..."          ^|
echo +=======================================================+
echo.

:: ============================================================================
:: FASE 1: ATAQUE AO KERNEL (REDE, TIMERS E PRIORIDADE)
:: ============================================================================
echo [+] FASE 1/4: CORTANDO TIMERS E INJETANDO HITREG PERFEITO (TCP/IP)...

bcdedit /deletevalue useplatformclock >nul 2>&1
bcdedit /set disabledynamictick yes >nul 2>&1
bcdedit /set tscsyncpolicy Enhanced >nul 2>&1

reg add "HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl" /v "Win32PrioritySeparation" /t REG_DWORD /d 40 /f >nul 2>&1

for /f "tokens=3*" %%i in ('reg query "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces" /s ^| findstr /i "Name"') do (
    reg add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\%%i" /v "TcpAckFrequency" /t REG_DWORD /d 1 /f >nul 2>&1
    reg add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\%%i" /v "TCPNoDelay" /t REG_DWORD /d 1 /f >nul 2>&1
    reg add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\%%i" /v "TcpDelAckTicks" /t REG_DWORD /d 0 /f >nul 2>&1
)

:: ============================================================================
:: FASE 2: BUSCA IMPLACAVEL DO ALVO (BYPASS DE ONEDRIVE E ACENTOS)
:: ============================================================================
echo [+] FASE 2/4: RASTREANDO ARQUIVO DO ROCKET LEAGUE...

set "TARGET_FILE=My Games\Rocket League\TAGame\Config\TASystemSettings.ini"
set "RL_CONFIG_PATH="

:: --- BUSCA DIRETA NO PERFIL DO USUARIO ATUAL (resolve 99%% dos casos) ---
if exist "%USERPROFILE%\Documents\%TARGET_FILE%" (
    set "RL_CONFIG_PATH=%USERPROFILE%\Documents\%TARGET_FILE%"
) else if exist "%USERPROFILE%\OneDrive\Documents\%TARGET_FILE%" (
    set "RL_CONFIG_PATH=%USERPROFILE%\OneDrive\Documents\%TARGET_FILE%"
) else if exist "%USERPROFILE%\OneDrive - Personal\Documents\%TARGET_FILE%" (
    set "RL_CONFIG_PATH=%USERPROFILE%\OneDrive - Personal\Documents\%TARGET_FILE%"
)

:: --- FALLBACK: busca em todos os perfis do PC (caso o usuario tenha multiplas contas) ---
if "!RL_CONFIG_PATH!"=="" (
    for /d %%U in ("C:\Users\*") do (
        if exist "%%U\Documents\!TARGET_FILE!" set "RL_CONFIG_PATH=%%U\Documents\!TARGET_FILE!"
        if exist "%%U\OneDrive\Documents\!TARGET_FILE!" set "RL_CONFIG_PATH=%%U\OneDrive\Documents\!TARGET_FILE!"
        if exist "%%U\OneDrive - Personal\Documents\!TARGET_FILE!" set "RL_CONFIG_PATH=%%U\OneDrive - Personal\Documents\!TARGET_FILE!"
    )
)

:: --- VERIFICACAO FINAL ---
if "!RL_CONFIG_PATH!"=="" (
    color 0E
    echo [-] ALVO NAO DETECTADO.
    echo.
    echo [!] O arquivo TASystemSettings.ini nao foi encontrado.
    echo     Isso acontece quando:
    echo       1. O Rocket League NUNCA foi aberto neste PC.
    echo       2. A pasta Documents esta no OneDrive com outro nome.
    echo.
    echo [?] SOLUCAO:
    echo     1. Abra o Rocket League ate o menu principal (onde aparece o carro).
    echo     2. Feche o jogo completamente.
    echo     3. Rode este script novamente.
    echo.
    echo [?] Se o erro persistir, verifique manualmente:
    echo     Win+R -^> cole: "%USERPROFILE%\Documents\My Games\Rocket League\TAGame\Config"
    echo.
    pause >nul
    exit /b 1
)

echo [+] ALVO LOCALIZADO: !RL_CONFIG_PATH!

:: ============================================================================
:: FASE 3: DESBLOQUEIO DE KERNEL (CRITICO)
:: ============================================================================
echo [+] FASE 3/4: QUEBRANDO BLINDAGEM DO ARQUIVO (READ-ONLY BYPASS)...
attrib -r -h -s "!RL_CONFIG_PATH!" >nul 2>&1
icacls "!RL_CONFIG_PATH!" /grant "%USERNAME%":F /t /c /q >nul 2>&1

set "RL_TARGET=!RL_CONFIG_PATH!"

:: ============================================================================
:: FASE 4: LOBOTOMIA MAXIMA DA UNREAL ENGINE 3
:: ============================================================================
echo [+] FASE 4/4: REDUZINDO ENGINE A BLOCOS MATEMATICOS...

set "PS_SCRIPT=%TEMP%\RL_Tesseract_V21.ps1"
if exist "%PS_SCRIPT%" del "%PS_SCRIPT%"

> "%PS_SCRIPT%" (
    echo $ErrorActionPreference = 'SilentlyContinue'
    echo $f = $env:RL_TARGET
    echo $c = Get-Content $f
    
    :: Sincronia e FPS
    echo $c = $c -replace '^\s*UncappedFramerate=.*', 'UncappedFramerate=True'
    echo $c = $c -replace '^\s*WaitForGPU=.*', 'WaitForGPU=True'
    echo $c = $c -replace '^\s*OneFrameThreadLag=.*', 'OneFrameThreadLag=True'
    echo $c = $c -replace '^\s*AllowPerFrameSleep=.*', 'AllowPerFrameSleep=True'
    echo $c = $c -replace '^\s*AllowPerFrameYield=.*', 'AllowPerFrameYield=True'
    echo $c = $c -replace '^\s*UseVsync=.*', 'UseVsync=False'
    echo $c = $c -replace '^\s*bSmoothFrameRate=.*', 'bSmoothFrameRate=False'
    
    :: Texturas e LOD
    echo $c = $c -replace 'MaxLODSize=\d+', 'MaxLODSize=16'
    echo $c = $c -replace 'MinLODSize=\d+', 'MinLODSize=1'
    echo $c = $c -replace 'LODBias=-?\d+', 'LODBias=15'
    echo $c = $c -replace 'MinMagFilter=\w+', 'MinMagFilter=Point'
    echo $c = $c -replace 'MipFilter=\w+', 'MipFilter=Point'
    echo $c = $c -replace '^\s*MaxAnisotropy=.*', 'MaxAnisotropy=0'
    echo $c = $c -replace '^\s*SkeletalMeshLODBias=.*', 'SkeletalMeshLODBias=15'
    echo $c = $c -replace '^\s*ParticleLODBias=.*', 'ParticleLODBias=15'
    
    :: Cena, fisica e efeitos avancados
    echo $c = $c -replace '^\s*SceneCaptureStreamingMultiplier=.*', 'SceneCaptureStreamingMultiplier=0.000000'
    echo $c = $c -replace '^\s*TessellationAdaptivePixelsPerTriangle=.*', 'TessellationAdaptivePixelsPerTriangle=0.000000'
    echo $c = $c -replace '^\s*bEnableParallelAPEXClothingFetch=.*', 'bEnableParallelAPEXClothingFetch=True'
    echo $c = $c -replace '^\s*ApexClothingAllowAsyncCooking=.*', 'ApexClothingAllowAsyncCooking=True'
    echo $c = $c -replace '^\s*bDisableSkeletalInstanceWeights=.*', 'bDisableSkeletalInstanceWeights=False'
    echo $c = $c -replace '^\s*AllowRadialBlur=.*', 'AllowRadialBlur=False'
    echo $c = $c -replace '^\s*AllowSubsurfaceScattering=.*', 'AllowSubsurfaceScattering=False'
    echo $c = $c -replace '^\s*AllowImageReflections=.*', 'AllowImageReflections=False'
    echo $c = $c -replace '^\s*AllowImageReflectionShadowing=.*', 'AllowImageReflectionShadowing=False'
    echo $c = $c -replace '^\s*bAllowSeparateTranslucency=.*', 'bAllowSeparateTranslucency=False'
    echo $c = $c -replace '^\s*bAllowPostprocessMLAA=.*', 'bAllowPostprocessMLAA=False'
    echo $c = $c -replace '^\s*AllowApexCloth=.*', 'AllowApexCloth=False'
    echo $c = $c -replace '^\s*ApexGRBEnable=.*', 'ApexGRBEnable=False'
    echo $c = $c -replace '^\s*ApexDestructionMaxChunkIslandCount=.*', 'ApexDestructionMaxChunkIslandCount=0'
    echo $c = $c -replace '^\s*bAllowFracturedDamage=.*', 'bAllowFracturedDamage=False'
    echo $c = $c -replace '^\s*NumFracturedPartsScale=.*', 'NumFracturedPartsScale=0.000000'
    echo $c = $c -replace '^\s*FractureDirectSpawnChanceScale=.*', 'FractureDirectSpawnChanceScale=0.000000'
    echo $c = $c -replace '^\s*FractureRadialSpawnChanceScale=.*', 'FractureRadialSpawnChanceScale=0.000000'
    echo $c = $c -replace '^\s*FloatingPointRenderTargets=.*', 'FloatingPointRenderTargets=False'
    echo $c = $c -replace '^\s*bUseTranslucentArenaShaders=.*', 'bUseTranslucentArenaShaders=False'
    echo $c = $c -replace '^\s*SpeedTreeLeaves=.*', 'SpeedTreeLeaves=False'
    echo $c = $c -replace '^\s*SpeedTreeFronds=.*', 'SpeedTreeFronds=False'
    echo $c = $c -replace '^\s*MaxMultiSamples=.*', 'MaxMultiSamples=0'
    echo $c = $c -replace '^\s*DetailMode=.*', 'DetailMode=0'
    echo $c = $c -replace '^\s*bAllowHighQualityMaterials=.*', 'bAllowHighQualityMaterials=False'
    
    :: Decals (DynamicDecals=True mantem o circulo da bola)
    echo $c = $c -replace '^\s*StaticDecals=.*', 'StaticDecals=False'
    echo $c = $c -replace '^\s*DynamicDecals=.*', 'DynamicDecals=True'
    echo $c = $c -replace '^\s*UnbatchedDecals=.*', 'UnbatchedDecals=False'
    
    :: Pos-processamento
    echo $c = $c -replace '^\s*DepthOfField=.*', 'DepthOfField=False'
    echo $c = $c -replace '^\s*AmbientOcclusion=.*', 'AmbientOcclusion=False'
    echo $c = $c -replace '^\s*Bloom=.*', 'Bloom=False'
    echo $c = $c -replace '^\s*MotionBlur=.*', 'MotionBlur=False'
    echo $c = $c -replace '^\s*MotionBlurPause=.*', 'MotionBlurPause=False'
    echo $c = $c -replace '^\s*LensFlares=.*', 'LensFlares=False'
    echo $c = $c -replace '^\s*bAllowLightShafts=.*', 'bAllowLightShafts=False'
    echo $c = $c -replace '^\s*FogVolumes=.*', 'FogVolumes=False'
    echo $c = $c -replace '^\s*Distortion=.*', 'Distortion=False'
    echo $c = $c -replace '^\s*DropParticleDistortion=.*', 'DropParticleDistortion=False'
    
    :: Sombras
    echo $c = $c -replace '^\s*DynamicShadows=.*', 'DynamicShadows=False'
    echo $c = $c -replace '^\s*LightEnvironmentShadows=.*', 'LightEnvironmentShadows=False'
    echo $c = $c -replace '^\s*CompositeDynamicLights=.*', 'CompositeDynamicLights=False'
    echo $c = $c -replace '^\s*SHSecondaryLighting=.*', 'SHSecondaryLighting=False'
    echo $c = $c -replace '^\s*DirectionalLightmaps=.*', 'DirectionalLightmaps=False'
    echo $c = $c -replace '^\s*bEnableForegroundShadowsOnWorld=.*', 'bEnableForegroundShadowsOnWorld=False'
    echo $c = $c -replace '^\s*bAllowWholeSceneDominantShadows=.*', 'bAllowWholeSceneDominantShadows=False'
    echo $c = $c -replace '^\s*ShadowTexelsPerPixel=.*', 'ShadowTexelsPerPixel=0.000000'
    echo $c = $c -replace '^\s*MaxWholeSceneDominantShadowResolution=.*', 'MaxWholeSceneDominantShadowResolution=16'
    echo $c = $c -replace '^\s*MaxShadowResolution=.*', 'MaxShadowResolution=16'
    echo $c = $c -replace '^\s*MinShadowResolution=.*', 'MinShadowResolution=16'
    echo $c = $c -replace '^\s*CSMSplitPenumbraScale=.*', 'CSMSplitPenumbraScale=0.000000'
    
    :: Render Scale
    echo $c = $c -replace '^\s*ScreenPercentage=.*', 'ScreenPercentage=100.000000'
    echo $c = $c -replace '^\s*UpscaleScreenPercentage=.*', 'UpscaleScreenPercentage=True'
    echo $c = $c -replace '^\s*MinimumScreenScale=.*', 'MinimumScreenScale=1.000000'
    
    echo Set-Content -Path $f -Value $c -Force
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%PS_SCRIPT%"
del "%PS_SCRIPT%" >nul 2>&1

:: ============================================================================
:: FASE 5: SELO DE BLINDAGEM
:: ============================================================================
echo [+] FASE 5: APLICANDO BLINDAGEM DE SISTEMA NO ARQUIVO...
attrib +r "!RL_CONFIG_PATH!" >nul 2>&1

echo.
color 0A
echo [+] LOBOTOMIA DE KERNEL E ENGINE CONCLUIDA.
echo.
echo ==============================================================================
echo [PROJECT TESSERACT - LIMITES QUEBRADOS]
echo.
echo O bug de diretorios com acento e OneDrive foi obliterado.
echo O arquivo foi desbloqueado, envenenado e trancado novamente.
echo O jogo agora corre nas veias diretas da placa mae.
echo ==============================================================================
pause >nul
exit /b 0
