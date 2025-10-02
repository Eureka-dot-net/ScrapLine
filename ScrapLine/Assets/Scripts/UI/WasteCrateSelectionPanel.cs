using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Selection panel for waste crates (used by spawner machines for purchasing).
/// Displays available WasteCrateDef objects from FactoryRegistry.
/// 
/// UNITY SETUP:
/// 1. Create UI Panel with Grid Layout Group
/// 2. Add this component to the panel
/// 3. Assign selectionPanel, buttonContainer, buttonPrefab
/// 4. Button prefab should have Button, Image, and Text components
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
            SetButtonImage(buttonObj, crateSprite);
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
        // Waste crate selection typically doesn't support "None" since you're purchasing
        return false;
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