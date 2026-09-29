#requires -Version 5.1
<#
  check-terms.ps1 - guardia de nomenclatura de LA VIGILIA (hook PostToolUse).

  OJO: este fichero es ASCII PURO a proposito. PowerShell 5.1 lee un .ps1 sin
  BOM como ANSI, y cualquier tilde o simbolo raro rompe el parser.

  CLAUDE.md seccion 0 es regla absoluta: NUNCA un termino de Trench Crusade, de
  Games Workshop ni del skin viejo ("EL SOLAR") en texto de pantalla. Hasta
  ahora eso se comprobaba a mano - y se escapo (Assets/Purga/README.md sigue
  titulado "EL SOLAR"). Este hook lo comprueba tras cada Edit/Write.

  Alcance deliberadamente estrecho para no dar falsos positivos:
    * Solo ficheros bajo Assets\  (.cs y .md).
    * En .cs solo se miran los LITERALES DE CADENA -> identificadores como
      warpDanger o Library.Promethium() no se tocan (los ids van en ingles
      generico y se quedan).
    * NO se miran docs\, CLAUDE.md ni .claude\: ahi los terminos viejos son
      legitimos (la seccion 7 es literalmente la tabla de traduccion).

  Salida: exit 2 + stderr -> Claude ve el aviso y lo corrige.
#>

$ErrorActionPreference = 'Stop'

try {
    $raw = [Console]::In.ReadToEnd()
    if ([string]::IsNullOrWhiteSpace($raw)) { exit 0 }
    $payload = $raw | ConvertFrom-Json

    $path = $payload.tool_input.file_path
    if ([string]::IsNullOrWhiteSpace($path)) { exit 0 }
    if (-not (Test-Path -LiteralPath $path)) { exit 0 }

    $norm = ($path -replace '/', '\')

    # Fuera de alcance: docs de diseno, CLAUDE.md y la propia config de Claude.
    if ($norm -match '\\docs\\' -or $norm -match '\\\.claude\\' -or $norm -match 'CLAUDE\.md$') { exit 0 }
    # Solo lo que llega a pantalla: codigo de juego y readmes del paquete.
    if ($norm -notmatch '\\Assets\\') { exit 0 }
    if ($norm -notmatch '\.(cs|md)$') { exit 0 }

    # Terminos prohibidos en texto visible. Clave = regex, valor = con que sustituir.
    $banned = [ordered]@{
        'EL SOLAR'                  = 'LA VIGILIA DE HIERRO (skin viejo)'
        'SOLAR_biblia'              = 'VIGILIA_biblia_del_mundo.md'
        'Trench\s*Crusade'          = '(propiedad ajena: no nombrarla)'
        'Genestealer'               = 'Descendido'
        'Purestrain'                = 'Verdugo'
        'Tyranid|Tiranido|Ti.anido' = 'la Marea del Foso'
        'Inquisidor|Inquisici'      = 'Comandante'
        'Emperador'                 = 'la Llama'
        'Warp'                      = 'el Foso'
        'Ogryn'                     = 'Acorazado'
        'Astartes|Adeptus|Imperium' = '(termino de GW)'
        'Exterminatus'              = 'el Bombardeo'
        'Reclusiam'                 = 'la Capilla'
        'Patriarca'                 = 'Confesor Rojo'
        'Lictor'                    = 'Desollador'
        'Vharsis'                   = 'Reducto de Vared'
        'Promethium|Promecio'       = 'fuego de trinchera'
        'Servocr'                   = 'el Farol'
        '\btronos\b'                = 'la paga'
    }

    # Claves de asset legitimas (el id se queda en ingles generico).
    # Coincidencia EXACTA del literal completo: "Servocraneo" con tilde SI se marca.
    $allowLiterals = @('"Promethium"', '"Servocraneo"')

    $lines = Get-Content -LiteralPath $path -Encoding UTF8
    $isCs  = $norm -match '\.cs$'
    $hits  = New-Object System.Collections.Generic.List[string]

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]

        # En C# solo interesa lo que va entre comillas (texto de pantalla / logs).
        $targets = if ($isCs) {
            # Primero vaciar los huecos de interpolacion {...} de las cadenas $"...":
            # ahi dentro hay IDENTIFICADORES (ItemKind.Promethium, Campaign.ReclusiamSlots)
            # que son legitimos - lo visible es el texto, no el simbolo. Se conservan las
            # subcadenas entrecomilladas del hueco (ternarios que si pintan texto).
            $clean = [regex]::Replace($line, '\{[^{}]*\}', {
                param($m)
                $inner = [regex]::Matches($m.Value, '"(?:[^"\\\r\n]|\\.)*"') |
                         ForEach-Object { $_.Value }
                if ($inner) { ' ' + ($inner -join ' ') + ' ' } else { ' ' }
            })
            [regex]::Matches($clean, '"(?:[^"\\\r\n]|\\.)*"') |
                Where-Object { $allowLiterals -notcontains $_.Value } |
                ForEach-Object { $_.Value }
        } else { @($line) }

        foreach ($t in $targets) {
            foreach ($pat in $banned.Keys) {
                if ($t -match $pat) {
                    $hits.Add(("  {0}:{1}  '{2}'  ->  usa: {3}" -f (Split-Path $path -Leaf), ($i + 1), $Matches[0], $banned[$pat]))
                    break
                }
            }
        }
    }

    if ($hits.Count -gt 0) {
        $msg = New-Object System.Collections.Generic.List[string]
        $msg.Add("NOMENCLATURA (CLAUDE.md seccion 0): termino prohibido en texto visible de $path")
        foreach ($h in ($hits | Select-Object -First 20)) { $msg.Add($h) }
        $msg.Add("")
        $msg.Add("Regla: ningun termino de Trench Crusade, de Games Workshop ni del skin viejo EL SOLAR")
        $msg.Add("puede aparecer en pantalla. Traduce con la tabla de CLAUDE.md seccion 7. Los")
        $msg.Add("IDENTIFICADORES de codigo en ingles generico se quedan: esto solo aplica a cadenas visibles.")
        [Console]::Error.WriteLine([string]::Join([Environment]::NewLine, $msg))
        exit 2
    }

    exit 0
}
catch {
    # Un hook roto no puede bloquear el trabajo: falla en silencio.
    exit 0
}
