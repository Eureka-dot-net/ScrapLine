using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UICell : MonoBehaviour
{
    // Essential UI components only
    public Button cellButton;

    // Data/state properties
    public CellType cellType = CellType.Blank;
    public CellRole cellRole = CellRole.Grid;

    // Position tracking
    [HideInInspector]
    public int x, y;
    private UIGridManager gridManager;

    // MachineRenderer handles ALL visuals now
    private MachineRenderer machineRenderer;

    public enum CellType { Blank, Machine }
    public enum CellRole { Grid, Top, Bottom }
    public enum Direction { Up, Right, Down, Left }

    void Awake()
    {
        if (cellButton == null) cellButton = GetComponent<Button>();
        cellButton.onClick.AddListener(OnCellClicked);
        
        Debug.Log($"UICell Awake() - cell will be initialized with MachineRenderer for ALL visuals");
    }

    // This method is now used to initialize the cell from a CellState model
    public void Init(int x, int y, UIGridManager gridManager)
    {
        this.x = x;
        this.y = y;
        this.gridManager = gridManager;
        
        Debug.Log($"UICell.Init({x}, {y}) - cell ready for machine setup");
    }

    public void SetCellRole(CellRole role)
    {
        cellRole = role;
        Debug.Log($"Cell ({x}, {y}) role set to: {role}");
    }

    // This method now receives all its state data and creates MachineRenderer for ALL visuals
    public void SetCellType(CellType type, Direction direction, string machineDefId = null)
    {
        cellType = type;
        Debug.Log($"Setting cell ({x}, {y}) to type: {type}, direction: {direction}, machineDefId: {machineDefId}");

        // Clean up any existing renderer
        if (machineRenderer != null)
        {
            Debug.Log($"Removing existing MachineRenderer from cell ({x}, {y})");
            DestroyImmediate(machineRenderer.gameObject);
            machineRenderer = null;
        }

        // Determine which machine definition to use
        string defIdToUse = machineDefId;
        if (type == CellType.Blank)
        {
            // Use different blank machine definitions based on cell role
            switch (cellRole)
            {
                case CellRole.Top:
                    defIdToUse = "blank_top";
                    Debug.Log($"Cell ({x}, {y}) is blank type with Top role - using 'blank_top' machine definition");
                    break;
                case CellRole.Bottom:
                    defIdToUse = "blank_bottom";
                    Debug.Log($"Cell ({x}, {y}) is blank type with Bottom role - using 'blank_bottom' machine definition");
                    break;
                default:
                    defIdToUse = "blank";
                    Debug.Log($"Cell ({x}, {y}) is blank type with Grid role - using 'blank' machine definition");
                    break;
            }
        }

        // Create MachineRenderer for ALL cell types (including blanks)
        if (!string.IsNullOrEmpty(defIdToUse))
        {
            var machineDef = FactoryRegistry.Instance.GetMachine(defIdToUse);
            if (machineDef != null)
            {
                Debug.Log($"Creating MachineRenderer for cell ({x}, {y}) with definition: {defIdToUse}");
                SetupMachineRenderer(machineDef, direction);
            }
            else
            {
                Debug.LogError($"Could not find machine definition for: {defIdToUse}");
            }
        }
    }

    private void SetupMachineRenderer(MachineDef def, Direction direction)
    {
        // Create MachineRenderer GameObject as child
        GameObject rendererObj = new GameObject("MachineRenderer");
        rendererObj.transform.SetParent(this.transform, false);
        
        RectTransform rendererRT = rendererObj.AddComponent<RectTransform>();
        rendererRT.anchorMin = Vector2.zero;
        rendererRT.anchorMax = Vector2.one;
        rendererRT.offsetMin = Vector2.zero;
        rendererRT.offsetMax = Vector2.zero;
        
        // Add ConveyorBelt component if this machine has moving parts that need animation
        if (!string.IsNullOrEmpty(def.movingPartMaterial) && !string.IsNullOrEmpty(def.movingPartSprite))
        {
            ConveyorBelt conveyorBelt = rendererObj.AddComponent<ConveyorBelt>();
            
            // Set the conveyor direction based on UICell direction
            float cellRotation = GetCellDirectionRotation(direction);
            rendererObj.transform.rotation = Quaternion.Euler(0, 0, cellRotation);
            
            Debug.Log($"Added ConveyorBelt component to machine '{def.id}' at cell ({x}, {y}) with direction {direction} and rotation {cellRotation}");
            Debug.Log($"Actual transform rotation after setting: {rendererObj.transform.eulerAngles}");
        }
        
        machineRenderer = rendererObj.AddComponent<MachineRenderer>();
        machineRenderer.Setup(def, direction, gridManager, x, y);
        
        Debug.Log($"MachineRenderer setup complete for cell ({x}, {y}) with definition: {def.id}");
    }
    
    private float GetCellDirectionRotation(Direction direction)
    {
        switch (direction)
        {
            case Direction.Up: return 0f;
            case Direction.Right: return -90f;
            case Direction.Down: return -180f;
            case Direction.Left: return -270f;
            default: return 0f;
        }
    }

    void OnCellClicked()
    {
        // We now forward this event to the GameManager to handle the core logic
        GameManager.Instance.OnCellClicked(x, y);
    }

    public RectTransform GetItemSpawnPoint()
    {
        // ALL cells use MachineRenderer now, including blank cells
        if (machineRenderer != null)
        {
            // Try to find the spawn point created by MachineRenderer
            Transform spawnPointTransform = machineRenderer.transform.Find("ItemSpawnPoint");
            if (spawnPointTransform != null)
            {
                return spawnPointTransform.GetComponent<RectTransform>();
            }
        }
        
        // Fallback: create a default spawn point if none found
        Transform fallbackSpawn = transform.Find("DefaultSpawnPoint");
        if (fallbackSpawn == null)
        {
            GameObject spawnObj = new GameObject("DefaultSpawnPoint");
            spawnObj.transform.SetParent(this.transform, false);
            RectTransform spawnRT = spawnObj.AddComponent<RectTransform>();
            spawnRT.anchorMin = new Vector2(0.5f, 0.5f);
            spawnRT.anchorMax = new Vector2(0.5f, 0.5f);
            spawnRT.anchoredPosition = Vector2.zero;
            spawnRT.sizeDelta = Vector2.zero;
            return spawnRT;
        }
        
        return fallbackSpawn.GetComponent<RectTransform>();
    }

    private string GetMachineDefId()
    {
        // Get machine definition ID from the cell data
        var gridManager = FindFirstObjectByType<UIGridManager>();
        if (gridManager != null)
        {
            var cellData = gridManager.GetCellData(x, y);
            return cellData?.machineDefId;
        }
        return null;
    }
}