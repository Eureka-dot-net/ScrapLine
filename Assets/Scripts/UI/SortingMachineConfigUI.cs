using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// UI component for configuring sorting machines with button-based item selection.
/// This component should be attached to a UI panel that contains:
/// - Two buttons for left and right item configuration (showing selected item pictures)
/// - A secondary panel for item selection with item buttons
/// - Confirm and Cancel buttons
/// 
/// UNITY SETUP REQUIRED:
/// 1. Create a main UI Panel GameObject in the scene
/// 2. Add this component to the main panel
/// 3. Add two Button components for leftConfigButton and rightConfigButton
/// 4. Add a secondary Panel for item selection (itemSelectionPanel)
/// 5. Add Button prefab for item buttons (itemButtonPrefab)
/// 6. Add two Button components for confirmButton and cancelButton
/// 7. Assign all UI components in the inspector
/// 8. Set the panels inactive by default (will be activated when configuration is needed)
/// </summary>
public class SortingMachineConfigUI : MonoBehaviour
{
    [Header("Main Configuration UI Components")]
    [Tooltip("Button for configuring item that goes left (shows selected item picture)")]
    public Button leftConfigButton;
    
    [Tooltip("Button for configuring item that goes right (shows selected item picture)")]
    public Button rightConfigButton;
    
    [Tooltip("Button to confirm configuration")]
    public Button confirmButton;
    
    [Tooltip("Button to cancel configuration")]
    public Button cancelButton;
    
    [Header("Item Selection Panel")]
    [Tooltip("Panel that shows when selecting items")]
    public GameObject itemSelectionPanel;
    
    [Tooltip("Container for item selection buttons")]
    public Transform itemButtonContainer;
    
    [Tooltip("Prefab for item buttons")]
    public GameObject itemButtonPrefab;
    
    [Header("Configuration")]
    [Tooltip("Main panel to show/hide for configuration")]
    public GameObject configPanel;

    private CellData currentCellData;
    private System.Action<string, string> onConfigurationConfirmed;
    private bool isSelectingForLeft; // Track whether we're selecting for left or right
    private string selectedLeftItemId = "";
    private string selectedRightItemId = "";

    void Start()
    {
        // Setup button listeners
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);
        
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);

        if (leftConfigButton != null)
            leftConfigButton.onClick.AddListener(() => ShowItemSelection(true));

        if (rightConfigButton != null)
            rightConfigButton.onClick.AddListener(() => ShowItemSelection(false));

        // Hide the configuration panels initially
        if (configPanel != null)
            configPanel.SetActive(false);
        
        if (itemSelectionPanel != null)
            itemSelectionPanel.SetActive(false);
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

        // Load current configuration if any
        if (cellData.sortingConfig != null)
        {
            selectedLeftItemId = cellData.sortingConfig.leftItemType;
            selectedRightItemId = cellData.sortingConfig.rightItemType;
        }
        else
        {
            selectedLeftItemId = "";
            selectedRightItemId = "";
        }

        // Update button visuals with current selection
        UpdateConfigButtonVisuals();

        // Show the main configuration panel
        if (configPanel != null)
            configPanel.SetActive(true);
        else
            gameObject.SetActive(true);

    }

    /// <summary>
    /// Show the item selection panel
    /// </summary>
    /// <param name="forLeft">True if selecting for left, false for right</param>
    private void ShowItemSelection(bool forLeft)
    {
        isSelectingForLeft = forLeft;
        
        // Populate item selection buttons
        PopulateItemSelectionButtons();
        
        // Show the item selection panel
        if (itemSelectionPanel != null)
            itemSelectionPanel.SetActive(true);
        
    }

    /// <summary>
    /// Hide the item selection panel
    /// </summary>
    private void HideItemSelection()
    {
        if (itemSelectionPanel != null)
            itemSelectionPanel.SetActive(false);
    }

    /// <summary>
    /// Populate the item selection panel with item buttons
    /// </summary>
    private void PopulateItemSelectionButtons()
    {
        if (itemButtonContainer == null || itemButtonPrefab == null)
        {
            Debug.LogError("SortingMachineConfigUI: Item button container or prefab not assigned!");
            return;
        }

        // Clear existing buttons
        foreach (Transform child in itemButtonContainer)
        {
            Destroy(child.gameObject);
        }

        // Add "None" option
        CreateItemButton("", "None", null);

        // Get available item types from FactoryRegistry
        if (FactoryRegistry.Instance != null)
        {
            foreach (var item in FactoryRegistry.Instance.Items.Values)
            {
                CreateItemButton(item.id, item.displayName, item.sprite);
            }
        }
        else
        {
            // Fallback if FactoryRegistry is not available
            CreateItemButton("can", "Aluminum Can", "can");
            CreateItemButton("shreddedAluminum", "Shredded Aluminum", "shredded_aluminum");
            CreateItemButton("plastic", "Plastic", "plastic");
        }
    }

    /// <summary>
    /// Create an item selection button
    /// </summary>
    private void CreateItemButton(string itemId, string displayName, string spriteName)
    {
        GameObject buttonObj = Instantiate(itemButtonPrefab, itemButtonContainer);
        Button button = buttonObj.GetComponent<Button>();
        
        if (button != null)
        {
            // Set up button click listener
            button.onClick.AddListener(() => OnItemSelected(itemId));
            
            // Try to find and set the sprite
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage == null)
                buttonImage = button.GetComponentInChildren<Image>();
            
            if (buttonImage != null && !string.IsNullOrEmpty(spriteName))
            {
                Sprite itemSprite = Resources.Load<Sprite>($"Sprites/Items/{spriteName}");
                if (itemSprite != null)
                {
                    buttonImage.sprite = itemSprite;
                }
            }
            
            // Set text if there's a Text component
            Text buttonText = button.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = displayName;
            }
        }
    }

    /// <summary>
    /// Called when an item is selected from the item selection panel
    /// </summary>
    private void OnItemSelected(string itemId)
    {
        if (isSelectingForLeft)
        {
            selectedLeftItemId = itemId;
        }
        else
        {
            selectedRightItemId = itemId;
        }

        // Update the configuration button visuals
        UpdateConfigButtonVisuals();
        
        // Hide the item selection panel
        HideItemSelection();
        
    }

    /// <summary>
    /// Update the left and right configuration buttons to show selected items
    /// </summary>
    private void UpdateConfigButtonVisuals()
    {
        UpdateConfigButtonVisual(leftConfigButton, selectedLeftItemId, "Left");
        UpdateConfigButtonVisual(rightConfigButton, selectedRightItemId, "Right");
    }

    /// <summary>
    /// Update a single configuration button to show the selected item
    /// </summary>
    private void UpdateConfigButtonVisual(Button configButton, string itemId, string defaultText)
    {
        if (configButton == null) return;

        // Get button image and text components
        Image buttonImage = configButton.GetComponent<Image>();
        if (buttonImage == null)
            buttonImage = configButton.GetComponentInChildren<Image>();
        
        Text buttonText = configButton.GetComponentInChildren<Text>();

        if (string.IsNullOrEmpty(itemId))
        {
            // No item selected - show default text
            if (buttonText != null)
                buttonText.text = defaultText;
            
            // Reset to default button appearance
            if (buttonImage != null)
                buttonImage.sprite = null;
        }
        else
        {
            // Item selected - show item info
            ItemDef itemDef = FactoryRegistry.Instance?.GetItem(itemId);
            string displayName = itemDef?.displayName ?? itemId;
            string spriteName = itemDef?.sprite ?? itemId;

            if (buttonText != null)
                buttonText.text = displayName;

            // Load and set the item sprite
            if (buttonImage != null && !string.IsNullOrEmpty(spriteName))
            {
                Sprite itemSprite = Resources.Load<Sprite>($"Sprites/Items/{spriteName}");
                if (itemSprite != null)
                {
                    buttonImage.sprite = itemSprite;
                }
            }
        }
    }

    private void OnConfirmClicked()
    {

        // Update the cell data
        if (currentCellData != null)
        {
            if (currentCellData.sortingConfig == null)
                currentCellData.sortingConfig = new SortingMachineConfig();
            
            currentCellData.sortingConfig.leftItemType = selectedLeftItemId;
            currentCellData.sortingConfig.rightItemType = selectedRightItemId;
        }

        // Call the callback
        onConfigurationConfirmed?.Invoke(selectedLeftItemId, selectedRightItemId);

       

        // Hide the configuration panel
        HideConfiguration();
    }

    private void OnCancelClicked()
    {
        HideConfiguration();
    }

    private void HideConfiguration()
    {
        if (configPanel != null)
            configPanel.SetActive(false);
        else
            gameObject.SetActive(false);

        if (itemSelectionPanel != null)
            itemSelectionPanel.SetActive(false);

        currentCellData = null;
        onConfigurationConfirmed = null;
    }
}