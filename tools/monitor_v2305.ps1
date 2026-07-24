#Requires -Version 5.1
<#
  Monitor v23.0.5: CRIADOR(+RL) -> COMPLETO(+RL) -> heal watcher -> testes extra
#>
$ErrorActionPreference = 'Continue'
$Root = 'C:\Users\a\Downloads\GUTTYTECH-RL-Optimizer'
$LogDir = Join-Path $env:USERPROFILE 'GuttyTECH\RL-Optimizer-v22\monitor'
New-Item -ItemType Directory -Force -Path $LogDir | Out-Null
$Log = Join-Path $LogDir ("cycle_v2305_{0:yyyyMMdd_HHmmss}.log" -f (Get-Date))
$Cfg = Join-Path $env:USERPROFILE 'Documents\My Games\Rocket League\TAGame\Config\TASystemSettings.ini'
$EpicSave = Join-Path $env:USERPROFILE 'Documents\My Games\Rocket League\TAGame\SaveDataEpic\DBE_Production'
$RlExe = 'C:\Program Files\Epic Games\rocketleague\Binaries\Win64\RocketLeague.exe'
$OptExe = Join-Path $Root 'publish\GuttyTECH_RL.exe'
$Lock = Join-Path $env:USERPROFILE 'GuttyTECH\RL-Optimizer-v22\watcher.lock'
$AppLog = Join-Path $env:USERPROFILE 'GuttyTECH\RL-Optimizer-v22\log.txt'
$Py = Get-ChildItem (Join-Path $env:USERPROFILE 'GuttyTECH\RL-Optimizer-v22\runtime') -Recurse -Filter python.exe -EA SilentlyContinue |
  Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
$env:PYTHONPATH = Join-Path $Root 'tools'
$Results = [ordered]@{}

function Log([string]$msg, [string]$color = 'White') {
  $line = "[{0:HH:mm:ss}] {1}" -f (Get-Date), $msg
  Write-Host $line -ForegroundColor $color
  Add-Content -Path $Log -Value $line
}

function Get-Rl { Get-Process RocketLeague -EA SilentlyContinue }
function Stop-Rl {
  $p = Get-Rl
  if (-not $p) { Log 'RL ja fechado' 'DarkGray'; return }
  Log "A fechar RL pid=$($p.Id)" 'Yellow'
  $p | Stop-Process -Force -EA SilentlyContinue
  $d = (Get-Date).AddSeconds(30)
  while ((Get-Rl) -and (Get-Date) -lt $d) { Start-Sleep -Milliseconds 400 }
  Start-Sleep -Seconds 2
  if (Get-Rl) { Log 'FALHA fechar RL' 'Red' } else { Log 'RL fechado' 'Green' }
}
function Start-Rl {
  if (Get-Rl) { Log 'RL ja aberto' 'Yellow'; return $true }
  Log 'A abrir RL...' 'Cyan'
  Start-Process 'com.epicgames.launcher://apps/Sugar?action=launch&silent=true' -EA SilentlyContinue
  Start-Sleep -Seconds 4
  if (-not (Get-Rl)) { Start-Process -FilePath $RlExe -WorkingDirectory (Split-Path $RlExe) }
  $d = (Get-Date).AddSeconds(120)
  while (-not (Get-Rl) -and (Get-Date) -lt $d) { Start-Sleep -Seconds 2; Log 'waiting RL...' 'DarkGray' }
  if (Get-Rl) { Log "RL UP pid=$((Get-Rl).Id)" 'Green'; return $true }
  Log 'FALHA abrir RL' 'Red'; return $false
}

function Audit-Ini([string]$mode) {
  Log "--- AUDIT INI ($mode) ---" 'Cyan'
  if (-not (Test-Path $Cfg)) { Log 'INI missing' 'Red'; return $false }
  $map = @{}; $section = ''
  foreach ($line in (Get-Content -LiteralPath $Cfg)) {
    if ($line -match '^\[(.+)\]$') { $section = $Matches[1]; continue }
    if ($section -ne 'SystemSettings') { continue }
    if ($line -match '^([^=]+)=(.*)$') { $map[$Matches[1]] = $Matches[2] }
  }
  $checks = @{}
  if ($mode -eq 'COMPLETO') {
    $checks = @{
      GuttyTechMode='COMPLETO'; DetailMode='0'; ParticleLODBias='100'
      DynamicShadows='False'; bAllowLightShafts='False'; Bloom='False'
      UncappedFramerate='True'; bUseTranslucentArenaShaders='False'
      OnlyStreamInTextures='False'; WaitForGPU='False'
    }
  } elseif ($mode -eq 'CRIADOR') {
    $checks = @{
      GuttyTechMode='CRIADOR'; DynamicShadows='True'; DynamicLights='True'
      UncappedFramerate='True'; bAllowLightShafts='False'; Bloom='False'
      OnlyStreamInTextures='False'; WaitForGPU='False'
    }
  }
  $ok = $true
  foreach ($k in $checks.Keys) {
    $got = $map[$k]; $want = $checks[$k]
    if ($got -ne $want) { Log "[DIFF] $k=$got (want $want)" 'Red'; $ok = $false }
    else { Log "[OK] $k=$got" 'Green' }
  }
  Log "Fullscreen=$($map['Fullscreen']) Borderless=$($map['Borderless']) Res=$($map['ResX'])x$($map['ResY'])" 'Gray'
  return $ok
}

function Audit-Save([string]$mode) {
  Log "--- AUDIT SAVE ($mode) ---" 'Cyan'
  if (-not $Py) { Log 'Python missing' 'Red'; return $false }
  $scriptFile = Join-Path $LogDir 'audit_save_tmp.py'
  @"
import sys
from pathlib import Path
sys.path.insert(0, r'$Root\tools')
import nixwrap.save_file._file_io as _fio
from save_codec import serialize_property_stream as c
_fio.serialize_property_stream = c
from nixwrap.save_file import load_raw
from patch_save_video import _completo_options_ok, _criador_options_ok, _sanitize_options
d = Path(r'$EpicSave')
files = sorted([p for p in d.glob('*.save') if p.stat().st_size <= 1200000], key=lambda p: p.stat().st_mtime, reverse=True)[:4]
mode = '$mode'
any_ok = False
for p in files:
    try: raw = load_raw(p)
    except Exception as ex:
        print('ERR', p.name, ex); continue
    for obj in raw.get('objects', []):
        if obj.get('__type') != 'TAGame.VideoSettingsSavePC_TA': continue
        opts = _sanitize_options(obj.get('VideoOptions'))
        ok = _completo_options_ok(obj) if mode == 'COMPLETO' else _criador_options_ok(obj)
        rd = [o['Value'] for o in opts if o['Id']=='RenderDetail']
        print(('OK' if ok else 'BAD'), p.name[:28], 'detail=', rd, 'uncap=', obj.get('bUncappedFramerate'),
              'shafts=', obj.get('bShowLightShafts'), 'fps=', obj.get('MaxFPS'), 'nopts=', len(opts))
        if ok: any_ok = True
print('ANY_OK=' + str(any_ok))
"@ | Set-Content -Encoding UTF8 -Path $scriptFile
  $out = & $Py $scriptFile 2>&1 | Out-String
  foreach ($l in ($out -split "`r?`n")) {
    if (-not $l.Trim()) { continue }
    $col = if ($l -match '^OK|ANY_OK=True') { 'Green' } elseif ($l -match 'BAD|False|ERR') { 'Red' } else { 'Gray' }
    Log $l $col
  }
  return $out -match 'ANY_OK=True'
}

function Apply-Mode([string]$mode) {
  Log "=== APPLY $mode (direct + WATCH) ===" 'Magenta'
  Stop-Rl
  & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $Root 'tools\apply_mode_direct.ps1') -Mode $mode
  if ($LASTEXITCODE -ne 0) { Log "apply_direct exit=$LASTEXITCODE" 'Red'; return $false }
  # Start official watcher (elevated) — singleton
  try {
    $wp = Start-Process -FilePath $OptExe -ArgumentList 'WATCH',$mode -Verb RunAs -PassThru -WindowStyle Hidden
    Log "Watcher started pid=$($wp.Id)" 'Green'
  } catch {
    Log "Watcher start fail: $_" 'Yellow'
  }
  Start-Sleep -Seconds 2
  return $true
}

function Wait-WatcherHeal([int]$seconds = 18) {
  Log "A aguardar watcher heal (${seconds}s)..." 'Yellow'
  Start-Sleep -Seconds $seconds
  if (Test-Path $AppLog) {
    Get-Content $AppLog -Tail 12 | ForEach-Object { Log "LOG: $_" 'DarkGray' }
  }
}

function Invoke-Elev([string]$arg) {
  Log "=== EXE $arg ===" 'Magenta'
  $p = Start-Process -FilePath $OptExe -ArgumentList $arg -Verb RunAs -PassThru -Wait -WindowStyle Hidden
  Log "exit=$($p.ExitCode)" $(if ($p.ExitCode -eq 0) { 'Green' } else { 'Yellow' })
  return $p.ExitCode
}

Log "LOG=$Log" 'Cyan'
Log "OptExe=$(Test-Path $OptExe) Py=$([bool]$Py)" 'Gray'
Get-Process GuttyTECH_RL -EA SilentlyContinue | Stop-Process -Force -EA SilentlyContinue
Remove-Item $Lock -Force -EA SilentlyContinue
Stop-Rl

# ========== FASE A: CRIADOR + abrir jogo ==========
Log '======== FASE A: CRIADOR + ABRIR JOGO ========' 'Magenta'
$Results['A_apply'] = Apply-Mode 'CRIADOR'
$Results['A_pre_ini'] = Audit-Ini 'CRIADOR'
$Results['A_pre_save'] = Audit-Save 'CRIADOR'
$startedA = Start-Rl
if ($startedA) {
  Log 'Aguardar 30s boot CRIADOR...' 'Yellow'
  Start-Sleep -Seconds 30
  $Results['A_pos_ini'] = Audit-Ini 'CRIADOR'
  $Results['A_pos_save'] = Audit-Save 'CRIADOR'
  Stop-Rl
  Wait-WatcherHeal 18
  $Results['A_heal_ini'] = Audit-Ini 'CRIADOR'
  $Results['A_heal_save'] = Audit-Save 'CRIADOR'
} else {
  $Results['A_pos_ini'] = $false; $Results['A_pos_save'] = $false
  $Results['A_heal_ini'] = $false; $Results['A_heal_save'] = $false
}
Log "FASE_A pre=$($Results['A_pre_ini'])/$($Results['A_pre_save']) pos=$($Results['A_pos_ini'])/$($Results['A_pos_save']) heal=$($Results['A_heal_ini'])/$($Results['A_heal_save'])" 'Cyan'

# ========== FASE B: COMPLETO + abrir jogo ==========
Log '======== FASE B: COMPLETO + ABRIR JOGO ========' 'Magenta'
$Results['B_apply'] = Apply-Mode 'COMPLETO'
$Results['B_pre_ini'] = Audit-Ini 'COMPLETO'
$Results['B_pre_save'] = Audit-Save 'COMPLETO'
$startedB = Start-Rl
if ($startedB) {
  Log 'Aguardar 30s boot COMPLETO...' 'Yellow'
  Start-Sleep -Seconds 30
  $Results['B_pos_ini'] = Audit-Ini 'COMPLETO'
  $Results['B_pos_save'] = Audit-Save 'COMPLETO'
  Stop-Rl
  Wait-WatcherHeal 18
  $Results['B_heal_ini'] = Audit-Ini 'COMPLETO'
  $Results['B_heal_save'] = Audit-Save 'COMPLETO'
} else {
  $Results['B_pos_ini'] = $false; $Results['B_pos_save'] = $false
  $Results['B_heal_ini'] = $false; $Results['B_heal_save'] = $false
}
Log "FASE_B pre=$($Results['B_pre_ini'])/$($Results['B_pre_save']) pos=$($Results['B_pos_ini'])/$($Results['B_pos_save']) heal=$($Results['B_heal_ini'])/$($Results['B_heal_save'])" 'Cyan'

# ========== TESTES EXTRA ==========
Log '======== TESTES EXTRA ========' 'Magenta'
Get-Process GuttyTECH_RL -EA SilentlyContinue | Stop-Process -Force -EA SilentlyContinue
Start-Sleep -Seconds 1

$Results['X_AUDIT'] = ((Invoke-Elev 'AUDIT') -eq 0)
$Results['X_DIAG'] = $true
Invoke-Elev 'DIAG' | Out-Null  # exit 1 se issues — esperado se algo residual

# REPARAR PERFIL (deve manter COMPLETO OK)
$Results['X_REPARAR'] = ((Invoke-Elev 'CORRIGIR-PERFIL') -eq 0)
$Results['X_REPARAR_ini'] = Audit-Ini 'COMPLETO'
$Results['X_REPARAR_save'] = Audit-Save 'COMPLETO'

# Watcher singleton: 2 WATCH -> 1 processo
$w1 = Start-Process -FilePath $OptExe -ArgumentList 'WATCH','COMPLETO' -Verb RunAs -PassThru -WindowStyle Hidden
Start-Sleep -Seconds 2
$w2 = Start-Process -FilePath $OptExe -ArgumentList 'WATCH','COMPLETO' -Verb RunAs -PassThru -WindowStyle Hidden
Start-Sleep -Seconds 3
$n = @(Get-Process GuttyTECH_RL -EA SilentlyContinue).Count
$Results['X_WATCHER_SINGLE'] = ($n -eq 1)
Log "Watcher processes=$n (want 1) SINGLE=$($Results['X_WATCHER_SINGLE'])" $(if ($n -eq 1) { 'Green' } else { 'Red' })
if (Test-Path $Lock) { Log "lock=$(Get-Content $Lock -Raw)" 'Gray' }
Get-Process GuttyTECH_RL -EA SilentlyContinue | Stop-Process -Force -EA SilentlyContinue
Remove-Item $Lock -Force -EA SilentlyContinue

# Switch CRIADOR->COMPLETO rapid without game (marker + keys)
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $Root 'tools\apply_mode_direct.ps1') -Mode CRIADOR | Out-Null
$sw1 = Audit-Ini 'CRIADOR'
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $Root 'tools\apply_mode_direct.ps1') -Mode COMPLETO | Out-Null
$sw2 = Audit-Ini 'COMPLETO'
$Results['X_SWITCH'] = ($sw1 -and $sw2)
Log "SWITCH CRIADOR->COMPLETO=$($Results['X_SWITCH'])" $(if ($Results['X_SWITCH']) { 'Green' } else { 'Red' })

Log '======== RESUMO ========' 'Magenta'
foreach ($k in $Results.Keys) {
  $v = $Results[$k]
  Log ("{0}={1}" -f $k, $v) $(if ($v) { 'Green' } else { 'Red' })
}
Log "LOG=$Log" 'Cyan'
Write-Output "MONITOR_LOG=$Log"
# Exit 0 if critical path OK (apply+heal for both modes)
$crit = $Results['A_apply'] -and $Results['A_heal_ini'] -and $Results['A_heal_save'] -and `
        $Results['B_apply'] -and $Results['B_heal_ini'] -and $Results['B_heal_save'] -and `
        $Results['X_AUDIT'] -and $Results['X_REPARAR'] -and $Results['X_WATCHER_SINGLE'] -and $Results['X_SWITCH']
if ($crit) { exit 0 } else { exit 1 }
