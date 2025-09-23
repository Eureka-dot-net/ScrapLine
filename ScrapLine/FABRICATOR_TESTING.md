# Fabricator Machine Testing Guide

## Overview
The fabricator machine has been implemented with the following key features:
1. User-selectable recipes (unlike processors that handle any compatible recipe)
2. Multi-input item requirements (waits for ALL required inputs before processing)
3. Intelligent item pulling (only pulls items needed for the selected recipe)

## Testing Setup in Unity

### Prerequisites
1. Open MobileGridScene.unity
2. Ensure FabricatorMachineConfigUI component is present in the scene
3. Place the following machines to create a complete production chain:

### Production Chain for Testing
```
Spawner → Conveyor → Shredder → Conveyor → PlatePress → Conveyor → Fabricator
                ↓                                            ↑
            Conveyor → Granulator → Conveyor → Conveyor ----┘
```

### Step-by-Step Test Procedure

#### 1. Setup Production Chain
- Place spawner machine (bottom edge) - configure for "can" items
- Place shredder machine - processes can → shreddedAluminum  
- Place plate press machine - processes shreddedAluminum → aluminumPlate
- Place spawner machine #2 (bottom edge) - configure for "plasticBottle" items
- Place granulator machine - processes plasticBottle → granulatedPlastic
- Place fabricator machine - this is our test subject
- Connect all with conveyor belts

#### 2. Configure Fabricator
- Click on fabricator machine
- Should open FabricatorMachineConfigUI
- Select recipe: "Reinforced Aluminum Plate" (requires 1 aluminumPlate + 2 granulatedPlastic)
- Confirm selection

#### 3. Test Multi-Input Behavior
- Start the scene
- Observe that items flow through the production chain
- **Key Test**: Fabricator should WAIT until it has:
  - 1 aluminumPlate AND
  - 2 granulatedPlastic
- Only then should it start processing (30 second process time)

#### 4. Test Recipe Selection Enforcement
- Send a "can" item directly to fabricator (bypass shredder)
- **Expected**: Fabricator should NOT pull the can item (it's not in the selected recipe)
- **Expected**: Can item should remain in waiting queue or timeout

#### 5. Test Configuration Persistence  
- Configure fabricator with a recipe
- Save game state
- Reload game
- **Expected**: Fabricator should remember selected recipe

## Expected Behaviors

### ✅ Correct Behaviors
1. **Recipe Selection**: Only shows craftable items in UI
2. **Multi-Input Collection**: Waits for ALL required items before processing
3. **Selective Item Pulling**: Only pulls items needed for selected recipe
4. **Batch Processing**: Consumes all inputs simultaneously when ready
5. **Configuration Requirement**: Won't process anything until recipe is selected

### ❌ Behaviors to Watch For (These Would Be Bugs)
1. Processing with incomplete inputs (e.g., only 1 granulatedPlastic instead of 2)
2. Pulling items not in the selected recipe
3. Starting processing immediately when only partial inputs are available
4. Forgetting selected recipe after save/load
5. Showing non-craftable items in recipe selection UI

## Debugging Tips

### Log Messages to Watch For
```
"Fabricator [machineId] has no recipe selected - cannot process items"
"Fabricator [machineId] has invalid recipe selected: [recipeId]"  
"Fabricator [machineId] has all items needed for recipe"
"Fabricator starting processing for recipe with output [outputItem]"
```

### Common Issues
1. **UI Not Showing**: Ensure FabricatorMachineConfigUI is in scene and assigned
2. **No Items Moving**: Check that conveyors are properly connected and oriented
3. **Items Not Being Pulled**: Verify recipe is selected and items match requirements
4. **Processing Never Starts**: Check that ALL required input items are available

## Recipe Details
The current fabricator recipe requires:
- **Inputs**: 1 aluminumPlate + 2 granulatedPlastic
- **Output**: 1 reinforcedAluminumPlate  
- **Process Time**: 30 seconds (10 base × 3 multiplier)

This means you need:
- 2 aluminum cans (for 1 aluminumPlate via shredder → platePress)
- 2 plastic bottles (for 2 granulatedPlastic via granulator)

## Performance Notes
- Fabricator only runs item-checking logic when in Idle state
- UI uses FactoryRegistry.GetRecipesForMachine() for efficient recipe lookup
- Recipe matching uses string-based IDs for save/load compatibility