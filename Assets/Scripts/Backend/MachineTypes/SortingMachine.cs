using UnityEngine;
using static UICell;

/// <summary>
/// Handles conveyor machine behavior. Conveyors move items but don't transform them.
/// This class is dedicated to conveyor functionality, separate from blank cells.
/// </summary>
public class SortingMachine : BaseMachine
{
    public SortingMachine(CellData cellData, MachineDef machineDef) : base(cellData, machineDef)
    {
        // Sorting machines can be configured 
        CanConfigure = true;
    }

    public override void OnConfigured()
    {
        // Configuration logic for sorting machine
        Debug.Log($"SortingMachine at ({cellData.x}, {cellData.y}) configured.");

        // Find the sorting configuration UI in the scene
        SortingMachineConfigUI configUI = Object.FindFirstObjectByType<SortingMachineConfigUI>(FindObjectsInactive.Include); //this returns null
        if (configUI != null)
        {
            configUI.ShowConfiguration(cellData, OnConfigurationConfirmed);
        }
        else
        {
            Debug.LogWarning("SortingMachineConfigUI not found in scene. Please add the UI component to configure sorting machines.");
            
            // Fallback: Set some default configuration for testing
            if (cellData.sortingConfig == null)
                cellData.sortingConfig = new SortingMachineConfig();
            
            cellData.sortingConfig.leftItemType = "can";
            cellData.sortingConfig.rightItemType = "shreddedAluminum";
            Debug.Log($"Applied default sorting configuration: Left=can, Right=shreddedAluminum");
        }
    }

    /// <summary>
    /// Called when the sorting configuration is confirmed by the user
    /// </summary>
    private void OnConfigurationConfirmed(string leftItemType, string rightItemType)
    {
        Debug.Log($"Sorting machine configured: Left={leftItemType}, Right={rightItemType}");
        
        // Update the cell data configuration (this is already done by the UI, but let's be explicit)
        if (cellData.sortingConfig == null)
            cellData.sortingConfig = new SortingMachineConfig();
            
        cellData.sortingConfig.leftItemType = leftItemType;
        cellData.sortingConfig.rightItemType = rightItemType;
        
        Debug.Log($"Sorting logic applied: Left={leftItemType} will turn left, Right={rightItemType} will turn right, others go straight");
    }
    
    /// <summary>
    /// Update logic for sorting machines - acts as failsafe to check for any Idle items and try to move them with sorting logic
    /// </summary>
    public override void UpdateLogic()
    {
        // Failsafe - check for any Idle items and try to move them with sorting logic
        for (int i = cellData.items.Count - 1; i >= 0; i--)
        {
            ItemData item = cellData.items[i];
            if (item.state == ItemState.Idle)
            {
                TryStartMoveWithSorting(item);
            }
        }
    }
    
    /// <summary>
    /// Handles items arriving at sorting machines - immediately try to start movement with sorting logic
    /// </summary>
    public override void OnItemArrived(ItemData item)
    {
        // Immediately try to start movement of the arrived item with sorting logic
        TryStartMoveWithSorting(item);
    }

    /// <summary>
    /// Attempts to start the movement of an item from this sorting machine with directional sorting logic.
    /// Items are sorted based on the configured left/right item types.
    /// </summary>
    private void TryStartMoveWithSorting(ItemData item)
    {
        if (item.state != ItemState.Idle || item.x != cellData.x || item.y != cellData.y)
        {
            // Only allow items in the Idle state at their current cell to be moved.
            if (item.state == ItemState.Waiting) 
            {
                // Allow items being "pulled" from a waiting queue
            }
            else
            {
                return;
            }
        }

        // Determine the direction based on sorting configuration
        Direction sortingDirection = GetSortingDirection(item);
        
        int nextX, nextY;
        GetNextCellCoordinatesForDirection(sortingDirection, out nextX, out nextY);

        if (nextX == -1 || nextY == -1)
        {
            return;
        }

        item.state = ItemState.Moving;
        item.sourceX = cellData.x;
        item.sourceY = cellData.y;
        item.targetX = nextX;
        item.targetY = nextY;
        item.moveStartTime = Time.time;
        
        Debug.Log($"SortingMachine: Item {item.id} ({item.itemType}) started moving from ({cellData.x},{cellData.y}) to ({nextX},{nextY}) in direction {sortingDirection}");

        // Create visual item if it doesn't exist
        if (!GameManager.Instance.activeGridManager.HasVisualItem(item.id))
        {
            GameManager.Instance.activeGridManager.CreateVisualItem(item.id, item.x, item.y, item.itemType);
        }
    }

    /// <summary>
    /// Determines the direction an item should move based on sorting configuration
    /// </summary>
    private Direction GetSortingDirection(ItemData item)
    {
        Direction baseDirection = cellData.direction;
        
        // Check if we have sorting configuration
        if (cellData.sortingConfig == null)
        {
            Debug.Log($"SortingMachine: No sorting config found, item {item.itemType} going straight");
            return baseDirection;
        }

        // Check if item matches left configuration
        if (!string.IsNullOrEmpty(cellData.sortingConfig.leftItemType) && 
            item.itemType == cellData.sortingConfig.leftItemType)
        {
            Direction leftDirection = RotateDirection(baseDirection, -1); // -90 degrees (left turn)
            Debug.Log($"SortingMachine: Item {item.itemType} matches left config, turning left to {leftDirection}");
            return leftDirection;
        }

        // Check if item matches right configuration
        if (!string.IsNullOrEmpty(cellData.sortingConfig.rightItemType) && 
            item.itemType == cellData.sortingConfig.rightItemType)
        {
            Direction rightDirection = RotateDirection(baseDirection, 1); // +90 degrees (right turn)
            Debug.Log($"SortingMachine: Item {item.itemType} matches right config, turning right to {rightDirection}");
            return rightDirection;
        }

        // Item doesn't match any configuration, continue straight
        Debug.Log($"SortingMachine: Item {item.itemType} doesn't match config, going straight");
        return baseDirection;
    }

    /// <summary>
    /// Rotates a direction by the specified number of 90-degree steps
    /// </summary>
    /// <param name="currentDirection">Current direction</param>
    /// <param name="steps">Number of 90-degree steps (positive = clockwise, negative = counter-clockwise)</param>
    private Direction RotateDirection(Direction currentDirection, int steps)
    {
        int directionCount = 4; // Up, Right, Down, Left
        int currentIndex = (int)currentDirection;
        int newIndex = (currentIndex + steps + directionCount) % directionCount;
        return (Direction)newIndex;
    }

    /// <summary>
    /// Gets the next cell coordinates for a specific direction
    /// </summary>
    private void GetNextCellCoordinatesForDirection(Direction direction, out int nextX, out int nextY)
    {
        nextX = cellData.x;
        nextY = cellData.y;

        switch (direction)
        {
            case Direction.Up:
                nextY -= 1;
                break;
            case Direction.Down:
                nextY += 1;
                break;
            case Direction.Left:
                nextX -= 1;
                break;
            case Direction.Right:
                nextX += 1;
                break;
        }

        var grid = GameManager.Instance.GetCurrentGrid();
        if (nextX < 0 || nextX >= grid.width || nextY < 0 || nextY >= grid.height)
        {
            nextX = -1;
            nextY = -1;
        }
    }
    
    /// <summary>
    /// Conveyors don't process items - they just move them
    /// </summary>
    public override void ProcessItem(ItemData item)
    {
        // Conveyors don't process items, they just move them
        // Movement is handled by OnItemArrived and UpdateLogic
    }
}