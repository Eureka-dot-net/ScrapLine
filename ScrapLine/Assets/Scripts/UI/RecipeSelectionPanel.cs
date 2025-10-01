using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Selection panel for recipes (used by fabricator machines).
/// Displays available RecipeDef objects filtered by machine type.
/// 
/// UNITY SETUP:
/// 1. Create UI Panel with Grid Layout Group  
/// 2. Add this component to the panel
/// 3. Assign selectionPanel, buttonContainer
/// 4. Assign buttonPrefab (simple button for "None" option)
/// 5. Create a RecipeIngredientDisplay prefab and assign to ingredientDisplayRow (for actual recipes)
/// 6. The ingredientDisplayRow prefab should have a Button component to make it clickable
/// 
/// NOTE: This now uses the same RecipeIngredientDisplay pattern as FabricatorMachineConfigPanel
/// for consistency. Each recipe gets its own RecipeIngredientDisplay instance.
/// 
/// IMPORTANT: Child UI elements (Images, Text) will have their raycastTarget disabled automatically
/// to prevent them from blocking button clicks. The Button component must be on the root GameObject.
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
            // None option - just set the text on the button
            SetButtonText(buttonObj, displayName);
            GameLogger.Log(LoggingManager.LogCategory.UI, 
                $"Setup None option button with text: {displayName}", ComponentId);
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
    /// Override to create selection buttons with proper prefab based on whether it's the "None" option
    /// </summary>
    protected override void CreateSelectionButton(RecipeDef item, string displayName)
    {
        GameObject prefabToUse;
        
        // Use buttonPrefab for "None" option, ingredientDisplayRow for actual recipes
        if (item == null)
        {
            // None option - use simple button prefab from base class
            prefabToUse = buttonPrefab;
            GameLogger.Log(LoggingManager.LogCategory.UI, 
                $"Using buttonPrefab for None option. buttonPrefab null? {buttonPrefab == null}", ComponentId);
        }
        else
        {
            // Actual recipe - use ingredientDisplayRow
            prefabToUse = ingredientDisplayRow != null ? ingredientDisplayRow.gameObject : buttonPrefab;
            GameLogger.Log(LoggingManager.LogCategory.UI, 
                $"Using ingredientDisplayRow for recipe '{displayName}'. ingredientDisplayRow null? {ingredientDisplayRow == null}, prefab: {prefabToUse?.name}", ComponentId);
        }
        
        if (buttonContainer == null || prefabToUse == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.UI, 
                $"Cannot create button - buttonContainer null? {buttonContainer == null}, prefabToUse null? {prefabToUse == null}", ComponentId);
            return;
        }

        // Count how many buttons already exist BEFORE instantiation
        int buttonIndex = buttonContainer.childCount;
        
        GameObject buttonObj = Instantiate(prefabToUse, buttonContainer);
        buttonObj.name = $"{prefabToUse.name}_Instance_{buttonIndex}_{displayName}";
        
        GameLogger.Log(LoggingManager.LogCategory.UI, 
            $"Instantiated button '{buttonObj.name}' at index {buttonIndex}", ComponentId);
        
        // Manually position the button/row to avoid LayoutGroup issues
        RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // Calculate height: parent width / 5 (same as RecipeIngredientDisplay does)
            RectTransform containerRect = buttonContainer as RectTransform;
            float parentWidth = containerRect != null ? containerRect.rect.width : 500f;
            float buttonHeight = parentWidth / 5f;
            
            GameLogger.Log(LoggingManager.LogCategory.UI, 
                $"Calculated buttonHeight = {buttonHeight} from parentWidth = {parentWidth}", ComponentId);
            
            // Set anchors to top-left FIRST
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(1, 1); // Stretch horizontally
            rectTransform.pivot = new Vector2(0.5f, 1);
            
            // Set the height explicitly
            rectTransform.sizeDelta = new Vector2(0, buttonHeight);
            
            // Position from top, each row below the previous
            rectTransform.anchoredPosition = new Vector2(0, -buttonIndex * buttonHeight);
            
            GameLogger.Log(LoggingManager.LogCategory.UI, 
                $"Positioned button at index {buttonIndex}, y={-buttonIndex * buttonHeight}, height={buttonHeight}", ComponentId);
        }
        
        Button button = buttonObj.GetComponent<Button>();
        
        // If Button not on root, try to find it in children
        if (button == null)
        {
            button = buttonObj.GetComponentInChildren<Button>();
            if (button != null)
            {
                GameLogger.LogWarning(LoggingManager.LogCategory.UI, 
                    $"Button component found on child GameObject '{button.gameObject.name}' instead of root. This may cause click issues. Please move Button component to root of prefab '{prefabToUse.name}'.", ComponentId);
            }
        }
        
        if (button != null)
        {
            // Set up button click listener
            button.onClick.AddListener(() => OnItemSelected(item));
            
            GameLogger.Log(LoggingManager.LogCategory.UI, 
                $"Added click listener to button for recipe: {displayName}", ComponentId);
            
            // Setup button visuals using derived class implementation (this creates child elements)
            SetupButtonVisuals(buttonObj, item, displayName);
            
            // Disable raycast on all child Images to prevent blocking button clicks
            // MUST be called AFTER SetupButtonVisuals since that creates the child elements
            DisableChildRaycastTargets(buttonObj);
        }
        else
        {
            GameLogger.LogError(LoggingManager.LogCategory.UI, 
                $"{GetType().Name}: Button prefab '{prefabToUse.name}' missing Button component on root GameObject or children! Button will not be clickable.", ComponentId);
        }
    }
    
    /// <summary>
    /// Disable raycast targets on all child UI elements to prevent them from blocking button clicks
    /// </summary>
    private void DisableChildRaycastTargets(GameObject buttonObj)
    {
        // Find the Button component (might be on root or child)
        Button button = buttonObj.GetComponent<Button>();
        if (button == null)
        {
            button = buttonObj.GetComponentInChildren<Button>();
        }
        
        GameObject buttonGameObject = button != null ? button.gameObject : buttonObj;
        
        // Get all Image components in children (including self)
        UnityEngine.UI.Image[] childImages = buttonObj.GetComponentsInChildren<UnityEngine.UI.Image>();
        GameLogger.Log(LoggingManager.LogCategory.UI, 
            $"Found {childImages.Length} Image components in button hierarchy", ComponentId);
            
        foreach (var img in childImages)
        {
            // Don't disable raycast on the Button's own Image component (if it has one)
            if (img.gameObject == buttonGameObject)
            {
                GameLogger.Log(LoggingManager.LogCategory.UI, 
                    $"Keeping raycast enabled on Button's own Image: {img.gameObject.name}", ComponentId);
                continue;
            }
            
            bool wasEnabled = img.raycastTarget;
            img.raycastTarget = false;
            GameLogger.Log(LoggingManager.LogCategory.UI, 
                $"Disabled raycast target on child Image: {img.gameObject.name} (was {wasEnabled})", ComponentId);
        }
        
        // Also disable Text components
        UnityEngine.UI.Text[] childTexts = buttonObj.GetComponentsInChildren<UnityEngine.UI.Text>();
        foreach (var txt in childTexts)
        {
            if (txt.gameObject != buttonGameObject)
            {
                txt.raycastTarget = false;
            }
        }
        
        // And TextMeshPro components
        TMPro.TextMeshProUGUI[] childTMPs = buttonObj.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
        foreach (var tmp in childTMPs)
        {
            if (tmp.gameObject != buttonGameObject)
            {
                tmp.raycastTarget = false;
            }
        }
        
        GameLogger.Log(LoggingManager.LogCategory.UI, 
            $"Completed disabling raycasts for button", ComponentId);
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