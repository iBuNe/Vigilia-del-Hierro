# Desarrollo y verificación

## Datos de juego

`Assets/Purga/Scripts/Library.cs` es la única fuente editable de unidades,
objetos, consumibles y equipo. Los archivos de `Assets/Purga/Resources/Purga/`
son artefactos generados que se versionan para que el runtime y CI tengan datos
deterministas. No se editan en el Inspector.

Después de modificar `Library.cs` o una definición de datos, en Unity ejecuta:

`Purga > Datos > Regenerar assets desde Library (sobrescribe)`

El comando borra y reconstruye esos assets a propósito. La opción de crear solo
los que faltan es una recuperación de proyecto, no el flujo normal.

## Verificación local

Con Unity cerrado, PowerShell puede ejecutar los asserts deterministas así:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\.claude\scripts\unity-run.ps1 `
  -Method Purga.EditorTools.HitoVerification.VerifyBatch -LogName verify
```

El informe se guarda en `PlaytestLogs/`. Para evaluar balance se puede lanzar el
simulador headless con la semilla reproducible descrita en
`.claude/skills/verificar/SKILL.md`.

## CI

GitHub Actions ejecuta la misma batería de assertions en cada pull request y
push a `main`, mediante `.github/workflows/verify.yml`. Antes del primer uso,
configura el secreto de repositorio `UNITY_LICENSE` con una licencia Unity
válida y apta para uso en CI. Los informes se publican como artefacto del job.
