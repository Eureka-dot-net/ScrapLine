# Unity Frontend Integration Guide - Waste Crate System

This guide provides step-by-step instructions for integrating the waste crate system into the Unity frontend.

## Overview
The waste crate system consists of:
- 4 different waste crates with increasing costs
- Queue management system (1 crate queue limit initially)
- Interactive UI that opens when clicking spawner machines
- Purchase validation and credit integration

## Required Unity Setup Steps

### 1. Create Sprite Assets

First, add the waste crate sprites to the project:

1. Create folder: `Assets/Resources/Sprites/waste/`
2. Add these sprite files (you mentioned you'll work on these):
   - `waste_1.png` - Starter Waste Crate sprite
   - `waste_2.png` - Medium Waste Crate sprite  
   - `waste_3.png` - Large Waste Crate sprite
   - `waste_4.png` - Premium Waste Crate sprite

3. Import settings for each sprite:
   - **Texture Type**: Sprite (2D and UI)
   - **Sprite Mode**: Single
   - **Pixels Per Unit**: 100 (or match your existing UI sprites)
   - **Filter Mode**: Bilinear
   - **Max Size**: 512 (or appropriate size)

### 2. Create UI Prefabs

#### 2.1 Main Waste Crate Panel Prefab

1. **Create the main panel**:
   - Right-click in Project → Create → UI → Panel
   - Name: `WasteCratePanel`
   - Set Canvas Group component (for fade in/out)

2. **Panel structure**:
   ```
   WasteCratePanel (Canvas Group + Image)
   ├── Header
   │   ├── Title Text (TextMeshPro) - "Waste Crate Management"
   │   └── Close Button (Button + Image + Text)
   ├── CurrentCratePanel (Image background)
   │   ├── CrateName (TextMeshPro) - "Current: [Crate Name]"
   │   ├── FullnessText (TextMeshPro) - "75/100 items"
   │   ├── ProgressBar (Slider)
   │   │   ├── Background (Image)
   │   │   └── Fill Area
   │   │       └── Fill (Image)
   │   └── QueueStatus (TextMeshPro) - "Queue: Empty (0/1)"
   ├── ButtonsPanel
   │   └── BuyButton (Button + Image + TextMeshPro) - "Buy Crate"
   └── PurchasePanel (Initially inactive)
       ├── PurchaseTitle (TextMeshPro) - "Select Crate to Purchase"
       ├── PurchaseOptionsGrid (Grid Layout Group)
       └── BackButton (Button) - "Back"
   ```

#### 2.2 Purchase Option Prefab

1. **Create purchase option button**:
   - Right-click in Project → Create → UI → Button - TextMeshPro
   - Name: `PurchaseOptionPrefab`

2. **Structure**:
   ```
   PurchaseOptionPrefab (Button + Image)
   ├── CrateIcon (Image) - For waste crate sprite
   ├── CrateName (TextMeshPro) - "Medium Waste Crate"
   ├── CrateDescription (TextMeshPro) - "150 items total"
   └── CratePrice (TextMeshPro) - "375 credits"
   ```

3. **Button styling**:
   - Normal Color: Light gray
   - Highlighted Color: Light blue
   - Pressed Color: Dark blue
   - Disabled Color: Dark gray

### 3. Scene Integration

#### 3.1 Add WasteCrateUI Component

1. **Find the GameManager**:
   - Open `MobileGridScene.unity` (main scene)
   - Locate the GameManager GameObject

2. **Add WasteCrateUI**:
   - Create empty GameObject named "WasteCrateUIManager"
   - Add Component → Scripts → WasteCrateUI
   - Position in UI Canvas hierarchy

#### 3.2 Wire Component References

In the WasteCrateUI component inspector, assign these references:

**UI References:**
- `Waste Crate Panel`: The main WasteCratePanel prefab instance
- `Current Crate Panel`: The CurrentCratePanel child object
- `Purchase Panel`: The PurchasePanel child object

**Current Crate Display:**
- `Current Crate Name Text`: TextMeshPro showing crate name
- `Current Crate Fullness Text`: TextMeshPro showing "X/Y items"
- `Current Crate Progress Bar`: Slider component for fullness
- `Queue Status Text`: TextMeshPro showing queue status

**Purchase Options:**
- `Buy Button`: Button to open purchase menu
- `Purchase Options Parent`: Grid Layout Group container
- `Purchase Option Prefab`: The PurchaseOptionPrefab you created

**Close Controls:**
- `Close Button`: Button to close the menu

#### 3.3 Connect to MachineManager

1. **Find MachineManager**:
   - In MobileGridScene, locate the MachineManager component
   - This is likely on the GameManager GameObject

2. **Assign WasteCrateUI Reference**:
   - In MachineManager inspector, find "Waste Crate UI" field
   - Drag the WasteCrateUIManager GameObject to this field

### 4. UI Layout Configuration

#### 4.1 Main Panel Settings

- **Canvas Group**: Alpha = 1, Interactable = true, Blocks Raycasts = true
- **Position**: Center of screen
- **Size**: Recommended 400x600 pixels
- **Background**: Semi-transparent dark color (0, 0, 0, 180)

#### 4.2 Progress Bar Setup

- **Slider Settings**:
  - Min Value: 0
  - Max Value: 1
  - Whole Numbers: false
  - Fill Rect: Green to red gradient based on fullness

#### 4.3 Grid Layout for Purchase Options

- **Grid Layout Group**:
  - Cell Size: 180x120
  - Spacing: 10x10
  - Start Corner: Upper Left
  - Start Axis: Horizontal
  - Child Alignment: Upper Left
  - Constraint: Fixed Column Count = 2

### 5. Animation & Polish (Optional)

#### 5.1 Panel Animations

1. **Fade In Animation**:
   - Create Animator Controller
   - Add fade in transition using Canvas Group alpha
   - Duration: 0.3 seconds

2. **Scale Animation**:
   - Start scale: 0.8
   - End scale: 1.0
   - Ease out curve

#### 5.2 Button Hover Effects

- Use UI Button Transition → Animation
- Scale slightly on hover (1.0 → 1.05)
- Add subtle color tint changes

### 6. Testing Checklist

After integration, test these scenarios:

#### 6.1 Basic Functionality
- [ ] Click spawner machine → Waste crate menu opens
- [ ] Current crate info displays correctly
- [ ] Progress bar shows proper fullness percentage
- [ ] Queue status shows "Empty (0/1)" initially

#### 6.2 Purchase Flow
- [ ] Click "Buy Crate" → Purchase panel opens
- [ ] All 4 crate options display with correct sprites
- [ ] Prices match the JSON definitions (250, 375, 562, 937)
- [ ] Click crate option → Purchase confirmation works
- [ ] Credits deduct properly after purchase
- [ ] Queue updates to show purchased crate

#### 6.3 Queue Management
- [ ] Queue shows "1/1" when full
- [ ] Buy button disables when queue is full
- [ ] Buy button disables when insufficient credits
- [ ] When current crate empties → Queue item moves to current

#### 6.4 Save/Load
- [ ] Queue state persists through save/load
- [ ] Current crate state persists
- [ ] Credit amounts persist

### 7. Common Issues & Troubleshooting

#### 7.1 Menu Doesn't Open
- Verify MachineManager.wasteCrateUI is assigned
- Check that spawner machine ID is "spawner"
- Ensure WasteCratePanel starts inactive

#### 7.2 Purchase Options Don't Show
- Verify PurchaseOptionPrefab is assigned
- Check that FactoryRegistry loads wastecrates.json
- Ensure Grid Layout Group is configured correctly

#### 7.3 Progress Bar Not Updating
- Verify Slider component references
- Check that SpawnerMachine methods are public
- Ensure GetTotalItemsInWasteCrate() returns correct values

#### 7.4 Sprites Not Loading
- Confirm sprites are in Resources/Sprites/waste/ folder
- Check that sprite names match JSON definitions exactly
- Verify import settings are correct

### 8. Advanced Customization

#### 8.1 Custom Styling
- Modify colors to match game theme
- Add background patterns or textures
- Implement custom fonts for text elements

#### 8.2 Enhanced Animations
- Add particle effects when purchasing
- Implement smooth transitions between panels
- Add sound effects for button clicks

#### 8.3 Mobile Optimization
- Ensure UI scales properly on different screen sizes
- Test touch interactions on mobile devices
- Optimize for different aspect ratios

## File References

The system integrates with these existing files:
- `MachineManager.cs` - Handles spawner clicks
- `GameManager.cs` - Manages purchases and credits
- `SpawnerMachine.cs` - Tracks crate status and queue
- `wastecrates.json` - Defines the 4 crate types
- `WasteCrateUI.cs` - Main UI controller (new file)

## Summary

Once completed, players will be able to:
1. Click any spawner to open the waste crate management UI
2. See their current crate status and remaining items
3. View their purchase queue (initially limited to 1 crate)
4. Purchase new crates using credits
5. Watch automatic queue processing when crates empty

The system is fully integrated with save/load functionality and follows the existing UI patterns in the game.