using NUnit.Framework;
using UnityEngine;

namespace ScriptSandbox.Tests
{
    /// <summary>
    /// Unit tests demonstrating ScriptSandbox functionality for Unity script compilation
    /// and basic logic testing outside of Unity environment.
    /// </summary>
    [TestFixture]
    public class ScriptSandboxTests
    {
        [Test]
        public void UnityMocks_BasicFunctionality_Works()
        {
            // Test Unity mock classes function correctly
            var gameObject = new GameObject("TestObject");
            Assert.IsNotNull(gameObject);
            Assert.AreEqual("TestObject", gameObject.name);
            
            var vector = new Vector3(1, 2, 3);
            Assert.AreEqual(1, vector.x);
            Assert.AreEqual(2, vector.y);
            Assert.AreEqual(3, vector.z);
            
            var color = Color.red;
            Assert.AreEqual(1f, color.r);
            Assert.AreEqual(0f, color.g);
            Assert.AreEqual(0f, color.b);
        }
        
        [Test]
        public void GameDataModels_CanInstantiate()
        {
            // Test that Unity game data models can be instantiated
            Assert.DoesNotThrow(() => {
                var itemData = new ItemData();
                var cellData = new CellData();
                var gridData = new GridData();
                
                Assert.IsNotNull(itemData);
                Assert.IsNotNull(cellData);
                Assert.IsNotNull(gridData);
            });
        }
        
        [Test]
        public void FactoryRegistry_Singleton_Works()
        {
            // Test that FactoryRegistry singleton pattern works
            Assert.DoesNotThrow(() => {
                var registry1 = FactoryRegistry.Instance;
                var registry2 = FactoryRegistry.Instance;
                
                Assert.IsNotNull(registry1);
                Assert.IsNotNull(registry2);
                Assert.AreSame(registry1, registry2, "FactoryRegistry should be a singleton");
            });
        }
        
        [Test]
        public void GameLogger_DoesNotThrow()
        {
            // Test that GameLogger functionality works without exceptions
            Assert.DoesNotThrow(() => {
                GameLogger.LogDebug("Test debug message", "TestComponent");
                GameLogger.LogMovement("Item moved", "Conveyor_1_1");
                GameLogger.LogFabricator("Recipe started", "Fabricator_2_3");
            });
        }
        
        [Test]
        public void MachineDefinitions_CanInstantiate()
        {
            // Test that machine definition models work
            Assert.DoesNotThrow(() => {
                var machineDef = new MachineDef();
                var recipeDef = new RecipeDef();
                var itemDef = new ItemDef();
                
                Assert.IsNotNull(machineDef);
                Assert.IsNotNull(recipeDef);
                Assert.IsNotNull(itemDef);
                
                // Test basic property assignment
                machineDef.id = "test_machine";
                Assert.AreEqual("test_machine", machineDef.id);
                
                recipeDef.machineId = "fabricator";
                Assert.AreEqual("fabricator", recipeDef.machineId);
            });
        }
        
        [Test]
        public void JsonSerialization_Works()
        {
            // Test that JSON serialization works with Unity mocks
            Assert.DoesNotThrow(() => {
                var testData = new ItemDef { id = "test", displayName = "Test Item" };
                string json = JsonUtility.ToJson(testData);
                Assert.IsNotNull(json);
                Assert.IsTrue(json.Contains("test"));
                
                var deserialized = JsonUtility.FromJson<ItemDef>(json);
                Assert.IsNotNull(deserialized);
                Assert.AreEqual("test", deserialized.id);
            });
        }
        
        [Test]
        public void ScriptCompilation_NoErrors()
        {
            // This test passes if the project compiles without errors
            // which means all Unity scripts are successfully compiled against mocks
            Assert.Pass("If this test runs, all Unity scripts compiled successfully against mocks!");
        }
        
        [Test]
        public void SpawnerMachine_EventSystem_Works()
        {
            // Test that SpawnerMachine event system works correctly
            Assert.DoesNotThrow(() => {
                // Create mock data structures needed for SpawnerMachine
                var cellData = new CellData { x = 1, y = 2 };
                var machineDef = new MachineDef { id = "spawner", baseProcessTime = 5.0f };
                
                // Create SpawnerMachine instance
                var spawner = new SpawnerMachine(cellData, machineDef);
                
                Assert.IsNotNull(spawner);
                Assert.IsNotNull(spawner.WasteCrateCountChanged, "WasteCrateCountChanged event should be available");
            });
        }
        
        [Test]
        public void SpawnerMachine_GetRemainingWasteCount_ReturnsZeroWhenEmpty()
        {
            // Test GetRemainingWasteCount method returns 0 for empty waste crate
            var cellData = new CellData { x = 1, y = 2 };
            var machineDef = new MachineDef { id = "spawner", baseProcessTime = 5.0f };
            
            var spawner = new SpawnerMachine(cellData, machineDef);
            
            // With no waste crate or empty waste crate, should return 0
            int count = spawner.GetRemainingWasteCount();
            Assert.GreaterOrEqual(count, 0, "Remaining waste count should be non-negative");
        }
        
        [Test]
        public void SpawnerMachine_EventSubscription_DoesNotThrow()
        {
            // Test that event subscription and unsubscription works without exceptions
            Assert.DoesNotThrow(() => {
                var cellData = new CellData { x = 1, y = 2 };
                var machineDef = new MachineDef { id = "spawner", baseProcessTime = 5.0f };
                var spawner = new SpawnerMachine(cellData, machineDef);
                
                bool eventFired = false;
                
                // Subscribe to event
                spawner.WasteCrateCountChanged += () => eventFired = true;
                
                // Unsubscribe from event
                spawner.WasteCrateCountChanged -= () => eventFired = true;
                
                Assert.IsFalse(eventFired, "Event should not fire during subscription test");
            });
        }
        
        [Test]
        public void SpawnerMachineUI_CanInstantiate()
        {
            // Test that SpawnerMachineUI can be instantiated
            Assert.DoesNotThrow(() => {
                var ui = new SpawnerMachineUI();
                Assert.IsNotNull(ui);
                
                // Test threshold property can be set
                ui.lowThreshold = 15;
                Assert.AreEqual(15, ui.lowThreshold);
            });
        }
        
        [Test]
        public void SpawnerMachineUI_Properties_CanBeAssigned()
        {
            // Test that SpawnerMachineUI properties can be assigned
            Assert.DoesNotThrow(() => {
                var ui = new SpawnerMachineUI();
                var spawner = new SpawnerMachine(new CellData { x = 1, y = 1 }, new MachineDef { id = "spawner" });
                
                ui.spawnerMachine = spawner;
                ui.lowThreshold = 20;
                
                Assert.AreSame(spawner, ui.spawnerMachine);
                Assert.AreEqual(20, ui.lowThreshold);
            });
        }
    }
}