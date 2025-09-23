using NUnit.Framework;
using System.Collections.Generic;

[TestFixture]
public class SpawnerMachineUpdateTests
{
    private SpawnerMachine spawner;
    private CellData cellData;
    private MachineDef machineDef;
    private FactoryRegistry factoryRegistry;

    [SetUp]
    public void Setup()
    {
        // Setup factory registry with test data
        factoryRegistry = FactoryRegistry.Instance;
        
        // Create test waste crate definition
        var wasteCrateData = new WasteCrateDef
        {
            id = "starter_crate",
            displayName = "Starter Waste Crate",
            items = new List<WasteCrateItemDef>
            {
                new WasteCrateItemDef { itemType = "can", count = 50 },
                new WasteCrateItemDef { itemType = "plasticBottle", count = 50 }
            }
        };
        
        // Create test item definitions
        var canDef = new ItemDef { id = "can", displayName = "Aluminum Can", sprite = "can_sprite" };
        var bottleDef = new ItemDef { id = "plasticBottle", displayName = "Plastic Bottle", sprite = "bottle_sprite" };
        
        // Manually set test data (normally loaded from JSON)
        factoryRegistry.WasteCrates["starter_crate"] = wasteCrateData;
        factoryRegistry.Items["can"] = canDef;
        factoryRegistry.Items["plasticBottle"] = bottleDef;
        
        // Create machine definition
        machineDef = new MachineDef
        {
            id = "spawner",
            type = "Spawner",
            baseProcessTime = 5.0f,
            className = "SpawnerMachine"
        };
        
        // Create cell data
        cellData = new CellData
        {
            x = 0,
            y = 0,
            cellType = UICell.CellType.Machine,
            direction = UICell.Direction.Up,
            machineDefId = "spawner",
            items = new List<ItemData>()
        };
        
        // Create spawner machine
        spawner = new SpawnerMachine(cellData, machineDef);
    }

    [Test]
    public void InitializeWasteCrate_NewSpawner_CreatesWasteCrateWithItems()
    {
        // Arrange & Act - constructor already called in Setup
        
        // Assert
        Assert.IsNotNull(cellData.wasteCrate);
        Assert.AreEqual("starter_crate", cellData.wasteCrate.wasteCrateDefId);
        Assert.AreEqual(2, cellData.wasteCrate.remainingItems.Count);
        
        var canItem = cellData.wasteCrate.remainingItems.Find(i => i.itemType == "can");
        var bottleItem = cellData.wasteCrate.remainingItems.Find(i => i.itemType == "plasticBottle");
        
        Assert.IsNotNull(canItem);
        Assert.IsNotNull(bottleItem);
        Assert.AreEqual(50, canItem.count);
        Assert.AreEqual(50, bottleItem.count);
    }

    [Test]
    public void InitializeWasteCrate_ExistingWasteCrate_DoesNotOverwrite()
    {
        // Arrange
        var existingWasteCrate = new WasteCrateInstance
        {
            wasteCrateDefId = "starter_crate",
            remainingItems = new List<WasteCrateItemDef>
            {
                new WasteCrateItemDef { itemType = "can", count = 25 },
                new WasteCrateItemDef { itemType = "plasticBottle", count = 30 }
            }
        };
        
        cellData.wasteCrate = existingWasteCrate;
        
        // Act - create new spawner (simulates save/load scenario)
        var newSpawner = new SpawnerMachine(cellData, machineDef);
        
        // Assert - existing waste crate should be preserved
        Assert.IsNotNull(cellData.wasteCrate);
        Assert.AreEqual("starter_crate", cellData.wasteCrate.wasteCrateDefId);
        Assert.AreEqual(2, cellData.wasteCrate.remainingItems.Count);
        
        var canItem = cellData.wasteCrate.remainingItems.Find(i => i.itemType == "can");
        var bottleItem = cellData.wasteCrate.remainingItems.Find(i => i.itemType == "plasticBottle");
        
        Assert.AreEqual(25, canItem.count);  // Original count preserved
        Assert.AreEqual(30, bottleItem.count); // Original count preserved
    }

    [Test]
    public void GetWasteCrateTooltip_WithItems_ReturnsFormattedTooltip()
    {
        // Act
        string tooltip = spawner.GetWasteCrateTooltip();
        
        // Assert
        Assert.IsNotNull(tooltip);
        Assert.IsTrue(tooltip.Contains("Starter Waste Crate"));
        Assert.IsTrue(tooltip.Contains("Aluminum Can: 50"));
        Assert.IsTrue(tooltip.Contains("Plastic Bottle: 50"));
    }

    [Test]
    public void GetWasteCrateTooltip_NoWasteCrate_ReturnsNoAssignedMessage()
    {
        // Arrange
        cellData.wasteCrate = null;
        
        // Act
        string tooltip = spawner.GetWasteCrateTooltip();
        
        // Assert
        Assert.AreEqual("No waste crate assigned", tooltip);
    }

    [Test]
    public void GetJunkyardSpriteName_FullCrate_Returns100Sprite()
    {
        // Act
        string spriteName = spawner.GetJunkyardSpriteName();
        
        // Assert
        Assert.AreEqual("junkYard_100", spriteName);
    }

    [Test]
    public void GetJunkyardSpriteName_75PercentFull_Returns100Sprite()
    {
        // Arrange - reduce items to 75% (75 out of 100)
        cellData.wasteCrate.remainingItems[0].count = 37; // 37 cans
        cellData.wasteCrate.remainingItems[1].count = 38; // 38 bottles = 75 total
        
        // Act
        string spriteName = spawner.GetJunkyardSpriteName();
        
        // Assert
        Assert.AreEqual("junkYard_100", spriteName);
    }

    [Test]
    public void GetJunkyardSpriteName_66PercentFull_Returns66Sprite()
    {
        // Arrange - reduce items to 66% (66 out of 100)
        cellData.wasteCrate.remainingItems[0].count = 33; // 33 cans
        cellData.wasteCrate.remainingItems[1].count = 33; // 33 bottles = 66 total
        
        // Act
        string spriteName = spawner.GetJunkyardSpriteName();
        
        // Assert
        Assert.AreEqual("junkYard_66", spriteName);
    }

    [Test]
    public void GetJunkyardSpriteName_33PercentFull_Returns33Sprite()
    {
        // Arrange - reduce items to 33% (33 out of 100)
        cellData.wasteCrate.remainingItems[0].count = 16; // 16 cans
        cellData.wasteCrate.remainingItems[1].count = 17; // 17 bottles = 33 total
        
        // Act
        string spriteName = spawner.GetJunkyardSpriteName();
        
        // Assert
        Assert.AreEqual("junkYard_33", spriteName);
    }

    [Test]
    public void GetJunkyardSpriteName_10PercentFull_Returns0Sprite()
    {
        // Arrange - reduce items to 10% (10 out of 100)
        cellData.wasteCrate.remainingItems[0].count = 5; // 5 cans
        cellData.wasteCrate.remainingItems[1].count = 5; // 5 bottles = 10 total
        
        // Act
        string spriteName = spawner.GetJunkyardSpriteName();
        
        // Assert
        Assert.AreEqual("junkYard_0", spriteName);
    }

    [Test]
    public void GetJunkyardSpriteName_EmptyCrate_Returns0Sprite()
    {
        // Arrange
        cellData.wasteCrate.remainingItems[0].count = 0;
        cellData.wasteCrate.remainingItems[1].count = 0;
        
        // Act
        string spriteName = spawner.GetJunkyardSpriteName();
        
        // Assert
        Assert.AreEqual("junkYard_0", spriteName);
    }

    [Test]
    public void GetSpawnProgress_InitialState_ReturnsZero()
    {
        // Act
        float progress = spawner.GetSpawnProgress();
        
        // Assert
        Assert.AreEqual(0.0f, progress, 0.01f);
    }

    [Test]
    public void GetProgress_CallsGetSpawnProgress()
    {
        // Act
        float progress = spawner.GetProgress();
        
        // Assert
        Assert.GreaterOrEqual(progress, 0.0f);
        Assert.LessOrEqual(progress, 1.0f);
    }

    [Test]
    public void GetTooltip_CallsGetWasteCrateTooltip()
    {
        // Act
        string tooltip = spawner.GetTooltip();
        
        // Assert
        Assert.IsNotNull(tooltip);
        Assert.IsTrue(tooltip.Contains("Starter Waste Crate"));
    }

    [Test]
    public void SpawnerMachine_WithNullWasteCrateRegistry_HandlesGracefully()
    {
        // Arrange - remove waste crate from registry
        factoryRegistry.WasteCrates.Clear();
        
        // Act - create new spawner
        var newCellData = new CellData
        {
            x = 1,
            y = 1,
            cellType = UICell.CellType.Machine,
            direction = UICell.Direction.Up,
            machineDefId = "spawner",
            items = new List<ItemData>()
        };
        
        var newSpawner = new SpawnerMachine(newCellData, machineDef);
        
        // Assert - should handle gracefully without throwing
        Assert.IsNull(newCellData.wasteCrate);
        Assert.AreEqual("No waste crate assigned", newSpawner.GetWasteCrateTooltip());
        Assert.AreEqual("junkYard_0", newSpawner.GetJunkyardSpriteName());
    }

    [Test]
    public void SpawnerMachine_LoadFromSaveScenario_PreservesExistingData()
    {
        // Arrange - simulate save/load scenario with partial waste crate
        var savedCellData = new CellData
        {
            x = 2,
            y = 2,
            cellType = UICell.CellType.Machine,
            direction = UICell.Direction.Up,
            machineDefId = "spawner",
            items = new List<ItemData>(),
            wasteCrate = new WasteCrateInstance
            {
                wasteCrateDefId = "starter_crate",
                remainingItems = new List<WasteCrateItemDef>
                {
                    new WasteCrateItemDef { itemType = "can", count = 10 },
                    new WasteCrateItemDef { itemType = "plasticBottle", count = 15 }
                }
            }
        };
        
        // Act - create spawner from saved data (simulates MachineFactory.CreateMachine)
        var loadedSpawner = new SpawnerMachine(savedCellData, machineDef);
        
        // Assert
        Assert.IsNotNull(savedCellData.wasteCrate);
        Assert.AreEqual("starter_crate", savedCellData.wasteCrate.wasteCrateDefId);
        Assert.AreEqual(10, savedCellData.wasteCrate.remainingItems[0].count);
        Assert.AreEqual(15, savedCellData.wasteCrate.remainingItems[1].count);
        
        string tooltip = loadedSpawner.GetWasteCrateTooltip();
        Assert.IsTrue(tooltip.Contains("Aluminum Can: 10"));
        Assert.IsTrue(tooltip.Contains("Plastic Bottle: 15"));
        
        // Should show junkYard_33 sprite for 25% remaining items (25% falls in 25-49% range)
        Assert.AreEqual("junkYard_33", loadedSpawner.GetJunkyardSpriteName());
    }
}