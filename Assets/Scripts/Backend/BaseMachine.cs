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
    /// Adds an item to this machine's waiting queue
    /// </summary>
    /// <param name="item">The item to add to the queue</param>
    protected virtual void AddToWaitingQueue(ItemData item)
    {
        cellData.waitingItems.Add(item);
        item.state = ItemState.Waiting;
        item.waitingStartTime = Time.time;
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
    /// This method now handles two-phase movement: full cell -> halfway -> full cell.
    /// </summary>
    public void TryStartMove(ItemData item)
    {
        if (item.state != ItemState.Idle || item.x != cellData.x || item.y != cellData.y)
        {
            return;
        }

        int nextX, nextY;
        GetNextCellCoordinates(out nextX, out nextY);

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

        if (!item.isHalfway)
        {
            // Phase 1: Full Cell to Halfway Point
            // Item will move toward next cell but stop at 50% progress
            // isHalfway will be set to true when it reaches the halfway point
            Debug.Log($"Item {item.id} ({item.itemType}) started Phase 1 movement (to halfway) from ({cellData.x},{cellData.y}) toward ({nextX},{nextY})");
        }
        else
        {
            // Phase 2: Halfway Point to Full Cell  
            // Item continues from halfway point to the destination
            // isHalfway will be set to false when movement completes
            Debug.Log($"Item {item.id} ({item.itemType}) started Phase 2 movement (to full cell) from halfway to ({nextX},{nextY})");
        }

        // Create visual item if it doesn't exist yet
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
        nextX = cellData.x;
        nextY = cellData.y;

        switch (cellData.direction)
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
    /// Public helper method to get next cell coordinates for movement calculations
    /// </summary>
    public void GetNextCellCoordinatesPublic(out int nextX, out int nextY)
    {
        GetNextCellCoordinates(out nextX, out nextY);
    }
}