using System.Collections.Generic;
using System.Linq;

namespace Purga
{
    /// <summary>
    /// Persistent expedition state (Milestone 3.1): the SAME CombatUnit heroes
    /// travel through a chain of encounters — HP, Corruption, Faith, ammo use,
    /// DoTs, Quebrantos and deaths carry over. Map, inventory and Vox signal
    /// arrive in later 3.x steps; for now the route is linear.
    /// </summary>
    /// <summary>A stack in the shared inventory: one slot, N charges left.</summary>
    public class ItemStack
    {
        public ItemDef def;
        public int chargesLeft;
        public int lootValue;   // Loot items only: sell value back at the ship
        public bool refundable; // bought this ship visit and not yet taken down: cancel at full price
    }

    public class Expedition
    {
        public const int MaxSlots = 16;

        public List<CombatUnit> heroes = new List<CombatUnit>();
        public List<string[]> encounters = new List<string[]>();
        public List<ItemStack> inventory = new List<ItemStack>(); // packed at the ship before descending
        public List<CombatUnit> fledEnemies = new List<CombatUnit>(); // fled at Cohesion 0: may reappear, HP intact
        public List<CombatUnit> casualties = new List<CombatUnit>(); // heroes killed this expedition (for roster permadeath)
        public int substageIndex = -1; // 0..2 = Stage-1 substage; -1 = boss mission
        public int sector = 1;         // 1 Primera Línea · 2 Galerías · 3 Tierra de Nadie · F Reducto (drives the bestiary tier)
        public FrontNode node;         // the front-map node this incursion belongs to
        public int wavesLeft;          // Aguantar arena: waves still to survive
        public int missionRoundBudget; // Sabotaje: total rounds before reinforcements arrive (0 = no timer)
        public string[] miniBoss;  // optional anomaly-room encounter for this substage (null = none)
        public bool isBossMission; // a Stage boss assault (unlocks after clearing the substages)
        public bool bossDefeated;  // the Stage boss fell (by HP or by Will): retards the Despertar clock
        public int deepestCol;  // (legacy) how deep you pushed
        public int voxBonus;    // (legacy) planted beacons
        public int signal = 100; // la Señal con el Fortín: 0–100, erodes progressively as you push deeper, up with repeaters
        public int carriedLight = 3; // Opción B (Hito 5): la Bengala persiste entre exploración y combate; se arrastra aquí

        public bool HasSpace => inventory.Count < MaxSlots;

        public ItemStack FindWithCharges(ItemKind kind) =>
            inventory.FirstOrDefault(s => s.def.kind == kind && s.chargesLeft > 0);

        /// <summary>A Saqueador in the party pockets a quarter of every loot item on the way back.
        /// Returns the total paga lost. Static + pure so it can be unit-tested directly.</summary>
        public static int SaqueadorSkim(IEnumerable<ItemStack> inventory, IEnumerable<CombatUnit> party)
        {
            if (!party.Any(h => h.Alive && h.soul == SoulState.Saqueador)) return 0;
            int pocketed = 0;
            foreach (var s in inventory.Where(x => x.def.kind == ItemKind.Loot))
            {
                int cut = s.lootValue / 4;
                s.lootValue -= cut;
                pocketed += cut;
            }
            return pocketed;
        }

        public void ConsumeCharge(ItemStack s)
        {
            s.chargesLeft--;
            if (s.chargesLeft <= 0) inventory.Remove(s);
        }

        public int LootValue => inventory.Where(s => s.def.kind == ItemKind.Loot).Sum(s => s.lootValue);

        public static readonly string[] SubstageNames = { "1-1 · Los Mercados", "1-2 · La Capilla Profanada", "1-3 · La Magistratura" };

        /// <summary>An expedition for a Stage-1 substage (0..2), scaled by index and the Despertar phase.</summary>
        public static Expedition ForSubstage(List<CombatUnit> party, int substageIndex, int despertarPhase, int sector = 1)
        {
            var e = new Expedition();
            e.heroes = party;
            e.substageIndex = substageIndex;
            e.sector = sector;
            e.encounters = BuildEncounters(substageIndex, despertarPhase, sector);
            e.miniBoss = MiniBossFor(substageIndex, sector);
            return e;
        }

        // Anomaly mini-boss hidden in a deep-enough raid (not the sector Jefe), one per sector (§9.5):
        // S1 Falso Capellán · S2 Zapador Injertado · S3 Alfa del Foso · F Desollador. Shallow raids have none.
        static string[] MiniBossFor(int substageIndex, int sector)
        {
            if (substageIndex < 1) return null;
            switch (sector)
            {
                case 1: return new[] { "Cultist", "Cultist", "FalseChaplain" };
                case 2: return new[] { "Sapper", "Aberrant", "Cultist" };
                case 3: return new[] { "Alpha", "Houndof", "Houndof" };   // Alfa del Foso + su jauría
                case 4: return new[] { "Flayer", "Larva", "Larva" };      // el Desollador en la biomasa
                default: return null;
            }
        }

        /// <summary>Aguantar la línea: a single-combat arena where waves keep coming (design §2).</summary>
        public static Expedition Arena(List<CombatUnit> party, int phase, int waves, int sector = 1)
        {
            var e = new Expedition();
            e.heroes = party;
            e.sector = sector;
            e.encounters = new List<string[]> { WaveEncounter(phase, sector) }; // the first wave; the rest refill in combat
            e.wavesLeft = waves;
            return e;
        }

        /// <summary>One wave for an Aguantar arena: harder than a normal fight — it is a continuous
        /// stand with no respite, so each wave hits above a normal Sector-1 combat.</summary>
        public static string[] WaveEncounter(int phase, int sector = 1)
        {
            int budget = BudgetBase + BudgetPerStage * 2 + 4 + phase / 6 + Rng.Range(0, 2);
            return RollEncounter(2, phase, 0, false, budget, sector);
        }

        /// <summary>Assault on a sector Jefe: a short substage whose final room is the boss of that sector.</summary>
        public static Expedition BossMission(List<CombatUnit> party, int despertarPhase, int sector = 1)
        {
            var e = new Expedition();
            e.heroes = party;
            e.isBossMission = true;
            e.sector = sector;
            switch (sector)
            {
                case 2: // las Galerías — la Matriarca de la Herida
                    e.encounters = new List<string[]>
                    {
                        new[]{ "Gorger", "Weeper", "Cultist" },
                        new[]{ "Stitched", "Crawler", "Neophyte" },
                        new[]{ "Matriarch", "Nest", "Nest" }, // the throne: la Matriarca + sus Nidos
                    };
                    break;
                case 3: // la Tierra de Nadie — el Guardián de la Herida
                    e.encounters = new List<string[]>
                    {
                        new[]{ "Houndof", "Houndof", "Carrion" },
                        new[]{ "Chorister", "Brander", "RatSwarm" },
                        new[]{ "Warden", "Houndof", "Houndof" }, // the throne: el Guardián + su jauría
                    };
                    break;
                case 4: // el Reducto — el Confesor Rojo (jefe final, 3 fases)
                    e.encounters = new List<string[]>
                    {
                        new[]{ "Butcher", "Maw", "Weeper" },
                        new[]{ "Chorister", "Brander", "Larva", "Larva" },
                        new[]{ "Confessor", "Cultist", "Cultist" }, // the sagrario: el Confesor Rojo
                    };
                    break;
                default: // Sector 1, Primera Línea — el Barón de Trinchera
                    e.encounters = new List<string[]>
                    {
                        new[]{ "Cultist", "Neophyte", "Whisperer" },
                        new[]{ "Cultist", "Whisperer", "Neophyte", "Cultist" },
                        new[]{ "Baron", "Neophyte", "Neophyte" }, // the throne room
                    };
                    break;
            }
            return e;
        }

        // --- Encounter generation from a THREAT BUDGET (design §5 Hito 3: first procedural
        // layer). Each enemy costs def.threat; a combat gets a budget that grows with substage
        // depth, its position in the substage, and the Crecida phase, then spends it on a random
        // affordable roster. Hard design rails keep difficulty bounded: no tanks in 1-1, the
        // opening fight is pure Renegados, and the closer of a tank-unlocked substage guarantees
        // one Aberrant. Rng-driven, so a campaign seed reproduces the same raids. ---
        const int BudgetBase = 4, BudgetPerStage = 2, CloserBonus = 2, MaxEnemies = 4;

        static List<string[]> BuildEncounters(int substageIndex, int phase, int sector = 1)
        {
            int combats = 4 + substageIndex; // 1-1→4, 1-2→5, 1-3→6
            var encounters = new List<string[]>();
            for (int i = 0; i < combats; i++)
            {
                bool isCloser = i == combats - 1;
                // Difficulty tracks substage depth + a mild Crecida creep; the closer is the peak.
                int budget = BudgetBase + BudgetPerStage * substageIndex + phase / 6 + (isCloser ? CloserBonus : 0) + Rng.Range(0, 2);
                encounters.Add(RollEncounter(substageIndex, phase, i, isCloser, budget, sector));
            }
            return encounters;
        }

        // The horror tier: which strata of the bestiary can spawn. The sector is the primary driver,
        // but as la Crecida rises the front collapses toward the Foso, so deep phases pull the deeper
        // horrors into the Sector-1 line too (design §2: "el frente se degrada con la Crecida").
        //   1 = HUMANOS puros · 2 = + Carne del Foso (§9.2) · 3 = + Descendidos (§9.3) y Fauna (§9.4)
        public static int HorrorTier(int sector, int phase)
        {
            int t = 1;
            if (sector >= 2 || phase >= 4) t = 2;
            if (sector >= 3 || phase >= 6) t = 3;
            return t;
        }

        // Distinctive/elite foes: at most one per fight, or rosters degenerate into 4 snipers (and
        // §3: máx 1 élite por combate en Sectores 1–2 — el Verdugo cuenta como ese único élite).
        static readonly HashSet<string> Specials = new HashSet<string> { "Bomber", "Sniper", "Zealot", "Ringleader", "Butcher" };

        static string[] RollEncounter(int substageIndex, int phase, int combatIndex, bool isCloser, int budget, int sector = 1)
        {
            bool opener = substageIndex == 0 && combatIndex == 0 && sector == 1; // tutorial fight: pure Renegados
            bool armed = phase >= 2 || substageIndex >= 1;          // shotgunners show up
            bool tanks = substageIndex >= 1;                        // Aberrants never in 1-1
            int tier = HorrorTier(sector, phase);

            var pool = new List<string> { "Cultist" };
            if (armed) pool.Add("Neophyte");
            if (!opener) pool.Add("Whisperer");
            if (tanks) { pool.Add("Aberrant"); pool.Add("Bomber"); pool.Add("Sniper"); pool.Add("Zealot"); }
            if (substageIndex >= 2) pool.Add("Ringleader"); // a leader in the deepest substage
            // Tier 2: la Carne del Foso (§9.2) bleeds into the line — canal Foso, hunts the dark.
            if (tier >= 2 && !opener)
            { pool.Add("Gorger"); pool.Add("Weeper"); pool.Add("Stitched"); pool.Add("Crawler"); pool.Add("Maw"); pool.Add("Butcher"); }
            // Tier 3: los Descendidos (§9.3) y la Fauna (§9.4) del frente hundido.
            if (tier >= 3)
            { pool.Add("Houndof"); pool.Add("Chorister"); pool.Add("Brander");
              pool.Add("RatSwarm"); pool.Add("Carrion"); pool.Add("WireBeast"); pool.Add("Scavver"); }

            int Cost(string k) => GameData.Unit(k).threat;

            var result = new List<string>();
            // The hardest fight of a tank-unlocked substage opens with a tank.
            if (isCloser && tanks && budget >= Cost("Aberrant")) { result.Add("Aberrant"); budget -= Cost("Aberrant"); }

            int guard = 0;
            while (result.Count < MaxEnemies && guard++ < 32)
            {
                var affordable = pool.Where(k => Cost(k) <= budget).ToList();
                if (affordable.Count == 0) break;
                string pick = affordable[Rng.Range(0, affordable.Count)];
                result.Add(pick);
                budget -= Cost(pick);
                if (Specials.Contains(pick)) pool.RemoveAll(Specials.Contains); // cap: one special per fight
            }
            if (result.Count == 0) result.Add("Cultist"); // never empty
            return result.ToArray();
        }

        /// <summary>Instantiate an encounter (duplicates get A/B/C suffixes).</summary>
        public List<CombatUnit> SpawnEncounter(string[] keys)
        {
            var counts = new Dictionary<string, int>();
            var list = new List<CombatUnit>();
            foreach (var k in keys)
            {
                counts.TryGetValue(k, out int c);
                counts[k] = c + 1;
                bool duplicated = keys.Count(x => x == k) > 1;
                list.Add(new CombatUnit(GameData.Unit(k), duplicated ? " " + (char)('A' + c) : ""));
            }
            return list;
        }
    }
}
