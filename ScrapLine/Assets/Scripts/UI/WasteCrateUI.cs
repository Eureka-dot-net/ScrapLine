using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Manages the waste crate menu UI that appears when clicking on spawners
/// </summary>
public class WasteCrateUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Main panel that contains the waste crate menu")]
    public GameObject wasteCratePanel;
    
    [Tooltip("Panel showing current crate info")]
    public GameObject currentCratePanel;
    
    [Tooltip("Panel showing purchase options")]
    public GameObject purchasePanel;
    
    [Header("Current Crate Display")]
    [Tooltip("Text showing current crate name")]
    public TextMeshProUGUI currentCrateNameText;
    
    [Tooltip("Text showing crate fullness (e.g. '75/100 items')")]
    public TextMeshProUGUI currentCrateFullnessText;
    
    [Tooltip("Progress bar showing crate fullness")]
    public Slider currentCrateProgressBar;
    
    [Tooltip("Text showing queue status")]
    public TextMeshProUGUI queueStatusText;
    
    [Header("Purchase Options")]
    [Tooltip("Button to open purchase panel")]
    public Button buyButton;
    
    [Tooltip("Parent for purchase option buttons")]
    public Transform purchaseOptionsParent;
    
    [Tooltip("Prefab for purchase option buttons")]
    public GameObject purchaseOptionPrefab;
    
    [Header("Close Controls")]
    [Tooltip("Button to close the menu")]
    public Button closeButton;
    
    // State
    private int currentSpawnerX;
    private int currentSpawnerY;
    private WasteCrateQueueStatus currentQueueStatus;
    
    /// <summary>
    /// Get the component ID for logging purposes
    /// </summary>
    private string ComponentId => $"WasteCrateUI_{GetInstanceID()}";

    void Start()
    {
        // Set up button listeners
        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyButtonClicked);
            
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseButtonClicked);
            
        // Hide panels by default
        HideMenu();
    }
    
    /// <summary>
    /// Show the waste crate menu for a specific spawner
    /// </summary>
    /// <param name="spawnerX">X coordinate of spawner</param>
    /// <param name="spawnerY">Y coordinate of spawner</param>
    public void ShowMenu(int spawnerX, int spawnerY)
    {
        currentSpawnerX = spawnerX;
        currentSpawnerY = spawnerY;
        currentQueueStatus = GameManager.Instance.GetSpawnerQueueStatus(spawnerX, spawnerY);
        
        if (currentQueueStatus == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.UI, $"Could not get queue status for spawner at ({spawnerX}, {spawnerY})", ComponentId);
            return;
        }
        
        UpdateCurrentCrateDisplay();
        UpdateBuyButtonState();
        
        if (wasteCratePanel != null)
            wasteCratePanel.SetActive(true);
            
        if (currentCratePanel != null)
            currentCratePanel.SetActive(true);
            
        // Hide purchase panel initially
        if (purchasePanel != null)
            purchasePanel.SetActive(false);
            
        GameLogger.LogUI($"Waste crate menu shown for spawner at ({spawnerX}, {spawnerY})", ComponentId);
    }
    
    /// <summary>
    /// Hide the waste crate menu
    /// </summary>
    public void HideMenu()
    {
        if (wasteCratePanel != null)
            wasteCratePanel.SetActive(false);
            
        GameLogger.LogUI("Waste crate menu hidden", ComponentId);
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
        var gridData = GameManager.Instance.GetCurrentGrid();
        var cellData = gridData?.cells?.Find(c => c.x == currentSpawnerX && c.y == currentSpawnerY);
        var spawner = cellData?.machine as SpawnerMachine;
        
        if (spawner != null)
        {
            // Calculate current fullness
            int currentItems = spawner.GetTotalItemsInWasteCrate();
            int initialItems = spawner.GetInitialWasteCrateTotal();
            
            if (currentCrateFullnessText != null)
                currentCrateFullnessText.text = $"{currentItems}/{initialItems} items";
                
            if (currentCrateProgressBar != null)
            {
                float fillPercent = initialItems > 0 ? (float)currentItems / initialItems : 0f;
                currentCrateProgressBar.value = fillPercent;
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
    
    /// <summary>
    /// Handle buy button click - show purchase options
    /// </summary>
    private void OnBuyButtonClicked()
    {
        GameLogger.LogUI("Buy button clicked - showing purchase options", ComponentId);
        ShowPurchaseOptions();
    }
    
    /// <summary>
    /// Show the purchase options panel with all available crates
    /// </summary>
    private void ShowPurchaseOptions()
    {
        if (purchasePanel == null || purchaseOptionsParent == null || purchaseOptionPrefab == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.UI, "Purchase panel components not set up", ComponentId);
            return;
        }
        
        // Clear existing options
        foreach (Transform child in purchaseOptionsParent)
        {
            Destroy(child.gameObject);
        }
        
        // Get all available waste crates
        var wasteCrates = FactoryRegistry.Instance?.GetAllWasteCrates();
        if (wasteCrates == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.UI, "Could not get waste crates from registry", ComponentId);
            return;
        }
        
        // Create purchase option for each crate
        foreach (var crate in wasteCrates)
        {
            CreatePurchaseOption(crate);
        }
        
        // Show purchase panel
        purchasePanel.SetActive(true);
        
        // Hide current crate panel to make room
        if (currentCratePanel != null)
            currentCratePanel.SetActive(false);
    }
    
    /// <summary>
    /// Create a purchase option button for a waste crate
    /// </summary>
    /// <param name="crateDef">Waste crate definition</param>
    private void CreatePurchaseOption(WasteCrateDef crateDef)
    {
        GameObject optionObj = Instantiate(purchaseOptionPrefab, purchaseOptionsParent);
        
        // Set up the option display
        var nameText = optionObj.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText != null)
        {
            int cost = crateDef.cost > 0 ? crateDef.cost : SpawnerMachine.CalculateWasteCrateCost(crateDef);
            nameText.text = $"{crateDef.displayName}\n{cost} credits";
        }
        
        // Set up the button
        var button = optionObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => OnPurchaseOptionClicked(crateDef));
            
            // Disable if player can't afford it
            int cost = crateDef.cost > 0 ? crateDef.cost : SpawnerMachine.CalculateWasteCrateCost(crateDef);
            button.interactable = GameManager.Instance.CanAfford(cost);
        }
    }
    
    /// <summary>
    /// Handle purchase option click - buy the selected crate
    /// </summary>
    /// <param name="crateDef">Waste crate to purchase</param>
    private void OnPurchaseOptionClicked(WasteCrateDef crateDef)
    {
        GameLogger.LogUI($"Purchase option clicked: {crateDef.displayName}", ComponentId);
        
        bool success = GameManager.Instance.PurchaseWasteCrate(crateDef.id, currentSpawnerX, currentSpawnerY);
        
        if (success)
        {
            GameLogger.LogEconomy($"Successfully purchased {crateDef.displayName}", ComponentId);
            
            // Refresh the queue status and update display
            currentQueueStatus = GameManager.Instance.GetSpawnerQueueStatus(currentSpawnerX, currentSpawnerY);
            UpdateCurrentCrateDisplay();
            UpdateBuyButtonState();
            
            // Hide purchase panel and show current crate panel
            if (purchasePanel != null)
                purchasePanel.SetActive(false);
            if (currentCratePanel != null)
                currentCratePanel.SetActive(true);
        }
        else
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Economy, $"Failed to purchase {crateDef.displayName}", ComponentId);
        }
    }
    
    /// <summary>
    /// Handle close button click - hide the entire menu
    /// </summary>
    private void OnCloseButtonClicked()
    {
        HideMenu();
    }
}