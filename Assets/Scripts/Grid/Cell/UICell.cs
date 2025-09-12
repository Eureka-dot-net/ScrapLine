// UICell.cs
using System;
using UnityEngine;
using UnityEngine.UI;

public class UICell : MonoBehaviour
{
    [HideInInspector]
    public int x, y;

    private UIGridManager gridManager;

    public Image borderImage;
    public RawImage innerRawImage;
    public Button cellButton;

    public Sprite blankSprite;
    public Sprite conveyorBorderSprite;
    public Sprite machineBorderSprite;
    public Texture conveyorInnerTexture;
    private Material conveyorMaterial;

    public enum CellType { Blank, Conveyor, Machine }
    public enum CellRole { Grid, Top, Bottom }
    public enum MachineType { None, Generic, Input }

    [HideInInspector]
    public CellType cellType = CellType.Blank;
    [HideInInspector]
    public CellRole cellRole = CellRole.Grid;
    [HideInInspector]
    public MachineType machineType = MachineType.None;

    public enum Direction { Up, Right, Down, Left }
    [HideInInspector]
    public Direction conveyorDirection = Direction.Up;

    public RectTransform[] spawnPrefabs;
    public float spawnInterval = 2f;
    public float cellSize = 1f;

    private float inputTimer = 0f;
    private int spawnIndex = 0;

    public RectTransform itemSpawnPoint;
    public RectTransform topSpawnPoint;

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

    public void SetCellType(CellType type, MachineType mType = MachineType.None)
    {
        cellType = type;
        machineType = mType;

        switch (type)
        {
            case CellType.Blank:
                SetBorderSprite(blankSprite);
                innerRawImage.enabled = false;
                break;
            case CellType.Conveyor:
                SetBorderSprite(conveyorBorderSprite);
                innerRawImage.enabled = true;
                innerRawImage.texture = conveyorInnerTexture;
                innerRawImage.material = conveyorMaterial;
                SetConveyorRotation(conveyorDirection);
                break;
            case CellType.Machine:
                SetBorderSprite(machineBorderSprite);
                innerRawImage.enabled = true;
                innerRawImage.texture = conveyorInnerTexture;
                innerRawImage.material = conveyorMaterial;
                break;
        }
    }

    void HandleGridCellClick()
    {
        if (cellType == CellType.Blank)
        {
            conveyorDirection = gridManager.lastConveyorDirection;
            gridManager.OnCellTypeChanged(x, y, CellType.Conveyor, MachineType.None);
        }
        else if (cellType == CellType.Conveyor)
        {
            RotateConveyorDirection();
            SetConveyorRotation(conveyorDirection);

            gridManager.lastConveyorDirection = conveyorDirection;
            gridManager.OnCellTypeChanged(x, y, CellType.Conveyor, MachineType.None);
        }
    }

    void OnCellClicked()
    {
        switch (cellRole)
        {
            case CellRole.Grid:
                conveyorDirection = gridManager.lastConveyorDirection;
                HandleGridCellClick();
                break;
            case CellRole.Top:
                break;
            case CellRole.Bottom:
                gridManager.OnCellTypeChanged(x, y, CellType.Machine, MachineType.Input);
                break;
        }
    }

    void RotateConveyorDirection()
    {
        conveyorDirection = (Direction)(((int)conveyorDirection + 1) % 4);
    }

    void SetConveyorRotation(Direction dir)
    {
        float zRot = 0f;
        switch (dir)
        {
            case Direction.Up: zRot = 0f; break;
            case Direction.Right: zRot = -90f; break;
            case Direction.Down: zRot = -180f; break;
            case Direction.Left: zRot = -270f; break;
        }
        transform.localEulerAngles = new Vector3(0, 0, zRot);
    }

    public Vector2 GetDirectionVector()
    {
        switch (conveyorDirection)
        {
            case Direction.Up: return Vector2.up;
            case Direction.Right: return Vector2.right;
            case Direction.Down: return Vector2.down;
            case Direction.Left: return Vector2.left;
            default: return Vector2.up;
        }
    }

    public RectTransform GetItemSpawnPoint()
    {
        if (cellType == CellType.Machine)
        {
            return itemSpawnPoint;
        }
        else
        {
            return topSpawnPoint;
        }
    }

    public void OnItemArrived(GameObject item)
{
    // Get the item's current world position before changing its parent
    Vector3 startPos = item.transform.position;
    
    // Reparent the item to the appropriate spawn point
    RectTransform targetSpawnPoint = GetItemSpawnPoint();
    item.transform.SetParent(targetSpawnPoint, false);
    
    // Explicitly set the local position to zero to place it in the center
    item.transform.localPosition = Vector3.zero;

    if (cellType == CellType.Conveyor)
    {
        ItemMover mover = item.GetComponent<ItemMover>();
        if (mover != null)
        {
            // Now we pass the correct parameters to the helper method
            StartItemMovement(mover, item.GetComponent<RectTransform>(), startPos);
        }
    }
}

    void Update()
    {
        if (cellType == CellType.Machine && machineType == MachineType.Input)
        {
            if (spawnPrefabs == null || spawnPrefabs.Length == 0) return;

            inputTimer += Time.deltaTime;

            if (inputTimer >= spawnInterval)
            {
                inputTimer = 0f;

                RectTransform prefabToSpawn = spawnPrefabs[spawnIndex];
                
                // Spawn the item into the central moving items container
                RectTransform spawnedObj = Instantiate(prefabToSpawn, gridManager.movingItemsContainer);
                
                ItemMover mover = spawnedObj.gameObject.AddComponent<ItemMover>();
                
                Vector3 startPos = GetItemSpawnPoint().position;
                
                StartItemMovement(mover, spawnedObj, startPos);

                spawnIndex = (spawnIndex + 1) % spawnPrefabs.Length;
            }
        }
    }

    private void StartItemMovement(ItemMover mover, RectTransform itemRect, Vector3 startPos)
    {
        UICell nextCell = GetNextCell();
        if (nextCell == null)
        {
            Debug.LogWarning("No next cell found, destroying item.");
            Destroy(mover.gameObject);
            return;
        }

        Vector3 nextCellPos = nextCell.GetComponent<RectTransform>().position;
        mover.StartMovement(nextCell, startPos, nextCellPos);
    }

    private UICell GetNextCell()
    {
        int nextX = x, nextY = y;
        switch (conveyorDirection)
        {
            case Direction.Up: nextY--; break;
            case Direction.Right: nextX++; break;
            case Direction.Down: nextY++; break;
            case Direction.Left: nextX--; break;
        }
        return gridManager.GetCell(nextX, nextY);
    }
}