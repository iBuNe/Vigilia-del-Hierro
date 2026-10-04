using UnityEngine;
using System.Linq;

namespace Purga
{
    /// <summary>
    /// IMGUI exploration view: map traversal, room interactions, inventory,
    /// and unit cards. It is presentation code only; expedition rules remain
    /// on CombatBootstrap's gameplay partial.
    /// </summary>
    public partial class CombatBootstrap
    {
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


    }
}
