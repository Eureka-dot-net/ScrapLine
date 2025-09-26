using NUnit.Framework;
using System.Collections.Generic;

namespace ScrapLine.Tests
{
    /// <summary>
    /// Basic tests for waste crate data structures and functionality
    /// </summary>
    [TestFixture]
    public class WasteCrateBasicTests
    {
        [Test]
        public void WasteCrateDef_NewFields_ExistAndWork()
        {
            // Arrange & Act  
            var crateDef = new WasteCrateDef
            {
                id = "test_crate",
                displayName = "Test Crate", 
                sprite = "waste_1",
                cost = 250,
                items = new List<WasteCrateItemDef>
                {
                    new WasteCrateItemDef { itemType = "can", count = 10 }
                }
            };
            
            // Assert
            Assert.AreEqual("test_crate", crateDef.id);
            Assert.AreEqual("Test Crate", crateDef.displayName); 
            Assert.AreEqual("waste_1", crateDef.sprite);
            Assert.AreEqual(250, crateDef.cost);
            Assert.AreEqual(1, crateDef.items.Count);
        }
        
        [Test]
        public void GameData_NewQueueFields_ExistAndWork()
        {
            // Arrange & Act
            // Test new per-machine queue system
            var gameData = new GameData
            {
                credits = 1000,
                machineWasteQueues = new Dictionary<string, List<string>>
                {
                    ["Spawner_0_0"] = new List<string> { "medium_crate", "large_crate" }
                },
                machineQueueLimits = new Dictionary<string, int>
                {
                    ["Spawner_0_0"] = 2
                }
            };
            
            // Assert
            Assert.AreEqual(1000, gameData.credits);
            Assert.AreEqual(1, gameData.machineWasteQueues.Count);
            Assert.AreEqual(2, gameData.machineWasteQueues["Spawner_0_0"].Count);
            Assert.AreEqual("medium_crate", gameData.machineWasteQueues["Spawner_0_0"][0]);
            Assert.AreEqual("large_crate", gameData.machineWasteQueues["Spawner_0_0"][1]);
        }
        
        [Test]
        public void WasteCrateQueueStatus_AllProperties_Work()
        {
            // Arrange & Act
            var status = new WasteCrateQueueStatus
            {
                currentCrateId = "starter_crate",
                queuedCrateIds = new List<string> { "medium_crate" },
                maxQueueSize = 1,
                canAddToQueue = false
            };
            
            // Assert
            Assert.AreEqual("starter_crate", status.currentCrateId);
            Assert.AreEqual(1, status.queuedCrateIds.Count);
            Assert.AreEqual("medium_crate", status.queuedCrateIds[0]);
            Assert.AreEqual(1, status.maxQueueSize);
            Assert.IsFalse(status.canAddToQueue);
        }
        
        [Test]
        public void WasteCrateInstance_WorksWithNewStructure()
        {
            // Arrange & Act
            var instance = new WasteCrateInstance
            {
                wasteCrateDefId = "starter_crate",
                remainingItems = new List<WasteCrateItemDef>
                {
                    new WasteCrateItemDef { itemType = "can", count = 25 },
                    new WasteCrateItemDef { itemType = "plasticBottle", count = 30 }
                }
            };
            
            // Assert
            Assert.AreEqual("starter_crate", instance.wasteCrateDefId);
            Assert.AreEqual(2, instance.remainingItems.Count);
            Assert.AreEqual(25, instance.remainingItems[0].count);
            Assert.AreEqual(30, instance.remainingItems[1].count);
        }
        
        [Test]
        public void CalculateWasteCrateCost_WithMockItems_ReturnsCorrectValue()
        {
            // Arrange
            var crateDef = new WasteCrateDef
            {
                id = "test_crate",
                displayName = "Test Crate",
                items = new List<WasteCrateItemDef>
                {
                    new WasteCrateItemDef { itemType = "can", count = 20 },
                    new WasteCrateItemDef { itemType = "plasticBottle", count = 10 }
                }
            };
            
            // Mock items in FactoryRegistry  
            FactoryRegistry.Instance.Items.Clear();
            FactoryRegistry.Instance.Items["can"] = new ItemDef { id = "can", sellValue = 5 };
            FactoryRegistry.Instance.Items["plasticBottle"] = new ItemDef { id = "plasticBottle", sellValue = 5 };
            
            // Act
            int cost = SpawnerMachine.CalculateWasteCrateCost(crateDef);
            
            // Assert
            // Total value: (20 * 5) + (10 * 5) = 150
            // Cost should be 50% of total value: 75
            Assert.AreEqual(75, cost, "Cost should be 50% of total item value");
        }
        
        [Test]
        public void WasteCrateItemDef_BasicFunctionality_Works()
        {
            // Arrange & Act
            var itemDef = new WasteCrateItemDef
            {
                itemType = "can",
                count = 50
            };
            
            // Assert
            Assert.AreEqual("can", itemDef.itemType);
            Assert.AreEqual(50, itemDef.count);
        }
    }
}