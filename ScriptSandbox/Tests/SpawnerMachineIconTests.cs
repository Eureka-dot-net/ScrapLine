using NUnit.Framework;
using System.Collections.Generic;

namespace ScrapLine.Tests
{
    /// <summary>
    /// Unit tests for SpawnerMachine icon sprite functionality based on waste crate fill percentage.
    /// </summary>
    [TestFixture]
    public class SpawnerMachineIconTests
    {
        private SpawnerMachine spawnerMachine;
        private CellData cellData;
        private MachineDef machineDef;
        private WasteCrateDef wasteCrateDef;

        [SetUp]
        public void Setup()
        {
            // Create test machine definition
            machineDef = new MachineDef
            {
                id = "spawner_test",
                type = "spawner",
                sprite = "spawner_ui",
                buildingIconSprite = "spawner_icon",
                baseProcessTime = 5.0f
            };

            // Create test waste crate definition with 100 total items
            wasteCrateDef = new WasteCrateDef
            {
                id = "test_crate",
                displayName = "Test Crate",
                items = new List<WasteCrateItemDef>
                {
                    new WasteCrateItemDef { itemType = "can", count = 50 },
                    new WasteCrateItemDef { itemType = "bottle", count = 50 }
                }
            };
            
            // Create test cell data
            cellData = new CellData
            {
                x = 0,
                y = 0,
                cellType = UICell.CellType.Machine,
                direction = UICell.Direction.Up,
                machineDefId = "spawner_test",
                items = new List<ItemData>(),
                wasteCrate = new WasteCrateInstance
                {
                    wasteCrateDefId = "test_crate",
                    remainingItems = new List<WasteCrateItemDef>
                    {
                        new WasteCrateItemDef { itemType = "can", count = 50 },
                        new WasteCrateItemDef { itemType = "bottle", count = 50 }
                    }
                }
            };

            // Create spawner machine
            spawnerMachine = new SpawnerMachine(cellData, machineDef);
        }

        [Test]
        public void GetBuildingIconSprite_EmptyWasteCrate_Returns_0()
        {
            // Arrange
            cellData.wasteCrate.remainingItems.Clear();

            // Act
            string iconSprite = spawnerMachine.GetBuildingIconSprite();

            // Assert
            Assert.AreEqual("spawner_icon_0", iconSprite, "Empty waste crate should return _0 sprite");
        }

        [Test]
        public void GetBuildingIconSprite_NoWasteCrate_Returns_0()
        {
            // Arrange
            cellData.wasteCrate = null;

            // Act
            string iconSprite = spawnerMachine.GetBuildingIconSprite();

            // Assert
            Assert.AreEqual("spawner_icon_0", iconSprite, "No waste crate should return _0 sprite");
        }

        [Test]
        public void GetBuildingIconSprite_FullWasteCrate_Returns_100()
        {
            // Arrange - waste crate is already full (100 items) from setup

            // Act
            string iconSprite = spawnerMachine.GetBuildingIconSprite();

            // Assert
            Assert.AreEqual("spawner_icon_100", iconSprite, "Full waste crate (100%) should return _100 sprite");
        }

        [Test]
        public void GetBuildingIconSprite_67Percent_Returns_100()
        {
            // Arrange - 67 items (67% full)
            cellData.wasteCrate.remainingItems[0].count = 34; // 34 cans
            cellData.wasteCrate.remainingItems[1].count = 33; // 33 bottles = 67 total

            // Act
            string iconSprite = spawnerMachine.GetBuildingIconSprite();

            // Assert
            Assert.AreEqual("spawner_icon_100", iconSprite, "67% full waste crate should return _100 sprite (>66%)");
        }

        [Test]
        public void GetBuildingIconSprite_66Percent_Returns_66()
        {
            // Arrange - 66 items (66% full)
            cellData.wasteCrate.remainingItems[0].count = 33; // 33 cans
            cellData.wasteCrate.remainingItems[1].count = 33; // 33 bottles = 66 total

            // Act
            string iconSprite = spawnerMachine.GetBuildingIconSprite();

            // Assert
            Assert.AreEqual("spawner_icon_66", iconSprite, "66% full waste crate should return _66 sprite");
        }

        [Test]
        public void GetBuildingIconSprite_50Percent_Returns_66()
        {
            // Arrange - 50 items (50% full, between 33-66%)
            cellData.wasteCrate.remainingItems[0].count = 25; // 25 cans
            cellData.wasteCrate.remainingItems[1].count = 25; // 25 bottles = 50 total

            // Act
            string iconSprite = spawnerMachine.GetBuildingIconSprite();

            // Assert
            Assert.AreEqual("spawner_icon_66", iconSprite, "50% full waste crate should return _66 sprite (34-66% range)");
        }

        [Test]
        public void GetBuildingIconSprite_34Percent_Returns_66()
        {
            // Arrange - 34 items (34% full, just above 33% threshold)
            cellData.wasteCrate.remainingItems[0].count = 17; // 17 cans
            cellData.wasteCrate.remainingItems[1].count = 17; // 17 bottles = 34 total

            // Act
            string iconSprite = spawnerMachine.GetBuildingIconSprite();

            // Assert
            Assert.AreEqual("spawner_icon_66", iconSprite, "34% full waste crate should return _66 sprite (34-66% range)");
        }

        [Test]
        public void GetBuildingIconSprite_33Percent_Returns_33()
        {
            // Arrange - 33 items (33% full, at threshold)
            cellData.wasteCrate.remainingItems[0].count = 17; // 17 cans
            cellData.wasteCrate.remainingItems[1].count = 16; // 16 bottles = 33 total

            // Act
            string iconSprite = spawnerMachine.GetBuildingIconSprite();

            // Assert
            Assert.AreEqual("spawner_icon_33", iconSprite, "33% full waste crate should return _33 sprite");
        }

        [Test]
        public void GetBuildingIconSprite_20Percent_Returns_33()
        {
            // Arrange - 20 items (20% full, between 1-33%)
            cellData.wasteCrate.remainingItems[0].count = 10; // 10 cans
            cellData.wasteCrate.remainingItems[1].count = 10; // 10 bottles = 20 total

            // Act
            string iconSprite = spawnerMachine.GetBuildingIconSprite();

            // Assert
            Assert.AreEqual("spawner_icon_33", iconSprite, "20% full waste crate should return _33 sprite (1-33% range)");
        }

        [Test]
        public void GetBuildingIconSprite_1Percent_Returns_33()
        {
            // Arrange - 1 item (1% full, minimum for _33)
            cellData.wasteCrate.remainingItems[0].count = 1; // 1 can
            cellData.wasteCrate.remainingItems[1].count = 0; // 0 bottles = 1 total

            // Act
            string iconSprite = spawnerMachine.GetBuildingIconSprite();

            // Assert
            Assert.AreEqual("spawner_icon_33", iconSprite, "1% full waste crate should return _33 sprite (1-33% range)");
        }

        [Test]
        public void GetBuildingIconSprite_CalledMultipleTimes_UsesCache()
        {
            // Arrange & Act - Call multiple times
            string firstCall = spawnerMachine.GetBuildingIconSprite();
            string secondCall = spawnerMachine.GetBuildingIconSprite();
            string thirdCall = spawnerMachine.GetBuildingIconSprite();

            // Assert - All calls return same result (testing consistency)
            Assert.AreEqual(firstCall, secondCall, "Multiple calls should return consistent results");
            Assert.AreEqual(secondCall, thirdCall, "Multiple calls should return consistent results");
        }

        [Test]
        public void GetBuildingIconSprite_DifferentInitialCrateSizes_HandledCorrectly()
        {
            // Test with different initial crate sizes
            
            // Create smaller crate (10 items total)
            var smallCellData = new CellData
            {
                x = 1,
                y = 1,
                cellType = UICell.CellType.Machine,
                direction = UICell.Direction.Up,
                machineDefId = "spawner_test",
                items = new List<ItemData>(),
                wasteCrate = new WasteCrateInstance
                {
                    wasteCrateDefId = "small_crate",
                    remainingItems = new List<WasteCrateItemDef>
                    {
                        new WasteCrateItemDef { itemType = "can", count = 5 } // 5 items = 50% of 10
                    }
                }
            };

            var smallCrateDef = new MachineDef
            {
                id = "spawner_small",
                type = "spawner",
                sprite = "small_spawner_ui",
                buildingIconSprite = "small_spawner",
                baseProcessTime = 3.0f
            };

            var smallSpawner = new SpawnerMachine(smallCellData, smallCrateDef);

            // Act
            string iconSprite = smallSpawner.GetBuildingIconSprite();

            // Assert - 5 out of 10 items should be in 34-66% range
            Assert.AreEqual("small_spawner_66", iconSprite, "50% of smaller crate should return _66 sprite");
        }
    }
}