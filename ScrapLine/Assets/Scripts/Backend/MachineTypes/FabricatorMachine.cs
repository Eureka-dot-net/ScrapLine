using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// Handles fabricator machine behavior. These machines take input items,
/// process them according to a user-selected recipe, and output transformed items.
/// Unlike processor machines that can handle any compatible recipe, fabricators
/// require the user to specify exactly which recipe to use.
/// </summary>
public class FabricatorMachine : ProcessorMachine
{
    /// <summary>
    /// Get the component ID for logging purposes
    /// </summary>
    protected new string ComponentId => $"Fabricator_{cellData.x}_{cellData.y}";
    
    public FabricatorMachine(CellData cellData, MachineDef machineDef) : base(cellData, machineDef)
    {
        // Fabricator machines can be configured to select specific recipes
        CanConfigure = true;
    }

    /// <summary>
    /// Called when the fabricator machine is configured by the player
    /// </summary>
    public override void OnConfigured()
    {
        // Find the fabricator configuration UI in the scene
        FabricatorMachineConfigUI configUI = UnityEngine.Object.FindFirstObjectByType<FabricatorMachineConfigUI>(FindObjectsInactive.Include);
        if (configUI != null)
        {
            configUI.ShowConfiguration(cellData, OnConfigurationConfirmed);
        }
        else
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Fabricator, "FabricatorMachineConfigUI not found in scene. Please add the UI component to configure fabricator machines.", ComponentId);
            
            // Fallback: Set a default configuration for testing if there are any fabricator recipes
            var fabricatorRecipes = FactoryRegistry.Instance.GetRecipesForMachine(cellData.machineDefId);
            if (fabricatorRecipes.Count > 0)
            {
                var defaultRecipe = fabricatorRecipes[0];
                cellData.selectedRecipeId = GetRecipeId(defaultRecipe);
                GameLogger.LogFabricator($"[FABRICATOR] Applied default recipe for testing: {defaultRecipe.outputItems[0].item}", ComponentId);
            }
        }
    }

    /// <summary>
    /// Called when the fabricator configuration is confirmed by the user
    /// </summary>
    private void OnConfigurationConfirmed(string selectedRecipeId)
    {
        GameLogger.LogFabricator($"[FABRICATOR] Recipe selected: {selectedRecipeId}", ComponentId);
        
        // The configuration UI already updates cellData.selectedRecipeId, but let's be explicit
        cellData.selectedRecipeId = selectedRecipeId;
        
        if (string.IsNullOrEmpty(selectedRecipeId))
        {
            GameLogger.LogFabricator("[FABRICATOR] Recipe cleared - machine will not process items", ComponentId);
        }
        else
        {
            RecipeDef recipe = GetSelectedRecipe();
            if (recipe != null && recipe.outputItems.Count > 0)
            {
                string inputsDescription = string.Join(", ", recipe.inputItems.Select(i => $"{i.count}x {i.item}"));
                GameLogger.LogFabricator($"Will craft: {recipe.outputItems[0].item} (inputs: {inputsDescription})", ComponentId);
            }
        }
    }

    /// <summary>
    /// Override to get the next item needed for the specific selected recipe
    /// </summary>
    protected override ItemData GetNextProcessableWaitingItem()
    {
        // Check if a recipe is selected
        if (string.IsNullOrEmpty(cellData.selectedRecipeId))
        {
            return null; // Silently return null - we don't want to spam warnings
        }

        // Get the selected recipe
        RecipeDef selectedRecipe = GetSelectedRecipe();
        if (selectedRecipe == null)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Fabricator, $"[FABRICATOR] Invalid recipe selected: {cellData.selectedRecipeId}", ComponentId);
            return null;
        }

        // Check what items we still need for this recipe
        var neededItems = GetNeededItemsForRecipe(selectedRecipe);
        if (neededItems.Count == 0)
        {
            GameLogger.LogFabricator($"[FABRICATOR] All items collected for recipe, ready to process", ComponentId);
            return null; // We have everything we need
        }

        // Find the first waiting item that we need for this recipe
        foreach (var waitingItem in cellData.waitingItems)
        {
            if (neededItems.ContainsKey(waitingItem.itemType) && neededItems[waitingItem.itemType] > 0)
            {
                GameLogger.LogFabricator($"[FABRICATOR] Found needed item: {waitingItem.itemType}", ComponentId);
                return waitingItem;
            }
        }

        return null; // No needed items are waiting
    }

    /// <summary>
    /// Override UpdateLogic to handle multi-input recipes
    /// Fabricators need to pull multiple items before they can start processing
    /// </summary>
    public override void UpdateLogic()
    {
        // Check for timed out waiting items first
        CheckWaitingItemTimeouts();
        
        // If we're processing, check if complete
        if (cellData.machineState == MachineState.Processing)
        {
            CheckProcessingComplete();
            return;
        }
        
        // If not idle, don't pull items
        if (cellData.machineState != MachineState.Idle || cellData.waitingItems.Count == 0)
            return;
            
        // Check if we have a recipe selected
        if (string.IsNullOrEmpty(cellData.selectedRecipeId))
            return;
            
        RecipeDef selectedRecipe = GetSelectedRecipe();
        if (selectedRecipe == null)
            return;
            
        // Check what items we still need
        var neededItems = GetNeededItemsForRecipe(selectedRecipe);
        if (neededItems.Count == 0)
        {
            // We have all items - check if ready to process (but only if machine is Idle)
            if (cellData.machineState == MachineState.Idle)
            {
                CheckIfReadyToProcess();
            }
            return;
        }
        
        // Pull items one by one until we have everything we need
        // Keep pulling as long as we need items and have waiting items
        while (neededItems.Count > 0 && cellData.waitingItems.Count > 0)
        {
            ItemData waitingItem = GetNextProcessableWaitingItem();
            
            if (waitingItem != null && waitingItem.isHalfway)
            {
                // Give the item permission to start moving again
                waitingItem.state = ItemState.Moving;
                
                // Temporarily set to receiving to prevent other items from being pulled immediately
                cellData.machineState = MachineState.Receiving;
                
                GameLogger.LogFabricator($"[FABRICATOR] Pulling {waitingItem.itemType}", ComponentId);
                
                // Break after pulling one item to avoid pulling all at once
                // The machine state will reset to Idle when the item arrives
                break;
            }
            else
            {
                // No more processable items available
                break;
            }
        }
    }

    /// <summary>
    /// Get the recipe definition from the selected recipe ID
    /// </summary>
    private RecipeDef GetSelectedRecipe()
    {
        if (string.IsNullOrEmpty(cellData.selectedRecipeId)) return null;
        
        foreach (var recipe in FactoryRegistry.Instance.Recipes)
        {
            if (GetRecipeId(recipe) == cellData.selectedRecipeId)
            {
                return recipe;
            }
        }
        
        GameLogger.LogWarning(LoggingManager.LogCategory.Fabricator, $"[FABRICATOR] Could not find recipe with ID: {cellData.selectedRecipeId}", ComponentId);
        return null;
    }

    /// <summary>
    /// Generate a unique ID for a recipe based on its inputs and outputs
    /// </summary>
    private string GetRecipeId(RecipeDef recipe)
    {
        string inputs = string.Join(",", recipe.inputItems.ConvertAll(i => i.item + ":" + i.count));
        string outputs = string.Join(",", recipe.outputItems.ConvertAll(o => o.item + ":" + o.count));
        return $"{recipe.machineId}_{inputs}_{outputs}";
    }

    /// <summary>
    /// Get a dictionary of items still needed for the selected recipe
    /// </summary>
    private Dictionary<string, int> GetNeededItemsForRecipe(RecipeDef recipe)
    {
        var needed = new Dictionary<string, int>();
        
        // Start with required items from recipe
        foreach (var input in recipe.inputItems)
        {
            needed[input.item] = input.count;
        }
        
        string recipeRequirements = string.Join(", ", needed.Select(kvp => $"{kvp.Value}x {kvp.Key}"));
        GameLogger.LogFabricator($"Recipe requires: {recipeRequirements}", ComponentId);
        
        string currentInventory = string.Join(", ", cellData.items.Select(i => $"{i.itemType}({i.state})"));
        GameLogger.LogFabricator($"Current inventory: [{currentInventory}]", ComponentId);
        
        // Count available items by type (excluding processing items)
        var availableItems = new Dictionary<string, int>();
        foreach (var item in cellData.items)
        {
            // Skip processing items - they don't count as inventory
            if (item.state == ItemState.Processing) 
            {
                GameLogger.LogFabricator($"[FABRICATOR] Skipping processing item: {item.itemType}", ComponentId);
                continue;
            }
            
            if (!availableItems.ContainsKey(item.itemType))
                availableItems[item.itemType] = 0;
            availableItems[item.itemType]++;
        }
        
        // GameLogger.LogFabricator(" Available items: [{string.Join(", ", availableItems.Select(kvp => $"{kvp.Value}x {kvp.Key}"))}]", ComponentId); // TODO: Fix complex string interpolation
        
        // Subtract available items from needed items
        foreach (var kvp in availableItems)
        {
            string itemType = kvp.Key;
            int availableCount = kvp.Value;
            
            if (needed.ContainsKey(itemType))
            {
                int neededCount = needed[itemType];
                int usedCount = Math.Min(neededCount, availableCount);
                needed[itemType] -= usedCount;
                
                GameLogger.LogFabricator($"[FABRICATOR] Using {usedCount}x {itemType} (have {availableCount}, need {neededCount}, remaining need: {needed[itemType]})", ComponentId);
                
                if (needed[itemType] <= 0)
                {
                    needed.Remove(itemType);
                }
                
                if (availableCount > neededCount)
                {
                    GameLogger.LogFabricator($"[FABRICATOR] Extra {itemType} in inventory: {availableCount - neededCount}x not needed for recipe", ComponentId);
                }
            }
            else
            {
                GameLogger.LogFabricator($"[FABRICATOR] Extra item type in inventory (not needed for recipe): {availableCount}x {itemType}", ComponentId);
            }
        }
        
        // GameLogger.LogFabricator(" Still need: [{string.Join(", ", needed.Select(kvp => $"{kvp.Value}x {kvp.Key}"))}]", ComponentId); // TODO: Fix complex string interpolation
        return needed;
    }

    /// <summary>
    /// Override to check if we have all required items for the selected recipe before starting processing
    /// </summary>
    public override void OnItemArrived(ItemData item)
    {
        GameLogger.LogFabricator($"[FABRICATOR] Item {item.itemType} (id: {item.id}) arrived", ComponentId);
        
        // Find and remove the item from waiting queue
        ItemData itemToProcess = cellData.waitingItems.Find(i => i.id == item.id);
        if (itemToProcess != null)
        {
            cellData.waitingItems.Remove(itemToProcess);
            UpdateStackIndices();
            UpdateWaitingItemVisualPositions();
            
            // NOTE: ItemMovementManager already added the item to cellData.items
            // DO NOT add it again here or we'll get duplicates!
            GameLogger.LogFabricator($"[FABRICATOR] ✓ Item {item.itemType} added to inventory (total items: {cellData.items.Count})", ComponentId);
            
            // Destroy visual item since it's now in the machine
            UIGridManager gridManager = UnityEngine.Object.FindAnyObjectByType<UIGridManager>();
            if (gridManager != null)
            {
                gridManager.DestroyVisualItem(itemToProcess.id);
            }
            
            // Reset state to Idle so we can pull more items if needed
            cellData.machineState = MachineState.Idle;
            
            // Check if we now have all items needed for the recipe (but only if machine is Idle)
            if (cellData.machineState == MachineState.Idle)
            {
                CheckIfReadyToProcess();
            }
        }
        else
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Fabricator, $"[FABRICATOR] ❌ Could not find item {item.id} in waiting queue", ComponentId);
        }
    }

    /// <summary>
    /// Check if we have all items needed for the selected recipe and start processing if we do
    /// </summary>
    private void CheckIfReadyToProcess()
    {
        GameLogger.LogFabricator($"[FABRICATOR] === CHECKING READINESS ===", ComponentId);
        GameLogger.LogFabricator($"[FABRICATOR] Machine state: {cellData.machineState}", ComponentId);
        // GameLogger.LogFabricator(" Current inventory: [{string.Join(", ", cellData.items.Select(i => $"{i.itemType}({i.state}) id:{i.id}"))}]", ComponentId); // TODO: Fix complex string interpolation
        // GameLogger.LogFabricator(" Waiting queue: [{string.Join(", ", cellData.waitingItems.Select(i => $"{i.itemType} id:{i.id}"))}]", ComponentId); // TODO: Fix complex string interpolation
        
        // CRITICAL: Double check machine state to prevent race conditions
        if (cellData.machineState != MachineState.Idle) 
        {
            GameLogger.LogFabricator($"[FABRICATOR] ❌ BLOCKED - machine state is {cellData.machineState}, not Idle", ComponentId);
            GameLogger.LogFabricator($"[FABRICATOR] === READINESS CHECK COMPLETE - MACHINE BUSY ===", ComponentId);
            return;
        }
        
        RecipeDef selectedRecipe = GetSelectedRecipe();
        if (selectedRecipe == null) 
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Fabricator, $"[FABRICATOR] ❌ BLOCKED - No recipe selected", ComponentId);
            GameLogger.LogFabricator($"[FABRICATOR] === READINESS CHECK COMPLETE - NO RECIPE ===", ComponentId);
            return;
        }
        
        // GameLogger.LogFabricator(" Recipe requirements: {string.Join(", ", selectedRecipe.inputItems.Select(i => $"{i.count}x {i.item}"))} → {string.Join(", ", selectedRecipe.outputItems.Select(o => $"{o.count}x {o.item}"))}", ComponentId); // TODO: Fix complex string interpolation
        
        // CRITICAL: Validate ingredients one more time right before processing
        var neededItems = GetNeededItemsForRecipe(selectedRecipe);
        GameLogger.LogFabricator($"[FABRICATOR] Ingredient validation result: need {neededItems.Count} more item types", ComponentId);
        
        if (neededItems.Count == 0)
        {
            // CRITICAL: Set machine state IMMEDIATELY to prevent multiple processing starts
            GameLogger.LogFabricator($"[FABRICATOR] ✓ All ingredients validated - SETTING MACHINE TO RECEIVING to prevent double processing", ComponentId);
            cellData.machineState = MachineState.Receiving;
            
            GameLogger.LogFabricator($"[FABRICATOR] === READINESS CHECK COMPLETE - STARTING PROCESSING ===", ComponentId);
            // We have all items needed - start processing
            StartFabricatorProcessing(selectedRecipe);
        }
        else
        {
            // GameLogger.LogFabricator(" ✗ Not ready to process - still missing: {string.Join(", ", neededItems.Select(kvp => $"{kvp.Value}x {kvp.Key}"))}", ComponentId); // TODO: Fix complex string interpolation
            GameLogger.LogFabricator($"[FABRICATOR] === READINESS CHECK COMPLETE - NOT READY ===", ComponentId);
        }
    }

    /// <summary>
    /// Override ProcessorMachine's CheckProcessingComplete to handle fabricator-specific completion
    /// </summary>
    protected new void CheckProcessingComplete()
    {
        // Find the item being processed
        foreach (var item in cellData.items)
        {
            if (item.state == ItemState.Processing)
            {
                float processingElapsed = Time.time - item.processingStartTime;
                if (processingElapsed >= item.processingDuration)
                {
                    GameLogger.LogFabricator($"[FABRICATOR] Processing complete for {item.itemType}", ComponentId);
                    CompleteFabricatorProcessing(item);
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Complete fabricator processing and prepare for next recipe cycle
    /// </summary>
    private void CompleteFabricatorProcessing(ItemData processingItem)
    {
        GameLogger.LogFabricator($"[FABRICATOR] === COMPLETION STARTED ===", ComponentId);
        // GameLogger.LogFabricator(" Inventory before completion: [{string.Join(", ", cellData.items.Select(i => $"{i.itemType}({i.state})"))}]", ComponentId); // TODO: Fix complex string interpolation
        
        // Remove the processing item
        cellData.items.Remove(processingItem);
        GameLogger.LogFabricator($"[FABRICATOR] Removed processing item: {processingItem.itemType} (id: {processingItem.id})", ComponentId);
        
        // Create output items
        RecipeDef selectedRecipe = GetSelectedRecipe();
        if (selectedRecipe != null)
        {
            foreach (var outputItem in selectedRecipe.outputItems)
            {
                for (int i = 0; i < outputItem.count; i++)
                {
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
                    UIGridManager gridManager = UnityEngine.Object.FindAnyObjectByType<UIGridManager>();
                    if (gridManager != null)
                    {
                        gridManager.CreateVisualItem(newItem.id, cellData.x, cellData.y, newItem.itemType);
                    }
                    
                    GameLogger.LogFabricator($"[FABRICATOR] Created output: {newItem.itemType} (id: {newItem.id})", ComponentId);
                    
                    // Try to start movement of the newly created item
                    TryStartMove(newItem);
                }
            }
        }
        
        // Reset machine state to Idle so it can start the next recipe cycle
        cellData.machineState = MachineState.Idle;
        // GameLogger.LogFabricator(" Inventory after completion: [{string.Join(", ", cellData.items.Select(i => $"{i.itemType}({i.state})"))}]", ComponentId); // TODO: Fix complex string interpolation
        // GameLogger.LogFabricator(" Waiting queue: [{string.Join(", ", cellData.waitingItems.Select(i => i.itemType))}]", ComponentId); // TODO: Fix complex string interpolation
        GameLogger.LogFabricator($"[FABRICATOR] === COMPLETION FINISHED - READY FOR NEXT CYCLE ===", ComponentId);
    }

    /// <summary>
    /// Start processing with all required items for the recipe
    /// </summary>
    private void StartFabricatorProcessing(RecipeDef recipe)
    {
        GameLogger.LogFabricator($"[FABRICATOR] === STARTING PROCESSING ===", ComponentId);
        GameLogger.LogFabricator($"[FABRICATOR] Starting processing for recipe: {recipe.outputItems[0].item}", ComponentId);
        GameLogger.LogFabricator($"[FABRICATOR] Machine state at start: {cellData.machineState}", ComponentId);
        
        // CRITICAL: Final state check to prevent race conditions
        if (cellData.machineState == MachineState.Processing)
        {
            GameLogger.LogError(LoggingManager.LogCategory.Fabricator, $"[FABRICATOR] CRITICAL ERROR: Already processing! Aborting to prevent duplicate processing.", ComponentId);
            return;
        }
        
        // Log current inventory before consumption
        // GameLogger.LogFabricator(" Inventory before consumption: [{string.Join(", ", cellData.items.Select(i => $"{i.itemType}({i.state}) id:{i.id}"))}]", ComponentId); // TODO: Fix complex string interpolation
        
        // CRITICAL: Final ingredient validation before consumption
        var finalValidation = GetNeededItemsForRecipe(recipe);
        if (finalValidation.Count > 0)
        {
            // GameLogger.LogError(LoggingManager.LogCategory.Fabricator, " CRITICAL ERROR: Ingredients missing at processing start! Missing: {string.Join(", ", finalValidation.Select(kvp => $"{kvp.Value}x {kvp.Key}"))}", ComponentId); // TODO: Fix complex string interpolation
            GameLogger.LogError(LoggingManager.LogCategory.Fabricator, $"[FABRICATOR] This should NEVER happen - resetting machine to Idle", ComponentId);
            cellData.machineState = MachineState.Idle;
            return;
        }
        
        GameLogger.LogFabricator($"[FABRICATOR] ✓ Final ingredient validation passed - proceeding with consumption", ComponentId);
        
        // Consume input items
        foreach (var inputReq in recipe.inputItems)
        {
            int consumed = 0;
            GameLogger.LogFabricator($"[FABRICATOR] Need to consume {inputReq.count}x {inputReq.item}", ComponentId);
            
            for (int i = cellData.items.Count - 1; i >= 0 && consumed < inputReq.count; i--)
            {
                // Also ensure we only consume Idle items (not Processing items from previous cycles)
        if (cellData.items[i].itemType == inputReq.item && cellData.items[i].state == ItemState.Idle)
                {
                    GameLogger.LogFabricator($"[FABRICATOR] Consuming {cellData.items[i].itemType} (id: {cellData.items[i].id}, state: {cellData.items[i].state})", ComponentId);
                    cellData.items.RemoveAt(i);
                    consumed++;
                }
            }
            
            if (consumed < inputReq.count)
            {
                GameLogger.LogError(LoggingManager.LogCategory.Fabricator, $"[FABRICATOR] CRITICAL ERROR: Couldn't consume enough {inputReq.item} items (needed {inputReq.count}, got {consumed})", ComponentId);
                // GameLogger.LogError(LoggingManager.LogCategory.Fabricator, " Inventory during failed consumption: [{string.Join(", ", cellData.items.Select(i => $"{i.itemType}({i.state}) id:{i.id}"))}]", ComponentId); // TODO: Fix complex string interpolation
                GameLogger.LogError(LoggingManager.LogCategory.Fabricator, $"[FABRICATOR] Resetting machine to Idle state", ComponentId);
                cellData.machineState = MachineState.Idle;
                return;
            }
            else
            {
                GameLogger.LogFabricator($"[FABRICATOR] ✓ Successfully consumed {consumed}x {inputReq.item}", ComponentId);
            }
        }
        
        // GameLogger.LogFabricator(" Inventory after consumption: [{string.Join(", ", cellData.items.Select(i => $"{i.itemType}({i.state}) id:{i.id}"))}]", ComponentId); // TODO: Fix complex string interpolation
        
        // Create a processing item to track the operation
        ItemData processingItem = new ItemData
        {
            id = GameManager.Instance.GenerateItemId(),
            itemType = recipe.outputItems[0].item, // We'll output this
            x = cellData.x,
            y = cellData.y,
            state = ItemState.Processing,
            processingStartTime = Time.time,
            processingDuration = recipe.processTime
        };
        
        cellData.items.Add(processingItem);
        cellData.machineState = MachineState.Processing;
        
        GameLogger.LogFabricator($"[FABRICATOR] ✓ Created processing item: {processingItem.itemType} (id: {processingItem.id})", ComponentId);
        GameLogger.LogFabricator($"[FABRICATOR] ✓ Machine state set to Processing", ComponentId);
        GameLogger.LogFabricator($"[FABRICATOR] ✓ Processing will complete in {recipe.processTime}s and output {recipe.outputItems[0].item}", ComponentId);
        // GameLogger.LogFabricator(" Final inventory after processing start: [{string.Join(", ", cellData.items.Select(i => $"{i.itemType}({i.state}) id:{i.id}"))}]", ComponentId); // TODO: Fix complex string interpolation
        GameLogger.LogFabricator($"[FABRICATOR] === PROCESSING STARTED SUCCESSFULLY ===", ComponentId);
    }
}
