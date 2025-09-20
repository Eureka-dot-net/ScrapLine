using UnityEngine;
using System.Collections.Generic;

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
}