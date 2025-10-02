using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace ScrapLine.Tests
{
    /// <summary>
    /// Unit tests for UIPanelManager
    /// Tests panel lifecycle management, initialization, and coordination
    /// </summary>
    [TestFixture]
    public class UIPanelManagerTests
    {
        [Test]
        public void UIPanelManager_ClassExists()
        {
            // Verify the class can be referenced
            var typeName = typeof(UIPanelManager).Name;
            Assert.AreEqual("UIPanelManager", typeName);
        }

        [Test]
        public void UIPanelManager_InheritsFromMonoBehaviour()
        {
            // Verify correct base class
            Assert.IsTrue(typeof(MonoBehaviour).IsAssignableFrom(typeof(UIPanelManager)));
        }

        [Test]
        public void UIPanelManager_HasConfigPanelsProperty()
        {
            // Verify the property exists
            var property = typeof(UIPanelManager).GetField("configPanels");
            Assert.IsNotNull(property);
            Assert.AreEqual(typeof(List<MonoBehaviour>), property.FieldType);
        }

        [Test]
        public void UIPanelManager_HasRegisterOpenPanelMethod()
        {
            // Verify method exists
            var method = typeof(UIPanelManager).GetMethod("RegisterOpenPanel");
            Assert.IsNotNull(method);
            Assert.AreEqual(typeof(void), method.ReturnType);
        }

        [Test]
        public void UIPanelManager_HasUnregisterPanelMethod()
        {
            // Verify method exists
            var method = typeof(UIPanelManager).GetMethod("UnregisterPanel");
            Assert.IsNotNull(method);
            Assert.AreEqual(typeof(void), method.ReturnType);
        }

        [Test]
        public void UIPanelManager_HasCloseCurrentPanelMethod()
        {
            // Verify method exists
            var method = typeof(UIPanelManager).GetMethod("CloseCurrentPanel");
            Assert.IsNotNull(method);
            Assert.AreEqual(typeof(void), method.ReturnType);
        }

        [Test]
        public void UIPanelManager_HasGetCurrentOpenPanelMethod()
        {
            // Verify method exists
            var method = typeof(UIPanelManager).GetMethod("GetCurrentOpenPanel");
            Assert.IsNotNull(method);
            Assert.AreEqual(typeof(MonoBehaviour), method.ReturnType);
        }

        [Test]
        public void UIPanelManager_HasInitializeAllPanelsMethod()
        {
            // Verify method exists (internal method for testing)
            var method = typeof(UIPanelManager).GetMethod("InitializeAllPanels",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(method);
        }

        [Test]
        public void UIPanelManager_API_IsConsistent()
        {
            // Verify all expected public methods exist
            var publicMethods = typeof(UIPanelManager).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            var methodNames = new List<string>();
            foreach (var method in publicMethods)
            {
                methodNames.Add(method.Name);
            }

            Assert.Contains("RegisterOpenPanel", methodNames);
            Assert.Contains("UnregisterPanel", methodNames);
            Assert.Contains("CloseCurrentPanel", methodNames);
            Assert.Contains("GetCurrentOpenPanel", methodNames);
        }

        [Test]
        public void UIPanelManager_Integration_WithBaseConfigPanel()
        {
            // Verify UIPanelManager and BaseConfigPanel are compatible
            // BaseConfigPanel should be able to call UIPanelManager methods
            var registerMethod = typeof(UIPanelManager).GetMethod("RegisterOpenPanel");
            var unregisterMethod = typeof(UIPanelManager).GetMethod("UnregisterPanel");

            Assert.IsNotNull(registerMethod);
            Assert.IsNotNull(unregisterMethod);

            // Both methods should accept MonoBehaviour as parameter
            var registerParams = registerMethod.GetParameters();
            var unregisterParams = unregisterMethod.GetParameters();

            Assert.AreEqual(1, registerParams.Length);
            Assert.AreEqual(1, unregisterParams.Length);
            Assert.AreEqual(typeof(MonoBehaviour), registerParams[0].ParameterType);
            Assert.AreEqual(typeof(MonoBehaviour), unregisterParams[0].ParameterType);
        }
        
        [Test]
        public void UIPanelManager_HasCacheRecipeDisplayPrefabMethod()
        {
            // Verify method exists
            var method = typeof(UIPanelManager).GetMethod("CacheRecipeDisplayPrefab");
            Assert.IsNotNull(method, "CacheRecipeDisplayPrefab method should exist");
            Assert.AreEqual(typeof(void), method.ReturnType);
            
            // Check parameters
            var parameters = method.GetParameters();
            Assert.AreEqual(2, parameters.Length);
            Assert.AreEqual(typeof(int), parameters[0].ParameterType, "First parameter should be int (panelInstanceId)");
            Assert.AreEqual(typeof(RecipeIngredientDisplay), parameters[1].ParameterType, "Second parameter should be RecipeIngredientDisplay");
        }
        
        [Test]
        public void UIPanelManager_HasGetRecipeDisplayPrefabMethod()
        {
            // Verify method exists
            var method = typeof(UIPanelManager).GetMethod("GetRecipeDisplayPrefab");
            Assert.IsNotNull(method, "GetRecipeDisplayPrefab method should exist");
            Assert.AreEqual(typeof(RecipeIngredientDisplay), method.ReturnType, "Should return RecipeIngredientDisplay");
            
            // Check parameters
            var parameters = method.GetParameters();
            Assert.AreEqual(1, parameters.Length);
            Assert.AreEqual(typeof(int), parameters[0].ParameterType, "Parameter should be int (panelInstanceId)");
        }
        
        [Test]
        public void UIPanelManager_RecipeDisplayPrefabCache_HasCorrectFieldType()
        {
            // Verify the private field exists with correct type
            var field = typeof(UIPanelManager).GetField("recipeDisplayPrefabCache",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.IsNotNull(field, "recipeDisplayPrefabCache field should exist");
            Assert.IsTrue(field.FieldType.IsGenericType, "Field should be a generic type (Dictionary)");
            Assert.AreEqual(typeof(Dictionary<,>), field.FieldType.GetGenericTypeDefinition(), "Field should be a Dictionary");
            
            // Check dictionary key/value types
            var genericArgs = field.FieldType.GetGenericArguments();
            Assert.AreEqual(typeof(int), genericArgs[0], "Dictionary key should be int");
            Assert.AreEqual(typeof(RecipeIngredientDisplay), genericArgs[1], "Dictionary value should be RecipeIngredientDisplay");
        }
    }
}
