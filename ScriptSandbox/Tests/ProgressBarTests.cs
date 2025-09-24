using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace ScrapLine.Tests
{
    [TestFixture]
    public class ProgressBarTests
    {
        private CellData cellData;
        private MachineDef spawnerDef;
        private MachineDef processorDef;

        [SetUp]
        public void Setup()
        {
            // Reset time for each test
            Time.time = 0f;
            
            // Setup basic cell data
            cellData = new CellData
            {
                x = 0,
                y = 0,
                machineDefId = "spawner",
                machineState = MachineState.Idle,
                items = new List<ItemData>(),
                waitingItems = new List<ItemData>(),
                wasteCrate = new WasteCrateInstance
                {
                    wasteCrateDefId = "starter_crate",
                    remainingItems = new List<WasteCrateItemDef>
                    {
                        new WasteCrateItemDef { itemType = "can", count = 10 }
                    }
                }
            };

            // Setup machine definitions
            spawnerDef = new MachineDef
            {
                id = "spawner",
                baseProcessTime = 5.0f,
                buildingIconSprite = "spawner_icon"
            };

            processorDef = new MachineDef
            {
                id = "processor",
                baseProcessTime = 3.0f,
                buildingIconSprite = "processor_icon"
            };
        }

        [Test]
        public void BaseMachine_GetProgress_ReturnsNegativeByDefault()
        {
            // Arrange
            var machine = new TestBaseMachine(cellData, spawnerDef);

            // Act
            float progress = machine.GetProgress();

            // Assert
            Assert.AreEqual(-1f, progress, "BaseMachine should return -1 for no progress by default");
        }

        [Test]
        public void BaseMachine_ShouldShowProgressBar_ReturnsFalseByDefault()
        {
            // Arrange
            var machine = new TestBaseMachine(cellData, spawnerDef);

            // Act
            bool shouldShow = machine.ShouldShowProgressBar();

            // Assert
            Assert.IsFalse(shouldShow, "BaseMachine should not show progress bar by default");
        }

        [Test]
        public void SpawnerMachine_GetProgress_ReturnsValidProgress()
        {
            // Arrange
            var spawner = new SpawnerMachine(cellData, spawnerDef);
            
            // Simulate time passage (half way through spawn interval)
            Time.time = 2.5f; // 2.5 seconds into 5 second interval

            // Act
            float progress = spawner.GetProgress();

            // Assert
            Assert.GreaterOrEqual(progress, 0f, "Spawner progress should be >= 0");
            Assert.LessOrEqual(progress, 1f, "Spawner progress should be <= 1");
            Assert.AreEqual(0.5f, progress, 0.01f, "Progress should be approximately 50% at 2.5s into 5s interval");
        }

        [Test]
        public void SpawnerMachine_GetProgress_ReturnsNegativeWhenCellOccupied()
        {
            // Arrange
            var spawner = new SpawnerMachine(cellData, spawnerDef);
            cellData.items.Add(new ItemData { id = "test_item" }); // Occupy cell

            // Act
            float progress = spawner.GetProgress();

            // Assert
            Assert.AreEqual(-1f, progress, "Spawner should return -1 progress when cell is occupied");
        }

        [Test]
        public void SpawnerMachine_ShouldShowProgressBar_ReturnsTrueWhenSpawning()
        {
            // Arrange
            var spawner = new SpawnerMachine(cellData, spawnerDef);
            Time.time = 1.0f; // Partially through spawn interval

            // Act
            bool shouldShow = spawner.ShouldShowProgressBar();

            // Assert
            Assert.IsTrue(shouldShow, "Spawner should show progress bar when actively spawning");
        }

        [Test]
        public void ProcessorMachine_GetProgress_ReturnsNegativeWhenIdle()
        {
            // Arrange
            var processor = new ProcessorMachine(cellData, processorDef);

            // Act
            float progress = processor.GetProgress();

            // Assert
            Assert.AreEqual(-1f, progress, "Processor should return -1 progress when idle");
        }

        [Test]
        public void ProcessorMachine_GetProgress_ReturnsValidProgressWhenProcessing()
        {
            // Arrange
            var processor = new ProcessorMachine(cellData, processorDef);
            cellData.machineState = MachineState.Processing;
            
            var processingItem = new ItemData
            {
                id = "test_item",
                state = ItemState.Processing,
                processingStartTime = 0f,
                processingDuration = 3f
            };
            cellData.items.Add(processingItem);
            
            Time.time = 1.5f; // 1.5 seconds into 3 second processing

            // Act
            float progress = processor.GetProgress();

            // Assert
            Assert.GreaterOrEqual(progress, 0f, "Processor progress should be >= 0");
            Assert.LessOrEqual(progress, 1f, "Processor progress should be <= 1");
            Assert.AreEqual(0.5f, progress, 0.01f, "Progress should be approximately 50% at 1.5s into 3s processing");
        }

        [Test]
        public void ProcessorMachine_ShouldShowProgressBar_ReturnsTrueWhenProcessing()
        {
            // Arrange
            var processor = new ProcessorMachine(cellData, processorDef);
            cellData.machineState = MachineState.Processing;
            
            var processingItem = new ItemData
            {
                id = "test_item",
                state = ItemState.Processing,
                processingStartTime = 0f,
                processingDuration = 3f
            };
            cellData.items.Add(processingItem);
            
            Time.time = 1.0f; // Partially through processing

            // Act
            bool shouldShow = processor.ShouldShowProgressBar();

            // Assert
            Assert.IsTrue(shouldShow, "Processor should show progress bar when actively processing");
        }

        // Helper class for testing BaseMachine functionality
        private class TestBaseMachine : BaseMachine
        {
            public TestBaseMachine(CellData cellData, MachineDef machineDef) : base(cellData, machineDef)
            {
            }

            public override void UpdateLogic()
            {
                // Test implementation
            }
        }
    }
}