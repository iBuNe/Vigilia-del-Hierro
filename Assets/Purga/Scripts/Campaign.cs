using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Purga
{
    public enum HeroAssignment { Available, Expedition, Reclusiam, Enfermeria }

    /// <summary>How a soldier joined the roster — sets the infiltration chance (§2, Hito 6).</summary>
    public enum RecruitRoute { None, Leva, Acogida, Prisioneros, Supervivientes }

    /// <summary>Campaign modifiers (§1 "votos", Hito 7): a vow the Comandante takes at the start — a cost
    /// paid for a reward, locked for the run. The replayability pillar.</summary>
    public enum Voto { Sangre, Silencio, Ceniza, Hierro }

    /// <summary>
    /// Persistent campaign state (Milestone 4): the roster that survives across
    /// expeditions with permadeath, the ship's money and storeroom, the imperial
    /// week counter and the Despertar threat clock. The full ship (Forja bonds,
    /// Celdas/infiltration, Librarium) arrives in later milestones; this is the
    /// minimum viable loop: recruit, rotate, heal, descend, bury.
    /// </summary>
    public class Campaign
    {
        public List<CombatUnit> roster = new List<CombatUnit>();
        public Dictionary<CombatUnit, HeroAssignment> assignment = new Dictionary<CombatUnit, HeroAssignment>();
        public List<ItemStack> storeroom = new List<ItemStack>();
        public List<string> memorial = new List<string>(); // the fallen, by cause

        // Librarium (Milestone 5.4): knowledge earned by killing and by surviving.
        public Dictionary<string, int> bestiaryKills = new Dictionary<string, int>();
        public HashSet<string> bestiaryEncountered = new HashSet<string>();
        public HashSet<string> boughtKnowledge = new HashSet<string>();
        public Dictionary<string, UnitDef> bestiaryDefs = new Dictionary<string, UnitDef>();
        public HashSet<string> glossary = new HashSet<string>();
        public const int BestiaryBuyCost = 120; // buy level-2 knowledge: money as a shortcut to info, never power

        /// <summary>0 = unknown (???), 1 = seen (1 kill), 2 = mastered (3 kills, bought, or a boss encountered).</summary>
        public int Knowledge(UnitDef def)
        {
            if (def == null) return 0;
            if (def.isBoss && bestiaryEncountered.Contains(def.displayName)) return 2; // bosses reveal on sight
            if (boughtKnowledge.Contains(def.displayName)) return 2;
            bestiaryKills.TryGetValue(def.displayName, out int k);
            return k >= 3 ? 2 : k >= 1 ? 1 : 0;
        }

        public void RecordEncounter(UnitDef def)
        {
            if (def == null) return;
            bestiaryEncountered.Add(def.displayName);
            bestiaryDefs[def.displayName] = def;
        }
        public void RecordKill(UnitDef def)
        {
            if (def == null) return;
            bestiaryEncountered.Add(def.displayName);
            bestiaryDefs[def.displayName] = def;
            bestiaryKills.TryGetValue(def.displayName, out int k);
            bestiaryKills[def.displayName] = k + 1;
        }
        public bool LearnGlossary(string entry) => !string.IsNullOrEmpty(entry) && glossary.Add(entry);

        public int tronos = 500;
        public int week = 1;
        public int phase = 1;        // Despertar: 1..8
        public int weeksInPhase = 0; // advances a phase every 2 weeks
        public int rosterCap = 8;
        public int seed;             // deterministic RNG seed for this run (shareable; base del procedural, §5)

        // The front: a node-graph (design §2), built in New(). Replaces the old linear substages.
        public FrontMap frontMap;
        public int currentSector = 1;         // H6: 1 Primera Línea · 2 Galerías · 3 Tierra de Nadie
        public const int MaxSector = 4;        // 1 Primera Línea · 2 Galerías · 3 Tierra de Nadie · 4 el Reducto (Confesor Rojo, Hito 7)

        /// <summary>Slay a sector boss → load the next sector's map (the roster/base carry over; la Crecida keeps ticking).</summary>
        public void AdvanceSector()
        {
            currentSector = Mathf.Min(MaxSector, currentSector + 1);
            frontMap = FrontMap.BuildSector(currentSector);
            stageWon = false;
        }
        public const int GraceWeeks = 4;     // phase 8: weeks before the sector falls
        public bool stageWon;                // the Herida is sealed: the campaign is won
        public bool confessorDown;           // Hito 7·ii: el Confesor cayó — la Herida yace abierta; hay que SELLARLA para ganar
        public bool planetFallen;            // the Crecida reached its end: campaign lost
        public bool BossUnlocked => frontMap != null && frontMap.BossReachable;
        public int substagesCleared => frontMap != null ? frontMap.ClearedCount : 0; // nodes cleared (report/compat)
        public bool CampaignOver => stageWon || planetFallen;

        // Deck capacities (upgradeable later)
        public const int ReclusiamSlots = 2;
        public const int EnfermeriaSlots = 2;
        public const int ReclusiamBase = 150;
        public const int EnfermeriaBase = 100;
        public const int ReclusiamCleanse = 20; // Corruption removed per week (level I)

        // A veteran costs more to keep than a fresh recruit: +40% of the base per level.
        public static int ReclusiamCost(CombatUnit h) => Mathf.RoundToInt(ReclusiamBase * (1f + 0.4f * h.level));
        public static int EnfermeriaCost(CombatUnit h) => Mathf.RoundToInt(EnfermeriaBase * (1f + 0.4f * h.level));

        // §2 indisponibilidad: a treatment benches the soldier 1–3 incursiones by severity (leve/media/grave).
        public static int ChapelRaids(CombatUnit h) => (h.corruption >= 150 || IsQuebranto(h.soul)) ? 3 : h.corruption >= 100 ? 2 : 1;
        public static int InfirmaryRaids(CombatUnit h)
        { int miss = h.EffMaxHP - h.hp; return (h.atDeathsDoor || miss >= h.EffMaxHP * 2 / 3) ? 3 : miss >= h.EffMaxHP / 3 ? 2 : 1; }

        /// <summary>Bench a soldier in la Capilla/Enfermería for its treatment (only if not already in one). Cost is the caller's.</summary>
        public void SendToTreatment(CombatUnit h, HeroAssignment kind)
        {
            if (h == null || h.treatmentRaidsLeft > 0) return;
            h.treatmentRaidsLeft = kind == HeroAssignment.Reclusiam ? ChapelRaids(h) : InfirmaryRaids(h);
            assignment[h] = kind;
        }

        // el Taller del Zapador: per-soldier gear upgrades, immediate (no indisponibilidad), cost rises per rank.
        public const int TallerWeaponBase = 400, TallerArmorBase = 300;
        public static int WeaponUpgradeCost(CombatUnit h) => TallerWeaponBase * (h.weaponRank + 1);
        public static int ArmorUpgradeCost(CombatUnit h) => TallerArmorBase * (h.armorRank + 1);

        // el Barracón: gear owned but not currently equipped (the armoury). Equipped gear lives on the soldier.
        public List<GearDef> armory = new List<GearDef>();

        /// <summary>Equip a piece from the armoury into its slot; any displaced item returns to the armoury.</summary>
        public void Equip(CombatUnit h, GearDef g)
        {
            if (h == null || g == null || !armory.Contains(g)) return; // must own it (unequipped) to equip it
            var prev = h.GearIn(g.slot);
            if (prev != null) armory.Add(prev);   // swap the old piece back into the armoury
            armory.Remove(g);
            h.SetGear(g.slot, g);
        }

        /// <summary>Strip the item in a slot back to the armoury.</summary>
        public void Unequip(CombatUnit h, GearSlot s)
        {
            if (h == null) return;
            var cur = h.GearIn(s);
            if (cur == null) return;
            h.SetGear(s, null);
            armory.Add(cur);
        }

        static readonly string[] Names =
        {
            "Kael", "Vorn", "Mala", "Grael", "Sisto", "Yrma", "Dain", "Halix",
            "Bruma", "Cato", "Nerva", "Volk", "Iona", "Rask", "Thane", "Ozma",
            "Perer", "Sand", "Tull", "Wex", "Aldo", "Fenn", "Lira", "Mordt"
        };
        static readonly string[] Classes = { "Veteran", "Sister", "Overseer", "Listener", "Preacher" };
        int nameIdx;

        /// <summary>Create a campaign. Pass a seed to reproduce/share a run; omit for a fresh random one.
        /// Seeding happens BEFORE the roster is built, so even the starting warband is deterministic.</summary>
        // los votos (§1, Hito 7): campaign modifiers locked at creation. Each is a cost + a reward.
        public HashSet<Voto> votos = new HashSet<Voto>();
        public bool HasVoto(Voto v) => votos.Contains(v);
        public static string VotoName(Voto v) => v switch {
            Voto.Sangre   => "Voto de Sangre",
            Voto.Silencio => "Voto de Silencio",
            Voto.Ceniza   => "Voto de Ceniza",
            Voto.Hierro   => "Voto de Hierro", _ => v.ToString() };
        public static string VotoDesc(Voto v) => v switch {
            Voto.Sangre   => "Sin Leva (no llegan reclutas) — pero la paga de misión ×1,3.",
            Voto.Silencio => "la Señal se agota antes (×1,5) — pero el botín ×1,3.",
            Voto.Ceniza   => "la Crecida avanza el doble de rápido — pero empiezas con +600 de paga.",
            Voto.Hierro   => "la Capilla y la Enfermería cuestan el doble — pero toda la compañía empieza con arma rango 1.", _ => "" };

        public static Campaign New(int? seed = null, IEnumerable<Voto> votos = null)
        {
            var c = new Campaign();
            c.seed = seed ?? Rng.NewSeed();
            if (votos != null) foreach (var v in votos) c.votos.Add(v);
            Rng.Init(c.seed);
            c.frontMap = FrontMap.BuildStage1();
            // Starting warband: one of each core class plus a spare Guardsman
            foreach (var cls in new[] { "Sister", "Overseer", "Listener", "Preacher", "Veteran", "Veteran" })
                c.roster.Add(c.MakeRecruit(cls));
            // A starter armoury so el Barracón isn't empty on day one.
            foreach (var k in new[] { "BayonetaAfilada", "PetoLigero", "MedallaVigilia", "AnilloOficial" })
                c.armory.Add(GameData.Gear(k));
            // Votos: starting boons paid for by their in-run cost.
            if (c.HasVoto(Voto.Ceniza)) c.tronos += 600;                                   // extra paga
            if (c.HasVoto(Voto.Hierro)) foreach (var h in c.roster) h.weaponRank = 1;       // company starts armed
            return c;
        }

        // el Taller forja Autómatas (clase-firma, Hito 6). A serial designation, not a human name.
        public const int AutomatonCost = 1500;
        public const int AutomatonRepairCost = 400; // patch a broken (averiado) Autómata back to full
        int automatonSerial = 1;

        /// <summary>el Taller repairs a broken Autómata: clears the avería and restores it to full HP (immediate).</summary>
        public void RepairAutomaton(CombatUnit a)
        {
            if (a == null || !a.averiado) return;
            a.averiado = false;
            a.hp = a.EffMaxHP;
            a.statuses.Clear(); // a rebuilt machine doesn't carry the Bleed/Burn/Gas it broke with
        }

        public CombatUnit BuildAutomaton()
        {
            var u = new CombatUnit(GameData.Unit("Automaton"), " Mk" + (automatonSerial++).ToString("00"));
            assignment[u] = HeroAssignment.Available;
            roster.Add(u);
            return u;
        }

        // Infiltración (§2): a recruit may be a hidden cult agent, by route. La Leva only turns after
        // the front rots (fase 4); the desperate routes (taking people in, prisoners) are riskier.
        public static int InfiltratorChance(RecruitRoute route, int phase) => route switch
        {
            RecruitRoute.Leva          => phase >= 4 ? 25 : 0,
            RecruitRoute.Acogida       => 35,
            RecruitRoute.Prisioneros   => 40,
            RecruitRoute.Supervivientes => 15,
            _ => 0,
        };

        public CombatUnit MakeRecruit(string cls, RecruitRoute route = RecruitRoute.None)
        {
            var u = new CombatUnit(GameData.Unit(cls), " " + Names[nameIdx++ % Names.Length]);
            // Default combat consumable loadout (2 per hero); the player will edit these later
            switch (cls)
            {
                case "Preacher": u.consumables.Add(GameData.Consumable("Incienso")); u.consumables.Add(GameData.Consumable("Amasec")); break;
                case "Listener":  u.consumables.Add(GameData.Consumable("Amasec")); u.consumables.Add(GameData.Consumable("Contraveneno")); break;
                case "Overseer": u.consumables.Add(GameData.Consumable("Coraje")); u.consumables.Add(GameData.Consumable("Recaf")); break;
                default:          u.consumables.Add(GameData.Consumable("Estimulante")); u.consumables.Add(GameData.Consumable("Recaf")); break;
            }
            if (route != RecruitRoute.None && Rng.Range(0, 100) < InfiltratorChance(route, phase))
                u.isInfiltrator = true; // hidden: the player is never told — only the tells give it away
            assignment[u] = HeroAssignment.Available;
            return u;
        }

        // la Brigada (§2): interrogate a suspected infiltrator. Reliability rises with the building rank;
        // it is never certain — a loyal soldier can be wrongly flagged, a real agent can pass clean.
        public int brigadaRank = 0;                                  // 0..2
        static readonly int[] BrigadaReliability = { 70, 85, 95 };   // % the verdict is correct
        public int InterrogationReliability => BrigadaReliability[Mathf.Clamp(brigadaRank, 0, 2)];
        public const int InterrogateCost = 120;
        public static int BrigadaUpgradeCost(int rank) => 800 * (rank + 1);
        public HashSet<CombatUnit> interrogated = new HashSet<CombatUnit>(); // one verdict stands per soldier per rank

        /// <summary>Interrogate a soldier. Returns the VERDICT (true = flagged as infiltrator) — correct only
        /// InterrogationReliability% of the time, so both false positives and false negatives happen.</summary>
        public bool Interrogate(CombatUnit h)
        {
            bool correct = Rng.Range(0, 100) < InterrogationReliability;
            return correct ? h.isInfiltrator : !h.isInfiltrator;
        }

        /// <summary>Fusilar/expel a suspected infiltrator: off the roster for good (a memorial line, not a combat death).</summary>
        public void Expel(CombatUnit h, System.Action<string> log)
        {
            if (h == null || !roster.Contains(h)) return;
            roster.Remove(h);
            assignment.Remove(h);
            memorial.Add($"{h.unitName} — fusilado por la Brigada");
            log?.Invoke($"La Brigada se lleva a {h.unitName} al paredón.{(h.isInfiltrator ? " Sus últimas palabras no son de la Vigilia." : " Muere en silencio.")}");
        }

        public IEnumerable<CombatUnit> Assigned(HeroAssignment a) => roster.Where(h => Get(h) == a);
        public HeroAssignment Get(CombatUnit h) => assignment.TryGetValue(h, out var a) ? a : HeroAssignment.Available;

        public float PriceMult => phase >= 3 ? 1.2f : 1f; // Despertar phase 3: the cult controls trade
        public int Price(int basePrice) => Mathf.RoundToInt(basePrice * PriceMult);

        /// <summary>The Leva: up to 2 recruits per week, never over the roster cap.</summary>
        public List<CombatUnit> Leva(System.Action<string> log)
        {
            var newcomers = new List<CombatUnit>();
            if (HasVoto(Voto.Sangre)) { log?.Invoke("Voto de Sangre: la Leva no acude. Sobrevivid con lo que tenéis."); return newcomers; }
            for (int i = 0; i < 2 && roster.Count < rosterCap; i++)
            {
                var cls = Classes[Rng.Range(0, Classes.Length)];
                var recruit = MakeRecruit(cls, RecruitRoute.Leva);
                roster.Add(recruit);
                newcomers.Add(recruit);
                log?.Invoke($"La Leva entrega a {recruit.unitName}.");
            }
            return newcomers;
        }

        public void AdvanceClock(System.Action<string> log)
        {
            weeksInPhase++;
            if (phase < 8)
            {
                if (weeksInPhase >= (HasVoto(Voto.Ceniza) ? 1 : 2)) // Voto de Ceniza: la Crecida corre el doble
                {
                    phase++;
                    weeksInPhase = 0;
                    log?.Invoke($"⏳ LA CRECIDA avanza a la fase {phase}. {PhaseNote(phase)}");
                    foreach (var lost in frontMap?.Degrade(phase) ?? new List<FrontNode>())
                        log?.Invoke($"✖ El frente se pudre con la Crecida: se pierde «{lost.name}».");
                }
            }
            else // phase 8: La Llamada — grace countdown to the planet falling
            {
                int grace = GraceWeeks - weeksInPhase;
                if (grace <= 0 && !stageWon)
                {
                    planetFallen = true;
                    log?.Invoke("☠ EL FORTÍN DE VARED HA CAÍDO. La Marea del Foso desciende. La campaña ha terminado.");
                }
                else if (!stageWon)
                    log?.Invoke($"⏳ La Llamada resuena en el Foso. Quedan {grace} semanas antes de que el sector caiga.");
            }
        }

        public static string PhaseNote(int phase)
        {
            switch (phase)
            {
                case 2: return "Renegados armados patrullan las Puertas de Vared.";
                case 3: return "El culto controla el comercio: los precios suben.";
                case 4: return "Se rumorea que la Leva ya no es de fiar.";
                case 5: return "Tocados de tercera generación por todas partes.";
                case 6: return "El Barón de Trinchera canaliza un ritual: la Mancha se filtra en el Fortín.";
                case 7: return "Se avistan Verdugos. Que la Llama se apiade.";
                case 8: return "LA LLAMADA. El planeta caerá en cuatro semanas.";
                default: return "";
            }
        }

        public void KillHero(CombatUnit h, string cause)
        {
            memorial.Add($"{h.unitName} — {cause} (semana {week})");
            Unbond(h); // the tie is severed with the body
            roster.Remove(h);
            assignment.Remove(h);
        }

        // el Comedor: sharing a mess-table bonds two soldiers and eases their Mancha. The bond
        // is the payoff AND the risk — losing a bonded comrade grieves the survivor (see OnDeath).
        public const int ComedorCost = 120;      // paga per shared meal
        public const int ComedorMealMancha = 15; // Mancha eased for each of the pair
        public const int GriefMancha = 20;        // extra Mancha the survivor takes when a bond falls

        public static void Bond(CombatUnit a, CombatUnit b)
        {
            if (a == null || b == null || a == b) return;
            Unbond(a); Unbond(b);
            a.bondPartner = b; b.bondPartner = a;
        }

        public static void Unbond(CombatUnit a)
        {
            if (a?.bondPartner != null) { a.bondPartner.bondPartner = null; a.bondPartner = null; }
        }

        public static bool IsQuebranto(SoulState s) =>
            s == SoulState.Conmocionado || s == SoulState.Desesperado || s == SoulState.Saqueador || s == SoulState.Apostata;

        /// <summary>
        /// An incursión passes: soldiers under treatment recover a step and their
        /// indisponibilidad counter drops (§2) — la Capilla cleanses Corruption and
        /// lifts a Quebranto on the last raid, Enfermería heals to full on the last
        /// raid; soldiers still recovering stay benched, the rest free up; then the
        /// Leva recruits and the Despertar clock ticks. Unit-testable on the campaign.
        /// </summary>
        public void PassWeek(System.Action<string> log)
        {
            // Treatment recovers gradually and keeps the soldier benched until its incursiones run out (§2).
            foreach (var h in Assigned(HeroAssignment.Reclusiam).Where(h => h.treatmentRaidsLeft > 0).ToList())
            {
                h.corruption = Mathf.Max(0, h.corruption - ReclusiamCleanse);
                h.treatmentRaidsLeft--;
                if (h.treatmentRaidsLeft == 0)
                {
                    if (IsQuebranto(h.soul)) { log?.Invoke($"la Capilla libra a {h.unitName} de su Trauma ({CombatUnit.SoulName(h.soul)}). Vuelve al servicio."); h.soul = SoulState.None; }
                    else log?.Invoke($"{h.unitName} cumple su penitencia en la Capilla ({h.corruption}/200 Mancha). Disponible.");
                }
                else log?.Invoke($"{h.unitName} reza en la Capilla: −{ReclusiamCleanse} Mancha ({h.corruption}/200). Aún {h.treatmentRaidsLeft} incursion(es) fuera.");
            }
            foreach (var h in Assigned(HeroAssignment.Enfermeria).Where(h => h.treatmentRaidsLeft > 0).ToList())
            {
                h.atDeathsDoor = false;
                h.hp = Mathf.Min(h.EffMaxHP, h.hp + Mathf.Max(1, h.EffMaxHP / 3));
                h.treatmentRaidsLeft--;
                if (h.treatmentRaidsLeft == 0) { h.hp = h.EffMaxHP; log?.Invoke($"{h.unitName} sale de la Enfermería restablecido ({h.hp}/{h.EffMaxHP} PV)."); }
                else log?.Invoke($"{h.unitName} sana en la Enfermería ({h.hp}/{h.EffMaxHP} PV). Aún {h.treatmentRaidsLeft} incursion(es) fuera.");
            }
            // Only soldiers NOT in treatment free up; those still recovering stay benched (forces roster rotation).
            foreach (var h in roster.ToList()) if (h.treatmentRaidsLeft <= 0) assignment[h] = HeroAssignment.Available;

            Leva(log);
            week++;
            AdvanceClock(log);
        }
    }
}
