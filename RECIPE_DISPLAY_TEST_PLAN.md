# Recipe Display Standardization - Test Plan

## Manual Testing Checklist

### Pre-Flight Checks
- [ ] Unity project opens without errors
- [ ] No compilation errors in Console
- [ ] RecipeSelectionPanel.cs compiles successfully
- [ ] FabricatorMachineConfigPanel.cs still works (no regressions)

### RecipeSelectionPanel Setup Test
1. [ ] Open RecipeSelectionPanel prefab in Inspector
2. [ ] Verify `ingredientDisplayRow` field exists (replaces old fields)
3. [ ] Create/Assign RecipeIngredientDisplayRow prefab to this field
4. [ ] Prefab should have:
   - [ ] RecipeIngredientDisplay component
   - [ ] Button component (makes row clickable)
   - [ ] ingredientContainer child with HorizontalLayoutGroup
   - [ ] useVerticalLayout = FALSE
   - [ ] showArrow = TRUE (optional)

### Functional Testing
1. [ ] Open a scene with FabricatorMachine
2. [ ] Click on a fabricator machine to open config panel
3. [ ] Click recipe selection button
4. [ ] RecipeSelectionPanel should appear with recipe rows
5. [ ] Each row should show:
   - [ ] Input ingredients (e.g., "1 x [icon]", "2 x [icon]")
   - [ ] Arrow (optional, if showArrow enabled)
   - [ ] Output result (e.g., "1 x [icon]")
6. [ ] Click a recipe row
7. [ ] Recipe should be selected and config panel should update
8. [ ] FabricatorMachineConfigPanel should show selected recipe ingredients

### Comparison Test (Both Panels Use Same Component)
1. [ ] Open FabricatorMachineConfigPanel
2. [ ] Verify RecipeIngredientDisplay is used for displaying selected recipe
3. [ ] Open RecipeSelectionPanel (click recipe button)
4. [ ] Verify each recipe row uses RecipeIngredientDisplay component
5. [ ] Ingredients should look consistent between both panels (same prefab)

### Edge Cases
- [ ] Test with 1 ingredient recipe
- [ ] Test with 2 ingredient recipe
- [ ] Test with 3 ingredient recipe (max supported)
- [ ] Test selecting "No Recipe" option
- [ ] Test switching between different recipes
- [ ] Test on different screen resolutions/aspect ratios

### Regression Testing
- [ ] Other UI panels still work (not affected by changes)
- [ ] Save/Load game state preserves recipe selection
- [ ] Recipe processing still works in gameplay
- [ ] No performance degradation

## Expected Results

### Success Criteria
✅ RecipeSelectionPanel displays all recipes as clickable rows
✅ Each row shows complete recipe (inputs → output)
✅ Selecting a recipe updates FabricatorMachineConfigPanel
✅ Both panels use the same RecipeIngredientDisplay component
✅ No console errors or warnings related to recipe display

### Known Issues (Not Related to Changes)
- Pre-existing SpawnerMachine test failures (unrelated)
- Pre-existing UniversalRotationCompensation test failures (unrelated)

## Validation Logs

### Check Console for These Messages
- [LOG] [UI] "Displaying recipe '[Recipe Name]' with X inputs using RecipeIngredientDisplay"
- [LOG] [UI] "Selected recipe: '[Recipe ID]'"
- [LOG] [UI] "Updated ingredient display for recipe: [Ingredients]"

### Watch for These Errors (Should NOT appear)
- ❌ "ingredientDisplayRow is null"
- ❌ "No RecipeIngredientDisplay component found"
- ❌ "panelIngredientItemPrefab is null" (old field, should not be referenced)

## Performance Verification

### Memory Usage
- [ ] Check Unity Profiler before changes
- [ ] Apply changes and check memory usage
- [ ] Should be similar or better (less manual instantiation)

### Frame Rate
- [ ] Test opening RecipeSelectionPanel multiple times
- [ ] Should not cause frame drops
- [ ] RecipeIngredientDisplay should create items efficiently

## Conclusion

If all tests pass:
✅ **Recipe display standardization is successful**
✅ **Both panels now use unified RecipeIngredientDisplay approach**
✅ **Code is cleaner, more maintainable, and consistent**

If tests fail:
❌ Review Frontend_Implementation_Guide_RecipeDisplay.md
❌ Verify prefab setup matches documentation
❌ Check Unity Console for specific error messages
❌ Refer to RECIPE_DISPLAY_MIGRATION_GUIDE.md
