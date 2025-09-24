using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Central logging manager that provides category-based logging with state-change detection.
/// Prevents log flooding by only showing messages when component state actually changes.
/// Allows runtime configuration of which log categories to display.
/// </summary>
public class LoggingManager : MonoBehaviour
{
    public static LoggingManager Instance { get; private set; }

    [Header("Logging Configuration")]
    [SerializeField] private bool enableMovementLogs = false;
    [SerializeField] private bool enableFabricatorLogs = false;
    [SerializeField] private bool enableProcessorLogs = false;
    [SerializeField] private bool enableGridLogs = false;
    [SerializeField] private bool enableUILogs = false;
    [SerializeField] private bool enableSaveLoadLogs = false;
    [SerializeField] private bool enableMachineLogs = false;
    [SerializeField] private bool enableEconomyLogs = false;
    [SerializeField] private bool enableSpawningLogs = true;
    [SerializeField] private bool enableSellingLogs = false;
    [SerializeField] private bool enableDebugLogs = true;

    [Header("Advanced Settings")]
    [SerializeField] private bool enableStateChangeDetection = true;
    [SerializeField] private float stateChangeTimeoutSeconds = 1.0f;
    [SerializeField] private bool showTimestamps = true;
    [SerializeField] private bool showCategoryPrefixes = true;

    // State tracking for duplicate prevention
    private Dictionary<string, StateInfo> componentStates = new Dictionary<string, StateInfo>();

    private struct StateInfo
    {
        public string lastMessage;
        public float lastLogTime;
        public int duplicateCount;
    }

    /// <summary>
    /// Available logging categories
    /// </summary>
    public enum LogCategory
    {
        Movement,
        Fabricator,
        Processor,
        Grid,
        UI,
        SaveLoad,
        Machine,
        Economy,
        Spawning,
        Selling,
        Debug
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        UnityGlobals.DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Check if a specific log category is enabled
    /// </summary>
    /// <param name="category">The category to check</param>
    /// <returns>True if the category is enabled</returns>
    public bool IsCategoryEnabled(LogCategory category)
    {
        return category switch
        {
            LogCategory.Movement => enableMovementLogs,
            LogCategory.Fabricator => enableFabricatorLogs,
            LogCategory.Processor => enableProcessorLogs,
            LogCategory.Grid => enableGridLogs,
            LogCategory.UI => enableUILogs,
            LogCategory.SaveLoad => enableSaveLoadLogs,
            LogCategory.Machine => enableMachineLogs,
            LogCategory.Economy => enableEconomyLogs,
            LogCategory.Spawning => enableSpawningLogs,
            LogCategory.Selling => enableSellingLogs,
            LogCategory.Debug => enableDebugLogs,
            _ => false
        };
    }

    /// <summary>
    /// Log a message with category filtering and state-change detection
    /// </summary>
    /// <param name="category">The log category</param>
    /// <param name="message">The message to log</param>
    /// <param name="componentId">Unique identifier for the component (e.g., "Fabricator_5_3" for fabricator at grid 5,3)</param>
    /// <param name="context">Optional Unity context object</param>
    public void Log(LogCategory category, string message, string componentId = null, UnityEngine.Object context = null)
    {
        // Early exit if category is disabled
        if (!IsCategoryEnabled(category))
            return;

        string finalMessage = FormatMessage(category, message);

        // Handle state change detection if enabled and componentId provided
        if (enableStateChangeDetection && !string.IsNullOrEmpty(componentId))
        {
            if (ShouldSuppressDuplicate(componentId, finalMessage))
                return;
        }

        // Log the message
        UnityEngine.Debug.Log(finalMessage, context);
    }

    /// <summary>
    /// Log a warning message with category filtering
    /// </summary>
    /// <param name="category">The log category</param>
    /// <param name="message">The warning message to log</param>
    /// <param name="componentId">Unique identifier for the component</param>
    /// <param name="context">Optional Unity context object</param>
    public void LogWarning(LogCategory category, string message, string componentId = null, UnityEngine.Object context = null)
    {
        // Early exit if category is disabled
        if (!IsCategoryEnabled(category))
            return;

        string finalMessage = FormatMessage(category, message);
        UnityEngine.Debug.LogWarning(finalMessage, context);
    }

    /// <summary>
    /// Log an error message with category filtering
    /// </summary>
    /// <param name="category">The log category</param>
    /// <param name="message">The error message to log</param>
    /// <param name="componentId">Unique identifier for the component</param>
    /// <param name="context">Optional Unity context object</param>
    public void LogError(LogCategory category, string message, string componentId = null, UnityEngine.Object context = null)
    {
        // Early exit if category is disabled
        if (!IsCategoryEnabled(category))
            return;

        string finalMessage = FormatMessage(category, message);
        UnityEngine.Debug.LogError(finalMessage, context);
    }

    /// <summary>
    /// Notify the logger that a component's state has changed, allowing the next log to be shown
    /// </summary>
    /// <param name="componentId">Unique identifier for the component</param>
    public void NotifyStateChange(string componentId)
    {
        if (string.IsNullOrEmpty(componentId))
            return;

        if (componentStates.ContainsKey(componentId))
        {
            var state = componentStates[componentId];
            state.lastMessage = null; // Clear last message to allow next log
            state.duplicateCount = 0;
            componentStates[componentId] = state;
        }
    }

    /// <summary>
    /// Enable or disable a specific log category at runtime
    /// </summary>
    /// <param name="category">The category to modify</param>
    /// <param name="enabled">Whether to enable or disable the category</param>
    public void SetCategoryEnabled(LogCategory category, bool enabled)
    {
        switch (category)
        {
            case LogCategory.Movement:
                enableMovementLogs = enabled;
                break;
            case LogCategory.Fabricator:
                enableFabricatorLogs = enabled;
                break;
            case LogCategory.Processor:
                enableProcessorLogs = enabled;
                break;
            case LogCategory.Grid:
                enableGridLogs = enabled;
                break;
            case LogCategory.UI:
                enableUILogs = enabled;
                break;
            case LogCategory.SaveLoad:
                enableSaveLoadLogs = enabled;
                break;
            case LogCategory.Machine:
                enableMachineLogs = enabled;
                break;
            case LogCategory.Economy:
                enableEconomyLogs = enabled;
                break;
            case LogCategory.Spawning:
                enableSpawningLogs = enabled;
                break;
            case LogCategory.Selling:
                enableSellingLogs = enabled;
                break;
            case LogCategory.Debug:
                enableDebugLogs = enabled;
                break;
        }
    }

    private string FormatMessage(LogCategory category, string message)
    {
        string formattedMessage = message;

        if (showCategoryPrefixes)
        {
            formattedMessage = $"[{category.ToString().ToUpper()}] {formattedMessage}";
        }

        if (showTimestamps)
        {
            formattedMessage = $"[{Time.time:F2}s] {formattedMessage}";
        }

        return formattedMessage;
    }

    private static string ExtractMessage(string logString)
    {
        if (string.IsNullOrEmpty(logString))
            return logString;
        // Find the index of the last closing bracket.
        int startIndex = logString.LastIndexOf(']');

        // If a closing bracket is found, return the substring after it.
        // We add +2 to skip the bracket and the space that follows.
        if (startIndex != -1 && startIndex + 2 <= logString.Length)
        {
            return logString.Substring(startIndex + 2);
        }

        // If no closing bracket is found, return the original string.
        return logString;
    }

    private bool ShouldSuppressDuplicate(string componentId, string message)
    {
        if (!componentStates.ContainsKey(componentId))
        {
            componentStates[componentId] = new StateInfo
            {
                lastMessage = message,
                lastLogTime = Time.time,
                duplicateCount = 0
            };
            return false; // First message from this component, allow it
        }

        var state = componentStates[componentId];

        // Check if this is the same message as last time
        if (ExtractMessage(state.lastMessage) == ExtractMessage(message))
        {
            // Check if enough time has passed to show it again
            if (Time.time - state.lastLogTime < stateChangeTimeoutSeconds)
            {
                state.duplicateCount++;
                componentStates[componentId] = state;
                return true; // Suppress duplicate
            }
        }

        // Different message or enough time passed, update state and allow
        state.lastMessage = message;
        state.lastLogTime = Time.time;
        state.duplicateCount = 0;
        componentStates[componentId] = state;
        return false;
    }

    /// <summary>
    /// Clear all cached component states (useful for testing or when restarting)
    /// </summary>
    public void ClearComponentStates()
    {
        componentStates.Clear();
    }

    /// <summary>
    /// Get statistics about logging activity
    /// </summary>
    /// <returns>A formatted string with logging statistics</returns>
    public string GetLoggingStats()
    {
        int totalComponents = componentStates.Count;
        int suppressedMessages = 0;

        foreach (var state in componentStates.Values)
        {
            suppressedMessages += state.duplicateCount;
        }

        return $"Logging Stats: {totalComponents} tracked components, {suppressedMessages} messages suppressed";
    }
}