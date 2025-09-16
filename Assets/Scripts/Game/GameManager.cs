using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
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
    public int startingCredits = 2000; // Enough for 1 spawner (50) + 5 conveyors (100) + 1 seller (50)
    private int currentCredits = 0;
    private CreditsUI creditsUI;

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
        
        // Get reference to credits UI
        creditsUI = FindFirstObjectByType<CreditsUI>();

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
        UpdateCreditsDisplay();
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
    /// Update the credits UI display
    /// </summary>
    private void UpdateCreditsDisplay()
    {
        if (creditsUI != null)
        {
            creditsUI.UpdateCredits(currentCredits);
        }
        machineBarManager?.UpdateAffordability();
    }
    
    /// <summary>
    /// Add credits (e.g., when selling items)
    /// </summary>
    /// <param name="amount">Amount to add</param>
    public void AddCredits(int amount)
    {
        currentCredits += amount;
        Debug.Log($"Added {amount} credits. Total: {currentCredits}");
        UpdateCreditsDisplay();
        
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
            UpdateCreditsDisplay();
            
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
            Debug.Log($"Machine type: {cellData.cellType}, MachineDefId: {cellData.machineDefId}");
            
            // Get machine definition to check canRotate
            MachineDef machineDef = FactoryRegistry.Instance.GetMachine(cellData.machineDefId);
            if (machineDef != null)
            {
                Debug.Log($"Machine {cellData.machineDefId} canRotate: {machineDef.canRotate}");
            }
            else
            {
                Debug.LogError($"Could not find machine definition for {cellData.machineDefId}");
            }
            
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
                    // Reset item state when cell changes
                    // (Removed references to fields that no longer exist)

                    // If not already moving, start movement in the new direction/type
                    if (item.state == ItemState.Idle)
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
        
        // Set direction based on whether the machine can be rotated
        if (machineDef.canRotate)
        {
            cellData.direction = lastMachineDirection; // Use last placed machine direction
        }
        else
        {
            cellData.direction = Direction.Up; // Default to "up" for non-rotatable machines
        }

        // Update visuals
        if (activeGridManager != null)
        {
            activeGridManager.UpdateCellVisuals(cellData.x, cellData.y, cellData.cellType, cellData.direction, cellData.machineDefId);
        }
    }

    private void RotateMachine(CellData cellData)
    {
        // Get the machine definition to check if it can be rotated
        MachineDef machineDef = FactoryRegistry.Instance.Machines.GetValueOrDefault(cellData.machineDefId);
        if (machineDef == null)
        {
            Debug.LogError($"Cannot find machine definition for {cellData.machineDefId}");
            return;
        }
        
        // Check if this machine can be rotated
        if (!machineDef.canRotate)
        {
            Debug.Log($"Machine {machineDef.id} cannot be rotated (canRotate = false)");
            return;
        }
        
        Debug.Log($"Rotating machine {machineDef.id} - current direction: {cellData.direction}");
        
        // Rotate the machine's direction
        cellData.direction = (UICell.Direction)(((int)cellData.direction + 1) % 4);
        
        Debug.Log($"New direction after rotation: {cellData.direction}");
        
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
            item.state = ItemState.Idle;
            
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
            // (Removed blank cell tracking as it's no longer needed)
            
            // If item was moving, stop it so it can be processed by the new machine
            if (item.state == ItemState.Moving)
            {
                item.state = ItemState.Idle;
                Debug.Log($"Stopped moving item {item.id} to be processed by new machine {machineDef.id}");
            }
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

        // Handle item movement
        ProcessItemMovement();
    }

    /// <summary>
    /// Main item movement processing loop following new architectural principles.
    /// Single point of control for all item state transitions, operating on master list of items.
    /// </summary>
    private void ProcessItemMovement()
    {
        GridData gridData = activeGrids[0];
        UIGridManager gridManager = FindAnyObjectByType<UIGridManager>();
        if (gridManager == null) return;

        // Process all items based on their current state
        foreach (var cell in gridData.cells)
        {
            for (int i = cell.items.Count - 1; i >= 0; i--)
            {
                ItemData item = cell.items[i];

                switch (item.state)
                {
                    case ItemState.Idle:
                        // Try to start movement for idle items
                        TryStartItemMovement(item, cell, gridData, gridManager);
                        break;

                    case ItemState.Moving:
                        ProcessMovingItem(item, cell, gridData, gridManager, ref i);
                        break;

                    case ItemState.Waiting:
                        ProcessWaitingItem(item, cell, gridManager);
                        break;

                    case ItemState.Processing:
                        ProcessProcessingItem(item, cell, gridData, gridManager, ref i);
                        break;
                }
            }
        }

        // Implement the "pull" system - check for waiting items that can be processed
        ProcessWaitingItemsPullSystem(gridData, gridManager);
    }

    /// <summary>
    /// New movement initiation logic using destination-based behavior.
    /// Checks target cell's GetRecipeDuration instead of hardcoded machine IDs.
    /// </summary>
    private void TryStartItemMovement(ItemData item, CellData cell, GridData gridData, UIGridManager gridManager)
    {
        // Only idle items can start moving
        if (item.state != ItemState.Idle) return;
        
        // Don't start movement from blank cells
        if (cell.cellType == CellType.Blank) return;

        // Determine next cell based on current cell type and direction
        int nextX, nextY;
        GetNextCellCoordinates(cell, out nextX, out nextY);

        // Check if next cell exists
        CellData nextCell = GetCellData(gridData, nextX, nextY);
        if (nextCell == null) return;

        // NEW LOGIC: Use destination-based behavior with GetRecipeDuration
        float recipeDuration = nextCell.GetRecipeDuration(item.itemType);
        
        if (recipeDuration > 0)
        {
            // Target cell has a machine with processing time > 0
            if (nextCell.machineState == MachineState.Idle)
            {
                // Machine is available - item can move and will be processed
                item.state = ItemState.Moving;
                item.sourceX = cell.x;
                item.sourceY = cell.y;
                item.targetX = nextX;
                item.targetY = nextY;
                item.moveStartTime = Time.time;
                item.moveProgress = 0f;
                
                Debug.Log($"Item {item.id} starting movement to processing machine at ({nextX}, {nextY}) - duration: {recipeDuration}s");
            }
            else
            {
                // Machine is busy - item will move and wait
                item.state = ItemState.Waiting;
                item.sourceX = cell.x;
                item.sourceY = cell.y;
                item.targetX = nextX;
                item.targetY = nextY;
                item.waitingStartTime = Time.time;
                
                // Move to waiting list
                cell.items.Remove(item);
                nextCell.waitingItems.Add(item);
                
                Debug.Log($"Item {item.id} moved to waiting list for busy machine at ({nextX}, {nextY})");
            }
        }
        else
        {
            // Target cell is conveyor, seller, or blank - item moves normally
            item.state = ItemState.Moving;
            item.sourceX = cell.x;
            item.sourceY = cell.y;
            item.targetX = nextX;
            item.targetY = nextY;
            item.moveStartTime = Time.time;
            item.moveProgress = 0f;
            
            Debug.Log($"Item {item.id} starting movement to non-processing cell at ({nextX}, {nextY})");
        }
    }

    /// <summary>
    /// Processes items in Moving state. Handles animation and completion logic.
    /// </summary>
    private void ProcessMovingItem(ItemData item, CellData cell, GridData gridData, UIGridManager gridManager, ref int index)
    {
        // Update movement progress
        float timeSinceStart = Time.time - item.moveStartTime;
        item.moveProgress = timeSinceStart * itemMoveSpeed;

        if (item.moveProgress >= 1.0f)
        {
            // Movement complete
            CompleteItemMovement(item, cell, gridData, gridManager);
            index--; // Account for item being removed from current cell
        }
        else
        {
            // Update visual position
            if (gridManager.HasVisualItem(item.id))
            {
                gridManager.UpdateItemVisualPosition(item.id, item.moveProgress, 
                    item.sourceX, item.sourceY, item.targetX, item.targetY, cell.direction);
            }
        }
    }

    /// <summary>
    /// Processes items in Waiting state. Updates visual position for queuing.
    /// </summary>
    private void ProcessWaitingItem(ItemData item, CellData cell, UIGridManager gridManager)
    {
        // Check for timeout (30 seconds)
        float waitingElapsed = Time.time - item.waitingStartTime;
        if (waitingElapsed >= 30f)
        {
            Debug.LogWarning($"Item {item.id} has been waiting for {waitingElapsed:F1} seconds - destroying due to timeout");
            
            // Remove from waiting list
            cell.waitingItems.Remove(item);
            
            // Destroy visual
            if (gridManager.HasVisualItem(item.id))
            {
                gridManager.DestroyVisualItem(item.id);
            }
        }
        else
        {
            // Update visual position for waiting items (queuing behavior)
            UpdateWaitingItemVisualPosition(item, cell, gridManager);
        }
    }

    /// <summary>
    /// Processes items in Processing state. Handles recipe completion.
    /// </summary>
    private void ProcessProcessingItem(ItemData item, CellData cell, GridData gridData, UIGridManager gridManager, ref int index)
    {
        // Check if processing is complete
        float processingElapsed = Time.time - item.processingStartTime;
        if (processingElapsed >= item.processingDuration)
        {
            CompleteRecipeProcessing(item, cell, gridData, gridManager);
            index--; // Account for item being potentially removed/replaced
        }
    }

    /// <summary>
    /// Pull system: checks for waiting items that can be processed when machines become idle.
    /// Implements the separate loop as specified in the architectural design.
    /// </summary>
    private void ProcessWaitingItemsPullSystem(GridData gridData, UIGridManager gridManager)
    {
        foreach (var cell in gridData.cells)
        {
            // If machine is idle AND has waiting items, pull the first one
            if (cell.machineState == MachineState.Idle && cell.waitingItems.Count > 0)
            {
                ItemData waitingItem = cell.waitingItems[0];
                cell.waitingItems.RemoveAt(0);
                
                // Move to primary items list and set to processing
                cell.items.Add(waitingItem);
                waitingItem.state = ItemState.Processing;
                
                // Set up processing data
                float recipeDuration = cell.GetRecipeDuration(waitingItem.itemType);
                waitingItem.processingStartTime = Time.time;
                waitingItem.processingDuration = recipeDuration;
                
                // Update machine state
                cell.machineState = MachineState.Processing;
                
                Debug.Log($"Pulled item {waitingItem.id} from waiting queue to start processing (duration: {recipeDuration}s)");
            }
        }
    }

    /// <summary>
    /// Gets the next cell coordinates based on current cell direction.
    /// Used by item movement logic to determine where items should move.
    /// </summary>

    /// <summary>
    /// Completes item movement using destination-based behavior.
    /// Uses GetRecipeDuration instead of hardcoded machine ID checks.
    /// </summary>
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

        // Check if target is blank cell - destroy item
        if (targetCell.cellType == CellType.Blank)
        {
            Debug.Log($"Item {item.id} reached blank cell - destroying");
            gridManager.DestroyVisualItem(item.id);
            return;
        }

        // NEW LOGIC: Use destination-based behavior with GetRecipeDuration
        float recipeDuration = targetCell.GetRecipeDuration(item.itemType);
        
        if (recipeDuration > 0)
        {
            // Target cell has a machine with processing time > 0
            Debug.Log($"Item {item.id} reached processing machine at ({targetCell.x}, {targetCell.y}) - starting processing (duration: {recipeDuration}s)");
            
            // Start processing immediately
            item.state = ItemState.Processing;
            item.processingStartTime = Time.time;
            item.processingDuration = recipeDuration;
            item.x = targetCell.x;
            item.y = targetCell.y;
            targetCell.items.Add(item);
            
            // Set machine state to processing
            targetCell.machineState = MachineState.Processing;
            
            // Destroy visual item when entering machine for processing
            gridManager.DestroyVisualItem(item.id);
            
            return;
        }
        else if (targetCell.cellType == CellType.Machine && targetCell.machineDefId == "seller")
        {
            // Special case for seller machines - consume item and add credits
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
        else
        {
            // Target cell is conveyor or other non-processing machine - item continues normally
            Debug.Log($"Item {item.id} reached non-processing cell at ({targetCell.x}, {targetCell.y}) - continuing");
            
            item.state = ItemState.Idle;
            item.moveProgress = 0f;
            item.x = targetCell.x;
            item.y = targetCell.y;
            targetCell.items.Add(item);

            // Update visual position to exact target
            gridManager.UpdateItemVisualPosition(item.id, 1f, sourceCell.x, sourceCell.y, item.targetX, item.targetY, sourceCell.direction);
        }
    }

    /// <summary>
    /// Updated UpdateWaitingItemVisualPosition for visual queuing behavior.
    /// Items are visually "parked" at the entrance of the machine.
    /// </summary>
    private void UpdateWaitingItemVisualPosition(ItemData item, CellData machineCell, UIGridManager gridManager)
    {
        if (!gridManager.HasVisualItem(item.id)) return;

        // Calculate position at machine entrance based on queue position
        int itemIndex = machineCell.waitingItems.FindIndex(i => i.id == item.id);
        if (itemIndex == -1) itemIndex = 0; // Fallback to prevent errors
        
        Vector2 cellSize = gridManager.GetCellSize();
        
        // Position items at the entrance with a small offset for queuing
        float queueOffset = itemIndex * cellSize.y * 0.1f; // 10% of cell size per item in queue
        Vector3 basePos = gridManager.GetCellWorldPosition(machineCell.x, machineCell.y);
        Vector3 queuePos = basePos + Vector3.down * queueOffset;
        
        // Update visual position using the grid manager
        gridManager.UpdateItemVisualPosition(item.id, 0.33f, 
            item.sourceX, item.sourceY, item.targetX, item.targetY, Direction.Up);
    }

    /// <summary>
    /// Updated SpawnItem method to work with new ItemData structure.
    /// </summary>
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

        // Create new item with updated ItemData structure
        ItemData newItem = new ItemData
        {
            id = "item_" + nextItemId++,
            itemType = itemType,
            x = cellData.x,
            y = cellData.y,
            state = ItemState.Idle,
            moveProgress = 0f,
            processingStartTime = 0f,
            processingDuration = 0f,
            waitingStartTime = 0f,
            targetMoveProgress = 0f
        };

        cellData.items.Add(newItem);

        // Tell visual manager to create visual representation
        UIGridManager gridManager = FindAnyObjectByType<UIGridManager>();
        if (gridManager != null)
        {
            gridManager.CreateVisualItem(newItem.id, cellData.x, cellData.y, newItem.itemType);
        }
    }

    /// <summary>
    /// Updated CompleteRecipeProcessing method to work with new architecture.
    /// </summary>
    private void CompleteRecipeProcessing(ItemData item, CellData cell, GridData gridData, UIGridManager gridManager)
    {
        Debug.Log($"Completing processing for item {item.id} ({item.itemType}) after {item.processingDuration}s");
        
        // Look up the recipe again to get output items
        float recipeDuration = cell.GetRecipeDuration(item.itemType);
        RecipeDef recipe = FactoryRegistry.Instance.GetRecipe(cell.machineDefId, item.itemType);
        
        if (recipe != null)
        {
            // Remove input item (current item)
            Debug.Log($"Removing input item {item.id} ({item.itemType})");
            cell.items.Remove(item);
            gridManager.DestroyVisualItem(item.id);
            
            // Create output items
            foreach (var outputItem in recipe.outputItems)
            {
                for (int i = 0; i < outputItem.count; i++)
                {
                    // Create new output item with updated ItemData structure
                    ItemData newItem = new ItemData
                    {
                        id = "item_" + nextItemId++,
                        itemType = outputItem.item,
                        x = cell.x,
                        y = cell.y,
                        state = ItemState.Idle,
                        moveProgress = 0f,
                        processingStartTime = 0f,
                        processingDuration = 0f,
                        waitingStartTime = 0f,
                        targetMoveProgress = 0f
                    };
                    
                    cell.items.Add(newItem);
                    
                    // Create visual representation
                    gridManager.CreateVisualItem(newItem.id, cell.x, cell.y, newItem.itemType);
                    
                    Debug.Log($"Created output item {newItem.id} ({outputItem.item}) at ({cell.x}, {cell.y})");
                }
            }
        }
        else
        {
            Debug.LogError($"Recipe not found when completing processing for machine {cell.machineDefId} with item {item.itemType}");
        }
        
        // Set machine state back to idle so it can accept new items from the pull system
        cell.machineState = MachineState.Idle;
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
            UpdateCreditsDisplay();
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
                    activeGridManager.CreateVisualItem(item.id, cell.x, cell.y, item.itemType);
                }
            }
        }
        
        // Refresh credits display and machine affordability after manual load
        UpdateCreditsDisplay();
        machineBarManager?.UpdateAffordability();
    }
}