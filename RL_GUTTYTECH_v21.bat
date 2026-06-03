@echo off
chcp 65001 >nul
setlocal EnableExtensions EnableDelayedExpansion
color 0C
title GUTTYTECH - RL ENGINE NUKER v21.3 (PROJECT TESSERACT FINAL)

:: ============================================================================
:: ELEVACAO DE PRIVILEGIO (ADMINISTRADOR)
:: ============================================================================
net session >nul 2>&1
if errorlevel 1 (
    echo [+] GUTTYTECH: ELEVANDO PRIVILEGIO...
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

echo +=======================================================+
echo ^| GUTTYTECH RL NUKER v21.3 - PROJECT TESSERACT FINAL   ^|
echo ^| Otimizacao Rocket League + timers/rede (reversivel)  ^|
echo +=======================================================+
echo.

:: ============================================================================
:: FASE 1/6: LOCALIZAR TASystemSettings.ini
:: ============================================================================
echo [+] FASE 1/6: RASTREANDO TASystemSettings.ini...

set "TARGET_REL=My Games\Rocket League\TAGame\Config\TASystemSettings.ini"
set "TARGET_REL_PT=Meus Jogos\Rocket League\TAGame\Config\TASystemSettings.ini"
set "RL_CONFIG_PATH="
set "RL_CONFIG_DIR="

:: --- CAMINHOS PADRAO (99% dos casos) ---
call :TryConfig "%USERPROFILE%\Documents\%TARGET_REL%" "Documents"
if not defined RL_CONFIG_PATH call :TryConfig "%USERPROFILE%\OneDrive\Documents\%TARGET_REL%" "OneDrive"
if not defined RL_CONFIG_PATH call :TryConfig "%USERPROFILE%\OneDrive - Personal\Documents\%TARGET_REL%" "OneDrive Personal"
if not defined RL_CONFIG_PATH call :TryConfig "%USERPROFILE%\OneDrive - Empresa\Documents\%TARGET_REL%" "OneDrive Empresa"
if not defined RL_CONFIG_PATH call :TryConfig "%USERPROFILE%\OneDrive - Company\Documents\%TARGET_REL%" "OneDrive Company"

:: --- CAMINHOS LEGADO (Windows PT-BR antigo) ---
if not defined RL_CONFIG_PATH call :TryConfig "%USERPROFILE%\Documents\%TARGET_REL_PT%" "Documents PT-BR"
if not defined RL_CONFIG_PATH call :TryConfig "%USERPROFILE%\OneDrive\Documents\%TARGET_REL_PT%" "OneDrive PT-BR"

:: --- FALLBACK: todos os perfis do PC ---
if not defined RL_CONFIG_PATH (
    echo     [*] Buscando em todos os perfis de usuario...
    for /d %%U in ("C:\Users\*") do (
        if not defined RL_CONFIG_PATH call :TryConfig "%%U\Documents\%TARGET_REL%" "%%~nxU\Documents"
        if not defined RL_CONFIG_PATH call :TryConfig "%%U\OneDrive\Documents\%TARGET_REL%" "%%~nxU\OneDrive"
        if not defined RL_CONFIG_PATH call :TryConfig "%%U\OneDrive - Personal\Documents\%TARGET_REL%" "%%~nxU\OneDrive Personal"
        if not defined RL_CONFIG_PATH call :TryConfig "%%U\OneDrive - Empresa\Documents\%TARGET_REL%" "%%~nxU\OneDrive Empresa"
        if not defined RL_CONFIG_PATH call :TryConfig "%%U\OneDrive - Company\Documents\%TARGET_REL%" "%%~nxU\OneDrive Company"
        if not defined RL_CONFIG_PATH call :TryConfig "%%U\Documents\%TARGET_REL_PT%" "%%~nxU\Documents PT-BR"
    )
)

:: --- BUSCA DE EMERGENCIA: procura recursiva no perfil do usuario ---
if not defined RL_CONFIG_PATH (
    echo     [*] BUSCA DE EMERGENCIA: procurando no perfil do usuario...
    echo     (Isso pode demorar 10-30 segundos...)
    for /f "delims=" %%F in ('dir /s /b "%USERPROFILE%\TASystemSettings.ini" 2^>nul') do (
        set "RL_CONFIG_PATH=%%F"
        echo     [+] ENCONTRADO via busca: %%F
        goto :FoundIni
    )
)

:: --- ULTIMO RECURSO: procura em todo C:\ (muito lento, so se necessario) ---
if not defined RL_CONFIG_PATH (
    echo     [*] ULTIMO RECURSO: procurando em C:\ (aguarde)...
    for /f "delims=" %%F in ('dir /s /b "C:\TASystemSettings.ini" 2^>nul') do (
        set "RL_CONFIG_PATH=%%F"
        echo     [+] ENCONTRADO em C:\: %%F
        goto :FoundIni
    )
)

:FoundIni
if not defined RL_CONFIG_PATH (
    color 0E
    echo.
    echo [-] ERRO: TASystemSettings.ini nao encontrado em lugar nenhum.
    echo.
    echo [!] Abra o Rocket League pelo menos uma vez para gerar o arquivo.
    echo [!] Nenhuma alteracao de sistema ou INI foi feita nesta execucao.
    echo.
    echo [?] Caminho esperado:
    echo     %USERPROFILE%\Documents\My Games\Rocket League\TAGame\Config
    echo.
    echo [?] Se voce TEM CERTEZA que abriu o jogo, execute este diagnostico:
    echo     1. Aperte Win + R
    echo     2. Cole: cmd /c dir /s /b "%USERPROFILE%\TASystemSettings.ini"
    echo     3. Se aparecer um caminho, copie e me envie.
    echo.
    pause
    exit /b 1
)

for %%I in ("!RL_CONFIG_PATH!") do set "RL_CONFIG_DIR=%%~dpI"
echo [+] ALVO: !RL_CONFIG_PATH!
echo.

:: ============================================================================
:: FASE 2/6: BACKUP + ROLLBACK
:: ============================================================================
echo [+] FASE 2/6: CRIANDO BACKUP E SCRIPT DE ROLLBACK...

set "RL_BACKUP=!RL_CONFIG_PATH!.gutty.bak"
set "RL_ROLLBACK=%TEMP%\GUTTY_RL_NUKER_ROLLBACK.bat"

copy /y "!RL_CONFIG_PATH!" "!RL_BACKUP!" >nul 2>&1
if errorlevel 1 (
    color 0E
    echo [-] ERRO: Nao foi possivel criar backup em:
    echo     !RL_BACKUP!
    echo [!] Abortado. Nenhuma outra alteracao sera aplicada.
    pause
    exit /b 1
)
echo     [+] Backup: !RL_BACKUP!

call :WriteRollbackScript
echo     [+] Rollback: !RL_ROLLBACK!
echo.

:: ============================================================================
:: FASE 3/6: TIMERS / PRIORIDADE / REDE
:: ============================================================================
echo [+] FASE 3/6: KERNEL E REDE (bcdedit + registro)...

set "SYS_WARN=0"

bcdedit /deletevalue useplatformclock >nul 2>&1
if errorlevel 1 (
    echo     [!] AVISO: bcdedit useplatformclock - falhou ou valor inexistente
    set "SYS_WARN=1"
)
bcdedit /set disabledynamictick yes >nul 2>&1
if errorlevel 1 (
    echo     [!] AVISO: bcdedit disabledynamictick - falhou
    set "SYS_WARN=1"
)
bcdedit /set tscsyncpolicy Enhanced >nul 2>&1
if errorlevel 1 (
    echo     [!] AVISO: bcdedit tscsyncpolicy - falhou
    set "SYS_WARN=1"
)

reg add "HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl" /v "Win32PrioritySeparation" /t REG_DWORD /d 40 /f >nul 2>&1
if errorlevel 1 (
    echo     [!] AVISO: Win32PrioritySeparation - falhou
    set "SYS_WARN=1"
)

echo     [+] Aplicando TCP por interface (GUID)...
set "TCP_COUNT=0"
for /f "usebackq tokens=1*" %%a in (`reg query "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces" 2^>nul ^| findstr /r /c:"\\Interfaces\\"`) do (
    set "IFACE_KEY=%%a"
    set "IFACE_KEY=!IFACE_KEY:HKEY_LOCAL_MACHINE\=HKLM\!"
    reg add "!IFACE_KEY!" /v "TcpAckFrequency" /t REG_DWORD /d 1 /f >nul 2>&1
    reg add "!IFACE_KEY!" /v "TCPNoDelay" /t REG_DWORD /d 1 /f >nul 2>&1
    reg add "!IFACE_KEY!" /v "TcpDelAckTicks" /t REG_DWORD /d 0 /f >nul 2>&1
    set /a TCP_COUNT+=1
)
if !TCP_COUNT! equ 0 (
    echo     [!] AVISO: Nenhuma interface TCP encontrada
    set "SYS_WARN=1"
) else (
    echo     [+] Interfaces TCP ajustadas: !TCP_COUNT!
)

if "!SYS_WARN!"=="1" echo     [!] Alguns ajustes de sistema falharam - rollback disponivel
echo [+] FASE 3 CONCLUIDA.
echo.

:: ============================================================================
:: FASE 4/6: DESBLOQUEIO DO INI
:: ============================================================================
echo [+] FASE 4/6: DESBLOQUEANDO ARQUIVO...

attrib -r -h -s "!RL_CONFIG_PATH!" >nul 2>&1
icacls "!RL_CONFIG_PATH!" /grant "%USERNAME%:(F)" /c /q >nul 2>&1
if errorlevel 1 echo     [!] AVISO: icacls falhou - tentando continuar

set "RL_TARGET=!RL_CONFIG_PATH!"
echo [+] FASE 4 CONCLUIDA.
echo.

:: ============================================================================
:: FASE 5/6: INJECAO NO INI (POWERSHELL)
:: ============================================================================
echo [+] FASE 5/6: APLICANDO TWEAKS NO INI...

set "PS_SCRIPT=%TEMP%\GUTTY_RL_Tesseract_v21.3.ps1"
if exist "%PS_SCRIPT%" del /f /q "%PS_SCRIPT%" >nul 2>&1

call :WritePowerShellScript

echo     [+] Executando PowerShell...
powershell -NoProfile -ExecutionPolicy Bypass -File "%PS_SCRIPT%"
set "PS_ERR=!errorlevel!"
del /f /q "%PS_SCRIPT%" >nul 2>&1

if not "!PS_ERR!"=="0" (
    color 0E
    echo.
    echo [-] ERRO: Falha ao modificar o INI.
    echo [+] Restaurando backup automaticamente...
    copy /y "!RL_BACKUP!" "!RL_CONFIG_PATH!" >nul 2>&1
    if errorlevel 1 (
        echo [-] Restauracao automatica falhou. Restaure manualmente:
        echo     copy /y "!RL_BACKUP!" "!RL_CONFIG_PATH!"
    ) else (
        echo [+] INI restaurado do backup.
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
    echo     [+] INI trancado - Steam/Epic nao sobrescrevem facilmente.
)
echo [+] FASE 6 CONCLUIDA.
echo.

:: ============================================================================
:: FIM
:: ============================================================================
color 0A
echo [+] TESSERACT v21.3 CONCLUIDO COM SUCESSO.
echo.
echo ==============================================================================
echo  Backup INI : !RL_BACKUP!
echo  Rollback   : !RL_ROLLBACK!
echo.
echo  Reinicie o PC para aplicar bcdedit/timers/rede.
echo  INI em somente-leitura - use o rollback para desbloquear/restaurar.
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
    echo echo [+] Restaurando INI...
    echo if exist "%RB_BK%" ^(
    echo     attrib -r -h -s "%RB_CFG%" ^>nul 2^>^&1
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
    echo echo [+] Removendo tweaks TCP das interfaces...
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

:WritePowerShellScript
set "RL_TARGET_SAFE=%RL_TARGET%"
setlocal DisableDelayedExpansion
(
    echo # GUTTYTECH RL Tesseract v21.3 - gerado automaticamente
    echo $ErrorActionPreference = 'Stop'
    echo.
    echo function Set-IniLine {
    echo     param^(
    echo         [System.Collections.Generic.List[string]]$Lines,
    echo         [string]$Key,
    echo         [string]$Value
    echo     ^)
    echo     $pattern = '^\s*' + ^[regex^]::Escape^($Key^) + '\s*='
    echo     $found = $false
    echo     for ^($i = 0; $i -lt $Lines.Count; $i++^) {
    echo         if ^($Lines[$i] -match $pattern^) {
    echo             $Lines[$i] = "$Key=$Value"
    echo             $found = $true
    echo         }
    echo     }
    echo     if ^(-not $found^) { [void]$Lines.Add^("$Key=$Value"^) }
    echo }
    echo.
    echo $path = $env:RL_TARGET
    echo if ^([string^]::IsNullOrWhiteSpace^($path^) -or -not ^(Test-Path -LiteralPath $path^)^) {
    echo     Write-Host '[-] ERRO: Caminho do INI invalido.' -ForegroundColor Red
    echo     exit 1
    echo }
    echo.
    echo try {
    echo     $bytes = [System.IO.File]::ReadAllBytes^($path^)
    echo     if ^($bytes.Length -eq 0^) { throw 'Arquivo vazio' }
    echo.
    echo     $enc = [System.Text.Encoding]::Default
    echo     if ^($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF^) {
    echo         $enc = [System.Text.UTF8Encoding]::new^($true^)
    echo     } elseif ^($bytes.Length -ge 2 -and $bytes[0] -eq 0xFF -and $bytes[1] -eq 0xFE^) {
    echo         $enc = [System.Text.UnicodeEncoding]::new^($false, $false^)
    echo     }
    echo.
    echo     $lineList = [System.Collections.Generic.List[string]]::new^(^)
    echo     foreach ^($line in [System.IO.File]::ReadAllLines^($path, $enc^)^) {
    echo         [void]$lineList.Add^($line^)
    echo     }
    echo.
    echo     $tweaks = [ordered]@{
    echo         'UncappedFramerate' = 'True'
    echo         'WaitForGPU' = 'True'
    echo         'OneFrameThreadLag' = 'True'
    echo         'AllowPerFrameSleep' = 'True'
    echo         'AllowPerFrameYield' = 'True'
    echo         'UseVsync' = 'False'
    echo         'bSmoothFrameRate' = 'False'
    echo         'MaxLODSize' = '16'
    echo         'MinLODSize' = '1'
    echo         'LODBias' = '15'
    echo         'MinMagFilter' = 'Point'
    echo         'MipFilter' = 'Point'
    echo         'MaxAnisotropy' = '0'
    echo         'SkeletalMeshLODBias' = '15'
    echo         'ParticleLODBias' = '15'
    echo         'SceneCaptureStreamingMultiplier' = '0.000000'
    echo         'TessellationAdaptivePixelsPerTriangle' = '0.000000'
    echo         'bEnableParallelAPEXClothingFetch' = 'True'
    echo         'ApexClothingAllowAsyncCooking' = 'True'
    echo         'bDisableSkeletalInstanceWeights' = 'False'
    echo         'AllowRadialBlur' = 'False'
    echo         'AllowSubsurfaceScattering' = 'False'
    echo         'AllowImageReflections' = 'False'
    echo         'AllowImageReflectionShadowing' = 'False'
    echo         'bAllowSeparateTranslucency' = 'False'
    echo         'bAllowPostprocessMLAA' = 'False'
    echo         'AllowApexCloth' = 'False'
    echo         'ApexGRBEnable' = 'False'
    echo         'ApexDestructionMaxChunkIslandCount' = '0'
    echo         'bAllowFracturedDamage' = 'False'
    echo         'NumFracturedPartsScale' = '0.000000'
    echo         'FractureDirectSpawnChanceScale' = '0.000000'
    echo         'FractureRadialSpawnChanceScale' = '0.000000'
    echo         'FloatingPointRenderTargets' = 'False'
    echo         'bUseTranslucentArenaShaders' = 'False'
    echo         'SpeedTreeLeaves' = 'False'
    echo         'SpeedTreeFronds' = 'False'
    echo         'MaxMultiSamples' = '0'
    echo         'DetailMode' = '0'
    echo         'bAllowHighQualityMaterials' = 'False'
    echo         'StaticDecals' = 'False'
    echo         'DynamicDecals' = 'True'
    echo         'UnbatchedDecals' = 'False'
    echo         'DepthOfField' = 'False'
    echo         'AmbientOcclusion' = 'False'
    echo         'Bloom' = 'False'
    echo         'MotionBlur' = 'False'
    echo         'MotionBlurPause' = 'False'
    echo         'LensFlares' = 'False'
    echo         'bAllowLightShafts' = 'False'
    echo         'FogVolumes' = 'False'
    echo         'Distortion' = 'False'
    echo         'DropParticleDistortion' = 'False'
    echo         'DynamicShadows' = 'False'
    echo         'LightEnvironmentShadows' = 'False'
    echo         'CompositeDynamicLights' = 'False'
    echo         'SHSecondaryLighting' = 'False'
    echo         'DirectionalLightmaps' = 'False'
    echo         'bEnableForegroundShadowsOnWorld' = 'False'
    echo         'bAllowWholeSceneDominantShadows' = 'False'
    echo         'ShadowTexelsPerPixel' = '0.000000'
    echo         'MaxWholeSceneDominantShadowResolution' = '16'
    echo         'MaxShadowResolution' = '16'
    echo         'MinShadowResolution' = '16'
    echo         'CSMSplitPenumbraScale' = '0.000000'
    echo         'ScreenPercentage' = '100.000000'
    echo         'UpscaleScreenPercentage' = 'True'
    echo         'MinimumScreenScale' = '1.000000'
    echo     }
    echo.
    echo     foreach ^($entry in $tweaks.GetEnumerator^(^)^) {
    echo         Set-IniLine -Lines $lineList -Key $entry.Key -Value $entry.Value
    echo     }
    echo.
    echo     $outText = ^($lineList -join [Environment]::NewLine^)
    echo     [System.IO.File]::WriteAllText^($path, $outText, $enc^)
    echo.
    echo     Write-Host '[+] INI modificado com sucesso.' -ForegroundColor Green
    echo     Write-Host ('[+] Chaves processadas: {0}' -f $tweaks.Count^) -ForegroundColor Gray
    echo     exit 0
    echo }
    echo catch {
    echo     Write-Host ('[-] ERRO: {0}' -f $_.Exception.Message^) -ForegroundColor Red
    echo     exit 1
    echo }
) > "%PS_SCRIPT%"
endlocal
exit /b 0
