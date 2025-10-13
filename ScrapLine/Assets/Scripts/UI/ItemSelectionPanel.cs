using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Selection panel for items (used by sorting machines and other item-based configs).
/// Displays all available ItemDef objects from FactoryRegistry.
/// 
/// UNITY SETUP:
/// 1. Create UI Panel WITHOUT Grid Layout Group (uses manual row positioning)
/// 2. Add this component to the panel
/// 3. Assign selectionPanel, buttonContainer, buttonPrefab
/// 4. Button prefab should have Button, Image, and Text components
/// </summary>
public class ItemSelectionPanel : BaseSelectionPanel<ItemDef>
{
    [Header("Item Selection Specific")]
    [Tooltip("Filter items by type (leave empty for all items)")]
    public List<string> itemTypeFilter = new List<string>();

    /// <summary>
    /// Override to use manual row-based positioning (5-6 items per row)
    /// This ensures ItemSelectionPanel is not affected by WasteCrateSelectionPanel's Grid Layout approach
    /// </summary>
    protected override void CreateSelectionButton(ItemDef item, string displayName)
    {
        // Use the base class implementation which does manual row positioning
        // This gives us the 5-6 items per row layout that works well for items
        base.CreateSelectionButton(item, displayName);
    }

    protected override List<ItemDef> GetAvailableItems()
    {
        var items = new List<ItemDef>();
        
        if (FactoryRegistry.Instance?.Items == null)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, "FactoryRegistry not available - using fallback items", ComponentId);
            
            // Fallback items if registry not available
            items.Add(new ItemDef { id = "can", displayName = "Aluminum Can", sprite = "can" });
            items.Add(new ItemDef { id = "shreddedAluminum", displayName = "Shredded Aluminum", sprite = "shredded_aluminum" });
            items.Add(new ItemDef { id = "plastic", displayName = "Plastic", sprite = "plastic" });
            
            return items;
        }

        // Get all items from registry
        foreach (var item in FactoryRegistry.Instance.Items.Values)
        {
            // Apply filter if specified
            if (itemTypeFilter.Count > 0 && !itemTypeFilter.Contains(item.id))
                continue;
                
            items.Add(item);
        }

        return items;
    }

    protected override string GetDisplayName(ItemDef item)
    {
        return item?.displayName ?? "Unknown Item";
    }

    protected override void SetupButtonVisuals(GameObject buttonObj, ItemDef item, string displayName)
    {
        // Set button text
        SetButtonText(buttonObj, displayName);

        // Set button sprite if item has one
        if (item != null && !string.IsNullOrEmpty(item.sprite))
        {
            string spritePath = $"Sprites/Items/{item.sprite}";
            Sprite itemSprite = LoadSprite(spritePath);
            SetButtonImage(buttonObj, itemSprite);
        }
    }

    protected override string GetNoneDisplayName()
    {
        return "No Item";
    }
}