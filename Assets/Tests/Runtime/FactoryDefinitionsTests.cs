using NUnit.Framework;
using System.Collections.Generic;

namespace ScrapLine.Tests
{
    /// <summary>
    /// Unit tests for FactoryDefinitions data models.
    /// Tests all serializable data classes used for machine, recipe, and item definitions.
    /// </summary>
    [TestFixture]
    public class FactoryDefinitionsTests
    {
        #region UpgradeMultiplier Tests

        [Test]
        public void UpgradeMultiplier_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var upgrade = new UpgradeMultiplier();

            // Assert
            Assert.AreEqual(0f, upgrade.multiplier, "Default multiplier should be 0");
            Assert.AreEqual(0, upgrade.cost, "Default cost should be 0");
        }

        [Test]
        public void UpgradeMultiplier_SetValues_StoresCorrectly()
        {
            // Arrange
            var upgrade = new UpgradeMultiplier();
            float expectedMultiplier = 1.5f;
            int expectedCost = 100;

            // Act
            upgrade.multiplier = expectedMultiplier;
            upgrade.cost = expectedCost;

            // Assert
            Assert.AreEqual(expectedMultiplier, upgrade.multiplier, "Multiplier should be stored correctly");
            Assert.AreEqual(expectedCost, upgrade.cost, "Cost should be stored correctly");
        }

        [Test]
        public void UpgradeMultiplier_NegativeValues_AreAccepted()
        {
            // Arrange
            var upgrade = new UpgradeMultiplier();

            // Act
            upgrade.multiplier = -1.0f;
            upgrade.cost = -50;

            // Assert
            Assert.AreEqual(-1.0f, upgrade.multiplier, "Negative multiplier should be accepted");
            Assert.AreEqual(-50, upgrade.cost, "Negative cost should be accepted");
        }

        #endregion

        #region MachineDef Tests

        [Test]
        public void MachineDef_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var machine = new MachineDef();

            // Assert
            Assert.IsNull(machine.id, "Default id should be null");
            Assert.IsNull(machine.type, "Default type should be null");
            Assert.IsNull(machine.sprite, "Default sprite should be null");
            Assert.AreEqual(0f, machine.baseProcessTime, "Default baseProcessTime should be 0");
            Assert.IsFalse(machine.isMoving, "Default isMoving should be false");
            Assert.IsNull(machine.borderSprite, "Default borderSprite should be null");
            Assert.IsNull(machine.buildingSprite, "Default buildingSprite should be null");
            Assert.IsNull(machine.borderColor, "Default borderColor should be null");
            Assert.AreEqual(0, machine.buildingDirection, "Default buildingDirection should be 0");
            Assert.IsNull(machine.upgradeMultipliers, "Default upgradeMultipliers should be null");
            Assert.IsNull(machine.gridPlacement, "Default gridPlacement should be null");
            Assert.AreEqual(0, machine.maxNumber, "Default maxNumber should be 0");
            Assert.IsTrue(machine.displayInPanel, "Default displayInPanel should be true");
            Assert.AreEqual(0, machine.cost, "Default cost should be 0");
            Assert.IsNull(machine.spawnableItems, "Default spawnableItems should be null");
            Assert.IsTrue(machine.canRotate, "Default canRotate should be true");
            Assert.IsNull(machine.className, "Default className should be null");
        }

        [Test]
        public void MachineDef_SetAllProperties_StoresCorrectly()
        {
            // Arrange
            var machine = new MachineDef();
            var upgradeMultipliers = new List<UpgradeMultiplier> { new UpgradeMultiplier { multiplier = 1.5f, cost = 100 } };
            var gridPlacement = new List<string> { "bottom", "top" };
            var spawnableItems = new List<string> { "can", "metal" };

            // Act
            machine.id = "conveyor";
            machine.type = "transport";
            machine.sprite = "conveyor_icon";
            machine.baseProcessTime = 2.0f;
            machine.isMoving = true;
            machine.borderSprite = "border_sprite";
            machine.buildingSprite = "building_sprite";
            machine.borderColor = "#FF0000";
            machine.buildingDirection = 90;
            machine.upgradeMultipliers = upgradeMultipliers;
            machine.gridPlacement = gridPlacement;
            machine.maxNumber = 10;
            machine.displayInPanel = false;
            machine.cost = 150;
            machine.spawnableItems = spawnableItems;
            machine.canRotate = false;
            machine.className = "ConveyorMachine";

            // Assert
            Assert.AreEqual("conveyor", machine.id, "ID should be stored correctly");
            Assert.AreEqual("transport", machine.type, "Type should be stored correctly");
            Assert.AreEqual("conveyor_icon", machine.sprite, "Sprite should be stored correctly");
            Assert.AreEqual(2.0f, machine.baseProcessTime, "BaseProcessTime should be stored correctly");
            Assert.IsTrue(machine.isMoving, "IsMoving should be stored correctly");
            Assert.AreEqual("border_sprite", machine.borderSprite, "BorderSprite should be stored correctly");
            Assert.AreEqual("building_sprite", machine.buildingSprite, "BuildingSprite should be stored correctly");
            Assert.AreEqual("#FF0000", machine.borderColor, "BorderColor should be stored correctly");
            Assert.AreEqual(90, machine.buildingDirection, "BuildingDirection should be stored correctly");
            Assert.AreEqual(upgradeMultipliers, machine.upgradeMultipliers, "UpgradeMultipliers should be stored correctly");
            Assert.AreEqual(gridPlacement, machine.gridPlacement, "GridPlacement should be stored correctly");
            Assert.AreEqual(10, machine.maxNumber, "MaxNumber should be stored correctly");
            Assert.IsFalse(machine.displayInPanel, "DisplayInPanel should be stored correctly");
            Assert.AreEqual(150, machine.cost, "Cost should be stored correctly");
            Assert.AreEqual(spawnableItems, machine.spawnableItems, "SpawnableItems should be stored correctly");
            Assert.IsFalse(machine.canRotate, "CanRotate should be stored correctly");
            Assert.AreEqual("ConveyorMachine", machine.className, "ClassName should be stored correctly");
        }

        [Test]
        public void MachineDef_EmptyLists_AreHandledCorrectly()
        {
            // Arrange
            var machine = new MachineDef();

            // Act
            machine.upgradeMultipliers = new List<UpgradeMultiplier>();
            machine.gridPlacement = new List<string>();
            machine.spawnableItems = new List<string>();

            // Assert
            Assert.IsNotNull(machine.upgradeMultipliers, "Empty upgradeMultipliers list should not be null");
            Assert.AreEqual(0, machine.upgradeMultipliers.Count, "Empty upgradeMultipliers list should have zero items");
            Assert.IsNotNull(machine.gridPlacement, "Empty gridPlacement list should not be null");
            Assert.AreEqual(0, machine.gridPlacement.Count, "Empty gridPlacement list should have zero items");
            Assert.IsNotNull(machine.spawnableItems, "Empty spawnableItems list should not be null");
            Assert.AreEqual(0, machine.spawnableItems.Count, "Empty spawnableItems list should have zero items");
        }

        #endregion

        #region RecipeItemDef Tests

        [Test]
        public void RecipeItemDef_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var recipeItem = new RecipeItemDef();

            // Assert
            Assert.IsNull(recipeItem.item, "Default item should be null");
            Assert.AreEqual(0, recipeItem.count, "Default count should be 0");
        }

        [Test]
        public void RecipeItemDef_SetValues_StoresCorrectly()
        {
            // Arrange
            var recipeItem = new RecipeItemDef();

            // Act
            recipeItem.item = "aluminum_can";
            recipeItem.count = 5;

            // Assert
            Assert.AreEqual("aluminum_can", recipeItem.item, "Item should be stored correctly");
            Assert.AreEqual(5, recipeItem.count, "Count should be stored correctly");
        }

        [Test]
        public void RecipeItemDef_NegativeCount_IsAccepted()
        {
            // Arrange
            var recipeItem = new RecipeItemDef();

            // Act
            recipeItem.count = -1;

            // Assert
            Assert.AreEqual(-1, recipeItem.count, "Negative count should be accepted");
        }

        #endregion

        #region RecipeDef Tests

        [Test]
        public void RecipeDef_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var recipe = new RecipeDef();

            // Assert
            Assert.IsNull(recipe.machineId, "Default machineId should be null");
            Assert.IsNull(recipe.inputItems, "Default inputItems should be null");
            Assert.IsNull(recipe.outputItems, "Default outputItems should be null");
            Assert.AreEqual(0f, recipe.processMultiplier, "Default processMultiplier should be 0");
        }

        [Test]
        public void RecipeDef_SetAllProperties_StoresCorrectly()
        {
            // Arrange
            var recipe = new RecipeDef();
            var inputItems = new List<RecipeItemDef> { new RecipeItemDef { item = "can", count = 1 } };
            var outputItems = new List<RecipeItemDef> { new RecipeItemDef { item = "shredded_aluminum", count = 1 } };

            // Act
            recipe.machineId = "shredder";
            recipe.inputItems = inputItems;
            recipe.outputItems = outputItems;
            recipe.processMultiplier = 1.5f;

            // Assert
            Assert.AreEqual("shredder", recipe.machineId, "MachineId should be stored correctly");
            Assert.AreEqual(inputItems, recipe.inputItems, "InputItems should be stored correctly");
            Assert.AreEqual(outputItems, recipe.outputItems, "OutputItems should be stored correctly");
            Assert.AreEqual(1.5f, recipe.processMultiplier, "ProcessMultiplier should be stored correctly");
        }

        [Test]
        public void RecipeDef_ProcessTime_CalculatesCorrectly()
        {
            // Arrange
            // Create a mock FactoryRegistry instance with a test machine
            var registry = FactoryRegistry.Instance;
            var testMachine = new MachineDef { id = "test_machine", baseProcessTime = 10f };
            registry.Machines.Clear();
            registry.Machines["test_machine"] = testMachine;

            var recipe = new RecipeDef();
            recipe.machineId = "test_machine";
            recipe.processMultiplier = 2.0f;

            // Act
            float processTime = recipe.processTime;

            // Assert
            Assert.AreEqual(20f, processTime, "ProcessTime should be baseProcessTime * processMultiplier");
        }

        [Test]
        public void RecipeDef_ProcessTime_WithNullMachine_ReturnsZero()
        {
            // Arrange
            var registry = FactoryRegistry.Instance;
            registry.Machines.Clear(); // Ensure no machines exist

            var recipe = new RecipeDef();
            recipe.machineId = "nonexistent_machine";
            recipe.processMultiplier = 2.0f;

            // Act
            float processTime = recipe.processTime;

            // Assert
            Assert.AreEqual(0f, processTime, "ProcessTime should be 0 when machine doesn't exist");
        }

        [Test]
        public void RecipeDef_EmptyItemLists_AreHandledCorrectly()
        {
            // Arrange
            var recipe = new RecipeDef();

            // Act
            recipe.inputItems = new List<RecipeItemDef>();
            recipe.outputItems = new List<RecipeItemDef>();

            // Assert
            Assert.IsNotNull(recipe.inputItems, "Empty inputItems list should not be null");
            Assert.AreEqual(0, recipe.inputItems.Count, "Empty inputItems list should have zero items");
            Assert.IsNotNull(recipe.outputItems, "Empty outputItems list should not be null");
            Assert.AreEqual(0, recipe.outputItems.Count, "Empty outputItems list should have zero items");
        }

        #endregion

        #region ItemDef Tests

        [Test]
        public void ItemDef_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var item = new ItemDef();

            // Assert
            Assert.IsNull(item.id, "Default id should be null");
            Assert.IsNull(item.displayName, "Default displayName should be null");
            Assert.IsNull(item.sprite, "Default sprite should be null");
            Assert.AreEqual(0, item.sellValue, "Default sellValue should be 0");
        }

        [Test]
        public void ItemDef_SetAllProperties_StoresCorrectly()
        {
            // Arrange
            var item = new ItemDef();

            // Act
            item.id = "aluminum_can";
            item.displayName = "Aluminum Can";
            item.sprite = "can_sprite";
            item.sellValue = 25;

            // Assert
            Assert.AreEqual("aluminum_can", item.id, "ID should be stored correctly");
            Assert.AreEqual("Aluminum Can", item.displayName, "DisplayName should be stored correctly");
            Assert.AreEqual("can_sprite", item.sprite, "Sprite should be stored correctly");
            Assert.AreEqual(25, item.sellValue, "SellValue should be stored correctly");
        }

        [Test]
        public void ItemDef_NegativeSellValue_IsAccepted()
        {
            // Arrange
            var item = new ItemDef();

            // Act
            item.sellValue = -10;

            // Assert
            Assert.AreEqual(-10, item.sellValue, "Negative sellValue should be accepted");
        }

        [Test]
        public void ItemDef_EmptyStrings_AreAccepted()
        {
            // Arrange
            var item = new ItemDef();

            // Act
            item.id = "";
            item.displayName = "";
            item.sprite = "";

            // Assert
            Assert.AreEqual("", item.id, "Empty ID string should be accepted");
            Assert.AreEqual("", item.displayName, "Empty displayName string should be accepted");
            Assert.AreEqual("", item.sprite, "Empty sprite string should be accepted");
        }

        #endregion
    }
}