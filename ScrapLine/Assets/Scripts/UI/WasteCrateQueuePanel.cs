using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Layout direction for queue items
/// </summary>
public enum QueueLayoutDirection
{
    Left,   // Items arranged left to right
    Right,  // Items arranged right to left
    Top,    // Items arranged top to bottom
    Bottom  // Items arranged bottom to top
}

/// <summary>
/// Reusable panel for displaying queued waste crates.
/// Shows ALL waste crates from the queue with their icons (uses ScrollRect for overflow).
/// Can be clicked to open the WasteCrateConfigPanel for purchasing.
/// 
/// UNITY SETUP REQUIRED:
/// 1. Create UI Panel GameObject with this component (must have Button component)
/// 2. Assign queuePanel (the panel GameObject itself - button will be auto-detected)
/// 3. Assign scrollView (optional - GameObject with ScrollRect for scrollable content)
/// 4. Assign queueContainer (parent for queue item displays - should have Layout Group)
///    - queueContainer should be child of scrollView's Content if using ScrollRect
/// 5. Assign queueItemPrefab (prefab with Image component for crate icon)
/// 6. Optionally assign emptyQueueText to show when queue is empty
/// 7. Set layoutDirection to control item arrangement (left, right, top, bottom)
/// 
/// DETAILED SCROLL SETUP:
/// For scrollable queue display, create hierarchy:
/// - QueuePanel (this component + Button)
///   - ScrollView (ScrollRect component)
///     - Viewport (RectMask2D)
///       - Content (assigned to scrollView.content)
///         - queueContainer (assigned to this.queueContainer, has Layout Group)
/// </summary>
public class WasteCrateQueuePanel : MonoBehaviour
{
    [Header("Queue Panel Components")]
    [Tooltip("The main panel GameObject (must have Button component)")]
    public GameObject queuePanel;
    
    [Tooltip("Optional ScrollRect GameObject for scrollable content - if null, all items shown without scrolling")]
    public ScrollRect scrollView;
    
    [Tooltip("Container for queue item displays (should have Layout Group)")]
    public Transform queueContainer;
    
    [Tooltip("Prefab for displaying a single queue item (should have Image component)")]
    public GameObject queueItemPrefab;
    
    [Tooltip("Text to show when queue is empty (optional)")]
    public TextMeshProUGUI emptyQueueText;
    
    [Header("Configuration")]
    [Tooltip("Direction to arrange queue items")]
    public QueueLayoutDirection layoutDirection = QueueLayoutDirection.Left;
    
    /// <summary>
    /// Event fired when the queue panel is clicked
    /// </summary>
    public System.Action OnQueueClicked;
    
    /// <summary>
    /// Component ID for logging purposes
    /// </summary>
    private string ComponentId => $"WasteCrateQueuePanel_{GetInstanceID()}";
    
    /// <summary>
    /// List of currently instantiated queue item GameObjects
    /// </summary>
    private List<GameObject> queueItemObjects = new List<GameObject>();
    
    /// <summary>
    /// Button component (auto-detected from queuePanel)
    /// </summary>
    private Button queueButton;
    
    void Start()
    {
        // Ensure queue panel is visible and active
        if (queuePanel != null)
        {
            queuePanel.SetActive(true);
            
            queueButton = queuePanel.GetComponent<Button>();
            if (queueButton == null)
            {
                queueButton = queuePanel.GetComponentInChildren<Button>();
            }
            
            if (queueButton != null)
            {
                queueButton.onClick.AddListener(OnQueueButtonClicked);
                // Ensure button starts enabled - users should always be able to click to purchase
                queueButton.interactable = true;
                
                // CRITICAL: Ensure the button has a target graphic for raycast detection
                // Without a target graphic, the button won't receive click events
                EnsureButtonHasTargetGraphic();
            }
            else
            {
                GameLogger.LogError(LoggingManager.LogCategory.UI, 
                    "Queue panel does not have a Button component! Please add Button to the queuePanel GameObject.", 
                    ComponentId);
            }
        }
        
        // Configure scroll view if assigned
        ConfigureScrollView();
        
        // Configure layout direction
        ConfigureLayoutDirection();
        
        GameLogger.LogUI("WasteCrateQueuePanel initialized and visible", ComponentId);
    }
    
    /// <summary>
    /// Ensure the button has a target graphic (Image) with raycastTarget enabled for click detection.
    /// Unity Buttons require a Graphic component to receive pointer events.
    /// </summary>
    private void EnsureButtonHasTargetGraphic()
    {
        if (queueButton == null) return;
        
        // Check if button already has a target graphic
        if (queueButton.targetGraphic != null)
        {
            // Ensure the target graphic has raycastTarget enabled
            queueButton.targetGraphic.raycastTarget = true;
            GameLogger.LogUI($"Button has target graphic: {queueButton.targetGraphic.name}, raycastTarget enabled", ComponentId);
            return;
        }
        
        // Button doesn't have a target graphic - try to find or create an Image
        Image panelImage = queuePanel.GetComponent<Image>();
        if (panelImage == null)
        {
            // Create a transparent Image for click detection
            panelImage = queuePanel.AddComponent<Image>();
            panelImage.color = new Color(1, 1, 1, 0.01f); // Nearly transparent but still clickable
            GameLogger.LogUI("Created transparent Image on queuePanel for button click detection", ComponentId);
        }
        
        // Ensure raycast target is enabled
        panelImage.raycastTarget = true;
        
        // Assign it as the button's target graphic
        queueButton.targetGraphic = panelImage;
        
        GameLogger.LogUI("Assigned Image as button's target graphic with raycastTarget enabled", ComponentId);
    }
    
    /// <summary>
    /// Configure scroll view for queue items
    /// </summary>
    private void ConfigureScrollView()
    {
        if (scrollView == null)
        {
            GameLogger.LogUI("No ScrollRect assigned - all queue items will be shown without scrolling", ComponentId);
            return;
        }
        
        // Configure scroll direction based on layout direction
        switch (layoutDirection)
        {
            case QueueLayoutDirection.Left:
            case QueueLayoutDirection.Right:
                scrollView.horizontal = true;
                scrollView.vertical = false;
                break;
                
            case QueueLayoutDirection.Top:
            case QueueLayoutDirection.Bottom:
                scrollView.horizontal = false;
                scrollView.vertical = true;
                break;
        }
        
        // Ensure scrollView content is set to queueContainer
        if (scrollView.content == null && queueContainer != null)
        {
            scrollView.content = queueContainer as RectTransform;
            GameLogger.LogUI("Auto-assigned queueContainer to scrollView.content", ComponentId);
        }
        
        GameLogger.LogUI($"Configured ScrollRect: horizontal={scrollView.horizontal}, vertical={scrollView.vertical}", ComponentId);
    }
    
    /// <summary>
    /// Configure the layout group based on the specified direction
    /// </summary>
    private void ConfigureLayoutDirection()
    {
        if (queueContainer == null) return;
        
        // Get or add HorizontalLayoutGroup or VerticalLayoutGroup based on direction
        var horizontalLayout = queueContainer.GetComponent<HorizontalLayoutGroup>();
        var verticalLayout = queueContainer.GetComponent<VerticalLayoutGroup>();
        
        switch (layoutDirection)
        {
            case QueueLayoutDirection.Left:
            case QueueLayoutDirection.Right:
                // Use horizontal layout
                if (horizontalLayout == null)
                {
                    horizontalLayout = queueContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
                }
                
                // Remove vertical layout if exists
                if (verticalLayout != null)
                {
                    Destroy(verticalLayout);
                }
                
                // Configure horizontal layout
                horizontalLayout.childControlWidth = false;
                horizontalLayout.childControlHeight = false;
                horizontalLayout.childForceExpandWidth = false;
                horizontalLayout.childForceExpandHeight = false;
                
                // Set child alignment based on direction
                if (layoutDirection == QueueLayoutDirection.Left)
                {
                    horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
                    horizontalLayout.reverseArrangement = false;
                }
                else // Right
                {
                    horizontalLayout.childAlignment = TextAnchor.MiddleRight;
                    horizontalLayout.reverseArrangement = true;
                }
                break;
                
            case QueueLayoutDirection.Top:
            case QueueLayoutDirection.Bottom:
                // Use vertical layout
                if (verticalLayout == null)
                {
                    verticalLayout = queueContainer.gameObject.AddComponent<VerticalLayoutGroup>();
                }
                
                // Remove horizontal layout if exists
                if (horizontalLayout != null)
                {
                    Destroy(horizontalLayout);
                }
                
                // Configure vertical layout
                verticalLayout.childControlWidth = false;
                verticalLayout.childControlHeight = false;
                verticalLayout.childForceExpandWidth = false;
                verticalLayout.childForceExpandHeight = false;
                
                // Set child alignment based on direction
                if (layoutDirection == QueueLayoutDirection.Top)
                {
                    verticalLayout.childAlignment = TextAnchor.UpperCenter;
                    verticalLayout.reverseArrangement = false;
                }
                else // Bottom
                {
                    verticalLayout.childAlignment = TextAnchor.LowerCenter;
                    verticalLayout.reverseArrangement = true;
                }
                break;
        }
        
        GameLogger.LogUI($"Configured queue layout direction: {layoutDirection}", ComponentId);
    }
    
    /// <summary>
    /// Update the queue display with current queue data
    /// </summary>
    /// <param name="queuedCrateIds">List of waste crate IDs in the queue</param>
    public void UpdateQueueDisplay(List<string> queuedCrateIds)
    {
        // Ensure queue panel is visible
        if (queuePanel != null && !queuePanel.activeSelf)
        {
            queuePanel.SetActive(true);
            GameLogger.LogUI("Queue panel was inactive - activating it", ComponentId);
        }
        
        // Clear existing queue items
        ClearQueueItems();
        
        if (queuedCrateIds == null || queuedCrateIds.Count == 0)
        {
            // Show empty state
            if (emptyQueueText != null)
            {
                emptyQueueText.gameObject.SetActive(true);
                emptyQueueText.text = "Queue Empty";
            }
            
            // Ensure button remains enabled even when queue is empty
            // Users should be able to click to purchase new crates
            if (queueButton != null)
            {
                queueButton.interactable = true;
            }
            
            // Ensure queue container is active even when empty
            if (queueContainer != null)
            {
                queueContainer.gameObject.SetActive(true);
            }
            
            GameLogger.LogUI("Queue is empty - button remains clickable for purchasing", ComponentId);
            return;
        }
        
        // Hide empty text
        if (emptyQueueText != null)
        {
            emptyQueueText.gameObject.SetActive(false);
        }
        
        // Ensure button is enabled when queue has items
        if (queueButton != null)
        {
            queueButton.interactable = true;
        }
        
        // Ensure queue container is active
        if (queueContainer != null)
        {
            queueContainer.gameObject.SetActive(true);
        }
        
        // Display ALL items from the queue (ScrollRect will handle overflow)
        for (int i = 0; i < queuedCrateIds.Count; i++)
        {
            string crateId = queuedCrateIds[i];
            CreateQueueItemDisplay(crateId);
        }
        
        GameLogger.LogUI($"Updated queue display with {queuedCrateIds.Count} items", ComponentId);
    }
    
    /// <summary>
    /// Create a visual display for a single queue item
    /// </summary>
    /// <param name="crateId">ID of the waste crate to display</param>
    private void CreateQueueItemDisplay(string crateId)
    {
        if (queueItemPrefab == null || queueContainer == null)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, "Queue item prefab or container not assigned", ComponentId);
            return;
        }
        
        // Instantiate the queue item
        GameObject itemObj = Instantiate(queueItemPrefab, queueContainer);
        queueItemObjects.Add(itemObj);
        
        // Get ALL Image components (including children) to disable raycast blocking
        Image[] allImages = itemObj.GetComponentsInChildren<Image>(true);
        
        // Get the main Image component to set the sprite
        Image iconImage = itemObj.GetComponent<Image>();
        if (iconImage == null)
        {
            iconImage = itemObj.GetComponentInChildren<Image>();
        }
        
        if (iconImage != null)
        {
            // Load the crate definition and sprite
            var crateDef = FactoryRegistry.Instance?.GetWasteCrate(crateId);
            if (crateDef != null && !string.IsNullOrEmpty(crateDef.sprite))
            {
                var sprite = Resources.Load<Sprite>($"Sprites/Waste/{crateDef.sprite}");
                if (sprite != null)
                {
                    iconImage.sprite = sprite;
                    itemObj.SetActive(true);
                }
                else
                {
                    GameLogger.LogWarning(LoggingManager.LogCategory.UI, $"Could not load sprite '{crateDef.sprite}' for queue item", ComponentId);
                    itemObj.SetActive(false);
                }
            }
            else
            {
                GameLogger.LogWarning(LoggingManager.LogCategory.UI, $"Could not find waste crate definition for '{crateId}'", ComponentId);
                itemObj.SetActive(false);
            }
        }
        
        // CRITICAL FIX: Disable raycast on ALL Image components to prevent blocking button clicks
        // This ensures the parent button receives click events even when queue items are present
        foreach (var image in allImages)
        {
            image.raycastTarget = false;
        }
        
        if (allImages.Length > 0)
        {
            GameLogger.LogUI($"Disabled raycast blocking on {allImages.Length} Image component(s) for queue item '{crateId}'", ComponentId);
        }
    }
    
    /// <summary>
    /// Clear all queue item displays
    /// </summary>
    private void ClearQueueItems()
    {
        foreach (var item in queueItemObjects)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        queueItemObjects.Clear();
    }
    
    /// <summary>
    /// Called when the queue button is clicked
    /// </summary>
    private void OnQueueButtonClicked()
    {
        GameLogger.LogUI("Queue panel clicked", ComponentId);
        OnQueueClicked?.Invoke();
    }
    
    /// <summary>
    /// Show the queue panel
    /// </summary>
    public void ShowPanel()
    {
        if (queuePanel != null)
        {
            queuePanel.SetActive(true);
        }
    }
    
    /// <summary>
    /// Hide the queue panel
    /// </summary>
    public void HidePanel()
    {
        if (queuePanel != null)
        {
            queuePanel.SetActive(false);
        }
    }
    
    void OnDestroy()
    {
        ClearQueueItems();
    }
}
