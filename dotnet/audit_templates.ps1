# Valida templates INI_COMPLETO / INI_CRIADOR (via GuttyTECH_RL.exe AUDIT).
param([string]$Root = (Split-Path $PSScriptRoot -Parent))

$ErrorActionPreference = 'Stop'
$dll = Join-Path $PSScriptRoot 'bin/Release/net9.0/win-x64/GuttyTECH_RL.dll'
if (-not (Test-Path $dll)) {
    Write-Host '[!] DLL Release nao encontrada; compilando...' -ForegroundColor Yellow
    & dotnet build (Join-Path $PSScriptRoot 'GuttyRL.csproj') -c Release -r win-x64 --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw 'Build falhou' }
}

& dotnet exec $dll AUDIT
exit $LASTEXITCODE
