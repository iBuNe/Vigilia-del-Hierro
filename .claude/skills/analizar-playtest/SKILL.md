---
name: analizar-playtest
description: Lee e interpreta los informes de PlaytestLogs (sim_*.log del simulador de campañas, session_*.log de partidas reales) para responder qué está pasando en el juego — balance, si una mecánica dispara, dónde muere la gente.
---

# /analizar-playtest — leer los logs de LA VIGILIA

`PlaytestLogs/` es el **único canal de feedback** del proyecto (el MCP de Unity está
bloqueado en el plan gratuito). Hay dos familias:

| Fichero | Qué es |
|---|---|
| `sim_*.log` | Informe del `CombatSimulator`: agregados de 200 campañas + **un** log de ejemplo |
| `session_*.log` | Partida real jugada por el usuario, vía `PlaytestLog.cs` |
| `verify_*.log` | Asserts deterministas (eso es `/verificar`, no esto) |

Léelos siempre con `-Encoding UTF8` (PS 5.1 usa ANSI por defecto y destroza las tildes).
Atajo: `& ".claude\scripts\unity-report.ps1" -Kind sim`

## La regla que más se olvida

> El informe del sim son **agregados de 200 campañas** seguidos de un **`LOG DE EJEMPLO` de UNA sola campaña**.

Por eso:

- ❌ **Nunca** grepees el `LOG DE EJEMPLO` para comprobar si una mecánica dispara. Es una
  muestra de tamaño 1: que no aparezca no prueba nada, y que aparezca tampoco mide nada.
- ✅ Para comprobar que una mecánica dispara, **añade una línea de métrica al informe**:
  en `CombatSimulator.BuildReport` hay un helper `CountLines(needle)` que cuenta a lo largo
  de las 200 campañas. Añade tu contador ahí y vuelve a correr el sim.

```csharp
// CombatSimulator.BuildReport — patrón existente
$"HUMANOS §9.1 — bombardeos: {CountLines("DETONA")} · rally del Iluminado: {CountLines("inflama al culto")}",
```

`CountLines` casa **subcadenas de texto de juego en español**. Si cambias un `displayName`
o el texto de un `Log`, esos contadores se quedan a cero en silencio → ver `/renombrar`.

## Cómo leer los agregados

| Línea | Lectura |
|---|---|
| `VIOLACIONES DE INVARIANTES` | **Debe ser 0.** Distinto de 0 = bug de estado. Prioridad máxima. |
| `Stage 1 GANADO` / `Sector caído` | El suelo. Con IA tonta ganar es raro **por diseño**. |
| `% de éxito` de expediciones | La métrica de balance útil. Compárala contra la corrida anterior. |
| `Agentes muertos por campaña` | Rotación de tropa. La economía de la Mancha **no cierra a propósito** (§2): números altos son intencionados. |
| `Bestiario del Foso (menciones)` | Comprueba que el `HorrorTier` está metiendo Carne/Descendidos/Fauna en el pool. |
| `Refugios: 0` | Síntoma clásico de que la IA del sim no usa una mecánica — **no** de que esté rota. |

## El sesgo del simulador (dilo siempre)

La IA headless: **no** usa consumibles, **no** usa acciones menores, elige habilidades al
azar y solo tiene una disciplina de luz mínima (relanza bengalas de forma tonta). El informe
lo advierte al final. Por tanto:

- El `% de éxito` es un **suelo pesimista**, no la dificultad que siente un jugador.
- Una mecánica que exige juego fino (cazar al Foso metiéndose en la oscuridad, rotar roster,
  el Fortín) **abarata** el suelo del sim aunque mejore el juego real.
- Nunca recomiendes recalibrar dificultad basándote solo en el sim. Cruza con los
  `session_*.log`, que sí son de un humano.

## Analizando una sesión real (`session_*.log`)

Busca el arco de la partida, no eventos sueltos:
en qué sala/sector se torció, la Señal cuando llegó la emboscada, la luz en el combate que
perdió, si llegó a usar el Refugio, y qué Traumas salieron de la Prueba de Fe. Contrasta la
sensación del usuario contra lo que dice el log antes de proponer cambios de números.

## Al terminar

Responde: **qué dicen los números**, **qué es bug (invariantes/asserts) y qué es balance**, y
**qué métrica falta** para contestar bien la pregunta. Si falta una métrica, propón la línea
de `CountLines` concreta a añadir.
