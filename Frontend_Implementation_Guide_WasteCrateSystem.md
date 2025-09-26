# Waste Crate Supply System - Frontend Implementation Guide (CORRECTED)

## 🎯 Overview

This guide provides step-by-step instructions for implementing the **corrected configurable waste crate supply system** in Unity. The system uses a **global queue** approach where:

1. **One global queue** for all waste crates (not per-spawner queues)
2. **Spawners filter from global queue** based on their `RequiredCrateId` configuration
3. **When spawner is empty**: It searches global queue for matching crate type
4. **When crate added to global queue**: All empty spawners check if they can use it

## ✅ Backend Implementation Complete (CORRECTED)

The following backend components have been implemented with the **correct global queue architecture**:

### Core Components
- ✅ **Global Queue System**: Uses existing `GameData.wasteQueue` and `wasteQueueLimit`
- ✅ **SpawnerMachine**: Enhanced with `RequiredCrateId` filtering and `TryRefillFromGlobalQueue()`
- ✅ **GameManager**: Updated `PurchaseWasteCrate()` to add to global queue and notify spawners
- ✅ **SpawnerConfigPanel**: UI for configuring spawner crate type requirements
- ✅ **WasteCrateSelectionPanel**: Enhanced for spawner configuration support

### Key Features Implemented
- ✅ **Global queue management** (single queue shared by all spawners)
- ✅ **Crate type filtering** (spawners only consume matching `RequiredCrateId`)
- ✅ **Configurable spawner behavior** (players set `RequiredCrateId`)
- ✅ **Automatic notification system** (empty spawners notified when new crates added)
- ✅ **Complete save/load support** using existing global queue structure

## 🚀 Required Frontend Setup in Unity

### Step 1: Update MachineManager Integration

**File:** `ScrapLine/Assets/Scripts/Game/MachineManager.cs`

Add spawner configuration support:

```csharp
// In HandleMachineClick method, add:
if (cellData.machine is SpawnerMachine)
{
    // Show spawner configuration panel instead of generic machine panel
    var spawnerConfigPanel = FindFirstObjectByType<SpawnerConfigPanel>(FindObjectsInactive.Include);
    if (spawnerConfigPanel != null)
    {
        spawnerConfigPanel.ShowConfiguration(cellData, (selectedCrateId) => 
        {
            // Configuration callback handled by SpawnerConfigPanel
            GameLogger.LogUI($"Spawner configured with crate type: {selectedCrateId}", ComponentId);
        });
    }
    else
    {
        GameLogger.LogWarning(LoggingManager.LogCategory.UI, "SpawnerConfigPanel not found. Please create UI prefab.", ComponentId);
    }
    return;
}
```

### Step 2: Create SpawnerConfigPanel UI Prefab

**Create a new UI Panel in Unity:**

1. **Right-click in Hierarchy** → UI → Panel
2. **Name it**: `SpawnerConfigPanel`
3. **Add Component**: `SpawnerConfigPanel` script
4. **Create this structure**:

```
SpawnerConfigPanel (Panel)
├── Background (Image)
├── Header
│   └── Title (Text - "Configure Spawner")
├── CurrentCrateDisplay
│   ├── CurrentCrateIcon (Image)
│   └── CurrentCrateText (Text - "Required: [CrateType]")
├── ConfigButtons
│   ├── CrateSelectionButton (Button - "Select Crate Type")
│   └── PurchaseButton (Button - "Purchase Crates (Global: 0/2)")
└── ActionButtons
    ├── ConfirmButton (Button - "Confirm")
    └── CancelButton (Button - "Cancel")
```

**Inspector Configuration for SpawnerConfigPanel:**
```
Base Config Panel:
- configPanel = SpawnerConfigPanel
- confirmButton = ConfirmButton  
- cancelButton = CancelButton

Spawner Specific:
- crateSelectionButton = CrateSelectionButton
- currentCrateTypeText = CurrentCrateText
- currentCrateIcon = CurrentCrateIcon
- purchaseButton = PurchaseButton
- wasteCrateSelectionPanel = [Reference to WasteCrateSelectionPanel - see Step 3]
```

### Step 3: Update WasteCrateSelectionPanel for Configuration

**Enhance existing WasteCrateSelectionPanel:**

1. **Open existing** `WasteCrateSelectionPanel` prefab
2. **Ensure it has** proper button layout for crate type selection
3. **The WasteCrateSelectionPanel script is enhanced** with `OnCrateSelected` event support

### Step 4: Update WasteCrateConfigPanel for Global Queue

**Modify existing WasteCrateConfigPanel** to work with global queue:

**Inspector Configuration:**
- Update button text to show global queue status: "Purchase Crates (Global: X/Y)"
- Remove spawner-specific queue references

### Step 5: Update Machine Prefabs

**For SpawnerMachine prefabs:**

1. **Open existing Spawner machine prefabs**
2. **Verify they can be clicked** for configuration (should work with existing MachineManager)
3. **Set `canRotate = false`** in machines.json for spawners (since they use click for config)

### Step 6: Scene Setup

**In your main game scene:**

1. **Add SpawnerConfigPanel prefab** to the scene (set inactive initially)
2. **Ensure WasteCrateConfigPanel and WasteCrateSelectionPanel** are present
3. **Connect references** between panels as specified in inspector configurations
4. **Test the panel navigation** (Config → Selection → Purchase)

## 🎮 Player Workflow (CORRECTED)

### New Player Experience:

1. **Place Spawner**: Player places a spawner machine (defaults to "starter_crate" requirement)
2. **Configure Spawner**: Click on spawner → opens SpawnerConfigPanel
3. **Select Crate Type**: Click "Select Crate Type" → opens WasteCrateSelectionPanel  
4. **Choose Required Type**: Select desired crate type (e.g., "medium_crate")
5. **Purchase Crates**: Click "Purchase Crates" → opens WasteCrateConfigPanel
6. **Buy Any Crates**: Purchase any crate types (all go to **global queue**)
7. **Automatic Filtering**: Spawners automatically check global queue for their `RequiredCrateId`

### Key Behavior Changes:

- ✅ **Global Queue**: All purchased crates go to one shared queue
- ✅ **Smart Filtering**: Each spawner only takes matching crates from global queue
- ✅ **Automatic Processing**: When crates added, all empty spawners check compatibility  
- ✅ **Configuration Persistence**: Spawner `RequiredCrateId` settings save/load properly

## 🧪 Testing Scenarios

### Scenario 1: Basic Global Queue
1. Purchase 3x mixed crates (starter + medium + large) → all go to global queue
2. Verify global queue shows 3/5 crates (or whatever the limit is)
3. Place spawners with different `RequiredCrateId` configurations  
4. Verify each spawner only takes its matching crate type from global queue

### Scenario 2: Filtering Test
1. Configure Spawner A to require "starter_crate"
2. Configure Spawner B to require "medium_crate"
3. Purchase 1x starter_crate, 1x medium_crate → both go to global queue
4. Verify Spawner A gets starter_crate, Spawner B gets medium_crate

### Scenario 3: Global Queue Management
1. Purchase crates up to global queue limit
2. Try to purchase more → should fail with "global queue is full" message
3. Let spawners consume crates → global queue space becomes available

### Scenario 4: Save/Load Persistence
1. Configure spawners with different crate requirements
2. Purchase various crates in global queue  
3. Save game → reload → verify global queue and spawner configurations restored

## 🔧 Troubleshooting

### Common Issues:

**"Global queue is full"**
- Check `GameData.wasteQueueLimit` value
- Verify spawners are consuming crates (check `RequiredCrateId` matches available crates)

**"Spawners not consuming crates"**
- Verify spawner `RequiredCrateId` matches crate types in global queue
- Check spawner is empty (not already has a crate)
- Verify global queue has crates: `GameManager.Instance.gameData.wasteQueue.Count`

**"Purchase button shows wrong queue status"**  
- Ensure WasteCrateConfigPanel uses `GetGlobalQueueStatus()` instead of spawner-specific status

### Debug Commands:
```csharp
// Check global queue status
var queue = GameManager.Instance.gameData.wasteQueue;
Debug.Log($"Global queue: {queue.Count}/{GameManager.Instance.gameData.wasteQueueLimit}");

// Check spawner configuration
Debug.Log($"Spawner RequiredCrateId: {spawnerMachine.RequiredCrateId}");

// Test filtering
spawnerMachine.TryRefillFromGlobalQueue();
```

## 📋 Implementation Checklist

- [ ] Update MachineManager to handle spawner clicks
- [ ] Create SpawnerConfigPanel UI prefab with proper structure
- [ ] Configure SpawnerConfigPanel inspector references
- [ ] Update WasteCrateConfigPanel for global queue display
- [ ] Add panels to main game scene (inactive initially)  
- [ ] Test configuration workflow (place → click → configure → purchase)
- [ ] Test global queue filtering (multiple spawners, different requirements)
- [ ] Test save/load persistence of global queue and spawner configurations
- [ ] Verify global queue capacity limits and error handling
- [ ] Test navigation between config and purchase panels

## ✨ Architecture Summary

**BEFORE (Incorrect):** Per-spawner queues → Complex management, hard to understand
**AFTER (Correct):** Global queue + filtering → Simple, intuitive, matches requirements

### Global Queue Flow:
1. **Player purchases crate** → Added to `GameData.wasteQueue`
2. **All empty spawners notified** → Each checks if crate matches `RequiredCrateId`  
3. **First matching spawner** → Takes crate from global queue
4. **Remaining crates** → Stay in global queue for other spawners

This matches the original requirements: "*the player should have a global queue*" and "*spawner only has its current waste crate*".

---

**Note**: The backend is complete and compiles successfully. This guide focuses only on the Unity UI setup required to expose the functionality to players.

## 🚀 Required Frontend Setup in Unity

### Step 1: Update MachineManager Integration

**File:** `ScrapLine/Assets/Scripts/Game/MachineManager.cs`

Add spawner configuration support:

```csharp
// In HandleMachineClick method, add:
if (cellData.machine is SpawnerMachine)
{
    // Show spawner configuration panel instead of generic machine panel
    var spawnerConfigPanel = FindFirstObjectByType<SpawnerConfigPanel>(FindObjectsInactive.Include);
    if (spawnerConfigPanel != null)
    {
        spawnerConfigPanel.ShowConfiguration(cellData, (selectedCrateId) => 
        {
            // Configuration callback handled by SpawnerConfigPanel
            GameLogger.LogUI($"Spawner configured with crate type: {selectedCrateId}", ComponentId);
        });
    }
    else
    {
        GameLogger.LogWarning(LoggingManager.LogCategory.UI, "SpawnerConfigPanel not found. Please create UI prefab.", ComponentId);
    }
    return;
}
```

### Step 2: Create SpawnerConfigPanel UI Prefab

**Create a new UI Panel in Unity:**

1. **Right-click in Hierarchy** → UI → Panel
2. **Name it**: `SpawnerConfigPanel`
3. **Add Component**: `SpawnerConfigPanel` script
4. **Create this structure**:

```
SpawnerConfigPanel (Panel)
├── Background (Image)
├── Header
│   └── Title (Text - "Configure Spawner")
├── CurrentCrateDisplay
│   ├── CurrentCrateIcon (Image)
│   └── CurrentCrateText (Text - "Required: [CrateType]")
├── ConfigButtons
│   ├── CrateSelectionButton (Button - "Select Crate Type")
│   └── PurchaseButton (Button - "Purchase Crates (0/2)")
└── ActionButtons
    ├── ConfirmButton (Button - "Confirm")
    └── CancelButton (Button - "Cancel")
```

**Inspector Configuration for SpawnerConfigPanel:**
```
Base Config Panel:
- configPanel = SpawnerConfigPanel
- confirmButton = ConfirmButton  
- cancelButton = CancelButton

Spawner Specific:
- crateSelectionButton = CrateSelectionButton
- currentCrateTypeText = CurrentCrateText
- currentCrateIcon = CurrentCrateIcon
- purchaseButton = PurchaseButton
- wasteCrateSelectionPanel = [Reference to WasteCrateSelectionPanel - see Step 3]
```

### Step 3: Update WasteCrateSelectionPanel for Configuration

**Enhance existing WasteCrateSelectionPanel:**

1. **Open existing** `WasteCrateSelectionPanel` prefab
2. **Ensure it has** proper button layout for crate type selection
3. **The existing WasteCrateSelectionPanel script is already updated** with `OnCrateSelected` event support

### Step 4: Update WasteCrateConfigPanel for Purchasing

**Modify existing WasteCrateConfigPanel** to work with per-spawner purchasing:

**Inspector Configuration:**
- Ensure it references the specific spawner for queue management
- Update button text to show queue status (implemented in backend)

### Step 5: Update Machine Prefabs

**For SpawnerMachine prefabs:**

1. **Open existing Spawner machine prefabs**
2. **Verify they can be clicked** for configuration (should work with existing MachineManager)
3. **Set `canRotate = false`** in machines.json for spawners (since they use click for config)

### Step 6: Scene Setup

**In your main game scene:**

1. **Add SpawnerConfigPanel prefab** to the scene (set inactive initially)
2. **Ensure WasteCrateConfigPanel and WasteCrateSelectionPanel** are present
3. **Connect references** between panels as specified in inspector configurations
4. **Test the panel navigation** (Config → Selection → Purchase)

## 🎮 Player Workflow

### New Player Experience:

1. **Place Spawner**: Player places a spawner machine (defaults to "starter_crate" requirement)
2. **Configure Spawner**: Click on spawner → opens SpawnerConfigPanel
3. **Select Crate Type**: Click "Select Crate Type" → opens WasteCrateSelectionPanel  
4. **Choose Required Type**: Select desired crate type (e.g., "medium_crate")
5. **Purchase Crates**: Click "Purchase Crates" → opens WasteCrateConfigPanel
6. **Buy Compatible Crates**: Purchase crates that match spawner's RequiredCrateId
7. **Automatic Processing**: Spawner only consumes matching crates from its queue

### Key Behavior Changes:

- ✅ **Filtering**: Spawners only consume crates that match their `RequiredCrateId`
- ✅ **Per-Machine Queues**: Each spawner has its own 2-crate queue (default capacity)
- ✅ **Configuration Persistence**: Spawner settings save/load properly
- ✅ **Smart Purchasing**: Purchase interface shows spawner-specific queue status

## 🧪 Testing Scenarios

### Scenario 1: Basic Configuration
1. Place spawner → click to configure
2. Change required crate type from "starter_crate" to "medium_crate"  
3. Purchase 2x medium_crate for this spawner
4. Verify spawner only consumes medium_crates and ignores others

### Scenario 2: Filtering Test
1. Configure Spawner A to require "starter_crate"
2. Configure Spawner B to require "medium_crate"
3. Purchase mixed crates for both spawners
4. Verify each spawner only consumes its required type

### Scenario 3: Queue Management
1. Purchase crates up to queue limit (2 per spawner by default)
2. Try to purchase more → should fail with "queue full" message
3. Let spawner consume crates → queue space becomes available

### Scenario 4: Save/Load Persistence
1. Configure spawners with different crate requirements
2. Purchase various crates in different queues  
3. Save game → reload → verify all configurations and queues restored

## 🔧 Troubleshooting

### Common Issues:

**"SpawnerConfigPanel not found"**
- Ensure SpawnerConfigPanel prefab is added to scene
- Check component references are properly assigned

**"Crates not being consumed"**
- Verify spawner RequiredCrateId matches purchased crate types
- Check WasteSupplyManager is properly initialized

**"Purchase button shows wrong queue status"**  
- Ensure WasteCrateConfigPanel references correct spawner
- Check ShowConfiguration method is called with proper CellData

### Debug Commands:
```csharp
// Check spawner configuration
Debug.Log($"Spawner RequiredCrateId: {spawnerMachine.RequiredCrateId}");

// Check queue status  
var status = WasteSupplyManager.Instance.GetMachineQueueStatus("Spawner_X_Y");
Debug.Log($"Queue: {status.queuedCrateIds.Count}/{status.maxQueueSize}");
```

## 📋 Implementation Checklist

- [ ] Update MachineManager to handle spawner clicks
- [ ] Create SpawnerConfigPanel UI prefab with proper structure
- [ ] Configure SpawnerConfigPanel inspector references
- [ ] Update WasteCrateConfigPanel for per-spawner purchasing
- [ ] Add panels to main game scene (inactive initially)  
- [ ] Test configuration workflow (place → click → configure → purchase)
- [ ] Test filtering behavior (spawners only consume matching crates)
- [ ] Test save/load persistence of configurations and queues
- [ ] Verify queue capacity limits and error handling
- [ ] Test navigation between config and purchase panels

## ✨ Future Enhancements

The system is designed to support future upgrades:

- **Queue Capacity Upgrades**: Increase per-spawner queue limits
- **Bulk Purchasing**: Purchase multiple crates at once
- **Crate Scheduling**: Time-based crate delivery
- **Advanced Filtering**: Multiple required crate types per spawner
- **Queue Priority**: VIP crates that jump to front of queue

---

**Note**: All backend logic is complete and thoroughly tested. This guide focuses only on the Unity UI setup required to expose the functionality to players.