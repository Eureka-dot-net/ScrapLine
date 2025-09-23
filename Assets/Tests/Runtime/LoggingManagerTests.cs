using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

/// <summary>
/// Unit tests for LoggingManager and GameLogger functionality.
/// Tests category filtering, state-change detection, and message formatting.
/// </summary>
public class LoggingManagerTests
{
    private LoggingManager loggingManager;
    private GameObject testGameObject;

    [SetUp]
    public void SetUp()
    {
        // Create a test GameObject with LoggingManager
        testGameObject = new GameObject("TestLoggingManager");
        loggingManager = testGameObject.AddComponent<LoggingManager>();
        
        // Set the static instance for testing
        var instanceField = typeof(LoggingManager).GetField("Instance", 
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        instanceField.SetValue(null, loggingManager);
    }

    [TearDown]
    public void TearDown()
    {
        if (testGameObject != null)
        {
            Object.DestroyImmediate(testGameObject);
        }
        
        // Clear the static instance
        var instanceField = typeof(LoggingManager).GetField("Instance", 
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        instanceField.SetValue(null, null);
    }

    [Test]
    public void LoggingManager_SingletonInitialization_SetsInstanceCorrectly()
    {
        // Arrange & Act - already done in SetUp
        
        // Assert
        Assert.IsNotNull(LoggingManager.Instance);
        Assert.AreEqual(loggingManager, LoggingManager.Instance);
    }

    [Test]
    public void IsCategoryEnabled_DefaultState_ReturnsExpectedValues()
    {
        // Arrange
        // Default state should have most categories disabled except Debug
        
        // Act & Assert
        Assert.IsFalse(loggingManager.IsCategoryEnabled(LoggingManager.LogCategory.Movement));
        Assert.IsFalse(loggingManager.IsCategoryEnabled(LoggingManager.LogCategory.Fabricator));
        Assert.IsFalse(loggingManager.IsCategoryEnabled(LoggingManager.LogCategory.Processor));
        Assert.IsFalse(loggingManager.IsCategoryEnabled(LoggingManager.LogCategory.Grid));
        Assert.IsFalse(loggingManager.IsCategoryEnabled(LoggingManager.LogCategory.UI));
        Assert.IsFalse(loggingManager.IsCategoryEnabled(LoggingManager.LogCategory.SaveLoad));
        Assert.IsFalse(loggingManager.IsCategoryEnabled(LoggingManager.LogCategory.Machine));
        Assert.IsFalse(loggingManager.IsCategoryEnabled(LoggingManager.LogCategory.Economy));
        Assert.IsFalse(loggingManager.IsCategoryEnabled(LoggingManager.LogCategory.Spawning));
        Assert.IsFalse(loggingManager.IsCategoryEnabled(LoggingManager.LogCategory.Selling));
        Assert.IsTrue(loggingManager.IsCategoryEnabled(LoggingManager.LogCategory.Debug));
    }

    [Test]
    public void SetCategoryEnabled_EnablesAndDisablesCategories()
    {
        // Arrange
        var category = LoggingManager.LogCategory.Movement;
        
        // Act - Enable
        loggingManager.SetCategoryEnabled(category, true);
        
        // Assert - Enabled
        Assert.IsTrue(loggingManager.IsCategoryEnabled(category));
        
        // Act - Disable
        loggingManager.SetCategoryEnabled(category, false);
        
        // Assert - Disabled
        Assert.IsFalse(loggingManager.IsCategoryEnabled(category));
    }

    [Test]
    public void Log_WhenCategoryDisabled_DoesNotLog()
    {
        // Arrange
        loggingManager.SetCategoryEnabled(LoggingManager.LogCategory.Movement, false);
        
        // Act
        LogTestUtils.ClearLogEntries();
        loggingManager.Log(LoggingManager.LogCategory.Movement, "Test message");
        
        // Assert
        LogAssert.NoUnexpectedReceived();
    }

    [Test]
    public void Log_WhenCategoryEnabled_LogsMessage()
    {
        // Arrange
        loggingManager.SetCategoryEnabled(LoggingManager.LogCategory.Movement, true);
        
        // Act & Assert
        LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(".*MOVEMENT.*Test message"));
        loggingManager.Log(LoggingManager.LogCategory.Movement, "Test message");
    }

    [Test]
    public void NotifyStateChange_ClearsComponentState()
    {
        // Arrange
        loggingManager.SetCategoryEnabled(LoggingManager.LogCategory.Movement, true);
        string componentId = "TestComponent_1";
        
        // Log the same message twice
        loggingManager.Log(LoggingManager.LogCategory.Movement, "Duplicate message", componentId);
        
        // Act - Notify state change
        loggingManager.NotifyStateChange(componentId);
        
        // Assert - Next identical message should be allowed
        LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(".*Duplicate message"));
        loggingManager.Log(LoggingManager.LogCategory.Movement, "Duplicate message", componentId);
    }

    [Test]
    public void ClearComponentStates_RemovesAllTrackedStates()
    {
        // Arrange
        loggingManager.SetCategoryEnabled(LoggingManager.LogCategory.Movement, true);
        loggingManager.Log(LoggingManager.LogCategory.Movement, "Test message", "Component1");
        loggingManager.Log(LoggingManager.LogCategory.Movement, "Test message", "Component2");
        
        // Act
        loggingManager.ClearComponentStates();
        
        // Assert - Logging stats should show no tracked components
        string stats = loggingManager.GetLoggingStats();
        Assert.IsTrue(stats.Contains("0 tracked components"));
    }

    [Test]
    public void GetLoggingStats_ReturnsValidStatistics()
    {
        // Arrange
        loggingManager.SetCategoryEnabled(LoggingManager.LogCategory.Movement, true);
        loggingManager.Log(LoggingManager.LogCategory.Movement, "Test message", "Component1");
        
        // Act
        string stats = loggingManager.GetLoggingStats();
        
        // Assert
        Assert.IsNotNull(stats);
        Assert.IsTrue(stats.Contains("tracked components"));
        Assert.IsTrue(stats.Contains("messages suppressed"));
    }

    [Test]
    public void GameLogger_StaticMethods_WorkWithoutInstance()
    {
        // Arrange - Destroy instance to test fallback
        Object.DestroyImmediate(testGameObject);
        var instanceField = typeof(LoggingManager).GetField("Instance", 
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        instanceField.SetValue(null, null);
        
        // Act & Assert - Should not throw exceptions
        Assert.DoesNotThrow(() => GameLogger.LogMovement("Test message"));
        Assert.DoesNotThrow(() => GameLogger.LogWarning(LoggingManager.LogCategory.Debug, "Warning"));
        Assert.DoesNotThrow(() => GameLogger.LogError(LoggingManager.LogCategory.Debug, "Error"));
        Assert.DoesNotThrow(() => GameLogger.NotifyStateChange("Component"));
        
        // IsCategoryEnabled should return true as fallback
        Assert.IsTrue(GameLogger.IsCategoryEnabled(LoggingManager.LogCategory.Movement));
    }

    [Test]
    public void GameLogger_ConvenienceMethods_CallCorrectCategories()
    {
        // Arrange
        loggingManager.SetCategoryEnabled(LoggingManager.LogCategory.Movement, true);
        loggingManager.SetCategoryEnabled(LoggingManager.LogCategory.Fabricator, true);
        loggingManager.SetCategoryEnabled(LoggingManager.LogCategory.Processor, true);
        
        // Act & Assert
        LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(".*MOVEMENT.*"));
        GameLogger.LogMovement("Movement test");
        
        LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(".*FABRICATOR.*"));
        GameLogger.LogFabricator("Fabricator test");
        
        LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(".*PROCESSOR.*"));
        GameLogger.LogProcessor("Processor test");
    }

    [Test]
    public void LogWarning_ProducesWarningMessage()
    {
        // Arrange
        loggingManager.SetCategoryEnabled(LoggingManager.LogCategory.Debug, true);
        
        // Act & Assert
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*DEBUG.*Warning message"));
        loggingManager.LogWarning(LoggingManager.LogCategory.Debug, "Warning message");
    }

    [Test]
    public void LogError_ProducesErrorMessage()
    {
        // Arrange
        loggingManager.SetCategoryEnabled(LoggingManager.LogCategory.Debug, true);
        
        // Act & Assert
        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*DEBUG.*Error message"));
        loggingManager.LogError(LoggingManager.LogCategory.Debug, "Error message");
    }

    [Test]
    public void AllCategories_CanBeSetAndRetrieved()
    {
        // Test all available categories
        var categories = new[]
        {
            LoggingManager.LogCategory.Movement,
            LoggingManager.LogCategory.Fabricator,
            LoggingManager.LogCategory.Processor,
            LoggingManager.LogCategory.Grid,
            LoggingManager.LogCategory.UI,
            LoggingManager.LogCategory.SaveLoad,
            LoggingManager.LogCategory.Machine,
            LoggingManager.LogCategory.Economy,
            LoggingManager.LogCategory.Spawning,
            LoggingManager.LogCategory.Selling,
            LoggingManager.LogCategory.Debug
        };

        foreach (var category in categories)
        {
            // Test enabling
            loggingManager.SetCategoryEnabled(category, true);
            Assert.IsTrue(loggingManager.IsCategoryEnabled(category), $"Category {category} should be enabled");
            
            // Test disabling
            loggingManager.SetCategoryEnabled(category, false);
            Assert.IsFalse(loggingManager.IsCategoryEnabled(category), $"Category {category} should be disabled");
        }
    }
}

/// <summary>
/// Helper class for log testing utilities
/// </summary>
public static class LogTestUtils
{
    public static void ClearLogEntries()
    {
        // This is a placeholder - Unity's LogAssert handles log clearing internally
        // In a real test environment, you might need platform-specific log clearing
    }
}