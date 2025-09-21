using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace ScrapLine.Tests
{
    /// <summary>
    /// Unit tests for BlankCellMachine.
    /// Tests blank cell behavior including item timeout and temporary storage.
    /// </summary>
    [TestFixture]
    public class BlankCellMachineTests
    {
        private BlankCellMachine blankCellMachine;
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
                y = 3,
                direction = UICell.Direction.Up,
                items = new List<ItemData>(),
                waitingItems = new List<ItemData>()
            };

            machineDef = new MachineDef
            {
                id = "blank_cell",
                type = "blank",
                baseProcessTime = 0.0f
            };

            // Set up mock GameManager
            SetupMockGameManager();
            
            blankCellMachine = new BlankCellMachine(cellData, machineDef);
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
            Assert.AreEqual(cellData, blankCellMachine.GetCellData(), "CellData should be set correctly");
            Assert.AreEqual(machineDef, blankCellMachine.GetMachineDef(), "MachineDef should be set correctly");
        }

        [Test]
        public void Constructor_InheritsFromBaseMachine_IsBaseMachine()
        {
            // Arrange & Act & Assert
            Assert.IsInstanceOf<BaseMachine>(blankCellMachine, "BlankCellMachine should inherit from BaseMachine");
        }

        #endregion

        #region UpdateLogic Tests

        [Test]
        public void UpdateLogic_NoItems_DoesNotThrow()
        {
            // Arrange - No items in cell

            // Act & Assert
            Assert.DoesNotThrow(() => blankCellMachine.UpdateLogic(), "UpdateLogic should not throw with no items");
        }

        [Test]
        public void UpdateLogic_RecentIdleItem_DoesNotRemove()
        {
            // Arrange
            var item = new ItemData
            {
                id = "recent_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle,
                moveStartTime = Time.time // Recent timestamp
            };
            cellData.items.Add(item);

            // Act
            blankCellMachine.UpdateLogic();

            // Assert
            Assert.AreEqual(1, cellData.items.Count, "Recent item should not be removed");
            Assert.AreEqual("recent_item", cellData.items[0].id, "Item should remain in cell");
        }

        [Test]
        public void UpdateLogic_MovingItem_DoesNotRemove()
        {
            // Arrange
            var item = new ItemData
            {
                id = "moving_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Moving,
                moveStartTime = Time.time - 15f // Old timestamp, but item is moving
            };
            cellData.items.Add(item);

            // Act
            blankCellMachine.UpdateLogic();

            // Assert
            Assert.AreEqual(1, cellData.items.Count, "Moving item should not be removed regardless of time");
            Assert.AreEqual(ItemState.Moving, cellData.items[0].state, "Item should remain in moving state");
        }

        [Test]
        public void UpdateLogic_ProcessingItem_DoesNotRemove()
        {
            // Arrange
            var item = new ItemData
            {
                id = "processing_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Processing,
                moveStartTime = Time.time - 15f // Old timestamp, but item is processing
            };
            cellData.items.Add(item);

            // Act
            blankCellMachine.UpdateLogic();

            // Assert
            Assert.AreEqual(1, cellData.items.Count, "Processing item should not be removed regardless of time");
            Assert.AreEqual(ItemState.Processing, cellData.items[0].state, "Item should remain in processing state");
        }

        [Test]
        public void UpdateLogic_WaitingItem_DoesNotRemove()
        {
            // Arrange
            var item = new ItemData
            {
                id = "waiting_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Waiting,
                moveStartTime = Time.time - 15f // Old timestamp, but item is waiting
            };
            cellData.items.Add(item);

            // Act
            blankCellMachine.UpdateLogic();

            // Assert
            Assert.AreEqual(1, cellData.items.Count, "Waiting item should not be removed regardless of time");
            Assert.AreEqual(ItemState.Waiting, cellData.items[0].state, "Item should remain in waiting state");
        }

        [Test]
        public void UpdateLogic_OldIdleItem_RemovesItem()
        {
            // Arrange
            var item = new ItemData
            {
                id = "old_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle,
                moveStartTime = Time.time - 15f // Old timestamp (more than 10 seconds + 1 second buffer)
            };
            cellData.items.Add(item);

            // Act
            blankCellMachine.UpdateLogic();

            // Assert
            Assert.AreEqual(0, cellData.items.Count, "Old idle item should be removed");
        }

        [Test]
        public void UpdateLogic_MultipleItems_RemovesOnlyTimedOutIdleItems()
        {
            // Arrange
            var recentItem = new ItemData
            {
                id = "recent_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle,
                moveStartTime = Time.time // Recent
            };

            var oldItem = new ItemData
            {
                id = "old_item",
                itemType = "metal",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle,
                moveStartTime = Time.time - 15f // Old
            };

            var movingItem = new ItemData
            {
                id = "moving_item",
                itemType = "plastic",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Moving,
                moveStartTime = Time.time - 20f // Very old, but moving
            };

            cellData.items.Add(recentItem);
            cellData.items.Add(oldItem);
            cellData.items.Add(movingItem);

            // Act
            blankCellMachine.UpdateLogic();

            // Assert
            Assert.AreEqual(2, cellData.items.Count, "Should have 2 items remaining (recent and moving)");
            Assert.IsTrue(cellData.items.Exists(i => i.id == "recent_item"), "Recent item should remain");
            Assert.IsTrue(cellData.items.Exists(i => i.id == "moving_item"), "Moving item should remain");
            Assert.IsFalse(cellData.items.Exists(i => i.id == "old_item"), "Old idle item should be removed");
        }

        [Test]
        public void UpdateLogic_ItemWithZeroMoveStartTime_UsesCurrentTimeForCalculation()
        {
            // Arrange
            var item = new ItemData
            {
                id = "zero_time_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle,
                moveStartTime = 0f // Zero timestamp
            };
            cellData.items.Add(item);

            // Act
            blankCellMachine.UpdateLogic();

            // Assert
            Assert.AreEqual(1, cellData.items.Count, "Item with zero timestamp should not be removed immediately");
        }

        #endregion

        #region OnItemArrived Tests

        [Test]
        public void OnItemArrived_AnyItem_DoesNotThrow()
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

            // Act & Assert
            Assert.DoesNotThrow(() => blankCellMachine.OnItemArrived(item), 
                "OnItemArrived should not throw for any item");
        }

        [Test]
        public void OnItemArrived_ValidItem_DoesNotModifyItem()
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
            blankCellMachine.OnItemArrived(item);

            // Assert
            Assert.AreEqual("test_item", item.id, "Item ID should not change");
            Assert.AreEqual("can", item.itemType, "Item type should not change");
            Assert.AreEqual(ItemState.Idle, item.state, "Item state should not change");
            Assert.AreEqual(0.5f, item.moveProgress, "Item progress should not change");
        }

        [Test]
        public void OnItemArrived_NullItem_DoesNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => blankCellMachine.OnItemArrived(null), 
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
                id = "process_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle
            };

            // Act & Assert
            Assert.DoesNotThrow(() => blankCellMachine.ProcessItem(item), 
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
                moveProgress = 0.3f
            };

            // Act
            blankCellMachine.ProcessItem(item);

            // Assert
            Assert.AreEqual("test_item", item.id, "Item ID should not change");
            Assert.AreEqual("can", item.itemType, "Item type should not change");
            Assert.AreEqual(ItemState.Idle, item.state, "Item state should not change");
            Assert.AreEqual(0.3f, item.moveProgress, "Item progress should not change");
        }

        [Test]
        public void ProcessItem_NullItem_DoesNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => blankCellMachine.ProcessItem(null), 
                "ProcessItem should handle null item gracefully");
        }

        #endregion

        #region Timeout Configuration Tests

        [Test]
        public void TimeoutConfiguration_HasReasonableDefault()
        {
            // Arrange & Act
            // The timeout duration is private, but we can test the behavior
            var item = new ItemData
            {
                id = "timeout_test_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle,
                moveStartTime = Time.time - 5f // 5 seconds ago (should not timeout yet)
            };
            cellData.items.Add(item);

            blankCellMachine.UpdateLogic();

            // Assert
            Assert.AreEqual(1, cellData.items.Count, "Item should not timeout after only 5 seconds");
        }

        [Test]
        public void TimeoutConfiguration_EventuallyTimesOut()
        {
            // Arrange
            var item = new ItemData
            {
                id = "timeout_test_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle,
                moveStartTime = Time.time - 15f // 15 seconds ago (should timeout)
            };
            cellData.items.Add(item);

            // Act
            blankCellMachine.UpdateLogic();

            // Assert
            Assert.AreEqual(0, cellData.items.Count, "Item should timeout after 15 seconds");
        }

        #endregion

        #region Edge Cases Tests

        [Test]
        public void EdgeCase_NegativeMoveStartTime_HandledGracefully()
        {
            // Arrange
            var item = new ItemData
            {
                id = "negative_time_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle,
                moveStartTime = -10f // Negative timestamp
            };
            cellData.items.Add(item);

            // Act & Assert
            Assert.DoesNotThrow(() => blankCellMachine.UpdateLogic(), 
                "Should handle negative timestamps gracefully");
            // Item behavior with negative timestamp depends on implementation
        }

        [Test]
        public void EdgeCase_VeryLargeMoveStartTime_HandledGracefully()
        {
            // Arrange
            var item = new ItemData
            {
                id = "future_time_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle,
                moveStartTime = Time.time + 1000f // Future timestamp
            };
            cellData.items.Add(item);

            // Act & Assert
            Assert.DoesNotThrow(() => blankCellMachine.UpdateLogic(), 
                "Should handle future timestamps gracefully");
            Assert.AreEqual(1, cellData.items.Count, "Future timestamped item should not be removed");
        }

        [Test]
        public void EdgeCase_EmptyItemId_HandledGracefully()
        {
            // Arrange
            var item = new ItemData
            {
                id = "", // Empty ID
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle,
                moveStartTime = Time.time - 15f
            };
            cellData.items.Add(item);

            // Act & Assert
            Assert.DoesNotThrow(() => blankCellMachine.UpdateLogic(), 
                "Should handle empty item ID gracefully");
        }

        [Test]
        public void EdgeCase_NullItemType_HandledGracefully()
        {
            // Arrange
            var item = new ItemData
            {
                id = "null_type_item",
                itemType = null, // Null item type
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle,
                moveStartTime = Time.time - 15f
            };
            cellData.items.Add(item);

            // Act & Assert
            Assert.DoesNotThrow(() => blankCellMachine.UpdateLogic(), 
                "Should handle null item type gracefully");
        }

        #endregion

        #region Integration Tests

        [Test]
        public void Integration_BlankCellWorkflow_WorksCorrectly()
        {
            // Arrange
            var item = new ItemData
            {
                id = "workflow_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle,
                moveStartTime = Time.time
            };

            // Act - Item arrives at blank cell
            cellData.items.Add(item);
            blankCellMachine.OnItemArrived(item);

            // Should remain in cell initially
            blankCellMachine.UpdateLogic();
            Assert.AreEqual(1, cellData.items.Count, "Item should remain initially");

            // Simulate time passing - set old timestamp
            item.moveStartTime = Time.time - 15f;
            blankCellMachine.UpdateLogic();

            // Assert
            Assert.AreEqual(0, cellData.items.Count, "Item should eventually timeout and be removed");
        }

        [Test]
        public void Integration_MultipleItemsWithDifferentTimestamps_ProcessedCorrectly()
        {
            // Arrange
            var items = new List<ItemData>
            {
                new ItemData { id = "item1", itemType = "can", x = cellData.x, y = cellData.y, state = ItemState.Idle, moveStartTime = Time.time },
                new ItemData { id = "item2", itemType = "metal", x = cellData.x, y = cellData.y, state = ItemState.Idle, moveStartTime = Time.time - 8f },
                new ItemData { id = "item3", itemType = "plastic", x = cellData.x, y = cellData.y, state = ItemState.Idle, moveStartTime = Time.time - 15f }
            };

            // Act
            foreach (var item in items)
            {
                cellData.items.Add(item);
                blankCellMachine.OnItemArrived(item);
            }

            blankCellMachine.UpdateLogic();

            // Assert
            Assert.AreEqual(2, cellData.items.Count, "Should have 2 items remaining (recent ones)");
            Assert.IsTrue(cellData.items.Exists(i => i.id == "item1"), "Most recent item should remain");
            Assert.IsTrue(cellData.items.Exists(i => i.id == "item2"), "Moderately old item should remain");
            Assert.IsFalse(cellData.items.Exists(i => i.id == "item3"), "Oldest item should be removed");
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