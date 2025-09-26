# Waste Crate Supply System - Frontend Implementation Guide

## 🎯 Overview

This guide provides step-by-step instructions for implementing the new **configurable waste crate supply system** in Unity. The system transforms spawner machines from passive item generators into sophisticated resource consumers that require player-managed waste crate purchasing and queuing with filtering capabilities.

## ✅ Backend Implementation Complete

The following backend components have been fully implemented and tested:

### Core Components
- ✅ **WasteSupplyManager**: Manages per-spawner crate queues with filtering
- ✅ **SpawnerMachine**: Updated with RequiredCrateId configuration and WasteSupplyManager integration  
- ✅ **SpawnerConfigPanel**: UI for configuring spawner crate type requirements
- ✅ **WasteCrateSelectionPanel**: Enhanced for spawner configuration support
- ✅ **GameData**: Updated with per-machine queue data instead of global queues
- ✅ **SaveLoadManager**: Integrated with WasteSupplyManager serialization

### Key Features Implemented
- ✅ Per-spawner queue management (replaces old global queue)
- ✅ Crate type filtering (spawners only consume matching crates)
- ✅ Configurable spawner behavior (players set RequiredCrateId)
- ✅ Navigation between configuration and purchase panels
- ✅ Complete save/load support for the new system

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