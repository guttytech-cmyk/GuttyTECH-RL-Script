@echo off
setlocal
cd /d "%~dp0"
echo Building GUTTY QuickChat...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
if errorlevel 1 exit /b 1
echo.
echo OK: publish\GuttyQuickChat.exe
endlocal
