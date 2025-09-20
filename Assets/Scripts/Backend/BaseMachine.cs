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
    /// This method is now concrete because the movement logic is generic.
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

        // Fix: Pass the individual properties of the item instead of the object itself.
        GameManager.Instance.activeGridManager.CreateVisualItem(item.id, item.x, item.y, item.itemType);
    }

    protected void GetNextCellCoordinates(out int nextX, out int nextY)
    {
        nextX = cellData.x;
        nextY = cellData.y;

        switch (cellData.direction)
        {
            case Direction.Up:
                nextY += 1;
                break;
            case Direction.Down:
                nextY -= 1;
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
}