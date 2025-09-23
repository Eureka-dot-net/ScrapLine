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
        Debug.Log($"FabricatorMachine at ({cellData.x}, {cellData.y}) configured.");

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
                Debug.Log($"Applied default fabricator recipe: {defaultRecipe.outputItems[0].item}");
            }
        }
    }

    /// <summary>
    /// Called when the fabricator configuration is confirmed by the user
    /// </summary>
    private void OnConfigurationConfirmed(string selectedRecipeId)
    {
        Debug.Log($"Fabricator machine configured with recipe: {selectedRecipeId}");
        
        // The configuration UI already updates cellData.selectedRecipeId, but let's be explicit
        cellData.selectedRecipeId = selectedRecipeId;
        
        if (string.IsNullOrEmpty(selectedRecipeId))
        {
            Debug.Log("Fabricator recipe cleared - machine will not process items until recipe is selected");
        }
        else
        {
            RecipeDef recipe = GetSelectedRecipe();
            if (recipe != null && recipe.outputItems.Count > 0)
            {
                Debug.Log($"Fabricator will now craft: {recipe.outputItems[0].item}");
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
            Debug.LogWarning($"Fabricator {machineDef.id} has no recipe selected - cannot process items");
            return null;
        }

        // Get the selected recipe
        RecipeDef selectedRecipe = GetSelectedRecipe();
        if (selectedRecipe == null)
        {
            Debug.LogWarning($"Fabricator {machineDef.id} has invalid recipe selected: {cellData.selectedRecipeId}");
            return null;
        }

        // Check what items we still need for this recipe
        var neededItems = GetNeededItemsForRecipe(selectedRecipe);
        if (neededItems.Count == 0)
        {
            Debug.Log($"Fabricator {machineDef.id} has all items needed for recipe");
            return null; // We have everything we need
        }

        // Find the first waiting item that we need for this recipe
        foreach (var waitingItem in cellData.waitingItems)
        {
            if (neededItems.ContainsKey(waitingItem.itemType) && neededItems[waitingItem.itemType] > 0)
            {
                return waitingItem;
            }
        }

        return null; // No needed items are waiting
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
                return recipe;
        }
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
        Debug.Log($"Item {item.id} has fully arrived at fabricator {machineDef.id}.");
        
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
            
            // Check if we now have all items needed for the recipe
            CheckIfReadyToProcess();
        }
        else
        {
            Debug.LogWarning($"Could not find item {item.id} in waiting queue.");
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
        
        var neededItems = GetNeededItemsForRecipe(selectedRecipe);
        if (neededItems.Count == 0)
        {
            // We have all items needed - start processing
            StartFabricatorProcessing(selectedRecipe);
        }
    }

    /// <summary>
    /// Start processing with all required items for the recipe
    /// </summary>
    private void StartFabricatorProcessing(RecipeDef recipe)
    {
        Debug.Log($"Fabricator {machineDef.id} starting processing for recipe with output {recipe.outputItems[0].item}");
        
        // Consume input items
        foreach (var inputReq in recipe.inputItems)
        {
            int consumed = 0;
            for (int i = cellData.items.Count - 1; i >= 0 && consumed < inputReq.count; i--)
            {
                if (cellData.items[i].itemType == inputReq.item)
                {
                    cellData.items.RemoveAt(i);
                    consumed++;
                }
            }
            
            if (consumed < inputReq.count)
            {
                Debug.LogError($"Fabricator {machineDef.id} couldn't consume enough {inputReq.item} items (needed {inputReq.count}, got {consumed})");
                return;
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
        
        Debug.Log($"Fabricator processing started, will complete in {recipe.processTime}s");
    }
}
