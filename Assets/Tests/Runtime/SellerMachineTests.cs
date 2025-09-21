using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace ScrapLine.Tests
{
    /// <summary>
    /// Unit tests for SellerMachine.
    /// Tests item selling functionality, credits awarding, and item removal.
    /// </summary>
    [TestFixture]
    public class SellerMachineTests
    {
        private SellerMachine sellerMachine;
        private CellData cellData;
        private MachineDef machineDef;
        private GameObject gameManagerObject;
        private MockGameManager mockGameManager;
        private FactoryRegistry factoryRegistry;

        [SetUp]
        public void SetUp()
        {
            // Arrange - Set up test data
            cellData = new CellData
            {
                x = 1,
                y = 1,
                direction = UICell.Direction.Up,
                items = new List<ItemData>(),
                waitingItems = new List<ItemData>()
            };

            machineDef = new MachineDef
            {
                id = "seller",
                type = "seller",
                baseProcessTime = 1.0f
            };

            // Set up mock GameManager and FactoryRegistry
            SetupMockGameManager();
            SetupFactoryRegistry();
            
            sellerMachine = new SellerMachine(cellData, machineDef);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up
            if (gameManagerObject != null)
            {
                Object.DestroyImmediate(gameManagerObject);
            }
            
            // Clear FactoryRegistry
            factoryRegistry.Items.Clear();
        }

        #region Constructor Tests

        [Test]
        public void Constructor_ValidParameters_InitializesCorrectly()
        {
            // Arrange & Act - Done in SetUp

            // Assert
            Assert.AreEqual(cellData, sellerMachine.GetCellData(), "CellData should be set correctly");
            Assert.AreEqual(machineDef, sellerMachine.GetMachineDef(), "MachineDef should be set correctly");
        }

        [Test]
        public void Constructor_InheritsFromBaseMachine_IsBaseMachine()
        {
            // Arrange & Act & Assert
            Assert.IsInstanceOf<BaseMachine>(sellerMachine, "SellerMachine should inherit from BaseMachine");
        }

        #endregion

        #region UpdateLogic Tests

        [Test]
        public void UpdateLogic_NoItems_DoesNotThrow()
        {
            // Arrange - No items in cell

            // Act & Assert
            Assert.DoesNotThrow(() => sellerMachine.UpdateLogic(), "UpdateLogic should not throw with no items");
        }

        [Test]
        public void UpdateLogic_IdleItem_SellsItem()
        {
            // Arrange
            var item = new ItemData
            {
                id = "sellable_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle
            };
            cellData.items.Add(item);

            // Act
            sellerMachine.UpdateLogic();

            // Assert
            Assert.AreEqual(0, cellData.items.Count, "Item should be removed from cell after selling");
            Assert.IsTrue(mockGameManager.AddCreditsCalled, "Credits should be added to player");
            Assert.AreEqual(25, mockGameManager.LastCreditsAdded, "Should add correct credits for can");
        }

        [Test]
        public void UpdateLogic_MovingItem_DoesNotSell()
        {
            // Arrange
            var item = new ItemData
            {
                id = "moving_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Moving
            };
            cellData.items.Add(item);

            // Act
            sellerMachine.UpdateLogic();

            // Assert
            Assert.AreEqual(1, cellData.items.Count, "Moving item should not be sold");
            Assert.IsFalse(mockGameManager.AddCreditsCalled, "No credits should be added for moving item");
        }

        [Test]
        public void UpdateLogic_ProcessingItem_DoesNotSell()
        {
            // Arrange
            var item = new ItemData
            {
                id = "processing_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Processing
            };
            cellData.items.Add(item);

            // Act
            sellerMachine.UpdateLogic();

            // Assert
            Assert.AreEqual(1, cellData.items.Count, "Processing item should not be sold");
            Assert.IsFalse(mockGameManager.AddCreditsCalled, "No credits should be added for processing item");
        }

        [Test]
        public void UpdateLogic_WaitingItem_DoesNotSell()
        {
            // Arrange
            var item = new ItemData
            {
                id = "waiting_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Waiting
            };
            cellData.items.Add(item);

            // Act
            sellerMachine.UpdateLogic();

            // Assert
            Assert.AreEqual(1, cellData.items.Count, "Waiting item should not be sold");
            Assert.IsFalse(mockGameManager.AddCreditsCalled, "No credits should be added for waiting item");
        }

        [Test]
        public void UpdateLogic_MultipleIdleItems_SellsAllIdleItems()
        {
            // Arrange
            var item1 = new ItemData { id = "item1", itemType = "can", x = cellData.x, y = cellData.y, state = ItemState.Idle };
            var item2 = new ItemData { id = "item2", itemType = "metal", x = cellData.x, y = cellData.y, state = ItemState.Idle };
            var item3 = new ItemData { id = "item3", itemType = "can", x = cellData.x, y = cellData.y, state = ItemState.Moving };
            
            cellData.items.Add(item1);
            cellData.items.Add(item2);
            cellData.items.Add(item3);

            // Act
            sellerMachine.UpdateLogic();

            // Assert
            Assert.AreEqual(1, cellData.items.Count, "Only moving item should remain");
            Assert.AreEqual("item3", cellData.items[0].id, "Moving item should be the one remaining");
            Assert.IsTrue(mockGameManager.AddCreditsCalled, "Credits should be added for sold items");
        }

        #endregion

        #region OnItemArrived Tests

        [Test]
        public void OnItemArrived_ValidItem_SellsImmediately()
        {
            // Arrange
            var item = new ItemData
            {
                id = "arriving_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle
            };
            cellData.items.Add(item); // Simulate item being in cell

            // Act
            sellerMachine.OnItemArrived(item);

            // Assert
            Assert.AreEqual(0, cellData.items.Count, "Item should be sold immediately on arrival");
            Assert.IsTrue(mockGameManager.AddCreditsCalled, "Credits should be added immediately");
            Assert.AreEqual(25, mockGameManager.LastCreditsAdded, "Should add correct credits for can");
        }

        [Test]
        public void OnItemArrived_HighValueItem_AwardsCorrectCredits()
        {
            // Arrange
            var item = new ItemData
            {
                id = "high_value_item",
                itemType = "shredded_aluminum",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle
            };
            cellData.items.Add(item);

            // Act
            sellerMachine.OnItemArrived(item);

            // Assert
            Assert.AreEqual(0, cellData.items.Count, "High value item should be sold");
            Assert.IsTrue(mockGameManager.AddCreditsCalled, "Credits should be added for high value item");
            Assert.AreEqual(50, mockGameManager.LastCreditsAdded, "Should add correct credits for shredded aluminum");
        }

        [Test]
        public void OnItemArrived_ZeroValueItem_DoesNotAwardCredits()
        {
            // Arrange
            var item = new ItemData
            {
                id = "zero_value_item",
                itemType = "trash",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle
            };
            cellData.items.Add(item);

            // Act
            sellerMachine.OnItemArrived(item);

            // Assert
            Assert.AreEqual(0, cellData.items.Count, "Zero value item should still be removed");
            Assert.IsFalse(mockGameManager.AddCreditsCalled, "No credits should be added for zero value item");
        }

        [Test]
        public void OnItemArrived_UnknownItemType_DoesNotAwardCredits()
        {
            // Arrange
            var item = new ItemData
            {
                id = "unknown_item",
                itemType = "unknown_type",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle
            };
            cellData.items.Add(item);

            // Act
            sellerMachine.OnItemArrived(item);

            // Assert
            Assert.AreEqual(0, cellData.items.Count, "Unknown item should still be removed");
            Assert.IsFalse(mockGameManager.AddCreditsCalled, "No credits should be added for unknown item");
        }

        [Test]
        public void OnItemArrived_NullItem_DoesNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => sellerMachine.OnItemArrived(null), 
                "OnItemArrived should handle null item gracefully");
        }

        #endregion

        #region ProcessItem Tests

        [Test]
        public void ProcessItem_ValidItem_SellsItem()
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
            cellData.items.Add(item);

            // Act
            sellerMachine.ProcessItem(item);

            // Assert
            Assert.AreEqual(0, cellData.items.Count, "Item should be sold when processed");
            Assert.IsTrue(mockGameManager.AddCreditsCalled, "Credits should be added when processing item");
            Assert.AreEqual(25, mockGameManager.LastCreditsAdded, "Should add correct credits for can");
        }

        [Test]
        public void ProcessItem_NullItem_DoesNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => sellerMachine.ProcessItem(null), 
                "ProcessItem should handle null item gracefully");
        }

        #endregion

        #region Selling Logic Tests

        [Test]
        public void SellItem_RemovesItemFromCell()
        {
            // Arrange
            var item = new ItemData
            {
                id = "remove_test_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle
            };
            cellData.items.Add(item);

            // Act
            sellerMachine.OnItemArrived(item);

            // Assert
            Assert.AreEqual(0, cellData.items.Count, "Item should be removed from cell");
            Assert.IsTrue(cellData.items.Find(i => i.id == "remove_test_item") == null, 
                "Specific item should no longer be in cell");
        }

        [Test]
        public void SellItem_MultipleItemTypes_AwardsCorrectCreditsForEach()
        {
            // Arrange & Act & Assert for can
            var canItem = new ItemData { id = "can_item", itemType = "can", x = cellData.x, y = cellData.y, state = ItemState.Idle };
            cellData.items.Add(canItem);
            mockGameManager.Reset();
            
            sellerMachine.OnItemArrived(canItem);
            Assert.AreEqual(25, mockGameManager.LastCreditsAdded, "Can should be worth 25 credits");

            // Arrange & Act & Assert for shredded aluminum
            var shreddedItem = new ItemData { id = "shredded_item", itemType = "shredded_aluminum", x = cellData.x, y = cellData.y, state = ItemState.Idle };
            cellData.items.Add(shreddedItem);
            mockGameManager.Reset();
            
            sellerMachine.OnItemArrived(shreddedItem);
            Assert.AreEqual(50, mockGameManager.LastCreditsAdded, "Shredded aluminum should be worth 50 credits");

            // Arrange & Act & Assert for metal
            var metalItem = new ItemData { id = "metal_item", itemType = "metal", x = cellData.x, y = cellData.y, state = ItemState.Idle };
            cellData.items.Add(metalItem);
            mockGameManager.Reset();
            
            sellerMachine.OnItemArrived(metalItem);
            Assert.AreEqual(10, mockGameManager.LastCreditsAdded, "Metal should be worth 10 credits");
        }

        #endregion

        #region Integration Tests

        [Test]
        public void Integration_SellerWorkflow_WorksCorrectly()
        {
            // Arrange
            var item = new ItemData
            {
                id = "workflow_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle
            };

            // Act - Add item to cell and process through seller workflow
            cellData.items.Add(item);
            sellerMachine.OnItemArrived(item);
            sellerMachine.UpdateLogic(); // Should not process item again since it's already sold

            // Assert
            Assert.AreEqual(0, cellData.items.Count, "Item should be sold and removed");
            Assert.IsTrue(mockGameManager.AddCreditsCalled, "Credits should be awarded");
            Assert.AreEqual(25, mockGameManager.LastCreditsAdded, "Correct credits should be awarded");
        }

        [Test]
        public void Integration_MultipleItemsSoldSequentially_AccumulatesCredits()
        {
            // Arrange
            var items = new List<ItemData>
            {
                new ItemData { id = "item1", itemType = "can", x = cellData.x, y = cellData.y, state = ItemState.Idle },
                new ItemData { id = "item2", itemType = "shredded_aluminum", x = cellData.x, y = cellData.y, state = ItemState.Idle },
                new ItemData { id = "item3", itemType = "metal", x = cellData.x, y = cellData.y, state = ItemState.Idle }
            };

            int totalCreditsAwarded = 0;

            // Act - Sell each item sequentially
            foreach (var item in items)
            {
                cellData.items.Add(item);
                mockGameManager.Reset();
                sellerMachine.OnItemArrived(item);
                totalCreditsAwarded += mockGameManager.LastCreditsAdded;
            }

            // Assert
            Assert.AreEqual(0, cellData.items.Count, "All items should be sold");
            Assert.AreEqual(85, totalCreditsAwarded, "Total credits should be 25 + 50 + 10 = 85");
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
        /// Sets up FactoryRegistry with test item definitions
        /// </summary>
        private void SetupFactoryRegistry()
        {
            factoryRegistry = FactoryRegistry.Instance;
            factoryRegistry.Items.Clear();
            
            // Add test item definitions
            factoryRegistry.Items["can"] = new ItemDef { id = "can", displayName = "Aluminum Can", sellValue = 25 };
            factoryRegistry.Items["shredded_aluminum"] = new ItemDef { id = "shredded_aluminum", displayName = "Shredded Aluminum", sellValue = 50 };
            factoryRegistry.Items["metal"] = new ItemDef { id = "metal", displayName = "Metal Scrap", sellValue = 10 };
            factoryRegistry.Items["trash"] = new ItemDef { id = "trash", displayName = "Trash", sellValue = 0 };
        }

        /// <summary>
        /// Mock GameManager for testing
        /// </summary>
        private class MockGameManager : GameManager
        {
            private GridData testGridData;
            
            public bool AddCreditsCalled { get; private set; }
            public int LastCreditsAdded { get; private set; }

            public void SetGridData(GridData gridData)
            {
                testGridData = gridData;
            }

            public override GridData GetCurrentGrid()
            {
                return testGridData;
            }

            public override void AddCredits(int amount)
            {
                AddCreditsCalled = true;
                LastCreditsAdded = amount;
            }

            public void Reset()
            {
                AddCreditsCalled = false;
                LastCreditsAdded = 0;
            }
        }

        /// <summary>
        /// Mock UIGridManager for testing
        /// </summary>
        private class MockUIGridManager : UIGridManager
        {
            public bool HasVisualItem(string itemId)
            {
                return true; // Assume visual items exist for destruction testing
            }

            public void CreateVisualItem(string itemId, int x, int y, string itemType)
            {
                // Mock implementation - do nothing
            }

            public void DestroyVisualItem(string itemId)
            {
                // Mock implementation - do nothing
            }
        }

        #endregion
    }
}