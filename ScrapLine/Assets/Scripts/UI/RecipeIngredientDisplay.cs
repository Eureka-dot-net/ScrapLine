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
    
    [Tooltip("Arrow color (default: white)")]
    public Color arrowColor = Color.white;

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
            // Horizontal layout (selection panel) - manual positioning
            // Calculate item sizes: parent width / 5 (3 inputs max + arrow + 1 output)
            RectTransform containerRect = ingredientContainer as RectTransform;
            if (containerRect == null)
            {
                GameLogger.LogError(LoggingManager.LogCategory.UI, "ingredientContainer is not a RectTransform", ComponentId);
                return;
            }
            
            float parentWidth = containerRect.rect.width;
            float itemSize = parentWidth / 5f;
            
            // Set container height to match item size (square items)
            containerRect.sizeDelta = new Vector2(containerRect.sizeDelta.x, itemSize);
            
            int itemIndex = 0;
            
            // Display input ingredients (always at indices 0, 1, 2)
            foreach (var ingredient in recipe.inputItems)
            {
                CreateIngredientWithManualLayout(ingredient, itemIndex, itemSize);
                itemIndex++;
            }
            
            // Add arrow at index 3 (right-aligned, always at same position)
            if (showArrow && arrowSprite != null)
            {
                CreateArrowWithManualLayout(3, itemSize);
            }
            
            // Display output items at index 4 (right-aligned, always at same position)
            if (recipe.outputItems != null && recipe.outputItems.Count > 0)
            {
                foreach (var output in recipe.outputItems)
                {
                    CreateIngredientWithManualLayout(output, 4, itemSize);
                }
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
            // Horizontal layout - always show count text (never multiple icons)
            CreateIngredientIcon(itemSprite, itemDef.displayName, ingredient.count);
        }
    }

    /// <summary>
    /// Create an ingredient item with manual positioning (no layout groups)
    /// </summary>
    private void CreateIngredientWithManualLayout(RecipeItemDef ingredient, int index, float itemSize)
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

        // Always show count text (e.g., "1x", "2x", etc.)
        GameObject ingredientObj = Instantiate(ingredientPrefab, ingredientContainer);
        
        // Set size and position manually
        RectTransform rectTransform = ingredientObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // Set anchors to top-left
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            
            // Set size
            rectTransform.sizeDelta = new Vector2(itemSize, itemSize);
            
            // Set position (index * itemSize from left, 0 from top)
            rectTransform.anchoredPosition = new Vector2(index * itemSize, 0);
        }
        
        // Find and set icon image
        Transform itemIconTransform = ingredientObj.transform.Find("ItemIcon");
        Image iconImage = itemIconTransform != null ? itemIconTransform.GetComponent<Image>() : ingredientObj.GetComponentInChildren<Image>();
        
        if (iconImage != null && itemSprite != null)
        {
            iconImage.sprite = itemSprite;
            iconImage.color = Color.white;
            
            // Set icon size
            RectTransform iconRect = iconImage.GetComponent<RectTransform>();
            if (iconRect != null)
            {
                iconRect.sizeDelta = iconSize;
            }
        }
        
        // Find and set count text
        Transform countTextTransform = ingredientObj.transform.Find("CountText");
        TextMeshProUGUI countText = countTextTransform != null ? countTextTransform.GetComponent<TextMeshProUGUI>() : ingredientObj.GetComponentInChildren<TextMeshProUGUI>();
        
        if (countText != null)
        {
            countText.fontSize = fontSize;
            countText.text = $"{ingredient.count} x";
            
            // Disable text wrapping to keep "1 x" on one line
            countText.enableWordWrapping = false;
            countText.overflowMode = TextOverflowModes.Overflow;
        }
        
        GameLogger.Log(LoggingManager.LogCategory.UI, $"Created ingredient at index {index}: {ingredient.count}x {itemDef.displayName ?? ingredient.item}", ComponentId);
    }

    /// <summary>
    /// Create an arrow with manual positioning (no layout groups)
    /// </summary>
    private void CreateArrowWithManualLayout(int index, float itemSize)
    {
        if (arrowSprite == null || ingredientPrefab == null)
            return;

        GameObject arrowObj = Instantiate(ingredientPrefab, ingredientContainer);
        
        // Set size and position manually
        RectTransform rectTransform = arrowObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // Set anchors to top-left
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            
            // Set size
            rectTransform.sizeDelta = new Vector2(itemSize, itemSize);
            
            // Set position
            rectTransform.anchoredPosition = new Vector2(index * itemSize, 0);
        }
        
        // Set arrow image with specified color
        Image arrowImage = arrowObj.GetComponent<Image>();
        if (arrowImage == null)
            arrowImage = arrowObj.GetComponentInChildren<Image>();
        
        if (arrowImage != null)
        {
            arrowImage.sprite = arrowSprite;
            arrowImage.color = arrowColor;  // Use the specified arrow color
        }
        
        // Hide text for arrow
        TextMeshProUGUI text = arrowObj.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = "";
        }
        
        GameLogger.Log(LoggingManager.LogCategory.UI, $"Created arrow at index {index}", ComponentId);
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
        
        // Find ItemIcon by name (common prefab naming convention)
        Transform itemIconTransform = ingredientObj.transform.Find("ItemIcon");
        Image iconImage = null;
        
        if (itemIconTransform != null)
        {
            iconImage = itemIconTransform.GetComponent<Image>();
        }
        else
        {
            // Fallback: search all Images in children
            iconImage = ingredientObj.GetComponentInChildren<Image>();
        }
        
        if (iconImage != null)
        {
            if (sprite != null)
            {
                iconImage.sprite = sprite;
                iconImage.color = Color.white;
                GameLogger.Log(LoggingManager.LogCategory.UI, $"Set icon sprite for {itemName}", ComponentId);
            }
            else
            {
                GameLogger.LogWarning(LoggingManager.LogCategory.UI, $"No sprite found for {itemName}", ComponentId);
            }
            
            // Set icon size for consistent layout
            RectTransform iconRect = iconImage.GetComponent<RectTransform>();
            if (iconRect != null)
            {
                iconRect.sizeDelta = iconSize;
            }
        }
        else
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, 
                "No Image component found in ingredient prefab. Check prefab structure.", ComponentId);
        }

        // Find CountText by name (common prefab naming convention)
        Transform countTextTransform = ingredientObj.transform.Find("CountText");
        TextMeshProUGUI iconText = null;
        
        if (countTextTransform != null)
        {
            iconText = countTextTransform.GetComponent<TextMeshProUGUI>();
            if (iconText == null)
                iconText = countTextTransform.GetComponentInChildren<TextMeshProUGUI>();
        }
        else
        {
            // Fallback: search all TextMeshPro components
            iconText = ingredientObj.GetComponentInChildren<TextMeshProUGUI>();
        }
        
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
                // Horizontal layout - show count with space to prevent wrapping
                iconText.text = $"{count} x";
                
                // Disable text wrapping to keep "1 x" on one line
                iconText.enableWordWrapping = false;
                iconText.overflowMode = TextOverflowModes.Overflow;
            }
        }

        // Log for debugging
        string tooltipText = count > 1 ? $"{count}x {itemName}" : itemName;
        GameLogger.Log(LoggingManager.LogCategory.UI, $"Created ingredient icon: {tooltipText}", ComponentId);
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