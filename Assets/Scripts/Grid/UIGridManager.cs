using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UICell;

public class UIGridManager : MonoBehaviour
{
    public RectTransform gridPanel;
    public GameObject cellPrefab;
    public RectTransform movingItemsContainer;
    public GameObject itemPrefab;

    // *** Add these fields for shared resources! ***
    public Texture conveyorSharedTexture;
    public Material conveyorSharedMaterial;

    private GridData gridData;
    private UICell[,] cellScripts;

    private Dictionary<string, GameObject> visualItems = new Dictionary<string, GameObject>();

    private RectTransform bordersContainer;
    private RectTransform buildingsContainer;
    private RectTransform itemsContainer; // New container for moving items

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

        CreateRenderingLayers();

        GridLayoutGroup layout = gridPanel.GetComponent<GridLayoutGroup>();
        if (layout != null)
        {
            float cellWidth = gridPanel.rect.width / gridData.width;
            float cellHeight = gridPanel.rect.height / gridData.height;
            layout.cellSize = new Vector2(cellWidth, cellHeight);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = gridData.width;
        }

        if (cellScripts != null)
        {
            foreach (var cell in cellScripts)
            {
                if (cell != null)
                    Destroy(cell.gameObject);
            }
        }

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

                // *** Pass shared resources to cell ***
                cellScript.Init(x, y, this, conveyorSharedTexture, conveyorSharedMaterial);

                // Set cell role first (for Top/Bottom row cells)
                CellData cellData = GetCellData(x, y);
                if (cellData != null)
                {
                    Debug.Log($"Cell ({x}, {y}) has data: role={cellData.cellRole}, type={cellData.cellType}, machineDefId={cellData.machineDefId}");
                    cellScript.SetCellRole(cellData.cellRole);
                    cellScript.SetCellType(cellData.cellType, cellData.direction, cellData.machineDefId);
                }
                else
                {
                    Debug.Log($"Cell ({x}, {y}) has no data - creating as blank cell");
                    cellScript.SetCellRole(CellRole.Grid);
                    cellScript.SetCellType(CellType.Blank, Direction.Up);
                }
            }
        }

        if (movingItemsContainer != null)
        {
            movingItemsContainer.SetAsLastSibling();
        }

        // Update movingItemsContainer to use the new ItemsContainer if it's not assigned
        if (movingItemsContainer == null && itemsContainer != null)
        {
            movingItemsContainer = itemsContainer;
            Debug.Log("Updated movingItemsContainer to use new ItemsContainer");
        }
    }

    private void CreateRenderingLayers()
    {
        // Get the parent container (should be the same parent as gridPanel)
        Transform parentContainer = gridPanel.parent;

        // Clean up existing rendering layers if they exist
        if (bordersContainer != null)
        {
            DestroyImmediate(bordersContainer.gameObject);
        }
        if (buildingsContainer != null)
        {
            DestroyImmediate(buildingsContainer.gameObject);
        }
        if (itemsContainer != null)
        {
            DestroyImmediate(itemsContainer.gameObject);
        }

        // Create BordersContainer (index 0) - BELOW GridPanel for borders and moving parts
        GameObject bordersObj = new GameObject("BordersContainer");
        bordersObj.transform.SetParent(parentContainer, false);
        bordersContainer = bordersObj.AddComponent<RectTransform>();

        // Make it fill the same area as gridPanel
        bordersContainer.anchorMin = gridPanel.anchorMin;
        bordersContainer.anchorMax = gridPanel.anchorMax;
        bordersContainer.offsetMin = gridPanel.offsetMin;
        bordersContainer.offsetMax = gridPanel.offsetMax;
        bordersContainer.anchoredPosition = gridPanel.anchoredPosition;
        bordersContainer.sizeDelta = gridPanel.sizeDelta;

        gridPanel.SetSiblingIndex(0);
        
        bordersObj.transform.SetSiblingIndex(1);

        // Create ItemsContainer (index 2) - ABOVE GridPanel for moving items
        GameObject itemsObj = new GameObject("ItemsContainer");
        itemsObj.transform.SetParent(parentContainer, false);
        itemsContainer = itemsObj.AddComponent<RectTransform>();

        // Make it fill the same area as gridPanel
        itemsContainer.anchorMin = gridPanel.anchorMin;
        itemsContainer.anchorMax = gridPanel.anchorMax;
        itemsContainer.offsetMin = gridPanel.offsetMin;
        itemsContainer.offsetMax = gridPanel.offsetMax;
        itemsContainer.anchoredPosition = gridPanel.anchoredPosition;
        itemsContainer.sizeDelta = gridPanel.sizeDelta;

        // Set as second sibling (index 2) - ABOVE GridPanel
        itemsObj.transform.SetSiblingIndex(2);

        // Create BuildingsContainer (index 3) - ABOVE everything for building sprites
        GameObject buildingsObj = new GameObject("BuildingsContainer");
        buildingsObj.transform.SetParent(parentContainer, false);
        buildingsContainer = buildingsObj.AddComponent<RectTransform>();

        // Make it fill the same area as gridPanel
        buildingsContainer.anchorMin = gridPanel.anchorMin;
        buildingsContainer.anchorMax = gridPanel.anchorMax;
        buildingsContainer.offsetMin = gridPanel.offsetMin;
        buildingsContainer.offsetMax = gridPanel.offsetMax;
        buildingsContainer.anchoredPosition = gridPanel.anchoredPosition;
        buildingsContainer.sizeDelta = gridPanel.sizeDelta;

        // Set as last sibling (index 3) - ABOVE everything
        buildingsObj.transform.SetSiblingIndex(3);

        Debug.Log($"Created rendering layers: BordersContainer (0-bottom), GridPanel (1-middle), ItemsContainer (2-items), BuildingsContainer (3-top)");
        Debug.Log($"BordersContainer parent: {bordersContainer.parent?.name}, position: {bordersContainer.position}, sizeDelta: {bordersContainer.sizeDelta}");
        Debug.Log($"ItemsContainer parent: {itemsContainer.parent?.name}, position: {itemsContainer.position}, sizeDelta: {itemsContainer.sizeDelta}");
        Debug.Log($"BuildingsContainer parent: {buildingsContainer.parent?.name}, position: {buildingsContainer.position}, sizeDelta: {buildingsContainer.sizeDelta}");
        Debug.Log($"GridPanel parent: {gridPanel.parent?.name}, position: {gridPanel.position}, sizeDelta: {gridPanel.sizeDelta}");
    }



    public void UpdateCellVisuals(int x, int y, CellType newType, Direction newDirection, string machineDefId = null)
    {
        UICell cell = GetCell(x, y);
        if (cell != null)
        {
            // UICell now handles ALL visual setup internally via MachineRenderer
            cell.SetCellType(newType, newDirection, machineDefId);
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
            UpdateCellVisuals(cell.x, cell.y, cell.cellType, cell.direction, cell.machineDefId);
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

    public void CreateVisualItem(string itemId, int x, int y, string itemType = null)
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

        // Use ItemsContainer for positioning if available, otherwise fallback to movingItemsContainer
        RectTransform targetContainer = itemsContainer != null ? itemsContainer : movingItemsContainer;
        if (targetContainer == null)
        {
            Debug.LogError("No container available for visual items");
            return;
        }

        // Create the new item instance in the appropriate container
        GameObject newItem = Instantiate(itemPrefab, targetContainer);
        RectTransform itemRect = newItem.GetComponent<RectTransform>();

        // Make item non-interactive to avoid blocking cell button clicks
        CanvasGroup itemCanvasGroup = newItem.AddComponent<CanvasGroup>();
        itemCanvasGroup.blocksRaycasts = false;
        itemCanvasGroup.interactable = false;

        // Position the item at the cell's world position
        Vector3 cellPosition = GetCellWorldPosition(x, y);
        itemRect.position = cellPosition;

        // Size the item to 1/2 of cell size  
        Vector2 cellSize = GetCellSize();
        Vector2 itemSize = cellSize / 2f; // User requested 1/2 cell size
        itemRect.sizeDelta = itemSize;

        Debug.Log($"Created visual item {itemId} in {targetContainer.name} at position {cellPosition} with size {itemSize}");

        // Set the item type on the UIItem component
        UIItem itemComponent = newItem.GetComponent<UIItem>();
        if (itemComponent != null && !string.IsNullOrEmpty(itemType))
        {
            itemComponent.itemType = itemType;
            SetItemSprite(newItem, itemType);
        }

        visualItems[itemId] = newItem;
        Debug.Log($"Created visual item {itemId} at ({x}, {y}) with size {itemSize} (1/3 of cell size {cellSize})");
    }

    private void SetItemSprite(GameObject itemObject, string itemType)
    {
        // Try to load sprite for the item type
        string spritePath = $"Sprites/Items/{itemType}";
        Sprite itemSprite = Resources.Load<Sprite>(spritePath);

        // Get the Image component on the item
        Image itemImage = itemObject.GetComponent<Image>();
        if (itemImage == null)
        {
            itemImage = itemObject.GetComponentInChildren<Image>();
        }

        if (itemImage != null)
        {
            if (itemSprite != null)
            {
                itemImage.sprite = itemSprite;
                itemImage.color = Color.white; // Reset to normal color
                Debug.Log($"Successfully loaded sprite for item type: {itemType}");
            }
            else
            {
                // Sprite not found - set fallback color and log warning
                Debug.LogWarning($"Sprite not found for item type '{itemType}' at path: {spritePath}");
                itemImage.color = Color.magenta; // Make it obvious something is wrong
            }
        }
        else
        {
            Debug.LogWarning($"No Image component found on item prefab for item type: {itemType}");
        }
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

    public bool HasVisualItem(string itemId)
    {
        return visualItems.TryGetValue(itemId, out GameObject item) && item != null;
    }
    
    public GameObject GetVisualItem(string itemId)
    {
        visualItems.TryGetValue(itemId, out GameObject item);
        return item; // Returns null if not found
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

        // Solution 1: Items always stay in grid hierarchy - no more parent switching!
        // The separate rendering layers ensure proper visual order without complex parent handovers
        //Debug.Log($"Item {itemId} progress: {progress} - staying in grid hierarchy for consistent rendering");
    }

    public RectTransform GetBordersContainer()
    {
        Debug.Log($"GetBordersContainer() called - returning: {(bordersContainer != null ? bordersContainer.name : "NULL")}");
        return bordersContainer;
    }

    public RectTransform GetBuildingsContainer()
    {
        Debug.Log($"GetBuildingsContainer() called - returning: {(buildingsContainer != null ? buildingsContainer.name : "NULL")}");
        return buildingsContainer;
    }

    public RectTransform GetItemsContainer()
    {
        Debug.Log($"GetItemsContainer() called - returning: {(itemsContainer != null ? itemsContainer.name : "NULL")}");
        return itemsContainer;
    }

    public Vector2 GetCellSize()
    {
        GridLayoutGroup layout = gridPanel.GetComponent<GridLayoutGroup>();
        Vector2 cellSize = layout != null ? layout.cellSize : Vector2.zero;
        Debug.Log($"GetCellSize() returning: {cellSize}");
        return cellSize;
    }

    public Vector3 GetCellWorldPosition(int x, int y)
    {
        // Always use calculated position to ensure accuracy during initialization
        GridLayoutGroup layout = gridPanel.GetComponent<GridLayoutGroup>();
        if (layout != null && gridData != null)
        {
            Vector2 cellSize = layout.cellSize;
            Vector2 spacing = layout.spacing;
            
            // Calculate position within grid (using grid coordinates)
            float xPos = x * (cellSize.x + spacing.x);
            float yPos = -y * (cellSize.y + spacing.y); // Negative Y because UI goes down
            
            // Get grid panel's world position and add offset
            Vector3 gridWorldPos = gridPanel.transform.position;
            Vector3 calculatedPos = new Vector3(
                gridWorldPos.x + xPos - (gridData.width * (cellSize.x + spacing.x)) / 2 + cellSize.x / 2,
                gridWorldPos.y + yPos + (gridData.height * (cellSize.y + spacing.y)) / 2 - cellSize.y / 2,
                gridWorldPos.z
            );
            
            Debug.Log($"GetCellWorldPosition({x}, {y}) calculated: {calculatedPos} (cellSize: {cellSize}, spacing: {spacing})");
            return calculatedPos;
        }

        // Fallback to cell transform if grid layout calculation fails
        UICell cell = GetCell(x, y);
        if (cell != null)
        {
            Vector3 position = cell.transform.position;
            Debug.Log($"GetCellWorldPosition({x}, {y}) from cell transform fallback: {position}");
            return position;
        }
        
        Debug.LogWarning($"GetCellWorldPosition({x}, {y}) failed - returning zero");
        return Vector3.zero;
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

}