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
        Debug.Log($"SortingMachine at ({cellData.x}, {cellData.y}) configured.");

        // Find the sorting configuration UI in the scene
        SortingMachineConfigUI configUI = FindAnyObjectByType<SortingMachineConfigUI>();
        if (configUI != null)
        {
            configUI.ShowConfiguration(cellData, OnConfigurationConfirmed);
        }
        else
        {
            Debug.LogWarning("SortingMachineConfigUI not found in scene. Please add the UI component to configure sorting machines.");
            
            // Fallback: Set some default configuration for testing
            if (cellData.sortingConfig == null)
                cellData.sortingConfig = new SortingMachineConfig();
            
            cellData.sortingConfig.leftItemType = "can";
            cellData.sortingConfig.rightItemType = "shreddedAluminum";
            Debug.Log($"Applied default sorting configuration: Left=can, Right=shreddedAluminum");
        }
    }

    /// <summary>
    /// Called when the sorting configuration is confirmed by the user
    /// </summary>
    private void OnConfigurationConfirmed(string leftItemType, string rightItemType)
    {
        Debug.Log($"Sorting machine configured: Left={leftItemType}, Right={rightItemType}");
        
        // Update the cell data configuration (this is already done by the UI, but let's be explicit)
        if (cellData.sortingConfig == null)
            cellData.sortingConfig = new SortingMachineConfig();
            
        cellData.sortingConfig.leftItemType = leftItemType;
        cellData.sortingConfig.rightItemType = rightItemType;
        
        // TODO: Implement sorting logic based on the configuration
        // This would be used in the item movement logic to determine which direction items should go
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