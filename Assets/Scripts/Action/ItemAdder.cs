using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class ItemAdder : MonoBehaviour
{
    [System.Serializable]
    public class ItemTuple
    {
        public Item item;
        public int quantity;
    }

    [Header("Items To Add")]
    public List<ItemTuple> itemsToAdd;

    void Start()
    {
        
        StartCoroutine(DelayedItemAdd());
    }

    IEnumerator DelayedItemAdd()
    {
        yield return new WaitForSeconds(0.1f);
        foreach (ItemTuple itemTuple in itemsToAdd)
        {
            InventorySystem.Instance.AddItem(itemTuple.item, itemTuple.quantity);
        }
    }
}
