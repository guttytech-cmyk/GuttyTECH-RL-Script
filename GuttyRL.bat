@echo off
setlocal EnableExtensions EnableDelayedExpansion
color 0C
title GUTTYTECH - RL INI OPTIMIZER v22.0

:: ============================================================================
::  GUTTYTECH - ROCKET LEAGUE INI OPTIMIZER  v22.0  (TESSERACT)
::  - Sem admin. Sem PowerShell no nucleo. Sem bcdedit/TCP/takeown.
::  - Aplica templates completos (sem regex), preservando sua resolucao.
::  - Trava (somente-leitura) para o jogo nao sobrescrever a otimizacao.
::  - REMOVER destrava e restaura o original/stock.
:: ============================================================================

:: --- Pastas de dados persistentes -------------------------------------------
set "GUTTY_DIR=%USERPROFILE%\GuttyTECH\RL-Optimizer-v22"
set "BK_DIR=%GUTTY_DIR%\Backups"
set "LOG=%GUTTY_DIR%\log.txt"
set "ORIG_BK=%BK_DIR%\TASystemSettings.original.ini"
if not exist "%GUTTY_DIR%" mkdir "%GUTTY_DIR%" >nul 2>&1
if not exist "%BK_DIR%" mkdir "%BK_DIR%" >nul 2>&1

:: --- Localizar os templates (ao lado do .bat, subpasta templates, ou dados) --
set "SELF_DIR=%~dp0"
set "TPL_COMPLETO="
set "TPL_CRIADOR="
set "TPL_STOCK="
call :FindTemplate "INI_COMPLETO.txt" TPL_COMPLETO
call :FindTemplate "INI_CRIADOR.txt" TPL_CRIADOR
call :FindTemplate "INI_STOCK_REFERENCE.txt" TPL_STOCK

:: --- Localizar o TASystemSettings.ini do jogo -------------------------------
call :FindIni
if not defined RL_CFG (
    color 0E
    echo.
    echo  [X] TASystemSettings.ini NAO foi encontrado.
    echo  [!] Abra o Rocket League uma vez para ele criar o arquivo e rode de novo.
    echo.
    pause
    exit /b 1
)

:: --- Modo nao-interativo via argumento: COMPLETO, CRIADOR ou REMOVER ---------
set "PAUSEOK=1"
if "%~1"=="" goto :Menu
set "PAUSEOK=0"
if /i "%~2"=="/keepopen" set "PAUSEOK=1"
set "ARG=%~1"
set "ARG=!ARG:/=!"
set "ARG=!ARG:-=!"
if /i "!ARG!"=="1" set "ARG=COMPLETO"
if /i "!ARG!"=="2" set "ARG=CRIADOR"
if /i "!ARG!"=="3" set "ARG=REMOVER"
if /i "!ARG!"=="COMPLETO" goto :DoCompleto
if /i "!ARG!"=="CRIADOR" goto :DoCriador
if /i "!ARG!"=="REMOVER" goto :DoRemover
echo  [X] Argumento invalido: %~1
echo      Uso: GuttyRL.bat [COMPLETO ^| CRIADOR ^| REMOVER]  - sem argumento abre o menu
exit /b 2

:Menu
cls
call :ReadState
echo +==========================================================+
echo ^|        GUTTYTECH - ROCKET LEAGUE INI OPTIMIZER           ^|
echo ^|                   v22.0 - TESSERACT                      ^|
echo +==========================================================+
echo.
echo   Arquivo : !RL_CFG!
echo   Estado  : !CUR_MODE!
echo   Travado : !CUR_LOCK!
echo.
echo   [1] MODO COMPLETO   - FPS maximo, graficos minimos (competitivo)
echo   [2] MODO CRIADOR    - Otimizado, visual preservado (streamers)
echo   [3] REMOVER         - Restaurar original / stock (destrava)
echo   [4] Sair
echo.
set "CHOICE="
set /p "CHOICE=  Escolha (1-4): "
if "!CHOICE!"=="1" goto :DoCompleto
if "!CHOICE!"=="2" goto :DoCriador
if "!CHOICE!"=="3" goto :DoRemover
if "!CHOICE!"=="4" exit /b 0
goto :Menu

:DoCompleto
if not defined TPL_COMPLETO ( call :NoTemplate "INI_COMPLETO.txt" & goto :AfterOp )
call :CheckGame
if errorlevel 1 goto :AfterOp
call :EnsureOriginalBackup
call :ApplyTemplate "!TPL_COMPLETO!" "COMPLETO"
if errorlevel 1 ( call :FailOrElevate "COMPLETO" & goto :AfterOp )
call :Success "MODO COMPLETO"
goto :AfterOp

:DoCriador
if not defined TPL_CRIADOR ( call :NoTemplate "INI_CRIADOR.txt" & goto :AfterOp )
call :CheckGame
if errorlevel 1 goto :AfterOp
call :EnsureOriginalBackup
call :ApplyTemplate "!TPL_CRIADOR!" "CRIADOR"
if errorlevel 1 ( call :FailOrElevate "CRIADOR" & goto :AfterOp )
call :Success "MODO CRIADOR"
goto :AfterOp

:DoRemover
call :CheckGame
if errorlevel 1 goto :AfterOp
call :Remover

:AfterOp
if "!PAUSEOK!"=="0" exit /b 0
goto :Menu

:Fail
color 0E
echo.
echo  [X] Falha ao aplicar. Seu arquivo NAO foi corrompido - ha backup em %BK_DIR%
echo  [!] Tente fechar o jogo por completo e checar antivirus / Acesso Controlado
echo      a Pastas do Windows Defender. Veja a secao Antivirus do README.
echo.
call :Pause
color 0C
exit /b 0

:FailOrElevate
::  %1 = modo. Sem admin e arquivo travado a nivel de sistema -> oferece elevar.
::  Se ja for admin, ou usuario recusar, mostra a falha normal.
net session >nul 2>&1
if not errorlevel 1 ( call :Fail & exit /b 0 )
color 0E
echo.
echo  [!] Nao consegui gravar. O arquivo pode estar travado a nivel de SISTEMA
echo      por um script antigo que rodou como administrador.
echo  [!] Posso tentar de novo como ADMINISTRADOR.
if "!PAUSEOK!"=="1" (
    set "K="
    set /p "K=  Elevar e tentar agora? S/N: "
    if /i not "!K!"=="S" ( call :Fail & exit /b 0 )
)
echo.
echo  [+] Pedindo elevacao ao Windows...
powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -ArgumentList '%~1','/keepopen' -Verb RunAs" >nul 2>&1
if errorlevel 1 (
    echo  [X] Nao consegui elevar. Clique com o botao direito no GuttyRL.bat
    echo      e escolha 'Executar como administrador'.
    call :Pause
    exit /b 0
)
echo  [+] Abri uma janela de administrador para aplicar. Pode fechar esta.
exit /b 0

:: ============================================================================
:: SUBROTINAS
:: ============================================================================

:Pause
if "!PAUSEOK!"=="1" pause
exit /b 0

:FindTemplate
::  %1 = nome do arquivo ; %2 = variavel de saida
set "_fn=%~1"
if exist "%SELF_DIR%%_fn%" ( set "%~2=%SELF_DIR%%_fn%" & exit /b 0 )
if exist "%SELF_DIR%templates\%_fn%" ( set "%~2=%SELF_DIR%templates\%_fn%" & exit /b 0 )
if exist "%GUTTY_DIR%\templates\%_fn%" ( set "%~2=%GUTTY_DIR%\templates\%_fn%" & exit /b 0 )
exit /b 1

:FindIni
set "TARGET_REL=My Games\Rocket League\TAGame\Config\TASystemSettings.ini"
set "RL_CFG="
call :TryCfg "%USERPROFILE%\Documents\%TARGET_REL%"
if not defined RL_CFG call :TryCfg "%USERPROFILE%\OneDrive\Documents\%TARGET_REL%"
if not defined RL_CFG call :TryCfg "%USERPROFILE%\OneDrive - Personal\Documents\%TARGET_REL%"
if not defined RL_CFG call :TryCfg "%USERPROFILE%\OneDrive - Pessoal\Documents\%TARGET_REL%"
if not defined RL_CFG (
    for /d %%U in ("%SystemDrive%\Users\*") do (
        if not defined RL_CFG call :TryCfg "%%U\Documents\%TARGET_REL%"
        if not defined RL_CFG call :TryCfg "%%U\OneDrive\Documents\%TARGET_REL%"
        if not defined RL_CFG call :TryCfg "%%U\OneDrive - Personal\Documents\%TARGET_REL%"
    )
)
if not defined RL_CFG (
    for /f "delims=" %%F in ('dir /s /b "%USERPROFILE%\TASystemSettings.ini" 2^>nul') do (
        if not defined RL_CFG set "RL_CFG=%%F"
    )
)
exit /b 0

:TryCfg
if exist "%~1" set "RL_CFG=%~1"
exit /b 0

:ReadState
set "CUR_MODE=Original / padrao - nao otimizado"
findstr /i /c:"MaxLODSize=16" "!RL_CFG!" >nul 2>&1 && set "CUR_MODE=Otimizado por versao antiga (v21) - reaplique pela v22"
findstr /i /c:"GUTTYTECH-RL-OPTIMIZER=COMPLETO" "!RL_CFG!" >nul 2>&1 && set "CUR_MODE=COMPLETO (FPS maximo)"
findstr /i /c:"GUTTYTECH-RL-OPTIMIZER=CRIADOR" "!RL_CFG!" >nul 2>&1 && set "CUR_MODE=CRIADOR (otimizado + visual)"
set "CUR_LOCK=NAO - o jogo pode sobrescrever"
set "_at="
for %%F in ("!RL_CFG!") do set "_at=%%~aF"
if not "!_at!"=="!_at:r=!" set "CUR_LOCK=SIM - otimizacao protegida"
exit /b 0

:CheckGame
tasklist /fi "IMAGENAME eq RocketLeague.exe" 2>nul | findstr /i "RocketLeague.exe" >nul 2>&1
if errorlevel 1 exit /b 0
color 0E
echo.
echo  [!] O Rocket League esta ABERTO. Ele sobrescreve o arquivo ao fechar.
if "!PAUSEOK!"=="0" ( echo  [X] Feche o jogo e rode de novo. & color 0C & exit /b 1 )
echo.
set "K="
set /p "K=  Fechar o jogo agora? (S/N): "
if /i "!K!"=="S" (
    taskkill /f /im RocketLeague.exe >nul 2>&1
    >nul timeout /t 2 /nobreak 2>nul
    tasklist /fi "IMAGENAME eq RocketLeague.exe" 2>nul | findstr /i "RocketLeague.exe" >nul 2>&1
    if errorlevel 1 ( color 0C & exit /b 0 )
    echo  [X] Nao consegui fechar. Feche manualmente e tente de novo.
    call :Pause
    color 0C
    exit /b 1
)
color 0C
exit /b 1

:EnsureOriginalBackup
if exist "%ORIG_BK%" exit /b 0
findstr /i /c:"GUTTYTECH-RL-OPTIMIZER=" "!RL_CFG!" >nul 2>&1
if not errorlevel 1 (
    >>"%LOG%" echo [%date% %time%] Arquivo atual ja era GuttyTECH; backup original nao e pristino - usar stock no REMOVER.
    exit /b 0
)
findstr /i /c:"MaxLODSize=16" "!RL_CFG!" >nul 2>&1
if not errorlevel 1 (
    >>"%LOG%" echo [%date% %time%] Arquivo atual ja parece otimizado v21.x - nao capturado como original; usar stock no REMOVER.
    exit /b 0
)
copy /y "!RL_CFG!" "%ORIG_BK%" >nul 2>&1
if exist "%ORIG_BK%" (
    attrib -r "%ORIG_BK%" >nul 2>&1
    echo  [+] Backup do seu original salvo em: %ORIG_BK%
    >>"%LOG%" echo [%date% %time%] Backup original pristino criado.
)
exit /b 0

:UnlockIni
::  Destrava o .ini mesmo que um script anterior tenha bloqueado o acesso
::  (somente-leitura/oculto/sistema, dono trocado ou ACL). Best-effort, sem admin.
attrib -r -h -s "!RL_CFG!" >nul 2>&1
takeown /f "!RL_CFG!" >nul 2>&1
icacls "!RL_CFG!" /reset >nul 2>&1
icacls "!RL_CFG!" /grant "%USERNAME%:(F)" /c /q >nul 2>&1
attrib -r -h -s "!RL_CFG!" >nul 2>&1
exit /b 0

:ApplyTemplate
::  %1 = caminho do template ; %2 = nome do modo (COMPLETO/CRIADOR)
set "_tpl=%~1"
set "_mode=%~2"
call :WriteTest || exit /b 1
call :UnlockIni
call :TimeStamp
copy /y "!RL_CFG!" "%BK_DIR%\TASystemSettings.!TS!.bak" >nul 2>&1
set "DSRC=!RL_CFG!"
if exist "%ORIG_BK%" set "DSRC=%ORIG_BK%"
call :ReadDisplay "!DSRC!"
set "_tmp=%GUTTY_DIR%\_apply.tmp"
if exist "!_tmp!" del /f /q "!_tmp!" >nul 2>&1
call :WriteWithDisplay "!_tpl!" "!_tmp!"
if not exist "!_tmp!" exit /b 1
del /f /q "!RL_CFG!" >nul 2>&1
copy /y "!_tmp!" "!RL_CFG!" >nul 2>&1
if errorlevel 1 ( del /f /q "!_tmp!" >nul 2>&1 & exit /b 1 )
del /f /q "!_tmp!" >nul 2>&1
findstr /i /c:"GUTTYTECH-RL-OPTIMIZER=!_mode!" "!RL_CFG!" >nul 2>&1 || exit /b 1
attrib +r "!RL_CFG!" >nul 2>&1
>>"%LOG%" echo [%date% %time%] Aplicado !_mode!. Backup: TASystemSettings.!TS!.bak
exit /b 0

:Remover
call :WriteTest || ( call :Pause & exit /b 0 )
call :UnlockIni
call :TimeStamp
copy /y "!RL_CFG!" "%BK_DIR%\TASystemSettings.!TS!.bak" >nul 2>&1
if exist "%ORIG_BK%" (
    del /f /q "!RL_CFG!" >nul 2>&1
    copy /y "%ORIG_BK%" "!RL_CFG!" >nul 2>&1
    attrib -r "!RL_CFG!" >nul 2>&1
    color 0A
    echo.
    echo  [+] Original restaurado do backup pristino. Arquivo destravado.
    >>"%LOG%" echo [%date% %time%] REMOVER: restaurado do backup original.
    echo.
    call :Pause
    color 0C
    exit /b 0
)
if defined TPL_STOCK (
    call :ReadDisplay "!RL_CFG!"
    set "_tmp=%GUTTY_DIR%\_apply.tmp"
    if exist "!_tmp!" del /f /q "!_tmp!" >nul 2>&1
    call :WriteWithDisplay "!TPL_STOCK!" "!_tmp!"
    if exist "!_tmp!" (
        del /f /q "!RL_CFG!" >nul 2>&1
        copy /y "!_tmp!" "!RL_CFG!" >nul 2>&1
        del /f /q "!_tmp!" >nul 2>&1
    )
    attrib -r "!RL_CFG!" >nul 2>&1
    color 0A
    echo.
    echo  [+] Restaurado para o padrao de fabrica stock, mantendo sua resolucao.
    echo  [+] Arquivo destravado para o jogo gerenciar normalmente.
    >>"%LOG%" echo [%date% %time%] REMOVER: restaurado do stock de referencia.
    echo.
    call :Pause
    color 0C
    exit /b 0
)
color 0E
echo.
echo  [!] Nenhum backup encontrado.
echo  [!] Posso DELETAR o TASystemSettings.ini para o jogo gerar um novo padrao.
echo.
set "K="
set /p "K=  Deletar agora? (S/N): "
if /i "!K!"=="S" (
    del /f /q "!RL_CFG!" >nul 2>&1
    echo  [+] Deletado. Abra o Rocket League para gerar um arquivo padrao novo.
) else (
    echo  [-] Cancelado. Nada foi alterado.
)
echo.
call :Pause
color 0C
exit /b 0

:WriteTest
set "_dir="
for %%I in ("!RL_CFG!") do set "_dir=%%~dpI"
set "_wt=!_dir!gutty_wtest.tmp"
( echo test ) > "!_wt!" 2>nul
if not exist "!_wt!" (
    color 0E
    echo.
    echo  [X] Nao consigo gravar na pasta do jogo:
    echo      !_dir!
    echo  [!] Causa provavel: "Acesso Controlado a Pastas" do Windows Defender
    echo      [protecao contra ransomware], ou um antivirus bloqueando.
    echo  [!] Solucao: permita o cmd.exe / desative temporariamente. Veja o README.
    echo.
    exit /b 1
)
del /f /q "!_wt!" >nul 2>&1
exit /b 0

:TimeStamp
set "TS=%date%_%time%"
set "TS=!TS::=-!"
set "TS=!TS:/=-!"
set "TS=!TS:\=-!"
set "TS=!TS: =0!"
set "TS=!TS:.=-!"
set "TS=!TS:,=-!"
exit /b 0

:ReadDisplay
::  %1 = arquivo de onde ler ResX/ResY/Fullscreen/Borderless/AutoDetect
set "D_ResX=" & set "D_ResY=" & set "D_Full=" & set "D_Border=" & set "D_Auto="
for /f "usebackq tokens=1,* delims==" %%A in (`findstr /b /i /c:"ResX=" /c:"ResY=" /c:"Fullscreen=" /c:"Borderless=" /c:"AutoDetectDesktopResolution=" "%~1" 2^>nul`) do (
    if /i "%%A"=="ResX" if not defined D_ResX set "D_ResX=%%B"
    if /i "%%A"=="ResY" if not defined D_ResY set "D_ResY=%%B"
    if /i "%%A"=="Fullscreen" if not defined D_Full set "D_Full=%%B"
    if /i "%%A"=="Borderless" if not defined D_Border set "D_Border=%%B"
    if /i "%%A"=="AutoDetectDesktopResolution" if not defined D_Auto set "D_Auto=%%B"
)
exit /b 0

:WriteWithDisplay
::  %1 = template de entrada ; %2 = arquivo de saida (com display preservado)
set "_in=%~1"
set "_out=%~2"
set "inSS=0"
set "didResX=" & set "didResY=" & set "didFull=" & set "didBorder=" & set "didAuto="
> "%_out%" (
    for /f "usebackq delims=" %%L in (`findstr /n "^" "%_in%"`) do (
        set "line=%%L"
        set "line=!line:*:=!"
        set "out=!line!"
        if "!line:~0,1!"=="[" (
            if /i "!line!"=="[SystemSettings]" ( set "inSS=1" ) else ( set "inSS=0" )
        )
        if "!inSS!"=="1" (
            if /i "!line:~0,5!"=="ResX=" if defined D_ResX if not defined didResX ( set "out=ResX=!D_ResX!" & set "didResX=1" )
            if /i "!line:~0,5!"=="ResY=" if defined D_ResY if not defined didResY ( set "out=ResY=!D_ResY!" & set "didResY=1" )
            if /i "!line:~0,11!"=="Fullscreen=" if defined D_Full if not defined didFull ( set "out=Fullscreen=!D_Full!" & set "didFull=1" )
            if /i "!line:~0,11!"=="Borderless=" if defined D_Border if not defined didBorder ( set "out=Borderless=!D_Border!" & set "didBorder=1" )
            if /i "!line:~0,28!"=="AutoDetectDesktopResolution=" if defined D_Auto if not defined didAuto ( set "out=AutoDetectDesktopResolution=!D_Auto!" & set "didAuto=1" )
        )
        echo(!out!
    )
)
exit /b 0

:NoTemplate
color 0E
echo.
echo  [X] Template %~1 nao encontrado.
echo  [!] Mantenha a pasta 'templates' (com os .txt) junto do GuttyRL.bat.
echo.
call :Pause
color 0C
exit /b 0

:Success
color 0A
echo.
echo  [+] %~1 aplicado com sucesso!
echo  [+] Arquivo travado (somente-leitura) para o jogo nao sobrescrever.
echo  [+] Sua resolucao/modo de tela foram preservados.
echo  [+] Backups em: %BK_DIR%
echo.
call :Pause
color 0C
exit /b 0
