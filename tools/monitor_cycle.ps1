#Requires -Version 5.1
<#
.SYNOPSIS
  Ciclo de monitorizacao GuttyTECH RL: jogo -> CRIADOR -> COMPLETO -> jogo
#>
$ErrorActionPreference = 'Continue'
$Root = 'C:\Users\a\Downloads\GUTTYTECH-RL-Optimizer'
$LogDir = Join-Path $env:USERPROFILE 'GuttyTECH\RL-Optimizer-v22\monitor'
New-Item -ItemType Directory -Force -Path $LogDir | Out-Null
$Log = Join-Path $LogDir ("cycle_{0:yyyyMMdd_HHmmss}.log" -f (Get-Date))
$Cfg = Join-Path $env:USERPROFILE 'Documents\My Games\Rocket League\TAGame\Config\TASystemSettings.ini'
$EpicSave = Join-Path $env:USERPROFILE 'Documents\My Games\Rocket League\TAGame\SaveDataEpic\DBE_Production'
$RlExe = 'C:\Program Files\Epic Games\rocketleague\Binaries\Win64\RocketLeague.exe'
$OptExe = Join-Path $Root 'publish\GuttyTECH_RL.exe'
$Py = Get-ChildItem (Join-Path $env:USERPROFILE 'GuttyTECH\RL-Optimizer-v22\runtime') -Recurse -Filter python.exe -ErrorAction SilentlyContinue |
  Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
$env:PYTHONPATH = Join-Path $Root 'tools'

function Log([string]$msg, [string]$color = 'White') {
  $line = "[{0:HH:mm:ss}] {1}" -f (Get-Date), $msg
  Write-Host $line -ForegroundColor $color
  Add-Content -Path $Log -Value $line
}

function Get-Rl {
  Get-Process RocketLeague -ErrorAction SilentlyContinue
}

function Stop-Rl {
  $p = Get-Rl
  if (-not $p) { Log 'RL ja fechado' 'DarkGray'; return }
  Log "A fechar RL (pid=$($p.Id))" 'Yellow'
  $p | Stop-Process -Force -ErrorAction SilentlyContinue
  $deadline = (Get-Date).AddSeconds(30)
  while ((Get-Rl) -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 500 }
  if (Get-Rl) { Log 'FALHA a fechar RL' 'Red' } else { Log 'RL fechado' 'Green' }
  Start-Sleep -Seconds 2
}

function Start-Rl {
  if (Get-Rl) { Log 'RL ja aberto' 'Yellow'; return $true }
  Log "A abrir RL: $RlExe" 'Cyan'
  try {
    # Prefer Epic protocol
    Start-Process 'com.epicgames.launcher://apps/Sugar?action=launch&silent=true' -ErrorAction SilentlyContinue
  } catch {}
  Start-Sleep -Seconds 3
  if (-not (Get-Rl)) {
    Start-Process -FilePath $RlExe -WorkingDirectory (Split-Path $RlExe)
  }
  $deadline = (Get-Date).AddSeconds(120)
  while (-not (Get-Rl) -and (Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 2
    Log 'A aguardar processo RocketLeague...' 'DarkGray'
  }
  if (Get-Rl) {
    Log "RL UP pid=$((Get-Rl).Id)" 'Green'
    return $true
  }
  Log 'FALHA: RL nao abriu em 120s' 'Red'
  return $false
}

function Audit-Ini([string]$expectMode) {
  Log "--- AUDIT INI (expect=$expectMode) ---" 'Cyan'
  if (-not (Test-Path $Cfg)) { Log 'INI em falta' 'Red'; return $false }
  $text = Get-Content -LiteralPath $Cfg -Raw
  $ok = $true
  $checks = @{}
  if ($expectMode -eq 'COMPLETO') {
    $checks = @{
      'GuttyTechMode' = 'COMPLETO'
      'DetailMode' = '0'
      'ParticleLODBias' = '100'
      'DynamicShadows' = 'False'
      'bAllowLightShafts' = 'False'
      'Bloom' = 'False'
      'UncappedFramerate' = 'True'
      'bUseTranslucentArenaShaders' = 'False'
      'OnlyStreamInTextures' = 'False'
      'WaitForGPU' = 'False'
    }
  } elseif ($expectMode -eq 'CRIADOR') {
    $checks = @{
      'GuttyTechMode' = 'CRIADOR'
      'DynamicShadows' = 'True'
      'DynamicLights' = 'True'
      'UncappedFramerate' = 'True'
      'bAllowLightShafts' = 'False'
      'Bloom' = 'False'
      'OnlyStreamInTextures' = 'False'
      'WaitForGPU' = 'False'
    }
  }

  # Parse [SystemSettings]
  $map = @{}
  $section = ''
  foreach ($line in ($text -split "`r?`n")) {
    if ($line -match '^\[(.+)\]$') { $section = $Matches[1]; continue }
    if ($section -ne 'SystemSettings') { continue }
    if ($line -match '^([^=]+)=(.*)$') { $map[$Matches[1]] = $Matches[2] }
  }

  foreach ($k in $checks.Keys) {
    $want = $checks[$k]
    $got = $map[$k]
    if ($null -eq $got) {
      Log "[MISSING] $k" 'Red'; $ok = $false
    } elseif ($got -ne $want) {
      Log "[DIFF] $k=$got (want $want)" 'Red'; $ok = $false
    } else {
      Log "[OK] $k=$got" 'Green'
    }
  }
  $border = "Fullscreen=$($map['Fullscreen']) Borderless=$($map['Borderless']) Res=$($map['ResX'])x$($map['ResY'])"
  Log $border 'Gray'
  return $ok
}

function Audit-Save([string]$expectMode) {
  Log "--- AUDIT SAVE ($expectMode) ---" 'Cyan'
  if (-not $Py) { Log 'Python runtime em falta' 'Red'; return $false }
  $tools = Join-Path $Root 'tools'
  $scriptFile = Join-Path $LogDir 'audit_save_tmp.py'
  @"
import sys
from pathlib import Path
sys.path.insert(0, r'$tools')
import nixwrap.save_file._file_io as _fio
from save_codec import serialize_property_stream as c
_fio.serialize_property_stream = c
from nixwrap.save_file import load_raw
from patch_save_video import _completo_options_ok, _criador_options_ok, _sanitize_options
d = Path(r'$EpicSave')
files = sorted([p for p in d.glob('*.save') if p.stat().st_size <= 1200000], key=lambda p: p.stat().st_mtime, reverse=True)[:4]
mode = '$expectMode'
any_ok = False
for p in files:
    try:
        raw = load_raw(p)
    except Exception as ex:
        print('ERR', p.name, ex)
        continue
    for obj in raw.get('objects', []):
        if obj.get('__type') != 'TAGame.VideoSettingsSavePC_TA':
            continue
        opts = _sanitize_options(obj.get('VideoOptions'))
        ok = _completo_options_ok(obj) if mode == 'COMPLETO' else _criador_options_ok(obj)
        rd = [o['Value'] for o in opts if o['Id']=='RenderDetail']
        print(('OK' if ok else 'BAD'), p.name[:28], 'detail=', rd, 'uncap=', obj.get('bUncappedFramerate'), 'shafts=', obj.get('bShowLightShafts'), 'weather=', obj.get('bShowWeatherFX'), 'fps=', obj.get('MaxFPS'), 'win=', obj.get('WindowMode'))
        if ok:
            any_ok = True
print('ANY_OK=' + str(any_ok))
"@ | Set-Content -Encoding UTF8 -Path $scriptFile
  $out = & $Py $scriptFile 2>&1 | Out-String
  foreach ($l in ($out -split "`r?`n")) {
    if (-not $l.Trim()) { continue }
    $col = if ($l -match '^OK|ANY_OK=True') { 'Green' } elseif ($l -match '^BAD|ANY_OK=False|ERR') { 'Red' } else { 'Gray' }
    Log $l $col
  }
  return $out -match 'ANY_OK=True'
}

function Invoke-Optimizer([string]$mode) {
  Log "=== APLICAR $mode (UAC) ===" 'Magenta'
  if (-not (Test-Path $OptExe)) { Log "Exe em falta: $OptExe" 'Red'; return $false }
  Stop-Rl
  # Elevacao — utilizador tem de clicar Sim no UAC
  $p = Start-Process -FilePath $OptExe -ArgumentList $mode -Verb RunAs -PassThru -Wait
  $code = $p.ExitCode
  Log "Optimizer exit=$code" $(if ($code -eq 0) { 'Green' } else { 'Yellow' })
  Start-Sleep -Seconds 1
  return ($code -eq 0)
}

Log "LOG=$Log" 'Cyan'
Log "OptExe=$OptExe exists=$(Test-Path $OptExe)" 'Gray'
Log "Py=$Py" 'Gray'

# ---- FASE 1: abrir jogo e auditar estado atual ----
Log '======== FASE 1: ABRIR JOGO + AUDIT ESTADO ATUAL ========' 'Magenta'
$started = Start-Rl
if ($started) {
  Log 'A aguardar 25s boot/menu...' 'Yellow'
  Start-Sleep -Seconds 25
  Audit-Ini 'UNKNOWN' | Out-Null
  # Snapshot parcial (estado atual pode ser COMPLETO parcial)
  $text = Get-Content -LiteralPath $Cfg -Raw
  if ($text -match 'GuttyTechMode=COMPLETO|GUTTYTECH-RL-OPTIMIZER=COMPLETO') {
    $a1 = Audit-Ini 'COMPLETO'
    $s1 = Audit-Save 'COMPLETO'
  } elseif ($text -match 'GuttyTechMode=CRIADOR|GUTTYTECH-RL-OPTIMIZER=CRIADOR') {
    $a1 = Audit-Ini 'CRIADOR'
    $s1 = Audit-Save 'CRIADOR'
  } else {
    Log 'Sem marcador GuttyTechMode — INI parcial/corrupto' 'Red'
    $a1 = $false
    $s1 = Audit-Save 'COMPLETO'
  }
  Log "FASE1 INI_OK=$a1 SAVE_OK=$s1" $(if ($a1 -and $s1) { 'Green' } else { 'Red' })
}

# ---- FASE 2: fechar + CRIADOR ----
Log '======== FASE 2: FECHAR + APLICAR CRIADOR ========' 'Magenta'
Stop-Rl
$cOk = Invoke-Optimizer 'CRIADOR'
$a2 = Audit-Ini 'CRIADOR'
$s2 = Audit-Save 'CRIADOR'
Log "FASE2 apply=$cOk INI_OK=$a2 SAVE_OK=$s2" $(if ($cOk -and $a2 -and $s2) { 'Green' } else { 'Red' })

# ---- FASE 3: fechar + COMPLETO + abrir jogo ----
Log '======== FASE 3: FECHAR + APLICAR COMPLETO + ABRIR JOGO ========' 'Magenta'
Stop-Rl
$pOk = Invoke-Optimizer 'COMPLETO'
$a3 = Audit-Ini 'COMPLETO'
$s3 = Audit-Save 'COMPLETO'
Log "FASE3 apply=$pOk INI_OK=$a3 SAVE_OK=$s3 (pre-boot)" $(if ($pOk -and $a3 -and $s3) { 'Green' } else { 'Red' })

$started3 = Start-Rl
if ($started3) {
  Log 'A aguardar 30s apos COMPLETO...' 'Yellow'
  Start-Sleep -Seconds 30
  # Re-audit: jogo pode ter reescrito
  $a3b = Audit-Ini 'COMPLETO'
  $s3b = Audit-Save 'COMPLETO'
  Log "FASE3 POS-BOOT INI_OK=$a3b SAVE_OK=$s3b" $(if ($a3b -and $s3b) { 'Green' } else { 'Red' })
  Stop-Rl
  # Se o jogo reescreveu, reaplicar COMPLETO
  if (-not ($a3b -and $s3b)) {
    Log 'REGRESSAO pos-boot — a reaplicar COMPLETO' 'Yellow'
    Invoke-Optimizer 'COMPLETO' | Out-Null
    Audit-Ini 'COMPLETO' | Out-Null
    Audit-Save 'COMPLETO' | Out-Null
  }
}

Log '======== FIM DO CICLO ========' 'Magenta'
Log "Log completo: $Log" 'Cyan'
Write-Output "MONITOR_LOG=$Log"
