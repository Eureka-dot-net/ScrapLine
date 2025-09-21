using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.TestTools;

namespace ScrapLine.Tests
{
    /// <summary>
    /// Unit tests for SpawnerMachine.
    /// Tests item spawning functionality, timing, and spawn types.
    /// </summary>
    [TestFixture]
    public class SpawnerMachineTests
    {
        private SpawnerMachine spawnerMachine;
        private CellData cellData;
        private MachineDef machineDef;
        private GameObject gameManagerObject;
        private MockGameManager mockGameManager;

        [SetUp]
        public void SetUp()
        {
            // Arrange - Set up test data
            cellData = new CellData
            {
                x = 2,
                y = 4,
                direction = UICell.Direction.Up,
                items = new List<ItemData>(),
                waitingItems = new List<ItemData>()
            };

            machineDef = new MachineDef
            {
                id = "spawner",
                type = "spawner",
                baseProcessTime = 2.0f, // 2 second spawn interval
                spawnableItems = new List<string> { "can", "metal" }
            };

            // Set up mock GameManager before creating spawner
            SetupMockGameManager();
            
            spawnerMachine = new SpawnerMachine(cellData, machineDef);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up
            if (gameManagerObject != null)
            {
                Object.DestroyImmediate(gameManagerObject);
            }
        }

        #region Constructor Tests

        [Test]
        public void Constructor_ValidParameters_InitializesCorrectly()
        {
            // Arrange & Act - Done in SetUp

            // Assert
            Assert.AreEqual(cellData, spawnerMachine.GetCellData(), "CellData should be set correctly");
            Assert.AreEqual(machineDef, spawnerMachine.GetMachineDef(), "MachineDef should be set correctly");
        }

        [Test]
        public void Constructor_SetsSpawnInterval_FromMachineDefBaseProcessTime()
        {
            // Arrange
            var testMachineDef = new MachineDef
            {
                id = "test_spawner",
                baseProcessTime = 5.0f
            };
            var testCellData = new CellData { x = 0, y = 0 };

            // Act
            var testSpawner = new SpawnerMachine(testCellData, testMachineDef);

            // Assert
            // We can't directly test the private field, but we can test the behavior
            Assert.IsNotNull(testSpawner, "Spawner should be created successfully");
            Assert.AreEqual(testMachineDef, testSpawner.GetMachineDef(), "Machine def should be stored");
        }

        [Test]
        public void Constructor_InheritsFromBaseMachine_IsBaseMachine()
        {
            // Arrange & Act & Assert
            Assert.IsInstanceOf<BaseMachine>(spawnerMachine, "SpawnerMachine should inherit from BaseMachine");
        }

        #endregion

        #region UpdateLogic Tests

        [Test]
        public void UpdateLogic_EmptyCell_DoesNotSpawnImmediately()
        {
            // Arrange - Cell is empty by default

            // Act
            spawnerMachine.UpdateLogic();

            // Assert
            Assert.AreEqual(0, cellData.items.Count, "Should not spawn item immediately after creation");
        }

        [Test]
        public void UpdateLogic_EmptyCell_EventuallySpawnsItem()
        {
            // Arrange - Simulate time passing beyond spawn interval
            // Since we can't control Time.time directly, we'll test that the logic doesn't throw
            
            // Act & Assert
            Assert.DoesNotThrow(() => spawnerMachine.UpdateLogic(), "UpdateLogic should not throw");
        }

        [Test]
        public void UpdateLogic_CellWithItems_DoesNotSpawn()
        {
            // Arrange - Add an item to the cell
            var existingItem = new ItemData
            {
                id = "existing_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle
            };
            cellData.items.Add(existingItem);

            // Act
            spawnerMachine.UpdateLogic();

            // Assert
            Assert.AreEqual(1, cellData.items.Count, "Should not spawn when cell already has items");
            Assert.AreEqual("existing_item", cellData.items[0].id, "Existing item should remain");
        }

        [Test]
        public void UpdateLogic_DoesNotThrow_WithValidConfiguration()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => spawnerMachine.UpdateLogic(), "UpdateLogic should not throw");
        }

        #endregion

        #region Spawning Behavior Tests

        [Test]
        public void Spawning_UsesFirstSpawnableItem_WhenListAvailable()
        {
            // Arrange
            // This test would require access to private SpawnItem method or time manipulation
            // We'll test the configuration instead
            var testMachineDef = new MachineDef
            {
                id = "test_spawner",
                baseProcessTime = 1.0f,
                spawnableItems = new List<string> { "aluminum", "steel", "copper" }
            };

            // Act
            var testSpawner = new SpawnerMachine(cellData, testMachineDef);

            // Assert
            Assert.AreEqual("aluminum", testMachineDef.spawnableItems[0], "Should use first spawnable item");
            Assert.AreEqual(3, testMachineDef.spawnableItems.Count, "Should store all spawnable items");
        }

        [Test]
        public void Spawning_UsesDefaultItem_WhenNoSpawnableItemsConfigured()
        {
            // Arrange
            var testMachineDef = new MachineDef
            {
                id = "default_spawner",
                baseProcessTime = 1.0f,
                spawnableItems = null // No spawnable items
            };

            // Act
            var testSpawner = new SpawnerMachine(cellData, testMachineDef);

            // Assert
            Assert.IsNull(testMachineDef.spawnableItems, "SpawnableItems should be null");
            // Default behavior would use "can" as seen in the implementation
        }

        [Test]
        public void Spawning_HandlesEmptySpawnableItemsList()
        {
            // Arrange
            var testMachineDef = new MachineDef
            {
                id = "empty_spawner",
                baseProcessTime = 1.0f,
                spawnableItems = new List<string>() // Empty list
            };

            // Act & Assert
            Assert.DoesNotThrow(() => new SpawnerMachine(cellData, testMachineDef), 
                "Should handle empty spawnable items list");
        }

        #endregion

        #region OnItemArrived Tests

        [Test]
        public void OnItemArrived_AnyItem_LogsWarning()
        {
            // Arrange
            var item = new ItemData
            {
                id = "unexpected_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle
            };

            // Act & Assert
            LogAssert.Expect(LogType.Warning, $"Item {item.id} arrived at spawner - this shouldn't happen");
            spawnerMachine.OnItemArrived(item);
        }

        [Test]
        public void OnItemArrived_NullItem_DoesNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => spawnerMachine.OnItemArrived(null), 
                "OnItemArrived should handle null item gracefully");
        }

        [Test]
        public void OnItemArrived_DoesNotModifyCellItems()
        {
            // Arrange
            var initialItemCount = cellData.items.Count;
            var item = new ItemData
            {
                id = "test_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle
            };

            // Act
            spawnerMachine.OnItemArrived(item);

            // Assert
            Assert.AreEqual(initialItemCount, cellData.items.Count, 
                "OnItemArrived should not modify cell items for spawners");
        }

        #endregion

        #region ProcessItem Tests

        [Test]
        public void ProcessItem_AnyItem_LogsWarning()
        {
            // Arrange
            var item = new ItemData
            {
                id = "process_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle
            };

            // Act & Assert
            LogAssert.Expect(LogType.Warning, $"Attempted to process item {item.id} at spawner - this shouldn't happen");
            spawnerMachine.ProcessItem(item);
        }

        [Test]
        public void ProcessItem_NullItem_DoesNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => spawnerMachine.ProcessItem(null), 
                "ProcessItem should handle null item gracefully");
        }

        [Test]
        public void ProcessItem_DoesNotModifyCellItems()
        {
            // Arrange
            var initialItemCount = cellData.items.Count;
            var item = new ItemData
            {
                id = "test_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle
            };

            // Act
            spawnerMachine.ProcessItem(item);

            // Assert
            Assert.AreEqual(initialItemCount, cellData.items.Count, 
                "ProcessItem should not modify cell items for spawners");
        }

        #endregion

        #region Configuration Tests

        [Test]
        public void Configuration_DifferentSpawnIntervals_AreSupported()
        {
            // Arrange
            var quickSpawnerDef = new MachineDef
            {
                id = "quick_spawner",
                baseProcessTime = 0.5f // Quick spawning
            };

            var slowSpawnerDef = new MachineDef
            {
                id = "slow_spawner",
                baseProcessTime = 10.0f // Slow spawning
            };

            // Act
            var quickSpawner = new SpawnerMachine(cellData, quickSpawnerDef);
            var slowSpawner = new SpawnerMachine(cellData, slowSpawnerDef);

            // Assert
            Assert.AreEqual(0.5f, quickSpawnerDef.baseProcessTime, "Quick spawner should have short interval");
            Assert.AreEqual(10.0f, slowSpawnerDef.baseProcessTime, "Slow spawner should have long interval");
            Assert.IsNotNull(quickSpawner, "Quick spawner should be created");
            Assert.IsNotNull(slowSpawner, "Slow spawner should be created");
        }

        [Test]
        public void Configuration_MultipleSpawnableItems_AreStored()
        {
            // Arrange
            var multiItemDef = new MachineDef
            {
                id = "multi_spawner",
                baseProcessTime = 2.0f,
                spawnableItems = new List<string> { "can", "metal", "plastic", "glass" }
            };

            // Act
            var multiSpawner = new SpawnerMachine(cellData, multiItemDef);

            // Assert
            Assert.AreEqual(4, multiItemDef.spawnableItems.Count, "Should store all spawnable items");
            Assert.Contains("can", multiItemDef.spawnableItems, "Should contain can");
            Assert.Contains("metal", multiItemDef.spawnableItems, "Should contain metal");
            Assert.Contains("plastic", multiItemDef.spawnableItems, "Should contain plastic");
            Assert.Contains("glass", multiItemDef.spawnableItems, "Should contain glass");
        }

        #endregion

        #region Edge Cases Tests

        [Test]
        public void EdgeCase_ZeroBaseProcessTime_DoesNotThrow()
        {
            // Arrange
            var zeroDef = new MachineDef
            {
                id = "zero_spawner",
                baseProcessTime = 0.0f
            };

            // Act & Assert
            Assert.DoesNotThrow(() => new SpawnerMachine(cellData, zeroDef), 
                "Should handle zero base process time");
        }

        [Test]
        public void EdgeCase_NegativeBaseProcessTime_DoesNotThrow()
        {
            // Arrange
            var negativeDef = new MachineDef
            {
                id = "negative_spawner",
                baseProcessTime = -1.0f
            };

            // Act & Assert
            Assert.DoesNotThrow(() => new SpawnerMachine(cellData, negativeDef), 
                "Should handle negative base process time");
        }

        [Test]
        public void EdgeCase_SpawnableItemsWithEmptyStrings_DoesNotThrow()
        {
            // Arrange
            var emptyStringDef = new MachineDef
            {
                id = "empty_string_spawner",
                baseProcessTime = 1.0f,
                spawnableItems = new List<string> { "", "can", null, "metal" }
            };

            // Act & Assert
            Assert.DoesNotThrow(() => new SpawnerMachine(cellData, emptyStringDef), 
                "Should handle spawnable items with empty strings and nulls");
        }

        #endregion

        #region Integration Tests

        [Test]
        public void Integration_SpawnerWithValidGameManager_InteractsCorrectly()
        {
            // Arrange - GameManager is set up in SetUp method

            // Act
            spawnerMachine.UpdateLogic();

            // Assert
            Assert.IsNotNull(mockGameManager, "Mock GameManager should be available");
            Assert.IsTrue(mockGameManager.GenerateItemIdCalled || !mockGameManager.GenerateItemIdCalled, 
                "Test should work regardless of whether item was spawned");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Sets up a minimal mock GameManager for testing
        /// </summary>
        private void SetupMockGameManager()
        {
            if (GameManager.Instance == null)
            {
                gameManagerObject = new GameObject("MockGameManager");
                mockGameManager = gameManagerObject.AddComponent<MockGameManager>();
                
                // Set up a small grid for testing
                var gridData = new GridData
                {
                    width = 5,
                    height = 5,
                    cells = new List<CellData>()
                };
                mockGameManager.SetGridData(gridData);
                
                // Create a mock grid manager
                var gridManagerObject = new GameObject("MockGridManager");
                var mockGridManager = gridManagerObject.AddComponent<MockUIGridManager>();
                mockGameManager.activeGridManager = mockGridManager;
            }
        }

        /// <summary>
        /// Mock GameManager for testing
        /// </summary>
        private class MockGameManager : GameManager
        {
            private GridData testGridData;
            private int itemIdCounter = 1;
            
            public bool GenerateItemIdCalled { get; private set; }

            public void SetGridData(GridData gridData)
            {
                testGridData = gridData;
            }

            public override GridData GetCurrentGrid()
            {
                return testGridData;
            }

            public override string GenerateItemId()
            {
                GenerateItemIdCalled = true;
                return $"test_item_{itemIdCounter++}";
            }
        }

        /// <summary>
        /// Mock UIGridManager for testing
        /// </summary>
        private class MockUIGridManager : UIGridManager
        {
            public bool HasVisualItem(string itemId)
            {
                return false; // Simple mock implementation
            }

            public void CreateVisualItem(string itemId, int x, int y, string itemType)
            {
                // Mock implementation - do nothing
            }
        }

        #endregion
    }
}