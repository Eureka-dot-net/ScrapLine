using UnityEngine;

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
        // For example, set sorting direction or criteria
        Debug.Log($"SortingMachine at ({cellData.x}, {cellData.y}) configured.");
    }
    
    /// <summary>
    /// Update logic for sorting machines - acts as failsafe to check for any Idle items and try to move them
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