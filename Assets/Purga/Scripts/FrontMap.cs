using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Purga
{
    public enum FrontNodeType { Asalto, Sabotaje, Rescate, Aguantar, Opcional, Jefe, Sellado }
    public enum FrontNodeState { Locked, Available, Cleared, Lost }

    /// A node on the front (design §2). Entering it launches an incursion parameterised by
    /// its type/difficulty. Completing it opens the nodes it links to.
    public class FrontNode
    {
        public int id;
        public string name;
        public FrontNodeType type;
        public int col, row;                 // for the front-map render
        public int difficulty;               // 0..2 → threat-budget substage index
        public int sector = 1;               // 1 Primera Línea · 2 Galerías · 3 Tierra de Nadie (drives the bestiary tier; H6 multi-sector)
        public FrontNodeState state = FrontNodeState.Locked;
        public List<int> links = new List<int>(); // nodes opened when this one is cleared
        // Type-specific parameters (only the relevant one is set):
        public int waves;                    // Aguantar: number of waves to survive
        public int roundBudget;              // Sabotaje: rounds before reinforcements arrive
        public bool hasVip;                  // Rescate: a VIP to extract
        public string reward;                // Opcional: "crecida" (−1 Crecida) | "botin" (+paga)
        public int loseAtPhase;              // degradation: Crecida phase at which an untaken node is lost (0 = never)

        public bool Takeable => state == FrontNodeState.Available;
    }

    /// <summary>
    /// The front: a node-graph for a sector. Hand-authored in Hito 4 (BuildStage1); the
    /// procedural generator arrives in Hito 6. Clearing a node opens the ones it links to;
    /// the front degrades with the Crecida (untaken side nodes are lost).
    /// </summary>
    public class FrontMap
    {
        public List<FrontNode> nodes = new List<FrontNode>();
        public int sector = 1; // which sector this graph is (H6: one map per sector)

        public FrontNode Get(int id) => nodes.FirstOrDefault(n => n.id == id);
        public FrontNode BossNode() => nodes.FirstOrDefault(n => n.type == FrontNodeType.Jefe);
        public IEnumerable<FrontNode> Available => nodes.Where(n => n.state == FrontNodeState.Available);
        public bool BossReachable => BossNode() != null &&
            (BossNode().state == FrontNodeState.Available || BossNode().state == FrontNodeState.Cleared);
        public int ClearedCount => nodes.Count(n => n.state == FrontNodeState.Cleared && n.type != FrontNodeType.Jefe);

        /// Clearing a node opens every Locked node it links to.
        public void OnClear(FrontNode node)
        {
            node.state = FrontNodeState.Cleared;
            foreach (var id in node.links)
            {
                var n = Get(id);
                if (n != null && n.state == FrontNodeState.Locked) n.state = FrontNodeState.Available;
            }
        }

        /// Untaken nodes past their loseAtPhase collapse (the front rots as the Crecida rises).
        public List<FrontNode> Degrade(int phase)
        {
            var lost = new List<FrontNode>();
            foreach (var n in nodes)
                if (n.loseAtPhase > 0 && phase >= n.loseAtPhase &&
                    (n.state == FrontNodeState.Available || n.state == FrontNodeState.Locked))
                {
                    n.state = FrontNodeState.Lost;
                    lost.Add(n);
                }
            return lost;
        }

        /// Is there still a chain of non-Lost nodes from a taken/takeable node to the boss?
        public bool RouteToBossExists()
        {
            var boss = BossNode();
            if (boss == null || boss.state == FrontNodeState.Lost) return false;
            var seen = new HashSet<int>();
            var queue = new Queue<FrontNode>();
            foreach (var n in nodes.Where(x => x.state == FrontNodeState.Available || x.state == FrontNodeState.Cleared))
            { queue.Enqueue(n); seen.Add(n.id); }
            while (queue.Count > 0)
            {
                var n = queue.Dequeue();
                if (n.type == FrontNodeType.Jefe) return true;
                foreach (var id in n.links)
                {
                    var next = Get(id);
                    if (next != null && next.state != FrontNodeState.Lost && seen.Add(id)) queue.Enqueue(next);
                }
            }
            return false;
        }

        // ---- Procedural front, one generated graph per sector (H6). A layered network (front → mid →
        // gate → Jefe) with side optionals, validated so the boss is always reachable and no node is
        // orphaned; deterministic under the campaign seed. Clearing a sector's Jefe loads the next map. ----
        public static FrontMap BuildStage1() => BuildSector(1); // compat alias (Sector 1)

        public static FrontMap BuildSector(int sector)
        {
            for (int tries = 0; tries < 16; tries++)
            {
                var m = GenerateSector(sector);
                if (m.Validate()) return m;
            }
            return GenerateSector(sector); // fallback (the layered build makes an invalid map effectively impossible)
        }

        /// <summary>A generated front is well-formed: exactly one boss, an open front, a route to the boss, no orphans.</summary>
        public bool Validate()
        {
            if (nodes.Count(n => n.type == FrontNodeType.Jefe) != 1) return false;
            if (!Available.Any()) return false;
            if (!RouteToBossExists()) return false;
            var seen = new HashSet<int>();
            var q = new Queue<FrontNode>();
            foreach (var n in Available) { q.Enqueue(n); seen.Add(n.id); }
            while (q.Count > 0) { var n = q.Dequeue(); foreach (var lid in n.links) { var x = Get(lid); if (x != null && seen.Add(lid)) q.Enqueue(x); } }
            return nodes.All(n => seen.Contains(n.id)); // every node is reachable from the open front
        }

        static string BossName(int sector) => sector == 2 ? "El útero de la Herida" : sector == 3 ? "El ojo del Foso" : sector >= 4 ? "El sagrario del Confesor" : "las Puertas de Vared";

        static string[] SectorNames(int sector) => sector switch
        {
            2 => new[]{ "Boca de la galería","Pozo de escaleras","Vena de gas","Cámara de nidos","Alijo en la galería","Túnel anegado","Cripta de raíces","Sala de bombas","Filón de carne","Escalera rota","Corredor sin luz","Pozo de ventilación","Nervadura del Foso","Antesala del útero" },
            3 => new[]{ "Cráter de obús","Nido de francotiradores","Campo de alambre","Trinchera anegada","Reliquia en el barro","Tanque calcinado","Cementerio de acero","Loma sin nombre","Alambrada roja","Foso de cal","Convoy volcado","Puesto perdido","Duna de ceniza","Antesala del ojo" },
            4 => new[]{ "Umbral de biomasa","Costilla catedral","Estanque de bilis","Pasillo de bocas","Coágulo mayor","Nervio expuesto","Cripta de fetos","Vena palpitante","Sala del coro","Membrana rota","Osario blando","Glándula del Foso","Tendón colgante","Antesala del sagrario" },
            _ => new[]{ "Trinchera de avanzada","Nido de ametralladoras","Puesto avanzado del culto","Convoy de la Congregación","Reducto de sacos terreros","Capilla profanada","Depósito de suministros","Parapeto derruido","Cráter inundado","Alambrada quemada","Refugio del sargento","Zanja de comunicación","Prisioneros en la alambrada","Puesto de escucha" },
        };

        static List<FrontNode> PickSome(List<FrontNode> src, int k) => src.OrderBy(_ => Rng.Value).Take(Mathf.Min(k, src.Count)).ToList();

        static FrontMap GenerateSector(int sector)
        {
            var m = new FrontMap { sector = sector };
            int id = 0;
            var pool = new Queue<string>(SectorNames(sector).OrderBy(_ => Rng.Value));
            string Name() => pool.Count > 0 ? pool.Dequeue() : $"Sector {sector} · nodo {id}";
            int baseDiff = Mathf.Clamp(sector - 1, 0, 2);

            FrontNode Add(FrontNodeType t, int col, int row, int diff, FrontNodeState st)
            {
                var n = new FrontNode { id = id++, name = Name(), type = t, col = col, row = row,
                                        difficulty = Mathf.Clamp(diff, 0, 2), state = st, sector = sector };
                m.nodes.Add(n);
                return n;
            }
            void Link(FrontNode a, FrontNode b) { if (a != b && !a.links.Contains(b.id)) a.links.Add(b.id); }

            // Columns: front (open) → mid → gate → boss. Multiple routes converge on the Jefe.
            var front = new List<FrontNode>();
            for (int i = 0, f = Rng.Range(2, 4); i < f; i++) front.Add(Add(FrontNodeType.Asalto, 0, i, baseDiff, FrontNodeState.Available));
            var mid = new List<FrontNode>();
            for (int i = 0, mm = Rng.Range(2, 4); i < mm; i++) mid.Add(Add(FrontNodeType.Asalto, 1, i, baseDiff + 1, FrontNodeState.Locked));
            var gate = new List<FrontNode>();
            for (int i = 0, g = Rng.Range(1, 3); i < g; i++) gate.Add(Add(FrontNodeType.Asalto, 2, i, 2, FrontNodeState.Locked));
            var boss = Add(FrontNodeType.Jefe, 3, 0, 2, FrontNodeState.Locked);
            boss.name = BossName(sector);
            // el Reducto: beyond the Confesor lies the open Herida. Clearing him opens the sealing ritual.
            if (sector >= 4)
            {
                var seal = Add(FrontNodeType.Sellado, 4, 0, 2, FrontNodeState.Locked);
                seal.name = "Sellar la Herida"; seal.waves = 5;
                boss.links.Add(seal.id);
            }

            // Forward links, guaranteeing a front→mid→gate→boss route and no orphans.
            foreach (var f in front) foreach (var t in PickSome(mid, Rng.Range(1, 3))) Link(f, t);
            foreach (var t in mid) if (!front.Any(f => f.links.Contains(t.id))) Link(front[Rng.Range(0, front.Count)], t);
            foreach (var mm in mid) foreach (var t in PickSome(gate, Rng.Range(1, 3))) Link(mm, t);
            foreach (var g in gate) if (!mid.Any(mm => mm.links.Contains(g.id))) Link(mid[Rng.Range(0, mid.Count)], g);
            foreach (var g in gate) Link(g, boss);

            // Critical-path variety: one Sabotaje, one Aguantar among the mid/gate nodes.
            var critical = mid.Concat(gate).ToList();
            var sab = critical[Rng.Range(0, critical.Count)];
            sab.type = FrontNodeType.Sabotaje; sab.roundBudget = 32;
            var arenaPool = critical.Where(n => n.type == FrontNodeType.Asalto).ToList();
            if (arenaPool.Count > 0) { var ar = arenaPool[Rng.Range(0, arenaPool.Count)]; ar.type = FrontNodeType.Aguantar; ar.waves = Rng.Range(5, 7); }

            // Optional side nodes (1–2 dead-ends) with a reward and a collapse phase.
            var hosts = front.Concat(mid).ToList();
            for (int i = 0, o = Rng.Range(1, 3); i < o; i++)
            {
                var host = hosts[Rng.Range(0, hosts.Count)];
                var opt = Add(FrontNodeType.Opcional, host.col, 5 + i, baseDiff, FrontNodeState.Locked);
                opt.reward = Rng.Value < 0.5f ? "crecida" : "botin";
                opt.loseAtPhase = Rng.Range(4, 7);
                Link(host, opt);
            }

            // Rescate: an optional escort, Sector 1 only (the current line is where you can still save people).
            if (sector == 1 && Rng.Value < 0.7f)
            {
                var host = front[Rng.Range(0, front.Count)];
                var r = Add(FrontNodeType.Rescate, host.col, 8, 0, FrontNodeState.Locked);
                r.hasVip = true; r.reward = "botin"; r.loseAtPhase = 6;
                Link(host, r);
            }

            return m;
        }
    }
}
