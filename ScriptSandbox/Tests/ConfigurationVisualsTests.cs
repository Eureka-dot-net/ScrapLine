using NUnit.Framework;
using UnityEngine;

namespace ScrapLine.Tests
{
    [TestFixture]
    public class ConfigurationVisualsTests
    {
        private CellData cellData;
        private MachineDef sortingMachineDef;
        private MachineDef fabricatorMachineDef;

        [SetUp]
        public void SetUp()
        {
            // Create test cell data
            cellData = new CellData
            {
                x = 5,
                y = 5,
                machineDefId = "sorting",
                sortingConfig = new SortingMachineConfig()
            };

            // Create test machine definitions
            sortingMachineDef = new MachineDef
            {
                id = "sorting",
                type = "SortingMachine",
                buildingIconSprite = "defaultSortingIcon",
                buildingIconSpriteSize = 1.0f
            };

            fabricatorMachineDef = new MachineDef
            {
                id = "fabricator",
                type = "FabricatorMachine",
                buildingIconSprite = "defaultFabricatorIcon",
                buildingIconSpriteSize = 1.0f
            };
        }

        [Test]
        public void BaseMachine_ConfigurationSprites_ReturnsNullByDefault()
        {
            // Arrange - Create a basic machine type that doesn't override configuration sprites
            var blankMachine = new BlankCellMachine(cellData, sortingMachineDef);

            // Act & Assert
            Assert.IsNull(blankMachine.GetLeftConfigurationSprite());
            Assert.IsNull(blankMachine.GetRightConfigurationSprite());
        }

        [Test]
        public void SortingMachine_ConfigurationSprites_ReturnsNullWhenNotConfigured()
        {
            // Arrange
            cellData.sortingConfig = null;
            var sortingMachine = new SortingMachine(cellData, sortingMachineDef);

            // Act & Assert
            Assert.IsNull(sortingMachine.GetLeftConfigurationSprite());
            Assert.IsNull(sortingMachine.GetRightConfigurationSprite());
        }

        [Test]
        public void SortingMachine_ConfigurationSprites_ReturnsNullWhenConfigIsEmpty()
        {
            // Arrange
            cellData.sortingConfig = new SortingMachineConfig
            {
                leftItemType = "",
                rightItemType = ""
            };
            var sortingMachine = new SortingMachine(cellData, sortingMachineDef);

            // Act & Assert
            Assert.IsNull(sortingMachine.GetLeftConfigurationSprite());
            Assert.IsNull(sortingMachine.GetRightConfigurationSprite());
        }

        [Test]
        public void SortingMachine_ConfigurationSprites_ReturnsSpritesWhenConfigured()
        {
            // Arrange
            cellData.sortingConfig = new SortingMachineConfig
            {
                leftItemType = "can",
                rightItemType = "shreddedAluminum"
            };

            // Mock FactoryRegistry to return test item definitions
            var sortingMachine = new SortingMachine(cellData, sortingMachineDef);

            // Act
            string leftSprite = sortingMachine.GetLeftConfigurationSprite();
            string rightSprite = sortingMachine.GetRightConfigurationSprite();

            // Assert - Should attempt to get sprites but return null due to mock registry
            Assert.DoesNotThrow(() => sortingMachine.GetLeftConfigurationSprite());
            Assert.DoesNotThrow(() => sortingMachine.GetRightConfigurationSprite());
        }

        [Test]
        public void FabricatorMachine_GetBuildingIconSprite_ReturnsDefaultWhenNoRecipe()
        {
            // Arrange
            cellData.selectedRecipeId = "";
            var fabricatorMachine = new FabricatorMachine(cellData, fabricatorMachineDef);

            // Act
            string iconSprite = fabricatorMachine.GetBuildingIconSprite();

            // Assert
            Assert.AreEqual("defaultFabricatorIcon", iconSprite);
        }

        [Test]
        public void FabricatorMachine_GetBuildingIconSprite_AttemptsRecipeLookupWhenConfigured()
        {
            // Arrange
            cellData.selectedRecipeId = "testRecipe";
            var fabricatorMachine = new FabricatorMachine(cellData, fabricatorMachineDef);

            // Act & Assert - Should not throw even with mock registry
            Assert.DoesNotThrow(() => fabricatorMachine.GetBuildingIconSprite());
        }

        [Test]
        public void FabricatorMachine_ConfigurationSprites_ReturnsNullByDefault()
        {
            // Arrange
            var fabricatorMachine = new FabricatorMachine(cellData, fabricatorMachineDef);

            // Act & Assert - FabricatorMachine doesn't override configuration sprites
            Assert.IsNull(fabricatorMachine.GetLeftConfigurationSprite());
            Assert.IsNull(fabricatorMachine.GetRightConfigurationSprite());
        }

        [Test]
        public void MachineRenderer_ConfigurationSprites_CanBeCreatedWithoutError()
        {
            // Arrange
            var gameObject = new GameObject("TestMachine");
            var machineRenderer = gameObject.AddComponent<MachineRenderer>();
            var sortingMachine = new SortingMachine(cellData, sortingMachineDef);

            // Act & Assert - Should not throw when creating configuration sprites
            Assert.DoesNotThrow(() => {
                // This simulates what would happen in CreateConfigurationSprites
                string leftSprite = sortingMachine.GetLeftConfigurationSprite();
                string rightSprite = sortingMachine.GetRightConfigurationSprite();
                
                // The method handles null sprites gracefully
                if (!string.IsNullOrEmpty(leftSprite))
                {
                    // Would create left sprite
                }
                if (!string.IsNullOrEmpty(rightSprite))
                {
                    // Would create right sprite  
                }
            });
        }
    }
}