using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages all UI configuration panels in the game.
/// Ensures panels are properly initialized at game start and manages panel visibility.
/// Only one configuration panel can be shown at a time.
/// 
/// This manager solves the issue where config panels sharing the same GameObject
/// would not get their Start() methods called when the GameObject was initially inactive.
/// </summary>
public class UIPanelManager : MonoBehaviour
{
    [Header("Config Panel References")]
    [Tooltip("All configuration panels in the scene")]
    public List<MonoBehaviour> configPanels = new List<MonoBehaviour>();

    // Track currently open panel
    private MonoBehaviour currentOpenPanel;

    // Component ID for logging
    private string ComponentId => $"UIPanelManager_{GetInstanceID()}";

    /// <summary>
    /// Initialize in Awake to run before any Start() methods
    /// This ensures panels are initialized even if their GameObjects start inactive
    /// </summary>
    private void Awake()
    {
        InitializeAllPanels();
        GameLogger.Log(LoggingManager.LogCategory.UI, $"UIPanelManager initialized with {configPanels.Count} panels", ComponentId);
    }

    /// <summary>
    /// Initialize all config panels to hidden state
    /// Calls the initialization method on each panel regardless of GameObject active state
    /// Made internal for testing purposes
    /// </summary>
    internal void InitializeAllPanels()
    {
        // Auto-discover config panels if not manually assigned
        if (configPanels.Count == 0)
        {
            DiscoverConfigPanels();
        }

        foreach (var panel in configPanels)
        {
            if (panel == null) continue;

            // Call the Initialize method via reflection to set initial hidden state
            // This works even if the panel's GameObject is inactive
            var initMethod = panel.GetType().GetMethod("InitializePanelState", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (initMethod != null)
            {
                initMethod.Invoke(panel, null);
                GameLogger.Log(LoggingManager.LogCategory.UI, $"Initialized panel: {panel.GetType().Name}", ComponentId);
            }
            else
            {
                GameLogger.LogWarning(LoggingManager.LogCategory.UI, 
                    $"Panel {panel.GetType().Name} does not have InitializePanelState method", ComponentId);
            }
        }
    }

    /// <summary>
    /// Auto-discover all config panels in the scene
    /// </summary>
    private void DiscoverConfigPanels()
    {
        GameLogger.Log(LoggingManager.LogCategory.UI, "Auto-discovering config panels...", ComponentId);

        // Find all types of config panels
        var sortingPanels = FindObjectsByType<SortingMachineConfigPanel>(FindObjectsSortMode.None);
        var fabricatorPanels = FindObjectsByType<FabricatorMachineConfigPanel>(FindObjectsSortMode.None);
        var wasteCratePanels = FindObjectsByType<WasteCrateConfigPanel>(FindObjectsSortMode.None);
        var spawnerPanels = FindObjectsByType<SpawnerConfigPanel>(FindObjectsSortMode.None);

        // Add to list
        foreach (var panel in sortingPanels)
            configPanels.Add(panel);
        
        foreach (var panel in fabricatorPanels)
            configPanels.Add(panel);
        
        foreach (var panel in wasteCratePanels)
            configPanels.Add(panel);
        
        foreach (var panel in spawnerPanels)
            configPanels.Add(panel);

        GameLogger.Log(LoggingManager.LogCategory.UI, $"Discovered {configPanels.Count} config panels", ComponentId);
    }

    /// <summary>
    /// Register a panel as currently open and close any other open panels
    /// </summary>
    /// <param name="panel">The panel being opened</param>
    public void RegisterOpenPanel(MonoBehaviour panel)
    {
        if (currentOpenPanel != null && currentOpenPanel != panel)
        {
            ClosePanel(currentOpenPanel);
        }

        currentOpenPanel = panel;
        GameLogger.Log(LoggingManager.LogCategory.UI, $"Registered open panel: {panel?.GetType().Name}", ComponentId);
    }

    /// <summary>
    /// Unregister a panel when it closes
    /// </summary>
    /// <param name="panel">The panel being closed</param>
    public void UnregisterPanel(MonoBehaviour panel)
    {
        if (currentOpenPanel == panel)
        {
            currentOpenPanel = null;
            GameLogger.Log(LoggingManager.LogCategory.UI, $"Unregistered panel: {panel?.GetType().Name}", ComponentId);
        }
    }

    /// <summary>
    /// Close the currently open panel if any
    /// </summary>
    public void CloseCurrentPanel()
    {
        if (currentOpenPanel != null)
        {
            ClosePanel(currentOpenPanel);
        }
    }

    /// <summary>
    /// Close a specific panel by calling its HideConfiguration method
    /// </summary>
    /// <param name="panel">Panel to close</param>
    private void ClosePanel(MonoBehaviour panel)
    {
        if (panel == null) return;

        var hideMethod = panel.GetType().GetMethod("HideConfiguration",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (hideMethod != null)
        {
            hideMethod.Invoke(panel, null);
            GameLogger.Log(LoggingManager.LogCategory.UI, $"Closed panel: {panel.GetType().Name}", ComponentId);
        }
    }

    /// <summary>
    /// Get the currently open panel
    /// </summary>
    /// <returns>Currently open panel or null</returns>
    public MonoBehaviour GetCurrentOpenPanel()
    {
        return currentOpenPanel;
    }
}
