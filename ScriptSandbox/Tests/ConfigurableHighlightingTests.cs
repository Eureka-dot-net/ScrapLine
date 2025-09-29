using NUnit.Framework;
using UnityEngine;

namespace ScrapLine.Tests
{
    /// <summary>
    /// Unit tests for the configurable highlighting system
    /// Tests the new color configuration functionality for edit mode and UI hover
    /// </summary>
    [TestFixture]
    public class ConfigurableHighlightingTests
    {
        private GridColorConfiguration colorConfig;

        [SetUp]
        public void Setup()
        {
            colorConfig = new GridColorConfiguration();
        }

        [Test]
        public void GridColorConfiguration_HasDefaultEditModeHighlightColor()
        {
            // Arrange & Act
            Color defaultColor = colorConfig.editModeHighlightColor;
            
            // Assert - Default should be light green overlay
            Assert.AreEqual(0.5f, defaultColor.r, 0.01f, "Red component should be 0.5");
            Assert.AreEqual(1f, defaultColor.g, 0.01f, "Green component should be 1.0");
            Assert.AreEqual(0.7f, defaultColor.b, 0.01f, "Blue component should be 0.7");
            Assert.AreEqual(0.15f, defaultColor.a, 0.01f, "Alpha component should be 0.15");
        }

        [Test]
        public void GridColorConfiguration_HasDefaultUIHoverColor()
        {
            // Arrange & Act
            Color defaultColor = colorConfig.uiHoverColor;
            
            // Assert - Default should be orange (#FFA500)
            Assert.AreEqual(1f, defaultColor.r, 0.01f, "Red component should be 1.0");
            Assert.AreEqual(165f / 255f, defaultColor.g, 0.01f, "Green component should be 165/255");
            Assert.AreEqual(0f, defaultColor.b, 0.01f, "Blue component should be 0.0");
            Assert.AreEqual(1f, defaultColor.a, 0.01f, "Alpha component should be 1.0");
        }

        [Test]
        public void GridColorConfiguration_CanSetCustomEditModeColor()
        {
            // Arrange
            Color customColor = new Color(1f, 0f, 0f, 0.5f); // Red with 50% alpha
            
            // Act
            colorConfig.editModeHighlightColor = customColor;
            
            // Assert
            Assert.AreEqual(customColor, colorConfig.editModeHighlightColor);
        }

        [Test]
        public void GridColorConfiguration_CanSetCustomUIHoverColor()
        {
            // Arrange
            Color customColor = new Color(0f, 1f, 0f, 1f); // Green
            
            // Act
            colorConfig.uiHoverColor = customColor;
            
            // Assert
            Assert.AreEqual(customColor, colorConfig.uiHoverColor);
        }

        [Test]
        public void GridColorConfiguration_ColorToHex_ConvertsColorsCorrectly()
        {
            // Arrange & Act
            string redHex = GridColorConfiguration.ColorToHex(Color.red);
            string greenHex = GridColorConfiguration.ColorToHex(Color.green);
            string blueHex = GridColorConfiguration.ColorToHex(Color.blue);
            
            // Assert
            Assert.AreEqual("#FF0000", redHex, "Red should convert to #FF0000");
            Assert.AreEqual("#00FF00", greenHex, "Green should convert to #00FF00");
            Assert.AreEqual("#0000FF", blueHex, "Blue should convert to #0000FF");
        }

        [Test]
        public void GridColorConfiguration_Implementation_Validation()
        {
            // Arrange & Act & Assert
            // Test that the new properties exist and are accessible
            Assert.IsNotNull(colorConfig, "GridColorConfiguration should be instantiable");
            
            // Verify new color properties exist
            Assert.DoesNotThrow(() => {
                Color editColor = colorConfig.editModeHighlightColor;
                Color hoverColor = colorConfig.uiHoverColor;
            }, "New color properties should be accessible");
            
            // Verify that colors have reasonable default values
            Assert.IsTrue(colorConfig.editModeHighlightColor.a > 0, "Edit mode highlight should have some alpha");
            Assert.IsTrue(colorConfig.uiHoverColor.a > 0, "UI hover color should have some alpha");
        }

        [Test]
        public void MachineRenderer_HighlightMethods_Exist()
        {
            // Arrange & Act & Assert
            // Test that the new methods exist without calling them (to avoid Unity dependency issues)
            var rendererType = typeof(MachineRenderer);
            
            Assert.IsNotNull(rendererType.GetMethod("HighlightBorder"), "HighlightBorder method should exist");
            Assert.IsNotNull(rendererType.GetMethod("HighlightBuilding"), "HighlightBuilding method should exist");
            Assert.IsNotNull(rendererType.GetMethod("SetBorderInteraction"), "SetBorderInteraction method should exist");
        }
    }
}