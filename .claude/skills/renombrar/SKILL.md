---
name: renombrar
description: Cambia con seguridad un displayName de LA VIGILIA (unidad, habilidad, ítem, equipo, edificio) barriendo todas las comparaciones por texto acopladas antes de regenerar assets y verificar. Úsala siempre en vez de un buscar-y-reemplazar a pelo.
disable-model-invocation: true
---

# /renombrar — cambiar un displayName sin romper la lógica

**Uso:** `/renombrar "Nombre viejo" "Nombre nuevo"`

El proyecto separa `id` (inglés genérico, permanente) de `displayName` (español, LA VIGILIA).
El problema: hay **~38 sitios donde la lógica compara contra el texto en español**, así que
renombrar a pelo rompe asserts y métricas **en silencio** (los contadores no fallan: se van a
cero). `CLAUDE.md` §0 avisa de esto explícitamente.

## 1. Validar el nombre nuevo contra §0

Antes de tocar nada, comprueba que el nombre propuesto cumple la nomenclatura:

- [ ] **Sin artículo** si es un **personaje** (unidad enemiga, clase/héroe, NPC):
      `Bombardero`, no "el Bombardero". `Autómata`, no "el Autómata".
- [ ] **Con artículo** si es **facción/orden, lugar, mecánica, edificio o ítem/gadget**:
      la Vigilia, el Foso, la Mancha, la Bengala, la Capilla, el Escapulario, el Farol.
- [ ] Adjetivo antepuesto cuando cambia el sentido: `Falso Capellán` (impostor), no "Capellán Falso".
- [ ] Registro WWI (Fusilero, Capellán, Zapador, Oficial) + nombres propios inventados
      **sin tildes ni símbolos**.
- [ ] **Cero** términos de Trench Crusade ni de Games Workshop.

Si el nombre propuesto incumple algo, dilo y propón alternativa **antes** de editar.

## 2. Cambiar la fuente

El `displayName` vive en la fábrica de `Library.cs`:

```csharp
public static UnitDef Houndof()          // <- el id NO se toca
{
    return UnitDef.New("Sabueso del Foso", u => { ... });   // <- esto es lo que cambia
}
```

**Nunca cambies el `id`/clave** (`"Houndof"`): rompería `GameData.Unit()`,
`DataAssetGenerator.UnitEntries` y las rutas de los `.asset`.

## 3. Barrer los acoplamientos (el paso que se olvida)

Busca el texto **viejo** en todo `Assets/Purga/Scripts/` y arregla cada acierto:

```
Grep: "Nombre viejo"   en Assets/Purga/Scripts
```

Sitios conocidos, por orden de riesgo:

| Fichero | Patrón | Qué pasa si no lo actualizas |
|---|---|---|
| `Editor/CombatSimulator.cs` | `CountLines("...")` | La métrica del informe se va a **0 en silencio**. Nadie se entera. |
| `Editor/HitoVerification.cs` | `displayName ==`, `Contains("...")` (~26 sitios) | Assert en `✗ FALLO`. Al menos avisa. |
| `CombatBootstrap.cs` | `displayName ==`, `Contains("...")` (~12 sitios) | **Lógica de combate rota** (targeting, telegrafíos, casos especiales). |
| `Campaign.cs` | `bestiaryEncountered.Contains(...)` | Entrada duplicada en el Manual de Campo. |
| Textos de `Log(...)` e IMGUI | cadena literal | Texto incoherente en pantalla. |

También revisa `docs/VIGILIA_biblia_del_mundo.md` (tabla maestra de nombres) y `CLAUDE.md`
§3/§4 si el nombre aparece ahí — la biblia manda en el detalle.

## 4. Regenerar y verificar

```powershell
& "E:\Proyectos Unity\DD 40k\.claude\scripts\unity-run.ps1" -Method Purga.EditorTools.DataAssetGenerator.RegenerateAll -LogName regen
& "E:\Proyectos Unity\DD 40k\.claude\scripts\unity-run.ps1" -Method Purga.EditorTools.HitoVerification.VerifyBatch -LogName verify
& "E:\Proyectos Unity\DD 40k\.claude\scripts\unity-report.ps1" -Kind verify
```

`RegenerateAll` es **obligatorio**: los `.asset` guardan el `displayName` viejo y son la
fuente de verdad en runtime (`GameData` solo cae a `Library.cs` si falta el asset).

## 5. Comprobación final anti-cero

Si tocaste algún `CountLines`, corre también el sim y verifica que **ese contador no está a 0**:

```powershell
& ".claude\scripts\unity-run.ps1" -Method Purga.EditorTools.CombatSimulator.RunBatch -LogName sim -ExtraArgs "-simCount 50"
& ".claude\scripts\unity-report.ps1" -Kind sim
```

Un contador que antes tenía miles de menciones y ahora marca 0 = te dejaste un acoplamiento.
