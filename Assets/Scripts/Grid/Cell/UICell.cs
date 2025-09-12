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

    // Input machine settings
    public GameObject[] spawnPrefabs;
    public float spawnInterval = 2f;
    public float cellSize = 1f;

    private float inputTimer = 0f;
    private int spawnIndex = 0;

    void Awake()
    {
        if (cellButton == null) cellButton = GetComponent<Button>();
        cellButton.onClick.AddListener(OnCellClicked);
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
                // No prefab spawning needed! Just set machineType
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
                HandleGridCellClick();
                break;
            case CellRole.Top:
                // Future: Handle top row clicks
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

    void Update()
{
    if (cellType == CellType.Machine && machineType == MachineType.Input)
    {
        if (spawnPrefabs == null || spawnPrefabs.Length == 0) return;
        inputTimer += Time.deltaTime;
        if (inputTimer >= spawnInterval)
        {
            inputTimer = 0f;

            // Get the cell's position in screen space (UI)
            Vector3 screenPos = borderImage.rectTransform.position; // or transform.position

            // Convert to world space
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
            worldPos.z = 0; // Set Z to 0 for 2D games

            // Spawn the prefab in world space
            GameObject prefabToSpawn = spawnPrefabs[spawnIndex];
            GameObject spawnedObj = Instantiate(prefabToSpawn, worldPos, Quaternion.identity);

            spawnIndex = (spawnIndex + 1) % spawnPrefabs.Length;
        }
    }
}
}