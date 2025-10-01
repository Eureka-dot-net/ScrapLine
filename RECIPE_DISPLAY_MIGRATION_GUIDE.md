# Recipe Display Standardization - Migration Guide

## What Changed?

The `RecipeSelectionPanel` has been refactored to use the same `RecipeIngredientDisplay` component as `FabricatorMachineConfigPanel`, creating a unified and consistent approach for displaying recipes across the application.

## Code Changes Summary

### Before (Old Approach)
RecipeSelectionPanel manually created ingredient items:
- Had two separate fields: `ingredientDisplayContainerPrefab` and `panelIngredientItemPrefab`
- Contained 100+ lines of manual ingredient creation code
- Created items by instantiating prefabs and manually setting sprites, text, etc.

### After (New Unified Approach)
RecipeSelectionPanel uses RecipeIngredientDisplay component:
- Single field: `ingredientDisplayRow` (RecipeIngredientDisplay prefab)
- Simplified to ~30 lines by calling `displayComponent.DisplayRecipe(recipe)`
- No manual item creation - handled by RecipeIngredientDisplay

## Unity Setup Migration

### If you have existing RecipeSelectionPanel setup:

1. **Create a new RecipeIngredientDisplay prefab for selection panel:**
   - Duplicate your existing config panel's RecipeIngredientDisplay
   - Name it "RecipeIngredientDisplayRow"
   - Change settings to horizontal layout:
     - Set `useVerticalLayout = FALSE`
     - Set `showArrow = TRUE` (optional, for showing arrow between inputs and outputs)
   - Add a `Button` component to make it clickable
   - Assign to RecipeSelectionPanel's `ingredientDisplayRow` field

2. **Update RecipeSelectionPanel inspector:**
   - Remove old references to `ingredientDisplayContainerPrefab` and `panelIngredientItemPrefab` (fields no longer exist)
   - Assign new `ingredientDisplayRow` field with your RecipeIngredientDisplayRow prefab

3. **Test in Unity:**
   - Open a scene with RecipeSelectionPanel
   - Verify recipes display correctly with ingredients shown horizontally
   - Verify clicking a row selects the recipe

## Benefits of This Change

1. **Consistency**: Both config and selection panels use the same component
2. **Maintainability**: Changes to recipe display logic apply to both panels automatically
3. **Less Code**: ~120 lines of duplicate code removed
4. **Easier Setup**: One prefab pattern instead of managing two separate prefabs
5. **Better Debugging**: Single source of truth means easier troubleshooting

## Troubleshooting

### Issue: "ingredientDisplayRow is null" warning
**Solution**: Assign the RecipeIngredientDisplayRow prefab to RecipeSelectionPanel's ingredientDisplayRow field

### Issue: "No RecipeIngredientDisplay component found" error
**Solution**: Ensure your assigned prefab has a RecipeIngredientDisplay component attached

### Issue: Recipes showing vertically instead of horizontally
**Solution**: In the RecipeIngredientDisplay component, set `useVerticalLayout = FALSE`

### Issue: No arrow showing between ingredients and output
**Solution**: In the RecipeIngredientDisplay component, set `showArrow = TRUE` and assign an arrow sprite

## Code Reference

### Old RecipeSelectionPanel Fields (Removed)
```csharp
// REMOVED - No longer needed
public GameObject ingredientDisplayContainerPrefab;
public GameObject panelIngredientItemPrefab;
```

### New RecipeSelectionPanel Field (Added)
```csharp
// NEW - Unified approach
public RecipeIngredientDisplay ingredientDisplayRow;
```

### Old SetupButtonVisuals (Removed ~100 lines)
```csharp
// OLD - Manual ingredient creation (REMOVED)
private void CreateIngredientItem(Transform container, string itemId, int count)
{
    // 100+ lines of manual sprite loading, instantiation, etc.
}
```

### New SetupButtonVisuals (Simplified)
```csharp
// NEW - Uses RecipeIngredientDisplay component
protected override void SetupButtonVisuals(GameObject buttonObj, RecipeDef recipe, string displayName)
{
    RecipeIngredientDisplay displayComponent = buttonObj.GetComponent<RecipeIngredientDisplay>();
    displayComponent.DisplayRecipe(recipe);  // That's it!
}
```

## For More Information

- See `Frontend_Implementation_Guide_RecipeDisplay.md` for complete Unity setup instructions
- See `RecipeSelectionPanel.cs` for implementation details
- See `RecipeIngredientDisplay.cs` for component documentation
