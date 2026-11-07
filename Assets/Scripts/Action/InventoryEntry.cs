[System.Serializable]
public class InventoryEntry
{
    public Item item;
    public int quantity;

    public InventoryEntry(Item item, int quantity)
    {
        this.item = item;
        this.quantity = quantity;
    }

    public bool IsEmpty => item == null;

    public void Clear()
    {
        item = null;
        quantity = 0;
    }
}
