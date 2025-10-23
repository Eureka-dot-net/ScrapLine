using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Controls the global "Expand Mode" state for the grid expansion feature.
/// Manages mode toggle, events, and visual feedback (dim overlay).
/// </summary>
public class ExpandModeController : MonoBehaviour
{
    [Header("Visual Feedback")]
    [Tooltip("Dim overlay shown when expand mode is active")]
    public Image dimOverlay;

    [Tooltip("Alpha value for dim overlay (0-1)")]
    [UnityEngine.Range(0f, 1f)]
    public float dimAlpha = 0.4f;

    [Header("Debug")]
    [Tooltip("Enable debug logs for expand mode operations")]
    public bool enableExpandModeLogs = true;

    // Current mode state
    private bool isExpandModeActive = false;

    /// <summary>
    /// Event fired when expand mode is enabled
    /// </summary>
    public event Action OnExpandModeEnabled;

    /// <summary>
    /// Event fired when expand mode is disabled
    /// </summary>
    public event Action OnExpandModeDisabled;

    /// <summary>
    /// Check if expand mode is currently active
    /// </summary>
    public bool IsActive => isExpandModeActive;

    private string ComponentId => $"ExpandModeController_{GetInstanceID()}";

    private void Awake()
    {
        // Ensure dim overlay starts hidden
        if (dimOverlay != null)
        {
            dimOverlay.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Toggle expand mode on/off
    /// </summary>
    public void ToggleExpandMode()
    {
        if (isExpandModeActive)
        {
            DisableExpandMode();
        }
        else
        {
            EnableExpandMode();
        }
    }

    /// <summary>
    /// Enable expand mode
    /// </summary>
    public void EnableExpandMode()
    {
        if (isExpandModeActive)
        {
            if (enableExpandModeLogs)
                GameLogger.LogGrid("Expand mode already active", ComponentId);
            return;
        }

        isExpandModeActive = true;

        // Show dim overlay
        ShowDimOverlay();

        // Notify listeners
        OnExpandModeEnabled?.Invoke();

        if (enableExpandModeLogs)
            GameLogger.LogGrid("Expand mode ENABLED", ComponentId);
    }

    /// <summary>
    /// Disable expand mode
    /// </summary>
    public void DisableExpandMode()
    {
        if (!isExpandModeActive)
        {
            if (enableExpandModeLogs)
                GameLogger.LogGrid("Expand mode already inactive", ComponentId);
            return;
        }

        isExpandModeActive = false;

        // Hide dim overlay
        HideDimOverlay();

        // Notify listeners
        OnExpandModeDisabled?.Invoke();

        if (enableExpandModeLogs)
            GameLogger.LogGrid("Expand mode DISABLED", ComponentId);
    }

    /// <summary>
    /// Show the dim overlay
    /// </summary>
    private void ShowDimOverlay()
    {
        if (dimOverlay == null)
        {
            if (enableExpandModeLogs)
                GameLogger.LogWarning(LoggingManager.LogCategory.Grid, "Dim overlay not assigned", ComponentId);
            return;
        }

        // Set alpha
        Color color = dimOverlay.color;
        color.a = dimAlpha;
        dimOverlay.color = color;

        // Show overlay
        dimOverlay.gameObject.SetActive(true);

        // Make overlay non-interactive (blocks clicks to grid)
        dimOverlay.raycastTarget = true;
    }

    /// <summary>
    /// Hide the dim overlay
    /// </summary>
    private void HideDimOverlay()
    {
        if (dimOverlay == null) return;

        dimOverlay.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        // Clean up events
        OnExpandModeEnabled = null;
        OnExpandModeDisabled = null;
    }
}

/*
 * UNITY WIRING INSTRUCTIONS:
 * 
 * 1. Add this component to the "ExpandModeSystem" GameObject (same as GridExpansionService)
 * 2. Create a full-screen UI Image for the dim overlay:
 *    - In the Canvas, create: UI > Image
 *    - Name it "DimOverlay"
 *    - Set RectTransform: Anchor to stretch-stretch, offsets all 0 (full screen)
 *    - Set color: Black (R:0, G:0, B:0, A:100) - alpha will be adjusted by script
 *    - Set Image raycastTarget: true (to block grid clicks)
 *    - Disable the GameObject by default (script will enable it)
 *    - Place it as a sibling ABOVE the grid container (higher in hierarchy = on top)
 * 3. Drag the DimOverlay Image into the "Dim Overlay" field in Inspector
 * 4. Configure:
 *    - Dim Alpha: 0.4 (40% opacity)
 *    - Enable Expand Mode Logs: true (for debugging)
 * 5. This controller will be referenced by:
 *    - ManageTabButtonBinder (for toggle button)
 *    - GridMarkersView (for showing/hiding markers)
 */
