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
/// 4. Assign buttonPrefab (simple clickable button - used for ALL rows)
/// 5. Create a RecipeIngredientDisplay prefab and assign to ingredientDisplayRow (visual display only, no Button needed)
/// 
/// ARCHITECTURE:
/// - buttonPrefab is used as the clickable background for ALL buttons (None and recipes)
/// - For recipes: ingredientDisplayRow is instantiated as a child ON TOP of the button, with all raycasts disabled
/// - This allows the recipe visuals to display while clicks pass through to the button underneath
/// 
/// NOTE: The ingredientDisplayRow should NOT have a Button component - it's purely for visual display.
/// All clicking is handled by the buttonPrefab underneath.
/// </summary>
public class RecipeSelectionPanel : BaseSelectionPanel<RecipeDef>
{
    [Header("Recipe Selection Specific")]
    [Tooltip("Machine ID to filter recipes by (set at runtime)")]
    public string machineId = "";
    
    [Tooltip("RecipeIngredientDisplay prefab - creates a row instance for each recipe (same pattern as FabricatorMachineConfigPanel)")]
    public RecipeIngredientDisplay ingredientDisplayRow;

    private CellData contextCellData; // Used to determine machine type
    private RecipeIngredientDisplay cachedIngredientDisplayRow; // Cache to prevent null reference issues
    
    /// <summary>
    /// Cache the ingredientDisplayRow reference early to prevent Unity serialization issues
    /// </summary>
    private void Awake()
    {
        // Cache the reference as soon as component wakes up
        if (ingredientDisplayRow != null)
        {
            cachedIngredientDisplayRow = ingredientDisplayRow;
            GameLogger.Log(LoggingManager.LogCategory.UI, 
                $"Awake: Cached ingredientDisplayRow reference: {cachedIngredientDisplayRow.name}", ComponentId);
        }
        else
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, 
                "Awake: ingredientDisplayRow is null - check Unity inspector assignment!", ComponentId);
        }
    }
    
    /// <summary>
    /// Ensure cache is valid whenever enabled
    /// </summary>
    private void OnEnable()
    {
        // Double-check cache whenever panel is enabled
        if (cachedIngredientDisplayRow == null && ingredientDisplayRow != null)
        {
            cachedIngredientDisplayRow = ingredientDisplayRow;
            GameLogger.Log(LoggingManager.LogCategory.UI, 
                $"OnEnable: Cached ingredientDisplayRow reference: {cachedIngredientDisplayRow.name}", ComponentId);
        }
        
        // If original is null but cache exists, restore it
        if (ingredientDisplayRow == null && cachedIngredientDisplayRow != null)
        {
            ingredientDisplayRow = cachedIngredientDisplayRow;
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, 
                $"OnEnable: Restored ingredientDisplayRow from cache: {ingredientDisplayRow.name}", ComponentId);
        }
        
        // Log current state
        GameLogger.Log(LoggingManager.LogCategory.UI, 
            $"OnEnable: ingredientDisplayRow={(ingredientDisplayRow != null ? ingredientDisplayRow.name : "NULL")}, cache={(cachedIngredientDisplayRow != null ? cachedIngredientDisplayRow.name : "NULL")}", ComponentId);
    }

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
    
    private int currentButtonIndex = 0; // Track button index across multiple CreateSelectionButton calls
    
    /// <summary>
    /// Override PopulateButtons to reset button index counter before creating buttons
    /// </summary>
    protected override void PopulateButtons()
    {
        GameLogger.Log(LoggingManager.LogCategory.UI, 
            $"PopulateButtons START: machineId='{machineId}', ingredientDisplayRow={(ingredientDisplayRow != null ? ingredientDisplayRow.name : "NULL")}, cache={(cachedIngredientDisplayRow != null ? cachedIngredientDisplayRow.name : "NULL")}", ComponentId);
        
        // Cache ingredientDisplayRow reference if not already cached
        if (cachedIngredientDisplayRow == null && ingredientDisplayRow != null)
        {
            cachedIngredientDisplayRow = ingredientDisplayRow;
            GameLogger.Log(LoggingManager.LogCategory.UI, 
                $"PopulateButtons: Cached ingredientDisplayRow reference: {cachedIngredientDisplayRow.name}", ComponentId);
        }
        
        // If original reference is null but we have cache, restore it
        if (ingredientDisplayRow == null && cachedIngredientDisplayRow != null)
        {
            ingredientDisplayRow = cachedIngredientDisplayRow;
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, 
                $"PopulateButtons: Restored ingredientDisplayRow from cache: {ingredientDisplayRow.name}", ComponentId);
        }
        
        // If BOTH are null, this is a problem
        if (ingredientDisplayRow == null && cachedIngredientDisplayRow == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.UI, 
                "PopulateButtons: Both ingredientDisplayRow and cache are NULL! Check Unity inspector assignment.", ComponentId);
        }
        
        // Reset button index counter before populating
        currentButtonIndex = 0;
        
        GameLogger.Log(LoggingManager.LogCategory.UI, 
            $"PopulateButtons END: About to call base.PopulateButtons()", ComponentId);
        
        // Call base implementation which will call CreateSelectionButton for each item
        base.PopulateButtons();
    }
    
    /// <summary>
    /// Override to create selection buttons with proper prefab based on whether it's the "None" option
    /// For recipes: Create a buttonPrefab as clickable background, then add ingredientDisplayRow on top with raycasts disabled
    /// </summary>
    protected override void CreateSelectionButton(RecipeDef item, string displayName)
    {
        if (buttonContainer == null || buttonPrefab == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.UI, 
                $"Cannot create button - buttonContainer null? {buttonContainer == null}, buttonPrefab null? {buttonPrefab == null}", ComponentId);
            return;
        }

        // Use tracked index instead of childCount (which includes old buttons marked for destruction)
        int buttonIndex = currentButtonIndex++;
        
        // ALWAYS create buttonPrefab as the clickable button background
        GameObject buttonObj = Instantiate(buttonPrefab, buttonContainer);
        buttonObj.name = $"Button_{buttonIndex}_{displayName}";
        
        GameLogger.Log(LoggingManager.LogCategory.UI, 
            $"Created button background '{buttonObj.name}' at index {buttonIndex}", ComponentId);
        
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
        
        if (button != null)
        {
            // Set up button click listener
            button.onClick.AddListener(() => OnItemSelected(item));
            
            GameLogger.Log(LoggingManager.LogCategory.UI, 
                $"Added click listener to button for recipe: {displayName}", ComponentId);
            
            // For recipes (not None option), add ingredientDisplayRow on top
            if (item != null && ingredientDisplayRow != null)
            {
                GameLogger.Log(LoggingManager.LogCategory.UI, 
                    $"Creating RecipeDisplay for '{displayName}' - ingredientDisplayRow: {ingredientDisplayRow?.name ?? "NULL"}", ComponentId);
                
                // Create ingredientDisplayRow as a child of the button (on top, non-blocking)
                GameObject displayObj = Instantiate(ingredientDisplayRow.gameObject, buttonObj.transform);
                displayObj.name = "RecipeDisplay";
                
                // Set the RectTransform FIRST so DisplayRecipe can calculate sizes correctly
                RectTransform displayRect = displayObj.GetComponent<RectTransform>();
                RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
                if (displayRect != null && buttonRect != null)
                {
                    // Set anchors to stretch in both directions
                    displayRect.anchorMin = Vector2.zero;
                    displayRect.anchorMax = Vector2.one;
                    displayRect.pivot = new Vector2(0.5f, 0.5f);
                    
                    // Set sizeDelta to match button size so DisplayRecipe can calculate widths correctly
                    displayRect.sizeDelta = buttonRect.sizeDelta;
                    displayRect.anchoredPosition = Vector2.zero;
                    
                    GameLogger.Log(LoggingManager.LogCategory.UI, 
                        $"RecipeDisplay rect BEFORE DisplayRecipe: sizeDelta={displayRect.sizeDelta}, button sizeDelta={buttonRect.sizeDelta}", ComponentId);
                }
                
                // NOW call DisplayRecipe with correct sizing
                RecipeIngredientDisplay displayComponent = displayObj.GetComponent<RecipeIngredientDisplay>();
                if (displayComponent != null)
                {
                    GameLogger.Log(LoggingManager.LogCategory.UI, 
                        $"Displaying recipe '{displayName}' with {item.inputItems?.Count ?? 0} inputs", ComponentId);
                    displayComponent.DisplayRecipe(item);
                }
                else
                {
                    GameLogger.LogError(LoggingManager.LogCategory.UI, 
                        $"No RecipeIngredientDisplay component found on ingredientDisplayRow!", ComponentId);
                }
                
                // FINALLY reset to anchor-based sizing
                if (displayRect != null)
                {
                    // Force offsets to 0 to fill parent completely
                    displayRect.offsetMin = Vector2.zero;  // Left and Bottom
                    displayRect.offsetMax = Vector2.zero;  // Right and Top
                    displayRect.sizeDelta = Vector2.zero; // Size controlled by anchors
                    
                    GameLogger.Log(LoggingManager.LogCategory.UI, 
                        $"RecipeDisplay rect AFTER reset: offsetMin={displayRect.offsetMin}, offsetMax={displayRect.offsetMax}, sizeDelta={displayRect.sizeDelta}", ComponentId);
                }
                
                // Disable ALL raycasts on the display so clicks pass through to button
                DisableAllRaycastsRecursive(displayObj);
            }
            else
            {
                GameLogger.Log(LoggingManager.LogCategory.UI, 
                    $"Skipping RecipeDisplay - item null? {item == null}, ingredientDisplayRow null? {ingredientDisplayRow == null}", ComponentId);
                
                // None option - just set button text
                SetupButtonVisuals(buttonObj, item, displayName);
            }
        }
        else
        {
            GameLogger.LogError(LoggingManager.LogCategory.UI, 
                $"{GetType().Name}: buttonPrefab missing Button component!", ComponentId);
        }
    }
    
    /// <summary>
    /// Disable ALL raycasts recursively on a GameObject and all its children
    /// This makes the entire hierarchy transparent to clicks
    /// </summary>
    private void DisableAllRaycastsRecursive(GameObject obj)
    {
        if (obj == null) return;
        
        // Disable raycasts on all UI components
        var images = obj.GetComponentsInChildren<UnityEngine.UI.Image>();
        foreach (var img in images)
        {
            img.raycastTarget = false;
        }
        
        var texts = obj.GetComponentsInChildren<UnityEngine.UI.Text>();
        foreach (var txt in texts)
        {
            txt.raycastTarget = false;
        }
        
        var tmps = obj.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
        foreach (var tmp in tmps)
        {
            tmp.raycastTarget = false;
        }
        
        // Also disable any CanvasGroup to be safe
        var canvasGroups = obj.GetComponentsInChildren<CanvasGroup>();
        foreach (var cg in canvasGroups)
        {
            cg.blocksRaycasts = false;
        }
        
        GameLogger.Log(LoggingManager.LogCategory.UI, 
            $"Disabled all raycasts on '{obj.name}': {images.Length} Images, {texts.Length} Texts, {tmps.Length} TMPs, {canvasGroups.Length} CanvasGroups", ComponentId);
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