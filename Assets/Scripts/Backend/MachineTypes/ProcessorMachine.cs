using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles processor machine behavior (like shredders). These machines take input items,
/// process them according to recipes, and output transformed items.
/// </summary>
public class ProcessorMachine : BaseMachine
{
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
            cellData.waitingItems.Add(item);
            Debug.Log($"Item {item.id} has been added to the waiting queue of processor {machineDef.id}. Queue size: {cellData.waitingItems.Count}");
        }
    }
    
    /// <summary>
    /// Update logic for processor - handles the pull system for waiting items
    /// </summary>
    public override void UpdateLogic()
    {
        // Implement pull system - if machine is idle and has waiting items, pull one
        if (cellData.machineState == MachineState.Idle && cellData.waitingItems.Count > 0)
        {
            // Get the next item from the queue
            ItemData waitingItem = cellData.waitingItems[0];
            
            // If the item is waiting at the halfway point, the processor "pulls" it.
            if (waitingItem.isHalfway)
            {
                // Give the item permission to start moving again.
                // The GameManager will handle the rest of the movement.
                waitingItem.state = ItemState.Moving;
                //waitingItem.moveStartTime = Time.time; // Restart the timer
                
                // Set machine state to processing to prevent it from pulling another item
                cellData.machineState = MachineState.Processing; 

                Debug.Log($"Processor {machineDef.id} is pulling in item {waitingItem.id} from the border.");
            }
        }
        
        // Check if current processing is complete
        if (cellData.machineState == MachineState.Processing)
        {
            CheckProcessingComplete();
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
            UIGridManager gridManager = Object.FindAnyObjectByType<UIGridManager>();
            if (gridManager != null)
            {
                gridManager.DestroyVisualItem(item.id);
            }
            
            Debug.Log($"Started processing item {item.id} ({item.itemType}) → {recipe.outputItems[0].item} in {item.processingDuration}s");
        }
        else
        {
            Debug.LogError($"No recipe found for item {item.itemType} in processor {machineDef.id}");
        }
    }
    
    /// <summary>
    /// Checks if the current processing operation is complete
    /// </summary>
    private void CheckProcessingComplete()
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
        Debug.Log($"Completed processing item {item.id} ({item.itemType}) after {item.processingDuration}s");
        
        RecipeDef recipe = GetRecipeForItem(item.itemType);
        if (recipe != null)
        {
            // Remove input item
            cellData.items.Remove(item);
            
            UIGridManager gridManager = Object.FindAnyObjectByType<UIGridManager>();
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
                    
                    Debug.Log($"Created output item {newItem.id} ({outputItem.item}) from {item.itemType}");
                    
                    // Immediately try to start movement of the newly created item
                    TryStartMove(newItem);
                }
            }
        }
        else
        {
            Debug.LogError($"Recipe not found when completing processing for machine {machineDef.id} with item {item.itemType}");
        }
        
        // Set machine state back to idle so it can accept new items
        cellData.machineState = MachineState.Idle;
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
        Debug.Log($"Item {item.id} has fully arrived at processor {machineDef.id}. Starting processing.");
        
        // We find the item in the waiting queue and process it
        ItemData itemToProcess = cellData.waitingItems.Find(i => i.id == item.id);
        if (itemToProcess != null)
        {
            StartProcessing(itemToProcess);
        }
        else
        {
            Debug.LogWarning($"Could not find item {item.id} in waiting queue to process.");
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
