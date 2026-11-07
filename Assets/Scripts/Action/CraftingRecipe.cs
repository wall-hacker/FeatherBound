using UnityEngine;

[System.Serializable]
public class RecipeIngredient
{
    public Item item;
    public int quantity;
}

[CreateAssetMenu(fileName = "New Recipe", menuName = "Crafting/Recipe")]
public class CraftingRecipe : ScriptableObject
{    
    [Header("Ingredients")]
    public RecipeIngredient[] ingredients;
    
    [Header("Output")]
    public Item outputItem;
    public int outputQuantity = 1;
    
    [Header("Crafting Settings")]
    [Range(0f, 1f)] public float successChance = 1f;
    public float craftingTime = 2f;
    
    [Header("On Failure")]
    public RecipeIngredient[] failureOutputs;

    public bool CanCraft()
    {
        foreach (var ingredient in ingredients)
        {
            int have = InventorySystem.Instance.GetItemCount(ingredient.item);
            if (have < ingredient.quantity)
                return false;
        }
        return true;
    }
    
    public void ConsumeIngredients()
    {
        foreach (var ingredient in ingredients)
        {
            InventorySystem.Instance.RemoveItem(ingredient.item, ingredient.quantity);
        }
    }

    public void GiveFailureItems()
    {
        foreach (var failure in failureOutputs)
        {
            InventorySystem.Instance.AddItem(failure.item, failure.quantity);
        }
    }
}
