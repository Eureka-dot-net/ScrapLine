# Expandable Factory Grid - Complete Unity Setup Guide

## Overview
This guide provides step-by-step instructions for setting up the expandable grid feature in Unity. The feature allows players to dynamically expand their factory grid by adding rows and columns at runtime.

## Prerequisites
- Unity 6000.2.3f1 (Unity 6 LTS) installed
- ScrapLine project open in Unity Editor
- All C# scripts are already in place (Phase 1-3 complete)

---

## Part 1: Scene Setup

### 1. Create ExpandModeSystem GameObject

1. In the Hierarchy, create an empty GameObject at the root level:
   - Right-click in Hierarchy → Create Empty
   - Name: `ExpandModeSystem`
   - Position: (0, 0, 0)

2. Add all expansion components to this GameObject:
   - Select `ExpandModeSystem`
   - In Inspector, click **Add Component**
   - Add these scripts in order:
     - `GridExpansionService`
     - `ExpandModeController`
     - `GridMarkersView`
     - `GridExpandAnimator`
     - `GridExpansionOrchestrator`

---

## Part 2: UI Setup

### 2. Create Dim Overlay

1. In the Canvas, create a full-screen Image for the dim overlay:
   - Right-click on Canvas → UI → Image
   - Name: `DimOverlay`

2. Configure the RectTransform:
   - Anchor Preset: **Stretch-Stretch** (bottom-right option)
   - Left/Right/Top/Bottom: all **0**
   - This makes it cover the entire canvas

3. Configure the Image:
   - Color: **Black** (R:0, G:0, B:0)
   - Alpha: **100** (will be adjusted by script)
   - Raycast Target: **Checked** ✓ (to block grid clicks)

4. Initial state:
   - **Disable** the DimOverlay GameObject (uncheck in hierarchy)
   - The script will enable it when expand mode is activated

5. Hierarchy placement:
   - Place DimOverlay as a sibling ABOVE the grid container
   - Higher in hierarchy = rendered on top

### 3. Create Marker Containers

Create three empty GameObjects with RectTransforms to hold markers:

#### a) RowMarkersContainer
1. Right-click on Canvas → Create Empty
2. Name: `RowMarkersContainer`
3. Add Component: **RectTransform** (if not already added)
4. Configure:
   - Anchor: **Stretch-Stretch**
   - Left/Right/Top/Bottom: all **0**
5. Place ABOVE DimOverlay in hierarchy

#### b) ColumnMarkersContainer
1. Right-click on Canvas → Create Empty
2. Name: `ColumnMarkersContainer`
3. Configure same as RowMarkersContainer
4. Place ABOVE DimOverlay in hierarchy

#### c) EdgeMarkersContainer
1. Right-click on Canvas → Create Empty
2. Name: `EdgeMarkersContainer`
3. Configure same as RowMarkersContainer
4. Place ABOVE DimOverlay in hierarchy

**Important**: All marker containers must be above DimOverlay to be visible when overlay is active.

### 4. Create Manage Tab Expand Button

1. Locate the **Manage** tab panel in your UI hierarchy

2. Create the expand toggle button:
   - Right-click on Manage tab panel → UI → Button
   - Name: `ExpandToggleButton`

3. Configure the Button:
   - Size: 50x50 to 70x70 (square)
   - Position: Top-right corner of Manage tab (or desired location)

4. Set up the button icon:
   - Find or create a "+" (plus) icon sprite
     - Recommended: Orange/yellow circular button
     - Import to Assets/Resources/UI/Icons/ or similar
   - Select the button's Image component
   - Drag the plus icon sprite into **Source Image**

5. Add the binder component:
   - Select ExpandToggleButton
   - Add Component → `ManageTabButtonBinder`

---

## Part 3: Prefab Creation

### 5. Create InlinePlusMarker Prefab

1. Create a new UI Image for the marker:
   - Right-click in Project → Create → Prefab
   - Name: `InlinePlusMarker`
   - Or: Create in scene first, then drag to Project to create prefab

2. Configure the marker (in scene or prefab):
   - **Image Component**:
     - Size: 40x40 to 48x48
     - Source Image: Small circular plus icon
     - Color: Orange (#FF9933) or similar bright color
     - Material: None (default UI material)
   
   - **Add Button Component**:
     - Add Component → Button
     - Transition: Color Tint
     - Normal Color: Orange
     - Highlighted Color: Lighter orange
     - Pressed Color: Darker orange
   
   - **Add Canvas Group** (for hover effects):
     - Add Component → Canvas Group
     - Alpha: 1.0
     - Blocks Raycasts: ✓

3. Styling recommendations:
   - Add a subtle outline or glow
   - Use a rounded, friendly design
   - Make it stand out but not too distracting
   - Consider adding a small drop shadow

4. Save as prefab in: `Assets/Prefabs/UI/InlinePlusMarker.prefab`

### 6. Create EdgePlusMarker Prefab

1. Create similar to InlinePlusMarker, with differences:
   - Name: `EdgePlusMarker`
   - Size: 48x48 to 56x56 (slightly larger)
   - Could use an arrow or directional indicator
   - Or use the same plus icon but larger

2. Optional enhancements:
   - Add a left/right arrow to indicate direction
   - Use a different color (e.g., blue-green)
   - Add an animated pulse effect

3. Save as prefab in: `Assets/Prefabs/UI/EdgePlusMarker.prefab`

### 7. Create ExpansionCostPrompt Panel

#### Panel Structure:
```
ExpansionCostPrompt (Panel)
├── Background (Image - semi-transparent dark)
├── CostText (TextMeshProUGUI)
├── ConfirmButton (Button)
│   └── Text (TextMeshProUGUI - "Confirm")
└── CancelButton (Button)
    └── Text (TextMeshProUGUI - "Cancel")
```

#### Step-by-step:

1. **Create the main panel**:
   - Right-click in Canvas → UI → Panel
   - Name: `ExpansionCostPrompt`
   - Size: Width 350, Height 180
   - Anchor: Center-center
   - Position: Center of screen

2. **Configure Background**:
   - The panel's Image component
   - Color: Dark semi-transparent (R:0, G:0, B:0, A:200)
   - Add a subtle border if desired

3. **Add Cost Text**:
   - Right-click ExpansionCostPrompt → UI → Text - TextMeshPro
   - Name: `CostText`
   - Position: Near top of panel (Y: +30)
   - Width: 300, Height: 40
   - Text: "Expand here for 100 credits?" (placeholder)
   - Font Size: 20-24
   - Alignment: Center
   - Color: White or bright color

4. **Add Confirm Button**:
   - Right-click ExpansionCostPrompt → UI → Button
   - Name: `ConfirmButton`
   - Size: 120x40
   - Position: Bottom-left (X: -60, Y: -40)
   - Background Color: Green (#4CAF50)
   - Add child Text (TMP):
     - Text: "Confirm" or "✓"
     - Font Size: 18
     - Color: White
     - Alignment: Center

5. **Add Cancel Button**:
   - Same as Confirm Button but:
   - Name: `CancelButton`
   - Position: Bottom-right (X: +60, Y: -40)
   - Background Color: Red (#F44336) or Gray
   - Text: "Cancel" or "✕"

6. **Add ExpansionCostPrompt Component**:
   - Select the ExpansionCostPrompt panel
   - Add Component → `ExpansionCostPrompt`

7. **Initial State**:
   - Disable the ExpansionCostPrompt GameObject
   - Script will enable it when needed

8. **Optional enhancements**:
   - Add CanvasGroup for fade effects
   - Add button hover animations
   - Add sound effects on button clicks

9. **Save as prefab** (optional but recommended):
   - Drag ExpansionCostPrompt to Project
   - Save in: `Assets/Prefabs/UI/ExpansionCostPrompt.prefab`

---

## Part 4: Inspector Configuration

### 8. Configure ExpandModeController

Select `ExpandModeSystem` in Hierarchy, find `ExpandModeController` component:

1. **Visual Feedback**:
   - Dim Overlay: Drag `DimOverlay` from Hierarchy
   - Dim Alpha: `0.4` (40% opacity)

2. **Debug**:
   - Enable Expand Mode Logs: ✓ (checked for debugging)

### 9. Configure GridExpansionService

In the same GameObject, find `GridExpansionService` component:

1. **Cost Configuration**:
   - Base Cost: `100` (starting cost)
   - Growth Factor: `2.0` (cost increases per grid area)

2. **Debug**:
   - Enable Expansion Logs: ✓

### 10. Configure GridMarkersView

Find `GridMarkersView` component:

1. **References** (drag from Hierarchy/Project):
   - Expand Mode Controller: `ExpandModeController` (same GameObject)
   - Grid Expansion Service: `GridExpansionService` (same GameObject)
   - Grid Manager: Find `GameManager` → `GridManager` component
   - UI Grid Manager: Find `UIGridManager` in scene

2. **Marker Prefabs** (drag from Project):
   - Inline Plus Marker Prefab: `InlinePlusMarker` prefab
   - Edge Plus Marker Prefab: `EdgePlusMarker` prefab

3. **Marker Containers** (drag from Hierarchy):
   - Row Markers Container: `RowMarkersContainer`
   - Column Markers Container: `ColumnMarkersContainer`
   - Edge Markers Container: `EdgeMarkersContainer`

4. **Marker Settings**:
   - Inline Marker Size: (40, 40)
   - Edge Marker Size: (48, 48)
   - Hover Scale Multiplier: `1.2` (20% larger on hover)
   - Scale Animation Duration: `0.15` seconds

5. **Debug**:
   - Enable Marker Logs: ✓

### 11. Configure GridExpandAnimator

Find `GridExpandAnimator` component:

1. **Animation Settings**:
   - Slide Duration: `0.15` seconds (fast, snappy)
   - Slide Ease: Click curve editor → Select **EaseOutQuad** preset
     - Or create custom: Start slow, end fast for snap-in effect

2. **Audio** (optional):
   - Build Sfx: Drag an AudioClip (e.g., pop/click sound)
     - Leave empty if no sound desired
     - Example sounds: build confirmation, placement sound

3. **Debug**:
   - Enable Animation Logs: ✓

### 12. Configure GridExpansionOrchestrator

Find `GridExpansionOrchestrator` component:

1. **System References** (drag from same GameObject):
   - Expand Mode Controller: `ExpandModeController`
   - Grid Expansion Service: `GridExpansionService`
   - Grid Markers View: `GridMarkersView`
   - Grid Expand Animator: `GridExpandAnimator`

2. **External References**:
   - Expansion Cost Prompt: `ExpansionCostPrompt` (from Canvas)
   - Grid Manager: `GameManager` → `GridManager`
   - UI Grid Manager: `UIGridManager` (in scene)
   - Credits Manager: `GameManager` → `CreditsManager`

3. **Debug**:
   - Enable Orchestration Logs: ✓

### 13. Configure ManageTabButtonBinder

Select `ExpandToggleButton`, find `ManageTabButtonBinder` component:

1. **References**:
   - Expand Mode Controller: Drag `ExpandModeController` from ExpandModeSystem
   - Expand Toggle Button: Drag the button itself (or use GetComponent)
   - Button Image: Drag the button's Image component

2. **Visual Feedback**:
   - Inactive Color: Orange (1.0, 0.6, 0.2, 1.0) - default state
   - Active Color: Green (0.2, 1.0, 0.2, 1.0) - expand mode active

3. **Debug**:
   - Enable Button Logs: ✓

### 14. Configure ExpansionCostPrompt

Select `ExpansionCostPrompt` panel, find `ExpansionCostPrompt` component:

1. **UI Components** (drag from same GameObject hierarchy):
   - Prompt Panel: The ExpansionCostPrompt GameObject itself
   - Cost Text: The `CostText` TextMeshProUGUI component
   - Confirm Button: The `ConfirmButton` Button component
   - Cancel Button: The `CancelButton` Button component

2. **Text Format**:
   - Cost Text Format: `"Expand here for {0} credits?"` (or customize)
   - The `{0}` will be replaced with the actual cost

3. **Debug**:
   - Enable Prompt Logs: ✓

---

## Part 5: Testing

### 15. Test Expand Mode Toggle

1. Enter Play Mode
2. Navigate to the Manage tab
3. Click the **"+"** button
4. **Expected behavior**:
   - Button changes color (orange → green)
   - Dim overlay appears over grid
   - Plus markers appear between rows/columns and at column edges
5. Click the **"+"** button again
6. **Expected behavior**:
   - Button returns to orange
   - Dim overlay disappears
   - All markers disappear

### 16. Test Marker Interaction

1. Enter Play Mode and enable expand mode
2. Hover over a plus marker
3. **Expected behavior**:
   - Marker scales up slightly (1.2x)
4. Move mouse away
5. **Expected behavior**:
   - Marker returns to normal size
6. Click a marker
7. **Expected behavior**:
   - Cost prompt appears
   - Shows calculated cost
   - Offers Confirm/Cancel buttons

### 17. Test Expansion Purchase

**Setup**: Ensure you have enough credits (use dev tools if needed)

1. Enable expand mode and click a marker
2. In the cost prompt, click **Confirm**
3. **Expected behavior**:
   - Credits deducted from player balance
   - Animation plays (brief wait)
   - Grid expands at the selected position
   - New cells appear
   - Expand mode exits automatically
   - UI updates to show new grid size

4. Try clicking **Cancel** instead
5. **Expected behavior**:
   - Prompt closes
   - No credits deducted
   - Grid unchanged
   - Remain in expand mode

### 18. Test Insufficient Funds

1. Set credits to less than expansion cost (dev tools)
2. Enable expand mode and click a marker
3. Click **Confirm** on the prompt
4. **Expected behavior**:
   - Prompt closes
   - No expansion occurs
   - Warning logged (check console)
   - Credits unchanged

### 19. Test Row vs Column Expansion

1. **Row Expansion** (internal only):
   - Only markers between existing rows
   - No markers at top or bottom edge
   - Clicking inserts a new row

2. **Column Expansion**:
   - Markers between existing columns
   - Edge markers at LEFT and RIGHT
   - Clicking any marker inserts a new column

---

## Part 6: Customization

### Cost Model Tuning

Adjust in `GridExpansionService`:
- **Base Cost**: Starting price for any expansion
- **Growth Factor**: How much cost increases per grid cell
- Formula: `cost = baseCost + (rows × cols × growthFactor)`

Example configurations:
- **Easy**: Base 50, Factor 1.0
- **Normal**: Base 100, Factor 2.0 (default)
- **Hard**: Base 200, Factor 5.0

### Animation Tuning

Adjust in `GridExpandAnimator`:
- **Slide Duration**: How long the expansion animation takes
  - Faster: 0.1s (snappy)
  - Default: 0.15s (balanced)
  - Slower: 0.3s (smooth)

- **Slide Ease**: Animation curve shape
  - EaseOutQuad: Fast start, slow end (recommended)
  - EaseInOut: Balanced acceleration
  - Linear: Constant speed

### Visual Customization

**Marker Appearance**:
- Size: Adjust in GridMarkersView inspector
- Color: Edit prefab Image component
- Icon: Replace sprite in prefab

**Dim Overlay**:
- Opacity: Adjust Dim Alpha in ExpandModeController
- Color: Change DimOverlay Image color (black, dark blue, etc.)

**Button Colors**:
- Active/Inactive: Adjust in ManageTabButtonBinder inspector
- Hover effects: Configure Button component transitions

---

## Part 7: Troubleshooting

### Markers don't appear
- Check that marker containers are above DimOverlay in hierarchy
- Verify marker prefabs are assigned in GridMarkersView
- Check console for errors about missing references

### Cost prompt doesn't show
- Verify ExpansionCostPrompt is assigned in GridExpansionOrchestrator
- Check that prompt panel starts disabled (not active)
- Look for "Canvas not found" errors

### Grid doesn't expand after confirm
- Check that Grid Manager and UI Grid Manager are assigned
- Verify Credits Manager is assigned and has sufficient balance
- Look for errors in console about null references

### Button doesn't toggle expand mode
- Verify ExpandModeController is assigned in ManageTabButtonBinder
- Check that button onClick is not already assigned to other handlers
- Look for "Button not assigned" errors

### Animation doesn't play
- Verify GridExpandAnimator is assigned in orchestrator
- Check that Slide Duration is > 0
- Animation is subtle by default - increase duration to see it better

---

## Part 8: Advanced Features (Optional)

### Add Cost Tooltips on Hover

1. Create a small UI Text (TMP) tooltip prefab
2. In GridMarkersView, add tooltip spawning logic on marker hover
3. Show computed cost next to hovered marker

### Add Sound Effects

1. Import audio clips:
   - Click sound for markers
   - Confirmation sound for purchase
   - Build/expansion sound for animation

2. Assign in Inspector:
   - GridExpandAnimator: Build Sfx
   - Button components: Navigation sounds

3. Add AudioSource to buttons if needed

### Add Particle Effects

1. Create a particle system for expansion
2. Trigger on successful expansion in GridExpandAnimator
3. Use cell position for particle spawn location

### Add Tutorial/Help

1. Create a tooltip or help panel explaining expand mode
2. Show first time player enters expand mode
3. Include instructions on how to expand

---

## Summary Checklist

Before marking the feature complete, verify:

### Scene Setup
- [ ] ExpandModeSystem GameObject created with all components
- [ ] DimOverlay created and configured
- [ ] Marker containers created (Row, Column, Edge)
- [ ] Manage tab expand button created

### Prefabs
- [ ] InlinePlusMarker prefab created and styled
- [ ] EdgePlusMarker prefab created and styled
- [ ] ExpansionCostPrompt panel created with all elements

### Inspector Configuration
- [ ] ExpandModeController: DimOverlay assigned
- [ ] GridExpansionService: Costs configured
- [ ] GridMarkersView: All references assigned, prefabs assigned
- [ ] GridExpandAnimator: Animation settings configured
- [ ] GridExpansionOrchestrator: All 8 references assigned
- [ ] ManageTabButtonBinder: Button and controller assigned
- [ ] ExpansionCostPrompt: All UI components assigned

### Testing Results
- [ ] Expand mode toggles on/off correctly
- [ ] Markers appear and disappear
- [ ] Hover effects work
- [ ] Cost prompt shows and closes
- [ ] Grid expands on confirm with sufficient funds
- [ ] Credits deducted correctly
- [ ] Animation plays
- [ ] Insufficient funds handled gracefully

### Polish
- [ ] Visual style matches game aesthetic
- [ ] Colors configured to match UI theme
- [ ] Costs balanced for gameplay
- [ ] Animations feel smooth
- [ ] (Optional) Sound effects added
- [ ] (Optional) Tutorial/help added

---

## Support

If you encounter issues not covered in this guide:

1. **Check Console**: Look for error messages or warnings
2. **Verify References**: Ensure all Inspector fields are assigned
3. **Enable Logging**: Turn on debug logs in all components
4. **Test in Isolation**: Test each component separately
5. **Review Scripts**: Check the inline documentation in each C# file

Each script contains detailed Unity wiring instructions in its comments at the end of the file.

---

## Contact

For additional assistance or to report issues:
- Create an issue in the GitHub repository
- Check documentation in individual script files
- Review the inline comments for specific component setup

---

**Version**: 1.0  
**Last Updated**: 2025-10-23  
**Compatible with**: Unity 6000.2.3f1 (Unity 6 LTS)
