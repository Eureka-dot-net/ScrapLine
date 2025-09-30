# Recipe Ingredient Display UI Enhancement - Frontend Implementation Guide

## Overview
This guide covers the Unity frontend setup required to implement recipe ingredient display functionality in fabricator machine UI configuration and selection panels.

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
├── Create Child: "CountText"
│   └── Add Component: TextMeshProUGUI
│       ├── Text: "1 x" (placeholder, set at runtime)
│       ├── Font Size: 14
│       ├── Alignment: Middle Left
│       ├── Color: White
│       └── Best Fit: unchecked
└── Create Child: "ItemIcon"
    └── Add Component: Image (for item sprite)
        ├── Sprite: None (set at runtime)
        ├── Color: White
        ├── Preserve Aspect: true
        ├── RectTransform Size: 32x32
        └── Native Size: false
```

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

### 3. Update RecipeSelectionPanel

The RecipeSelectionPanel automatically displays recipe ingredients in two ways:
1. **Text Display (Automatic)**: Recipe button text includes ingredient counts, e.g., "Aluminum Plate (1x Aluminum Can + 2x Plastic Bottle)"
2. **Visual Display (Optional)**: Add RecipeIngredientDisplay component to button prefab for icon-based ingredient display

#### 3.1. Text-Only Display (No Additional Setup)
The `GetEnhancedDisplayName()` method automatically adds ingredient text to button labels. This works out-of-the-box with no additional setup.

#### 3.2. Visual Icon Display (Optional Enhancement)

To add visual ingredient icons to selection buttons:

**Step 1: Modify SelectionButtonPrefab**
```
SelectionButtonPrefab (Enhanced with Visual Ingredients)
├── Button Component
├── Background (Image)
├── MainContent (Vertical Layout Group)
│   ├── TopSection (Horizontal Layout Group)
│   │   ├── OutputIcon (Image - for recipe output)
│   │   └── OutputName (TextMeshProUGUI - for recipe name)
│   └── IngredientSection (NEW - for visual ingredient display)
│       ├── Add Component: RecipeIngredientDisplay
│       ├── Add Component: HorizontalLayoutGroup
│       │   ├── Spacing: 5
│       │   ├── Child Force Expand: false
│       │   └── Child Control Size: true
│       ├── Add Component: Content Size Fitter
│       │   ├── Horizontal Fit: Preferred Size
│       │   └── Vertical Fit: Preferred Size
│       └── RecipeIngredientDisplay Settings:
│           ├── ingredientContainer: self (IngredientSection transform)
│           ├── ingredientPrefab: IngredientItemPrefab (same as config panel)
│           ├── iconSize: (24, 24) - smaller for compact display
│           ├── fontSize: 10-12
│           ├── useVerticalLayout: FALSE (horizontal for selection panel)
│           ├── maxIconsPerIngredient: 3
│           └── showArrow: true (optional)
```

**How It Works:**
- `RecipeSelectionPanel.SetupButtonVisuals()` calls `UpdateButtonIngredientDisplay()`
- This method searches for `RecipeIngredientDisplay` component in the button using `GetComponentInChildren<RecipeIngredientDisplay>()`
- If found, it calls `DisplayRecipe()` to show ingredient icons
- If not found, only text display is used (which still works fine)

**Step 2: Configure RecipeSelectionPanel Component**
```
RecipeSelectionPanel Component:
├── Base Selection Panel (inherited)
│   ├── selectionPanel: RecipeSelectionPanel GameObject
│   ├── buttonContainer: Content (Grid Layout Group)
│   └── buttonPrefab: SelectionButtonPrefab (with or without IngredientSection)
└── Recipe Selection Specific
    └── machineId: "" (set at runtime by FabricatorMachineConfigPanel)
```

**Important Notes:**
- The visual ingredient display is **optional** - text display works without it
- If you don't add RecipeIngredientDisplay to button prefab, you'll only see text descriptions
- The code automatically detects if the component exists and uses it if available

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

**Symptom**: Recipe selection buttons show output item name but no ingredient information

**Diagnosis:**
1. Check if text includes ingredients: Button text should show format like "Aluminum Plate (1x Can + 2x Bottle)"
   - **If text IS showing ingredients**: Text display is working, visual icons are optional
   - **If text is NOT showing ingredients**: Check `RecipeSelectionPanel.GetEnhancedDisplayName()` method

2. Check for visual ingredient icons:
   - Visual icons require `RecipeIngredientDisplay` component added to button prefab
   - If you see text but no icons, the button prefab needs the IngredientSection added (see section 3.2)

**Solutions:**

**For Text-Only Display (Quick Setup):**
- No additional setup needed
- `RecipeSelectionPanel` automatically adds ingredient text to button labels via `GetEnhancedDisplayName()`
- Check that `FactoryRegistry.Instance` is loaded with recipe data

**For Visual Icon Display:**
1. Add `IngredientSection` GameObject to SelectionButtonPrefab
2. Add `RecipeIngredientDisplay` component to IngredientSection
3. Add `HorizontalLayoutGroup` to IngredientSection
4. Configure RecipeIngredientDisplay properties:
   - Assign `ingredientContainer` = IngredientSection transform
   - Assign `ingredientPrefab` = IngredientItemPrefab
   - Set `useVerticalLayout = FALSE`
   - Set `iconSize = (24, 24)`
5. The code will automatically detect and use the component via `UpdateButtonIngredientDisplay()`

**Common Mistakes:**
- ❌ Forgetting to assign `ingredientContainer` in RecipeIngredientDisplay
- ❌ Using `useVerticalLayout = TRUE` for selection panel (should be FALSE)
- ❌ Not assigning `ingredientPrefab` reference
- ❌ Expecting visual icons without adding RecipeIngredientDisplay to button prefab

### Config Panel vs Selection Panel Differences

| Feature | Config Panel | Selection Panel |
|---------|-------------|-----------------|
| Layout | VerticalLayoutGroup | HorizontalLayoutGroup |
| useVerticalLayout | TRUE | FALSE |
| Display Format | Stacked lines: "N x [icon]" | Side-by-side: "[icon][icon]" |
| Spacers | Automatic (top/bottom) | None |
| Icon Size | 32x32 (touch-friendly) | 24x24 (compact) |
| Component Location | Separate container in panel | Inside button prefab |

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

### Phase 2: Selection Panel (Optional Enhancement)
1. Add HorizontalLayoutGroup to selection button ingredient section
2. Add RecipeIngredientDisplay with `useVerticalLayout = FALSE`
3. Configure for compact horizontal display
4. Test on different screen resolutions
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