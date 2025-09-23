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

                if (cellObj.GetComponent<Image>() == null)
                {
                    var image = cellObj.AddComponent<Image>();
                    image.color = new Color(0, 0, 0, 0); // Fully transparent
                    image.raycastTarget = true; // Must be true for UI events!
                }

                // *** Pass shared resources to cell ***
                cellScript.Init(x, y, this, conveyorSharedTexture, conveyorSharedMaterial);

                // Set cell role first (for Top/Bottom row cells)
                CellData cellData = GetCellData(x, y);
                if (cellData != null)
                {
                    cellScript.SetCellRole(cellData.cellRole);
                    cellScript.SetCellType(cellData.cellType, cellData.direction, cellData.machineDefId);
                }
                else
                {
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
                    // Only highlight the machine slot (not the grid)
                    HighlightSlot(x, y, true);
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
                // Only clear slot highlights (not grid highlights)
                HighlightSlot(x, y, false);
            }
        }
    }

    /// <summary>
    /// Highlight all machines on the grid that have CanConfigure = true
    /// </summary>
    public void HighlightConfigurableMachines()
    {
        if (cellScripts == null || gridData == null)
        {
            Debug.LogError("Cannot highlight configurable machines - grid not initialized");
            return;
        }

        ClearHighlights(); // Clear any existing highlights

        // Check each cell for configurable machines
        for (int y = 0; y < gridData.height; y++)
        {
            for (int x = 0; x < gridData.width; x++)
            {
                CellData cellData = GetCellData(x, y);
                if (cellData != null && cellData.cellType == CellType.Machine && cellData.machine != null)
                {
                    // Check if the machine can be configured
                    if (cellData.machine.CanConfigure)
                    {
                        HighlightSlot(x, y, true);
                        Debug.Log($"Highlighting configurable machine at ({x}, {y}): {cellData.machineDefId}");
                    }
                }
            }
        }
    }


    private void HighlightSlot(int x, int y, bool highlight)
    {
        if (bordersContainer == null) return;

        // Create highlighting overlay in BordersContainer or higher layer to ensure visibility
        string overlayName = $"SlotHighlight_{x}_{y}";
        Transform existingOverlay = bordersContainer.Find(overlayName);

        if (highlight)
        {
            if (existingOverlay == null)
            {
                // Create slot highlight overlay in BordersContainer (above grid but below buildings)
                GameObject overlay = new GameObject(overlayName);
                overlay.transform.SetParent(bordersContainer, false);

                Image overlayImage = overlay.AddComponent<Image>();
                overlayImage.color = new Color(0.5f, 1f, 0.7f, 0.15f); // Much more subtle green overlay
                
                // Add subtle outline for visibility without being overwhelming
                // Outline outline = overlay.AddComponent<Outline>();
                // outline.effectColor = new Color(0f, 1f, 0f, 0.8f); // Green outline instead of yellow
                // outline.effectDistance = new Vector2(2, 2); // Smaller outline for subtlety

                // Position and size the overlay to match the cell
                RectTransform overlayRT = overlay.GetComponent<RectTransform>();
                Vector3 cellPosition = GetCellWorldPosition(x, y);
                Vector2 cellSize = GetCellSize();
                overlayRT.position = cellPosition;
                overlayRT.sizeDelta = cellSize;

                // Make it non-interactive to avoid blocking clicks
                CanvasGroup overlayCanvasGroup = overlay.AddComponent<CanvasGroup>();
                overlayCanvasGroup.blocksRaycasts = false;
                overlayCanvasGroup.interactable = false;

                // Put it on top within BordersContainer
                overlay.transform.SetAsLastSibling();
            }
            else
            {
                existingOverlay.gameObject.SetActive(true);
                // Ensure it's still on top
                existingOverlay.SetAsLastSibling();
            }
        }
        else
        {
            if (existingOverlay != null)
            {
                existingOverlay.gameObject.SetActive(false);
            }
        }
    }

    private bool IsValidPlacement(int x, int y, MachineDef machineDef)
    {
        // Get the cell data
        CellData cellData = GetCellData(x, y);
        if (cellData == null) return false;

        // First check if cell is empty (blank) - same logic as CanDropMachineWithDefId
        if (cellData.cellType != CellType.Blank)
        {
            return false;
        }

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
        Debug.Log($"Created visual item {itemId} at ({x}, {y}) with size {itemSize} (1/2 of cell size {cellSize})");
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

        // Check if this item is waiting and should be stacked
        Vector3 targetPos = Vector3.Lerp(startPos, endPos, progress);

        // If the item is at the halfway point (progress 0.5) and moving to a processor, apply stacking
        if (progress >= 0.5f && endCell.GetComponent<UICell>() != null)
        {
            CellData endCellData = GetCellData(endX, endY);
            if (endCellData != null && endCellData.machine is ProcessorMachine)
            {
                // Find the item data to get its stack index
                ItemData itemData = FindItemDataById(itemId, endCellData);
                if (itemData != null && itemData.state == ItemState.Waiting)
                {
                    Vector3 stackOffset = CalculateStackOffset(itemData.stackIndex, endCell);
                    targetPos += stackOffset;
                }
            }
        }

        // Set the final position
        item.transform.position = targetPos;

        // Solution 1: Items always stay in grid hierarchy - no more parent switching!
        // The separate rendering layers ensure proper visual order without complex parent handovers
        //Debug.Log($"Item {itemId} progress: {progress} - staying in grid hierarchy for consistent rendering");
    }

    /// <summary>
    /// Finds item data by ID in the given cell's waiting items
    /// </summary>
    private ItemData FindItemDataById(string itemId, CellData cellData)
    {
        foreach (var item in cellData.waitingItems)
        {
            if (item.id == itemId)
                return item;
        }
        return null;
    }

    /// <summary>
    /// Calculates the visual offset for stacking items based on their stack index
    /// </summary>
    private Vector3 CalculateStackOffset(int stackIndex, UICell cell)
    {
        if (stackIndex == 0)
            return Vector3.zero; // Center item

        Vector2 cellSize = GetCellSize();
        float itemSize = cellSize.x / 2f; // Items are 1/2 of cell size (corrected from 1/3)
        float maxStackOffset = (cellSize.x / 2f) - (itemSize / 2f); // Don't go beyond cell boundary

        // Alternate left and right: 1=left, 2=right, 3=left2, 4=right2, etc.
        bool isLeft = (stackIndex % 2 == 1);
        int stackLevel = (stackIndex + 1) / 2; // How far from center (1, 2, 3...)

        // Use much smaller spacing - just a few pixels between items
        float smallSpacing = itemSize * 0.15f; // 15% of item size for minimal spacing
        float requestedOffset = stackLevel * smallSpacing;

        // If the requested offset would go beyond boundaries, stack items on top of each other
        float offsetDistance;
        if (requestedOffset > maxStackOffset)
        {
            // Items that can't fit horizontally stay at max boundary (stacked visually)
            offsetDistance = maxStackOffset;
        }
        else
        {
            offsetDistance = requestedOffset;
        }

        float xOffset = isLeft ? -offsetDistance : offsetDistance;

        Debug.Log($"Stack index {stackIndex}: isLeft={isLeft}, level={stackLevel}, requested={requestedOffset:F1}, actual offset=({xOffset:F1}, 0), itemSize={itemSize}, maxOffset={maxStackOffset}");

        return new Vector3(xOffset, 0, 0);
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