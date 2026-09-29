using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Purga
{
    public enum RoomKind { Entrada, Combate, Tesoro, Vacia, Curiosidad, Trampa, Santuario }

    /// <summary>H5·3c-ii: a discrete event that fires mid-corridor as you march (not in a junction room).
    /// Alijo (cache), Trampa (tripwire), Gas (toxic pocket), Obstaculo (wire tangle: costs time/Señal).</summary>
    public enum CorridorEventKind { Alijo, Trampa, Gas, Obstaculo }

    public class CorridorEvent
    {
        public int step;                 // which corridor step (1..len-1) it sits on
        public CorridorEventKind kind;
        public bool sprung;              // fired once already
    }

    /// Telegraphed environmental hazards (design §2). GasCloud gasses the unmasked
    /// each round; RedSky bleeds passive Mancha each round. Intensity scales with the
    /// Crecida phase at combat time; density (how many rooms) scales at generation time.
    public enum RoomHazard { None, GasCloud, RedSky }

    public class Room
    {
        public int id;
        public int col, row;
        public RoomKind kind;
        public int corridorSteps = 1; // H5·3: length of the corridor leading INTO this room (raid-turns to traverse)
        public List<CorridorEvent> corridorEvents = new List<CorridorEvent>(); // H5·3c-ii: events along that corridor
        public string[] encounter;   // Combate rooms only
        public bool isMiniBoss;      // an anomaly room: optional, guards the best loot
        public bool visited;
        public bool cleared;         // combat won / treasure looted / curiosity spent / trap sprung
        public int curiosityType;    // 0 altar, 1 cogitador, 2 cadáver, 3 relicario
        public RoomHazard hazard;    // telegraphed ambient hazard, if any
        public bool campUsed;        // Santuario: one camp per substage
        public List<ItemStack> pendingLoot; // treasure waiting to be picked up (lost if you walk away)
        public List<Room> links = new List<Room>();
    }

    /// <summary>
    /// DD-style substage map (Milestone 3.2): rooms laid out in columns and
    /// connected left-to-right, the route is the player's choice, and fog of
    /// war hides a room's content until it is visited or adjacent to a visited
    /// one. Encounters escalate with the column. Special rooms (curiosidades,
    /// santuario, trampas) arrive in 3.4; ambushes and Vox in 3.5/3.6.
    /// </summary>
    public class SubstageMap
    {
        public List<Room> rooms = new List<Room>();
        public Room current;
        public bool augurRevealed; // a cult cogitator dumped the level plan: full map visible
        public int sector = 1;     // H5·3d: 1 trinchera · 2 galería · 3 campo abierto · F biomasa (biome + rule twist)

        // Mandatory combats only (mini-boss anomaly rooms are optional, excluded here).
        public IEnumerable<Room> CombatRooms => rooms.Where(r => r.kind == RoomKind.Combate && !r.isMiniBoss);
        public bool AllCombatsCleared => CombatRooms.All(r => r.cleared);
        public int CombatsCleared => CombatRooms.Count(r => r.cleared);
        public int CombatsTotal => CombatRooms.Count();

        /// <summary>Biome dressing per sector (§5: the corridor is reskinned by sector).</summary>
        public static string Biome(int sector) => sector <= 1 ? "trinchera" : sector == 2 ? "galería" : sector == 3 ? "campo abierto" : "biomasa";

        /// <summary>H5·3d validation: entrance exists and every mandatory combat + the Santuario is reachable from it.</summary>
        public bool Validate()
        {
            var entrance = rooms.FirstOrDefault(r => r.kind == RoomKind.Entrada);
            if (entrance == null) return false;
            var seen = new HashSet<Room> { entrance };
            var q = new Queue<Room>(); q.Enqueue(entrance);
            while (q.Count > 0) { var r = q.Dequeue(); foreach (var l in r.links) if (seen.Add(l)) q.Enqueue(l); }
            if (!CombatRooms.All(seen.Contains)) return false;                 // every fight is completable
            var sanct = rooms.FirstOrDefault(r => r.kind == RoomKind.Santuario);
            return sanct == null || seen.Contains(sanct);                       // the Refugio is reachable
        }

        /// <summary>Generate a validated substage: retries the template until every route is completable
        /// and the Refugio is reachable (§5: generación procedural con validación).</summary>
        public static SubstageMap Generate(List<string[]> encounters, string[] miniBoss = null, int phase = 1, int sector = 1)
        {
            SubstageMap m = null;
            for (int tries = 0; tries < 8; tries++)
            {
                m = GenerateInternal(encounters, miniBoss, phase, sector);
                if (m.Validate()) return m;
            }
            return m; // fallback: never null (the built-in back-links make an invalid map effectively impossible)
        }

        static SubstageMap GenerateInternal(List<string[]> encounters, string[] miniBoss, int phase, int sector)
        {
            var m = new SubstageMap();
            m.sector = sector;
            var byCol = new List<List<Room>>();
            int id = 0;

            Room NewRoom(int col, int row)
            {
                var room = new Room { id = id++, col = col, row = row, kind = RoomKind.Vacia };
                m.rooms.Add(room);
                return room;
            }

            // Skeleton: entrance + 3-4 middle columns of 2-4 rooms + a narrow final column.
            var colCounts = new List<int> { 1 };
            int middleCols = Rng.Range(3, 5);
            for (int i = 0; i < middleCols; i++) colCounts.Add(Rng.Range(2, 5));
            colCounts.Add(Rng.Range(1, 3));
            while (colCounts.Sum() < 11) // enough rooms for 6 combats + loot + emptiness
            {
                int c = Rng.Range(1, colCounts.Count - 1);
                colCounts[c] = Mathf.Min(4, colCounts[c] + 1);
            }

            for (int c = 0; c < colCounts.Count; c++)
            {
                var colRooms = new List<Room>();
                for (int r = 0; r < colCounts[c]; r++) colRooms.Add(NewRoom(c, r));
                byCol.Add(colRooms);
            }

            void Link(Room a, Room b)
            {
                if (a != b && !a.links.Contains(b)) { a.links.Add(b); b.links.Add(a); }
            }

            // Reachability: every room links BACK to the previous column, and that is
            // the only guarantee. Forward links are not: a room nobody picks from the
            // next column becomes a dead end you must backtrack out of.
            for (int c = 1; c < byCol.Count; c++)
                foreach (var r in byCol[c])
                    Link(r, byCol[c - 1][Rng.Range(0, byCol[c - 1].Count)]);

            // A few extra corridors for loops and alternate routes
            int extra = Rng.Range(1, 4);
            for (int i = 0; i < extra; i++)
            {
                int c = Rng.Range(0, byCol.Count - 1);
                Link(byCol[c][Rng.Range(0, byCol[c].Count)],
                     byCol[c + 1][Rng.Range(0, byCol[c + 1].Count)]);
            }

            // Sometimes a vertical corridor inside a column
            if (Rng.Value < 0.5f)
            {
                int c = Rng.Range(1, byCol.Count - 1);
                if (byCol[c].Count >= 2)
                {
                    int r0 = Rng.Range(0, byCol[c].Count - 1);
                    Link(byCol[c][r0], byCol[c][r0 + 1]);
                }
            }

            // 1-2 pocket rooms: guaranteed dead ends hanging off the route
            int pockets = Rng.Range(1, 3);
            for (int i = 0; i < pockets; i++)
            {
                int c = Rng.Range(1, byCol.Count - 1);
                var pocket = NewRoom(c, byCol[c].Count);
                Link(pocket, byCol[c][Rng.Range(0, byCol[c].Count)]);
                byCol[c].Add(pocket);
            }

            byCol[0][0].kind = RoomKind.Entrada;

            // Combats: the hardest encounter guards the final column; the rest spread
            // over the middle columns round-robin, so escalation follows the depth.
            var lastCol = byCol[byCol.Count - 1];
            var closer = lastCol[Rng.Range(0, lastCol.Count)];
            closer.kind = RoomKind.Combate;
            closer.encounter = encounters[encounters.Count - 1];

            var slots = new List<Room>();
            int guard = 0;
            while (slots.Count < encounters.Count - 1 && guard++ < 200)
            {
                for (int c = 1; c < byCol.Count - 1 && slots.Count < encounters.Count - 1; c++)
                {
                    var free = byCol[c].Where(r => r.kind == RoomKind.Vacia && !slots.Contains(r)).ToList();
                    if (free.Count > 0) slots.Add(free[Rng.Range(0, free.Count)]);
                }
            }
            int e = 0;
            foreach (var room in slots.OrderBy(r => r.col).ThenBy(_ => Rng.Value))
            {
                room.kind = RoomKind.Combate;
                room.encounter = encounters[Mathf.Min(e++, encounters.Count - 2)];
            }

            // Special rooms among what remains: one Santuario, curiosities, traps
            Room TakeVacia()
            {
                var free = m.rooms.Where(x => x.kind == RoomKind.Vacia).ToList();
                return free.Count > 0 ? free[Rng.Range(0, free.Count)] : null;
            }

            // A mini-boss anomaly room (optional): a normal-looking combat room, never the
            // first column, that guards the best loot. Unmarked (design: no augur = ⚔ Hostiles).
            if (miniBoss != null)
            {
                var spot = m.rooms.Where(x => x.kind == RoomKind.Vacia && x.col >= 1).OrderBy(_ => Rng.Value).FirstOrDefault();
                if (spot != null)
                {
                    spot.kind = RoomKind.Combate;
                    spot.isMiniBoss = true;
                    spot.encounter = miniBoss;
                }
            }

            var sanct = m.rooms.Where(x => x.kind == RoomKind.Vacia && x.col > 0 && x.col < byCol.Count - 1)
                .OrderBy(_ => Rng.Value).FirstOrDefault();
            if (sanct != null) sanct.kind = RoomKind.Santuario;

            int nCurios = Rng.Range(1, 3);
            for (int i = 0; i < nCurios; i++)
            {
                var room = TakeVacia();
                if (room == null) break;
                room.kind = RoomKind.Curiosidad;
                room.curiosityType = Rng.Range(0, 4);
            }

            int nTraps = Rng.Range(1, 3);
            for (int i = 0; i < nTraps; i++)
            {
                var room = TakeVacia();
                if (room == null) break;
                room.kind = RoomKind.Trampa;
            }

            // Treasures prefer the dead ends: risk deserves loot
            int nTreasures = Rng.Range(2, 4);
            foreach (var room in m.rooms.Where(r => r.kind == RoomKind.Vacia)
                         .OrderBy(r => r.links.Count).ThenBy(_ => Rng.Value).Take(nTreasures))
                room.kind = RoomKind.Tesoro;

            // Environmental hazards (telegraphed): denser as the Crecida rises. Only real
            // combat rooms (where rounds tick); intensity is applied at combat time from the
            // phase. phase <= 0 disables them (used by focused boss/mini-boss test harnesses).
            float hazardChance = phase <= 0 ? 0f : Mathf.Min(0.6f, 0.12f + 0.06f * phase);
            // Each sector breaks a rule (§5): las Galerías (S2) acumulan Gas; la Tierra de Nadie (S3+)
            // se abre bajo cielo rojo. So the hazard MIX shifts with the sector, not just its density.
            float gasBias = sector == 2 ? 0.72f : sector >= 3 ? 0.25f : 0.5f;
            foreach (var room in m.rooms.Where(r => r.kind == RoomKind.Combate && !r.isMiniBoss))
                if (Rng.Value < hazardChance)
                    room.hazard = Rng.Value < gasBias ? RoomHazard.GasCloud : RoomHazard.RedSky;

            // Corridor length (H5·3): the tunnel INTO each room is 2–4 steps long, so "how far you
            // push" costs la Señal per step (design §5: nodos-cruce unidos por tramos con LONGITUD).
            // The entrance has no incoming corridor.
            foreach (var room in m.rooms)
                room.corridorSteps = room.kind == RoomKind.Entrada ? 0 : Rng.Range(2, 5);

            // Mid-corridor events (H5·3c-ii): things that hit you AS YOU MARCH — a cache, a tripwire, a
            // gas pocket, a wire tangle — placed at step positions along the tunnel INTO each room. Denser
            // and more toxic as la Crecida rises. Curiosities and the Santuario stay as junction stops.
            foreach (var room in m.rooms.Where(r => r.kind != RoomKind.Entrada && r.corridorSteps >= 2))
            {
                int nEv = Rng.Value < 0.45f ? 0 : Rng.Value < 0.85f ? 1 : 2;
                var used = new List<int>();
                for (int i = 0; i < nEv; i++)
                {
                    int step = Rng.Range(1, room.corridorSteps); // 1..len-1 (never the threshold step 0)
                    if (used.Contains(step)) continue;
                    used.Add(step);
                    int roll = Rng.Range(0, 100);
                    var kind = roll < 35 ? CorridorEventKind.Alijo
                             : roll < 60 ? CorridorEventKind.Obstaculo
                             : roll < 82 ? CorridorEventKind.Trampa
                                         : CorridorEventKind.Gas;
                    // the rotting front breathes Gas: nudge toward it as the Crecida climbs
                    if (phase > 0 && Rng.Range(0, 100) < Mathf.Min(30, 3 * phase)) kind = CorridorEventKind.Gas;
                    room.corridorEvents.Add(new CorridorEvent { step = step, kind = kind });
                }
            }

            m.current = byCol[0][0];
            m.current.visited = true;
            return m;
        }

        /// <summary>BFS path from `from` to `to`, both inclusive. Null if unreachable.</summary>
        public List<Room> Path(Room from, Room to)
        {
            var parent = new Dictionary<Room, Room> { [from] = from };
            var queue = new Queue<Room>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var r = queue.Dequeue();
                if (r == to) break;
                foreach (var l in r.links)
                    if (!parent.ContainsKey(l)) { parent[l] = r; queue.Enqueue(l); }
            }
            if (!parent.ContainsKey(to)) return null;
            var path = new List<Room> { to };
            while (path[0] != from) path.Insert(0, parent[path[0]]);
            return path;
        }

        // The sim (and "auto-path") only pursues MANDATORY combats; mini-boss anomaly
        // rooms are optional detours a human may choose for the loot.
        public Room NearestUnclearedCombat()
        {
            Room best = null; int bestLen = int.MaxValue;
            foreach (var r in CombatRooms.Where(r => !r.cleared))
            {
                var p = Path(current, r);
                if (p != null && p.Count < bestLen) { best = r; bestLen = p.Count; }
            }
            return best;
        }
    }
}
