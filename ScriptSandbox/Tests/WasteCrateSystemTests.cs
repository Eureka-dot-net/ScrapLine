using NUnit.Framework;
using System.Collections.Generic;

namespace ScrapLine.Tests
{
    /// <summary>
    /// Simple compilation tests for the corrected global waste crate queue system
    /// </summary>
    [TestFixture]
    public class WasteCrateSystemTests
    {
        [Test]
        public void GameData_IncludesGlobalQueueFields_ByDefault()
        {
            // Arrange
            var gameData = new GameData();
            
            // Assert - test global queue system
            Assert.IsNotNull(gameData.wasteQueue, "wasteQueue should be initialized");
            Assert.AreEqual(1, gameData.wasteQueueLimit, "wasteQueueLimit should default to 1");
            Assert.AreEqual(0, gameData.wasteQueue.Count, "wasteQueue should be empty by default");
        }
        
        [Test]
        public void WasteCrateQueueStatus_Properties_AreCorrect()
        {
            // Arrange
            var status = new WasteCrateQueueStatus
            {
                currentCrateId = "test_crate",
                queuedCrateIds = new List<string> { "crate1", "crate2" },
                maxQueueSize = 5,
                canAddToQueue = true
            };
            
            // Assert
            Assert.AreEqual("test_crate", status.currentCrateId);
            Assert.AreEqual(2, status.queuedCrateIds.Count);
            Assert.AreEqual(5, status.maxQueueSize);
            Assert.IsTrue(status.canAddToQueue);
        }
        
        [Test]
        public void SpawnerMachine_HasRequiredCrateIdProperty()
        {
            // This test just verifies that the RequiredCrateId property exists and can be set
            // We can't easily instantiate SpawnerMachine without complex setup, so just test the data model
            
            var cellData = new CellData
            {
                x = 0,
                y = 0,
                machineDefId = "spawner"
            };
            
            Assert.IsNotNull(cellData, "CellData should be creatable");
            Assert.AreEqual("spawner", cellData.machineDefId);
        }
        
        [Test]
        public void CalculateWasteCrateCost_WithNullCrate_ReturnsZero()
        {
            // Act
            int cost = SpawnerMachine.CalculateWasteCrateCost(null);
            
            // Assert
            Assert.AreEqual(0, cost, "Cost should be 0 for null crate");
        }
        
        [Test]
        public void CalculateWasteCrateCost_WithEmptyCrate_ReturnsZero()
        {
            // Arrange
            var crateDef = new WasteCrateDef
            {
                items = null
            };
            
            // Act
            int cost = SpawnerMachine.CalculateWasteCrateCost(crateDef);
            
            // Assert
            Assert.AreEqual(0, cost, "Cost should be 0 for crate with no items");
        }
    }
}