using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Purga
{
    public class StatusInstance
    {
        public StatusKind kind;
        public int power;
        public int rounds;             // ticks down at the unit's own turn start
        public CombatUnit guardTarget; // only for Guard
        public StatusApply onExpire;   // applied when this status runs out (stim hangover)
    }

    public class CombatUnit
    {
        public const int MaxLevel = 6;

        public UnitDef def;
        public string unitName;
        public int level;   // 0..MaxLevel; +10% PV and +5 PRE per level, +1 damage every 2 levels
        public const int MaxGearRank = 3;
        public int weaponRank;  // Taller del Zapador: +1 damage & +5 accuracy per rank
        public int armorRank;   // Taller del Zapador: +1 armor per rank
        // -- el Barracón: equipment slots (gear MODIFIES on top of class base + Taller rank) --
        public GearDef weapon, armor, neck, ring;
        public IEnumerable<GearDef> Gear
        {
            get { if (weapon != null) yield return weapon;
                  if (armor  != null) yield return armor;
                  if (neck   != null) yield return neck;
                  if (ring   != null) yield return ring; }
        }
        int GearSum(System.Func<GearDef, int> f) => Gear.Sum(f);
        public int GearDamage => GearSum(g => g.dmgBonus);
        public int GearAcc    => GearSum(g => g.accBonus);
        public int GearDodge  => GearSum(g => g.dodgeBonus);
        public int GearArmor  => GearSum(g => g.armorBonus);
        public int GearSpeed  => GearSum(g => g.speedBonus);
        public int GearHP     => GearSum(g => g.hpBonus);
        public int GearMancha => GearSum(g => g.manchaResBonus);
        public int GearCrit   => GearSum(g => g.critBonus);
        /// <summary>Equipped item in a slot (null = empty).</summary>
        public GearDef GearIn(GearSlot s) => s == GearSlot.Weapon ? weapon
                                           : s == GearSlot.Armor  ? armor
                                           : s == GearSlot.Neck   ? neck : ring;
        public void SetGear(GearSlot s, GearDef g)
        {
            switch (s) { case GearSlot.Weapon: weapon = g; break; case GearSlot.Armor: armor = g; break;
                         case GearSlot.Neck: neck = g; break; case GearSlot.Ring: ring = g; break; }
        }
        public int hp;
        public int ammo;
        public int corruption;   // 0..200; Soul Trial at 100, se somete a la oscuridad (muerte) at 200
        public int faith;        // 0..10, heroes start at 3; each point = +3% corruption resist
        public SoulState soul = SoulState.None;
        /// Display name with the proper accent (enum identifiers stay accent-free).
        public static string SoulName(SoulState s) => s == SoulState.Apostata ? "Apóstata" : s.ToString();
        public bool soulTested;  // one Soul Trial per combat
        public bool meleeMode;
        public int speedPenaltyNextRound; // Eviscerador
        public List<StatusInstance> statuses = new List<StatusInstance>();
        public Dictionary<AbilityDef, int> usesLeft = new Dictionary<AbilityDef, int>();
        public List<ConsumableDef> consumables = new List<ConsumableDef>(); // max 2, single-use
        public int endCombatCorruptionDebt; // Coraje embotellado: paid when the combat ends
        public int stunResistBonus;         // +30 each time a Stun wears off (anti-stunlock, both sides)
        public bool atDeathsDoor;           // hero at 0 HP: every hit forces a death check; 1 HP of healing exits
        public bool retreated;              // left the current combat via group retreat
        public bool hasFled;                // enemy: already fled once this expedition — never flees again
        public int bossPhase = 1;           // bosses: current phase (1, then 2 below phaseTwoAtPct HP)
        public bool bombPrimed;             // el Bombardero: armed a charge last turn; detonates this turn
        public CombatUnit bondPartner;      // el Comedor: a bonded comrade — grief hits hard if they fall
        public int zone;                    // light axis (Fase posiciones): 0=Galería(rear/dark) … 3=Tierra de Nadie(front/lit; flare over the front)
        // -- Hito 5: CARNE DEL FOSO runtime state --
        public bool hasSplit;               // Cosido: already burst into halves (only once)
        public bool revivedOnce;            // Descendido: already raised by the Guardián de la Herida (only once)
        public bool tookDamageSinceTurn;    // Verdugo: hurt since its last turn → no bonus action
        public bool averiado;               // el Autómata (Hito 6): broke at 0 HP — a KO, not a casualty; repaired at the Taller
        public bool isInfiltrator;          // Hito 6: a cult agent hiding in the roster. Tells (never flagged): slow Mancha, never gains Llama, refuses el Comedor
        public bool grabbedByFlayer;        // Hito 7: el Desollador has this hero pinned out of formation, flaying them until it falls
        public int treatmentRaidsLeft;      // §2 indisponibilidad: incursiones this soldier is still benched in la Capilla/Enfermería (0 = available)

        public bool Alive => hp > 0 || atDeathsDoor;
        public Team TeamOf => def.team;

        /// <summary>Max HP scaled by level (+10% per level) plus equipped gear.</summary>
        public int EffMaxHP => Mathf.RoundToInt(def.maxHP * (1f + 0.10f * level)) + GearHP;
        /// <summary>Flat damage bonus from level (+1 every 2 levels).</summary>
        public int LevelDamage => level / 2;

        public CombatUnit(UnitDef d, string nameSuffix = "")
        {
            def = d;
            unitName = d.displayName + nameSuffix;
            hp = EffMaxHP;
            ammo = d.maxAmmo;
            faith = (d.team == Team.Heroes && !d.isAutomaton) ? 3 : 0; // el Autómata has no soul to kindle: la Llama starts (and stays) 0
            foreach (var ab in d.abilities)
                if (ab.usesPerCombat > 0) usesLeft[ab] = ab.usesPerCombat;
        }

        /// <summary>Gain a level (capped): raises Max HP and heals the gained margin.</summary>
        public bool LevelUp()
        {
            if (level >= MaxLevel) return false;
            int before = EffMaxHP;
            level++;
            hp += EffMaxHP - before; // the new vitality is granted, not left as missing HP
            return true;
        }

        /// <summary>
        /// Between-combat reset ("the corridor"): DoTs travel with the group
        /// (design), everything combat-scoped clears. A new Soul Trial can only
        /// trigger if the soul is currently intact.
        /// </summary>
        public void PrepareNextCombat()
        {
            statuses.RemoveAll(s => s.kind != StatusKind.Bleed && s.kind != StatusKind.Burn && s.kind != StatusKind.Toxin);
            stunResistBonus = 0;
            meleeMode = false;
            retreated = false;
            grabbedByFlayer = false; // combat-scoped: never carries into the next fight
            speedPenaltyNextRound = 0;
            // ammo is NOT refilled here: refills consume Cargador/Frasco supplies (CombatBootstrap.SetupCombat)
            soulTested = soul != SoulState.None;
            usesLeft.Clear();
            foreach (var ab in def.abilities)
                if (ab.usesPerCombat > 0) usesLeft[ab] = ab.usesPerCombat;
        }

        /// <summary>
        /// Apply damage under Death's Door rules (heroes only; enemies die at 0).
        /// At the Door the AMOUNT is irrelevant: any hit forces a death check.
        /// The caller logs the outcome (so the damage line prints first).
        /// </summary>
        public DamageOutcome TakeDamage(int dmg)
        {
            // el Autómata never bleeds out on Death's Door: at 0 HP it BREAKS (avería) — a KO, not a death.
            if (def.isAutomaton)
            {
                hp = Mathf.Max(0, hp - dmg);
                if (hp > 0) return DamageOutcome.Damaged;
                averiado = true;
                return DamageOutcome.Died; // "down": out of the fight; OnDeath treats it as an avería, not a casualty
            }
            if (def.team == Team.Heroes && atDeathsDoor)
            {
                if (Rng.Range(0, 100) < def.deathBlowPct)
                {
                    atDeathsDoor = false;
                    hp = 0;
                    return DamageOutcome.Died;
                }
                return DamageOutcome.DeathsDoorResisted;
            }

            hp = Mathf.Max(0, hp - dmg);
            if (hp > 0) return DamageOutcome.Damaged;
            if (def.team == Team.Heroes)
            {
                atDeathsDoor = true;
                return DamageOutcome.EnteredDeathsDoor;
            }
            return DamageOutcome.Died;
        }

        /// <summary>
        /// Clean up after an expedition: clear all combat-scoped state. A survivor
        /// who made it back off Death's Door is stabilized to at least 1 HP (the
        /// Enfermería heals the rest). HP, Corruption, Faith and Quebrantos persist.
        /// </summary>
        public void RestoreAtShip()
        {
            if (averiado) return; // a broken Autómata stays down until repaired at the Taller (not stabilized at the ship)
            statuses.Clear();
            meleeMode = false;
            retreated = false;
            grabbedByFlayer = false; // the flaying doesn't follow you home
            hasFled = false;
            stunResistBonus = 0;
            speedPenaltyNextRound = 0;
            endCombatCorruptionDebt = 0;
            if (atDeathsDoor || hp < 1) hp = Mathf.Max(1, hp);
            atDeathsDoor = false;
            soulTested = false; // a new expedition can trigger a fresh Soul Trial
        }

        public bool Has(StatusKind k) => statuses.Any(s => s.kind == k);

        public int StatusSum(StatusKind k) => statuses.Where(s => s.kind == k).Sum(s => s.power);

        /// <summary>Total DoT power (Bleed+Burn+Toxin) — the damage a lingering wound deals per tick.</summary>
        public int DotPower => StatusSum(StatusKind.Bleed) + StatusSum(StatusKind.Burn) + StatusSum(StatusKind.Toxin);

        /// <summary>Age out DoT one step (used out of combat: los DoT tictean al recorrer los pasillos, H5·3).</summary>
        public void TickDotDurations()
        {
            foreach (var s in statuses)
                if (s.kind == StatusKind.Bleed || s.kind == StatusKind.Burn || s.kind == StatusKind.Toxin)
                    s.rounds--;
            statuses.RemoveAll(s => (s.kind == StatusKind.Bleed || s.kind == StatusKind.Burn || s.kind == StatusKind.Toxin) && s.rounds <= 0);
        }

        public int EffAccuracyBonus =>
            StatusSum(StatusKind.AccBuff) - StatusSum(StatusKind.AccDebuff)
            - (soul == SoulState.Desesperado ? 10 : 0)
            + (soul == SoulState.Iluminado ? 10 : 0)
            + 5 * level        // +5 PRE per level
            + 5 * weaponRank   // +5 PRE per weapon rank (Taller del Zapador)
            + GearAcc;         // equipped gear (el Barracón)

        public int EffDodge
        {
            get
            {
                int d = def.dodge + GearDodge + StatusSum(StatusKind.DodgeBuff);
                if (Has(StatusKind.Defend)) d += 15;
                return d;
            }
        }

        public float DamageDealtMult => 1f + StatusSum(StatusKind.DmgBuffPct) / 100f;

        /// <summary>Effective corruption resistance: class + 3%/Faith ± consumable mods, 0–90.</summary>
        public int EffCorrResist => Mathf.Clamp(def.corrResist + 3 * faith + GearMancha + StatusSum(StatusKind.CorrResistMod), 0, 90);

        public float DamageTakenMult =>
            (Has(StatusKind.Defend) ? 0.8f : 1f)
            * (soul == SoulState.Inquebrantable ? 0.8f : 1f)
            * (1f + StatusSum(StatusKind.Marked) / 100f)
            * Mathf.Max(0.1f, 1f - StatusSum(StatusKind.DmgResistPct) / 100f);

        public int EffArmor => def.armor + armorRank + GearArmor + StatusSum(StatusKind.ArmorBuff);

        public void AddStatus(StatusKind kind, int power, int rounds, CombatUnit guardTarget = null, StatusApply onExpire = null)
        {
            // Non-DoT effects of the same kind AND power refresh their duration
            // instead of stacking (recasting Escudo must not compound to 90%
            // damage reduction). Different powers still combine (Premonición +10
            // dodge + Cuerpo a tierra +15), and DoTs always stack (design).
            bool isDot = kind == StatusKind.Bleed || kind == StatusKind.Burn || kind == StatusKind.Toxin;
            if (!isDot)
            {
                var existing = statuses.FirstOrDefault(s => s.kind == kind && s.power == power);
                if (existing != null)
                {
                    existing.rounds = Mathf.Max(existing.rounds, rounds);
                    if (guardTarget != null) existing.guardTarget = guardTarget;
                    if (onExpire != null) existing.onExpire = onExpire;
                    return;
                }
            }
            statuses.Add(new StatusInstance { kind = kind, power = power, rounds = rounds, guardTarget = guardTarget, onExpire = onExpire });
        }

        public void GainCorruption(int amount, System.Action<string> log)
        {
            if (def.isAutomaton) return; // el Autómata: a machine, wholly immune to la Mancha
            if (amount > 0)
            {
                // Class resist + 3% per Faith point ± consumable modifiers, capped; gains never zeroed (min 1).
                int resist = EffCorrResist;
                int real = Mathf.Max(1, Mathf.RoundToInt(amount * (100 - resist) / 100f));
                if (isInfiltrator) real = Mathf.Max(1, Mathf.RoundToInt(real * 0.5f)); // tell: the Foso barely stains its own agent
                corruption = Mathf.Min(200, corruption + real);
                log($"{unitName} gana +{real} de Mancha ({corruption}/200)");
                if (corruption >= 100 && !soulTested && def.team == Team.Heroes)
                    SoulTrial(log); // resolved ON THE SPOT, even mid-round (design rule)
            }
            else if (amount < 0)
            {
                int before = corruption;
                corruption = Mathf.Max(0, corruption + amount);
                if (before != corruption) log($"{unitName} purga {before - corruption} de Mancha ({corruption}/200)");
            }
        }

        public void GainFaith(int amount, System.Action<string> log)
        {
            if (def.isAutomaton) return; // el Autómata never gains la Llama — a machine has no soul to kindle
            if (isInfiltrator && amount > 0) return; // tell: a cult agent never truly kindles la Llama
            if (amount == 0 || def.team != Team.Heroes) return;
            int before = faith;
            faith = Mathf.Clamp(faith + amount, 0, 10);
            if (faith != before)
                log($"{unitName} {(amount > 0 ? "gana" : "pierde")} {Mathf.Abs(faith - before)} de Llama ({faith}/10)");
        }

        /// <summary>
        /// Soul Trial at 100 Corruption: base 25% Momento de Fe (+4% per Faith
        /// point) vs 75% Quebranto. One trial per combat.
        /// </summary>
        void SoulTrial(System.Action<string> log)
        {
            soulTested = true;
            log($"⚡ LA PRUEBA DE FE — la Mancha de {unitName} alcanza el umbral.");
            int virtueChance = Mathf.Min(95, 25 + 4 * faith);
            if (Rng.Range(0, 100) < virtueChance)
            {
                soul = Rng.Value < 0.5f ? SoulState.Iluminado : SoulState.Inquebrantable;
                if (soul == SoulState.Iluminado)
                {
                    corruption = Mathf.Max(0, corruption - 30);
                    // +10 PRE is intrinsic to the Iluminado state (see EffAccuracyBonus)
                    log($"✝ TEMPLE: {unitName} es ILUMINADO — la Llama lo atraviesa (−30 Mancha, +10 PRE).");
                }
                else
                {
                    corruption = Mathf.Max(0, corruption - 15);
                    log($"✝ TEMPLE: {unitName} se alza INQUEBRANTABLE (−15 Mancha, −20% daño recibido).");
                }
            }
            else
            {
                int r = Rng.Range(0, 4);
                soul = r == 0 ? SoulState.Conmocionado : r == 1 ? SoulState.Desesperado : r == 2 ? SoulState.Saqueador : SoulState.Apostata;
                switch (soul)
                {
                    case SoulState.Conmocionado:
                        log($"☠ TRAUMA: {unitName} sucumbe — CONMOCIONADO. \"Ninguno de vosotros es quien dice ser.\"");
                        break;
                    case SoulState.Desesperado:
                        log($"☠ TRAUMA: {unitName} sucumbe — DESESPERADO (−10 PRE; su desesperación contagia al grupo).");
                        break;
                    case SoulState.Saqueador:
                        log($"☠ TRAUMA: {unitName} sucumbe — SAQUEADOR. Solo ve botín: a veces se pierde rebuscando y mete mano al saqueo.");
                        break;
                    case SoulState.Apostata:
                        log($"☠ TRAUMA: {unitName} sucumbe — APÓSTATA. Maldice el nombre de la Llama en voz alta.");
                        break;
                }
            }
        }

        /// <summary>
        /// Own-turn-start tick: DoT damage first, then status durations count
        /// down. Ticking durations at the START of the own turn (not the end)
        /// makes a 1-round self-buff last "until my next turn" — Defend,
        /// Desafío, Guard… would otherwise expire before any enemy acts.
        /// Stun is excluded: it is consumed explicitly when the turn is skipped.
        /// Returns true if the unit died to a DoT.
        /// </summary>
        public bool TickTurnStart(System.Action<string> log)
        {
            // A 200 la Mancha consume por entero: el alma se somete a la oscuridad (Caído — muerte simple, permadeath).
            if (def.team == Team.Heroes && corruption >= 200)
            {
                hp = 0; atDeathsDoor = false; // sin Borde de la Muerte: la oscuridad se lo lleva
                log($"⚫ {unitName} se somete a la oscuridad: la Mancha lo consume por entero.");
                return true;
            }
            foreach (var s in statuses.Where(s => s.kind == StatusKind.Bleed || s.kind == StatusKind.Burn || s.kind == StatusKind.Toxin).ToList())
            {
                int dot = s.power;
                if (s.kind == StatusKind.Burn && def.weakToFire) dot *= 2; // fire x2 vs aberrants/nests
                string dotName = s.kind == StatusKind.Bleed ? "Sangrado" : s.kind == StatusKind.Burn ? "Quemadura" : "Gas";
                var outcome = TakeDamage(dot); // DoTs ignore armor; Door rules apply
                log($"{unitName} sufre {dot} por {dotName} ({hp} PV)");
                switch (outcome)
                {
                    case DamageOutcome.EnteredDeathsDoor:
                        log($"‼ {unitName} está AL BORDE DE LA MUERTE. Un golpe más puede ser el último.");
                        GainCorruption(10, log); // staring into the abyss (OUR number, DD-style)
                        break;
                    case DamageOutcome.DeathsDoorResisted:
                        log($"{unitName} se niega a morir (prueba de muerte {def.deathBlowPct}% superada).");
                        break;
                    case DamageOutcome.Died:
                        log($"{unitName} sucumbe a sus heridas.");
                        return true;
                }
            }

            foreach (var s in statuses) if (s.kind != StatusKind.Stun) s.rounds--;
            var expiring = statuses.Where(s => s.kind != StatusKind.Stun && s.rounds <= 0 &&
                                               s.onExpire != null && s.onExpire.kind != StatusKind.None).ToList();
            statuses.RemoveAll(s => s.kind != StatusKind.Stun && s.rounds <= 0);
            foreach (var e in expiring)
            {
                AddStatus(e.onExpire.kind, e.onExpire.power, e.onExpire.duration);
                if (e.onExpire.kind == StatusKind.CorrResistMod)
                    log($"{unitName} acusa la resaca: {e.onExpire.power}% de resistencia a Mancha el resto del combate.");
            }
            return false;
        }
    }
}
