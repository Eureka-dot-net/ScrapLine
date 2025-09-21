using UnityEngine;

/// <summary>
/// Defines the current game interaction mode
/// </summary>
public enum GameMode
{
    Build,   // Building and placing machines (current functionality + drag-drop)
    Manage   // Future: Managing existing machines with multi-select operations
}

/// <summary>
/// Configuration settings for drag and drop operations
/// </summary>
[System.Serializable]
public class DragDropSettings
{
    [Header("Refund Settings")]
    [Range(0f, 1f)] 
    [Tooltip("Percentage of credits refunded when a machine is deleted (0.8 = 80%)")]
    public float refundPercentage = 0.8f;

    [Header("Input Detection")]
    [Range(0.1f, 1f)] 
    [Tooltip("Time threshold to distinguish click from drag (seconds)")]
    public float dragTimeThreshold = 0.3f;
    
    [Range(1, 50)] 
    [Tooltip("Pixel movement threshold to distinguish click from drag")]
    public int pixelMovementThreshold = 5;

    [Header("Visual Feedback")]
    [Tooltip("Enable grid highlighting during drag operations")]
    public bool showGridHighlighting = true;
    
    [Tooltip("Enable haptic feedback on mobile devices")]
    public bool enableHapticFeedback = true;
    
    [Range(0.1f, 1f)]
    [Tooltip("Alpha transparency for ghost machine during drag")]
    public float ghostMachineAlpha = 0.6f;
}