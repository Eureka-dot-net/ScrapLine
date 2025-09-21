using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
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
    
    // Input System
    private PlayerInput playerInput;
    private InputAction pointerPositionAction;
    private InputAction pointerPressAction;
    
    void Awake()
    {
        SetupInputActions();
        
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
    }
    
    void SetupInputActions()
    {
        // Create input action map for drag operations
        var actionMap = new InputActionMap("DragDrop");
        
        // Pointer position (mouse/touch position)
        pointerPositionAction = actionMap.AddAction("PointerPosition", 
            InputActionType.Value, "<Pointer>/position");
            
        // Pointer press (mouse click/touch)
        pointerPressAction = actionMap.AddAction("PointerPress", 
            InputActionType.Button, "<Pointer>/press");
            
        pointerPressAction.started += OnPointerDown;
        pointerPressAction.canceled += OnPointerUp;
        
        actionMap.Enable();
    }
    
    void OnPointerDown(InputAction.CallbackContext context)
    {
        Vector2 screenPosition = pointerPositionAction.ReadValue<Vector2>();
        startInputPosition = screenPosition;
        inputStartTime = Time.time;
        hasMovedBeyondThreshold = false;
        
        // Check if we're clicking on an existing machine
        if (TryGetMachineAtScreenPosition(screenPosition, out CellData cellData, out MachineDef machineDef))
        {
            // Start potential drag operation
            originalMachineCell = cellData;
            draggedMachineType = machineDef;
        }
        else
        {
            // Check if we have a selected machine for placement
            MachineDef selectedMachine = machineManager.GetSelectedMachine();
            if (selectedMachine != null)
            {
                draggedMachineType = selectedMachine;
                originalMachineCell = null; // This is a new placement
            }
        }
    }
    
    void OnPointerUp(InputAction.CallbackContext context)
    {
        if (isDragging)
        {
            CompleteDragOperation();
        }
        else if (draggedMachineType != null)
        {
            // This was a click, not a drag - handle as traditional click
            Vector2 screenPosition = pointerPositionAction.ReadValue<Vector2>();
            HandleClickOperation(screenPosition);
        }
        
        // Reset state
        CleanupDragOperation();
    }
    
    void Update()
    {
        if (draggedMachineType != null && !isDragging)
        {
            CheckForDragStart();
        }
        
        if (isDragging)
        {
            UpdateDragVisuals();
            UpdateGridHighlighting();
        }
    }
    
    void CheckForDragStart()
    {
        Vector2 currentPosition = pointerPositionAction.ReadValue<Vector2>();
        float timeSinceStart = Time.time - inputStartTime;
        float distanceMoved = Vector2.Distance(startInputPosition, currentPosition);
        
        // Check if we should start dragging
        bool timeThresholdMet = timeSinceStart >= settings.dragTimeThreshold;
        bool distanceThresholdMet = distanceMoved >= settings.pixelMovementThreshold;
        
        if (timeThresholdMet || distanceThresholdMet)
        {
            StartDragOperation();
        }
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
    
    void UpdateDragVisuals()
    {
        if (ghostMachine != null)
        {
            Vector2 screenPosition = pointerPositionAction.ReadValue<Vector2>();
            ghostMachine.transform.position = screenPosition;
        }
    }
    
    void UpdateGridHighlighting()
    {
        Vector2 screenPosition = pointerPositionAction.ReadValue<Vector2>();
        
        // Check if we're over the grid
        if (TryGetGridCellAtScreenPosition(screenPosition, out int x, out int y))
        {
            CellData cellData = gridManager.GetCellData(x, y);
            bool isValidPlacement = IsValidPlacement(cellData, draggedMachineType);
            
            // Update ghost machine color based on validity
            if (ghostMachine != null)
            {
                Image ghostImage = ghostMachine.GetComponent<Image>();
                Color ghostColor = isValidPlacement ? Color.green : Color.red;
                ghostColor.a = settings.ghostMachineAlpha;
                ghostImage.color = ghostColor;
            }
        }
        else
        {
            // Outside grid - show delete indication
            if (ghostMachine != null)
            {
                Image ghostImage = ghostMachine.GetComponent<Image>();
                Color deleteColor = Color.red;
                deleteColor.a = settings.ghostMachineAlpha * 0.5f; // Extra faded for delete
                ghostImage.color = deleteColor;
            }
        }
    }
    
    void CompleteDragOperation()
    {
        Vector2 screenPosition = pointerPositionAction.ReadValue<Vector2>();
        
        if (TryGetGridCellAtScreenPosition(screenPosition, out int x, out int y))
        {
            // Dropped on grid - try to place machine
            CellData targetCell = gridManager.GetCellData(x, y);
            
            if (IsValidPlacement(targetCell, draggedMachineType))
            {
                // Check if we can afford this placement (for new machines or moves that change cost)
                bool canAfford = true;
                int costDifference = 0;
                
                if (originalMachineCell == null)
                {
                    // New machine placement
                    costDifference = draggedMachineType.cost;
                    canAfford = creditsManager.CanAfford(costDifference);
                }
                // For existing machine moves, no cost difference
                
                if (canAfford)
                {
                    PlaceMachineAtCell(targetCell, draggedMachineType);
                    if (costDifference > 0)
                    {
                        creditsManager.TrySpendCredits(costDifference);
                    }
                }
                else
                {
                    // Can't afford - restore original machine if it existed
                    RestoreOriginalMachine();
                }
            }
            else
            {
                // Invalid placement - restore original machine
                RestoreOriginalMachine();
            }
        }
        else
        {
            // Dropped outside grid - delete machine and refund credits
            DeleteMachineWithRefund();
        }
    }
    
    void HandleClickOperation(Vector2 screenPosition)
    {
        if (TryGetGridCellAtScreenPosition(screenPosition, out int x, out int y))
        {
            // Use existing click logic but updated for new system
            CellData cellData = gridManager.GetCellData(x, y);
            
            if (cellData.cellType == CellType.Machine && !string.IsNullOrEmpty(cellData.machineDefId))
            {
                // Rotate existing machine
                RotateMachine(cellData);
            }
            else if (draggedMachineType != null && originalMachineCell == null)
            {
                // Place new machine
                if (IsValidPlacement(cellData, draggedMachineType) && creditsManager.CanAfford(draggedMachineType.cost))
                {
                    PlaceMachineAtCell(cellData, draggedMachineType);
                    creditsManager.TrySpendCredits(draggedMachineType.cost);
                }
            }
        }
    }
    
    void RestoreOriginalMachine()
    {
        if (originalMachineCell != null)
        {
            PlaceMachineAtCell(originalMachineCell, draggedMachineType);
        }
    }
    
    void DeleteMachineWithRefund()
    {
        if (originalMachineCell != null)
        {
            // Calculate refund amount
            int refundAmount = Mathf.RoundToInt(draggedMachineType.cost * settings.refundPercentage);
            creditsManager.AddCredits(refundAmount);
            
            Debug.Log($"Machine deleted. Refunded {refundAmount} credits ({settings.refundPercentage * 100}%)");
        }
    }
    
    void CleanupDragOperation()
    {
        isDragging = false;
        draggedMachineType = null;
        originalMachineCell = null;
        
        if (ghostMachine != null)
        {
            DestroyImmediate(ghostMachine);
            ghostMachine = null;
        }
        
        if (settings.showGridHighlighting)
        {
            uiGridManager.ClearHighlights();
        }
    }
    
    // Helper methods
    CellData GetCellData(int x, int y)
    {
        return gridManager.GetCellData(x, y);
    }
    
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
        
        // Convert screen position to grid coordinates
        RectTransform gridRect = uiGridManager.gridPanel;
        
        Vector2 localPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            gridRect, screenPosition, uiCamera, out localPosition))
        {
            // Convert local position to grid indices
            Vector2 gridSize = gridRect.rect.size;
            GridData gridData = gridManager.GetCurrentGrid();
            
            float cellWidth = gridSize.x / gridData.width;
            float cellHeight = gridSize.y / gridData.height;
            
            // Adjust for center-based positioning
            localPosition += gridSize * 0.5f;
            
            x = Mathf.FloorToInt(localPosition.x / cellWidth);
            y = Mathf.FloorToInt(localPosition.y / cellHeight);
            
            return x >= 0 && x < gridData.width && y >= 0 && y < gridData.height;
        }
        
        return false;
    }
    
    bool IsValidPlacement(CellData cellData, MachineDef machineDef)
    {
        if (cellData == null || machineDef == null) return false;
        
        // Check machine-specific placement rules
        foreach (string placement in machineDef.gridPlacement)
        {
            switch (placement.ToLower())
            {
                case "any": return true;
                case "grid": return cellData.cellRole == CellRole.Grid;
                case "top": return cellData.cellRole == CellRole.Top;
                case "bottom": return cellData.cellRole == CellRole.Bottom;
            }
        }
        
        return false;
    }
    
    void PlaceMachineAtCell(CellData cellData, MachineDef machineDef)
    {
        cellData.cellType = CellType.Machine;
        cellData.machineDefId = machineDef.id;
        cellData.direction = machineManager.GetLastMachineDirection();
        
        // Create machine behavior object
        cellData.machine = MachineFactory.CreateMachine(cellData);
        
        // Update visual
        uiGridManager.UpdateCellVisuals(cellData.x, cellData.y, cellData.cellType, cellData.direction, cellData.machineDefId);
    }
    
    void RemoveMachineFromCell(CellData cellData)
    {
        cellData.cellType = CellType.Blank;
        cellData.machineDefId = null;
        cellData.machine = null;
        
        // Update visual
        uiGridManager.UpdateCellVisuals(cellData.x, cellData.y, cellData.cellType, cellData.direction);
    }
    
    void RotateMachine(CellData cellData)
    {
        MachineDef machineDef = FactoryRegistry.Instance.GetMachine(cellData.machineDefId);
        if (machineDef != null && machineDef.canRotate)
        {
            cellData.direction = (Direction)(((int)cellData.direction + 1) % 4);
            machineManager.SetLastMachineDirection(cellData.direction);
            
            uiGridManager.UpdateCellVisuals(cellData.x, cellData.y, cellData.cellType, cellData.direction, cellData.machineDefId);
        }
    }
    
    Sprite GetMachineSprite(MachineDef machineDef)
    {
        // Try to get sprite from existing machine renderer or load from resources
        if (!string.IsNullOrEmpty(machineDef.sprite))
        {
            return Resources.Load<Sprite>($"Sprites/{machineDef.sprite}");
        }
        
        return null;
    }
    
    void OnDestroy()
    {
        // Clean up input actions
        if (pointerPressAction != null)
        {
            pointerPressAction.started -= OnPointerDown;
            pointerPressAction.canceled -= OnPointerUp;
        }
        
        CleanupDragOperation();
    }
}