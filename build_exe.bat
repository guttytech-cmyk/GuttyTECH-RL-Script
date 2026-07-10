@echo off
setlocal EnableExtensions
title GUTTYTECH - Build GuttyTECH_RL.exe (.NET 9)
color 0B

:: ============================================================================
::  build_exe.bat - Compila o GuttyTECH_RL.exe (console .NET 9, arquivo UNICO).
::  - Self-contained: o cliente NAO precisa ter .NET instalado.
::  - Single-file: voce manda so o GuttyTECH_RL.exe (templates embutidos dentro).
::  - Roda UMA vez, na SUA maquina (precisa do .NET 9 SDK). O cliente so executa.
:: ============================================================================

set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"

echo.
echo  [+] Verificando .NET SDK...
where dotnet >nul 2>&1
if errorlevel 1 (
    color 0E
    echo  [X] .NET SDK nao encontrado. Instale o .NET 9 SDK:
    echo      https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo  [+] Regenerando Templates.cs a partir de templates\ ...
powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%\dotnet\gen_templates.ps1" -Root "%ROOT%"
if errorlevel 1 ( color 0E & echo  [X] Falha ao gerar Templates.cs & pause & exit /b 1 )

echo  [+] Compilando GuttyTECH_RL.exe ^(single-file^)... pode levar ~1 min na 1a vez.
::  PublishTrimmed=false: trimming quebrava startup em varios PCs (fecha na hora).
dotnet publish "%ROOT%\dotnet\GuttyRL.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=false -p:NoWarn=CA1416 -o "%ROOT%\dotnet\publish" --nologo

if not exist "%ROOT%\dotnet\publish\GuttyTECH_RL.exe" (
    color 0E
    echo  [X] Compilacao falhou. Veja as mensagens acima.
    pause
    exit /b 1
)

copy /y "%ROOT%\dotnet\publish\GuttyTECH_RL.exe" "%ROOT%\GuttyTECH_RL.exe" >nul

color 0A
echo.
echo  [+] PRONTO: %ROOT%\GuttyTECH_RL.exe
echo  [+] Arquivo UNICO e autossuficiente - mande so ele pros clientes.
echo  [+] Abre uma janela de menu ao dar 2 cliques (nao precisa de .NET no cliente).
echo.
pause
exit /b 0
