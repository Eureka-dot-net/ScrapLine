using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UICell;

public class UIGridManager : MonoBehaviour
{
    public RectTransform gridPanel;
    public GameObject cellPrefab;
    public Material conveyorSharedMaterial;

    public int width = 5, height = 5;

    public UICell.Direction lastConveyorDirection = UICell.Direction.Up;

    private UICell[,] cellScripts;
    private UICell.MachineType[,] machineTypes;

    public RectTransform movingItemsContainer;

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
        if (x >= 0 && x < width && y >= 0 && y < height + 2)
        {
            return cellScripts[x, y];
        }
        return null;
    }

    void Start()
    {
        GridLayoutGroup layout = gridPanel.GetComponent<GridLayoutGroup>();
        if (layout != null)
        {
            float cellWidth = gridPanel.rect.width / width;
            float cellHeight = gridPanel.rect.height / (height + 2);
            layout.cellSize = new Vector2(cellWidth, cellHeight);

            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = width;
        }

        cellScripts = new UICell[width, height + 2];
        machineTypes = new UICell.MachineType[width, height + 2];

        for (int y = 0; y < height + 2; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                GameObject cellObj = Instantiate(cellPrefab, gridPanel);
                UICell cellScript = cellObj.GetComponent<UICell>();
                cellScripts[x, y] = cellScript;
                cellScript.Init(x, y, this, conveyorSharedMaterial);

                cellScript.SetCellType(UICell.CellType.Blank);

                if (y == 0)
                    cellScript.SetCellRole(UICell.CellRole.Top);
                else if (y == height + 1)
                    cellScript.SetCellRole(UICell.CellRole.Bottom);
                else
                    cellScript.SetCellRole(UICell.CellRole.Grid);

                machineTypes[x, y] = UICell.MachineType.None;
            }
        }
        
        if (movingItemsContainer != null)
        {
            movingItemsContainer.SetAsLastSibling();
        }
    }
    
    public void OnCellTypeChanged(int x, int y, UICell.CellType newType, UICell.MachineType machineType = UICell.MachineType.None)
    {
        UICell cell = GetCell(x, y);
        if (cell != null)
        {
            cell.SetCellType(newType, machineType);
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
                // Rule: Up/Left movement, change parent right before hitting the center
                if (fractionOfJourney >= 0.99f) shouldChangeParent = true;
            }
            else if (data.currentDirection == Direction.Down || data.currentDirection == Direction.Right)
            {
                // Rule: Down/Right movement, change parent when it HITS the boundary
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