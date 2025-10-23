using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Binds a button in the Manage tab to toggle grid expand mode.
/// Updates button visual state when expand mode is active/inactive.
/// </summary>
public class ManageTabButtonBinder : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the expand mode controller")]
    public ExpandModeController expandModeController;

    [Tooltip("The button that toggles expand mode (typically a '+' button)")]
    public Button expandToggleButton;

    [Header("Visual Feedback")]
    [Tooltip("Image component to change color when mode is active")]
    public Image buttonImage;

    [Tooltip("Color when expand mode is inactive")]
    public Color inactiveColor = new Color(1f, 0.6f, 0.2f, 1f); // Orange

    [Tooltip("Color when expand mode is active")]
    public Color activeColor = new Color(0.2f, 1f, 0.2f, 1f); // Green

    [Header("Debug")]
    [Tooltip("Enable debug logs for button operations")]
    public bool enableButtonLogs = true;

    private string ComponentId => $"ManageTabButtonBinder_{GetInstanceID()}";

    private void Awake()
    {
        // Setup button click listener
        if (expandToggleButton != null)
        {
            expandToggleButton.onClick.AddListener(OnExpandToggleClicked);
        }
        else
        {
            GameLogger.LogError(LoggingManager.LogCategory.UI, "Expand toggle button not assigned", ComponentId);
        }

        // Subscribe to expand mode events
        if (expandModeController != null)
        {
            expandModeController.OnExpandModeEnabled += OnExpandModeEnabled;
            expandModeController.OnExpandModeDisabled += OnExpandModeDisabled;
        }
        else
        {
            GameLogger.LogError(LoggingManager.LogCategory.UI, "Expand mode controller not assigned", ComponentId);
        }

        // Set initial visual state
        UpdateButtonVisual(false);
    }

    /// <summary>
    /// Handle expand toggle button click
    /// </summary>
    private void OnExpandToggleClicked()
    {
        if (expandModeController == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.UI, "Cannot toggle expand mode - controller not assigned", ComponentId);
            return;
        }

        if (enableButtonLogs)
            GameLogger.Log(LoggingManager.LogCategory.UI, "Expand toggle button clicked", ComponentId);

        expandModeController.ToggleExpandMode();
    }

    /// <summary>
    /// Handle expand mode enabled event
    /// </summary>
    private void OnExpandModeEnabled()
    {
        UpdateButtonVisual(true);

        if (enableButtonLogs)
            GameLogger.Log(LoggingManager.LogCategory.UI, "Button visual updated: expand mode active", ComponentId);
    }

    /// <summary>
    /// Handle expand mode disabled event
    /// </summary>
    private void OnExpandModeDisabled()
    {
        UpdateButtonVisual(false);

        if (enableButtonLogs)
            GameLogger.Log(LoggingManager.LogCategory.UI, "Button visual updated: expand mode inactive", ComponentId);
    }

    /// <summary>
    /// Update button visual state
    /// </summary>
    /// <param name="isActive">Whether expand mode is active</param>
    private void UpdateButtonVisual(bool isActive)
    {
        if (buttonImage == null) return;

        buttonImage.color = isActive ? activeColor : inactiveColor;
    }

    private void OnDestroy()
    {
        // Clean up button listener
        if (expandToggleButton != null)
        {
            expandToggleButton.onClick.RemoveListener(OnExpandToggleClicked);
        }

        // Unsubscribe from events
        if (expandModeController != null)
        {
            expandModeController.OnExpandModeEnabled -= OnExpandModeEnabled;
            expandModeController.OnExpandModeDisabled -= OnExpandModeDisabled;
        }
    }
}

/*
 * UNITY WIRING INSTRUCTIONS:
 * 
 * 1. Locate or create the Manage tab in your UI:
 *    - Find the "Manage" tab panel/container in your Canvas
 *    - This is where machine management and grid controls live
 * 
 * 2. Create the expand toggle button:
 *    - Create: UI > Button
 *    - Name it "ExpandToggleButton"
 *    - Position it appropriately in the Manage tab (e.g., top-right corner)
 *    - Size: 50x50 to 70x70 (square button)
 *    - Icon: Use a "+" (plus) icon sprite
 *      - Create or import a plus icon sprite
 *      - Set button's Image component to use the plus icon
 *      - Style: Orange/yellow color, rounded square background
 * 
 * 3. Add this ManageTabButtonBinder component to the button:
 *    - Select the ExpandToggleButton GameObject
 *    - Add Component > ManageTabButtonBinder
 * 
 * 4. Assign references in Inspector:
 *    - Expand Mode Controller: Drag the ExpandModeController component from ExpandModeSystem
 *    - Expand Toggle Button: Drag the button itself (or use GetComponent if on same GameObject)
 *    - Button Image: Drag the Image component from the button (usually the background)
 * 
 * 5. Configure visual feedback colors:
 *    - Inactive Color: Orange (1.0, 0.6, 0.2, 1.0) - default state
 *    - Active Color: Green (0.2, 1.0, 0.2, 1.0) - when expand mode is on
 *    - Adjust colors to match your game's UI theme
 * 
 * 6. Configure debug:
 *    - Enable Button Logs: true (for debugging)
 * 
 * 7. Button styling recommendations:
 *    - Add button hover effect (scale or brightness change)
 *    - Add button press animation (scale down slightly)
 *    - Consider adding a glow effect when active
 *    - Use ButtonHoverOutline component if available
 * 
 * 8. Optional enhancements:
 *    - Add a tooltip: "Expand Grid" on hover
 *    - Add a badge or indicator when available
 *    - Add animation when toggling (rotation, pulse)
 *    - Add sound effect on click
 * 
 * ALTERNATIVE SETUP (if button is separate):
 * - If the button is NOT the GameObject with this component:
 *   1. Create an empty GameObject "ExpandToggleButtonManager"
 *   2. Add ManageTabButtonBinder to the manager
 *   3. Drag the button GameObject into "Expand Toggle Button" field
 */
