using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Configuration panel for waste crate purchasing - combines queue display and purchase grid.
/// This panel is different from other config panels as it's a direct purchase interface.
/// 
/// CHANGES FROM PREVIOUS VERSION:
/// - Combines queue display and crate selection in a single panel
/// - Shows current queue status using WasteCrateQueuePanel
/// - Displays grid of all purchasable crates (similar to WasteCrateSelectionPanel)
/// - Immediate purchase on crate click - no confirm/cancel workflow
/// - Does NOT inherit from BaseConfigPanel (too different in behavior)
/// 
/// UNITY SETUP REQUIRED:
/// 1. Create main UI Panel GameObject with this component
/// 2. Assign mainPanel (the panel to show/hide)
/// 3. Assign queuePanel component (WasteCrateQueuePanel)
/// 4. Assign crateGridContainer (Transform with GridLayoutGroup for crate buttons)
/// 5. Assign crateButtonPrefab (button prefab for each purchasable crate)
/// 6. Optionally assign closeButton to close the panel
/// 7. Set panel inactive by default (activated when needed)
/// </summary>
public class WasteCrateConfigPanel : MonoBehaviour
{
    [Header("Main Panel Components")]
    [Tooltip("Main panel GameObject to show/hide")]
    public GameObject mainPanel;
    
    [Tooltip("Button to close the panel")]
    public Button closeButton;

    [Header("Queue Display")]
    [Tooltip("Queue panel component showing current waste crate queue")]
    public WasteCrateQueuePanel queuePanel;

    [Header("Purchase Grid")]
    [Tooltip("Container for crate purchase buttons (should have GridLayoutGroup)")]
    public Transform crateGridContainer;
    
    [Tooltip("Prefab for crate purchase buttons (must have Button, Image, TextMeshProUGUI)")]
    public GameObject crateButtonPrefab;
    
    [Tooltip("Show cost in button text")]
    public bool showCostInText = true;

    /// <summary>
    /// Component ID for logging purposes
    /// </summary>
    private string ComponentId => $"WasteCrateConfigPanel_{GetInstanceID()}";
    
    /// <summary>
    /// List of instantiated crate button objects
    /// </summary>
    private List<GameObject> crateButtons = new List<GameObject>();
    
    /// <summary>
    /// Callback when panel is closed
    /// </summary>
    private System.Action onPanelClosed;

    void Start()
    {
        // Setup close button listener
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HidePanel);
        }
        
        // Initialize panel as hidden
        if (mainPanel != null)
        {
            mainPanel.SetActive(false);
        }
        
        GameLogger.LogUI("WasteCrateConfigPanel initialized", ComponentId);
        
        // Auto-open panel if waste queue is empty on game start
        StartCoroutine(CheckAndAutoOpenPanel());
    }
    
    /// <summary>
    /// Check if the waste queue is empty and auto-open the panel if needed
    /// </summary>
    private IEnumerator CheckAndAutoOpenPanel()
    {
        // Wait a frame to ensure GameManager is fully initialized
        yield return new WaitForEndOfFrame();
        
        // Check if waste queue is empty
        if (GameManager.Instance?.gameData != null)
        {
            var wasteQueue = GameManager.Instance.gameData.wasteQueue;
            if (wasteQueue == null || wasteQueue.Count == 0)
            {
                GameLogger.LogUI("Waste queue is empty - auto-opening purchase panel", ComponentId);
                ShowPanel();
            }
        }
    }

    /// <summary>
    /// Show the waste crate configuration panel
    /// </summary>
    /// <param name="onClosed">Optional callback when panel is closed</param>
    public void ShowPanel(System.Action onClosed = null)
    {
        onPanelClosed = onClosed;
        
        // Register with panel manager
        var panelManager = FindFirstObjectByType<UIPanelManager>();
        if (panelManager != null)
        {
            panelManager.RegisterOpenPanel(this);
        }
        
        // Show main panel first so layout can be calculated
        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
        }
        
        // Update queue display
        UpdateQueueDisplay();
        
        // Populate purchase grid after panel is shown and layout calculated
        StartCoroutine(PopulatePurchaseGridDelayed());
        
        GameLogger.LogUI("Waste crate config panel shown", ComponentId);
    }
    
    /// <summary>
    /// Populate the purchase grid after a delay to ensure layout is calculated
    /// </summary>
    private IEnumerator PopulatePurchaseGridDelayed()
    {
        // Wait for end of frame to ensure layout has been calculated
        yield return new WaitForEndOfFrame();
        
        PopulatePurchaseGrid();
    }

    /// <summary>
    /// Hide the waste crate configuration panel
    /// </summary>
    public void HidePanel()
    {
        // Unregister from panel manager
        var panelManager = FindFirstObjectByType<UIPanelManager>();
        if (panelManager != null)
        {
            panelManager.UnregisterPanel(this);
        }
        
        // Hide main panel
        if (mainPanel != null)
        {
            mainPanel.SetActive(false);
        }
        
        // Call close callback
        onPanelClosed?.Invoke();
        onPanelClosed = null;
        
        GameLogger.LogUI("Waste crate config panel hidden", ComponentId);
    }

    /// <summary>
    /// Update the queue display with current queue data
    /// </summary>
    private void UpdateQueueDisplay()
    {
        if (queuePanel == null)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, "Queue panel not assigned", ComponentId);
            return;
        }
        
        // Get queue status from WasteSupplyManager
        var wasteSupplyManager = GameManager.Instance?.wasteSupplyManager;
        if (wasteSupplyManager != null)
        {
            var queueStatus = wasteSupplyManager.GetGlobalQueueStatus();
            queuePanel.UpdateQueueDisplay(queueStatus.queuedCrateIds);
        }
        else
        {
            GameLogger.LogError(LoggingManager.LogCategory.UI, "WasteSupplyManager not found", ComponentId);
        }
    }

    /// <summary>
    /// Populate the grid with all purchasable waste crates
    /// </summary>
    private void PopulatePurchaseGrid()
    {
        // Clear existing buttons
        ClearPurchaseGrid();
        
        if (crateGridContainer == null || crateButtonPrefab == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.UI, "Crate grid container or button prefab not assigned", ComponentId);
            return;
        }
        
        // Configure grid layout (3 columns like WasteCrateSelectionPanel)
        ConfigureGridLayout();
        
        // Get all available waste crates - use FactoryRegistry directly for reliability
        var availableCrates = FactoryRegistry.Instance?.GetAllWasteCrates() ?? new List<WasteCrateDef>();
        
        GameLogger.LogUI($"FactoryRegistry returned {availableCrates.Count} waste crates", ComponentId);
        
        if (availableCrates.Count == 0)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, "No waste crates available - check if wastecrates.json loaded correctly", ComponentId);
            return;
        }
        
        // Create button for each crate
        foreach (var crate in availableCrates)
        {
            GameLogger.LogUI($"Creating button for crate: {crate.id} - {crate.displayName}", ComponentId);
            CreateCrateButton(crate);
        }
        
        GameLogger.LogUI($"Populated purchase grid with {availableCrates.Count} crates", ComponentId);
    }

    /// <summary>
    /// Configure the grid layout for 3-column responsive display
    /// </summary>
    private void ConfigureGridLayout()
    {
        var gridLayout = crateGridContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            // Get container width for responsive sizing
            RectTransform containerRect = crateGridContainer as RectTransform;
            
            // Force layout rebuild to get accurate dimensions
            if (containerRect != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
            }
            
            float containerWidth = containerRect != null ? containerRect.rect.width : 500f;
            
            GameLogger.LogUI($"Container width: {containerWidth}", ComponentId);
            
            // If container width is still 0 or too small, use fallback
            if (containerWidth <= 100f)
            {
                GameLogger.LogWarning(LoggingManager.LogCategory.UI, $"Container width too small ({containerWidth}), using fallback value of 500", ComponentId);
                containerWidth = 500f;
            }
            
            // Calculate cell size for 3 columns with spacing
            float spacingX = gridLayout.spacing.x > 0 ? gridLayout.spacing.x : 10f;
            float spacingY = gridLayout.spacing.y > 0 ? gridLayout.spacing.y : 10f;
            float totalSpacing = spacingX * 2; // 2 gaps for 3 columns
            
            // Available width = container width - spacing
            float availableWidth = containerWidth - totalSpacing;
            float cellSize = availableWidth / 3f;
            
            // Ensure minimum cell size
            cellSize = Mathf.Max(cellSize, 50f);
            
            GameLogger.LogUI($"Calculated cell size: containerWidth={containerWidth}, spacing={totalSpacing}, availableWidth={availableWidth}, cellSize={cellSize}", ComponentId);
            
            // Configure grid layout
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3;
            gridLayout.cellSize = new Vector2(cellSize, cellSize);
            gridLayout.spacing = new Vector2(spacingX, spacingY);
            
            GameLogger.LogUI($"Configured grid layout: cellSize={cellSize}x{cellSize}, spacing={spacingX}x{spacingY}", ComponentId);
        }
        else
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, "Crate grid container missing GridLayoutGroup", ComponentId);
        }
    }

    /// <summary>
    /// Create a purchase button for a waste crate
    /// </summary>
    /// <param name="crate">Crate definition to create button for</param>
    private void CreateCrateButton(WasteCrateDef crate)
    {
        GameObject buttonObj = Instantiate(crateButtonPrefab, crateGridContainer);
        crateButtons.Add(buttonObj);
        
        // Try to find Button component on the object or its children
        Button button = buttonObj.GetComponent<Button>();
        if (button == null)
        {
            button = buttonObj.GetComponentInChildren<Button>();
        }
        
        if (button != null)
        {
            // Setup click listener for immediate purchase
            button.onClick.AddListener(() => OnCratePurchaseClicked(crate));
            
            GameLogger.LogUI($"Added click listener to button for crate {crate.id}", ComponentId);
            
            // Setup button visuals
            SetupButtonVisuals(buttonObj, crate);
            
            // Enable/disable based on affordability
            var wasteSupplyManager = GameManager.Instance?.wasteSupplyManager;
            if (wasteSupplyManager != null)
            {
                bool canAfford = wasteSupplyManager.CanAffordWasteCrate(crate.id);
                var queueStatus = wasteSupplyManager.GetGlobalQueueStatus();
                bool queueHasSpace = queueStatus.canAddToQueue;
                
                button.interactable = canAfford && queueHasSpace;
                
                GameLogger.LogUI($"Button for {crate.id}: interactable={button.interactable}, canAfford={canAfford}, queueHasSpace={queueHasSpace}", ComponentId);
                
                if (!canAfford || !queueHasSpace)
                {
                    // Dim the button if can't afford or queue full
                    var colors = button.colors;
                    colors.normalColor = colors.disabledColor;
                    button.colors = colors;
                }
            }
        }
        else
        {
            GameLogger.LogError(LoggingManager.LogCategory.UI, $"Crate button prefab missing Button component for {crate.id}", ComponentId);
        }
    }

    /// <summary>
    /// Setup visual appearance for a crate button
    /// </summary>
    /// <param name="buttonObj">Button GameObject</param>
    /// <param name="crate">Crate definition</param>
    private void SetupButtonVisuals(GameObject buttonObj, WasteCrateDef crate)
    {
        // Get the cost first (needed for both text and logging)
        int crateCost = crate.cost > 0 ? crate.cost : 0;
        if (crateCost == 0)
        {
            // Fallback: Calculate from WasteSupplyManager if cost is 0
            var wasteSupplyManager = GameManager.Instance?.wasteSupplyManager;
            crateCost = wasteSupplyManager?.GetWasteCrateCost(crate.id) ?? 0;
        }
        
        // Set button text with price
        var buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            string displayText = crate.displayName ?? crate.id ?? "Crate";
            
            // Always show the cost (showCostInText is always true by default)
            displayText += $"\n{crateCost} credits";
            
            buttonText.text = displayText;
            GameLogger.LogUI($"Set button text: '{displayText}' for crate {crate.id}", ComponentId);
        }
        else
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, $"No TextMeshProUGUI found in button prefab for crate {crate.id}", ComponentId);
        }
        
        // Set button sprite
        if (!string.IsNullOrEmpty(crate.sprite))
        {
            string spritePath = $"Sprites/Waste/{crate.sprite}";
            Sprite crateSprite = Resources.Load<Sprite>(spritePath);
            
            if (crateSprite != null)
            {
                var buttonImage = buttonObj.GetComponent<Image>();
                if (buttonImage == null)
                    buttonImage = buttonObj.GetComponentInChildren<Image>();
                
                if (buttonImage != null)
                {
                    buttonImage.sprite = crateSprite;
                    buttonImage.color = Color.white;
                    buttonImage.preserveAspect = true;
                    buttonImage.type = Image.Type.Simple;
                    GameLogger.LogUI($"Set sprite '{spritePath}' for crate {crate.id}", ComponentId);
                }
            }
            else
            {
                GameLogger.LogWarning(LoggingManager.LogCategory.UI, $"Could not load sprite '{spritePath}' for crate {crate.id}", ComponentId);
            }
        }
        else
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, $"No sprite defined for crate {crate.id}", ComponentId);
        }
    }

    /// <summary>
    /// Handle crate purchase button click - immediate purchase
    /// </summary>
    /// <param name="crate">Crate to purchase</param>
    private void OnCratePurchaseClicked(WasteCrateDef crate)
    {
        GameLogger.LogUI($"Button clicked for crate: {crate?.id ?? "null"}", ComponentId);
        
        if (crate == null)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, "Attempted to purchase null crate", ComponentId);
            return;
        }
        
        // Attempt to purchase the crate
        var wasteSupplyManager = GameManager.Instance?.wasteSupplyManager;
        if (wasteSupplyManager == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.Economy, "WasteSupplyManager not found", ComponentId);
            return;
        }
        
        GameLogger.LogUI($"Attempting to purchase {crate.displayName} (ID: {crate.id})", ComponentId);
        
        bool success = wasteSupplyManager.PurchaseWasteCrate(crate.id);
        
        if (success)
        {
            GameLogger.LogEconomy($"Successfully purchased {crate.displayName}", ComponentId);
            
            // Refresh the display to show updated queue and button states
            UpdateQueueDisplay();
            PopulatePurchaseGrid();
        }
        else
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Economy, $"Failed to purchase {crate.displayName} - check credits and queue space", ComponentId);
        }
    }

    /// <summary>
    /// Clear all purchase grid buttons
    /// </summary>
    private void ClearPurchaseGrid()
    {
        foreach (var button in crateButtons)
        {
            if (button != null)
            {
                Destroy(button);
            }
        }
        crateButtons.Clear();
    }

    void OnDestroy()
    {
        ClearPurchaseGrid();
    }
}