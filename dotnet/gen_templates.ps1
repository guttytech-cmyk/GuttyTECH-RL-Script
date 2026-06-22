# Gera dotnet/Templates.cs embutindo templates/*.txt como const string (verbatim).
# Rode via build_exe.bat (ou direto). Os .txt nao tem aspas duplas, entao @"..." e seguro.
param([string]$Root = (Split-Path $PSScriptRoot -Parent))

$map = [ordered]@{
    Completo = 'INI_COMPLETO.txt'
    Criador  = 'INI_CRIADOR.txt'
    Stock    = 'INI_STOCK_REFERENCE.txt'
}

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('namespace GuttyRL;')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('// AUTO-GERADO a partir de templates/*.txt por gen_templates.ps1. Nao editar a mao.')
[void]$sb.AppendLine('internal static class Templates')
[void]$sb.AppendLine('{')
foreach ($k in $map.Keys) {
    $path = Join-Path $Root "templates\$($map[$k])"
    if (-not (Test-Path $path)) { throw "Template nao encontrado: $path" }
    $raw = Get-Content $path -Raw
    $esc = $raw.Replace('"', '""')
    [void]$sb.AppendLine('    public const string ' + $k + ' = @"' + $esc + '";')
    [void]$sb.AppendLine('')
}
[void]$sb.AppendLine('}')

$out = Join-Path $PSScriptRoot 'Templates.cs'
[IO.File]::WriteAllText($out, $sb.ToString(), (New-Object System.Text.UTF8Encoding($false)))
Write-Host "[+] Templates.cs regenerado ($([math]::Round((Get-Item $out).Length/1KB)) KB)."
