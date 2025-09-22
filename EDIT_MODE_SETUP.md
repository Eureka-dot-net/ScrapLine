# Edit Mode Implementation - Unity Setup Guide

## Overview
This implementation adds edit mode functionality that allows players to configure machines (specifically SortingMachine) by toggling an edit button, highlighting configurable machines, and providing a configuration UI.

## Features Implemented in Code

### 1. Edit Mode State Management
- **GameManager**: Added `SetEditMode(bool)` and `IsInEditMode()` methods
- **MachineBarUIManager**: Modified `OnEditModeToggled()` to call `GameManager.SetEditMode()`
- **Edit mode state**: Properly managed and communicated between UI and game logic

### 2. Machine Highlighting
- **UIGridManager**: Added `HighlightConfigurableMachines()` method
- **Highlighting system**: Uses existing blank cell highlighting style for consistency
- **Automatic highlighting**: Only machines with `CanConfigure = true` are highlighted

### 3. Configuration Click Handling
- **MachineManager**: Enhanced `OnCellClicked()` to handle edit mode
- **Configuration flow**: In edit mode, clicking highlighted machines calls `OnConfigured()`
- **Mode separation**: Normal mode still handles placement/rotation, edit mode handles configuration

### 4. SortingMachine Configuration
- **SortingMachineConfig**: New data class for left/right item type settings
- **CellData**: Extended with `sortingConfig` field for persistence
- **SortingMachine**: Enhanced `OnConfigured()` to show configuration UI
- **Configuration UI**: `SortingMachineConfigUI` component for dropdown-based configuration

## Unity Scene/Prefab Setup Required

⚠️ **USER ASSISTANCE NEEDED - The following Unity UI setup cannot be completed in code:**

### 1. SortingMachine Configuration UI Panel

**Step 1: Create the Configuration Panel**
1. In the Unity scene, create a new UI Canvas if one doesn't exist
2. Right-click the Canvas → UI → Panel
3. Name the panel "SortingConfigPanel"
4. Position it in the center of the screen
5. Add the `SortingMachineConfigUI` component to this panel

**Step 2: Add Dropdown Components**
1. Right-click SortingConfigPanel → UI → Dropdown - TextMeshPro
2. Name it "LeftItemDropdown" 
3. Right-click SortingConfigPanel → UI → Dropdown - TextMeshPro  
4. Name it "RightItemDropdown"
5. Position them appropriately (left side and right side of panel)

**Step 3: Add Button Components**
1. Right-click SortingConfigPanel → UI → Button - TextMeshPro
2. Name it "ConfirmButton", set text to "Confirm"
3. Right-click SortingConfigPanel → UI → Button - TextMeshPro
4. Name it "CancelButton", set text to "Cancel"
5. Position them at the bottom of the panel

**Step 4: Add Labels (Optional)**
1. Add Text components with labels like "Left Item:", "Right Item:"
2. Position above respective dropdowns for clarity

**Step 5: Configure the SortingMachineConfigUI Component**
1. Select the SortingConfigPanel
2. In the Inspector, find the SortingMachineConfigUI component
3. Assign the following fields:
   - **Left Item Dropdown**: Drag LeftItemDropdown from hierarchy
   - **Right Item Dropdown**: Drag RightItemDropdown from hierarchy  
   - **Confirm Button**: Drag ConfirmButton from hierarchy
   - **Cancel Button**: Drag CancelButton from hierarchy
   - **Config Panel**: Drag SortingConfigPanel itself from hierarchy

**Step 6: Set Initial State**
1. Set the SortingConfigPanel to **inactive** in the hierarchy (uncheck the checkbox)
2. The panel will be automatically activated when configuration is needed

### 2. Edit Button Setup

**Verify Edit Button Connection:**
1. Find the MachineBarUIManager in the scene
2. Ensure the `editButton` field is assigned to the actual edit button in the UI
3. Verify the button's OnClick event calls `OnEditModeToggled()`

### 3. Testing the Implementation

**Test Scenario 1: Edit Mode Toggle**
1. Play the scene
2. Click the Edit button
3. **Expected**: All SortingMachine instances on the grid should be highlighted with green overlay
4. Click Edit button again
5. **Expected**: Highlights should disappear

**Test Scenario 2: SortingMachine Configuration**
1. Place a SortingMachine on the grid (should use className "SortingMachine" from machines.json)
2. Enable Edit mode
3. Click the highlighted SortingMachine
4. **Expected**: Configuration panel should appear with dropdowns for left/right items
5. Select items and click Confirm
6. **Expected**: Panel should close and configuration should be saved

**Test Scenario 3: Non-Configurable Machines**
1. Place other machine types (Conveyor, Spawner, etc.)
2. Enable Edit mode  
3. **Expected**: Only SortingMachines should be highlighted (others have CanConfigure = false)
4. Click non-highlighted machines
5. **Expected**: No configuration should occur

## Configuration Persistence

The sorting configuration is automatically saved in the game's save system via:
- `CellData.sortingConfig.leftItemType`
- `CellData.sortingConfig.rightItemType`

This data persists across save/load cycles and can be used by game logic to determine item routing.

## Fallback Behavior

If the Unity UI components are not set up, the system provides graceful fallbacks:
- Missing `SortingMachineConfigUI`: Logs warning and applies default configuration (can → left, shreddedAluminum → right)
- Missing dropdown options: Uses hardcoded item list as fallback
- Missing buttons: Configuration still works but without UI interaction

## Development Notes

### Code Architecture
- **Minimal changes**: Implementation uses existing highlighting and click handling systems
- **Consistent style**: Highlighting matches existing blank cell highlight appearance
- **Clean separation**: Edit mode and normal mode logic are clearly separated
- **Extensible**: Framework supports adding configuration to other machine types

### Performance Considerations
- Highlighting only recalculates when edit mode is toggled
- Configuration UI is only created once and reused
- No impact on normal gameplay performance

### Future Enhancements
- Add configuration options to other machine types (ProcessorMachine, SpawnerMachine)
- Support for more complex sorting rules (multiple item types, conditions)
- Visual indicators on configured machines showing their settings
- Undo/redo for configuration changes