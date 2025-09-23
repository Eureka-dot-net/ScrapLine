using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Handles fabricator machine behavior. These machines take input items,
/// process them according to a user-selected recipe, and output transformed items.
/// Unlike processor machines that can handle any compatible recipe, fabricators
/// require the user to specify exactly which recipe to use.
/// </summary>
public class FabricatorMachine : ProcessorMachine
{
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
            Debug.LogWarning("FabricatorMachineConfigUI not found in scene. Please add the UI component to configure fabricator machines.");
            
            // Fallback: Set a default configuration for testing if there are any fabricator recipes
            var fabricatorRecipes = FactoryRegistry.Instance.GetRecipesForMachine(cellData.machineDefId);
            if (fabricatorRecipes.Count > 0)
            {
                var defaultRecipe = fabricatorRecipes[0];
                cellData.selectedRecipeId = GetRecipeId(defaultRecipe);
                Debug.Log($"[FABRICATOR] Applied default recipe for testing: {defaultRecipe.outputItems[0].item}");
            }
        }
    }

    /// <summary>
    /// Called when the fabricator configuration is confirmed by the user
    /// </summary>
    private void OnConfigurationConfirmed(string selectedRecipeId)
    {
        Debug.Log($"[FABRICATOR] Recipe selected: {selectedRecipeId}");
        
        // The configuration UI already updates cellData.selectedRecipeId, but let's be explicit
        cellData.selectedRecipeId = selectedRecipeId;
        
        if (string.IsNullOrEmpty(selectedRecipeId))
        {
            Debug.Log("[FABRICATOR] Recipe cleared - machine will not process items");
        }
        else
        {
            RecipeDef recipe = GetSelectedRecipe();
            if (recipe != null && recipe.outputItems.Count > 0)
            {
                Debug.Log($"[FABRICATOR] Will craft: {recipe.outputItems[0].item} (inputs: {string.Join(", ", recipe.inputItems.Select(i => $"{i.count}x {i.item}"))})");
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
            Debug.LogWarning($"[FABRICATOR] Invalid recipe selected: {cellData.selectedRecipeId}");
            return null;
        }

        // Check what items we still need for this recipe
        var neededItems = GetNeededItemsForRecipe(selectedRecipe);
        if (neededItems.Count == 0)
        {
            Debug.Log($"[FABRICATOR] All items collected for recipe, ready to process");
            return null; // We have everything we need
        }

        // Find the first waiting item that we need for this recipe
        foreach (var waitingItem in cellData.waitingItems)
        {
            if (neededItems.ContainsKey(waitingItem.itemType) && neededItems[waitingItem.itemType] > 0)
            {
                Debug.Log($"[FABRICATOR] Found needed item: {waitingItem.itemType}");
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
            // We have all items - check if ready to process
            CheckIfReadyToProcess();
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
                
                Debug.Log($"[FABRICATOR] Pulling {waitingItem.itemType}");
                
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
        
        Debug.LogWarning($"[FABRICATOR] Could not find recipe with ID: {cellData.selectedRecipeId}");
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
        
        // Subtract items we already have in the machine
        foreach (var item in cellData.items)
        {
            // Skip processing items - they don't count as inventory
            if (item.state == ItemState.Processing) continue;
            
            if (needed.ContainsKey(item.itemType))
            {
                needed[item.itemType]--;
                if (needed[item.itemType] <= 0)
                {
                    needed.Remove(item.itemType);
                }
            }
        }
        
        return needed;
    }

    /// <summary>
    /// Override to check if we have all required items for the selected recipe before starting processing
    /// </summary>
    public override void OnItemArrived(ItemData item)
    {
        Debug.Log($"[FABRICATOR] Item {item.itemType} arrived");
        
        // Find and remove the item from waiting queue
        ItemData itemToProcess = cellData.waitingItems.Find(i => i.id == item.id);
        if (itemToProcess != null)
        {
            cellData.waitingItems.Remove(itemToProcess);
            UpdateStackIndices();
            UpdateWaitingItemVisualPositions();
            
            // Add the item to our inventory
            cellData.items.Add(itemToProcess);
            
            // Destroy visual item since it's now in the machine
            UIGridManager gridManager = UnityEngine.Object.FindAnyObjectByType<UIGridManager>();
            if (gridManager != null)
            {
                gridManager.DestroyVisualItem(itemToProcess.id);
            }
            
            // Reset state to Idle so we can pull more items if needed
            cellData.machineState = MachineState.Idle;
            
            // Check if we now have all items needed for the recipe
            CheckIfReadyToProcess();
        }
        else
        {
            Debug.LogWarning($"[FABRICATOR] Could not find item {item.id} in waiting queue");
        }
    }

    /// <summary>
    /// Check if we have all items needed for the selected recipe and start processing if we do
    /// </summary>
    private void CheckIfReadyToProcess()
    {
        if (cellData.machineState != MachineState.Idle) return;
        
        RecipeDef selectedRecipe = GetSelectedRecipe();
        if (selectedRecipe == null) return;
        
        Debug.Log($"[FABRICATOR] Using recipe: {string.Join(", ", selectedRecipe.inputItems.Select(i => $"{i.count}x {i.item}"))} → {string.Join(", ", selectedRecipe.outputItems.Select(o => $"{o.count}x {o.item}"))}");
        
        var neededItems = GetNeededItemsForRecipe(selectedRecipe);
        if (neededItems.Count == 0)
        {
            Debug.Log($"[FABRICATOR] All inputs ready - starting processing. Current inventory: {string.Join(", ", cellData.items.Select(i => i.itemType))}");
            // We have all items needed - start processing
            StartFabricatorProcessing(selectedRecipe);
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
                    Debug.Log($"[FABRICATOR] Processing complete for {item.itemType}");
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
        // Remove the processing item
        cellData.items.Remove(processingItem);
        
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
                    
                    Debug.Log($"[FABRICATOR] Created output: {newItem.itemType} (id: {newItem.id})");
                    
                    // Try to start movement of the newly created item
                    TryStartMove(newItem);
                }
            }
        }
        
        // Reset machine state to Idle so it can start the next recipe cycle
        cellData.machineState = MachineState.Idle;
        Debug.Log($"[FABRICATOR] Ready for next recipe cycle. Inventory now: {string.Join(", ", cellData.items.Select(i => i.itemType))}");
    }

    /// <summary>
    /// Start processing with all required items for the recipe
    /// </summary>
    private void StartFabricatorProcessing(RecipeDef recipe)
    {
        Debug.Log($"[FABRICATOR] Starting processing for recipe: {recipe.outputItems[0].item}");
        
        // Log current inventory before consumption
        Debug.Log($"[FABRICATOR] Current inventory: {string.Join(", ", cellData.items.Select(i => i.itemType))}");
        
        // Consume input items
        foreach (var inputReq in recipe.inputItems)
        {
            int consumed = 0;
            for (int i = cellData.items.Count - 1; i >= 0 && consumed < inputReq.count; i--)
            {
                if (cellData.items[i].itemType == inputReq.item)
                {
                    Debug.Log($"[FABRICATOR] Consuming {cellData.items[i].itemType} (id: {cellData.items[i].id})");
                    cellData.items.RemoveAt(i);
                    consumed++;
                }
            }
            
            if (consumed < inputReq.count)
            {
                Debug.LogError($"[FABRICATOR] ERROR: Couldn't consume enough {inputReq.item} items (needed {inputReq.count}, got {consumed})");
                return;
            }
            else
            {
                Debug.Log($"[FABRICATOR] Successfully consumed {consumed}x {inputReq.item}");
            }
        }
        
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
        
        Debug.Log($"[FABRICATOR] Processing started, will complete in {recipe.processTime}s and output {recipe.outputItems[0].item}");
    }
}
