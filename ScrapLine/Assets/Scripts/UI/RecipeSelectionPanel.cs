using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Selection panel for recipes (used by fabricator machines).
/// Displays available RecipeDef objects filtered by machine type.
/// 
/// UNITY SETUP:
/// 1. Create UI Panel with Grid Layout Group  
/// 2. Add this component to the panel
/// 3. Assign selectionPanel, buttonContainer
/// 4. Create a RecipeIngredientDisplay prefab and assign to ingredientDisplayRow
/// 5. The prefab should have a Button component to make it clickable
/// 
/// NOTE: This now uses the same RecipeIngredientDisplay pattern as FabricatorMachineConfigPanel
/// for consistency. Each recipe gets its own RecipeIngredientDisplay instance.
/// </summary>
public class RecipeSelectionPanel : BaseSelectionPanel<RecipeDef>
{
    [Header("Recipe Selection Specific")]
    [Tooltip("Machine ID to filter recipes by (set at runtime)")]
    public string machineId = "";
    
    [Tooltip("RecipeIngredientDisplay prefab - creates a row instance for each recipe (same pattern as FabricatorMachineConfigPanel)")]
    public RecipeIngredientDisplay ingredientDisplayRow;

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
        if (recipe == null)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, 
                "Recipe is null - cannot setup visuals", ComponentId);
            return;
        }

        if (ingredientDisplayRow == null)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, 
                "ingredientDisplayRow is null - cannot setup visuals", ComponentId);
            return;
        }
        
        // Get or create the RecipeIngredientDisplay component on the button row
        RecipeIngredientDisplay displayComponent = buttonObj.GetComponent<RecipeIngredientDisplay>();
        
        if (displayComponent == null)
        {
            // If the button doesn't have the component, we need to create a child with it
            // This happens when buttonObj is instantiated from ingredientDisplayRow prefab
            displayComponent = buttonObj.GetComponentInChildren<RecipeIngredientDisplay>();
        }
        
        if (displayComponent == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.UI, 
                $"No RecipeIngredientDisplay component found on button or children for recipe '{displayName}'", ComponentId);
            return;
        }
        
        GameLogger.Log(LoggingManager.LogCategory.UI, 
            $"Displaying recipe '{displayName}' with {recipe.inputItems?.Count ?? 0} inputs using RecipeIngredientDisplay", ComponentId);
        
        // Use the standardized RecipeIngredientDisplay to show the recipe
        displayComponent.DisplayRecipe(recipe);
    }
    
    /// <summary>
    /// Override to use ingredientDisplayRow prefab's GameObject if assigned, otherwise use buttonPrefab
    /// </summary>
    protected override GameObject GetButtonPrefabToUse()
    {
        // If user assigned the RecipeIngredientDisplay prefab, use its GameObject as the button
        if (ingredientDisplayRow != null)
        {
            return ingredientDisplayRow.gameObject;
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