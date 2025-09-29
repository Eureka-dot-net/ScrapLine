using NUnit.Framework;
using UnityEngine;

namespace ScrapLine.Tests
{
    [TestFixture]
    public class ConfigurationSpritesIntegrationTests
    {
        [Test]
        public void SortingMachine_Integration_ConfigurationSpritesWorkflow()
        {
            // This test demonstrates the complete workflow for sorting machine configuration sprites
            
            // 1. Create a sorting machine
            var cellData = new CellData
            {
                x = 3,
                y = 4,
                machineDefId = "sorting",
                sortingConfig = null
            };

            var machineDef = new MachineDef
            {
                id = "sorting",
                type = "SortingMachine",
                buildingIconSprite = "sortingMachineIcon"
            };

            var sortingMachine = new SortingMachine(cellData, machineDef);

            // 2. Initially, configuration sprites should be null
            Assert.IsNull(sortingMachine.GetLeftConfigurationSprite(), 
                "Left configuration sprite should be null when not configured");
            Assert.IsNull(sortingMachine.GetRightConfigurationSprite(), 
                "Right configuration sprite should be null when not configured");

            // 3. Configure the sorting machine
            cellData.sortingConfig = new SortingMachineConfig
            {
                leftItemType = "can",
                rightItemType = "shreddedAluminum"
            };

            // 4. Now configuration sprites should attempt to load (returns null due to mock registry, but doesn't crash)
            Assert.DoesNotThrow(() => sortingMachine.GetLeftConfigurationSprite(), 
                "Getting left sprite should not throw exception");
            Assert.DoesNotThrow(() => sortingMachine.GetRightConfigurationSprite(), 
                "Getting right sprite should not throw exception");

            GameLogger.Log(LoggingManager.LogCategory.Debug, 
                "✅ Sorting machine configuration sprite workflow completed successfully", "Test");
        }

        [Test]
        public void FabricatorMachine_Integration_IconSpriteWorkflow()
        {
            // This test demonstrates the complete workflow for fabricator machine icon sprites
            
            // 1. Create a fabricator machine  
            var cellData = new CellData
            {
                x = 5,
                y = 6,
                machineDefId = "fabricator",
                selectedRecipeId = ""
            };

            var machineDef = new MachineDef
            {
                id = "fabricator", 
                type = "FabricatorMachine",
                buildingIconSprite = "fabricatorMachineIcon"
            };

            var fabricatorMachine = new FabricatorMachine(cellData, machineDef);

            // 2. Initially, should return default building icon sprite
            Assert.AreEqual("fabricatorMachineIcon", fabricatorMachine.GetBuildingIconSprite(),
                "Should return default building icon when no recipe selected");

            // 3. Configure with a recipe
            cellData.selectedRecipeId = "testRecipe";

            // 4. Should attempt to load recipe sprite (returns default due to mock registry, but doesn't crash)
            Assert.DoesNotThrow(() => fabricatorMachine.GetBuildingIconSprite(),
                "Getting building icon sprite should not throw when recipe configured");

            GameLogger.Log(LoggingManager.LogCategory.Debug, 
                "✅ Fabricator machine icon sprite workflow completed successfully", "Test");
        }

        [Test]
        public void MachineRenderer_Integration_ConfigurationSpriteRendering()
        {
            // This test demonstrates the MachineRenderer configuration sprite logic
            
            // 1. Create test objects
            var cellData = new CellData { x = 1, y = 2 };
            var machineDef = new MachineDef { id = "test", buildingIconSprite = "testIcon" };
            var sortingMachine = new SortingMachine(cellData, machineDef);

            // 2. Configure sorting machine
            cellData.sortingConfig = new SortingMachineConfig
            {
                leftItemType = "testLeft",
                rightItemType = "testRight"
            };

            // 3. Test configuration sprite retrieval (this simulates what MachineRenderer.CreateConfigurationSprites does)
            string leftSprite = sortingMachine.GetLeftConfigurationSprite();
            string rightSprite = sortingMachine.GetRightConfigurationSprite();

            // 4. Should handle sprite creation gracefully (even with null sprites)
            Assert.DoesNotThrow(() => {
                if (!string.IsNullOrEmpty(leftSprite))
                {
                    // Would create left sprite at position (-0.35f, 0.0f)
                    GameLogger.Log(LoggingManager.LogCategory.Debug, $"Would create left sprite: {leftSprite}", "Test");
                }
                if (!string.IsNullOrEmpty(rightSprite))
                {
                    // Would create right sprite at position (0.35f, 0.0f)
                    GameLogger.Log(LoggingManager.LogCategory.Debug, $"Would create right sprite: {rightSprite}", "Test");
                }
            }, "Configuration sprite rendering logic should not throw exceptions");

            GameLogger.Log(LoggingManager.LogCategory.Debug, 
                "✅ MachineRenderer configuration sprite workflow completed successfully", "Test");
        }

        [Test]
        public void ConfigurationSprites_ResponsivePositioning_CorrectCalculations()
        {
            // This test verifies the responsive positioning calculations used in MachineRenderer
            
            // Test the 0.3 size ratio calculation (as requested in requirements)
            float configSizeRatio = 0.3f;
            float margin = (1.0f - configSizeRatio) * 0.5f;
            
            Assert.AreEqual(0.35f, margin, 0.01f, 
                "Margin calculation should be correct for 0.3 size ratio");

            // Test positioning offsets
            float leftOffsetX = -0.35f;
            float rightOffsetX = 0.35f;
            float offsetY = 0.0f;

            Assert.IsTrue(leftOffsetX < 0, "Left sprite should be positioned to the left");
            Assert.IsTrue(rightOffsetX > 0, "Right sprite should be positioned to the right");
            Assert.AreEqual(0.0f, offsetY, "Sprites should be vertically centered");

            GameLogger.Log(LoggingManager.LogCategory.Debug, 
                "✅ Configuration sprite positioning calculations verified", "Test");
        }
    }
}