#Requires -Version 5.1
param(
    [string]$Root = (Split-Path $PSScriptRoot -Parent)
)

$ErrorActionPreference = 'Stop'
$DotnetDir = Join-Path $Root 'dotnet'
$EmbedRoot = Join-Path $DotnetDir 'embed'
$PyDir = Join-Path $EmbedRoot 'py311'
$ToolsDir = Join-Path $EmbedRoot 'tools'
$BundleZip = Join-Path $DotnetDir 'embed-bundle.zip'
$Wheel = Join-Path $Root 'tools\nixwrap_rl-0.1.3-py3-none-any.whl'
$PyVer = '3.11.9'
$PyZipName = "python-$PyVer-embed-amd64.zip"
$PyUrl = "https://www.python.org/ftp/python/$PyVer/$PyZipName"

function Ensure-PythonEmbed {
    if (Test-Path (Join-Path $PyDir 'python.exe')) { return }

    New-Item -ItemType Directory -Force -Path $PyDir | Out-Null
    $tmpZip = Join-Path $env:TEMP $PyZipName
    if (-not (Test-Path $tmpZip)) {
        Write-Host "[+] Baixando Python embed $PyVer..."
        Invoke-WebRequest -Uri $PyUrl -OutFile $tmpZip -UseBasicParsing
    }
    Expand-Archive -Path $tmpZip -DestinationPath $PyDir -Force

    $pth = Get-ChildItem $PyDir -Filter 'python*._pth' | Select-Object -First 1
    if (-not $pth) { throw 'python*._pth nao encontrado no embed.' }
    $pthText = Get-Content $pth.FullName -Raw
    $pthText = $pthText -replace '#import site', 'import site'
    if ($pthText -notmatch 'Lib\\site-packages') {
        $pthText = $pthText.TrimEnd() + "`nLib\site-packages`n"
    }
    Set-Content -Path $pth.FullName -Value $pthText -NoNewline

    $siteDir = Join-Path $PyDir 'Lib\site-packages'
    New-Item -ItemType Directory -Force -Path $siteDir | Out-Null

    $getPip = Join-Path $env:TEMP 'get-pip.py'
    if (-not (Test-Path $getPip)) {
        Write-Host '[+] Baixando get-pip.py...'
        Invoke-WebRequest -Uri 'https://bootstrap.pypa.io/get-pip.py' -OutFile $getPip -UseBasicParsing
    }

    $py = Join-Path $PyDir 'python.exe'
    Write-Host '[+] Instalando pip no embed...'
    & $py $getPip --no-warn-script-location -q
    if ($LASTEXITCODE -ne 0) { throw "get-pip falhou ($LASTEXITCODE)" }

    Write-Host '[+] Instalando nixwrap + deps no embed...'
  & $py -m pip install --no-warn-script-location -q `
        --target $siteDir `
        --no-deps --ignore-requires-python $Wheel `
        pycryptodome psutil
    if ($LASTEXITCODE -ne 0) { throw "pip install falhou ($LASTEXITCODE)" }

    & $py -c 'import nixwrap.save_file'
    if ($LASTEXITCODE -ne 0) { throw 'nixwrap nao importa no embed.' }
}

function Ensure-Tools {
    New-Item -ItemType Directory -Force -Path $ToolsDir | Out-Null
    Copy-Item -Force (Join-Path $Root 'tools\patch_save_video.py') (Join-Path $ToolsDir 'patch_save_video.py')
    Copy-Item -Force (Join-Path $Root 'tools\save_codec.py') (Join-Path $ToolsDir 'save_codec.py')
}

function Build-Bundle {
    if (Test-Path $BundleZip) { Remove-Item $BundleZip -Force }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $stage = Join-Path $env:TEMP "gutty-embed-stage-$([guid]::NewGuid().ToString('N'))"
    New-Item -ItemType Directory -Force -Path $stage | Out-Null
    try {
        Copy-Item -Recurse -Force $PyDir (Join-Path $stage 'py311')
        Copy-Item -Recurse -Force $ToolsDir (Join-Path $stage 'tools')
        if (-not (Test-Path (Join-Path $stage 'py311\python.exe'))) {
            throw 'python.exe ausente no stage do bundle.'
        }
        [System.IO.Compression.ZipFile]::CreateFromDirectory($stage, $BundleZip, [System.IO.Compression.CompressionLevel]::Optimal, $false)
        Set-Content -Path $stampFile -Value $latest
        $mb = [math]::Round((Get-Item $BundleZip).Length / 1MB, 1)
        Write-Host "[+] embed-bundle.zip gerado ($mb MB)."
    }
    finally {
        Remove-Item -Recurse -Force $stage -ErrorAction SilentlyContinue
    }
}

if (-not (Test-Path $Wheel)) { throw "Wheel ausente: $Wheel" }

$stampFile = Join-Path $DotnetDir '.embed-stamp'
$inputs = @(
    $Wheel,
    (Join-Path $Root 'tools\patch_save_video.py'),
    (Join-Path $Root 'tools\save_codec.py'),
    (Join-Path $PSScriptRoot 'build_embed_bundle.ps1')
)
$latest = ($inputs | ForEach-Object { (Get-Item $_).LastWriteTimeUtc.Ticks } | Measure-Object -Maximum).Maximum
if ((Test-Path $BundleZip) -and (Test-Path $stampFile) -and [int64](Get-Content $stampFile) -ge $latest) {
    Write-Host '[+] embed-bundle.zip em cache (sem mudancas).'
    return
}

Ensure-PythonEmbed
Ensure-Tools
Build-Bundle
