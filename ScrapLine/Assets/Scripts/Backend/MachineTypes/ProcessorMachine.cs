using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles processor machine behavior (like shredders). These machines take input items,
/// process them according to recipes, and output transformed items.
/// </summary>
public class ProcessorMachine : BaseMachine
{
    /// <summary>
    /// Get the component ID for logging purposes
    /// </summary>
    protected string ComponentId => $"Processor_{cellData.x}_{cellData.y}";
    
    public ProcessorMachine(CellData cellData, MachineDef machineDef) : base(cellData, machineDef)
    {
    }
    
    /// <summary>
    /// Adds an item to this machine's internal waiting queue.
    /// This method is called by the GameManager at the halfway point.
    /// </summary>
    public void AddToWaitingQueue(ItemData item)
    {
        if (!cellData.waitingItems.Contains(item))
        {
            // Assign stack index based on queue position
            item.stackIndex = cellData.waitingItems.Count;
            cellData.waitingItems.Add(item);
        }
    }
    
    /// <summary>
    /// Update logic for processor - handles the pull system for waiting items
    /// </summary>
    public override void UpdateLogic()
    {
        // Check for timed out waiting items first
        CheckWaitingItemTimeouts();
        
        // Implement pull system - if machine is idle and has waiting items, pull one
        if (cellData.machineState == MachineState.Idle && cellData.waitingItems.Count > 0)
        {
            // Find the first item in the queue that we can process
            ItemData waitingItem = GetNextProcessableWaitingItem();
            
            // If we found a processable item and it's waiting at the halfway point, pull it
            if (waitingItem != null && waitingItem.isHalfway)
            {
                // Give the item permission to start moving again.
                // The GameManager will handle the rest of the movement.
                waitingItem.state = ItemState.Moving;
                //waitingItem.moveStartTime = Time.time; // Restart the timer
                
                // Set machine state to processing to prevent it from pulling another item
                cellData.machineState = MachineState.Processing;
                
                // Note: We don't remove the item from waitingItems here because it will be removed
                // in OnItemArrived when it fully reaches the machine. This allows the visual
                // system to continue tracking it during the second half of movement.
            }
        }
        
        // Check if current processing is complete
        if (cellData.machineState == MachineState.Processing)
        {
            CheckProcessingComplete();
        }
    }
    
    /// <summary>
    /// Updates stack indices for all waiting items after a change in the queue
    /// </summary>
    protected void UpdateStackIndices()
    {
        for (int i = 0; i < cellData.waitingItems.Count; i++)
        {
            cellData.waitingItems[i].stackIndex = i;
        }
    }
    
    /// <summary>
    /// Updates visual positions for all waiting items to reflect their current stack positions
    /// </summary>
    protected void UpdateWaitingItemVisualPositions()
    {
        UIGridManager gridManager = UnityEngine.Object.FindAnyObjectByType<UIGridManager>();
        if (gridManager == null) return;
        
        foreach (var item in cellData.waitingItems)
        {
            if (item.state == ItemState.Waiting && item.isHalfway)
            {
                // Force a visual update for this waiting item with its current progress
                gridManager.UpdateItemVisualPosition(item.id, item.moveProgress,
                    item.sourceX, item.sourceY, item.targetX, item.targetY, 
                    UICell.Direction.Up); // Direction doesn't matter for waiting items
            }
        }
    }
    
    /// <summary>
    /// Checks for items that have been waiting too long and removes them
    /// </summary>
    protected void CheckWaitingItemTimeouts()
    {
        ItemMovementManager itemMovementManager = UnityEngine.Object.FindAnyObjectByType<ItemMovementManager>();
        if (itemMovementManager == null) return;
        
        float waitingTimeout = itemMovementManager.GetItemWaitingTimeout();
        bool anyItemRemoved = false;
        
        for (int i = cellData.waitingItems.Count - 1; i >= 0; i--)
        {
            ItemData item = cellData.waitingItems[i];
            
            if (item.state == ItemState.Waiting && item.waitingStartTime > 0)
            {
                float timeWaiting = Time.time - item.waitingStartTime;
                
                if (timeWaiting >= waitingTimeout)
                {
                    // Remove from waiting queue
                    cellData.waitingItems.RemoveAt(i);
                    anyItemRemoved = true;
                    
                    // Find and remove from source cell's items list
                    CellData sourceCell = UnityEngine.Object.FindAnyObjectByType<GridManager>()?.GetCellData(item.x, item.y);
                    if (sourceCell != null)
                    {
                        sourceCell.items.Remove(item);
                    }
                    
                    // Destroy visual item
                    UIGridManager gridManager = UnityEngine.Object.FindAnyObjectByType<UIGridManager>();
                    if (gridManager != null && gridManager.HasVisualItem(item.id))
                    {
                        gridManager.DestroyVisualItem(item.id);
                    }
                }
            }
        }
        
        // Update stack indices if any items were removed
        if (anyItemRemoved)
        {
            UpdateStackIndices();
            UpdateWaitingItemVisualPositions();
        }
    }
    
    /// <summary>
    /// Starts processing an item that was pulled from the waiting queue
    /// </summary>
    private void StartProcessing(ItemData item)
    {
        RecipeDef recipe = GetRecipeForItem(item.itemType);
        if (recipe != null)
        {
            // Move item from waiting to processing
            cellData.items.Add(item);
            
            item.state = ItemState.Processing;
            item.x = cellData.x;
            item.y = cellData.y;
            item.processingStartTime = Time.time;
            item.processingDuration = recipe.processTime;
            
            cellData.machineState = MachineState.Processing;
            
            // Destroy visual item when it enters processing
            UIGridManager gridManager = UnityEngine.Object.FindAnyObjectByType<UIGridManager>();
            if (gridManager != null)
            {
                gridManager.DestroyVisualItem(item.id);
            }
            
            cellData.machineState = MachineState.Processing;
        }
        else
        {
            GameLogger.LogError(LoggingManager.LogCategory.Processor, $"No recipe found for item {item.itemType} in processor {machineDef.id}", ComponentId);
        }
    }
    
    /// <summary>
    /// Checks if the current processing operation is complete
    /// </summary>
    protected void CheckProcessingComplete()
    {
        // Find the item being processed
        foreach (var item in cellData.items)
        {
            if (item.state == ItemState.Processing)
            {
                float processingElapsed = Time.time - item.processingStartTime;
                if (processingElapsed >= item.processingDuration)
                {
                    CompleteProcessing(item);
                    return;
                }
            }
        }
    }
    
    /// <summary>
    /// Completes the processing of an item, transforming it according to the recipe
    /// </summary>
    private void CompleteProcessing(ItemData item)
    {
        RecipeDef recipe = GetRecipeForItem(item.itemType);
        if (recipe != null)
        {
            // Remove input item
            cellData.items.Remove(item);
            
            UIGridManager gridManager = UnityEngine.Object.FindAnyObjectByType<UIGridManager>();
            if (gridManager != null)
            {
                gridManager.DestroyVisualItem(item.id);
            }
            
            // Create output items according to recipe
            foreach (var outputItem in recipe.outputItems)
            {
                for (int i = 0; i < outputItem.count; i++)
                {
                    // Create new output item
                    ItemData newItem = new ItemData
                    {
                        id = GameManager.Instance.GenerateItemId(),
                        itemType = outputItem.item,
                        x = cellData.x,
                        y = cellData.y,
                        state = ItemState.Idle,
                        moveProgress = 0f,
                        processingStartTime = 0f,
                        processingDuration = 0f,
                        waitingStartTime = 0f
                    };
                    
                    cellData.items.Add(newItem);
                    
                    // Create visual representation
                    if (gridManager != null)
                    {
                        gridManager.CreateVisualItem(newItem.id, cellData.x, cellData.y, newItem.itemType);
                    }
                    
                    // Immediately try to start movement of the newly created item
                    TryStartMove(newItem);
                }
            }
        }
        else
        {
            GameLogger.LogError(LoggingManager.LogCategory.Processor, $"Recipe not found when completing processing for machine {machineDef.id} with item {item.itemType}", ComponentId);
        }
        
        // Set machine state back to idle so it can accept new items
        cellData.machineState = MachineState.Idle;
    }
    
    /// <summary>
    /// Gets the next waiting item that can be processed by this machine (has a valid recipe)
    /// </summary>
    protected virtual ItemData GetNextProcessableWaitingItem()
    {
        // For basic processors, find the first item that has a recipe
        foreach (var item in cellData.waitingItems)
        {
            if (GetRecipeForItem(item.itemType) != null)
            {
                return item;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Gets the recipe for processing a specific item type in this machine
    /// </summary>
    private RecipeDef GetRecipeForItem(string itemType)
    {
        return FactoryRegistry.Instance.GetRecipe(machineDef.id, itemType);
    }
    
    /// <summary>
    /// Handles items arriving at this processor (adds them to waiting queue)
    /// </summary>
    public override void OnItemArrived(ItemData item)
    {
        // When an item fully arrives in this cell, it means it completed the second half of its move.
        // It is already in the waitingItems queue, so we just need to start processing it.
        
        // We find the item in the waiting queue and process it
        ItemData itemToProcess = cellData.waitingItems.Find(i => i.id == item.id);
        if (itemToProcess != null)
        {
            // Remove the item from waiting queue
            cellData.waitingItems.Remove(itemToProcess);
            
            // Update stack indices for remaining items
            UpdateStackIndices();
            
            // Update visual positions of all remaining waiting items
            UpdateWaitingItemVisualPositions();
            
            StartProcessing(itemToProcess);
        }
        else
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Processor, $"Could not find item {item.id} in waiting queue to process.", ComponentId);
        }
    }
    
    /// <summary>
    /// Processes an item immediately (used for direct processing calls)
    /// </summary>
    public override void ProcessItem(ItemData item)
    {
        // For processors, items should go through the waiting queue first
        AddToWaitingQueue(item);
    }
}
