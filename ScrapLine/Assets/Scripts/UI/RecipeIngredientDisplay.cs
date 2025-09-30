using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Reusable UI component for displaying recipe ingredients with icons and counts.
/// Supports up to 3 different ingredient types with quantities.
/// 
/// UNITY SETUP:
/// 1. Create a GameObject with VerticalLayoutGroup (for config panel) or HorizontalLayoutGroup (for selection panel)
/// 2. Add this component
/// 3. Assign ingredientContainer and ingredientPrefab
/// 4. Ingredient prefab should have Image and TextMeshProUGUI components
/// 5. Configure Content Size Fitter for responsive layout
/// </summary>
public class RecipeIngredientDisplay : MonoBehaviour
{
    [Header("Ingredient Display Configuration")]
    [Tooltip("Container with VerticalLayoutGroup or HorizontalLayoutGroup for ingredient items")]
    public Transform ingredientContainer;
    
    [Tooltip("Prefab for each ingredient item (should have Image and TextMeshProUGUI components)")]
    public GameObject ingredientPrefab;
    
    [Tooltip("Prefab for spacer (blank GameObject with fixed height)")]
    public GameObject spacerPrefab;
    
    [Tooltip("Maximum number of individual icons to show for each ingredient type")]
    public int maxIconsPerIngredient = 5;
    
    [Tooltip("Size for ingredient icons (default 32x32 for mobile touch)")]
    public Vector2 iconSize = new Vector2(32, 32);
    
    [Tooltip("Font size for count text")]
    public float fontSize = 12f;
    
    [Tooltip("Use vertical layout (for config panel) vs horizontal layout (for selection panel)")]
    public bool useVerticalLayout = false;
    
    [Tooltip("Show arrow between ingredients and output")]
    public bool showArrow = false;
    
    [Tooltip("Arrow sprite (optional)")]
    public Sprite arrowSprite;

    private string ComponentId => $"RecipeIngredientDisplay_{GetInstanceID()}";

    /// <summary>
    /// Display ingredients for a given recipe
    /// </summary>
    /// <param name="recipe">Recipe to display ingredients for</param>
    public void DisplayRecipe(RecipeDef recipe)
    {
        // Clear existing ingredients
        ClearIngredients();
        
        if (recipe?.inputItems == null || recipe.inputItems.Count == 0)
        {
            GameLogger.Log(LoggingManager.LogCategory.UI, "Recipe has no input items to display", ComponentId);
            return;
        }

        GameLogger.Log(LoggingManager.LogCategory.UI, $"Displaying recipe with {recipe.inputItems.Count} ingredient types", ComponentId);

        // For vertical layout (config panel), add spacer logic
        if (useVerticalLayout)
        {
            // Add top spacer if only one ingredient type
            if (recipe.inputItems.Count == 1)
            {
                CreateSpacer();
            }
            
            // Display each ingredient type
            foreach (var ingredient in recipe.inputItems)
            {
                DisplayIngredient(ingredient);
            }
            
            // Add bottom spacer
            CreateSpacer();
        }
        else
        {
            // Horizontal layout (selection panel) - original behavior
            foreach (var ingredient in recipe.inputItems)
            {
                DisplayIngredient(ingredient);
            }
            
            // Add arrow if requested
            if (showArrow && arrowSprite != null)
            {
                DisplayArrow();
            }
        }
    }

    /// <summary>
    /// Display a single ingredient with count
    /// </summary>
    /// <param name="ingredient">Ingredient to display</param>
    private void DisplayIngredient(RecipeItemDef ingredient)
    {
        if (ingredient == null || ingredientContainer == null || ingredientPrefab == null)
            return;

        ItemDef itemDef = FactoryRegistry.Instance?.GetItem(ingredient.item);
        if (itemDef == null)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, $"Item definition not found for '{ingredient.item}'", ComponentId);
            return;
        }

        // Load item sprite
        Sprite itemSprite = null;
        if (!string.IsNullOrEmpty(itemDef.sprite))
        {
            itemSprite = Resources.Load<Sprite>($"Sprites/Items/{itemDef.sprite}");
        }

        // For vertical layout, always show count + single icon
        if (useVerticalLayout)
        {
            CreateIngredientIcon(itemSprite, itemDef.displayName, ingredient.count);
        }
        else
        {
            // Horizontal layout - original behavior with multiple icons
            if (ingredient.count <= maxIconsPerIngredient)
            {
                // Show individual icons for each item
                for (int i = 0; i < ingredient.count; i++)
                {
                    CreateIngredientIcon(itemSprite, itemDef.displayName, 1);
                }
            }
            else
            {
                // Show single icon with count text
                CreateIngredientIcon(itemSprite, itemDef.displayName, ingredient.count);
            }
        }
    }

    /// <summary>
    /// Create a single ingredient icon with optional count
    /// </summary>
    /// <param name="sprite">Item sprite</param>
    /// <param name="itemName">Item display name</param>
    /// <param name="count">Count to display (1 = no count text)</param>
    private void CreateIngredientIcon(Sprite sprite, string itemName, int count)
    {
        GameObject ingredientObj = Instantiate(ingredientPrefab, ingredientContainer);
        
        // Setup image
        Image iconImage = ingredientObj.GetComponent<Image>();
        if (iconImage == null)
            iconImage = ingredientObj.GetComponentInChildren<Image>();
        
        if (iconImage != null && sprite != null)
        {
            iconImage.sprite = sprite;
            iconImage.color = Color.white;
            
            // Set icon size for consistent layout
            RectTransform iconRect = iconImage.GetComponent<RectTransform>();
            if (iconRect != null)
            {
                iconRect.sizeDelta = iconSize;
            }
        }

        // Setup text (count or item name) - Use TextMeshPro
        TextMeshProUGUI iconText = ingredientObj.GetComponentInChildren<TextMeshProUGUI>();
        if (iconText != null)
        {
            iconText.fontSize = fontSize;
            
            // For vertical layout, always show count
            if (useVerticalLayout)
            {
                iconText.text = $"{count}  x";
            }
            else
            {
                // Horizontal layout - original behavior
                if (count > 1)
                {
                    iconText.text = $"{count}x";
                }
                else
                {
                    // For single items, we might not want to show text or show item name
                    iconText.text = ""; // Clean look with just icons
                }
            }
        }

        // Setup tooltip or hover text if needed
        var button = ingredientObj.GetComponent<Button>();
        if (button != null)
        {
            // Could add hover functionality here
            string tooltipText = count > 1 ? $"{count}x {itemName}" : itemName;
            GameLogger.Log(LoggingManager.LogCategory.UI, $"Created ingredient icon: {tooltipText}", ComponentId);
        }
    }

    /// <summary>
    /// Create a spacer for vertical layout
    /// </summary>
    private void CreateSpacer()
    {
        if (ingredientContainer == null) return;
        
        GameObject spacer;
        if (spacerPrefab != null)
        {
            spacer = Instantiate(spacerPrefab, ingredientContainer);
        }
        else
        {
            // Create a simple spacer GameObject if no prefab provided
            spacer = new GameObject("Spacer");
            spacer.transform.SetParent(ingredientContainer);
            
            RectTransform rectTransform = spacer.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0, 10); // 10 pixel height spacer
            
            // Optional: Add LayoutElement for better control
            LayoutElement layoutElement = spacer.AddComponent<LayoutElement>();
            layoutElement.minHeight = 10;
            layoutElement.preferredHeight = 10;
        }
        
        GameLogger.Log(LoggingManager.LogCategory.UI, "Created spacer", ComponentId);
    }

    /// <summary>
    /// Display an arrow indicating recipe flow
    /// </summary>
    private void DisplayArrow()
    {
        if (arrowSprite == null || ingredientPrefab == null) return;

        GameObject arrowObj = Instantiate(ingredientPrefab, ingredientContainer);
        
        Image arrowImage = arrowObj.GetComponent<Image>();
        if (arrowImage == null)
            arrowImage = arrowObj.GetComponentInChildren<Image>();
        
        if (arrowImage != null)
        {
            arrowImage.sprite = arrowSprite;
            arrowImage.color = Color.white;
        }

        // Clear text for arrow - Use TextMeshPro
        TextMeshProUGUI arrowText = arrowObj.GetComponentInChildren<TextMeshProUGUI>();
        if (arrowText != null)
        {
            arrowText.text = "→"; // Unicode arrow as fallback
            arrowText.fontSize = fontSize;
        }
    }

    /// <summary>
    /// Clear all displayed ingredients
    /// </summary>
    public void ClearIngredients()
    {
        if (ingredientContainer == null) return;

        // Destroy all child objects
        for (int i = ingredientContainer.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
            {
                Destroy(ingredientContainer.GetChild(i).gameObject);
            }
            else
            {
                DestroyImmediate(ingredientContainer.GetChild(i).gameObject);
            }
        }

        GameLogger.Log(LoggingManager.LogCategory.UI, "Cleared all ingredient displays", ComponentId);
    }

    /// <summary>
    /// Quick method to display ingredients as a formatted string (for debugging)
    /// </summary>
    /// <param name="recipe">Recipe to get ingredient string for</param>
    /// <returns>Formatted ingredient string</returns>
    public static string GetIngredientsString(RecipeDef recipe)
    {
        if (recipe?.inputItems == null || recipe.inputItems.Count == 0)
            return "No ingredients";

        var parts = new List<string>();
        foreach (var ingredient in recipe.inputItems)
        {
            ItemDef itemDef = FactoryRegistry.Instance?.GetItem(ingredient.item);
            string itemName = itemDef?.displayName ?? ingredient.item;
            parts.Add($"{ingredient.count}x {itemName}");
        }

        return string.Join(" + ", parts);
    }
}