using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Configuration panel for waste crate purchasing using Plan A base classes.
/// Reduces original 314 lines to ~70 lines by leveraging BaseConfigPanel.
/// 
/// Handles waste crate purchasing for spawner machines with current crate display.
/// Note: This is different from other config panels as it's more of a purchase interface.
/// 
/// UNITY SETUP REQUIRED:
/// 1. Create main UI Panel GameObject with this component
/// 2. Assign configPanel, confirmButton, cancelButton (from BaseConfigPanel)
/// 3. Assign currentCrateInfo components and buyButton
/// 4. Create WasteCrateSelectionPanel and assign wasteCrateSelectionPanel reference  
/// 5. Set panels inactive by default (activated when configuration needed)
/// </summary>
public class WasteCrateConfigPanel : BaseConfigPanel<CellData, string>
{
    [Header("Waste Crate Specific")]
    [Tooltip("Button to open purchase panel")]
    public Button buyButton;
    
    [Tooltip("Waste crate selection panel component")]
    public WasteCrateSelectionPanel wasteCrateSelectionPanel;

    [Header("Current Crate Display")]
    [Tooltip("Text showing current crate name")]
    public TextMeshProUGUI currentCrateNameText;
    
    [Tooltip("Text showing crate fullness (e.g. '75/100 items')")]
    public TextMeshProUGUI currentCrateFullnessText;
    
    [Tooltip("Progress bar showing crate fullness")]
    public Slider currentCrateProgressBar;
    
    [Tooltip("Text showing queue status")]
    public TextMeshProUGUI queueStatusText;

    // State for current spawner
    private int currentSpawnerX;
    private int currentSpawnerY;
    private WasteCrateQueueStatus currentQueueStatus;
    private string selectedCrateId = "";

    /// <summary>
    /// Show configuration for a specific spawner location
    /// </summary>
    /// <param name="spawnerX">X coordinate of spawner</param>
    /// <param name="spawnerY">Y coordinate of spawner</param>
    /// <param name="onConfirmed">Callback when purchase is confirmed</param>
    public void ShowConfiguration(int spawnerX, int spawnerY, System.Action<string> onConfirmed)
    {
        currentSpawnerX = spawnerX;
        currentSpawnerY = spawnerY;
        
        // Create a fake CellData for base class compatibility
        var cellData = new CellData { x = spawnerX, y = spawnerY };
        
        ShowConfiguration(cellData, onConfirmed);
    }

    protected override void SetupCustomButtonListeners()
    {
        if (buyButton != null)
            buyButton.onClick.AddListener(ShowWasteCrateSelection);
    }

    protected override void LoadCurrentConfiguration()
    {
        currentQueueStatus = GameManager.Instance?.GetSpawnerQueueStatus(currentSpawnerX, currentSpawnerY);
        selectedCrateId = "";
        
        if (currentQueueStatus == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.UI, $"Could not get queue status for spawner at ({currentSpawnerX}, {currentSpawnerY})", ComponentId);
        }
    }

    protected override void UpdateUIFromCurrentState()
    {
        UpdateCurrentCrateDisplay();
        UpdateBuyButtonState();
    }

    protected override string GetCurrentSelection()
    {
        return selectedCrateId;
    }

    protected override void UpdateDataWithSelection(string selection)
    {
        // For waste crates, the "selection" is actually a purchase action
        // The actual purchase happens in OnWasteCrateSelected
        selectedCrateId = selection;
    }

    protected override void HideSelectionPanels()
    {
        if (wasteCrateSelectionPanel != null)
            wasteCrateSelectionPanel.HidePanel();
    }

    /// <summary>
    /// Override to customize behavior for waste crate "confirmation"
    /// </summary>
    protected override void OnConfigurationConfirmed(string selection)
    {
        // For waste crates, confirmation means successful purchase
        GameLogger.LogEconomy($"Waste crate purchase confirmed: {selection}", ComponentId);
    }

    /// <summary>
    /// Show waste crate selection panel
    /// </summary>
    private void ShowWasteCrateSelection()
    {
        if (wasteCrateSelectionPanel != null)
        {
            wasteCrateSelectionPanel.ShowPanel(OnWasteCrateSelected);
        }
        else
        {
            GameLogger.LogError(LoggingManager.LogCategory.UI, "WasteCrateSelectionPanel not assigned!", ComponentId);
        }
    }

    /// <summary>
    /// Handle waste crate selection and immediate purchase
    /// </summary>
    /// <param name="crate">Selected waste crate</param>
    private void OnWasteCrateSelected(WasteCrateDef crate)
    {
        if (crate == null)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, "No crate selected", ComponentId);
            return;
        }

        selectedCrateId = crate.id;
        
        // Attempt immediate purchase
        bool success = GameManager.Instance?.PurchaseWasteCrate(crate.id, currentSpawnerX, currentSpawnerY) ?? false;
        
        if (success)
        {
            GameLogger.LogEconomy($"Successfully purchased {crate.displayName}", ComponentId);
            
            // Refresh the queue status and update display
            currentQueueStatus = GameManager.Instance.GetSpawnerQueueStatus(currentSpawnerX, currentSpawnerY);
            UpdateUIFromCurrentState();
            
            // Call the callback to notify of successful purchase
            onConfigurationConfirmed?.Invoke(selectedCrateId);
            
            // Hide the configuration (purchase complete)
            HideConfiguration();
        }
        else
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Economy, $"Failed to purchase {crate.displayName}", ComponentId);
        }
    }

    /// <summary>
    /// Update the current crate display with latest information
    /// </summary>
    private void UpdateCurrentCrateDisplay()
    {
        if (currentQueueStatus == null) return;
        
        // Get current crate definition
        var crateDef = FactoryRegistry.Instance?.GetWasteCrate(currentQueueStatus.currentCrateId);
        if (crateDef != null)
        {
            if (currentCrateNameText != null)
                currentCrateNameText.text = crateDef.displayName;
        }
        else
        {
            if (currentCrateNameText != null)
                currentCrateNameText.text = "No Crate";
        }
        
        // Get spawner to check current fullness
        var gridData = GameManager.Instance?.GetCurrentGrid();
        var cellData = gridData?.cells?.Find(c => c.x == currentSpawnerX && c.y == currentSpawnerY);
        var spawner = cellData?.machine as SpawnerMachine;
        
        if (spawner != null)
        {
            // Use reflection to get spawner methods (since we can't directly reference SpawnerMachine)
            try
            {
                var spawnerType = spawner.GetType();
                var getTotalItemsMethod = spawnerType.GetMethod("GetTotalItemsInWasteCrate");
                var getInitialTotalMethod = spawnerType.GetMethod("GetInitialWasteCrateTotal");
                
                if (getTotalItemsMethod != null && getInitialTotalMethod != null)
                {
                    int currentItems = (int)getTotalItemsMethod.Invoke(spawner, null);
                    int initialItems = (int)getInitialTotalMethod.Invoke(spawner, null);
                    
                    if (currentCrateFullnessText != null)
                        currentCrateFullnessText.text = $"{currentItems}/{initialItems} items";
                        
                    if (currentCrateProgressBar != null)
                    {
                        float fillPercent = initialItems > 0 ? (float)currentItems / initialItems : 0f;
                        currentCrateProgressBar.value = fillPercent;
                    }
                }
            }
            catch (System.Exception e)
            {
                GameLogger.LogWarning(LoggingManager.LogCategory.UI, $"Could not get spawner crate info: {e.Message}", ComponentId);
            }
        }
        
        // Update queue status
        if (queueStatusText != null)
        {
            if (currentQueueStatus.queuedCrateIds.Count > 0)
            {
                string queuedCrateName = "Unknown";
                var queuedCrateDef = FactoryRegistry.Instance?.GetWasteCrate(currentQueueStatus.queuedCrateIds[0]);
                if (queuedCrateDef != null)
                    queuedCrateName = queuedCrateDef.displayName;
                    
                queueStatusText.text = $"Queue: {queuedCrateName} ({currentQueueStatus.queuedCrateIds.Count}/{currentQueueStatus.maxQueueSize})";
            }
            else
            {
                queueStatusText.text = $"Queue: Empty ({currentQueueStatus.queuedCrateIds.Count}/{currentQueueStatus.maxQueueSize})";
            }
        }
    }

    /// <summary>
    /// Update the state of the buy button based on queue capacity
    /// </summary>
    private void UpdateBuyButtonState()
    {
        if (buyButton != null && currentQueueStatus != null)
        {
            buyButton.interactable = currentQueueStatus.canAddToQueue;
            
            // Update button text based on state
            var buttonText = buyButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                if (currentQueueStatus.canAddToQueue)
                {
                    buttonText.text = "Buy Crate";
                }
                else
                {
                    buttonText.text = "Queue Full";
                }
            }
        }
    }
}