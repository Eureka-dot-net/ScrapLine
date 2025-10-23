using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Small reusable confirmation UI for grid expansion costs.
/// Shows cost and provides confirm/cancel options.
/// </summary>
public class ExpansionCostPrompt : MonoBehaviour
{
    [Header("UI Components")]
    [Tooltip("Main panel GameObject to show/hide")]
    public GameObject promptPanel;

    [Tooltip("Text component to display the cost")]
    public TextMeshProUGUI costText;

    [Tooltip("Confirm button")]
    public Button confirmButton;

    [Tooltip("Cancel button")]
    public Button cancelButton;

    [Header("Text Format")]
    [Tooltip("Text format for cost display. Use {0} for cost value")]
    public string costTextFormat = "Expand here for {0} credits?";

    [Header("Debug")]
    [Tooltip("Enable debug logs for prompt operations")]
    public bool enablePromptLogs = true;

    // Current callbacks
    private Action onConfirmCallback;
    private Action onCancelCallback;

    private string ComponentId => $"ExpansionCostPrompt_{GetInstanceID()}";

    private void Awake()
    {
        // Setup button listeners
        if (confirmButton != null)
            confirmButton.onClick.AddListener(HandleConfirm);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(HandleCancel);

        // Start hidden
        if (promptPanel != null)
            promptPanel.SetActive(false);
    }

    /// <summary>
    /// Show the prompt with specified cost and callbacks
    /// </summary>
    /// <param name="cost">Cost to display</param>
    /// <param name="onConfirm">Callback when confirmed</param>
    /// <param name="onCancel">Callback when cancelled</param>
    public void Show(int cost, Action onConfirm, Action onCancel)
    {
        if (promptPanel == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.UI, "Cannot show prompt - prompt panel not assigned", ComponentId);
            return;
        }

        // Store callbacks
        onConfirmCallback = onConfirm;
        onCancelCallback = onCancel;

        // Update cost text
        if (costText != null)
        {
            costText.text = string.Format(costTextFormat, cost);
        }

        // Show panel
        promptPanel.SetActive(true);

        if (enablePromptLogs)
            GameLogger.Log(LoggingManager.LogCategory.UI, $"Showing expansion cost prompt: {cost} credits", ComponentId);
    }

    /// <summary>
    /// Hide the prompt without invoking callbacks
    /// </summary>
    public void Hide()
    {
        if (promptPanel != null)
            promptPanel.SetActive(false);

        // Clear callbacks
        onConfirmCallback = null;
        onCancelCallback = null;

        if (enablePromptLogs)
            GameLogger.Log(LoggingManager.LogCategory.UI, "Hiding expansion cost prompt", ComponentId);
    }

    /// <summary>
    /// Handle confirm button click
    /// </summary>
    private void HandleConfirm()
    {
        if (enablePromptLogs)
            GameLogger.Log(LoggingManager.LogCategory.UI, "Expansion confirmed by user", ComponentId);

        // Invoke callback
        onConfirmCallback?.Invoke();

        // Hide prompt
        Hide();
    }

    /// <summary>
    /// Handle cancel button click
    /// </summary>
    private void HandleCancel()
    {
        if (enablePromptLogs)
            GameLogger.Log(LoggingManager.LogCategory.UI, "Expansion cancelled by user", ComponentId);

        // Invoke callback
        onCancelCallback?.Invoke();

        // Hide prompt
        Hide();
    }

    private void OnDestroy()
    {
        // Clean up button listeners
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(HandleConfirm);

        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(HandleCancel);

        // Clear callbacks
        onConfirmCallback = null;
        onCancelCallback = null;
    }
}

/*
 * UNITY WIRING INSTRUCTIONS:
 * 
 * 1. Create a UI Panel prefab named "ExpansionCostPrompt":
 *    - Create: UI > Panel (or use existing prompt panel)
 *    - Name it "ExpansionCostPrompt"
 *    - Size: Width 300-400, Height 150-200 (compact prompt)
 *    - Anchor: Center-center (so it appears centered)
 *    - Background: Semi-transparent dark panel (to stand out)
 * 
 * 2. Add child elements to the panel:
 *    a) Text (TMP) - "CostText":
 *       - Position: Near top of panel
 *       - Font size: 18-24
 *       - Alignment: Center
 *       - Color: White or bright color
 *       - Text: "Expand here for 100 credits?" (example, will be replaced by script)
 *    
 *    b) Button - "ConfirmButton":
 *       - Position: Bottom left or center-left
 *       - Size: 120x40
 *       - Text: "Confirm" or "Yes" or checkmark icon
 *       - Color: Green or positive color
 *    
 *    c) Button - "CancelButton":
 *       - Position: Bottom right or center-right
 *       - Size: 120x40
 *       - Text: "Cancel" or "No" or X icon
 *       - Color: Red or neutral color
 * 
 * 3. Add this ExpansionCostPrompt component to the panel GameObject
 * 4. Assign references in Inspector:
 *    - Prompt Panel: The panel GameObject itself (drag and drop)
 *    - Cost Text: The TextMeshProUGUI component (drag CostText)
 *    - Confirm Button: The confirm button (drag ConfirmButton)
 *    - Cancel Button: The cancel button (drag CancelButton)
 * 
 * 5. Configure text format:
 *    - Cost Text Format: "Expand here for {0} credits?" (or customize)
 *    - {0} will be replaced with the actual cost value
 * 
 * 6. Set initial state:
 *    - Disable the prompt panel GameObject by default (unchecked in hierarchy)
 *    - Script will enable it when Show() is called
 * 
 * 7. Place in scene:
 *    - Add as child of Canvas (at root level or in UI layer)
 *    - Set high sibling index (near end) so it appears on top
 *    - Consider adding to a dedicated "Popups" container in Canvas
 * 
 * 8. Optional enhancements:
 *    - Add a CanvasGroup for fade in/out effects
 *    - Add an Image as semi-transparent background blocker (full screen, behind panel)
 *    - Add button hover effects (scale, color change)
 *    - Add sound effects to button clicks
 */
