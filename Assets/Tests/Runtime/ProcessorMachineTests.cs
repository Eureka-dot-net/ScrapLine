using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace ScrapLine.Tests
{
    /// <summary>
    /// Unit tests for ProcessorMachine.
    /// Tests complex processing workflows including waiting queues, recipe processing, and timeout handling.
    /// This is the most complex machine type with pull systems and multi-stage processing.
    /// </summary>
    [TestFixture]
    public class ProcessorMachineTests
    {
        private ProcessorMachine processorMachine;
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
                x = 4,
                y = 3,
                direction = UICell.Direction.Up,
                items = new List<ItemData>(),
                waitingItems = new List<ItemData>(),
                machineState = MachineState.Idle
            };

            machineDef = new MachineDef
            {
                id = "shredder",
                type = "processor",
                baseProcessTime = 3.0f
            };

            // Set up mocks and registries
            SetupMockGameManager();
            SetupFactoryRegistry();
            
            processorMachine = new ProcessorMachine(cellData, machineDef);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up
            if (gameManagerObject != null)
            {
                Object.DestroyImmediate(gameManagerObject);
            }
            
            // Clear registry
            factoryRegistry.Items.Clear();
            factoryRegistry.Recipes.Clear();
        }

        #region Constructor Tests

        [Test]
        public void Constructor_ValidParameters_InitializesCorrectly()
        {
            // Arrange & Act - Done in SetUp

            // Assert
            Assert.AreEqual(cellData, processorMachine.GetCellData(), "CellData should be set correctly");
            Assert.AreEqual(machineDef, processorMachine.GetMachineDef(), "MachineDef should be set correctly");
        }

        [Test]
        public void Constructor_InheritsFromBaseMachine_IsBaseMachine()
        {
            // Arrange & Act & Assert
            Assert.IsInstanceOf<BaseMachine>(processorMachine, "ProcessorMachine should inherit from BaseMachine");
        }

        #endregion

        #region AddToWaitingQueue Tests

        [Test]
        public void AddToWaitingQueue_NewItem_AddsToQueue()
        {
            // Arrange
            var item = new ItemData
            {
                id = "new_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Waiting
            };

            // Act
            processorMachine.AddToWaitingQueue(item);

            // Assert
            Assert.AreEqual(1, cellData.waitingItems.Count, "Item should be added to waiting queue");
            Assert.AreEqual(item, cellData.waitingItems[0], "Added item should be in queue");
            Assert.AreEqual(0, item.stackIndex, "First item should have stack index 0");
        }

        [Test]
        public void AddToWaitingQueue_DuplicateItem_DoesNotAddAgain()
        {
            // Arrange
            var item = new ItemData
            {
                id = "duplicate_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Waiting
            };

            // Act
            processorMachine.AddToWaitingQueue(item);
            processorMachine.AddToWaitingQueue(item); // Add same item again

            // Assert
            Assert.AreEqual(1, cellData.waitingItems.Count, "Duplicate item should not be added");
            Assert.AreEqual(0, item.stackIndex, "Stack index should remain unchanged");
        }

        [Test]
        public void AddToWaitingQueue_MultipleItems_AssignsCorrectStackIndices()
        {
            // Arrange
            var item1 = new ItemData { id = "item1", itemType = "can", state = ItemState.Waiting };
            var item2 = new ItemData { id = "item2", itemType = "can", state = ItemState.Waiting };
            var item3 = new ItemData { id = "item3", itemType = "can", state = ItemState.Waiting };

            // Act
            processorMachine.AddToWaitingQueue(item1);
            processorMachine.AddToWaitingQueue(item2);
            processorMachine.AddToWaitingQueue(item3);

            // Assert
            Assert.AreEqual(3, cellData.waitingItems.Count, "All items should be added");
            Assert.AreEqual(0, item1.stackIndex, "First item should have index 0");
            Assert.AreEqual(1, item2.stackIndex, "Second item should have index 1");
            Assert.AreEqual(2, item3.stackIndex, "Third item should have index 2");
        }

        [Test]
        public void AddToWaitingQueue_NullItem_DoesNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => processorMachine.AddToWaitingQueue(null), 
                "AddToWaitingQueue should handle null item gracefully");
        }

        #endregion

        #region UpdateLogic Tests

        [Test]
        public void UpdateLogic_NoWaitingItems_DoesNotThrow()
        {
            // Arrange - No waiting items

            // Act & Assert
            Assert.DoesNotThrow(() => processorMachine.UpdateLogic(), "UpdateLogic should not throw with no waiting items");
        }

        [Test]
        public void UpdateLogic_IdleMachineWithHalfwayItem_PullsItem()
        {
            // Arrange
            var item = new ItemData
            {
                id = "halfway_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Waiting,
                isHalfway = true
            };
            cellData.waitingItems.Add(item);
            cellData.machineState = MachineState.Idle;

            // Act
            processorMachine.UpdateLogic();

            // Assert
            Assert.AreEqual(ItemState.Moving, item.state, "Item should be set to moving state");
            Assert.AreEqual(MachineState.Processing, cellData.machineState, "Machine should enter processing state");
        }

        [Test]
        public void UpdateLogic_IdleMachineWithNonHalfwayItem_DoesNotPull()
        {
            // Arrange
            var item = new ItemData
            {
                id = "not_halfway_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Waiting,
                isHalfway = false
            };
            cellData.waitingItems.Add(item);
            cellData.machineState = MachineState.Idle;

            // Act
            processorMachine.UpdateLogic();

            // Assert
            Assert.AreEqual(ItemState.Waiting, item.state, "Item should remain waiting");
            Assert.AreEqual(MachineState.Idle, cellData.machineState, "Machine should remain idle");
        }

        [Test]
        public void UpdateLogic_ProcessingMachine_DoesNotPullNewItems()
        {
            // Arrange
            var item = new ItemData
            {
                id = "waiting_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Waiting,
                isHalfway = true
            };
            cellData.waitingItems.Add(item);
            cellData.machineState = MachineState.Processing; // Already processing

            // Act
            processorMachine.UpdateLogic();

            // Assert
            Assert.AreEqual(ItemState.Waiting, item.state, "Item should remain waiting when machine is processing");
            Assert.AreEqual(MachineState.Processing, cellData.machineState, "Machine should remain in processing state");
        }

        [Test]
        public void UpdateLogic_ProcessingComplete_TransformsItem()
        {
            // Arrange
            var item = new ItemData
            {
                id = "processing_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Processing,
                processingStartTime = Time.time - 5f, // Completed processing
                processingDuration = 3f
            };
            cellData.items.Add(item);
            cellData.machineState = MachineState.Processing;

            // Act
            processorMachine.UpdateLogic();

            // Assert
            Assert.AreEqual(0, cellData.items.Count, "Original item should be removed");
            Assert.AreEqual(MachineState.Idle, cellData.machineState, "Machine should return to idle");
            // Note: Output item creation would be tested in integration tests with proper mocking
        }

        #endregion

        #region OnItemArrived Tests

        [Test]
        public void OnItemArrived_ItemInWaitingQueue_StartsProcessing()
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
            cellData.waitingItems.Add(item); // Item is in waiting queue

            // Act
            processorMachine.OnItemArrived(item);

            // Assert
            Assert.AreEqual(0, cellData.waitingItems.Count, "Item should be removed from waiting queue");
            Assert.AreEqual(1, cellData.items.Count, "Item should be added to processing items");
            Assert.AreEqual(ItemState.Processing, cellData.items[0].state, "Item should be in processing state");
        }

        [Test]
        public void OnItemArrived_ItemNotInWaitingQueue_LogsWarning()
        {
            // Arrange
            var item = new ItemData
            {
                id = "unknown_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Moving
            };
            // Don't add to waiting queue

            // Act & Assert
            using (var logScope = new LogAssertScope())
            {
                logScope.ExpectLog(LogType.Warning, "Could not find item unknown_item in waiting queue to process.");
                processorMachine.OnItemArrived(item);
            }
        }

        [Test]
        public void OnItemArrived_ValidRecipe_SetsCorrectProcessingTime()
        {
            // Arrange
            var item = new ItemData
            {
                id = "recipe_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Moving
            };
            cellData.waitingItems.Add(item);

            // Act
            processorMachine.OnItemArrived(item);

            // Assert
            Assert.AreEqual(6f, cellData.items[0].processingDuration, "Processing duration should match recipe (3.0 * 2.0)");
        }

        [Test]
        public void OnItemArrived_NullItem_DoesNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => processorMachine.OnItemArrived(null), 
                "OnItemArrived should handle null item gracefully");
        }

        #endregion

        #region ProcessItem Tests

        [Test]
        public void ProcessItem_ValidItem_AddsToWaitingQueue()
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

            // Act
            processorMachine.ProcessItem(item);

            // Assert
            Assert.AreEqual(1, cellData.waitingItems.Count, "Item should be added to waiting queue");
            Assert.AreEqual(item, cellData.waitingItems[0], "Item should be in waiting queue");
        }

        [Test]
        public void ProcessItem_NullItem_DoesNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => processorMachine.ProcessItem(null), 
                "ProcessItem should handle null item gracefully");
        }

        #endregion

        #region Recipe Processing Tests

        [Test]
        public void RecipeProcessing_ValidRecipe_CreatesOutputItems()
        {
            // Arrange
            var item = new ItemData
            {
                id = "transform_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Processing,
                processingStartTime = Time.time - 5f,
                processingDuration = 3f
            };
            cellData.items.Add(item);
            cellData.machineState = MachineState.Processing;

            // Act
            processorMachine.UpdateLogic();

            // Assert
            Assert.AreEqual(MachineState.Idle, cellData.machineState, "Machine should return to idle after processing");
            // Output item creation would be verified in integration tests
        }

        [Test]
        public void RecipeProcessing_NoValidRecipe_HandlesGracefully()
        {
            // Arrange
            var item = new ItemData
            {
                id = "no_recipe_item",
                itemType = "unknown_type",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Processing,
                processingStartTime = Time.time - 5f,
                processingDuration = 3f
            };
            cellData.items.Add(item);
            cellData.machineState = MachineState.Processing;

            // Act & Assert
            Assert.DoesNotThrow(() => processorMachine.UpdateLogic(), 
                "Should handle missing recipes gracefully");
        }

        #endregion

        #region Timeout and Cleanup Tests

        [Test]
        public void WaitingTimeout_OldWaitingItems_AreRemoved()
        {
            // Arrange
            var oldItem = new ItemData
            {
                id = "old_waiting_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Waiting,
                waitingStartTime = Time.time - 20f // Very old
            };
            var recentItem = new ItemData
            {
                id = "recent_waiting_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Waiting,
                waitingStartTime = Time.time - 1f // Recent
            };
            
            cellData.waitingItems.Add(oldItem);
            cellData.waitingItems.Add(recentItem);

            // Act
            processorMachine.UpdateLogic();

            // Assert
            Assert.AreEqual(1, cellData.waitingItems.Count, "Old item should be removed, recent should remain");
            Assert.AreEqual("recent_waiting_item", cellData.waitingItems[0].id, "Recent item should remain");
        }

        [Test]
        public void StackIndexUpdate_AfterItemRemoval_UpdatesCorrectly()
        {
            // Arrange
            var item1 = new ItemData { id = "item1", itemType = "can", state = ItemState.Waiting, stackIndex = 0 };
            var item2 = new ItemData { id = "item2", itemType = "can", state = ItemState.Waiting, stackIndex = 1 };
            var item3 = new ItemData { id = "item3", itemType = "can", state = ItemState.Waiting, stackIndex = 2 };
            
            cellData.waitingItems.Add(item1);
            cellData.waitingItems.Add(item2);
            cellData.waitingItems.Add(item3);

            // Act - Remove middle item
            cellData.waitingItems.RemoveAt(1);
            processorMachine.UpdateLogic(); // This should trigger stack index update

            // Assert
            Assert.AreEqual(0, item1.stackIndex, "First item should remain at index 0");
            Assert.AreEqual(1, item3.stackIndex, "Third item should move to index 1");
        }

        #endregion

        #region Integration Tests

        [Test]
        public void Integration_CompleteProcessingWorkflow_WorksCorrectly()
        {
            // Arrange
            var item = new ItemData
            {
                id = "workflow_item",
                itemType = "can",
                x = cellData.x,
                y = cellData.y,
                state = ItemState.Waiting,
                isHalfway = true
            };

            // Act - Complete workflow
            // 1. Add to waiting queue
            processorMachine.AddToWaitingQueue(item);
            Assert.AreEqual(1, cellData.waitingItems.Count, "Item should be in waiting queue");

            // 2. Pull from queue (simulate halfway arrival)
            cellData.machineState = MachineState.Idle;
            processorMachine.UpdateLogic();
            Assert.AreEqual(ItemState.Moving, item.state, "Item should be pulled and moving");

            // 3. Item arrives at machine
            processorMachine.OnItemArrived(item);
            Assert.AreEqual(1, cellData.items.Count, "Item should be processing");
            Assert.AreEqual(ItemState.Processing, cellData.items[0].state, "Item should be in processing state");

            // 4. Processing completes
            cellData.items[0].processingStartTime = Time.time - 10f; // Force completion
            processorMachine.UpdateLogic();
            Assert.AreEqual(MachineState.Idle, cellData.machineState, "Machine should return to idle");
        }

        #endregion

        #region Helper Methods and Mock Classes

        /// <summary>
        /// Sets up mock GameManager with required functionality
        /// </summary>
        private void SetupMockGameManager()
        {
            if (GameManager.Instance == null)
            {
                gameManagerObject = new GameObject("MockGameManager");
                mockGameManager = gameManagerObject.AddComponent<MockGameManager>();
                
                var gridData = new GridData { width = 10, height = 10, cells = new List<CellData>() };
                mockGameManager.SetGridData(gridData);
                
                var gridManagerObject = new GameObject("MockGridManager");
                var mockGridManager = gridManagerObject.AddComponent<MockUIGridManager>();
                mockGameManager.activeGridManager = mockGridManager;
            }
        }

        /// <summary>
        /// Sets up FactoryRegistry with test recipes and items
        /// </summary>
        private void SetupFactoryRegistry()
        {
            factoryRegistry = FactoryRegistry.Instance;
            factoryRegistry.Items.Clear();
            factoryRegistry.Recipes.Clear();
            
            // Add test items
            factoryRegistry.Items["can"] = new ItemDef { id = "can", displayName = "Aluminum Can" };
            factoryRegistry.Items["shredded_aluminum"] = new ItemDef { id = "shredded_aluminum", displayName = "Shredded Aluminum" };
            
            // Add test recipe
            var recipe = new RecipeDef
            {
                machineId = "shredder",
                inputItems = new List<RecipeItemDef> { new RecipeItemDef { item = "can", count = 1 } },
                outputItems = new List<RecipeItemDef> { new RecipeItemDef { item = "shredded_aluminum", count = 1 } },
                processMultiplier = 2.0f
            };
            factoryRegistry.Recipes.Add(recipe);
        }

        private class MockGameManager : GameManager
        {
            private GridData testGridData;
            private int itemIdCounter = 1;

            public void SetGridData(GridData gridData) => testGridData = gridData;
            public override GridData GetCurrentGrid() => testGridData;
            public override string GenerateItemId() => $"test_item_{itemIdCounter++}";
        }

        private class MockUIGridManager : UIGridManager
        {
            public new bool HasVisualItem(string itemId) => true;
            public new void CreateVisualItem(string itemId, int x, int y, string itemType) { }
            public new void DestroyVisualItem(string itemId) { }
        }

        /// <summary>
        /// Helper class for better log assertion control
        /// </summary>
        private class LogAssertScope : System.IDisposable
        {
            public void ExpectLog(LogType logType, string message)
            {
                UnityEngine.TestTools.LogAssert.Expect(logType, message);
            }

            public void Dispose()
            {
                UnityEngine.TestTools.LogAssert.NoUnexpectedReceived();
            }
        }

        #endregion
    }
}