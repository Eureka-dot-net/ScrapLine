using UnityEngine;
using static UICell;

/// <summary>
/// Manages item movement processing and visual updates.
/// Handles item movement logic, visual positioning, and movement completion.
/// </summary>
public class ItemMovementManager : MonoBehaviour
{
    [Header("Movement Configuration")]
    [Tooltip("Speed at which items move between cells")]
    public float itemMoveSpeed = 1f;
    
    [Header("Item Management")]
    [Tooltip("Timeout for items on blank cells")]
    public float itemTimeoutOnBlankCells = 10f;
    
    [Tooltip("Maximum number of items allowed on the grid")]
    public int maxItemsOnGrid = 100;
    
    [Tooltip("Show warning when item limit is reached")]
    public bool showItemLimitWarning = true;
    
    [Header("Debug")]
    [Tooltip("Enable debug logs for item movement")]
    public bool enableMovementLogs = false;

    private int nextItemId = 1;
    private GridManager gridManager;
    private UIGridManager activeGridManager;

    /// <summary>
    /// Initialize the item movement manager
    /// </summary>
    /// <param name="gridManager">Reference to the grid manager</param>
    /// <param name="activeGridManager">Reference to the UI grid manager</param>
    public void Initialize(GridManager gridManager, UIGridManager activeGridManager)
    {
        this.gridManager = gridManager;
        this.activeGridManager = activeGridManager;
    }

    /// <summary>
    /// Generate a unique item ID
    /// </summary>
    /// <returns>Unique item ID string</returns>
    public string GenerateItemId()
    {
        return "item_" + nextItemId++;
    }

    /// <summary>
    /// Process all item movement in the current grid
    /// </summary>
    public void ProcessItemMovement()
    {
        GridData gridData = gridManager.GetCurrentGrid();
        if (gridData == null || activeGridManager == null) 
            return;

        foreach (var cell in gridData.cells)
        {
            for (int i = cell.items.Count - 1; i >= 0; i--)
            {
                ItemData item = cell.items[i];
                
                if (item.state == ItemState.Moving)
                {
                    ProcessMovingItem(item, cell);
                }
                // Removed Idle item handling - machines are now responsible for initiating movement
            }
        }
    }

    /// <summary>
    /// Process the movement of a single moving item
    /// </summary>
    /// <param name="item">The item being moved</param>
    /// <param name="sourceCell">The source cell of the movement</param>
    private void ProcessMovingItem(ItemData item, CellData sourceCell)
    {
        float timeSinceStart = Time.time - item.moveStartTime;
        item.moveProgress = timeSinceStart * itemMoveSpeed;

        // Handle two-phase movement with halfway pause
        if (!item.isHalfway)
        {
            // Phase 1: Moving to halfway point - check if we've reached 50% progress
            if (item.moveProgress >= 0.5f)
            {
                // Item has reached the halfway point
                item.state = ItemState.Idle; // Pause at halfway point
                item.moveProgress = 0.5f; // Clamp to exactly 50%
                item.isHalfway = true; // Mark that item is now at halfway point
                
                if (activeGridManager.HasVisualItem(item.id))
                {
                    // Update visual position to halfway point
                    activeGridManager.UpdateItemVisualPosition(item.id, 0.5f,
                        item.sourceX, item.sourceY, item.targetX, item.targetY, sourceCell.direction);
                }
                
                // Do NOT call CompleteItemMovement - item stays in source cell data model
                if (enableMovementLogs)
                {
                    Debug.Log($"Item {item.id} reached halfway point - pausing for phase 2");
                }
            }
            else
            {
                // Still moving toward halfway point
                if (activeGridManager.HasVisualItem(item.id))
                {
                    activeGridManager.UpdateItemVisualPosition(item.id, item.moveProgress,
                        item.sourceX, item.sourceY, item.targetX, item.targetY, sourceCell.direction);
                }
            }
        }
        else
        {
            // Phase 2: Moving from halfway to full cell - check if we've completed the movement
            if (item.moveProgress >= 1.0f)
            {
                // Movement complete - call CompleteItemMovement to update data model
                CompleteItemMovement(item, sourceCell);
            }
            else
            {
                // Still moving from halfway to full cell
                if (activeGridManager.HasVisualItem(item.id))
                {
                    // Calculate position from halfway point to target
                    float adjustedProgress = 0.5f + (item.moveProgress * 0.5f); // Map 0-1 to 0.5-1
                    activeGridManager.UpdateItemVisualPosition(item.id, adjustedProgress,
                        item.sourceX, item.sourceY, item.targetX, item.targetY, sourceCell.direction);
                }
            }
        }
    }

    /// <summary>
    /// Complete the movement of an item to its target cell
    /// </summary>
    /// <param name="item">The item to complete movement for</param>
    /// <param name="sourceCell">The source cell of the movement</param>
    private void CompleteItemMovement(ItemData item, CellData sourceCell)
    {
        CellData targetCell = gridManager.GetCellData(item.targetX, item.targetY);
        if (targetCell == null)
        {
            Debug.LogError($"Target cell not found at ({item.targetX}, {item.targetY})");
            sourceCell.items.Remove(item);
            if (activeGridManager.HasVisualItem(item.id))
            {
                activeGridManager.DestroyVisualItem(item.id);
            }
            return;
        }

        sourceCell.items.Remove(item);
        targetCell.items.Add(item);

        item.state = ItemState.Idle;
        item.x = targetCell.x;
        item.y = targetCell.y;
        item.moveProgress = 0f;
        item.isHalfway = false; // Reset halfway flag when movement completes

        if (targetCell.machine != null)
        {
            targetCell.machine.OnItemArrived(item);
        }

        if (activeGridManager.HasVisualItem(item.id))
        {
            activeGridManager.UpdateItemVisualPosition(item.id, 1f, item.sourceX, item.sourceY, item.targetX, item.targetY, sourceCell.direction);
        }
    }

    /// <summary>
    /// Get the current item move speed
    /// </summary>
    /// <returns>Current item move speed</returns>
    public float GetItemMoveSpeed()
    {
        return itemMoveSpeed;
    }

    /// <summary>
    /// Set the item move speed
    /// </summary>
    /// <param name="speed">New move speed</param>
    public void SetItemMoveSpeed(float speed)
    {
        itemMoveSpeed = speed;
    }

    /// <summary>
    /// Get the item timeout on blank cells
    /// </summary>
    /// <returns>Timeout in seconds</returns>
    public float GetItemTimeoutOnBlankCells()
    {
        return itemTimeoutOnBlankCells;
    }

    /// <summary>
    /// Set the item timeout on blank cells
    /// </summary>
    /// <param name="timeout">Timeout in seconds</param>
    public void SetItemTimeoutOnBlankCells(float timeout)
    {
        itemTimeoutOnBlankCells = timeout;
    }

    /// <summary>
    /// Get the maximum items on grid limit
    /// </summary>
    /// <returns>Maximum number of items</returns>
    public int GetMaxItemsOnGrid()
    {
        return maxItemsOnGrid;
    }

    /// <summary>
    /// Set the maximum items on grid limit
    /// </summary>
    /// <param name="maxItems">Maximum number of items</param>
    public void SetMaxItemsOnGrid(int maxItems)
    {
        maxItemsOnGrid = maxItems;
    }

    /// <summary>
    /// Check if the item limit warning should be shown
    /// </summary>
    /// <returns>True if warning should be shown</returns>
    public bool ShouldShowItemLimitWarning()
    {
        return showItemLimitWarning;
    }

    /// <summary>
    /// Set whether to show item limit warnings
    /// </summary>
    /// <param name="show">Whether to show warnings</param>
    public void SetShowItemLimitWarning(bool show)
    {
        showItemLimitWarning = show;
    }

    /// <summary>
    /// Count total items on the current grid
    /// </summary>
    /// <returns>Total number of items</returns>
    public int GetTotalItemsOnGrid()
    {
        GridData gridData = gridManager.GetCurrentGrid();
        if (gridData == null) return 0;

        int totalItems = 0;
        foreach (var cell in gridData.cells)
        {
            totalItems += cell.items.Count;
            totalItems += cell.waitingItems.Count;
        }
        return totalItems;
    }

    /// <summary>
    /// Check if the grid has reached its item limit
    /// </summary>
    /// <returns>True if at or over limit</returns>
    public bool IsAtItemLimit()
    {
        return GetTotalItemsOnGrid() >= maxItemsOnGrid;
    }
}