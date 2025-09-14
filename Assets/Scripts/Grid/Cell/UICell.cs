using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UICell : MonoBehaviour
{
    // These are now references to the visual components
    public Image borderImage;
    public RawImage innerRawImage;
    public Button cellButton;

    public Sprite blankSprite;
    public Sprite conveyorBorderSprite;
    public Sprite machineBorderSprite;
    public Texture conveyorInnerTexture;
    private Material conveyorMaterial;

    // These are now purely visual properties, not the source of truth
    public CellType cellType = CellType.Blank;
    public CellRole cellRole = CellRole.Grid;
    public Direction conveyorDirection = Direction.Up;

    // These are references to the visual GameObjects  
    public RectTransform topSpawnPoint;  // For blank cells and fallback, items go on top

    // We no longer need these as the state is in our GridState model
    [HideInInspector]
    public int x, y;
    private UIGridManager gridManager;

    public enum CellType { Blank, Machine }
    public enum CellRole { Grid, Top, Bottom }
    public enum Direction { Up, Right, Down, Left }

    void Awake()
    {
        if (cellButton == null) cellButton = GetComponent<Button>();
        cellButton.onClick.AddListener(OnCellClicked);

        if (topSpawnPoint == null)
        {
            GameObject topPoint = new GameObject("TopSpawnPoint");
            topSpawnPoint = topPoint.AddComponent<RectTransform>();
            topSpawnPoint.SetParent(transform, false);
            topSpawnPoint.anchoredPosition = Vector2.zero;
            topSpawnPoint.sizeDelta = Vector2.zero;
        }
    }

    // This method is now used to initialize the cell from a CellState model
    public void Init(int x, int y, UIGridManager gridManager, Material conveyorMaterial)
    {
        this.x = x;
        this.y = y;
        this.gridManager = gridManager;
        this.conveyorMaterial = conveyorMaterial;
    }

    public void SetCellRole(CellRole role)
    {
        cellRole = role;
        borderImage.sprite = blankSprite;
        borderImage.enabled = true;

        switch (role)
        {
            case CellRole.Grid:
                borderImage.color = Color.gray;
                break;
            case CellRole.Top:
                borderImage.color = new Color(0.6f, 0.8f, 1f);
                break;
            case CellRole.Bottom:
                borderImage.color = new Color(1f, 0.7f, 0.7f);
                break;
        }
    }

    public void SetBorderSprite(Sprite sprite)
    {
        borderImage.sprite = sprite;
        borderImage.enabled = true;
    }

    // This method now receives all its state data from the GameManager
    public void SetCellType(CellType type, Direction direction, string machineDefId = null)
    {
        cellType = type;
        conveyorDirection = direction;

        switch (type)
        {
            case CellType.Blank:
                // For blank cells, hide both border and inner images as requested
                // This overrides any previous settings from SetCellRole
                borderImage.enabled = false;
                innerRawImage.enabled = false;
                
                // Also ensure any MachineRenderer is removed when switching to blank
                MachineRenderer renderer = GetComponentInChildren<MachineRenderer>();
                if (renderer != null)
                {
                    DestroyImmediate(renderer.gameObject);
                }
                break;
            case CellType.Machine:
                // All machines use the same rendering system - no special handling for conveyors
                if (!string.IsNullOrEmpty(machineDefId))
                {
                    var machineDef = FactoryRegistry.Instance.GetMachine(machineDefId);
                    if (machineDef != null)
                    {
                        // All machines now use the same border style
                        SetBorderSprite(machineBorderSprite);
                        innerRawImage.enabled = false; // MachineRenderer handles all visuals
                    }
                }
                else
                {
                    // Fallback for machines without definition
                    SetBorderSprite(machineBorderSprite);
                    innerRawImage.enabled = false;
                }
                break;
        }
    }

    void OnCellClicked()
    {
        // We now forward this event to the GameManager to handle the core logic
        GameManager.Instance.OnCellClicked(x, y);
    }

    public RectTransform GetItemSpawnPoint()
    {
        if (cellType == CellType.Machine && !string.IsNullOrEmpty(GetMachineDefId()))
        {
            // For all machines, try to find the spawn point created by MachineRenderer
            MachineRenderer machineRenderer = GetComponentInChildren<MachineRenderer>();
            if (machineRenderer != null)
            {
                Transform spawnPointTransform = machineRenderer.transform.Find("ItemSpawnPoint");
                if (spawnPointTransform != null)
                {
                    return spawnPointTransform.GetComponent<RectTransform>();
                }
            }
            // Fallback to topSpawnPoint if machine spawn point not found
            return topSpawnPoint;
        }
        else
        {
            // For blank cells, use topSpawnPoint
            return topSpawnPoint;
        }
    }

    private string GetMachineDefId()
    {
        // Get machine definition ID from the cell data
        var gridManager = FindFirstObjectByType<UIGridManager>();
        if (gridManager != null)
        {
            // This is a simplified approach - in a real implementation you'd want
            // a more direct way to get the machine def ID for this cell
            var cellData = gridManager.GetCellData(x, y);
            return cellData?.machineDefId;
        }
        return null;
    }
}