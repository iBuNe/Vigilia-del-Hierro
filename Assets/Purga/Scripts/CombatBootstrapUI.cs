using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Purga
{
    /// <summary>
    /// HITO 9 (provisional) — pantalla de combate en UI Toolkit, con el LAYOUT del mockup:
    /// dos rangos enfrentados (héroes / enemigos) del MISMO tamaño estilo DD, iluminados por la
    /// Bengala (luz ROJA suave que se atenúa de la retaguardia al frente y retrocede al bajar).
    /// Partial de CombatBootstrap para leer el estado del combate sin exponerlo.
    /// Convive con la IMGUI: por defecto se juega en IMGUI y F9 PREVISUALIZA esta pantalla.
    ///
    /// TODO el aspecto vive en Resources/Purga/UI/CombatTheme.uss. Los recuadros de unidad
    /// (.figure), el retrato y los iconos son MARCADORES: el artista mete su pixel-art como
    /// background-image en el USS, sin tocar este C#.
    /// </summary>
    public partial class CombatBootstrap
    {
        // ---- UI Toolkit state ----
        bool useToolkitUI = false; // false = IMGUI (jugable). F9 alterna. La tanda de interacción lo pondrá en true.
        bool toolkitReady;
        UIDocument uiDoc;
        VisualElement combatRoot;
        // top bar
        Label hdrNode, hdrRound, bengalaLabel, signalPct;
        VisualElement turnoTokens, flareGlow, bengalaMeter;
        // battlefield
        VisualElement lightGrad, heroesRank, enemiesRank;
        // bottom panel
        VisualElement portrait, abilitySlots, actionsRow, manchaBar;
        Label heroName, heroStats, abilityName, abilityTags, manchaLabel;

        bool ToolkitCombatActive => toolkitReady && useToolkitUI && !atShip && inCombat && !headless;

        // =================================================================== setup / toggle
        void SetupToolkitUI()
        {
            if (headless || !Application.isPlaying || toolkitReady) return;
            try
            {
                var ps = Resources.Load<PanelSettings>("Purga/UI/CombatPanelSettings");
                if (ps == null)
                {
                    Debug.LogWarning("[Purga] Falta CombatPanelSettings. Ejecuta 'Purga/UI/Crear ajustes de UI de combate'. De momento se juega en IMGUI.");
                    useToolkitUI = false; return;
                }
                var go = new GameObject("PurgaCombatUI");
                go.transform.SetParent(transform, false);
                uiDoc = go.AddComponent<UIDocument>();
                uiDoc.panelSettings = ps;
                var root = uiDoc.rootVisualElement;
                if (root == null) { useToolkitUI = false; return; }

                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font != null) root.style.unityFont = font;
                var uss = Resources.Load<StyleSheet>("Purga/UI/CombatTheme");
                if (uss != null) root.styleSheets.Add(uss);

                BuildCombatTree(root);
                toolkitReady = true;
                Debug.Log("[Purga] UI Toolkit de combate lista. F9 en combate para previsualizarla.");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Purga] No se pudo montar la UI Toolkit ({e.Message}). Se juega en IMGUI.");
                useToolkitUI = false; toolkitReady = false;
            }
        }

        void ToolkitHotkeyOnGUI()
        {
            if (headless) return;
            var e = Event.current;
            if (e != null && e.type == EventType.KeyDown && e.keyCode == KeyCode.F9)
            {
                useToolkitUI = !useToolkitUI;
                if (useToolkitUI && !toolkitReady) SetupToolkitUI();
                e.Use();
            }
        }

        void UpdateToolkitUI()
        {
            if (headless || !Application.isPlaying || combatRoot == null) return;
            bool show = ToolkitCombatActive;
            combatRoot.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (show) RefreshCombatTree();
        }

        // =================================================================== build helpers
        static VisualElement Div(string cls, VisualElement parent = null)
        {
            var v = new VisualElement();
            if (!string.IsNullOrEmpty(cls)) foreach (var c in cls.Split(' ')) v.AddToClassList(c);
            parent?.Add(v);
            return v;
        }
        static Label Lbl(string cls, VisualElement parent, string text = "")
        {
            var l = new Label(text);
            if (!string.IsNullOrEmpty(cls)) foreach (var c in cls.Split(' ')) l.AddToClassList(c);
            parent?.Add(l);
            return l;
        }
        static void Bar(VisualElement parent, string trackCls, string fillCls, float pct01)
        {
            var track = Div(trackCls, parent);
            var fill = Div(fillCls, track);
            fill.style.width = Length.Percent(Mathf.Clamp01(pct01) * 100f);
        }

        // =================================================================== build (once)
        void BuildCombatTree(VisualElement root)
        {
            combatRoot = Div("combat-root", root);

            // ---------- top bar ----------
            var top = Div("topbar", combatRoot);

            // left: comms-tower icon + % (no "SEÑAL" caption)
            var tl = Div("top-left", top);
            hdrNode = Lbl("hdr-node", tl);
            var sig = Div("signal-row", tl);
            var tower = Div("signal-tower", sig);   // transmission-tower icon (stacked rects + emitter)
            Div("st-emit", tower);
            Div("st-r1", tower); Div("st-r2", tower); Div("st-r3", tower); Div("st-r4", tower);
            signalPct = Lbl("signal-pct", sig);

            // center: red flare icon + BENGALA x/4 + shrinking meter
            var tc = Div("top-center", top);
            var flare = Div("flare", tc);
            flareGlow = Div("flare-glow", flare);   // red halo (dims as the flare burns down)
            Div("flare-tip", flare);
            Div("flare-stick", flare);
            bengalaLabel = Lbl("bengala-label", tc);
            bengalaMeter = Div("bengala-meter", tc);

            // right: RONDA + turn tokens
            var tr = Div("top-right", top);
            hdrRound = Lbl("hdr-round", tr);
            var trn = Div("turno-row", tr);
            Lbl("turno-caption", trn, "TURNO");
            turnoTokens = Div("turno-tokens", trn);

            // ---------- battlefield: two facing ranks under one red light field ----------
            var field = Div("battlefield", combatRoot);
            Div("ground", field);
            lightGrad = Div("light-grad", field);   // red light strips (behind), rebuilt each refresh
            lightGrad.pickingMode = PickingMode.Ignore;
            heroesRank = Div("rank rank-heroes", field);
            Div("nomans", field);
            enemiesRank = Div("rank rank-enemies", field);
            var axis = Div("axis-caption", field);
            axis.pickingMode = PickingMode.Ignore;
            Lbl("axis-side", axis, "◄ retaguardia · sombra");
            Lbl("axis-front", axis, "el frente · luz de la Bengala");
            Lbl("axis-side", axis, "sombra · retaguardia enemiga ►");

            // ---------- bottom panel ----------
            var bottom = Div("bottompanel", combatRoot);
            portrait = Div("portrait", bottom);
            var info = Div("hero-info", bottom);
            heroName = Lbl("hero-name", info);
            heroStats = Lbl("hero-stats", info);
            manchaLabel = Lbl("mancha-label", info);
            manchaBar = Div("mancha-wrap", info);

            var abil = Div("ability-area", bottom);
            var abilHead = Div("ability-head", abil);
            abilityName = Lbl("ability-name", abilHead);
            abilityTags = Lbl("ability-tags", abilHead);
            abilitySlots = Div("slots", abil);

            actionsRow = Div("actions", bottom);

            Lbl("hint", combatRoot, "UI Toolkit provisional (solo lectura) — F9 para volver a la interfaz clasica");
        }

        // =================================================================== refresh (per frame)
        void RefreshCombatTree()
        {
            if (campaign == null) return;
            float litFrac = MaxLight > 0 ? lightLevel / (float)MaxLight : 0f;

            // ---- top bar ----
            hdrNode.text = $"{NodeTypeLabel()}   ·   {SectorShort().ToUpper()}";
            hdrRound.text = $"RONDA {round}";
            signalPct.text = $"{Signal}%";
            bengalaLabel.text = $"BENGALA  {lightLevel}/{MaxLight}";
            flareGlow.style.opacity = 0.25f + 0.75f * litFrac;
            bengalaMeter.Clear();
            Bar(bengalaMeter, "bmeter-track", "bmeter-fill", litFrac);

            turnoTokens.Clear();
            var order = turnQueue.Skip(Mathf.Max(0, turnIndex)).Where(u => u.Alive && !u.retreated).Take(6).ToList();
            for (int i = 0; i < order.Count; i++)
            {
                var t = Div(order[i].TeamOf == Team.Heroes ? "turno-tok tok-hero" : "turno-tok tok-enemy", turnoTokens);
                if (i == 0) t.AddToClassList("tok-now");
            }

            // ---- the light: the Bengala is thrown over the FRONT (no man's land = the CENTRE), so the
            // soft RED glow is brightest in the middle and fades toward BOTH rears (the edges). As the
            // flare burns down the lit centre shrinks and the darkness creeps in from the rears. ----
            lightGrad.Clear();
            const int strips = 30;
            float reach = 0.14f + 0.34f * litFrac; // half-width of the lit region around the front (centre)
            for (int i = 0; i < strips; i++)
            {
                float f = strips > 1 ? i / (float)(strips - 1) : 0f; // 0 = retaguardia héroe … 0.5 = frente … 1 = retaguardia enemiga
                float d = Mathf.Abs(f - 0.5f);                       // distancia al frente (la bengala está en el centro)
                float glow = Mathf.Clamp01((reach - d) / 0.26f);     // 1 en el frente, → 0 hacia las retaguardias
                float a = Mathf.Clamp01(0.09f + glow * 0.44f);       // suelo rojo tenue + glow del frente
                var s = Div("grad-strip", lightGrad);
                s.style.backgroundColor = new Color(0.58f, 0.16f, 0.13f, a); // rojo suave de la Bengala
            }

            // ---- two facing ranks, same size (DD): heroes rear→front (zone asc), enemies front→rear ----
            var cur = Current();
            heroesRank.Clear();
            foreach (var h in heroes.Where(x => !x.retreated).OrderBy(x => x.zone))
                heroesRank.Add(UnitFigure(h, false, h == cur));
            enemiesRank.Clear();
            foreach (var e in enemies)
                enemiesRank.Add(UnitFigure(e, true, e == cur));

            // ---- bottom panel: feature the acting hero (or first alive hero) ----
            var hero = (cur != null && cur.TeamOf == Team.Heroes) ? cur
                     : heroes.FirstOrDefault(h => h.Alive && !h.retreated);
            RefreshBottom(hero);
        }

        VisualElement UnitFigure(CombatUnit u, bool enemy, bool active)
        {
            bool lit = Lit(u);
            var col = Div(enemy ? "unit unit-enemy" : "unit", null);
            col.AddToClassList(lit ? "unit-lit" : "unit-dark");
            if (active) col.AddToClassList("unit-active");
            if (!u.Alive) col.AddToClassList("unit-dead");

            Lbl("lightmark", col, lit ? "☼" : "▓");

            var fig = Div("figure", col);
            Lbl("figure-initial", fig, string.IsNullOrEmpty(u.unitName) ? "?" : u.unitName.Substring(0, 1));

            float hpPct = u.EffMaxHP > 0 ? Mathf.Max(0, u.hp) / (float)u.EffMaxHP : 0f;
            Bar(col, "hpbar", enemy ? "hpfill hpfill-enemy" : "hpfill", u.hp <= 0 ? 0f : hpPct);

            var toks = Div("token-row", col);
            foreach (var s in u.statuses.Where(s => s.kind != StatusKind.None).Take(4))
            {
                var chip = Div("token " + StatusClass(s.kind), toks);
                Lbl("token-txt", chip, StatusGlyph(s.kind, s.power));
            }
            return col;
        }

        void RefreshBottom(CombatUnit hero)
        {
            portrait.Clear();
            actionsRow.Clear();
            abilitySlots.Clear();
            manchaBar.Clear();
            if (hero == null)
            {
                heroName.text = ""; heroStats.text = ""; manchaLabel.text = "";
                abilityName.text = ""; abilityTags.text = "";
                return;
            }

            Lbl("portrait-initial", portrait, hero.unitName.Substring(0, 1));
            heroName.text = hero.unitName;

            var feat = pendingAbility ?? hero.def.abilities.FirstOrDefault();
            heroStats.text = StatLine(feat);
            abilityName.text = feat != null ? feat.displayName : "";
            abilityTags.text = AbilityTags(feat);

            manchaLabel.text = $"Mancha {hero.corruption}/200 · Llama {hero.faith}";
            Bar(manchaBar, "mancha-track", "mancha-fill", hero.corruption / 200f);

            var list = hero.def.abilities.Take(5).ToList();
            for (int i = 0; i < 5; i++)
            {
                var slot = Div("slot", abilitySlots);
                if (i < list.Count)
                {
                    if (list[i] == feat) slot.AddToClassList("slot-sel");
                    Lbl("slot-glyph", slot, "◆");
                    string uses = hero.usesLeft.ContainsKey(list[i]) ? hero.usesLeft[list[i]].ToString() : "";
                    if (!string.IsNullOrEmpty(uses)) Lbl("slot-badge", slot, uses);
                }
                else slot.AddToClassList("slot-empty");
            }

            var moveCol = Div("move-col", actionsRow);
            Lbl("act act-move", moveCol, "AVANZAR ›");
            Lbl("act act-move", moveCol, "‹ REPLEGAR");
            ActionIcon("BENGALA", flareCharges > 0 ? flareCharges.ToString() : "");
            ActionIcon("PASAR", "");
            ActionIcon("MOCHILA", "");
            ActionIcon("MAPA", "");
            ActionIcon("DIARIO", "");
        }

        void ActionIcon(string label, string badge)
        {
            var b = Div("act-icon", actionsRow);
            Div("act-glyph", b);
            Lbl("act-label", b, label);
            if (!string.IsNullOrEmpty(badge)) Lbl("act-badge", b, badge);
        }

        // =================================================================== small helpers
        string NodeTypeLabel()
        {
            if (expedition?.node == null) return "INCURSION";
            switch (expedition.node.type)
            {
                case FrontNodeType.Asalto: return "ASALTO";
                case FrontNodeType.Sabotaje: return "SABOTAJE";
                case FrontNodeType.Rescate: return "RESCATE";
                case FrontNodeType.Aguantar: return "AGUANTAR LA LINEA";
                case FrontNodeType.Opcional: return "OPCIONAL";
                case FrontNodeType.Jefe: return "JEFE";
                case FrontNodeType.Sellado: return "SELLAR LA HERIDA";
                default: return "INCURSION";
            }
        }
        string SectorShort()
        {
            var n = SectorName(campaign.currentSector);
            int i = n.IndexOf('·');
            return (i >= 0 ? n.Substring(i + 1) : n).Trim();
        }
        static string StatLine(AbilityDef a)
        {
            if (a == null) return "";
            string s = a.dmgMax > 0 ? $"DMG {a.dmgMin}-{a.dmgMax}"
                     : a.healMax > 0 ? $"CURA {a.healMin}-{a.healMax}" : "SIN DAÑO";
            if (a.critPct > 0) s += $"   CRIT {a.critPct}%";
            if (a.ignoresArmor) s += "   · Ignora arm.";
            return s;
        }
        static string AbilityTags(AbilityDef a)
        {
            if (a == null) return "";
            if (a.warpDanger) return "PSIQUICO · FOSO";
            if (a.isFaithAbility) return "MILAGRO · LLAMA";
            if (a.isFire) return "FUEGO";
            return "";
        }
        static string StatusClass(StatusKind k)
        {
            switch (k)
            {
                case StatusKind.Bleed: return "tok-bleed";
                case StatusKind.Burn: return "tok-burn";
                case StatusKind.Stun: return "tok-stun";
                case StatusKind.Defend: case StatusKind.Guard:
                case StatusKind.AccBuff: case StatusKind.DodgeBuff: case StatusKind.DmgBuffPct: return "tok-buff";
                default: return "tok-debuff";
            }
        }
        static string StatusGlyph(StatusKind k, int power)
        {
            switch (k)
            {
                case StatusKind.Bleed: return "S";
                case StatusKind.Burn: return "F";
                case StatusKind.Stun: return "!";
                case StatusKind.Defend: return "D";
                case StatusKind.Guard: return "G";
                case StatusKind.AccBuff: return "+" + power;
                case StatusKind.AccDebuff: return "-" + power;
                case StatusKind.DodgeBuff: return "E";
                case StatusKind.DmgBuffPct: return "%";
                case StatusKind.SpeedDebuff: return "v";
                default: return "•";
            }
        }
    }
}
