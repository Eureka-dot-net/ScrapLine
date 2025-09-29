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
        // Set button text to include ingredients and output
        string enhancedDisplayName = GetEnhancedDisplayName(recipe, displayName);
        SetButtonText(buttonObj, enhancedDisplayName);

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

        // Try to find and update ingredient display if available
        UpdateButtonIngredientDisplay(buttonObj, recipe);
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

    /// <summary>
    /// Get enhanced display name that includes ingredient information
    /// </summary>
    /// <param name="recipe">Recipe to describe</param>
    /// <param name="baseDisplayName">Base display name</param>
    /// <returns>Enhanced display name with ingredients</returns>
    private string GetEnhancedDisplayName(RecipeDef recipe, string baseDisplayName)
    {
        if (recipe?.inputItems == null || recipe.inputItems.Count == 0)
            return baseDisplayName;

        // Create ingredients string 
        string ingredientsString = RecipeIngredientDisplay.GetIngredientsString(recipe);
        
        // Return format: "Output Item (ingredients)"
        return $"{baseDisplayName} ({ingredientsString})";
    }

    /// <summary>
    /// Update ingredient display in a recipe button if available
    /// </summary>
    /// <param name="buttonObj">Button object to check for ingredient display</param>
    /// <param name="recipe">Recipe to display</param>
    private void UpdateButtonIngredientDisplay(GameObject buttonObj, RecipeDef recipe)
    {
        // Try to find RecipeIngredientDisplay component in the button
        RecipeIngredientDisplay ingredientDisplay = buttonObj.GetComponentInChildren<RecipeIngredientDisplay>();
        
        if (ingredientDisplay != null)
        {
            if (recipe != null)
            {
                ingredientDisplay.DisplayRecipe(recipe);
                GameLogger.Log(LoggingManager.LogCategory.UI, $"Updated button ingredient display for recipe", ComponentId);
            }
            else
            {
                ingredientDisplay.ClearIngredients();
            }
        }
        // If no ingredient display found, that's fine - it's optional
    }
}