# Waste Crate Config Panel - UI Mockup

## Panel Layout (Visual Reference)

```
┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃  PURCHASE WASTE CRATES                                          [X] ┃
┣━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┫
┃                                                                      ┃
┃  Current Queue:                                                      ┃
┃  ┌─────┐  ┌─────┐  ┌─────┐                                         ┃
┃  │ [1] │  │ [2] │  │ [3] │   (shows up to 3 queued crates)         ┃
┃  └─────┘  └─────┘  └─────┘                                         ┃
┃                                                                      ┃
┃  Available Crates:                                                   ┃
┃  ┌──────────────────────────────────────────────────────────┐      ┃
┃  │ ┌─────────┐  ┌─────────┐  ┌─────────┐                   │      ┃
┃  │ │ Starter │  │ Medium  │  │  Large  │                   │      ┃
┃  │ │  Crate  │  │  Crate  │  │  Crate  │   (3 columns)     │      ┃
┃  │ │ 100 cr  │  │ 250 cr  │  │ 500 cr  │                   │      ┃
┃  │ └─────────┘  └─────────┘  └─────────┘                   │      ┃
┃  │                                                           │      ┃
┃  │ ┌─────────┐  ┌─────────┐  ┌─────────┐                   │      ┃
┃  │ │ Premium │  │  Jumbo  │  │  Mixed  │   (scrollable)    │      ┃
┃  │ │  Crate  │  │  Crate  │  │  Crate  │                   │      ┃
┃  │ │ 750 cr  │  │ 1000 cr │  │ 600 cr  │                   │      ┃
┃  │ └─────────┘  └─────────┘  └─────────┘                   │      ┃
┃  └──────────────────────────────────────────────────────────┘      ┃
┃                                                                      ┃
┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛
```

## User Interaction Flow

### Opening the Panel
```
[Spawner Machine]
        ↓ (click)
[SpawnerConfigPanel]
        ↓ (click "Purchase Crates")
[WasteCrateConfigPanel] ← Opens with queue + grid
```

### Purchase Flow
```
[WasteCrateConfigPanel]
        ↓ (click on crate button)
[Immediate Purchase] ← If affordable & queue has space
        ↓
[Queue Updated] ← Crate added to queue
[Credits Deducted] ← Payment processed
[Grid Refreshed] ← Buttons update affordability
```

### State Feedback
```
Crate Button States:
┌─────────────┐
│ Affordable  │ ← Full color, clickable
└─────────────┘

┌─────────────┐
│ Too Expensive│ ← Grayed out, disabled
└─────────────┘

┌─────────────┐
│ Queue Full  │ ← All buttons disabled
└─────────────┘
```

## Component Hierarchy

```
WasteCrateConfigPanel (GameObject)
├── mainPanel (GameObject)
│   ├── Background (Image)
│   └── ContentPanel (Panel)
│       ├── Header
│       │   ├── TitleText (TextMeshProUGUI)
│       │   └── CloseButton (Button) [X]
│       │
│       ├── QueueSection (Panel)
│       │   ├── QueueLabel (TextMeshProUGUI)
│       │   └── QueueDisplay (WasteCrateQueuePanel)
│       │       ├── queuePanel (GameObject)
│       │       └── queueContainer (Transform)
│       │           └── HorizontalLayoutGroup
│       │               ├── QueueItem1 (Image)
│       │               ├── QueueItem2 (Image)
│       │               └── QueueItem3 (Image)
│       │
│       └── PurchaseSection (Panel)
│           ├── PurchaseLabel (TextMeshProUGUI)
│           └── CrateGridScrollView (Scroll View)
│               └── CrateGridContainer (Transform)
│                   └── GridLayoutGroup (3 columns)
│                       ├── CrateButton1 (prefab instance)
│                       ├── CrateButton2 (prefab instance)
│                       ├── CrateButton3 (prefab instance)
│                       └── ... (populated at runtime)
```

## Inspector Reference Map

```
WasteCrateConfigPanel Component:
┌────────────────────────────────────────────┐
│ Main Panel Components:                     │
│   mainPanel: → WasteCrateConfigPanel       │
│   closeButton: → CloseButton               │
│                                             │
│ Queue Display:                             │
│   queuePanel: → QueueDisplay               │
│                                             │
│ Purchase Grid:                             │
│   crateGridContainer: → CrateGridContainer │
│   crateButtonPrefab: → CrateButtonPrefab   │
│   showCostInText: ☑ true                   │
└────────────────────────────────────────────┘

WasteCrateQueuePanel Component (on QueueDisplay):
┌────────────────────────────────────────────┐
│ Queue Panel Components:                    │
│   queuePanel: → QueueDisplay (this)        │
│   queueContainer: → QueueContainer         │
│   queueItemPrefab: → QueueItemPrefab       │
│   emptyQueueText: → (optional)             │
│                                             │
│ Configuration:                             │
│   maxDisplayItems: 3                       │
│   layoutDirection: Left                    │
└────────────────────────────────────────────┘
```

## Code Flow Diagram

```
Unity Event:
  OnPurchaseButtonClick()
          ↓
  wasteCrateConfigPanel.ShowPanel()
          ↓
  ┌───────────────────────────────────────┐
  │ UpdateQueueDisplay()                   │
  │   - Get queue from WasteSupplyManager  │
  │   - Update WasteCrateQueuePanel        │
  └───────────────────────────────────────┘
          ↓
  ┌───────────────────────────────────────┐
  │ PopulatePurchaseGrid()                 │
  │   - Get all crates from registry       │
  │   - Configure GridLayoutGroup          │
  │   - Create button for each crate       │
  │   - Setup click listeners              │
  └───────────────────────────────────────┘
          ↓
  Panel Visible to User
          ↓
User clicks on crate:
  OnCratePurchaseClicked(crate)
          ↓
  ┌───────────────────────────────────────┐
  │ wasteSupplyManager.PurchaseWasteCrate()│
  │   - Check affordability                │
  │   - Check queue space                  │
  │   - Deduct credits                     │
  │   - Add to queue                       │
  │   - Return success/failure             │
  └───────────────────────────────────────┘
          ↓
  If successful:
    ┌─────────────────────────────────────┐
    │ UpdateQueueDisplay()                 │
    │ PopulatePurchaseGrid()               │
    │   - Refresh all UI elements          │
    └─────────────────────────────────────┘
```

## Size Recommendations

```
Panel Dimensions:
  Width: 80% of screen width (800px on 1000px screen)
  Height: 70% of screen height (700px on 1000px screen)

Queue Items:
  Size: 60x60 pixels each
  Spacing: 10px between items
  Layout: Horizontal, left-aligned

Crate Grid:
  Columns: 3 (fixed)
  Cell Size: Auto-calculated (responsive)
  Spacing: 10x10 pixels
  Button Size: ~150x150 pixels (varies by screen)

Mobile Minimum:
  Button touch target: 100x100 pixels
  Text size: 14pt minimum
  Spacing: 10px minimum
```

## Color Scheme Example

```
Panel:
  Background: #000000 alpha 0.8 (semi-transparent black)
  Content area: #E0E0E0 (light gray)

Buttons:
  Normal: #FFFFFF (white)
  Hover: #F0F0F0 (light gray)
  Disabled: #808080 (dark gray)
  Pressed: #D0D0D0 (medium gray)

Text:
  Header: #000000 (black) on light background
  Labels: #333333 (dark gray)
  Button text: #000000 (black)
  Cost text: #FF6B00 (orange) - draws attention

Queue:
  Empty state: #CCCCCC (light gray)
  Item background: #FFFFFF (white)
  Item border: #BBBBBB (gray)
```

## Animation Suggestions (Optional)

```
Panel Opening:
  Scale from 0.8 to 1.0 over 0.2 seconds
  Fade from 0 to 1 over 0.2 seconds

Button Click:
  Scale to 0.95 on press
  Return to 1.0 on release
  Duration: 0.1 seconds

Purchase Success:
  Queue item: Fade in from left over 0.3 seconds
  Credits: Flash green for 0.5 seconds
  Grid: Fade refresh over 0.2 seconds

Purchase Failure:
  Button: Shake animation (5px left/right, 3 cycles)
  Duration: 0.3 seconds
```

---

**This mockup provides visual reference for implementing the redesigned WasteCrateConfigPanel in Unity.**
