# Drag and Drop Refactoring - Manual Validation Guide

## Changes Made

### 1. Replaced Copy-Based Drag System with Actual Object Movement

**Before:**
- `CreateDragVisual()` method created a copy of machine renderer
- Copy had red color and incorrect positioning
- Original machine stayed in place during drag

**After:**
- `MoveMachineToMovingContainer()` moves the actual machine renderer
- `UpdateMachinePosition()` positions the real machine at cursor
- `RestoreMachineToOriginalPosition()` returns machine to original cell if needed

### 2. Core Method Changes

#### OnBeginDrag()
- **Old**: Called `CreateDragVisual()` to create copy
- **New**: Calls `MoveMachineToMovingContainer()` to move actual object

#### OnDrag() 
- **Old**: Called `UpdateDragVisualPosition()` on copy
- **New**: Calls `UpdateMachinePosition()` on real machine

#### OnEndDrag()
- **Old**: Called `ClearDragVisual()` and performed game logic
- **New**: Uses `CanDropMachine()` validation before move, restores position if invalid

### 3. Container Management
- Uses `UIGridManager.GetItemsContainer()` or fallback to `movingItemsContainer`
- Stores original parent, position, and sibling index for restoration
- Proper cleanup and error handling throughout

### 4. Raycast Target Settings
- Added `EnsureChildImageRaycastSettings()` method
- Root cell Image: `raycastTarget = true` (receives events)
- Child images: `raycastTarget = false` (don't block events)

### 5. Visual Feedback During Drag
- CanvasGroup with `alpha = 0.8f` for semi-transparency
- `blocksRaycasts = false` to avoid interfering with drop detection
- Proper restoration of visual settings when drag ends

## Manual Validation Steps

### 1. Basic Drag Functionality
```
Test Case: Drag a conveyor belt from one cell to another
Expected: The actual conveyor belt visual moves with cursor
Expected: Original cell becomes empty during drag
Expected: Machine appears in target cell when dropped
```

### 2. Invalid Drop Handling
```
Test Case: Drag machine to invalid location (e.g., occupied cell)
Expected: Machine returns to original position
Expected: No duplicate machines created
Expected: Game state remains consistent
```

### 3. Drag Outside Grid
```
Test Case: Drag machine outside grid boundaries
Expected: Machine is deleted from game
Expected: No visual artifacts remain
Expected: Credits may be affected per game rules
```

### 4. Same Cell Drop (Rotation)
```
Test Case: Drag machine and drop on same cell
Expected: Machine returns to original position
Expected: Machine rotation is triggered
Expected: No movement in game data
```

### 5. Raycast Target Validation
```
Test Case: Check Image components after machine setup
Expected: Root cell Image has raycastTarget=true
Expected: All child images have raycastTarget=false
Expected: Touch/mouse events properly detected
```

## Code Quality Checks

### 1. Null Safety
- All methods check for null objects before operations
- Graceful degradation when containers are missing
- Comprehensive error logging for debugging

### 2. State Management
- Original transform data stored and restored properly
- Visual feedback applied and removed correctly
- Game state synchronization maintained

### 3. Performance
- No memory leaks from object creation/destruction
- Efficient RectTransform operations
- Minimal impact on frame rate during drag

## Testing Commands (When Unity Available)

```bash
# Run all tests
Unity -batchmode -quit -projectPath /home/runner/work/ScrapLine/ScrapLine \\
  -runTests -testPlatform PlayMode \\
  -testResults TestResults.xml -logFile tests.log

# Validate JSON files
python3 -m json.tool Assets/Resources/items.json > /dev/null
python3 -m json.tool Assets/Resources/machines.json > /dev/null  
python3 -m json.tool Assets/Resources/recipes.json > /dev/null

# Check test results
grep -q 'result="Failed"' TestResults.xml && echo "TESTS FAILED" || echo "TESTS PASSED"
```

## Expected Behavior Changes

### Before Refactoring
1. Drag started → Red copy created → Copy moved with cursor → Copy destroyed on drop
2. Original machine never moved during drag
3. Positioning issues due to layout group conflicts
4. Visual inconsistency between drag visual and actual machine

### After Refactoring  
1. Drag started → Actual machine moved to MovingContainer → Machine follows cursor → Machine placed in target cell
2. Original machine disappears from cell during drag (proper visual feedback)
3. Smooth positioning using canvas coordinates
4. Perfect visual consistency (same object throughout)

## Integration Points

### GameManager Integration
- `CanDropMachine()` validation before move attempts
- `OnCellDropped()` called only for valid moves  
- `OnMachineDraggedOutsideGrid()` for deletion
- `OnCellClicked()` for same-cell rotation

### UIGridManager Integration
- `GetItemsContainer()` for proper container hierarchy
- `movingItemsContainer` fallback support
- Maintains existing rendering layer system

## Risk Mitigation

### 1. Backward Compatibility
- All existing GameManager methods still called correctly
- No changes to public APIs or save file format
- MachineRenderer functionality unchanged

### 2. Error Recovery
- Failed moves restore machine to original position
- Invalid drops don't leave orphaned objects
- Comprehensive logging for debugging

### 3. Performance Impact
- Minimal - moving objects vs creating copies is more efficient
- No additional memory allocations during drag
- Reduced GPU load (fewer draw calls)