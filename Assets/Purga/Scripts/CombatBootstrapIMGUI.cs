using UnityEngine;

namespace Purga
{
    /// <summary>
    /// IMGUI-only view state for CombatBootstrap.
    ///
    /// Gameplay state remains in CombatBootstrap.cs; these fields only describe
    /// what the player is currently inspecting, targeting, or scrolling. Keeping
    /// them together is the first boundary for replacing IMGUI screen by screen
    /// with UI Toolkit without changing combat or campaign rules.
    /// </summary>
    public partial class CombatBootstrap
    {
        Vector2 logScroll;
        Vector2 abilityScroll;
        Vector2 shipScroll;
        bool showBarracon;
        bool showTienda;
        int gearHeroIdx = -1;
        string seedInput = "";

        AbilityDef pendingAbility;
        ConsumableDef pendingConsumable;
        bool pendingGive;
        bool burnSanity;
        bool pendingOrder;
        CombatUnit orderFirst;
        ItemStack pendingItem;
    }
}
