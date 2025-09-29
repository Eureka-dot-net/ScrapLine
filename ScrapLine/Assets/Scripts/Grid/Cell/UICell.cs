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

    // Store complete data of the machine being dragged
    private string draggedMachineDefId;
    private Direction draggedMachineDirection;
    private CellData draggedCellData; // Complete machine configuration data

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

    public void SetCellType(CellType type, Direction direction, BaseMachine baseMachine = null)
    {
        cellType = type;

        // Clean up any existing renderer
        if (machineRenderer != null)
        {
            DestroyImmediate(machineRenderer.gameObject);
            machineRenderer = null;
        }

        // Create MachineRenderer for ALL cell types (including blanks)
        if (baseMachine != null)
        {
            SetupMachineRenderer(baseMachine, direction);
        }
        else
        {
            // This should not happen with proper grid setup, but provide fallback
            GameLogger.LogWarning(LoggingManager.LogCategory.Grid, $"SetCellType called with null baseMachine at ({x},{y}) - attempting fallback", ComponentId);
            
            // Try to create a default blank machine as fallback
            string defaultMachineId = cellRole switch
            {
                CellRole.Top => "blank_top",
                CellRole.Bottom => "blank_bottom",
                _ => "blank"
            };
            
            var fallbackCellData = new CellData
            {
                x = x,
                y = y,
                cellType = CellType.Blank,
                direction = direction,
                cellRole = cellRole,
                machineDefId = defaultMachineId
            };
            
            var fallbackMachine = MachineFactory.CreateMachine(fallbackCellData);
            if (fallbackMachine != null)
            {
                SetupMachineRenderer(fallbackMachine, direction);
            }
            else
            {
                GameLogger.LogError(LoggingManager.LogCategory.Grid, $"Failed to create fallback machine for cell at ({x},{y})", ComponentId);
            }
        }
    }

    private void SetupMachineRenderer(BaseMachine baseMachine, Direction direction)
    {
        GameObject rendererObj = new GameObject("MachineRenderer");
        rendererObj.transform.SetParent(this.transform, false);

        RectTransform rendererRT = rendererObj.AddComponent<RectTransform>();
        rendererRT.anchorMin = Vector2.zero;
        rendererRT.anchorMax = Vector2.one;
        rendererRT.offsetMin = Vector2.zero;
        rendererRT.offsetMax = Vector2.zero;

        if (baseMachine.MachineDef.isMoving)
        {
            ConveyorBelt conveyorBelt = rendererObj.AddComponent<ConveyorBelt>();
            conveyorBelt.SetConveyorDirection(direction);
        }

        machineRenderer = rendererObj.AddComponent<MachineRenderer>();
        machineRenderer.Setup(
            baseMachine,
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

        // Store the complete machine data before blanking the cell
        var gridManager = GameManager.Instance.GetGridManager();
        if (gridManager != null)
        {
            var cellData = gridManager.GetCellData(x, y);
            if (cellData != null && cellData.cellType == CellType.Machine)
            {
                // Store basic data for backward compatibility
                draggedMachineDefId = cellData.machineDefId;
                draggedMachineDirection = cellData.direction;
                
                // Store complete machine configuration data for drag visual and restoration
                draggedCellData = new CellData
                {
                    x = cellData.x,
                    y = cellData.y,
                    cellType = cellData.cellType,
                    direction = cellData.direction,
                    cellRole = cellData.cellRole,
                    machineDefId = cellData.machineDefId,
                    machineState = cellData.machineState,
                    selectedRecipeId = cellData.selectedRecipeId,
                    sortingConfig = cellData.sortingConfig != null ? new SortingMachineConfig 
                    {
                        leftItemType = cellData.sortingConfig.leftItemType,
                        rightItemType = cellData.sortingConfig.rightItemType
                    } : null,
                    wasteCrate = cellData.wasteCrate
                    // Note: items and waitingItems are not copied as they represent current state
                    // machine object will be recreated
                };
                
                GameLogger.LogMachine($"Stored complete machine data for drag: {draggedMachineDefId} with config", ComponentId);
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
            bool placementSuccess = false;
            
            // Try using complete configuration data first
            if (draggedCellData != null)
            {
                placementSuccess = GameManager.Instance.PlaceDraggedMachineWithData(
                    targetCell.x,
                    targetCell.y,
                    draggedCellData
                );
                GameLogger.LogMachine($"Attempted placement with complete data: {placementSuccess}", ComponentId);
            }
            
            // Fallback to basic method if complete data placement failed
            if (!placementSuccess)
            {
                placementSuccess = GameManager.Instance.PlaceDraggedMachine(
                    targetCell.x,
                    targetCell.y,
                    draggedMachineDefId,
                    draggedMachineDirection
                );
                GameLogger.LogMachine($"Fallback placement with basic data: {placementSuccess}", ComponentId);
            }

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
        if (draggedCellData == null && string.IsNullOrEmpty(draggedMachineDefId))
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Grid, $"Cannot restore machine to cell ({x}, {y}) - no stored machine data", ComponentId);
            return;
        }

        bool restoreSuccess = false;
        
        // Try using complete configuration data first
        if (draggedCellData != null)
        {
            // Update position to current cell
            draggedCellData.x = x;
            draggedCellData.y = y;
            
            restoreSuccess = GameManager.Instance.PlaceDraggedMachineWithData(x, y, draggedCellData);
            GameLogger.LogMachine($"Attempted restore with complete data: {restoreSuccess}", ComponentId);
        }
        
        // Fallback to basic method if complete data restoration failed
        if (!restoreSuccess && !string.IsNullOrEmpty(draggedMachineDefId))
        {
            restoreSuccess = GameManager.Instance.PlaceDraggedMachine(x, y, draggedMachineDefId, draggedMachineDirection);
            GameLogger.LogMachine($"Fallback restore with basic data: {restoreSuccess}", ComponentId);
        }
        
        if (!restoreSuccess)
        {
            GameLogger.LogError(LoggingManager.LogCategory.Grid, $"Failed to restore machine to original cell ({x}, {y})", ComponentId);
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
    /// Creates machine visuals from definition for drag display using complete configuration data
    /// </summary>
    private bool CreateMachineVisualFromDefinition()
    {
        if (draggedCellData == null || dragVisual == null)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, "CreateMachineVisualFromDefinition: No complete machine data stored, falling back to basic data", ComponentId);
            return CreateMachineVisualFromBasicData();
        }

        var machineDef = FactoryRegistry.Instance.GetMachine(draggedCellData.machineDefId);
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
            // Create BaseMachine instance using complete configuration data
            var tempBaseMachine = MachineFactory.CreateMachine(draggedCellData);
            if (tempBaseMachine != null)
            {
                machineRenderer.Setup(tempBaseMachine, draggedCellData.direction, gridManager, 
                                    draggedCellData.x, draggedCellData.y,
                                    gridManager.conveyorSharedTexture, gridManager.conveyorSharedMaterial);
                                    
                GameLogger.LogMachine($"Created drag visual with complete configuration for {draggedCellData.machineDefId}", ComponentId);
                return true;
            }
            else
            {
                GameLogger.LogError(LoggingManager.LogCategory.UI, $"Failed to create temporary machine instance for drag visual: {draggedCellData.machineDefId}", ComponentId);
            }
        }

        DestroyImmediate(tempRenderer);
        return false;
    }

    /// <summary>
    /// Fallback method to create machine visuals using basic data (for backward compatibility)
    /// </summary>
    private bool CreateMachineVisualFromBasicData()
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
            // Create temporary CellData and BaseMachine instance for drag visual (basic data only)
            var tempCellData = new CellData
            {
                x = x,
                y = y,
                cellType = CellType.Machine,
                direction = draggedMachineDirection,
                machineDefId = draggedMachineDefId
            };
            
            var tempBaseMachine = MachineFactory.CreateMachine(tempCellData);
            if (tempBaseMachine != null)
            {
                machineRenderer.Setup(tempBaseMachine, draggedMachineDirection, gridManager, x, y,
                                    gridManager.conveyorSharedTexture, gridManager.conveyorSharedMaterial);
                                    
                GameLogger.LogMachine($"Created drag visual with basic data for {draggedMachineDefId}", ComponentId);
                return true;
            }
            else
            {
                GameLogger.LogError(LoggingManager.LogCategory.UI, $"Failed to create temporary machine instance for drag visual: {draggedMachineDefId}", ComponentId);
            }
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
        //DO NOT update TextAnchor.MiddleCenter to use the ScriptSandbox fqdn as that breaks the unity project
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

        //DO NOT update RenderMode.ScreenSpaceOverlay to use the ScriptSandbox fqdn as that breaks the unity project
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

    #endregion
}