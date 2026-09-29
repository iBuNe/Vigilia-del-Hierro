using UnityEngine;

namespace Purga
{
    /// <summary>
    /// Data-driven expedition item (slice §5.1): supplies bought before leaving
    /// and loot found inside. Each occupies ONE of the 16 shared inventory
    /// slots regardless of charges.
    /// </summary>
    [CreateAssetMenu(menuName = "Purga/Item", fileName = "NewItem")]
    public class ItemDef : ScriptableObject
    {
        public string displayName;
        public int price;
        public int charges = 1;   // uses before the item is spent (Cargador: 6)
        public ItemKind kind;
        public int power;         // heal amount / corruption cleanse
        [TextArea] public string description;

        public static ItemDef New(string n, System.Action<ItemDef> cfg)
        {
            var i = CreateInstance<ItemDef>();
            i.displayName = n;
            cfg(i);
            return i;
        }
    }
}
