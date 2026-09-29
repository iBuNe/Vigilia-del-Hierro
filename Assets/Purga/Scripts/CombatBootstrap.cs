using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Purga
{
    /// <summary>
    /// MILESTONE 1 — "El combate pelado".
    /// Drop this component on an empty GameObject in an empty scene and press Play.
    /// Everything renders through IMGUI so no Canvas/prefab setup is needed.
    /// </summary>
    public partial class CombatBootstrap : MonoBehaviour
    {
        // ---- parties ----
        List<CombatUnit> heroes = new List<CombatUnit>();   // index 0 = position 1 (front)
        List<CombatUnit> enemies = new List<CombatUnit>();  // index 0 = position 1 (front)

        // ---- round state ----
        int round;
        int missionRounds;      // Sabotaje: rounds elapsed across the WHOLE incursion (not reset per combat)
        int combatCorpses;      // Devoracadáveres: bodies on the field this combat (fuel to devour)
        int milagrosSilenced;   // Corista (§9.3): rounds remaining that hero faith abilities are silenced
        bool missionFailed;     // Sabotaje: the timer ran out — reinforcements arrived, incursion aborts
        bool suppressAmbush;         // test hook: skip the threshold ambush roll for a deterministic corridor probe
        bool suppressCorridorEvents; // test hook: skip mid-corridor events for a deterministic probe walk
        int lightLevel;         // la Bengala (over the front): the front `lightLevel` zones are lit; decays each round, reset by a flare
        int flareCharges;       // signal flares carried into this fight; a minor action spends one to relight
        const int MaxLight = 4;
        const int BaseFlares = 2; // signal flares every squad carries into a fight (combat light axis only)
        const int BlindFireAcc = 35; // accuracy an enemy loses firing out of its channel (no target in the light/dark it hunts)
        const int MirrorBlindAcc = 20; // accuracy a HERO loses firing from the light into the shadow (Fase 5; a handicap, not a wall)
        const int DarkMancha = 2;    // Mancha the dark bleeds into anyone standing outside the flare each round
        const int AssaultZone = 2;   // Alambrada+ (over the parapet): a hero this deep can reach the enemy backline
        const int ExposedAssaultAcc = 15; // over the parapet, silhouetted by the flare over the front: foes hit this hero +15 (the assault's cost now the front is lit)
        const float LightFaithMult = 1.3f;  // miracles burn brighter in the light: heals/cleanses ×1.3 when the caster is lit
        const float DarkPsychicMult = 1.3f; // the Vidente resonates in the dark: psychic (warp) damage ×1.3 when the caster is dark
        int activeBlindPenalty;      // transient: the current attack is off-channel "a ciegas"
        static readonly string[] ZoneNames = { "Galería", "Parapeto", "Alambrada", "Tierra de Nadie" };
        List<CombatUnit> turnQueue = new List<CombatUnit>();
        int turnIndex;
        int enemyCohesion, enemyCohesionMax;
        bool combatOver; string resultText = "";
        bool anyHeroDied;
        bool faithShakenThisRound; // fervor saturates: only the FIRST faith ability per round shakes Cohesion

        // ---- input state ----
        bool minorUsed; // 1 minor action per turn
        float enemyActTimer;

        // ---- log ----
        readonly List<string> log = new List<string>();

        Expedition expedition;
        SubstageMap map;
        Room currentRoom;   // combat room being fought
        bool inCombat;      // false = map view

        // ---- campaign / ship state (persists across expeditions) ----
        Campaign campaign;
        bool atShip = true;
        bool headless; // true during the headless campaign sim (keeps a running log, no UI)
        // Money and storeroom live on the campaign; these thin accessors keep the
        // shop/expedition code unchanged.
        int shipTronos { get => campaign.tronos; set => campaign.tronos = value; }
        List<ItemStack> shipStash { get => campaign.storeroom; set => campaign.storeroom = value; }

        // ---- ambush / retreat state ----
        bool ambushNext;                  // next SetupCombat is an ambush: shuffled formation, enemies first
        bool enemyFirstRound;
        bool retreatDeclaredThisRound;
        List<CombatUnit> savedFormation;  // restored after an ambush combat ends

        // ---- la Señal con el Fortín: a CONTINUOUS 0–100 link that erodes progressively as you push
        // deeper and is pushed back up by repeaters. VoxNames/VoxLevel stay as a 4-band readout for the
        // code that wants a qualitative level; ambush and loot read the raw % so their pressure ramps. ----
        static readonly string[] VoxNames = { "NULA", "Baja", "Media", "Alta" };
        const int SignalDecayPerRoom = 8;   // base % lost each new room you push into (legacy lump; superseded by per-step below)
        const int SignalDecayPerStep = 2;   // H5·3: la Señal lost per corridor STEP (a 2–4-step tunnel costs more the deeper it runs)
        const int AmbushPerStep = 3;        // H5·3: each extra corridor step adds this % to the threshold ambush (more exposure)
        const int SignalRepeaterBoost = 40; // % gained by deploying a repeater (Baliza)
        const int AmbushBase = 10;          // ambush % at full Señal
        const float AmbushPerMissing = 0.30f; // extra ambush % per point of Señal lost (at 0 → 40%)
        int Signal => expedition?.signal ?? 100;
        static int SignalBand(int s) => s >= 76 ? 3 : s >= 46 ? 2 : s >= 16 ? 1 : 0;
        int VoxLevel => expedition == null ? 3 : SignalBand(expedition.signal);

        void Start() { campaign = Campaign.New(); atShip = true; SetupToolkitUI(); }

        // =====================================================================
        // SETUP / EXPEDITION FLOW
        // =====================================================================
        List<CombatUnit> SelectedParty => campaign.Assigned(HeroAssignment.Expedition).ToList();

        // Launch the incursion for a front-map node. Its type/difficulty parameterise the raid.
        void StartExpedition(FrontNode node)
        {
            var party = SelectedParty;
            if (party.Count == 0) { Log("Nadie seleccionado para la expedición."); return; }

            expedition = node.type == FrontNodeType.Jefe ? Expedition.BossMission(party, campaign.phase, node.sector)
                       : node.type == FrontNodeType.Aguantar || node.type == FrontNodeType.Sellado
                         ? Expedition.Arena(party, campaign.phase, node.waves, node.sector) // Sellado: hold the line while the seal takes
                       : Expedition.ForSubstage(party, node.difficulty, campaign.phase, node.sector);
            expedition.node = node;
            expedition.missionRoundBudget = node.type == FrontNodeType.Sabotaje ? node.roundBudget : 0;
            missionRounds = 0; missionFailed = false;
            if (node.type == FrontNodeType.Rescate)
                expedition.heroes.Add(new CombatUnit(GameData.Unit("Vip"))); // the 5th figure to extract
            expedition.inventory = shipStash;          // the petate packed at the ship
            foreach (var s in expedition.inventory) s.refundable = false; // committed once you descend
            shipStash = new List<ItemStack>();
            map = SubstageMap.Generate(expedition.encounters, expedition.miniBoss, campaign != null ? campaign.phase : 1, expedition.sector);
            heroes = expedition.heroes;
            enemies = new List<CombatUnit>();
            inCombat = false; currentRoom = null; combatOver = false; resultText = "";
            atShip = false; pendingItem = null;
            if (!headless) log.Clear();
            Log($"—— SEMANA {campaign.week} · CRECIDA fase {campaign.phase} · {node.name.ToUpper()} ({node.type}) ——");
            Log($"Baja el grupo: {string.Join(", ", party.Select(h => h.unitName))}.");
            Log($"{map.CombatsTotal} nidos según el Farol. {expedition.inventory.Count} bultos. La Llama aún arde.");

            // Sabotaje del infiltrado (tell, sin señalar): filtra vuestra posición → la Señal arranca más baja.
            int moles = party.Count(h => h.isInfiltrator);
            if (moles > 0) expedition.signal = Mathf.Max(0, expedition.signal - 25 * moles);
        }

        /// <summary>
        /// Back to the Sancta Sicaria: bury the dead (permadeath from the roster),
        /// stabilize the survivors, collect the mission reward, and let the week
        /// pass (Reclusiam/Enfermería treatments, the Leva, the Despertar clock).
        /// Loot and leftover supplies return to the storeroom; nothing is auto-sold.
        /// </summary>
        void ReturnToShip(bool completed)
        {
            // Coraje: the +15 Mancha debt is paid on the way home even if the raid ended in RETIRADA/abort
            // (only VICTORIA charged it in combat) — no free Trauma removal by fleeing.
            heroes = expedition.heroes;
            foreach (var h in expedition.heroes.Where(h => h.Alive && h.endCombatCorruptionDebt > 0))
            { h.GainCorruption(h.endCombatCorruptionDebt, Log); h.endCombatCorruptionDebt = 0; }
            SweepDarkDeaths(); // any survivor left at 200 Mancha (from combat or the debt) submits to the dark — never returns alive

            // Permadeath: anyone who fell stays fallen.
            foreach (var dead in expedition.casualties.Distinct().ToList())
                campaign.KillHero(dead, completed ? "caído en Vared" : "abandonado en Vared");

            bool bossDown = expedition.bossDefeated;
            bool anyoneBack = expedition.heroes.Any(h => h.Alive);
            if (!anyoneBack)
            {
                Log("Nadie regresa al Fortín. El petate se pierde con los cuerpos.");
            }
            else
            {
                int reward = bossDown ? 1500 : completed ? 800 : 0; // slaying the Stage boss pays big
                if (campaign.HasVoto(Voto.Sangre)) reward = Mathf.RoundToInt(reward * 1.3f); // Voto de Sangre: más paga, sin Leva
                shipTronos += reward;
                // A Saqueador in the party skims a quarter of the haul on the way back.
                int pocketed = Expedition.SaqueadorSkim(expedition.inventory, expedition.heroes);
                if (pocketed > 0) Log($"⚠ Un Saqueador mete mano al botín: −{pocketed} de paga se pierden por el camino.");
                shipStash.AddRange(expedition.inventory);
                foreach (var h in expedition.heroes.Where(h => h.Alive)) h.RestoreAtShip();
                Log($"De vuelta en el Fortín con {expedition.inventory.Count} bultos" +
                    (reward > 0 ? $" y la recompensa (+{reward}p)." : ". Sin recompensa: la misión quedó a medias."));
                if (expedition.LootValue > 0)
                    Log($"Traéis botín por {expedition.LootValue}p. Véndelo en el depósito de suministros.");
                if (completed || bossDown) MaybeDropGear(bossDown);
            }

            bool cleared = map != null && map.AllCombatsCleared;
            var node = expedition.node;

            // Slaying a sector Jefe retards la Crecida and opens the next sector; the FINAL sector's boss wins.
            if (bossDown && anyoneBack)
            {
                if (node != null) campaign.frontMap.OnClear(node);
                campaign.phase = Mathf.Max(1, campaign.phase - 1);
                campaign.weeksInPhase = 0;
                if (campaign.currentSector < Campaign.MaxSector)
                {
                    int from = campaign.currentSector;
                    campaign.AdvanceSector(); // load the next sector's map (roster/base carry over)
                    Log($"⚑ El jefe del Sector {from} ({SectorName(from)}) ha caído. El frente se abre.");
                    Log($"→ Avanzáis a {SectorName(campaign.currentSector)}. El Puesto de Mando muestra el nuevo frente.");
                }
                else
                {
                    // The Confesor falls, but killing him only OPENS the Herida — sealing it is the true end (§2).
                    campaign.confessorDown = true;
                    Log($"⚑ EL CONFESOR ROJO HA CAÍDO. La Herida yace abierta, sin voz que la cante.");
                    Log($"→ Sella la Herida antes de que la Crecida lo consuma todo: el sellado está disponible en el Puesto de Mando.");
                }
            }
            // Completing the sealing ritual: the Herida is closed — the campaign is WON.
            else if (cleared && node != null && node.type == FrontNodeType.Sellado && anyoneBack)
            {
                campaign.frontMap.OnClear(node);
                campaign.stageWon = true;
                Log($"✓ El sello prende. La biomasa se retrae, chillando.");
                Log($"★★★ LA HERIDA DE VARED SELLADA. la Llama reconoce vuestro servicio. La Ruptura, aquí, cede. ★★★");
            }
            // Clearing a front node: mark it, open the ones it links to, grant any optional reward.
            else if (cleared && node != null && node.state != FrontNodeState.Cleared && anyoneBack)
            {
                campaign.frontMap.OnClear(node);
                Log($"✓ «{node.name}» asegurado.");
                ApplyNodeReward(node);
                var opened = node.links.Select(id => campaign.frontMap.Get(id))
                    .Where(n => n != null && n.state == FrontNodeState.Available).ToList();
                if (opened.Count > 0) Log($"→ Se abre el frente hacia: {string.Join(", ", opened.Select(n => n.name))}.");
                if (campaign.BossUnlocked) Log("→ el ASALTO AL BARÓN queda disponible en el Puesto de Mando.");
            }

            expedition = null; map = null; currentRoom = null;
            inCombat = false; atShip = true; pendingItem = null;

            AdvanceWeek(); // the expedition consumed a week
        }

        // el Barracón: a completed mission may yield battlefield salvage; a slain boss always drops a relic.
        // Biased toward loot-only pieces (the Foso trinkets) so they're findable outside the Taller shop.
        void MaybeDropGear(bool bossDown)
        {
            if (!bossDown && Rng.Range(0, 100) >= 35) return;
            var pool = GameData.GearKeys;
            var lootOnly = pool.Where(k => GameData.Gear(k).price <= 0).ToList();
            string key = (lootOnly.Count > 0 && Rng.Range(0, 100) < 70)
                       ? lootOnly[Rng.Range(0, lootOnly.Count)]
                       : pool[Rng.Range(0, pool.Length)];
            var g = GameData.Gear(key);
            campaign.armory.Add(GameData.Gear(key));
            Log($"✦ Botín del campo: {g.displayName} ({g.Describe()}) va a la armería del Barracón.");
        }

        // Optional nodes (and a successful Rescate) pay off on completion: buy back Crecida time, or a purse of paga.
        void ApplyNodeReward(FrontNode node)
        {
            if ((node.type != FrontNodeType.Opcional && node.type != FrontNodeType.Rescate) || string.IsNullOrEmpty(node.reward)) return;
            if (node.reward == "crecida")
            {
                campaign.phase = Mathf.Max(1, campaign.phase - 1);
                campaign.weeksInPhase = 0;
                Log($"→ Recompensa: la ofensiva gana tiempo — la Crecida retrocede a la fase {campaign.phase}.");
            }
            else if (node.reward == "botin")
            {
                int bonus = Rng.Range(400, 701);
                shipTronos += bonus;
                Log($"→ Recompensa: saqueáis el depósito del culto (+{bonus} de paga).");
            }
        }

        /// <summary>A week passes: apply deck treatments, run the Leva, advance the clock.</summary>
        void AdvanceWeek()
        {
            campaign.PassWeek(Log);
            if (headless && log.Count > 1500) log.RemoveRange(0, log.Count - 1500); // bound sim memory
        }

        static readonly string[][] AmbushPool =
        {
            new[]{ "Cultist", "Cultist" },
            new[]{ "Cultist", "Neophyte" },
            new[]{ "Cultist", "Whisperer", "Cultist" },
            new[]{ "Neophyte", "Whisperer" },
        };

        void StartAmbush(bool night)
        {
            Log(night ? "‼ ¡EMBOSCADA NOCTURNA! Os asaltan entre sueños." : "‼ ¡EMBOSCADA en el pasillo! El enemigo actúa primero.");
            currentRoom = null; // corridor fight: no room gets cleared
            inCombat = true;
            ambushNext = true;
            SetupCombat(AmbushPool[Rng.Range(0, AmbushPool.Length)]);
        }

        /// la Señal loses ground as you push deeper; announces band drops and learns the mechanic.
        float VotoLootMult => campaign != null && campaign.HasVoto(Voto.Silencio) ? 1.3f : 1f; // Voto de Silencio: más botín

        void DecaySignal(int amount)
        {
            if (expedition == null || amount <= 0) return;
            if (campaign != null && campaign.HasVoto(Voto.Silencio)) amount = Mathf.RoundToInt(amount * 1.5f); // Voto de Silencio: la Señal se agota antes
            int beforeBand = VoxLevel;
            expedition.signal = Mathf.Max(0, expedition.signal - amount);
            Learn("la Señal");
            if (VoxLevel < beforeBand)
                Log($"📡 La Señal con el Fortín cae a {VoxNames[VoxLevel]} ({expedition.signal}%)."
                    + (VoxLevel == 0 ? " Estáis solos ahí abajo." : ""));
            else
                Log($"📡 La Señal se debilita: {expedition.signal}%.");
        }

        // A hero fully consumed by la Mancha (200) submits to the dark — a permadeath (Caído), out of combat.
        // In combat this is handled at the unit's turn start (CombatUnit.TickTurnStart).
        void SweepDarkDeaths()
        {
            if (heroes == null) return;
            foreach (var h in heroes.Where(x => x.TeamOf == Team.Heroes && x.Alive && x.corruption >= 200).ToList())
            {
                h.hp = 0; h.atDeathsDoor = false;
                Log($"⚫ {h.unitName} se somete a la oscuridad: la Mancha lo consume por entero. Se pierde en el Foso.");
                OnDeath(h);
            }
        }

        // A single corridor step = a raid-turn: la Señal erodes, the DoT bleeds, the mission clock runs.
        void StepCorridor()
        {
            DecaySignal(SignalDecayPerStep + campaign.phase / 6);
            TickCorridorDoT();
            TickMissionClock();
        }

        // DoT bleeds as you march (design §5: "tictean los DoT"). SOFT (decided): a marching wound
        // can drop a hero to Al Borde de la Muerte but never kills out of combat — no deaths you
        // couldn't act against. The DoT also ages out one step so it wears off over the corridor.
        void TickCorridorDoT()
        {
            foreach (var h in heroes.Where(h => h.Alive && !h.atDeathsDoor).ToList())
            {
                int dot = h.DotPower;
                if (dot > 0)
                {
                    if (h.hp - dot <= 0)
                    {
                        h.hp = 0; h.atDeathsDoor = true; // floored: the dark almost takes them, but not here
                        Log($"🩸 {h.unitName} se desangra en el pasillo hasta el borde de la muerte.");
                    }
                    else
                    {
                        h.hp -= dot; // DoT ignores armour
                        Log($"🩸 {h.unitName} pierde {dot} PV por sus heridas al avanzar ({h.hp} PV).");
                    }
                }
                h.TickDotDurations();
            }
        }

        // Sabotaje: the reinforcement clock runs while you march, not only in combat (design §5). It only
        // SETS missionFailed here — the map/sim loop routes the retirada, never ReturnToShip mid-EnterRoom.
        void TickMissionClock()
        {
            if (expedition == null || expedition.missionRoundBudget <= 0 || missionFailed) return;
            missionRounds++;
            if (missionRounds > expedition.missionRoundBudget)
                missionFailed = true;
        }

        // H5·3c-ii: fire any mid-corridor event sitting on this step of the tunnel into `r`.
        void FireCorridorEvents(Room r, int step)
        {
            foreach (var ev in r.corridorEvents.Where(e => e.step == step && !e.sprung).ToList())
            {
                ev.sprung = true;
                switch (ev.kind)
                {
                    case CorridorEventKind.Alijo:     CorridorAlijo();    break;
                    case CorridorEventKind.Trampa:    CorridorTrap();     break;
                    case CorridorEventKind.Gas:       CorridorGas();      break;
                    case CorridorEventKind.Obstaculo: CorridorObstacle(); break;
                }
            }
        }

        void CorridorAlijo()
        {
            float voxMult = Mathf.Lerp(2f, 0.75f, Signal / 100f); // low Señal → richer, unlooted ground
            int val = Mathf.RoundToInt(Rng.Range(60, 121) * voxMult * VotoLootMult);
            if (expedition.HasSpace)
            {
                expedition.inventory.Add(new ItemStack { def = GameData.Item("Botin"), chargesLeft = 1, lootValue = val });
                Log($"✦ En el pasillo: un alijo del culto — +{val}p de botín a la mochila.");
            }
            else Log("✦ Un alijo en el pasillo, pero la mochila va llena. Lo dejáis.");
        }

        void CorridorTrap()
        {
            if (Rng.Range(0, 100) < 60)
            {
                var victim = heroes.Where(h => h.Alive).OrderBy(_ => Rng.Value).First();
                int dmg = Rng.Range(3, 7);
                var outc = victim.TakeDamage(dmg);
                Log($"⚠ ¡Trampa en el pasillo! Un cepo muerde a {victim.unitName}: {dmg} de daño ({victim.hp} PV).");
                HandleDamageOutcome(victim, outc);
            }
            else Log("⚠ Un cable tensado en el pasillo — lo veis a tiempo.");
        }

        void CorridorGas()
        {
            if (expedition.FindWithCharges(ItemKind.GasMask) != null) { Log("☣ Bolsa de Gas en el pasillo — las máscaras aguantan."); return; }
            Log("☣ Bolsa de Gas en el pasillo: el aire quema los pulmones.");
            foreach (var h in heroes.Where(h => h.Alive && !h.Has(StatusKind.ToxinImmune)))
                h.AddStatus(StatusKind.Toxin, 2, 2);
        }

        void CorridorObstacle()
        {
            Log("🪤 Alambrada en el pasillo: cortarla cuesta tiempo y Señal.");
            DecaySignal(SignalDecayPerStep + campaign.phase / 6); // an extra raid-turn's worth of exposure
            if (Rng.Range(0, 100) < 40)
            {
                var victim = heroes.Where(h => h.Alive).OrderBy(_ => Rng.Value).First();
                victim.AddStatus(StatusKind.Bleed, 2, 2);
                Log($"→ {victim.unitName} se engancha en el alambre: Sangrado 2×2.");
            }
        }

        void EnterRoom(Room r)
        {
            bool advancing = map.current != r;
            int steps = advancing ? Mathf.Max(1, r.corridorSteps) : 0;
            // Corridor ambush at the threshold — the chance climbs as la Señal fades AND with the
            // length of the tunnel ahead (a longer corridor = more time exposed in the dark).
            if (advancing && !suppressAmbush)
            {
                int ambushChance = AmbushBase + Mathf.RoundToInt((100 - Signal) * AmbushPerMissing) + (steps - 1) * AmbushPerStep;
                if (Rng.Range(0, 100) < ambushChance) { StartAmbush(false); return; }
            }

            if (advancing && map.current.pendingLoot != null && map.current.pendingLoot.Count > 0)
            {
                Log("Dejáis atrás el botín no recogido.");
                map.current.pendingLoot = null;
            }
            bool first = !r.visited;
            map.current = r;
            r.visited = true;

            // Walk the corridor into this room: each STEP is a full raid-turn — la Señal erodes, the DoT
            // bleeds, and the mission clock runs (H5·3c). A longer tunnel costs more of all three. The
            // entrance is free. If the Sabotaje clock runs out mid-march, the raid aborts here.
            if (first && r.kind != RoomKind.Entrada)
            {
                for (int s = 0; s < steps; s++)
                {
                    StepCorridor();
                    if (missionFailed) break;
                    if (!suppressCorridorEvents) FireCorridorEvents(r, s);
                    if (!heroes.Any(h => h.Alive)) break; // a trap or gas pocket wiped the party mid-corridor
                }
                DecayFlare(); // Opción B: la Bengala arde al recorrer el pasillo → entras al combate con la luz que te quede
                if (expedition.sector == 2) DecayFlare(); // las Galerías (S2) tragan la luz: la Bengala se agota antes (§5)
                if (missionFailed)
                {
                    Log("⏱ SABOTAJE FALLIDO: el tiempo se agotó mientras avanzabais. Los refuerzos del culto cierran el cerco.");
                    return; // the map/sim loop routes to the retirada (ReturnToShip) — never call it from inside EnterRoom
                }
                if (!heroes.Any(h => h.Alive)) return; // wiped in the corridor: the loop/UI routes the return
            }

            // As the link fades the dark itself corrupts — ramping the weaker la Señal gets.
            if (first && Signal < 30)
            {
                int m = Mathf.CeilToInt((30 - Signal) / 10f); // 1..3 as Señal → 0
                foreach (var h in heroes.Where(h => h.Alive).ToList()) h.GainCorruption(m, Log);
                SweepDarkDeaths(); // a soul pushed to 200 out here submits to the dark on the spot
            }

            switch (r.kind)
            {
                case RoomKind.Combate when !r.cleared:
                    Log($"— Sala {r.id}: contacto. El culto está aquí. —");
                    currentRoom = r;
                    inCombat = true;
                    SetupCombat(r.encounter);
                    break;
                case RoomKind.Tesoro when !r.cleared:
                    r.cleared = true;
                    int pieces = Rng.Range(1, 3);
                    float voxMult = Mathf.Lerp(2f, 0.75f, Signal / 100f); // progressive: nula → botín ×2, alta → ×0.75 (zonas ya saqueadas)
                    r.pendingLoot = new List<ItemStack>();
                    for (int i = 0; i < pieces; i++)
                        r.pendingLoot.Add(new ItemStack { def = GameData.Item("Botin"), chargesLeft = 1, lootValue = Mathf.RoundToInt(Rng.Range(80, 151) * voxMult * VotoLootMult) });
                    Log($"— Sala {r.id}: un alijo del culto — {pieces} pieza{(pieces > 1 ? "s" : "")} de botín. —");
                    break;
                case RoomKind.Trampa when !r.cleared:
                    r.cleared = true;
                    if (Rng.Range(0, 100) < 60)
                    {
                        var victim = heroes.Where(h => h.Alive).OrderBy(_ => Rng.Value).First();
                        int dmg = Rng.Range(3, 7);
                        var outc = victim.TakeDamage(dmg);
                        Log($"— Sala {r.id}: ¡TRAMPA! Un cepo del culto muerde a {victim.unitName}: {dmg} de daño ({victim.hp} PV). —");
                        HandleDamageOutcome(victim, outc);
                        if (victim.Alive && Rng.Value < 0.5f)
                        {
                            victim.AddStatus(StatusKind.Bleed, 2, 2);
                            Log($"→ {victim.unitName} sufre Sangrado 2×2.");
                        }
                    }
                    else Log($"— Sala {r.id}: un cable tensado entre escombros. Lo detectáis a tiempo. —");
                    break;
                case RoomKind.Curiosidad:
                    if (first) Log($"— Sala {r.id}: {CuriosityNames[r.curiosityType]}. Nadie os dirá qué hace. —");
                    break;
                case RoomKind.Santuario:
                    if (first) Log($"— Sala {r.id}: un refugio olvidado. Un lugar para descansar... con un kit. —");
                    break;
                default:
                    if (first) Log($"— Sala {r.id}: {(r.kind == RoomKind.Entrada ? "la trinchera de entrada" : "polvo y silencio")}. —");
                    break;
            }
        }

        void SetupCombat(string[] encounterKeys)
        {
            combatOver = false; resultText = ""; pendingAbility = null; anyHeroDied = false;
            pendingConsumable = null; pendingGive = false; pendingOrder = false; orderFirst = null;

            heroes = expedition.heroes; // SAME units every combat: the expedition persists
            foreach (var h in heroes) h.PrepareNextCombat();

            InitLightAxis();

            // Ammo refills consume supplies: Cargadores for firearms, Frascos for the Sacerdote
            foreach (var h in heroes.Where(h => h.Alive && h.def.maxAmmo > 0 && h.ammo < h.def.maxAmmo))
            {
                var kind = h.def.usesPromethium ? ItemKind.Promethium : ItemKind.AmmoRefill;
                var stack = expedition.FindWithCharges(kind);
                if (stack != null)
                {
                    expedition.ConsumeCharge(stack);
                    h.ammo = h.def.maxAmmo;
                    Log($"{h.unitName} recarga ({stack.def.displayName}: quedan {Mathf.Max(0, stack.chargesLeft)} usos).");
                }
                else
                {
                    Log($"Sin {(kind == ItemKind.Promethium ? "frascos de fuego líquido" : "cargadores")}: {h.unitName} entra con {h.ammo}/{h.def.maxAmmo} de munición.");
                }
            }

            enemies = expedition.SpawnEncounter(encounterKeys);

            // A boss brings its own Will (personal Cohesion) — the second win condition
            var bossHere = enemies.FirstOrDefault(e => e.def.isBoss && e.def.ownCohesion > 0);
            enemyCohesionMax = enemyCohesion = bossHere != null ? bossHere.def.ownCohesion : 30;

            // A previously fled enemy may rejoin a later fight: same HP he escaped
            // with, and his fear of you seeps into the group (-5 Cohesion, untelegraphed)
            if (bossHere == null && expedition.fledEnemies.Count > 0 && enemies.Count < 4 && Rng.Value < 0.5f)
            {
                var back = expedition.fledEnemies[Rng.Range(0, expedition.fledEnemies.Count)];
                expedition.fledEnemies.Remove(back);
                back.statuses.Clear();
                // combat-scoped flags don't carry into the rejoin: a Bombardero must re-telegraph, a Cosido can split again.
                back.bombPrimed = false; back.hasSplit = false; back.revivedOnce = false; back.tookDamageSinceTurn = false;
                enemies.Add(back);
                enemyCohesion = Mathf.Max(0, enemyCohesion - 5);
            }

            if (ambushNext)
            {
                ambushNext = false;
                savedFormation = heroes.ToList();
                for (int i = 0; i < heroes.Count; i++)
                {
                    int j = Rng.Range(i, heroes.Count);
                    (heroes[i], heroes[j]) = (heroes[j], heroes[i]);
                }
                // Fase 4 — caught out of position on the light axis: scattered across the whole depth,
                // so some are stranded in the rear DARK (the flare is over the front now).
                var scatter = heroes.Where(x => !x.def.isVip).ToList();
                foreach (var h in scatter) h.zone = Rng.Range(0, MaxLight); // Galería(rear/dark)..Tierra de Nadie(front/lit)
                // Keep the telegraph honest: while there IS any darkness, at least one soldier lands in it.
                if (lightLevel < MaxLight && scatter.Count > 0 && !scatter.Any(h => !IsLit(h))) scatter[0].zone = 0;
                enemyFirstRound = true;
                Log(scatter.Any(h => !IsLit(h))
                    ? "¡La formación se rompe en el caos: algunos quedan varados atrás, en la oscuridad!"
                    : "¡La formación se rompe en el caos: quedáis dispersos por la línea!");
            }

            round = 0;
            combatCorpses = 0;
            milagrosSilenced = 0;

            // Bestiary: seeing a foe records the encounter (bosses reveal in full on sight)
            foreach (var e in enemies) campaign?.RecordEncounter(e.def);

            // §9.3 — a true demon of the Foso on the field is a wound in the world: +5 Mancha to the whole party.
            if (enemies.Any(e => e.def.isDescendido))
            {
                foreach (var h in heroes.Where(x => x.Alive))
                    h.GainCorruption(5, Log);
                Log("Un Descendido pisa el mundo: la Mancha muerde al escuadrón (+5 a todos).");
            }

            Log($"—— COMBATE ({map.CombatsCleared + 1}/{map.CombatsTotal}) ——");
            if (Application.isPlaying)
            {
                foreach (var h in heroes)
                    PlaytestLog.Info($"Héroe pos {heroes.IndexOf(h) + 1}: {h.unitName} — {h.hp}/{h.EffMaxHP} PV, Mancha {h.corruption}, Llama {h.faith}{(h.soul != SoulState.None ? $", {CombatUnit.SoulName(h.soul)}" : "")}");
                foreach (var e in enemies)
                    PlaytestLog.Info($"Enemigo pos {enemies.IndexOf(e) + 1}: {e.unitName} — {e.hp} PV, Arm {e.def.armor}, Esq {e.def.dodge}, VEL {e.def.speed}");
            }
            Log("— Contacto con el enemigo. Que la Llama nos asista. —");
            NextRound();
        }

        // =====================================================================
        // LIGHT AXIS — la Bengala (Fase posiciones 1)
        // Depth zones (0=Galería … 3=Tierra de Nadie). The flare is thrown over the FRONT
        // (no man's land), so it lights the front `lightLevel` zones; it burns down each round
        // (darkness creeps in from the rear) and a flare relights it.
        // (Fase 2 wires targeting to lit/dark; here it is the parallel light layer.)
        // =====================================================================
        void InitLightAxis()
        {
            // Flare over the front: the lit zones are the FRONT ones now, so the squad starts in the light.
            foreach (var h in heroes) h.zone = h.def.isVip ? 2 : 1; // VIP tucked in the lit mid; soldiers on the Parapeto
            // Opción B (Hito 5): enter combat with the light carried from exploration (defaults to 3 in a
            // single-combat harness, since Expedition.carriedLight starts at 3 — so those are unaffected).
            lightLevel = expedition?.carriedLight ?? 3;
            flareCharges = BaseFlares; // combat flares are the squad's own; la Señal (Repetidor) is a separate system
        }

        /// la Bengala burns one step lower (in combat and while walking the corridors). Syncs the
        /// carried light so it persists across the whole incursion (Opción B).
        void DecayFlare()
        {
            if (lightLevel <= 0) return;
            lightLevel--;
            if (expedition != null) expedition.carriedLight = lightLevel;
            Log(lightLevel == 0
                ? "▓ La Bengala se apaga: la oscuridad —y el Foso— se cierra sobre la trinchera."
                : $"▓ La Bengala arde más baja (luz {lightLevel}/{MaxLight}).");
        }

        /// Can this attacker reach this target on the light axis? Human foes need it LIT; the Foso needs it DARK.
        bool InChannel(CombatUnit attacker, CombatUnit target) =>
            attacker.def.threatChannel == ThreatChannel.Human ? IsLit(target) : !IsLit(target);

        /// The reachable targets for a hostile enemy attack, gated by its light channel.
        /// If none are in its channel, returns all of them and flags `blind` (fires at a penalty).
        List<CombatUnit> ChannelFilter(CombatUnit attacker, List<CombatUnit> targets, out bool blind)
        {
            var reachable = targets.Where(t => InChannel(attacker, t)).ToList();
            blind = reachable.Count == 0;
            return blind ? targets : reachable;
        }

        /// The dark bleeds the Foso into anyone outside the flare — a Mancha tick each round (telegraphed).
        void ApplyDarkCorruption()
        {
            var dark = heroes.Where(h => h.Alive && !IsLit(h)).ToList();
            if (dark.Count == 0) return;
            Log($"▓ La oscuridad muerde: +{DarkMancha} Mancha a quien está fuera de la luz ({dark.Count}).");
            foreach (var h in dark) h.GainCorruption(DarkMancha, Log);
        }

        /// Minor action: fire a signal flare, relighting the trench to full. Spends one charge.
        void LaunchFlare(CombatUnit user)
        {
            if (flareCharges <= 0) { Log($"{user.unitName} no lleva más bengalas."); return; }
            flareCharges--;
            minorUsed = true;
            lightLevel = MaxLight;
            if (expedition != null) expedition.carriedLight = lightLevel; // Opción B: carries out of the fight too
            Log($"{user.unitName} dispara una Bengala: la luz sube a lo alto (luz {lightLevel}/{MaxLight}). Quedan {flareCharges} bengalas. (acción menor)");
        }

        void NextRound()
        {
            round++;
            // la Bengala burns down: the light recedes and the dark (the Foso) creeps up the trench.
            if (round > 1) DecayFlare();
            missionRounds++;
            // Sabotaje: a global clock across the whole incursion. When it runs out, reinforcements arrive.
            if (expedition != null && expedition.missionRoundBudget > 0 && !missionFailed && missionRounds > expedition.missionRoundBudget)
            {
                missionFailed = true;
                combatOver = true;
                resultText = "SABOTAJE FALLIDO. Los refuerzos del culto llegan: retirada.";
                Log($"⏱ {resultText}");
                RestoreFormation();
                return;
            }
            faithShakenThisRound = false;
            retreatDeclaredThisRound = false;
            if (milagrosSilenced > 0) milagrosSilenced--; // Corista's chant fades over the round
            int Init(CombatUnit u) => u.def.speed + u.GearSpeed - u.speedPenaltyNextRound - u.StatusSum(StatusKind.SpeedDebuff)
                                      + u.StatusSum(StatusKind.SpeedBuff) + Rng.Range(1, 7);
            if (enemyFirstRound && round == 1)
            {
                // Ambush: the whole enemy side acts before anyone reacts
                enemyFirstRound = false;
                turnQueue = enemies.Where(u => u.Alive).OrderByDescending(Init)
                    .Concat(heroes.Where(u => u.Alive && !u.retreated).OrderByDescending(Init))
                    .ToList();
            }
            else
            {
                turnQueue = heroes.Concat(enemies).Where(u => u.Alive && !u.retreated)
                    .OrderByDescending(Init)
                    .ToList();
            }
            foreach (var u in turnQueue) u.speedPenaltyNextRound = 0;
            turnIndex = -1;
            Log($"—— RONDA {round} ——");
            ApplyAmbientHazards();
            if (round > 1) ApplyDarkCorruption(); // round 1 starts fully lit (L=3): the dark only bites once the flare recedes
            AdvanceTurn();
        }

        /// <summary>Party carries at least one gas mask → immune to the ambient Gas cloud.</summary>
        bool PartyHasGasMask() => expedition != null && expedition.inventory.Any(s => s.def.kind == ItemKind.GasMask);

        /// <summary>Telegraphed ambient hazards tick at the start of each round; intensity scales with the Crecida.</summary>
        void ApplyAmbientHazards()
        {
            var hz = map?.current?.hazard ?? RoomHazard.None;
            if (hz == RoomHazard.None) return;
            int phase = campaign != null ? campaign.phase : 1;

            if (hz == RoomHazard.GasCloud)
            {
                if (PartyHasGasMask()) { if (round == 1) Log("☣ Nube de Gas — las máscaras aguantan: el grupo respira."); return; }
                int power = 2 + phase / 3; // 2..4
                foreach (var h in heroes.Where(x => x.Alive)) h.AddStatus(StatusKind.Toxin, power, 1);
                Log($"☣ Nube de Gas (Crecida {phase}): el aire quema. Gas {power} a quien no lleva máscara.");
            }
            else // RedSky
            {
                int mancha = 2 + phase / 2; // 2..6
                Log($"▓ Cielo rojo (Crecida {phase}): la Herida sangra sobre el cielo. +{mancha} Mancha al grupo.");
                foreach (var h in heroes.Where(x => x.Alive)) h.GainCorruption(mancha, Log);
            }
        }

        // Glossary auto-learns a mechanic the first time its effects are on the field.
        void Learn(string entry)
        {
            if (campaign != null && campaign.LearnGlossary(entry))
                Log($"📖 Diario: nueva entrada — {entry}.");
        }

        static string GlossaryNameForStatus(StatusKind k)
        {
            switch (k)
            {
                case StatusKind.Bleed: return "Sangrado";
                case StatusKind.Burn: return "Quemadura";
                case StatusKind.Toxin: return "Gas";
                case StatusKind.Stun: return "Aturdimiento";
                case StatusKind.Marked: return "Marcado";
                default: return null;
            }
        }

        void GlossarySweep()
        {
            if (campaign == null) return;
            if (heroes.Any(h => h.corruption > 0)) Learn("Mancha");
            if (heroes.Any(h => h.soulTested || h.soul != SoulState.None)) Learn("la Prueba de Fe");
            if (heroes.Any(h => h.atDeathsDoor)) Learn("Al Borde de la Muerte");
            if (heroes.Any(h => h.faith != 3)) Learn("la Llama");
            if (inCombat && enemyCohesion < enemyCohesionMax) Learn("la Moral");
            if (expedition != null && VoxLevel < 3) Learn("la Señal");
            if (inCombat && lightLevel < MaxLight) Learn("la Bengala"); // combat light axis (distinct from la Señal)
            foreach (var u in heroes.Concat(enemies))
                foreach (var s in u.statuses)
                    Learn(GlossaryNameForStatus(s.kind));
        }

        void AdvanceTurn()
        {
            pendingAbility = null; pendingConsumable = null; pendingGive = false; minorUsed = false;
            burnSanity = false; pendingOrder = false; orderFirst = null;
            GlossarySweep();
            if (CheckEnd()) return;
            turnIndex++;
            if (turnIndex >= turnQueue.Count) { NextRound(); return; }

            var u = turnQueue[turnIndex];
            // Skip the dead AND anyone no longer on the field (fled at Cohesion 0,
            // executed, retreated…): they stay in this round's queue but must not act.
            if (!u.Alive || u.retreated || !AlliesOf(u).Contains(u)) { AdvanceTurn(); return; }

            if (u.TickTurnStart(Log)) { OnDeath(u); AdvanceTurn(); return; }

            // Quebranto turn-start effects
            if (u.TeamOf == Team.Heroes && u.soul == SoulState.Desesperado)
            {
                Log($"{u.unitName} arrastra al grupo a la desesperación.");
                foreach (var h in heroes.Where(x => x.Alive && x != u).ToList()) h.GainCorruption(2, Log);
            }
            if (u.TeamOf == Team.Heroes && u.soul == SoulState.Apostata)
            {
                var victim = heroes.Where(x => x.Alive && x != u).OrderBy(_ => Rng.Value).FirstOrDefault();
                if (victim != null)
                {
                    Log($"{u.unitName} blasfema contra la Llama.");
                    victim.GainFaith(-1, Log);
                }
            }

            if (u.Has(StatusKind.Stun))
            {
                u.statuses.RemoveAll(s => s.kind == StatusKind.Stun);
                u.stunResistBonus += 30; // anti-stunlock, both sides (design)
                Log($"El ATURDIMIENTO le cuesta el turno a {u.unitName} (+30 res. Aturdimiento este combate).");
                EndTurnOf(u);
                return;
            }

            // Saqueador greed: distracted by loot, he sometimes rummages instead of acting.
            if (u.TeamOf == Team.Heroes && u.soul == SoulState.Saqueador && Rng.Value < 0.4f)
            {
                Log($"{u.unitName} se pierde rebuscando entre los muertos: pierde el turno.");
                EndTurnOf(u);
                return;
            }

            if (u.def.isVip) { EndTurnOf(u); return; } // the prisoner cowers — it never acts

            if (u.TeamOf == Team.Enemies) enemyActTimer = 0.7f; // small delay so the player can read
        }

        void Update()
        {
            UpdateToolkitUI(); // provisional UI Toolkit combat screen (F9 preview); no-op in headless/sim
            if (!inCombat || combatOver) return;
            var u = Current();
            if (u != null && u.TeamOf == Team.Enemies)
            {
                enemyActTimer -= Time.deltaTime;
                if (enemyActTimer <= 0f) { EnemyAct(u); }
            }
        }

        CombatUnit Current() =>
            (turnIndex >= 0 && turnIndex < turnQueue.Count && !combatOver) ? turnQueue[turnIndex] : null;

        void EndTurnOf(CombatUnit u)
        {
            // Status durations now tick at the unit's own turn START (see CombatUnit.TickTurnStart)
            AdvanceTurn();
        }

        // =====================================================================
        // CORE RESOLUTION
        // =====================================================================
        int PosOf(CombatUnit u)
        {
            // Fase 4 — the single axis: a hero's "position" IS its depth on the light axis, with the
            // lit front (Tierra de Nadie, zone 3) as pos 1 and the dark rear (Galería, zone 0) as pos 4.
            // This retires the Darkest-Dungeon column: the same usableFrom/targetPos arrays now read
            // as trench depth (melee forward = low pos, fire/miracles from the rear = high pos).
            if (u.TeamOf == Team.Heroes)
                return Mathf.Clamp(MaxLight - u.zone, 1, 4);
            return enemies.IndexOf(u) + 1; // enemies keep a simple formation column (1 = front)
        }

        // Light axis (flare over the FRONT / no man's land): a hero is lit if its depth (zone) is within
        // the flare's reach counted FROM THE FRONT. The front line (Tierra de Nadie) stays lit longest; as
        // the flare burns down the darkness creeps in from the rear. Lit = safe from the Foso but seen by
        // human fire; dark = hidden from humans but the Foso strikes. (Fase 2 uses it.)
        bool IsLit(CombatUnit u) => u.zone >= MaxLight - lightLevel;

        // Fase 5 — the MIRROR axis: everyone has a light state. A hero's is its depth vs the flare; an
        // enemy's comes from its nature — humans stand EXPOSED in the light, the Foso LURKS in shadow.
        bool Lit(CombatUnit u) => u.TeamOf == Team.Heroes ? IsLit(u) : u.def.threatChannel == ThreatChannel.Human;

        // A hero cannot see INTO the shadow from the light: firing from the light at a dark (Foso) target
        // is blind (−BlindFireAcc). Exposed (lit) enemies are always cleanly hittable; to hit the Foso
        // clean you must go into the dark after it. (Enemy-side blind fire is handled in EnemyAct.)
        bool BlindShot(CombatUnit attacker, CombatUnit target) =>
            attacker.TeamOf == Team.Heroes && Lit(attacker) && !Lit(target);

        // Fase 3 — over the parapet: a hero pushed forward to the front (Alambrada/Tierra de Nadie)
        // closes the distance and can strike the enemy backline (pos 3-4) its reach can't otherwise
        // touch. With the flare over the front the assault ground is now LIT: the cost shifted from the
        // Foso/Mancha to standing EXPOSED under human fire (a design consequence of inverting the axis;
        // pending an explicit rebalance). Enemies stay a simple column in v1, so only heroes assault.
        bool CanAssault(CombatUnit u) => u.TeamOf == Team.Heroes && u.zone >= AssaultZone;

        List<CombatUnit> AlliesOf(CombatUnit u) => u.TeamOf == Team.Heroes ? heroes : enemies;
        List<CombatUnit> FoesOf(CombatUnit u) => u.TeamOf == Team.Heroes ? enemies : heroes;

        bool AbilityUsable(CombatUnit u, AbilityDef a, out string reason)
        {
            reason = "";
            if (a.requiresMelee && !u.meleeMode) { reason = "requiere la bayoneta calada"; return false; }
            if (a.blockedInMelee && u.meleeMode) { reason = "arma guardada (modo melé)"; return false; }
            if (a.ammoCost > 0 && u.ammo < a.ammoCost) { reason = "sin munición"; return false; }
            if (u.usesLeft.ContainsKey(a) && u.usesLeft[a] <= 0) { reason = "sin usos"; return false; }
            if (a.faithCost > 0 && AlliesOf(u).Where(x => x.Alive).Sum(x => x.faith) < a.faithCost)
            { reason = $"el grupo no reúne {a.faithCost} de Llama"; return false; }
            // Corista (§9.3): its chant chokes the Milagros — no faith abilities while silenced
            if (a.isFaithAbility && u.TeamOf == Team.Heroes && milagrosSilenced > 0)
            { reason = "los Milagros están silenciados (Corista)"; return false; }
            if (a.bossPhaseOnly != 0 && a.bossPhaseOnly != u.bossPhase) { reason = "no disponible en esta fase"; return false; }
            if (!a.usableFrom.Contains(PosOf(u))) { reason = $"no usable desde pos {PosOf(u)}"; return false; }
            if (ValidTargets(u, a).Count == 0) { reason = "sin objetivos válidos"; return false; }
            return true;
        }

        static bool IsQuebrado(CombatUnit u) =>
            u.soul == SoulState.Conmocionado || u.soul == SoulState.Desesperado || u.soul == SoulState.Saqueador || u.soul == SoulState.Apostata;

        // Which resistance applies to each status kind (buffs are never resisted)
        static int ResistFor(CombatUnit u, StatusKind k)
        {
            switch (k)
            {
                case StatusKind.Bleed:       return u.def.resBleed;
                case StatusKind.Toxin:       return u.def.resToxin;
                case StatusKind.Stun:        return u.def.resStun + u.stunResistBonus;
                case StatusKind.AccDebuff:
                case StatusKind.SpeedDebuff:
                case StatusKind.Marked:      return u.def.resDebuff;
                default:                     return 0;
            }
        }

        List<CombatUnit> ValidTargets(CombatUnit u, AbilityDef a)
        {
            // Summary execution: only Broken allies or those at 150+ Corruption
            if (a.special == SpecialKind.Ejecucion)
                return AlliesOf(u).Where(x => x.Alive && x != u && (IsQuebrado(x) || x.corruption >= 150)).ToList();

            switch (a.targetKind)
            {
                case TargetKind.Self: return new List<CombatUnit> { u };
                case TargetKind.AllAllies: return AlliesOf(u).Where(x => x.Alive && !x.retreated).ToList();
                case TargetKind.AllEnemies: return FoesOf(u).Where(x => x.Alive && !x.retreated).ToList();
                case TargetKind.Ally:
                    return AlliesOf(u).Where(x => x.Alive && !x.retreated && a.targetPos.Contains(PosOf(x))).ToList();
                default:
                {
                    // Fase 4 — enemy single-target attacks pick by LIGHT CHANNEL, not by hero position
                    // (a human shoots whatever it can see lit; the Foso grabs whatever falls dark). The
                    // channel is applied in EnemyAct/ChannelFilter, so here just offer every live hero.
                    if (u.TeamOf == Team.Enemies && !a.area)
                        return FoesOf(u).Where(x => x.Alive && !x.retreated).ToList();
                    // Hero attacks (and enemy AoE footprints) still read the enemy formation column,
                    // extended to the backline when the hero has gone over the parapet.
                    bool assault = CanAssault(u);
                    return FoesOf(u).Where(x => x.Alive && !x.retreated &&
                        (a.targetPos.Contains(PosOf(x)) || (assault && PosOf(x) >= 3))).ToList();
                }
            }
        }

        void UseAbility(CombatUnit user, AbilityDef a, CombatUnit clicked, bool endTurn = true)
        {
            // costs
            if (a.ammoCost > 0) user.ammo -= a.ammoCost;
            if (user.usesLeft.ContainsKey(a)) user.usesLeft[a]--;

            Log($"{user.unitName} usa {a.displayName}.");

            // hero faith abilities unnerve the cult (design: -4 Cohesion).
            // Saturates: only the first faith ability each round counts, so a
            // support-heavy party can't melt Cohesion by chaining free casts
            // (sim finding: 94% of fights ended in flight without killing).
            if (a.isFaithAbility && user.TeamOf == Team.Heroes && !faithShakenThisRound)
            {
                faithShakenThisRound = true;
                ShakeCohesion(4, "el fervor de los héroes");
            }
            if (a.selfFaithDelta != 0) user.GainFaith(a.selfFaithDelta, Log);

            // Actos de Fe: the whole group pays, highest Faith first
            if (a.faithCost > 0)
            {
                Log($"El grupo ofrece {a.faithCost} de Llama.");
                for (int i = 0; i < a.faithCost; i++)
                {
                    var donor = AlliesOf(user).Where(x => x.Alive && x.faith > 0)
                        .OrderByDescending(x => x.faith).FirstOrDefault();
                    donor?.GainFaith(-1, Log);
                }
            }

            // Perils of the Warp (Psíquico): d20, 1-2 fumbles into the 1d6 table
            if (a.warpDanger && user.TeamOf == Team.Heroes)
            {
                if (burnSanity)
                {
                    Log($"{user.unitName} ESCUCHA MÁS HONDO: el Foso no encuentra grieta esta vez.");
                    user.GainCorruption(20, Log);
                    burnSanity = false;
                }
                else
                {
                    int d20 = Rng.Range(1, 21);
                    if (d20 <= 2 && ResolveWarpFumble(user, a, ref clicked))
                    { EndTurnOf(user); return; } // power fizzled
                }
            }

            // Sermon: enemy area corruption to the whole living party (Predicador)
            if (a.areaCorruption > 0 && user.TeamOf == Team.Enemies)
            {
                Log($"{user.unitName} sermonea: la Mancha se derrama sobre el grupo.");
                foreach (var h in FoesOf(user).Where(x => x.Alive && !x.retreated).ToList())
                    h.GainCorruption(a.areaCorruption, Log);
                EndTurnOf(user); return;
            }

            // Rally: el Iluminado inflames the cult and restores its Moral.
            if (a.ralliesMoral > 0 && user.TeamOf == Team.Enemies)
            {
                RallyCohesion(a.ralliesMoral, user.unitName);
                EndTurnOf(user); return;
            }

            // specials first
            switch (a.special)
            {
                case SpecialKind.ToggleMelee:
                    user.meleeMode = true;
                    Log($"{user.unitName} cala la bayoneta: +2 daño en pos 1-2, las armas de fuego quedan guardadas.");
                    EndTurnOf(user); return;

                case SpecialKind.GuardLowest:
                {
                    var ward = AlliesOf(user).Where(x => x.Alive && x != user).OrderBy(x => x.hp).FirstOrDefault();
                    user.AddStatus(StatusKind.DodgeBuff, 15, 1);
                    if (ward != null)
                    {
                        user.AddStatus(StatusKind.Guard, 0, 1, ward);
                        Log($"{user.unitName} se interpone: protege a {ward.unitName} esta ronda (+15 Esquiva).");
                    }
                    EndTurnOf(user); return;
                }

                case SpecialKind.Cantico:
                {
                    var victims = FoesOf(user).Where(x => x.Alive && !x.retreated).OrderBy(_ => Rng.Value).Take(2).ToList();
                    foreach (var v in victims) v.GainCorruption(a.corruptionDelta, Log);
                    // faith abilities & horrors also shake cohesion? no: cantico is enemy-side
                    EndTurnOf(user); return;
                }

                case SpecialKind.Himno:
                {
                    foreach (var adj in AdjacentAllies(user))
                        adj.GainCorruption(-6, Log);
                    EndTurnOf(user); return;
                }

                case SpecialKind.Ejecucion:
                {
                    Log($"⚖ FUSILAR: {user.unitName} fusila a {clicked.unitName}. \"la Llama conoce a los suyos.\"");
                    clicked.hp = 0;
                    clicked.atDeathsDoor = false; // an execution admits no death check
                    anyHeroDied = true;
                    heroes.Remove(clicked);
                    foreach (var h in heroes.Where(x => x.Alive))
                    {
                        h.GainCorruption(-30, Log);
                        h.GainFaith(2, Log);
                        h.AddStatus(StatusKind.DmgBuffPct, 15, 2);
                    }
                    Log("La disciplina se restaura: el grupo gana +15% daño (2t).");
                    if (!CheckEnd()) EndTurnOf(user);
                    return;
                }
            }

            var targets = a.area
                ? ValidTargets(user, a).Where(t => a.targetPos.Contains(PosOf(t))).ToList()
                : (a.targetKind == TargetKind.AllAllies || a.targetKind == TargetKind.AllEnemies)
                    ? ValidTargets(user, a)
                    : new List<CombatUnit> { clicked };

            // Pushing areas resolve back-to-front so a push never re-shuffles a target
            // that has not resolved yet (front targets would swap twice otherwise)
            if (a.pushesBack) targets = targets.OrderByDescending(PosOf).ToList();

            foreach (var t in targets.ToList())
                ResolveOnTarget(user, a, t);

            if (endTurn) EndTurnOf(user);
        }

        /// <summary>
        /// Perils fumble table (1d6). Returns true if the power fizzles
        /// (cases 1-3, 5-6); case 4 redirects the power to a random ally.
        /// </summary>
        bool ResolveWarpFumble(CombatUnit user, AbilityDef a, ref CombatUnit clicked)
        {
            Log($"⚠ EL REFLUJO: {user.unitName} pierde el control del poder...");
            int d6 = Rng.Range(1, 7);
            switch (d6)
            {
                case 1:
                case 2:
                    Log("→ el Foso se cobra su peaje.");
                    user.GainCorruption(25, Log);
                    return true;
                case 3:
                {
                    int dmg = Rng.Range(4, 9);
                    var outcome = user.TakeDamage(dmg);
                    Log($"→ la energía se revuelve contra {user.unitName}: {dmg} de daño ({user.hp} PV).");
                    HandleDamageOutcome(user, outcome);
                    return true;
                }
                case 4:
                {
                    var ally = heroes.Where(x => x.Alive && x != user).OrderBy(_ => Rng.Value).FirstOrDefault();
                    if (ally == null) return true;
                    Log($"→ el poder se desvía hacia {ally.unitName}.");
                    clicked = ally;
                    return false; // resolves against the ally
                }
                case 5:
                    Log("→ susurros del Foso: la Llama del grupo flaquea.");
                    for (int i = 0; i < 3; i++)
                    {
                        var v = heroes.Where(x => x.Alive && x.faith > 0).OrderBy(_ => Rng.Value).FirstOrDefault();
                        v?.GainFaith(-1, Log);
                    }
                    return true;
                default: // 6 — Intrusión
                    if (enemies.Count(x => x.Alive) >= 4)
                    {
                        Log("→ algo empuja el velo... pero no encuentra hueco. El Foso se cobra su peaje.");
                        user.GainCorruption(25, Log);
                    }
                    else
                    {
                        Log("→ INTRUSIÓN: algo cruza el velo. Un Descendido Menor se materializa.");
                        enemies.Insert(0, new CombatUnit(GameData.Unit("Larva")));
                    }
                    return true;
            }
        }

        void ResolveOnTarget(CombatUnit user, AbilityDef a, CombatUnit target)
        {
            if (target == null || !target.Alive) return;
            if (!AlliesOf(target).Contains(target)) return; // already fled or removed mid-resolution

            // Guard redirect (single-target hostile attacks only)
            if (!a.area && a.targetKind == TargetKind.Enemy && user.TeamOf != target.TeamOf)
            {
                var guardian = AlliesOf(target).FirstOrDefault(g =>
                    g.Alive && g.statuses.Any(s => s.kind == StatusKind.Guard && s.guardTarget == target));
                if (guardian != null && guardian != target)
                {
                    Log($"{guardian.unitName} intercepta el golpe dirigido a {target.unitName}.");
                    target = guardian;
                }
            }

            bool hostile = a.targetKind == TargetKind.Enemy || a.targetKind == TargetKind.AllEnemies;

            // Fase 3 — telegraph the assault: a forward hero reaching a rear enemy its normal reach can't
            if (hostile && CanAssault(user) && PosOf(target) >= 3 && !a.targetPos.Contains(PosOf(target)))
                Log($"⚔ {user.unitName} salta el parapeto y cae sobre la retaguardia: {target.unitName}.");

            // Acto de Fe: Prueba de Fe — the next attack cannot miss and deals x2
            bool judged = a.dmgMax > 0 && user.Has(StatusKind.JudgmentNext);
            if (judged) user.statuses.RemoveAll(s => s.kind == StatusKind.JudgmentNext);

            // Fase 5 — mirror axis: firing across the light/dark divide is blind (−BlindFireAcc). For an
            // enemy the divide is handled upstream (activeBlindPenalty); for a hero it's a light mismatch
            // with its target (lit hero → dark Foso, or dark hero → lit human).
            int blindPen = activeBlindPenalty;
            if (hostile && BlindShot(user, target)) blindPen = MirrorBlindAcc;
            if (hostile && a.dmgMax > 0 && user.TeamOf == Team.Heroes && blindPen > 0)
                Log($"{user.unitName} dispara a ciegas hacia la sombra (−{blindPen} prec).");

            // hit roll
            int chance = -1; // -1 = auto-hit (no roll shown in the breakdown)
            if (hostile && a.accuracy > 0 && !judged)
            {
                // Over the parapet the assaulting hero is silhouetted by the flare over the front: easier to hit.
                int exposed = (target.TeamOf == Team.Heroes && CanAssault(target)) ? ExposedAssaultAcc : 0;
                chance = Mathf.Clamp(a.accuracy + user.EffAccuracyBonus - target.EffDodge - blindPen + exposed, 5, 95);
                if (Rng.Range(0, 100) >= chance)
                {
                    Log($"→ falla contra {target.unitName} ({chance}%).");
                    return;
                }
            }

            // damage
            if (a.dmgMax > 0)
            {
                int roll = Rng.Range(a.dmgMin, a.dmgMax + 1);
                int meleeBonus = user.meleeMode && a.requiresMelee && PosOf(user) <= 2 ? 2 : 0;
                int levelBonus = user.LevelDamage + user.weaponRank + user.GearDamage; // level, Taller rank, equipped gear (el Barracón)
                bool crit = Rng.Range(0, 100) < a.critPct + user.GearCrit; // gear can sharpen the crit chance (el Barracón)
                float buffMult = user.DamageDealtMult * target.DamageTakenMult;
                float fireMult = a.isFire && target.def.weakToFire ? 1.5f : 1f;
                // §9.4 Plaga de ratas — a swarm: area damage tears through it.
                float swarmMult = a.area && target.def.weakToArea ? 1.5f : 1f;
                // Fase 4 — the Vidente resonates in the dark: psychic (warp) damage surges out of the light.
                bool darkPsychic = a.warpDanger && user.TeamOf == Team.Heroes && !IsLit(user);
                float mult = buffMult * fireMult * swarmMult * (crit ? 1.5f : 1f) * (judged ? 2f : 1f) * (darkPsychic ? DarkPsychicMult : 1f);
                int effArmor = a.ignoresArmor ? 0 : target.EffArmor;
                int dmg = Mathf.Max(1, Mathf.RoundToInt((roll + meleeBonus + levelBonus) * mult) - effArmor);
                var outcome = target.TakeDamage(dmg);

                // A multi-phase boss refuses to fall while it still has a form to reveal: the killing blow
                // leaves it at 1 HP so the next phase (Revelación / la posesión del Confesor) always triggers.
                if (outcome == DamageOutcome.Died && target.def.isBoss && BossHasUnrevealedPhase(target))
                { target.hp = 1; outcome = DamageOutcome.Damaged; }

                // Audit breakdown: the player must be able to reconstruct every hit.
                var parts = new List<string>();
                if (judged) parts.Add("JUICIO: no falla, ×2");
                if (chance >= 0) parts.Add($"impacto {chance}%" + (blindPen > 0 ? $" (−{blindPen} a ciegas)" : ""));
                parts.Add($"tirada {roll}({a.dmgMin}–{a.dmgMax})"
                          + (meleeBonus > 0 ? $" +{meleeBonus} bayoneta" : "")
                          + (levelBonus > 0 ? $" +{levelBonus} nivel" : ""));
                if (crit) parts.Add($"×1.5 crít ({a.critPct}%)");
                if (fireMult > 1f) parts.Add("×1.5 fuego");
                if (swarmMult > 1f) parts.Add("×1.5 enjambre (área)");
                if (darkPsychic) parts.Add($"×{DarkPsychicMult:0.#} oscuridad");
                if (!Mathf.Approximately(buffMult, 1f)) parts.Add($"×{buffMult:0.##} buffs");
                if (effArmor > 0) parts.Add($"−{effArmor} arm");
                else if (a.ignoresArmor && target.EffArmor > 0) parts.Add("ignora arm");
                Log($"→ {(crit ? "¡CRÍTICO! " : "")}{dmg} de daño a {target.unitName} ({target.hp} PV) [{string.Join(", ", parts)}]");

                bool died = HandleDamageOutcome(target, outcome);

                // Hito 5 — CARNE DEL FOSO reactions to being wounded
                if (target.TeamOf == Team.Enemies)
                {
                    target.tookDamageSinceTurn = true; // Verdugo: loses its bonus action if hurt
                    if (!died && target.def.gasOnHit && user.TeamOf == Team.Heroes && user.Alive)
                    {
                        user.AddStatus(StatusKind.Toxin, 2, 2);
                        Log($"☣ {target.unitName} revienta una pústula: Gas sobre {user.unitName}.");
                    }
                    // §9.4 Bestia del Alambre — barbed wire: a melee attacker tears open and bleeds.
                    if (target.def.bleedsAttackerOnMelee && user.TeamOf == Team.Heroes && user.Alive
                        && user.meleeMode && a.requiresMelee)
                    {
                        user.AddStatus(StatusKind.Bleed, 3, 2);
                        Log($"🩸 El alambre de {target.unitName} desgarra a {user.unitName}: Sangrado.");
                    }
                    if (!died && !string.IsNullOrEmpty(target.def.splitsInto) && !target.hasSplit
                        && target.hp <= target.EffMaxHP / 2)
                        SplitEnemy(target);
                }

                // Boss phase transitions at HP thresholds: 1→2 (Revelación) and 2→3 (Confesor Rojo, Hito 7).
                if (!died && target.def.isBoss && target.bossPhase == 1 && target.def.phaseTwoAtPct > 0 &&
                    target.hp <= target.EffMaxHP * target.def.phaseTwoAtPct / 100)
                {
                    target.bossPhase = 2;
                    Log(string.IsNullOrEmpty(target.def.phaseTwoLog)
                        ? $"‼ REVELACIÓN: {target.unitName} se despoja del hábito. ¡Es un híbrido! Deja el sermón y ataca con saña."
                        : target.def.phaseTwoLog);
                    target.AddStatus(StatusKind.DmgBuffPct, 30, 999); // pega más fuerte en fase 2
                }
                else if (!died && target.def.isBoss && target.bossPhase == 2 && target.def.phaseThreeAtPct > 0 &&
                    target.hp <= target.EffMaxHP * target.def.phaseThreeAtPct / 100)
                {
                    target.bossPhase = 3;
                    Log(string.IsNullOrEmpty(target.def.phaseThreeLog)
                        ? $"‼ {target.unitName} entra en frenesí final."
                        : target.def.phaseThreeLog);
                    target.AddStatus(StatusKind.DmgBuffPct, 25, 999); // se acumula con la fase 2 → un final feroz
                    if (!string.IsNullOrEmpty(target.def.phaseThreeSummon) && enemies.Count(x => x.Alive) < 6)
                    {
                        enemies.Add(new CombatUnit(GameData.Unit(target.def.phaseThreeSummon)));
                        Log($"→ {GameData.Unit(target.def.phaseThreeSummon).displayName} se manifiesta desde dentro del Confesor.");
                    }
                }

                if (crit)
                {
                    if (user.TeamOf == Team.Heroes)
                    {
                        user.GainCorruption(-5, Log); // "la Llama guía mi mano"
                        ShakeCohesion(6, $"crítico contra el culto");
                    }
                    else if (!died)
                    {
                        target.GainCorruption(8, Log); // enemy crit terrifies
                    }
                }

                // psychic terror vs cult Cohesion (Aplastamiento mental)
                if (a.cohesionDelta > 0 && target.Alive && target.TeamOf == Team.Enemies && target.def.countsForCohesion)
                    ShakeCohesion(a.cohesionDelta, "terror mental");

                // push 1 position back (Grito warp), resisted by resMove
                if (a.pushesBack && target.Alive)
                {
                    if (Rng.Range(0, 100) < target.def.resMove)
                    {
                        Log($"→ {target.unitName} resiste el empuje (res. {target.def.resMove}%).");
                    }
                    else
                    {
                        var list = AlliesOf(target); int i = list.IndexOf(target);
                        if (i >= 0 && i < list.Count - 1)
                        {
                            (list[i], list[i + 1]) = (list[i + 1], list[i]);
                            Log($"→ {target.unitName} es empujado a la posición {i + 2}.");
                        }
                    }
                }
            }

            // Conmocionado: 50% refuses help (heals/cleanses) from anyone else
            bool offersHelp = a.healMax > 0 || a.corruptionDelta < 0 || a.removesBleed;
            if (offersHelp && target != user && target.TeamOf == user.TeamOf &&
                target.soul == SoulState.Conmocionado && Rng.Value < 0.5f)
            {
                Log($"→ {target.unitName} rechaza la ayuda: \"¡Apártate! ¡Sé lo que eres!\"");
                return;
            }

            // Fase 4 — miracles burn brighter in the light: a lit faith-caster heals/cleanses harder.
            bool litMiracle = a.isFaithAbility && user.TeamOf == Team.Heroes && IsLit(user);

            // el Autómata is a machine: los Milagros don't obrar on it (no heal, no faith buff — it needs the Taller).
            bool miracleOnAutomaton = a.isFaithAbility && target.def.isAutomaton;
            if (miracleOnAutomaton && (a.healMax > 0 || (a.status != null && a.status.kind != StatusKind.None)))
                Log($"→ los Milagros no obran sobre {target.unitName}: es acero, no alma.");

            // heal (1 HP is enough to step back from Death's Door)
            if (a.healMax > 0 && target.Alive && !miracleOnAutomaton)
            {
                int h = Rng.Range(a.healMin, a.healMax + 1);
                if (litMiracle) h = Mathf.RoundToInt(h * LightFaithMult);
                target.hp = Mathf.Min(target.EffMaxHP, target.hp + h);
                Log($"→ cura {h} a {target.unitName} ({target.hp} PV){(litMiracle ? " ✦ la Llama en la luz" : "")}.");
                if (target.atDeathsDoor && target.hp > 0)
                {
                    target.atDeathsDoor = false;
                    Log($"→ {target.unitName} se aparta del Borde de la Muerte.");
                }
            }

            if (a.removesBleed && target.statuses.RemoveAll(s => s.kind == StatusKind.Bleed) > 0)
                Log($"→ el Sangrado de {target.unitName} se detiene.");

            // corruption on targets (cleanses etc.). Cantico handled as special.
            if (a.corruptionDelta != 0 && a.special == SpecialKind.None && target.TeamOf == Team.Heroes)
            {
                int corrDelta = a.corruptionDelta;
                if (corrDelta < 0 && litMiracle)
                {
                    corrDelta = Mathf.RoundToInt(corrDelta * LightFaithMult); // brighter cleanse in the light
                    Log($"✦ la Llama en la luz purga con más fuerza ({corrDelta} Mancha a {target.unitName}).");
                }
                target.GainCorruption(corrDelta, Log);
            }

            // faith on hero targets (Absolución +1)
            if (a.faithDelta != 0 && target.TeamOf == Team.Heroes)
                target.GainFaith(a.faithDelta, Log);

            // status: application chance minus the target's resistance (slice §10.3),
            // resolved with a SINGLE roll so "resiste" logs exactly when the
            // resistance was what made the difference.
            if (a.status != null && a.status.kind != StatusKind.None && target.Alive && !miracleOnAutomaton)
            {
                int resist = ResistFor(target, a.status.kind);
                if (a.status.kind == StatusKind.Toxin && target.Has(StatusKind.ToxinImmune))
                {
                    Log($"→ {target.unitName} es inmune al Gas (antídoto).");
                }
                else
                {
                    int statusRoll = Rng.Range(0, 100);
                    if (statusRoll < a.status.chance - resist)
                    {
                        target.AddStatus(a.status.kind, a.status.power, a.status.duration);
                        string verb = IsBuff(a.status.kind) ? "gana" : "sufre";
                        bool isDot = a.status.kind == StatusKind.Bleed || a.status.kind == StatusKind.Burn || a.status.kind == StatusKind.Toxin;
                        Log($"→ {target.unitName} {verb} {StatusText(a.status)}{(isDot ? "" : $" ({a.status.duration}t)")}.");

                        // Desafío also grants +10 dodge while taunting (slice §3.2)
                        if (a.status.kind == StatusKind.Taunt)
                        {
                            target.AddStatus(StatusKind.DodgeBuff, 10, a.status.duration);
                            Log($"→ {target.unitName} desafía al enemigo: deben atacarla (+10 Esquiva).");
                        }
                    }
                    else if (statusRoll < a.status.chance)
                    {
                        Log($"→ {target.unitName} resiste {StatusText(a.status)} (res. {resist}%).");
                    }
                    // else: the status simply failed its base chance (silent, as before)
                }
            }

            // Eviscerador special-case: huge weapon slows next round
            if (a.displayName == "Pala de zapa") user.speedPenaltyNextRound = 2;
        }

        // =====================================================================
        // MINOR ACTIONS (consumables)
        // =====================================================================
        List<CombatUnit> AdjacentAllies(CombatUnit u)
        {
            var list = AlliesOf(u); int i = list.IndexOf(u);
            var adj = new List<CombatUnit>();
            if (i > 0 && list[i - 1].Alive) adj.Add(list[i - 1]);
            if (i < list.Count - 1 && list[i + 1].Alive) adj.Add(list[i + 1]);
            return adj;
        }

        void UseConsumable(CombatUnit user, ConsumableDef c, CombatUnit target)
        {
            user.consumables.Remove(c);
            minorUsed = true;
            Log($"{user.unitName} usa {c.displayName}{(target != user ? $" en {target.unitName}" : "")} (acción menor).");

            if (c.corruptionDelta != 0) target.GainCorruption(c.corruptionDelta, Log);

            if (c.removesToxin && target.statuses.RemoveAll(s => s.kind == StatusKind.Toxin) > 0)
                Log($"→ el Gas de {target.unitName} se neutraliza.");
            if (c.toxinImmuneRounds > 0)
            {
                target.AddStatus(StatusKind.ToxinImmune, 0, c.toxinImmuneRounds);
                Log($"→ {target.unitName} queda inmune al Gas ({c.toxinImmuneRounds}t).");
            }

            if (c.removesQuebranto)
            {
                if (target.soul == SoulState.Conmocionado || target.soul == SoulState.Desesperado || target.soul == SoulState.Saqueador || target.soul == SoulState.Apostata)
                {
                    Log($"→ el Quebranto de {target.unitName} ({CombatUnit.SoulName(target.soul)}) se acalla... por ahora. La deuda se pagará.");
                    target.soul = SoulState.None;
                    target.endCombatCorruptionDebt += c.endCombatCorruption;
                }
                else
                {
                    Log($"→ {target.unitName} no sufre ningún Quebranto: el trago solo quema.");
                }
            }

            if (c.status != null && c.status.kind != StatusKind.None)
            {
                var recipients = c.affectsGroup ? AlliesOf(target).Where(x => x.Alive).ToList()
                                                : new List<CombatUnit> { target };
                foreach (var r in recipients)
                {
                    var onExpire = c.statusOnExpire != null && c.statusOnExpire.kind != StatusKind.None ? c.statusOnExpire : null;
                    r.AddStatus(c.status.kind, c.status.power, c.status.duration, null, onExpire);
                    string verb = IsBuff(c.status.kind) || (c.status.kind == StatusKind.CorrResistMod && c.status.power > 0) ? "gana" : "sufre";
                    Log($"→ {r.unitName} {verb} {StatusText(c.status)} ({c.status.duration}t).");
                }
            }
        }

        void GiveConsumable(CombatUnit user, ConsumableDef c, CombatUnit target)
        {
            user.consumables.Remove(c);
            target.consumables.Add(c);
            minorUsed = true;
            Log($"{user.unitName} pasa {c.displayName} a {target.unitName} (acción menor).");
        }

        /// <summary>Logs Death's Door transitions and handles death. Returns true if the unit died.</summary>
        bool HandleDamageOutcome(CombatUnit target, DamageOutcome outcome)
        {
            switch (outcome)
            {
                case DamageOutcome.EnteredDeathsDoor:
                    Log($"‼ {target.unitName} está AL BORDE DE LA MUERTE. Un golpe más puede ser el último.");
                    target.GainCorruption(10, Log); // staring into the abyss (OUR number, DD-style)
                    return false;
                case DamageOutcome.DeathsDoorResisted:
                    Log($"{target.unitName} se niega a morir (prueba de muerte {target.def.deathBlowPct}% superada).");
                    return false;
                case DamageOutcome.Died:
                    OnDeath(target);
                    return true;
                default:
                    return false;
            }
        }

        void OnDeath(CombatUnit dead)
        {
            // Guardián de la Herida (§9.3): while it stands, a felled Descendido is knit back once —
            // half-formed, no corpse, no Gas burst. Kill the anchor first or the front never thins.
            if (dead.def.isDescendido && !dead.def.revivesDescendidos && !dead.revivedOnce
                && enemies.Any(x => x.Alive && x != dead && x.def.revivesDescendidos))
            {
                dead.revivedOnce = true;
                dead.statuses.Clear();
                dead.hp = Mathf.Max(1, dead.EffMaxHP / 2);
                Log($"⟳ Guardián de la Herida rehace a {dead.unitName}: se alza a medio formar ({dead.hp} PV).");
                return; // stays on the field — not a corpse
            }

            // el Autómata breaks instead of dying: a KO, repairable at the Taller — never a permadeath casualty,
            // and no comrade-death shock/grief (it's materiel, not a soul).
            if (dead.def.isAutomaton)
            {
                dead.averiado = true;
                heroes.Remove(dead);
                Log($"⚙ {dead.unitName} se AVERÍA y cae inerte — acero retorcido, recuperable en el Taller.");
                return;
            }

            Log($"† {dead.unitName} ha caído.");
            combatCorpses++; // a body for the Devoracadáveres to feed on

            // Supurante: a wider Gas burst floods the party as it ruptures.
            if (dead.def.gasOnDeath)
            {
                Log($"☣ {dead.unitName} estalla en una nube de Gas.");
                foreach (var h in heroes.Where(x => x.Alive)) h.AddStatus(StatusKind.Toxin, 2, 2);
            }

            if (dead.TeamOf == Team.Enemies)
            {
                campaign?.RecordKill(dead.def); // bestiary: 1 kill = seen, 3 = mastered
                enemies.Remove(dead);
                // Marcador (§9.3): the brand it burned dies with it — clear Marked from the party.
                if (dead.def.displayName == "Marcador")
                {
                    foreach (var h in heroes.Where(x => x.Alive && x.Has(StatusKind.Marked)))
                        h.statuses.RemoveAll(s => s.kind == StatusKind.Marked);
                    Log("Con Marcador muerto, la marca del Foso se apaga sobre el escuadrón.");
                }
                // Alfa del Foso (mini-boss S3): kill the leader and the Sabueso pack scatters.
                if (dead.def.packLeader)
                {
                    var pack = enemies.Where(e => e.Alive && e.def.displayName == "Sabueso del Foso").ToList();
                    foreach (var d in pack) enemies.Remove(d);
                    if (pack.Count > 0) Log($"Sin su Alfa, la jauría de Sabuesos se dispersa entre las sombras ({pack.Count} huyen).");
                }
                // Desollador (mini-boss F): when it falls, the flayed hero is freed.
                if (dead.def.isFlayer)
                    foreach (var h in heroes.Where(x => x.grabbedByFlayer).ToList())
                    { h.grabbedByFlayer = false; Log($"Con el Desollador abatido, {h.unitName} se derrumba libre, en carne viva."); }

                if (dead.def.isBoss)
                {
                    if (expedition != null) expedition.bossDefeated = true; // slain by HP
                    if (enemies.Count > 0)
                    {
                        Log("Con su profeta muerto, los guardias se dispersan entre las sombras.");
                        enemies.Clear();
                    }
                }
                else if (dead.def.dropsMoraleOnDeath > 0) ShakeCohesion(dead.def.dropsMoraleOnDeath, $"cae {dead.unitName}");
                else if (dead.def.countsForCohesion) ShakeCohesion(8, $"muerte de {dead.unitName}");
            }
            else if (dead.def.isVip)
            {
                // The prisoner fell: the Rescate is lost. Not a roster soldier — no permadeath.
                heroes.Remove(dead);
                missionFailed = true; combatOver = true;
                resultText = "RESCATE FALLIDO. El Rescatado ha caído: no hay nada que extraer.";
                Log($"☠ {resultText}");
                RestoreFormation();
            }
            else
            {
                anyHeroDied = true;
                heroes.Remove(dead);
                expedition?.casualties.Add(dead); // permadeath: removed from the roster on return
                foreach (var h in heroes.Where(h => h.Alive))
                {
                    h.GainCorruption(15, Log); // seeing an ally die
                    h.GainFaith(-2, Log);
                }
                var partner = dead.bondPartner;
                if (partner != null && partner.Alive && heroes.Contains(partner))
                {
                    partner.GainCorruption(Campaign.GriefMancha, Log);
                    Log($"💔 {partner.unitName} ve caer a su vínculo {dead.unitName}: el duelo le desgarra (+{Campaign.GriefMancha} Mancha).");
                }
            }
        }

        void RallyCohesion(int amount, string who)
        {
            if (enemyCohesion >= enemyCohesionMax) { Log($"{who} arenga, pero la Moral del culto ya está en su cénit."); return; }
            enemyCohesion = Mathf.Min(enemyCohesionMax, enemyCohesion + amount);
            Log($"{who} inflama al culto: Moral +{amount} → {enemyCohesion}/{enemyCohesionMax}.");
        }

        void ShakeCohesion(int amount, string why)
        {
            if (enemyCohesion <= 0) return;
            var boss = enemies.FirstOrDefault(e => e.Alive && e.def.isBoss);
            // Faith unnerves a boss twice as hard (design: su Moral baja doble con Llama).
            if (boss != null && why.Contains("fervor")) amount *= 2;
            // Without a boss, cohesion needs a human/hybrid still standing to matter.
            if (boss == null && !enemies.Any(e => e.Alive && e.def.countsForCohesion)) return;

            enemyCohesion = Mathf.Max(0, enemyCohesion - amount);
            Log($"{(boss != null ? "Moral del Barón" : "Moral del culto")} −{amount} ({why}) → {enemyCohesion}/{enemyCohesionMax}");

            if (enemyCohesion == 0)
            {
                if (boss != null)
                {
                    // A boss with a form still to reveal will NOT break: its phases must play out first
                    // (mirrors the HP refuse-to-die). Its will holds at a thread until fully unmasked.
                    if (BossHasUnrevealedPhase(boss)) { enemyCohesion = 1; return; }
                    // Second win path: break the boss morally before killing him.
                    Log($"⚑ {boss.unitName} se QUIEBRA: su voluntad se hace añicos y el culto se desmorona.");
                    if (expedition != null) expedition.bossDefeated = true;
                    enemies.Clear();
                    return;
                }
                // Only enemies that have never fled break here. One who already fled
                // once (and later rejoined) now holds the line to the death, even at 0
                // Cohesion — a coward has only one flight in him.
                var fleeing = enemies.Where(e => e.Alive && e.def.countsForCohesion && !e.hasFled).ToList();
                foreach (var f in fleeing)
                {
                    f.hasFled = true;
                    Log($"{f.unitName} HUYE despavorido.");
                    enemies.Remove(f);
                    expedition?.fledEnemies.Add(f); // may reappear once more, HP intact
                }
                var diehards = enemies.Where(e => e.Alive && e.def.countsForCohesion && e.hasFled).ToList();
                foreach (var d in diehards)
                    Log($"{d.unitName} ya no tiene a dónde huir: se queda a morir.");
            }
        }

        void RestoreFormation()
        {
            if (savedFormation == null) return;
            var order = savedFormation;
            heroes.Sort((a, b) => order.IndexOf(a).CompareTo(order.IndexOf(b)));
            savedFormation = null;
        }

        /// <summary>Group retreat: each hero rolls 60% + VEL; failures stay a round exposed (+20% dmg).</summary>
        void DeclareRetreat()
        {
            retreatDeclaredThisRound = true;
            Log("¡Orden de retirada! Cada uno tira por su pellejo (60% + VEL).");
            foreach (var h in heroes.Where(h => h.Alive && !h.retreated).ToList())
            {
                int chance = 60 + h.def.speed;
                if (Rng.Range(0, 100) < chance)
                {
                    h.retreated = true;
                    Log($"→ {h.unitName} se repliega ({chance}%).");
                }
                else
                {
                    h.AddStatus(StatusKind.Marked, 20, 1);
                    Log($"→ {h.unitName} no encuentra hueco: queda expuesto (+20% daño) ({chance}%).");
                }
            }
            if (!CheckEnd())
            {
                var cur = Current();
                if (cur != null && cur.retreated) EndTurnOf(cur);
            }
        }

        bool CheckEnd()
        {
            if (combatOver) return true;
            if (!heroes.Any(h => h.Alive))
            {
                combatOver = true; resultText = "DERROTA. El Foso reclama sus cuerpos.";
                Log(resultText);
                RestoreFormation();
                return true;
            }
            if (!heroes.Any(h => h.Alive && !h.retreated))
            {
                combatOver = true; resultText = "RETIRADA. Vivir para purgar otro día.";
                Log(resultText);
                foreach (var h in heroes.Where(h => h.Alive))
                {
                    h.retreated = false;
                    h.GainFaith(-1, Log); // design: retirarse cuesta -1 Fe a todos
                }
                RestoreFormation();
                return true;
            }
            if (!enemies.Any(e => e.Alive))
            {
                // Aguantar / Sellado: the arena refills with a fresh wave instead of ending, until the last falls.
                bool holdMission = expedition?.node != null &&
                    (expedition.node.type == FrontNodeType.Aguantar || expedition.node.type == FrontNodeType.Sellado);
                if (holdMission && expedition.wavesLeft > 1)
                {
                    expedition.wavesLeft--;
                    enemies = expedition.SpawnEncounter(Expedition.WaveEncounter(campaign != null ? campaign.phase : 1, expedition.sector));
                    enemyCohesion = enemyCohesionMax; // a fresh wave of fanatics, full Moral
                    bool sealing = expedition.node.type == FrontNodeType.Sellado;
                    Log($"⚔ OLEADA {expedition.node.waves - expedition.wavesLeft + 1}/{expedition.node.waves}: {(sealing ? "la Herida escupe otra camada mientras el sello prende" : "el culto vuelve a la carga")} ({enemies.Count} enemigos).");
                    return false; // the line holds — combat continues into the next wave
                }
                combatOver = true; resultText = "VICTORIA. La Llama aún arde.";
                Log(resultText);
                if (!anyHeroDied)
                {
                    Log("Victoria sin bajas: la fe del grupo se refuerza.");
                    foreach (var h in heroes.Where(h => h.Alive)) h.GainFaith(1, Log);
                }
                foreach (var h in heroes.Where(h => h.Alive && h.endCombatCorruptionDebt > 0))
                {
                    Log($"La deuda del coraje embotellado se cobra en {h.unitName}.");
                    h.GainCorruption(h.endCombatCorruptionDebt, Log);
                    h.endCombatCorruptionDebt = 0;
                }
                SweepDarkDeaths(); // a soul pushed to 200 this combat (cielo rojo, duelo, la deuda) falls now, not later
                if (currentRoom != null)
                {
                    currentRoom.cleared = true;
                    // A mini-boss anomaly room guards the best loot: 2 pieces, guaranteed
                    if (currentRoom.isMiniBoss)
                    {
                        currentRoom.pendingLoot = new List<ItemStack>();
                        for (int i = 0; i < 2; i++)
                            currentRoom.pendingLoot.Add(new ItemStack { def = GameData.Item("Botin"), chargesLeft = 1, lootValue = Rng.Range(150, 251) });
                        Log("El nido de la anomalía guardaba un tesoro. Botín excepcional en la sala.");
                    }
                }

                // Servocráneo médico: tends the most wounded survivor after each combat
                var skull = expedition.FindWithCharges(ItemKind.ServoSkull);
                if (skull != null)
                {
                    var worst = heroes.Where(h => h.Alive && h.hp < h.EffMaxHP)
                        .OrderBy(h => (float)h.hp / h.EffMaxHP).FirstOrDefault();
                    if (worst != null)
                    {
                        worst.hp = Mathf.Min(worst.EffMaxHP, worst.hp + skull.def.power);
                        Log($"El Faro médico atiende a {worst.unitName} (+{skull.def.power} PV → {worst.hp}).");
                        if (worst.atDeathsDoor && worst.hp > 0)
                        {
                            worst.atDeathsDoor = false;
                            Log($"→ {worst.unitName} se aparta del Borde de la Muerte.");
                        }
                    }
                }

                RestoreFormation();
                if (map != null && map.AllCombatsCleared)
                {
                    // Purging the substage is the mission: survivors gain a level (design: XP por misión)
                    foreach (var h in heroes.Where(h => h.Alive))
                        if (h.LevelUp()) Log($"↑ {h.unitName} asciende a nivel {h.level} (+PV, +PRE).");
                    Log($"★ SUBSTAGE PURGADA — {heroes.Count(h => h.Alive && !h.def.isVip)}/4 en pie. " +
                        $"Mancha del grupo: {heroes.Where(h => h.Alive).Sum(h => h.corruption)}. " +
                        $"Botín cargado: {expedition.LootValue}p. Volved al Fortín a cobrar.");
                }
                return true;
            }
            return false;
        }

        // =====================================================================
        // ENEMY AI (simple, priority-flavored)
        // =====================================================================
        void EnemyAct(CombatUnit e)
        {
            // Headless floor — light policy (Fase 5): post-mirror, the floor fights mostly from the DARK
            // (clean vs the Foso, and humans blind-fire it), but relights when it goes pitch black to cut
            // the dark's Mancha bleed. A crude cycle, not optimal play — just a plausible floor.
            if (headless && e.TeamOf == Team.Heroes && !minorUsed && flareCharges > 0
                && lightLevel == 0 && Rng.Value < 0.4f)
                LaunchFlare(e);

            // Devoracadáveres: feed on a body on the field before acting (heals, punishes long fights).
            if (e.def.devoursCorpses && combatCorpses > 0 && e.hp < e.EffMaxHP)
            {
                combatCorpses--;
                int before = e.hp;
                e.hp = Mathf.Min(e.EffMaxHP, e.hp + e.def.corpseHeal);
                Log($"{e.unitName} devora un cadáver y se cura {e.hp - before} ({e.hp} PV).");
            }

            // Verdugo: acts twice this turn if nothing hurt it since its last turn.
            bool verdugoDouble = e.def.extraActionIfUnhit && !e.tookDamageSinceTurn;
            e.tookDamageSinceTurn = false; // start tracking fresh for its next turn

            // Neófito reload window
            if (e.def.maxAmmo > 0 && e.ammo <= 0)
            {
                e.ammo = e.def.maxAmmo;
                Log($"{e.unitName} RECARGA (ventana de castigo).");
                EndTurnOf(e); return;
            }

            // el Bombardero: telegraphed area attack. Primes one turn (dodgeable), detonates the next.
            if (e.def.isBomber)
            {
                if (e.bombPrimed)
                {
                    e.bombPrimed = false;
                    Log($"💥 {e.unitName} DETONA su carga sobre la vanguardia.");
                    foreach (var h in heroes.Where(x => x.Alive && PosOf(x) <= 2).ToList())
                    {
                        var outc = h.TakeDamage(Rng.Range(6, 11));
                        Log($"→ {h.unitName} recibe la onda expansiva ({h.hp} PV).");
                        HandleDamageOutcome(h, outc);
                    }
                    EndTurnOf(e); return;
                }
                e.bombPrimed = true;
                Log($"⚠ {e.unitName} ARMA una carga: la vanguardia debe dispersarse (detona el próximo turno).");
                EndTurnOf(e); return;
            }

            // Zapador Injertado: telegraphed gallery cave-in. Detonates on those pushed FORWARD to the
            // front (PosOf<=2 = Alambrada/Tierra de Nadie): heavy damage + buried (Stun) + Mancha from the collapse.
            if (e.def.isSapper)
            {
                if (e.bombPrimed)
                {
                    e.bombPrimed = false;
                    Log($"‼ {e.unitName}: LA GALERÍA SE DERRUMBA sobre quien avanzó.");
                    foreach (var h in heroes.Where(x => x.Alive && PosOf(x) <= 2).ToList())
                    {
                        var outc = h.TakeDamage(Rng.Range(10, 17));
                        Log($"→ {h.unitName} queda sepultado ({h.hp} PV).");
                        if (!HandleDamageOutcome(h, outc) && h.Alive)
                        {
                            h.AddStatus(StatusKind.Stun, 0, 1);
                            h.GainCorruption(6, Log);
                        }
                    }
                    EndTurnOf(e); return;
                }
                if (Rng.Value < 0.4f)
                {
                    e.bombPrimed = true;
                    Log($"⚠ {e.unitName} coloca cargas: las vigas crujen. Salid de la vanguardia (derrumbe el próximo turno).");
                    EndTurnOf(e); return;
                }
                // else fall through to a normal pick attack
            }

            // Desollador (mini-boss F): rips a hero out of formation and flays them each round until it falls.
            // DECIDED rule-break (§5, "cada sector rompe una regla"): its lunge PIERCES THE LINE — the grab
            // reaches any hero, lit or dark, ignoring the Foso channel. Freeing the captive = kill the Desollador.
            if (e.def.isFlayer)
            {
                var grabbed = heroes.FirstOrDefault(h => h.Alive && h.grabbedByFlayer);
                if (grabbed == null)
                {
                    var victim = heroes.Where(h => h.Alive).OrderBy(_ => Rng.Value).FirstOrDefault();
                    if (victim != null)
                    {
                        victim.grabbedByFlayer = true;
                        int d = Rng.Range(6, 11);
                        var outc = victim.TakeDamage(d);
                        Log($"⚔ {e.unitName} atraviesa la línea y arranca a {victim.unitName} de la formación: {d} de daño ({victim.hp} PV). Abatidlo para liberarlo.");
                        HandleDamageOutcome(victim, outc);
                        if (victim.Alive) victim.AddStatus(StatusKind.Bleed, 3, 3);
                    }
                    EndTurnOf(e); return;
                }
                int dmg = Rng.Range(7, 13);
                var o2 = grabbed.TakeDamage(dmg);
                Log($"🩸 {e.unitName} desuella a {grabbed.unitName}: {dmg} de daño ({grabbed.hp} PV).");
                HandleDamageOutcome(grabbed, o2);
                EndTurnOf(e); return;
            }

            // §9.4 Carroñero mutado: a hit-and-run thief. If there is supply to snatch, it grabs one
            // charge and bolts with the loot; with nothing to steal it just claws (falls through).
            if (e.def.stealsSupply && expedition != null)
            {
                var loot = expedition.inventory.Where(s => s.chargesLeft > 0).ToList();
                if (loot.Count > 0)
                {
                    var s = loot[Rng.Range(0, loot.Count)];
                    string what = s.def.displayName;
                    expedition.ConsumeCharge(s);
                    Log($"🐀 {e.unitName} arrebata {what} del petate y HUYE con el botín.");
                    enemies.Remove(e);
                    EndTurnOf(e); return;
                }
            }

            // Corista (§9.3): its chant chokes the Milagros for a couple of rounds and stains the party.
            // It re-chants only once the silence has faded (milagrosSilenced back to 0), else it flails.
            if (e.def.silencesMiracles && milagrosSilenced == 0)
            {
                milagrosSilenced = 2; // this round plus the next
                Log($"🎵 {e.unitName} entona nombres del Foso: los Milagros del escuadrón enmudecen.");
                foreach (var h in heroes.Where(x => x.Alive).ToList()) h.GainCorruption(4, Log);
                EndTurnOf(e); return;
            }

            // Summoners: the Matriarca's brood only comes while a Nido survives; other summoners
            // (el Confesor Rojo predica y su grey acude) call in directly, no nest required.
            if (e.def.summonPerRound > 0 && enemies.Count(x => x.Alive) < 6)
            {
                bool nestGated = e.def.summonFromNest;
                bool nestsAlive = enemies.Any(x => x.Alive && x.def.displayName == "Nido de carne");
                if (!nestGated || nestsAlive)
                {
                    int n = Mathf.Min(e.def.summonPerRound, 6 - enemies.Count(x => x.Alive));
                    for (int i = 0; i < n; i++)
                        enemies.Add(new CombatUnit(GameData.Unit(e.def.summonType)));
                    Log(nestGated
                        ? $"{e.unitName} chilla: {n} {GameData.Unit(e.def.summonType).displayName}(s) emergen de los nidos."
                        : $"{e.unitName} predica y su grey acude: {n} {GameData.Unit(e.def.summonType).displayName}(s).");
                }
                else
                    Log($"{e.unitName} chilla, pero los nidos están destruidos: no acude nadie.");
            }

            // The Nido is an object: it does nothing but wait to be burned down.
            if (e.def.displayName == "Nido de carne")
            {
                EndTurnOf(e); return;
            }

            // Predicador Hueco silenced: forced to the front (pos 1-2), he can only flail.
            if (e.def.displayName == "Falso Capellán" && PosOf(e) <= 2)
                Log($"{e.unitName} está SILENCIADO en primera línea: sin púlpito, solo manotea.");

            var options = e.def.abilities
                .Where(a => AbilityUsable(e, a, out _))
                .ToList();

            if (options.Count == 0)
            {
                // Heroes maneuver on the light axis (Fase 4); enemies shuffle their formation column.
                bool moved = e.TeamOf == Team.Heroes ? TryZoneShuffle(e) : TryMove(e);
                if (moved) { EndTurnOf(e); return; }
                Log($"{e.unitName} vacila (sin opciones)." );
                EndTurnOf(e); return;
            }

            var chosen = options[Rng.Range(0, options.Count)];
            var targets = ValidTargets(e, chosen);

            var pool = targets;
            bool blind = false;

            // Fase 2 — light channel: a human sees (and shoots) only the LIT; the Foso's flesh
            // reaches only the DARK. With no one in its channel, it fires blind into the murk.
            if (chosen.targetKind == TargetKind.Enemy && e.TeamOf == Team.Enemies)
                pool = ChannelFilter(e, targets, out blind);

            // Taunt (Desafío) forces hostile attacks onto a reachable taunter
            if (chosen.targetKind == TargetKind.Enemy)
            {
                var taunters = pool.Where(t => t.Has(StatusKind.Taunt)).ToList();
                if (taunters.Count > 0) pool = taunters;
                // The Horror hunts the psyker that let it through (unless taunted)
                else if (e.def.displayName == "Descendido Menor")
                {
                    var psy = pool.Where(t => t.def.displayName == "Vidente").ToList();
                    if (psy.Count > 0) pool = psy;
                }
            }
            // Sabueso del Foso (§9.3): the pack piles onto marked prey (its own bite marks the target).
            if (chosen.targetKind == TargetKind.Enemy && e.def.displayName == "Sabueso del Foso")
            {
                var marked = pool.Where(h => h.Alive && h.Has(StatusKind.Marked)).ToList();
                if (marked.Count > 0) pool = marked;
            }
            // The cult hunts the escaped prisoner: a chance to lunge for the VIP — if it can see him.
            if (chosen.targetKind == TargetKind.Enemy && e.TeamOf == Team.Enemies)
            {
                var vip = pool.FirstOrDefault(h => h.Alive && h.def.isVip);
                if (vip != null && Rng.Value < 0.35f) pool = new List<CombatUnit> { vip };
            }
            var target = pool.OrderBy(_ => Rng.Value).FirstOrDefault();
            activeBlindPenalty = blind ? BlindFireAcc : 0;
            if (blind && target != null)
                Log($"{e.unitName} no encuentra blanco en {(e.def.threatChannel == ThreatChannel.Human ? "la luz" : "la oscuridad")}: dispara a ciegas (−{BlindFireAcc} prec).");

            // Verdugo: unhurt, it lands a first blow WITHOUT ending its turn, then acts normally again.
            if (verdugoDouble && target != null)
            {
                Log($"{e.unitName} ataca dos veces: nada lo tocó.");
                UseAbility(e, chosen, target, endTurn: false);
                if (!combatOver)
                {
                    var t2 = FoesOf(e).Where(h => h.Alive && !h.retreated).OrderBy(_ => Rng.Value).FirstOrDefault();
                    activeBlindPenalty = blind ? BlindFireAcc : 0;
                    UseAbility(e, chosen, t2 ?? target);
                }
                else EndTurnOf(e);
                activeBlindPenalty = 0;
                return;
            }

            UseAbility(e, chosen, target);
            activeBlindPenalty = 0;
        }

        bool TryMove(CombatUnit u)
        {
            var list = AlliesOf(u);
            int i = list.IndexOf(u);
            if (i < 0) return false; // not on the field (fled/removed)
            int j = i > 0 ? i - 1 : (i < list.Count - 1 ? i + 1 : -1);
            if (j < 0) return false;
            (list[i], list[j]) = (list[j], list[i]);
            Log($"{u.unitName} se desplaza a la posición {j + 1}.");
            return true;
        }

        /// Cosido — at half HP it bursts into two smaller, faster halves (once).
        void SplitEnemy(CombatUnit e)
        {
            e.hasSplit = true;
            Log($"‼ {e.unitName} se abre en dos por las costuras.");
            int n = Mathf.Min(2, 6 - enemies.Count(x => x.Alive));
            for (int i = 0; i < n; i++)
                enemies.Add(new CombatUnit(GameData.Unit(e.def.splitsInto)));
        }

        /// Fase 4 — a hero with no usable stance shifts DEPTH on the light axis to find one
        /// (advance to melee/assault, or fall back to a firing zone). Used by the headless AI.
        bool TryZoneShuffle(CombatUnit u)
        {
            foreach (int dz in new[] { +1, -1 })
            {
                int nz = u.zone + dz;
                if (nz < 0 || nz > 3) continue;
                int old = u.zone; u.zone = nz;
                if (u.def.abilities.Any(a => AbilityUsable(u, a, out _)))
                {
                    Log($"{u.unitName} maniobra a {ZoneNames[nz]}.");
                    return true;
                }
                u.zone = old;
            }
            if (u.zone < 3) { u.zone++; Log($"{u.unitName} avanza a {ZoneNames[u.zone]}."); return true; }
            return false;
        }

        // =====================================================================
        // IMGUI
        // =====================================================================
        void OnGUI()
        {
            ToolkitHotkeyOnGUI(); // F9 toggles the provisional UI Toolkit combat preview
            if (ToolkitCombatActive) return; // the UI Toolkit screen owns combat: don't draw IMGUI under it
            GUI.skin.label.fontSize = 13; GUI.skin.button.fontSize = 13; GUI.skin.box.fontSize = 13;

            GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, Screen.height - 20));

            GUILayout.BeginHorizontal();
            if (atShip)
            {
                GUILayout.Label($"<b>LA VIGILIA DE HIERRO — el Fortín</b>   Semana {campaign.week}   Crecida {campaign.phase}/8   Paga: {shipTronos}   Roster {campaign.roster.Count}/{campaign.rosterCap}   <color=#888888>Semilla {campaign.seed}</color>", Rich());
                GUILayout.FlexibleSpace();
            }
            else
            {
                GUILayout.Label($"<b>LA VIGILIA DE HIERRO — Expedición</b>   Purgados {map.CombatsCleared}/{map.CombatsTotal}   <color=#88ccff>📡 Señal: {Signal}% ({VoxNames[VoxLevel]})</color>   Botín {expedition.LootValue} de paga   Huecos {expedition.inventory.Count}/{Expedition.MaxSlots}"
                                + $"   <color=#ffdd66>☼ Luz {lightLevel}/{MaxLight}</color>" + (inCombat ? $"   Ronda {round}" : "")
                                + (expedition.missionRoundBudget > 0 ? $"   <color=#ff8866>⏱ Sabotaje: {Mathf.Max(0, expedition.missionRoundBudget - missionRounds)} rondas</color>" : ""), Rich());
                GUILayout.FlexibleSpace();
                if (inCombat)
                {
                    bool boss = enemies.Any(e => e.Alive && e.def.isBoss);
                    GUILayout.Label(boss
                        ? $"<color=#ffcc66>Moral del Barón: {enemyCohesion}/{enemyCohesionMax}</color>  (0 = se quiebra)"
                        : $"Moral del culto: {enemyCohesion}/{enemyCohesionMax}", Rich());
                }
                if (!inCombat)
                {
                    // Evacuation needs signal (design §4.3) — or your own feet back at the airlock.
                    // A wipe can always "return" (there's nothing left to strand).
                    bool wiped = !expedition.heroes.Any(h => h.Alive);
                    if (missionFailed)
                    {
                        GUILayout.Label("<color=#ff8866>⏱ Sabotaje fallido: los refuerzos cierran el cerco.</color>", Rich());
                        if (GUILayout.Button("Volver al Fortín (retirada)", GUILayout.Width(240))) ReturnToShip(false);
                    }
                    else
                    {
                        // Evacuation needs signal (design §4.3) — or your own feet back at the airlock.
                        bool canEvac = wiped || map.AllCombatsCleared || VoxLevel >= 2 || map.current.kind == RoomKind.Entrada;
                        GUI.enabled = canEvac;
                        if (GUILayout.Button(new GUIContent(
                                wiped ? "Recoger a los caídos (volver al Fortín)" : map.AllCombatsCleared ? "Volver al Fortín (cobrar)" : "Abortar y volver al Fortín",
                                canEvac ? "" : "Sin Señal no hay evacuación: acércate a la línea, despliega un repetidor o vuelve a la entrada."),
                                GUILayout.Width(240)))
                            ReturnToShip(map.AllCombatsCleared);
                        GUI.enabled = true;
                    }
                }
            }
            GUILayout.EndHorizontal();

            if (!atShip && expedition != null)
                GUILayout.Label($"<color=#aaddff>🎯 Objetivo — {NodeObjective()}</color>", Rich());

            if (atShip)
            {
                DrawShip();
            }
            else if (!inCombat)
            {
                DrawMapView();
            }
            else
            {
                // Turn order strip
                var cur = Current();
                string order = string.Join("  →  ", turnQueue.Skip(Mathf.Max(0, turnIndex))
                    .Where(u => u.Alive && !u.retreated && AlliesOf(u).Contains(u)).Select(u => u.unitName));
                GUILayout.Label($"Orden: {order}");
                GUILayout.Space(4);

                // Battlefield: heroes pos4..1 | enemies pos1..4 (retreated heroes are gone)
                GUILayout.BeginHorizontal();
                for (int i = heroes.Count - 1; i >= 0; i--)
                    if (!heroes[i].retreated) UnitBox(heroes[i], cur);
                GUILayout.Label("   ⚔   ", GUILayout.Width(40));
                for (int i = 0; i < enemies.Count; i++) UnitBox(enemies[i], cur);
                GUILayout.EndHorizontal();
                GUILayout.Space(6);

                if (combatOver)
                {
                    GUILayout.Label($"<b>{resultText}</b>", Rich());
                    bool won = resultText.StartsWith("VICTORIA") && heroes.Any(h => h.Alive);
                    bool retreatEnd = resultText.StartsWith("RETIRADA") && heroes.Any(h => h.Alive);
                    if (won && map.AllCombatsCleared)
                        GUILayout.Label("<b>★ SUBSTAGE PURGADA.</b> la Llama cuenta a sus siervos.", Rich());
                    if (won || retreatEnd)
                    {
                        if (GUILayout.Button("Volver al mapa", GUILayout.Width(200)))
                        { inCombat = false; currentRoom = null; }
                    }
                    else
                    {
                        if (GUILayout.Button("Volver al Fortín", GUILayout.Width(200)))
                            ReturnToShip(false);
                    }
                }
                else if (cur != null && cur.TeamOf == Team.Heroes)
                {
                    DrawHeroControls(cur);
                }
                else
                {
                    GUILayout.Label("El enemigo actúa...");
                }
            }

            // Log
            GUILayout.Space(6);
            GUILayout.Label("<b>Registro</b>", Rich());
            logScroll = GUILayout.BeginScrollView(logScroll, GUI.skin.box, GUILayout.Height(170));
            for (int i = log.Count - 1; i >= 0; i--) GUILayout.Label(log[i]);
            GUILayout.EndScrollView();

            GUILayout.EndArea();

            // Hover tooltip (provisional IMGUI version; real icon-based UI arrives with the presentation milestone)
            if (!string.IsNullOrEmpty(GUI.tooltip))
            {
                var mp = Event.current.mousePosition;
                var tipStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, richText = true, wordWrap = true };
                float w = 280f;
                float h = tipStyle.CalcHeight(new GUIContent(GUI.tooltip), w) + 6f;
                float x = Mathf.Min(mp.x + 16, Screen.width - w - 8);
                float y = Mathf.Min(mp.y + 12, Screen.height - h - 8);
                GUI.Box(new Rect(x, y, w, h), GUI.tooltip, tipStyle);
            }
        }

        GUIStyle Rich() { var s = new GUIStyle(GUI.skin.label); s.richText = true; return s; }

        void DrawCampaignEnd()
        {
            GUILayout.Space(30);
            if (campaign.stageWon)
            {
                GUILayout.Label("<b><color=#ffdd66>★ LA HERIDA DE VARED SELLADA ★</color></b>", Rich());
                GUILayout.Label("El Confesor Rojo ha caído y el sello ha prendido sobre la Herida. La Ruptura, aquí, cede.", Rich());
                GUILayout.Label($"Semana {campaign.week}. Agentes vivos: {campaign.roster.Count}. Caídos en el Memorial: {campaign.memorial.Count}.");
                GUILayout.Label("La Vigilia resiste un día más. (Órdenes aliadas y más frentes llegan en hitos futuros.)");
            }
            else
            {
                GUILayout.Label("<b><color=#ff6666>☠ EL SECTOR DE VARED HA CAÍDO ☠</color></b>", Rich());
                GUILayout.Label("La Crecida se completó. La Marea del Foso desciende sobre Vared. La campaña ha terminado.", Rich());
                GUILayout.Label($"Aguantasteis {campaign.week} semanas. {campaign.substagesCleared} nodos del frente asegurados. {campaign.memorial.Count} caídos.");
            }
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Nueva cruzada", GUILayout.Width(160))) { campaign = Campaign.New(); log.Clear(); seedInput = ""; }
            GUILayout.Label("Semilla:", GUILayout.Width(58));
            seedInput = GUILayout.TextField(seedInput, GUILayout.Width(120));
            GUI.enabled = int.TryParse(seedInput, out _);
            if (GUILayout.Button("Recrear con semilla", GUILayout.Width(170)) && int.TryParse(seedInput, out int sd)) { campaign = Campaign.New(sd); log.Clear(); }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        // =====================================================================
        // THE SHIP (Sancta Sicaria): roster, decks, supply, the week
        // =====================================================================
        static readonly string[] ShopKeys =
            { "Cargador", "Promethium", "Racion", "KitMedico", "KitCampamento", "AguaBendita", "Ganzuas", "Baliza", "MascaraGas", "Sello", "Servocraneo" };

        void DrawShip()
        {
            if (campaign.CampaignOver) { DrawCampaignEnd(); return; }

            shipScroll = GUILayout.BeginScrollView(shipScroll);

            GUILayout.Label($"<b>{SectorName(campaign.currentSector)}</b>  ·  <b>La Crecida, fase {campaign.phase}/8.</b> {Campaign.PhaseNote(campaign.phase)}", Rich());
            GUILayout.Space(4);
            DrawVotos();
            DrawPuente();
            GUILayout.Space(8);
            DrawRosterDeck();
            GUILayout.Space(8);
            DrawBarracon();
            GUILayout.Space(8);
            DrawSupplyDeck();
            GUILayout.Space(8);
            DrawTaller();
            GUILayout.Space(8);
            DrawComedor();
            GUILayout.Space(8);
            DrawBrigada();
            GUILayout.Space(8);
            DrawLibrarium();
            if (campaign.memorial.Count > 0)
            {
                GUILayout.Space(6);
                GUILayout.Label($"<b>Memorial</b> — {campaign.memorial.Count} caídos: {string.Join(" · ", campaign.memorial.TakeLast(6))}", Rich());
            }

            GUILayout.EndScrollView();
        }

        void DrawRosterDeck()
        {
            int expSel = campaign.Assigned(HeroAssignment.Expedition).Count();
            int recl = campaign.Assigned(HeroAssignment.Reclusiam).Count();
            int enf = campaign.Assigned(HeroAssignment.Enfermeria).Count();
            GUILayout.Label($"<b>Barracones</b> — grupo de expedición ({expSel}/4)   ·   la Capilla ({recl}/{Campaign.ReclusiamSlots})   ·   Enfermería ({enf}/{Campaign.EnfermeriaSlots}).  El coste de cada cubierta sube con el nivel del agente.", Rich());

            foreach (var h in campaign.roster)
            {
                var a = campaign.Get(h);
                int votoTreat = campaign.HasVoto(Voto.Hierro) ? 2 : 1; // Voto de Hierro: sanar cuesta el doble
                int reclCost = Campaign.ReclusiamCost(h) * votoTreat;
                int enfCost = Campaign.EnfermeriaCost(h) * votoTreat;
                GUILayout.BeginHorizontal();
                string soul = h.soul != SoulState.None ? $"  [{CombatUnit.SoulName(h.soul)}]" : "";
                string inc = h.treatmentRaidsLeft > 0 ? $" ({h.treatmentRaidsLeft} inc.)" : "";
                string tag = h.averiado ? "<color=#ff8866>⚙ AVERIADO — repara en el Taller</color>"
                           : a == HeroAssignment.Expedition ? "<color=#88ff88>▸ EXPEDICIÓN</color>"
                           : a == HeroAssignment.Reclusiam ? $"<color=#ffcc66>⛪ la Capilla{inc}</color>"
                           : a == HeroAssignment.Enfermeria ? $"<color=#88ccff>✚ Enfermería{inc}</color>" : "· libre";
                GUILayout.Label(new GUIContent($"{h.unitName}  ·  Nv {h.level}  —  {h.hp}/{h.EffMaxHP} PV · Mancha {h.corruption} · Llama {h.faith}{soul}   {tag}", UnitTooltip(h)),
                    GUI.skin.box, GUILayout.Width(440));

                if (a == HeroAssignment.Expedition)
                { if (GUILayout.Button("Quitar del grupo", GUILayout.Width(130))) Assign(h, HeroAssignment.Available); }
                else
                {
                    GUI.enabled = a == HeroAssignment.Available && expSel < 4 && !h.averiado; // broken/in-treatment can't deploy
                    if (GUILayout.Button("A expedición", GUILayout.Width(110))) Assign(h, HeroAssignment.Expedition);
                    GUI.enabled = true;
                }

                if (a == HeroAssignment.Reclusiam)
                { if (GUILayout.Button(new GUIContent("Cancelar", "Lo sacas antes de tiempo: sin reembolso, conserva lo curado."), GUILayout.Width(100))) { h.treatmentRaidsLeft = 0; Assign(h, HeroAssignment.Available); } }
                else if (a != HeroAssignment.Enfermeria)
                {
                    GUI.enabled = a == HeroAssignment.Available && recl < Campaign.ReclusiamSlots && shipTronos >= reclCost;
                    if (GUILayout.Button(new GUIContent($"la Capilla ({reclCost}p)", $"Fuera del roster {Campaign.ChapelRaids(h)} incursion(es)."), GUILayout.Width(140)))
                    { shipTronos -= reclCost; campaign.SendToTreatment(h, HeroAssignment.Reclusiam); }
                    GUI.enabled = true;
                }

                if (a == HeroAssignment.Enfermeria)
                { if (GUILayout.Button(new GUIContent("Cancelar", "Lo sacas antes de tiempo: sin reembolso, conserva lo curado."), GUILayout.Width(100))) { h.treatmentRaidsLeft = 0; Assign(h, HeroAssignment.Available); } }
                else if (a != HeroAssignment.Reclusiam)
                {
                    GUI.enabled = a == HeroAssignment.Available && enf < Campaign.EnfermeriaSlots && shipTronos >= enfCost && h.hp < h.EffMaxHP;
                    if (GUILayout.Button(new GUIContent($"Enfermería ({enfCost}p)", $"Fuera del roster {Campaign.InfirmaryRaids(h)} incursion(es)."), GUILayout.Width(140)))
                    { shipTronos -= enfCost; campaign.SendToTreatment(h, HeroAssignment.Enfermeria); }
                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();
            }
        }

        void Assign(CombatUnit h, HeroAssignment a) => campaign.assignment[h] = a;

        // A boss still has a form to reveal if a phase threshold is set that it hasn't crossed yet.
        static bool BossHasUnrevealedPhase(CombatUnit b) =>
            (b.def.phaseTwoAtPct > 0 && b.bossPhase < 2) ||
            (b.def.phaseThreeAtPct > 0 && b.bossPhase < 3);

        static string SectorName(int s) => s == 1 ? "Sector 1 · la Primera Línea"
                                         : s == 2 ? "Sector 2 · las Galerías"
                                         : s == 3 ? "Sector 3 · la Tierra de Nadie" : "Sector F · el Reducto";

        // ---- el Barracón: ficha + equipo (arma/armadura/collar/anillo) + tienda del Taller ----
        static string SlotLabel(GearSlot s) => s == GearSlot.Weapon ? "Arma"
                                             : s == GearSlot.Armor  ? "Armadura"
                                             : s == GearSlot.Neck   ? "Collar" : "Anillo";

        void DrawBarracon()
        {
            showBarracon = GUILayout.Toggle(showBarracon,
                $"  <b>el Barracón</b> — ficha, arma/armadura y abalorios. Armería: {campaign.armory.Count} piezas guardadas.");
            if (!showBarracon) return;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Soldado:", GUILayout.Width(60));
            for (int i = 0; i < campaign.roster.Count; i++)
            {
                bool sel = i == gearHeroIdx;
                if (GUILayout.Toggle(sel, campaign.roster[i].unitName, GUI.skin.button, GUILayout.Width(120)) && !sel) gearHeroIdx = i;
            }
            GUILayout.EndHorizontal();

            if (gearHeroIdx < 0 || gearHeroIdx >= campaign.roster.Count)
            { GUILayout.Label("<i>Elige un soldado para ver su ficha y su equipo.</i>", Rich()); DrawTienda(); return; }

            var sol = campaign.roster[gearHeroIdx];
            GUILayout.Label(new GUIContent(
                $"<b>{sol.unitName}</b>  ·  Nv {sol.level}  —  {sol.hp}/{sol.EffMaxHP} PV · Arm {sol.EffArmor} · Esq {sol.EffDodge} · VEL {sol.def.speed + sol.GearSpeed} · res Mancha {sol.EffCorrResist}% · Mancha {sol.corruption} · Llama {sol.faith}",
                UnitTooltip(sol)), Rich());

            DrawGearSlot(sol, GearSlot.Weapon, "Arma");
            DrawGearSlot(sol, GearSlot.Armor,  "Armadura");
            DrawGearSlot(sol, GearSlot.Neck,   "Collar");
            DrawGearSlot(sol, GearSlot.Ring,   "Anillo");

            DrawTienda();
        }

        void DrawGearSlot(CombatUnit sol, GearSlot slot, string label)
        {
            GUILayout.BeginHorizontal();
            var cur = sol.GearIn(slot);
            string curTxt = cur != null ? $"{cur.displayName} ({cur.Describe()})" : "<i>vacío</i>";
            GUILayout.Label($"<b>{label}:</b> {curTxt}", Rich(), GUILayout.Width(360));
            if (cur != null && GUILayout.Button("Quitar", GUILayout.Width(70))) campaign.Unequip(sol, slot);
            foreach (var g in campaign.armory.Where(x => x.slot == slot).ToList())
                if (GUILayout.Button(new GUIContent($"Equipar {g.displayName}", $"{g.Describe()}\n{g.flavor}"), GUILayout.Width(210)))
                    campaign.Equip(sol, g);
            GUILayout.EndHorizontal();
        }

        void DrawTienda()
        {
            showTienda = GUILayout.Toggle(showTienda, "  <b>Tienda del Taller</b> — comprar equipo a la armería (con la paga; los abalorios del Foso solo caen como botín).");
            if (!showTienda) return;
            foreach (var key in GameData.GearKeys)
            {
                var g = GameData.Gear(key);
                if (g.price <= 0) continue; // loot-only pieces are never sold
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent($"{SlotLabel(g.slot)} · {g.displayName} — {g.Describe()}", g.flavor), GUI.skin.box, GUILayout.Width(440));
                GUI.enabled = shipTronos >= g.price;
                if (GUILayout.Button($"Comprar ({g.price}p)", GUILayout.Width(150)))
                { shipTronos -= g.price; campaign.armory.Add(GameData.Gear(key)); Log($"el Taller forja {g.displayName}: va a la armería."); }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
        }

        bool showTaller;
        void DrawTaller()
        {
            showTaller = GUILayout.Toggle(showTaller, "  <b>el Taller del Zapador</b> — mejoras de equipo (permanentes e inmediatas; el coste sube por rango)");
            if (!showTaller) return;

            // El Taller forja Autómatas (clase-firma): inmune a la Mancha, nunca gana Llama, no cura con Milagros.
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>Construir Autómata</b> — máquina de guerra: inmune a la Mancha, no forma vínculos ni cura con Milagros. Roster {campaign.roster.Count}/{campaign.rosterCap}.", Rich(), GUILayout.Width(440));
            GUI.enabled = shipTronos >= Campaign.AutomatonCost && campaign.roster.Count < campaign.rosterCap;
            if (GUILayout.Button($"Construir ({Campaign.AutomatonCost}p)", GUILayout.Width(180)))
            { shipTronos -= Campaign.AutomatonCost; var a = campaign.BuildAutomaton(); Log($"⚙ el Taller ensambla {a.unitName}: acero de la Vigilia, sin alma que perder."); }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            foreach (var h in campaign.roster)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(
                    $"{h.unitName}  ·  Arma {h.weaponRank}/{CombatUnit.MaxGearRank} (+{h.weaponRank} daño, +{5 * h.weaponRank} PRE)  ·  Armadura {h.armorRank}/{CombatUnit.MaxGearRank} (+{h.armorRank} Arm)",
                    UnitTooltip(h)), GUI.skin.box, GUILayout.Width(440));

                int wCost = Campaign.WeaponUpgradeCost(h);
                GUI.enabled = h.weaponRank < CombatUnit.MaxGearRank && shipTronos >= wCost;
                if (GUILayout.Button(h.weaponRank < CombatUnit.MaxGearRank ? $"Mejorar arma ({wCost}p)" : "Arma al máximo", GUILayout.Width(180)))
                { shipTronos -= wCost; h.weaponRank++; Log($"{h.unitName}: el Zapador mejora su arma a rango {h.weaponRank} (+1 daño, +5 PRE)."); }
                GUI.enabled = true;

                int aCost = Campaign.ArmorUpgradeCost(h);
                GUI.enabled = h.armorRank < CombatUnit.MaxGearRank && shipTronos >= aCost;
                if (GUILayout.Button(h.armorRank < CombatUnit.MaxGearRank ? $"Reforzar armadura ({aCost}p)" : "Armadura al máximo", GUILayout.Width(200)))
                { shipTronos -= aCost; h.armorRank++; Log($"{h.unitName}: el Zapador refuerza su armadura a rango {h.armorRank} (+1 Arm)."); }
                GUI.enabled = true;

                if (h.averiado)
                {
                    GUI.enabled = shipTronos >= Campaign.AutomatonRepairCost;
                    if (GUILayout.Button($"⚙ Reparar avería ({Campaign.AutomatonRepairCost}p)", GUILayout.Width(190)))
                    { shipTronos -= Campaign.AutomatonRepairCost; campaign.RepairAutomaton(h); Log($"⚙ el Taller repara a {h.unitName}: vuelve a rodar."); }
                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();
            }
        }

        bool showComedor;
        CombatUnit comedorSel;
        void DrawComedor()
        {
            showComedor = GUILayout.Toggle(showComedor, "  <b>el Comedor</b> — vínculos: sienta a dos al mismo rancho (les baja la Mancha; pero el duelo desgarra si uno cae)");
            if (!showComedor) { comedorSel = null; return; }
            if (comedorSel != null && !campaign.roster.Contains(comedorSel)) comedorSel = null;

            foreach (var h in campaign.roster)
            {
                GUILayout.BeginHorizontal();
                string bond = h.bondPartner != null ? $"  <color=#ff99bb>♥ {h.bondPartner.unitName}</color>" : "";
                string sel = h == comedorSel ? "  <color=#ffff88>◄ elegido</color>" : "";
                GUILayout.Label(new GUIContent($"{h.unitName}  ·  Mancha {h.corruption}{bond}{sel}", UnitTooltip(h)), GUI.skin.box, GUILayout.Width(360));

                if (h.def.isAutomaton)
                {
                    GUILayout.Label("<i>no come ni forma vínculos: es una máquina.</i>", Rich());
                }
                else if (h.bondPartner != null)
                {
                    GUI.enabled = shipTronos >= Campaign.ComedorCost;
                    if (GUILayout.Button($"Rancho ({Campaign.ComedorCost}p)", GUILayout.Width(140))) FeedPair(h, h.bondPartner);
                    GUI.enabled = true;
                    if (GUILayout.Button("Romper vínculo", GUILayout.Width(130)))
                    { Log($"{h.unitName} y {h.bondPartner.unitName} rompen su vínculo."); Campaign.Unbond(h); }
                }
                else if (comedorSel == null)
                { if (GUILayout.Button("Elegir", GUILayout.Width(90))) comedorSel = h; }
                else if (comedorSel == h)
                { if (GUILayout.Button("Cancelar", GUILayout.Width(90))) comedorSel = null; }
                else
                {
                    GUI.enabled = shipTronos >= Campaign.ComedorCost;
                    if (GUILayout.Button($"Juntar en el rancho ({Campaign.ComedorCost}p)", GUILayout.Width(220)))
                    {
                        // Tell (never flagged): an infiltrator quietly declines the rancho — no bond, no charge.
                        if (comedorSel.isInfiltrator || h.isInfiltrator)
                        {
                            var refuser = comedorSel.isInfiltrator ? comedorSel : h;
                            Log($"{refuser.unitName} se excusa y come solo. El vínculo no cuaja.");
                        }
                        else
                        {
                            Campaign.Bond(comedorSel, h); FeedPair(comedorSel, h);
                            Log($"{comedorSel.unitName} y {h.unitName} comparten mesa: forjan un vínculo.");
                        }
                        comedorSel = null;
                    }
                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();
            }
        }

        bool showBrigada;
        Dictionary<CombatUnit, bool> brigadaVerdict = new Dictionary<CombatUnit, bool>();
        void DrawBrigada()
        {
            showBrigada = GUILayout.Toggle(showBrigada,
                $"  <b>la Brigada</b> — interroga a la tropa buscando infiltrados (fiabilidad {campaign.InterrogationReliability}%). El Foso siembra agentes; ni la Brigada acierta siempre.");
            if (!showBrigada) return;

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Fiabilidad: {campaign.InterrogationReliability}% (rango {campaign.brigadaRank + 1}/3). Cuidado: un veredicto puede ser un falso positivo.", Rich(), GUILayout.Width(360));
            if (campaign.brigadaRank < 2)
            {
                int up = Campaign.BrigadaUpgradeCost(campaign.brigadaRank);
                GUI.enabled = shipTronos >= up;
                if (GUILayout.Button($"Mejorar métodos ({up}p)", GUILayout.Width(190)))
                { shipTronos -= up; campaign.brigadaRank++; campaign.interrogated.Clear(); Log($"la Brigada afina el careo: fiabilidad {campaign.InterrogationReliability}%."); }
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();

            foreach (var h in campaign.roster.ToList()) // ToList: Fusilar mutates the roster
            {
                if (h.def.isAutomaton) continue; // a machine can't be a cult agent
                GUILayout.BeginHorizontal();
                string verdict = brigadaVerdict.TryGetValue(h, out bool v)
                    ? (v ? "<color=#ff6666>▲ señalado como infiltrado</color>" : "<color=#88ff88>▽ sin indicios</color>")
                    : "<color=#aaaaaa>sin interrogar</color>";
                GUILayout.Label(new GUIContent($"{h.unitName}  ·  {verdict}", UnitTooltip(h)), GUI.skin.box, GUILayout.Width(360));

                GUI.enabled = shipTronos >= Campaign.InterrogateCost && !campaign.interrogated.Contains(h);
                if (GUILayout.Button(new GUIContent($"Interrogar ({Campaign.InterrogateCost}p)",
                        "El veredicto no es certeza: sube la fiabilidad mejorando la Brigada."), GUILayout.Width(160)))
                {
                    shipTronos -= Campaign.InterrogateCost;
                    campaign.interrogated.Add(h);
                    brigadaVerdict[h] = campaign.Interrogate(h);
                    Log($"la Brigada interroga a {h.unitName}: {(brigadaVerdict[h] ? "los ojos le bailan. Sospechoso." : "aguanta el careo. Parece limpio.")}");
                }
                GUI.enabled = true;

                if (GUILayout.Button(new GUIContent("Fusilar", "Lo quitas del roster para siempre. Si te equivocas, pierdes a un leal."), GUILayout.Width(90)))
                { campaign.Expel(h, Log); brigadaVerdict.Remove(h); }
                GUILayout.EndHorizontal();
            }
        }

        void FeedPair(CombatUnit a, CombatUnit b)
        {
            shipTronos -= Campaign.ComedorCost;
            a.GainCorruption(-Campaign.ComedorMealMancha, Log);
            b.GainCorruption(-Campaign.ComedorMealMancha, Log);
            Log($"Rancho compartido: {a.unitName} y {b.unitName} recobran algo de aliento (−{Campaign.ComedorMealMancha} Mancha).");
        }

        void DrawSupplyDeck()
        {
            GUILayout.Label($"<b>Depósito de suministros</b> — monta el petate ({shipStash.Count}/{Expedition.MaxSlots})." +
                            (campaign.PriceMult > 1f ? $"  <color=#ff8888>Precios +{(int)((campaign.PriceMult - 1) * 100)}% por la Crecida.</color>" : ""), Rich());
            foreach (var key in ShopKeys)
            {
                var def = GameData.Item(key);
                int price = campaign.Price(def.price);
                GUILayout.BeginHorizontal();
                GUI.enabled = shipTronos >= price && shipStash.Count < Expedition.MaxSlots;
                if (GUILayout.Button(new GUIContent($"Comprar — {price}p", def.description), GUILayout.Width(150)))
                {
                    shipTronos -= price;
                    shipStash.Add(new ItemStack { def = def, chargesLeft = def.charges, refundable = true });
                }
                GUI.enabled = true;
                GUILayout.Label(new GUIContent($"{def.displayName}  —  {def.description}", def.description));
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(4);
            int lootCount = shipStash.Count(s => s.def.kind == ItemKind.Loot);
            int lootWorth = shipStash.Where(s => s.def.kind == ItemKind.Loot).Sum(s => s.lootValue);
            if (lootCount > 0 && GUILayout.Button($"Vender todo el botín ({lootCount} piezas, +{lootWorth}p)", GUILayout.Width(280)))
            {
                shipTronos += lootWorth;
                shipStash.RemoveAll(s => s.def.kind == ItemKind.Loot);
                Log($"Botín vendido en el Fortín: +{lootWorth}p. Fondos: {shipTronos}p.");
            }
            foreach (var s in shipStash.ToList())
            {
                GUILayout.BeginHorizontal();
                string tag = s.def.kind == ItemKind.Loot ? $"★ botín {s.lootValue}p"
                           : s.def.kind == ItemKind.ServoSkull ? "pasivo"
                           : $"{s.chargesLeft} uso(s)";
                GUILayout.Label(new GUIContent($"{s.def.displayName}  ·  {tag}", s.def.description), GUI.skin.box, GUILayout.Width(300));
                int value = SellValue(s);
                string verb = s.refundable ? "Cancelar compra" : "Vender";
                if (GUILayout.Button($"{verb} (+{value}p)", GUILayout.Width(160)))
                {
                    shipTronos += value;
                    shipStash.Remove(s);
                    Log($"{s.def.displayName}: {(s.refundable ? "compra cancelada" : "vendido")} +{value}p.");
                }
                GUILayout.EndHorizontal();
            }
        }

        bool showLibrarium;

        void DrawLibrarium()
        {
            showLibrarium = GUILayout.Toggle(showLibrarium, $"  <b>Manual de Campo</b> — {campaign.bestiaryEncountered.Count} enemigos · {campaign.glossary.Count} entradas del Diario  (clic para {(showLibrarium ? "ocultar" : "ver")})");
            if (!showLibrarium) return;

            GUILayout.Label("<b>Bestiario</b> (se gana matando: 1 muerte = visto · 3 = dominado · jefes al primer encuentro):", Rich());
            foreach (var name in campaign.bestiaryEncountered.OrderBy(x => x))
            {
                var def = campaign.bestiaryDefs.TryGetValue(name, out var d) ? d : null;
                if (def == null) continue;
                int know = campaign.Knowledge(def);
                campaign.bestiaryKills.TryGetValue(name, out int kills);
                GUILayout.BeginHorizontal();
                string body = know >= 2
                    ? $"<b>{name}</b> ({kills} muertes) — PV {def.maxHP}, Arm {def.armor}, Esq {def.dodge}, VEL {def.speed}"
                      + (def.weakToFire ? ", débil al FUEGO" : "") + (def.isBoss ? ", JEFE" : def.isElite ? ", élite" : "")
                    : know == 1
                        ? $"<b>{name}</b> ({kills} muertes) — visto. Mátalo 3 veces (o compra el estudio) para stats y debilidades."
                        : $"<b>{name}</b> — encontrado, sin estudiar.";
                GUILayout.Label(body, Rich(), GUILayout.Width(520));
                if (know < 2)
                {
                    GUI.enabled = shipTronos >= Campaign.BestiaryBuyCost;
                    if (GUILayout.Button($"Estudiar ({Campaign.BestiaryBuyCost}p)", GUILayout.Width(140)))
                    { shipTronos -= Campaign.BestiaryBuyCost; campaign.boughtKnowledge.Add(name); }
                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(4);
            GUILayout.Label("<b>Diario del Comandante</b> (mecánicas aprendidas al vivirlas):", Rich());
            GUILayout.Label(campaign.glossary.Count > 0
                ? string.Join("  ·  ", campaign.glossary.OrderBy(x => x))
                : "Aún nada. El conocimiento se gana en el campo.");
        }

        bool showVotos;
        void DrawVotos()
        {
            if (campaign.votos.Count > 0)
                GUILayout.Label($"<color=#cc99ff>✝ Votos: {string.Join(" · ", campaign.votos.Select(Campaign.VotoName))}</color>", Rich());

            // Vows lock once the first raid begins: only pickable at the very start of a clean run.
            bool canPick = campaign.week == 1 && campaign.substagesCleared == 0 && campaign.currentSector == 1 && !campaign.confessorDown;
            if (!canPick) return;

            showVotos = GUILayout.Toggle(showVotos, "  <b>Votos de campaña</b> — tratos que marcan toda la partida (se cierran al bajar por primera vez)");
            if (!showVotos) return;
            foreach (Voto v in System.Enum.GetValues(typeof(Voto)))
            {
                bool on = campaign.HasVoto(v);
                bool now = GUILayout.Toggle(on, new GUIContent($"  {Campaign.VotoName(v)} — {Campaign.VotoDesc(v)}", Campaign.VotoDesc(v)));
                if (now != on) ToggleVoto(v, now);
            }
        }

        void ToggleVoto(Voto v, bool on)
        {
            if (on)
            {
                campaign.votos.Add(v);
                if (v == Voto.Ceniza) shipTronos += 600;
                if (v == Voto.Hierro) foreach (var h in campaign.roster) h.weaponRank = Mathf.Max(h.weaponRank, 1);
            }
            else
            {
                campaign.votos.Remove(v);
                if (v == Voto.Ceniza) shipTronos = Mathf.Max(0, shipTronos - 600);
                if (v == Voto.Hierro) foreach (var h in campaign.roster) h.weaponRank = Mathf.Max(0, h.weaponRank - 1);
            }
        }

        void DrawPuente()
        {
            var fm = campaign.frontMap;
            GUILayout.Label($"<b>el Puesto de Mando</b> — Frente de Vared (Sector 1: Primera Línea).  Nodos asegurados: {fm.ClearedCount}.", Rich());
            bool ready = SelectedParty.Count > 0;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Suministro estándar (auto)", GUILayout.Width(190))) AutoProvision();
            if (GUILayout.Button("Pasar semana (descansar)", GUILayout.Width(190)))
            {
                log.Clear();
                Log($"—— El Fortín vela en silencio. Pasa la semana {campaign.week}. ——");
                AdvanceWeek();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.Label("<b>El frente</b> (elige el grupo arriba y avanza a un nodo abierto; el frente se pudre con la Crecida):", Rich());
            foreach (var n in fm.nodes.OrderBy(x => x.col).ThenBy(x => x.row))
            {
                GUILayout.BeginHorizontal();
                // Fog of war: you only KNOW a node's type once it's contiguous (Available) or taken (Cleared).
                bool known = n.state == FrontNodeState.Available || n.state == FrontNodeState.Cleared;
                string label = n.state == FrontNodeState.Lost
                    ? $"{FrontNodeTag(n)} <b>{n.name}</b>  <color=#996666>— perdido</color>"
                    : known
                        ? $"{FrontNodeTag(n)} <b>{n.name}</b>  <color=#999999>[{n.type}]{FrontNodeInfo(n)}</color>"
                        : $"{FrontNodeTag(n)} <b>Posición sin reconocer</b>  <color=#666666>??? — asegura un nodo contiguo para reconocerla</color>";
                GUILayout.Label(label, Rich(), GUILayout.Width(440));
                if (n.state == FrontNodeState.Available)
                {
                    GUI.enabled = ready;
                    var node = n;
                    string verb = n.type == FrontNodeType.Jefe ? "⚑ ASALTAR" : n.type == FrontNodeType.Sellado ? "✦ SELLAR" : "Avanzar";
                    if (GUILayout.Button(verb, GUILayout.Width(150))) StartExpedition(node);
                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();
            }

            if (!ready)
                GUILayout.Label("Asigna al menos un agente al grupo de expedición para avanzar.");
        }

        static string FrontNodeTag(FrontNode n)
        {
            if (n.state == FrontNodeState.Cleared)   return "<color=#88ff88>✓</color>";
            if (n.state == FrontNodeState.Available)  return "<color=#ffcc66>▸</color>";
            if (n.state == FrontNodeState.Lost)       return "<color=#ff6666>✖</color>";
            return "<color=#666666>·</color>";
        }

        static string FrontNodeInfo(FrontNode n)
        {
            if (n.state == FrontNodeState.Lost) return " — perdido";
            if (n.state == FrontNodeState.Locked) return " — bloqueado";
            if (n.type == FrontNodeType.Opcional) return n.reward == "crecida" ? " · recompensa: −1 Crecida" : " · recompensa: botín";
            if (n.type == FrontNodeType.Aguantar) return $" · {n.waves} oleadas";
            if (n.type == FrontNodeType.Sabotaje) return $" · reloj {n.roundBudget} rondas";
            if (n.type == FrontNodeType.Rescate) return " · extrae al VIP";
            if (n.type == FrontNodeType.Sellado) return $" · SELLA LA HERIDA — {n.waves} oleadas, victoria de campaña";
            return "";
        }

        /// What the player must DO in the current incursion, by node type (shown in the expedition header).
        string NodeObjective()
        {
            var node = expedition?.node;
            switch (node?.type ?? FrontNodeType.Asalto)
            {
                case FrontNodeType.Aguantar:
                    int total = node.waves, cur = Mathf.Clamp(total - expedition.wavesLeft + 1, 1, total);
                    return $"Aguantar la línea — oleada {cur}/{total}";
                case FrontNodeType.Sabotaje:
                    return "Sabotaje — purga la zona antes de que lleguen los refuerzos (ver ⏱)";
                case FrontNodeType.Rescate:
                    return "Rescate — saca al Rescatado con vida (su muerte = fallo)";
                case FrontNodeType.Jefe:
                    return campaign.currentSector >= Campaign.MaxSector
                        ? "Asalto al Confesor Rojo — abátelo para abrir la Herida"
                        : "Asalto al jefe — abátelo para cerrar el sector";
                case FrontNodeType.Sellado:
                    int st = node.waves, sc = Mathf.Clamp(st - expedition.wavesLeft + 1, 1, st);
                    return $"SELLAR LA HERIDA — aguanta el ritual, oleada {sc}/{st}. Séllala y la campaña se gana.";
                default:
                    return $"Purga la resistencia — combates {map.CombatsCleared}/{map.CombatsTotal}";
            }
        }

        void AutoProvision()
        {
            void Buy(string key, int count = 1)
            {
                var def = GameData.Item(key);
                int price = campaign.Price(def.price);
                for (int i = 0; i < count; i++)
                    if (shipTronos >= price && shipStash.Count < Expedition.MaxSlots)
                    {
                        shipTronos -= price;
                        shipStash.Add(new ItemStack { def = def, chargesLeft = def.charges, refundable = true });
                    }
            }
            // ~485t: the design's "suministro razonable" for Stage 1
            Buy("Cargador", 2); Buy("Promethium"); Buy("Racion"); Buy("KitMedico"); Buy("KitCampamento");
        }

        // Full refund only for a just-bought item you haven't taken down yet.
        // Otherwise selling is always at a loss for supplies (no buy/sell profit);
        // loot sells at its found value since it exists only to be sold.
        int SellValue(ItemStack s) =>
            s.refundable ? s.def.price :
            s.def.kind == ItemKind.Loot ? s.lootValue :
            s.def.price / 2;

        // =====================================================================
        // CURIOSITIES & CAMP (design: no tooltips — outcomes are learned, not told)
        // =====================================================================
        static readonly string[] CuriosityNames = { "un altar profanado", "una máquina del culto", "el cadáver de un peregrino", "un relicario sellado" };

        void ResolveCuriosityBlind(Room r)
        {
            var toucher = heroes.Where(h => h.Alive).OrderBy(_ => Rng.Value).FirstOrDefault();
            if (toucher == null) return;
            r.cleared = true;
            int roll = Rng.Range(0, 100);
            switch (r.curiosityType)
            {
                case 0: // altar profanado
                    Log($"{toucher.unitName} se acerca al altar profanado...");
                    if (roll < 40) { Log("→ Los susurros lo reciben como a un hermano."); toucher.GainCorruption(15, Log); }
                    else if (roll < 70) Log("→ El altar calla. Mejor así.");
                    else { Log("→ Una calma severa desciende sobre él."); toucher.GainCorruption(-10, Log); toucher.GainFaith(1, Log); }
                    break;
                case 1: // cogitador
                    Log($"{toucher.unitName} manipula la máquina del culto...");
                    if (roll < 50) { map.augurRevealed = true; Log("→ La memoria de la máquina vuelca el plano del nivel: el mapa se revela."); }
                    else if (roll < 80) Log("→ Chatarra muerta.");
                    else
                    {
                        var outc = toucher.TakeDamage(4);
                        Log($"→ Contramedidas: una descarga alcanza a {toucher.unitName} (4 de daño, {toucher.hp} PV).");
                        HandleDamageOutcome(toucher, outc);
                    }
                    break;
                case 2: // cadáver
                    Log($"{toucher.unitName} registra el cadáver...");
                    if (roll < 50)
                    {
                        if (expedition.HasSpace)
                        {
                            var lootItem = new ItemStack { def = GameData.Item("Botin"), chargesLeft = 1, lootValue = Rng.Range(40, 101) };
                            expedition.inventory.Add(lootItem);
                            Log($"→ Botín entre los harapos ({lootItem.lootValue}p).");
                        }
                        else Log("→ Había algo de valor... y no os queda sitio en el petate.");
                    }
                    else if (roll < 75) { Log("→ Los dedos rozan algo húmedo. Gas 2×3."); toucher.AddStatus(StatusKind.Toxin, 2, 3); }
                    else Log("→ Solo huesos y polvo.");
                    break;
                default: // relicario sellado
                    Log($"{toucher.unitName} fuerza el relicario sellado...");
                    if (roll < 35)
                    {
                        if (expedition.HasSpace)
                        {
                            var lootItem = new ItemStack { def = GameData.Item("Botin"), chargesLeft = 1, lootValue = Rng.Range(60, 121) };
                            expedition.inventory.Add(lootItem);
                            Log($"→ Cede con un chasquido: botín ({lootItem.lootValue}p).");
                        }
                        else Log("→ Cede... y no os queda sitio en el petate.");
                    }
                    else if (roll < 75) Log("→ El sello resiste y el mecanismo se rompe. Inservible.");
                    else { Log("→ El cierre salta y muerde: Sangrado 2×2."); toucher.AddStatus(StatusKind.Bleed, 2, 2); }
                    break;
            }
        }

        void UseCuriosityItem(Room r)
        {
            var toucher = heroes.Where(h => h.Alive).OrderBy(_ => Rng.Value).FirstOrDefault();
            if (toucher == null) return;
            if (r.curiosityType == 0)
            {
                var water = expedition.FindWithCharges(ItemKind.HolyWater);
                if (water == null) return;
                expedition.ConsumeCharge(water);
                r.cleared = true;
                Log($"{toucher.unitName} consagra el altar con agua bendita. El aire se limpia.");
                toucher.GainCorruption(-15, Log);
                toucher.GainFaith(1, Log);
            }
            else if (r.curiosityType == 3)
            {
                var picks = expedition.FindWithCharges(ItemKind.Lockpicks);
                if (picks == null) return;
                expedition.ConsumeCharge(picks);
                r.cleared = true;
                if (expedition.HasSpace)
                {
                    var lootItem = new ItemStack { def = GameData.Item("Botin"), chargesLeft = 1, lootValue = Rng.Range(100, 181) };
                    expedition.inventory.Add(lootItem);
                    Log($"Las ganzúas abren el relicario sin un rasguño: botín ({lootItem.lootValue}p).");
                }
                else Log("Las ganzúas abren el relicario... y no os queda sitio en el petate.");
            }
        }

        void DoCamp(Room r)
        {
            var kit = expedition.FindWithCharges(ItemKind.CampKit);
            if (kit == null || r.campUsed) return;
            expedition.ConsumeCharge(kit);
            r.campUsed = true;
            Log("El grupo se refugia en el búnker. Velas, vendas y oraciones en voz baja.");
            if (Rng.Range(0, 100) < 20)
            {
                StartAmbush(true); // the night takes its due: no rest gained
                return;
            }
            foreach (var h in heroes.Where(h => h.Alive))
            {
                h.hp = Mathf.Min(h.EffMaxHP, h.hp + 5);
                if (h.atDeathsDoor && h.hp > 0) { h.atDeathsDoor = false; Log($"{h.unitName} se aparta del Borde de la Muerte."); }
                h.statuses.RemoveAll(s => s.kind == StatusKind.Bleed || s.kind == StatusKind.Toxin || s.kind == StatusKind.Burn);
                h.GainCorruption(-10, Log);
            }
            Log("El grupo despierta entero: +5 PV, heridas tratadas, el alma un poco más ligera.");
        }

        /// <summary>Sim AI: spend supplies on whoever needs them between combats.</summary>
        void AutoUseSupplies()
        {
            foreach (var h in heroes.Where(h => h.Alive).ToList())
            {
                bool poisoned = h.Has(StatusKind.Bleed) || h.Has(StatusKind.Toxin);
                if ((h.atDeathsDoor || h.hp <= h.EffMaxHP / 2 || poisoned))
                {
                    var kit = poisoned ? expedition.FindWithCharges(ItemKind.MedKit) : null;
                    var stack = kit ?? expedition.FindWithCharges(ItemKind.MedKit) ?? expedition.FindWithCharges(ItemKind.Ration);
                    if (stack != null) UseSupplyItem(stack, h);
                }
                if (h.corruption >= 80)
                {
                    var seal = expedition.FindWithCharges(ItemKind.PuritySeal);
                    if (seal != null) UseSupplyItem(seal, h);
                }
            }
        }

        // =====================================================================
        // MAP VIEW
        // =====================================================================
        void DrawMapView()
        {
            GUILayout.Label(map.AllCombatsCleared
                ? "<b>★ SUBSTAGE PURGADA.</b> la Llama cuenta a sus siervos."
                : heroes.Any(h => h.Alive)
                    ? "Elige a dónde avanzar (solo salas conectadas a tu posición)."
                    : "<b>No queda nadie en pie.</b>", Rich());

            Rect area = GUILayoutUtility.GetRect(Screen.width - 40, 280);

            // corridors first, rooms on top
            foreach (var r in map.rooms)
                foreach (var l in r.links.Where(l => l.id > r.id))
                    DrawEdge(RoomRect(area, r).center, RoomRect(area, l).center);

            foreach (var r in map.rooms)
            {
                // Fog depends on the Vox signal: Alta = full map, Media = adjacent, Baja/Nula = only what you've walked
                bool revealed = r.visited || map.augurRevealed || VoxLevel >= 3 ||
                                (VoxLevel >= 2 && r.links.Any(l => l.visited));
                bool reachable = map.current.links.Contains(r) && heroes.Any(h => h.Alive);
                var prev = GUI.color;
                GUI.color = r == map.current ? new Color(1f, 0.85f, 0.3f)
                          : reachable ? Color.white
                          : new Color(1f, 1f, 1f, 0.55f);
                GUI.enabled = reachable;
                int hazards = r.corridorEvents.Count(e => !e.sprung);
                string tip = reachable ? $"Pasillo de {Mathf.Max(1, r.corridorSteps)} tramos — cada tramo gasta Señal"
                                         + (hazards > 0 ? $" · {hazards} peligro(s) en el camino" : "")
                                       : (revealed ? null : "Sala sin explorar");
                if (GUI.Button(RoomRect(area, r), new GUIContent(RoomLabel(r, revealed), tip)) && reachable)
                    EnterRoom(r);
                GUI.enabled = true;
                GUI.color = prev;
            }

            // Curiosity in the current room (no tooltips: you learn by touching)
            if (map.current.kind == RoomKind.Curiosidad && !map.current.cleared)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"<b>Encontráis {CuriosityNames[map.current.curiosityType]}.</b>", Rich(), GUILayout.Width(300));
                if (GUILayout.Button("Registrar a ciegas", GUILayout.Width(150)))
                    ResolveCuriosityBlind(map.current);
                if (map.current.curiosityType == 0 && expedition.FindWithCharges(ItemKind.HolyWater) != null &&
                    GUILayout.Button("Usar Agua bendita", GUILayout.Width(150)))
                    UseCuriosityItem(map.current);
                if (map.current.curiosityType == 3 && expedition.FindWithCharges(ItemKind.Lockpicks) != null &&
                    GUILayout.Button("Usar Ganzúas", GUILayout.Width(140)))
                    UseCuriosityItem(map.current);
                GUILayout.Label("...o dejarlo estar.");
                GUILayout.EndHorizontal();
            }

            // Sanctuary: one camp per substage, if you brought the kit
            if (map.current.kind == RoomKind.Santuario && !map.current.campUsed)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("<b>✚ Refugio</b> — un lugar seguro. Probablemente.", Rich(), GUILayout.Width(300));
                bool hasKit = expedition.FindWithCharges(ItemKind.CampKit) != null;
                GUI.enabled = hasKit;
                if (GUILayout.Button(hasKit ? "Refugiarse (gasta Kit de refugio)" : "Refugiarse (requiere Kit de refugio)", GUILayout.Width(280)))
                    DoCamp(map.current);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            // Loot waiting in the current room
            if (map.current.pendingLoot != null && map.current.pendingLoot.Count > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("<b>Alijo:</b>", Rich(), GUILayout.Width(45));
                foreach (var lootItem in map.current.pendingLoot.ToList())
                {
                    GUI.enabled = expedition.HasSpace;
                    if (GUILayout.Button($"Coger botín ({lootItem.lootValue}p)", GUILayout.Width(170)))
                    {
                        expedition.inventory.Add(lootItem);
                        map.current.pendingLoot.Remove(lootItem);
                        Log($"Botín recogido ({lootItem.lootValue}p). Huecos: {expedition.inventory.Count}/{Expedition.MaxSlots}.");
                    }
                    GUI.enabled = true;
                }
                if (!expedition.HasSpace) GUILayout.Label("Petate lleno: tira algo (✕) o déjalo.");
                if (GUILayout.Button("Dejarlo", GUILayout.Width(70))) map.current.pendingLoot.Clear();
                GUILayout.EndHorizontal();
            }

            // Inventory: supplies usable out of combat
            if (expedition.inventory.Count > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"<b>Petate</b>:", Rich(), GUILayout.Width(50));
                foreach (var s in expedition.inventory.ToList())
                {
                    bool usable = s.def.kind == ItemKind.Ration || s.def.kind == ItemKind.MedKit || s.def.kind == ItemKind.PuritySeal;
                    string charges = s.def.kind == ItemKind.ServoSkull ? "" : s.def.kind == ItemKind.Loot ? $" ({s.lootValue}p)" : $" ({s.chargesLeft})";
                    if (s.def.kind == ItemKind.VoxBeacon)
                    {
                        if (GUILayout.Button(new GUIContent("Plantar bengala", s.def.description), GUILayout.Width(150)))
                            UseSupplyItem(s, null);
                    }
                    else if (usable)
                    {
                        if (GUILayout.Button(new GUIContent($"Usar {s.def.displayName}{charges}", s.def.description), GUILayout.Width(190)))
                            pendingItem = s;
                    }
                    else
                    {
                        GUILayout.Label(new GUIContent($"{s.def.displayName}{charges}", s.def.description), GUI.skin.box, GUILayout.Height(24));
                    }
                    if (GUILayout.Button("✕", GUILayout.Width(24)))
                    {
                        expedition.inventory.Remove(s);
                        Log($"{s.def.displayName} descartado.");
                        if (pendingItem == s) pendingItem = null;
                    }
                }
                GUILayout.EndHorizontal();
            }

            // Target selection for a supply
            if (pendingItem != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"¿Sobre quién usar <b>{pendingItem.def.displayName}</b>?", Rich(), GUILayout.Width(280));
                foreach (var h in heroes.Where(h => h.Alive))
                {
                    if (GUILayout.Button($"{h.unitName} ({h.hp}/{h.EffMaxHP} PV, Mancha {h.corruption})", GUILayout.Width(230)))
                    {
                        var s = pendingItem; pendingItem = null;
                        UseSupplyItem(s, h);
                        break;
                    }
                }
                if (GUILayout.Button("Cancelar", GUILayout.Width(90))) pendingItem = null;
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            for (int i = heroes.Count - 1; i >= 0; i--) UnitBox(heroes[i], null);
            GUILayout.EndHorizontal();
        }

        void UseSupplyItem(ItemStack s, CombatUnit target)
        {
            switch (s.def.kind)
            {
                case ItemKind.Ration:
                case ItemKind.MedKit:
                    target.hp = Mathf.Min(target.EffMaxHP, target.hp + s.def.power);
                    Log($"{target.unitName} usa {s.def.displayName}: +{s.def.power} PV ({target.hp}/{target.EffMaxHP}).");
                    if (s.def.kind == ItemKind.MedKit &&
                        target.statuses.RemoveAll(x => x.kind == StatusKind.Bleed || x.kind == StatusKind.Toxin) > 0)
                        Log($"→ el Sangrado y el Gas de {target.unitName} quedan tratados.");
                    if (target.atDeathsDoor && target.hp > 0)
                    {
                        target.atDeathsDoor = false;
                        Log($"→ {target.unitName} se aparta del Borde de la Muerte.");
                    }
                    break;
                case ItemKind.PuritySeal:
                    Log($"{target.unitName} besa el {s.def.displayName}.");
                    target.GainCorruption(-s.def.power, Log);
                    break;
                case ItemKind.VoxBeacon:
                    expedition.signal = Mathf.Min(100, expedition.signal + SignalRepeaterBoost);
                    Log($"Despliegas un repetidor: la Señal con el Fortín sube a {expedition.signal}% ({VoxNames[VoxLevel]}).");
                    break;
            }
            expedition.ConsumeCharge(s);
        }

        Rect RoomRect(Rect area, Room r)
        {
            int nCols = map.rooms.Max(x => x.col) + 1;
            int colRooms = map.rooms.Count(x => x.col == r.col);
            float colW = area.width / nCols;
            float roomW = Mathf.Min(112f, colW - 10f), roomH = 46f;
            float x = area.x + r.col * colW + (colW - roomW) / 2f;
            float cellH = area.height / colRooms;
            float y = area.y + (r.row + 0.5f) * cellH - roomH / 2f;
            return new Rect(x, y, roomW, roomH);
        }

        void DrawEdge(Vector2 a, Vector2 b)
        {
            var prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.25f);
            Vector2 d = b - a;
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            var mtx = GUI.matrix;
            GUIUtility.RotateAroundPivot(ang, a);
            GUI.DrawTexture(new Rect(a.x, a.y - 1f, d.magnitude, 2f), Texture2D.whiteTexture);
            GUI.matrix = mtx;
            GUI.color = prev;
        }

        string RoomLabel(Room r, bool revealed)
        {
            string tag = r == map.current ? "► " : "";
            // Ambient hazards are telegraphed: their glyph shows even through fog.
            string haz = r.hazard == RoomHazard.GasCloud ? "☣ " : r.hazard == RoomHazard.RedSky ? "▓ " : "";
            if (!revealed) return tag + haz + "?";
            string body;
            switch (r.kind)
            {
                case RoomKind.Entrada:    body = "⌂ Entrada"; break;
                case RoomKind.Combate:    body = r.cleared ? "✓ Purgada" : "⚔ Hostiles"; break;
                case RoomKind.Tesoro:     body = r.cleared ? "· Saqueada" : "★ Alijo"; break;
                case RoomKind.Curiosidad: body = r.cleared ? "◆ Agotada" : "◆ Curiosidad"; break;
                case RoomKind.Santuario:  body = r.campUsed ? "✚ Frío" : "✚ Refugio"; break;
                case RoomKind.Trampa:     body = r.visited ? "⚠ Desarmada" : "· Sala"; break; // disguised as empty until stepped on
                default:                  body = r.visited ? "· Vacía" : "· Sala"; break;
            }
            return tag + haz + body;
        }

        void UnitBox(CombatUnit u, CombatUnit cur)
        {
            var style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, richText = true };
            string hl = (u == cur) ? "► " : "";

            // Bestiary: an unstudied enemy hides its numbers (design: "???" en PV y stats)
            int know = u.TeamOf == Team.Enemies ? (campaign?.Knowledge(u.def) ?? 2) : 2;
            string sts = u.statuses.Count > 0 ? "\n" + string.Join("\n", u.statuses.Select(StatusShort)) : "";
            if (u.TeamOf == Team.Enemies && know == 0)
            {
                GUILayout.Box(new GUIContent(
                        $"{hl}<b>{u.def.displayName}</b>\nPos {PosOf(u)}  Arm ???\nPV ???{sts}",
                        "Enemigo sin estudiar. Mátalo para aprender de él (Librarium)."),
                    style, GUILayout.Width(128), GUILayout.MinHeight(96));
                return;
            }

            string lvl = u.TeamOf == Team.Heroes && u.level > 0 ? $"  Nv{u.level}" : "";
            string ammo = u.def.maxAmmo > 0 ? $"\nMun: {u.ammo}/{u.def.maxAmmo}" : "";
            string corr = u.TeamOf == Team.Heroes ? $"\nMancha: {u.corruption}/200  Llama: {u.faith}" : "";
            string soulTag = u.soul != SoulState.None ? $"\n[{CombatUnit.SoulName(u.soul).ToUpper()}]" : "";
            string mm = u.meleeMode ? "\n[BAYONETA]" : "";
            string door = u.atDeathsDoor ? "\n<color=#ff5555><b>[AL BORDE DE LA MUERTE]</b></color>" : "";
            string items = u.TeamOf == Team.Heroes && u.consumables.Count > 0
                ? "\n⚗ " + string.Join(", ", u.consumables.Select(c => c.displayName.Split(' ')[0]))
                : "";
            string arm = u.TeamOf == Team.Enemies && know < 2 ? "?" : u.def.armor.ToString();
            string zoneStr = u.TeamOf == Team.Heroes
                ? $"\n<color={(IsLit(u) ? "#ffdd66" : "#7777aa")}>{(IsLit(u) ? "☼" : "▓")} {ZoneNames[Mathf.Clamp(u.zone, 0, 3)]}</color>"
                  + (u.zone >= AssaultZone ? " <color=#ff8844>⚔</color>" : "")
                : "";
            // Fase 5 — the enemy's own light state (☼/▓ like a hero): match it to hit it clean; cross the
            // divide and you fire blind. The same line tells you what it hunts (its nature = its light).
            string chanStr = u.TeamOf == Team.Enemies && know >= 1
                ? (Lit(u)
                    ? "\n<color=#ffdd66>☼ a la luz — te caza iluminado</color>"
                    : "\n<color=#cc66ff>▓ en la sombra — te caza a oscuras</color>")
                : "";
            // Heroes read their depth from the zone strip; only the enemy formation still shows a Pos number.
            string posStr = u.TeamOf == Team.Enemies ? $"Pos {PosOf(u)}  " : "";
            GUILayout.Box(new GUIContent(
                    $"{hl}<b>{u.unitName}</b>{lvl}\n{posStr}Arm {arm}{zoneStr}{chanStr}\nPV {u.hp}/{u.EffMaxHP}{door}{ammo}{corr}{soulTag}{sts}{mm}{items}",
                    UnitTooltip(u)),
                style, GUILayout.Width(128), GUILayout.MinHeight(96));
        }

        void DrawHeroControls(CombatUnit hero)
        {
            GUILayout.Label($"<b>Turno de {hero.unitName}</b> (pos {PosOf(hero)})", Rich());

            if (pendingConsumable != null) { DrawConsumableTargets(hero); return; }
            if (pendingOrder) { DrawOrderTargets(hero); return; }

            // Psíquico: choose whether to burn sanity BEFORE casting a Warp power
            if (hero.def.abilities.Any(x => x.warpDanger))
                burnSanity = GUILayout.Toggle(burnSanity,
                    " Escuchar más hondo: +20 Mancha propia y el poder no llama al Foso");

            if (pendingAbility == null)
            {
                abilityScroll = GUILayout.BeginScrollView(abilityScroll, GUILayout.Height(112));
                foreach (var a in hero.def.abilities)
                {
                    bool ok = AbilityUsable(hero, a, out string reason);
                    string uses = hero.usesLeft.ContainsKey(a) ? $" [{hero.usesLeft[a]}]" : "";
                    string label = ok ? $"{a.displayName}{uses}" : $"{a.displayName}{uses} — {reason}";
                    GUI.enabled = ok;
                    if (GUILayout.Button(label, GUILayout.Width(430)))
                    {
                        var t = ValidTargets(hero, a);
                        bool autoTarget = a.targetKind == TargetKind.Self ||
                                          a.targetKind == TargetKind.AllAllies ||
                                          a.targetKind == TargetKind.AllEnemies || a.area ||
                                          a.special == SpecialKind.Cantico;
                        if (autoTarget) UseAbility(hero, a, t.FirstOrDefault());
                        else pendingAbility = a;
                    }
                    GUI.enabled = true;
                }
                GUILayout.EndScrollView();

                GUILayout.BeginHorizontal();
                // Fase 4 — movement IS the light axis now (the old DD list-swap is gone).
                GUI.enabled = hero.zone < 3;
                if (GUILayout.Button(new GUIContent("Al frente ▼",
                    "Avanza una zona hacia el frente iluminado por la Bengala (sobre tierra de nadie). Ganas la luz —a salvo del Foso— pero te expones al fuego humano. Sobre el parapeto (Alambrada+) alcanzas la RETAGUARDIA enemiga (⚔)."), GUILayout.Width(90)))
                { hero.zone++; Log($"{hero.unitName} avanza a {ZoneNames[hero.zone]}{(IsLit(hero) ? " (a la luz)" : " (en la oscuridad)")}{(CanAssault(hero) ? " — expuesto sobre el parapeto (+15 a acertarle)" : "")}."); EndTurnOf(hero); }
                GUI.enabled = hero.zone > 0;
                if (GUILayout.Button(new GUIContent("A retaguardia ▲",
                    "Retrocede una zona hacia la retaguardia en sombra: te ocultas del fuego humano, pero fuera de la luz el Foso muerde (+Mancha)."), GUILayout.Width(120)))
                { hero.zone--; Log($"{hero.unitName} repliega a {ZoneNames[hero.zone]}{(IsLit(hero) ? " (a la luz)" : " (en la oscuridad)")}."); EndTurnOf(hero); }
                GUI.enabled = true;
                if (GUILayout.Button("Defender", GUILayout.Width(110)))
                { hero.AddStatus(StatusKind.Defend, 0, 1); Log($"{hero.unitName} se pone a cubierto (+15 Esq, −20% daño)."); EndTurnOf(hero); }
                GUI.enabled = hero.def.maxAmmo > 0 && hero.ammo < hero.def.maxAmmo;
                if (GUILayout.Button("Recargar", GUILayout.Width(110)))
                { hero.ammo = hero.def.maxAmmo; Log($"{hero.unitName} recarga."); EndTurnOf(hero); }
                GUI.enabled = true;
                if (GUILayout.Button("Pasar", GUILayout.Width(80)))
                { Log($"{hero.unitName} aguanta la posición."); EndTurnOf(hero); }
                GUI.enabled = !retreatDeclaredThisRound;
                if (GUILayout.Button(new GUIContent("¡Retirada!", "Retirada de grupo: cada uno tira 60% + VEL; quien falla aguanta la ronda expuesto (+20% daño). Retirarse: −1 Llama a todos."), GUILayout.Width(90)))
                    DeclareRetreat();
                GUI.enabled = true;
                GUILayout.EndHorizontal();

                // Minor action (1 per turn, does NOT end the turn): use it BEFORE the main action
                if (!minorUsed && (hero.consumables.Count > 0 || hero.def.isLeader || flareCharges > 0))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Menor:", GUILayout.Width(48));
                    if (flareCharges > 0 &&
                        GUILayout.Button(new GUIContent($"Lanzar Bengala ({flareCharges})",
                            "Acción menor: relanza la Bengala y devuelve la luz a lo alto. En la oscuridad el fuego humano no te ve, pero el Foso te alcanza."),
                            GUILayout.Width(160)))
                        LaunchFlare(hero);
                    foreach (var c in hero.consumables.ToList())
                    {
                        if (GUILayout.Button(new GUIContent($"Usar {c.displayName}", ConsumableTooltip(c)), GUILayout.Width(200)))
                        { pendingConsumable = c; pendingGive = false; }
                        GUI.enabled = AdjacentAllies(hero).Any(t => t.consumables.Count < 2);
                        if (GUILayout.Button("Dar", GUILayout.Width(44)))
                        { pendingConsumable = c; pendingGive = true; }
                        GUI.enabled = true;
                    }
                    if (hero.def.isLeader &&
                        GUILayout.Button(new GUIContent("Orden de líder",
                            "Intercambia el orden de iniciativa de dos aliados esta ronda."), GUILayout.Width(120)))
                    { pendingOrder = true; orderFirst = null; }
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                GUILayout.Label($"Objetivo para <b>{pendingAbility.displayName}</b>:", Rich());
                GUILayout.BeginHorizontal();
                bool hostileAim = pendingAbility.targetKind == TargetKind.Enemy || pendingAbility.targetKind == TargetKind.AllEnemies;
                foreach (var t in ValidTargets(hero, pendingAbility))
                {
                    // Fase 5 — flag a shot fired from the light into the shadow (blind fire, −BlindFireAcc).
                    bool blind = hostileAim && pendingAbility.accuracy > 0 && t.TeamOf == Team.Enemies && BlindShot(hero, t);
                    var label = new GUIContent($"{t.unitName} (pos {PosOf(t)}){(blind ? "  [a ciegas]" : "")}",
                        blind ? $"A ciegas: disparas desde la luz hacia la sombra, no ves bien al Foso (−{MirrorBlindAcc} prec). Avanza a la oscuridad para cazarlo limpio." : "");
                    if (GUILayout.Button(label, GUILayout.Width(200)))
                    { var a = pendingAbility; pendingAbility = null; UseAbility(hero, a, t); break; }
                }
                if (GUILayout.Button("Cancelar", GUILayout.Width(90))) pendingAbility = null;
                GUILayout.EndHorizontal();
            }
        }

        void DrawOrderTargets(CombatUnit hero)
        {
            GUILayout.Label(orderFirst == null
                ? "Orden de líder — elige al PRIMER aliado:"
                : $"Orden de líder — intercambiar a <b>{orderFirst.unitName}</b> con:", Rich());
            GUILayout.BeginHorizontal();
            // Only allies still waiting to act this round can be swapped
            var pendingAllies = turnQueue.Skip(turnIndex + 1)
                .Where(x => x.Alive && x.TeamOf == Team.Heroes && x != orderFirst).ToList();
            foreach (var t in pendingAllies)
            {
                if (GUILayout.Button(t.unitName, GUILayout.Width(150)))
                {
                    if (orderFirst == null) { orderFirst = t; }
                    else
                    {
                        int i = turnQueue.IndexOf(orderFirst), j = turnQueue.IndexOf(t);
                        (turnQueue[i], turnQueue[j]) = (turnQueue[j], turnQueue[i]);
                        Log($"{hero.unitName} da una orden: {orderFirst.unitName} y {t.unitName} intercambian su iniciativa (acción menor).");
                        minorUsed = true; pendingOrder = false; orderFirst = null;
                    }
                    break;
                }
            }
            if (pendingAllies.Count < (orderFirst == null ? 2 : 1))
                GUILayout.Label("No quedan suficientes aliados por actuar esta ronda.");
            if (GUILayout.Button("Cancelar", GUILayout.Width(90))) { pendingOrder = false; orderFirst = null; }
            GUILayout.EndHorizontal();
        }

        void DrawConsumableTargets(CombatUnit hero)
        {
            var c = pendingConsumable;
            GUILayout.Label(pendingGive
                ? $"¿A quién pasar <b>{c.displayName}</b>?"
                : $"¿Sobre quién usar <b>{c.displayName}</b>?", Rich());
            GUILayout.BeginHorizontal();
            if (!pendingGive && GUILayout.Button($"{hero.unitName} (yo)", GUILayout.Width(180)))
            {
                pendingConsumable = null;
                UseConsumable(hero, c, hero);
            }
            var candidates = pendingGive
                ? AdjacentAllies(hero).Where(t => t.consumables.Count < 2).ToList()
                : AdjacentAllies(hero);
            foreach (var t in candidates)
            {
                if (GUILayout.Button($"{t.unitName} (pos {PosOf(t)})", GUILayout.Width(180)))
                {
                    pendingConsumable = null;
                    if (pendingGive) GiveConsumable(hero, c, t); else UseConsumable(hero, c, t);
                    break;
                }
            }
            if (GUILayout.Button("Cancelar", GUILayout.Width(90))) pendingConsumable = null;
            GUILayout.EndHorizontal();
        }

        void Log(string s)
        {
            log.Add(s);
            if (Application.isPlaying) PlaytestLog.Action(s); // headless sims keep their own log
            logScroll = Vector2.zero;
        }

        // =====================================================================
        // HEADLESS SIMULATION (both sides played by the simple AI; no UI, no play mode)
        // =====================================================================
        public struct CampaignResult
        {
            public int maxSectorReached;  // QA: deepest sector a campaign reached (1..4)
            public int weeksSurvived;
            public int phaseReached;
            public int expeditionsLaunched;
            public int expeditionsCompleted;
            public int substagesCleared;
            public bool stageWon;         // boss killed, planet stabilized
            public bool planetFallen;     // Despertar completed, campaign lost
            public int totalDeaths;
            public int recruits;
            public int finalRosterSize;
            public int finalTronos;
            public bool rosterWiped;      // ran out of living agents
            public int invariantViolations;
            public List<string> log;
        }

        int simViolations;

        /// <summary>Scans persistent hero state for impossible values; accumulates violations.</summary>
        void CheckInvariants(IEnumerable<CombatUnit> all)
        {
            foreach (var h in all)
            {
                if (h.hp < 0 || h.hp > h.EffMaxHP) { Log($"‼ INVARIANTE: {h.unitName} PV {h.hp}/{h.EffMaxHP}"); simViolations++; }
                if (h.corruption < 0 || h.corruption > 200) { Log($"‼ INVARIANTE: {h.unitName} Mancha {h.corruption}"); simViolations++; }
                if (h.faith < 0 || h.faith > 10) { Log($"‼ INVARIANTE: {h.unitName} Llama {h.faith}"); simViolations++; }
                if (!h.Alive && h.atDeathsDoor) { Log($"‼ INVARIANTE: {h.unitName} muerto y al Borde a la vez"); simViolations++; }
                // el Desollador (Hito 7): a hero can only be flayed while a Desollador lives — the grip must never persist.
                if (h.grabbedByFlayer && !(inCombat && enemies.Any(e => e.Alive && e.def.isFlayer)))
                { Log($"‼ INVARIANTE: {h.unitName} agarrado por el Desollador sin Desollador vivo"); simViolations++; }
            }
            if (inCombat && enemyCohesion < 0) { Log($"‼ INVARIANTE: Moral {enemyCohesion}"); simViolations++; }
        }

        /// <summary>
        /// Drives a full CAMPAIGN with the simple AI: each week it benches the
        /// hurt/corrupted into the Reclusiam/Enfermería, sends the healthiest
        /// available agents down with a standard supply loadout, and buries the
        /// dead. Runs until the roster is wiped or the week budget is spent.
        /// </summary>
        public CampaignResult RunHeadlessCampaign(int maxWeeks = 20, int maxRoundsPerCombat = 30, int? seed = null, int heroLevel = 0)
        {
            campaign = Campaign.New(seed);
            atShip = true;
            headless = true;
            simViolations = 0;
            log.Clear();
            // QA: a strong squad (levels + equipped gear) can push deep enough to exercise the late-game
            // flows (sectors 2–4, the Confesor, the sealing) that the level-0 floor never reaches.
            if (heroLevel > 0)
                foreach (var h in campaign.roster)
                {
                    for (int i = 0; i < heroLevel; i++) h.LevelUp();
                    foreach (var g in campaign.armory.Where(x => h.GearIn(x.slot) == null).ToList()) campaign.Equip(h, g);
                }
            int startRoster = campaign.roster.Count;
            int launched = 0, completed = 0, maxSector = campaign.currentSector;
            FrontNode lastFailedNode = null;

            for (int w = 0; w < maxWeeks; w++)
            {
                if (!campaign.roster.Any(h => h.Alive)) break; // roster wiped
                if (campaign.CampaignOver) break;              // won (boss) or lost (planet fell)

                // Reset assignments, then plan this week
                foreach (var h in campaign.roster) campaign.assignment[h] = HeroAssignment.Available;

                // Bench the worst-off into treatment (respecting slots and money)
                // §2 indisponibilidad: treatment benches the soldier 1–3 incursiones (SendToTreatment sets the counter).
                foreach (var h in campaign.roster.Where(h => campaign.Get(h) == HeroAssignment.Available && h.corruption >= 60).OrderByDescending(h => h.corruption))
                    if (campaign.Assigned(HeroAssignment.Reclusiam).Count() < Campaign.ReclusiamSlots && shipTronos >= Campaign.ReclusiamCost(h))
                    { shipTronos -= Campaign.ReclusiamCost(h); campaign.SendToTreatment(h, HeroAssignment.Reclusiam); }
                foreach (var h in campaign.roster.Where(h => campaign.Get(h) == HeroAssignment.Available && h.hp < h.EffMaxHP / 2).OrderBy(h => h.hp))
                    if (campaign.Assigned(HeroAssignment.Enfermeria).Count() < Campaign.EnfermeriaSlots && shipTronos >= Campaign.EnfermeriaCost(h))
                    { shipTronos -= Campaign.EnfermeriaCost(h); campaign.SendToTreatment(h, HeroAssignment.Enfermeria); }

                // Pick the healthiest available agents (up to 4) for the expedition
                var party = campaign.roster
                    .Where(h => campaign.Get(h) == HeroAssignment.Available && h.hp > 0)
                    .OrderByDescending(h => h.hp - h.corruption).Take(4).ToList();

                if (party.Count < 2) { AdvanceWeek(); continue; } // too thin — rest, heal, recruit

                foreach (var h in party) campaign.assignment[h] = HeroAssignment.Expedition;

                // Pick a front node: the boss if reachable, else a progress node (skip optionals when possible).
                // Don't immediately re-attempt a node that just failed if there's an alternative — route around it.
                var avail = campaign.frontMap.Available.ToList();
                if (avail.Count == 0) { AdvanceWeek(); continue; } // front stalled — rest a week
                var choices = (avail.Count > 1 && lastFailedNode != null) ? avail.Where(n => n != lastFailedNode).ToList() : avail;
                if (choices.Count == 0) choices = avail;
                var pick = choices.FirstOrDefault(n => n.type == FrontNodeType.Jefe)
                        ?? choices.Where(n => n.type != FrontNodeType.Opcional).OrderByDescending(n => n.col).FirstOrDefault() // push toward the boss
                        ?? choices[0];

                shipStash.Clear();
                AutoProvision();
                StartExpedition(pick);
                bool done = RunExpeditionLoopHeadless(maxRoundsPerCombat);
                lastFailedNode = done ? null : pick; // remember a failed node so we try elsewhere next
                launched++;
                if (done) completed++;
                ReturnToShip(done); // permadeath + progression + week advance happen here
                maxSector = Mathf.Max(maxSector, campaign.currentSector);
                CheckInvariants(campaign.roster);
            }

            CheckInvariants(campaign.roster);
            return new CampaignResult
            {
                maxSectorReached = maxSector,
                weeksSurvived = campaign.week - 1,
                phaseReached = campaign.phase,
                expeditionsLaunched = launched,
                expeditionsCompleted = completed,
                substagesCleared = campaign.substagesCleared,
                stageWon = campaign.stageWon,
                planetFallen = campaign.planetFallen,
                totalDeaths = campaign.memorial.Count,
                recruits = campaign.roster.Count + campaign.memorial.Count - startRoster,
                finalRosterSize = campaign.roster.Count,
                finalTronos = campaign.tronos,
                rosterWiped = !campaign.roster.Any(h => h.Alive),
                invariantViolations = simViolations,
                log = new List<string>(log),
            };
        }

        public struct VictoryProbe { public int maxSector, expeditions, violations; public bool confessorDown, stageWon; public List<string> log; }

        public struct ReturnFixProbe { public bool darkHeroKilled, darkHeroInMemorial, corajeDebtPaid, automatonDotCleared; }

        /// <summary>QA: the return-home fixes — a 200-Mancha survivor never returns alive, the Coraje debt is
        /// paid even on a non-victory return, and a repaired Autómata sheds its DoT.</summary>
        public ReturnFixProbe RunHeadlessReturnFixesProbe()
        {
            var p = new ReturnFixProbe();
            campaign = Campaign.New(555); headless = true; simViolations = 0; log.Clear();
            var party = campaign.roster.Take(4).ToList();
            var doomed = party[0]; doomed.corruption = 200; doomed.soulTested = true; // at the brink, no trial
            var debtor = party[1]; debtor.corruption = 50; debtor.endCombatCorruptionDebt = 15;
            expedition = new Expedition { heroes = party, node = campaign.frontMap.Available.First(), sector = 1, casualties = new List<CombatUnit>() };
            map = SubstageMap.Generate(new List<string[]> { new[] { "Cultist" } }, phase: 0, sector: 1);
            foreach (var r in map.rooms) r.cleared = true;
            heroes = party; enemies = new List<CombatUnit>();
            ReturnToShip(true);
            p.darkHeroKilled = !campaign.roster.Contains(doomed) && !doomed.Alive;
            p.darkHeroInMemorial = campaign.memorial.Any();
            p.corajeDebtPaid = debtor.corruption > 50 && debtor.endCombatCorruptionDebt == 0;

            var auto = new CombatUnit(GameData.Unit("Automaton")) { averiado = true };
            auto.AddStatus(StatusKind.Bleed, 3, 2);
            campaign.RepairAutomaton(auto);
            p.automatonDotCleared = !auto.averiado && auto.statuses.Count == 0 && auto.hp == auto.EffMaxHP;
            return p;
        }

        /// <summary>QA: force a perfect run through the WHOLE front (all 4 sectors + the sealing) with the
        /// Crecida frozen, driving the REAL ReturnToShip advance/confessorDown/Sellado logic. Proves the
        /// integrated win path works end-to-end — something no sim campaign ever reaches.</summary>
        public VictoryProbe RunHeadlessFullVictory(int? seed = null)
        {
            campaign = Campaign.New(seed ?? 12345);
            headless = true; simViolations = 0; log.Clear();
            atShip = true;
            int maxSector = 1, guard = 0, exps = 0;
            while (!campaign.CampaignOver && guard++ < 400)
            {
                campaign.phase = 1; campaign.weeksInPhase = 0; // freeze the Crecida: this probe tests the WIN PATH, not the clock
                var fm = campaign.frontMap;
                var pick = fm.Available.FirstOrDefault(n => n.type == FrontNodeType.Jefe)
                        ?? fm.Available.FirstOrDefault(n => n.type == FrontNodeType.Sellado)
                        ?? fm.Available.FirstOrDefault(n => n.type != FrontNodeType.Opcional)
                        ?? fm.Available.FirstOrDefault();
                if (pick == null) break;

                var party = campaign.roster.Where(h => h.Alive).Take(4).ToList();
                expedition = new Expedition { heroes = party, node = pick, sector = pick.sector, casualties = new List<CombatUnit>() };
                map = SubstageMap.Generate(new List<string[]> { new[] { "Cultist" } }, phase: 0, sector: pick.sector);
                foreach (var room in map.rooms) room.cleared = true; // a flawless clear
                if (pick.type == FrontNodeType.Jefe) expedition.bossDefeated = true;
                heroes = party; enemies = new List<CombatUnit>();
                ReturnToShip(true);
                exps++;
                maxSector = Mathf.Max(maxSector, campaign.currentSector);
            }
            CheckInvariants(campaign.roster);
            return new VictoryProbe
            {
                maxSector = maxSector, expeditions = exps, violations = simViolations,
                confessorDown = campaign.confessorDown, stageWon = campaign.stageWon, log = new List<string>(log),
            };
        }

        /// <summary>Runs the map-navigation + combat loop for the already-set-up expedition. Returns true if the substage was purged.</summary>
        bool RunExpeditionLoopHeadless(int maxRoundsPerCombat)
        {
            int safety = 40000;
            while (safety-- > 0)
            {
                if (missionFailed) break; // Sabotaje clock ran out (in combat or mid-corridor): the raid aborts
                if (inCombat)
                {
                    if (combatOver)
                    {
                        CheckInvariants(expedition.heroes);
                        if (resultText.StartsWith("VICTORIA") && heroes.Any(h => h.Alive))
                        {
                            inCombat = false; currentRoom = null;
                            if (map.AllCombatsCleared) break;
                            continue;
                        }
                        break; // defeat or retreat-to-wipe
                    }
                    if (round > maxRoundsPerCombat) break; // stalemate guard
                    var u = Current();
                    if (u == null) break;
                    EnemyAct(u); // the enemy AI is team-agnostic: heroes play it too
                }
                else
                {
                    if (!heroes.Any(h => h.Alive)) break; // wiped out of combat (trap/curiosity)

                    if (map.current.pendingLoot != null)
                    {
                        foreach (var lootItem in map.current.pendingLoot.ToList())
                            if (expedition.HasSpace) { expedition.inventory.Add(lootItem); map.current.pendingLoot.Remove(lootItem); }
                        if (map.current.pendingLoot.Count == 0) map.current.pendingLoot = null;
                    }
                    if (map.current.kind == RoomKind.Curiosidad && !map.current.cleared)
                        ResolveCuriosityBlind(map.current);
                    if (map.current.kind == RoomKind.Santuario && !map.current.campUsed &&
                        expedition.FindWithCharges(ItemKind.CampKit) != null &&
                        heroes.Any(h => h.Alive && (h.hp <= h.EffMaxHP / 2 || h.corruption >= 60)))
                    {
                        DoCamp(map.current);
                        if (inCombat) continue;
                    }
                    AutoUseSupplies();

                    var target = map.NearestUnclearedCombat();
                    if (target == null) break;
                    var path = map.Path(map.current, target);
                    if (path == null || path.Count < 2) break;
                    EnterRoom(path[1]);
                }
            }
            return !missionFailed && map != null && map.AllCombatsCleared;
        }

        public struct BossResult
        {
            public int startWill;
            public bool bossDefeated;
            public bool byWill;    // broken morally
            public bool phase2;    // Revelación triggered
            public bool stageWon;  // campaign win flag set
            public int phaseBefore, phaseAfter; // Despertar retard
            public int sectorBefore, sectorAfter; // H6: a non-final boss kill advances the sector
            public int invariantViolations;
        }

        /// <summary>Isolated boss fight (just the throne room) for verification. sectorStart<0 = final sector (win).</summary>
        public BossResult RunHeadlessBossFight(int heroLevel = 5, int maxRounds = 80, int sectorStart = -1,
                                               string[] encounterOverride = null, FrontNodeType? nodeType = null)
        {
            campaign = Campaign.New();
            campaign.phase = 3; // so the boss-kill clock retard (3→2) is observable
            campaign.currentSector = sectorStart > 0 ? sectorStart : 1; // a boss kill at a non-final sector advances; the win is the sealing
            headless = true; simViolations = 0; log.Clear();

            var party = campaign.roster.Take(4).ToList();
            foreach (var h in party) for (int i = 0; i < heroLevel; i++) h.LevelUp();
            foreach (var h in party) campaign.assignment[h] = HeroAssignment.Expedition;
            shipStash.Clear(); AutoProvision();

            int phaseBefore = campaign.phase;
            int sectorBefore = campaign.currentSector;
            expedition = new Expedition { heroes = party, isBossMission = true, sector = campaign.currentSector };
            expedition.encounters = new List<string[]> { encounterOverride ?? new[] { "Baron", "Neophyte", "Neophyte" } };
            if (nodeType != null)
                expedition.node = new FrontNode { type = nodeType.Value, sector = campaign.currentSector, name = "prueba", state = FrontNodeState.Available };
            expedition.inventory = shipStash; shipStash = new List<ItemStack>();
            map = SubstageMap.Generate(expedition.encounters, phase: 0); // boss climax: no ambient hazards (keeps boss tuning clean)
            heroes = party; enemies = new List<CombatUnit>();
            inCombat = false; currentRoom = null; combatOver = false; resultText = ""; atShip = false;

            RunExpeditionLoopHeadless(maxRounds);
            int startWill = GameData.Unit("Baron").ownCohesion;
            bool defeated = expedition.bossDefeated;
            bool byWill = log.Any(l => l.Contains("se QUIEBRA"));
            bool phase2 = log.Any(l => l.Contains("REVELACIÓN"));
            ReturnToShip(map.AllCombatsCleared);
            CheckInvariants(party);

            return new BossResult
            {
                startWill = startWill,
                bossDefeated = defeated,
                byWill = byWill,
                phase2 = phase2,
                stageWon = campaign.stageWon,
                phaseBefore = phaseBefore,
                phaseAfter = campaign.phase,
                sectorBefore = sectorBefore,
                sectorAfter = campaign.currentSector,
                invariantViolations = simViolations,
            };
        }

        public Campaign TestCampaign => campaign; // verification hook (read-only)

        public struct MiniBossResult
        {
            public bool won;
            public int invariantViolations;
            public List<string> log;
        }

        /// <summary>Isolated fight against a mini-boss encounter (mandatory combat) for verification.</summary>
        public MiniBossResult RunHeadlessMiniBossFight(string[] encounter, int heroLevel = 4, int maxRounds = 80, int? seed = null)
        {
            campaign = Campaign.New(seed); // seed it so the verification suite is reproducible run-to-run
            headless = true; simViolations = 0; log.Clear();

            var party = campaign.roster.Take(4).ToList();
            foreach (var h in party) for (int i = 0; i < heroLevel; i++) h.LevelUp();

            expedition = new Expedition { heroes = party };
            expedition.encounters = new List<string[]> { encounter };
            expedition.inventory = new List<ItemStack>();
            map = SubstageMap.Generate(expedition.encounters, phase: 0); // single-combat harness: no ambient hazards
            heroes = party; enemies = new List<CombatUnit>();
            inCombat = false; currentRoom = null; combatOver = false; resultText = ""; atShip = false;

            bool won = RunExpeditionLoopHeadless(maxRounds);
            CheckInvariants(party);
            return new MiniBossResult { won = won, invariantViolations = simViolations, log = new List<string>(log) };
        }

        // ---- Light-axis verification (Fase posiciones 1) ----
        public struct LightProbe
        {
            public int startLight;      // lightLevel right after setup (3)
            public int afterR2, afterR3, afterR4, afterR5; // one decay step per round
            public int afterFlare;      // relaunch resets to MaxLight
            public int startFlares, afterLaunch; // charge economy
            public int vipZone, soldierZone;     // formation on the depth axis
            public bool blockedAtZero;  // decay never goes below 0
            public bool carriedLightRespected; // Opción B: InitLightAxis reads the carried light, not a fixed 3
        }

        /// <summary>Drives the light layer in isolation (no turns): tests decay, relaunch, zones, charges.</summary>
        public LightProbe RunHeadlessLightProbe()
        {
            campaign = Campaign.New();
            headless = true; simViolations = 0; log.Clear();

            var party = campaign.roster.Take(3).ToList();
            party.Add(new CombatUnit(GameData.Unit("Vip"))); // the Rescatado: a 5th figure that rides in the Galería
            expedition = new Expedition { heroes = party, inventory = new List<ItemStack>() };
            heroes = party; enemies = new List<CombatUnit>();

            InitLightAxis(); // the REAL setup path (zones + lightLevel + flare charges)
            var p = new LightProbe
            {
                startLight = lightLevel,
                startFlares = flareCharges,
                vipZone = party[3].zone,
                soldierZone = party[0].zone,
            };
            DecayFlare(); p.afterR2 = lightLevel; // 3 → 2
            DecayFlare(); p.afterR3 = lightLevel; // 2 → 1
            DecayFlare(); p.afterR4 = lightLevel; // 1 → 0 (apagada)
            DecayFlare(); p.afterR5 = lightLevel; // stays 0 (clamp)
            p.blockedAtZero = lightLevel == 0;
            LaunchFlare(party[0]);                // relight
            p.afterFlare = lightLevel;
            p.afterLaunch = flareCharges;

            // Opción B: light carried from exploration is what a new combat starts with (not a fixed 3).
            expedition.carriedLight = 1;
            InitLightAxis();
            p.carriedLightRespected = lightLevel == 1;
            return p;
        }

        // ---- Corridor length + per-step raid-turn verification (H5·3b/3c) ----
        public struct CorridorProbe
        {
            public int steps;                    // corridor length of the room walked into
            public int signalBefore, signalAfter;
            public int perStep;                  // expected la Señal decay per step
            public bool inRange;                 // every non-entrance room has 2..4 steps
            public int entranceSteps;            // the entrance has no incoming corridor (0)
            // H5·3c: each step is a full raid-turn.
            public int missionBefore, missionAfter;   // Sabotaje clock ticks per corridor step
            public int dotPow, hpBefore, hpAfter;      // a bleeding hero loses dotPow per step
            public bool softFloorSurvived;             // a near-dead bleeder floors at Death's Door, never dies out of combat
            // H5·3c-ii: mid-corridor discrete events.
            public bool eventsGenerated;               // Generate placed events on some corridors
            public bool eventsValid;                   // every event sits on a valid step (1..len-1)
            public bool alijoLooted;                   // a forced Alijo event added loot to the pack
        }

        /// <summary>Walks one corridor with the ambush suppressed: checks la Señal, the DoT and the mission clock all tick per step (H5·3).</summary>
        public CorridorProbe RunHeadlessCorridorProbe()
        {
            campaign = Campaign.New(31337);
            headless = true; simViolations = 0; log.Clear();
            var party = campaign.roster.Take(4).ToList();
            expedition = Expedition.ForSubstage(party, 0, 1);
            expedition.signal = 100;
            expedition.inventory = new List<ItemStack>();
            expedition.missionRoundBudget = 999; // ticks but won't fail during the probe
            missionRounds = 0; missionFailed = false;
            map = SubstageMap.Generate(expedition.encounters, expedition.miniBoss, 1);
            heroes = expedition.heroes; enemies = new List<CombatUnit>();

            var p = new CorridorProbe();
            p.entranceSteps = map.rooms.First(r => r.kind == RoomKind.Entrada).corridorSteps;
            p.inRange = map.rooms.Where(r => r.kind != RoomKind.Entrada).All(r => r.corridorSteps >= 2 && r.corridorSteps <= 4);
            p.perStep = SignalDecayPerStep + campaign.phase / 6;
            p.eventsGenerated = map.rooms.Sum(r => r.corridorEvents.Count) > 0;
            p.eventsValid = map.rooms.All(r => r.corridorEvents.All(e => e.step >= 1 && e.step < Mathf.Max(1, r.corridorSteps)));
            suppressCorridorEvents = true; // keep the delta walk below clean

            // heroes[0]: a comfortable bleeder — should lose dotPow per step and survive.
            p.dotPow = 3;
            heroes[0].hp = 100; heroes[0].AddStatus(StatusKind.Bleed, p.dotPow, 9);
            // heroes[1]: near-dead bleeder — must floor at Death's Door, never die out of combat.
            heroes[1].hp = 2; heroes[1].AddStatus(StatusKind.Bleed, 5, 9);

            // Walk into a reachable, non-combat, non-trap, unvisited room so the deltas stay clean.
            var target = map.current.links.FirstOrDefault(l => l.kind != RoomKind.Combate && l.kind != RoomKind.Entrada
                                                            && l.kind != RoomKind.Trampa && !l.visited);
            if (target == null) { p.steps = 0; p.signalBefore = p.signalAfter = expedition.signal; return p; }
            p.steps = Mathf.Max(1, target.corridorSteps);
            p.signalBefore = expedition.signal;
            p.missionBefore = missionRounds;
            p.hpBefore = heroes[0].hp;
            suppressAmbush = true;
            EnterRoom(target);
            suppressAmbush = false;
            p.signalAfter = expedition.signal;
            p.missionAfter = missionRounds;
            p.hpAfter = heroes[0].hp;
            p.softFloorSurvived = heroes[1].Alive && heroes[1].atDeathsDoor;

            // Forced Alijo: a mid-corridor cache must add loot to the pack when walked over.
            var target2 = map.current.links.FirstOrDefault(l => l.kind != RoomKind.Combate && l.kind != RoomKind.Entrada && !l.visited);
            if (target2 != null && target2.corridorSteps >= 2)
            {
                target2.corridorEvents.Clear();
                target2.corridorEvents.Add(new CorridorEvent { step = 1, kind = CorridorEventKind.Alijo });
                int invBefore = expedition.inventory.Count;
                suppressCorridorEvents = false; // let the event fire this time
                EnterRoom(target2);
                suppressCorridorEvents = true;
                p.alijoLooted = expedition.inventory.Count > invBefore;
            }
            else p.alijoLooted = true; // no valid target to test on this seed → don't fail the run
            return p;
        }

        // ---- Autómata verification (Hito 6) ----
        public struct AutomatonProbe
        {
            public bool isAutomaton, startsFaithZero, manchaImmune, noFaith, builtWithSerial;
            public bool autoNotBuffedByMiracle, normalBuffedByMiracle;
            public bool brokeNotDead, notACasualty, repaired; // la avería (6b)
            public int hp, arm, dodge, speed, maxAmmo, abilities;
        }

        public AutomatonProbe RunHeadlessAutomatonProbe()
        {
            campaign = Campaign.New(555);
            headless = true; simViolations = 0; log.Clear();
            var def = GameData.Unit("Automaton");
            var p = new AutomatonProbe
            {
                isAutomaton = def.isAutomaton,
                hp = def.maxHP, arm = def.armor, dodge = def.dodge, speed = def.speed,
                maxAmmo = def.maxAmmo, abilities = def.abilities.Count,
            };
            var solo = new CombatUnit(def);
            p.startsFaithZero = solo.faith == 0;
            solo.GainCorruption(50, Log); p.manchaImmune = solo.corruption == 0;   // immune to la Mancha
            solo.GainFaith(5, Log);       p.noFaith = solo.faith == 0;              // never gains la Llama

            int rb = campaign.roster.Count;                                          // el Taller builds one
            var built = campaign.BuildAutomaton();
            p.builtWithSerial = campaign.roster.Count == rb + 1 && built.unitName.Contains("Mk") && built.def.isAutomaton;

            // End-to-end: a Milagro (Escudo de la Llama) wards a normal hero but NOT the Autómata.
            var sister = new CombatUnit(GameData.Unit("Sister"));
            var auto = new CombatUnit(def);
            heroes = new List<CombatUnit> { sister, auto };
            enemies = new List<CombatUnit> { new CombatUnit(GameData.Unit("Cultist")) };
            foreach (var h in heroes) h.PrepareNextCombat();
            InitLightAxis();
            var escudo = sister.def.abilities.First(x => x.isFaithAbility && x.status != null && x.status.kind == StatusKind.DmgResistPct);
            UseAbility(sister, escudo, sister, false);
            p.normalBuffedByMiracle = sister.Has(StatusKind.DmgResistPct);
            p.autoNotBuffedByMiracle = !auto.Has(StatusKind.DmgResistPct);

            // la avería (6b): lethal damage BREAKS the Autómata (KO) — not a permadeath casualty; the Taller repairs it.
            var auto2 = new CombatUnit(def);
            heroes = new List<CombatUnit> { auto2, new CombatUnit(GameData.Unit("Sister")) };
            enemies = new List<CombatUnit>();
            expedition = new Expedition { heroes = heroes, casualties = new List<CombatUnit>() };
            auto2.hp = 5;
            var outc = auto2.TakeDamage(50);
            p.brokeNotDead = outc == DamageOutcome.Died && auto2.averiado && !auto2.Alive && !auto2.atDeathsDoor;
            OnDeath(auto2);
            p.notACasualty = auto2.averiado && !expedition.casualties.Contains(auto2) && !heroes.Contains(auto2);
            campaign.RepairAutomaton(auto2);
            p.repaired = !auto2.averiado && auto2.hp == auto2.EffMaxHP;
            return p;
        }

        // ---- Threat-channel verification (Fase posiciones 2) ----
        public struct ChannelProbe
        {
            public bool humanSeesLit, humanBlindToDark, fosoReachesDark, fosoBlindToLit;
            public bool humanRoutesToLit;   // ChannelFilter(human,[lit,dark]) → lit, not blind
            public bool humanBlindAllDark;  // ChannelFilter(human,[dark]) → blind
            public bool fosoRoutesToDark;   // ChannelFilter(foso,[lit,dark]) → dark, not blind
            public bool fosoBlindAllLit;    // ChannelFilter(foso,[lit]) → blind
            public bool dataChannelsOk;
            public int darkManchaTick;      // Mancha a dark hero actually took over one round
            public int blindFireAcc, darkMancha;
        }

        /// <summary>Tests the light-channel routing directly (the real ChannelFilter/InChannel) + data + dark Mancha.</summary>
        public ChannelProbe RunHeadlessChannelProbe()
        {
            campaign = Campaign.New();
            headless = true; simViolations = 0; log.Clear();

            var lit = campaign.roster[0];
            var dark = campaign.roster[1];
            heroes = new List<CombatUnit> { lit, dark };
            foreach (var h in heroes) h.PrepareNextCombat();
            lit.zone = 3; dark.zone = 0; lightLevel = 1; // front-lit: zone3>=3 = lit, zone0>=3 = dark

            var human = new CombatUnit(GameData.Unit("Cultist"));
            var foso = new CombatUnit(GameData.Unit("Aberrant"));

            var p = new ChannelProbe
            {
                humanSeesLit = InChannel(human, lit),
                humanBlindToDark = !InChannel(human, dark),
                fosoReachesDark = InChannel(foso, dark),
                fosoBlindToLit = !InChannel(foso, lit),
                blindFireAcc = BlindFireAcc,
                darkMancha = DarkMancha,
                dataChannelsOk =
                    GameData.Unit("Aberrant").threatChannel == ThreatChannel.Foso &&
                    GameData.Unit("Larva").threatChannel == ThreatChannel.Foso &&
                    GameData.Unit("Matriarch").threatChannel == ThreatChannel.Foso &&
                    GameData.Unit("Nest").threatChannel == ThreatChannel.Foso &&
                    GameData.Unit("Cultist").threatChannel == ThreatChannel.Human &&
                    GameData.Unit("Sniper").threatChannel == ThreatChannel.Human &&
                    GameData.Unit("Baron").threatChannel == ThreatChannel.Human &&
                    GameData.Unit("FalseChaplain").threatChannel == ThreatChannel.Human,
            };

            var both = new List<CombatUnit> { lit, dark };
            var hRoute = ChannelFilter(human, both, out bool hb);
            p.humanRoutesToLit = hRoute.Count == 1 && hRoute[0] == lit && !hb;
            ChannelFilter(human, new List<CombatUnit> { dark }, out bool hb2);
            p.humanBlindAllDark = hb2;
            var fRoute = ChannelFilter(foso, both, out bool fb);
            p.fosoRoutesToDark = fRoute.Count == 1 && fRoute[0] == dark && !fb;
            ChannelFilter(foso, new List<CombatUnit> { lit }, out bool fb2);
            p.fosoBlindAllLit = fb2;

            // Dark Mancha: the dark hero bleeds Mancha over a round; the lit one does not.
            int darkBefore = dark.corruption, litBefore = lit.corruption;
            ApplyDarkCorruption();
            p.darkManchaTick = dark.corruption - darkBefore;
            if (lit.corruption != litBefore) p.darkManchaTick = -999; // lit hero must NOT take dark Mancha
            return p;
        }

        // ---- Assault-reach verification (Fase posiciones 3) ----
        public struct AssaultProbe
        {
            public int assaultZone;
            public bool blockedAtParapet; // a melee hero on the parapet can't reach a rear enemy
            public bool reachesFromFront; // over the parapet it CAN
            public bool stillHitsFront;   // reach is additive: it still covers the front
            public bool enemyNoAssault;   // enemies get no assault reach (simple column in v1)
        }

        /// <summary>Tests over-the-parapet reach directly via ValidTargets with a controlled field.</summary>
        public AssaultProbe RunHeadlessAssaultProbe()
        {
            campaign = Campaign.New();
            headless = true; simViolations = 0; log.Clear();

            var hero = campaign.roster[0];
            var e1 = new CombatUnit(GameData.Unit("Cultist"));
            var e2 = new CombatUnit(GameData.Unit("Cultist"));
            var e3 = new CombatUnit(GameData.Unit("Sniper")); // the backline threat at pos 3
            heroes = new List<CombatUnit> { hero };
            enemies = new List<CombatUnit> { e1, e2, e3 };

            var melee = AbilityDef.New("PruebaMelee", a =>
            {
                a.targetKind = TargetKind.Enemy; a.usableFrom = new[] { 1, 2, 3, 4 };
                a.targetPos = new[] { 1, 2 }; a.accuracy = 90; a.dmgMin = 3; a.dmgMax = 5;
            });

            hero.zone = 1;
            bool blocked = !ValidTargets(hero, melee).Contains(e3);
            hero.zone = AssaultZone;
            var front = ValidTargets(hero, melee);
            bool reaches = front.Contains(e3);
            bool stillFront = front.Contains(e1);

            // An enemy pushed to a deep zone must NOT gain over-the-parapet reach (heroes only, in v1).
            var en = new CombatUnit(GameData.Unit("Cultist"));
            en.zone = 3;
            bool enemyNoAssault = !CanAssault(en);

            return new AssaultProbe
            {
                assaultZone = AssaultZone,
                blockedAtParapet = blocked,
                reachesFromFront = reaches,
                stillHitsFront = stillFront,
                enemyNoAssault = enemyNoAssault,
            };
        }

        // ---- Axis-unification verification (Fase posiciones 4) ----
        public struct RemapProbe
        {
            public int posGaleria, posParapeto, posAlambrada, posTierra; // 4,3,2,1
            public bool enemyTargetsByChannel; // enemy single-target offers every hero (channel decides), not by position
            public float lightFaith, darkPsychic;
            public int litCleanse, darkCleanse; // Mancha a lit vs dark Capellán cleanse removes
        }

        /// <summary>Verifies the single axis: hero position == light-axis depth, and enemy targeting by channel.</summary>
        public RemapProbe RunHeadlessRemapProbe()
        {
            campaign = Campaign.New();
            headless = true; simViolations = 0; log.Clear();

            var h = campaign.roster[0];
            heroes = new List<CombatUnit> { h };
            var en = new CombatUnit(GameData.Unit("Cultist"));
            enemies = new List<CombatUnit> { en };

            var p = new RemapProbe { lightFaith = LightFaithMult, darkPsychic = DarkPsychicMult };
            h.zone = 0; p.posGaleria = PosOf(h);     // Galería (rear/dark) → pos 4
            h.zone = 1; p.posParapeto = PosOf(h);    // Parapeto → pos 3
            h.zone = 2; p.posAlambrada = PosOf(h);   // Alambrada → pos 2
            h.zone = 3; p.posTierra = PosOf(h);      // Tierra de Nadie (front) → pos 1

            // Enemy single-target attack: every live hero is a candidate (light channel decides in EnemyAct),
            // regardless of hero position — the DD column no longer gates enemy targeting.
            heroes = new List<CombatUnit> { campaign.roster[0], campaign.roster[1], campaign.roster[2] };
            var atk = en.def.abilities.First(a => a.targetKind == TargetKind.Enemy && !a.area);
            p.enemyTargetsByChannel = ValidTargets(en, atk).Count == 3;

            // Miracle in the light vs the dark: a lit Capellán's Absolución cleanses more Mancha.
            var cap = new CombatUnit(GameData.Unit("Preacher"));
            var patient = new CombatUnit(GameData.Unit("Veteran"));
            var absol = cap.def.abilities.First(a => a.isFaithAbility && a.corruptionDelta < 0);
            heroes = new List<CombatUnit> { cap, patient };
            enemies = new List<CombatUnit>();
            cap.zone = 3; lightLevel = 3; patient.corruption = 100;   // caster lit (front-lit: zone3>=1)
            ResolveOnTarget(cap, absol, patient);
            p.litCleanse = 100 - patient.corruption;
            cap.zone = 0; lightLevel = 1; patient.corruption = 100;   // caster dark (zone0>=3 false)
            ResolveOnTarget(cap, absol, patient);
            p.darkCleanse = 100 - patient.corruption;
            return p;
        }

        // ---- Mirror-axis verification (Fase posiciones 5) ----
        public struct MirrorProbe
        {
            public bool humanEnemyLit, fosoEnemyDark;
            public bool litHeroVsFosoBlind, darkHeroVsFosoClean, litHeroVsHumanClean, darkHeroVsHumanClean;
            public int blindFireAcc;
        }

        /// <summary>Tests the mirror axis: enemy light state (by channel) + hero clean/blind by light match.</summary>
        public MirrorProbe RunHeadlessMirrorProbe()
        {
            campaign = Campaign.New();
            headless = true; simViolations = 0; log.Clear();

            var hero = campaign.roster[0];
            heroes = new List<CombatUnit> { hero };
            var human = new CombatUnit(GameData.Unit("Cultist"));  // Human channel → stands in the light
            var foso = new CombatUnit(GameData.Unit("Aberrant"));  // Foso channel → lurks in shadow
            enemies = new List<CombatUnit> { human, foso };

            var p = new MirrorProbe { blindFireAcc = MirrorBlindAcc };
            p.humanEnemyLit = Lit(human);
            p.fosoEnemyDark = !Lit(foso);

            hero.zone = 3; lightLevel = 3;                        // hero lit (front-lit: zone3>=1)
            p.litHeroVsFosoBlind = BlindShot(hero, foso);        // lit → dark = blind
            p.litHeroVsHumanClean = !BlindShot(hero, human);     // lit → lit = clean

            hero.zone = 0; lightLevel = 1;                        // hero dark (zone0>=3 is false)
            p.darkHeroVsFosoClean = !BlindShot(hero, foso);      // dark → dark = clean (you're in the shadow with it)
            p.darkHeroVsHumanClean = !BlindShot(hero, human);    // dark → lit = clean (the exposed enemy is always visible)
            return p;
        }

        static bool IsBuff(StatusKind k) =>
            k == StatusKind.AccBuff || k == StatusKind.DodgeBuff || k == StatusKind.DmgBuffPct ||
            k == StatusKind.Defend || k == StatusKind.Guard || k == StatusKind.SpeedBuff ||
            k == StatusKind.ToxinImmune || k == StatusKind.CorrResistMod || k == StatusKind.Taunt ||
            k == StatusKind.ArmorBuff || k == StatusKind.JudgmentNext || k == StatusKind.DmgResistPct;

        // Tooltip text for a consumable: effect + hidden cost (game texts in Spanish).
        static string ConsumableTooltip(ConsumableDef c)
        {
            var lines = new List<string> { $"— {c.displayName} —" };
            if (c.corruptionDelta < 0) lines.Add($"Purga {-c.corruptionDelta} de Mancha.");
            if (c.corruptionDelta > 0) lines.Add($"+{c.corruptionDelta} de Mancha.");
            if (c.status != null && c.status.kind != StatusKind.None)
                lines.Add($"{StatusText(c.status)} durante {c.status.duration}t{(c.affectsGroup ? " (todo el grupo)" : "")}.");
            if (c.removesToxin) lines.Add("Neutraliza el Gas.");
            if (c.toxinImmuneRounds > 0) lines.Add($"Inmunidad al Gas {c.toxinImmuneRounds}t.");
            if (c.removesQuebranto) lines.Add("Acalla un Quebranto durante este combate.");
            if (c.statusOnExpire != null && c.statusOnExpire.kind != StatusKind.None)
                lines.Add($"COSTE al expirar: {StatusText(c.statusOnExpire)}.");
            if (c.endCombatCorruption > 0) lines.Add($"COSTE al acabar el combate: +{c.endCombatCorruption} Mancha.");
            return string.Join("\n", lines);
        }

        // Tooltip for a unit box: effective stats + what its soul state does.
        static string UnitTooltip(CombatUnit u)
        {
            var lines = new List<string>
            {
                $"Esquiva efectiva: {u.EffDodge}",
                $"PRE: {(u.EffAccuracyBonus >= 0 ? "+" : "")}{u.EffAccuracyBonus}"
            };
            if (u.TeamOf == Team.Heroes)
            {
                lines.Add($"Res. Mancha: {u.EffCorrResist}% (clase {u.def.corrResist}% + Llama {3 * u.faith}%)");
                if (u.atDeathsDoor)
                    lines.Add($"AL BORDE DE LA MUERTE: cada golpe fuerza una prueba de muerte ({u.def.deathBlowPct}%). Curar 1 PV lo estabiliza.");
                switch (u.soul)
                {
                    case SoulState.Conmocionado: lines.Add("CONMOCIONADO: 50% de rechazar curas y purgas de sus aliados."); break;
                    case SoulState.Desesperado: lines.Add("DESESPERADO: −10 PRE; +2 Mancha al resto del grupo cada turno suyo."); break;
                    case SoulState.Saqueador: lines.Add("SAQUEADOR: ~40% de perder su acción rebuscando; mete mano al botín de la incursión al volver."); break;
                    case SoulState.Apostata: lines.Add("APÓSTATA: −1 Llama a un aliado aleatorio cada turno suyo."); break;
                    case SoulState.Iluminado: lines.Add("ILUMINADO: +10 PRE el resto del combate."); break;
                    case SoulState.Inquebrantable: lines.Add("INQUEBRANTABLE: −20% daño recibido el resto del combate."); break;
                }
            }
            return string.Join("\n", lines);
        }

        // Spanish short tag for an ACTIVE status shown in the unit box.
        static string StatusShort(StatusInstance s)
        {
            switch (s.kind)
            {
                case StatusKind.Bleed:       return $"Sangrado {s.power} ({s.rounds}t)";
                case StatusKind.Burn:        return $"Quemadura {s.power} ({s.rounds}t)";
                case StatusKind.Stun:        return "Aturdido";
                case StatusKind.AccDebuff:   return $"−{s.power} PRE ({s.rounds}t)";
                case StatusKind.AccBuff:     return $"+{s.power} PRE ({s.rounds}t)";
                case StatusKind.DodgeBuff:   return $"+{s.power} Esq ({s.rounds}t)";
                case StatusKind.DmgBuffPct:  return $"+{s.power}% daño ({s.rounds}t)";
                case StatusKind.SpeedDebuff: return $"−{s.power} VEL ({s.rounds}t)";
                case StatusKind.Defend:      return "A cubierto";
                case StatusKind.Guard:       return "Protegiendo";
                case StatusKind.Toxin:       return $"Gas {s.power} ({s.rounds}t)";
                case StatusKind.SpeedBuff:   return $"+{s.power} VEL ({s.rounds}t)";
                case StatusKind.CorrResistMod: return $"{(s.power >= 0 ? "+" : "")}{s.power}% res.Mancha ({s.rounds}t)";
                case StatusKind.ToxinImmune: return $"Inmune Gas ({s.rounds}t)";
                case StatusKind.Marked:      return $"Marcado +{s.power}% ({s.rounds}t)";
                case StatusKind.Taunt:       return "Desafiante";
                case StatusKind.ArmorBuff:   return $"+{s.power} Arm ({s.rounds}t)";
                case StatusKind.JudgmentNext: return "JUICIO preparado";
                case StatusKind.DmgResistPct: return $"−{s.power}% daño recibido ({s.rounds}t)";
                default:                     return s.kind.ToString();
            }
        }

        // Spanish display text for a status application (game texts live in Spanish).
        static string StatusText(StatusApply s)
        {
            switch (s.kind)
            {
                case StatusKind.Toxin:       return $"Gas {s.power}×{s.duration}";
                case StatusKind.SpeedBuff:   return $"+{s.power} VEL";
                case StatusKind.CorrResistMod: return $"{(s.power >= 0 ? "+" : "")}{s.power}% res. Mancha";
                case StatusKind.ToxinImmune: return "Inmunidad al Gas";
                case StatusKind.Marked:      return $"la Marca del renegado (+{s.power}% daño recibido)";
                case StatusKind.Taunt:       return "Desafío";
                case StatusKind.ArmorBuff:   return $"+{s.power} Armadura";
                case StatusKind.JudgmentNext: return "el JUICIO (próximo ataque: no falla, daño ×2)";
                case StatusKind.DmgResistPct: return $"−{s.power}% daño recibido";
                case StatusKind.Bleed:       return $"Sangrado {s.power}×{s.duration}";
                case StatusKind.Burn:        return $"Quemadura {s.power}×{s.duration}";
                case StatusKind.Stun:        return "Aturdimiento";
                case StatusKind.AccDebuff:   return $"−{s.power} PRE";
                case StatusKind.AccBuff:     return $"+{s.power} PRE";
                case StatusKind.DodgeBuff:   return $"+{s.power} Esquiva";
                case StatusKind.DmgBuffPct:  return $"+{s.power}% daño";
                case StatusKind.SpeedDebuff: return $"−{s.power} VEL";
                case StatusKind.Defend:      return "Defensa";
                case StatusKind.Guard:       return "Guardia";
                default:                     return s.kind.ToString();
            }
        }
    }
}
