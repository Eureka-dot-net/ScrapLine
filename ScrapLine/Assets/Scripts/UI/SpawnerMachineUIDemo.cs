using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Demonstration script showing how to use the new SpawnerMachine event system
/// with SpawnerMachineUI for waste crate visualization.
/// 
/// UNITY SETUP INSTRUCTIONS:
/// 1. Create a GameObject in your scene and attach this script
/// 2. Create a UI Canvas if not already present
/// 3. Create a UI Image GameObject for the waste crate display
/// 4. Assign sprites for empty, low, and full states
/// 5. Set up references in the Inspector
/// 
/// This demonstrates the event-driven architecture where SpawnerMachine emits
/// events and SpawnerMachineUI responds without tight coupling.
/// </summary>
public class SpawnerMachineUIDemo : MonoBehaviour
{
    [Header("Demo Setup")]
    [Tooltip("The UI Image that will show the waste crate state")]
    public Image wasteCrateDisplay;
    
    [Tooltip("Sprites for different waste crate states")]
    public Sprite emptySprite;
    public Sprite lowSprite; 
    public Sprite fullSprite;
    
    [Header("Demo Configuration")]
    [Tooltip("Threshold for low/full state (default: 10)")]
    public int lowThreshold = 10;
    
    private SpawnerMachineUI spawnerUI;
    private SpawnerMachine demoSpawner;
    
    /// <summary>
    /// Get the component ID for logging purposes
    /// </summary>
    private string ComponentId => $"SpawnerMachineUIDemo_{GetInstanceID()}";
    
    void Start()
    {
        SetupDemo();
    }
    
    /// <summary>
    /// Sets up the demonstration of SpawnerMachine events and UI
    /// </summary>
    private void SetupDemo()
    {
        if (wasteCrateDisplay == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.UI, "Waste crate display Image not assigned!", ComponentId);
            return;
        }
        
        // Create demo spawner machine
        var cellData = new CellData { x = 0, y = 0 };
        var machineDef = new MachineDef { id = "demo_spawner", baseProcessTime = 3.0f };
        demoSpawner = new SpawnerMachine(cellData, machineDef);
        
        // Create and configure SpawnerMachineUI
        spawnerUI = gameObject.AddComponent<SpawnerMachineUI>();
        spawnerUI.spawnerMachine = demoSpawner;
        spawnerUI.wasteCrateImage = wasteCrateDisplay;
        spawnerUI.emptySprite = emptySprite;
        spawnerUI.lowSprite = lowSprite;
        spawnerUI.fullSprite = fullSprite;
        spawnerUI.lowThreshold = lowThreshold;
        
        GameLogger.LogUI("SpawnerMachine UI Demo setup complete", ComponentId);
        
        // The SpawnerMachineUI will automatically subscribe to events and update the display
        // as the waste crate count changes during spawner operation
    }
    
    /// <summary>
    /// Demo method to manually trigger waste crate count change (for testing)
    /// </summary>
    [ContextMenu("Test UI Update")]
    public void TestUIUpdate()
    {
        if (demoSpawner != null)
        {
            // Manually trigger the event to test UI response
            demoSpawner.WasteCrateCountChanged?.Invoke();
            GameLogger.LogUI("Manual waste crate count change triggered", ComponentId);
        }
    }
    
    /// <summary>
    /// Display current waste count in console (for debugging)
    /// </summary>
    [ContextMenu("Show Waste Count")]
    public void ShowWasteCount()
    {
        if (demoSpawner != null)
        {
            int count = demoSpawner.GetRemainingWasteCount();
            GameLogger.LogUI($"Current waste count: {count}", ComponentId);
        }
    }
    
    void OnDestroy()
    {
        // Cleanup is handled automatically by SpawnerMachineUI.OnDestroy()
        GameLogger.LogUI("SpawnerMachine UI Demo destroyed", ComponentId);
    }
}