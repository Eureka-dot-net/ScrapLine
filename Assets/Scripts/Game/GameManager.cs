using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using static UICell;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public List<GridData> activeGrids = new List<GridData>();
    private const string SAVE_FILE_NAME = "game_data.json";

    public float spawnInterval = 5f;
    private float spawnTimer;

    // Movement settings
    public float itemMoveSpeed = 1f;
    private int nextItemId = 1;

    // Item management settings
    public float itemTimeoutOnBlankCells = 10f;
    public int maxItemsOnGrid = 100;
    public bool showItemLimitWarning = true;

    // Credits system
    [Header("Credits System")]
    [Tooltip("Starting credits amount for new games")]
    public int startingCredits = 2000;
    private int currentCredits = 0;
    private CreditsUI creditsUI;

    private Direction lastMachineDirection = Direction.Up;

    // Machine placement state
    private MachineDef selectedMachine;
    private MachineBarUIManager machineBarManager;

    public UIGridManager activeGridManager;

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

        machineBarManager = FindFirstObjectByType<MachineBarUIManager>();
        machineBarManager?.InitBar();

        creditsUI = FindFirstObjectByType<CreditsUI>();

        string path = Application.persistentDataPath + "/" + SAVE_FILE_NAME;
        if (File.Exists(path))
        {
            Debug.Log("Save file found. Loading saved grid.");
            LoadGame();
            StartCoroutine(InitializeMachinesFromSave());
        }
        else
        {
            Debug.Log("No save file found. Creating a new default grid.");

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

                    var cell = new CellData
                    {
                        x = x,
                        y = y,
                        cellType = CellType.Blank,
                        direction = Direction.Up,
                        cellRole = role,
                    };
                    defaultGrid.cells.Add(cell);
                    cell.machine = MachineFactory.CreateMachine(cell);
                }
            }
            activeGrids.Add(defaultGrid);
        }

        UIGridManager gridManager = FindAnyObjectByType<UIGridManager>();
        if (gridManager != null)
        {
            this.activeGridManager = gridManager;
            gridManager.InitGrid(activeGrids[0]);
        }

        UpdateCreditsDisplay();
    }

    private IEnumerator InitializeMachinesFromSave()
    {
        yield return new WaitUntil(() => FactoryRegistry.Instance.IsLoaded());

        foreach (var cell in activeGrids[0].cells)
        {
            if (cell.machine == null && !string.IsNullOrEmpty(cell.machineDefId))
            {
                cell.machine = MachineFactory.CreateMachine(cell);
            }
            else if (cell.machine == null)
            {
                cell.machine = MachineFactory.CreateMachine(cell);
            }
        }
    }

    // === CREDITS SYSTEM METHODS ===

    public int GetCredits()
    {
        return currentCredits;
    }

    private void UpdateCreditsDisplay()
    {
        if (creditsUI != null)
        {
            creditsUI.UpdateCredits(currentCredits);
        }
        machineBarManager?.UpdateAffordability();
    }

    public void AddCredits(int amount)
    {
        currentCredits += amount;
        Debug.Log($"Added {amount} credits. Total: {currentCredits}");
        UpdateCreditsDisplay();

        machineBarManager?.UpdateAffordability();
    }

    public string GenerateItemId()
    {
        return "item_" + nextItemId++;
    }

    public bool TrySpendCredits(int amount)
    {
        if (currentCredits >= amount)
        {
            currentCredits -= amount;
            Debug.Log($"Spent {amount} credits. Remaining: {currentCredits}");
            UpdateCreditsDisplay();

            machineBarManager?.UpdateAffordability();
            return true;
        }
        else
        {
            Debug.LogWarning($"Insufficient credits! Need {amount}, have {currentCredits}");
            return false;
        }
    }

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

        if (cellData.cellType == UICell.CellType.Machine && !string.IsNullOrEmpty(cellData.machineDefId))
        {
            RotateMachine(cellData);
            return;
        }

        if (selectedMachine != null)
        {
            if (IsValidMachinePlacement(cellData, selectedMachine))
            {
                if (CanAfford(selectedMachine.cost))
                {
                    PlaceMachine(cellData, selectedMachine);
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
        
        Debug.Log("No machine selected for placement");
    }

    private bool IsValidMachinePlacement(CellData cellData, MachineDef machineDef)
    {
        foreach (string placement in machineDef.gridPlacement)
        {
            switch (placement.ToLower())
            {
                case "any": return true;
                case "grid": return cellData.cellRole == UICell.CellRole.Grid;
                case "top": return cellData.cellRole == UICell.CellRole.Top;
                case "bottom": return cellData.cellRole == UICell.CellRole.Bottom;
            }
        }
        return false;
    }

    private void PlaceMachine(CellData cellData, MachineDef machineDef)
    {
        Debug.Log($"Placing machine {machineDef.id} at ({cellData.x}, {cellData.y})");

        if (!TrySpendCredits(machineDef.cost))
        {
            Debug.LogError($"Failed to place machine {machineDef.id} - insufficient credits!");
            return;
        }

        cellData.cellType = UICell.CellType.Machine;
        cellData.machineDefId = machineDef.id;

        if (machineDef.canRotate)
        {
            cellData.direction = lastMachineDirection;
        }
        else
        {
            cellData.direction = Direction.Up;
        }

        cellData.machine = MachineFactory.CreateMachine(cellData);
        if (cellData.machine == null)
        {
            Debug.LogError($"Failed to create machine object for {machineDef.id}");
        }

        if (activeGridManager != null)
        {
            activeGridManager.UpdateCellVisuals(cellData.x, cellData.y, cellData.cellType, cellData.direction, cellData.machineDefId);
        }
    }

    private void RotateMachine(CellData cellData)
    {
        MachineDef machineDef = FactoryRegistry.Instance.GetMachine(cellData.machineDefId);
        if (machineDef == null)
        {
            Debug.LogError($"Cannot find machine definition for {cellData.machineDefId}");
            return;
        }

        if (!machineDef.canRotate)
        {
            Debug.Log($"Machine {machineDef.id} cannot be rotated (canRotate = false)");
            return;
        }

        Debug.Log($"Rotating machine {machineDef.id} - current direction: {cellData.direction}");

        cellData.direction = (UICell.Direction)(((int)cellData.direction + 1) % 4);

        Debug.Log($"New direction after rotation: {cellData.direction}");

        lastMachineDirection = cellData.direction;

        Debug.Log($"Rotating machine at ({cellData.x}, {cellData.y}) to direction: {cellData.direction}");

        if (activeGridManager != null)
        {
            activeGridManager.UpdateCellVisuals(cellData.x, cellData.y, cellData.cellType, cellData.direction, cellData.machineDefId);
        }
    }


    private void Update()
    {
        GridData gridData = activeGrids[0];
        
        // !!! IMPORTANT: Do NOT remove this. This delegates behavior to the new machine objects.
        // This is the core of the hybrid OO/data-driven architecture.
        // All machine-specific logic has been moved out of the GameManager.
        foreach (var cell in gridData.cells)
        {
            if (cell.machine != null)
            {
                cell.machine.UpdateLogic();
            }
        }
        
        ProcessItemMovement();
    }

    private void ProcessItemMovement()
    {
        GridData gridData = activeGrids[0];
        if (activeGridManager == null) return;

        foreach (var cell in gridData.cells)
        {
            for (int i = cell.items.Count - 1; i >= 0; i--)
            {
                ItemData item = cell.items[i];
                
                if (item.state == ItemState.Moving)
                {
                    ProcessMovingItem(item, cell);
                }
                else if (item.state == ItemState.Idle)
                {
                    if (cell.machine != null)
                    {
                        cell.machine.TryStartMove(item);
                    }
                }
            }
        }
    }

    private void ProcessMovingItem(ItemData item, CellData sourceCell)
    {
        float timeSinceStart = Time.time - item.moveStartTime;
        item.moveProgress = timeSinceStart * itemMoveSpeed;

        if (item.moveProgress >= 1.0f)
        {
            CompleteItemMovement(item, sourceCell);
        }
        else
        {
            if (activeGridManager.HasVisualItem(item.id))
            {
                activeGridManager.UpdateItemVisualPosition(item.id, item.moveProgress,
                    item.sourceX, item.sourceY, item.targetX, item.targetY, sourceCell.direction);
            }
        }
    }

    private void CompleteItemMovement(ItemData item, CellData sourceCell)
    {
        CellData targetCell = GetCellData(activeGrids[0], item.targetX, item.targetY);
        if (targetCell == null)
        {
            Debug.LogError($"Target cell not found at ({item.targetX}, {item.targetY})");
            sourceCell.items.Remove(item);
            if (activeGridManager.HasVisualItem(item.id))
            {
                activeGridManager.DestroyVisualItem(item.id);
            }
            return;
        }

        sourceCell.items.Remove(item);
        targetCell.items.Add(item);

        item.state = ItemState.Idle;
        item.x = targetCell.x;
        item.y = targetCell.y;
        item.moveProgress = 0f;

        if (targetCell.machine != null)
        {
            targetCell.machine.OnItemArrived(item);
        }

        if (activeGridManager.HasVisualItem(item.id))
        {
            activeGridManager.UpdateItemVisualPosition(item.id, 1f, item.sourceX, item.sourceY, item.targetX, item.targetY, sourceCell.direction);
        }
    }

    public void ClearGrid()
    {
        Debug.Log("Clearing grid...");
        GridData gridData = activeGrids[0];

        if (activeGridManager == null) return;

        foreach (var cell in gridData.cells)
        {
            if (activeGridManager != null)
            {
                foreach (var item in cell.items)
                {
                    activeGridManager.DestroyVisualItem(item.id);
                }
            }

            cell.cellType = CellType.Blank;
            cell.direction = Direction.Up;
            cell.machineDefId = null;
            cell.items.Clear();
            cell.waitingItems.Clear();
        }

        activeGridManager.UpdateAllVisuals();
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

            currentCredits = data.credits;
            Debug.Log($"Loaded {currentCredits} credits from save file");

            FactoryRegistry.Instance.LoadFromGameData(data);
        }
    }
}