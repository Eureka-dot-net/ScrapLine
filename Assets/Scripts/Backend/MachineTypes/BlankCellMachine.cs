using UnityEngine;

/// <summary>
/// Handles blank cell and conveyor behavior. Blank cells don't process items,
/// they just hold them temporarily. Conveyors move items but don't transform them.
/// </summary>
public class BlankCellMachine : BaseMachine
{
    private float itemTimeoutDuration = 10f; // Items disappear after this time on blank cells
    
    public BlankCellMachine(CellData cellData, MachineDef machineDef) : base(cellData, machineDef)
    {
    }
    
    /// <summary>
    /// Update logic for blank cells - handles item timeout on blank cells
    /// </summary>
    public override void UpdateLogic()
    {
        // For true blank cells (not conveyors), items should timeout and disappear
        if (machineDef.id == "blank" || machineDef.id == "blank_top" || machineDef.id == "blank_bottom")
        {
            CheckItemTimeouts();
        }
        
        // Conveyors don't need special update logic - they just hold items
        // The movement logic is handled by GameManager
    }
    
    /// <summary>
    /// Checks for items that have been on blank cells too long and removes them
    /// </summary>
    private void CheckItemTimeouts()
    {
        UIGridManager gridManager = Object.FindAnyObjectByType<UIGridManager>();
        
        for (int i = cellData.items.Count - 1; i >= 0; i--)
        {
            ItemData item = cellData.items[i];
            
            // Check if item has been idle on this blank cell for too long
            if (item.state == ItemState.Idle)
            {
                // Use the time since the item was last moved or created
                float timeOnCell = Time.time - (item.moveStartTime > 0 ? item.moveStartTime + 1f : Time.time);
                
                if (timeOnCell >= itemTimeoutDuration)
                {
                    Debug.Log($"Item {item.id} timed out on blank cell at ({cellData.x}, {cellData.y}) - removing");
                    
                    cellData.items.RemoveAt(i);
                    
                    if (gridManager != null && gridManager.HasVisualItem(item.id))
                    {
                        gridManager.DestroyVisualItem(item.id);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Handles items arriving at blank cells or conveyors
    /// </summary>
    public override void OnItemArrived(ItemData item)
    {
        // Blank cells and conveyors just accept items without processing
        // The item's state should already be set to Idle by the movement system
        Debug.Log($"Item {item.id} arrived at {machineDef.type} cell ({cellData.x}, {cellData.y})");
    }
    
    /// <summary>
    /// Blank cells don't process items - they just hold them
    /// </summary>
    public override void ProcessItem(ItemData item)
    {
        // Blank cells don't process items, they just hold them
        // Items will be moved by the GameManager's movement system
    }
}