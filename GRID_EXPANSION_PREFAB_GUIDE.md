# Grid Expansion Prefab Creation Guide

## Quick Reference for Creating Expansion UI Prefabs

This guide provides detailed instructions for creating the three prefab types needed for the grid expansion feature.

---

## Prefab 1: InlinePlusMarker

### Purpose
Small circular button that appears between grid rows and columns to indicate where expansions can occur.

### Visual Design
- **Shape**: Circular or rounded square
- **Size**: 40x40 to 48x48 pixels
- **Color**: Bright orange (#FF9933) or yellow
- **Icon**: Plus (+) symbol in center
- **Style**: Flat design with subtle shadow/glow

### Creation Steps

1. **Create the GameObject**:
   ```
   Right-click in Hierarchy → UI → Image
   Name: InlinePlusMarker
   ```

2. **Configure RectTransform**:
   - Width: 40
   - Height: 40
   - Pivot: (0.5, 0.5) - centered

3. **Configure Image Component**:
   - Source Image: Create or import a plus icon sprite
     - Resolution: 128x128 or higher for crisp scaling
     - Format: PNG with transparency
     - Import Settings: Sprite (2D and UI), Compression: None
   - Color: RGB(255, 153, 51) or #FF9933 (orange)
   - Image Type: Simple
   - Preserve Aspect: ✓

4. **Add Button Component**:
   ```
   Add Component → Button
   ```
   - Interactable: ✓
   - Transition: Color Tint
   - Normal Color: White (1, 1, 1, 1)
   - Highlighted Color: Light Yellow (1, 1, 0.8, 1)
   - Pressed Color: Dark Orange (0.8, 0.6, 0.2, 1)
   - Selected Color: White
   - Disabled Color: Gray (0.5, 0.5, 0.5, 0.5)
   - Color Multiplier: 1
   - Fade Duration: 0.1

5. **Add Canvas Group** (for script control):
   ```
   Add Component → Canvas Group
   ```
   - Alpha: 1
   - Interactable: ✓
   - Blocks Raycasts: ✓

6. **Optional: Add Outline** (for better visibility):
   ```
   Add Component → Outline
   ```
   - Effect Color: Black (0, 0, 0, 0.5) - 50% transparent
   - Effect Distance: (2, -2)
   - Use Graphic Alpha: ✓

7. **Save as Prefab**:
   - Drag from Hierarchy to Project
   - Location: `Assets/Prefabs/UI/InlinePlusMarker.prefab`
   - Delete from scene after creating prefab

---

## Prefab 2: EdgePlusMarker

### Purpose
Larger button that appears at the left and right edges of the grid to indicate column expansion at edges.

### Visual Design
- **Shape**: Circular or rounded square (same as inline but larger)
- **Size**: 48x48 to 56x56 pixels
- **Color**: Bright blue-green (#33CCCC) or similar distinct color
- **Icon**: Plus (+) with optional arrow pointing inward
- **Style**: Same as inline but more prominent

### Creation Steps

1. **Option A: Duplicate InlinePlusMarker**:
   - Right-click InlinePlusMarker prefab → Duplicate
   - Rename to: EdgePlusMarker
   - Open prefab for editing

2. **Option B: Create from scratch**:
   - Follow InlinePlusMarker steps 1-6
   - Apply changes below

3. **Modify for Edge Marker**:
   - **Size**: Change to 48x48 or 56x56
   - **Color**: Change to blue-green (#33CCCC) or keep orange
   - **Icon** (optional): Add directional arrow
     - Create child Image for arrow
     - Point arrow toward grid center

4. **Enhanced Styling** (optional):
   - Larger outline: Effect Distance (3, -3)
   - Add pulsing animation (see Animation section below)
   - Add glow effect using duplicate Image with blur

5. **Save as Prefab**:
   - Location: `Assets/Prefabs/UI/EdgePlusMarker.prefab`

---

## Prefab 3: ExpansionCostPrompt

### Purpose
Modal dialog that displays the expansion cost and allows player to confirm or cancel.

### Visual Design
- **Size**: 350x180 pixels (compact dialog)
- **Style**: Semi-transparent dark background with bright text
- **Layout**: Centered message with buttons at bottom

### Hierarchy Structure
```
ExpansionCostPrompt (Panel)
├── Background (Image)
├── CostText (TextMeshProUGUI)
├── ButtonContainer (Empty GameObject)
│   ├── ConfirmButton (Button)
│   │   └── ButtonText (TextMeshProUGUI)
│   └── CancelButton (Button)
│       └── ButtonText (TextMeshProUGUI)
```

### Creation Steps

#### Step 1: Create Main Panel

1. **Create Panel**:
   ```
   Right-click in Canvas → UI → Panel
   Name: ExpansionCostPrompt
   ```

2. **Configure RectTransform**:
   - Anchor: Center-center
   - Pivot: (0.5, 0.5)
   - Width: 350
   - Height: 180
   - Position: (0, 0, 0)

3. **Configure Background Image**:
   - Color: Dark semi-transparent
     - RGB: (0, 0, 0)
     - Alpha: 220 (about 86%)
   - Image Type: Sliced (if using 9-slice sprite)
   - Or: Simple with Source Image (rounded rectangle)

4. **Optional: Add Shadow/Border**:
   - Add Shadow component
   - Effect Color: Black (0, 0, 0, 0.8)
   - Effect Distance: (4, -4)

#### Step 2: Add Cost Text

1. **Create Text**:
   ```
   Right-click ExpansionCostPrompt → UI → Text - TextMeshPro
   Name: CostText
   ```

2. **Configure RectTransform**:
   - Anchor: Top-center
   - Pivot: (0.5, 1)
   - Width: 300
   - Height: 60
   - Position: (0, -20, 0) - 20px from top

3. **Configure TextMeshProUGUI**:
   - Text: "Expand here for 100 credits?" (placeholder)
   - Font: Use game's UI font
   - Font Size: 22
   - Color: White or bright color
   - Alignment: Center and Middle
   - Overflow: Ellipsis
   - Enable Auto Size: ✓ (optional)

#### Step 3: Add Button Container

1. **Create Container**:
   ```
   Right-click ExpansionCostPrompt → Create Empty
   Name: ButtonContainer
   ```

2. **Configure**:
   - Anchor: Bottom-center
   - Pivot: (0.5, 0)
   - Width: 300
   - Height: 50
   - Position: (0, 15, 0) - 15px from bottom

3. **Add Horizontal Layout Group** (optional):
   ```
   Add Component → Horizontal Layout Group
   ```
   - Spacing: 20
   - Child Alignment: Middle Center
   - Control Child Size: Width ✓, Height ✓
   - Child Force Expand: Width ✓, Height ✗

#### Step 4: Add Confirm Button

1. **Create Button**:
   ```
   Right-click ButtonContainer → UI → Button - TextMeshPro
   Name: ConfirmButton
   ```

2. **Configure RectTransform** (if not using layout group):
   - Anchor: Middle-left (or adjust based on layout)
   - Width: 130
   - Height: 45
   - Position: (-75, 0, 0)

3. **Configure Button**:
   - Target Graphic: Button's Image
   - Transition: Color Tint
   - Normal Color: Green (#4CAF50)
   - Highlighted Color: Light Green (#66BB6A)
   - Pressed Color: Dark Green (#388E3C)
   - Selected Color: Green
   - Disabled Color: Gray
   - Color Multiplier: 1
   - Fade Duration: 0.1

4. **Configure Button Text**:
   - Text: "Confirm" or "✓" (checkmark)
   - Font Size: 18
   - Color: White
   - Alignment: Center and Middle
   - Enable Auto Size: ✓

5. **Optional: Add Icon**:
   - Add child Image for checkmark icon
   - Place to left of text

#### Step 5: Add Cancel Button

1. **Create Button** (same as Confirm):
   ```
   Right-click ButtonContainer → UI → Button - TextMeshPro
   Name: CancelButton
   ```

2. **Configure RectTransform**:
   - Width: 130
   - Height: 45
   - Position: (75, 0, 0) - opposite side from Confirm

3. **Configure Button**:
   - Normal Color: Red (#F44336) or Gray (#757575)
   - Highlighted Color: Light Red/Gray
   - Pressed Color: Dark Red/Gray
   - Other settings same as Confirm

4. **Configure Button Text**:
   - Text: "Cancel" or "✕" (X mark)
   - Same text settings as Confirm

#### Step 6: Add ExpansionCostPrompt Script

1. **Add Component**:
   ```
   Select ExpansionCostPrompt
   Add Component → ExpansionCostPrompt
   ```

2. **Assign References** (drag from hierarchy):
   - Prompt Panel: ExpansionCostPrompt (itself)
   - Cost Text: CostText component
   - Confirm Button: ConfirmButton component
   - Cancel Button: CancelButton component

3. **Configure Text Format**:
   - Cost Text Format: `"Expand here for {0} credits?"`
   - Or customize: `"Add row/column for {0}?"`
   - The `{0}` will be replaced with actual cost

#### Step 7: Initial State and Save

1. **Set Initial State**:
   - Disable ExpansionCostPrompt GameObject
   - The script will enable it when needed

2. **Test Standalone** (before saving):
   - Temporarily enable the panel
   - Enter Play Mode
   - Check layout and sizing
   - Test button hover/click (won't work yet but visual feedback should)

3. **Save as Prefab** (optional):
   - Drag ExpansionCostPrompt to Project
   - Location: `Assets/Prefabs/UI/ExpansionCostPrompt.prefab`
   - You can either:
     - Keep in scene (direct reference)
     - Or delete from scene and instantiate from prefab

---

## Advanced Customization

### Animation: Pulsing Effect for Markers

Add a simple animation to make markers more noticeable:

1. **Create Animation**:
   - Select marker in scene (not prefab)
   - Window → Animation → Animation
   - Create New Clip: "MarkerPulse"

2. **Add Scale Animation**:
   - Frame 0: Scale (1, 1, 1)
   - Frame 30: Scale (1.1, 1.1, 1)
   - Frame 60: Scale (1, 1, 1)

3. **Configure Animator**:
   - Create Animator Controller: "MarkerAnimator"
   - Add MarkerPulse animation
   - Set to loop

4. **Apply to Prefab**:
   - Save animation to prefab
   - Or add at runtime via script

### Tooltip for Cost Display

Add a small tooltip that shows cost on hover:

1. **Create Tooltip Prefab**:
   - Small UI Panel
   - TextMeshProUGUI showing cost
   - Size: 80x30

2. **Styling**:
   - Dark background
   - Bright text
   - Small arrow pointing to marker (optional)

3. **Usage**:
   - Instantiate on marker hover
   - Position next to marker
   - Destroy on pointer exit

### Particle Effects for Expansion

Add visual feedback when grid expands:

1. **Create Particle System**:
   - Small burst of particles
   - Color: Match marker color
   - Duration: 0.5-1 second

2. **Trigger**:
   - Play at cell positions during expansion
   - Sync with animation timing

---

## Icon Asset Creation

### Plus Icon Sprite

If you don't have a plus icon sprite:

1. **Option A: Create in Photoshop/GIMP**:
   - New image: 128x128, transparent background
   - Draw plus symbol: 2 rectangles crossing
   - Center aligned
   - Add subtle gradient or shadow
   - Export as PNG

2. **Option B: Use free resources**:
   - Search for "plus icon png free"
   - Ensure license allows commercial use
   - Download high-resolution version
   - Import to Unity

3. **Import Settings**:
   - Texture Type: Sprite (2D and UI)
   - Sprite Mode: Single
   - Pixels Per Unit: 100
   - Filter Mode: Bilinear
   - Compression: None (for best quality)

### Arrow Icon for Edge Markers

1. **Create or find arrow sprite**:
   - Pointing right (will flip for left edge)
   - Same resolution as plus icon
   - Can be combined with plus icon

2. **Usage**:
   - Add as child Image to EdgePlusMarker
   - Position to left or right of plus
   - Flip for left edge: Scale.x = -1

---

## Testing Checklist for Prefabs

### InlinePlusMarker
- [ ] Displays correctly at 40x40 size
- [ ] Plus icon clearly visible
- [ ] Button hover effect works
- [ ] Color matches game theme
- [ ] No console errors when instantiated

### EdgePlusMarker
- [ ] Displays correctly at 48x48 size
- [ ] Distinct from inline markers
- [ ] Button hover effect works
- [ ] (Optional) Directional indicator visible

### ExpansionCostPrompt
- [ ] Centers correctly on screen
- [ ] Text displays placeholder correctly
- [ ] Buttons are clickable
- [ ] Button hover effects work
- [ ] Layout adapts if text is long
- [ ] No overlap between elements
- [ ] Background blocks clicks to grid

---

## Quick Copy Templates

### Marker Button Configuration
```
Button Component:
- Transition: Color Tint
- Normal: (1, 1, 1, 1)
- Highlighted: (1, 1, 0.8, 1)
- Pressed: (0.8, 0.6, 0.2, 1)
- Fade Duration: 0.1
```

### Cost Prompt Text Configuration
```
TextMeshProUGUI Component:
- Font Size: 22
- Color: White (1, 1, 1, 1)
- Alignment: Center, Middle
- Overflow: Ellipsis
- Auto Size: ✓
- Min: 14, Max: 22
```

### Button Text Configuration
```
TextMeshProUGUI Component:
- Font Size: 18
- Color: White (1, 1, 1, 1)
- Alignment: Center, Middle
- Enable Auto Size: ✓
- Min: 14, Max: 18
```

---

## Common Issues

### Markers too small/large on screen
- Adjust size in GridMarkersView inspector
- Or modify prefab RectTransform size
- Check Canvas Scaler settings

### Buttons not responding to clicks
- Ensure Canvas has GraphicRaycaster
- Check that Button.interactable is ✓
- Verify no other UI blocking (wrong sort order)

### Prompt doesn't center
- Check anchor is set to center-center
- Verify position is (0, 0, 0)
- Check Canvas Scaler reference resolution

### Text too small/large
- Adjust font size
- Enable Auto Sizing
- Check Canvas Scaler scale factor

---

**End of Prefab Guide**

For complete setup instructions, see: `EXPANDABLE_GRID_SETUP_GUIDE.md`
