#requires -Version 5.1
<#
  unity-run.ps1 - UNA llamada bloqueante a Unity en batchmode contra LA VIGILIA.
  (Fichero en ASCII PURO: PS 5.1 lee un .ps1 sin BOM como ANSI y las tildes rompen el parser.)

  Por que existe este script (cosas aprendidas a base de golpes):
    * En PowerShell 5.1 el operador de llamada (&) NO bloquea sobre Unity.exe:
      el comando siguiente lee un log a medio escribir. Siempre Start-Process.
    * La ruta del proyecto lleva espacios -> los argumentos van como UN solo
      string, no como array (Start-Process lo partiria por los espacios).
    * Unity escribe el log en UTF-8; Get-Content de PS 5.1 usa ANSI por defecto
      y convierte el castellano en mojibake. Siempre -Encoding UTF8 al leer.
    * Batchmode no puede abrir un proyecto que ya esta abierto en el Editor.

  Uso:
    .\.claude\scripts\unity-run.ps1 -Method Purga.EditorTools.DataAssetGenerator.RegenerateAll -LogName regen
    .\.claude\scripts\unity-run.ps1 -Method Purga.EditorTools.HitoVerification.VerifyBatch     -LogName verify
    .\.claude\scripts\unity-run.ps1 -Method Purga.EditorTools.CombatSimulator.RunBatch         -LogName sim -ExtraArgs "-simCount 200 -simSeed 20260727"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Method,                  # p.ej. Purga.EditorTools.HitoVerification.VerifyBatch
    [string]$LogName = "",            # p.ej. verify  ->  <proyecto>\verify.log
    [string]$ExtraArgs = "",          # p.ej. "-simCount 50 -simSeed 20260727"
    [int]$TimeoutMinutes = 30
)

$ErrorActionPreference = 'Stop'

$Unity   = "C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Unity.exe"
$Project = "E:\Proyectos Unity\DD 40k"

if (-not (Test-Path $Unity)) {
    # Fallback: coge la version mas alta instalada en el Hub.
    $hub = "C:\Program Files\Unity\Hub\Editor"
    $cand = if (Test-Path $hub) {
        Get-ChildItem $hub -Directory | Sort-Object Name -Descending |
            ForEach-Object { Join-Path $_.FullName "Editor\Unity.exe" } |
            Where-Object { Test-Path $_ } | Select-Object -First 1
    }
    if ($cand) { $Unity = $cand; Write-Warning "Usando Unity de respaldo: $Unity" }
    else { Write-Error "No encuentro Unity.exe. Edita `$Unity en .claude\scripts\unity-run.ps1."; exit 1 }
}

$lock = Join-Path $Project "Temp\UnityLockfile"
if (Test-Path $lock) {
    Write-Warning "Temp\UnityLockfile existe: parece que el proyecto esta ABIERTO en el Editor. Batchmode va a fallar: cierra Unity."
}

if ([string]::IsNullOrWhiteSpace($LogName)) { $LogName = ($Method -split '\.')[-1] }
$logPath = Join-Path $Project "$LogName.log"
if (Test-Path $logPath) { Remove-Item $logPath -Force }

# UN solo string de argumentos (la ruta lleva espacios).
$argString = "-batchmode -quit -projectPath `"$Project`" -executeMethod $Method -logFile `"$logPath`""
if ($ExtraArgs) { $argString = "$argString $ExtraArgs" }

Write-Host "> Unity $Method" -ForegroundColor Cyan
$sw = [Diagnostics.Stopwatch]::StartNew()

$p = Start-Process -FilePath $Unity -ArgumentList $argString -PassThru -NoNewWindow
if (-not $p.WaitForExit($TimeoutMinutes * 60 * 1000)) {
    Write-Warning "TIMEOUT tras $TimeoutMinutes min: matando Unity."
    try { $p.Kill() } catch { }
    exit 124
}
$sw.Stop()
$code = $p.ExitCode

Write-Host ("  exit {0} en {1:n0}s  ->  {2}" -f $code, $sw.Elapsed.TotalSeconds, $logPath)

# Errores de compilacion / excepciones: sacarlos a la superficie, no enterrarlos en 30k lineas.
if (Test-Path $logPath) {
    $bad = Select-String -Path $logPath -Encoding UTF8 -Pattern 'error CS\d+|Unhandled Exception|executeMethod method .* could not be found|Compilation failed' |
           Select-Object -First 25
    if ($bad) {
        Write-Host "`n--- PROBLEMAS EN EL LOG DE UNITY ---" -ForegroundColor Red
        $bad | ForEach-Object { Write-Host ("  " + $_.Line.Trim()) }
    }
}

exit $code
