using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Reusable panel for displaying queued waste crates.
/// Shows up to a configurable number of waste crates from the queue with their icons.
/// Can be clicked to open the WasteCrateConfigPanel for purchasing.
/// 
/// UNITY SETUP REQUIRED:
/// 1. Create UI Panel GameObject with this component
/// 2. Assign queuePanel (the panel GameObject itself)
/// 3. Assign queueButton (button that covers the panel for click detection)
/// 4. Assign queueContainer (parent for queue item displays)
/// 5. Assign queueItemPrefab (prefab with Image component for crate icon)
/// 6. Optionally assign emptyQueueText to show when queue is empty
/// </summary>
public class WasteCrateQueuePanel : MonoBehaviour
{
    [Header("Queue Panel Components")]
    [Tooltip("The main panel GameObject")]
    public GameObject queuePanel;
    
    [Tooltip("Button covering the panel for click detection")]
    public Button queueButton;
    
    [Tooltip("Container for queue item displays (should have Layout Group)")]
    public Transform queueContainer;
    
    [Tooltip("Prefab for displaying a single queue item (should have Image component)")]
    public GameObject queueItemPrefab;
    
    [Tooltip("Text to show when queue is empty (optional)")]
    public TextMeshProUGUI emptyQueueText;
    
    [Header("Configuration")]
    [Tooltip("Maximum number of queue items to display (default: 3)")]
    public int maxDisplayItems = 3;
    
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
    
    void Start()
    {
        if (queueButton != null)
        {
            queueButton.onClick.AddListener(OnQueueButtonClicked);
        }
    }
    
    /// <summary>
    /// Update the queue display with current queue data
    /// </summary>
    /// <param name="queuedCrateIds">List of waste crate IDs in the queue</param>
    public void UpdateQueueDisplay(List<string> queuedCrateIds)
    {
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
            return;
        }
        
        // Hide empty text
        if (emptyQueueText != null)
        {
            emptyQueueText.gameObject.SetActive(false);
        }
        
        // Display up to maxDisplayItems from the queue
        int displayCount = Mathf.Min(queuedCrateIds.Count, maxDisplayItems);
        for (int i = 0; i < displayCount; i++)
        {
            string crateId = queuedCrateIds[i];
            CreateQueueItemDisplay(crateId);
        }
        
        GameLogger.LogUI($"Updated queue display with {displayCount} items", ComponentId);
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
        
        // Get the Image component to set the sprite
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
