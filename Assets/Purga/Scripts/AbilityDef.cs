using System;
using UnityEngine;

namespace Purga
{
    /// <summary>
    /// Data-driven ability. Lives in its own file (class name = file name) so
    /// Unity can bind the script reference when serialized as an .asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Purga/Ability", fileName = "NewAbility")]
    public class AbilityDef : ScriptableObject
    {
        public string displayName;
        public int[] usableFrom = { 1, 2, 3, 4 }; // positions (1 = front)
        public int[] targetPos = { 1, 2, 3, 4 };
        public TargetKind targetKind = TargetKind.Enemy;
        public bool area;                          // hits every valid unit in targetPos
        public int accuracy = 90;                  // <= 0 means auto-hit
        public int dmgMin, dmgMax, critPct;
        public int healMin, healMax;
        public int corruptionDelta;                // >0 inflict, <0 cleanse (applied to targets)
        public int faithDelta;                     // Fe applied to each hero target (Absolución +1)
        public int selfFaithDelta;                 // Fe applied to the USER on use (¡Por Cadia! +2)
        public bool isFaithAbility;                // hero faith abilities shake enemy Cohesion (-4)
        public StatusApply status = new StatusApply();
        public bool removesBleed;
        public int ammoCost;
        public int usesPerCombat;                  // 0 = unlimited
        public bool requiresMelee;                 // only usable after ToggleMelee
        public bool blockedInMelee;                // ranged weapon put away after ToggleMelee
        public SpecialKind special = SpecialKind.None;
        // -- Milestone 2.4 --
        public int faithCost;                      // Actos de Fe: paid by the whole group (highest Fe first)
        public bool warpDanger;                    // Psíquico: rolls Perils of the Warp (d20, 1-2 = pifia)
        public bool ignoresArmor;                  // psychic damage bypasses armor
        public bool isFire;                        // fire damage: x1.5 vs weakToFire units
        public int cohesionDelta;                  // extra enemy Cohesion damage on hit (Aplastamiento mental)
        public bool pushesBack;                    // pushes the target 1 position back on hit (Grito warp)
        // -- Milestone 5 --
        public int bossPhaseOnly;                  // 0 = any phase; else the boss must be in this phase to use it
        public int areaCorruption;                 // enemy AoE: +corruption to every living hero (Sermón)
        public int ralliesMoral;                   // el Iluminado: restores this much enemy Moral on use (rally)

        public static AbilityDef New(string n, Action<AbilityDef> cfg)
        {
            var a = CreateInstance<AbilityDef>();
            a.displayName = n;
            a.status = new StatusApply();
            cfg(a);
            return a;
        }
    }
}
