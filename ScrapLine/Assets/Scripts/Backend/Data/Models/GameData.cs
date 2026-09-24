// Add these classes to your data model file (or create a new one)

using System.Collections.Generic;

public enum ItemState
{
    Idle,      // Item is stationary and ready to move
    Moving,    // Item is moving between cells
    Waiting,   // Item is waiting because target machine is busy
    Processing // Item is being processed by a machine
}

public enum MachineState
{
    Idle,       // Machine is ready to receive items
    Receiving,  // Item is moving to machine (prevents race conditions)
    Processing  // Machine is actively processing an item
}

[System.Serializable]
public class ItemData
{
    //public string id;
    public string id;
    public string itemType;
    public int x;
    public int y;

    public ItemState state = ItemState.Idle;
    public float moveStartTime;
    public float moveProgress;
    public int sourceX;
    public int sourceY;
    public int targetX;
    public int targetY;

    public float processingStartTime;
    public float processingDuration;

    public float waitingStartTime;

    public bool isHalfway = false; // Flag to indicate if the item is halfway in its movement
    
    public int stackIndex = 0; // Index in the waiting stack (0 = center, 1 = left, 2 = right, 3 = left2, etc.)

}

[System.Serializable]
public class WasteCrateInstance {
    public string wasteCrateDefId;
    public List<WasteCrateItemDef> remainingItems = new List<WasteCrateItemDef>();
}

/// <summary>
/// Status information for waste crate queue (used for UI display)
/// </summary>
[System.Serializable]
public class WasteCrateQueueStatus
{
    public string currentCrateId;
    public List<string> queuedCrateIds;
    public int maxQueueSize;
    public bool canAddToQueue;
}

[System.Serializable]
public class SortingMachineConfig
{
    public string leftItemType = ""; // Item type that should go left
    public string rightItemType = ""; // Item type that should go right
}

[System.Serializable]
public class CellData
{
    public int x;
    public int y;
    public UICell.CellType cellType;
    public UICell.Direction direction;
    public UICell.CellRole cellRole;
    public string machineDefId; // References the specific machine definition from FactoryRegistry
    public List<ItemData> items = new List<ItemData>();
    public List<ItemData> waitingItems = new List<ItemData>(); // List for items waiting to enter this machine
    public MachineState machineState = MachineState.Idle; // Current state of the machine
    [System.NonSerialized]
    public BaseMachine machine; // Runtime-only machine object that handles behavior
    public string selectedRecipeId; // Player's configuration choice for this machine
    public SortingMachineConfig sortingConfig = new SortingMachineConfig(); // Configuration for sorting machines
    public WasteCrateInstance wasteCrate; // WasteCrate assigned to spawner machines
    public List<string> wasteDeliveryQueue = new List<string>(); // Unopened deliveries owned by this spawner
}

/// <summary>
/// One factory site: its stable identity plus the grid it occupies.
///
/// Ownership: a GridData is PER-FACTORY state. Credits, machine licences, the warehouse, the ship
/// blueprint and lifetime statistics are global and live on <see cref="GameData"/>.
///
/// A site exists as soon as a GridData carrying its ID is present; there is no separate "owned"
/// flag. The planned site list, the starting size and the maximum size are configuration, in
/// Resources/factorysites.json - see <see cref="FactorySiteConfiguration"/>.
/// </summary>
[System.Serializable]
public class GridData
{
    /// <summary>
    /// Stable site identity. Assigned exactly once by the schema 3 to 4 migration for pre-existing
    /// saves (always <see cref="FactorySiteConfiguration.DefaultFactoryId"/>) and never reassigned.
    /// </summary>
    public string factoryId;

    /// <summary>Player-facing name; defaults from the site plan and may later be renamed.</summary>
    public string displayName;

    /// <summary>Zero-based position in the configured site plan.</summary>
    public int siteIndex;

    public int width;
    public int height;
    public List<CellData> cells = new List<CellData>();
}

[System.Serializable]
public class UserMachineProgress
{
    public string machineId;
    public bool unlocked;
    public int upgradeLevel;
}

[System.Serializable]
public class ObjectiveProgressData
{
    public string objectiveId;
    public int progress;
    public bool completed;
    public bool rewardClaimed;
    public List<string> processedEventIds = new List<string>();
}

/// <summary>
/// The whole persisted game.
///
/// Ownership model:
///   GLOBAL  - credits, userMachineProgress (machine licences), objectiveProgress, warehouse,
///             shipBlueprint, lifetimeStats, launchResult, and both clock anchors.
///   PER-FACTORY - every entry in <see cref="grids"/>, including its cells, placed machines,
///             queued scrap and machine configuration.
///
/// Machine licences and credits are deliberately global: buying a licence unlocks that machine at
/// every site, and there is one shared balance.
/// </summary>
[System.Serializable]
public class GameData
{
    public int schemaVersion = GameSaveMigrations.CurrentSchemaVersion;

    // --- Session clock (runtime) --------------------------------------------------------------
    // Unity's Time.time restarts at zero each launch. These anchors rebase in-flight item timers
    // across a load and are required by the live simulation; they are NOT usable for offline time.
    public bool hasRuntimeClockAnchor;
    public float savedAtRuntimeTime;

    // --- Wall clock (offline) -----------------------------------------------------------------
    /// <summary>
    /// Trusted UTC save anchor. Normally the time of the write; held at the previous high-water
    /// mark when the device clock has moved backwards. Zero until the first v4 save.
    /// </summary>
    public long savedAtUtcTicks;

    /// <summary>
    /// Highest UTC value ever observed by this save. Offline progress is measured forward from
    /// <see cref="savedAtUtcTicks"/> but never credited when the device clock reads below this
    /// high-water mark, so winding the clock back cannot manufacture elapsed time.
    /// </summary>
    public long highWaterUtcTicks;

    /// <summary>
    /// False until wall-clock anchors have been established once. A save resumed without an anchor
    /// grants no offline progress; the next write establishes the anchor for future resumes.
    /// </summary>
    public bool hasUtcClockAnchor;

    // --- Per-factory state --------------------------------------------------------------------
    public List<GridData> grids = new List<GridData>();

    // --- Global state -------------------------------------------------------------------------
    public List<UserMachineProgress> userMachineProgress = new List<UserMachineProgress>();
    public List<ObjectiveProgressData> objectiveProgress = new List<ObjectiveProgressData>();
    public int credits = 0; // Credits (money) system for purchasing machines
    public WarehouseData warehouse = new WarehouseData();
    public ShipBlueprintData shipBlueprint = new ShipBlueprintData();
    public LifetimeStatsData lifetimeStats = new LifetimeStatsData();
    public LaunchResultData launchResult = new LaunchResultData();

    public static GameData CreateNewGame()
    {
        return new GameData
        {
            userMachineProgress = MachineUnlockState.CreateCleanSaveProgress()
        };
    }
}
