using System;

namespace Purga
{
    public enum Team { Heroes, Enemies }

    public enum TargetKind { Enemy, Ally, Self, AllAllies, AllEnemies }

    /// <summary>
    /// How an enemy perceives its prey on the light axis (Fase posiciones 2).
    /// Human foes shoot what the flare LIGHTS (they need to see it); the Foso's
    /// flesh and its Descendidos reach only what falls in the DARK.
    /// </summary>
    public enum ThreatChannel { Human, Foso }

    /// <summary>Equipment slots on a soldier (el Barracón). Weapon/Armor modify stats on top of the
    /// class base + Taller rank; Neck/Ring are pro/con trinkets (el collar, el anillo).</summary>
    public enum GearSlot { Weapon, Armor, Neck, Ring }

    public enum StatusKind
    {
        None,
        Bleed,       // power dmg at turn start, ignores armor
        Burn,        // power dmg at turn start, ignores armor
        Stun,        // skips next turn
        AccDebuff,   // -power accuracy
        AccBuff,     // +power accuracy
        DodgeBuff,   // +power dodge
        DmgBuffPct,  // +power% damage dealt
        SpeedDebuff, // -power speed (initiative)
        Defend,      // +15 dodge, -20% damage taken, until own next turn
        Guard,       // redirects single-target attacks aimed at guardTarget to self
        // -- appended in Milestone 2.3 (append-only: assets serialize these as ints) --
        Toxin,       // power dmg at turn start, ignores armor, stacks with Bleed; cured by Contraveneno
        SpeedBuff,   // +power speed (initiative)
        CorrResistMod, // ±power % corruption resistance (Incienso +15, stim hangover -10)
        ToxinImmune, // blocks new Toxin applications
        // -- appended in Milestone 2.4 --
        Marked,      // +power % damage taken (Señalar al hereje)
        Taunt,       // enemies must target this unit (Desafío)
        ArmorBuff,   // +power armor (Barrera cinética)
        JudgmentNext, // next own attack: cannot miss, damage x2 (Acto de Fe: Juicio)
        DmgResistPct // -power % damage taken (Escudo del Emperador)
    }

    public enum SpecialKind
    {
        None,
        ToggleMelee, // Veterano: Fijar bayonetas
        GuardLowest, // Veterano: Cuerpo a tierra
        Cantico,     // Susurrante: +corruption to 2 random heroes
        // -- appended in Milestone 2.4 --
        Ejecucion,   // Comisario: kill a Broken ally (or 150+ Corr); rest -30 Corr +2 Fe +15% dmg
        Himno        // Hermana: -6 Corruption to the two adjacent allies
    }

    /// <summary>
    /// Result of a Soul Trial (Prueba de Alma) at 100 Corruption.
    /// First three are Quebrantos (afflictions), last two Momentos de Fe (virtues).
    /// </summary>
    public enum SoulState
    {
        None,
        Conmocionado,   // 50% refuses heals/cleanses from allies (shell-shock)
        Desesperado,    // -10 PRE; +2 Corruption to the rest of the group at his turn start
        Saqueador,      // greed: ~40% loses his combat action; skims part of the raid's loot on return
        Apostata,       // -1 Fe to a random ally at his turn start
        Iluminado,      // -30 Corruption on trigger, +10 PRE rest of combat
        Inquebrantable  // -15 Corruption on trigger, -20% damage taken rest of combat
    }

    /// <summary>What an expedition item does (slice §5.1). One item = one of the 16 slots.</summary>
    public enum ItemKind
    {
        AmmoRefill,  // Cargador: refills firearms between combats (6 charges)
        Promethium,  // Frasco: refills the Sacerdote's flasks; burns nests (3.4+)
        Ration,      // heals 3 HP out of combat
        MedKit,      // heals 4 HP, removes Bleed/Toxin out of combat
        PuritySeal,  // -25 Corruption, single use
        ServoSkull,  // passive: +2 HP to the most wounded hero after each combat
        Loot,        // cult valuables: sells back at the ship
        // -- appended in 3.4-3.6 --
        HolyWater,   // consecrates profaned curiosities (altar)
        Lockpicks,   // opens sealed reliquaries/chests
        CampKit,     // allows camping at a Santuario (20% night ambush)
        VoxBeacon,   // +2 Vox signal levels when planted
        GasMask      // passive supply: negates the ambient Gas cloud hazard for the party
    }

    /// <summary>Result of applying damage to a unit (Death's Door rules for heroes).</summary>
    public enum DamageOutcome
    {
        Damaged,           // took the hit, still standing
        EnteredDeathsDoor, // hero dropped to 0 HP: Al Borde de la Muerte
        DeathsDoorResisted,// hero at the Door survived a death check
        Died               // enemy at 0 HP, or hero failed the death check
    }

    [Serializable]
    public class StatusApply
    {
        public StatusKind kind = StatusKind.None;
        public int chance = 100;   // % after hit
        public int power;
        public int duration;       // in own-turns of the target
    }
}
