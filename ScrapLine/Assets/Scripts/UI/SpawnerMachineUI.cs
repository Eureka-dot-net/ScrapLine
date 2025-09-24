using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI component that displays the waste crate state for SpawnerMachine instances.
/// This component listens to WasteCrateCountChanged events and updates the assigned 
/// Image component to show one of three states: empty, low, or full.
/// 
/// UNITY SETUP REQUIRED:
/// 1. Add this component to a GameObject in the scene
/// 2. Assign the SpawnerMachine reference in the inspector  
/// 3. Assign the UI Image component that will display the state
/// 4. Assign the three Sprite assets for empty, low, and full states
/// 5. Configure thresholds if different from defaults (lowThreshold=10, fullThreshold=20)
/// </summary>
public class SpawnerMachineUI : MonoBehaviour
{
    [Header("Required References")]
    [Tooltip("The spawner machine to monitor for waste crate changes")]
    public SpawnerMachine spawnerMachine;
    
    [Tooltip("The UI Image component that will display the waste crate state")]
    public Image wasteCrateImage;
    
    [Header("State Sprites")]
    [Tooltip("Sprite to display when waste crate is empty (0 items)")]
    public Sprite emptySprite;
    
    [Tooltip("Sprite to display when waste crate has low items (1 to lowThreshold)")]
    public Sprite lowSprite;
    
    [Tooltip("Sprite to display when waste crate has many items (above lowThreshold)")]
    public Sprite fullSprite;
    
    [Header("Configuration")]
    [Tooltip("Items count threshold below which crate is considered 'low' (default: 10)")]
    [UnityEngine.Range(1, 50)]
    public int lowThreshold = 10;
    
    /// <summary>
    /// Get the component ID for logging purposes
    /// </summary>
    private string ComponentId => $"SpawnerMachineUI_{GetInstanceID()}";
    
    void Start()
    {
        // Validate required references
        if (spawnerMachine == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.UI, "SpawnerMachine reference not assigned!", ComponentId);
            return;
        }
        
        if (wasteCrateImage == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.UI, "WasteCrateImage reference not assigned!", ComponentId);
            return;
        }
        
        if (emptySprite == null || lowSprite == null || fullSprite == null)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, "One or more state sprites not assigned - UI may not display correctly", ComponentId);
        }
        
        // Subscribe to waste crate count change events
        spawnerMachine.WasteCrateCountChanged += OnWasteCrateCountChanged;
        
        // Initialize UI with current state
        UpdateWasteCrateDisplay();
        
        GameLogger.LogUI($"SpawnerMachineUI initialized for spawner at ({spawnerMachine})", ComponentId);
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (spawnerMachine != null)
        {
            spawnerMachine.WasteCrateCountChanged -= OnWasteCrateCountChanged;
        }
    }
    
    /// <summary>
    /// Event handler called when the waste crate count changes
    /// </summary>
    private void OnWasteCrateCountChanged()
    {
        GameLogger.LogUI("Waste crate count changed - updating display", ComponentId);
        UpdateWasteCrateDisplay();
    }
    
    /// <summary>
    /// Updates the waste crate image based on current item count
    /// </summary>
    private void UpdateWasteCrateDisplay()
    {
        if (spawnerMachine == null || wasteCrateImage == null)
            return;
            
        int remainingCount = spawnerMachine.GetRemainingWasteCount();
        Sprite targetSprite = null;
        string stateName = "";
        
        if (remainingCount == 0)
        {
            targetSprite = emptySprite;
            stateName = "empty";
        }
        else if (remainingCount <= lowThreshold)
        {
            targetSprite = lowSprite;
            stateName = "low";
        }
        else
        {
            targetSprite = fullSprite;
            stateName = "full";
        }
        
        if (targetSprite != null)
        {
            wasteCrateImage.sprite = targetSprite;
            GameLogger.LogUI($"Updated waste crate display to '{stateName}' state (count: {remainingCount})", ComponentId);
        }
        else
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, $"No sprite assigned for '{stateName}' state", ComponentId);
        }
    }
}