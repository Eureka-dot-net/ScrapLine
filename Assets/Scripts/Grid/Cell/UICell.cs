using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UICell : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    // Data/state properties
    public CellType cellType = CellType.Blank;
    public CellRole cellRole = CellRole.Grid;

    // Position tracking
    [HideInInspector]
    public int x, y;
    private UIGridManager gridManager;

    // Shared visual resources for moving parts
    private Texture conveyorTexture;
    private Material conveyorMaterial;

    // MachineRenderer handles ALL visuals now
    private MachineRenderer machineRenderer;

    // Drag and drop variables
    private bool isDragging = false;
    private Vector2 dragStartPosition;
    private Vector2 pointerDownPosition;
    private float dragThreshold = 10f; // pixels
    private float clickTimeThreshold = 0.5f; // seconds
    private float pointerDownTime;

    // Visual feedback during drag
    private CanvasGroup canvasGroup;
    private GameObject dragVisual;
    private Canvas dragCanvas;

    // Store data of the machine being dragged
    private string draggedMachineDefId;
    private Direction draggedMachineDirection;

    public enum CellType { Blank, Machine }
    public enum CellRole { Grid, Top, Bottom }
    public enum Direction { Up, Right, Down, Left }

    void Awake()
    {
        // Remove button functionality - we'll handle all interactions manually
        Button existingButton = GetComponent<Button>();
        if (existingButton != null)
        {
            DestroyImmediate(existingButton);
        }

        // Add CanvasGroup for visual feedback
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        Debug.Log($"UICell Awake() - cell will be initialized with drag and drop support");
    }

    public void Init(int x, int y, UIGridManager gridManager, Texture conveyorTexture, Material conveyorMaterial)
    {
        this.x = x;
        this.y = y;
        this.gridManager = gridManager;
        this.conveyorTexture = conveyorTexture;
        this.conveyorMaterial = conveyorMaterial;

        Debug.Log($"UICell.Init({x}, {y}) - cell ready for machine setup and drag interactions");
    }

    public void SetCellRole(CellRole role)
    {
        cellRole = role;
        Debug.Log($"Cell ({x}, {y}) role set to: {role}");
    }

    public void SetCellType(CellType type, Direction direction, string machineDefId = null)
    {
        cellType = type;
        Debug.Log($"Setting cell ({x}, {y}) to type: {type}, direction: {direction}, machineDefId: {machineDefId}");

        // Clean up any existing renderer
        if (machineRenderer != null)
        {
            Debug.Log($"Removing existing MachineRenderer from cell ({x}, {y})");
            DestroyImmediate(machineRenderer.gameObject);
            machineRenderer = null;
        }

        // Determine which machine definition to use
        string defIdToUse = machineDefId;
        if (type == CellType.Blank)
        {
            switch (cellRole)
            {
                case CellRole.Top:
                    defIdToUse = "blank_top";
                    Debug.Log($"Cell ({x}, {y}) is blank type with Top role - using 'blank_top' machine definition");
                    break;
                case CellRole.Bottom:
                    defIdToUse = "blank_bottom";
                    Debug.Log($"Cell ({x}, {y}) is blank type with Bottom role - using 'blank_bottom' machine definition");
                    break;
                default:
                    defIdToUse = "blank";
                    Debug.Log($"Cell ({x}, {y}) is blank type with Grid role - using 'blank' machine definition");
                    break;
            }
        }

        // Create MachineRenderer for ALL cell types (including blanks)
        if (!string.IsNullOrEmpty(defIdToUse))
        {
            var machineDef = FactoryRegistry.Instance.GetMachine(defIdToUse);
            if (machineDef != null)
            {
                Debug.Log($"Creating MachineRenderer for cell ({x}, {y}) with definition: {defIdToUse}");
                SetupMachineRenderer(machineDef, direction);
            }
            else
            {
                Debug.LogError($"Could not find machine definition for: {defIdToUse}");
            }
        }
    }

    private void SetupMachineRenderer(MachineDef def, Direction direction)
    {
        GameObject rendererObj = new GameObject("MachineRenderer");
        rendererObj.transform.SetParent(this.transform, false);

        RectTransform rendererRT = rendererObj.AddComponent<RectTransform>();
        rendererRT.anchorMin = Vector2.zero;
        rendererRT.anchorMax = Vector2.one;
        rendererRT.offsetMin = Vector2.zero;
        rendererRT.offsetMax = Vector2.zero;

        if (def.isMoving)
        {
            ConveyorBelt conveyorBelt = rendererObj.AddComponent<ConveyorBelt>();
            conveyorBelt.SetConveyorDirection(direction);
            Debug.Log($"Set ConveyorBelt direction to {direction} for machine '{def.id}' at cell ({x}, {y})");
        }

        machineRenderer = rendererObj.AddComponent<MachineRenderer>();
        machineRenderer.Setup(
            def,
            direction,
            gridManager,
            x,
            y,
            movingPartTexture: conveyorTexture,
            movingPartMaterial: conveyorMaterial
        );

        Debug.Log($"MachineRenderer setup complete for cell ({x}, {y}) with definition: {def.id}");
    }

    #region Pointer Event Handlers

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDownPosition = eventData.position;
        pointerDownTime = Time.time;
        dragStartPosition = eventData.position;

        Debug.Log($"Pointer down on cell ({x}, {y}) at position {eventData.position}");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Check if we have a machine that can be dragged
        if (!GameManager.Instance.CanStartDrag(x, y))
        {
            Debug.Log($"Cannot start drag on cell ({x}, {y}) - no draggable machine");
            return;
        }

        // Check if we've moved far enough to start dragging
        float dragDistance = Vector2.Distance(eventData.position, dragStartPosition);
        if (dragDistance < dragThreshold)
        {
            return;
        }

        // Store the machine data before blanking the cell
        var gridManager = FindFirstObjectByType<UIGridManager>();
        if (gridManager != null)
        {
            var cellData = gridManager.GetCellData(x, y);
            if (cellData != null && cellData.cellType == CellType.Machine)
            {
                draggedMachineDefId = cellData.machineDefId;
                draggedMachineDirection = cellData.direction;
                
                Debug.Log($"Storing dragged machine data: {draggedMachineDefId}, direction: {draggedMachineDirection}");
            }
            else
            {
                Debug.LogError($"Failed to get machine data for cell ({x}, {y})");
                return;
            }
        }
        else
        {
            Debug.LogError("Could not find UIGridManager for storing machine data");
            return;
        }

        isDragging = true;

        // Create drag visual BEFORE blanking the cell so we can copy the visuals
        Debug.Log($"Creating drag visual for machine {draggedMachineDefId} at screen position {eventData.position}");
        CreateDragVisual();

        // Position drag visual at current mouse position immediately BEFORE blanking
        if (dragVisual != null)
        {
            UpdateDragVisualPosition(eventData);
            Debug.Log($"Drag visual created and positioned. GameObject active: {dragVisual.activeInHierarchy}");
        }
        else
        {
            Debug.LogError("Failed to create drag visual!");
            return;
        }

        // Now blank the original cell immediately (both data and visuals)
        // This prevents phantom machine effects
        GameManager.Instance.OnCellDragStarted(x, y);

        Debug.Log($"Started dragging machine from cell ({x}, {y}) - original cell is now blank");
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        // Update drag visual position
        UpdateDragVisualPosition(eventData);

        // Highlight potential drop targets
        UICell hoveredCell = GetCellUnderPointer(eventData);
        HighlightDropTarget(hoveredCell);
    }


    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;
        ClearDragVisual();
        ClearDropTargetHighlights();

        // Determine where we dropped
        UICell targetCell = GetCellUnderPointer(eventData);

        if (targetCell != null && targetCell != this)
        {
            // Dropped on another cell - attempt to place machine using stored data
            bool placementSuccess = GameManager.Instance.PlaceDraggedMachine(
                targetCell.x, 
                targetCell.y, 
                draggedMachineDefId, 
                draggedMachineDirection
            );

            if (placementSuccess)
            {
                Debug.Log($"Successfully placed dragged machine {draggedMachineDefId} at ({targetCell.x}, {targetCell.y})");
            }
            else
            {
                // Placement failed - restore machine to original cell
                RestoreMachineToOriginalCell();
                Debug.Log($"Failed to place machine at ({targetCell.x}, {targetCell.y}) - restored to original cell ({x}, {y})");
            }
        }
        else if (targetCell == this)
        {
            // Dropped on same cell - restore machine and trigger rotation
            RestoreMachineToOriginalCell();
            GameManager.Instance.OnCellClicked(x, y);
            Debug.Log($"Dropped machine on same cell ({x}, {y}) - rotating");
        }
        else
        {
            // Dropped outside grid - machine is deleted (no need to restore)
            Debug.Log($"Dropped machine from ({x}, {y}) outside grid - machine deleted");
        }

        // Clear stored drag data
        draggedMachineDefId = null;
        draggedMachineDirection = Direction.Up;
    }

    /// <summary>
    /// Restore the dragged machine to its original cell
    /// Used when drag operation is cancelled or placement fails
    /// </summary>
    private void RestoreMachineToOriginalCell()
    {
        if (string.IsNullOrEmpty(draggedMachineDefId))
        {
            Debug.LogWarning($"Cannot restore machine to cell ({x}, {y}) - no stored machine data");
            return;
        }

        bool restoreSuccess = GameManager.Instance.PlaceDraggedMachine(x, y, draggedMachineDefId, draggedMachineDirection);
        if (!restoreSuccess)
        {
            Debug.LogError($"Failed to restore machine {draggedMachineDefId} to original cell ({x}, {y})");
        }
        else
        {
            Debug.Log($"Restored machine {draggedMachineDefId} to original cell ({x}, {y})");
        }
    }



    public void OnPointerUp(PointerEventData eventData)
    {
        // If we never started dragging, treat as click
        if (!isDragging)
        {
            float clickTime = Time.time - pointerDownTime;
            float clickDistance = Vector2.Distance(eventData.position, pointerDownPosition);

            if (clickTime <= clickTimeThreshold && clickDistance <= dragThreshold)
            {
                Debug.Log($"Treating as click on cell ({x}, {y})");
                GameManager.Instance.OnCellClicked(x, y);
            }
        }
    }

    #endregion

    #region Drag Visual Methods

    private void CreateDragVisual()
    {
        if (machineRenderer == null) return;

        // Find the main Canvas in the scene
        Canvas mainCanvas = FindObjectOfType<Canvas>();
        if (mainCanvas == null)
        {
            Debug.LogError("Could not find main Canvas for drag visual");
            return;
        }

        // Create a simple GameObject for the drag visual
        dragVisual = new GameObject("DragVisual");
        dragVisual.transform.SetParent(mainCanvas.transform, false);

        // Add RectTransform
        RectTransform dragRT = dragVisual.AddComponent<RectTransform>();

        // Use full canvas anchoring for precise positioning
        dragRT.anchorMin = Vector2.zero;
        dragRT.anchorMax = Vector2.zero;
        dragRT.pivot = new Vector2(0.5f, 0.5f);

        // Size it similar to a grid cell
        Vector2 cellSize = GetComponent<RectTransform>().sizeDelta;
        dragRT.sizeDelta = cellSize;

        // Set initial position (will be updated in UpdateDragVisualPosition)
        dragRT.anchoredPosition = Vector2.zero;

        // Copy the visual appearance from the machine renderer
        if (machineRenderer != null)
        {
            // Get all Image components from the machine renderer
            Image[] sourceImages = machineRenderer.GetComponentsInChildren<Image>();

            if (sourceImages.Length > 0)
            {
                foreach (Image sourceImage in sourceImages)
                {
                    // Create a copy of each image
                    GameObject imageObj = new GameObject(sourceImage.name + "_Copy");
                    imageObj.transform.SetParent(dragVisual.transform, false);

                    Image copyImage = imageObj.AddComponent<Image>();
                    copyImage.sprite = sourceImage.sprite;
                    copyImage.color = sourceImage.color;
                    copyImage.material = sourceImage.material;

                    // Copy the RectTransform properties
                    RectTransform sourceRT = sourceImage.GetComponent<RectTransform>();
                    RectTransform copyRT = imageObj.GetComponent<RectTransform>();

                    copyRT.anchorMin = sourceRT.anchorMin;
                    copyRT.anchorMax = sourceRT.anchorMax;
                    copyRT.anchoredPosition = sourceRT.anchoredPosition;
                    copyRT.sizeDelta = sourceRT.sizeDelta;
                    copyRT.pivot = sourceRT.pivot;
                    
                    Debug.Log($"Copied image {sourceImage.name} with sprite: {copyImage.sprite?.name ?? "null"}");
                }
            }
            else
            {
                // Fallback: create a simple colored rectangle if no images found
                Debug.LogWarning("No images found in machine renderer, creating fallback visual");
                CreateFallbackVisual();
            }
        }
        else
        {
            // Fallback: create a simple colored rectangle
            Debug.LogWarning("No machine renderer found, creating fallback visual");
            CreateFallbackVisual();
        }

        // Make it slightly transparent and ensure it's visible
        CanvasGroup dragCanvasGroup = dragVisual.AddComponent<CanvasGroup>();
        dragCanvasGroup.alpha = 0.9f; // Increased alpha for better visibility
        dragCanvasGroup.blocksRaycasts = false; // Important: don't block raycasts

        // Make sure it renders on top of everything
        dragVisual.transform.SetAsLastSibling();

        Debug.Log($"Created drag visual with size: {dragRT.sizeDelta} at position: {dragRT.anchoredPosition}");
    }

    /// <summary>
    /// Creates a simple colored rectangle as a fallback visual when machine renderer copying fails
    /// </summary>
    private void CreateFallbackVisual()
    {
        if (dragVisual == null) return;

        GameObject fallbackObj = new GameObject("FallbackVisual");
        fallbackObj.transform.SetParent(dragVisual.transform, false);

        Image fallbackImage = fallbackObj.AddComponent<Image>();
        fallbackImage.color = Color.yellow; // Bright yellow for visibility
        
        RectTransform fallbackRT = fallbackObj.GetComponent<RectTransform>();
        fallbackRT.anchorMin = Vector2.zero;
        fallbackRT.anchorMax = Vector2.one;
        fallbackRT.offsetMin = Vector2.zero;
        fallbackRT.offsetMax = Vector2.zero;
        
        Debug.Log("Created fallback visual with bright yellow color");
    }

    private void UpdateDragVisualPosition(PointerEventData eventData)
    {
        if (dragVisual == null) return;

        RectTransform dragRT = dragVisual.GetComponent<RectTransform>();
        if (dragRT == null) return;

        // Get the canvas
        Canvas canvas = dragVisual.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        Vector2 localPosition;
        RectTransform canvasRT = canvas.transform as RectTransform;

        // Convert screen position to local canvas position
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT,
            eventData.position,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out localPosition))
        {
            // Directly set the anchored position
            dragRT.anchoredPosition = localPosition;
            
            Debug.Log($"Drag visual positioned at screen: {eventData.position}, local: {localPosition}");
        }
        else
        {
            Debug.LogWarning($"Failed to convert screen position {eventData.position} to local canvas position");
        }
    }


    private void ClearDragVisual()
{
    if (dragVisual != null)
    {
        DestroyImmediate(dragVisual);
        dragVisual = null;
    }
}



    private UICell GetCellUnderPointer(PointerEventData eventData)
    {
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            // Check if this GameObject has a UICell component
            UICell cell = result.gameObject.GetComponent<UICell>();
            if (cell != null)
            {
                Debug.Log($"Found UICell at ({cell.x}, {cell.y}) under pointer");
                return cell;
            }

            // Also check parent objects in case the raycast hit a child
            UICell parentCell = result.gameObject.GetComponentInParent<UICell>();
            if (parentCell != null)
            {
                Debug.Log($"Found UICell in parent at ({parentCell.x}, {parentCell.y}) under pointer");
                return parentCell;
            }
        }

        Debug.Log("No UICell found under pointer - will be treated as outside grid");
        return null;
    }

    private void HighlightDropTarget(UICell cell)
    {
        // Clear previous highlights
        ClearDropTargetHighlights();

        if (cell != null && cell != this && !string.IsNullOrEmpty(draggedMachineDefId))
        {
            // Use the new validation method that works with stored machine data
            bool canDrop = GameManager.Instance.CanDropMachineWithDefId(cell.x, cell.y, draggedMachineDefId);

            // Add visual highlight
            cell.SetHighlight(canDrop);
        }
    }

    private void ClearDropTargetHighlights()
    {
        // Find all cells and clear their highlights
        UICell[] allCells = FindObjectsOfType<UICell>();
        foreach (UICell cell in allCells)
        {
            cell.SetHighlight(false);
        }
    }

    private void SetHighlight(bool highlight)
    {
        // Simple highlight implementation - could be enhanced with better visuals
        if (canvasGroup != null)
        {
            canvasGroup.alpha = highlight ? 1.2f : 1.0f;
        }
    }

    #endregion

    #region Existing Methods (unchanged)

    public RectTransform GetItemSpawnPoint()
    {
        RectTransform itemsContainer = gridManager?.GetItemsContainer();
        if (itemsContainer != null)
        {
            Vector3 cellPosition = gridManager.GetCellWorldPosition(x, y);
            Debug.Log($"GetItemSpawnPoint for cell ({x}, {y}) using ItemsContainer positioning");
            return this.GetComponent<RectTransform>();
        }

        if (machineRenderer != null)
        {
            Transform spawnPointTransform = machineRenderer.transform.Find("ItemSpawnPoint");
            if (spawnPointTransform != null)
            {
                return spawnPointTransform.GetComponent<RectTransform>();
            }
        }

        Transform fallbackSpawn = transform.Find("DefaultSpawnPoint");
        if (fallbackSpawn == null)
        {
            GameObject spawnObj = new GameObject("DefaultSpawnPoint");
            spawnObj.transform.SetParent(this.transform, false);
            RectTransform spawnRT = spawnObj.AddComponent<RectTransform>();
            spawnRT.anchorMin = new Vector2(0.5f, 0.5f);
            spawnRT.anchorMax = new Vector2(0.5f, 0.5f);
            spawnRT.anchoredPosition = Vector2.zero;
            spawnRT.sizeDelta = Vector2.zero;
            return spawnRT;
        }

        return fallbackSpawn.GetComponent<RectTransform>();
    }

    private string GetMachineDefId()
    {
        var gridManager = FindFirstObjectByType<UIGridManager>();
        if (gridManager != null)
        {
            var cellData = gridManager.GetCellData(x, y);
            return cellData?.machineDefId;
        }
        return null;
    }

    #endregion
}