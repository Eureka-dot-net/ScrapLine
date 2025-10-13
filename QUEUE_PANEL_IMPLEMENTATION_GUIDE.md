# Queue Panel Implementation Guide

## Overview
This document provides detailed instructions for implementing the WasteCrateQueuePanel component in Unity. This panel is a reusable UI component that displays the top N waste crates in the global queue and can be clicked to open the purchase interface.

## Purpose
The WasteCrateQueuePanel serves two main purposes:
1. **Visual Feedback**: Shows players what waste crates are queued and ready to be used by spawners
2. **Interaction Point**: Provides a clickable area to open the WasteCrateConfigPanel for purchasing new crates

## Design Philosophy
The queue panel is designed to be **reusable** across different parts of the UI:
- Currently used in SpawnerConfigPanel to show queued crates
- Can be integrated into WasteCrateConfigPanel to show the same queue information
- Designed with configurable max display items (default: 3)

## Component Architecture

### WasteCrateQueuePanel.cs
**Location**: `ScrapLine/Assets/Scripts/UI/WasteCrateQueuePanel.cs`

**Key Features**:
- Displays up to `maxDisplayItems` (default: 3) waste crates from the queue
- Loads crate sprites dynamically from Resources
- Provides click event (`OnQueueClicked`) for interaction
- Handles empty queue state with optional text display
- Automatically destroys and recreates queue item displays on update

### Public Properties
```csharp
// Required assignments in Unity Inspector
public GameObject queuePanel;           // Main panel GameObject
public Button queueButton;              // Button covering the panel
public Transform queueContainer;        // Parent for queue items (with Layout Group)
public GameObject queueItemPrefab;      // Prefab for single queue item display

// Optional
public TextMeshProUGUI emptyQueueText; // Shows "Queue Empty" when no items
public int maxDisplayItems = 3;        // Configurable display limit
```

### Public Methods
```csharp
// Update the queue display with current data
void UpdateQueueDisplay(List<string> queuedCrateIds)

// Show/hide the panel
void ShowPanel()
void HidePanel()
```

### Public Events
```csharp
// Event fired when the queue panel is clicked
System.Action OnQueueClicked
```

## Unity Setup Instructions

### Step 1: Create Queue Item Prefab
1. Create a new UI GameObject in your scene (right-click in Hierarchy → UI → Image)
2. Name it `QueueItemPrefab`
3. Configure the Image component:
   - **Sprite**: Leave empty (will be set dynamically)
   - **Image Type**: Simple
   - **Preserve Aspect**: True (recommended)
   - **Raycast Target**: False (parent button handles clicks)
4. Set appropriate size (e.g., 64x64 or 80x80 pixels)
5. Drag this GameObject into your Project's Prefabs folder
6. Delete the GameObject from the scene

### Step 2: Create Queue Panel Structure
In your SpawnerConfigPanel or other parent panel:

```
SpawnerConfigPanel
└── QueuePanel (GameObject with WasteCrateQueuePanel component)
    ├── QueueButton (Button component - transparent, covers entire panel)
    ├── QueueContainer (GameObject with Horizontal/Vertical Layout Group)
    │   └── [Queue items will be instantiated here at runtime]
    └── EmptyQueueText (TextMeshProUGUI - optional)
```

### Step 3: Configure Queue Panel GameObject
1. Create a new UI Panel GameObject (right-click in Hierarchy → UI → Panel)
2. Rename it to `QueuePanel`
3. Add the `WasteCrateQueuePanel` component to it
4. Configure panel appearance:
   - Background color/sprite as desired
   - Set appropriate size (width should fit 3 items + spacing)

### Step 4: Add Queue Button
1. Add a UI Button as a child of QueuePanel
2. Rename it to `QueueButton`
3. Configure:
   - **Image**: Set alpha to 0 or very low (transparent clickable area)
   - **RectTransform**: Stretch to fill parent (Anchor: Stretch, Offset: 0,0,0,0)
   - Remove any child Text if present

### Step 5: Add Queue Container
1. Add an empty GameObject as child of QueuePanel
2. Rename it to `QueueContainer`
3. Add a Layout Group component (Horizontal or Vertical):
   - **Horizontal Layout Group** (recommended for side-by-side display):
     - Spacing: 10-20 pixels
     - Child Alignment: Middle Left (or Middle Center)
     - Child Force Expand: Width = False, Height = False
     - Child Control Size: Width = True, Height = True
   - **Vertical Layout Group** (for stacked display):
     - Spacing: 10-20 pixels
     - Child Alignment: Upper Center
4. Position the container appropriately within the panel

### Step 6: Add Empty Queue Text (Optional)
1. Add TextMeshProUGUI as child of QueuePanel
2. Rename it to `EmptyQueueText`
3. Configure:
   - **Text**: "Queue Empty" (will be overridden at runtime)
   - **Alignment**: Center
   - **Font Size**: Appropriate for your design
   - Position in center of panel
4. This text will be hidden automatically when queue has items

### Step 7: Assign References in Inspector
Select the `QueuePanel` GameObject and assign in the WasteCrateQueuePanel component:
1. **Queue Panel**: Drag the QueuePanel GameObject itself
2. **Queue Button**: Drag the QueueButton GameObject
3. **Queue Container**: Drag the QueueContainer GameObject
4. **Queue Item Prefab**: Drag the QueueItemPrefab from your Prefabs folder
5. **Empty Queue Text**: Drag the EmptyQueueText GameObject (if created)
6. **Max Display Items**: Set to 3 (or desired number)

## Integration with SpawnerConfigPanel

### Step 1: Add Component Reference
In SpawnerConfigPanel.cs, the following property already exists:
```csharp
[Tooltip("Queue panel showing top 3 queued waste crates")]
public WasteCrateQueuePanel queuePanel;
```

### Step 2: Assign in Unity Inspector
1. Select your SpawnerConfigPanel GameObject
2. In the Inspector, find the "Queue Panel" field
3. Drag your created QueuePanel GameObject into this field

### Step 3: Optional - Assign WasteCrateConfigPanel
For better performance and explicit references:
```csharp
[Tooltip("Waste crate config panel for purchasing")]
public WasteCrateConfigPanel wasteCrateConfigPanel;
```
Assign this in the Inspector to avoid FindFirstObjectByType calls.

## Integration with WasteCrateConfigPanel (Future)

To reuse the queue panel in WasteCrateConfigPanel:

### Option 1: Add Separate Instance
1. Create a second QueuePanel following the same setup steps
2. Add a reference in WasteCrateConfigPanel.cs:
```csharp
[Tooltip("Queue panel showing queued waste crates")]
public WasteCrateQueuePanel queuePanel;
```
3. Update the queue display in LoadCurrentConfiguration:
```csharp
protected override void LoadCurrentConfiguration()
{
    // Existing code...
    
    // Update queue display
    if (queuePanel != null)
    {
        queuePanel.UpdateQueueDisplay(currentQueueStatus.queuedCrateIds);
        queuePanel.ShowPanel();
    }
}
```

### Option 2: Share Instance
If both panels should show the same queue and are never visible simultaneously:
1. Use the same QueuePanel instance
2. Parent it to a common parent or Canvas
3. Assign the same instance to both SpawnerConfigPanel and WasteCrateConfigPanel
4. Each panel will update it when shown

## Visual Design Guidelines

### Layout Recommendations
- **Horizontal Layout** (Recommended):
  - Displays 3 crate icons side-by-side
  - Clear visual queue from left to right
  - Compact footprint
  
- **Vertical Layout** (Alternative):
  - Displays crates top-to-bottom
  - Good for narrow UI spaces
  - Shows queue order more clearly

### Sizing Guidelines
- **Queue Item Size**: 64x64 to 80x80 pixels
- **Container Spacing**: 10-20 pixels between items
- **Panel Padding**: 10-15 pixels around container
- **Overall Panel Width** (horizontal): ~250-300 pixels for 3 items

### Visual Hierarchy
1. **Primary**: Current crate icon on spawner (larger, prominent)
2. **Secondary**: Progress bar (vertical, next to current crate)
3. **Tertiary**: Queue panel (smaller icons, less prominent)

### Color and State
- **Queue Panel Background**: Subtle, distinguishable from main panel
- **Empty State**: Light gray text, centered
- **Hover State**: Consider adding hover effect to QueueButton
- **Disabled State**: If queue is full, consider graying out purchase area

## Resource Requirements

### Sprites
Queue items will dynamically load waste crate sprites from:
```
Resources/Sprites/Waste/{crateDef.sprite}
```

Ensure all waste crate sprites defined in `wastecrates.json` exist at this path.

### Prefab Structure
The queue item prefab must have:
- **Image component** (can be on root or child)
- Appropriate size for display
- No raycasting (parent button handles clicks)

## Testing Checklist

### Visual Testing
- [ ] Queue panel appears when spawner config is shown
- [ ] Up to 3 crate icons are displayed correctly
- [ ] Sprites load properly for all crate types
- [ ] "Queue Empty" text shows when queue is empty
- [ ] Layout is correct (spacing, alignment)

### Interaction Testing
- [ ] Clicking queue panel opens WasteCrateConfigPanel
- [ ] Queue panel closes when spawner config closes
- [ ] Multiple queue items display in correct order

### Edge Cases
- [ ] Empty queue shows appropriate state
- [ ] Single item in queue displays correctly
- [ ] Queue with more than 3 items shows only first 3
- [ ] Invalid/missing crate IDs are handled gracefully
- [ ] Missing sprites are handled without errors

### Integration Testing
- [ ] Queue updates when crates are purchased
- [ ] Queue updates when crates are consumed by spawners
- [ ] Multiple spawners can access the same queue data
- [ ] Panel state persists correctly across scene changes

## Troubleshooting

### Queue Items Not Displaying
1. Check if `queueItemPrefab` is assigned in Inspector
2. Verify `queueContainer` has a Layout Group component
3. Check Unity Console for sprite loading errors
4. Ensure waste crate sprites exist in Resources/Sprites/Waste/

### Click Not Working
1. Verify `queueButton` is assigned and enabled
2. Check that QueueButton has a Button component
3. Ensure QueueButton RectTransform covers the clickable area
4. Verify `OnQueueClicked` event is subscribed in SpawnerConfigPanel

### Layout Issues
1. Check Layout Group settings on queueContainer
2. Verify queue item prefab size is reasonable
3. Check panel size can accommodate items + spacing
4. Ensure RectTransform anchors are set correctly

### Empty Queue Text Always Visible
1. Check if `emptyQueueText` is assigned correctly
2. Verify queue actually has items (check WasteSupplyManager)
3. Add debug logging to `UpdateQueueDisplay` to verify data

## Performance Considerations

### Object Pooling (Future Enhancement)
Currently, queue items are destroyed and recreated on each update. For better performance:
1. Implement object pooling for queue item GameObjects
2. Show/hide existing items instead of destroying
3. Only create new items when pool is exhausted

### Update Frequency
The queue panel updates when:
- Spawner config panel is shown
- A waste crate is purchased
- (Future) When crates are consumed by spawners

Avoid calling `UpdateQueueDisplay` every frame.

## Example Usage Code

### Basic Update
```csharp
// Get queue status from WasteSupplyManager
var wasteSupplyManager = GameManager.Instance?.wasteSupplyManager;
if (wasteSupplyManager != null)
{
    var queueStatus = wasteSupplyManager.GetGlobalQueueStatus();
    queuePanel.UpdateQueueDisplay(queueStatus.queuedCrateIds);
    queuePanel.ShowPanel();
}
```

### Subscribe to Click Event
```csharp
// In SetupCustomButtonListeners or similar
if (queuePanel != null)
{
    queuePanel.OnQueueClicked += OnQueuePanelClicked;
}

private void OnQueuePanelClicked()
{
    // Open purchase panel
    wasteCrateConfigPanel.ShowConfiguration(currentData, null);
}
```

### Show/Hide
```csharp
// Show the queue panel
queuePanel.ShowPanel();

// Hide the queue panel
queuePanel.HidePanel();
```

## Summary

The WasteCrateQueuePanel is a flexible, reusable component for displaying queued waste crates. Key implementation points:

1. **Setup**: Create prefab → Build panel structure → Assign references
2. **Integration**: Add reference in parent panel → Subscribe to events → Update display
3. **Testing**: Verify visuals → Test interactions → Handle edge cases

The component is designed to work seamlessly with the existing waste crate system and can be easily integrated into multiple UI contexts.
