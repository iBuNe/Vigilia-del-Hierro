---
name: nuevo-enemigo
description: Añade una criatura al bestiario de LA VIGILIA (Carne del Foso, Descendido, Fauna, humano de la Congregación, mini-boss o jefe de sector) tocando en orden los 5 puntos del pipeline data-driven — Library, GameData, DataAssetGenerator, encuentros y asserts — y verificando.
disable-model-invocation: true
---

# /nuevo-enemigo — dar de alta una criatura

**Uso:** `/nuevo-enemigo <Id> "<displayName>" [categoria]`
p. ej. `/nuevo-enemigo Skinner "Desollador" carne`

Añadir un enemigo toca **siempre los mismos 5 sitios**. Saltarse uno da fallos raros:
sin la entrada del generador el `.asset` no existe y `GameData` cae al fallback con un
warning; sin cablearlo al pool, el enemigo nunca aparece en una partida.

Consulta `references/plantilla.md` para las plantillas por categoría y la tabla de flags.

## Antes de escribir código

1. **El nombre**: aplica §0 — personaje ⇒ **sin artículo** (`Desollador`, no "el Desollador");
   sin tildes en nombres propios inventados; nada de Trench Crusade ni GW.
2. **La categoría** decide los flags y el canal de amenaza:

   | Categoría | Canal | Moral | Flags de familia |
   |---|---|---|---|
   | §9.1 Humanos (Congregación) | `Human` | **sí** (`countsForCohesion = true`) | — |
   | §9.2 Carne del Foso | `Foso` | no | `weakToFire = true` |
   | §9.3 Descendidos | `Foso` | no | `isDescendido = true` (⇒ **+5 Mancha al grupo** al entrar en combate) |
   | §9.4 Fauna | `Human` | no | `isFauna = true` (**sin** `isDescendido`) |

   Solo §9.1 tiene Moral. El canal es la mecánica central del **eje de la luz**: los humanos
   solo alcanzan a lo **iluminado**, la carne del Foso solo a lo **oscuro**.
3. **Los números**: calíbralos contra el marco base (~12–16 de daño enemigo por ronda a
   nivel 0). El `threat` es lo que gasta del presupuesto de amenaza del encuentro —
   mírate un vecino de su misma familia en `Library.cs` antes de inventar.
4. **La biblia manda**: si la criatura ya está descrita en
   `docs/VIGILIA_biblia_del_mundo.md` §9, **usa esos números y ese texto**, no los tuyos.

## Los 5 puntos, en orden

### 1. `Library.cs` — la fábrica
Coloca la función en el bloque de su categoría (los bloques están comentados:
`CARNE DEL FOSO (§9.2)`, `FAUNA (§9.4, la Tierra de Nadie, neutral)`, …).

### 2. `GameData.cs` — el `switch` de fallback
Añade `case "<Id>": return Library.<Id>();` en `GameData.Unit`. Es el respaldo para que
darle a Play siempre funcione aunque falte el `.asset`.

### 3. `Editor/DataAssetGenerator.cs` — `UnitEntries`
Añade `("<Id>", Library.<Id>),`. **Sin esto no se genera el `.asset`** y el juego arranca
con un warning en consola en vez de con tus datos.

### 4. Cablearlo a los encuentros
Un enemigo que no entra en ningún pool no existe. Según el caso:

- **Enemigo normal** → pool de `RollEncounter`, filtrado por
  `Expedition.HorrorTier(sector, phase)`: tier 2 mete Carne del Foso (Sector ≥2 **o** Crecida ≥4),
  tier 3 mete Descendidos + Fauna (Sector ≥3 **o** fase ≥6). Fase 1 debe seguir siendo
  **humanos puros** — hay un assert que lo comprueba.
- **Mini-boss** (`isElite = true`) → `MiniBossFor(sector)`.
- **Jefe de sector** (`isBoss`, `ownCohesion`, `phaseTwoAtPct`) → `BossMission(party, phase, sector)`
  y `FrontMap.BuildSector(n)`.
- Élites y especiales entran en el cap de **1 élite por combate** en los Sectores 1–2.

### 5. `Editor/HitoVerification.cs` — asserts
Añade tus `Check(...)` en la función de su hito (`CarneChecks`, `DescendidosChecks`,
`FaunaChecks`, `SectorBestiaryChecks`…). Cubre como mínimo:

- que el `.asset` existe y tiene los flags de familia correctos
  (`GameData.Unit("<Id>").isDescendido` etc.);
- **end-to-end de la mecánica firma**: no basta con comprobar que el bool está a `true` —
  monta un combate de prueba y comprueba el **efecto** (así se verificó el Cosido partiéndose,
  el silencio del Corista y la revivificación del Guardián).

Opcional pero recomendado: una línea de métrica en `CombatSimulator.BuildReport` vía
`CountLines("<displayName>")` para ver que aparece de verdad en las 200 campañas.

## Verificar

```powershell
& "E:\Proyectos Unity\DD 40k\.claude\scripts\unity-run.ps1" -Method Purga.EditorTools.DataAssetGenerator.RegenerateAll -LogName regen
& "E:\Proyectos Unity\DD 40k\.claude\scripts\unity-run.ps1" -Method Purga.EditorTools.HitoVerification.VerifyBatch -LogName verify
& "E:\Proyectos Unity\DD 40k\.claude\scripts\unity-report.ps1" -Kind verify
& "E:\Proyectos Unity\DD 40k\.claude\scripts\unity-run.ps1" -Method Purga.EditorTools.CombatSimulator.RunBatch -LogName sim
& "E:\Proyectos Unity\DD 40k\.claude\scripts\unity-report.ps1" -Kind sim
```

El contador de assets debe **subir en 1** y las violaciones de invariantes seguir en **0**.
Al terminar, añade la criatura al bestiario de `CLAUDE.md` §3 y a la biblia §9 si no estaba.
