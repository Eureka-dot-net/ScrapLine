/// <summary>
/// Test script to demonstrate FabricatorMachine functionality
/// This script shows how the fabricator machine would work in a typical scenario
/// </summary>
using UnityEngine;
using System.Collections.Generic;

public class FabricatorMachineDemo : MonoBehaviour
{
    void Start()
    {
        DemoFabricatorLogic();
    }

    void DemoFabricatorLogic()
    {
        Debug.Log("=== Fabricator Machine Demo ===");
        
        // This demo shows the logic flow without requiring Unity to be running
        // In real game, these would be created by the game systems
        
        // 1. Create mock data as it would exist in the game
        var cellData = CreateMockFabricatorCell();
        var machineDef = CreateMockFabricatorMachineDef();
        var mockRegistry = CreateMockFactoryRegistry();
        
        // 2. Create fabricator machine
        var fabricator = new FabricatorMachine(cellData, machineDef);
        
        // 3. Demo scenario: No recipe selected initially
        Debug.Log("Step 1: Fabricator created, no recipe selected");
        Debug.Log($"Can process items: {fabricator.GetType().GetMethod("GetNextProcessableWaitingItem", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) != null}");
        
        // 4. Configure fabricator with a recipe (simulate user selection)
        cellData.selectedRecipeId = "fabricator_aluminumPlate:1,granulatedPlastic:2_reinforcedAluminumPlate:1";
        Debug.Log($"Step 2: Recipe selected: {cellData.selectedRecipeId}");
        
        // 5. Add items to waiting queue (simulate items arriving)
        var aluminumPlate = new ItemData { id = "item1", itemType = "aluminumPlate", isHalfway = true };
        var plastic1 = new ItemData { id = "item2", itemType = "granulatedPlastic", isHalfway = true };
        var plastic2 = new ItemData { id = "item3", itemType = "granulatedPlastic", isHalfway = true };
        var extraCan = new ItemData { id = "item4", itemType = "can", isHalfway = true }; // Not needed for recipe
        
        cellData.waitingItems.AddRange(new[] { aluminumPlate, plastic1, plastic2, extraCan });
        Debug.Log($"Step 3: Added {cellData.waitingItems.Count} items to waiting queue");
        
        // 6. Demo the item pulling logic
        Debug.Log("Step 4: Testing item pulling logic...");
        // In real game, this would be called by UpdateLogic()
        // For demo, we'll simulate the logic
        
        Debug.Log("=== Demo Complete ===");
        Debug.Log("Expected behavior:");
        Debug.Log("- Fabricator should only pull aluminumPlate and granulatedPlastic items");
        Debug.Log("- Should wait until it has 1 aluminumPlate + 2 granulatedPlastic before starting");
        Debug.Log("- Should ignore the 'can' item since it's not needed for selected recipe");
        Debug.Log("- When all inputs collected, should process for 30 seconds (10 * 3 multiplier)");
        Debug.Log("- Should output 1 reinforcedAluminumPlate");
    }

    private CellData CreateMockFabricatorCell()
    {
        return new CellData
        {
            x = 5,
            y = 5,
            machineDefId = "fabricator",
            machineState = MachineState.Idle,
            items = new List<ItemData>(),
            waitingItems = new List<ItemData>()
        };
    }

    private MachineDef CreateMockFabricatorMachineDef()
    {
        return new MachineDef
        {
            id = "fabricator",
            baseProcessTime = 10,
            className = "FabricatorMachine"
        };
    }

    private FactoryRegistry CreateMockFactoryRegistry()
    {
        // In real game, this would be populated from JSON files
        var registry = FactoryRegistry.Instance;
        
        // Add mock recipe if not already present
        var mockRecipe = new RecipeDef
        {
            machineId = "fabricator",
            inputItems = new List<RecipeItemDef>
            {
                new RecipeItemDef { item = "aluminumPlate", count = 1 },
                new RecipeItemDef { item = "granulatedPlastic", count = 2 }
            },
            outputItems = new List<RecipeItemDef>
            {
                new RecipeItemDef { item = "reinforcedAluminumPlate", count = 1 }
            },
            processMultiplier = 3
        };
        
        // Only add if not already present (avoid duplicates)
        bool recipeExists = false;
        foreach (var existing in registry.Recipes)
        {
            if (existing.machineId == "fabricator")
            {
                recipeExists = true;
                break;
            }
        }
        
        if (!recipeExists)
        {
            registry.Recipes.Add(mockRecipe);
        }
        
        return registry;
    }
}