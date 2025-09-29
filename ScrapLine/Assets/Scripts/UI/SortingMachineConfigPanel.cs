using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Configuration panel for sorting machines using Plan A base classes.
/// Reduces original 334 lines to ~80 lines by leveraging BaseConfigPanel.
/// 
/// Handles left/right item selection for sorting machine configuration.
/// 
/// UNITY SETUP REQUIRED:
/// 1. Create main UI Panel GameObject with this component
/// 2. Assign configPanel, confirmButton, cancelButton (from BaseConfigPanel)
/// 3. Assign leftConfigButton, rightConfigButton  
/// 4. Create ItemSelectionPanel and assign itemSelectionPanel reference
/// 5. Set panels inactive by default (activated when configuration needed)
/// </summary>
public class SortingMachineConfigPanel : BaseConfigPanel<CellData, Tuple<string, string>>
{
    [Header("Sorting Machine Specific")]
    [Tooltip("Button for configuring item that goes left (shows selected item picture)")]
    public Button leftConfigButton;
    
    [Tooltip("Button for configuring item that goes right (shows selected item picture)")]
    public Button rightConfigButton;
    
    [Tooltip("Item selection panel component")]
    public ItemSelectionPanel itemSelectionPanel;

    // Selection state
    private string selectedLeftItemId = "";
    private string selectedRightItemId = "";
    private bool isSelectingForLeft = false;

    protected override void SetupCustomButtonListeners()
    {
        if (leftConfigButton != null)
            leftConfigButton.onClick.AddListener(() => ShowItemSelection(true));
        
        if (rightConfigButton != null)
            rightConfigButton.onClick.AddListener(() => ShowItemSelection(false));
    }

    protected override void LoadCurrentConfiguration()
    {
        if (currentData?.sortingConfig != null)
        {
            selectedLeftItemId = currentData.sortingConfig.leftItemType ?? "";
            selectedRightItemId = currentData.sortingConfig.rightItemType ?? "";
        }
        else
        {
            selectedLeftItemId = selectedRightItemId = "";
        }
        
        GameLogger.Log(LoggingManager.LogCategory.UI, $"Loaded sorting config: Left='{selectedLeftItemId}', Right='{selectedRightItemId}'", ComponentId);
    }

    protected override void UpdateUIFromCurrentState()
    {
        UpdateConfigButtonVisual(leftConfigButton, selectedLeftItemId, "Left");
        UpdateConfigButtonVisual(rightConfigButton, selectedRightItemId, "Right");
    }

    protected override Tuple<string, string> GetCurrentSelection()
    {
        return new Tuple<string, string>(selectedLeftItemId, selectedRightItemId);
    }

    protected override void UpdateDataWithSelection(Tuple<string, string> selection)
    {
        if (currentData != null)
        {
            if (currentData.sortingConfig == null)
                currentData.sortingConfig = new SortingMachineConfig();
            
            currentData.sortingConfig.leftItemType = selection.Item1;
            currentData.sortingConfig.rightItemType = selection.Item2;
        }
    }

    protected override void HideSelectionPanels()
    {
        if (itemSelectionPanel != null)
            itemSelectionPanel.HidePanel();
    }

    /// <summary>
    /// Show item selection for left or right button
    /// </summary>
    /// <param name="forLeft">True if selecting for left button, false for right</param>
    private void ShowItemSelection(bool forLeft)
    {
        isSelectingForLeft = forLeft;
        
        if (itemSelectionPanel != null)
        {
            itemSelectionPanel.ShowPanel(OnItemSelected);
        }
        else
        {
            GameLogger.LogError(LoggingManager.LogCategory.UI, "ItemSelectionPanel not assigned!", ComponentId);
        }
    }

    /// <summary>
    /// Handle item selection from the selection panel
    /// </summary>
    /// <param name="item">Selected item (null for "None")</param>
    private void OnItemSelected(ItemDef item)
    {
        string itemId = item?.id ?? "";
        
        if (isSelectingForLeft)
        {
            selectedLeftItemId = itemId;
            GameLogger.Log(LoggingManager.LogCategory.UI, $"Selected left item: '{itemId}'", ComponentId);
        }
        else
        {
            selectedRightItemId = itemId;
            GameLogger.Log(LoggingManager.LogCategory.UI, $"Selected right item: '{itemId}'", ComponentId);
        }

        // Update button visuals
        UpdateUIFromCurrentState();
    }

    /// <summary>
    /// Update a configuration button to show the selected item
    /// </summary>
    /// <param name="configButton">Button to update</param>
    /// <param name="itemId">Selected item ID</param>
    /// <param name="defaultText">Default text when no item selected</param>
    private void UpdateConfigButtonVisual(Button configButton, string itemId, string defaultText)
    {
        if (configButton == null) return;

        // Get button components
        Image buttonImage = configButton.GetComponent<Image>();
        if (buttonImage == null)
            buttonImage = configButton.GetComponentInChildren<Image>();
        
        Text buttonText = configButton.GetComponentInChildren<Text>();

        if (string.IsNullOrEmpty(itemId))
        {
            // No item selected - show default
            if (buttonText != null)
                buttonText.text = defaultText;
            
           // if (buttonImage != null)
           //     buttonImage.sprite = null;
        }
        else
        {
            // Item selected - show item info
            ItemDef itemDef = FactoryRegistry.Instance?.GetItem(itemId);
            
            if (itemDef != null)
            {
                if (buttonText != null)
                    buttonText.text = itemDef.displayName ?? itemId;

                if (buttonImage != null && !string.IsNullOrEmpty(itemDef.sprite))
                {
                    Sprite itemSprite = Resources.Load<Sprite>($"Sprites/Items/{itemDef.sprite}");
                    if (itemSprite != null)
                    {
                        buttonImage.sprite = itemSprite;
                    }
                }
            }
            else
            {
                // Fallback for unknown item
                if (buttonText != null)
                    buttonText.text = itemId;
            }
        }
    }
}