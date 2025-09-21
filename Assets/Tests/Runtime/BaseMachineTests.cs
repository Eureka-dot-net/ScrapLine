using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace ScrapLine.Tests
{
    /// <summary>
    /// Unit tests for BaseMachine abstract class.
    /// Tests common machine functionality including item processing, movement, and waiting queue management.
    /// Uses concrete test implementation to test abstract functionality.
    /// </summary>
    [TestFixture]
    public class BaseMachineTests
    {
        private TestConcreteMachine machine;
        private CellData cellData;
        private MachineDef machineDef;
        private GridData gridData;

        [SetUp]
        public void SetUp()
        {
            // Arrange - Set up test data
            cellData = new CellData
            {
                x = 5,
                y = 3,
                direction = UICell.Direction.Up,
                items = new List<ItemData>(),
                waitingItems = new List<ItemData>()
            };

            machineDef = new MachineDef
            {
                id = "test_machine",
                type = "test",
                baseProcessTime = 2.0f
            };

            // Set up grid data for boundary checking
            gridData = new GridData
            {
                width = 10,
                height = 8,
                cells = new List<CellData>()
            };

            // Create test machine
            machine = new TestConcreteMachine(cellData, machineDef);

            // Set up mock GameManager if needed (simplified approach)
            SetupMockGameManager();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up any GameManager instance
            if (GameManager.Instance != null)
            {
                Object.DestroyImmediate(GameManager.Instance.gameObject);
            }
        }

        #region Constructor Tests

        [Test]
        public void Constructor_ValidParameters_InitializesCorrectly()
        {
            // Arrange & Act - Done in SetUp
            
            // Assert
            Assert.AreEqual(cellData, machine.GetCellData(), "CellData should be set correctly");
            Assert.AreEqual(machineDef, machine.GetMachineDef(), "MachineDef should be set correctly");
        }

        [Test]
        public void Constructor_NullCellData_StoresNull()
        {
            // Arrange & Act
            var testMachine = new TestConcreteMachine(null, machineDef);

            // Assert
            Assert.IsNull(testMachine.GetCellData(), "Should store null cellData");
            Assert.AreEqual(machineDef, testMachine.GetMachineDef(), "Should still store machineDef");
        }

        [Test]
        public void Constructor_NullMachineDef_StoresNull()
        {
            // Arrange & Act
            var testMachine = new TestConcreteMachine(cellData, null);

            // Assert
            Assert.AreEqual(cellData, testMachine.GetCellData(), "Should still store cellData");
            Assert.IsNull(testMachine.GetMachineDef(), "Should store null machineDef");
        }

        #endregion

        #region Virtual Method Default Implementation Tests

        [Test]
        public void OnItemArrived_DefaultImplementation_DoesNotThrow()
        {
            // Arrange
            var item = new ItemData { id = "test_item", itemType = "can" };

            // Act & Assert
            Assert.DoesNotThrow(() => machine.OnItemArrived(item), "Default OnItemArrived should not throw");
        }

        [Test]
        public void ProcessItem_DefaultImplementation_DoesNotThrow()
        {
            // Arrange
            var item = new ItemData { id = "test_item", itemType = "can" };

            // Act & Assert
            Assert.DoesNotThrow(() => machine.ProcessItem(item), "Default ProcessItem should not throw");
        }

        [Test]
        public void UpdateLogic_DefaultImplementation_DoesNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => machine.UpdateLogic(), "Default UpdateLogic should not throw");
        }

        #endregion

        #region Waiting Items Queue Tests

        [Test]
        public void GetNextWaitingItem_EmptyQueue_ReturnsNull()
        {
            // Arrange - waitingItems is empty by default

            // Act
            var result = machine.TestGetNextWaitingItem();

            // Assert
            Assert.IsNull(result, "Should return null when no waiting items");
        }

        [Test]
        public void GetNextWaitingItem_OneItem_ReturnsItemAndRemovesFromQueue()
        {
            // Arrange
            var item = new ItemData { id = "waiting_item", itemType = "can" };
            cellData.waitingItems.Add(item);

            // Act
            var result = machine.TestGetNextWaitingItem();

            // Assert
            Assert.AreEqual(item, result, "Should return the waiting item");
            Assert.AreEqual(0, cellData.waitingItems.Count, "Should remove item from waiting queue");
        }

        [Test]
        public void GetNextWaitingItem_MultipleItems_ReturnsFIFO()
        {
            // Arrange
            var item1 = new ItemData { id = "item1", itemType = "can" };
            var item2 = new ItemData { id = "item2", itemType = "metal" };
            var item3 = new ItemData { id = "item3", itemType = "can" };
            
            cellData.waitingItems.Add(item1);
            cellData.waitingItems.Add(item2);
            cellData.waitingItems.Add(item3);

            // Act
            var result1 = machine.TestGetNextWaitingItem();
            var result2 = machine.TestGetNextWaitingItem();
            var result3 = machine.TestGetNextWaitingItem();

            // Assert
            Assert.AreEqual(item1, result1, "Should return first item first");
            Assert.AreEqual(item2, result2, "Should return second item second");
            Assert.AreEqual(item3, result3, "Should return third item third");
            Assert.AreEqual(0, cellData.waitingItems.Count, "Queue should be empty after removing all items");
        }

        [Test]
        public void GetNextWaitingItem_CalledMoreThanAvailable_ReturnsNullForExtra()
        {
            // Arrange
            var item = new ItemData { id = "only_item", itemType = "can" };
            cellData.waitingItems.Add(item);

            // Act
            var result1 = machine.TestGetNextWaitingItem();
            var result2 = machine.TestGetNextWaitingItem();

            // Assert
            Assert.AreEqual(item, result1, "Should return the only item");
            Assert.IsNull(result2, "Should return null when queue is empty");
        }

        #endregion

        #region GetNextCellCoordinates Tests

        [Test]
        public void GetNextCellCoordinates_DirectionUp_CalculatesCorrectly()
        {
            // Arrange
            cellData.direction = UICell.Direction.Up;
            cellData.x = 5;
            cellData.y = 3;

            // Act
            machine.TestGetNextCellCoordinates(out int nextX, out int nextY);

            // Assert
            Assert.AreEqual(5, nextX, "X should remain the same for Up direction");
            Assert.AreEqual(2, nextY, "Y should decrease by 1 for Up direction");
        }

        [Test]
        public void GetNextCellCoordinates_DirectionDown_CalculatesCorrectly()
        {
            // Arrange
            cellData.direction = UICell.Direction.Down;
            cellData.x = 5;
            cellData.y = 3;

            // Act
            machine.TestGetNextCellCoordinates(out int nextX, out int nextY);

            // Assert
            Assert.AreEqual(5, nextX, "X should remain the same for Down direction");
            Assert.AreEqual(4, nextY, "Y should increase by 1 for Down direction");
        }

        [Test]
        public void GetNextCellCoordinates_DirectionLeft_CalculatesCorrectly()
        {
            // Arrange
            cellData.direction = UICell.Direction.Left;
            cellData.x = 5;
            cellData.y = 3;

            // Act
            machine.TestGetNextCellCoordinates(out int nextX, out int nextY);

            // Assert
            Assert.AreEqual(4, nextX, "X should decrease by 1 for Left direction");
            Assert.AreEqual(3, nextY, "Y should remain the same for Left direction");
        }

        [Test]
        public void GetNextCellCoordinates_DirectionRight_CalculatesCorrectly()
        {
            // Arrange
            cellData.direction = UICell.Direction.Right;
            cellData.x = 5;
            cellData.y = 3;

            // Act
            machine.TestGetNextCellCoordinates(out int nextX, out int nextY);

            // Assert
            Assert.AreEqual(6, nextX, "X should increase by 1 for Right direction");
            Assert.AreEqual(3, nextY, "Y should remain the same for Right direction");
        }

        [Test]
        public void GetNextCellCoordinates_OutOfBoundsLeft_ReturnsInvalidCoordinates()
        {
            // Arrange
            cellData.direction = UICell.Direction.Left;
            cellData.x = 0; // At left edge
            cellData.y = 3;

            // Act
            machine.TestGetNextCellCoordinates(out int nextX, out int nextY);

            // Assert
            Assert.AreEqual(-1, nextX, "Should return -1 for out of bounds X");
            Assert.AreEqual(-1, nextY, "Should return -1 for out of bounds Y");
        }

        [Test]
        public void GetNextCellCoordinates_OutOfBoundsRight_ReturnsInvalidCoordinates()
        {
            // Arrange
            cellData.direction = UICell.Direction.Right;
            cellData.x = 9; // At right edge (grid width is 10)
            cellData.y = 3;

            // Act
            machine.TestGetNextCellCoordinates(out int nextX, out int nextY);

            // Assert
            Assert.AreEqual(-1, nextX, "Should return -1 for out of bounds X");
            Assert.AreEqual(-1, nextY, "Should return -1 for out of bounds Y");
        }

        [Test]
        public void GetNextCellCoordinates_OutOfBoundsUp_ReturnsInvalidCoordinates()
        {
            // Arrange
            cellData.direction = UICell.Direction.Up;
            cellData.x = 5;
            cellData.y = 0; // At top edge

            // Act
            machine.TestGetNextCellCoordinates(out int nextX, out int nextY);

            // Assert
            Assert.AreEqual(-1, nextX, "Should return -1 for out of bounds X");
            Assert.AreEqual(-1, nextY, "Should return -1 for out of bounds Y");
        }

        [Test]
        public void GetNextCellCoordinates_OutOfBoundsDown_ReturnsInvalidCoordinates()
        {
            // Arrange
            cellData.direction = UICell.Direction.Down;
            cellData.x = 5;
            cellData.y = 7; // At bottom edge (grid height is 8)

            // Act
            machine.TestGetNextCellCoordinates(out int nextX, out int nextY);

            // Assert
            Assert.AreEqual(-1, nextX, "Should return -1 for out of bounds X");
            Assert.AreEqual(-1, nextY, "Should return -1 for out of bounds Y");
        }

        #endregion

        #region TryStartMove Tests

        [Test]
        public void TryStartMove_ValidIdleItem_StartsMovement()
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

            // Act
            machine.TryStartMove(item);

            // Assert
            Assert.AreEqual(ItemState.Moving, item.state, "Item state should be set to Moving");
            Assert.AreEqual(cellData.x, item.sourceX, "SourceX should be set correctly");
            Assert.AreEqual(cellData.y, item.sourceY, "SourceY should be set correctly");
            Assert.AreEqual(cellData.x, item.targetX, "TargetX should be calculated (same as source for Up direction within bounds)");
            Assert.AreEqual(cellData.y - 1, item.targetY, "TargetY should be calculated (up direction)");
        }

        [Test]
        public void TryStartMove_ItemNotAtCorrectPosition_DoesNotStartMovement()
        {
            // Arrange
            var item = new ItemData
            {
                id = "test_item",
                itemType = "can",
                x = cellData.x + 1, // Wrong position
                y = cellData.y,
                state = ItemState.Idle
            };

            // Act
            machine.TryStartMove(item);

            // Assert
            Assert.AreEqual(ItemState.Idle, item.state, "Item state should remain Idle");
        }

        [Test]
        public void TryStartMove_ItemNotIdle_DoesNotStartMovement()
        {
            // Arrange
            var item = new ItemData
            {
                id = "test_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Moving // Already moving
            };

            // Act
            machine.TryStartMove(item);

            // Assert
            Assert.AreEqual(ItemState.Moving, item.state, "Item state should remain Moving");
            Assert.AreEqual(0, item.sourceX, "SourceX should not be modified");
            Assert.AreEqual(0, item.sourceY, "SourceY should not be modified");
        }

        [Test]
        public void TryStartMove_WaitingItem_AllowsMovement()
        {
            // Arrange
            var item = new ItemData
            {
                id = "test_item",
                itemType = "can",
                x = cellData.x + 1, // Different position (waiting items can be in different positions)
                y = cellData.y,
                state = ItemState.Waiting
            };

            // Act
            machine.TryStartMove(item);

            // Assert
            Assert.AreEqual(ItemState.Moving, item.state, "Waiting item should be allowed to move");
        }

        [Test]
        public void TryStartMove_OutOfBoundsDestination_DoesNotStartMovement()
        {
            // Arrange
            cellData.x = 0;
            cellData.y = 0;
            cellData.direction = UICell.Direction.Up; // Would go out of bounds

            var item = new ItemData
            {
                id = "test_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Idle
            };

            // Act
            machine.TryStartMove(item);

            // Assert
            Assert.AreEqual(ItemState.Idle, item.state, "Item should not move when destination is out of bounds");
        }

        #endregion

        #region Helper Methods and Classes

        /// <summary>
        /// Sets up a minimal mock GameManager for testing
        /// </summary>
        private void SetupMockGameManager()
        {
            if (GameManager.Instance == null)
            {
                var gameObject = new GameObject("MockGameManager");
                var gameManager = gameObject.AddComponent<MockGameManager>();
                
                // Set up the grid data
                gameManager.SetGridData(gridData);
                
                // Create a mock grid manager
                var gridManagerObject = new GameObject("MockGridManager");
                var mockGridManager = gridManagerObject.AddComponent<MockUIGridManager>();
                gameManager.activeGridManager = mockGridManager;
            }
        }

        /// <summary>
        /// Concrete test implementation of BaseMachine for testing abstract functionality
        /// </summary>
        private class TestConcreteMachine : BaseMachine
        {
            public TestConcreteMachine(CellData cellData, MachineDef machineDef) : base(cellData, machineDef)
            {
            }

            // Expose protected methods for testing
            public ItemData TestGetNextWaitingItem()
            {
                return GetNextWaitingItem();
            }

            public void TestGetNextCellCoordinates(out int nextX, out int nextY)
            {
                GetNextCellCoordinates(out nextX, out nextY);
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