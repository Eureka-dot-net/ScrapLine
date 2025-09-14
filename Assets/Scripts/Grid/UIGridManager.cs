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
                    cellScript.SetCellType(cellData.cellType, cellData.direction, cellData.machineDefId);
                    
                    // If this is a machine cell, set up the machine renderer
                    if (cellData.cellType == CellType.Machine && !string.IsNullOrEmpty(cellData.machineDefId))
                    {
                        SetupMachineRenderer(cellScript, cellData.machineDefId);
                    }
                }
            }
        }

        if (movingItemsContainer != null)
        {
            movingItemsContainer.SetAsLastSibling();
        }
    }



    public void UpdateCellVisuals(int x, int y, CellType newType, Direction newDirection, string machineDefId = null)
    {
        UICell cell = GetCell(x, y);
        if (cell != null)
        {
            cell.SetCellType(newType, newDirection, machineDefId);
            
            // If this is a machine cell, set up the machine renderer
            if (newType == CellType.Machine && !string.IsNullOrEmpty(machineDefId))
            {
                SetupMachineRenderer(cell, machineDefId);
            }
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
            UpdateCellVisuals(cell.x, cell.y, cell.cellType, cell.direction, cell.machineType, cell.machineDefId);
        }
    }

    // Grid highlighting for machine placement
    public void HighlightValidPlacements(MachineDef machineDef)
    {
        if (cellScripts == null || gridData == null)
        {
            Debug.LogError("Cannot highlight - grid not initialized");
            return;
        }

        ClearHighlights(); // Clear any existing highlights

        // Check each cell to see if it's a valid placement for this machine
        for (int y = 0; y < gridData.height; y++)
        {
            for (int x = 0; x < gridData.width; x++)
            {
                if (IsValidPlacement(x, y, machineDef))
                {
                    HighlightCell(x, y, true);
                }
            }
        }
    }

    public void ClearHighlights()
    {
        if (cellScripts == null) return;

        for (int y = 0; y < gridData.height; y++)
        {
            for (int x = 0; x < gridData.width; x++)
            {
                HighlightCell(x, y, false);
            }
        }
    }

    private void HighlightCell(int x, int y, bool highlight)
    {
        UICell cell = GetCell(x, y);
        if (cell == null) return;

        // Create or get highlight overlay
        Transform highlightOverlay = cell.transform.Find("HighlightOverlay");
        
        if (highlight)
        {
            if (highlightOverlay == null)
            {
                // Create highlight overlay
                GameObject overlay = new GameObject("HighlightOverlay");
                overlay.transform.SetParent(cell.transform, false);
                
                Image overlayImage = overlay.AddComponent<Image>();
                overlayImage.color = new Color(0f, 1f, 0f, 0.3f); // Semi-transparent green
                
                // Make it fill the cell
                RectTransform rt = overlay.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;
                
                // Put it on top but behind any items
                overlay.transform.SetSiblingIndex(cell.transform.childCount - 1);
            }
            else
            {
                highlightOverlay.gameObject.SetActive(true);
            }
        }
        else
        {
            if (highlightOverlay != null)
            {
                highlightOverlay.gameObject.SetActive(false);
            }
        }
    }

    private bool IsValidPlacement(int x, int y, MachineDef machineDef)
    {
        // Get the cell data
        CellData cellData = GetCellData(x, y);
        if (cellData == null) return false;

        // Check if machine's grid placement rules allow this cell
        foreach (string placement in machineDef.gridPlacement)
        {
            switch (placement.ToLower())
            {
                case "any":
                    return true;
                case "grid":
                    return cellData.cellRole == CellRole.Grid;
                case "top":
                    return cellData.cellRole == CellRole.Top;
                case "bottom":
                    return cellData.cellRole == CellRole.Bottom;
            }
        }

        return false;
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
        CellData endCellData = GetCellData(endX, endY);
        bool shouldChangeParent = ShouldChangeParent(progress, movementDirection, endCellData?.machineDefId);
        RectTransform currentParent = item.transform.parent as RectTransform;
        RectTransform targetParent = shouldChangeParent ? endSpawnPoint : startSpawnPoint;
        Debug.Log($"Item {itemId} progress: {progress}, shouldChangeParent: {shouldChangeParent}, currentParent: {currentParent?.name}, targetParent: {targetParent?.name}");
        if (currentParent != targetParent)
        {
            item.transform.SetParent(targetParent, true);
        }
    }

    private bool ShouldChangeParent(float progress, UICell.Direction movementDirection, string machineDefId)
    {
        // For Down and Right movement: change parent before hitting boundary (30%)
        // Also for specific machine types to ensure items appear below roof
        bool isSpecialMachine = false;
        if (!string.IsNullOrEmpty(machineDefId))
        {
            var machineDef = FactoryRegistry.Instance.GetMachine(machineDefId);
            // Check for machines that need special handling (like the old ThreeInputsOneOutput)
            isSpecialMachine = machineDef != null && machineDef.type == "Shredder"; // Example special case
        }
        
        if (movementDirection == UICell.Direction.Down || movementDirection == UICell.Direction.Right || isSpecialMachine)
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

    public CellData GetCellData(int x, int y)
    {
        if (gridData == null) return null;
        
        foreach (var cell in gridData.cells)
        {
            if (cell.x == x && cell.y == y)
            {
                return cell;
            }
        }
        return null;
    }

    private void SetupMachineRenderer(UICell cell, string machineDefId)
    {
        // Get the machine definition
        MachineDef machineDef = FactoryRegistry.Instance.GetMachine(machineDefId);
        if (machineDef == null)
        {
            Debug.LogWarning($"Machine definition not found for ID: {machineDefId}");
            return;
        }

        // Find or create a MachineRenderer component
        MachineRenderer renderer = cell.GetComponentInChildren<MachineRenderer>();
        if (renderer == null)
        {
            // Create a new GameObject for the machine renderer
            GameObject rendererObj = new GameObject("MachineRenderer");
            rendererObj.transform.SetParent(cell.transform, false);
            
            // Set up the RectTransform to fill the cell
            RectTransform rt = rendererObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            
            renderer = rendererObj.AddComponent<MachineRenderer>();
        }

        // Setup the renderer with the machine definition
        renderer.Setup(machineDef);
    }

}