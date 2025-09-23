using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UICell : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    // Data/state properties
    public CellType cellType = CellType.Blank;
    public CellRole cellRole = CellRole.Grid;

    // Position tracking
    [HideInInspector]
    public int x, y;
    private UIGridManager gridManager;

    /// <summary>
    /// Get the component ID for logging purposes
    /// </summary>
    private string ComponentId => $"UICell_{x}_{y}";

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

    }

    public void Init(int x, int y, UIGridManager gridManager, Texture conveyorTexture, Material conveyorMaterial)
    {
        this.x = x;
        this.y = y;
        this.gridManager = gridManager;
        this.conveyorTexture = conveyorTexture;
        this.conveyorMaterial = conveyorMaterial;

    }

    public void SetCellRole(CellRole role)
    {
        cellRole = role;
    }

    public void SetCellType(CellType type, Direction direction, string machineDefId = null)
    {
        cellType = type;

        // Clean up any existing renderer
        if (machineRenderer != null)
        {
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
                    break;
                case CellRole.Bottom:
                    defIdToUse = "blank_bottom";
                    break;
                default:
                    defIdToUse = "blank";
                    break;
            }
        }

        // Create MachineRenderer for ALL cell types (including blanks)
        if (!string.IsNullOrEmpty(defIdToUse))
        {
            var machineDef = FactoryRegistry.Instance.GetMachine(defIdToUse);
            if (machineDef != null)
            {
                SetupMachineRenderer(machineDef, direction);
            }
            else
            {
                GameLogger.LogError(LoggingManager.LogCategory.Grid, $"Could not find machine definition for: {defIdToUse}", ComponentId);
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

    }

    #region Pointer Event Handlers

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDownPosition = eventData.position;
        pointerDownTime = Time.time;
        dragStartPosition = eventData.position;

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

        var machineBarManager = FindAnyObjectByType<MachineBarUIManager>();
        if (machineBarManager != null)
        {
            machineBarManager.ClearSelection();
        }

        // Store the machine data before blanking the cell
        var gridManager = GameManager.Instance.GetGridManager();
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
                GameLogger.LogError(LoggingManager.LogCategory.Grid, $"Failed to get machine data for cell ({x}, {y})", ComponentId);
                return;
            }
        }
        else
        {
            GameLogger.LogError(LoggingManager.LogCategory.Grid, $"Could not find UIGridManager for storing machine data", ComponentId);
            return;
        }

        if (!string.IsNullOrEmpty(draggedMachineDefId))
        {
            var machineDef = FactoryRegistry.Instance.GetMachine(draggedMachineDefId);
            if (machineDef != null)
            {
                var uiGridManager = FindAnyObjectByType<UIGridManager>();
                if (uiGridManager != null)
                {
                    uiGridManager.HighlightValidPlacements(machineDef);
                }
            }
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
            GameLogger.LogError(LoggingManager.LogCategory.Grid, "Failed to create drag visual!", ComponentId);
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

        var uiGridManager = FindAnyObjectByType<UIGridManager>();
        if (uiGridManager != null)
        {
            uiGridManager.ClearHighlights();
        }

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
            GameLogger.LogWarning(LoggingManager.LogCategory.Grid, $"Cannot restore machine to cell ({x}, {y}) - no stored machine data", ComponentId);
            return;
        }

        bool restoreSuccess = GameManager.Instance.PlaceDraggedMachine(x, y, draggedMachineDefId, draggedMachineDirection);
        if (!restoreSuccess)
        {
            GameLogger.LogError(LoggingManager.LogCategory.Grid, $"Failed to restore machine {draggedMachineDefId} to original cell ({x}, {y})", ComponentId);
        }
        else
        {
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
            GameLogger.LogError(LoggingManager.LogCategory.Grid, "CreateDragVisual: No machine data stored", ComponentId);
            return;
        }

        // Find the UI Canvas
        Canvas targetCanvas = FindFirstObjectByType<UIGridManager>()?.gridPanel?.GetComponentInParent<Canvas>()
                             ?? FindFirstObjectByType<Canvas>();

        if (targetCanvas == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.Grid, "Could not find Canvas for drag visual", ComponentId);
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
        machineText.alignment = UnityEngine.UI.Text.TextAnchor.MiddleCenter;

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
            canvas.renderMode == Canvas.RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
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
                return cell;
            }

            // Also check parent objects in case the raycast hit a child
            UICell parentCell = result.gameObject.GetComponentInParent<UICell>();
            if (parentCell != null)
            {
                return parentCell;
            }
        }

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
        UICell[] allCells = UnityEngine.Object.FindObjectsByType<UICell>(FindObjectsSortMode.None);
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
    
    #region Hover/Tooltip Implementation
    
    /// <summary>
    /// Called when pointer enters the cell - show tooltip if machine has one
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (cellType == CellType.Machine && gridManager != null)
        {
            CellData cellData = gridManager.GetCellData(x, y);
            if (cellData?.machine != null)
            {
                string tooltip = cellData.machine.GetTooltip();
                if (!string.IsNullOrEmpty(tooltip))
                {
                    // For now, use Unity's Debug.Log. In a full implementation, 
                    // you'd show a proper UI tooltip here
                    GameLogger.LogUI($"Tooltip for cell ({x},{y}): {tooltip}", ComponentId);
                    
                    // TODO: Show actual tooltip UI element at cursor position
                    // Example: TooltipManager.Instance.ShowTooltip(tooltip, Input.mousePosition);
                }
            }
        }
    }
    
    /// <summary>
    /// Called when pointer exits the cell - hide tooltip
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        // TODO: Hide tooltip UI element
        // Example: TooltipManager.Instance.HideTooltip();
    }
    
    #endregion

    #endregion
}