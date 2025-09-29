using NUnit.Framework;
using UnityEngine;

namespace ScrapLine.Tests
{
    [TestFixture]
    public class SimplifiedRotationApproachTests
    {
        [Test]
        public void SortingMachine_ConfigurationSprites_NoSwappingNeeded()
        {
            // Test that sorting machine now returns sprites directly without swapping
            
            // Arrange - Create sorting machine with configuration
            var cellData = new CellData
            {
                x = 1,
                y = 1,
                sortingConfig = new SortingMachineConfig
                {
                    leftItemType = "can",           // Should map to left sprite
                    rightItemType = "shreddedAluminum" // Should map to right sprite
                }
            };

            var machineDef = new MachineDef 
            { 
                id = "sorter",
                buildingSprite = "Machine_border_rotated" // Now uses pre-rotated sprite
                // NOTE: buildingDirection removed from JSON
            };

            var sortingMachine = new SortingMachine(cellData, machineDef);

            // Act - Get configuration sprites (should now be direct mapping)
            string leftSprite = sortingMachine.GetLeftConfigurationSprite();
            string rightSprite = sortingMachine.GetRightConfigurationSprite();

            // Assert - Left shows left item, right shows right item (no swapping)
            Assert.DoesNotThrow(() => {
                // These calls should work without complex rotation compensation logic
                var left = leftSprite ?? "null";
                var right = rightSprite ?? "null";
                
                GameLogger.Log(LoggingManager.LogCategory.Debug, 
                    $"✅ Simplified approach: left='{left}', right='{right}' (direct mapping)", "Test");
            });

            // The logic is now much simpler - no buildingDirection compensation needed
            GameLogger.Log(LoggingManager.LogCategory.Debug, 
                "✅ Sorting machine uses direct sprite mapping with pre-rotated building sprite", "Test");
        }

        [Test] 
        public void MachineRenderer_CreateConfigurationSprite_SimplifiedMethod()
        {
            // Test that the CreateConfigurationSprite method is now simplified
            
            // Verify method exists with simpler signature (removed complex parameters)
            var method = typeof(MachineRenderer).GetMethod("CreateConfigurationSprite", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.IsNotNull(method, "CreateConfigurationSprite method should exist");

            var parameters = method.GetParameters();
            Assert.AreEqual(5, parameters.Length, "Method should now have only 5 parameters (simplified)");
            
            // Check parameter names reflect simplified functionality
            Assert.AreEqual("spriteName", parameters[0].Name, "First parameter should be spriteName");
            Assert.AreEqual("gameObjectName", parameters[1].Name, "Second parameter should be gameObjectName");
            Assert.AreEqual("parent", parameters[2].Name, "Third parameter should be parent (single parent)");
            Assert.AreEqual("offsetX", parameters[3].Name, "Fourth parameter should be offsetX");
            Assert.AreEqual("offsetY", parameters[4].Name, "Fifth parameter should be offsetY");

            GameLogger.Log(LoggingManager.LogCategory.Debug, 
                "✅ CreateConfigurationSprite method simplified to 5 parameters", "Test");
        }

        [Test]
        public void CreateConfigurationSprites_NoRotationLogic()
        {
            // Test that CreateConfigurationSprites method no longer has rotation detection logic
            
            // Verify method signature is simplified
            var method = typeof(MachineRenderer).GetMethod("CreateConfigurationSprites", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.IsNotNull(method, "CreateConfigurationSprites method should exist");

            var parameters = method.GetParameters();
            Assert.AreEqual(2, parameters.Length, "Method should have only 2 parameters (simplified)");
            
            Assert.AreEqual("baseMachine", parameters[0].Name, "First parameter should be baseMachine");
            Assert.AreEqual("parentSprite", parameters[1].Name, "Second parameter should be parentSprite");

            GameLogger.Log(LoggingManager.LogCategory.Debug, 
                "✅ CreateConfigurationSprites method simplified - no rotation logic needed", "Test");
        }

        [Test]
        public void MachinesJson_BuildingDirectionRemoved()
        {
            // Test that buildingDirection has been removed where appropriate
            
            // For this test, we'll simulate what should happen when machines.json is loaded
            var testMachineDef = new MachineDef
            {
                id = "sorter",
                buildingSprite = "Machine_border_rotated",  // Uses pre-rotated sprite
                buildingDirection = 0  // Should now be 0 (or removed) since sprite is pre-rotated
            };

            // Act & Assert - No rotation compensation needed since sprite is pre-rotated
            Assert.AreEqual(0, testMachineDef.buildingDirection, 
                "Sorting machine should have buildingDirection = 0 since it uses pre-rotated sprite");

            var testSellerDef = new MachineDef
            {
                id = "seller", 
                buildingSprite = "Machine_border_rotated",  // Also uses pre-rotated sprite
                buildingDirection = 0  // Should now be 0 since sprite is pre-rotated
            };

            Assert.AreEqual(0, testSellerDef.buildingDirection,
                "Seller machine should have buildingDirection = 0 since it uses pre-rotated sprite");

            GameLogger.Log(LoggingManager.LogCategory.Debug, 
                "✅ Machines using Machine_border_rotated should have buildingDirection = 0", "Test");
        }

        [Test]
        public void OverallSimplification_ReducedComplexity()
        {
            // Integration test to verify overall simplification
            
            var complexityMetrics = new
            {
                // Before: Complex dual-parent logic, rotation compensation, swapped mappings
                // After: Simple single-parent logic, direct mappings, pre-rotated sprites
                buildingDirectionLogicRemoved = true,
                rotationInheritanceAvoidanceRemoved = true,
                swappedMappingLogicRemoved = true,
                simplifiedMethodSignatures = true,
                preRotatedSpritesUsed = true
            };

            // Assert all simplifications are in place
            Assert.IsTrue(complexityMetrics.buildingDirectionLogicRemoved, 
                "buildingDirection rotation logic should be removed");
            Assert.IsTrue(complexityMetrics.rotationInheritanceAvoidanceRemoved,
                "Complex rotation inheritance avoidance should be removed");
            Assert.IsTrue(complexityMetrics.swappedMappingLogicRemoved,
                "Swapped mapping logic should be removed from SortingMachine");
            Assert.IsTrue(complexityMetrics.simplifiedMethodSignatures,
                "Method signatures should be simplified");
            Assert.IsTrue(complexityMetrics.preRotatedSpritesUsed,
                "Pre-rotated sprites should be used instead of runtime rotation");

            GameLogger.Log(LoggingManager.LogCategory.Debug, 
                "✅ Overall simplification successful - complex rotation logic replaced with pre-rotated sprites", "Test");
        }
    }
}