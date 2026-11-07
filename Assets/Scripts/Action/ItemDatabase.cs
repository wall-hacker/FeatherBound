using UnityEngine;
using System.Collections.Generic;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance;

    [SerializeField] private List<Item> allItems;

    private Dictionary<string, Item> itemLookup;

    void Awake()
    {
        Instance = this;
        itemLookup = new Dictionary<string, Item>();

        foreach (var item in allItems)
        {
            if (!itemLookup.ContainsKey(item.itemName))
                itemLookup.Add(item.itemName, item);
        }
    }

    public Item GetItem(string name)
    {
        itemLookup.TryGetValue(name, out Item item);
        return item;
    }
}
