using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace ScrapLine.Tests
{
    /// <summary>
    /// Unit tests for GameData models.
    /// Tests all data classes used for game state management, items, cells, grids, and user progress.
    /// </summary>
    [TestFixture]
    public class GameDataTests
    {
        #region ItemState and MachineState Enum Tests

        [Test]
        public void ItemState_AllValues_AreValid()
        {
            // Arrange & Act & Assert
            Assert.IsTrue(System.Enum.IsDefined(typeof(ItemState), ItemState.Idle), "Idle state should be defined");
            Assert.IsTrue(System.Enum.IsDefined(typeof(ItemState), ItemState.Moving), "Moving state should be defined");
            Assert.IsTrue(System.Enum.IsDefined(typeof(ItemState), ItemState.Waiting), "Waiting state should be defined");
            Assert.IsTrue(System.Enum.IsDefined(typeof(ItemState), ItemState.Processing), "Processing state should be defined");
        }

        [Test]
        public void MachineState_AllValues_AreValid()
        {
            // Arrange & Act & Assert
            Assert.IsTrue(System.Enum.IsDefined(typeof(MachineState), MachineState.Idle), "Idle state should be defined");
            Assert.IsTrue(System.Enum.IsDefined(typeof(MachineState), MachineState.Receiving), "Receiving state should be defined");
            Assert.IsTrue(System.Enum.IsDefined(typeof(MachineState), MachineState.Processing), "Processing state should be defined");
        }

        #endregion

        #region ItemData Tests

        [Test]
        public void ItemData_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var item = new ItemData();

            // Assert
            Assert.IsNull(item.id, "Default id should be null");
            Assert.IsNull(item.itemType, "Default itemType should be null");
            Assert.AreEqual(0, item.x, "Default x should be 0");
            Assert.AreEqual(0, item.y, "Default y should be 0");
            Assert.AreEqual(ItemState.Idle, item.state, "Default state should be Idle");
            Assert.AreEqual(0f, item.moveStartTime, "Default moveStartTime should be 0");
            Assert.AreEqual(0f, item.moveProgress, "Default moveProgress should be 0");
            Assert.AreEqual(0, item.sourceX, "Default sourceX should be 0");
            Assert.AreEqual(0, item.sourceY, "Default sourceY should be 0");
            Assert.AreEqual(0, item.targetX, "Default targetX should be 0");
            Assert.AreEqual(0, item.targetY, "Default targetY should be 0");
            Assert.AreEqual(0f, item.processingStartTime, "Default processingStartTime should be 0");
            Assert.AreEqual(0f, item.processingDuration, "Default processingDuration should be 0");
            Assert.AreEqual(0f, item.waitingStartTime, "Default waitingStartTime should be 0");
            Assert.IsFalse(item.isHalfway, "Default isHalfway should be false");
            Assert.AreEqual(0, item.stackIndex, "Default stackIndex should be 0");
        }

        [Test]
        public void ItemData_SetAllProperties_StoresCorrectly()
        {
            // Arrange
            var item = new ItemData();

            // Act
            item.id = "item_123";
            item.itemType = "aluminum_can";
            item.x = 5;
            item.y = 3;
            item.state = ItemState.Moving;
            item.moveStartTime = 10.5f;
            item.moveProgress = 0.75f;
            item.sourceX = 4;
            item.sourceY = 2;
            item.targetX = 6;
            item.targetY = 4;
            item.processingStartTime = 15.0f;
            item.processingDuration = 3.0f;
            item.waitingStartTime = 12.0f;
            item.isHalfway = true;
            item.stackIndex = 2;

            // Assert
            Assert.AreEqual("item_123", item.id, "ID should be stored correctly");
            Assert.AreEqual("aluminum_can", item.itemType, "ItemType should be stored correctly");
            Assert.AreEqual(5, item.x, "X position should be stored correctly");
            Assert.AreEqual(3, item.y, "Y position should be stored correctly");
            Assert.AreEqual(ItemState.Moving, item.state, "State should be stored correctly");
            Assert.AreEqual(10.5f, item.moveStartTime, "MoveStartTime should be stored correctly");
            Assert.AreEqual(0.75f, item.moveProgress, "MoveProgress should be stored correctly");
            Assert.AreEqual(4, item.sourceX, "SourceX should be stored correctly");
            Assert.AreEqual(2, item.sourceY, "SourceY should be stored correctly");
            Assert.AreEqual(6, item.targetX, "TargetX should be stored correctly");
            Assert.AreEqual(4, item.targetY, "TargetY should be stored correctly");
            Assert.AreEqual(15.0f, item.processingStartTime, "ProcessingStartTime should be stored correctly");
            Assert.AreEqual(3.0f, item.processingDuration, "ProcessingDuration should be stored correctly");
            Assert.AreEqual(12.0f, item.waitingStartTime, "WaitingStartTime should be stored correctly");
            Assert.IsTrue(item.isHalfway, "IsHalfway should be stored correctly");
            Assert.AreEqual(2, item.stackIndex, "StackIndex should be stored correctly");
        }

        [Test]
        public void ItemData_NegativeValues_AreAccepted()
        {
            // Arrange
            var item = new ItemData();

            // Act
            item.x = -5;
            item.y = -3;
            item.sourceX = -1;
            item.sourceY = -2;
            item.targetX = -10;
            item.targetY = -15;
            item.stackIndex = -1;

            // Assert
            Assert.AreEqual(-5, item.x, "Negative X should be accepted");
            Assert.AreEqual(-3, item.y, "Negative Y should be accepted");
            Assert.AreEqual(-1, item.sourceX, "Negative sourceX should be accepted");
            Assert.AreEqual(-2, item.sourceY, "Negative sourceY should be accepted");
            Assert.AreEqual(-10, item.targetX, "Negative targetX should be accepted");
            Assert.AreEqual(-15, item.targetY, "Negative targetY should be accepted");
            Assert.AreEqual(-1, item.stackIndex, "Negative stackIndex should be accepted");
        }

        #endregion

        #region CellData Tests

        [Test]
        public void CellData_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var cell = new CellData();

            // Assert
            Assert.AreEqual(0, cell.x, "Default x should be 0");
            Assert.AreEqual(0, cell.y, "Default y should be 0");
            Assert.AreEqual(UICell.CellType.Blank, cell.cellType, "Default cellType should be Blank");
            Assert.AreEqual(UICell.Direction.Up, cell.direction, "Default direction should be Up");
            Assert.AreEqual(UICell.CellRole.Grid, cell.cellRole, "Default cellRole should be Grid");
            Assert.IsNull(cell.machineDefId, "Default machineDefId should be null");
            Assert.IsNotNull(cell.items, "Items list should not be null");
            Assert.AreEqual(0, cell.items.Count, "Items list should be empty by default");
            Assert.IsNotNull(cell.waitingItems, "WaitingItems list should not be null");
            Assert.AreEqual(0, cell.waitingItems.Count, "WaitingItems list should be empty by default");
            Assert.AreEqual(MachineState.Idle, cell.machineState, "Default machineState should be Idle");
            Assert.IsNull(cell.machine, "Default machine should be null");
            Assert.IsNull(cell.selectedRecipeId, "Default selectedRecipeId should be null");
        }

        [Test]
        public void CellData_SetAllProperties_StoresCorrectly()
        {
            // Arrange
            var cell = new CellData();
            var items = new List<ItemData> { new ItemData { id = "item1" } };
            var waitingItems = new List<ItemData> { new ItemData { id = "item2" } };
            var mockMachine = new TestMockMachine();

            // Act
            cell.x = 7;
            cell.y = 4;
            cell.cellType = UICell.CellType.Machine;
            cell.direction = UICell.Direction.Right;
            cell.cellRole = UICell.CellRole.Bottom;
            cell.machineDefId = "conveyor";
            cell.items = items;
            cell.waitingItems = waitingItems;
            cell.machineState = MachineState.Processing;
            cell.machine = mockMachine;
            cell.selectedRecipeId = "recipe_123";

            // Assert
            Assert.AreEqual(7, cell.x, "X should be stored correctly");
            Assert.AreEqual(4, cell.y, "Y should be stored correctly");
            Assert.AreEqual(UICell.CellType.Machine, cell.cellType, "CellType should be stored correctly");
            Assert.AreEqual(UICell.Direction.Right, cell.direction, "Direction should be stored correctly");
            Assert.AreEqual(UICell.CellRole.Bottom, cell.cellRole, "CellRole should be stored correctly");
            Assert.AreEqual("conveyor", cell.machineDefId, "MachineDefId should be stored correctly");
            Assert.AreEqual(items, cell.items, "Items should be stored correctly");
            Assert.AreEqual(waitingItems, cell.waitingItems, "WaitingItems should be stored correctly");
            Assert.AreEqual(MachineState.Processing, cell.machineState, "MachineState should be stored correctly");
            Assert.AreEqual(mockMachine, cell.machine, "Machine should be stored correctly");
            Assert.AreEqual("recipe_123", cell.selectedRecipeId, "SelectedRecipeId should be stored correctly");
        }

        [Test]
        public void CellData_NegativeCoordinates_AreAccepted()
        {
            // Arrange
            var cell = new CellData();

            // Act
            cell.x = -10;
            cell.y = -5;

            // Assert
            Assert.AreEqual(-10, cell.x, "Negative X should be accepted");
            Assert.AreEqual(-5, cell.y, "Negative Y should be accepted");
        }

        [Test]
        public void CellData_AddItemsToLists_WorksCorrectly()
        {
            // Arrange
            var cell = new CellData();
            var item1 = new ItemData { id = "item1" };
            var item2 = new ItemData { id = "item2" };
            var waitingItem = new ItemData { id = "waiting1" };

            // Act
            cell.items.Add(item1);
            cell.items.Add(item2);
            cell.waitingItems.Add(waitingItem);

            // Assert
            Assert.AreEqual(2, cell.items.Count, "Items list should contain 2 items");
            Assert.AreEqual(item1, cell.items[0], "First item should be stored correctly");
            Assert.AreEqual(item2, cell.items[1], "Second item should be stored correctly");
            Assert.AreEqual(1, cell.waitingItems.Count, "WaitingItems list should contain 1 item");
            Assert.AreEqual(waitingItem, cell.waitingItems[0], "Waiting item should be stored correctly");
        }

        #endregion

        #region GridData Tests

        [Test]
        public void GridData_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var grid = new GridData();

            // Assert
            Assert.AreEqual(0, grid.width, "Default width should be 0");
            Assert.AreEqual(0, grid.height, "Default height should be 0");
            Assert.IsNotNull(grid.cells, "Cells list should not be null");
            Assert.AreEqual(0, grid.cells.Count, "Cells list should be empty by default");
        }

        [Test]
        public void GridData_SetProperties_StoresCorrectly()
        {
            // Arrange
            var grid = new GridData();
            var cells = new List<CellData> 
            { 
                new CellData { x = 0, y = 0 },
                new CellData { x = 1, y = 0 },
                new CellData { x = 0, y = 1 },
                new CellData { x = 1, y = 1 }
            };

            // Act
            grid.width = 10;
            grid.height = 8;
            grid.cells = cells;

            // Assert
            Assert.AreEqual(10, grid.width, "Width should be stored correctly");
            Assert.AreEqual(8, grid.height, "Height should be stored correctly");
            Assert.AreEqual(cells, grid.cells, "Cells should be stored correctly");
            Assert.AreEqual(4, grid.cells.Count, "Cells list should contain 4 cells");
        }

        [Test]
        public void GridData_NegativeDimensions_AreAccepted()
        {
            // Arrange
            var grid = new GridData();

            // Act
            grid.width = -5;
            grid.height = -3;

            // Assert
            Assert.AreEqual(-5, grid.width, "Negative width should be accepted");
            Assert.AreEqual(-3, grid.height, "Negative height should be accepted");
        }

        [Test]
        public void GridData_AddCells_WorksCorrectly()
        {
            // Arrange
            var grid = new GridData();
            var cell1 = new CellData { x = 0, y = 0 };
            var cell2 = new CellData { x = 1, y = 1 };

            // Act
            grid.cells.Add(cell1);
            grid.cells.Add(cell2);

            // Assert
            Assert.AreEqual(2, grid.cells.Count, "Grid should contain 2 cells");
            Assert.AreEqual(cell1, grid.cells[0], "First cell should be stored correctly");
            Assert.AreEqual(cell2, grid.cells[1], "Second cell should be stored correctly");
        }

        #endregion

        #region UserMachineProgress Tests

        [Test]
        public void UserMachineProgress_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var progress = new UserMachineProgress();

            // Assert
            Assert.IsNull(progress.machineId, "Default machineId should be null");
            Assert.IsFalse(progress.unlocked, "Default unlocked should be false");
            Assert.AreEqual(0, progress.upgradeLevel, "Default upgradeLevel should be 0");
        }

        [Test]
        public void UserMachineProgress_SetAllProperties_StoresCorrectly()
        {
            // Arrange
            var progress = new UserMachineProgress();

            // Act
            progress.machineId = "conveyor";
            progress.unlocked = true;
            progress.upgradeLevel = 5;

            // Assert
            Assert.AreEqual("conveyor", progress.machineId, "MachineId should be stored correctly");
            Assert.IsTrue(progress.unlocked, "Unlocked should be stored correctly");
            Assert.AreEqual(5, progress.upgradeLevel, "UpgradeLevel should be stored correctly");
        }

        [Test]
        public void UserMachineProgress_NegativeUpgradeLevel_IsAccepted()
        {
            // Arrange
            var progress = new UserMachineProgress();

            // Act
            progress.upgradeLevel = -1;

            // Assert
            Assert.AreEqual(-1, progress.upgradeLevel, "Negative upgradeLevel should be accepted");
        }

        [Test]
        public void UserMachineProgress_EmptyMachineId_IsAccepted()
        {
            // Arrange
            var progress = new UserMachineProgress();

            // Act
            progress.machineId = "";

            // Assert
            Assert.AreEqual("", progress.machineId, "Empty machineId should be accepted");
        }

        #endregion

        #region GameData Tests

        [Test]
        public void GameData_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var gameData = new GameData();

            // Assert
            Assert.IsNotNull(gameData.grids, "Grids list should not be null");
            Assert.AreEqual(0, gameData.grids.Count, "Grids list should be empty by default");
            Assert.IsNotNull(gameData.userMachineProgress, "UserMachineProgress list should not be null");
            Assert.AreEqual(0, gameData.userMachineProgress.Count, "UserMachineProgress list should be empty by default");
            Assert.AreEqual(0, gameData.credits, "Default credits should be 0");
        }

        [Test]
        public void GameData_SetAllProperties_StoresCorrectly()
        {
            // Arrange
            var gameData = new GameData();
            var grids = new List<GridData> { new GridData { width = 10, height = 10 } };
            var userProgress = new List<UserMachineProgress> 
            { 
                new UserMachineProgress { machineId = "conveyor", unlocked = true, upgradeLevel = 2 } 
            };

            // Act
            gameData.grids = grids;
            gameData.userMachineProgress = userProgress;
            gameData.credits = 1500;

            // Assert
            Assert.AreEqual(grids, gameData.grids, "Grids should be stored correctly");
            Assert.AreEqual(userProgress, gameData.userMachineProgress, "UserMachineProgress should be stored correctly");
            Assert.AreEqual(1500, gameData.credits, "Credits should be stored correctly");
        }

        [Test]
        public void GameData_NegativeCredits_AreAccepted()
        {
            // Arrange
            var gameData = new GameData();

            // Act
            gameData.credits = -100;

            // Assert
            Assert.AreEqual(-100, gameData.credits, "Negative credits should be accepted");
        }

        [Test]
        public void GameData_AddToLists_WorksCorrectly()
        {
            // Arrange
            var gameData = new GameData();
            var grid1 = new GridData { width = 5, height = 5 };
            var grid2 = new GridData { width = 10, height = 10 };
            var progress1 = new UserMachineProgress { machineId = "conveyor", unlocked = true };
            var progress2 = new UserMachineProgress { machineId = "shredder", unlocked = false };

            // Act
            gameData.grids.Add(grid1);
            gameData.grids.Add(grid2);
            gameData.userMachineProgress.Add(progress1);
            gameData.userMachineProgress.Add(progress2);

            // Assert
            Assert.AreEqual(2, gameData.grids.Count, "GameData should contain 2 grids");
            Assert.AreEqual(grid1, gameData.grids[0], "First grid should be stored correctly");
            Assert.AreEqual(grid2, gameData.grids[1], "Second grid should be stored correctly");
            Assert.AreEqual(2, gameData.userMachineProgress.Count, "GameData should contain 2 progress entries");
            Assert.AreEqual(progress1, gameData.userMachineProgress[0], "First progress should be stored correctly");
            Assert.AreEqual(progress2, gameData.userMachineProgress[1], "Second progress should be stored correctly");
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Mock implementation of BaseMachine for testing purposes
        /// </summary>
        private class TestMockMachine : BaseMachine
        {
            public TestMockMachine() : base(null, null)
            {
            }
        }

        #endregion
    }
}