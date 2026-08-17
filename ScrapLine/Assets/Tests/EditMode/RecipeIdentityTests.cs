using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ScrapLine.Tests.EditMode
{
    public sealed class RecipeIdentityTests
    {
        private object registry;

        [SetUp]
        public void SetUp()
        {
            registry = ProductionType("FactoryRegistry").GetProperty("Instance").GetValue(null);
            LoadProjectContent();
        }

        [Test]
        public void ShippedRecipesHaveStableExplicitIds()
        {
            Assert.That(RecipeIds(), Is.EqualTo(new[]
            {
                "fabricate_reinforced_aluminum_plate",
                "granulate_plastic_bottle",
                "press_aluminum_plate",
                "shred_can"
            }));
        }

        [Test]
        public void RegistryResolvesAuthoredIdAndRejectsBlankOrUnknownIds()
        {
            object recipe = GetRecipe("fabricate_reinforced_aluminum_plate");

            Assert.That(recipe, Is.Not.Null);
            Assert.That((string)Field(recipe, "machineId"), Is.EqualTo("fabricator"));
            Assert.That(GetRecipe(null), Is.Null);
            Assert.That(GetRecipe("   "), Is.Null);
            Assert.That(GetRecipe("fabricator_aluminumPlate:1_reinforcedAluminumPlate:1"), Is.Null);
        }

        [Test]
        public void RecipeSelectionUiStoresAuthoredId()
        {
            object cell = ParseCell("", 0, "[]", "[]");
            GameObject owner = new GameObject("RecipeIdentityUiTest");
            try
            {
                Component panel = owner.AddComponent(ProductionType("FabricatorMachineConfigPanel"));
                FieldInHierarchy(panel.GetType(), "currentData").SetValue(panel, cell);
                object recipe = GetRecipe("fabricate_reinforced_aluminum_plate");

                InvokePrivate(panel, "OnRecipeSelected", recipe);
                string selected = (string)InvokeInHierarchy(panel, "GetCurrentSelection");
                InvokeInHierarchy(panel, "UpdateDataWithSelection", selected);

                Assert.That(selected, Is.EqualTo("fabricate_reinforced_aluminum_plate"));
                Assert.That((string)Field(cell, "selectedRecipeId"), Is.EqualTo(selected));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void FabricatorResolvesAuthoredSelectionAndItSurvivesSerialization()
        {
            const string recipeId = "fabricate_reinforced_aluminum_plate";
            object cell = ParseCell(recipeId, 0, "[]", "[]");
            object machine = CreateMachine(cell);
            object resolved = InvokePrivate(machine, "GetSelectedRecipe");

            Assert.That((string)Field(resolved, "id"), Is.EqualTo(recipeId));

            string json = JsonUtility.ToJson(cell);
            object restoredCell = JsonUtility.FromJson(json, ProductionType("CellData"));
            object restoredMachine = CreateMachine(restoredCell);
            object restoredRecipe = InvokePrivate(restoredMachine, "GetSelectedRecipe");

            Assert.That((string)Field(restoredCell, "selectedRecipeId"), Is.EqualTo(recipeId));
            Assert.That((string)Field(restoredRecipe, "id"), Is.EqualTo(recipeId));
        }

        [Test]
        public void RebalancingQuantitiesDoesNotChangeRecipeIdentity()
        {
            const string recipeId = "fabricate_reinforced_aluminum_plate";
            object recipe = GetRecipe(recipeId);
            IList inputs = (IList)Field(recipe, "inputItems");
            object firstInput = inputs[0];
            int originalCount = (int)Field(firstInput, "count");
            try
            {
                Field(firstInput, "count", originalCount + 7);
                Assert.That(GetRecipe(recipeId), Is.SameAs(recipe));

                object cell = ParseCell(recipeId, 0, "[]", "[]");
                object machine = CreateMachine(cell);
                Assert.That(InvokePrivate(machine, "GetSelectedRecipe"), Is.SameAs(recipe));
            }
            finally
            {
                Field(firstInput, "count", originalCount);
            }
        }

        [Test]
        public void UnknownSavedSelectionIsClearedWithoutConsumingOrReplacingItems()
        {
            const string items =
                "[{\"id\":\"processing\",\"itemType\":\"reinforcedAluminumPlate\",\"state\":3," +
                "\"processingStartTime\":10,\"processingDuration\":20}]";
            const string waiting =
                "[{\"id\":\"waiting\",\"itemType\":\"aluminumPlate\",\"state\":2," +
                "\"isHalfway\":true}]";
            object cell = ParseCell(
                "fabricator_aluminumPlate:1,granulatedPlastic:5_reinforcedAluminumPlate:1",
                2, items, waiting);

            object machine = CreateMachine(cell);
            machine.GetType().GetMethod("UpdateLogic").Invoke(machine, null);

            Assert.That((string)Field(cell, "selectedRecipeId"), Is.Null);
            Assert.That(Convert.ToInt32(Field(cell, "machineState")), Is.EqualTo(0));
            IList preservedItems = (IList)Field(cell, "items");
            IList preservedWaiting = (IList)Field(cell, "waitingItems");
            Assert.That(preservedItems.Count, Is.EqualTo(1));
            Assert.That(preservedWaiting.Count, Is.EqualTo(1));
            Assert.That(Convert.ToInt32(Field(preservedItems[0], "state")), Is.EqualTo(0));
            Assert.That((float)Field(preservedItems[0], "processingDuration"), Is.Zero);
        }

        [Test]
        public void BlankNewFabricatorDoesNotSilentlySelectFirstRecipe()
        {
            object cell = ParseCell("", 0, "[]", "[]");

            object machine = CreateMachine(cell);

            Assert.That(machine, Is.Not.Null);
            Assert.That((string)Field(cell, "selectedRecipeId"), Is.Null.Or.Empty);
            Assert.That(InvokePrivate(machine, "GetSelectedRecipe"), Is.Null);
        }

        private void LoadProjectContent()
        {
            registry.GetType().GetMethod("LoadFromJson").Invoke(registry, new object[]
            {
                Resources.Load<TextAsset>("machines").text,
                Resources.Load<TextAsset>("recipes").text,
                Resources.Load<TextAsset>("items").text,
                Resources.Load<TextAsset>("wastecrates").text,
                null
            });
        }

        private string[] RecipeIds()
        {
            IEnumerable recipes = (IEnumerable)registry.GetType().GetProperty("Recipes").GetValue(registry);
            return recipes.Cast<object>()
                .Select(recipe => (string)Field(recipe, "id"))
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();
        }

        private object GetRecipe(string id) =>
            registry.GetType().GetMethod("GetRecipeById").Invoke(registry, new object[] { id });

        private static object CreateMachine(object cell) =>
            ProductionType("MachineFactory").GetMethod("CreateMachine").Invoke(null, new[] { cell });

        private static object ParseCell(string recipeId, int machineState, string items, string waitingItems)
        {
            string escapedRecipeId = string.IsNullOrEmpty(recipeId)
                ? "null"
                : "\"" + recipeId.Replace("\"", "\\\"") + "\"";
            string json = "{\"x\":0,\"y\":0,\"cellType\":1,\"machineDefId\":\"fabricator\"," +
                          $"\"selectedRecipeId\":{escapedRecipeId},\"machineState\":{machineState}," +
                          $"\"items\":{items},\"waitingItems\":{waitingItems}}}";
            return JsonUtility.FromJson(json, ProductionType("CellData"));
        }

        private static object InvokePrivate(object target, string method, params object[] arguments)
        {
            return target.GetType().GetMethod(method,
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, arguments);
        }

        private static object InvokeInHierarchy(object target, string method, params object[] arguments)
        {
            Type type = target.GetType();
            while (type != null)
            {
                MethodInfo info = type.GetMethod(method,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);
                if (info != null)
                    return info.Invoke(target, arguments);
                type = type.BaseType;
            }
            throw new MissingMethodException(target.GetType().FullName, method);
        }

        private static FieldInfo FieldInHierarchy(Type type, string name)
        {
            while (type != null)
            {
                FieldInfo field = type.GetField(name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);
                if (field != null)
                    return field;
                type = type.BaseType;
            }
            throw new MissingFieldException(name);
        }

        private static object Field(object target, string name) =>
            target.GetType().GetField(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(target);

        private static void Field(object target, string name, object value) =>
            target.GetType().GetField(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(target, value);

        private static Type ProductionType(string name)
        {
            Type type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(name, false))
                .FirstOrDefault(candidate => candidate != null);
            Assert.That(type, Is.Not.Null, $"Production type '{name}' was not found.");
            return type;
        }
    }
}
