@echo off
chcp 65001 >nul
setlocal EnableExtensions EnableDelayedExpansion
color 0C
title GUTTYTECH - RL ENGINE NUKER v21.7 (PROJECT TESSERACT FINAL)

:: ============================================================================
:: ELEVACAO DE PRIVILEGIO
:: ============================================================================
net session >nul 2>&1
if errorlevel 1 (
    echo [+] ELEVANDO PRIVILEGIO...
    powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

echo +=======================================================+
echo ^| GUTTYTECH RL NUKER v21.7 - PROJECT TESSERACT FINAL   ^|
echo +=======================================================+
echo.

:: ============================================================================
:: FASE 1/6: LOCALIZAR INI
:: ============================================================================
echo [+] FASE 1/6: RASTREANDO TASystemSettings.ini...

set "TARGET_REL=My Games\Rocket League\TAGame\Config\TASystemSettings.ini"
set "RL_CONFIG_PATH="

call :TryConfig "%USERPROFILE%\Documents\%TARGET_REL%" "Documents"
if not defined RL_CONFIG_PATH call :TryConfig "%USERPROFILE%\OneDrive\Documents\%TARGET_REL%" "OneDrive"
if not defined RL_CONFIG_PATH call :TryConfig "%USERPROFILE%\OneDrive - Personal\Documents\%TARGET_REL%" "OneDrive Personal"
if not defined RL_CONFIG_PATH call :TryConfig "%USERPROFILE%\OneDrive - Empresa\Documents\%TARGET_REL%" "OneDrive Empresa"
if not defined RL_CONFIG_PATH call :TryConfig "%USERPROFILE%\OneDrive - Company\Documents\%TARGET_REL%" "OneDrive Company"

if not defined RL_CONFIG_PATH (
    for /d %%U in ("C:\Users\*") do (
        if not defined RL_CONFIG_PATH call :TryConfig "%%U\Documents\%TARGET_REL%" "%%~nxU\Documents"
        if not defined RL_CONFIG_PATH call :TryConfig "%%U\OneDrive\Documents\%TARGET_REL%" "%%~nxU\OneDrive"
        if not defined RL_CONFIG_PATH call :TryConfig "%%U\OneDrive - Personal\Documents\%TARGET_REL%" "%%~nxU\OneDrive Personal"
        if not defined RL_CONFIG_PATH call :TryConfig "%%U\OneDrive - Empresa\Documents\%TARGET_REL%" "%%~nxU\OneDrive Empresa"
        if not defined RL_CONFIG_PATH call :TryConfig "%%U\OneDrive - Company\Documents\%TARGET_REL%" "%%~nxU\OneDrive Company"
    )
)

if not defined RL_CONFIG_PATH (
    for /f "delims=" %%F in ('dir /s /b "%USERPROFILE%\TASystemSettings.ini" 2^>nul') do (
        set "RL_CONFIG_PATH=%%F" & goto :FoundIni
    )
)

if not defined RL_CONFIG_PATH (
    for /f "delims=" %%F in ('dir /s /b "C:\TASystemSettings.ini" 2^>nul') do (
        set "RL_CONFIG_PATH=%%F" & goto :FoundIni
    )
)

:FoundIni
if not defined RL_CONFIG_PATH (
    color 0E
    echo [-] ERRO: TASystemSettings.ini nao encontrado.
    echo [!] Abra o Rocket League 1x antes.
    pause
    exit /b 1
)

echo [+] ALVO: !RL_CONFIG_PATH!
echo.

:: ============================================================================
:: FASE 2/6: DESTRANCAR ARQUIVO (se foi trancado por execucao anterior)
:: ============================================================================
echo [+] FASE 2/6: DESTRANCANDO ARQUIVO (execucao anterior detectada)...

:: --- TAKEOWN: assume posse do arquivo (resolve owner estranho) ---
takeown /f "!RL_CONFIG_PATH!" >nul 2>&1
if errorlevel 1 echo     [!] takeown falhou (arquivo pode estar em uso)

:: --- ICACLS: da permissao total para o usuario atual ---
icacls "!RL_CONFIG_PATH!" /grant "%USERNAME%:(F)" /c /q >nul 2>&1
icacls "!RL_CONFIG_PATH!" /grant *S-1-1-0:(F) /c /q >nul 2>&1

:: --- ATTRIB: remove read-only, hidden, system ---
attrib -r -h -s "!RL_CONFIG_PATH!" >nul 2>&1

:: --- TENTATIVA EXTRA: se o arquivo estiver em subpasta protegida ---
for %%I in ("!RL_CONFIG_PATH!") do (
    icacls "%%~dpI." /grant "%USERNAME%:(OI)(CI)F" /c /q >nul 2>&1
    attrib -r -h -s "%%~dpI." >nul 2>&1
)

echo     [+] Arquivo destrancado.
echo.

:: ============================================================================
:: FASE 3/6: BACKUP + ROLLBACK
:: ============================================================================
echo [+] FASE 3/6: CRIANDO BACKUP E SCRIPT DE ROLLBACK...

for %%F in ("!RL_CONFIG_PATH!") do set "INI_FILENAME=%%~nxF"
set "RL_BACKUP=%TEMP%\!INI_FILENAME!.gutty.bak"
set "RL_ROLLBACK=%TEMP%\GUTTY_RL_NUKER_ROLLBACK.bat"

:: --- TENTA COPIAR COM 3 METODOS ---
set "BK_OK=0"

copy /y "!RL_CONFIG_PATH!" "!RL_BACKUP!" >nul 2>&1
if not errorlevel 1 set "BK_OK=1"

if "!BK_OK!"=="0" (
    xcopy "!RL_CONFIG_PATH!" "!RL_BACKUP!" /Y /R /H >nul 2>&1
    if not errorlevel 1 set "BK_OK=1"
)

if "!BK_OK!"=="0" (
    powershell -NoProfile -Command "Copy-Item -Path '%RL_CONFIG_PATH%' -Destination '%RL_BACKUP%' -Force" >nul 2>&1
    if not errorlevel 1 set "BK_OK=1"
)

if "!BK_OK!"=="0" (
    color 0E
    echo [-] ERRO: Nao foi possivel criar backup.
    echo     O arquivo pode estar em uso por outro programa.
    echo [!] FECHE O ROCKET LEAGUE COMPLETAMENTE antes de rodar o script.
    echo [!] Abortado.
    pause
    exit /b 1
)

echo     [+] Backup: !RL_BACKUP!

call :WriteRollbackScript
echo     [+] Rollback: !RL_ROLLBACK!
echo.

:: ============================================================================
:: FASE 4/6: KERNEL E REDE
:: ============================================================================
echo [+] FASE 4/6: KERNEL E REDE...

set "SYS_WARN=0"

bcdedit /deletevalue useplatformclock >nul 2>&1
if errorlevel 1 set "SYS_WARN=1"
bcdedit /set disabledynamictick yes >nul 2>&1
if errorlevel 1 set "SYS_WARN=1"
bcdedit /set tscsyncpolicy Enhanced >nul 2>&1
if errorlevel 1 set "SYS_WARN=1"

reg add "HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl" /v "Win32PrioritySeparation" /t REG_DWORD /d 40 /f >nul 2>&1
if errorlevel 1 set "SYS_WARN=1"

set "TCP_COUNT=0"
for /f "usebackq tokens=1*" %%a in (`reg query "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces" 2^>nul ^| findstr /r /c:"\\Interfaces\\"`) do (
    set "IFACE_KEY=%%a"
    set "IFACE_KEY=!IFACE_KEY:HKEY_LOCAL_MACHINE\=HKLM\!"
    reg add "!IFACE_KEY!" /v "TcpAckFrequency" /t REG_DWORD /d 1 /f >nul 2>&1
    reg add "!IFACE_KEY!" /v "TCPNoDelay" /t REG_DWORD /d 1 /f >nul 2>&1
    reg add "!IFACE_KEY!" /v "TcpDelAckTicks" /t REG_DWORD /d 0 /f >nul 2>&1
    set /a TCP_COUNT+=1
)
if !TCP_COUNT! equ 0 set "SYS_WARN=1"

if "!SYS_WARN!"=="1" echo     [!] Alguns ajustes de sistema falharam
echo [+] FASE 4 CONCLUIDA.
echo.

:: ============================================================================
:: FASE 5/6: INJECAO (POWERSHELL — EXATAMENTE O SEU)
:: ============================================================================
echo [+] FASE 5/6: APLICANDO TWEAKS...

set "PS_SCRIPT=%TEMP%\RL_Tesseract_V21.ps1"
if exist "%PS_SCRIPT%" del "%PS_SCRIPT%"

> "%PS_SCRIPT%" (
    echo $ErrorActionPreference = 'SilentlyContinue'
    echo $f = $env:RL_TARGET
    echo $c = Get-Content $f
    
    echo $c = $c -replace '^\s*UncappedFramerate=.*', 'UncappedFramerate=True'
    echo $c = $c -replace '^\s*WaitForGPU=.*', 'WaitForGPU=True'
    echo $c = $c -replace '^\s*OneFrameThreadLag=.*', 'OneFrameThreadLag=True'
    echo $c = $c -replace '^\s*AllowPerFrameSleep=.*', 'AllowPerFrameSleep=True'
    echo $c = $c -replace '^\s*AllowPerFrameYield=.*', 'AllowPerFrameYield=True'
    echo $c = $c -replace '^\s*UseVsync=.*', 'UseVsync=False'
    echo $c = $c -replace '^\s*bSmoothFrameRate=.*', 'bSmoothFrameRate=False'
    
    echo $c = $c -replace 'MaxLODSize=\d+', 'MaxLODSize=16'
    echo $c = $c -replace 'MinLODSize=\d+', 'MinLODSize=1'
    echo $c = $c -replace 'LODBias=-?\d+', 'LODBias=15'
    echo $c = $c -replace 'MinMagFilter=\w+', 'MinMagFilter=Point'
    echo $c = $c -replace 'MipFilter=\w+', 'MipFilter=Point'
    echo $c = $c -replace '^\s*MaxAnisotropy=.*', 'MaxAnisotropy=0'
    echo $c = $c -replace '^\s*SkeletalMeshLODBias=.*', 'SkeletalMeshLODBias=15'
    echo $c = $c -replace '^\s*ParticleLODBias=.*', 'ParticleLODBias=15'
    
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
    echo $c = $c -replace '^\s*StaticDecals=.*', 'StaticDecals=False'
    echo $c = $c -replace '^\s*DynamicDecals=.*', 'DynamicDecals=True'
    echo $c = $c -replace '^\s*UnbatchedDecals=.*', 'UnbatchedDecals=False'
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
    
    echo $c = $c -replace '^\s*ScreenPercentage=.*', 'ScreenPercentage=100.000000'
    echo $c = $c -replace '^\s*UpscaleScreenPercentage=.*', 'UpscaleScreenPercentage=True'
    echo $c = $c -replace '^\s*MinimumScreenScale=.*', 'MinimumScreenScale=1.000000'
    echo Set-Content -Path $f -Value $c -Force
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%PS_SCRIPT%"
set "PS_ERR=!errorlevel!"
del "%PS_SCRIPT%" >nul 2>&1

if not "!PS_ERR!"=="0" (
    color 0E
    echo [-] ERRO: PowerShell falhou. Restaurando backup...
    copy /y "!RL_BACKUP!" "!RL_CONFIG_PATH!" >nul 2>&1
    if errorlevel 1 (
        echo [-] Restauracao falhou. Faca manualmente:
        echo     copy /y "!RL_BACKUP!" "!RL_CONFIG_PATH!"
    ) else (
        echo [+] INI restaurado.
    )
    pause
    exit /b 1
)

echo [+] FASE 5 CONCLUIDA.
echo.

:: ============================================================================
:: FASE 6/6: BLINDAGEM
:: ============================================================================
echo [+] FASE 6/6: TRANCANDO ARQUIVO...

attrib +r "!RL_CONFIG_PATH!" >nul 2>&1
if errorlevel 1 (
    echo     [!] AVISO: Nao foi possivel marcar somente-leitura.
) else (
    echo     [+] INI trancado.
)
echo [+] FASE 6 CONCLUIDA.
echo.

:: ============================================================================
:: FIM
:: ============================================================================
color 0A
echo [+] TESSERACT v21.7 CONCLUIDO.
echo.
echo ==============================================================================
echo  Backup INI : !RL_BACKUP!
echo  Rollback   : !RL_ROLLBACK!
echo ==============================================================================
echo.
pause
exit /b 0

:: ============================================================================
:: SUBROTINAS
:: ============================================================================

:TryConfig
if exist "%~1" (
    set "RL_CONFIG_PATH=%~1"
    echo     [+] Encontrado em %~2
)
exit /b 0

:WriteRollbackScript
set "RB_BK=!RL_BACKUP!"
set "RB_CFG=!RL_CONFIG_PATH!"
set "RB_OUT=!RL_ROLLBACK!"
setlocal DisableDelayedExpansion
(
    echo @echo off
    echo chcp 65001 ^>nul
    echo setlocal EnableExtensions EnableDelayedExpansion
    echo title GUTTYTECH RL NUKER - ROLLBACK
    echo.
    echo net session ^>nul 2^>^&1
    echo if errorlevel 1 ^(
    echo     echo Execute como Administrador.
    echo     pause
    echo     exit /b 1
    echo ^)
    echo.
    echo echo [+] Destrancando arquivo...
    echo takeown /f "%RB_CFG%" ^>nul 2^>^&1
    echo attrib -r -h -s "%RB_CFG%" ^>nul 2^>^&1
    echo icacls "%RB_CFG%" /grant "%USERNAME%:(F)" /c /q ^>nul 2^>^&1
    echo.
    echo echo [+] Restaurando INI...
    echo if exist "%RB_BK%" ^(
    echo     copy /y "%RB_BK%" "%RB_CFG%" ^>nul
    echo     if errorlevel 1 ^( echo [-] Falha ao restaurar INI ^& pause ^& exit /b 1 ^)
    echo     echo [+] INI restaurado.
    echo ^) else ^(
    echo     echo [-] Backup nao encontrado: %RB_BK%
    echo ^)
    echo.
    echo echo [+] Revertendo bcdedit...
    echo bcdedit /deletevalue disabledynamictick ^>nul 2^>^&1
    echo bcdedit /deletevalue tscsyncpolicy ^>nul 2^>^&1
    echo bcdedit /set useplatformclock true ^>nul 2^>^&1
    echo.
    echo echo [+] Removendo tweaks TCP...
    echo for /f "usebackq tokens=1*" %%%%G in ^(`reg query "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces" 2^>nul ^^| findstr /r /c:"\\Interfaces\\"`^) do ^(
    echo     set "IFACE=%%%%G"
    echo     set "IFACE=^!IFACE:HKEY_LOCAL_MACHINE\=HKLM\^!"
    echo     reg delete "^!IFACE^!" /v TcpAckFrequency /f ^>nul 2^>^&1
    echo     reg delete "^!IFACE^!" /v TCPNoDelay /f ^>nul 2^>^&1
    echo     reg delete "^!IFACE^!" /v TcpDelAckTicks /f ^>nul 2^>^&1
    echo ^)
    echo.
    echo echo [+] Rollback concluido. Reinicie o PC.
    echo pause
) > "%RB_OUT%"
endlocal
exit /b 0
