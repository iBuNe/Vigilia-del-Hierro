# LA VIGILIA DE HIERRO — Guía de reskin del código existente (primera tarea para Claude Code)

Objetivo: aplicar el reskin de textos a la nomenclatura **LA VIGILIA DE HIERRO** SIN cambiar ninguna mecánica ni número. Solo cambian cadenas visibles al jugador y, opcionalmente, se separa `id` de `displayName`. Es la tarea de arranque: pequeña, segura, verificable.

## Regla de oro
- **Lógica, stats, fórmulas, flujo: NO se tocan.**
- **`displayName`, logs (`Log(...)`), textos de botones IMGUI, mensajes: SÍ**, a nombres LA VIGILIA.
- Identificadores de código (métodos, clases, variables, claves de enum): se quedan en inglés genérico. No renombrar el método `Veterano()` → cámbiese solo el `displayName` que produce. (Opcional: renombrar métodos de fábrica a inglés `Veteran()`, `Priest()`… actualizando sus llamadas en `CombatBootstrap.SetupCombat()`.)
- Nombres propios sin tildes ni símbolos.

## Cambios en `Library.cs` (solo cadenas `displayName`)
| Actual | Nuevo (LA VIGILIA) |
|---|---|
| "Veterano" | "Fusilero" |
| "Sacerdote" | "Capellán" |
| "Cultista" | "Renegado" |
| "Neófito" | "Renegado con escopeta" |
| "Iniciado Susurrante" | "Predicador de la Herida" |
| "Aberrante" | "el Tocado deforme" |
| "Descarga de lasgun" | "Descarga de fusil" |
| "Fijar bayonetas" | "Calar la bayoneta" |
| "Estocada de bayoneta" | "Estocada" |
| "Fuego de supresión" | "Fuego de contención" |
| "Granada frag" | "Granada de trinchera" |
| "¡Por Cadia!" | "¡Aguantad la línea!" |
| "Cuerpo a tierra" | "Cuerpo a tierra" (se queda) |
| "Eviscerador" | "Pala de zapa" |
| "Sermón de la Llama" | "Sermón de la Llama" (se queda: encaja) |
| "Absolución" | "Absolución" (se queda) |
| "Ungüentos y rezos" | "Vendas y rezos" |
| "¡Arded, herejes!" | "¡Fuego de trinchera!" |
| "Letanía del Odio" | "Letanía del Odio" (se queda) |
| "Cántico de la Mano" | "Prédica de la Herida" |
| "Cuchillo" | "Cuchillo de zanja" |
| "Pistola auto" | "Pistola" |
| "Escopeta" | "Escopeta" (se queda) |
| "Maza minera" | "Garrote de hueso" |

## Cambios en `CombatBootstrap.cs` (logs y UI)
- "Corrupción" → "Mancha" en todos los `Log(...)` y etiquetas (`corr` en `UnitBox`, "Corr:" → "Mancha:").
- "Cohesión enemiga" → "Moral del culto" (la variable interna puede seguir siendo `enemyCohesion`).
- "PURGA — Prototipo de combate" → "LA VIGILIA DE HIERRO — Prototipo de combate".
- "El Emperador protege." → "La Llama aún arde." ; "El Nido reclama sus cuerpos." → "El Foso reclama sus cuerpos."
- "fija bayonetas" → "cala la bayoneta"; "modo melé" se queda; "[BAYONETA]" se queda.
- "HUYE despavorido" se queda (encaja con perder la Moral).

## Terminología de barras y mecánicas (fijar ya para no re-tocar en Hito 2+)
- Corrupción/estrés → **"Mancha"** (0–200). Fe → **"Llama"** (0–10).
- Prueba de Alma → **"Prueba de Fe"**. Aflicción/Virtud → **"Trauma"** / **"Temple"**.
- Cohesión enemiga → **"Moral"**. Ejecución → **"fusilar"**.
- Antorcha/luz → **"la Bengala"**. Campamento → **"el Refugio"**.
- Moneda → **"la paga"**. Gas (estado) → **"Gas"** (= Toxina + −precisión).

## Mejora recomendada (aprobar antes): separar id de displayName
Añadir a `UnitDef` y `AbilityDef` un campo `id` (string, inglés) además de `displayName` (español). Rellenar `id` con el nombre inglés genérico (`Veteran`, `Priest`, `Cultist`, `Neophyte`, `Whisperer`, `Aberrant`, y cada habilidad). Beneficio: guardado, bestiario (el Manual de Campo) y debug usan `id` estable; la pantalla usa `displayName`. No romper la API de `Library.New(...)`: añadir parámetro opcional `id`.

## Verificación
Tras el reskin: arrancar y comprobar que en pantalla no aparece ningún término de la columna "Actual" ni de Warhammer/Trench Crusade/GW. La partida debe comportarse EXACTAMENTE igual (mismos números, mismas rondas). El reskin es puramente cosmético.

*Cuento los que salen. Cuento los que vuelven. El Foso se queda con la diferencia.*
