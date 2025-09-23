using UnityEngine;
using System.Collections.Generic;
using static UICell;

/// <summary>
/// Base class for all machine behavior in the factory automation game.
/// This class handles common machine functionality and provides virtual methods
/// for machine-specific behavior. Does NOT inherit from MonoBehaviour.
/// </summary>
public abstract class BaseMachine
{
    protected CellData cellData;
    protected MachineDef machineDef;

    public bool CanConfigure = false;

    /// <summary>
    /// Constructor that injects required data dependencies
    /// </summary>
    /// <param name="cellData">The cell data this machine operates on</param>
    /// <param name="machineDef">The machine definition from JSON</param>
    public BaseMachine(CellData cellData, MachineDef machineDef)
    {
        this.cellData = cellData;
        this.machineDef = machineDef;
    }

    /// <summary>
    /// Called when the machine is configured
    /// </summary>
    public virtual void OnConfigured()
    {
        if (!CanConfigure)
            return;
        // Default implementation: do nothing
        // Subclasses override for specific behavior
    }

    /// <summary>
    /// Called when an item arrives at this machine's cell
    /// </summary>
    /// <param name="item">The item that arrived</param>
    public virtual void OnItemArrived(ItemData item)
    {
        // Default implementation: do nothing
        // Subclasses override for specific behavior
    }

    /// <summary>
    /// Called to process an item at this machine
    /// </summary>
    /// <param name="item">The item to process</param>
    public virtual void ProcessItem(ItemData item)
    {
        // Default implementation: do nothing
        // Subclasses override for specific behavior
    }

    /// <summary>
    /// Called every frame to update machine logic (replaces switch statements)
    /// </summary>
    public virtual void UpdateLogic()
    {
        // Default implementation: do nothing
        // Subclasses override for specific behavior
    }

    /// <summary>
    /// Manages the waiting items queue for this machine
    /// </summary>
    /// <returns>Returns the next item to process, or null if none available</returns>
    protected virtual ItemData GetNextWaitingItem()
    {
        if (cellData.waitingItems.Count > 0)
        {
            ItemData nextItem = cellData.waitingItems[0];
            cellData.waitingItems.RemoveAt(0);
            return nextItem;
        }
        return null;
    }


    /// <summary>
    /// Gets the machine definition for this machine
    /// </summary>
    public MachineDef GetMachineDef()
    {
        return machineDef;
    }

    /// <summary>
    /// Gets the cell data this machine operates on
    /// </summary>
    public CellData GetCellData()
    {
        return cellData;
    }

    /// <summary>
    /// Attempts to start the movement of an item from this machine's cell.
    /// This method is now concrete because the movement logic is generic.
    /// </summary>
    /// <param name="item">The item to move</param>
    /// <param name="overrideDirection">Optional direction override. If null, uses machine's direction</param>
    public void TryStartMove(ItemData item, Direction? overrideDirection = null)
    {
        if (item.state != ItemState.Idle || item.x != cellData.x || item.y != cellData.y)
        {
            // Only allow items in the Idle state at their current cell to be moved.
            // This prevents an endless loop of movement starting.
            if (item.state == ItemState.Waiting) 
            {
                // This is the case where an item is being "pulled" from a waiting queue.
                // We'll allow it to move even if its coordinates are not at this cell,
                // because it's technically in a "halfway" position.
                // The GameManager's logic will handle this.
            }
            else
            {
                return;
            }
        }

        int nextX, nextY;
        // Use override direction if provided, otherwise use machine's direction
        Direction moveDirection = overrideDirection ?? cellData.direction;
        GetNextCellCoordinatesForDirection(moveDirection, out nextX, out nextY);

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
        
        string directionInfo = overrideDirection.HasValue ? $" (direction override: {moveDirection})" : "";

        // Fix: Pass the individual properties of the item instead of the object itself.
        if (!GameManager.Instance.activeGridManager.HasVisualItem(item.id))
        {
            GameManager.Instance.activeGridManager.CreateVisualItem(item.id, item.x, item.y, item.itemType);
        }
    }

    // The Y-coordinate needs to decrease to move "up" because the Unity UI coordinate system
    // has its origin (0,0) at the bottom-left of the canvas.
    // This means as Y increases, the position moves visually upwards on the screen.
    // This is the correct logic for moving items upwards in the UI. 
    protected void GetNextCellCoordinates(out int nextX, out int nextY)
    {
        GetNextCellCoordinatesForDirection(cellData.direction, out nextX, out nextY);
    }

    /// <summary>
    /// Gets the next cell coordinates for a specific direction
    /// </summary>
    /// <param name="direction">The direction to move in</param>
    /// <param name="nextX">Output X coordinate</param>
    /// <param name="nextY">Output Y coordinate</param>
    protected void GetNextCellCoordinatesForDirection(Direction direction, out int nextX, out int nextY)
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
    /// Rotates a direction by the specified number of 90-degree steps
    /// </summary>
    /// <param name="currentDirection">Current direction</param>
    /// <param name="steps">Number of 90-degree steps (positive = clockwise, negative = counter-clockwise)</param>
    /// <returns>The rotated direction</returns>
    protected Direction RotateDirection(Direction currentDirection, int steps)
    {
        int directionCount = 4; // Up, Right, Down, Left
        int currentIndex = (int)currentDirection;
        int newIndex = (currentIndex + steps + directionCount) % directionCount;
        return (Direction)newIndex;
    }
}
