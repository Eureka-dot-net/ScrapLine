using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UICell;

public class UIGridManager : MonoBehaviour
{
    public RectTransform gridPanel;
    public GameObject cellPrefab;
    public Material conveyorSharedMaterial;
    public RectTransform movingItemsContainer;
    public GameObject itemPrefab;

    private GridData gridData;
    private UICell[,] cellScripts;
    
    // Track visual items by ID
    private Dictionary<string, GameObject> visualItems = new Dictionary<string, GameObject>();

    public UICell GetCell(int x, int y)
    {
        if (x >= 0 && x < gridData.width && y >= 0 && y < gridData.height)
        {
            return cellScripts[x, y];
        }
        return null;
    }

    public void InitGrid(GridData data)
    {
        Debug.Log("UIGridManager InitGrid() called.");
        this.gridData = data;

        GridLayoutGroup layout = gridPanel.GetComponent<GridLayoutGroup>();
        if (layout != null)
        {
            float cellWidth = gridPanel.rect.width / gridData.width;
            float cellHeight = gridPanel.rect.height / gridData.height;
            layout.cellSize = new Vector2(cellWidth, cellHeight);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = gridData.width;
        }

        // Clean up old grid before creating a new one
        if (cellScripts != null)
        {
            foreach (var cell in cellScripts)
            {
                if (cell != null)
                    Destroy(cell.gameObject);
            }
        }

        // Clear visual items
        foreach (var item in visualItems.Values)
        {
            if (item != null)
                Destroy(item);
        }
        visualItems.Clear();

        cellScripts = new UICell[gridData.width, gridData.height];

        for (int y = 0; y < gridData.height; ++y)
        {
            for (int x = 0; x < gridData.width; ++x)
            {
                GameObject cellObj = Instantiate(cellPrefab, gridPanel);
                UICell cellScript = cellObj.GetComponent<UICell>();
                cellScripts[x, y] = cellScript;

                cellScript.Init(x, y, this, conveyorSharedMaterial);

                CellData cellData = GetCellData(x, y);
                if (cellData != null)
                {
                    cellScript.SetCellRole(cellData.cellRole);
                    cellScript.SetCellType(cellData.cellType, cellData.direction, cellData.machineType);
                }
            }
        }

        if (movingItemsContainer != null)
        {
            movingItemsContainer.SetAsLastSibling();
        }
    }

    private CellData GetCellData(int x, int y)
    {
        foreach (var cell in gridData.cells)
        {
            if (cell.x == x && cell.y == y)
            {
                return cell;
            }
        }
        return null;
    }

    public void UpdateCellVisuals(int x, int y, CellType newType, Direction newDirection, MachineType machineType = MachineType.None)
    {
        UICell cell = GetCell(x, y);
        if (cell != null)
        {
            cell.SetCellType(newType, newDirection, machineType);
        }
    }

    public void UpdateAllVisuals()
    {
        if (cellScripts == null || gridData == null)
        {
            Debug.LogError("UIGridManager is not initialized. Cannot update visuals.");
            return;
        }

        foreach (var cell in gridData.cells)
        {
            UpdateCellVisuals(cell.x, cell.y, cell.cellType, cell.direction, cell.machineType);
        }
    }

    // New methods for GameManager to control visual items

    public void CreateVisualItem(string itemId, int x, int y)
    {
        UICell cell = GetCell(x, y);
        if (cell == null)
        {
            Debug.LogError($"Cannot create visual item - cell at ({x}, {y}) not found");
            return;
        }

        // Check if item already exists
        if (visualItems.ContainsKey(itemId))
        {
            Debug.LogWarning($"Visual item {itemId} already exists!");
            return;
        }

        // Find the spawn point on the cell
        RectTransform spawnPoint = cell.GetItemSpawnPoint();

        // Create the new item instance
        GameObject newItem = Instantiate(itemPrefab, spawnPoint);
        newItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        visualItems[itemId] = newItem;
        Debug.Log($"Created visual item {itemId} at ({x}, {y})");
    }

    public void DestroyVisualItem(string itemId)
    {
        if (visualItems.TryGetValue(itemId, out GameObject item))
        {
            if (item != null)
                Destroy(item);
            visualItems.Remove(itemId);
            Debug.Log($"Destroyed visual item {itemId}");
        }
        else
        {
            Debug.LogWarning($"Cannot destroy visual item {itemId} - not found");
        }
    }

    public void UpdateItemVisualPosition(string itemId, float progress, int startX, int startY, int endX, int endY, UICell.Direction movementDirection)
    {
        if (!visualItems.TryGetValue(itemId, out GameObject item) || item == null)
        {
            Debug.LogWarning($"Cannot update position for visual item {itemId} - not found");
            return;
        }

        UICell startCell = GetCell(startX, startY);
        UICell endCell = GetCell(endX, endY);

        if (startCell == null || endCell == null)
        {
            Debug.LogError($"Cannot update item position - invalid cell coordinates");
            return;
        }

        // Calculate positions based on spawn point type
        RectTransform startSpawnPoint = startCell.GetItemSpawnPoint();
        RectTransform endSpawnPoint = endCell.GetItemSpawnPoint();

        Vector3 startPos = startSpawnPoint.position;
        Vector3 endPos = endSpawnPoint.position;

        // Interpolate position
        Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);
        item.transform.position = currentPos;

        // Handle parent changes for proper rendering order based on movement direction
        bool shouldChangeParent = ShouldChangeParent(progress, movementDirection, endCell.machineType);
        RectTransform currentParent = item.transform.parent as RectTransform;
        RectTransform targetParent = shouldChangeParent ? endSpawnPoint : startSpawnPoint;
        Debug.Log($"Item {itemId} progress: {progress}, shouldChangeParent: {shouldChangeParent}, currentParent: {currentParent?.name}, targetParent: {targetParent?.name}");
        if (currentParent != targetParent)
        {
            item.transform.SetParent(targetParent, true);
        }
    }

    private bool ShouldChangeParent(float progress, UICell.Direction movementDirection,  UICell.MachineType machineType)
    {
        // For Down and Right movement: change parent before hitting boundary (30%)
        // Also for machines to ensure items appear below roof
        if (movementDirection == UICell.Direction.Down || movementDirection == UICell.Direction.Right || machineType == UICell.MachineType.ThreeeInputsOneOutput)
        {
            return progress >= 0.3f;
        }
        // For Up and Left movement: change parent after crossing boundary (99%)
        else if (movementDirection == UICell.Direction.Up || movementDirection == UICell.Direction.Left)
        {
            return progress >= 0.7f;
        }
        return progress >= 0.5f; // Default fallback
    }

}