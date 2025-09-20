using UnityEngine;

/// <summary>
/// Handles blank cell behavior. Blank cells don't process items,
/// they just hold them temporarily and destroy them after a timeout.
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
        // Blank cells should timeout and destroy items
        CheckItemTimeouts();
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
    /// Handles items arriving at blank cells - just accepts them without any special processing
    /// </summary>
    public override void OnItemArrived(ItemData item)
    {
        // Blank cells just accept items without processing or moving them
        // Items will timeout and be destroyed by UpdateLogic
        Debug.Log($"Item {item.id} arrived at blank cell ({cellData.x}, {cellData.y})");
    }
    
    /// <summary>
    /// Blank cells don't process items - they just hold them until timeout
    /// </summary>
    public override void ProcessItem(ItemData item)
    {
        // Blank cells don't process items, they just hold them until timeout
    }
}