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
    /// Handles items arriving at conveyors - check isHalfway status and start appropriate movement phase
    /// </summary>
    public override void OnItemArrived(ItemData item)
    {
        // Check item.isHalfway to determine which phase to start
        if (!item.isHalfway)
        {
            // Item just came from another machine, start Phase 1 (to halfway point)
            TryStartMove(item);
        }
        else
        {
            // Item is at halfway point, start Phase 2 (to next full cell)
            TryStartMove(item);
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