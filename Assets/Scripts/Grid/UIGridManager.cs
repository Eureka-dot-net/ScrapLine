using UnityEngine;
using UnityEngine.UI;

public class UIGridManager : MonoBehaviour
{
    public RectTransform gridPanel;
    public GameObject cellPrefab;
    public Material conveyorSharedMaterial;

    public int width = 5, height = 5;

    public UICell.Direction lastConveyorDirection = UICell.Direction.Up;

    private UICell[,] cellScripts;
    private UICell.MachineType[,] machineTypes; // Track machine type for each cell

    public RectTransform movingItemsContainer;

    public RectTransform GetCellRect(int x, int y)
    {
        // Make sure the coordinates are within the grid bounds
        if (x >= 0 && x < width && y >= 0 && y < height + 2)
        {
            return cellScripts[x, y].GetComponent<RectTransform>();
        }
        return null; // Return null if the coordinates are out of bounds
    }

    // NEW METHOD: Get the UICell script at coordinates
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
                cellScript.Init(x, y, this, conveyorSharedMaterial);

                cellScript.SetCellType(UICell.CellType.Blank);

                if (y == 0)
                    cellScript.SetCellRole(UICell.CellRole.Top);
                else if (y == height + 1)
                    cellScript.SetCellRole(UICell.CellRole.Bottom);
                else
                    cellScript.SetCellRole(UICell.CellRole.Grid);

                cellScripts[x, y] = cellScript;
                machineTypes[x, y] = UICell.MachineType.None;
            }
        }
         movingItemsContainer.SetAsLastSibling();
    }

    // Track cellType and machineType for each cell
    public void OnCellTypeChanged(int x, int y, UICell.CellType newType, UICell.MachineType machineType = UICell.MachineType.None)
    {
        cellScripts[x, y].SetCellType(newType, machineType);
        machineTypes[x, y] = machineType;
    }

    // Optional: Query machine type of cell
    public UICell.MachineType GetMachineType(int x, int y)
    {
        return machineTypes[x, y];
    }
}