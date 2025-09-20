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
    /// Update logic for processor - handles the pull system for waiting items
    /// </summary>
    public override void UpdateLogic()
    {
        // Implement pull system - if machine is idle and has waiting items, start processing
        if (cellData.machineState == MachineState.Idle && cellData.waitingItems.Count > 0)
        {
            ItemData waitingItem = GetNextWaitingItem();
            if (waitingItem != null)
            {
                StartProcessing(waitingItem);
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
            
            Debug.Log($"Pulled item {item.id} into processing at ({cellData.x}, {cellData.y}) for {item.processingDuration}s");
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
        Debug.Log($"Completing processing for item {item.id} ({item.itemType}) after {item.processingDuration}s");
        
        RecipeDef recipe = GetRecipeForItem(item.itemType);
        if (recipe != null)
        {
            // Remove input item
            Debug.Log($"Removing input item {item.id} ({item.itemType})");
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
                        waitingStartTime = 0f,
                        targetMoveProgress = 0f
                    };
                    
                    cellData.items.Add(newItem);
                    
                    // Create visual representation
                    if (gridManager != null)
                    {
                        gridManager.CreateVisualItem(newItem.id, cellData.x, cellData.y, newItem.itemType);
                    }
                    
                    Debug.Log($"Created output item {newItem.id} ({outputItem.item}) at ({cellData.x}, {cellData.y})");
                    
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
        // Check if we have a recipe for this item
        RecipeDef recipe = GetRecipeForItem(item.itemType);
        if (recipe != null)
        {
            AddToWaitingQueue(item);
            Debug.Log($"Item {item.id} added to waiting queue for processor at ({cellData.x}, {cellData.y})");
        }
        else
        {
            Debug.LogWarning($"No recipe found for item {item.itemType} in processor {machineDef.id}");
        }
    }
    
    /// <summary>
    /// Processes an item immediately (used for direct processing calls)
    /// </summary>
    public override void ProcessItem(ItemData item)
    {
        // For processors, items should go through the waiting queue first
        OnItemArrived(item);
    }
}