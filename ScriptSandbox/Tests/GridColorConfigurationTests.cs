using NUnit.Framework;
using UnityEngine;

namespace ScrapLine.Tests
{
    [TestFixture]
    public class GridColorConfigurationTests
    {
        [Test]
        public void GridColorConfiguration_DefaultColors_AreCorrect()
        {
            // Arrange
            var config = new GridColorConfiguration();

            // Act & Assert - Check default colors match the original JSON values
            Assert.AreEqual("#FFAAAA", config.GetTopRowHexColor(), "Top row should be light pink by default");
            Assert.IsNull(config.GetGridHexColor(), "Grid should use default color (null)");
            Assert.AreEqual("#AAFFAA", config.GetBottomRowHexColor(), "Bottom row should be light green by default");
        }

        [Test]
        public void GridColorConfiguration_CustomColors_ConvertCorrectly()
        {
            // Arrange
            var config = new GridColorConfiguration();
            config.topRowColor = Color.red;
            config.bottomRowColor = Color.blue;

            // Act & Assert
            Assert.AreEqual("#FF0000", config.GetTopRowHexColor(), "Red should convert to #FF0000");
            Assert.AreEqual("#0000FF", config.GetBottomRowHexColor(), "Blue should convert to #0000FF");
        }

        [Test]
        public void ColorToHex_ConvertsColorsCorrectly()
        {
            // Test various colors
            Assert.AreEqual("#FF0000", GridColorConfiguration.ColorToHex(Color.red));
            Assert.AreEqual("#00FF00", GridColorConfiguration.ColorToHex(Color.green));
            Assert.AreEqual("#0000FF", GridColorConfiguration.ColorToHex(Color.blue));
            Assert.AreEqual("#FFFFFF", GridColorConfiguration.ColorToHex(Color.white));
            Assert.AreEqual("#000000", GridColorConfiguration.ColorToHex(Color.black));
        }

        [Test]
        public void FactoryRegistry_ApplyColorConfiguration_UpdatesMachineDefinitions()
        {
            // Arrange
            var registry = FactoryRegistry.Instance;
            
            // Create test machine definitions
            var testMachines = "{ \"machines\": [ " +
                "{ \"id\": \"blank_top\", \"borderColor\": \"#FFAAAA\" }, " +
                "{ \"id\": \"blank\", \"borderColor\": null }, " +
                "{ \"id\": \"blank_bottom\", \"borderColor\": \"#AAFFAA\" } " +
                "] }";
            
            var colorConfig = new GridColorConfiguration();
            colorConfig.topRowColor = Color.magenta;
            colorConfig.bottomRowColor = Color.cyan;

            // Act
            registry.LoadFromJson(testMachines, "{}", "{ \"items\": [] }", null, colorConfig);

            // Assert
            var topMachine = registry.GetMachine("blank_top");
            var gridMachine = registry.GetMachine("blank");
            var bottomMachine = registry.GetMachine("blank_bottom");

            Assert.IsNotNull(topMachine);
            Assert.IsNotNull(gridMachine);
            Assert.IsNotNull(bottomMachine);

            Assert.AreEqual("#FF00FF", topMachine.borderColor, "Top machine should use magenta color");
            Assert.IsNull(gridMachine.borderColor, "Grid machine should use default color (null)");
            Assert.AreEqual("#00FFFF", bottomMachine.borderColor, "Bottom machine should use cyan color");
        }
    }
}