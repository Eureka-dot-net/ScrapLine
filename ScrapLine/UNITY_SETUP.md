# Dynamic Machine System - Unity Setup Guide

This document outlines the Unity-specific setup required for the dynamic machine system to work properly.

## Overview

The implementation adds dynamic machine placement, highlighting, and rotation functionality while maintaining compatibility with existing systems.

## Required Unity Setup

### 1. Machine Bar UI Setup

**MachineBarPanel GameObject:**
- Must have a `MachineBarUIManager` component
- Must reference a `machineButtonPrefab` that contains:
  - `MachineButton` component
  - `MachineRenderer` component  
  - `Button` component for interaction
  - `LayoutElement` component (will be added automatically if missing)

**Machine Button Prefab Structure:**
```
MachineButtonPrefab
├── Button component
├── MachineButton component
├── MachineRenderer component
└── (child objects for visual elements)
```

### 2. Grid Cell Setup

**UICell Prefab:**
- Must have `UICell` component with these fields assigned:
  - `borderImage`: Image component for cell border
  - `innerRawImage`: RawImage for inner content
  - `cellButton`: Button component for interaction
  - `blankSprite`: Sprite to show for blank cells
  - `topSpawnPoint`: RectTransform for item spawning (will be created automatically)
  - `itemSpawnPoint`: Will be created by MachineRenderer for machines

### 3. Sprite Resources

Place machine sprites in `Resources/Sprites/Machines/` folder:
- Border sprites (e.g., "conveyor_border", "Machine_border")
- Building sprites (machine-specific buildings)
- Moving part sprites (e.g., "conveyor_moving")
- Main sprites (machine icons)

### 4. Material Resources

Place materials in appropriate Resources folders:
- `mat_conveyor`: Material for conveyor movement
- `mat_spawner`: Material for spawner machines
- `mat_seller`: Material for seller machines

### 5. Scene Hierarchy

**Required GameObjects:**
```
Game Scene
├── GameManager (with GameManager component)
├── UI Canvas
│   ├── MachineBarPanel (with MachineBarUIManager)
│   └── GridPanel (with UIGridManager)
│       └── CellPrefab instances (with UICell components)
└── Resources folder structure as described above
```

## Features Implemented

### 1. Dynamic Machine Bar
- Automatically populates from `FactoryRegistry.Instance.Machines.Values`
- Visual selection feedback with green outline
- Automatic cleanup of selection state

### 2. Grid Highlighting System
- Semi-transparent green overlay on valid placement cells
- Respects machine `gridPlacement` rules from JSON
- Automatic highlighting cleanup

### 3. Machine Placement
- Click highlighted cells to place selected machines
- Replaces existing machines if cell occupied
- Validates placement rules before allowing placement
- Uses actual `MachineDef` data instead of hardcoded types

### 4. Machine Rotation
- Click placed machines to rotate them
- Updates visual direction and item flow

### 5. Blank Cell Protection
- Items cannot move into blank cells
- Blank cells display `blankSprite`
- No "blank machine" type created

### 6. Dynamic Spawn Points
- `MachineRenderer` creates spawn points between border and building
- Blank cells use `topSpawnPoint` for items
- Machine cells use `itemSpawnPoint` for proper item positioning

## Testing

Use the `SystemValidator` script:
1. Add `SystemValidator` component to any GameObject in the scene
2. Right-click component in Inspector
3. Select "Run Validation" from context menu
4. Check Console for validation results

## Configuration

Machine placement rules are defined in `machines.json`:
```json
{
  "gridPlacement": ["any"] | ["grid"] | ["top"] | ["bottom"]
}
```

- `"any"`: Can be placed anywhere
- `"grid"`: Only in grid cells (middle area)
- `"top"`: Only in top row cells
- `"bottom"`: Only in bottom row cells

## Debugging

Enable debug logs to see:
- Machine selection events
- Grid highlighting operations
- Placement validation results
- Item movement blocking at blank cells

## Migration Notes

This implementation maintains backward compatibility:
- Existing save files will load correctly
- Legacy `MachineType` enum still used for compatibility
- New `machineDefId` field tracks specific machine instances
- Visual updates handle both legacy and new machine data

## Performance Considerations

- Highlighting overlays are created once and toggled on/off
- Machine renderers are created dynamically only when needed
- Sprite loading uses Unity's Resources system for easy modding
- Grid operations are optimized for real-time interaction