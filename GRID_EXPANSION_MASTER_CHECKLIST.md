# Grid Expansion Feature - Master Checklist

## Quick Start Guide

This is your master checklist for implementing the expandable grid feature. Follow these steps in order, checking off each item as you complete it.

---

## Pre-Implementation

- [ ] Read `EXPANDABLE_GRID_SETUP_GUIDE.md` (overview and complete instructions)
- [ ] Read `GRID_EXPANSION_PREFAB_GUIDE.md` (prefab creation details)
- [ ] Open ScrapLine project in Unity 6000.2.3f1
- [ ] Ensure latest scripts are pulled from repository
- [ ] Backup current project before starting

---

## Phase 1: Scene Setup (15 minutes)

### Create Core GameObject
- [ ] Create empty GameObject: `ExpandModeSystem` at root level
- [ ] Add component: `GridExpansionService`
- [ ] Add component: `ExpandModeController`
- [ ] Add component: `GridMarkersView`
- [ ] Add component: `GridExpandAnimator`
- [ ] Add component: `GridExpansionOrchestrator`

### Create UI Elements
- [ ] Create UI Image: `DimOverlay`
  - [ ] Set to full screen (stretch-stretch anchors)
  - [ ] Set color: Black, Alpha 100
  - [ ] Enable raycast target
  - [ ] Disable GameObject (script will enable it)
  - [ ] Place above grid container in hierarchy

- [ ] Create empty GameObject: `RowMarkersContainer` (with RectTransform)
  - [ ] Set to full screen (stretch-stretch)
  - [ ] Place above DimOverlay

- [ ] Create empty GameObject: `ColumnMarkersContainer` (with RectTransform)
  - [ ] Set to full screen (stretch-stretch)
  - [ ] Place above DimOverlay

- [ ] Create empty GameObject: `EdgeMarkersContainer` (with RectTransform)
  - [ ] Set to full screen (stretch-stretch)
  - [ ] Place above DimOverlay

### Create Manage Tab Button
- [ ] Locate Manage tab in UI
- [ ] Create Button: `ExpandToggleButton`
  - [ ] Size: 60x60
  - [ ] Position: Top-right of Manage tab (or desired location)
  - [ ] Add plus (+) icon sprite
  - [ ] Add component: `ManageTabButtonBinder`

---

## Phase 2: Prefab Creation (30 minutes)

### InlinePlusMarker Prefab
- [ ] Create UI Image (40x40)
- [ ] Add plus icon sprite (orange #FF9933)
- [ ] Add Button component with color tint transitions
- [ ] Add Canvas Group component
- [ ] Optional: Add Outline component
- [ ] Save as prefab: `Assets/Prefabs/UI/InlinePlusMarker.prefab`
- [ ] Delete from scene after saving

### EdgePlusMarker Prefab
- [ ] Duplicate InlinePlusMarker or create new (48x48)
- [ ] Change color to blue-green #33CCCC (or keep orange)
- [ ] Optional: Add directional arrow
- [ ] Save as prefab: `Assets/Prefabs/UI/EdgePlusMarker.prefab`
- [ ] Delete from scene after saving

### ExpansionCostPrompt Panel
- [ ] Create UI Panel: `ExpansionCostPrompt` (350x180)
  - [ ] Set anchor: Center-center
  - [ ] Set background color: Black, Alpha 220
  
- [ ] Add TextMeshProUGUI: `CostText`
  - [ ] Position: Top center
  - [ ] Size: 300x60
  - [ ] Font size: 22
  - [ ] Alignment: Center
  - [ ] Color: White
  
- [ ] Add Button: `ConfirmButton`
  - [ ] Size: 130x45
  - [ ] Position: Bottom-left
  - [ ] Color: Green #4CAF50
  - [ ] Text: "Confirm" or "✓"
  
- [ ] Add Button: `CancelButton`
  - [ ] Size: 130x45
  - [ ] Position: Bottom-right
  - [ ] Color: Red #F44336
  - [ ] Text: "Cancel" or "✕"
  
- [ ] Add component: `ExpansionCostPrompt` to panel
- [ ] Disable panel GameObject (script will enable it)
- [ ] Optional: Save as prefab

---

## Phase 3: Inspector Configuration (20 minutes)

### ExpandModeController Component
- [ ] Drag `DimOverlay` to "Dim Overlay" field
- [ ] Set "Dim Alpha" to 0.4
- [ ] Check "Enable Expand Mode Logs" for debugging

### GridExpansionService Component
- [ ] Set "Base Cost" to 100
- [ ] Set "Growth Factor" to 2.0
- [ ] Check "Enable Expansion Logs" for debugging

### GridMarkersView Component
- [ ] **References** (drag from Hierarchy/Scene):
  - [ ] Expand Mode Controller: `ExpandModeController` (same GameObject)
  - [ ] Grid Expansion Service: `GridExpansionService` (same GameObject)
  - [ ] Grid Manager: Find GameManager → GridManager component
  - [ ] UI Grid Manager: Find UIGridManager in scene

- [ ] **Marker Prefabs** (drag from Project):
  - [ ] Inline Plus Marker Prefab: `InlinePlusMarker` prefab
  - [ ] Edge Plus Marker Prefab: `EdgePlusMarker` prefab

- [ ] **Marker Containers** (drag from Hierarchy):
  - [ ] Row Markers Container: `RowMarkersContainer`
  - [ ] Column Markers Container: `ColumnMarkersContainer`
  - [ ] Edge Markers Container: `EdgeMarkersContainer`

- [ ] **Marker Settings**:
  - [ ] Inline Marker Size: (40, 40)
  - [ ] Edge Marker Size: (48, 48)
  - [ ] Hover Scale Multiplier: 1.2
  - [ ] Scale Animation Duration: 0.15

- [ ] Check "Enable Marker Logs" for debugging

### GridExpandAnimator Component
- [ ] Set "Slide Duration" to 0.15
- [ ] Click "Slide Ease" curve → Select EaseOutQuad or create custom
- [ ] Optional: Drag audio clip to "Build Sfx"
- [ ] Check "Enable Animation Logs" for debugging

### GridExpansionOrchestrator Component
- [ ] **System References** (drag from same GameObject):
  - [ ] Expand Mode Controller: `ExpandModeController`
  - [ ] Grid Expansion Service: `GridExpansionService`
  - [ ] Grid Markers View: `GridMarkersView`
  - [ ] Grid Expand Animator: `GridExpandAnimator`

- [ ] **External References**:
  - [ ] Expansion Cost Prompt: `ExpansionCostPrompt` panel
  - [ ] Grid Manager: GameManager → GridManager
  - [ ] UI Grid Manager: UIGridManager in scene
  - [ ] Credits Manager: GameManager → CreditsManager

- [ ] Check "Enable Orchestration Logs" for debugging

### ManageTabButtonBinder Component
- [ ] Drag `ExpandModeController` to "Expand Mode Controller"
- [ ] Drag button to "Expand Toggle Button" (or GetComponent)
- [ ] Drag button's Image to "Button Image"
- [ ] Set "Inactive Color": (1.0, 0.6, 0.2, 1.0) - Orange
- [ ] Set "Active Color": (0.2, 1.0, 0.2, 1.0) - Green
- [ ] Check "Enable Button Logs" for debugging

### ExpansionCostPrompt Component
- [ ] Drag `ExpansionCostPrompt` panel to "Prompt Panel"
- [ ] Drag `CostText` to "Cost Text"
- [ ] Drag `ConfirmButton` to "Confirm Button"
- [ ] Drag `CancelButton` to "Cancel Button"
- [ ] Verify "Cost Text Format": "Expand here for {0} credits?"
- [ ] Check "Enable Prompt Logs" for debugging

---

## Phase 4: Testing (20 minutes)

### Basic Functionality Tests
- [ ] Enter Play Mode
- [ ] Navigate to Manage tab
- [ ] Click "+" button
  - [ ] Button changes from orange to green
  - [ ] Dim overlay appears
  - [ ] Plus markers appear between rows/columns
  - [ ] Edge markers appear at left and right
- [ ] Click "+" button again
  - [ ] Button returns to orange
  - [ ] Dim overlay disappears
  - [ ] All markers disappear

### Marker Interaction Tests
- [ ] Enable expand mode
- [ ] Hover over a marker
  - [ ] Marker scales up to 1.2x size
- [ ] Move mouse away
  - [ ] Marker returns to normal size
- [ ] Click a marker
  - [ ] Cost prompt appears
  - [ ] Shows calculated cost (e.g., "Expand here for 100 credits?")
  - [ ] Confirm and Cancel buttons visible

### Expansion Tests (with sufficient credits)
- [ ] Click a row marker
- [ ] Click Confirm in prompt
  - [ ] Credits deducted
  - [ ] Brief animation plays
  - [ ] New row appears at correct position
  - [ ] Grid height increases by 1
  - [ ] Expand mode exits automatically
  - [ ] All markers disappear

- [ ] Enable expand mode again
- [ ] Click a column marker
- [ ] Click Confirm
  - [ ] Credits deducted
  - [ ] New column appears at correct position
  - [ ] Grid width increases by 1

- [ ] Enable expand mode again
- [ ] Click left edge marker
- [ ] Click Confirm
  - [ ] New column appears at left edge
  - [ ] Existing columns shift right

- [ ] Enable expand mode again
- [ ] Click right edge marker
- [ ] Click Confirm
  - [ ] New column appears at right edge

### Edge Case Tests
- [ ] Set credits to 0 (dev tools)
- [ ] Try to expand
  - [ ] Cost prompt appears
  - [ ] Click Confirm
  - [ ] Prompt closes, no expansion
  - [ ] Warning in console about insufficient funds

- [ ] Click Cancel in cost prompt
  - [ ] Prompt closes
  - [ ] No credits deducted
  - [ ] No expansion occurs
  - [ ] Remain in expand mode

### Console Check
- [ ] Review console for errors
  - [ ] No null reference exceptions
  - [ ] No missing component warnings
  - [ ] Only expected debug logs (if enabled)

---

## Phase 5: Polish & Tuning (15 minutes)

### Visual Adjustments
- [ ] Check marker sizes on various screen resolutions
- [ ] Adjust colors to match game aesthetic
- [ ] Verify dim overlay opacity is appropriate
- [ ] Ensure buttons have good hover feedback

### Cost Balancing
- [ ] Test expansion costs feel fair
- [ ] Adjust Base Cost and Growth Factor if needed
- [ ] Verify costs scale appropriately with grid size

### Animation Tuning
- [ ] Check animation feels smooth
- [ ] Adjust Slide Duration if too fast/slow
- [ ] Tune Slide Ease curve for desired feel

### Audio (Optional)
- [ ] Add sound effect to GridExpandAnimator
- [ ] Test sound volume is appropriate
- [ ] Add click sounds to buttons (optional)

---

## Phase 6: Cleanup & Finalization (10 minutes)

### Disable Debug Logging
- [ ] ExpandModeController: Uncheck "Enable Expand Mode Logs"
- [ ] GridExpansionService: Uncheck "Enable Expansion Logs"
- [ ] GridMarkersView: Uncheck "Enable Marker Logs"
- [ ] GridExpandAnimator: Uncheck "Enable Animation Logs"
- [ ] GridExpansionOrchestrator: Uncheck "Enable Orchestration Logs"
- [ ] ManageTabButtonBinder: Uncheck "Enable Button Logs"
- [ ] ExpansionCostPrompt: Uncheck "Enable Prompt Logs"

### Verify Scene State
- [ ] DimOverlay is disabled
- [ ] ExpansionCostPrompt is disabled
- [ ] No marker GameObjects in scene
- [ ] Button shows inactive color (orange)
- [ ] All references assigned (no "None" in Inspector)

### Documentation
- [ ] Add comments to scene explaining setup
- [ ] Document any custom changes made
- [ ] Note any issues encountered for future reference

### Build Test
- [ ] Create test build
- [ ] Verify feature works in build
- [ ] Check mobile compatibility (if applicable)

---

## Acceptance Criteria Verification

Final verification before marking complete:

- [ ] ✅ Clicking "+" toggles Expand Mode on/off cleanly
- [ ] ✅ Clicking "+" again exits mode
- [ ] ✅ Inline + markers appear between rows/columns
- [ ] ✅ Edge markers only appear for columns (left/right)
- [ ] ✅ NO edge markers for rows (top/bottom)
- [ ] ✅ Hover/tap feedback works (marker scales)
- [ ] ✅ Tooltips show correct computed cost (or prompt shows cost)
- [ ] ✅ Confirming performs cost check
- [ ] ✅ Credits deducted on confirmation
- [ ] ✅ Row/column inserts at correct index
- [ ] ✅ Animation plays on expansion
- [ ] ✅ Expand mode exits automatically after expansion
- [ ] ✅ Dim overlay appears in mode and disappears on exit
- [ ] ✅ Normal grid interactions blocked while in mode
- [ ] ✅ All costs/animations are Inspector-tunable
- [ ] ✅ Clear Unity wiring instructions provided
- [ ] ✅ No unrelated systems changed

---

## Troubleshooting Reference

If you encounter issues, check:

1. **Console Errors**: Look for null references or missing components
2. **References**: Verify all Inspector fields are assigned
3. **Hierarchy**: Ensure correct parent-child relationships
4. **Prefabs**: Check prefabs are properly saved and assigned
5. **Logs**: Enable debug logs to trace execution
6. **Guides**: Reference detailed guides for specific issues

See `EXPANDABLE_GRID_SETUP_GUIDE.md` Part 7 for detailed troubleshooting.

---

## Estimated Time

- **Total Implementation Time**: 2-3 hours for first-time setup
- **Phase 1 (Scene)**: 15 minutes
- **Phase 2 (Prefabs)**: 30 minutes
- **Phase 3 (Config)**: 20 minutes
- **Phase 4 (Testing)**: 20 minutes
- **Phase 5 (Polish)**: 15 minutes
- **Phase 6 (Cleanup)**: 10 minutes
- **Buffer**: 30 minutes for issues/learning

---

## Completion Sign-Off

When all items are checked and acceptance criteria verified:

- [ ] Feature fully implemented
- [ ] All tests passing
- [ ] Documentation reviewed
- [ ] Ready for production use

**Implemented by**: _________________  
**Date**: _________________  
**Build Version**: _________________

---

## Additional Resources

- **Main Setup Guide**: `EXPANDABLE_GRID_SETUP_GUIDE.md`
- **Prefab Guide**: `GRID_EXPANSION_PREFAB_GUIDE.md`
- **Script Documentation**: Inline comments in each C# file
- **GitHub Issues**: Report problems or request help

---

**Last Updated**: 2025-10-23  
**Version**: 1.0  
**Compatible with**: Unity 6000.2.3f1 (Unity 6 LTS)
