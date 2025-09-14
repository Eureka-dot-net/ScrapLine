using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UICell : MonoBehaviour
{
    // These are now references to the visual components
    public Image borderImage;
    public Button cellButton;

    public Sprite blankSprite;

    // These are now purely visual properties, not the source of truth
    public CellType cellType = CellType.Blank;
    public CellRole cellRole = CellRole.Grid;

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
        
        // Initialize as blank by default - start with proper blank cell appearance
        InitializeAsBlankCell();
    }

    private void InitializeAsBlankCell()
    {
        // Blank cells should be completely invisible - no borders or sprites
        if (borderImage != null)
        {
            borderImage.enabled = false;
            borderImage.gameObject.SetActive(false);
        }
    }

    // This method is now used to initialize the cell from a CellState model
    public void Init(int x, int y, UIGridManager gridManager)
    {
        this.x = x;
        this.y = y;
        this.gridManager = gridManager;
    }

    public void SetCellRole(CellRole role)
    {
        cellRole = role;
        
        // Only show borders for cells that have an actual role (Top/Bottom spawners/sellers)
        // Regular grid cells should remain hidden unless they become machines
        if (role == CellRole.Top || role == CellRole.Bottom)
        {
            borderImage.sprite = blankSprite;
            borderImage.gameObject.SetActive(true);
            borderImage.enabled = true;

            switch (role)
            {
                case CellRole.Top:
                    borderImage.color = new Color(0.6f, 0.8f, 1f);
                    break;
                case CellRole.Bottom:
                    borderImage.color = new Color(1f, 0.7f, 0.7f);
                    break;
            }
        }
        else
        {
            // For Grid role cells, ensure they remain completely hidden (blank cells)
            if (borderImage != null)
            {
                borderImage.enabled = false;
                borderImage.gameObject.SetActive(false);
            }
        }
    }


    // This method now receives all its state data from the GameManager
    public void SetCellType(CellType type, Direction direction, string machineDefId = null)
    {
        cellType = type;

        switch (type)
        {
            case CellType.Blank:
                // For blank cells, reset to proper blank cell appearance
                InitializeAsBlankCell();
                
                // Also ensure any MachineRenderer is removed when switching to blank
                MachineRenderer renderer = GetComponentInChildren<MachineRenderer>();
                if (renderer != null)
                {
                    DestroyImmediate(renderer.gameObject);
                }
                break;
            case CellType.Machine:
                // All machines use the same rendering system
                // Just show the border for machines - MachineRenderer handles all visuals
                borderImage.gameObject.SetActive(true);
                borderImage.sprite = blankSprite; // Use blank sprite as base for machines too
                borderImage.color = Color.white; // Default color for machines
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