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
        // Check for any Idle items and try to move them
        for (int i = cellData.items.Count - 1; i >= 0; i--)
        {
            ItemData item = cellData.items[i];
            if (item.state == ItemState.Idle)
            {
                // Try to start the appropriate movement phase
                TryStartMove(item);
            }
        }
    }
    
    /// <summary>
    /// Handles items arriving at conveyors - start Phase 1 movement for newly arrived items
    /// </summary>
    public override void OnItemArrived(ItemData item)
    {
        // When an item arrives at a conveyor (completes Phase 2), it should be isHalfway=false
        // Immediately try to start the next movement (Phase 1 to next halfway point)
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