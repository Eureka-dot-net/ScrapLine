using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Selection panel for waste crates (used by spawner machines for purchasing).
/// Displays available WasteCrateDef objects from FactoryRegistry.
/// 
/// UNITY SETUP:
/// 1. Create UI Panel with Grid Layout Group
///    - The Grid Layout will be configured at runtime for responsive sizing
///    - No manual cell size configuration needed
/// 2. Add this component to the panel
/// 3. Assign selectionPanel, buttonContainer, buttonPrefab
/// 4. Button prefab should have Button and Image components
/// 5. Image component should be a child of the button for proper sizing
/// 
/// NOTE: This panel uses responsive sizing - sprites are sized based on container width
/// to show 3 items per row with consistent dimensions.
/// </summary>
public class WasteCrateSelectionPanel : BaseSelectionPanel<WasteCrateDef>
{
    [Header("Waste Crate Selection Specific")]
    [Tooltip("Show cost in button text")]
    public bool showCostInText = true;

    /// <summary>
    /// Event fired when a crate is selected (for spawner configuration)
    /// </summary>
    public System.Action<string> OnCrateSelected;

    /// <summary>
    /// Override to configure Grid Layout Group for responsive 3-column layout
    /// </summary>
    protected override void PopulateButtons()
    {
        // Configure Grid Layout Group for this panel specifically
        ConfigureGridLayoutForWasteCrates();
        
        // Call base implementation to create buttons
        base.PopulateButtons();
    }
    
    /// <summary>
    /// Configure Grid Layout Group for responsive 3-column waste crate display
    /// </summary>
    private void ConfigureGridLayoutForWasteCrates()
    {
        if (buttonContainer == null) return;
        
        var gridLayout = buttonContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            // Get container width for responsive sizing
            RectTransform containerRect = buttonContainer as RectTransform;
            float containerWidth = containerRect != null ? containerRect.rect.width : 500f;
            
            // Calculate cell size for 3 columns with spacing
            float spacingX = gridLayout.spacing.x > 0 ? gridLayout.spacing.x : 10f;
            float spacingY = gridLayout.spacing.y > 0 ? gridLayout.spacing.y : 10f;
            float totalSpacing = spacingX * 2; // 2 gaps for 3 columns
            float cellSize = (containerWidth - totalSpacing) / 3f;
            
            // Set Grid Layout to fixed 3 columns with square cells
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3;
            // Ensure cells are perfectly square by setting both width and height to the same value
            gridLayout.cellSize = new Vector2(cellSize, cellSize);
            // Also ensure spacing is applied correctly
            gridLayout.spacing = new Vector2(spacingX, spacingY);
            
            GameLogger.Log(LoggingManager.LogCategory.UI, 
                $"Configured Grid Layout: containerWidth={containerWidth}, cellSize={cellSize}x{cellSize}, spacing={spacingX}x{spacingY}", ComponentId);
        }
        else
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, 
                "Button container does not have GridLayoutGroup component!", ComponentId);
        }
    }

    /// <summary>
    /// Show crate selection for spawner configuration (different from purchase)
    /// </summary>
    /// <param name="availableCrates">List of crates to show</param>
    /// <param name="currentSelection">Currently selected crate ID</param>
    public void ShowCrateSelection(List<WasteCrateDef> availableCrates, string currentSelection)
    {
        ShowPanel((selectedCrate) => 
        {
            if (selectedCrate != null)
            {
                OnCrateSelected?.Invoke(selectedCrate.id);
                HidePanel();
            }
        });
    }

    protected override List<WasteCrateDef> GetAvailableItems()
    {
        var crates = new List<WasteCrateDef>();
        
        if (FactoryRegistry.Instance == null)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, "FactoryRegistry not available", ComponentId);
            return crates;
        }

        // Get all waste crates from registry
        var allCrates = FactoryRegistry.Instance.GetAllWasteCrates();
        if (allCrates != null)
        {
            crates.AddRange(allCrates);
        }

        return crates;
    }

    protected override string GetDisplayName(WasteCrateDef crate)
    {
        if (crate == null) return "Unknown Crate";
        
        string displayName = crate.displayName ?? crate.id ?? "Crate";
        
        if (showCostInText)
        {
            int cost = GetCrateCost(crate);
            displayName += $"\n{cost} credits";
        }
        
        return displayName;
    }

    protected override void SetupButtonVisuals(GameObject buttonObj, WasteCrateDef crate, string displayName)
    {
        // Set button text
        SetButtonText(buttonObj, displayName);

        // Set button sprite if crate has one
        if (crate != null && !string.IsNullOrEmpty(crate.sprite))
        {
            string spritePath = $"Sprites/Waste/{crate.sprite}";
            Sprite crateSprite = LoadSprite(spritePath);
            
            if (crateSprite != null)
            {
                // Find the Image component in the button
                var buttonImage = buttonObj.GetComponent<Image>();
                if (buttonImage == null)
                    buttonImage = buttonObj.GetComponentInChildren<Image>();
                
                if (buttonImage != null)
                {
                    buttonImage.sprite = crateSprite;
                    buttonImage.color = Color.white;
                    
                    // Enable preserve aspect to maintain sprite proportions
                    buttonImage.preserveAspect = true;
                    
                    // Set the Image to fill the button while preserving aspect
                    buttonImage.type = Image.Type.Simple;
                    
                    GameLogger.Log(LoggingManager.LogCategory.UI, 
                        $"Set sprite for crate {crate.id} with preserveAspect=true", ComponentId);
                }
            }
        }

        // Disable button if player can't afford it
        if (buttonObj != null && crate != null)
        {
            var button = buttonObj.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                int cost = GetCrateCost(crate);
                bool canAfford = GameManager.Instance != null && GameManager.Instance.CanAfford(cost);
                button.interactable = canAfford;
                
                if (!canAfford)
                {
                    // Dim the button if can't afford
                    var colors = button.colors;
                    colors.normalColor = colors.disabledColor;
                    button.colors = colors;
                }
            }
        }
    }

    protected override bool SupportsNoneOption()
    {
        // Support "None" option to allow clearing the selection
        // This is needed for spawner configuration to remove crate type filter
        return true;
    }
    
    protected override string GetNoneDisplayName()
    {
        return "No Filter";
    }

    /// <summary>
    /// Calculate the cost of a waste crate
    /// </summary>
    /// <param name="crate">Crate to calculate cost for</param>
    /// <returns>Cost in credits</returns>
    private int GetCrateCost(WasteCrateDef crate)
    {
        if (crate == null) return 0;
        
        // Use the crate's defined cost, or calculate from SpawnerMachine if available
        if (crate.cost > 0)
        {
            return crate.cost;
        }
        
        // Fallback calculation (if SpawnerMachine.CalculateWasteCrateCost exists)
        try
        {
            var spawnerType = System.Type.GetType("SpawnerMachine");
            if (spawnerType != null)
            {
                var method = spawnerType.GetMethod("CalculateWasteCrateCost", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                if (method != null)
                {
                    var result = method.Invoke(null, new object[] { crate });
                    if (result is int cost)
                    {
                        return cost;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, $"Could not calculate crate cost: {e.Message}", ComponentId);
        }
        
        // Final fallback - simple calculation based on item count
        int totalItems = 0;
        if (crate.items != null)
        {
            foreach (var item in crate.items)
            {
                totalItems += item.count;
            }
        }
        
        return Mathf.Max(1, totalItems * 10); // 10 credits per item as fallback
    }
}