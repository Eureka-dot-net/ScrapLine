using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Configuration panel for spawner machines using Plan A base classes.
/// Handles waste crate type selection and configuration for spawner filtering.
/// 
/// UNITY SETUP REQUIRED:
/// 1. Create main UI Panel GameObject with this component
/// 2. Assign configPanel, confirmButton, cancelButton (from BaseConfigPanel)
/// 3. Assign crateSelectionButton for opening crate selection
/// 4. Create WasteCrateSelectionPanel and assign wasteCrateSelectionPanel reference
/// 5. Set panels inactive by default (activated when configuration needed)
/// </summary>
public class SpawnerConfigPanel : BaseConfigPanel<CellData, string>
{
    [Header("Spawner Configuration")]
    [Tooltip("Button to select required crate type")]
    public Button crateSelectionButton;
    
    [Tooltip("Text showing current required crate type")]
    public TextMeshProUGUI currentCrateTypeText;
    
    [Tooltip("Image showing current crate icon")]
    public Image currentCrateIcon;
    
    [Tooltip("Waste crate selection panel component")]
    public WasteCrateSelectionPanel wasteCrateSelectionPanel;
    
    [Tooltip("Button to navigate to purchase panel")]
    public Button purchaseButton;

    // State
    private string selectedRequiredCrateId = "";
    private SpawnerMachine currentSpawnerMachine;

    protected override void SetupCustomButtonListeners()
    {
        // Setup crate selection button
        if (crateSelectionButton != null)
        {
            crateSelectionButton.onClick.AddListener(OnSelectCrateTypeClicked);
        }
        
        // Setup purchase navigation button
        if (purchaseButton != null)
        {
            purchaseButton.onClick.AddListener(OnPurchaseClicked);
        }

        // Setup selection panel callback
        if (wasteCrateSelectionPanel != null)
        {
            wasteCrateSelectionPanel.OnCrateSelected += OnCrateTypeSelected;
        }
    }

    protected override void LoadCurrentConfiguration()
    {
        if (currentData?.machine is SpawnerMachine spawner)
        {
            currentSpawnerMachine = spawner;
            selectedRequiredCrateId = spawner.RequiredCrateId ?? "starter_crate";
            GameLogger.LogUI($"Loaded spawner config: RequiredCrateId = '{selectedRequiredCrateId}'", ComponentId);
        }
        else
        {
            selectedRequiredCrateId = "starter_crate";
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, "No SpawnerMachine found in cell data", ComponentId);
        }
    }

    protected override void UpdateUIFromCurrentState()
    {
        // Update current crate type display
        if (currentCrateTypeText != null)
        {
            var crateDef = FactoryRegistry.Instance?.GetWasteCrate(selectedRequiredCrateId);
            string displayName = crateDef?.displayName ?? selectedRequiredCrateId;
            currentCrateTypeText.text = $"Required: {displayName}";
        }

        // Update crate icon
        if (currentCrateIcon != null && !string.IsNullOrEmpty(selectedRequiredCrateId))
        {
            var crateDef = FactoryRegistry.Instance?.GetWasteCrate(selectedRequiredCrateId);
            if (crateDef != null && !string.IsNullOrEmpty(crateDef.sprite))
            {
                // Try to load sprite from Resources
                var sprite = Resources.Load<Sprite>($"Sprites/{crateDef.sprite}");
                if (sprite != null)
                {
                    currentCrateIcon.sprite = sprite;
                    currentCrateIcon.gameObject.SetActive(true);
                }
                else
                {
                    GameLogger.LogWarning(LoggingManager.LogCategory.UI, $"Could not load sprite '{crateDef.sprite}' for crate", ComponentId);
                    currentCrateIcon.gameObject.SetActive(false);
                }
            }
            else
            {
                currentCrateIcon.gameObject.SetActive(false);
            }
        }

        // Update button text to show current selection
        if (crateSelectionButton != null)
        {
            var buttonText = crateSelectionButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                var crateDef = FactoryRegistry.Instance?.GetWasteCrate(selectedRequiredCrateId);
                buttonText.text = crateDef?.displayName ?? "Select Crate Type";
            }
        }
    }

    protected override string GetCurrentSelection()
    {
        return selectedRequiredCrateId;
    }

    protected override void UpdateDataWithSelection(string selection)
    {
        if (currentSpawnerMachine != null && !string.IsNullOrEmpty(selection))
        {
            currentSpawnerMachine.RequiredCrateId = selection;
            GameLogger.LogMachine($"Updated spawner required crate type to '{selection}'", ComponentId);
        }
    }

    protected override void HideSelectionPanels()
    {
        if (wasteCrateSelectionPanel != null)
            wasteCrateSelectionPanel.HidePanel();
    }

    /// <summary>
    /// Called when the "Select Crate Type" button is clicked
    /// </summary>
    private void OnSelectCrateTypeClicked()
    {
        if (wasteCrateSelectionPanel != null)
        {
            var allCrates = FactoryRegistry.Instance?.GetAllWasteCrates() ?? new List<WasteCrateDef>();
            wasteCrateSelectionPanel.ShowCrateSelection(allCrates, selectedRequiredCrateId);
        }
        else
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, "WasteCrateSelectionPanel not assigned", ComponentId);
        }
    }

    /// <summary>
    /// Called when a crate type is selected from the selection panel
    /// </summary>
    /// <param name="selectedCrateId">ID of the selected crate</param>
    private void OnCrateTypeSelected(string selectedCrateId)
    {
        if (!string.IsNullOrEmpty(selectedCrateId))
        {
            selectedRequiredCrateId = selectedCrateId;
            UpdateUIFromCurrentState();
            GameLogger.LogUI($"Selected crate type: '{selectedCrateId}'", ComponentId);
        }
    }

    /// <summary>
    /// Called when the "Purchase Crates" button is clicked
    /// </summary>
    private void OnPurchaseClicked()
    {
        HideConfiguration();
        
        // Find and show the waste crate config panel (purchase interface)
        var purchasePanel = FindFirstObjectByType<WasteCrateConfigPanel>(FindObjectsInactive.Include);
        if (purchasePanel != null && currentData != null)
        {
            purchasePanel.ShowConfiguration(currentData, null); // No callback needed for purchase
        }
        else
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, "WasteCrateConfigPanel not found for purchase navigation", ComponentId);
        }
    }

    /// <summary>
    /// Show configuration for a specific spawner
    /// </summary>
    /// <param name="cellData">The spawner cell data</param>
    /// <param name="onConfirmed">Callback when configuration is confirmed</param>
    public new void ShowConfiguration(CellData cellData, System.Action<string> onConfirmed)
    {
        base.ShowConfiguration(cellData, onConfirmed);
        
        // Additional setup for spawner-specific UI
        if (purchaseButton != null && cellData?.machine is SpawnerMachine spawner)
        {
            // Enable purchase button and show global queue status
            purchaseButton.gameObject.SetActive(true);
            
            // Get global queue status instead of per-spawner queue
            var gameManager = GameManager.Instance;
            if (gameManager?.gameData != null)
            {
                var buttonText = purchaseButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = $"Purchase Crates (Global: {gameManager.gameData.wasteQueue.Count}/{gameManager.gameData.wasteQueueLimit})";
                }
            }
        }
    }
}