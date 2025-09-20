using UnityEngine;

/// <summary>
/// Handles conveyor machine behavior. Conveyors move items but don't transform them.
/// This class is dedicated to conveyor functionality, separate from blank cells.
/// </summary>
public class ConveyorMachine : BaseMachine
{
    public ConveyorMachine(CellData cellData, MachineDef machineDef) : base(cellData, machineDef)
    {
    }
    
    /// <summary>
    /// Update logic for conveyors - acts as failsafe to check for any Idle items and try to move them
    /// </summary>
    public override void UpdateLogic()
    {
        // Failsafe - check for any Idle items and try to move them
        for (int i = cellData.items.Count - 1; i >= 0; i--)
        {
            ItemData item = cellData.items[i];
            if (item.state == ItemState.Idle)
            {
                TryStartMove(item);
            }
        }
    }
    
    /// <summary>
    /// Handles items arriving at conveyors - immediately try to start movement
    /// </summary>
    public override void OnItemArrived(ItemData item)
    {
        Debug.Log($"Item {item.id} arrived at conveyor ({cellData.x}, {cellData.y})");
        
        // Immediately try to start movement of the arrived item
        TryStartMove(item);
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