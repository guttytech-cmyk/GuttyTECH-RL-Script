# Apply COMPLETO/CRIADOR sem UAC (grava Documents + patch save)
param(
  [Parameter(Mandatory=$true)][ValidateSet('COMPLETO','CRIADOR')][string]$Mode
)
$ErrorActionPreference = 'Stop'
$Root = 'C:\Users\a\Downloads\GUTTYTECH-RL-Optimizer'
$Cfg = Join-Path $env:USERPROFILE 'Documents\My Games\Rocket League\TAGame\Config\TASystemSettings.ini'
$EpicSave = Join-Path $env:USERPROFILE 'Documents\My Games\Rocket League\TAGame\SaveDataEpic\DBE_Production'
$Tpl = if ($Mode -eq 'COMPLETO') { Join-Path $Root 'templates\INI_COMPLETO.txt' } else { Join-Path $Root 'templates\INI_CRIADOR.txt' }
$Py = Get-ChildItem (Join-Path $env:USERPROFILE 'GuttyTECH\RL-Optimizer-v22\runtime') -Recurse -Filter python.exe -EA SilentlyContinue |
  Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName

# Kill RL if open
Get-Process RocketLeague -EA SilentlyContinue | Stop-Process -Force -EA SilentlyContinue
Start-Sleep -Seconds 2

# Read display from current INI
$disp = @{ Fullscreen='True'; Borderless='False'; ResX='1920'; ResY='1080'; AutoDetectDesktopResolution='False' }
if (Test-Path $Cfg) {
  $section = ''
  foreach ($line in Get-Content $Cfg) {
    if ($line -match '^\[(.+)\]$') { $section = $Matches[1]; continue }
    if ($section -ne 'SystemSettings') { continue }
    if ($line -match '^(ResX|ResY|Fullscreen|Borderless|AutoDetectDesktopResolution)=(.*)$') {
      $disp[$Matches[1]] = $Matches[2]
    }
  }
}

$content = Get-Content $Tpl -Raw
# Ensure mode marker
if ($content -notmatch 'GuttyTechMode=') {
  $content = $content -replace '\[SystemSettings\]', "[SystemSettings]`r`nGuttyTechMode=$Mode"
} else {
  $content = [regex]::Replace($content, '(?im)^GuttyTechMode=.*$', "GuttyTechMode=$Mode")
}
# Apply display keys in first SystemSettings only (simple line replace global is OK for these)
foreach ($k in $disp.Keys) {
  $content = [regex]::Replace($content, "(?im)^$k=.*$", "$k=$($disp[$k])")
}

# CompletoForce essentials via regex for critical keys when COMPLETO
if ($Mode -eq 'COMPLETO') {
  $force = @{
    ParticleLODBias='100'; DetailMode='0'; bUseTranslucentArenaShaders='False'
    DynamicShadows='False'; DynamicLights='False'; Bloom='False'; bAllowLightShafts='False'
    UncappedFramerate='True'; bSmoothFrameRate='False'; CustomFPS='0'
    OnlyStreamInTextures='False'; WaitForGPU='False'; UseVsync='False'
    ScreenPercentage='100.000000'; MaxAnisotropy='0'
  }
  foreach ($k in $force.Keys) {
    $content = [regex]::Replace($content, "(?im)^$k=.*$", "$k=$($force[$k])")
  }
  # Potato texture groups (all TEXTUREGROUP_ lines)
  $potato = '(MinLODSize=1,MaxLODSize=16,LODBias=15,MinMagFilter=Point,MipFilter=Point,MipGenSettings=TMGS_SimpleAverage)'
  $content = [regex]::Replace($content, '(?im)^(TEXTUREGROUP_[^=]+)=.*$', "`$1=$potato")
}

# Backup then write
$bakDir = Join-Path $env:USERPROFILE 'GuttyTECH\RL-Optimizer-v22\Backups'
New-Item -ItemType Directory -Force -Path $bakDir | Out-Null
if (Test-Path $Cfg) {
  Copy-Item $Cfg (Join-Path $bakDir ("TASystemSettings_{0:yyyyMMdd_HHmmss}.ini" -f (Get-Date))) -Force
  attrib -R $Cfg 2>$null
}
[IO.File]::WriteAllText($Cfg, $content.Replace("`n","`r`n").Replace("`r`r`n","`r`n"), [Text.UTF8Encoding]::new($false))
Write-Host "[+] INI escrito ($Mode)" -ForegroundColor Green

# Patch saves
$env:PYTHONPATH = Join-Path $Root 'tools'
$modeArg = if ($Mode -eq 'COMPLETO') { 'completo' } else { 'criador' }
& $Py (Join-Path $Root 'tools\patch_save_video.py') --mode $modeArg $EpicSave
Write-Host "[+] Save patch done" -ForegroundColor Green

# Quick verify
$t = Get-Content $Cfg -Raw
if ($t -match "GuttyTechMode=$Mode") { Write-Host "[OK] GuttyTechMode=$Mode" -ForegroundColor Green } else { Write-Host "[X] marker" -ForegroundColor Red; exit 1 }
exit 0
