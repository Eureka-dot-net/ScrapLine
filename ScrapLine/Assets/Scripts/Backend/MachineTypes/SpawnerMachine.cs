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
    /// The crate type this spawner is configured to accept (e.g., "medium_crate")
    /// </summary>
    public string RequiredCrateId { get; set; } = "starter_crate";

    /// <summary>
    /// Get the machine identifier for queuing system
    /// </summary>
    public string MachineId => $"Spawner_{cellData.x}_{cellData.y}";
    
    /// <summary>
    /// Get the component ID for logging purposes
    /// </summary>
    protected string ComponentId => MachineId;
    
    public SpawnerMachine(CellData cellData, MachineDef machineDef) : base(cellData, machineDef)
    {
        // Set spawn interval from machine definition
        spawnInterval = machineDef.baseProcessTime;
        lastSpawnTime = Time.time;
        
        // Enable configuration for this machine type
        CanConfigure = true;
        
        GameLogger.LogSpawning($"Spawner created at ({cellData.x}, {cellData.y}) with interval {spawnInterval}s", ComponentId);
        
        // Initialize with starter crate configuration by default
        RequiredCrateId = "starter_crate";
        
        // Assign the starter waste crate to this spawner when created
        CreateWasteCrate();
    }
    
    /// <summary>
    /// Called when the spawner machine is configured by the player
    /// </summary>
    public override void OnConfigured()
    {
        // Find the spawner configuration UI in the scene
        var configUI = UnityEngine.Object.FindFirstObjectByType<SpawnerConfigPanel>(UnityEngine.FindObjectsInactive.Include);
        if (configUI != null)
        {
            configUI.ShowConfiguration(cellData, OnConfigurationConfirmed);
        }
        else
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Machine, "SpawnerConfigPanel not found in scene. Please add the UI component to configure spawner machines.", ComponentId);

            // Fallback: Cycle through available crate types for testing
            var allCrates = FactoryRegistry.Instance?.GetAllWasteCrates();
            if (allCrates != null && allCrates.Count > 0)
            {
                // Find current crate index and select next one
                int currentIndex = allCrates.FindIndex(c => c.id == RequiredCrateId);
                int nextIndex = (currentIndex + 1) % allCrates.Count;
                var nextCrate = allCrates[nextIndex];
                
                RequiredCrateId = nextCrate.id;
                GameLogger.LogMachine($"Fallback: Set required crate to '{nextCrate.displayName}' for testing", ComponentId);
                
                // Trigger supply check
                TryRefillFromQueue();
            }
        }
    }
    
    /// <summary>
    /// Called when configuration is confirmed in the UI
    /// </summary>
    /// <param name="selectedCrateId">The selected crate type ID</param>
    private void OnConfigurationConfirmed(string selectedCrateId)
    {
        if (!string.IsNullOrEmpty(selectedCrateId))
        {
            RequiredCrateId = selectedCrateId;
            GameLogger.LogMachine($"Spawner configured to require '{selectedCrateId}' crates", ComponentId);
            
            // Trigger supply check immediately after configuration change
            TryRefillFromQueue();
        }
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
        
        // Check if waste crate is empty and try to refill from queue
        if (!HasItemsInWasteCrate())
        {
            TryRefillFromQueue();
        }
        
        // Debug logging (only if enabled to avoid spam)
        else if (GameLogger.IsCategoryEnabled(LoggingManager.LogCategory.Spawning))
        {
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
        lastSpawnTime = Time.time;
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
            // Current crate is empty - check queue for next compatible crate
            GameLogger.LogSpawning("Current waste crate is empty, checking queue for compatible crate", ComponentId);
            if (TryRefillFromQueue())
            {
                GameLogger.LogSpawning("Successfully moved compatible crate from queue to current", ComponentId);
                // Recursive call to try again with the new crate
                return GetRandomItemFromWasteCrate();
            }
            else
            {
                GameLogger.LogSpawning($"No '{RequiredCrateId}' crates in queue - spawner cannot produce items", ComponentId);
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
    
    /// <summary>
    /// Try to refill from queue using the new WasteSupplyManager with filtering
    /// </summary>
    /// <returns>True if a compatible crate was consumed and loaded</returns>
    private bool TryRefillFromQueue()
    {
        var wasteSupplyManager = WasteSupplyManager.Instance;
        if (wasteSupplyManager == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.Spawning, "WasteSupplyManager not available", ComponentId);
            return false;
        }

        // Try to consume a crate that matches our required type
        if (wasteSupplyManager.TryConsumeNextCrate(MachineId, RequiredCrateId, out string consumedCrateId))
        {
            GameLogger.LogSpawning($"Consumed crate '{consumedCrateId}' from queue", ComponentId);

            // Get the crate definition and create instance
            var crateDef = FactoryRegistry.Instance?.GetWasteCrate(consumedCrateId);
            if (crateDef == null)
            {
                GameLogger.LogError(LoggingManager.LogCategory.Spawning, $"Could not find crate definition for '{consumedCrateId}'", ComponentId);
                return false;
            }

            // Replace current waste crate with the new one
            cellData.wasteCrate = new WasteCrateInstance
            {
                wasteCrateDefId = crateDef.id,
                remainingItems = new List<WasteCrateItemDef>()
            };

            // Copy items from definition to instance
            foreach (var item in crateDef.items)
            {
                cellData.wasteCrate.remainingItems.Add(new WasteCrateItemDef
                {
                    itemType = item.itemType,
                    count = item.count
                });
            }

            // Reset cached icon sprite so it updates
            cachedIconSprite = null;
            initialWasteCrateTotal = -1; // Reset cache

            GameLogger.LogSpawning($"New waste crate '{crateDef.displayName}' activated with {cellData.wasteCrate.remainingItems.Count} item types", ComponentId);
            return true;
        }

        return false;
    }
    
    /// <summary>
    /// Adds a waste crate to this spawner's queue using WasteSupplyManager
    /// </summary>
    public bool TryAddToQueue(string crateId)
    {
        var wasteSupplyManager = WasteSupplyManager.Instance;
        if (wasteSupplyManager == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.Spawning, "WasteSupplyManager not available", ComponentId);
            return false;
        }

        return wasteSupplyManager.TryAddCrateToQueue(MachineId, crateId);
    }
    
    /// <summary>
    /// Gets the current queue status for UI display using WasteSupplyManager
    /// </summary>
    public WasteCrateQueueStatus GetQueueStatus()
    {
        var wasteSupplyManager = WasteSupplyManager.Instance;
        if (wasteSupplyManager == null)
        {
            return new WasteCrateQueueStatus
            {
                currentCrateId = cellData.wasteCrate?.wasteCrateDefId,
                queuedCrateIds = new List<string>(),
                maxQueueSize = 2,
                canAddToQueue = false
            };
        }

        var status = wasteSupplyManager.GetMachineQueueStatus(MachineId);
        status.currentCrateId = cellData.wasteCrate?.wasteCrateDefId;
        return status;
    }
    
    /// <summary>
    /// Calculates the cost of a waste crate based on item values (50% of total item value)
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
        
        // Return 50% of total item value
        return (int)(totalValue * 0.5f);
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