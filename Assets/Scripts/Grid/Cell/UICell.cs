using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UICell : MonoBehaviour
{
    [Tooltip("Time in seconds before a parked item is destroyed.")]
    public float itemTimeout = 10f;

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

   private Dictionary<GameObject, float> parkedItems = new Dictionary<GameObject, float>();

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
        // New Logic: If the cell type has changed and items are parked here, restart their movement
        if (parkedItems.Count > 0)
        {
            if (cellType == CellType.Conveyor || cellType == CellType.Machine)
            {
                // This cell is no longer blank, so we can trigger the movement of all parked items
                foreach (var item in parkedItems.Keys)
                {
                    OnItemArrived(item);
                }
                parkedItems.Clear(); // Clear the dictionary after starting the movement
            }
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
        // Apply rotation to the inner and border children instead of the parent
        if (innerRawImage != null)
        {
            innerRawImage.rectTransform.localEulerAngles = new Vector3(0, 0, zRot);
        }
        if (borderImage != null)
        {
            borderImage.rectTransform.localEulerAngles = new Vector3(0, 0, zRot);
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
        if (cellType == CellType.Conveyor || (cellType == CellType.Machine && machineType == MachineType.Input))
        {
            UICell nextCell = GetNextCell();
            if (nextCell != null)
            {
                gridManager.StartItemMovement(item, this, nextCell);
            }
            else
            {
                Destroy(item);
            }
        }
        else // Blank cell
        {
            parkedItems.Add(item, Time.time);
        }
    }

    void Update()
    {
        if (parkedItems.Count > 0)
        {
            // Use a temporary list to hold items that should be destroyed
            List<GameObject> itemsToDestroy = new List<GameObject>();

            foreach (var kvp in parkedItems)
            {
                if (Time.time - kvp.Value > itemTimeout)
                {
                    itemsToDestroy.Add(kvp.Key);
                }
            }
            
            if (itemsToDestroy.Count > 0)
            {
                Debug.Log($"Items timed out on cell ({x},{y}), destroying {itemsToDestroy.Count} items.");
                foreach (var item in itemsToDestroy)
                {
                    Destroy(item);
                    parkedItems.Remove(item);
                }
            }
        }
        if (cellType == CellType.Machine && machineType == MachineType.Input)
        {
            if (spawnPrefabs == null || spawnPrefabs.Length == 0) return;

            inputTimer += Time.deltaTime;

            if (inputTimer >= spawnInterval)
            {
                inputTimer = 0f;

                RectTransform prefabToSpawn = spawnPrefabs[spawnIndex];

                Vector3 spawnPos = GetItemSpawnPoint().position;

                RectTransform spawnedObj = Instantiate(prefabToSpawn, gridManager.movingItemsContainer);

                spawnedObj.position = spawnPos;

                UICell nextCell = GetNextCell();
                if (nextCell != null)
                {
                    gridManager.StartItemMovement(spawnedObj.gameObject, this, nextCell);
                }
                else
                {
                    Destroy(spawnedObj.gameObject);
                }

                spawnIndex = (spawnIndex + 1) % spawnPrefabs.Length;
            }
        }
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