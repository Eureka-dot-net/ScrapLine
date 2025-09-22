using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// UI component for configuring sorting machines.
/// This component should be attached to a UI panel that contains:
/// - Two dropdowns for left and right item selection
/// - Confirm and Cancel buttons
/// 
/// UNITY SETUP REQUIRED:
/// 1. Create a UI Panel GameObject in the scene
/// 2. Add this component to the panel
/// 3. Add two Dropdown components for leftItemDropdown and rightItemDropdown
/// 4. Add two Button components for confirmButton and cancelButton
/// 5. Assign all UI components in the inspector
/// 6. Set the panel inactive by default (will be activated when configuration is needed)
/// </summary>
public class SortingMachineConfigUI : MonoBehaviour
{
    [Header("UI Components - MUST BE ASSIGNED IN INSPECTOR")]
    [Tooltip("Dropdown for selecting item that goes left")]
    public Dropdown leftItemDropdown;
    
    [Tooltip("Dropdown for selecting item that goes right")]
    public Dropdown rightItemDropdown;
    
    [Tooltip("Button to confirm configuration")]
    public Button confirmButton;
    
    [Tooltip("Button to cancel configuration")]
    public Button cancelButton;
    
    [Header("Configuration")]
    [Tooltip("Panel to show/hide for configuration")]
    public GameObject configPanel;

    private CellData currentCellData;
    private System.Action<string, string> onConfigurationConfirmed;

    void Start()
    {
        // Setup button listeners
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);
        
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);

        // Hide the configuration panel initially
        if (configPanel != null)
            configPanel.SetActive(false);
    }

    /// <summary>
    /// Show the configuration UI for a sorting machine
    /// </summary>
    /// <param name="cellData">The cell data of the sorting machine to configure</param>
    /// <param name="onConfirmed">Callback when configuration is confirmed</param>
    public void ShowConfiguration(CellData cellData, System.Action<string, string> onConfirmed)
    {
        currentCellData = cellData;
        onConfigurationConfirmed = onConfirmed;

        // Populate dropdowns with available item types
        PopulateItemDropdowns();

        // Set current values if any
        if (cellData.sortingConfig != null)
        {
            SetDropdownValue(leftItemDropdown, cellData.sortingConfig.leftItemType);
            SetDropdownValue(rightItemDropdown, cellData.sortingConfig.rightItemType);
        }

        // Show the configuration panel
        if (configPanel != null)
            configPanel.SetActive(true);
        else
            gameObject.SetActive(true);

        Debug.Log($"Showing sorting configuration UI for machine at ({cellData.x}, {cellData.y})");
    }

    /// <summary>
    /// Populate dropdown with available item types from the FactoryRegistry
    /// </summary>
    private void PopulateItemDropdowns()
    {
        if (leftItemDropdown == null || rightItemDropdown == null)
        {
            Debug.LogError("SortingMachineConfigUI: Dropdowns not assigned in inspector!");
            return;
        }

        List<string> itemOptions = new List<string>();
        itemOptions.Add("None"); // Default option

        // Get available item types from FactoryRegistry
        if (FactoryRegistry.Instance != null)
        {
            foreach (var item in FactoryRegistry.Instance.Items.Values)
            {
                itemOptions.Add(item.displayName ?? item.id);
            }
        }
        else
        {
            // Fallback if FactoryRegistry is not available
            itemOptions.Add("can");
            itemOptions.Add("shreddedAluminum");
            itemOptions.Add("plastic");
        }

        // Populate both dropdowns
        leftItemDropdown.ClearOptions();
        leftItemDropdown.AddOptions(itemOptions);

        rightItemDropdown.ClearOptions();
        rightItemDropdown.AddOptions(itemOptions);
    }

    /// <summary>
    /// Set dropdown value by item type string
    /// </summary>
    private void SetDropdownValue(Dropdown dropdown, string itemType)
    {
        if (dropdown == null || string.IsNullOrEmpty(itemType)) 
            return;

        for (int i = 0; i < dropdown.options.Count; i++)
        {
            if (dropdown.options[i].text == itemType || 
                (FactoryRegistry.Instance != null && 
                 FactoryRegistry.Instance.Items.ContainsKey(itemType) &&
                 dropdown.options[i].text == FactoryRegistry.Instance.Items[itemType].displayName))
            {
                dropdown.value = i;
                return;
            }
        }
    }

    /// <summary>
    /// Get item type ID from dropdown selection
    /// </summary>
    private string GetItemTypeFromDropdown(Dropdown dropdown)
    {
        if (dropdown == null || dropdown.value == 0) // 0 is "None"
            return "";

        string selectedText = dropdown.options[dropdown.value].text;
        
        // Try to find the item ID by display name
        if (FactoryRegistry.Instance != null)
        {
            foreach (var kvp in FactoryRegistry.Instance.Items)
            {
                if (kvp.Value.displayName == selectedText || kvp.Value.id == selectedText)
                {
                    return kvp.Value.id;
                }
            }
        }

        // Fallback - return the text as-is
        return selectedText.ToLower();
    }

    private void OnConfirmClicked()
    {
        string leftItemType = GetItemTypeFromDropdown(leftItemDropdown);
        string rightItemType = GetItemTypeFromDropdown(rightItemDropdown);

        Debug.Log($"Sorting configuration confirmed: Left={leftItemType}, Right={rightItemType}");

        // Update the cell data
        if (currentCellData != null && currentCellData.sortingConfig != null)
        {
            currentCellData.sortingConfig.leftItemType = leftItemType;
            currentCellData.sortingConfig.rightItemType = rightItemType;
        }

        // Call the callback
        onConfigurationConfirmed?.Invoke(leftItemType, rightItemType);

        // Hide the configuration panel
        HideConfiguration();
    }

    private void OnCancelClicked()
    {
        Debug.Log("Sorting configuration cancelled");
        HideConfiguration();
    }

    private void HideConfiguration()
    {
        if (configPanel != null)
            configPanel.SetActive(false);
        else
            gameObject.SetActive(false);

        currentCellData = null;
        onConfigurationConfirmed = null;
    }
}