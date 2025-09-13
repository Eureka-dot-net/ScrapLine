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

    private class MovingItemData
    {
        public GameObject item;
        public Vector3 startPos;
        public Vector3 endPos;
        public float startTime;
        public float journeyLength;
        public UICell nextCell;
        public bool isStuck;
        public bool parentChanged;
        public Direction currentDirection;
    }

    private List<MovingItemData> movingItems = new List<MovingItemData>();

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

    public void StartItemMovement(GameObject item, UICell currentCell, UICell nextCell)
    {
        if (nextCell == null)
        {
            Debug.Log("Item reached end of conveyor path. Stopping.");
            MovingItemData stuckItem = new MovingItemData
            {
                item = item,
                isStuck = true
            };
            movingItems.Add(stuckItem);
            return;
        }

        Vector3 startPos = item.transform.position;
        Vector3 endPos = nextCell.GetComponent<RectTransform>().position;

        MovingItemData data = new MovingItemData
        {
            item = item,
            startPos = startPos,
            endPos = endPos,
            startTime = Time.time,
            journeyLength = Vector3.Distance(startPos, endPos),
            nextCell = nextCell,
            isStuck = false,
            parentChanged = false,
            currentDirection = currentCell.conveyorDirection,
        };

        movingItems.Add(data);
    }

    void Update()
    {
        for (int i = movingItems.Count - 1; i >= 0; i--)
        {
            MovingItemData data = movingItems[i];

            if (data.isStuck)
            {
                continue;
            }

            float distCovered = (Time.time - data.startTime) * 100f;
            float fractionOfJourney = distCovered / data.journeyLength;

            bool shouldChangeParent = false;

            if (data.currentDirection == Direction.Up || data.currentDirection == Direction.Left)
            {
                if (fractionOfJourney >= 0.99f) shouldChangeParent = true;
            }
            else if (data.currentDirection == Direction.Down || data.currentDirection == Direction.Right)
            {
                if (fractionOfJourney >= 0.3f) shouldChangeParent = true;
            }

            if (shouldChangeParent && !data.parentChanged)
            {
                data.item.transform.SetParent(data.nextCell.GetItemSpawnPoint(), false);
                data.parentChanged = true;
            }

            data.item.transform.position = Vector3.Lerp(data.startPos, data.endPos, fractionOfJourney);

            if (fractionOfJourney >= 1f)
            {
                data.item.transform.position = data.endPos;

                if (data.nextCell != null)
                {
                    data.nextCell.OnItemArrived(data.item);
                }

                movingItems.RemoveAt(i);
            }
        }
    }
}