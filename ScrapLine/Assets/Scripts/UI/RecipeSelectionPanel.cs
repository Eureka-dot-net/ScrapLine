using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Selection panel for recipes (used by fabricator machines).
/// Displays available RecipeDef objects filtered by machine type.
/// 
/// UNITY SETUP:
/// 1. Create UI Panel with Grid Layout Group  
/// 2. Add this component to the panel
/// 3. Assign selectionPanel, buttonContainer, buttonPrefab
/// 4. Button prefab should have Button, Image, and Text components
/// </summary>
public class RecipeSelectionPanel : BaseSelectionPanel<RecipeDef>
{
    [Header("Recipe Selection Specific")]
    [Tooltip("Machine ID to filter recipes by (set at runtime)")]
    public string machineId = "";
    
    [Tooltip("IngredientDisplayContainer prefab - row container with Button component for each recipe")]
    public GameObject ingredientDisplayContainerPrefab;
    
    [Tooltip("PanelIngredientItemPrefab - shows individual ingredients (same as config panel uses)")]
    public GameObject panelIngredientItemPrefab;

    private CellData contextCellData; // Used to determine machine type

    /// <summary>
    /// Show the recipe selection panel for a specific machine
    /// </summary>
    /// <param name="cellData">Cell data to determine machine type</param>
    /// <param name="onSelected">Selection callback</param>
    public void ShowPanel(CellData cellData, System.Action<RecipeDef> onSelected)
    {
        contextCellData = cellData;
        machineId = cellData?.machineDefId ?? "";
        
        // Call base implementation
        ShowPanel(onSelected);
    }

    protected override List<RecipeDef> GetAvailableItems()
    {
        var recipes = new List<RecipeDef>();
        
        if (FactoryRegistry.Instance?.Recipes == null)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, "FactoryRegistry recipes not available", ComponentId);
            return recipes;
        }

        // Filter recipes by machine ID
        foreach (var recipe in FactoryRegistry.Instance.Recipes)
        {
            if (recipe.machineId == machineId)
            {
                recipes.Add(recipe);
            }
        }

        GameLogger.Log(LoggingManager.LogCategory.UI, $"Found {recipes.Count} recipes for machine '{machineId}'", ComponentId);
        return recipes;
    }

    protected override string GetDisplayName(RecipeDef recipe)
    {
        if (recipe == null) return "Unknown Recipe";
        
        // Use the first output item as display name
        if (recipe.outputItems != null && recipe.outputItems.Count > 0)
        {
            string outputItemId = recipe.outputItems[0].item;
            ItemDef outputItem = FactoryRegistry.Instance?.GetItem(outputItemId);
            
            if (outputItem != null)
            {
                return outputItem.displayName ?? outputItemId;
            }
            
            return outputItemId;
        }
        
        return "Recipe";
    }

    protected override void SetupButtonVisuals(GameObject buttonObj, RecipeDef recipe, string displayName)
    {
        if (recipe == null || panelIngredientItemPrefab == null)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, 
                "Recipe or panelIngredientItemPrefab is null - cannot setup visuals", ComponentId);
            return;
        }
        
        // The buttonObj is the IngredientDisplayContainer row
        // We need to populate it with PanelIngredientItemPrefab instances:
        // - One for each input ingredient
        // - One arrow/separator (optional)
        // - One for the output result
        
        // Find the container where we'll add ingredient item prefabs
        // This could be the buttonObj itself or a child container
        Transform container = buttonObj.transform;
        
        // Check if there's a specific child container (like "Content" or "ItemsContainer")
        Transform contentChild = buttonObj.transform.Find("Content");
        if (contentChild != null)
        {
            container = contentChild;
        }
        
        // Clear any existing children
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
        
        GameLogger.Log(LoggingManager.LogCategory.UI, 
            $"Creating ingredient items for recipe '{displayName}' with {recipe.inputItems?.Count ?? 0} inputs", ComponentId);
        
        // Create ingredient items for inputs
        if (recipe.inputItems != null)
        {
            foreach (var ingredient in recipe.inputItems)
            {
                CreateIngredientItem(container, ingredient.item, ingredient.count);
            }
        }
        
        // TODO: Add arrow/separator visual if needed
        // For now we can add a simple text element or skip
        
        // Create ingredient item for output/result
        if (recipe.outputItems != null && recipe.outputItems.Count > 0)
        {
            var output = recipe.outputItems[0];
            CreateIngredientItem(container, output.item, output.count);
        }
    }
    
    /// <summary>
    /// Create a single ingredient item instance in the container
    /// </summary>
    private void CreateIngredientItem(Transform container, string itemId, int count)
    {
        GameObject itemObj = Instantiate(panelIngredientItemPrefab, container);
        
        // Get item definition
        ItemDef itemDef = FactoryRegistry.Instance?.GetItem(itemId);
        if (itemDef == null)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, $"Item definition not found for '{itemId}'", ComponentId);
            return;
        }
        
        // Load sprite
        Sprite itemSprite = null;
        if (!string.IsNullOrEmpty(itemDef.sprite))
        {
            itemSprite = Resources.Load<Sprite>($"Sprites/Items/{itemDef.sprite}");
        }
        
        // Find and set the icon image (looking for child named "ItemIcon")
        Transform iconTransform = itemObj.transform.Find("ItemIcon");
        UnityEngine.UI.Image iconImage = null;
        
        if (iconTransform != null)
        {
            iconImage = iconTransform.GetComponent<UnityEngine.UI.Image>();
        }
        else
        {
            iconImage = itemObj.GetComponentInChildren<UnityEngine.UI.Image>();
        }
        
        if (iconImage != null && itemSprite != null)
        {
            iconImage.sprite = itemSprite;
            iconImage.color = Color.white;
        }
        
        // Find and set the count text (looking for child named "CountText")
        Transform textTransform = itemObj.transform.Find("CountText");
        TMPro.TextMeshProUGUI countText = null;
        
        if (textTransform != null)
        {
            countText = textTransform.GetComponent<TMPro.TextMeshProUGUI>();
        }
        else
        {
            countText = itemObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        }
        
        if (countText != null)
        {
            countText.text = count > 1 ? $"{count} x" : "";
        }
        
        GameLogger.Log(LoggingManager.LogCategory.UI, 
            $"Created ingredient item: {count}x {itemDef.displayName ?? itemId}", ComponentId);
    }
    
    /// <summary>
    /// Override to use ingredientDisplayContainerPrefab if assigned, otherwise use buttonPrefab
    /// </summary>
    protected override GameObject GetButtonPrefabToUse()
    {
        // If user assigned the IngredientDisplayContainer prefab, use that as the button
        if (ingredientDisplayContainerPrefab != null)
        {
            return ingredientDisplayContainerPrefab;
        }
        
        // Otherwise fallback to standard buttonPrefab
        return buttonPrefab;
    }

    protected override string GetNoneDisplayName()
    {
        return "No Recipe";
    }

    /// <summary>
    /// Generate a unique ID for a recipe based on its definition
    /// This is used to identify recipes since they don't have built-in IDs
    /// </summary>
    /// <param name="recipe">Recipe to generate ID for</param>
    /// <returns>Unique recipe ID</returns>
    public static string GetRecipeId(RecipeDef recipe)
    {
        if (recipe == null) return "";
        
        // Create a unique ID based on machine, inputs, and outputs
        string inputs = "";
        if (recipe.inputItems != null)
        {
            var inputStrings = new List<string>();
            foreach (var input in recipe.inputItems)
            {
                inputStrings.Add($"{input.item}:{input.count}");
            }
            inputs = string.Join(",", inputStrings);
        }
        
        string outputs = "";
        if (recipe.outputItems != null)
        {
            var outputStrings = new List<string>();
            foreach (var output in recipe.outputItems)
            {
                outputStrings.Add($"{output.item}:{output.count}");
            }
            outputs = string.Join(",", outputStrings);
        }
        
        return $"{recipe.machineId}_{inputs}_{outputs}";
    }

    /// <summary>
    /// Find a recipe by its generated ID
    /// </summary>
    /// <param name="recipeId">Recipe ID to search for</param>
    /// <returns>Matching recipe or null</returns>
    public static RecipeDef GetRecipeById(string recipeId)
    {
        if (string.IsNullOrEmpty(recipeId) || FactoryRegistry.Instance?.Recipes == null) 
            return null;
            
        foreach (var recipe in FactoryRegistry.Instance.Recipes)
        {
            if (GetRecipeId(recipe) == recipeId)
                return recipe;
        }
        
        return null;
    }
}