using UnityEngine;

/// <summary>
/// Static helper class for easy access to LoggingManager functionality.
/// Provides convenience methods that automatically handle instance checking.
/// 
/// IMPORTANT: Components MUST use this class instead of Debug.Log directly.
/// All logging must go through the central LoggingManager to enable filtering and state-change detection.
/// </summary>
public static class GameLogger
{
    /// <summary>
    /// Log a message with automatic fallback to Debug.Log if LoggingManager not available
    /// </summary>
    /// <param name="category">The log category</param>
    /// <param name="message">The message to log</param>
    /// <param name="componentId">Unique identifier for the component (optional)</param>
    /// <param name="context">Optional Unity context object</param>
    public static void Log(LoggingManager.LogCategory category, string message, string componentId = null, UnityEngine.Object context = null)
    {
        if (LoggingManager.Instance != null)
        {
            LoggingManager.Instance.Log(category, message, componentId, context);
        }
        else
        {
            // Fallback for cases where LoggingManager is not yet initialized
            Debug.Log($"[{category}] {message}", context);
        }
    }

    /// <summary>
    /// Log a warning message
    /// </summary>
    /// <param name="category">The log category</param>
    /// <param name="message">The warning message to log</param>
    /// <param name="componentId">Unique identifier for the component (optional)</param>
    /// <param name="context">Optional Unity context object</param>
    public static void LogWarning(LoggingManager.LogCategory category, string message, string componentId = null, UnityEngine.Object context = null)
    {
        if (LoggingManager.Instance != null)
        {
            LoggingManager.Instance.LogWarning(category, message, componentId, context);
        }
        else
        {
            Debug.LogWarning($"[{category}] {message}", context);
        }
    }

    /// <summary>
    /// Log an error message
    /// </summary>
    /// <param name="category">The log category</param>
    /// <param name="message">The error message to log</param>
    /// <param name="componentId">Unique identifier for the component (optional)</param>
    /// <param name="context">Optional Unity context object</param>
    public static void LogError(LoggingManager.LogCategory category, string message, string componentId = null, UnityEngine.Object context = null)
    {
        if (LoggingManager.Instance != null)
        {
            LoggingManager.Instance.LogError(category, message, componentId, context);
        }
        else
        {
            Debug.LogError($"[{category}] {message}", context);
        }
    }

    /// <summary>
    /// Notify the logger that a component's state has changed
    /// </summary>
    /// <param name="componentId">Unique identifier for the component</param>
    public static void NotifyStateChange(string componentId)
    {
        if (LoggingManager.Instance != null)
        {
            LoggingManager.Instance.NotifyStateChange(componentId);
        }
    }

    /// <summary>
    /// Check if a specific log category is enabled
    /// </summary>
    /// <param name="category">The category to check</param>
    /// <returns>True if the category is enabled</returns>
    public static bool IsCategoryEnabled(LoggingManager.LogCategory category)
    {
        if (LoggingManager.Instance != null)
        {
            return LoggingManager.Instance.IsCategoryEnabled(category);
        }
        return true; // Default to enabled if manager not available
    }

    /// <summary>
    /// Enable or disable a specific log category at runtime
    /// </summary>
    /// <param name="category">The category to modify</param>
    /// <param name="enabled">Whether to enable or disable the category</param>
    public static void SetCategoryEnabled(LoggingManager.LogCategory category, bool enabled)
    {
        if (LoggingManager.Instance != null)
        {
            LoggingManager.Instance.SetCategoryEnabled(category, enabled);
        }
    }

    // Convenience methods for common categories

    /// <summary>
    /// Log a movement-related message
    /// </summary>
    public static void LogMovement(string message, string componentId = null, UnityEngine.Object context = null)
    {
        Log(LoggingManager.LogCategory.Movement, message, componentId, context);
    }

    /// <summary>
    /// Log a fabricator-related message
    /// </summary>
    public static void LogFabricator(string message, string componentId = null, UnityEngine.Object context = null)
    {
        Log(LoggingManager.LogCategory.Fabricator, message, componentId, context);
    }

    /// <summary>
    /// Log a processor-related message
    /// </summary>
    public static void LogProcessor(string message, string componentId = null, UnityEngine.Object context = null)
    {
        Log(LoggingManager.LogCategory.Processor, message, componentId, context);
    }

    /// <summary>
    /// Log a grid-related message
    /// </summary>
    public static void LogGrid(string message, string componentId = null, UnityEngine.Object context = null)
    {
        Log(LoggingManager.LogCategory.Grid, message, componentId, context);
    }

    /// <summary>
    /// Log a UI-related message
    /// </summary>
    public static void LogUI(string message, string componentId = null, UnityEngine.Object context = null)
    {
        Log(LoggingManager.LogCategory.UI, message, componentId, context);
    }

    /// <summary>
    /// Log a save/load-related message
    /// </summary>
    public static void LogSaveLoad(string message, string componentId = null, UnityEngine.Object context = null)
    {
        Log(LoggingManager.LogCategory.SaveLoad, message, componentId, context);
    }

    /// <summary>
    /// Log a machine-related message
    /// </summary>
    public static void LogMachine(string message, string componentId = null, UnityEngine.Object context = null)
    {
        Log(LoggingManager.LogCategory.Machine, message, componentId, context);
    }

    /// <summary>
    /// Log an economy-related message
    /// </summary>
    public static void LogEconomy(string message, string componentId = null, UnityEngine.Object context = null)
    {
        Log(LoggingManager.LogCategory.Economy, message, componentId, context);
    }

    /// <summary>
    /// Log a spawning-related message
    /// </summary>
    public static void LogSpawning(string message, string componentId = null, UnityEngine.Object context = null)
    {
        Log(LoggingManager.LogCategory.Spawning, message, componentId, context);
    }

    /// <summary>
    /// Log a selling-related message
    /// </summary>
    public static void LogSelling(string message, string componentId = null, UnityEngine.Object context = null)
    {
        Log(LoggingManager.LogCategory.Selling, message, componentId, context);
    }

    /// <summary>
    /// Log a debug message
    /// </summary>
    public static void LogDebug(string message, string componentId = null, UnityEngine.Object context = null)
    {
        Log(LoggingManager.LogCategory.Debug, message, componentId, context);
    }
}