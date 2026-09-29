using System;
using System.Collections.Generic;
using UnityEngine;

namespace Purga
{
    /// <summary>
    /// Equippable gear (el Barracón): weapons, armour and pro/con trinkets (collar/anillo).
    /// Data-driven and self-contained: every field is a flat stat MODIFIER applied on top of the
    /// class base + Taller rank (decided with the user — simple & compatible, not a base-replacer).
    /// A gear icon Sprite can be added later (Hito 9) without touching combat logic.
    /// Its own file (class name = file name) so Unity binds the serialized script reference.
    /// </summary>
    [CreateAssetMenu(menuName = "Purga/Gear", fileName = "NewGear")]
    public class GearDef : ScriptableObject
    {
        public string displayName;
        public GearSlot slot;
        // Flat modifiers (may be negative — that is the "contra" of a trinket).
        public int dmgBonus;         // flat damage added to attacks
        public int accBonus;         // +precisión
        public int dodgeBonus;       // +esquiva
        public int speedBonus;       // +VEL (initiative)
        public int armorBonus;       // +armadura (flat reduction)
        public int hpBonus;          // +PV máximos
        public int manchaResBonus;   // +% resistencia a la Mancha
        public int critBonus;        // +% de crítico a los ataques
        public int price;            // la paga: Taller tienda cost (0 = solo botín)
        [TextArea] public string flavor;

        public static GearDef New(string n, GearSlot s, Action<GearDef> cfg)
        {
            var g = CreateInstance<GearDef>();
            g.displayName = n;
            g.slot = s;
            cfg?.Invoke(g);
            return g;
        }

        static void Part(List<string> to, int v, string label)
        {
            if (v != 0) to.Add((v > 0 ? "+" : "") + v + " " + label);
        }

        /// <summary>Human-readable pro/con line for tooltips (auto-built from the modifiers).</summary>
        public string Describe()
        {
            var parts = new List<string>();
            Part(parts, dmgBonus, "daño");
            Part(parts, accBonus, "prec");
            Part(parts, dodgeBonus, "esq");
            Part(parts, speedBonus, "VEL");
            Part(parts, armorBonus, "arm");
            Part(parts, hpBonus, "PV");
            Part(parts, manchaResBonus, "% res Mancha");
            Part(parts, critBonus, "% crít");
            return parts.Count == 0 ? "sin efecto" : string.Join(", ", parts);
        }
    }
}
