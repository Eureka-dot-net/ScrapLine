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
    private int initialWasteCrateTotal = -1; // Cache initial total for percentage calculations
    private string cachedIconSprite = null; // Cache current icon sprite to detect changes

    
    /// <summary>
    /// Get the component ID for logging purposes
    /// </summary>
    protected string ComponentId => $"Spawner_{cellData.x}_{cellData.y}";
    
    public SpawnerMachine(CellData cellData, MachineDef machineDef) : base(cellData, machineDef)
    {
        // Set spawn interval from machine definition
        spawnInterval = machineDef.baseProcessTime;
        lastSpawnTime = Time.time;
        GameLogger.LogSpawning($"Spawner created at ({cellData.x}, {cellData.y}) with interval {spawnInterval}s", ComponentId);
        // Assign the starter waste crate to this spawner when created
        CreateWasteCrate();
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
            
            // Check if icon changed after spawning and update visuals
            CheckAndUpdateIconVisual();
        }
        else if (GameLogger.IsCategoryEnabled(LoggingManager.LogCategory.Spawning))
        {
            // Only log blocking reasons if spawning logs are enabled to avoid spam
            float timeUntilNext = spawnInterval - (Time.time - lastSpawnTime);
            if (timeUntilNext > 0)
            {
                // Don't spam - only log occasionally when close to spawn time
                if (Mathf.Floor(timeUntilNext) != Mathf.Floor(timeUntilNext + Time.deltaTime))
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
    private void CreateWasteCrate()
    {
        // For now, assign the starter crate to all spawners when they are created
        var starterCrateDef = FactoryRegistry.Instance.GetWasteCrate("starter_crate");
        GameLogger.LogSpawning($"Assigning starter crate to spawner with {starterCrateDef.displayName}", ComponentId);
        if (starterCrateDef != null && (cellData.wasteCrate == null || cellData.wasteCrate.wasteCrateDefId == null || string.IsNullOrEmpty(cellData.wasteCrate.wasteCrateDefId)))
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
            GameLogger.LogSpawning($"Waste crate initialized with {cellData.wasteCrate.remainingItems.Count} item types", ComponentId);
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
    
    public override string GetBuildingIconSprite()
    {
        int currentItems = GetTotalItemsInWasteCrate();
        int initialTotal = GetInitialWasteCrateTotal();
        
        if (initialTotal == 0 || currentItems == 0)
        {
            return machineDef.buildingIconSprite + "_0";
        }
        
        // Calculate percentage
        float percentage = (float)currentItems / initialTotal * 100f;
        
        // Return appropriate sprite based on percentage ranges
        if (percentage > 66f)
        {
            return machineDef.buildingIconSprite + "_100";
        }
        else if (percentage > 33f)
        {
            return machineDef.buildingIconSprite + "_66";
        }
        else
        {
            return machineDef.buildingIconSprite + "_33";
        }
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
    /// Get initial total capacity of the waste crate (cached for performance)
    /// </summary>
    private int GetInitialWasteCrateTotal()
    {
        if (initialWasteCrateTotal >= 0)
            return initialWasteCrateTotal;
            
        if (cellData.wasteCrate == null || string.IsNullOrEmpty(cellData.wasteCrate.wasteCrateDefId))
        {
            initialWasteCrateTotal = 0;
            return 0;
        }
        
        // Try to get definition from FactoryRegistry first
        try
        {
            var crateDef = FactoryRegistry.Instance?.GetWasteCrate(cellData.wasteCrate.wasteCrateDefId);
            if (crateDef != null && crateDef.items != null)
            {
                int total = 0;
                foreach (var item in crateDef.items)
                {
                    total += item.count;
                }
                
                initialWasteCrateTotal = total;
                GameLogger.LogSpawning($"Cached initial waste crate total from definition: {total} items", ComponentId);
                return total;
            }
        }
        catch
        {
            // FactoryRegistry may not be available in test context
        }
        
        // Fallback: Calculate from current remaining items (assuming they haven't been consumed yet)
        if (cellData.wasteCrate.remainingItems != null)
        {
            int total = 0;
            foreach (var item in cellData.wasteCrate.remainingItems)
            {
                total += item.count;
            }
            
            // Only cache if this seems reasonable (non-zero)
            if (total > 0)
            {
                initialWasteCrateTotal = total;
                GameLogger.LogSpawning($"Cached initial waste crate total from remaining items: {total} items", ComponentId);
                return total;
            }
        }
        
        // Final fallback
        initialWasteCrateTotal = 0;
        return 0;
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
        int randomIndex = UnityEngine.Random.Range(0, availableItems.Count);
        var selectedItem = availableItems[randomIndex];
        
        // Decrease count by 1
        selectedItem.count--;
        
        return selectedItem.itemType;
    }
    
    /// <summary>
    /// Check if the building icon sprite has changed and update grid visuals if needed
    /// </summary>
    private void CheckAndUpdateIconVisual()
    {
        string currentIcon = GetBuildingIconSprite();
        if (cachedIconSprite != currentIcon)
        {
            GameLogger.LogMachine($"Icon sprite changed from '{cachedIconSprite}' to '{currentIcon}' - updating grid visual", ComponentId);
            cachedIconSprite = currentIcon;
            
            // Update the grid visual - need to get reference to UIGridManager
            try 
            {
                var gameManager = GameManager.Instance;
                if (gameManager != null)
                {
                    var gridManager = gameManager.GetComponent<UIGridManager>();
                    if (gridManager != null)
                    {
                        gridManager.UpdateCellVisuals(cellData.x, cellData.y, cellData.cellType, cellData.direction, this);
                        GameLogger.LogMachine($"Grid visual updated successfully for cell ({cellData.x}, {cellData.y})", ComponentId);
                    }
                    else
                    {
                        GameLogger.LogWarning(LoggingManager.LogCategory.Machine, "Could not find UIGridManager to update visual", ComponentId);
                    }
                }
                else
                {
                    GameLogger.LogWarning(LoggingManager.LogCategory.Machine, "GameManager.Instance is null - cannot update visual", ComponentId);
                }
            }
            catch (System.Exception ex)
            {
                GameLogger.LogWarning(LoggingManager.LogCategory.Machine, $"Exception updating grid visual: {ex.Message}", ComponentId);
            }
        }
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

    // Progress Bar Implementation
    // ===========================
    
    /// <summary>
    /// Gets the current spawning progress as a value between 0.0 and 1.0.
    /// Returns progress toward next spawn based on time elapsed since last spawn.
    /// </summary>
    /// <returns>Progress value 0.0-1.0, or -1 if not spawning</returns>
    public override float GetProgress()
    {
        // Only show progress if we have items to spawn and cell is available
        if (!HasItemsInWasteCrate() || cellData.items.Count > 0)
        {
            return -1f; // No progress when can't spawn
        }

        float timeSinceLastSpawn = Time.time - lastSpawnTime;
        float progress = timeSinceLastSpawn / spawnInterval;
        
        // Show 100% when progress is 80% or higher (before spawning)
        if (progress >= 0.8f)
        {
            GameLogger.LogSpawning("Showing 100% progress - ready to spawn", ComponentId);
            return 1.0f;
        }
        
        GameLogger.LogSpawning($"Spawner progress: {progress:P1} (time since last spawn: {timeSinceLastSpawn:F1}s)", ComponentId);
        return Mathf.Clamp01(progress);
    }

    /// <summary>
    /// Spawners should show progress bar when actively counting down to spawn
    /// </summary>
    /// <returns>True if progress bar should be shown</returns>
    public override bool ShouldShowProgressBar(float progress)
    {
        return progress >= 0f && HasItemsInWasteCrate() && cellData.items.Count == 0;
    }
}