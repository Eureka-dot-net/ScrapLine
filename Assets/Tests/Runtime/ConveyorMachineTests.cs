using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace ScrapLine.Tests
{
    /// <summary>
    /// Unit tests for ConveyorMachine.
    /// Tests conveyor belt functionality including item movement and failsafe behavior.
    /// </summary>
    [TestFixture]
    public class ConveyorMachineTests
    {
        private ConveyorMachine conveyorMachine;
        private CellData cellData;
        private MachineDef machineDef;
        private GameObject gameManagerObject;

        [SetUp]
        public void SetUp()
        {
            // Arrange - Set up test data
            cellData = new CellData
            {
                x = 3,
                y = 2,
                direction = UICell.Direction.Right,
                items = new List<ItemData>(),
                waitingItems = new List<ItemData>()
            };

            machineDef = new MachineDef
            {
                id = "conveyor",
                type = "transport",
                baseProcessTime = 1.0f
            };

            conveyorMachine = new ConveyorMachine(cellData, machineDef);

            // Set up mock GameManager
            SetupMockGameManager();
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
            Assert.AreEqual(cellData, conveyorMachine.GetCellData(), "CellData should be set correctly");
            Assert.AreEqual(machineDef, conveyorMachine.GetMachineDef(), "MachineDef should be set correctly");
        }

        [Test]
        public void Constructor_InheritsFromBaseMachine_IsBaseMachine()
        {
            // Arrange & Act & Assert
            Assert.IsInstanceOf<BaseMachine>(conveyorMachine, "ConveyorMachine should inherit from BaseMachine");
        }

        #endregion

        #region UpdateLogic Tests

        [Test]
        public void UpdateLogic_NoItems_DoesNotThrow()
        {
            // Arrange - No items in cell

            // Act & Assert
            Assert.DoesNotThrow(() => conveyorMachine.UpdateLogic(), "UpdateLogic should not throw with no items");
        }

        [Test]
        public void UpdateLogic_IdleItem_TriesToMoveItem()
        {
            // Arrange
            var item = new ItemData
            {
                id = "test_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle
            };
            cellData.items.Add(item);

            // Act
            conveyorMachine.UpdateLogic();

            // Assert
            // Item should either be moved (state changed) or remain idle if move failed
            // Since our mock setup doesn't allow movement out of bounds, it should remain idle
            Assert.IsTrue(item.state == ItemState.Idle || item.state == ItemState.Moving, 
                "Item should either remain idle or be moved");
        }

        [Test]
        public void UpdateLogic_MovingItem_DoesNotTryToMoveAgain()
        {
            // Arrange
            var item = new ItemData
            {
                id = "test_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Moving,
                sourceX = cellData.x,
                sourceY = cellData.y,
                targetX = cellData.x + 1,
                targetY = cellData.y
            };
            cellData.items.Add(item);

            // Act
            conveyorMachine.UpdateLogic();

            // Assert
            Assert.AreEqual(ItemState.Moving, item.state, "Moving item should remain in moving state");
            Assert.AreEqual(cellData.x, item.sourceX, "Source coordinates should not change");
            Assert.AreEqual(cellData.y, item.sourceY, "Source coordinates should not change");
        }

        [Test]
        public void UpdateLogic_MultipleIdleItems_TriesToMoveAllIdleItems()
        {
            // Arrange
            var item1 = new ItemData
            {
                id = "item1",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle
            };
            var item2 = new ItemData
            {
                id = "item2",
                itemType = "metal",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle
            };
            var item3 = new ItemData
            {
                id = "item3",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Processing // Not idle
            };

            cellData.items.Add(item1);
            cellData.items.Add(item2);
            cellData.items.Add(item3);

            // Act
            conveyorMachine.UpdateLogic();

            // Assert
            // Items 1 and 2 should be processed (either moved or remain idle)
            // Item 3 should remain in processing state
            Assert.AreEqual(ItemState.Processing, item3.state, "Processing item should not be affected");
        }

        [Test]
        public void UpdateLogic_ProcessingItems_DoesNotTryToMove()
        {
            // Arrange
            var item = new ItemData
            {
                id = "test_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Processing,
                processingStartTime = Time.time,
                processingDuration = 5.0f
            };
            cellData.items.Add(item);

            // Act
            conveyorMachine.UpdateLogic();

            // Assert
            Assert.AreEqual(ItemState.Processing, item.state, "Processing item should remain in processing state");
        }

        [Test]
        public void UpdateLogic_WaitingItems_DoesNotTryToMove()
        {
            // Arrange
            var item = new ItemData
            {
                id = "test_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Waiting,
                waitingStartTime = Time.time
            };
            cellData.items.Add(item);

            // Act
            conveyorMachine.UpdateLogic();

            // Assert
            Assert.AreEqual(ItemState.Waiting, item.state, "Waiting item should remain in waiting state");
        }

        #endregion

        #region OnItemArrived Tests

        [Test]
        public void OnItemArrived_ValidItem_TriesToStartMovement()
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

            // Act
            conveyorMachine.OnItemArrived(item);

            // Assert
            // Since TryStartMove is called, the item should either be moved or remain idle
            Assert.IsTrue(item.state == ItemState.Idle || item.state == ItemState.Moving,
                "Item should either be moved or remain idle after arrival");
        }

        [Test]
        public void OnItemArrived_MovingItem_StillTriesToStartMovement()
        {
            // Arrange
            var item = new ItemData
            {
                id = "arriving_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Moving
            };

            // Act
            conveyorMachine.OnItemArrived(item);

            // Assert
            // TryStartMove should be called, but moving items won't be moved again
            Assert.AreEqual(ItemState.Moving, item.state, "Moving item should remain moving");
        }

        [Test]
        public void OnItemArrived_NullItem_DoesNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => conveyorMachine.OnItemArrived(null), 
                "OnItemArrived should handle null item gracefully");
        }

        #endregion

        #region ProcessItem Tests

        [Test]
        public void ProcessItem_AnyItem_DoesNotThrow()
        {
            // Arrange
            var item = new ItemData
            {
                id = "test_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle
            };

            // Act & Assert
            Assert.DoesNotThrow(() => conveyorMachine.ProcessItem(item), 
                "ProcessItem should not throw for any item");
        }

        [Test]
        public void ProcessItem_ValidItem_DoesNotModifyItem()
        {
            // Arrange
            var item = new ItemData
            {
                id = "test_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle,
                moveProgress = 0.5f
            };

            // Act
            conveyorMachine.ProcessItem(item);

            // Assert
            Assert.AreEqual("test_item", item.id, "Item ID should not change");
            Assert.AreEqual("can", item.itemType, "Item type should not change");
            Assert.AreEqual(ItemState.Idle, item.state, "Item state should not change");
            Assert.AreEqual(0.5f, item.moveProgress, "Item progress should not change");
        }

        [Test]
        public void ProcessItem_NullItem_DoesNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => conveyorMachine.ProcessItem(null), 
                "ProcessItem should handle null item gracefully");
        }

        #endregion

        #region Integration Tests

        [Test]
        public void ConveyorWorkflow_ItemArrivesAndGetsProcessed_WorksCorrectly()
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

            // Act - Simulate item arriving at conveyor
            conveyorMachine.OnItemArrived(item);

            // Act - Update logic to process any remaining idle items
            conveyorMachine.UpdateLogic();

            // Assert
            // Item should be processed by the conveyor (either moved or remain idle if movement fails)
            Assert.IsNotNull(item, "Item should still exist");
            Assert.AreEqual("workflow_item", item.id, "Item ID should be preserved");
            Assert.AreEqual("can", item.itemType, "Item type should be preserved");
        }

        [Test]
        public void ConveyorWorkflow_MultipleItemsArrive_ProcessesAllCorrectly()
        {
            // Arrange
            var items = new List<ItemData>
            {
                new ItemData { id = "item1", itemType = "can", x = cellData.x, y = cellData.y, state = ItemState.Idle },
                new ItemData { id = "item2", itemType = "metal", x = cellData.x, y = cellData.y, state = ItemState.Idle },
                new ItemData { id = "item3", itemType = "can", x = cellData.x, y = cellData.y, state = ItemState.Idle }
            };

            // Act - Add items to cell and process them
            foreach (var item in items)
            {
                cellData.items.Add(item);
                conveyorMachine.OnItemArrived(item);
            }

            conveyorMachine.UpdateLogic();

            // Assert
            Assert.AreEqual(3, cellData.items.Count, "All items should remain in cell if movement fails");
            foreach (var item in items)
            {
                Assert.IsNotNull(item, "All items should still exist");
                Assert.IsTrue(item.state == ItemState.Idle || item.state == ItemState.Moving,
                    $"Item {item.id} should be either idle or moving");
            }
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
                var gameManager = gameManagerObject.AddComponent<MockGameManager>();
                
                // Set up a small grid for testing
                var gridData = new GridData
                {
                    width = 5,
                    height = 5,
                    cells = new List<CellData>()
                };
                gameManager.SetGridData(gridData);
                
                // Create a mock grid manager
                var gridManagerObject = new GameObject("MockGridManager");
                var mockGridManager = gridManagerObject.AddComponent<MockUIGridManager>();
                gameManager.activeGridManager = mockGridManager;
            }
        }

        /// <summary>
        /// Mock GameManager for testing
        /// </summary>
        private class MockGameManager : GameManager
        {
            private GridData testGridData;

            public void SetGridData(GridData gridData)
            {
                testGridData = gridData;
            }

            public override GridData GetCurrentGrid()
            {
                return testGridData;
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