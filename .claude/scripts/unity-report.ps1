#requires -Version 5.1
<#
  unity-report.ps1 - lee el informe mas reciente de PlaytestLogs y lo resume.
  (Fichero en ASCII PURO: PS 5.1 lee un .ps1 sin BOM como ANSI y las tildes rompen el parser.)

  IMPORTANTE: los informes van en UTF-8. Get-Content de PS 5.1 usa ANSI por
  defecto y destroza el castellano ("Simulacion" -> "SimulaciA3n"), asi que
  aqui se lee SIEMPRE con -Encoding UTF8.

  Uso:
    .\.claude\scripts\unity-report.ps1 -Kind verify
    .\.claude\scripts\unity-report.ps1 -Kind sim
    .\.claude\scripts\unity-report.ps1 -Kind sim -Full     # informe entero (sin el log de ejemplo)
#>
[CmdletBinding()]
param(
    [ValidateSet('verify','sim')]
    [string]$Kind = 'verify',
    [switch]$Full
)

$Project = "E:\Proyectos Unity\DD 40k"
$dir = Join-Path $Project "PlaytestLogs"

$f = Get-ChildItem (Join-Path $dir "$Kind`_*.log") -ErrorAction SilentlyContinue |
     Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $f) { Write-Error "No hay ningun $Kind`_*.log en $dir"; exit 1 }

Write-Host "== $($f.Name)  ($($f.LastWriteTime))" -ForegroundColor Cyan

if ($Kind -eq 'verify') {
    $res = Select-String -Path $f.FullName -Encoding UTF8 -Pattern '^RESULTADO:' | Select-Object -First 1
    if ($res) { Write-Host $res.Line.Trim() -ForegroundColor Green }

    $fallos = Select-String -Path $f.FullName -Encoding UTF8 -Pattern 'FALLO'
    if ($fallos) {
        Write-Host "`n--- ASSERTS EN FALLO ($($fallos.Count)) ---" -ForegroundColor Red
        $fallos | ForEach-Object { Write-Host ("  " + $_.Line.Trim()) }
    } else {
        Write-Host "Sin asserts en fallo."
    }
    if ($Full) { Get-Content $f.FullName -Encoding UTF8 }
}
else {
    # El informe del sim = cabecera de agregados + "LOG DE EJEMPLO" de UNA campana.
    # Los agregados son lo que vale; la muestra es anecdota (ver .claude/skills/analizar-playtest).
    $all = Get-Content $f.FullName -Encoding UTF8
    $cut = ($all | Select-String -Pattern 'LOG DE EJEMPLO' | Select-Object -First 1).LineNumber
    $head = if ($cut) { $all[0..($cut - 2)] } else { $all }
    $head | ForEach-Object { Write-Host $_ }

    $viol = $head | Where-Object { $_ -match 'VIOLACIONES DE INVARIANTES' }
    if ($viol -and $viol -notmatch ':\s*0\b') {
        Write-Host "`n>>> HAY VIOLACIONES DE INVARIANTES. Esto es un bug, no balance. <<<" -ForegroundColor Red
    }
    if ($Full -and $cut) {
        Write-Host "`n--- LOG DE EJEMPLO (1 campana, anecdotico) ---" -ForegroundColor DarkGray
        $all[($cut - 1)..($all.Count - 1)] | ForEach-Object { Write-Host $_ }
    }
}
