using UnityEngine;
using static UICell;

/// <summary>
/// Handles conveyor machine behavior. Conveyors move items but don't transform them.
/// This class is dedicated to conveyor functionality, separate from blank cells.
/// </summary>
public class SortingMachine : BaseMachine
{
    /// <summary>
    /// Get the component ID for logging purposes
    /// </summary>
    protected string ComponentId => $"Sorting_{cellData.x}_{cellData.y}";
    
    public SortingMachine(CellData cellData, MachineDef machineDef) : base(cellData, machineDef)
    {
        // Sorting machines can be configured 
        CanConfigure = true;
    }

    public override void OnConfigured()
    {
        // Configuration logic for sorting machine

        // Find the sorting configuration UI in the scene
        SortingMachineConfigUI configUI = UnityEngine.Object.FindFirstObjectByType<SortingMachineConfigUI>(FindObjectsInactive.Include); //this returns null
        if (configUI != null)
        {
            configUI.ShowConfiguration(cellData, OnConfigurationConfirmed);
        }
        else
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Processor, "SortingMachineConfigUI not found in scene. Please add the UI component to configure sorting machines.", ComponentId);
            
            // Fallback: Set some default configuration for testing
            if (cellData.sortingConfig == null)
                cellData.sortingConfig = new SortingMachineConfig();
            
            cellData.sortingConfig.leftItemType = "can";
            cellData.sortingConfig.rightItemType = "shreddedAluminum";
        }
    }

    /// <summary>
    /// Called when the sorting configuration is confirmed by the user
    /// </summary>
    private void OnConfigurationConfirmed(string leftItemType, string rightItemType)
    {
        // Update the cell data configuration (this is already done by the UI, but let's be explicit)
        if (cellData.sortingConfig == null)
            cellData.sortingConfig = new SortingMachineConfig();
            
        cellData.sortingConfig.leftItemType = leftItemType;
        cellData.sortingConfig.rightItemType = rightItemType;
    }
    
    /// <summary>
    /// Update logic for sorting machines - acts as failsafe to check for any Idle items and try to move them with sorting logic
    /// </summary>
    public override void UpdateLogic()
    {
        // Failsafe - check for any Idle items and try to move them with sorting logic
        for (int i = cellData.items.Count - 1; i >= 0; i--)
        {
            ItemData item = cellData.items[i];
            if (item.state == ItemState.Idle)
            {
                // Use the enhanced TryStartMove with sorting direction
                Direction sortingDirection = GetSortingDirection(item);
                TryStartMove(item, sortingDirection);
            }
        }
    }
    
    /// <summary>
    /// Handles items arriving at sorting machines - immediately try to start movement with sorting logic
    /// </summary>
    public override void OnItemArrived(ItemData item)
    {
        // Use the enhanced TryStartMove with sorting direction
        Direction sortingDirection = GetSortingDirection(item);
        TryStartMove(item, sortingDirection);
    }

    /// <summary>
    /// Determines the direction an item should move based on sorting configuration
    /// </summary>
    private Direction GetSortingDirection(ItemData item)
    {
        Direction baseDirection = cellData.direction;
        
        // Check if we have sorting configuration
        if (cellData.sortingConfig == null)
        {
            return baseDirection;
        }

        // Check if item matches left configuration
        if (!string.IsNullOrEmpty(cellData.sortingConfig.leftItemType) && 
            item.itemType == cellData.sortingConfig.leftItemType)
        {
            Direction leftDirection = RotateDirection(baseDirection, -1); // -90 degrees (left turn)
            return leftDirection;
        }

        // Check if item matches right configuration
        if (!string.IsNullOrEmpty(cellData.sortingConfig.rightItemType) && 
            item.itemType == cellData.sortingConfig.rightItemType)
        {
            Direction rightDirection = RotateDirection(baseDirection, 1); // +90 degrees (right turn)
            return rightDirection;
        }

        // Item doesn't match any configuration, continue straight
        return baseDirection;
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