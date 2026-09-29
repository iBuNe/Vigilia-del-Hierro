using System;
using System.Collections.Generic;
using UnityEngine;

namespace Purga
{
    /// <summary>
    /// Data-driven unit (hero or enemy). Lives in its own file (class name =
    /// file name) so Unity can bind the script reference when serialized.
    /// </summary>
    [CreateAssetMenu(menuName = "Purga/Unit", fileName = "NewUnit")]
    public class UnitDef : ScriptableObject
    {
        public string displayName;
        public Team team;
        public int maxHP, armor, dodge, speed;
        public int corrResist;                     // % resistance to corruption gains
        public int maxAmmo;
        public bool countsForCohesion = true;      // humans/hybrids: true. Aberrants/monsters: false
        public bool weakToFire;                    // Aberrantes/nests: fire damage x1.5, Burn DoT x2
        public bool isLeader;                      // can issue the leader order (initiative swap minor action)
        // Status resistances (%): subtracted from the application chance (slice §1.1)
        public int resBleed, resToxin, resStun, resMove, resDebuff;
        public int deathBlowPct = 33;              // heroes: % to die per hit taken at Death's Door (design base 33)
        public int threat = 1;                     // encounter-budget cost: how much of a combat's threat budget this enemy spends
        public bool usesPromethium;                // Sacerdote: ammo refills consume Frascos, not Cargadores
        // -- Milestone 5: bosses & elites --
        public bool isBoss;                        // multi-phase, ends the substage, retards the Despertar clock
        public bool isElite;                       // mini-boss: not counted for group flee, own quirks
        public int ownCohesion;                    // >0: a personal Will bar (dual win: HP 0 OR Will 0). Faith halves it double
        public int phaseTwoAtPct;                  // boss: HP% that triggers phase 2 (e.g. 50)
        public int phaseThreeAtPct;                // boss: HP% that triggers phase 3 (Confesor Rojo, Hito 7)
        public string phaseTwoLog, phaseThreeLog;  // boss: flavour line on each phase transition (fallback generic)
        public string phaseThreeSummon;            // boss: a one-time spawn on entering phase 3 (Confesor → un Verdugo lo posee)
        public string summonType;                  // elite/boss: what it calls in (Madre de la Camada / Confesor)
        public int summonPerRound;                 // how many summoned each round
        public bool summonFromNest;                // Matriarca: her brood only comes while a Nido survives (others summon directly)
        // -- Milestone 4: HUMANOS bestiary --
        public int dropsMoraleOnDeath;             // el Cabecilla: his death caves the cult's Moral by this much
        public bool isBomber;                      // el Bombardero: telegraphed area attack (prime → detonate)
        public bool isVip;                         // el Rescatado: non-controllable escort — its death fails the Rescate
        // -- Fase posiciones 2: light channel --
        public ThreatChannel threatChannel = ThreatChannel.Human; // Human foes hit the LIT; Foso flesh/demons hit the DARK
        // -- Hito 5: CARNE DEL FOSO (§9.2) --
        public bool devoursCorpses;                // Devoracadáveres: on its turn, eats a corpse on the field and heals
        public int corpseHeal = 8;                 // how much it heals per corpse devoured
        public bool gasOnHit;                      // Supurante: bursts a Gas cloud onto the attacker when wounded
        public bool gasOnDeath;                    // Supurante: area Gas burst over the party when it dies
        public string splitsInto;                  // Cosido: at half HP, splits into two of this unit key
        public bool extraActionIfUnhit;            // Verdugo: acts twice in a round if nothing hurt it since its last turn
        public bool isSapper;                      // Zapador Injertado (mini-boss S2): telegraphed gallery cave-in
        public bool packLeader;                    // Alfa del Foso (mini-boss S3): killing it scatters its Sabueso pack
        public bool isFlayer;                      // Desollador (mini-boss F): rips a hero out of formation and flays them
        // -- Hito 6: DESCENDIDOS (§9.3) --
        public bool isDescendido;                  // a true demon of the Foso: its presence adds +5 Mancha to the party
        public bool silencesMiracles;              // el Corista: its chant silences hero faith abilities for a round + area Mancha
        public bool revivesDescendidos;            // el Guardián de la Herida: while it lives, dead Descendidos rise once
        // -- Hito 6: AUTÓMATA (clase-firma) --
        public bool isAutomaton;                   // a machine: immune to la Mancha, never gains la Llama, no Milagro benefit, no bonds; breaks (KO) instead of dying
        // -- Hito 6: FAUNA (§9.4) — neutral beasts of la Tierra de Nadie --
        public bool isFauna;                       // mutated wildlife: neutral flavour, drops carrion loot (not the Congregación)
        public bool weakToArea;                    // Plaga de ratas: a swarm — area damage tears through it (x1.5)
        public bool bleedsAttackerOnMelee;         // Bestia del Alambre: barbed wire — a melee attacker bleeds
        public bool stealsSupply;                  // Carroñero mutado: grabs a supply from the inventory, then flees
        public List<AbilityDef> abilities = new List<AbilityDef>();

        public static UnitDef New(string n, Action<UnitDef> cfg)
        {
            var u = CreateInstance<UnitDef>();
            u.displayName = n;
            cfg(u);
            return u;
        }
    }
}
