using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Purga.EditorTools
{
    /// <summary>
    /// Builds generated gameplay data from Library.cs.
    /// Library.cs is the only editable source of truth; the assets in
    /// Resources/Purga are generated runtime artifacts and must not be edited
    /// in the Inspector. Regeneration deliberately replaces them all.
    /// </summary>
    public static class DataAssetGenerator
    {
        const string UnitsRoot = "Assets/Purga/Resources/Purga/Units";
        const string ConsumablesRoot = "Assets/Purga/Resources/Purga/Consumables";

        static (string key, Func<UnitDef> make)[] UnitEntries => new (string, Func<UnitDef>)[]
        {
            ("Veteran",     Library.Veteran),
            ("Preacher",    Library.Preacher),
            ("Sister",      Library.Sister),
            ("Overseer",    Library.Overseer),
            ("Listener",    Library.Listener),
            ("Automaton",   Library.Automaton),
            ("Cultist",     Library.Cultist),
            ("Neophyte",    Library.Neophyte),
            ("Whisperer",   Library.Whisperer),
            ("Aberrant",    Library.Aberrant),
            ("Larva",       Library.Larva),
            ("Baron",              Library.Baron),
            ("FalseChaplain",      Library.FalseChaplain),
            ("Matriarch",          Library.Matriarch),
            ("Nest",               Library.Nest),
            ("Zealot",             Library.Zealot),
            ("Bomber",             Library.Bomber),
            ("Sniper",             Library.Sniper),
            ("Ringleader",         Library.Ringleader),
            ("Vip",                Library.Vip),
            // CARNE DEL FOSO §9.2 (Hito 5)
            ("Gorger",             Library.Gorger),
            ("Weeper",             Library.Weeper),
            ("Stitched",           Library.Stitched),
            ("StitchedHalf",       Library.StitchedHalf),
            ("Crawler",            Library.Crawler),
            ("Maw",                Library.Maw),
            ("Butcher",            Library.Butcher),
            ("Sapper",             Library.Sapper),
            ("Houndof",            Library.Houndof),
            ("Chorister",          Library.Chorister),
            ("Brander",            Library.Brander),
            ("Warden",             Library.Warden),
            ("Confessor",          Library.Confessor),
            ("Alpha",              Library.Alpha),
            ("Flayer",             Library.Flayer),
            ("RatSwarm",           Library.RatSwarm),
            ("Carrion",            Library.Carrion),
            ("WireBeast",          Library.WireBeast),
            ("Scavver",            Library.Scavver),
        };

        static (string key, Func<ConsumableDef> make)[] ConsumableEntries => new (string, Func<ConsumableDef>)[]
        {
            ("Recaf",        Library.Recaf),
            ("Amasec",       Library.Amasec),
            ("Estimulante",  Library.Estimulante),
            ("Contraveneno", Library.Contraveneno),
            ("Incienso",     Library.Incienso),
            ("Coraje",       Library.Coraje),
        };

        const string ItemsRoot = "Assets/Purga/Resources/Purga/Items";

        static (string key, Func<ItemDef> make)[] ItemEntries => new (string, Func<ItemDef>)[]
        {
            ("Cargador",    Library.Cargador),
            ("Promethium",  Library.Promethium),
            ("Racion",      Library.Racion),
            ("KitMedico",   Library.KitMedico),
            ("Sello",       Library.Sello),
            ("Servocraneo", Library.Servocraneo),
            ("Botin",       Library.Botin),
            ("AguaBendita", Library.AguaBendita),
            ("Ganzuas",     Library.Ganzuas),
            ("KitCampamento", Library.KitCampamento),
            ("Baliza",      Library.Baliza),
            ("MascaraGas",  Library.MascaraGas),
        };

        const string GearRoot = "Assets/Purga/Resources/Purga/Gear";

        static (string key, Func<GearDef> make)[] GearEntries => new (string, Func<GearDef>)[]
        {
            ("FusilLargo",         Library.FusilLargo),
            ("Recortada",          Library.Recortada),
            ("BayonetaAfilada",    Library.BayonetaAfilada),
            ("RevolverRepeticion", Library.RevolverRepeticion),
            ("CorazaAsalto",       Library.CorazaAsalto),
            ("PetoLigero",         Library.PetoLigero),
            ("PlacasTrinchera",    Library.PlacasTrinchera),
            ("MedallaVigilia",     Library.MedallaVigilia),
            ("ColmilloFoso",       Library.ColmilloFoso),
            ("RosarioLaton",       Library.RosarioLaton),
            ("AnilloOficial",      Library.AnilloOficial),
            ("SelloPlomo",         Library.SelloPlomo),
            ("AnilloHerida",       Library.AnilloHerida),
        };

        [MenuItem("Purga/Datos/Regenerar assets desde Library (sobrescribe)")]
        public static void RegenerateAll()
        {
            foreach (var (key, _) in UnitEntries)
                AssetDatabase.DeleteAsset($"{UnitsRoot}/{key}.asset");
            foreach (var (key, _) in ConsumableEntries)
                AssetDatabase.DeleteAsset($"{ConsumablesRoot}/{key}.asset");
            foreach (var (key, _) in ItemEntries)
                AssetDatabase.DeleteAsset($"{ItemsRoot}/{key}.asset");
            foreach (var (key, _) in GearEntries)
                AssetDatabase.DeleteAsset($"{GearRoot}/{key}.asset");
            Generate();
        }

        [MenuItem("Purga/Datos/Crear solo assets faltantes (recuperación)")]
        public static void Generate()
        {
            Directory.CreateDirectory(UnitsRoot);
            Directory.CreateDirectory(ConsumablesRoot);
            AssetDatabase.Refresh();

            int created = 0, skipped = 0;

            foreach (var (key, make) in UnitEntries)
            {
                string path = $"{UnitsRoot}/{key}.asset";
                if (AssetDatabase.LoadAssetAtPath<UnitDef>(path) != null) { skipped++; continue; }

                var unit = make();
                unit.name = key;
                AssetDatabase.CreateAsset(unit, path);
                foreach (var ability in unit.abilities)
                {
                    ability.name = ability.displayName;
                    AssetDatabase.AddObjectToAsset(ability, unit);
                }
                EditorUtility.SetDirty(unit);
                created++;
            }

            foreach (var (key, make) in ConsumableEntries)
            {
                string path = $"{ConsumablesRoot}/{key}.asset";
                if (AssetDatabase.LoadAssetAtPath<ConsumableDef>(path) != null) { skipped++; continue; }

                var item = make();
                item.name = key;
                AssetDatabase.CreateAsset(item, path);
                created++;
            }

            Directory.CreateDirectory(ItemsRoot);
            foreach (var (key, make) in ItemEntries)
            {
                string path = $"{ItemsRoot}/{key}.asset";
                if (AssetDatabase.LoadAssetAtPath<ItemDef>(path) != null) { skipped++; continue; }

                var item = make();
                item.name = key;
                AssetDatabase.CreateAsset(item, path);
                created++;
            }

            Directory.CreateDirectory(GearRoot);
            foreach (var (key, make) in GearEntries)
            {
                string path = $"{GearRoot}/{key}.asset";
                if (AssetDatabase.LoadAssetAtPath<GearDef>(path) != null) { skipped++; continue; }

                var gear = make();
                gear.name = key;
                AssetDatabase.CreateAsset(gear, path);
                created++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Purga] Assets de datos: {created} creados, {skipped} ya existían (no se tocan). " +
                      "El flujo normal es Regenerar assets desde Library.");
        }

        /// <summary>Batch/CI guard: every key declared by Library must have its generated asset.</summary>
        public static void ValidateGeneratedAssets()
        {
            int missing = 0;
            missing += CountMissing(UnitEntries, UnitsRoot);
            missing += CountMissing(ConsumableEntries, ConsumablesRoot);
            missing += CountMissing(ItemEntries, ItemsRoot);
            missing += CountMissing(GearEntries, GearRoot);
            if (missing > 0)
                throw new InvalidOperationException($"[Purga] Faltan {missing} assets generados. Ejecuta Regenerar assets desde Library.");
            Debug.Log("[Purga] Datos generados completos y disponibles para runtime.");
        }

        static int CountMissing<T>((string key, Func<T> make)[] entries, string root) where T : ScriptableObject
        {
            int missing = 0;
            foreach (var (key, _) in entries)
                if (AssetDatabase.LoadAssetAtPath<T>($"{root}/{key}.asset") == null) missing++;
            return missing;
        }
    }
}
