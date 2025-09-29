using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Reusable UI component for displaying recipe ingredients with icons and counts.
/// Supports up to 3 different ingredient types with quantities.
/// 
/// UNITY SETUP:
/// 1. Create a GameObject with HorizontalLayoutGroup
/// 2. Add this component
/// 3. Assign ingredientContainer and ingredientPrefab
/// 4. Ingredient prefab should have Image and Text components
/// </summary>
public class RecipeIngredientDisplay : MonoBehaviour
{
    [Header("Ingredient Display Configuration")]
    [Tooltip("Container with HorizontalLayoutGroup for ingredient items")]
    public Transform ingredientContainer;
    
    [Tooltip("Prefab for each ingredient item (should have Image and Text components)")]
    public GameObject ingredientPrefab;
    
    [Tooltip("Maximum number of individual icons to show for each ingredient type")]
    public int maxIconsPerIngredient = 5;
    
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

        // Display each ingredient type
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

        // Create ingredient display based on count
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
        }

        // Setup text (count or item name)
        Text iconText = ingredientObj.GetComponentInChildren<Text>();
        if (iconText != null)
        {
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

        // Clear text for arrow
        Text arrowText = arrowObj.GetComponentInChildren<Text>();
        if (arrowText != null)
        {
            arrowText.text = "→"; // Unicode arrow as fallback
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