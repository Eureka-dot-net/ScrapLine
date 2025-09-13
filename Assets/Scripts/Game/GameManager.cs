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

        UICell.CellType newType = cellData.cellType;
        UICell.Direction newDirection = cellData.direction;
        UICell.MachineType newMachineType = cellData.machineType;

        switch (cellData.cellRole)
        {
            case UICell.CellRole.Grid:
                if (cellData.cellType == UICell.CellType.Blank)
                {
                    newType = UICell.CellType.Conveyor;
                    newDirection = UICell.Direction.Up;
                }
                else if (cellData.cellType == UICell.CellType.Conveyor)
                {
                    newDirection = (UICell.Direction)(((int)cellData.direction + 1) % 4);
                    newType = UICell.CellType.Conveyor;
                }
                break;

            case UICell.CellRole.Top:
                Debug.Log("Clicked on Top role. Creating a placeholder Output machine.");
                newType = UICell.CellType.Machine;
                newMachineType = UICell.MachineType.Output;
                newDirection = UICell.Direction.Up;
                break;

            case UICell.CellRole.Bottom:
                Debug.Log("Clicked on Bottom role. Creating a placeholder Input machine.");
                newType = UICell.CellType.Machine;
                newMachineType = UICell.MachineType.Input;
                newDirection = UICell.Direction.Up;
                break;
        }

        cellData.cellType = newType;
        cellData.direction = newDirection;
        cellData.machineType = newMachineType;

        UIGridManager activeGridManager = FindAnyObjectByType<UIGridManager>();
        if (activeGridManager != null)
        {
            activeGridManager.UpdateCellVisuals(x, y, newType, newDirection, newMachineType);
        }
    }

    private void Update()
    {
        // Handle item spawning
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0)
        {
            spawnTimer = spawnInterval;

            // Find all input machines in our data model
            GridData gridData = activeGrids[0];
            foreach (var cell in gridData.cells)
            {
                if (cell.cellType == UICell.CellType.Machine && cell.machineType == UICell.MachineType.Input)
                {
                    // Check if the cell already has an item
                    if (cell.items.Count == 0)
                    {
                        // Spawn a new item if the cell is empty
                        SpawnItem(cell);
                    }
                }
            }
        }

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
                        gridManager.UpdateItemVisualPosition(item.id, item.moveProgress, cell.x, cell.y, item.targetX, item.targetY, cell.direction);
                    }
                }
                else
                {
                    if (cell.cellType != CellType.Blank)
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
            Debug.Log($"Item {item.id} moving to invalid cell - will stop at current position");
            item.shouldStopAtTarget = true;
            return;
        }

        // Check what the target cell contains and decide next action
        if (targetCell.cellType == CellType.Blank)
        {
            // Moving to empty cell - stop there
            Debug.Log($"Item {item.id} moving to empty cell - will stop");
            item.isMoving = false;
           // item.moveProgress = 0f;
          //  item.shouldStopAtTarget = true;
        }
        else if (targetCell.cellType == CellType.Machine)
        {
            // Moving to machine - will be processed
            Debug.Log($"Item {item.id} moving to machine - will be processed");
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
        else if (targetCell.cellType == CellType.Conveyor)
        {
            // Moving to conveyor - determine next movement based on conveyor direction
            int nextX, nextY;
            GetNextCellCoordinates(targetCell, out nextX, out nextY);
            CellData nextCell = GetCellData(gridData, nextX, nextY);

            if (nextCell != null)
            {
                // Queue up the next movement
                item.queuedTargetX = nextX;
                item.queuedTargetY = nextY;
                item.hasQueuedMovement = true;
                Debug.Log($"Item {item.id} will continue to ({nextX}, {nextY}) following conveyor");
            }
            else
            {
                Debug.Log($"Item {item.id} will stop at conveyor end");
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

        // Check if target is output machine
        if (targetCell.cellType == CellType.Machine && targetCell.machineType == MachineType.Output)
        {
            // TODO: Replace this with proper output handling (scoring, collection, etc.)
            Debug.Log($"Item {item.id} reached output machine - destroying");
            gridManager.DestroyVisualItem(item.id);
            return;
        }

        // Add to target cell and reset movement state
        item.isMoving = false;
        item.moveProgress = 0f;
        targetCell.items.Add(item);

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
        // Determine next cell based on current cell type and direction
        int nextX, nextY;
        GetNextCellCoordinates(cell, out nextX, out nextY);

        // Check if next cell exists
        CellData nextCell = GetCellData(gridData, nextX, nextY);
        if (nextCell == null)
        {
            return; // No movement possible
        }

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

        // For input machines, items move in the "up" direction (toward the grid)
        if (cell.cellType == CellType.Machine && cell.machineType == MachineType.Input)
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

        // Create new item with unique ID
        ItemData newItem = new ItemData
        {
            id = "item_" + nextItemId++,
            itemType = "Placeholder",
            isMoving = false,
            moveProgress = 0f
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
            cell.machineType = MachineType.None;
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
        GameData data = new GameData { grids = activeGrids };
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(Application.persistentDataPath + "/" + SAVE_FILE_NAME, json);
        Debug.Log("Game saved to location " + Application.persistentDataPath + "/" + SAVE_FILE_NAME + "!");
    }

    private void LoadGame()
    {
        string path = Application.persistentDataPath + "/" + SAVE_FILE_NAME;
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            GameData data = JsonUtility.FromJson<GameData>(json);
            activeGrids = data.grids;

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
    }
}