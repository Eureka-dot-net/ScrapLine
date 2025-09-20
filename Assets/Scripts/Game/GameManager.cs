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
    /// Generate a unique item ID for use by machine objects
    /// </summary>
    /// <returns>A unique item ID string</returns>
    public string GenerateItemId()
    {
        return "item_" + nextItemId++;
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

        // Create machine object using MachineFactory
        cellData.machine = MachineFactory.CreateMachine(cellData);
        if (cellData.machine == null)
        {
            Debug.LogError($"Failed to create machine object for {machineDef.id}");
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


    private void Update()
    {
        GridData gridData = activeGrids[0];
        
        // Check current item count for spawning limits
        int currentItemCount = 0;
        foreach (var cell in gridData.cells)
        {
            currentItemCount += cell.items.Count;
        }
        
        // Update all machine objects (replaces hardcoded spawner logic)
        foreach (var cell in gridData.cells)
        {
            // Create machine object if it doesn't exist and cell has a machine
            if (cell.machine == null && cell.cellType == UICell.CellType.Machine && !string.IsNullOrEmpty(cell.machineDefId))
            {
                cell.machine = MachineFactory.CreateMachine(cell);
            }
            
            // Update machine logic (replaces switch statements)
            if (cell.machine != null)
            {
                // For spawners, check item limit before allowing updates
                if (cell.machineDefId == "spawner" && currentItemCount >= maxItemsOnGrid)
                {
                    if (showItemLimitWarning)
                    {
                        Debug.LogWarning($"Cannot spawn new items: Grid limit reached ({currentItemCount}/{maxItemsOnGrid})");
                    }
                    continue; // Skip spawner updates when at limit
                }
                
                cell.machine.UpdateLogic();
            }
        }

        // Handle item movement - activate the pure pull system
        ProcessItemMovement();
    }

    // ============================================================================
    // PURE PULL SYSTEM - ITEM MOVEMENT ARCHITECTURE
    // ============================================================================
    // 
    // This system implements a pure "pull" architecture where:
    // 1. Items with recipes NEVER enter Moving state - they go directly to Waiting
    // 2. Only non-recipe targets (conveyors, sellers, blanks) use Moving state
    // 3. Processing machines "pull" items from their waiting queues when idle
    // 4. Movement and processing are completely separated concerns
    //
    // FLOW:
    // - Recipe target: Idle -> Waiting -> Processing -> Idle
    // - Non-recipe target: Idle -> Moving -> Idle
    // ============================================================================

    /// <summary>
    /// Main item movement processing loop with pure pull system architecture.
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
                        TryStartItemMovement(item, cell, gridData, gridManager);
                        break;

                    case ItemState.Moving:
                        ProcessMovingItem(item, cell, gridData, gridManager, ref i);
                        break;

                    case ItemState.Waiting:
                        ProcessWaitingItem(item, cell, gridManager);
                        break;

                    case ItemState.Processing:
                        // Processing is now handled by machine objects in their UpdateLogic() methods
                        // This case is kept for backward compatibility but should rarely be used
                        ProcessProcessingItem(item, cell, gridData, gridManager, ref i);
                        break;
                }
            }
        }

        // Machine objects now handle their own pull system logic via UpdateLogic()
        // No need for centralized ProcessWaitingItemsPullSystem anymore
    }

    /// <summary>
    /// Gets recipe definition for a machine and item type. Single source of truth for recipe checking.
    /// </summary>
    private RecipeDef GetRecipe(string machineDefId, string itemType)
    {
        if (string.IsNullOrEmpty(machineDefId) || string.IsNullOrEmpty(itemType))
            return null;
            
        return FactoryRegistry.Instance.GetRecipe(machineDefId, itemType);
    }

    /// <summary>
    /// Pure pull system: Items with recipes go directly to Waiting, others go to Moving.
    /// </summary>
    private void TryStartItemMovement(ItemData item, CellData cell, GridData gridData, UIGridManager gridManager)
    {
        if (item.state != ItemState.Idle) return;
        if (cell.cellType == CellType.Blank) return;

        int nextX, nextY;
        GetNextCellCoordinates(cell, out nextX, out nextY);

        CellData nextCell = GetCellData(gridData, nextX, nextY);
        if (nextCell == null) return;

        // Check if target cell has a recipe for this item
        RecipeDef recipe = GetRecipe(nextCell.machineDefId, item.itemType);
        
        if (recipe != null)
        {
            // Target has a recipe - item goes directly to waiting list (NEVER Moving state)
            item.state = ItemState.Waiting;
            item.waitingStartTime = Time.time;
            
            // Transfer to waiting list
            cell.items.Remove(item);
            nextCell.waitingItems.Add(item);
            
            Debug.Log($"Item {item.id} moved to waiting list for processor at ({nextX}, {nextY})");
        }
        else
        {
            // No recipe - standard movement (conveyor, seller, blank)
            item.state = ItemState.Moving;
            item.sourceX = cell.x;
            item.sourceY = cell.y;
            item.targetX = nextX;
            item.targetY = nextY;
            item.moveStartTime = Time.time;
            item.moveProgress = 0f;
            
            Debug.Log($"Item {item.id} starting movement to ({nextX}, {nextY})");
        }
    }

    /// <summary>
    /// Handles items in Moving state (only for non-recipe targets).
    /// </summary>
    private void ProcessMovingItem(ItemData item, CellData cell, GridData gridData, UIGridManager gridManager, ref int index)
    {
        float timeSinceStart = Time.time - item.moveStartTime;
        item.moveProgress = timeSinceStart * itemMoveSpeed;

        if (item.moveProgress >= 1.0f)
        {
            CompleteItemMovement(item, cell, gridData, gridManager);
            index--;
        }
        else
        {
            if (gridManager.HasVisualItem(item.id))
            {
                gridManager.UpdateItemVisualPosition(item.id, item.moveProgress, 
                    item.sourceX, item.sourceY, item.targetX, item.targetY, cell.direction);
            }
        }
    }

    /// <summary>
    /// Simplified CompleteItemMovement - delegates to machine objects instead of hardcoded logic.
    /// </summary>
    private void CompleteItemMovement(ItemData item, CellData sourceCell, GridData gridData, UIGridManager gridManager)
    {
        CellData targetCell = GetCellData(gridData, item.targetX, item.targetY);
        if (targetCell == null)
        {
            Debug.LogError($"Target cell not found at ({item.targetX}, {item.targetY})");
            return;
        }

        // Remove from source
        sourceCell.items.Remove(item);

        // Create machine object if it doesn't exist
        if (targetCell.machine == null && targetCell.cellType == CellType.Machine && !string.IsNullOrEmpty(targetCell.machineDefId))
        {
            targetCell.machine = MachineFactory.CreateMachine(targetCell);
        }

        // Handle destination - delegate to machine object or handle blank cells
        if (targetCell.cellType == CellType.Blank)
        {
            // Blank cell - destroy item
            Debug.Log($"Item {item.id} reached blank cell - destroying");
            gridManager.DestroyVisualItem(item.id);
            return;
        }
        else if (targetCell.machine != null)
        {
            // Let the machine handle the arriving item (replaces hardcoded seller logic)
            item.state = ItemState.Idle;
            item.x = targetCell.x;
            item.y = targetCell.y;
            item.moveProgress = 0f;
            targetCell.items.Add(item);
            
            // Notify machine that item has arrived
            targetCell.machine.OnItemArrived(item);
            
            gridManager.UpdateItemVisualPosition(item.id, 1f, item.sourceX, item.sourceY, item.targetX, item.targetY, sourceCell.direction);
        }
        else
        {
            // Regular destination (conveyor, etc.) - update position and set to idle
            item.state = ItemState.Idle;
            item.x = targetCell.x;
            item.y = targetCell.y;
            item.moveProgress = 0f;
            targetCell.items.Add(item);

            gridManager.UpdateItemVisualPosition(item.id, 1f, item.sourceX, item.sourceY, item.targetX, item.targetY, sourceCell.direction);
        }
    }

    /// <summary>
    /// Handles items in Waiting state with simplified visual positioning.
    /// </summary>
    private void ProcessWaitingItem(ItemData item, CellData cell, UIGridManager gridManager)
    {
        float waitingElapsed = Time.time - item.waitingStartTime;
        if (waitingElapsed >= 30f)
        {
            Debug.LogWarning($"Item {item.id} timeout after waiting {waitingElapsed:F1}s - destroying");
            
            cell.waitingItems.Remove(item);
            if (gridManager.HasVisualItem(item.id))
            {
                gridManager.DestroyVisualItem(item.id);
            }
        }
        else
        {
            UpdateWaitingItemVisualPosition(item, cell, gridManager);
        }
    }

    /// <summary>
    /// Direction-aware visual positioning for waiting items - creates a queue at machine entrance.
    /// </summary>
    private void UpdateWaitingItemVisualPosition(ItemData item, CellData machineCell, UIGridManager gridManager)
    {
        if (!gridManager.HasVisualItem(item.id)) return;

        int itemIndex = machineCell.waitingItems.FindIndex(i => i.id == item.id);
        if (itemIndex == -1) return;
        
        // Simple queue positioning - items line up at machine entrance based on direction
        Vector2 cellSize = gridManager.GetCellSize();
        Vector3 machinePos = gridManager.GetCellWorldPosition(machineCell.x, machineCell.y);
        
        // Calculate direction-aware queue offset
        float queueOffset = itemIndex * cellSize.y * 0.15f;
        Vector3 offsetVector;
        
        // Queue forms at the entrance of the machine based on its direction
        switch (machineCell.direction)
        {
            case Direction.Up:
                offsetVector = Vector3.down * queueOffset;
                break;
            case Direction.Right:
                offsetVector = Vector3.left * queueOffset;
                break;
            case Direction.Down:
                offsetVector = Vector3.up * queueOffset;
                break;
            case Direction.Left:
                offsetVector = Vector3.right * queueOffset;
                break;
            default:
                offsetVector = Vector3.down * queueOffset;
                break;
        }
        
        Vector3 queuePos = machinePos + offsetVector;
        
        GameObject visualItem = gridManager.GetVisualItem(item.id);
        if (visualItem != null)
        {
            visualItem.transform.position = queuePos;
        }
    }

    /// <summary>
    /// Handles items in Processing state.
    /// </summary>
    private void ProcessProcessingItem(ItemData item, CellData cell, GridData gridData, UIGridManager gridManager, ref int index)
    {
        float processingElapsed = Time.time - item.processingStartTime;
        if (processingElapsed >= item.processingDuration)
        {
            CompleteRecipeProcessing(item, cell, gridData, gridManager);
            index--;
        }
    }

    /// <summary>
    /// Gets next cell coordinates based on current cell direction.
    /// </summary>
    private void GetNextCellCoordinates(CellData cell, out int nextX, out int nextY)
    {
        nextX = cell.x;
        nextY = cell.y;

        // Spawner machines always move items "up" (toward the grid)
        if (cell.cellType == CellType.Machine && cell.machineDefId == "spawner")
        {
            nextY--;
            return;
        }

        // Other machines use their direction
        switch (cell.direction)
        {
            case Direction.Up: nextY--; break;
            case Direction.Right: nextX++; break;
            case Direction.Down: nextY++; break;
            case Direction.Left: nextX--; break;
        }
    }

    /// <summary>
    /// Updated CompleteRecipeProcessing method to work with new architecture.
    /// </summary>
    private void CompleteRecipeProcessing(ItemData item, CellData cell, GridData gridData, UIGridManager gridManager)
    {
        Debug.Log($"Completing processing for item {item.id} ({item.itemType}) after {item.processingDuration}s");
        
        // Look up the recipe to get output items
        RecipeDef recipe = GetRecipe(cell.machineDefId, item.itemType);
        
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