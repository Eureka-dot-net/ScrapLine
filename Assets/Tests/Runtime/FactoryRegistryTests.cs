using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace ScrapLine.Tests
{
    /// <summary>
    /// Unit tests for FactoryRegistry singleton class.
    /// Tests JSON loading, machine/item retrieval, user progress tracking, and save/load functionality.
    /// </summary>
    [TestFixture]
    public class FactoryRegistryTests
    {
        private FactoryRegistry registry;

        [SetUp]
        public void SetUp()
        {
            // Arrange - Get a fresh instance for each test
            registry = FactoryRegistry.Instance;
            
            // Clear all data to ensure clean state
            registry.Machines.Clear();
            registry.Recipes.Clear();
            registry.Items.Clear();
            registry.UserMachines.Clear();
        }

        #region Singleton Pattern Tests

        [Test]
        public void Instance_CalledMultipleTimes_ReturnsSameInstance()
        {
            // Arrange & Act
            var instance1 = FactoryRegistry.Instance;
            var instance2 = FactoryRegistry.Instance;

            // Assert
            Assert.AreSame(instance1, instance2, "Singleton should return same instance");
            Assert.IsNotNull(instance1, "Instance should not be null");
        }

        #endregion

        #region Data Collection Tests

        [Test]
        public void DataCollections_DefaultState_AreInitialized()
        {
            // Arrange & Act - Already done in SetUp

            // Assert
            Assert.IsNotNull(registry.Machines, "Machines collection should be initialized");
            Assert.IsNotNull(registry.Recipes, "Recipes collection should be initialized");
            Assert.IsNotNull(registry.Items, "Items collection should be initialized");
            Assert.IsNotNull(registry.UserMachines, "UserMachines collection should be initialized");
            Assert.AreEqual(0, registry.Machines.Count, "Machines should be empty initially");
            Assert.AreEqual(0, registry.Recipes.Count, "Recipes should be empty initially");
            Assert.AreEqual(0, registry.Items.Count, "Items should be empty initially");
            Assert.AreEqual(0, registry.UserMachines.Count, "UserMachines should be empty initially");
        }

        #endregion

        #region IsLoaded Tests

        [Test]
        public void IsLoaded_WithEmptyCollections_ReturnsFalse()
        {
            // Arrange - Collections are already empty from SetUp

            // Act
            bool isLoaded = registry.IsLoaded();

            // Assert
            Assert.IsFalse(isLoaded, "IsLoaded should return false when collections are empty");
        }

        [Test]
        public void IsLoaded_WithOnlyMachines_ReturnsFalse()
        {
            // Arrange
            registry.Machines.Add("conveyor", new MachineDef { id = "conveyor" });

            // Act
            bool isLoaded = registry.IsLoaded();

            // Assert
            Assert.IsFalse(isLoaded, "IsLoaded should return false when only machines are loaded");
        }

        [Test]
        public void IsLoaded_WithAllCollections_ReturnsTrue()
        {
            // Arrange
            registry.Machines.Add("conveyor", new MachineDef { id = "conveyor" });
            registry.Recipes.Add(new RecipeDef { machineId = "shredder" });
            registry.Items.Add("can", new ItemDef { id = "can" });

            // Act
            bool isLoaded = registry.IsLoaded();

            // Assert
            Assert.IsTrue(isLoaded, "IsLoaded should return true when all collections have data");
        }

        #endregion

        #region LoadFromJson Tests

        [Test]
        public void LoadFromJson_ValidMachineJson_LoadsMachinesCorrectly()
        {
            // Arrange
            string machinesJson = @"{""machines"":[{""id"":""conveyor"",""type"":""transport"",""sprite"":""conveyor_icon""}]}";
            string recipesJson = "[]";
            string itemsJson = @"{""items"":[{""id"":""can"",""displayName"":""Can""}]}";

            // Act
            registry.LoadFromJson(machinesJson, recipesJson, itemsJson);

            // Assert
            Assert.AreEqual(1, registry.Machines.Count, "Should load one machine");
            Assert.IsTrue(registry.Machines.ContainsKey("conveyor"), "Should contain conveyor machine");
            Assert.AreEqual("conveyor", registry.Machines["conveyor"].id, "Machine ID should be correct");
            Assert.AreEqual("transport", registry.Machines["conveyor"].type, "Machine type should be correct");
        }

        [Test]
        public void LoadFromJson_ValidItemJson_LoadsItemsCorrectly()
        {
            // Arrange
            string machinesJson = @"{""machines"":[{""id"":""conveyor""}]}";
            string recipesJson = "[]";
            string itemsJson = @"{""items"":[{""id"":""can"",""displayName"":""Aluminum Can"",""sellValue"":25}]}";

            // Act
            registry.LoadFromJson(machinesJson, recipesJson, itemsJson);

            // Assert
            Assert.AreEqual(1, registry.Items.Count, "Should load one item");
            Assert.IsTrue(registry.Items.ContainsKey("can"), "Should contain can item");
            Assert.AreEqual("can", registry.Items["can"].id, "Item ID should be correct");
            Assert.AreEqual("Aluminum Can", registry.Items["can"].displayName, "Item display name should be correct");
            Assert.AreEqual(25, registry.Items["can"].sellValue, "Item sell value should be correct");
        }

        [Test]
        public void LoadFromJson_ValidRecipeJson_LoadsRecipesCorrectly()
        {
            // Arrange
            string machinesJson = @"{""machines"":[{""id"":""shredder""}]}";
            string recipesJson = @"[{""machineId"":""shredder"",""inputItems"":[{""item"":""can"",""count"":1}],""outputItems"":[{""item"":""shredded"",""count"":1}]}]";
            string itemsJson = @"{""items"":[{""id"":""can""}]}";

            // Act
            registry.LoadFromJson(machinesJson, recipesJson, itemsJson);

            // Assert
            Assert.AreEqual(1, registry.Recipes.Count, "Should load one recipe");
            Assert.AreEqual("shredder", registry.Recipes[0].machineId, "Recipe machine ID should be correct");
            Assert.AreEqual(1, registry.Recipes[0].inputItems.Count, "Should have one input item");
            Assert.AreEqual("can", registry.Recipes[0].inputItems[0].item, "Input item should be can");
        }

        [Test]
        public void LoadFromJson_InvalidRecipeJson_UsesEmptyList()
        {
            // Arrange
            string machinesJson = @"{""machines"":[{""id"":""conveyor""}]}";
            string recipesJson = "invalid json";
            string itemsJson = @"{""items"":[{""id"":""can""}]}";

            // Act
            registry.LoadFromJson(machinesJson, recipesJson, itemsJson);

            // Assert
            Assert.AreEqual(0, registry.Recipes.Count, "Should use empty list for invalid recipe JSON");
            Assert.AreEqual(1, registry.Machines.Count, "Should still load machines");
            Assert.AreEqual(1, registry.Items.Count, "Should still load items");
        }

        [Test]
        public void LoadFromJson_ClearsExistingData_BeforeLoading()
        {
            // Arrange
            registry.Machines.Add("old", new MachineDef { id = "old" });
            registry.Items.Add("old", new ItemDef { id = "old" });
            
            string machinesJson = @"{""machines"":[{""id"":""new""}]}";
            string recipesJson = "[]";
            string itemsJson = @"{""items"":[{""id"":""new""}]}";

            // Act
            registry.LoadFromJson(machinesJson, recipesJson, itemsJson);

            // Assert
            Assert.AreEqual(1, registry.Machines.Count, "Should only have new machine");
            Assert.IsFalse(registry.Machines.ContainsKey("old"), "Should not contain old machine");
            Assert.IsTrue(registry.Machines.ContainsKey("new"), "Should contain new machine");
            Assert.AreEqual(1, registry.Items.Count, "Should only have new item");
            Assert.IsFalse(registry.Items.ContainsKey("old"), "Should not contain old item");
            Assert.IsTrue(registry.Items.ContainsKey("new"), "Should contain new item");
        }

        #endregion

        #region Machine and Item Retrieval Tests

        [Test]
        public void GetMachine_ExistingMachine_ReturnsCorrectMachine()
        {
            // Arrange
            var machine = new MachineDef { id = "conveyor", type = "transport" };
            registry.Machines.Add("conveyor", machine);

            // Act
            var result = registry.GetMachine("conveyor");

            // Assert
            Assert.IsNotNull(result, "Should return machine");
            Assert.AreEqual(machine, result, "Should return correct machine");
            Assert.AreEqual("conveyor", result.id, "Machine ID should be correct");
        }

        [Test]
        public void GetMachine_NonExistentMachine_ReturnsNull()
        {
            // Arrange - No machines added

            // Act
            var result = registry.GetMachine("nonexistent");

            // Assert
            Assert.IsNull(result, "Should return null for non-existent machine");
        }

        [Test]
        public void GetMachine_NullMachineId_ReturnsNull()
        {
            // Arrange
            registry.Machines.Add("conveyor", new MachineDef { id = "conveyor" });

            // Act
            var result = registry.GetMachine(null);

            // Assert
            Assert.IsNull(result, "Should return null for null machine ID");
        }

        [Test]
        public void GetItem_ExistingItem_ReturnsCorrectItem()
        {
            // Arrange
            var item = new ItemDef { id = "can", displayName = "Aluminum Can" };
            registry.Items.Add("can", item);

            // Act
            var result = registry.GetItem("can");

            // Assert
            Assert.IsNotNull(result, "Should return item");
            Assert.AreEqual(item, result, "Should return correct item");
            Assert.AreEqual("can", result.id, "Item ID should be correct");
        }

        [Test]
        public void GetItem_NonExistentItem_ReturnsNull()
        {
            // Arrange - No items added

            // Act
            var result = registry.GetItem("nonexistent");

            // Assert
            Assert.IsNull(result, "Should return null for non-existent item");
        }

        [Test]
        public void GetItem_NullItemId_ReturnsNull()
        {
            // Arrange
            registry.Items.Add("can", new ItemDef { id = "can" });

            // Act
            var result = registry.GetItem(null);

            // Assert
            Assert.IsNull(result, "Should return null for null item ID");
        }

        #endregion

        #region Recipe Retrieval Tests

        [Test]
        public void GetRecipe_ExistingMachineAndItem_ReturnsCorrectRecipe()
        {
            // Arrange
            var recipe = new RecipeDef 
            { 
                machineId = "shredder",
                inputItems = new List<RecipeItemDef> { new RecipeItemDef { item = "can", count = 1 } }
            };
            registry.Recipes.Add(recipe);

            // Act
            var result = registry.GetRecipe("shredder", "can");

            // Assert
            Assert.IsNotNull(result, "Should return recipe");
            Assert.AreEqual(recipe, result, "Should return correct recipe");
            Assert.AreEqual("shredder", result.machineId, "Recipe machine ID should be correct");
        }

        [Test]
        public void GetRecipe_NonExistentMachine_ReturnsNull()
        {
            // Arrange
            var recipe = new RecipeDef 
            { 
                machineId = "shredder",
                inputItems = new List<RecipeItemDef> { new RecipeItemDef { item = "can", count = 1 } }
            };
            registry.Recipes.Add(recipe);

            // Act
            var result = registry.GetRecipe("nonexistent", "can");

            // Assert
            Assert.IsNull(result, "Should return null for non-existent machine");
        }

        [Test]
        public void GetRecipe_NonExistentInputItem_ReturnsNull()
        {
            // Arrange
            var recipe = new RecipeDef 
            { 
                machineId = "shredder",
                inputItems = new List<RecipeItemDef> { new RecipeItemDef { item = "can", count = 1 } }
            };
            registry.Recipes.Add(recipe);

            // Act
            var result = registry.GetRecipe("shredder", "nonexistent");

            // Assert
            Assert.IsNull(result, "Should return null for non-existent input item");
        }

        [Test]
        public void GetRecipe_MultipleInputItems_MatchesAnyInputItem()
        {
            // Arrange
            var recipe = new RecipeDef 
            { 
                machineId = "processor",
                inputItems = new List<RecipeItemDef> 
                { 
                    new RecipeItemDef { item = "can", count = 1 },
                    new RecipeItemDef { item = "metal", count = 2 }
                }
            };
            registry.Recipes.Add(recipe);

            // Act
            var result1 = registry.GetRecipe("processor", "can");
            var result2 = registry.GetRecipe("processor", "metal");

            // Assert
            Assert.IsNotNull(result1, "Should return recipe for first input item");
            Assert.IsNotNull(result2, "Should return recipe for second input item");
            Assert.AreEqual(recipe, result1, "Should return same recipe for both input items");
            Assert.AreEqual(recipe, result2, "Should return same recipe for both input items");
        }

        #endregion

        #region User Machine Progress Tests

        [Test]
        public void FindMachineProgress_ExistingMachine_ReturnsProgress()
        {
            // Arrange
            var progress = new UserMachineProgress { machineId = "conveyor", unlocked = true, upgradeLevel = 2 };
            registry.UserMachines.Add(progress);

            // Act
            var result = registry.FindMachineProgress("conveyor");

            // Assert
            Assert.IsNotNull(result, "Should return progress");
            Assert.AreEqual(progress, result, "Should return correct progress");
            Assert.AreEqual("conveyor", result.machineId, "Machine ID should be correct");
        }

        [Test]
        public void FindMachineProgress_NonExistentMachine_ReturnsNull()
        {
            // Arrange - No progress added

            // Act
            var result = registry.FindMachineProgress("nonexistent");

            // Assert
            Assert.IsNull(result, "Should return null for non-existent machine");
        }

        [Test]
        public void UnlockMachine_NewMachine_CreatesProgressAndUnlocks()
        {
            // Arrange - No existing progress

            // Act
            registry.UnlockMachine("conveyor");

            // Assert
            Assert.AreEqual(1, registry.UserMachines.Count, "Should create one progress entry");
            var progress = registry.UserMachines[0];
            Assert.AreEqual("conveyor", progress.machineId, "Machine ID should be correct");
            Assert.IsTrue(progress.unlocked, "Machine should be unlocked");
            Assert.AreEqual(0, progress.upgradeLevel, "Upgrade level should be 0");
        }

        [Test]
        public void UnlockMachine_ExistingMachine_UnlocksExistingProgress()
        {
            // Arrange
            var progress = new UserMachineProgress { machineId = "conveyor", unlocked = false, upgradeLevel = 3 };
            registry.UserMachines.Add(progress);

            // Act
            registry.UnlockMachine("conveyor");

            // Assert
            Assert.AreEqual(1, registry.UserMachines.Count, "Should still have only one progress entry");
            Assert.IsTrue(progress.unlocked, "Machine should be unlocked");
            Assert.AreEqual(3, progress.upgradeLevel, "Upgrade level should be preserved");
        }

        [Test]
        public void UpgradeMachine_UnlockedMachine_IncreasesUpgradeLevel()
        {
            // Arrange
            var progress = new UserMachineProgress { machineId = "conveyor", unlocked = true, upgradeLevel = 2 };
            registry.UserMachines.Add(progress);

            // Act
            registry.UpgradeMachine("conveyor");

            // Assert
            Assert.AreEqual(3, progress.upgradeLevel, "Upgrade level should be increased");
        }

        [Test]
        public void UpgradeMachine_LockedMachine_DoesNotUpgrade()
        {
            // Arrange
            var progress = new UserMachineProgress { machineId = "conveyor", unlocked = false, upgradeLevel = 2 };
            registry.UserMachines.Add(progress);

            // Act
            registry.UpgradeMachine("conveyor");

            // Assert
            Assert.AreEqual(2, progress.upgradeLevel, "Upgrade level should not change for locked machine");
        }

        [Test]
        public void UpgradeMachine_NonExistentMachine_DoesNothing()
        {
            // Arrange - No progress entries

            // Act
            registry.UpgradeMachine("nonexistent");

            // Assert
            Assert.AreEqual(0, registry.UserMachines.Count, "Should not create progress entry");
        }

        [Test]
        public void IsMachineUnlocked_UnlockedMachine_ReturnsTrue()
        {
            // Arrange
            var progress = new UserMachineProgress { machineId = "conveyor", unlocked = true };
            registry.UserMachines.Add(progress);

            // Act
            bool isUnlocked = registry.IsMachineUnlocked("conveyor");

            // Assert
            Assert.IsTrue(isUnlocked, "Should return true for unlocked machine");
        }

        [Test]
        public void IsMachineUnlocked_LockedMachine_ReturnsFalse()
        {
            // Arrange
            var progress = new UserMachineProgress { machineId = "conveyor", unlocked = false };
            registry.UserMachines.Add(progress);

            // Act
            bool isUnlocked = registry.IsMachineUnlocked("conveyor");

            // Assert
            Assert.IsFalse(isUnlocked, "Should return false for locked machine");
        }

        [Test]
        public void IsMachineUnlocked_NonExistentMachine_ReturnsFalse()
        {
            // Arrange - No progress entries

            // Act
            bool isUnlocked = registry.IsMachineUnlocked("nonexistent");

            // Assert
            Assert.IsFalse(isUnlocked, "Should return false for non-existent machine");
        }

        [Test]
        public void GetMachineUpgradeLevel_ExistingMachine_ReturnsCorrectLevel()
        {
            // Arrange
            var progress = new UserMachineProgress { machineId = "conveyor", upgradeLevel = 5 };
            registry.UserMachines.Add(progress);

            // Act
            int upgradeLevel = registry.GetMachineUpgradeLevel("conveyor");

            // Assert
            Assert.AreEqual(5, upgradeLevel, "Should return correct upgrade level");
        }

        [Test]
        public void GetMachineUpgradeLevel_NonExistentMachine_ReturnsZero()
        {
            // Arrange - No progress entries

            // Act
            int upgradeLevel = registry.GetMachineUpgradeLevel("nonexistent");

            // Assert
            Assert.AreEqual(0, upgradeLevel, "Should return 0 for non-existent machine");
        }

        #endregion

        #region Save/Load Tests

        [Test]
        public void LoadFromGameData_ValidGameData_LoadsUserMachineProgress()
        {
            // Arrange
            var gameData = new GameData();
            gameData.userMachineProgress = new List<UserMachineProgress>
            {
                new UserMachineProgress { machineId = "conveyor", unlocked = true, upgradeLevel = 3 },
                new UserMachineProgress { machineId = "shredder", unlocked = false, upgradeLevel = 0 }
            };

            // Act
            registry.LoadFromGameData(gameData);

            // Assert
            Assert.AreEqual(2, registry.UserMachines.Count, "Should load 2 progress entries");
            Assert.AreEqual("conveyor", registry.UserMachines[0].machineId, "First machine ID should be correct");
            Assert.IsTrue(registry.UserMachines[0].unlocked, "First machine should be unlocked");
            Assert.AreEqual(3, registry.UserMachines[0].upgradeLevel, "First machine upgrade level should be correct");
        }

        [Test]
        public void LoadFromGameData_NullUserMachineProgress_CreatesEmptyList()
        {
            // Arrange
            var gameData = new GameData();
            gameData.userMachineProgress = null;

            // Act
            registry.LoadFromGameData(gameData);

            // Assert
            Assert.IsNotNull(registry.UserMachines, "UserMachines should not be null");
            Assert.AreEqual(0, registry.UserMachines.Count, "UserMachines should be empty");
        }

        [Test]
        public void SaveToGameData_ExistingUserMachines_SavesCorrectly()
        {
            // Arrange
            registry.UserMachines.Add(new UserMachineProgress { machineId = "conveyor", unlocked = true, upgradeLevel = 2 });
            registry.UserMachines.Add(new UserMachineProgress { machineId = "shredder", unlocked = false, upgradeLevel = 0 });
            var gameData = new GameData();

            // Act
            registry.SaveToGameData(gameData);

            // Assert
            Assert.IsNotNull(gameData.userMachineProgress, "UserMachineProgress should not be null");
            Assert.AreEqual(2, gameData.userMachineProgress.Count, "Should save 2 progress entries");
            Assert.AreEqual("conveyor", gameData.userMachineProgress[0].machineId, "First machine ID should be correct");
            Assert.IsTrue(gameData.userMachineProgress[0].unlocked, "First machine should be unlocked");
            Assert.AreEqual(2, gameData.userMachineProgress[0].upgradeLevel, "First machine upgrade level should be correct");
        }

        [Test]
        public void SaveToGameData_EmptyUserMachines_SavesEmptyList()
        {
            // Arrange
            var gameData = new GameData();

            // Act
            registry.SaveToGameData(gameData);

            // Assert
            Assert.IsNotNull(gameData.userMachineProgress, "UserMachineProgress should not be null");
            Assert.AreEqual(0, gameData.userMachineProgress.Count, "Should save empty list");
        }

        #endregion
    }
}