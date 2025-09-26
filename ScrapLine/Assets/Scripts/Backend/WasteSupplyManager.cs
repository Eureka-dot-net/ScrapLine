using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages waste crate supply system with per-spawner queues and filtering.
/// Handles crate definitions, credit deduction, and per-machine queue management.
/// Replaces the old global queue system with configurable machine-specific queues.
/// </summary>
public class WasteSupplyManager : MonoBehaviour
{
    private static WasteSupplyManager _instance;
    public static WasteSupplyManager Instance 
    { 
        get 
        {
            if (_instance == null)
            {
                GameObject managerObj = new GameObject("WasteSupplyManager");
                _instance = managerObj.AddComponent<WasteSupplyManager>();
                DontDestroyOnLoad(managerObj);
            }
            return _instance;
        }
    }

    /// <summary>
    /// Per-machine waste crate queues. Key = machineId (e.g., "Spawner_2_1"), Value = queue of crate IDs
    /// </summary>
    private Dictionary<string, Queue<string>> machineQueues = new Dictionary<string, Queue<string>>();

    /// <summary>
    /// Queue capacity per machine (can be upgraded later)
    /// </summary>
    private Dictionary<string, int> machineQueueLimits = new Dictionary<string, int>();

    /// <summary>
    /// Default queue capacity for new spawners
    /// </summary>
    private const int DEFAULT_QUEUE_CAPACITY = 2;

    /// <summary>
    /// Get the component ID for logging purposes
    /// </summary>
    private string ComponentId => $"WasteSupplyManager_{GetInstanceID()}";

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            GameLogger.Log(LoggingManager.LogCategory.Debug, "WasteSupplyManager initialized", ComponentId);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Initialize or get the queue for a specific machine
    /// </summary>
    /// <param name="machineId">Machine identifier (e.g., "Spawner_2_1")</param>
    /// <returns>The queue for this machine</returns>
    public Queue<string> GetOrCreateMachineQueue(string machineId)
    {
        if (!machineQueues.ContainsKey(machineId))
        {
            machineQueues[machineId] = new Queue<string>();
            machineQueueLimits[machineId] = DEFAULT_QUEUE_CAPACITY;
            GameLogger.Log(LoggingManager.LogCategory.Debug, $"Created new queue for machine '{machineId}' with capacity {DEFAULT_QUEUE_CAPACITY}", ComponentId);
        }
        return machineQueues[machineId];
    }

    /// <summary>
    /// Try to consume the next crate from a machine's queue if it matches the required type
    /// </summary>
    /// <param name="machineId">Machine identifier</param>
    /// <param name="requiredCrateId">Required crate type (e.g., "medium_crate")</param>
    /// <param name="consumedCrateId">Output: The crate ID that was consumed</param>
    /// <returns>True if a matching crate was consumed, false otherwise</returns>
    public bool TryConsumeNextCrate(string machineId, string requiredCrateId, out string consumedCrateId)
    {
        consumedCrateId = null;

        var queue = GetOrCreateMachineQueue(machineId);
        if (queue.Count == 0)
        {
            GameLogger.Log(LoggingManager.LogCategory.Spawning, $"No crates in queue for machine '{machineId}'", ComponentId);
            return false;
        }

        // Check if the next crate matches the required type
        string nextCrateId = queue.Peek();
        if (nextCrateId != requiredCrateId)
        {
            GameLogger.Log(LoggingManager.LogCategory.Spawning, 
                $"Next crate '{nextCrateId}' does not match required '{requiredCrateId}' for machine '{machineId}'. Crate remains in queue.", 
                ComponentId);
            return false;
        }

        // Consume the matching crate
        consumedCrateId = queue.Dequeue();
        GameLogger.Log(LoggingManager.LogCategory.Spawning, 
            $"Consumed crate '{consumedCrateId}' for machine '{machineId}'. Queue size now: {queue.Count}", 
            ComponentId);
        
        return true;
    }

    /// <summary>
    /// Add a crate to a machine's queue
    /// </summary>
    /// <param name="machineId">Machine identifier</param>
    /// <param name="crateId">Crate ID to add</param>
    /// <returns>True if added successfully, false if queue is full</returns>
    public bool TryAddCrateToQueue(string machineId, string crateId)
    {
        var queue = GetOrCreateMachineQueue(machineId);
        int queueLimit = machineQueueLimits.GetValueOrDefault(machineId, DEFAULT_QUEUE_CAPACITY);

        if (queue.Count >= queueLimit)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Spawning, 
                $"Cannot add crate '{crateId}' to machine '{machineId}' - queue full ({queue.Count}/{queueLimit})", 
                ComponentId);
            return false;
        }

        queue.Enqueue(crateId);
        GameLogger.Log(LoggingManager.LogCategory.Economy, 
            $"Added crate '{crateId}' to machine '{machineId}' queue. Queue size: {queue.Count}/{queueLimit}", 
            ComponentId);
        
        return true;
    }

    /// <summary>
    /// Purchase a waste crate for a specific machine
    /// </summary>
    /// <param name="machineId">Machine identifier</param>
    /// <param name="crateId">Crate ID to purchase</param>
    /// <returns>True if purchase was successful</returns>
    public bool PurchaseWasteCrate(string machineId, string crateId)
    {
        var crateDef = FactoryRegistry.Instance?.GetWasteCrate(crateId);
        if (crateDef == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.Economy, $"Cannot find waste crate definition for '{crateId}'", ComponentId);
            return false;
        }

        // Calculate cost
        int crateCost = crateDef.cost > 0 ? crateDef.cost : SpawnerMachine.CalculateWasteCrateCost(crateDef);
        
        // Check if player can afford it
        var gameManager = GameManager.Instance;
        if (gameManager == null || !gameManager.CanAfford(crateCost))
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Economy, 
                $"Cannot afford waste crate '{crateDef.displayName}' - costs {crateCost} credits", 
                ComponentId);
            return false;
        }

        // Try to add to queue
        if (!TryAddCrateToQueue(machineId, crateId))
        {
            return false;
        }

        // Deduct credits
        if (!gameManager.TrySpendCredits(crateCost))
        {
            // Remove from queue since payment failed
            var queue = GetOrCreateMachineQueue(machineId);
            if (queue.Count > 0)
            {
                queue.Dequeue(); // Remove the crate we just added
            }
            GameLogger.LogError(LoggingManager.LogCategory.Economy, $"Failed to spend {crateCost} credits for waste crate", ComponentId);
            return false;
        }

        GameLogger.LogEconomy($"Purchased waste crate '{crateDef.displayName}' for {crateCost} credits for machine '{machineId}'", ComponentId);
        return true;
    }

    /// <summary>
    /// Get the queue status for a specific machine
    /// </summary>
    /// <param name="machineId">Machine identifier</param>
    /// <returns>Queue status information</returns>
    public WasteCrateQueueStatus GetMachineQueueStatus(string machineId)
    {
        var queue = GetOrCreateMachineQueue(machineId);
        int queueLimit = machineQueueLimits.GetValueOrDefault(machineId, DEFAULT_QUEUE_CAPACITY);

        var queuedCrates = new List<string>();
        foreach (string crateId in queue)
        {
            queuedCrates.Add(crateId);
        }

        return new WasteCrateQueueStatus
        {
            currentCrateId = null, // Will be set by the spawner machine
            queuedCrateIds = queuedCrates,
            maxQueueSize = queueLimit,
            canAddToQueue = queue.Count < queueLimit
        };
    }

    /// <summary>
    /// Set the queue capacity for a specific machine (for future upgrades)
    /// </summary>
    /// <param name="machineId">Machine identifier</param>
    /// <param name="newCapacity">New capacity limit</param>
    public void SetMachineQueueCapacity(string machineId, int newCapacity)
    {
        machineQueueLimits[machineId] = newCapacity;
        GameLogger.Log(LoggingManager.LogCategory.Machine, 
            $"Updated queue capacity for machine '{machineId}' to {newCapacity}", 
            ComponentId);
    }

    /// <summary>
    /// Get all waste crate definitions for UI display
    /// </summary>
    /// <returns>List of all available waste crates</returns>
    public List<WasteCrateDef> GetAllWasteCrates()
    {
        return FactoryRegistry.Instance?.GetAllWasteCrates() ?? new List<WasteCrateDef>();
    }

    /// <summary>
    /// Clear all data (for testing or reset)
    /// </summary>
    public void ClearAllQueues()
    {
        machineQueues.Clear();
        machineQueueLimits.Clear();
        GameLogger.Log(LoggingManager.LogCategory.Debug, "Cleared all machine queues", ComponentId);
    }

    /// <summary>
    /// Serialize per-machine queues to save data format
    /// </summary>
    /// <returns>Dictionary suitable for JSON serialization</returns>
    public Dictionary<string, List<string>> SerializeQueues()
    {
        var serialized = new Dictionary<string, List<string>>();
        foreach (var kvp in machineQueues)
        {
            var queueList = new List<string>();
            foreach (string crateId in kvp.Value)
            {
                queueList.Add(crateId);
            }
            serialized[kvp.Key] = queueList;
        }
        return serialized;
    }

    /// <summary>
    /// Deserialize per-machine queues from save data
    /// </summary>
    /// <param name="serializedQueues">Dictionary from JSON deserialization</param>
    public void DeserializeQueues(Dictionary<string, List<string>> serializedQueues)
    {
        ClearAllQueues();
        if (serializedQueues == null) return;

        foreach (var kvp in serializedQueues)
        {
            var queue = GetOrCreateMachineQueue(kvp.Key);
            foreach (string crateId in kvp.Value)
            {
                queue.Enqueue(crateId);
            }
        }
        GameLogger.Log(LoggingManager.LogCategory.SaveLoad, $"Deserialized queues for {serializedQueues.Count} machines", ComponentId);
    }
}