using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UICell : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
{
    // Essential UI components only
    public Button cellButton;

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

    // Drag-and-drop state
    private bool isDragging = false;
    private GameObject draggedMachineVisual;
    private Canvas dragCanvas;
    private Vector2 dragStartPosition;

    public enum CellType { Blank, Machine }
    public enum CellRole { Grid, Top, Bottom }
    public enum Direction { Up, Right, Down, Left }

    void Awake()
    {
        if (cellButton == null) cellButton = GetComponent<Button>();
        cellButton.onClick.AddListener(OnCellClicked);

        Debug.Log($"UICell Awake() - cell will be initialized with MachineRenderer for ALL visuals");
    }

    // Now receive texture/material in Init
    public void Init(int x, int y, UIGridManager gridManager, Texture conveyorTexture, Material conveyorMaterial)
    {
        this.x = x;
        this.y = y;
        this.gridManager = gridManager;
        this.conveyorTexture = conveyorTexture;
        this.conveyorMaterial = conveyorMaterial;

        Debug.Log($"UICell.Init({x}, {y}) - cell ready for machine setup");
    }

    public void SetCellRole(CellRole role)
    {
        cellRole = role;
        Debug.Log($"Cell ({x}, {y}) role set to: {role}");
    }

    // This method now receives all its state data and creates MachineRenderer for ALL visuals
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
            // Use different blank machine definitions based on cell role
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
        // Create MachineRenderer GameObject as child
        GameObject rendererObj = new GameObject("MachineRenderer");
        rendererObj.transform.SetParent(this.transform, false);

        RectTransform rendererRT = rendererObj.AddComponent<RectTransform>();
        rendererRT.anchorMin = Vector2.zero;
        rendererRT.anchorMax = Vector2.one;
        rendererRT.offsetMin = Vector2.zero;
        rendererRT.offsetMax = Vector2.zero;

        // Add ConveyorBelt component if this machine has moving parts that need animation
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
        
        // Ensure any existing highlights are positioned correctly above the machine renderer
        Transform existingHighlight = transform.Find("HighlightOverlay");
        if (existingHighlight != null)
        {
            existingHighlight.SetAsLastSibling();
        }
    }

    private float GetCellDirectionRotation(Direction direction)
    {
        switch (direction)
        {
            case Direction.Up: return 0f;
            case Direction.Right: return -90f;
            case Direction.Down: return -180f;
            case Direction.Left: return -270f;
            default: return 0f;
        }
    }

    void OnCellClicked()
    {
        // For now, always use the MachineManager directly to ensure placement works
        GameManager.Instance.GetMachineManager().OnCellClicked(x, y);
        
        // Future: Use MachineInputManager for drag-and-drop features
        // var inputManager = GameManager.Instance.inputManager;
        // if (inputManager != null)
        // {
        //     inputManager.HandleCellClick(x, y);
        // }
        // else
        // {
        //     GameManager.Instance.GetMachineManager().OnCellClicked(x, y);
        // }
    }

    // Drag and Drop Event Handlers
    public void OnPointerDown(PointerEventData eventData)
    {
        if (cellType == CellType.Machine && !string.IsNullOrEmpty(GetMachineDefId()))
        {
            dragStartPosition = eventData.position;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // This handles click when not dragging
        if (!isDragging)
        {
            OnCellClicked();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Only allow dragging existing machines
        if (cellType == CellType.Machine && !string.IsNullOrEmpty(GetMachineDefId()))
        {
            isDragging = true;
            CreateDraggedMachineVisual();
            
            // Hide the original machine temporarily
            if (machineRenderer != null)
            {
                machineRenderer.gameObject.SetActive(false);
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging && draggedMachineVisual != null)
        {
            // Update dragged visual position
            draggedMachineVisual.transform.position = eventData.position;
            
            // Check if we're over a valid drop location and provide visual feedback
            UpdateDragFeedback(eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            isDragging = false;
            
            // Check where we dropped the machine
            bool machineMovedOrDeleted = HandleMachineDrop(eventData.position);
            
            if (!machineMovedOrDeleted)
            {
                // Restore original machine if drop failed
                if (machineRenderer != null)
                {
                    machineRenderer.gameObject.SetActive(true);
                }
            }
            
            // Cleanup drag visuals
            CleanupDragOperation();
        }
    }

    public RectTransform GetItemSpawnPoint()
    {
        // Items should spawn in the ItemsContainer, positioned at this cell's location
        RectTransform itemsContainer = gridManager?.GetItemsContainer();
        if (itemsContainer != null)
        {
            // Create a virtual spawn point positioned at this cell
            Vector3 cellPosition = gridManager.GetCellWorldPosition(x, y);
            
            // For now, return the cell's own transform as the spawn reference
            // The actual item positioning will be handled by UIGridManager.CreateVisualItem
            Debug.Log($"GetItemSpawnPoint for cell ({x}, {y}) using ItemsContainer positioning");
            return this.GetComponent<RectTransform>();
        }

        // Fallback to original behavior if ItemsContainer not available
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
        var gridManager = Object.FindFirstObjectByType<UIGridManager>();
        if (gridManager != null)
        {
            var cellData = gridManager.GetCellData(x, y);
            return cellData?.machineDefId;
        }
        return null;
    }

    private void CreateDraggedMachineVisual()
    {
        // Find or create drag canvas
        dragCanvas = Object.FindFirstObjectByType<Canvas>();
        GameObject canvasObj = GameObject.Find("DragCanvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("DragCanvas");
            dragCanvas = canvasObj.AddComponent<Canvas>();
            dragCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            dragCanvas.sortingOrder = 1000;
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            dragCanvas = canvasObj.GetComponent<Canvas>();
        }

        // Create visual copy of the machine
        draggedMachineVisual = new GameObject("DraggedMachine");
        draggedMachineVisual.transform.SetParent(dragCanvas.transform, false);

        // Copy the machine visual
        if (machineRenderer != null)
        {
            Image dragImage = draggedMachineVisual.AddComponent<Image>();
            Image originalImage = machineRenderer.GetComponentInChildren<Image>();
            if (originalImage != null)
            {
                dragImage.sprite = originalImage.sprite;
                Color dragColor = Color.white;
                dragColor.a = 0.6f; // Semi-transparent
                dragImage.color = dragColor;
            }

            RectTransform dragRT = draggedMachineVisual.GetComponent<RectTransform>();
            RectTransform originalRT = machineRenderer.GetComponent<RectTransform>();
            dragRT.sizeDelta = originalRT.sizeDelta;
        }

        // Disable raycast blocking so we can detect drop targets
        Image img = draggedMachineVisual.GetComponent<Image>();
        if (img != null) img.raycastTarget = false;
    }

    private void UpdateDragFeedback(Vector2 screenPosition)
    {
        if (draggedMachineVisual == null) return;

        // Check if we're over the grid or outside it
        bool isOverGrid = IsPositionOverGrid(screenPosition, out int targetX, out int targetY);
        
        Image dragImage = draggedMachineVisual.GetComponent<Image>();
        if (dragImage != null)
        {
            if (isOverGrid)
            {
                // Check if target location is valid
                var gridManager = Object.FindFirstObjectByType<UIGridManager>();
                var cellData = gridManager?.GetCellData(targetX, targetY);
                bool isValidTarget = cellData != null && cellData.cellType == CellType.Blank;
                
                // Green for valid placement, red for invalid
                Color feedbackColor = isValidTarget ? Color.green : Color.red;
                feedbackColor.a = 0.6f;
                dragImage.color = feedbackColor;
            }
            else
            {
                // Outside grid - show delete indication
                Color deleteColor = Color.red;
                deleteColor.a = 0.3f; // More transparent for delete
                dragImage.color = deleteColor;
            }
        }
    }

    private bool HandleMachineDrop(Vector2 screenPosition)
    {
        bool isOverGrid = IsPositionOverGrid(screenPosition, out int targetX, out int targetY);
        
        if (isOverGrid)
        {
            // Dropped on grid - try to move machine
            return TryMoveMachine(targetX, targetY);
        }
        else
        {
            // Dropped outside grid - delete machine with refund
            return DeleteMachineWithRefund();
        }
    }

    private bool IsPositionOverGrid(Vector2 screenPosition, out int gridX, out int gridY)
    {
        gridX = -1;
        gridY = -1;

        var gridManager = Object.FindFirstObjectByType<UIGridManager>();
        if (gridManager == null) return false;

        RectTransform gridRect = gridManager.gridPanel;
        Camera uiCamera = Camera.main;

        Vector2 localPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(gridRect, screenPosition, uiCamera, out localPosition))
        {
            var gridData = GameManager.Instance.GetCurrentGrid();
            Vector2 gridSize = gridRect.rect.size;
            
            float cellWidth = gridSize.x / gridData.width;
            float cellHeight = gridSize.y / gridData.height;
            
            localPosition += gridSize * 0.5f;
            
            gridX = Mathf.FloorToInt(localPosition.x / cellWidth);
            gridY = Mathf.FloorToInt(localPosition.y / cellHeight);
            
            return gridX >= 0 && gridX < gridData.width && gridY >= 0 && gridY < gridData.height;
        }
        
        return false;
    }

    private bool TryMoveMachine(int targetX, int targetY)
    {
        if (targetX == x && targetY == y) return false; // Same position

        var gridManager = Object.FindFirstObjectByType<UIGridManager>();
        var targetCellData = gridManager?.GetCellData(targetX, targetY);
        var sourceCellData = gridManager?.GetCellData(x, y);
        
        if (targetCellData == null || sourceCellData == null) return false;
        
        // Target must be empty
        if (targetCellData.cellType != CellType.Blank) return false;
        
        // Get machine definition for placement rules
        string machineDefId = sourceCellData.machineDefId;
        var machineDef = FactoryRegistry.Instance.GetMachine(machineDefId);
        if (machineDef == null) return false;
        
        // Check if target location is valid for this machine type
        bool isValidPlacement = false;
        foreach (string placement in machineDef.gridPlacement)
        {
            switch (placement.ToLower())
            {
                case "any": isValidPlacement = true; break;
                case "grid": isValidPlacement = targetCellData.cellRole == CellRole.Grid; break;
                case "top": isValidPlacement = targetCellData.cellRole == CellRole.Top; break;
                case "bottom": isValidPlacement = targetCellData.cellRole == CellRole.Bottom; break;
            }
            if (isValidPlacement) break;
        }
        
        if (!isValidPlacement) return false;
        
        // Move the machine
        targetCellData.cellType = CellType.Machine;
        targetCellData.machineDefId = sourceCellData.machineDefId;
        targetCellData.direction = sourceCellData.direction;
        targetCellData.machine = sourceCellData.machine;
        
        // Clear source cell
        sourceCellData.cellType = CellType.Blank;
        sourceCellData.machineDefId = null;
        sourceCellData.direction = Direction.Up;
        sourceCellData.machine = null;
        
        // Update visuals
        gridManager.UpdateCellVisuals(targetX, targetY, targetCellData.cellType, targetCellData.direction, targetCellData.machineDefId);
        gridManager.UpdateCellVisuals(x, y, sourceCellData.cellType, sourceCellData.direction);
        
        return true;
    }

    private bool DeleteMachineWithRefund()
    {
        var gridManager = Object.FindFirstObjectByType<UIGridManager>();
        var cellData = gridManager?.GetCellData(x, y);
        
        if (cellData == null || cellData.cellType != CellType.Machine) return false;
        
        // Get machine definition for refund calculation
        var machineDef = FactoryRegistry.Instance.GetMachine(cellData.machineDefId);
        if (machineDef == null) return false;
        
        // Calculate refund (80% by default)
        float refundPercentage = 0.8f;
        int refundAmount = Mathf.RoundToInt(machineDef.cost * refundPercentage);
        
        // Give refund
        GameManager.Instance.GetCreditsManager().AddCredits(refundAmount);
        
        // Clear the cell
        cellData.cellType = CellType.Blank;
        cellData.machineDefId = null;
        cellData.direction = Direction.Up;
        cellData.machine = null;
        
        // Update visuals
        gridManager.UpdateCellVisuals(x, y, cellData.cellType, cellData.direction);
        
        Debug.Log($"Machine deleted. Refunded {refundAmount} credits ({refundPercentage * 100}%)");
        return true;
    }

    private void CleanupDragOperation()
    {
        if (draggedMachineVisual != null)
        {
            DestroyImmediate(draggedMachineVisual);
            draggedMachineVisual = null;
        }
    }
}