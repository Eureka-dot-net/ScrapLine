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
    public MachineType machineType = MachineType.None;
    public Direction conveyorDirection = Direction.Up;

    // These are references to the visual GameObjects
    public RectTransform itemSpawnPoint;
    public RectTransform topSpawnPoint;

    // We no longer need these as the state is in our GridState model
    [HideInInspector]
    public int x, y;
    private UIGridManager gridManager;

    public enum CellType { Blank, Conveyor, Machine }
    public enum CellRole { Grid, Top, Bottom }
    public enum MachineType { None, Generic, Input, Output }
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
    public void SetCellType(CellType type, Direction direction, MachineType mType = MachineType.None)
    {
        cellType = type;
        conveyorDirection = direction;
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
                SetConveyorRotation(conveyorDirection); // Keep the conveyor rotating

                Color machineColor = Color.gray; // Default
                if (mType == MachineType.Input)
                {
                    machineColor = Color.green;
                }
                else if (mType == MachineType.Output)
                {
                    machineColor = Color.red;
                }
                innerRawImage.color = machineColor; // Apply the tint

                break;
        }
    }

    void OnCellClicked()
    {
        // We now forward this event to the GameManager to handle the core logic
        GameManager.Instance.OnCellClicked(x, y);
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

    // Removed OnItemArrived and GetNextCell methods as they're no longer needed
    // GameManager now handles all movement logic

    // The Update method is now empty, as all logic is handled by the GameManager
    void Update()
    {
        // Empty - all logic now handled by GameManager
    }
}