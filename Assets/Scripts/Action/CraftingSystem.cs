using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CraftingSystem : MonoBehaviour
{
    public static CraftingSystem Instance;

    [Header("UI References")]
    [SerializeField] private Transform recipeListContainer;
    [SerializeField] private GameObject recipeButtonPrefab;
    
    [Header("Recipe Detail Panel")]
    [SerializeField] private GameObject recipeDetailPanel;
    [SerializeField] private TextMeshProUGUI recipeNameDescriptionText;
    [SerializeField] private Transform ingredientsContainer;
    [SerializeField] private GameObject ingredientDisplayPrefab;
    [SerializeField] private Button craftButton;
    [SerializeField] private TextMeshProUGUI craftButtonText;
    
    [Header("Crafting Animation")]
    [SerializeField] private Slider craftingProgressBar;
    [SerializeField] private TextMeshProUGUI craftingStatusText;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip craftingBuildupSound;
    [SerializeField] private AudioClip craftingSuccessSound;
    [SerializeField] private AudioClip craftingFailureSound;
    
    [Header("Recipes")]
    [SerializeField] private List<CraftingRecipe> allRecipes;
    
    private CraftingRecipe selectedRecipe;
    private bool isCrafting = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        if (recipeDetailPanel != null)
            recipeDetailPanel.SetActive(false);
        
        
        if (craftButton != null)
            craftButton.onClick.AddListener(StartCrafting);
    }

    void Update()
    {
    }

    public void PopulateRecipeList()
    {
        // Clear existing buttons
        foreach (Transform child in recipeListContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Create button for each recipe
        foreach (var recipe in allRecipes)
        {
            GameObject buttonObj = Instantiate(recipeButtonPrefab, recipeListContainer);
            
            // Setup button
            Button button = buttonObj.GetComponent<Button>();
            button.onClick.AddListener(() => SelectRecipe(recipe));
            
            // Setup icon
            var icon = buttonObj.transform.Find("Item")?.GetComponent<Image>();
            if (icon != null && recipe.outputItem.icon != null)
                icon.sprite = recipe.outputItem.icon;
            
            // Setup text
            TextMeshProUGUI nameText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
                nameText.text = recipe.outputItem.itemName;
            
            // Color code based on availability
            bool canCraft = recipe.CanCraft();
            if (nameText != null)
                nameText.color = canCraft ? Color.gray : Color.red;
        }
    }

    void SelectRecipe(CraftingRecipe recipe)
    {
        selectedRecipe = recipe;
        
        if (recipeDetailPanel != null)
            recipeDetailPanel.SetActive(true);
        
        // Update detail panel
        if (recipeNameDescriptionText != null)
            recipeNameDescriptionText.text = $"{recipe.outputItem.itemName}<size=75><color=#AAAAAA> • {recipe.outputItem.itemDescription}</color></size>";
        
        // Clear ingredients
        foreach (Transform child in ingredientsContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Display ingredients
        foreach (var ingredient in recipe.ingredients)
        {
            GameObject ingredientObj = Instantiate(ingredientDisplayPrefab, ingredientsContainer);
            
            // Icon            
            var icon = ingredientObj.transform.Find("Item")?.GetComponent<Image>();
            if (icon != null && ingredient.item.icon != null)
                icon.sprite = ingredient.item.icon;
            
            // Counter text
            TextMeshProUGUI counterText = ingredientObj.GetComponentInChildren<TextMeshProUGUI>();
            if (counterText != null)
            {
                int have = InventorySystem.Instance.GetItemCount(ingredient.item);
                int need = ingredient.quantity;
                counterText.text = $"{have}/{need}";
                counterText.color = have >= need ? Color.green : Color.red;
            }
        }
        
        // Update craft button
        bool canCraft = recipe.CanCraft();
        if (craftButton != null)
        {
            craftButton.interactable = canCraft;
            if (craftButtonText != null)
            {
                craftButtonText.text = canCraft ? $"Craft ({recipe.successChance * 100}% success)" : "Missing Materials";
            }
        }
    }

    void StartCrafting()
    {
        if (selectedRecipe == null || isCrafting) return;
        
        if (!selectedRecipe.CanCraft())
        {
            Debug.Log("Cannot craft: missing ingredients!");
            return;
        }
        
        StartCoroutine(CraftingSequence());
    }

    IEnumerator CraftingSequence()
    {
        isCrafting = true;
        
        // Hide panels
        if (recipeListContainer != null)
            recipeListContainer.gameObject.SetActive(false);
        
        if (craftingStatusText != null)
            craftingStatusText.text = "Crafting...";
        
        // Consume ingredients
        selectedRecipe.ConsumeIngredients();
        
        // Play buildup sound
        if (audioSource != null && craftingBuildupSound != null)
            audioSource.PlayOneShot(craftingBuildupSound);
        
        // Animate progress bar
        float elapsed = 0f;
        float duration = selectedRecipe.craftingTime;
        craftingProgressBar.value = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            if (craftingProgressBar != null)
                craftingProgressBar.value = progress;

            yield return null;
        }

        // Ensure it finishes at 100%
        if (craftingProgressBar != null)
            craftingProgressBar.value = 1f;
        
        // Determine success/failure
        float roll = Random.value;
        bool success = roll <= selectedRecipe.successChance;
        
        // Show result
        if (success)
        {
            // Success!
            if (craftingStatusText != null)
                craftingStatusText.text = "Success!";
            
            if (audioSource != null && craftingSuccessSound != null)
                audioSource.PlayOneShot(craftingSuccessSound);
            
            // Add item to inventory
            InventorySystem.Instance.AddItem(selectedRecipe.outputItem, selectedRecipe.outputQuantity);
        }
        else
        {
            // Failure!
            if (craftingStatusText != null)
                craftingStatusText.text = "Failed...";

            if (audioSource != null && craftingFailureSound != null)
                audioSource.PlayOneShot(craftingFailureSound);

            // Add all failure items
            if (selectedRecipe.failureOutputs != null)
            {
                selectedRecipe.GiveFailureItems();
            }
        }
        
        // Wait for player to see result
        yield return new WaitForSeconds(2f);
        
        // Show crafting panel again
        if (recipeListContainer != null)
            recipeListContainer.gameObject.SetActive(true);
        
        // Refresh
        PopulateRecipeList();
        if (selectedRecipe != null)
            SelectRecipe(selectedRecipe);
        
        isCrafting = false;
    }
}