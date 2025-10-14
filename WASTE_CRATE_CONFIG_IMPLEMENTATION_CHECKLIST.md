# Waste Crate Config Panel - Implementation Checklist

## ✅ Pre-Implementation Verification

Before starting Unity implementation, verify these backend changes are complete:

- [x] **BaseConfigPanel.cs**: Added `hideCancelButton` field
- [x] **WasteCrateConfigPanel.cs**: Complete redesign (no longer inherits from BaseConfigPanel)
- [x] **SpawnerConfigPanel.cs**: Updated to call `ShowPanel()` instead of `ShowConfiguration()`
- [x] **ScriptSandbox compilation**: All changes compile successfully
- [x] **Core tests passing**: WasteCrate data structures and logic working

## 📋 Unity Implementation Steps

### Phase 1: Prefab Creation

#### 1.1 Create Main Panel Structure
- [ ] Create WasteCrateConfigPanel GameObject in scene
- [ ] Add WasteCrateConfigPanel component to GameObject
- [ ] Create Background (Image) child
- [ ] Create ContentPanel child
- [ ] Set panel inactive by default

#### 1.2 Create Header Section
- [ ] Create Header panel under ContentPanel
- [ ] Add TitleText (TextMeshProUGUI) - "Purchase Waste Crates"
- [ ] Add CloseButton (Button) with "X" icon
- [ ] Connect closeButton in inspector

#### 1.3 Create Queue Section
- [ ] Create QueueSection panel under ContentPanel
- [ ] Add QueueLabel (TextMeshProUGUI) - "Current Queue:"
- [ ] Create QueueDisplay GameObject
- [ ] Add WasteCrateQueuePanel component to QueueDisplay
- [ ] Create QueueContainer with HorizontalLayoutGroup
- [ ] Connect queuePanel reference in inspector

#### 1.4 Create Purchase Section
- [ ] Create PurchaseSection panel under ContentPanel
- [ ] Add PurchaseLabel (TextMeshProUGUI) - "Available Crates:"
- [ ] Create CrateGridScrollView (Scroll View)
- [ ] Create CrateGridContainer under scroll view content
- [ ] Add GridLayoutGroup to CrateGridContainer
- [ ] Configure GridLayoutGroup (3 columns, auto cell size)
- [ ] Connect crateGridContainer in inspector

### Phase 2: Prefab Creation

#### 2.1 Create Crate Button Prefab
- [ ] Create new prefab: CrateButtonPrefab
- [ ] Add Button component
- [ ] Add Background Image (for crate sprite)
- [ ] Add CrateNameText (TextMeshProUGUI)
- [ ] Add CostText (TextMeshProUGUI)
- [ ] Configure Image to preserve aspect
- [ ] Save prefab to Resources or prefab folder
- [ ] Assign to crateButtonPrefab in inspector

#### 2.2 Create Queue Item Prefab
- [ ] Create new prefab: QueueItemPrefab
- [ ] Add Image component (for crate icon)
- [ ] Set size to 60x60 pixels
- [ ] Configure to preserve aspect
- [ ] Save prefab
- [ ] Assign to queueItemPrefab in WasteCrateQueuePanel

### Phase 3: Inspector Configuration

#### 3.1 Configure WasteCrateConfigPanel Component
- [ ] mainPanel: Assign WasteCrateConfigPanel root GameObject
- [ ] closeButton: Assign CloseButton
- [ ] queuePanel: Assign QueueDisplay (WasteCrateQueuePanel component)
- [ ] crateGridContainer: Assign CrateGridContainer Transform
- [ ] crateButtonPrefab: Assign CrateButtonPrefab
- [ ] showCostInText: Check true

#### 3.2 Configure WasteCrateQueuePanel Component
- [ ] queuePanel: Assign QueueDisplay GameObject
- [ ] queueContainer: Assign QueueContainer Transform
- [ ] queueItemPrefab: Assign QueueItemPrefab
- [ ] emptyQueueText: (Optional) assign if using empty state text
- [ ] maxDisplayItems: Set to 3
- [ ] layoutDirection: Set to Left

#### 3.3 Update SpawnerConfigPanel Reference
- [ ] Find SpawnerConfigPanel in scene
- [ ] Locate wasteCrateConfigPanel field
- [ ] Assign WasteCrateConfigPanel instance
- [ ] Save changes

### Phase 4: Layout and Styling

#### 4.1 Configure Layout Groups
- [ ] QueueContainer: HorizontalLayoutGroup configured
  - [ ] Child Control: Width/Height disabled
  - [ ] Child Alignment: Middle Left
  - [ ] Spacing: 10 pixels
- [ ] CrateGridContainer: GridLayoutGroup configured
  - [ ] Constraint: Fixed Column Count = 3
  - [ ] Cell Size: Auto (set at runtime)
  - [ ] Spacing: 10x10 pixels
  - [ ] Child Alignment: Upper Center

#### 4.2 Apply Visual Styling
- [ ] Panel background: Semi-transparent dark color
- [ ] Content area: Light background color
- [ ] Header text: Large, bold, readable
- [ ] Button colors: Match game theme
- [ ] Disabled button state: Grayed out
- [ ] Text sizes: Readable on mobile (14pt minimum)

#### 4.3 Configure Scroll View
- [ ] Enable vertical scrolling
- [ ] Disable horizontal scrolling
- [ ] Set scroll sensitivity for mobile
- [ ] Configure viewport mask
- [ ] Test with many crate types (6+)

### Phase 5: Testing

#### 5.1 Panel Display Tests
- [ ] Panel opens when clicking "Purchase Crates" in SpawnerConfigPanel
- [ ] Queue section displays at top
- [ ] Purchase grid displays at bottom
- [ ] Grid shows 3 columns of crates
- [ ] All crates from FactoryRegistry are shown
- [ ] Close button works correctly

#### 5.2 Queue Display Tests
- [ ] Empty queue shows "Queue Empty" or empty state
- [ ] Purchased crates appear in queue (up to 3)
- [ ] Queue items show correct sprites
- [ ] Queue updates immediately after purchase
- [ ] Queue display matches actual game data

#### 5.3 Purchase Workflow Tests
- [ ] Start with 1000 credits
- [ ] Click on affordable crate
- [ ] Verify immediate purchase (no confirm dialog)
- [ ] Credits deducted correctly
- [ ] Crate added to queue
- [ ] Queue display updates
- [ ] Grid refreshes with new button states

#### 5.4 Affordability Tests
- [ ] Affordable crates: Full color, clickable
- [ ] Unaffordable crates: Grayed out, disabled
- [ ] Purchase reduces credits → buttons update
- [ ] Add credits → buttons become clickable

#### 5.5 Queue Full Tests
- [ ] Purchase 3 crates (fill queue)
- [ ] Verify all purchase buttons disabled
- [ ] Verify queue shows 3/3 status
- [ ] Cannot purchase more crates
- [ ] Consume queue (spawn items) → buttons enabled

#### 5.6 Responsive Layout Tests
- [ ] Test on 1920x1080 desktop
- [ ] Test on 1280x720 laptop
- [ ] Test on 1080x1920 mobile (portrait)
- [ ] Test on 1920x1080 mobile (landscape)
- [ ] Grid maintains 3 columns on all screens
- [ ] Touch targets are large enough (100x100 minimum)

#### 5.7 Edge Cases
- [ ] No crates in registry → graceful handling
- [ ] Zero credits → all buttons disabled
- [ ] Empty queue + zero credits → works correctly
- [ ] Rapidly clicking buttons → no double purchase
- [ ] Panel closed mid-purchase → no issues

### Phase 6: Polish

#### 6.1 Visual Refinement
- [ ] Consistent colors throughout panel
- [ ] Proper spacing between elements
- [ ] Aligned text and icons
- [ ] Hover states on buttons (desktop)
- [ ] Visual feedback on button press

#### 6.2 Animation (Optional)
- [ ] Panel fade in on open
- [ ] Panel fade out on close
- [ ] Button scale on press
- [ ] Queue item slide in on purchase
- [ ] Credits flash on deduction

#### 6.3 Accessibility
- [ ] Text readable on all backgrounds
- [ ] Color contrast meets standards
- [ ] Touch targets large enough (WCAG 2.1)
- [ ] Works with screen readers (if applicable)

## 🧪 Final Validation

### Before Marking Complete
- [ ] All inspector references assigned (no null warnings)
- [ ] Panel starts inactive in scene
- [ ] Can open panel from SpawnerConfigPanel
- [ ] Can purchase crates successfully
- [ ] Queue updates correctly
- [ ] Credits deducted properly
- [ ] Close button works
- [ ] No console errors or warnings
- [ ] Tested on target platform (mobile/desktop)
- [ ] Performance acceptable (no lag)

### Documentation Review
- [ ] Read WASTE_CRATE_CONFIG_PANEL_REDESIGN_GUIDE.md
- [ ] Review WASTE_CRATE_CONFIG_REDESIGN_SUMMARY.md
- [ ] Check WASTE_CRATE_CONFIG_UI_MOCKUP.md for reference
- [ ] Understand backend changes in code

### Integration Verification
- [ ] SpawnerConfigPanel opens WasteCrateConfigPanel correctly
- [ ] WasteSupplyManager integration works
- [ ] CreditsManager integration works
- [ ] FactoryRegistry provides crate definitions
- [ ] Global queue system functioning

## 📊 Completion Metrics

### Required for "Done"
- Panel structure: 100% complete
- Prefabs created: 100% complete
- Inspector references: 100% complete
- Basic functionality: 100% working
- Purchase workflow: 100% working

### Recommended for "Production Ready"
- Visual polish: 90%+ complete
- Responsive layout: 100% tested
- Edge cases: 90%+ handled
- Performance: No lag or stuttering
- Mobile testing: Complete on target device

### Optional Enhancements
- Animations: Nice to have
- Sound effects: Nice to have
- Tooltips: Nice to have
- Advanced sorting/filtering: Future feature

## 🚨 Common Issues & Solutions

| Issue | Solution |
|-------|----------|
| Panel doesn't show | Check mainPanel is assigned and starts inactive |
| No crate buttons | Verify crateButtonPrefab assigned and FactoryRegistry has crates |
| Queue empty | Purchase a crate first, check WasteSupplyManager working |
| Buttons not clickable | Check Button component exists, EventSystem in scene |
| Layout broken | Verify GridLayoutGroup settings (3 columns, auto size) |
| Credits not deducting | Check CreditsManager reference in WasteSupplyManager |
| Queue not updating | Verify WasteCrateQueuePanel properly configured |

## ✅ Sign-Off

- [ ] Implementation complete
- [ ] All tests passing
- [ ] No console errors
- [ ] Code reviewed
- [ ] Ready for production

**Implementer:** ________________  
**Date:** ________________  
**Build Version:** ________________

---

**Use this checklist to track progress when implementing the redesigned WasteCrateConfigPanel in Unity.**
