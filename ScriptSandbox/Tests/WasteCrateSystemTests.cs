using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ScrapLine.Tests
{
    /// <summary>
    /// Unit tests for the waste crate queue system functionality
    /// </summary>
    [TestFixture]
    public class WasteCrateSystemTests
    {
        private GameManager gameManager;
        private SpawnerMachine spawnerMachine;
        private CellData cellData;
        
        [SetUp]
        public void Setup()
        {
            // Create a mock GameManager instance
            var gameObject = new GameObject("TestGameManager");
            gameManager = gameObject.AddComponent<GameManager>();
            
            // Initialize the singleton manually since Awake is private in tests
            // gameManager.Awake(); // This is called automatically by Unity
            
            // Initialize game data
            gameManager.gameData = new GameData
            {
                credits = 1000,
                wasteQueueLimit = 1,
                wasteQueue = new List<string>()
            };
            
            // Create test cell data  
            cellData = new CellData
            {
                x = 0,
                y = 0,
                cellType = UICell.CellType.Machine,
                machineDefId = "spawner"
            };
            
            // Create test machine definition
            var machineDef = new MachineDef
            {
                id = "spawner",
                type = "Spawner",
                baseProcessTime = 5.0f
            };
            
            // Create spawner machine
            spawnerMachine = new SpawnerMachine(cellData, machineDef);
            cellData.machine = spawnerMachine;
        }
        
        [TearDown]
        public void TearDown()
        {
            if (gameManager != null)
                UnityEngine.Object.Destroy(gameManager.gameObject);
        }
        
        [Test]
        public void WasteCrateQueueStatus_InitialState_IsCorrect()
        {
            // Act
            var queueStatus = spawnerMachine.GetQueueStatus();
            
            // Assert
            Assert.IsNotNull(queueStatus, "Queue status should not be null");
            Assert.AreEqual("starter_crate", queueStatus.currentCrateId, "Should start with starter crate");
            Assert.AreEqual(0, queueStatus.queuedCrateIds.Count, "Queue should be empty initially");
            Assert.AreEqual(1, queueStatus.maxQueueSize, "Queue limit should be 1");
            Assert.IsTrue(queueStatus.canAddToQueue, "Should be able to add to queue when empty");
        }
        
        [Test]
        public void TryAddToQueue_WithSpace_ReturnsTrue()
        {
            // Act
            bool result = spawnerMachine.TryAddToQueue("medium_crate");
            
            // Assert
            Assert.IsTrue(result, "Should be able to add crate to empty queue");
            Assert.AreEqual(1, gameManager.gameData.wasteQueue.Count, "Queue should have 1 item");
            Assert.AreEqual("medium_crate", gameManager.gameData.wasteQueue[0], "Queue should contain the added crate");
        }
        
        [Test]
        public void TryAddToQueue_WhenFull_ReturnsFalse()
        {
            // Arrange - fill the queue
            spawnerMachine.TryAddToQueue("medium_crate");
            
            // Act
            bool result = spawnerMachine.TryAddToQueue("large_crate");
            
            // Assert
            Assert.IsFalse(result, "Should not be able to add to full queue");
            Assert.AreEqual(1, gameManager.gameData.wasteQueue.Count, "Queue should still have only 1 item");
        }
        
        [Test]
        public void GetQueueStatus_WhenQueueFull_CannotAddMore()
        {
            // Arrange - fill the queue
            spawnerMachine.TryAddToQueue("medium_crate");
            
            // Act
            var queueStatus = spawnerMachine.GetQueueStatus();
            
            // Assert
            Assert.IsFalse(queueStatus.canAddToQueue, "Should not be able to add to full queue");
            Assert.AreEqual(1, queueStatus.queuedCrateIds.Count, "Queue should show 1 item");
            Assert.AreEqual("medium_crate", queueStatus.queuedCrateIds[0], "Queue should contain correct crate");
        }
        
        [Test]
        public void CalculateWasteCrateCost_WithValidCrate_ReturnsHalfValue()
        {
            // Arrange
            var crateDef = new WasteCrateDef
            {
                id = "test_crate",
                displayName = "Test Crate",
                items = new List<WasteCrateItemDef>
                {
                    new WasteCrateItemDef { itemType = "can", count = 10 },
                    new WasteCrateItemDef { itemType = "plasticBottle", count = 10 }
                }
            };
            
            // Mock FactoryRegistry with item definitions
            if (FactoryRegistry.Instance.Items.Count == 0)
            {
                FactoryRegistry.Instance.Items["can"] = new ItemDef { id = "can", sellValue = 5 };
                FactoryRegistry.Instance.Items["plasticBottle"] = new ItemDef { id = "plasticBottle", sellValue = 5 };
            }
            
            // Act
            int cost = SpawnerMachine.CalculateWasteCrateCost(crateDef);
            
            // Assert
            // Total value: (10 * 5) + (10 * 5) = 100
            // Cost should be 50% of total value: 50
            Assert.AreEqual(50, cost, "Cost should be 50% of total item value");
        }
        
        [Test]
        public void GetTotalItemsInWasteCrate_WithItems_ReturnsCorrectCount()
        {
            // Arrange
            if (cellData.wasteCrate != null)
            {
                cellData.wasteCrate.remainingItems = new List<WasteCrateItemDef>
                {
                    new WasteCrateItemDef { itemType = "can", count = 25 },
                    new WasteCrateItemDef { itemType = "plasticBottle", count = 30 }
                };
            }
            
            // Act
            int totalItems = spawnerMachine.GetTotalItemsInWasteCrate();
            
            // Assert
            Assert.AreEqual(55, totalItems, "Should return sum of all item counts");
        }
        
        [Test]
        public void GetTotalItemsInWasteCrate_EmptyItems_ReturnsZero()
        {
            // Arrange
            if (cellData.wasteCrate != null)
            {
                cellData.wasteCrate.remainingItems = new List<WasteCrateItemDef>
                {
                    new WasteCrateItemDef { itemType = "can", count = 0 },
                    new WasteCrateItemDef { itemType = "plasticBottle", count = 0 }
                };
            }
            
            // Act
            int totalItems = spawnerMachine.GetTotalItemsInWasteCrate();
            
            // Assert
            Assert.AreEqual(0, totalItems, "Should return 0 when all items are consumed");
        }
        
        [Test] 
        public void GameData_IncludesQueueFields_ByDefault()
        {
            // Arrange
            var gameData = new GameData();
            
            // Assert
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
                queuedCrateIds = new List<string> { "queued_crate" },
                maxQueueSize = 2,
                canAddToQueue = true
            };
            
            // Assert
            Assert.AreEqual("test_crate", status.currentCrateId);
            Assert.AreEqual(1, status.queuedCrateIds.Count);
            Assert.AreEqual("queued_crate", status.queuedCrateIds[0]);
            Assert.AreEqual(2, status.maxQueueSize);
            Assert.IsTrue(status.canAddToQueue);
        }
    }
}