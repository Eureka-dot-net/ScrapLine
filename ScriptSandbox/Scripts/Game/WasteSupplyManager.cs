using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages the global waste crate queue system and spawner interactions.
/// Handles crate purchasing, queue management, and spawner notification logic.
/// Separates waste crate responsibilities from GameManager for better organization.
/// </summary>
public class WasteSupplyManager : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Credits manager for handling payments")]
    public CreditsManager creditsManager;
    
    [Tooltip("Grid manager for finding spawner machines")]
    public GridManager gridManager;

    /// <summary>
    /// Component ID for logging purposes
    /// </summary>
    private string ComponentId => $"WasteSupplyManager_{GetInstanceID()}";

    /// <summary>
    /// Purchase a waste crate and add it to the global queue
    /// </summary>
    /// <param name="crateId">ID of the waste crate to purchase</param>
    /// <returns>True if purchase was successful</returns>
    public bool PurchaseWasteCrate(string crateId)
    {
        // Get crate definition and validate
        var crateDef = FactoryRegistry.Instance?.GetWasteCrate(crateId);
        if (crateDef == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.Economy, $"Cannot find waste crate definition for '{crateId}'", ComponentId);
            return false;
        }
        
        // Check if player can afford the crate
        int crateCost = crateDef.cost > 0 ? crateDef.cost : SpawnerMachine.CalculateWasteCrateCost(crateDef);
        if (!creditsManager.CanAfford(crateCost))
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Economy, $"Cannot afford waste crate '{crateDef.displayName}' - costs {crateCost} credits", ComponentId);
            return false;
        }
        
        // Check if global queue has space
        var gameData = GameManager.Instance.gameData;
        if (gameData.wasteQueue.Count >= gameData.wasteQueueLimit)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Economy, $"Global waste queue is full ({gameData.wasteQueue.Count}/{gameData.wasteQueueLimit})", ComponentId);
            return false;
        }
        
        // Deduct credits first
        if (!creditsManager.TrySpendCredits(crateCost))
        {
            GameLogger.LogError(LoggingManager.LogCategory.Economy, $"Failed to spend {crateCost} credits for waste crate", ComponentId);
            return false;
        }
        
        // Add crate to global queue
        gameData.wasteQueue.Add(crateId);
        GameLogger.LogEconomy($"Purchased waste crate '{crateDef.displayName}' for {crateCost} credits and added to global queue ({gameData.wasteQueue.Count}/{gameData.wasteQueueLimit})", ComponentId);
        
        // Notify all empty spawners that a new crate is available
        NotifySpawnersOfNewCrate(crateId);
        
        return true;
    }

    /// <summary>
    /// Notify all empty spawners that a new crate has been added to the global queue
    /// Each spawner will check if the crate matches their RequiredCrateId
    /// </summary>
    /// <param name="newCrateId">The ID of the crate that was just added to the queue</param>
    private void NotifySpawnersOfNewCrate(string newCrateId)
    {
        if (gridManager == null) return;
        
        var gridData = gridManager.GetCurrentGrid();
        if (gridData?.cells == null) return;
        
        int notifiedSpawners = 0;
        foreach (var cell in gridData.cells)
        {
            if (cell.machine is SpawnerMachine spawner)
            {
                // Only notify spawners that are empty and need this crate type
                if (!spawner.HasItemsInWasteCrate() && spawner.RequiredCrateId == newCrateId)
                {
                    spawner.TryRefillFromGlobalQueue();
                    notifiedSpawners++;
                }
            }
        }
        
        if (notifiedSpawners > 0)
        {
            GameLogger.LogEconomy($"Notified {notifiedSpawners} empty spawners about new '{newCrateId}' crate", ComponentId);
        }
    }

    /// <summary>
    /// Get global waste crate queue status for UI display
    /// </summary>
    /// <returns>Current global queue status</returns>
    public WasteCrateQueueStatus GetGlobalQueueStatus()
    {
        var gameData = GameManager.Instance.gameData;
        return new WasteCrateQueueStatus
        {
            currentCrateId = null, // Global queue doesn't have "current" crate
            queuedCrateIds = gameData.wasteQueue ?? new List<string>(),
            maxQueueSize = gameData.wasteQueueLimit,
            canAddToQueue = (gameData.wasteQueue?.Count ?? 0) < gameData.wasteQueueLimit
        };
    }
    
    /// <summary>
    /// Return a waste crate to the global queue (e.g., when spawner configuration changes)
    /// </summary>
    /// <param name="crateId">ID of the waste crate to return</param>
    /// <returns>True if crate was successfully returned to queue</returns>
    public bool ReturnCrateToQueue(string crateId)
    {
        if (string.IsNullOrEmpty(crateId))
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Economy, "Cannot return null/empty crate to queue", ComponentId);
            return false;
        }
        
        var gameData = GameManager.Instance.gameData;
        
        // Check if queue has space
        if (gameData.wasteQueue.Count >= gameData.wasteQueueLimit)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Economy, $"Cannot return crate '{crateId}' - global queue is full ({gameData.wasteQueue.Count}/{gameData.wasteQueueLimit})", ComponentId);
            return false;
        }
        
        // Add crate back to queue
        gameData.wasteQueue.Add(crateId);
        GameLogger.LogEconomy($"Returned waste crate '{crateId}' to global queue ({gameData.wasteQueue.Count}/{gameData.wasteQueueLimit})", ComponentId);
        
        // Notify spawners that a crate is available
        NotifySpawnersOfNewCrate(crateId);
        
        return true;
    }

    /// <summary>
    /// Get all available waste crate types for UI selection
    /// </summary>
    /// <returns>List of available waste crate definitions</returns>
    public List<WasteCrateDef> GetAvailableWasteCrates()
    {
        return FactoryRegistry.Instance?.GetAllWasteCrates() ?? new List<WasteCrateDef>();
    }

    /// <summary>
    /// Check if a specific waste crate can be afforded
    /// </summary>
    /// <param name="crateId">ID of the waste crate to check</param>
    /// <returns>True if player can afford this crate</returns>
    public bool CanAffordWasteCrate(string crateId)
    {
        var crateDef = FactoryRegistry.Instance?.GetWasteCrate(crateId);
        if (crateDef == null) return false;
        
        int crateCost = crateDef.cost > 0 ? crateDef.cost : SpawnerMachine.CalculateWasteCrateCost(crateDef);
        return creditsManager.CanAfford(crateCost);
    }

    /// <summary>
    /// Get the cost of a specific waste crate
    /// </summary>
    /// <param name="crateId">ID of the waste crate</param>
    /// <returns>Cost in credits, or 0 if crate not found</returns>
    public int GetWasteCrateCost(string crateId)
    {
        var crateDef = FactoryRegistry.Instance?.GetWasteCrate(crateId);
        if (crateDef == null) return 0;
        
        return crateDef.cost > 0 ? crateDef.cost : SpawnerMachine.CalculateWasteCrateCost(crateDef);
    }
}