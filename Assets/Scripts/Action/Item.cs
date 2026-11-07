using UnityEngine;

public enum ItemCategory
{
    Currency,
    CraftingMaterial,
    Consumable,
    Weapon,
    Armor,
    Companion,
    Upgrade
}

public enum EquipmentSlotType
{
    None,
    Weapon,
    Helmet,
    Chest,
    Legs,
    Boots,
    Companion
}

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;
    [TextArea] public string itemTooltip = "press [E] to pick up";
    [TextArea] public string itemDescription;
    public Sprite icon;

    [Header("Classification")]
    public ItemCategory category;
    public EquipmentSlotType equipSlot = EquipmentSlotType.None;

    [Header("Stacking")]
    public bool stackable = true;
    public int maxStack = 99;

    [Header("Consumable Effects")]
    public int healthRestore;
    public int staminaRestore;
    [Tooltip("If true, stamina will go beyond max limit")]
    public bool grantSurplusStamina = true;

    [Header("Equipment Stats")]
    public float attackBonus;
    public float defenseBonus;

    [Header("Crafting")]
    public bool usedInCrafting = true;

    // Called when item is used
    public virtual void Use()
    {
        switch (category)
        {
            case ItemCategory.Consumable:
                Consume();
                break;

            case ItemCategory.Weapon:
            case ItemCategory.Armor:
            case ItemCategory.Companion:
                Equip();
                break;

            case ItemCategory.Upgrade:
                ApplyUpgrade();
                break;

            default:
                Debug.Log($"{itemName} cannot be used directly.");
                break;
        }
    }

    protected virtual void Consume()
    {
        // Find PlayerStats at runtime when the item is actually used
        PlayerStats playerStats = FindObjectOfType<PlayerStats>();
        
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats not found! Cannot consume item.");
            return;
        }
        
        Debug.Log($"Consuming {itemName}: Healing {healthRestore}, Stamina {staminaRestore}");
        
        playerStats.Heal(healthRestore);
        
        // Grant surplus stamina that goes beyond the limit
        if (grantSurplusStamina)
        {
            playerStats.RestoreSurplusStamina(staminaRestore);
        }
        else
        {
            playerStats.RestoreStamina(staminaRestore);
        }

        InventorySystem.Instance.RemoveItem(this);
    }

    protected virtual void Equip()
    {
        EquipmentSystem.Instance.Equip(this);
    }

    protected virtual void ApplyUpgrade()
    {
        // CraftingSystem.Instance.ApplyUpgrade(this);
        InventorySystem.Instance.RemoveItem(this);
    }
}