using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using static UICell;

/// <summary>
/// Handles drag-and-drop functionality for machines
/// Works alongside existing MachineManager without replacing it
/// </summary>
public class MachineDragDropManager : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Drag Settings")]
    [Tooltip("Percentage of credits refunded when machine is deleted (0.0 to 1.0)")]
    [Range(0f, 1f)]
    public float refundPercentage = 0.8f;
    
    [Tooltip("Alpha value for ghost machine during drag")]
    [Range(0f, 1f)]
    public float ghostAlpha = 0.6f;

    // Internal state
    private bool isDragging = false;
    private CellData draggedMachineCell = null;
    private GameObject ghostMachine = null;
    private Canvas dragCanvas = null;
    
    // Manager references
    private MachineManager machineManager;
    private CreditsManager creditsManager;
    private GridManager gridManager;
    private UIGridManager uiGridManager;

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        // Get manager references
        machineManager = GameManager.Instance.machineManager;
        creditsManager = GameManager.Instance.creditsManager;
        gridManager = GameManager.Instance.gridManager;
        uiGridManager = GameManager.Instance.activeGridManager;

        // Create high-priority canvas for drag visuals
        GameObject canvasGO = new GameObject("DragCanvas");
        canvasGO.transform.SetParent(transform);
        dragCanvas = canvasGO.AddComponent<Canvas>();
        dragCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        dragCanvas.sortingOrder = 1000; // Ensure it's above everything
        canvasGO.AddComponent<GraphicRaycaster>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Convert screen position to grid coordinates
        Vector2 localPos;
        RectTransform gridRect = uiGridManager.gridPanel;
        
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            gridRect, eventData.position, eventData.pressEventCamera, out localPos))
            return;

        // Find which cell was clicked
        int gridX, gridY;
        if (!ScreenToGridCoordinates(localPos, out gridX, out gridY))
            return;

        // Check if there's a machine at this position
        CellData cellData = gridManager.GetCellData(gridX, gridY);
        if (cellData == null || cellData.cellType != CellType.Machine || string.IsNullOrEmpty(cellData.machineDefId))
            return;

        // Start dragging this machine
        StartDrag(cellData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || ghostMachine == null)
            return;

        // Update ghost machine position
        Vector2 screenPos = eventData.position;
        ghostMachine.transform.position = screenPos;

        // Update visual feedback based on drop location
        UpdateDragFeedback(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        // Determine drop action
        HandleDrop(eventData);
        
        // Clean up
        EndDrag();
    }

    private void StartDrag(CellData cellData)
    {
        isDragging = true;
        draggedMachineCell = cellData;
        
        // Create ghost machine
        CreateGhostMachine(cellData);
        
        // Remove machine from its current cell visually (will be restored if drag cancelled)
        uiGridManager.UpdateCellVisuals(cellData.x, cellData.y, CellType.Blank, Direction.Up);
    }

    private void CreateGhostMachine(CellData cellData)
    {
        // Get machine definition
        MachineDef machineDef = ResourceManager.Instance.GetMachineDefById(cellData.machineDefId);
        if (machineDef == null) return;

        // Create ghost machine GameObject
        ghostMachine = new GameObject("GhostMachine");
        ghostMachine.transform.SetParent(dragCanvas.transform);
        
        // Add Image component and set sprite
        UnityEngine.UI.Image image = ghostMachine.AddComponent<UnityEngine.UI.Image>();
        image.sprite = machineDef.sprite;
        image.color = new Color(1f, 1f, 1f, ghostAlpha);
        
        // Set size
        RectTransform rectTransform = ghostMachine.GetComponent<RectTransform>();
        Vector2 cellSize = uiGridManager.GetCellSize();
        rectTransform.sizeDelta = cellSize;
    }

    private void UpdateDragFeedback(PointerEventData eventData)
    {
        if (ghostMachine == null) return;

        // Check if we're over a valid drop location
        Vector2 localPos;
        RectTransform gridRect = uiGridManager.gridPanel;
        
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            gridRect, eventData.position, eventData.pressEventCamera, out localPos))
        {
            int gridX, gridY;
            if (ScreenToGridCoordinates(localPos, out gridX, out gridY))
            {
                // Check if this is a valid placement location
                CellData targetCell = gridManager.GetCellData(gridX, gridY);
                MachineDef machineDef = ResourceManager.Instance.GetMachineDefById(draggedMachineCell.machineDefId);
                
                if (IsValidDropLocation(targetCell, machineDef))
                {
                    // Valid drop - green tint
                    ghostMachine.GetComponent<UnityEngine.UI.Image>().color = new Color(0f, 1f, 0f, ghostAlpha);
                    return;
                }
                else
                {
                    // Invalid drop - red tint
                    ghostMachine.GetComponent<UnityEngine.UI.Image>().color = new Color(1f, 0f, 0f, ghostAlpha);
                    return;
                }
            }
        }
        
        // Outside grid - deletion zone (faded red)
        ghostMachine.GetComponent<UnityEngine.UI.Image>().color = new Color(1f, 0.5f, 0.5f, ghostAlpha * 0.5f);
    }

    private bool IsValidDropLocation(CellData targetCell, MachineDef machineDef)
    {
        if (targetCell == null || machineDef == null) return false;
        
        // Must be blank cell
        if (targetCell.cellType != CellType.Blank) return false;
        
        // Check role restrictions
        UICell targetUICell = uiGridManager.GetCell(targetCell.x, targetCell.y);
        if (targetUICell == null) return false;
        
        // Use existing placement validation logic
        return machineManager.IsValidMachinePlacement(targetCell, machineDef);
    }

    private void HandleDrop(PointerEventData eventData)
    {
        Vector2 localPos;
        RectTransform gridRect = uiGridManager.gridPanel;
        
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            gridRect, eventData.position, eventData.pressEventCamera, out localPos))
        {
            int gridX, gridY;
            if (ScreenToGridCoordinates(localPos, out gridX, out gridY))
            {
                // Dropping on grid
                CellData targetCell = gridManager.GetCellData(gridX, gridY);
                MachineDef machineDef = ResourceManager.Instance.GetMachineDefById(draggedMachineCell.machineDefId);
                
                if (IsValidDropLocation(targetCell, machineDef))
                {
                    // Valid move - relocate machine
                    MoveMachine(draggedMachineCell, targetCell);
                    return;
                }
            }
        }
        
        // Check if we're outside the grid (deletion)
        if (IsOutsideGrid(eventData.position))
        {
            // Delete machine and give refund
            DeleteMachineWithRefund(draggedMachineCell);
            return;
        }
        
        // Invalid drop - restore original position
        RestoreMachine(draggedMachineCell);
    }

    private void MoveMachine(CellData fromCell, CellData toCell)
    {
        // Update data model
        toCell.cellType = CellType.Machine;
        toCell.machineDefId = fromCell.machineDefId;
        toCell.direction = fromCell.direction;
        
        fromCell.cellType = CellType.Blank;
        fromCell.machineDefId = null;
        
        // Update visuals
        uiGridManager.UpdateCellVisuals(toCell.x, toCell.y, toCell.cellType, toCell.direction, toCell.machineDefId);
        uiGridManager.UpdateCellVisuals(fromCell.x, fromCell.y, fromCell.cellType, fromCell.direction);
    }

    private void DeleteMachineWithRefund(CellData cellData)
    {
        // Calculate refund
        MachineDef machineDef = ResourceManager.Instance.GetMachineDefById(cellData.machineDefId);
        if (machineDef != null)
        {
            int refund = Mathf.RoundToInt(machineDef.cost * refundPercentage);
            creditsManager.AddCredits(refund);
        }
        
        // Clear cell
        cellData.cellType = CellType.Blank;
        cellData.machineDefId = null;
        
        // Update visuals
        uiGridManager.UpdateCellVisuals(cellData.x, cellData.y, cellData.cellType, cellData.direction);
    }

    private void RestoreMachine(CellData cellData)
    {
        // Restore machine to original position
        uiGridManager.UpdateCellVisuals(cellData.x, cellData.y, cellData.cellType, cellData.direction, cellData.machineDefId);
    }

    private bool IsOutsideGrid(Vector2 screenPosition)
    {
        Vector2 localPos;
        RectTransform gridRect = uiGridManager.gridPanel;
        
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            gridRect, screenPosition, null, out localPos))
            return true;
            
        Rect gridBounds = gridRect.rect;
        return !gridBounds.Contains(localPos);
    }

    private bool ScreenToGridCoordinates(Vector2 localPos, out int gridX, out int gridY)
    {
        gridX = gridY = -1;
        
        Vector2 cellSize = uiGridManager.GetCellSize();
        GridData gridData = gridManager.GetCurrentGrid();
        
        if (gridData == null || cellSize.x <= 0 || cellSize.y <= 0)
            return false;
        
        // Convert local position to grid coordinates
        Rect gridRect = uiGridManager.gridPanel.rect;
        Vector2 relativePos = localPos - gridRect.min;
        
        gridX = Mathf.FloorToInt(relativePos.x / cellSize.x);
        gridY = Mathf.FloorToInt(relativePos.y / cellSize.y);
        
        // Validate bounds
        return gridX >= 0 && gridX < gridData.width && gridY >= 0 && gridY < gridData.height;
    }

    private void EndDrag()
    {
        isDragging = false;
        draggedMachineCell = null;
        
        if (ghostMachine != null)
        {
            Destroy(ghostMachine);
            ghostMachine = null;
        }
    }

    private void OnDestroy()
    {
        EndDrag();
    }
}