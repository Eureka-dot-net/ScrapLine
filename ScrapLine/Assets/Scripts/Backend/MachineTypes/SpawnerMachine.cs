using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles spawner machine behavior. Spawners create new items at regular intervals
/// and can spawn different item types based on their configuration.
/// </summary>
public class SpawnerMachine : BaseMachine
{
    public const int DeliveryQueueCapacity = 3;
    private float lastSpawnTime;
    private float spawnInterval;
    private int initialWasteCrateTotal = -1; // Cache initial total for percentage calculations

    /// <summary>
    /// Get the component ID for logging purposes
    /// </summary>
    protected string ComponentId => $"Spawner_{cellData.x}_{cellData.y}";
    
    public SpawnerMachine(CellData cellData, MachineDef machineDef) : base(cellData, machineDef)
    {
        // Set spawn interval from machine definition
        spawnInterval = machineDef.baseProcessTime;
        lastSpawnTime = SimulationClock.Time;
        
        // Enable configuration for this machine type
        CanConfigure = true;
        
        GameLogger.LogSpawning($"Spawner created at ({cellData.x}, {cellData.y}) with interval {spawnInterval}s", ComponentId);
        
        cellData.wasteDeliveryQueue ??= new List<string>();
    }
    
    /// <summary>
    /// Called when the spawner machine is configured by the player
    /// </summary>
    public override void OnConfigured()
    {
        // Find the spawner configuration UI in the scene
        var configUI = UnityEngine.Object.FindAnyObjectByType<SpawnerConfigPanel>(UnityEngine.FindObjectsInactive.Include);
        if (configUI != null)
        {
            configUI.ShowConfiguration(cellData, null);
        }
        else
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Machine,
                "Spawner panel not found in scene.", ComponentId);
        }
    }
    
    /// <summary>
    /// Update logic for spawner - handles item spawning and FIFO delivery activation.
    /// </summary>
    public override void UpdateLogic()
    {
        // Empty spawners automatically activate the oldest unopened delivery.
        if (!HasItemsInWasteCrate())
            TryActivateNextDelivery();
        
        // Check if it's time to spawn and if the cell is empty and waste crate has items
        if (SimulationClock.Time - lastSpawnTime >= spawnInterval && cellData.items.Count == 0 && HasItemsInWasteCrate())
        {
            GameLogger.NotifyStateChange(ComponentId); // State change for spawning
            GameLogger.LogSpawning($"Spawn conditions met - triggering spawn", ComponentId);
            SpawnItem();
            lastSpawnTime = SimulationClock.Time;
            
            // Check if icon changed after spawning and update visuals using new refresh system
            RefreshConfigurationVisuals();
        }
        
        // Debug logging (only if enabled to avoid spam)
        else if (GameLogger.IsCategoryEnabled(LoggingManager.LogCategory.Spawning))
        {
            float timeUntilNext = spawnInterval - (SimulationClock.Time - lastSpawnTime);
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
                GameLogger.LogSpawning("Waste crate empty - waiting for a delivery", ComponentId);
            }
        }
    }
    
    /// <summary>
    /// Check if the waste crate has any items remaining
    /// </summary>
    public bool HasItemsInWasteCrate()
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
    /// Get total count of items in waste crate (for debugging and UI)
    /// </summary>
    public int GetTotalItemsInWasteCrate()
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
    /// Get initial total capacity of the waste crate (cached for performance, public for UI)
    /// </summary>
    public int GetInitialWasteCrateTotal()
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
        lastSpawnTime = SimulationClock.Time;
        GameLogger.LogSpawning($"Spawn complete, next spawn in {spawnInterval}s", ComponentId);
    }
    
    /// <summary>
    /// Gets a random item from the waste crate and removes one count from it.
    /// If the current crate is empty, checks the queue and moves the next crate to current.
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
        {
            // Current crate is empty - check queue for next crate
            GameLogger.LogSpawning("Current waste crate is empty, checking queue for next crate", ComponentId);
            if (TryActivateNextDelivery())
            {
                GameLogger.LogSpawning("Successfully moved crate from queue to current", ComponentId);
                // Recursive call to try again with the new crate
                return GetRandomItemFromWasteCrate();
            }
            else
            {
                GameLogger.LogSpawning("No crates in queue - spawner cannot produce items", ComponentId);
                return null;
            }
        }
            
        // Select random item from available items
        int randomIndex = UnityEngine.Random.Range(0, availableItems.Count);
        var selectedItem = availableItems[randomIndex];
        
        // Decrease count by 1
        selectedItem.count--;
        
        return selectedItem.itemType;
    }
    
    public bool TryEnqueueDelivery(string crateId)
    {
        cellData.wasteDeliveryQueue ??= new List<string>();
        if (string.IsNullOrWhiteSpace(crateId) ||
            cellData.wasteDeliveryQueue.Count >= DeliveryQueueCapacity ||
            FactoryRegistry.Instance?.GetWasteCrate(crateId) == null)
            return false;

        cellData.wasteDeliveryQueue.Add(crateId);
        if (!HasItemsInWasteCrate())
            TryActivateNextDelivery();
        return true;
    }

    public WasteCrateQueueStatus GetDeliveryQueueStatus()
    {
        cellData.wasteDeliveryQueue ??= new List<string>();
        return new WasteCrateQueueStatus
        {
            currentCrateId = cellData.wasteCrate?.wasteCrateDefId,
            queuedCrateIds = new List<string>(cellData.wasteDeliveryQueue),
            maxQueueSize = DeliveryQueueCapacity,
            canAddToQueue = cellData.wasteDeliveryQueue.Count < DeliveryQueueCapacity
        };
    }

    private bool TryActivateNextDelivery()
    {
        cellData.wasteDeliveryQueue ??= new List<string>();
        if (cellData.wasteDeliveryQueue.Count == 0)
            return false;

        string crateId = cellData.wasteDeliveryQueue[0];
        if (!ActivateWasteCrate(crateId))
            return false;

        cellData.wasteDeliveryQueue.RemoveAt(0);
        GameLogger.LogSpawning($"Activated next '{crateId}' delivery", ComponentId);
        return true;
    }

    private bool ActivateWasteCrate(string crateId)
    {
        var crateDef = FactoryRegistry.Instance?.GetWasteCrate(crateId);
        if (crateDef == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.Spawning, $"Could not find crate definition for '{crateId}'", ComponentId);
            return false;
        }

        cellData.wasteCrate = new WasteCrateInstance
        {
            wasteCrateDefId = crateDef.id,
            remainingItems = new List<WasteCrateItemDef>()
        };
        foreach (var item in crateDef.items)
        {
            cellData.wasteCrate.remainingItems.Add(new WasteCrateItemDef
            {
                itemType = item.itemType,
                count = item.count
            });
        }

        initialWasteCrateTotal = -1;
        GameLogger.LogSpawning($"New waste crate '{crateDef.displayName}' activated with {cellData.wasteCrate.remainingItems.Count} item types", ComponentId);
        return true;
    }
    
    /// <summary>
    /// Calculates the fallback cost of a waste crate at 80% of its raw contents value.
    /// </summary>
    public static int CalculateWasteCrateCost(WasteCrateDef crateDef)
    {
        if (crateDef?.items == null)
            return 0;
            
        int totalValue = 0;
        foreach (var item in crateDef.items)
        {
            var itemDef = FactoryRegistry.Instance?.GetItem(item.itemType);
            if (itemDef != null)
            {
                totalValue += itemDef.sellValue * item.count;
            }
        }
        
        // Paid supply leaves a 20% raw-selling margin while strongly rewarding processing.
        return Mathf.RoundToInt(totalValue * 0.8f);
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

        float timeSinceLastSpawn = SimulationClock.Time - lastSpawnTime;
        float progress = timeSinceLastSpawn / spawnInterval;

        
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
