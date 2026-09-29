using UnityEngine;

namespace Purga
{
    /// <summary>
    /// Data-driven consumable (slice §5.2). Max 2 equipped per character,
    /// each is single-use, spent as a MINOR action on self or an adjacent ally.
    /// </summary>
    [CreateAssetMenu(menuName = "Purga/Consumable", fileName = "NewConsumable")]
    public class ConsumableDef : ScriptableObject
    {
        public string displayName;
        public int price;                  // tronos (shop arrives with the ship milestone)
        public int corruptionDelta;        // <0 cleanses (Amasec -10)
        public bool removesToxin;          // Contraveneno
        public int toxinImmuneRounds;      // Contraveneno: 3
        public bool removesQuebranto;      // Coraje embotellado
        public int endCombatCorruption;    // Coraje: +15 when the combat ends (the debt)
        public bool affectsGroup;          // Incienso: status applies to every living ally
        public StatusApply status = new StatusApply();          // main effect
        public StatusApply statusOnExpire = new StatusApply();  // applied when `status` expires (stim hangover)

        public static ConsumableDef New(string n, System.Action<ConsumableDef> cfg)
        {
            var c = CreateInstance<ConsumableDef>();
            c.displayName = n;
            c.status = new StatusApply();
            c.statusOnExpire = new StatusApply();
            cfg(c);
            return c;
        }
    }
}
