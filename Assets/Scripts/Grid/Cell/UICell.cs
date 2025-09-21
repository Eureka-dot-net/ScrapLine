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

        // Create drag visual
        CreateDragVisual();

        // Position drag visual at current mouse position immediately
        if (dragVisual != null)
        {
            UpdateDragVisualPosition(eventData);
        }
        else
        {
            Debug.LogError("Failed to create drag visual!");
            return;
        }
        // Now blank the original cell immediately (both data and visuals)
        // This prevents phantom machine effects
        GameManager.Instance.OnCellDragStarted(x, y);

    }
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        // Update drag visual position - this should make it follow the cursor
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

            if (!placementSuccess)
            {
                // Placement failed - restore machine to original cell
                RestoreMachineToOriginalCell();
            }
        }
        else if (targetCell == this)
        {
            // Dropped on same cell - restore machine and trigger rotation
            RestoreMachineToOriginalCell();
            GameManager.Instance.OnCellClicked(x, y);
        }
        else
        {
            // Dropped outside grid - machine is deleted (no need to restore)
            GameManager.Instance.RefundMachineWithId(draggedMachineDefId);
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
        if (string.IsNullOrEmpty(draggedMachineDefId))
        {
            Debug.LogError("CreateDragVisual: No machine data stored");
            return;
        }

        // Find the UI Canvas
        Canvas targetCanvas = FindFirstObjectByType<UIGridManager>()?.gridPanel?.GetComponentInParent<Canvas>() 
                             ?? FindObjectOfType<Canvas>();
        
        if (targetCanvas == null)
        {
            Debug.LogError("Could not find Canvas for drag visual");
            return;
        }

        // Create drag visual container with proper setup
        dragVisual = new GameObject("DragVisual");
        dragVisual.transform.SetParent(targetCanvas.transform, false);

        RectTransform dragRT = dragVisual.AddComponent<RectTransform>();
        dragRT.anchorMin = new Vector2(0.5f, 0.5f);  // Center anchoring for smooth positioning
        dragRT.anchorMax = new Vector2(0.5f, 0.5f);
        dragRT.pivot = new Vector2(0.5f, 0.5f);
        dragRT.sizeDelta = GetComponent<RectTransform>().sizeDelta;

        // Create machine visual (with fallback if needed)
        if (!CreateMachineVisualFromDefinition())
        {
            CreateFallbackVisual();
        }

        // Configure visual appearance
        CanvasGroup dragCanvasGroup = dragVisual.AddComponent<CanvasGroup>();
        dragCanvasGroup.alpha = 0.9f;
        dragCanvasGroup.blocksRaycasts = false;
        dragVisual.transform.SetAsLastSibling();
    }

    /// <summary>
    /// Creates machine visuals from definition for drag display
    /// </summary>
    private bool CreateMachineVisualFromDefinition()
    {
        if (string.IsNullOrEmpty(draggedMachineDefId) || dragVisual == null)
            return false;

        var machineDef = FactoryRegistry.Instance.GetMachine(draggedMachineDefId);
        if (machineDef == null)
            return false;

        // Create MachineRenderer in menu mode (keeps sprites local)
        GameObject tempRenderer = new GameObject("TempMachineRenderer");
        tempRenderer.transform.SetParent(dragVisual.transform, false);
        
        RectTransform tempRT = tempRenderer.AddComponent<RectTransform>();
        tempRT.anchorMin = Vector2.zero;
        tempRT.anchorMax = Vector2.one;
        tempRT.offsetMin = Vector2.zero;
        tempRT.offsetMax = Vector2.zero;

        MachineRenderer machineRenderer = tempRenderer.AddComponent<MachineRenderer>();
        machineRenderer.isInMenu = true;  // Keep sprites local, don't use grid containers
        
        var gridManager = FindFirstObjectByType<UIGridManager>();
        if (gridManager != null)
        {
            machineRenderer.Setup(machineDef, draggedMachineDirection, gridManager, x, y,
                                gridManager.conveyorSharedTexture, gridManager.conveyorSharedMaterial);
            return true;
        }
        
        DestroyImmediate(tempRenderer);
        return false;
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
        fallbackImage.color = new Color(1f, 1f, 0f, 0.8f); // Semi-transparent yellow
        
        RectTransform fallbackRT = fallbackObj.GetComponent<RectTransform>();
        fallbackRT.anchorMin = Vector2.zero;
        fallbackRT.anchorMax = Vector2.one;
        fallbackRT.offsetMin = Vector2.zero;
        fallbackRT.offsetMax = Vector2.zero;
        
        // Add a simple text to show what machine this represents
        GameObject textObj = new GameObject("MachineText");
        textObj.transform.SetParent(fallbackObj.transform, false);
        
        Text machineText = textObj.AddComponent<Text>();
        machineText.text = draggedMachineDefId ?? "Machine";
        machineText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        machineText.fontSize = 12;
        machineText.color = Color.black;
        machineText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
    }

    private void UpdateDragVisualPosition(PointerEventData eventData)
    {
        if (dragVisual == null) return;

        RectTransform dragRT = dragVisual.GetComponent<RectTransform>();
        if (dragRT == null) return;

        // Get the canvas that the drag visual is in
        Canvas canvas = dragVisual.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        // Convert screen position to canvas local position
        Vector2 localPosition;
        RectTransform canvasRT = canvas.transform as RectTransform;

        bool converted = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT,
            eventData.position,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out localPosition);

        if (converted)
        {
            // For center anchoring with Screen Space - Overlay canvas, 
            // we can use the converted local position directly as anchored position
            // since the anchor is at the center and local position is relative to center
            dragRT.anchoredPosition = localPosition;
            
            // Only log position updates when user is actively dragging (not on every frame)
            if (isDragging && Time.frameCount % 30 == 0) // Log every 30 frames (~0.5 sec)
            {
                Debug.Log($"Drag position: Screen({eventData.position.x:F0},{eventData.position.y:F0}) -> Canvas({localPosition.x:F0},{localPosition.y:F0})");
            }
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

    #endregion
}