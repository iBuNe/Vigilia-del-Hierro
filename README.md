# La Vigilia de Hierro

Vertical slice de un RPG táctico por turnos para Unity, ambientado en una
Primera Guerra Mundial apocalíptica original. El jugador dirige una compañía
de la Vigilia de Hierro contra la Congregación de la Herida y el Foso.

El slice actual incluye combate por turnos, expediciones procedurales, Fortín,
equipo, infiltración, cuatro sectores, jefes y el final de campaña al sellar la
Herida.

## Requisitos

- Unity `6000.5.4f1`.
- Git LFS no es necesario actualmente.
- Una licencia Unity válida para ejecutar las verificaciones batch o CI.

## Arranque rápido

1. Clona el repositorio y ábrelo con la versión indicada de Unity.
2. Abre `Assets/Scenes/SampleScene.unity`.
3. Pulsa Play. La escena ya contiene el componente de arranque del prototipo.

La interfaz jugable actual usa IMGUI. Durante combate, `F9` permite previsualizar
la interfaz de UI Toolkit, que todavía no sustituye el flujo completo.

## Antes de modificar el proyecto

Lee [CLAUDE.md](CLAUDE.md). Es la guía de producto y de desarrollo: fija la
ambientación, la nomenclatura visible, decisiones de diseño cerradas, alcance,
convenciones y el roadmap. Sus reglas importantes son:

- El texto visible usa la nomenclatura de **La Vigilia de Hierro**; no se deben
  introducir referencias visibles a propiedad intelectual de terceros.
- Identificadores de código en inglés genérico; textos de juego en español.
- No cambiar una decisión marcada como cerrada sin validarla antes.
- Mantener la lógica separada del arte y de los datos visuales.

Para el detalle de mundo y diseño, consulta `docs/`:

- `VIGILIA_biblia_del_mundo.md`: mundo y nombres maestros.
- `purga_gdd_draft.md`: diseño global.
- `vharsis_vertical_slice.md`: números y contenido del vertical slice.
- `DEVELOPMENT.md`: contrato de datos y verificación local/CI.

## Datos de juego

`Assets/Purga/Scripts/Library.cs` es la **única fuente editable** de unidades,
objetos, consumibles y equipo. Los ScriptableObjects en
`Assets/Purga/Resources/Purga/` son artefactos generados y versionados que usa
el runtime; no deben editarse a mano en el Inspector.

Tras cambiar `Library.cs` o una definición de datos, ejecuta en Unity:

`Purga > Datos > Regenerar assets desde Library (sobrescribe)`

## Verificación

El proyecto mantiene asserts deterministas y un simulador headless. Con Unity
cerrado, ejecuta los asserts desde PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\.claude\scripts\unity-run.ps1 `
  -Method Purga.EditorTools.HitoVerification.VerifyBatch -LogName verify
```

La automatización de GitHub Actions está en
[`.github/workflows/verify.yml`](.github/workflows/verify.yml). Requiere el
secreto de repositorio `UNITY_LICENSE`.

## Estado del repositorio

No se versionan las carpetas generadas de Unity (`Library`, `Temp`, `Logs`,
`obj`, etc.) ni registros de playtest. Los assets generados de gameplay sí se
versionan para que el proyecto arranque y se verifique de forma reproducible.
