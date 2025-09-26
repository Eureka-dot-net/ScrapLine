using UnityEngine;
using System.Collections;

/// <summary>
/// Core game manager that orchestrates all subsystems.
/// Maintains singleton pattern and delegates responsibilities to specialized managers.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Manager References")]
    [Tooltip("Resource loading and initialization manager")]
    public ResourceManager resourceManager;

    [Tooltip("Credits and economy system manager")]
    public CreditsManager creditsManager;

    [Tooltip("Grid operations manager")]
    public GridManager gridManager;

    [Tooltip("Save and load system manager")]
    public SaveLoadManager saveLoadManager;

    [Tooltip("Machine placement and rotation manager")]
    public MachineManager machineManager;

    [Tooltip("Item movement processing manager")]
    public ItemMovementManager itemMovementManager;


    [Header("Timing")]
    [Tooltip("Interval for spawner machines")]
    public float spawnInterval = 5f;
    private float spawnTimer;

    [Header("Edit Mode")]
    [Tooltip("Whether the game is currently in edit mode")]
    private bool isInEditMode = false;

    public UIGridManager activeGridManager;

    // Game data for save/load and queue management
    private GameData _gameData;
    
    /// <summary>
    /// Access to the current game data (creates if null)
    /// </summary>
    public GameData gameData
    {
        get
        {
            if (_gameData == null)
            {
                _gameData = new GameData();
            }
            return _gameData;
        }
        set { _gameData = value; }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeManagers();
        InitializeGame();
    }

    /// <summary>
    /// Initialize all manager components
    /// </summary>
    private void InitializeManagers()
    {
        // Find or create managers if not assigned
        if (resourceManager == null)
            resourceManager = GetComponent<ResourceManager>() ?? gameObject.AddComponent<ResourceManager>();

        if (creditsManager == null)
            creditsManager = GetComponent<CreditsManager>() ?? gameObject.AddComponent<CreditsManager>();

        if (gridManager == null)
            gridManager = GetComponent<GridManager>() ?? gameObject.AddComponent<GridManager>();

        if (saveLoadManager == null)
            saveLoadManager = GetComponent<SaveLoadManager>() ?? gameObject.AddComponent<SaveLoadManager>();

        if (machineManager == null)
            machineManager = GetComponent<MachineManager>() ?? gameObject.AddComponent<MachineManager>();

        if (itemMovementManager == null)
            itemMovementManager = GetComponent<ItemMovementManager>() ?? gameObject.AddComponent<ItemMovementManager>();

        // Find the UI grid manager
        if (activeGridManager == null)
            activeGridManager = FindAnyObjectByType<UIGridManager>();

        // Initialize resource manager first
        resourceManager.Initialize();

        // Initialize other managers
        creditsManager.Initialize(resourceManager.GetCreditsUI(), resourceManager.GetMachineBarManager());
        gridManager.Initialize(activeGridManager);
        saveLoadManager.Initialize(gridManager, creditsManager);
        machineManager.Initialize(creditsManager, gridManager, activeGridManager);
        itemMovementManager.Initialize(gridManager, activeGridManager);
    }

    /// <summary>
    /// Initialize the game based on save file existence
    /// </summary>
    private void InitializeGame()
    {
        if (saveLoadManager.SaveFileExists())
        {
            if (saveLoadManager.LoadGame())
            {
                StartCoroutine(InitializeFromSave());
            }
            else
            {
                GameLogger.LogError(LoggingManager.LogCategory.SaveLoad, $"Failed to load save file. Starting new game.");
                StartNewGame();
            }
        }
        else
        {
            StartNewGame();
        }

        // Initialize UI
        gridManager.InitializeUIGrid();
        creditsManager.UpdateCreditsDisplay();
    }

    /// <summary>
    /// Start a new game with default settings
    /// </summary>
    private void StartNewGame()
    {
        creditsManager.InitializeNewGame();
        gridManager.CreateDefaultGrid();

    }

    /// <summary>
    /// Initialize game from loaded save data
    /// </summary>
    private IEnumerator InitializeFromSave()
    {
        yield return saveLoadManager.InitializeMachinesFromSave();
    }

    private void Update()
    {
        GridData gridData = GetCurrentGrid();
        if (gridData == null) return;

        // Update machine logic - delegates to machine objects
        foreach (var cell in gridData.cells)
        {
            if (cell.machine != null)
            {
                cell.machine.UpdateLogic();
            }
        }

        // Process item movement
        itemMovementManager.ProcessItemMovement();
    }

    #region Public API Methods (Backward Compatibility)

    /// <summary>
    /// Gets the current grid. Currently returns the first grid but can be extended
    /// for multiple grid support in the future.
    /// </summary>
    public GridData GetCurrentGrid()
    {
        return gridManager.GetCurrentGrid();
    }

    /// <summary>
    /// Handle cell click events
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    public void OnCellClicked(int x, int y)
    {
        machineManager.OnCellClicked(x, y);
    }

    // <summary>
    /// Check if a cell contains a machine that can be dragged
    /// </summary>
    /// <param name="x">X coordinate of the cell</param>
    /// <param name="y">Y coordinate of the cell</param>
    /// <returns>True if the cell contains a draggable machine</returns>
    public bool CanStartDrag(int x, int y)
    {
        return machineManager.CanStartDrag(x, y);
    }

    /// <summary>
    /// Called when a drag operation starts on a cell
    /// </summary>
    /// <param name="x">X coordinate of the cell being dragged</param>
    /// <param name="y">Y coordinate of the cell being dragged</param>
    public void OnCellDragStarted(int x, int y)
    {
        machineManager.StartMachineDrag(x, y);
    }

    /// <summary>
    /// Check if a machine can be dropped at the target location
    /// </summary>
    /// <param name="fromX">Source X coordinate</param>
    /// <param name="fromY">Source Y coordinate</param>
    /// <param name="toX">Target X coordinate</param>
    /// <param name="toY">Target Y coordinate</param>
    /// <returns>True if the machine can be dropped at the target location</returns>
    public bool CanDropMachine(int fromX, int fromY, int toX, int toY)
    {
        return machineManager.CanDropMachine(fromX, fromY, toX, toY);
    }

    /// <summary>
    /// Called when a machine is dropped on another cell
    /// </summary>
    /// <param name="fromX">Source X coordinate</param>
    /// <param name="fromY">Source Y coordinate</param>
    /// <param name="toX">Target X coordinate</param>
    /// <param name="toY">Target Y coordinate</param>
    public void OnCellDropped(int fromX, int fromY, int toX, int toY)
    {
        machineManager.MoveMachine(fromX, fromY, toX, toY);
    }

    /// <summary>
    /// Called when a machine is dragged outside the grid (should be deleted)
    /// </summary>
    /// <param name="x">X coordinate of the machine to delete</param>
    /// <param name="y">Y coordinate of the machine to delete</param>
    public void OnMachineDraggedOutsideGrid(int x, int y)
    {
        machineManager.DeleteMachine(x, y);
    }

    /// <summary>
    /// Place a dragged machine at the target location
    /// </summary>
    /// <param name="x">Target X coordinate</param>
    /// <param name="y">Target Y coordinate</param>
    /// <param name="machineDefId">Machine definition ID to place</param>
    /// <param name="direction">Direction of the machine</param>
    /// <returns>True if placement was successful, false otherwise</returns>
    public bool PlaceDraggedMachine(int x, int y, string machineDefId, UICell.Direction direction)
    {
        return machineManager.PlaceDraggedMachine(x, y, machineDefId, direction);
    }

    /// <summary>
    /// Check if a machine can be dropped at the target location using machine definition ID
    /// Used for drag-and-drop validation when source cell is already blanked
    /// </summary>
    /// <param name="x">Target X coordinate</param>
    /// <param name="y">Target Y coordinate</param>
    /// <param name="machineDefId">Machine definition ID to check</param>
    /// <returns>True if the machine can be dropped at the target location</returns>
    public bool CanDropMachineWithDefId(int x, int y, string machineDefId)
    {
        return machineManager.CanDropMachineWithDefId(x, y, machineDefId);
    }

    public void RefundMachineWithId(string machineDefId)
    {
        var machineDef = FactoryRegistry.Instance.GetMachine(machineDefId);
        if (machineDef != null)
        {
            creditsManager.RefundMachine(machineDef.cost);
        }
        else
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Machine, $"RefundMachineWithId: Invalid machineDefId '{machineDefId}'");
        }
    }

    /// <summary>
    /// Set the selected machine for placement
    /// </summary>
    /// <param name="machine">Machine definition to select</param>
    public void SetSelectedMachine(MachineDef machine)
    {
        machineManager.SetSelectedMachine(machine);
    }

    /// <summary>
    /// Save the current game state
    /// </summary>
    public void SaveGame()
    {
        saveLoadManager.SaveGame();
    }

    /// <summary>
    /// Clear the current grid
    /// </summary>
    public void ClearGrid()
    {
        gridManager.ClearGrid();
    }

    /// <summary>
    /// Generate a unique item ID
    /// </summary>
    /// <returns>Unique item ID</returns>
    public string GenerateItemId()
    {
        return itemMovementManager.GenerateItemId();
    }

    /// <summary>
    /// Get current credits amount
    /// </summary>
    /// <returns>Current credits</returns>
    public int GetCredits()
    {
        return creditsManager.GetCredits();
    }

    /// <summary>
    /// Add credits to the player's account
    /// </summary>
    /// <param name="amount">Amount to add</param>
    public void AddCredits(int amount)
    {
        creditsManager.AddCredits(amount);
    }

    /// <summary>
    /// Try to spend credits
    /// </summary>
    /// <param name="amount">Amount to spend</param>
    /// <returns>True if successful, false if insufficient funds</returns>
    public bool TrySpendCredits(int amount)
    {
        return creditsManager.TrySpendCredits(amount);
    }

    /// <summary>
    /// Check if player can afford an amount
    /// </summary>
    /// <param name="amount">Amount to check</param>
    /// <returns>True if affordable</returns>
    public bool CanAfford(int amount)
    {
        return creditsManager.CanAfford(amount);
    }
    
    /// <summary>
    /// Purchase a waste crate and add it to the global queue
    /// </summary>
    /// <param name="crateId">ID of the waste crate to purchase</param>
    /// <param name="spawnerX">X coordinate of the spawner (for reference only)</param>
    /// <param name="spawnerY">Y coordinate of the spawner (for reference only)</param>
    /// <returns>True if purchase was successful</returns>
    public bool PurchaseWasteCrate(string crateId, int spawnerX, int spawnerY)
    {
        // Get crate definition and validate
        var crateDef = FactoryRegistry.Instance?.GetWasteCrate(crateId);
        if (crateDef == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.Economy, $"Cannot find waste crate definition for '{crateId}'");
            return false;
        }
        
        // Check if player can afford the crate
        int crateCost = crateDef.cost > 0 ? crateDef.cost : SpawnerMachine.CalculateWasteCrateCost(crateDef);
        if (!CanAfford(crateCost))
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Economy, $"Cannot afford waste crate '{crateDef.displayName}' - costs {crateCost} credits");
            return false;
        }
        
        // Check if global queue has space
        if (gameData.wasteQueue.Count >= gameData.wasteQueueLimit)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Economy, $"Global waste queue is full ({gameData.wasteQueue.Count}/{gameData.wasteQueueLimit})");
            return false;
        }
        
        // Deduct credits first
        if (!TrySpendCredits(crateCost))
        {
            GameLogger.LogError(LoggingManager.LogCategory.Economy, $"Failed to spend {crateCost} credits for waste crate");
            return false;
        }
        
        // Add crate to global queue
        gameData.wasteQueue.Add(crateId);
        GameLogger.LogEconomy($"Purchased waste crate '{crateDef.displayName}' for {crateCost} credits and added to global queue ({gameData.wasteQueue.Count}/{gameData.wasteQueueLimit})", $"GameManager_{GetInstanceID()}");
        
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
        var gridData = GetCurrentGrid();
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
            GameLogger.LogEconomy($"Notified {notifiedSpawners} empty spawners about new '{newCrateId}' crate", $"GameManager_{GetInstanceID()}");
        }
    }
    
    /// <summary>
    /// Get global waste crate queue status for UI display
    /// </summary>
    /// <returns>Global queue status</returns>
    public WasteCrateQueueStatus GetGlobalQueueStatus()
    {
        return new WasteCrateQueueStatus
        {
            currentCrateId = null, // Global queue doesn't have "current" crate
            queuedCrateIds = gameData.wasteQueue ?? new List<string>(),
            maxQueueSize = gameData.wasteQueueLimit,
            canAddToQueue = (gameData.wasteQueue?.Count ?? 0) < gameData.wasteQueueLimit
        };
    }

    /// <summary>
    /// Get waste crate queue status for a specific spawner (LEGACY - now returns global status)
    /// </summary>
    /// <param name="spawnerX">X coordinate of spawner</param>
    /// <param name="spawnerY">Y coordinate of spawner</param>
    /// <returns>Global queue status</returns>
    public WasteCrateQueueStatus GetSpawnerQueueStatus(int spawnerX, int spawnerY)
    {
        // Now returns global queue status since we're using global queue system
        return GetGlobalQueueStatus();
    }

    /// <summary>
    /// Set the edit mode state
    /// </summary>
    /// <param name="editMode">True to enable edit mode, false to disable</param>
    public void SetEditMode(bool editMode)
    {
        isInEditMode = editMode;
        
        // Update UI highlights based on edit mode
        if (activeGridManager != null)
        {
            if (editMode)
            {
                activeGridManager.HighlightConfigurableMachines();
            }
            else
            {
                activeGridManager.ClearHighlights();
            }
        }
    }

    /// <summary>
    /// Get the current edit mode state
    /// </summary>
    /// <returns>True if in edit mode, false otherwise</returns>
    public bool IsInEditMode()
    {
        return isInEditMode;
    }

    #endregion

    #region Manager Access (For advanced usage)

    /// <summary>
    /// Get the resource manager instance
    /// </summary>
    public ResourceManager GetResourceManager() => resourceManager;

    /// <summary>
    /// Get the credits manager instance
    /// </summary>
    public CreditsManager GetCreditsManager() => creditsManager;

    /// <summary>
    /// Get the grid manager instance
    /// </summary>
    public GridManager GetGridManager() => gridManager;

    /// <summary>
    /// Get the save/load manager instance
    /// </summary>
    public SaveLoadManager GetSaveLoadManager() => saveLoadManager;

    /// <summary>
    /// Get the machine manager instance
    /// </summary>
    public MachineManager GetMachineManager() => machineManager;

    /// <summary>
    /// Get the item movement manager instance
    /// </summary>
    public ItemMovementManager GetItemMovementManager() => itemMovementManager;

    #endregion
}