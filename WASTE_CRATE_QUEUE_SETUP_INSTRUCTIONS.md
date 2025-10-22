# WasteCrateQueuePanel Unity Setup Instructions

## Overview
This guide provides detailed instructions for setting up the WasteCrateQueuePanel in Unity with scrolling support and proper click handling.

## Problem Fixed
1. **Raycast Blocking Issue**: The button requires a target graphic (Image) to detect clicks. The fix ensures the queuePanel has an Image with `raycastTarget = true` for click detection, while queue items have `raycastTarget = false` to prevent blocking.
2. **Limited Display**: The panel now shows ALL queue items with ScrollRect support instead of limiting to a fixed number (previously 3 items).

## Required Setup

### 1. Basic Panel Setup (Without Scrolling)

#### Hierarchy Structure:
```
QueuePanel (GameObject)
├── Button (Component on QueuePanel)
├── WasteCrateQueuePanel (Component on QueuePanel)
├── EmptyText (TextMeshProUGUI - optional)
└── QueueContainer (Transform with LayoutGroup)
```

#### Step-by-Step Instructions:

1. **Create the Main Panel**:
   - Right-click in Hierarchy → UI → Panel
   - Rename to "WasteCrateQueuePanel"
   - The Panel comes with an Image component - **keep it** (required for button clicks)
   - Add `Button` component to this GameObject
   - Add `WasteCrateQueuePanel` script to this GameObject
   - The script will automatically configure the Image as the Button's target graphic

2. **Add Queue Container**:
   - Right-click on WasteCrateQueuePanel → UI → Empty GameObject
   - Rename to "QueueContainer"
   - Add `Horizontal Layout Group` (for left/right layout) OR `Vertical Layout Group` (for top/bottom layout)
   - Configure Layout Group:
     - Child Control Width: ☐ (unchecked)
     - Child Control Height: ☐ (unchecked)
     - Child Force Expand Width: ☐ (unchecked)
     - Child Force Expand Height: ☐ (unchecked)
     - Spacing: 10 (adjust as needed)

3. **Add Empty Queue Text (Optional)**:
   - Right-click on WasteCrateQueuePanel → UI → Text - TextMeshPro
   - Rename to "EmptyQueueText"
   - Set text to "Queue Empty" or similar
   - This will show when the queue is empty

4. **Create Queue Item Prefab**:
   - Create a new GameObject in Assets → Create → Prefab
   - Name it "QueueItemPrefab"
   - Add `Image` component
   - Set desired size (e.g., 64x64)
   - **IMPORTANT**: The script will automatically set `raycastTarget = false` on queue item Images to prevent them from blocking button clicks

5. **Configure WasteCrateQueuePanel Component**:
   - Select WasteCrateQueuePanel GameObject
   - In Inspector, find WasteCrateQueuePanel component:
     - **Queue Panel**: Drag the WasteCrateQueuePanel GameObject itself
     - **Scroll View**: Leave empty (null) for basic setup
     - **Queue Container**: Drag the QueueContainer GameObject
     - **Queue Item Prefab**: Drag the QueueItemPrefab
     - **Empty Queue Text**: Drag the EmptyQueueText (if created)
     - **Layout Direction**: Choose from:
       - `Left`: Items arrange left to right
       - `Right`: Items arrange right to left
       - `Top`: Items arrange top to bottom
       - `Bottom`: Items arrange bottom to top

### 2. Advanced Setup with Scrolling (RECOMMENDED)

#### Hierarchy Structure:
```
QueuePanel (GameObject)
├── Button (Component on QueuePanel)
├── WasteCrateQueuePanel (Component on QueuePanel)
├── EmptyText (TextMeshProUGUI - optional)
└── ScrollView (GameObject with ScrollRect)
    └── Viewport (RectMask2D)
        └── Content (RectTransform)
            └── QueueContainer (Transform with LayoutGroup)
```

#### Step-by-Step Instructions:

1. **Follow Basic Setup Steps 1-4** from above

2. **Add Scroll View**:
   - Right-click on WasteCrateQueuePanel → UI → Scroll View
   - Rename to "ScrollView"
   - Configure ScrollRect component:
     - **Horizontal**: ☑ (checked) for Left/Right layout
     - **Vertical**: ☑ (checked) for Top/Bottom layout
     - **Movement Type**: Elastic or Clamped
     - **Elasticity**: 0.1
     - **Inertia**: ☑ (checked)
     - **Deceleration Rate**: 0.135
     - **Scroll Sensitivity**: 1
     - **Viewport**: Should auto-assign to Viewport child
     - **Content**: Will be assigned to QueueContainer

3. **Configure Viewport**:
   - Select "Viewport" child of ScrollView
   - Should have `RectMask2D` component (added automatically)
   - Adjust RectTransform to desired scroll area size

4. **Configure Content**:
   - Select "Content" child of Viewport
   - Add `Content Size Fitter` component:
     - **Horizontal Fit**: Preferred Size (for horizontal scrolling)
     - **Vertical Fit**: Preferred Size (for vertical scrolling)

5. **Move QueueContainer**:
   - Drag QueueContainer to be a child of "Content"
   - Or create new container as child of Content
   - Add Layout Group as described in Basic Setup Step 2

6. **Configure WasteCrateQueuePanel Component**:
   - Select WasteCrateQueuePanel GameObject
   - In Inspector, find WasteCrateQueuePanel component:
     - **Queue Panel**: Drag the WasteCrateQueuePanel GameObject itself
     - **Scroll View**: Drag the ScrollView GameObject (with ScrollRect)
     - **Queue Container**: Drag the QueueContainer (child of Content)
     - **Queue Item Prefab**: Drag the QueueItemPrefab
     - **Empty Queue Text**: Drag the EmptyQueueText (if created)
     - **Layout Direction**: Choose appropriate direction (must match ScrollRect configuration)

## Layout Direction Configuration

### Horizontal Layouts (Left/Right):
- Use `Horizontal Layout Group` on QueueContainer
- Set ScrollRect `Horizontal = true`, `Vertical = false`
- Items will scroll horizontally
- **Left**: Items added from left, scroll to see more on right
- **Right**: Items added from right, scroll to see more on left

### Vertical Layouts (Top/Bottom):
- Use `Vertical Layout Group` on QueueContainer
- Set ScrollRect `Horizontal = false`, `Vertical = true`
- Items will scroll vertically
- **Top**: Items added from top, scroll to see more below
- **Bottom**: Items added from bottom, scroll to see more above

## Important Notes

1. **Raycast Blocking Fixed**: The script automatically disables `raycastTarget` on ALL Image components within queue items. You don't need to manually configure this.

2. **Automatic ScrollRect Configuration**: The script will automatically configure ScrollRect direction based on `layoutDirection` setting.

3. **No Item Limit**: Unlike previous versions, ALL queue items are now displayed. Use ScrollRect to handle overflow.

4. **Button Always Clickable**: The button remains clickable even when the queue is empty, allowing users to purchase new crates.

5. **Prefab Requirements**: The queue item prefab must have an Image component (or child with Image). The script will load the waste crate sprite and apply it automatically.

## Testing Your Setup

1. **Test Empty Queue**:
   - Play the game
   - Queue should show "Queue Empty" text
   - Button should be clickable

2. **Test With Items**:
   - Add items to the waste queue
   - Items should appear in the container
   - Click on the panel should trigger the button (not blocked by images)

3. **Test Scrolling** (if using ScrollRect):
   - Add more items than can fit in viewport
   - Scroll should work smoothly
   - All items should be visible by scrolling

## Common Issues

### Issue: Button not clicking
- **Solution**: The queuePanel needs an Image component with `raycastTarget = true` for the Button to work
- **Note**: The script now automatically creates and configures this if missing
- **Note**: Queue items have `raycastTarget = false` to prevent blocking clicks

### Issue: Items not displaying
- **Check**: Queue Item Prefab has Image component
- **Check**: Sprite path is correct in wastecrates.json
- **Check**: QueueContainer is assigned correctly

### Issue: Scrolling not working
- **Check**: ScrollRect is assigned to `scrollView` field
- **Check**: Content is assigned in ScrollRect component
- **Check**: ContentSizeFitter is on Content GameObject
- **Check**: Layout direction matches ScrollRect horizontal/vertical setting

### Issue: Layout wrong direction
- **Solution**: Match the `layoutDirection` setting with the correct Layout Group:
  - Left/Right → Horizontal Layout Group
  - Top/Bottom → Vertical Layout Group

## Code Integration

### Accessing the Queue Panel in Code:
```csharp
// Get reference
WasteCrateQueuePanel queuePanel = GetComponent<WasteCrateQueuePanel>();

// Update display with queue data
List<string> queuedCrates = GetQueuedCrateIds(); // Your method
queuePanel.UpdateQueueDisplay(queuedCrates);

// Subscribe to click event
queuePanel.OnQueueClicked += OnQueuePanelClicked;

// Show/hide panel
queuePanel.ShowPanel();
queuePanel.HidePanel();
```

### Example: WasteCrateConfigPanel Integration:
```csharp
[Header("Queue Display")]
public WasteCrateQueuePanel queuePanel;

private void UpdateQueueDisplay()
{
    var wasteSupplyManager = GameManager.Instance?.wasteSupplyManager;
    if (wasteSupplyManager != null && queuePanel != null)
    {
        var queueStatus = wasteSupplyManager.GetGlobalQueueStatus();
        queuePanel.UpdateQueueDisplay(queueStatus.queuedCrateIds);
    }
}
```

## Visual Layout Examples

### Example 1: Horizontal Queue at Top of Panel
```
┌─────────────────────────────────┐
│ WasteCrateQueuePanel            │
│ ┌─────────────────────────────┐ │
│ │ ScrollView (Horizontal)     │ │
│ │ [🧃][🧃][🧃][🧃][🧃] →    │ │
│ └─────────────────────────────┘ │
│                                 │
│  (Rest of panel content)        │
└─────────────────────────────────┘
```

### Example 2: Vertical Queue on Side of Panel
```
┌──────┬────────────────────┐
│ Q    │                    │
│ u    │                    │
│ e ┌─┐│  Panel Content     │
│ u │🧃││                    │
│ e │🧃││                    │
│   │🧃││                    │
│ S │🧃││                    │
│ c │🧃││                    │
│ r └─┘│                    │
│ o ↓  │                    │
│ l    │                    │
│ l    │                    │
└──────┴────────────────────┘
```

## Summary of Changes

### What's New:
1. ✅ **Raycast blocking fixed**: All Image components automatically have `raycastTarget = false`
2. ✅ **Unlimited items**: Removed `maxDisplayItems` limitation
3. ✅ **ScrollRect support**: Added `scrollView` field for scrollable content
4. ✅ **Automatic scroll configuration**: Script configures scroll direction based on layout
5. ✅ **Better documentation**: Comprehensive setup instructions

### What's Removed:
- ❌ `maxDisplayItems` field (no longer needed)
- ❌ `queueButton` field (now private, auto-detected)

### Backward Compatibility:
- Old setups without ScrollRect will still work (all items shown, no scrolling)
- Existing prefabs and containers remain compatible
