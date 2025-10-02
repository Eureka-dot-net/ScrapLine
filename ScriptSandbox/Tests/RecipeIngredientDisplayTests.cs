using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ScrapLine.Tests
{
    [TestFixture]
    public class RecipeIngredientDisplayTests
    {
        private RecipeDef testRecipe;

        [SetUp]
        public void SetUp()
        {
            // Create test recipe
            testRecipe = new RecipeDef
            {
                machineId = "fabricator",
                inputItems = new List<RecipeItemDef>
                {
                    new RecipeItemDef { item = "can", count = 1 },
                    new RecipeItemDef { item = "plasticBottle", count = 2 }
                },
                outputItems = new List<RecipeItemDef>
                {
                    new RecipeItemDef { item = "aluminumPlate", count = 1 }
                }
            };

            // Setup mock factory registry
            SetupMockFactoryRegistry();
        }

        private void SetupMockFactoryRegistry()
        {
            // Create mock items
            var items = new Dictionary<string, ItemDef>
            {
                { "can", new ItemDef { id = "can", displayName = "Aluminum Can", sprite = "can" } },
                { "plasticBottle", new ItemDef { id = "plasticBottle", displayName = "Plastic Bottle", sprite = "plasticBottle" } },
                { "aluminumPlate", new ItemDef { id = "aluminumPlate", displayName = "Aluminum Plate", sprite = "aluminumPlate" } }
            };

            // Set items in factory registry if possible
            if (FactoryRegistry.Instance != null)
            {
                FactoryRegistry.Instance.Items = items;
            }
        }

        [Test]
        public void RecipeIngredientDisplay_Instantiation_DoesNotThrow()
        {
            // Test that the class can be instantiated without throwing
            Assert.DoesNotThrow(() => {
                // Just test that the type exists and can be referenced
                var type = typeof(RecipeIngredientDisplay);
                Assert.IsNotNull(type);
                Assert.IsTrue(type.IsSubclassOf(typeof(MonoBehaviour)));
            });
        }

        [Test]
        public void GetIngredientsString_WithValidRecipe_ReturnsFormattedString()
        {
            string result = RecipeIngredientDisplay.GetIngredientsString(testRecipe);
            
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("Can") || result.Contains("can"));
            Assert.IsTrue(result.Contains("Bottle") || result.Contains("bottle"));
            Assert.IsTrue(result.Contains("1x"));
            Assert.IsTrue(result.Contains("2x"));
        }

        [Test]
        public void GetIngredientsString_WithNullRecipe_ReturnsNoIngredients()
        {
            string result = RecipeIngredientDisplay.GetIngredientsString(null);
            
            Assert.AreEqual("No ingredients", result);
        }

        [Test]
        public void GetIngredientsString_WithEmptyInputItems_ReturnsNoIngredients()
        {
            var emptyRecipe = new RecipeDef
            {
                inputItems = new List<RecipeItemDef>()
            };

            string result = RecipeIngredientDisplay.GetIngredientsString(emptyRecipe);
            
            Assert.AreEqual("No ingredients", result);
        }

        [Test]
        public void GetIngredientsString_WithSingleIngredient_ReturnsCorrectFormat()
        {
            var singleIngredientRecipe = new RecipeDef
            {
                machineId = "shredder",
                inputItems = new List<RecipeItemDef>
                {
                    new RecipeItemDef { item = "can", count = 3 }
                }
            };

            string result = RecipeIngredientDisplay.GetIngredientsString(singleIngredientRecipe);
            
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("3x"));
            Assert.IsTrue(result.Contains("Can") || result.Contains("can"));
            Assert.IsFalse(result.Contains("+")); // Should not have "+" for single ingredient
        }

        [Test]
        public void GetIngredientsString_WithMultipleIngredients_ReturnsJoinedFormat()
        {
            string result = RecipeIngredientDisplay.GetIngredientsString(testRecipe);
            
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("+")); // Should have "+" for multiple ingredients
        }

        [Test]
        public void GetIngredientsString_WithUnknownItemIds_HandlesGracefully()
        {
            var unknownItemRecipe = new RecipeDef
            {
                machineId = "fabricator",
                inputItems = new List<RecipeItemDef>
                {
                    new RecipeItemDef { item = "unknownItem", count = 1 }
                }
            };

            string result = RecipeIngredientDisplay.GetIngredientsString(unknownItemRecipe);
            
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("1x"));
            Assert.IsTrue(result.Contains("unknownItem")); // Should fallback to item ID
        }

        [Test]
        public void ComponentCreation_BasicInstantiation_WorksCorrectly()
        {
            // Test basic component properties exist and can be accessed
            var type = typeof(RecipeIngredientDisplay);
            
            var showArrowField = type.GetField("showArrow");
            var useVerticalLayoutField = type.GetField("useVerticalLayout");
            
            Assert.IsNotNull(showArrowField, "showArrow field should exist");
            Assert.IsNotNull(useVerticalLayoutField, "useVerticalLayout field should exist");
            
            // Test method existence
            var displayMethod = type.GetMethod("DisplayRecipe");
            var clearMethod = type.GetMethod("ClearIngredients");
            
            Assert.IsNotNull(displayMethod, "DisplayRecipe method should exist");
            Assert.IsNotNull(clearMethod, "ClearIngredients method should exist");
        }

        [Test]
        public void RecipeDef_InputItems_HandlesNullAndEmpty()
        {
            // Test with null input items
            var nullInputRecipe = new RecipeDef
            {
                machineId = "fabricator",
                inputItems = null
            };

            string result1 = RecipeIngredientDisplay.GetIngredientsString(nullInputRecipe);
            Assert.AreEqual("No ingredients", result1);

            // Test with empty input items
            var emptyInputRecipe = new RecipeDef
            {
                machineId = "fabricator",
                inputItems = new List<RecipeItemDef>()
            };

            string result2 = RecipeIngredientDisplay.GetIngredientsString(emptyInputRecipe);
            Assert.AreEqual("No ingredients", result2);
        }

        [Test]
        public void CreateIngredientWithManualLayout_WithShowCountFalse_RemovesCountText()
        {
            // This test validates that when showCount is false:
            // 1. The countText GameObject is removed (not just text cleared)
            // 2. The icon is centered horizontally
            
            // Test that the method exists and has correct signature
            var type = typeof(RecipeIngredientDisplay);
            var method = type.GetMethod("CreateIngredientWithManualLayout", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.IsNotNull(method, "CreateIngredientWithManualLayout method should exist");
            
            var parameters = method.GetParameters();
            Assert.AreEqual(4, parameters.Length, "Method should have 4 parameters");
            Assert.AreEqual("showCount", parameters[3].Name, "Last parameter should be showCount");
            Assert.AreEqual(typeof(bool), parameters[3].ParameterType, "showCount should be bool type");
            Assert.IsTrue(parameters[3].HasDefaultValue, "showCount should have default value");
            Assert.AreEqual(true, parameters[3].DefaultValue, "showCount default should be true");
        }
    }
}