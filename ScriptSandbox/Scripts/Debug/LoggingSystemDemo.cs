using UnityEngine;

/// <summary>
/// Demonstration script showing how to use the new central logging system.
/// This script provides examples of proper logging usage patterns and state change detection.
/// </summary>
public class LoggingSystemDemo : MonoBehaviour
{
    [Header("Demo Configuration")]
    [Tooltip("Enable demo logging to see the system in action")]
    public bool enableDemo = true;
    
    [Tooltip("Interval between demo log messages")]
    public float demoInterval = 2f;
    
    private float lastDemoTime;
    private int demoStep = 0;
    private string ComponentId => $"LoggingDemo_{GetInstanceID()}";

    void Start()
    {
        if (enableDemo)
        {
            // Ensure LoggingManager is available
            if (LoggingManager.Instance == null)
            {
                // Create LoggingManager if it doesn't exist
                GameObject loggingManagerObject = new GameObject("LoggingManager");
                loggingManagerObject.AddComponent<LoggingManager>();
                DontDestroyOnLoad(loggingManagerObject);
                
                Debug.Log("LoggingManager created for demo");
            }
            
            DemonstrateLoggingFeatures();
        }
    }

    void Update()
    {
        if (enableDemo && Time.time - lastDemoTime >= demoInterval)
        {
            DemonstrateStateChangeDetection();
            lastDemoTime = Time.time;
        }
    }

    private void DemonstrateLoggingFeatures()
    {
        GameLogger.LogDebug("=== Central Logging System Demo Started ===", ComponentId);
        
        // Demonstrate category-specific logging
        GameLogger.LogMovement("Item moved from conveyor to processor", "Conveyor_2_3");
        GameLogger.LogFabricator("Starting recipe processing for reinforced aluminum", "Fabricator_4_5");
        GameLogger.LogProcessor("Shredding aluminum can into pieces", "Shredder_1_2");
        GameLogger.LogGrid("Cell placement validated at coordinates", "GridManager_12345");
        GameLogger.LogMachine("Machine successfully upgraded to level 2", "Machine_3_4");
        GameLogger.LogEconomy("Player earned 150 credits from sale", "SellerMachine_5_1");
        
        // Demonstrate different log levels
        GameLogger.LogWarning(LoggingManager.LogCategory.SaveLoad, "Save file is getting large", ComponentId);
        GameLogger.LogError(LoggingManager.LogCategory.UI, "Button interaction failed", ComponentId);
        
        // Demonstrate category checking for performance
        if (GameLogger.IsCategoryEnabled(LoggingManager.LogCategory.Movement))
        {
            string expensiveReport = GenerateMovementReport();
            GameLogger.LogMovement(expensiveReport, ComponentId);
        }
        
        GameLogger.LogDebug("Basic logging features demonstrated", ComponentId);
    }

    private void DemonstrateStateChangeDetection()
    {
        demoStep++;
        
        switch (demoStep % 6)
        {
            case 1:
                // This message will show
                GameLogger.LogFabricator("Processing step 1: Collecting ingredients", ComponentId);
                break;
                
            case 2:
                // This identical message will be suppressed
                GameLogger.LogFabricator("Processing step 1: Collecting ingredients", ComponentId);
                break;
                
            case 3:
                // Notify state change - next identical message will now show
                GameLogger.NotifyStateChange(ComponentId);
                GameLogger.LogFabricator("Processing step 1: Collecting ingredients", ComponentId);
                break;
                
            case 4:
                // Different message will always show
                GameLogger.LogFabricator("Processing step 2: Combining materials", ComponentId);
                break;
                
            case 5:
                // Show category enable/disable
                GameLogger.SetCategoryEnabled(LoggingManager.LogCategory.Fabricator, false);
                GameLogger.LogFabricator("This message will NOT appear", ComponentId);
                break;
                
            case 0:
                // Re-enable category
                GameLogger.SetCategoryEnabled(LoggingManager.LogCategory.Fabricator, true);
                GameLogger.LogFabricator("Fabricator logging re-enabled", ComponentId);
                break;
        }
    }

    private string GenerateMovementReport()
    {
        // Simulate expensive string generation that should only happen if logging is enabled
        return $"Movement Report: {UnityEngine.Random.Range(1, 100)} items processed, {UnityEngine.Random.Range(0, 10)} items queued";
    }

    /// <summary>
    /// Demonstrate proper component ID patterns for different types of components
    /// </summary>
    public void DemonstrateComponentIdPatterns()
    {
        // Machine components: "MachineType_X_Y"
        // Example IDs that would be used in actual game scenarios
        var fabricatorPosition = new { x = 5, y = 3 };
        var conveyorPosition = new { x = 2, y = 1 };
        
        // Manager components: "ManagerName_InstanceId"
        string gridManagerId = $"GridManager_{GetInstanceID()}";
        string saveManagerId = $"SaveLoadManager_{GetInstanceID()}";
        
        // UI components: "UIComponent_SpecificId"
        string buttonId = $"MachineButton_shredder";
        string uiPanelId = $"ConfigPanel_fabricator";
        
        GameLogger.LogDebug("Component ID patterns demonstrated", ComponentId);
    }

    [ContextMenu("Test All Logging Categories")]
    public void TestAllCategories()
    {
        GameLogger.LogMovement("Movement system test", ComponentId);
        GameLogger.LogFabricator("Fabricator system test", ComponentId);
        GameLogger.LogProcessor("Processor system test", ComponentId);
        GameLogger.LogGrid("Grid system test", ComponentId);
        GameLogger.LogUI("UI system test", ComponentId);
        GameLogger.LogSaveLoad("Save/Load system test", ComponentId);
        GameLogger.LogMachine("Machine system test", ComponentId);
        GameLogger.LogEconomy("Economy system test", ComponentId);
        GameLogger.LogSpawning("Spawning system test", ComponentId);
        GameLogger.LogSelling("Selling system test", ComponentId);
        GameLogger.LogDebug("Debug system test", ComponentId);
    }
}