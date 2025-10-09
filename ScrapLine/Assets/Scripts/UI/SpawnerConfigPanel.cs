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
/// 3. Assign currentCrateButton (button that will display crate icon - must have Image component on it or child)
/// 4. Assign emptySelectionSprite and emptySelectionColor for when no crate is selected
/// 5. Assign currentCrateProgressBar (vertical Slider for showing crate fullness)
/// 6. Create WasteCrateSelectionPanel and assign wasteCrateSelectionPanel reference
/// 7. Create WasteCrateQueuePanel and assign queuePanel reference
/// 8. Optionally assign wasteCrateConfigPanel (or it will be found at runtime)
/// 9. Set panels inactive by default (activated when configuration needed)
/// </summary>
public class SpawnerConfigPanel : BaseConfigPanel<CellData, string>
{
    [Header("Spawner Configuration")]
    [Tooltip("Button showing current crate icon - clicking opens waste crate selection")]
    public Button currentCrateButton;
    
    [Tooltip("Sprite to show when no crate is selected")]
    public Sprite emptySelectionSprite;
    
    [Tooltip("Color to use when no crate is selected")]
    public Color emptySelectionColor = Color.gray;
    
    [Tooltip("Vertical progress bar showing current crate fullness")]
    public Slider currentCrateProgressBar;
    
    [Tooltip("Waste crate selection panel component")]
    public WasteCrateSelectionPanel wasteCrateSelectionPanel;
    
    [Tooltip("Queue panel showing top 3 queued waste crates")]
    public WasteCrateQueuePanel queuePanel;
    
    [Tooltip("Waste crate config panel for purchasing")]
    public WasteCrateConfigPanel wasteCrateConfigPanel;

    // State
    private string selectedRequiredCrateId = "";
    private SpawnerMachine currentSpawnerMachine;
    private Image currentCrateIconImage; // Cached reference to Image within button

    protected override void SetupCustomButtonListeners()
    {
        // Setup current crate button to open selection panel
        if (currentCrateButton != null)
        {
            currentCrateButton.onClick.AddListener(OnSelectCrateTypeClicked);
        }
        
        // Setup queue panel click to open purchase panel
        if (queuePanel != null)
        {
            queuePanel.OnQueueClicked += OnQueuePanelClicked;
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
        // Get the Image component within the current crate button
        if (currentCrateIconImage == null && currentCrateButton != null)
        {
            currentCrateIconImage = currentCrateButton.GetComponent<Image>();
            if (currentCrateIconImage == null)
            {
                currentCrateIconImage = currentCrateButton.GetComponentInChildren<Image>();
            }
            
            if (currentCrateIconImage == null)
            {
                GameLogger.LogError(LoggingManager.LogCategory.UI, "Current crate button has no Image component!", ComponentId);
            }
        }
        
        // Update crate icon
        if (currentCrateIconImage != null)
        {
            if (!string.IsNullOrEmpty(selectedRequiredCrateId))
            {
                var crateDef = FactoryRegistry.Instance?.GetWasteCrate(selectedRequiredCrateId);
                if (crateDef != null && !string.IsNullOrEmpty(crateDef.sprite))
                {
                    // Try to load sprite from Resources
                    var sprite = Resources.Load<Sprite>($"Sprites/Waste/{crateDef.sprite}");
                    if (sprite != null)
                    {
                        currentCrateIconImage.sprite = sprite;
                        currentCrateIconImage.color = Color.white; // Configured crate uses white color
                        currentCrateIconImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        GameLogger.LogWarning(LoggingManager.LogCategory.UI, $"Could not load sprite '{crateDef.sprite}' for crate", ComponentId);
                        // Fall back to empty state
                        SetEmptySelectionState();
                    }
                }
                else
                {
                    // Fall back to empty state
                    SetEmptySelectionState();
                }
            }
            else
            {
                // No selection - use empty state
                SetEmptySelectionState();
            }
        }

        // Update current crate progress bar
        UpdateCrateProgressBar();
        
        // Update queue panel display
        UpdateQueuePanelDisplay();
    }
    
    /// <summary>
    /// Set the current crate icon to empty selection state
    /// </summary>
    private void SetEmptySelectionState()
    {
        if (currentCrateIconImage != null)
        {
            if (emptySelectionSprite != null)
            {
                currentCrateIconImage.sprite = emptySelectionSprite;
                currentCrateIconImage.color = emptySelectionColor;
                currentCrateIconImage.gameObject.SetActive(true);
            }
            else
            {
                // If no empty sprite is set, hide the image
                currentCrateIconImage.gameObject.SetActive(false);
            }
        }
    }

    protected override string GetCurrentSelection()
    {
        return selectedRequiredCrateId;
    }

    protected override void UpdateDataWithSelection(string selection)
    {
        if (currentSpawnerMachine != null)
        {
            // Allow empty string to clear the filter
            string previousCrateId = currentSpawnerMachine.RequiredCrateId;
            currentSpawnerMachine.RequiredCrateId = string.IsNullOrEmpty(selection) ? "" : selection;
            
            if (string.IsNullOrEmpty(selection))
            {
                GameLogger.LogMachine($"Cleared spawner crate type filter", ComponentId);
            }
            else
            {
                GameLogger.LogMachine($"Updated spawner required crate type to '{selection}'", ComponentId);
            }
            
            // If the required crate type changed, handle the current waste crate
            if (previousCrateId != currentSpawnerMachine.RequiredCrateId)
            {
                HandleCrateTypeChange(previousCrateId);
            }
        }
    }
    
    /// <summary>
    /// Handle when the required crate type changes - return incompatible crate to queue
    /// </summary>
    /// <param name="previousCrateId">The previous required crate ID</param>
    private void HandleCrateTypeChange(string previousCrateId)
    {
        if (currentSpawnerMachine == null || currentData == null) return;
        
        var gridData = GameManager.Instance?.GetCurrentGrid();
        var cellData = gridData?.cells?.Find(c => c.x == currentData.x && c.y == currentData.y);
        
        if (cellData?.wasteCrate != null && !string.IsNullOrEmpty(cellData.wasteCrate.wasteCrateDefId))
        {
            string currentCrateId = cellData.wasteCrate.wasteCrateDefId;
            
            // If current crate doesn't match new required type, return it to queue
            bool crateMatches = string.IsNullOrEmpty(currentSpawnerMachine.RequiredCrateId) || 
                               currentCrateId == currentSpawnerMachine.RequiredCrateId;
            
            if (!crateMatches)
            {
                // Return the current crate to the global queue
                var wasteSupplyManager = GameManager.Instance?.wasteSupplyManager;
                if (wasteSupplyManager != null)
                {
                    wasteSupplyManager.ReturnCrateToQueue(currentCrateId);
                    GameLogger.LogMachine($"Returned incompatible crate '{currentCrateId}' to queue (new filter: '{currentSpawnerMachine.RequiredCrateId}')", ComponentId);
                    
                    // Clear the spawner's current crate
                    cellData.wasteCrate = null;
                    
                    // Try to get a matching crate from queue
                    if (currentSpawnerMachine != null)
                    {
                        // Use reflection to call TryRefillFromGlobalQueue
                        var spawnerType = currentSpawnerMachine.GetType();
                        var refillMethod = spawnerType.GetMethod("TryRefillFromGlobalQueue");
                        if (refillMethod != null)
                        {
                            refillMethod.Invoke(currentSpawnerMachine, null);
                        }
                    }
                }
                else
                {
                    GameLogger.LogWarning(LoggingManager.LogCategory.UI, "WasteSupplyManager not found - cannot return crate to queue", ComponentId);
                }
            }
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
    /// Called when the queue panel is clicked
    /// </summary>
    private void OnQueuePanelClicked()
    {
        HideConfiguration();
        
        // Find and show the waste crate config panel (purchase interface)
        if (wasteCrateConfigPanel != null && currentData != null)
        {
            wasteCrateConfigPanel.ShowConfiguration(currentData, null); // No callback needed for purchase
        }
        else
        {
            // Fallback to finding the panel if not assigned
            var purchasePanel = FindFirstObjectByType<WasteCrateConfigPanel>(FindObjectsInactive.Include);
            if (purchasePanel != null && currentData != null)
            {
                purchasePanel.ShowConfiguration(currentData, null);
            }
            else
            {
                GameLogger.LogWarning(LoggingManager.LogCategory.UI, "WasteCrateConfigPanel not found for purchase navigation", ComponentId);
            }
        }
    }
    
    /// <summary>
    /// Update the current crate progress bar based on spawner's current crate
    /// </summary>
    private void UpdateCrateProgressBar()
    {
        if (currentCrateProgressBar == null || currentSpawnerMachine == null)
            return;
            
        try
        {
            // Use reflection to get spawner methods (since we can't directly reference SpawnerMachine)
            var spawnerType = currentSpawnerMachine.GetType();
            var getTotalItemsMethod = spawnerType.GetMethod("GetTotalItemsInWasteCrate");
            var getInitialTotalMethod = spawnerType.GetMethod("GetInitialWasteCrateTotal");
            
            if (getTotalItemsMethod != null && getInitialTotalMethod != null)
            {
                int currentItems = (int)getTotalItemsMethod.Invoke(currentSpawnerMachine, null);
                int initialItems = (int)getInitialTotalMethod.Invoke(currentSpawnerMachine, null);
                
                // Update progress bar (fills from top to bottom for vertical bar)
                float fillPercent = initialItems > 0 ? (float)currentItems / initialItems : 0f;
                currentCrateProgressBar.value = fillPercent;
                currentCrateProgressBar.gameObject.SetActive(true);
            }
            else
            {
                currentCrateProgressBar.gameObject.SetActive(false);
            }
        }
        catch (System.Exception e)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, $"Could not update crate progress bar: {e.Message}", ComponentId);
            currentCrateProgressBar.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Update the queue panel display with current queue data
    /// </summary>
    private void UpdateQueuePanelDisplay()
    {
        if (queuePanel == null)
            return;
            
        // Get global queue status from WasteSupplyManager
        var wasteSupplyManager = GameManager.Instance?.wasteSupplyManager;
        if (wasteSupplyManager != null)
        {
            var queueStatus = wasteSupplyManager.GetGlobalQueueStatus();
            queuePanel.UpdateQueueDisplay(queueStatus.queuedCrateIds);
            queuePanel.ShowPanel();
        }
        else
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, "WasteSupplyManager not found for queue display", ComponentId);
            queuePanel.HidePanel();
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
        
        // Update queue panel when showing configuration
        UpdateQueuePanelDisplay();
    }
}