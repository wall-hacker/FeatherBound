using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public List<InventorySlotSave> inventory = new();
    public List<EquipmentSlotSave> equipment = new();
}

[Serializable]
public class InventorySlotSave
{
    public int slotIndex;
    public string itemName;
    public int quantity;
}

[Serializable]
public class EquipmentSlotSave
{
    public EquipmentSlotType slotType;
    public string itemName;
}
