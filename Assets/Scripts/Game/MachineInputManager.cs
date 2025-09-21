using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static UICell;

/// <summary>
/// Manages all machine input interactions including clicks, drags, and placement.
/// Handles both desktop (mouse) and mobile (touch) input with visual feedback.
/// </summary>
public class MachineInputManager : MonoBehaviour
{
    [Header("Configuration")]
    public DragDropSettings settings;
    
    [Header("Visual Feedback")]
    public GameObject ghostMachinePrefab;
    public Canvas dragCanvas; // High-priority canvas for drag operations
    
    // Input tracking
    private Vector2 startInputPosition;
    private float inputStartTime;
    private bool isDragging = false;
    private bool hasMovedBeyondThreshold = false;
    
    // Drag state
    private MachineDef draggedMachineType;
    private CellData originalMachineCell;
    private GameObject ghostMachine;
    private Camera uiCamera;
    
    // Manager references
    private MachineManager machineManager;
    private CreditsManager creditsManager;
    private GridManager gridManager;
    private UIGridManager uiGridManager;
    
    void Awake()
    {
        // Default settings if not configured
        if (settings == null)
        {
            settings = new DragDropSettings();
        }
        
        // Find UI camera
        uiCamera = Camera.main;
        Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera != null)
            {
                uiCamera = canvas.worldCamera;
                break;
            }
        }
    }
    
    void Start()
    {
        // Get manager references
        machineManager = GameManager.Instance.GetMachineManager();
        creditsManager = GameManager.Instance.GetCreditsManager();
        gridManager = GameManager.Instance.GetGridManager();
        uiGridManager = GameManager.Instance.activeGridManager;
        
        // Create drag canvas if not assigned
        if (dragCanvas == null)
        {
            GameObject canvasObj = new GameObject("DragCanvas");
            dragCanvas = canvasObj.AddComponent<Canvas>();
            dragCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            dragCanvas.sortingOrder = 1000; // Very high priority
            canvasObj.AddComponent<GraphicRaycaster>();
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        // Set up global drag detection on grid
        if (uiGridManager != null && uiGridManager.gridPanel != null)
        {
            // Add event trigger to grid panel for global drag detection
            EventTrigger trigger = uiGridManager.gridPanel.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = uiGridManager.gridPanel.gameObject.AddComponent<EventTrigger>();
            }

            // Add drag event handlers
            AddEventTrigger(trigger, EventTriggerType.BeginDrag, (data) => { OnGridBeginDrag((PointerEventData)data); });
            AddEventTrigger(trigger, EventTriggerType.Drag, (data) => { OnGridDrag((PointerEventData)data); });
            AddEventTrigger(trigger, EventTriggerType.EndDrag, (data) => { OnGridEndDrag((PointerEventData)data); });
            AddEventTrigger(trigger, EventTriggerType.PointerDown, (data) => { OnGridPointerDown((PointerEventData)data); });
            AddEventTrigger(trigger, EventTriggerType.PointerUp, (data) => { OnGridPointerUp((PointerEventData)data); });
        }
    }

    void AddEventTrigger(EventTrigger trigger, EventTriggerType eventType, System.Action<BaseEventData> action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = eventType;
        entry.callback.AddListener((data) => { action(data); });
        trigger.triggers.Add(entry);
    }
    
    // Grid drag event handlers
    void OnGridPointerDown(PointerEventData eventData)
    {
        startInputPosition = eventData.position;
        inputStartTime = Time.time;
        isDragging = false;
        hasMovedBeyondThreshold = false;

        // Check if we clicked on a machine
        if (TryGetMachineAtScreenPosition(eventData.position, out CellData cellData, out MachineDef machineDef))
        {
            originalMachineCell = cellData;
            draggedMachineType = machineDef;
            Debug.Log($"Potential drag start on machine {machineDef.id} at ({cellData.x}, {cellData.y})");
        }
        else
        {
            originalMachineCell = null;
            draggedMachineType = null;
        }
    }

    void OnGridPointerUp(PointerEventData eventData)
    {
        if (isDragging)
        {
            CompleteDragOperation(eventData.position);
        }
        else if (originalMachineCell != null)
        {
            // This was a click, not a drag - delegate to normal click handling
            machineManager.OnCellClicked(originalMachineCell.x, originalMachineCell.y);
        }

        CleanupDragOperation();
    }

    void OnGridBeginDrag(PointerEventData eventData)
    {
        if (originalMachineCell != null && draggedMachineType != null)
        {
            float timeSinceStart = Time.time - inputStartTime;
            float distanceMoved = Vector2.Distance(startInputPosition, eventData.position);

            if (timeSinceStart >= settings.dragTimeThreshold || distanceMoved >= settings.pixelMovementThreshold)
            {
                StartDragOperation();
                Debug.Log($"Started dragging machine {draggedMachineType.id}");
            }
        }
    }

    void OnGridDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            UpdateDragVisuals(eventData.position);
            UpdateGridHighlighting(eventData.position);
        }
    }

    void OnGridEndDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            CompleteDragOperation(eventData.position);
        }
        CleanupDragOperation();
    }
    
    void StartDragOperation()
    {
        isDragging = true;
        
        if (originalMachineCell != null && draggedMachineType != null)
        {
            // Create ghost machine visual
            CreateGhostMachine();
            
            // Hide original machine temporarily
            uiGridManager.UpdateCellVisuals(originalMachineCell.x, originalMachineCell.y, CellType.Blank, Direction.Up);
        }
    }

    void CreateGhostMachine()
    {
        if (dragCanvas == null) return;

        ghostMachine = new GameObject("GhostMachine");
        ghostMachine.transform.SetParent(dragCanvas.transform, false);

        Image ghostImage = ghostMachine.AddComponent<Image>();
        
        // Try to get the sprite from the machine definition
        string spritePath = $"Sprites/Machines/{draggedMachineType.id}";
        Sprite machineSprite = Resources.Load<Sprite>(spritePath);
        
        if (machineSprite != null)
        {
            ghostImage.sprite = machineSprite;
        }
        else
        {
            // Fallback to a simple colored rectangle
            ghostImage.color = Color.white;
        }

        // Make it semi-transparent
        Color ghostColor = ghostImage.color;
        ghostColor.a = settings.ghostMachineAlpha;
        ghostImage.color = ghostColor;

        // Set size
        RectTransform ghostRT = ghostMachine.GetComponent<RectTransform>();
        Vector2 cellSize = uiGridManager.GetCellSize();
        ghostRT.sizeDelta = cellSize;

        // Disable raycast so it doesn't block input
        ghostImage.raycastTarget = false;
    }

    void UpdateDragVisuals(Vector2 screenPosition)
    {
        if (ghostMachine != null)
        {
            ghostMachine.transform.position = screenPosition;
        }
    }

    void UpdateGridHighlighting(Vector2 screenPosition)
    {
        // Clear existing highlights
        uiGridManager.ClearHighlights();

        // Check if we're over a valid drop location
        if (TryGetGridCellAtScreenPosition(screenPosition, out int x, out int y))
        {
            CellData targetCell = gridManager.GetCellData(x, y);
            bool isValidTarget = IsValidDragTarget(targetCell);

            if (ghostMachine != null)
            {
                Image ghostImage = ghostMachine.GetComponent<Image>();
                Color feedbackColor = isValidTarget ? Color.green : Color.red;
                feedbackColor.a = settings.ghostMachineAlpha;
                ghostImage.color = feedbackColor;
            }

            // Highlight the target cell
            if (isValidTarget)
            {
                uiGridManager.HighlightCell(x, y, true);
            }
        }
        else
        {
            // Outside grid - deletion zone
            if (ghostMachine != null)
            {
                Image ghostImage = ghostMachine.GetComponent<Image>();
                Color deleteColor = Color.red;
                deleteColor.a = settings.ghostMachineAlpha * 0.5f; // Extra faded for delete
                ghostImage.color = deleteColor;
            }
        }
    }

    bool IsValidDragTarget(CellData targetCell)
    {
        if (targetCell == null || draggedMachineType == null) return false;
        
        // Can't drop on the same cell
        if (originalMachineCell != null && targetCell.x == originalMachineCell.x && targetCell.y == originalMachineCell.y)
            return false;

        // Target must be empty
        if (targetCell.cellType != CellType.Blank) return false;

        // Check placement rules
        foreach (string placement in draggedMachineType.gridPlacement)
        {
            switch (placement.ToLower())
            {
                case "any": return true;
                case "grid": return targetCell.cellRole == CellRole.Grid;
                case "top": return targetCell.cellRole == CellRole.Top;
                case "bottom": return targetCell.cellRole == CellRole.Bottom;
            }
        }

        return false;
    }
    
    void StartDragOperation()
    {
        isDragging = true;
        
        // Remove machine from original position if it exists
        if (originalMachineCell != null)
        {
            RemoveMachineFromCell(originalMachineCell);
        }
        
        // Create ghost machine
        CreateGhostMachine();
        
        // Enable grid highlighting
        if (settings.showGridHighlighting)
        {
            uiGridManager.HighlightValidPlacements(draggedMachineType);
        }
        
        // Haptic feedback on mobile
        if (settings.enableHapticFeedback && Application.isMobilePlatform)
        {
            Handheld.Vibrate();
        }
    }
    
    void CreateGhostMachine()
    {
        if (ghostMachine != null)
        {
            DestroyImmediate(ghostMachine);
        }
        
        // Create ghost machine visual
        ghostMachine = new GameObject("GhostMachine");
        ghostMachine.transform.SetParent(dragCanvas.transform, false);
        
        // Add visual components
        Image ghostImage = ghostMachine.AddComponent<Image>();
        
        // Get machine sprite
        Sprite machineSprite = GetMachineSprite(draggedMachineType);
        if (machineSprite != null)
        {
            ghostImage.sprite = machineSprite;
            Color ghostColor = Color.white;
            ghostColor.a = settings.ghostMachineAlpha;
            ghostImage.color = ghostColor;
        }
        
        // Set size to match grid cell size
        RectTransform ghostRT = ghostMachine.GetComponent<RectTransform>();
        Vector2 cellSize = uiGridManager.GetCellSize();
        ghostRT.sizeDelta = cellSize;
        
        // Disable raycasting so it doesn't interfere with grid detection
        ghostImage.raycastTarget = false;
    }
    
    void CompleteDragOperation(Vector2 screenPosition)
    {
        if (!isDragging || originalMachineCell == null || draggedMachineType == null) return;

        bool machineMovedOrDeleted = false;

        if (TryGetGridCellAtScreenPosition(screenPosition, out int targetX, out int targetY))
        {
            // Dropped on grid
            CellData targetCell = gridManager.GetCellData(targetX, targetY);
            
            if (IsValidDragTarget(targetCell))
            {
                // Move machine to new location
                MoveMachineToCell(originalMachineCell, targetCell);
                machineMovedOrDeleted = true;
                Debug.Log($"Moved machine {draggedMachineType.id} from ({originalMachineCell.x}, {originalMachineCell.y}) to ({targetX}, {targetY})");
            }
        }
        else
        {
            // Dropped outside grid - delete with refund
            DeleteMachineWithRefund(originalMachineCell);
            machineMovedOrDeleted = true;
            Debug.Log($"Deleted machine {draggedMachineType.id} and refunded credits");
        }

        if (!machineMovedOrDeleted)
        {
            // Invalid drop - restore original machine
            RestoreOriginalMachine();
            Debug.Log($"Invalid drop - restored machine {draggedMachineType.id} to original position");
        }
    }

    void MoveMachineToCell(CellData sourceCell, CellData targetCell)
    {
        // Move machine data
        targetCell.cellType = CellType.Machine;
        targetCell.machineDefId = sourceCell.machineDefId;
        targetCell.direction = sourceCell.direction;
        targetCell.machine = sourceCell.machine;

        // Clear source cell
        sourceCell.cellType = CellType.Blank;
        sourceCell.machineDefId = null;
        sourceCell.direction = Direction.Up;
        sourceCell.machine = null;

        // Update visuals
        uiGridManager.UpdateCellVisuals(targetCell.x, targetCell.y, targetCell.cellType, targetCell.direction, targetCell.machineDefId);
        uiGridManager.UpdateCellVisuals(sourceCell.x, sourceCell.y, sourceCell.cellType, sourceCell.direction);
    }

    void DeleteMachineWithRefund(CellData machineCell)
    {
        // Calculate refund
        float refundPercentage = 0.8f; // 80% refund
        int refundAmount = Mathf.RoundToInt(draggedMachineType.cost * refundPercentage);

        // Give refund
        creditsManager.AddCredits(refundAmount);

        // Clear the cell
        machineCell.cellType = CellType.Blank;
        machineCell.machineDefId = null;
        machineCell.direction = Direction.Up;
        machineCell.machine = null;

        // Update visuals
        uiGridManager.UpdateCellVisuals(machineCell.x, machineCell.y, machineCell.cellType, machineCell.direction);

        Debug.Log($"Machine deleted. Refunded {refundAmount} credits ({refundPercentage * 100}%)");
    }

    void RestoreOriginalMachine()
    {
        if (originalMachineCell != null)
        {
            // Restore the original machine visual
            uiGridManager.UpdateCellVisuals(originalMachineCell.x, originalMachineCell.y, 
                originalMachineCell.cellType, originalMachineCell.direction, originalMachineCell.machineDefId);
        }
    }

    void CleanupDragOperation()
    {
        isDragging = false;
        hasMovedBeyondThreshold = false;
        
        if (ghostMachine != null)
        {
            DestroyImmediate(ghostMachine);
            ghostMachine = null;
        }

        uiGridManager.ClearHighlights();

        originalMachineCell = null;
        draggedMachineType = null;
    }
    // Helper methods
    bool TryGetMachineAtScreenPosition(Vector2 screenPosition, out CellData cellData, out MachineDef machineDef)
    {
        cellData = null;
        machineDef = null;

        if (TryGetGridCellAtScreenPosition(screenPosition, out int x, out int y))
        {
            cellData = gridManager.GetCellData(x, y);
            if (cellData != null && cellData.cellType == CellType.Machine && !string.IsNullOrEmpty(cellData.machineDefId))
            {
                machineDef = FactoryRegistry.Instance.GetMachine(cellData.machineDefId);
                return machineDef != null;
            }
        }

        return false;
    }

    bool TryGetGridCellAtScreenPosition(Vector2 screenPosition, out int x, out int y)
    {
        x = -1;
        y = -1;

        if (uiGridManager == null || uiGridManager.gridPanel == null) return false;

        Vector2 localPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            uiGridManager.gridPanel, screenPosition, uiCamera, out localPosition))
        {
            var gridData = GameManager.Instance.GetCurrentGrid();
            if (gridData == null) return false;

            Vector2 gridSize = uiGridManager.gridPanel.rect.size;
            float cellWidth = gridSize.x / gridData.width;
            float cellHeight = gridSize.y / gridData.height;

            // Convert local position to grid coordinates
            localPosition += gridSize * 0.5f; // Offset from center to bottom-left
            
            x = Mathf.FloorToInt(localPosition.x / cellWidth);
            y = Mathf.FloorToInt(localPosition.y / cellHeight);

            return x >= 0 && x < gridData.width && y >= 0 && y < gridData.height;
        }

        return false;
    }

    /// <summary>
    /// Handle cell click from UICell - delegates to MachineManager for now
    /// </summary>
    public void HandleCellClick(int x, int y)
    {
        if (machineManager != null)
        {
            machineManager.OnCellClicked(x, y);
        }
    }

    void OnDestroy()
    {
        CleanupDragOperation();
    }
}