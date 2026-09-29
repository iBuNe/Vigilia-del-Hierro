---
name: verificar
description: Regenera los assets ScriptableObject, corre los asserts de hito y el simulador headless de campañas en Unity batchmode, y resume el resultado (N/N asserts, win-rate, violaciones de invariantes). Úsala tras tocar Library.cs, CombatBootstrap.cs o cualquier dato del juego.
disable-model-invocation: true
---

# /verificar — el pipeline de LA VIGILIA

Cierra el bucle de trabajo del proyecto: **datos → asserts → simulación → veredicto**.
Es la única forma fiable de saber si un cambio rompió algo, porque no hay tests unitarios
ni CI: la verdad vive en `HitoVerification` (asserts deterministas) y en `CombatSimulator`
(200 campañas headless con IA tonta).

## Argumentos

- sin argumentos → pipeline completo (regen + verify + sim 200)
- `rapido` → regen + verify, **sin** sim (el sim tarda varios minutos)
- `sim <N>` → solo el simulador con N campañas (p. ej. `/verificar sim 50`)
- `verify` → solo los asserts

## Procedimiento

### 0. Comprobación previa

Si `Temp\UnityLockfile` existe, el proyecto está abierto en el Editor y batchmode
fallará. Avisa al usuario y para: **no intentes matar Unity tú**.

### 1. Regenerar los assets de datos

Solo es necesario si se tocó `Library.cs`, `GameData.cs`, `DataAssetGenerator.cs` o
cualquier `*Def.cs`. Si el cambio fue solo de lógica en `CombatBootstrap.cs`, sáltatelo
(ahorra ~1 minuto).

```powershell
& "E:\Proyectos Unity\DD 40k\.claude\scripts\unity-run.ps1" -Method Purga.EditorTools.DataAssetGenerator.RegenerateAll -LogName regen
```

Espera **71 assets** (36 unidades + 12 ítems + 6 consumibles + 13 gear + subassets de
habilidades). Si el número baja, se perdió una entrada en `DataAssetGenerator.UnitEntries`.

> `RegenerateAll` **borra ediciones hechas a mano en el Inspector**. Es lo correcto en este
> proyecto (la fuente de verdad es `Library.cs`), pero dilo si el usuario había tuneado algo a mano.

### 2. Asserts de hito

```powershell
& "E:\Proyectos Unity\DD 40k\.claude\scripts\unity-run.ps1" -Method Purga.EditorTools.HitoVerification.VerifyBatch -LogName verify
& "E:\Proyectos Unity\DD 40k\.claude\scripts\unity-report.ps1" -Kind verify
```

El informe empieza con `RESULTADO: N/N comprobaciones superadas`. **Cualquier `✗ FALLO` es
una regresión**: repórtala con el nombre del assert antes de seguir.

### 3. Simulador de campañas

```powershell
& "E:\Proyectos Unity\DD 40k\.claude\scripts\unity-run.ps1" -Method Purga.EditorTools.CombatSimulator.RunBatch -LogName sim -ExtraArgs "-simCount 200 -simSeed 20260727"
& "E:\Proyectos Unity\DD 40k\.claude\scripts\unity-report.ps1" -Kind sim
```

Flags: `-simCount N` · `-simWeeks W` · `-simSeed S`. La semilla base fija hace el lote
**reproducible**: usa siempre `20260727` para comparar contra corridas anteriores, y cámbiala
solo si quieres un lote distinto (también repetible).

### 4. Veredicto

Reporta exactamente tres cosas, en este orden:

| Señal | Qué significa |
|---|---|
| `RESULTADO: N/N` | ✅ o ❌ **duro**. Un fallo = regresión, hay que arreglarlo. |
| `VIOLACIONES DE INVARIANTES: 0` | ❌ **duro**. Distinto de 0 = bug de estado, no balance. |
| `% de éxito` de expediciones | 📈 **blando**. Es un *suelo pesimista*: la IA del sim no usa consumibles, ni acciones menores, ni disciplina de luz. Compáralo con la corrida anterior; no lo trates como dificultad real. |

Compara el % contra el que registre `CLAUDE.md` §4 para el último hito cerrado. Una caída
grande tras añadir contenido suele ser dificultad legítima (más horror en el pool), no un bug
— dilo, pero no "arregles" el balance sin que el usuario lo pida.

## Reglas de invocación de Unity (no las reinventes)

Están encapsuladas en `.claude/scripts/unity-run.ps1`. Si alguna vez lo llamas a mano:

- `&` **no bloquea** sobre `Unity.exe` en PS 5.1 → usa `Start-Process -Wait -PassThru`.
- Los argumentos van como **UN solo string** (la ruta del proyecto lleva espacios).
- Lee los logs con **`-Encoding UTF8`** o el castellano sale como mojibake.
