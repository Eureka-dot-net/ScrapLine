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
    
    [Header("Visual Ingredient Display (Optional)")]
    [Tooltip("Component to display selected recipe ingredients with icons (optional)")]
    public RecipeIngredientDisplay ingredientDisplay;

    private CellData contextCellData; // Used to determine machine type
    private RecipeDef currentlyDisplayedRecipe; // Track which recipe is being displayed

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
        // Set button text (just output name, not full ingredients since we show visually)
        SetButtonText(buttonObj, displayName);

        // Set button sprite based on first output item
        if (recipe?.outputItems != null && recipe.outputItems.Count > 0)
        {
            string outputItemId = recipe.outputItems[0].item;
            ItemDef outputItem = FactoryRegistry.Instance?.GetItem(outputItemId);
            
            if (outputItem != null && !string.IsNullOrEmpty(outputItem.sprite))
            {
                string spritePath = $"Sprites/Items/{outputItem.sprite}";
                Sprite itemSprite = LoadSprite(spritePath);
                SetButtonImage(buttonObj, itemSprite);
            }
        }

        // Add hover/click handlers to show ingredient display
        var button = buttonObj.GetComponent<UnityEngine.UI.Button>();
        if (button != null && ingredientDisplay != null)
        {
            // Store recipe reference for button click
            button.onClick.AddListener(() => OnRecipeButtonClicked(recipe));
        }
    }

    /// <summary>
    /// Handle recipe button click to update ingredient display
    /// </summary>
    /// <param name="recipe">Recipe to display</param>
    private void OnRecipeButtonClicked(RecipeDef recipe)
    {
        // Update ingredient display when button is clicked
        if (ingredientDisplay != null && recipe != null)
        {
            currentlyDisplayedRecipe = recipe;
            ingredientDisplay.DisplayRecipe(recipe);
            GameLogger.Log(LoggingManager.LogCategory.UI, $"Displaying ingredients for recipe: {GetDisplayName(recipe)}", ComponentId);
        }
    }

    /// <summary>
    /// Override to clear ingredient display when panel is hidden
    /// </summary>
    public override void HidePanel()
    {
        if (ingredientDisplay != null)
        {
            ingredientDisplay.ClearIngredients();
            currentlyDisplayedRecipe = null;
        }
        
        base.HidePanel();
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