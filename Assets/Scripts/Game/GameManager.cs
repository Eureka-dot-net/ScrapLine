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

    // Add this new Update method to your GameManager class
    private void Update()
    {
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
    }

    // Add this new helper method to your GameManager class
    private void SpawnItem(CellData cellData)
    {
        Debug.Log($"Spawning new item at ({cellData.x}, {cellData.y})");

        // 1. Update the data model
        ItemData newItem = new ItemData { itemType = "Placeholder" }; // "Placeholder" for now
        cellData.items.Add(newItem);

        // 2. Tell the visual manager to create a visual representation
        UIGridManager gridManager = FindAnyObjectByType<UIGridManager>();
        if (gridManager != null)
        {
            gridManager.SpawnVisualItem(cellData);
        }
    }

    public void ClearGrid()
    {
        Debug.Log("Clearing grid...");
        GridData gridData = activeGrids[0];

        foreach (var cell in gridData.cells)
        {
            cell.cellType = CellType.Blank;
            cell.direction = Direction.Up;
            cell.machineType = MachineType.None;
            cell.items.Clear();
        }

        UIGridManager activeGridManager = FindAnyObjectByType<UIGridManager>();
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
        }
    }
}