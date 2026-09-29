using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Purga.EditorTools
{
    /// <summary>
    /// Headless CAMPAIGN runner: drives full campaigns (roster, weeks, Despertar
    /// clock, permadeath) with the simple AI on both sides, no play mode needed.
    /// Aggregates campaign-level stats and writes a report plus one sample log to
    /// PlaytestLogs/sim_*.log.
    /// Command line: -executeMethod Purga.EditorTools.CombatSimulator.RunBatch [-simCount N] [-simWeeks W]
    /// </summary>
    public static class CombatSimulator
    {
        [MenuItem("Purga/Simular 100 campañas (headless)")]
        public static void RunMenu() => Run(100, 20, 20260727);

        public static void RunBatch()
        {
            int n = 200, weeks = 20, baseSeed = 20260727, level = 0;
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-simCount" && int.TryParse(args[i + 1], out int c)) n = c;
                if (args[i] == "-simWeeks" && int.TryParse(args[i + 1], out int w)) weeks = w;
                if (args[i] == "-simSeed" && int.TryParse(args[i + 1], out int s)) baseSeed = s;
                if (args[i] == "-simLevel" && int.TryParse(args[i + 1], out int l)) level = l;
            }
            Run(n, weeks, baseSeed, level);
        }

        // Each campaign gets seed baseSeed+i: the batch is fully reproducible run-to-run,
        // yet every campaign in it differs. Change -simSeed for a different (repeatable) batch.
        static void Run(int n, int weeks, int baseSeed, int level = 0)
        {
            var results = new List<CombatBootstrap.CampaignResult>(n);
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject("PurgaSim");
                try
                {
                    var cb = go.AddComponent<CombatBootstrap>();
                    results.Add(cb.RunHeadlessCampaign(weeks, seed: baseSeed + i, heroLevel: level));
                }
                finally { UnityEngine.Object.DestroyImmediate(go); }
            }

            string report = BuildReport(results, weeks, baseSeed);
            string dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "PlaytestLogs");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"sim_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            File.WriteAllText(path, report);
            Debug.Log($"[Purga] Simulación completada: {results.Count} campañas → {path}\n{report}");
        }

        static string BuildReport(List<CombatBootstrap.CampaignResult> r, int weeks, int baseSeed)
        {
            int n = r.Count;
            int CountLines(string needle) => r.Sum(x => x.log.Count(l => l.Contains(needle)));

            int wiped = r.Count(x => x.rosterWiped);
            int violations = r.Sum(x => x.invariantViolations);
            int ambushes = CountLines("EMBOSCADA");
            int traps = CountLines("¡TRAMPA!");
            int camps = CountLines("El grupo se refugia");
            int levaLines = CountLines("La Leva entrega");
            int reclusiam = CountLines("reza en la Capilla") + CountLines("libra a");
            int enfermeria = CountLines("sana en la Enfermería");
            int soulTrials = CountLines("⚡ LA PRUEBA DE FE");
            int doors = CountLines("AL BORDE DE LA MUERTE");
            int won = r.Count(x => x.stageWon);
            int fell = r.Count(x => x.planetFallen);

            var lines = new List<string>
            {
                $"—— SIMULACIÓN LA VIGILIA · CAMPAÑAS | {DateTime.Now:yyyy-MM-dd HH:mm} | {n} campañas de hasta {weeks} semanas · semilla base {baseSeed} (IA simple) ——",
                "",
                $"★ CAMPAÑA GANADA (Herida sellada): {won}/{n} ({100f * won / n:0.#}%)",
                $"Sector más profundo alcanzado — S1:{r.Count(x => x.maxSectorReached == 1)} · S2:{r.Count(x => x.maxSectorReached == 2)} · S3:{r.Count(x => x.maxSectorReached == 3)} · Reducto:{r.Count(x => x.maxSectorReached >= 4)}",
                $"☠ Sector caído (Crecida agotada): {fell}/{n} ({100f * fell / n:0.#}%)",
                $"Rosters aniquilados: {wiped}/{n} ({100f * wiped / n:0.#}%)",
                $"Nodos del frente asegurados (media): {r.Average(x => x.substagesCleared):0.##}",
                $"Semanas sobrevividas (media): {r.Average(x => x.weeksSurvived):0.#} / {weeks}",
                $"Fase de la Crecida alcanzada (media): {r.Average(x => x.phaseReached):0.#} / 8   ·   máx {r.Max(x => x.phaseReached)}",
                "",
                $"Expediciones lanzadas (media): {r.Average(x => x.expeditionsLaunched):0.#}   ·   completadas: {r.Average(x => x.expeditionsCompleted):0.#} ({100f * r.Sum(x => x.expeditionsCompleted) / Mathf.Max(1, r.Sum(x => x.expeditionsLaunched)):0.#}% de éxito)",
                $"Agentes muertos (media): {r.Average(x => x.totalDeaths):0.#} por campaña   ·   reclutados por la Leva: {r.Average(x => x.recruits):0.#}",
                $"Roster final (media): {r.Average(x => x.finalRosterSize):0.#}   ·   Paga final (media): {r.Average(x => x.finalTronos):0}",
                "",
                $"la Capilla usada: {reclusiam} veces · Enfermería: {enfermeria} · Leva: {levaLines} reclutas",
                $"Emboscadas: {ambushes} · Trampas: {traps} · Refugios: {camps} · Pruebas de Fe: {soulTrials} · Bordes de la Muerte: {doors}",
                $"Peligros ambientales (rondas) — nubes de Gas: {CountLines("Nube de Gas")} · cielo rojo: {CountLines("Cielo rojo")} · máscara salvó: {CountLines("máscaras aguantan")}",
                $"Traumas — Conmocionado: {CountLines("— CONMOCIONADO")} · Desesperado: {CountLines("— DESESPERADO")} · Saqueador: {CountLines("— SAQUEADOR")} · Apóstata: {CountLines("— APÓSTATA")}",
                $"Caídos a la oscuridad (200 Mancha): {CountLines("se somete a la oscuridad")}",
                $"Saqueador — codicia (acciones perdidas): {CountLines("se pierde rebuscando")} · saqueo (botín mermado): {CountLines("mete mano al bot")}",
                $"HUMANOS §9.1 — bombardeos: {CountLines("DETONA")} · rally del Iluminado: {CountLines("inflama al culto")} · Cabecillas caídos: {CountLines("cae Cabecilla")}",
                $"Bestiario del Foso en combate (menciones) — Carne §9.2: {CountLines("Devoracadáveres") + CountLines("Supurante") + CountLines("Cosido") + CountLines("Reptante") + CountLines("Boca en la Pared") + CountLines("Verdugo")} · Descendidos §9.3: {CountLines("Sabueso del Foso") + CountLines("Corista") + CountLines("Marcador")} · Fauna §9.4: {CountLines("Plaga de ratas") + CountLines("Cuervo carro") + CountLines("Bestia del Alambre") + CountLines("Carroñero mutado")}",
                $"‼ VIOLACIONES DE INVARIANTES: {violations}  (debe ser 0)",
                "",
                "NOTA: la IA no usa consumibles ni acciones menores y elige habilidades al azar:",
                "es un suelo pesimista. Un jugador competente sobrevive bastante más.",
                "",
                "—— LOG DE EJEMPLO (" + (r.Any(x => x.stageWon) ? "primera campaña GANADA" : r.Any(x => x.planetFallen) ? "primera campaña con planeta caído" : "primera campaña") + ") ——"
            };

            var sample = r.FirstOrDefault(x => x.stageWon);
            if (sample.log == null) sample = r.FirstOrDefault(x => x.planetFallen);
            if (sample.log == null) sample = r[0];
            // Keep the sample readable: cap to the last ~400 lines
            var slog = sample.log;
            if (slog.Count > 400) slog = slog.Skip(slog.Count - 400).ToList();
            lines.AddRange(slog);

            return string.Join(Environment.NewLine, lines);
        }
    }
}
