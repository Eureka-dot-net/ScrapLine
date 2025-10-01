# Recipe Ingredient Display UI Enhancement - Frontend Implementation Guide

## ⚠️ UNIFIED ARCHITECTURE: Both Panels Use RecipeIngredientDisplay

**IMPORTANT UPDATE**: As of the latest changes, both panels now use the same standardized approach:

- **Config Panel**: Uses `RecipeIngredientDisplay` component to display a single recipe
- **Selection Panel**: Uses `RecipeIngredientDisplay` component to create a row for EACH recipe

**Standardization Benefits**:
- Single source of truth for recipe display logic
- Consistent behavior between panels
- Easier maintenance and debugging
- Simpler Unity setup

## Overview
This guide covers the Unity frontend setup required to implement recipe ingredient display functionality in fabricator machine UI configuration and selection panels.

**KEY DESIGN**: Both panels now use `RecipeIngredientDisplay` component for consistency. The selection panel creates multiple instances (one per recipe), while the config panel uses a single instance.

## Debugging Issues

**MOST COMMON CAUSE**: ingredientDisplayRow prefab not assigned in RecipeSelectionPanel inspector

Other possible causes - enable Unity logging and check console:
- "ingredientDisplayRow is null" → Assign the RecipeIngredientDisplay prefab in RecipeSelectionPanel inspector
- "No RecipeIngredientDisplay component found" → Ensure the prefab has RecipeIngredientDisplay component
- "Failed to load sprite from path" → Check sprite paths in items.json
- "No Image component found" → Ensure ingredientPrefab has child named "ItemIcon" with Image component
- "No TextMeshProUGUI component found" → Ensure ingredientPrefab has child named "CountText" with TextMeshProUGUI component
- Check that FactoryRegistry has loaded recipe data correctly

**To fix issues:**
1. Verify RecipeSelectionPanel has ingredientDisplayRow assigned
2. Verify the assigned prefab has a RecipeIngredientDisplay component
3. Verify RecipeIngredientDisplay has ingredientContainer and ingredientPrefab assigned
4. Test again

## New Components Added

### 1. RecipeIngredientDisplay Component

**Location**: `Assets/Scripts/UI/RecipeIngredientDisplay.cs`

**Purpose**: Reusable UI component that displays recipe ingredients with icons and counts (e.g., 3x[can_icon] + 2x[bottle_icon]).

**Features**:
- Supports up to 3 different ingredient types (as per game design constraint)
- Displays individual icons for small quantities (≤5 items)
- Shows single icon with count text for larger quantities (>5 items)
- Optional arrow display for recipe flow visualization
- Handles missing item definitions gracefully
- Same component works for both vertical (config) and horizontal (selection) layouts

## Unity Setup Requirements

### 1. Create Ingredient Display Prefabs

#### 1.1. Ingredient Item Prefab (For Both Layouts)
```
Create GameObject: "IngredientItemPrefab"
├── Add Component: HorizontalLayoutGroup
│   ├── Spacing: 5
│   ├── Child Force Expand: false
│   └── Child Control Size: true
├── Add Component: Content Size Fitter
│   ├── Horizontal Fit: Preferred Size
│   └── Vertical Fit: Preferred Size
├── Create Child: "CountText" ⚠️ IMPORTANT: This child MUST be named exactly "CountText"
│   └── Add Component: TextMeshProUGUI
│       ├── Text: "1 x" (placeholder, set at runtime)
│       ├── Font Size: 14
│       ├── Alignment: Middle Left
│       ├── Color: White
│       └── Best Fit: unchecked
└── Create Child: "ItemIcon" ⚠️ IMPORTANT: This child MUST be named exactly "ItemIcon"
    └── Add Component: Image (for item sprite)
        ├── Sprite: None (set at runtime)
        ├── Color: White
        ├── Preserve Aspect: true
        ├── RectTransform Size: 32x32 (for config panel) or 24x24 (for selection panel)
        └── Native Size: false
```

**⚠️ CRITICAL NAMING REQUIREMENTS**:
- The count text child MUST be named exactly "CountText" (code searches by name)
- The icon image child MUST be named exactly "ItemIcon" (code searches by name)
- These names are case-sensitive and must match exactly
- If names don't match, you'll see "1 x" text but no icons!

**Layout Structure**: The ingredient item should have the text BEFORE the icon in hierarchy for proper "Count x [Icon]" display.

#### 1.2. Spacer Prefab (Optional)
```
Create GameObject: "SpacerPrefab"
└── Add Component: LayoutElement
    ├── Min Height: 10
    ├── Preferred Height: 10
    └── Flexible Height: 0
```

### 2. Update FabricatorMachineConfigPanel

#### 2.1. Panel Structure Enhancement - VERTICAL LAYOUT
```
FabricatorConfigPanel
├── Background (Image)
├── Header
│   └── Title (TextMeshProUGUI - "Configure Fabricator")
├── RecipeConfigSection
│   ├── RecipeConfigButton (Button)
│   │   ├── Background (Image - for output item sprite)
│   │   └── Label (TextMeshProUGUI - for output item name)
│   └── IngredientDisplayContainer (NEW - VERTICAL)
│       ├── Add Component: RecipeIngredientDisplay
│       ├── Add Component: VerticalLayoutGroup (NOT Horizontal!)
│       │   ├── Spacing: 5
│       │   ├── Child Force Expand Width: false
│       │   ├── Child Force Expand Height: false
│       │   ├── Child Control Size: true
│       │   └── Child Alignment: Upper Left
│       ├── Add Component: Content Size Fitter
│       │   ├── Horizontal Fit: Preferred Size
│       │   └── Vertical Fit: Preferred Size
│       ├── Assign ingredientContainer: self (the transform of this GameObject)
│       ├── Assign ingredientPrefab: IngredientItemPrefab
│       ├── Assign spacerPrefab: SpacerPrefab (optional)
│       ├── iconSize: (32, 32)
│       ├── fontSize: 14
│       ├── maxIconsPerIngredient: 5
│       ├── useVerticalLayout: TRUE (IMPORTANT!)
│       └── showArrow: false
├── RecipeSelectionPanel (existing)
└── ActionButtonsContainer
    ├── ConfirmButton
    └── CancelButton
```

**CRITICAL SETTINGS**:
- Use **VerticalLayoutGroup** (not HorizontalLayoutGroup) for the config panel
- Set **useVerticalLayout = TRUE** in RecipeIngredientDisplay component
- This will display ingredients vertically with automatic spacers

#### 2.2. Inspector Configuration
```
FabricatorMachineConfigPanel Component:
├── Base Config Panel (inherited)
│   ├── configPanel: FabricatorConfigPanel
│   ├── confirmButton: ConfirmButton
│   └── cancelButton: CancelButton
└── Fabricator Specific (NEW)
    ├── recipeConfigButton: RecipeConfigButton
    ├── recipeSelectionPanel: RecipeSelectionPanel
    └── ingredientDisplay: IngredientDisplayContainer (NEW)
```

### 3. RecipeSelectionPanel Setup (Unified Approach)

**UPDATED STANDARDIZED APPROACH**: RecipeSelectionPanel now uses RecipeIngredientDisplay component (same as FabricatorMachineConfigPanel) for consistency!

#### 3.1. Create RecipeIngredientDisplay Prefab (Row Container)

This is the same RecipeIngredientDisplay used in the config panel, but configured for horizontal layout:

```
Create GameObject: "RecipeIngredientDisplayRow"
├── Add Component: Button (REQUIRED - makes row clickable)
│   └── Navigation: None (to prevent interference with touch)
├── Add Component: RecipeIngredientDisplay (REQUIRED)
│   ├── ingredientContainer: IngredientContainer child (see below)
│   ├── ingredientPrefab: IngredientItemPrefab
│   ├── iconSize: (24, 24) or (32, 32)
│   ├── fontSize: 12
│   ├── maxIconsPerIngredient: 5
│   ├── useVerticalLayout: FALSE (horizontal for selection panel)
│   └── showArrow: true (optional - shows arrow between inputs and output)
├── Create Child: "IngredientContainer"
│   ├── Add Component: HorizontalLayoutGroup
│   │   ├── Spacing: 8-10
│   │   ├── Child Force Expand: false
│   │   ├── Child Control Size: true (width and height)
│   │   ├── Child Alignment: Middle Left
│   │   └── Padding: Left/Right 10-15, Top/Bottom 5-8
│   └── Add Component: Content Size Fitter
│       ├── Horizontal Fit: Preferred Size
│       └── Vertical Fit: Preferred Size
└── Add Component: Image (optional background for row)
    ├── Color: Semi-transparent or themed color
    └── Material: None
```

**⚠️ IMPORTANT CHANGES**: 
- This prefab DOES have RecipeIngredientDisplay component (unified approach!)
- Set `useVerticalLayout = FALSE` for horizontal layout
- Set `showArrow = TRUE` to show arrow between ingredients and output
- The Button component MUST be on the root GameObject
- Child UI elements will have raycastTarget disabled automatically (no manual setup needed)

**⚠️ RAYCAST BLOCKING FIX (AUTOMATIC)**:
- RecipeSelectionPanel automatically disables `raycastTarget` on all child Images, Text, and TextMeshPro components
- This prevents child elements from blocking button clicks
- No manual configuration needed in the prefab
- The fix is applied at runtime when buttons are created

#### 3.2. RecipeSelectionPanel Inspector Configuration

```
RecipeSelectionPanel Component:
├── Base Selection Panel (inherited)
│   ├── selectionPanel: RecipeSelectionPanel GameObject
│   ├── buttonContainer: Content/ScrollView (Grid Layout Group)
│   └── buttonPrefab: (LEAVE EMPTY - not used)
└── Recipe Selection Specific
    ├── machineId: "" (set at runtime)
    ├── ingredientDisplayRow: RecipeIngredientDisplayRow prefab (NEW!)
    └── emptyRowPrefab: (OPTIONAL) Prefab for "No Recipe" option
```

**NEW: Empty Row Prefab (Optional)**
- If you want a custom appearance for the "No Recipe" option, create a separate prefab
- Should have a Button component on root GameObject
- Can be a simple button with text "No Recipe" or custom styling
- If not set, the system uses ingredientDisplayRow for the empty option

#### 3.3. Important: Button Clickability

**The RecipeSelectionPanel automatically handles button clickability:**
- Child UI elements (Images, Text, TextMeshPro) have `raycastTarget` disabled automatically
- This prevents them from blocking clicks to the parent Button component
- No manual configuration needed in prefabs
- The Button component MUST be on the root GameObject of the prefab

**If buttons are not clickable, verify:**
1. Button component is on the root GameObject (not a child)
2. The ingredientDisplayRow prefab is properly assigned in inspector
3. EventSystem exists in the scene (Unity requirement for UI interaction)

#### 3.4. How It Works

**For each recipe in the list, RecipeSelectionPanel:**
1. Instantiates ONE `RecipeIngredientDisplayRow` prefab (the row with Button + RecipeIngredientDisplay)
2. Calls `DisplayRecipe(recipe)` on the RecipeIngredientDisplay component
3. RecipeIngredientDisplay automatically creates ingredient items inside its container

**Visual Result:**
```
Recipe 1 Row (RecipeIngredientDisplay with Button):
  Ingredient1 [icon] + Ingredient2 [icon] → Result [icon]

Recipe 2 Row (RecipeIngredientDisplay with Button):
  Ingredient1 [icon] + Ingredient2 [icon] + Ingredient3 [icon] → Result [icon]

Recipe 3 Row (RecipeIngredientDisplay with Button):
  Ingredient1 [icon] → Result [icon]
```

**User clicks entire row** → Recipe is selected

#### 3.4. Key Advantages

- **Unified architecture**: SAME RecipeIngredientDisplay component used in both panels
- **Single source of truth**: Recipe display logic is centralized
- **Easier maintenance**: Changes to recipe display apply to both panels
- **Simpler code**: RecipeSelectionPanel is ~120 lines shorter
- **Consistent behavior**: Both panels handle recipes identically
- **Flexible**: Easy to switch between vertical and horizontal layouts

#### 3.5. Panel Structure

```
RecipeSelectionPanel
├── Background (Image - optional)
├── ScrollView
│   └── Content (Grid Layout Group)
│       ├── Recipe1Row (RecipeIngredientDisplayRow instance with Button)
│       │   └── IngredientContainer (child with HorizontalLayoutGroup)
│       │       ├── Ingredient1 (IngredientItemPrefab - auto-created by RecipeIngredientDisplay)
│       │       ├── Ingredient2 (IngredientItemPrefab - auto-created by RecipeIngredientDisplay)
│       │       ├── Arrow (optional - auto-created if showArrow = true)
│       │       └── Output (IngredientItemPrefab - auto-created by RecipeIngredientDisplay)
│       ├── Recipe2Row (RecipeIngredientDisplayRow instance with Button)
│       │   └── IngredientContainer (child with HorizontalLayoutGroup)
│       │       ├── Ingredient1 (IngredientItemPrefab - auto-created)
│       │       ├── Arrow (optional)
│       │       └── Output (IngredientItemPrefab - auto-created)
│       └── ... (one row per recipe - all auto-created by RecipeIngredientDisplay)
└── ActionButtons (optional)
```

**Key Points:**
- Each row is a complete RecipeIngredientDisplayRow prefab instance
- RecipeIngredientDisplay component automatically creates all ingredient items
- No manual item creation needed - just call DisplayRecipe()
- Same component used as in FabricatorMachineConfigPanel (unified approach)
│       │   └── Output (PanelIngredientItemPrefab instance)
│       ├── Recipe2Row (IngredientDisplayContainer instance)
│       │   ├── Ingredient1 (PanelIngredientItemPrefab instance)
│       │   └── Output (PanelIngredientItemPrefab instance)
│       └── ... (one row per recipe)
└── ActionButtons (optional)
```

## Asset Requirements

### Sprites Needed
- **Arrow Sprite** (optional): For showing recipe flow (ingredients → output)
  - Recommended size: 16x16 or 24x24 pixels
  - Format: PNG with transparency
  - Color: White (will be tinted at runtime)

### Prefab Hierarchy
```
Assets/Prefabs/UI/
├── IngredientItemPrefab.prefab (NEW)
├── FabricatorConfigPanel.prefab (UPDATED)
└── RecipeSelectionPanel.prefab (UPDATED)
```

## Layout Issues and Solutions

### Vertical vs Horizontal Layout Modes

**The RecipeIngredientDisplay component supports TWO layout modes:**

#### Vertical Layout (Config Panel)
**When to use**: Fabricator Machine Configuration Panel
**Setting**: `useVerticalLayout = TRUE`

**Display Format:**
```
Single ingredient recipe (e.g., 1 can):
  [Blank Spacer]
  1 x [can_icon]
  [Blank Spacer]

Multiple ingredient recipe (e.g., 2 cans + 3 bottles):
  2 x [can_icon]
  3 x [bottle_icon]
  [Blank Spacer]
```

**Key Features:**
- Each ingredient on its own line
- Always shows count with format "N x [icon]"
- Automatic spacer logic:
  - Single ingredient: spacer above AND below
  - Multiple ingredients: spacer below only
- Uses VerticalLayoutGroup on the container

#### Horizontal Layout (Selection Panel)
**When to use**: Recipe Selection Panel buttons
**Setting**: `useVerticalLayout = FALSE` (default)

**Display Format:**
```
Shows all ingredients side-by-side: [icon][icon][icon] or 3x[icon] + 2x[icon]
```

**Key Features:**
- Multiple icons for small counts (≤5)
- Single icon with "Nx" text for large counts
- Optional arrow for recipe flow
- Uses HorizontalLayoutGroup on the container

### Common Layout Problems

**Problem 1: Icons appear too large or in wrong positions**
- **Solution**: Set explicit iconSize in RecipeIngredientDisplay (default: 32x32)
- **Unity Setup**: Ensure Image components have correct RectTransform sizing
- **Content Size Fitter**: Use Preferred Size for both horizontal and vertical

**Problem 2: Ingredient display doesn't fit in panel**
- **Solution**: Add Content Size Fitter to ingredient container
- **Layout Group**: Configure VerticalLayoutGroup (config panel) or HorizontalLayoutGroup (selection panel) with appropriate spacing
- **Parent Constraints**: Ensure parent panel can accommodate the ingredient display

**Problem 3: Text is too small or incorrectly positioned**
- **Solution**: Use TextMeshProUGUI with appropriate fontSize (12-16 recommended)
- **Text Placement**: In IngredientItemPrefab, place CountText BEFORE ItemIcon in hierarchy
- **Anchoring**: Properly anchor text relative to icon using HorizontalLayoutGroup on the prefab

**Problem 4: Ingredients showing horizontally instead of vertically in config panel**
- **Solution**: 
  1. Change container's LayoutGroup from HorizontalLayoutGroup to VerticalLayoutGroup
  2. Set `useVerticalLayout = TRUE` in RecipeIngredientDisplay component
  3. Verify spacerPrefab is assigned (optional but recommended)

**Problem 5: Seeing "1 x" text but no icons (MOST COMMON)**
- **Root Cause**: Prefab child objects are not named correctly
- **Solution**: 
  1. Open IngredientItemPrefab
  2. Ensure text child is named exactly "CountText" (case-sensitive!)
  3. Ensure image child is named exactly "ItemIcon" (case-sensitive!)
  4. Code searches for children by these exact names
- **Why this happens**: If names don't match, code finds the TextMeshProUGUI component (showing "1 x") but can't find the Image component (no icons)
- **Verification**: 
  - Check Unity Console for warnings: "No Image component found in ingredient prefab"
  - Verify hierarchy structure matches the required naming convention
  - Use "Find References in Scene" to ensure correct prefab is assigned

**Problem 6: RecipeSelectionPanel showing individual items instead of recipe rows**
- **Symptom**: Grid shows individual items instead of complete recipe rows
- **Root Cause**: Wrong prefab assigned to `ingredientDisplayRow` field or missing RecipeIngredientDisplay component
- **UPDATED SOLUTION (unified approach)**: 
  1. Assign a RecipeIngredientDisplay prefab to `ingredientDisplayRow` field
  2. Make sure this prefab has:
     - Button component at root (makes it clickable)
     - RecipeIngredientDisplay component (REQUIRED)
     - ingredientContainer child with HorizontalLayoutGroup
     - `useVerticalLayout = FALSE` for selection panel
     - `showArrow = TRUE` for "inputs → output" display (optional)
     - `ingredientPrefab` set to IngredientItemPrefab (the small "1 x [icon]" prefab)
  3. The prefab becomes a clickable recipe row that the panel duplicates
- **Why this works**: 
  - Config panel: Uses RecipeIngredientDisplay with vertical layout (shows one recipe)
  - Selection panel: Uses RecipeIngredientDisplay with horizontal layout (shows multiple recipe rows)
  - SAME component, different layout configuration!
- **Key Fields**:
  - `ingredientDisplayRow` in RecipeSelectionPanel = The RecipeIngredientDisplay prefab
  - `ingredientPrefab` INSIDE RecipeIngredientDisplay component = The small item prefab ("1 x [icon]")
  - These are TWO DIFFERENT prefabs at different levels!

### Responsive Design Configuration

#### For Vertical Layout (Config Panel):
```
IngredientDisplayContainer Settings:
├── VerticalLayoutGroup
│   ├── Padding: Left=4, Right=4, Top=2, Bottom=2
│   ├── Spacing: 5
│   ├── Child Alignment: Upper Left
│   ├── Child Force Expand Width: false
│   ├── Child Force Expand Height: false
│   ├── Child Control Width: true
│   └── Child Control Height: true
├── Content Size Fitter
│   ├── Horizontal Fit: Preferred Size
│   └── Vertical Fit: Preferred Size
└── RecipeIngredientDisplay
    ├── useVerticalLayout: TRUE
    ├── iconSize: (32, 32) for mobile
    ├── fontSize: 14
    └── maxIconsPerIngredient: N/A (not used in vertical mode)
```

#### For Horizontal Layout (Selection Panel):
```
IngredientDisplayContainer Settings:
├── HorizontalLayoutGroup
│   ├── Padding: Left=4, Right=4, Top=2, Bottom=2
│   ├── Spacing: 8
│   ├── Child Alignment: Middle Left
│   ├── Child Force Expand Width: false
│   ├── Child Force Expand Height: false
│   ├── Child Control Width: true
│   └── Child Control Height: true
├── Content Size Fitter
│   ├── Horizontal Fit: Preferred Size
│   └── Vertical Fit: Preferred Size
└── RecipeIngredientDisplay
    ├── useVerticalLayout: FALSE
    ├── iconSize: (24, 24) for compact layouts
    ├── fontSize: 12
    ├── maxIconsPerIngredient: 5 (show individual icons up to 5)
    └── showArrow: true (optional)
```

## Visual Design Recommendations

### Display Format Examples

#### Config Panel (Vertical Layout):
```
Recipe with 2 ingredients (2 cans + 3 bottles):
┌─────────────────┐
│ [Blank Space]   │  ← No spacer (multiple ingredients)
│ 2 x [🥫]       │  ← Ingredient 1
│ 3 x [🍾]       │  ← Ingredient 2
│ [Blank Space]   │  ← Bottom spacer
└─────────────────┘

Recipe with 1 ingredient (1 can):
┌─────────────────┐
│ [Blank Space]   │  ← Top spacer (single ingredient)
│ 1 x [🥫]       │  ← Ingredient
│ [Blank Space]   │  ← Bottom spacer
└─────────────────┘
```

#### Selection Panel (Horizontal Layout):
```
Small count: [🥫][🥫] + [🍾][🍾][🍾] → [📦]
Large count: 10x[🥫] + 5x[🍾] → [📦]
```

### Color Scheme
- **Ingredient Icons**: Use original item sprite colors
- **Count Text**: White text with subtle shadow/outline for readability
- **Background**: Maintain existing panel styling
- **Spacers**: Transparent or match panel background

### Animation (Future Enhancement)
- Consider subtle fade-in for ingredient displays
- Possible hover effects for ingredient tooltips
- Smooth transitions when switching recipes

## Troubleshooting

### Selection Panel Not Showing Recipe Ingredients

**Symptom**: Recipe selection grid shows empty buttons or no ingredient icons

**Solution**: 
1. Ensure button prefab has `IngredientDisplayContainer` child GameObject
2. Add `RecipeIngredientDisplay` component to IngredientDisplayContainer
3. Configure with `HorizontalLayoutGroup` and `useVerticalLayout = FALSE`
4. Assign `ingredientPrefab` field (same as config panel uses)
5. Set `showArrow = TRUE` to show "→" between inputs and output

**Common Issues**:
- Button prefab missing IngredientDisplayContainer child → Add it as child GameObject
- RecipeIngredientDisplay not configured → Add component and set properties
- Missing ingredientPrefab reference → Assign the IngredientItemPrefab
- Icons not appearing → Check iconSize (recommended 24x24) and ingredientPrefab setup

### Config Panel vs Selection Panel - Setup Differences

| Feature | Config Panel | Selection Panel |
|---------|-------------|-----------------|
| **Layout Direction** | Vertical (stacked) | Horizontal (side-by-side) |
| **useVerticalLayout** | TRUE | FALSE |
| **Layout Component** | VerticalLayoutGroup | HorizontalLayoutGroup |
| **Display Format** | "N x [icon]" on lines | "[icon] + [icon] → [icon]" |
| **Spacers** | Yes (top/bottom) | No |
| **Icon Size** | 32x32 (touch-friendly) | 24x24 (compact) |
| **Display Location** | Separate container on panel | Inside each button prefab |
| **Component Reuse** | Same IngredientItemPrefab | Same IngredientItemPrefab |
| **When Populated** | When recipe selected | When buttons created |
| **User Interaction** | View detail of ONE recipe | Compare MANY recipes at once |

**Key Insight**: Both use RecipeIngredientDisplay component and same IngredientItemPrefab, just different layout configurations and locations!

## Implementation Steps

### Phase 1: Basic Setup - Config Panel (Priority)
1. Create IngredientItemPrefab with HorizontalLayoutGroup, CountText, and ItemIcon
2. Create SpacerPrefab (optional but recommended)
3. Add VerticalLayoutGroup to IngredientDisplayContainer in FabricatorConfigPanel
4. Add RecipeIngredientDisplay component and configure:
   - Set `useVerticalLayout = TRUE`
   - Assign ingredientPrefab and spacerPrefab
   - Set iconSize to (32, 32)
   - Set fontSize to 14
5. Test with recipes having 1, 2, and 3 ingredients

### Phase 2: Selection Panel Setup (Per-Button Ingredient Display)
1. Create RecipeSelectionButtonPrefab with Button component
2. Add child GameObject "IngredientDisplayContainer" to button
3. Add RecipeIngredientDisplay component to container with `useVerticalLayout = FALSE`
4. Add HorizontalLayoutGroup to container (spacing: 5-8)
5. Assign same IngredientItemPrefab as config panel
6. Set showArrow = TRUE for "inputs → output" format
7. Assign button prefab to RecipeSelectionPanel
4. Fine-tune visual appearance

### Phase 3: Advanced Features (Optional)
1. Add ingredient tooltips on hover/touch
2. Implement smooth animations
3. Add accessibility features
4. Performance optimization for complex recipes

## Testing Checklist

### Functional Testing
- [ ] Simple recipes (1 ingredient) display correctly
- [ ] Complex recipes (2-3 ingredients) display correctly  
- [ ] High quantity ingredients (>5) show count text
- [ ] Missing item definitions handled gracefully
- [ ] Panel resizing works with ingredient displays

### Visual Testing
- [ ] Ingredient icons are clearly visible
- [ ] Count text is readable
- [ ] Layout doesn't break with long recipe names
- [ ] Mobile interface remains touch-friendly
- [ ] Arrow positioning (if used) looks natural

### Integration Testing
- [ ] Recipe selection updates ingredient display
- [ ] Configuration saving/loading preserves ingredient info
- [ ] Panel opening/closing animations work smoothly
- [ ] No performance impact with multiple panels

## Troubleshooting

### Common Issues

#### Issue 1: Ingredient Icons Too Large or Mispositioned
**Symptoms**: Icons appear oversized or in wrong locations (as seen in screenshot)
**Solution**:
```
1. Set RecipeIngredientDisplay.iconSize to (32, 32) or smaller
2. Ensure IngredientItemPrefab Image has RectTransform:
   - Width: 32, Height: 32
   - Anchors: Center
   - Pivot: Center (0.5, 0.5)
3. Add Content Size Fitter to ingredient container:
   - Horizontal Fit: Preferred Size
   - Vertical Fit: Preferred Size
```

#### Issue 2: Layout Breaking with Multiple Ingredients
**Symptoms**: Ingredients overflow or overlap
**Solution**:
```
1. Configure HorizontalLayoutGroup properly:
   - Spacing: 8
   - Child Force Expand Width: false
   - Child Control Width: true
2. Set maxIconsPerIngredient to 3-4 for limited space
3. Ensure parent container has adequate width
```

#### Issue 3: TextMeshPro Components Missing
**Symptoms**: Count text not displaying or compilation errors
**Solution**:
```
1. Import TextMeshPro package in Unity (Window > TextMeshPro > Import)
2. Replace Text components with TextMeshProUGUI in prefabs
3. Update using statements in scripts to include TMPro
```

### Debug Features
- Enable logging in RecipeIngredientDisplay for troubleshooting
- Use `RecipeIngredientDisplay.GetIngredientsString()` for text-based debugging
- Check FactoryRegistry.Instance.Items for missing item definitions

## Code Integration Points

### Key Methods to Hook Into
- `FabricatorMachineConfigPanel.UpdateIngredientDisplay()`: Updates ingredient display when recipe changes
- `RecipeSelectionPanel.UpdateButtonIngredientDisplay()`: Updates individual selection buttons
- `RecipeIngredientDisplay.DisplayRecipe()`: Core display logic

### Event Flow
1. User selects recipe → `OnRecipeSelected()` called
2. Panel updates → `UpdateUIFromCurrentState()` → `UpdateIngredientDisplay()`
3. Ingredient display → `DisplayRecipe()` → Individual icons created
4. Visual update complete

This implementation provides a flexible, extensible system for displaying recipe ingredients while maintaining the existing UI architecture and mobile-first design philosophy.