using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace ScriptSandbox.Tests
{
    /// <summary>
    /// Unit tests for SpawnerMachine event system and SpawnerMachineUI component.
    /// These tests validate the new functionality added for waste crate UI updates.
    /// </summary>
    [TestFixture]
    public class SpawnerMachineEventTests
    {
        [Test]
        public void SpawnerMachineUI_CanInstantiate()
        {
            // Test that SpawnerMachineUI can be instantiated without Unity context
            Assert.DoesNotThrow(() => {
                var ui = new SpawnerMachineUI();
                Assert.IsNotNull(ui);
                
                // Test threshold property can be set
                ui.lowThreshold = 15;
                Assert.AreEqual(15, ui.lowThreshold);
            });
        }
        
        [Test]
        public void SpawnerMachineUI_ThresholdProperty_WorksCorrectly()
        {
            // Test that threshold property works within expected ranges
            var ui = new SpawnerMachineUI();
            
            // Test valid threshold values
            ui.lowThreshold = 5;
            Assert.AreEqual(5, ui.lowThreshold);
            
            ui.lowThreshold = 25;
            Assert.AreEqual(25, ui.lowThreshold);
            
            ui.lowThreshold = 10; // Default
            Assert.AreEqual(10, ui.lowThreshold);
        }
        
        [Test]
        public void SpawnerMachine_EventDeclaration_Exists()
        {
            // Test that WasteCrateCountChanged event is properly declared
            // We can't easily test the full SpawnerMachine constructor due to dependencies,
            // but we can verify the class exists and compiles
            Assert.DoesNotThrow(() => {
                // This test passes if SpawnerMachine class exists and compiles
                var spawnerType = typeof(SpawnerMachine);
                Assert.IsNotNull(spawnerType);
                
                // Check that the WasteCrateCountChanged property exists
                var eventProperty = spawnerType.GetField("WasteCrateCountChanged");
                Assert.IsNotNull(eventProperty, "WasteCrateCountChanged event field should exist");
            });
        }
        
        [Test]
        public void SpawnerMachine_GetRemainingWasteCountMethod_Exists()
        {
            // Test that GetRemainingWasteCount method is publicly accessible
            Assert.DoesNotThrow(() => {
                var spawnerType = typeof(SpawnerMachine);
                var method = spawnerType.GetMethod("GetRemainingWasteCount");
                Assert.IsNotNull(method, "GetRemainingWasteCount method should be public");
                Assert.AreEqual(typeof(int), method.ReturnType, "GetRemainingWasteCount should return int");
            });
        }
        
        [Test]
        public void EventSystem_ActionType_IsCorrect()
        {
            // Test that the event uses System.Action (no parameters)
            var spawnerType = typeof(SpawnerMachine);
            var eventField = spawnerType.GetField("WasteCrateCountChanged");
            
            if (eventField != null)
            {
                Assert.AreEqual(typeof(System.Action), eventField.FieldType, 
                    "WasteCrateCountChanged should be System.Action type");
            }
        }
        
        [Test]
        public void SpawnerMachineUI_ComponentIdProperty_Exists()
        {
            // Test that ComponentId property exists for logging
            var uiType = typeof(SpawnerMachineUI);
            var componentIdProperty = uiType.GetProperty("ComponentId", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.IsNotNull(componentIdProperty, "ComponentId property should exist for logging");
        }
        
        [Test]
        public void UIComponent_HasRequiredFields()
        {
            // Test that SpawnerMachineUI has the required public fields for Unity Inspector
            var uiType = typeof(SpawnerMachineUI);
            
            var spawnerMachineField = uiType.GetField("spawnerMachine");
            Assert.IsNotNull(spawnerMachineField, "spawnerMachine field should be public for Inspector");
            
            var wasteCrateImageField = uiType.GetField("wasteCrateImage");
            Assert.IsNotNull(wasteCrateImageField, "wasteCrateImage field should be public for Inspector");
            
            var emptySpriteField = uiType.GetField("emptySprite");
            Assert.IsNotNull(emptySpriteField, "emptySprite field should be public for Inspector");
            
            var lowSpriteField = uiType.GetField("lowSprite");
            Assert.IsNotNull(lowSpriteField, "lowSprite field should be public for Inspector");
            
            var fullSpriteField = uiType.GetField("fullSprite");
            Assert.IsNotNull(fullSpriteField, "fullSprite field should be public for Inspector");
        }
        
        [Test]
        public void Implementation_FollowsUnityPatterns()
        {
            // Test that SpawnerMachineUI follows Unity MonoBehaviour patterns
            var uiType = typeof(SpawnerMachineUI);
            
            // Should inherit from MonoBehaviour
            Assert.IsTrue(typeof(MonoBehaviour).IsAssignableFrom(uiType), 
                "SpawnerMachineUI should inherit from MonoBehaviour");
            
            // Should have Start method for initialization
            var startMethod = uiType.GetMethod("Start", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(startMethod, "Start method should exist for Unity lifecycle");
            
            // Should have OnDestroy method for cleanup
            var onDestroyMethod = uiType.GetMethod("OnDestroy", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(onDestroyMethod, "OnDestroy method should exist for event cleanup");
        }
    }
}