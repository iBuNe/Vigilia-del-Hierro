# Plantillas de `UnitDef` por categoría

Copia la plantilla de la familia, cambia números y habilidades. El `id` (nombre de la
función) va en **inglés genérico**; el `displayName` en español LA VIGILIA.

---

## §9.2 Carne del Foso — abominación

```csharp
/// <summary>Nombre: una línea de qué hace y por qué da miedo.</summary>
public static UnitDef Skinner()
{
    return UnitDef.New("Desollador", u =>
    {
        u.team = Team.Enemies;
        u.maxHP = 34; u.armor = 1; u.dodge = 12; u.speed = 7; u.threat = 6;
        u.countsForCohesion = false;                    // sin Moral: no se quiebra ni huye
        u.threatChannel = ThreatChannel.Foso;           // caza en la OSCURIDAD
        u.weakToFire = true;                            // fuego x1.5, Quemadura x2
        u.abilities = new List<AbilityDef>
        {
            AbilityDef.New("Arrancar de la formación", a => {
                a.usableFrom = new[]{1,2,3,4}; a.targetPos = new[]{1,2,3,4};
                a.accuracy = 80; a.dmgMin = 7; a.dmgMax = 12; a.critPct = 8;
                a.corruptionDelta = 6;
            }),
        };
    });
}
```

## §9.3 Descendido — demonio

```csharp
u.team = Team.Enemies;
u.countsForCohesion = false;
u.threatChannel = ThreatChannel.Foso;
u.isDescendido = true;          // +5 Mancha al grupo solo por entrar en combate
```

## §9.4 Fauna — bestia neutral de la Tierra de Nadie

```csharp
u.team = Team.Enemies;
u.countsForCohesion = false;
u.threatChannel = ThreatChannel.Human;   // caza lo que VE: lo iluminado
u.isFauna = true;                        // OJO: sin isDescendido -> sin el +5
```

## §9.1 Humano de la Congregación

```csharp
u.team = Team.Enemies;
u.countsForCohesion = true;              // ÚNICA familia con Moral
u.threatChannel = ThreatChannel.Human;
```

## Mini-boss (élite)

```csharp
u.isElite = true;
u.resStun = 50; u.resMove = 60;          // los élites aguantan el control
// + su mecánica firma en un flag propio de UnitDef (isSapper, extraActionIfUnhit, ...)
```

## Jefe de sector

```csharp
u.isBoss = true;
u.ownCohesion = 45;                      // barra de Voluntad propia: doble vía de derrota
u.phaseTwoAtPct = 50;                    // % de PV que dispara la fase 2
u.resStun = 60; u.resMove = 70;
```

---

# Tabla de flags mecánicos de `UnitDef`

Antes de inventar un flag nuevo, mira si ya existe uno que sirva.

| Flag | Criatura de referencia | Efecto |
|---|---|---|
| `devoursCorpses` / `corpseHeal` | Devoracadáveres | Come un cadáver del campo y se cura |
| `gasOnHit` / `gasOnDeath` | Supurante | Gas al herirlo / al morir |
| `splitsInto` | Cosido | A media vida se parte en dos de esa clave |
| `extraActionIfUnhit` | Verdugo | Doble acción si nada lo tocó |
| `isSapper` | Zapador Injertado | Derrumbe telegrafiado sobre la vanguardia |
| `silencesMiracles` | Corista | Bloquea los `isFaithAbility` del grupo |
| `revivesDescendidos` | Guardián de la Herida | Rehace una vez a un Descendido caído |
| `weakToArea` | Plaga de ratas | El daño de área la destroza (x1.5) |
| `bleedsAttackerOnMelee` | Bestia del Alambre | El atacante cuerpo a cuerpo sangra |
| `stealsSupply` | Carroñero mutado | Roba una carga y huye con el botín |
| `dropsMoraleOnDeath` | Cabecilla | Su muerte desploma la Moral del culto |
| `isBomber` | Bombardero | Ataque de área telegrafiado (prime → detonar) |
| `isVip` | Rescatado | Escolta no controlable; su muerte falla el Rescate |
| `summonType` / `summonPerRound` | Matriarca | Invoca esa clave cada ronda |

**Flag nuevo = trabajo en tres sitios**: el campo en `UnitDef.cs`, el punto de lectura en
`CombatBootstrap.cs`, y un assert end-to-end en `HitoVerification.cs`. Un flag que nadie lee
compila perfectamente y no hace nada.

# Resistencias y presupuesto

- `resBleed`, `resToxin`, `resStun`, `resMove`, `resDebuff`: **%** restado a la probabilidad
  de aplicar el estado. Chusma 0–15, élites 50–60, jefes 60–70.
- `threat`: coste en el presupuesto de amenaza del encuentro. Chusma 1–2, tropa 3–4,
  élite 6–8. Es el knob que mantiene la dificultad acotada — respétalo.
