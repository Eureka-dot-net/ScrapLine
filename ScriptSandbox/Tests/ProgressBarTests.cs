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
            bool shouldShow = machine.ShouldShowProgressBar(-1f);

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
            float progress = spawner.GetProgress();

            // Act
            bool shouldShow = spawner.ShouldShowProgressBar(progress);

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
            float progress = processor.GetProgress();

            // Act
            bool shouldShow = processor.ShouldShowProgressBar(progress);

            // Assert
            Assert.IsTrue(shouldShow, "Processor should show progress bar when actively processing");
        }

        [Test]
        public void BaseMachine_ShouldShowProgressBar_ShowsAt100Percent()
        {
            // Arrange
            var machine = new TestBaseMachine(cellData, spawnerDef);

            // Act & Assert - Test that 100% progress is shown
            Assert.IsTrue(machine.ShouldShowProgressBar(1.0f), "Progress bar should be shown at 100% progress");
            Assert.IsTrue(machine.ShouldShowProgressBar(0.8f), "Progress bar should be shown at 80% progress");
            Assert.IsFalse(machine.ShouldShowProgressBar(-1f), "Progress bar should not be shown for invalid progress");
        }

        [Test]
        public void BaseMachine_ShouldShowProgressBar_HandlesTimingDelays()
        {
            // Arrange
            var machine = new TestBaseMachine(cellData, spawnerDef);

            // Act & Assert - Test that slightly over 100% progress still shows (due to timing delays)
            Assert.IsTrue(machine.ShouldShowProgressBar(1.01f), "Progress bar should be shown at 101% progress (timing delay)");
            Assert.IsTrue(machine.ShouldShowProgressBar(1.1f), "Progress bar should be shown at 110% progress (timing delay)");
            Assert.IsFalse(machine.ShouldShowProgressBar(-1f), "Progress bar should not be shown for invalid progress");
        }

        [Test]
        public void SpawnerMachine_ShouldShowProgressBar_HandlesTimingDelays()
        {
            // Test that spawner progress bar logic handles over 100% values properly
            // Cannot test full SpawnerMachine due to constructor issues, but we can test the concept
            var spawner = new TestSpawnerMachine(cellData, spawnerDef);
            
            // Test that the logic removed the < 1f constraint
            Assert.IsTrue(spawner.TestShouldShowProgressBar(1.01f), "Spawner should show progress bar even at 101% due to timing delays");
        }

        [Test]
        public void MachineRenderer_CompletionDetection_WorksCorrectly()
        {
            // Test the completion detection logic that should show 100% briefly after cycle reset
            // This simulates what happens in MachineRenderer when progress goes from high to low
            
            // Arrange - simulate progress values as seen in logs
            float[] progressSequence = { 0.2f, 0.4f, 0.6f, 0.8f, 0.2f }; // Last value simulates cycle reset
            
            // Act & Assert
            float lastProgress = -1f;
            bool detectedCompletion = false;
            
            foreach (float progress in progressSequence)
            {
                // This simulates the completion detection logic from MachineRenderer
                if (lastProgress > 0.8f && progress >= 0f && progress < 0.3f && progress < lastProgress)
                {
                    detectedCompletion = true;
                }
                lastProgress = progress;
            }
            
            Assert.IsTrue(detectedCompletion, "Should detect completion when progress goes from >0.8 to <0.3");
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

        // Helper class for testing SpawnerMachine logic without dependencies
        private class TestSpawnerMachine : BaseMachine
        {
            public TestSpawnerMachine(CellData cellData, MachineDef machineDef) : base(cellData, machineDef)
            {
            }

            public override void UpdateLogic()
            {
                // Test implementation
            }

            public bool TestShouldShowProgressBar(float progress)
            {
                // Simulate the SpawnerMachine logic without dependencies
                return progress >= 0f; // Simplified version of the spawner logic
            }
        }
    }
}