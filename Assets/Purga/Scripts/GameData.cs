using System;
using UnityEngine;

namespace Purga
{
    /// <summary>
    /// Runtime access to generated gameplay data.
    ///
    /// Authoring contract: Library.cs is the single editable source of truth.
    /// DataAssetGenerator turns it into the ScriptableObject assets under
    /// Resources/Purga/, and runtime always consumes those generated assets.
    /// Never edit generated assets in the Inspector: the next regeneration
    /// replaces them.
    /// </summary>
    public static class GameData
    {
        static T Required<T>(string path, string key) where T : ScriptableObject
        {
            var asset = Resources.Load<T>(path);
            if (asset != null) return asset;

            throw new InvalidOperationException(
                $"[Purga] Falta el dato generado '{key}' en Resources/{path}. " +
                "Ejecuta Purga/Datos/Regenerar assets desde Library.");
        }

        public static UnitDef Unit(string key) => Required<UnitDef>("Purga/Units/" + key, key);
        public static ItemDef Item(string key) => Required<ItemDef>("Purga/Items/" + key, key);
        public static GearDef Gear(string key) => Required<GearDef>("Purga/Gear/" + key, key);
        public static ConsumableDef Consumable(string key) => Required<ConsumableDef>("Purga/Consumables/" + key, key);

        /// <summary>All gear keys (el Barracón: armería y tienda del Taller).</summary>
        public static readonly string[] GearKeys =
        {
            "FusilLargo", "Recortada", "BayonetaAfilada", "RevolverRepeticion",
            "CorazaAsalto", "PetoLigero", "PlacasTrinchera",
            "MedallaVigilia", "ColmilloFoso", "RosarioLaton",
            "AnilloOficial", "SelloPlomo", "AnilloHerida",
        };
    }
}
