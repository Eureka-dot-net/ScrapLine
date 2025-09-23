using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles spawner machine behavior. Spawners create new items at regular intervals
/// and can spawn different item types based on their configuration.
/// </summary>
public class SpawnerMachine : BaseMachine
{
    private float lastSpawnTime;
    private float spawnInterval;
    
    /// <summary>    /// Get the component ID for logging purposes    /// </summary>    protected string ComponentId => $"Spawner_{cellData.x}_{cellData.y}"; 
    public SpawnerMachine(CellData cellData, MachineDef machineDef) : base(cellData, machineDef)
    {
        // Set spawn interval from machine definition
        spawnInterval = machineDef.baseProcessTime;
        lastSpawnTime = Time.time;
        
        // Assign the starter waste crate to this spawner when created
        InitializeWasteCrate();
    }
    
    /// <summary>
    /// Update logic for spawner - handles item spawning timer
    /// </summary>
    public override void UpdateLogic()
    {
        // Check if it's time to spawn and if the cell is empty and waste crate has items
        if (Time.time - lastSpawnTime >= spawnInterval && cellData.items.Count == 0 && HasItemsInWasteCrate())
        {
            GameLogger.NotifyStateChange(ComponentId); // State change for spawning
            GameLogger.LogSpawning($"Spawn conditions met - triggering spawn", ComponentId);
            SpawnItem();
            lastSpawnTime = Time.time;
        }
        else if (GameLogger.IsCategoryEnabled(LoggingManager.LogCategory.Spawning))
        {
            // Only log blocking reasons if spawning logs are enabled to avoid spam
            float timeUntilNext = spawnInterval - (Time.time - lastSpawnTime);
            if (timeUntilNext > 0)
            {
                // Don't spam - only log occasionally when close to spawn time
                if (timeUntilNext < 1.0f)
                {
                    GameLogger.LogSpawning($"Spawn in {timeUntilNext:F1}s", ComponentId);
                }
            }
            else if (cellData.items.Count > 0)
            {
                GameLogger.LogSpawning($"Cell occupied - {cellData.items.Count} items present", ComponentId);
            }
            else if (!HasItemsInWasteCrate())
            {
                GameLogger.LogSpawning("Waste crate empty - no items to spawn", ComponentId);
            }
        }
    }
    
    /// <summary>
    /// Initialize the waste crate for this spawner
    /// </summary>
    private void InitializeWasteCrate()
    {
        // For now, assign the starter crate to all spawners when they are created
        var starterCrateDef = FactoryRegistry.Instance.GetWasteCrate("starter_crate");
        if (starterCrateDef != null && (cellData.wasteCrate == null || cellData.wasteCrate.wasteCrateDefId == null))
        {
            cellData.wasteCrate = new WasteCrateInstance
            {
                wasteCrateDefId = starterCrateDef.id,
                remainingItems = new List<WasteCrateItemDef>()
            };
            
            // Copy items from definition to instance
            foreach (var item in starterCrateDef.items)
            {
                cellData.wasteCrate.remainingItems.Add(new WasteCrateItemDef
                {
                    itemType = item.itemType,
                    count = item.count
                });
            }
        }
    }
    
    /// <summary>
    /// Check if the waste crate has any items remaining
    /// </summary>
    private bool HasItemsInWasteCrate()
    {
        if (cellData.wasteCrate == null || cellData.wasteCrate.remainingItems == null)
            return false;
            
        foreach (var item in cellData.wasteCrate.remainingItems)
        {
            if (item.count > 0)
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// Get total count of items in waste crate (for debugging)
    /// </summary>
    private int GetTotalItemsInWasteCrate()
    {
        if (cellData.wasteCrate == null || cellData.wasteCrate.remainingItems == null)
            return 0;
            
        int total = 0;
        foreach (var item in cellData.wasteCrate.remainingItems)
        {
            total += item.count;
        }
        return total;
    }
    
    /// <summary>
    /// Spawns a new item at this spawner's location
    /// </summary>
    private void SpawnItem()
    {
        // Get a random item from the waste crate
        string itemType = GetRandomItemFromWasteCrate();
        
        if (string.IsNullOrEmpty(itemType))
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Spawning, $"Spawner at ({cellData.x}, {cellData.y}) has no items in waste crate to spawn", ComponentId);
            return;
        }

        GameLogger.LogSpawning($"Spawning {itemType} after {spawnInterval}s interval", ComponentId);

        // Create new item with proper ItemData structure
        ItemData newItem = new ItemData
        {
            id = GameManager.Instance.GenerateItemId(), // Use centralized ID generation
            itemType = itemType,
            x = cellData.x,
            y = cellData.y,
            state = ItemState.Idle,
            moveProgress = 0f,
            processingStartTime = 0f,
            processingDuration = 0f,
            waitingStartTime = 0f,
        };
        
        GameLogger.LogSpawning($"Created new item: {newItem.itemType} (id: {newItem.id})", ComponentId);

        cellData.items.Add(newItem);

        // Tell visual manager to create visual representation
        UIGridManager gridManager = Object.FindAnyObjectByType<UIGridManager>();
        if (gridManager != null)
        {
            gridManager.CreateVisualItem(newItem.id, cellData.x, cellData.y, newItem.itemType);
            GameLogger.LogSpawning($"Visual created for spawned item {newItem.itemType}", ComponentId);
        }
        else
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Spawning, "No UIGridManager found to create visual for spawned item", ComponentId);
        }
        
        // Immediately try to start movement of the newly spawned item
        TryStartMove(newItem);
        
        // Update spawn timing
        lastSpawnTime = Time.time;
        GameLogger.LogSpawning($"Spawn complete, next spawn in {spawnInterval}s", ComponentId);
    }
    
    /// <summary>
    /// Gets a random item from the waste crate and removes one count from it
    /// </summary>
    private string GetRandomItemFromWasteCrate()
    {
        if (cellData.wasteCrate == null || cellData.wasteCrate.remainingItems == null)
            return null;
            
        // Create list of available items (with counts > 0)
        var availableItems = new List<WasteCrateItemDef>();
        foreach (var item in cellData.wasteCrate.remainingItems)
        {
            if (item.count > 0)
                availableItems.Add(item);
        }
        
        if (availableItems.Count == 0)
            return null;
            
        // Select random item from available items
        int randomIndex = Random.Range(0, availableItems.Count);
        var selectedItem = availableItems[randomIndex];
        
        // Decrease count by 1
        selectedItem.count--;
        
        return selectedItem.itemType;
    }
    
    /// <summary>
    /// Spawners don't process arriving items - they create new ones
    /// </summary>
    public override void OnItemArrived(ItemData item)
    {
        // Spawners don't handle incoming items
        // Items shouldn't arrive at spawners in normal gameplay
        GameLogger.LogWarning(LoggingManager.LogCategory.Spawning, $"Item {item.id} arrived at spawner - this shouldn't happen", ComponentId);
    }
    
    /// <summary>
    /// Spawners don't process items - they create them
    /// </summary>
    public override void ProcessItem(ItemData item)
    {
        // Spawners don't process items
        GameLogger.LogWarning(LoggingManager.LogCategory.Spawning, $"Attempted to process item {item.id} at spawner - this shouldn't happen", ComponentId);
    }
}