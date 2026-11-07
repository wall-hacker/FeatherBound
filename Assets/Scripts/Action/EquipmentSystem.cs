using UnityEngine;
using System.Collections.Generic;

public class EquipmentSystem : MonoBehaviour
{
    public static EquipmentSystem Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private EquipmentSlot weaponSlot;
    [SerializeField] private EquipmentSlot helmetSlot;
    [SerializeField] private EquipmentSlot chestSlot;
    [SerializeField] private EquipmentSlot legsSlot;
    [SerializeField] private EquipmentSlot bootsSlot;
    [SerializeField] private EquipmentSlot companionSlot;
    
    [Header("Player Stats")]
    [SerializeField] private PlayerStats playerStats;

    [Header("Lock Companion Slot")]
    [SerializeField] public bool isCompanionSlotLocked = true;
    
    private Dictionary<EquipmentSlotType, EquipmentSlot> slotMap;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        InitializeSlots();
    }

    void Start()
    {
        if (playerStats == null)
            playerStats = FindObjectOfType<PlayerStats>();
    }

    void InitializeSlots()
    {
        slotMap = new Dictionary<EquipmentSlotType, EquipmentSlot>
        {
            { global::EquipmentSlotType.Weapon, weaponSlot },
            { global::EquipmentSlotType.Helmet, helmetSlot },
            { global::EquipmentSlotType.Chest, chestSlot },
            { global::EquipmentSlotType.Legs, legsSlot },
            { global::EquipmentSlotType.Boots, bootsSlot },
            { global::EquipmentSlotType.Companion, companionSlot }
        };
    }

    public bool Equip(Item item, bool force = false)
    {
        if (item == null || item.equipSlot == global::EquipmentSlotType.None)
            return false;
        
        if (slotMap.TryGetValue(item.equipSlot, out EquipmentSlot slot))
        {
            Debug.Log($"Equipping {item.itemName} to {item.equipSlot} slot.");
            return slot.TryEquip(item, force);
        }
        
        return false;
    }

    public void ApplyItemStats(Item item, bool apply)
    {
        if (playerStats == null || item == null) return;
        
        int multiplier = apply ? 1 : -1;
        
        playerStats.ModifyAttack(item.attackBonus * multiplier);
        playerStats.ModifyDefense(item.defenseBonus * multiplier);
    }

    public Item GetEquippedItem(global::EquipmentSlotType slotType)
    {
        if (slotMap.TryGetValue(slotType, out EquipmentSlot slot))
        {
            return slot.equippedItem;
        }
        return null;
    }

    public void UnequipAll()
    {
        foreach (var slot in slotMap.Values)
            slot.Unequip();
    }

    public Item GetCompanion()
    {
        return companionSlot.equippedItem;
    }

    public Item GetWeapon()
    {
        return weaponSlot.equippedItem;
    }
}

