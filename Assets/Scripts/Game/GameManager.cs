using UnityEngine;
using System.IO;
using System.Collections.Generic;
using static UICell;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private List<GridData> activeGrids = new List<GridData>();
    private const string SAVE_FILE_NAME = "game_data.json";

    public float spawnInterval = 5f;
    private float spawnTimer;

    // Movement settings
    public float itemMoveSpeed = 1f; // cells per second
    private int nextItemId = 1;
    
    // Item management settings
    public float itemTimeoutOnBlankCells = 10f; // seconds items stay on blank cells before disappearing
    public int maxItemsOnGrid = 100; // maximum items allowed on grid
    public bool showItemLimitWarning = true; // whether to show warning when limit reached

    // Credits system
    [Header("Credits System")]
    [Tooltip("Starting credits amount for new games")]
    public int startingCredits = 200; // Enough for 1 spawner (50) + 5 conveyors (100) + 1 seller (50)
    private int currentCredits = 0;

    private Direction lastMachineDirection = Direction.Up;
    
    // Machine placement state
    private MachineDef selectedMachine;
    private MachineBarUIManager machineBarManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        Debug.Log("GameManager Start() called.");

        TextAsset machinesAsset = Resources.Load<TextAsset>("machines");
        string machinesJson = machinesAsset.text;

        TextAsset recipesAsset = Resources.Load<TextAsset>("recipes");
        string recipesJson = recipesAsset.text;

        TextAsset itemsAsset = Resources.Load<TextAsset>("items");
        string itemsJson = itemsAsset.text;
        
        FactoryRegistry.Instance.LoadFromJson(machinesJson, recipesJson, itemsJson);
        Debug.Log("Factory definitions loaded.");

        // Get reference to machine bar manager
        machineBarManager = FindFirstObjectByType<MachineBarUIManager>();
        machineBarManager?.InitBar();

        // Check if a save file exists
        string path = Application.persistentDataPath + "/" + SAVE_FILE_NAME;
        if (File.Exists(path))
        {
            Debug.Log("Save file found. Loading saved grid.");
            LoadGame(); // Only load the data from disk
        }
        else
        {
            Debug.Log("No save file found. Creating a new default grid.");
            
            // Initialize starting credits for new game
            currentCredits = startingCredits;
            Debug.Log($"New game started with {currentCredits} credits");
            
            GridData defaultGrid = new GridData();
            defaultGrid.width = 5;
            defaultGrid.height = 7;

            for (int y = 0; y < defaultGrid.height; y++)
            {
                for (int x = 0; x < defaultGrid.width; x++)
                {
                    CellRole role = CellRole.Grid;
                    if (y == 0)
                        role = CellRole.Top;
                    else if (y == defaultGrid.height - 1)
                        role = CellRole.Bottom;
                    defaultGrid.cells.Add(new CellData
                    {
                        x = x,
                        y = y,
                        cellType = CellType.Blank,
                        direction = Direction.Up,
                        cellRole = role
                    });
                }
            }
            activeGrids.Add(defaultGrid);
        }

        // Regardless of whether a new or loaded grid was prepared, build the visuals once
        UIGridManager gridManager = FindAnyObjectByType<UIGridManager>();
        if (gridManager != null)
        {
            gridManager.InitGrid(activeGrids[0]);
        }
        
        // Initialize credits display
        Credits.RefreshDisplay();
    }

    // === CREDITS SYSTEM METHODS ===
    
    /// <summary>
    /// Get current credits amount
    /// </summary>
    /// <returns>Current credits</returns>
    public int GetCredits()
    {
        return currentCredits;
    }
    
    /// <summary>
    /// Add credits (e.g., when selling items)
    /// </summary>
    /// <param name="amount">Amount to add</param>
    public void AddCredits(int amount)
    {
        currentCredits += amount;
        Debug.Log($"Added {amount} credits. Total: {currentCredits}");
        Credits.RefreshDisplay();
        
        // Update machine bar to enable/disable buttons based on new credits
        machineBarManager?.UpdateAffordability();
    }
    
    /// <summary>
    /// Try to spend credits (e.g., when placing machines)
    /// </summary>
    /// <param name="amount">Amount to spend</param>
    /// <returns>True if successful, false if insufficient credits</returns>
    public bool TrySpendCredits(int amount)
    {
        if (currentCredits >= amount)
        {
            currentCredits -= amount;
            Debug.Log($"Spent {amount} credits. Remaining: {currentCredits}");
            Credits.RefreshDisplay();
            
            // Update machine bar to enable/disable buttons based on new credits
            machineBarManager?.UpdateAffordability();
            return true;
        }
        else
        {
            Debug.LogWarning($"Insufficient credits! Need {amount}, have {currentCredits}");
            return false;
        }
    }
    
    /// <summary>
    /// Check if player can afford a specific amount
    /// </summary>
    /// <param name="amount">Amount to check</param>
    /// <returns>True if affordable</returns>
    public bool CanAfford(int amount)
    {
        return currentCredits >= amount;
    }

    public void SetSelectedMachine(MachineDef machine)
    {
        selectedMachine = machine;
        Debug.Log($"Selected machine set to: {machine?.id ?? "null"}");
    }

    public void OnCellClicked(int x, int y)
    {
        GridData gridData = activeGrids[0];
        CellData cellData = GetCellData(gridData, x, y);

        if (cellData == null)
        {
            Debug.LogError("Clicked cell is not in the data model! Coords: " + x + ", " + y);
            return;
        }

        Debug.Log($"Cell clicked at ({x}, {y}): cellType={cellData.cellType}, machineDefId={cellData.machineDefId}, selectedMachine={selectedMachine?.id ?? "null"}");

        // Handle machine rotation if clicking on an existing machine (should work regardless of selection)
        if (cellData.cellType == UICell.CellType.Machine && !string.IsNullOrEmpty(cellData.machineDefId))
        {
            Debug.Log($"Attempting to rotate machine {cellData.machineDefId} at ({x}, {y})");
            RotateMachine(cellData);
            return;
        }

        // Handle machine placement if a machine is selected
        if (selectedMachine != null)
        {
            if (IsValidMachinePlacement(cellData, selectedMachine))
            {
                // Check if player can afford the machine
                if (CanAfford(selectedMachine.cost))
                {
                    PlaceMachine(cellData, selectedMachine);
                    // Don't clear selection - allow continuous placement
                    // Selection will be cleared when user clicks a different machine or manually clears
                    return;
                }
                else
                {
                    Debug.LogWarning($"Cannot afford {selectedMachine.id} - costs {selectedMachine.cost}, have {currentCredits} credits");
                    return;
                }
            }
            else
            {
                Debug.Log($"Cannot place {selectedMachine.id} here - invalid placement");
                return;
            }
        }

        // Only allow fallback behavior for Top/Bottom roles when no machine is selected
        // This preserves the original spawner/seller placement behavior
        if (selectedMachine == null && (cellData.cellRole == UICell.CellRole.Top || cellData.cellRole == UICell.CellRole.Bottom))
        {
            // Original cell cycling logic for Top/Bottom roles only
            UICell.CellType newType = cellData.cellType;
            UICell.Direction newDirection = cellData.direction;

            switch (cellData.cellRole)
            {
                case UICell.CellRole.Top:
                    Debug.Log("Clicked on Top role. Creating a placeholder Output machine.");
                    newType = UICell.CellType.Machine;
                    newDirection = UICell.Direction.Up;
                    cellData.machineDefId = "seller"; // Use seller machine for top
                    break;

                case UICell.CellRole.Bottom:
                    Debug.Log("Clicked on Bottom role. Creating a placeholder Input machine.");
                    newType = UICell.CellType.Machine;
                    newDirection = UICell.Direction.Up;
                    cellData.machineDefId = "spawner"; // Use spawner machine for bottom
                    break;
            }

            cellData.cellType = newType;
            cellData.direction = newDirection;

            UIGridManager activeGridManager = FindAnyObjectByType<UIGridManager>();
            if (activeGridManager != null)
            {
                activeGridManager.UpdateCellVisuals(cellData.x, cellData.y, newType, newDirection, cellData.machineDefId);
            }

            // Only try to move items if the cell is now a machine
            if (cellData.cellType == UICell.CellType.Machine)
            {
                foreach (var item in cellData.items)
                {
                    item.shouldStopAtTarget = false;
                    item.hasCheckedMiddle = false;
                    item.hasQueuedMovement = false;

                    // If not already moving, start movement in the new direction/type
                    if (!item.isMoving)
                    {
                        TryStartItemMovement(item, cellData, gridData, activeGridManager);
                    }
                }
            }
            return;
        }

        // If no machine is selected and this is a Grid cell, don't do anything
        if (selectedMachine == null && cellData.cellRole == UICell.CellRole.Grid)
        {
            Debug.Log("No machine selected for grid placement");
            return;
        }
    }

    private bool IsValidMachinePlacement(CellData cellData, MachineDef machineDef)
    {
        // Check if machine's grid placement rules allow this cell
        foreach (string placement in machineDef.gridPlacement)
        {
            switch (placement.ToLower())
            {
                case "any":
                    return true;
                case "grid":
                    return cellData.cellRole == UICell.CellRole.Grid;
                case "top":
                    return cellData.cellRole == UICell.CellRole.Top;
                case "bottom":
                    return cellData.cellRole == UICell.CellRole.Bottom;
            }
        }
        return false;
    }

    private void PlaceMachine(CellData cellData, MachineDef machineDef)
    {
        Debug.Log($"Placing machine {machineDef.id} at ({cellData.x}, {cellData.y})");
        
        // Deduct credits for machine placement
        if (!TrySpendCredits(machineDef.cost))
        {
            Debug.LogError($"Failed to place machine {machineDef.id} - insufficient credits!");
            return;
        }
        
        // Process any existing items in the cell by the new machine
        UIGridManager activeGridManager = FindAnyObjectByType<UIGridManager>();
        if (activeGridManager != null && cellData.items.Count > 0)
        {
            Debug.Log($"Processing {cellData.items.Count} existing items with new machine {machineDef.id}");
            
            // Create a copy of the items list to avoid modification during iteration
            var itemsToProcess = new List<ItemData>(cellData.items);
            
            foreach (var item in itemsToProcess)
            {
                ProcessItemAtMachine(item, cellData, machineDef);
            }
        }
        
        // Set cell to machine type with the specific machine definition
        cellData.cellType = UICell.CellType.Machine;
        cellData.machineDefId = machineDef.id;
        cellData.direction = lastMachineDirection; // Use last placed machine direction

        // Update visuals
        if (activeGridManager != null)
        {
            activeGridManager.UpdateCellVisuals(cellData.x, cellData.y, cellData.cellType, cellData.direction, cellData.machineDefId);
        }
    }

    private void RotateMachine(CellData cellData)
    {
        // Rotate the machine's direction
        cellData.direction = (UICell.Direction)(((int)cellData.direction + 1) % 4);
        
        // Store this as the last machine direction for future placements
        lastMachineDirection = cellData.direction;
        
        Debug.Log($"Rotating machine at ({cellData.x}, {cellData.y}) to direction: {cellData.direction}");
        
        // Update visuals
        UIGridManager activeGridManager = FindAnyObjectByType<UIGridManager>();
        if (activeGridManager != null)
        {
            activeGridManager.UpdateCellVisuals(cellData.x, cellData.y, cellData.cellType, cellData.direction, cellData.machineDefId);
        }
    }

    private void ProcessItemAtMachine(ItemData item, CellData cellData, MachineDef machineDef)
    {
        Debug.Log($"Processing item {item.id} at machine {machineDef.id}");
        
        // For now, implement basic machine processing:
        // - Spawner: should not process items (items shouldn't be placed on spawners)
        // - Seller: consume the item and remove it from grid, add credits
        // - Conveyor and other machines: let item continue through normal flow
        
        if (machineDef.id == "seller")
        {
            // Get item definition to check sell value
            ItemDef itemDef = FactoryRegistry.Instance.GetItem(item.itemType);
            int sellValue = itemDef?.sellValue ?? 0;
            
            Debug.Log($"Item {item.id} ({item.itemType}) sold by seller machine for {sellValue} credits");
            
            // Add credits for selling the item
            if (sellValue > 0)
            {
                AddCredits(sellValue);
            }
            
            // Stop any movement immediately to prevent visual updates after destruction
            item.isMoving = false;
            item.shouldStopAtTarget = true;
            item.hasQueuedMovement = false;
            
            cellData.items.Remove(item);
            
            UIGridManager activeGridManager = FindAnyObjectByType<UIGridManager>();
            if (activeGridManager != null)
            {
                activeGridManager.DestroyVisualItem(item.id);
            }
        }
        else
        {
            Debug.Log($"Item {item.id} will continue through machine {machineDef.id} normally");
            // For conveyors and other machines, items will be processed in the normal movement flow
            // Reset item state since it's now on a machine
            item.isOnBlankCell = false;
            item.timeOnBlankCell = 0f;
            
            // If item was moving, stop it so it can be processed by the new machine
            if (item.isMoving)
            {
                item.isMoving = false;
                item.shouldStopAtTarget = true;
                item.hasQueuedMovement = false;
                Debug.Log($"Stopped moving item {item.id} to be processed by new machine {machineDef.id}");
            }
        }
    }

    private void HandleItemTimeoutAndLimits()
    {
        GridData gridData = activeGrids[0];
        UIGridManager gridManager = FindAnyObjectByType<UIGridManager>();
        if (gridManager == null) return;

        // Count total items on grid and handle timeouts
        int totalItems = 0;
        List<ItemData> itemsToRemove = new List<ItemData>();
        List<CellData> cellsWithItemsToRemove = new List<CellData>();

        foreach (var cell in gridData.cells)
        {
            totalItems += cell.items.Count;

            // Handle timeout for items on blank cells
            for (int i = cell.items.Count - 1; i >= 0; i--)
            {
                ItemData item = cell.items[i];

                // Update blank cell time tracking
                if (cell.cellType == CellType.Blank)
                {
                    if (!item.isOnBlankCell)
                    {
                        // Item just arrived on blank cell
                        item.isOnBlankCell = true;
                        item.timeOnBlankCell = 0f;
                        Debug.Log($"Item {item.id} arrived on blank cell at ({cell.x}, {cell.y})");
                    }
                    else
                    {
                        // Item has been on blank cell, update timer
                        item.timeOnBlankCell += Time.deltaTime;

                        if (item.timeOnBlankCell >= itemTimeoutOnBlankCells)
                        {
                            Debug.Log($"Item {item.id} timed out on blank cell after {item.timeOnBlankCell:F1} seconds");
                            itemsToRemove.Add(item);
                            cellsWithItemsToRemove.Add(cell);
                        }
                    }
                }
                else
                {
                    // Item is no longer on blank cell
                    if (item.isOnBlankCell)
                    {
                        item.isOnBlankCell = false;
                        item.timeOnBlankCell = 0f;
                        Debug.Log($"Item {item.id} left blank cell and moved to machine {cell.machineDefId}");
                    }
                }
            }
        }

        // Remove timed-out items
        for (int i = 0; i < itemsToRemove.Count; i++)
        {
            var item = itemsToRemove[i];
            var cell = cellsWithItemsToRemove[i];
            
            cell.items.Remove(item);
            gridManager.DestroyVisualItem(item.id);
        }

        // Check grid item limit
        if (totalItems >= maxItemsOnGrid && showItemLimitWarning)
        {
            Debug.LogWarning($"Grid item limit reached! {totalItems}/{maxItemsOnGrid} items. Consider adding more sellers or increasing the limit.");
            // You could add UI notification here in the future
        }
    }

    private bool didSpawn = false;
    private void Update()
    {
        // Handle item spawning
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0 && !didSpawn)
        {

            spawnTimer = spawnInterval;

            // Find all spawner machines in our data model
            GridData gridData = activeGrids[0];
            
            // Check current item count before spawning
            int currentItemCount = 0;
            foreach (var cell in gridData.cells)
            {
                currentItemCount += cell.items.Count;
            }
            
            if (currentItemCount >= maxItemsOnGrid)
            {
                if (showItemLimitWarning)
                {
                    Debug.LogWarning($"Cannot spawn new items: Grid limit reached ({currentItemCount}/{maxItemsOnGrid})");
                }
                return;
            }
            
            foreach (var cell in gridData.cells)
            {
                if (cell.cellType == UICell.CellType.Machine && cell.machineDefId == "spawner")
                {
                    // Check if the cell already has an item
                    if (cell.items.Count == 0)
                    {
                      //  didSpawn = true;
                        // Spawn a new item if the cell is empty (and grid limit allows)
                        SpawnItem(cell);
                    }
                }
            }
        }

        // Handle item timeout on blank cells and grid limits
        HandleItemTimeoutAndLimits();

        // Handle item movement
        ProcessItemMovement();
    }

    private void ProcessItemMovement()
    {
        GridData gridData = activeGrids[0];
        UIGridManager gridManager = FindAnyObjectByType<UIGridManager>();
        if (gridManager == null) return;

        foreach (var cell in gridData.cells)
        {
            for (int i = cell.items.Count - 1; i >= 0; i--)
            {
                ItemData item = cell.items[i];

                if (item.isMoving)
                {
                    // Update movement progress
                    float timeSinceStart = Time.time - item.moveStartTime;
                    item.moveProgress = timeSinceStart * itemMoveSpeed;

                    if (item.moveProgress >= 1.0f)
                    {
                        // Movement complete - transfer item to target cell
                        CompleteItemMovement(item, cell, gridData, gridManager);
                        i--; // Account for item being removed from current cell
                    }
                    else
                    {
                        // Check if we've reached the middle of movement and need to decide next action
                        if (item.moveProgress >= 0.5f && !item.hasCheckedMiddle)
                        {
                            CheckItemAtCellMiddle(item, gridData);
                        }

                        // Update visual position with proper parent handover timing
                        // Only update if the visual item still exists (might have been destroyed by machine processing)
                        if (gridManager.HasVisualItem(item.id))
                        {
                            gridManager.UpdateItemVisualPosition(item.id, item.moveProgress, cell.x, cell.y, item.targetX, item.targetY, cell.direction);
                        }
                    }
                }
                else
                {
                    if (cell.cellType != CellType.Blank && !item.shouldStopAtTarget /* && !item.hasStopped */)
                    {
                        // Check if item can start moving
                        TryStartItemMovement(item, cell, gridData, gridManager);
                    }
                }
            }
        }

    }

    private void CheckItemAtCellMiddle(ItemData item, GridData gridData)
    {
        item.hasCheckedMiddle = true;

        // Find the target cell we're moving towards
        CellData targetCell = GetCellData(gridData, item.targetX, item.targetY);
        if (targetCell == null)
        {
            item.shouldStopAtTarget = true;
            return;
        }

        if (targetCell.cellType == CellType.Blank)
        {
            // Items can move into blank cells and stop there (intended behavior)
            item.shouldStopAtTarget = true;
            Debug.Log($"Item {item.id} moving to blank cell - will stop there");
            return;
        }
        else if (targetCell.cellType == CellType.Machine)
        {
            // Moving to machine - will be processed
            Debug.Log($"Item {item.id} moving to machine {targetCell.machineDefId} - will be processed");
            // TODO: Process item at machine (crafting, upgrades, etc.)

            // After processing, determine next movement direction
            int nextX, nextY;
            GetNextCellCoordinates(targetCell, out nextX, out nextY);
            CellData nextCell = GetCellData(gridData, nextX, nextY);

            if (nextCell != null)
            {
                // Queue up the next movement
                item.queuedTargetX = nextX;
                item.queuedTargetY = nextY;
                item.hasQueuedMovement = true;
                Debug.Log($"Item {item.id} will continue to ({nextX}, {nextY}) after machine processing");
            }
            else
            {
                item.shouldStopAtTarget = true;
            }
        }
    }

    private void CompleteItemMovement(ItemData item, CellData sourceCell, GridData gridData, UIGridManager gridManager)
    {
        // Find target cell
        CellData targetCell = GetCellData(gridData, item.targetX, item.targetY);

        if (targetCell == null)
        {
            Debug.LogError($"Target cell not found at ({item.targetX}, {item.targetY})");
            return;
        }

        // Remove from source cell
        sourceCell.items.Remove(item);

        // Check if target is seller machine
        if (targetCell.cellType == CellType.Machine && targetCell.machineDefId == "seller")
        {
            // Get item definition to check sell value
            ItemDef itemDef = FactoryRegistry.Instance.GetItem(item.itemType);
            int sellValue = itemDef?.sellValue ?? 0;
            
            Debug.Log($"Item {item.id} ({item.itemType}) reached seller machine - selling for {sellValue} credits");
            
            // Add credits for selling the item
            if (sellValue > 0)
            {
                AddCredits(sellValue);
            }
            
            gridManager.DestroyVisualItem(item.id);
            return;
        }

        // Add to target cell and reset movement state
        item.isMoving = false;
        item.moveProgress = 0f;
        targetCell.items.Add(item);

        // Update blank cell tracking based on target cell type
        if (targetCell.cellType == CellType.Blank)
        {
            item.isOnBlankCell = true;
            item.timeOnBlankCell = 0f;
            Debug.Log($"Item {item.id} moved to blank cell at ({targetCell.x}, {targetCell.y})");
        }
        else
        {
            item.isOnBlankCell = false;
            item.timeOnBlankCell = 0f;
        }

        // Update visual position to exact target
        gridManager.UpdateItemVisualPosition(item.id, 1f, sourceCell.x, sourceCell.y, item.targetX, item.targetY, sourceCell.direction);

        if (item.hasQueuedMovement && !item.shouldStopAtTarget)
        {
            // Start the queued movement immediately
            item.targetX = item.queuedTargetX;
            item.targetY = item.queuedTargetY;
            item.isMoving = true;
            item.moveStartTime = Time.time;
            item.moveProgress = 0f;
            item.hasCheckedMiddle = false;
            item.hasQueuedMovement = false;

            Debug.Log($"Starting queued movement for item {item.id} to ({item.targetX}, {item.targetY})");
        }

        // Reset flags
        item.shouldStopAtTarget = false;
        item.hasQueuedMovement = false;
    }

    private void TryStartItemMovement(ItemData item, CellData cell, GridData gridData, UIGridManager gridManager)
    {
        // Don't start movement from blank cells
        if (cell.cellType == CellType.Blank)
        {
            return;
        }

        // Determine next cell based on current cell type and direction
        int nextX, nextY;
        GetNextCellCoordinates(cell, out nextX, out nextY);

        // Check if next cell exists
        CellData nextCell = GetCellData(gridData, nextX, nextY);
        if (nextCell == null)
        {
            return; // No movement possible
        }

        // Allow movement into blank cells - items will stop there
        // Removed the blank cell check as items should be able to move into blank spaces

        // Start movement
        item.isMoving = true;
        item.targetX = nextX;
        item.targetY = nextY;
        item.moveStartTime = Time.time;
        item.moveProgress = 0f;

        Debug.Log($"Starting movement for item {item.id} from ({cell.x},{cell.y}) to ({nextX},{nextY})");
    }

    private void GetNextCellCoordinates(CellData cell, out int nextX, out int nextY)
    {
        nextX = cell.x;
        nextY = cell.y;

        // For spawner machines, items move in the "up" direction (toward the grid)
        if (cell.cellType == CellType.Machine && cell.machineDefId == "spawner")
        {
            nextY--;
            return;
        }

        // For conveyors and other machines, use their direction
        switch (cell.direction)
        {
            case Direction.Up: nextY--; break;
            case Direction.Right: nextX++; break;
            case Direction.Down: nextY++; break;
            case Direction.Left: nextX--; break;
        }
    }

    private void SpawnItem(CellData cellData)
    {
        Debug.Log($"Spawning new item at ({cellData.x}, {cellData.y})");

        // Get spawner machine definition to determine what item to spawn
        MachineDef spawnerDef = FactoryRegistry.Instance.GetMachine(cellData.machineDefId);
        string itemType = "can"; // Default item type
        
        // Use first spawnable item from spawner definition if available
        if (spawnerDef != null && spawnerDef.spawnableItems != null && spawnerDef.spawnableItems.Count > 0)
        {
            itemType = spawnerDef.spawnableItems[0]; // Use first spawnable item for now
        }

        // Create new item with unique ID
        ItemData newItem = new ItemData
        {
            id = "item_" + nextItemId++,
            itemType = itemType, // Use actual item type instead of "Placeholder"
            isMoving = false,
            moveProgress = 0f,
            isOnBlankCell = false,
            timeOnBlankCell = 0f
        };

        cellData.items.Add(newItem);

        // Tell visual manager to create visual representation
        UIGridManager gridManager = FindAnyObjectByType<UIGridManager>();
        if (gridManager != null)
        {
            gridManager.CreateVisualItem(newItem.id, cellData.x, cellData.y);
        }
    }

    public void ClearGrid()
    {
        Debug.Log("Clearing grid...");
        GridData gridData = activeGrids[0];

        UIGridManager activeGridManager = FindAnyObjectByType<UIGridManager>();

        foreach (var cell in gridData.cells)
        {
            // Destroy visual items before clearing data
            if (activeGridManager != null)
            {
                foreach (var item in cell.items)
                {
                    activeGridManager.DestroyVisualItem(item.id);
                }
            }

            cell.cellType = CellType.Blank;
            cell.direction = Direction.Up;
            cell.machineDefId = null; // Clear machine definition reference
            cell.items.Clear();
        }

        if (activeGridManager != null)
        {
            activeGridManager.UpdateAllVisuals();
        }
    }

    private CellData GetCellData(GridData grid, int x, int y)
    {
        foreach (var cell in grid.cells)
        {
            if (cell.x == x && cell.y == y)
            {
                return cell;
            }
        }
        return null;
    }

    public void SaveGame()
    {
        GameData data = new GameData 
        { 
            grids = activeGrids,
            credits = currentCredits
        };
        
        // Save user machine progress to game data
        FactoryRegistry.Instance.SaveToGameData(data);
        
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(Application.persistentDataPath + "/" + SAVE_FILE_NAME, json);
        Debug.Log($"Game saved with {currentCredits} credits to location " + Application.persistentDataPath + "/" + SAVE_FILE_NAME + "!");
    }

    private void LoadGame()
    {
        string path = Application.persistentDataPath + "/" + SAVE_FILE_NAME;
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            GameData data = JsonUtility.FromJson<GameData>(json);
            activeGrids = data.grids;
            
            // Load credits from save data
            currentCredits = data.credits;
            Debug.Log($"Loaded {currentCredits} credits from save file");

            FactoryRegistry.Instance.LoadFromGameData(data);

            // Find highest item ID to continue sequence
            foreach (var grid in activeGrids)
            {
                foreach (var cell in grid.cells)
                {
                    foreach (var item in cell.items)
                    {
                        if (item.id.StartsWith("item_"))
                        {
                            string idStr = item.id.Substring(5);
                            if (int.TryParse(idStr, out int id))
                            {
                                nextItemId = Mathf.Max(nextItemId, id + 1);
                            }
                        }
                    }
                }
            }

            Debug.Log("Game loaded!");
            
            // Update credits display after loading
            Credits.RefreshDisplay();
        }
    }

    public void ManualLoadGame()
    {
        Debug.Log("Manual Load button clicked.");
        LoadGame(); // Load the data from disk

        UIGridManager activeGridManager = FindAnyObjectByType<UIGridManager>();
        if (activeGridManager != null)
        {
            activeGridManager.InitGrid(activeGrids[0]);

            // Recreate visual items for loaded data
            GridData gridData = activeGrids[0];
            foreach (var cell in gridData.cells)
            {
                foreach (var item in cell.items)
                {
                    activeGridManager.CreateVisualItem(item.id, cell.x, cell.y);
                }
            }
        }
        
        // Refresh credits display and machine affordability after manual load
        Credits.RefreshDisplay();
        machineBarManager?.UpdateAffordability();
    }
}