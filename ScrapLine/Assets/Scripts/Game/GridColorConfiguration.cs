using UnityEngine;

/// <summary>
/// Configuration class for customizing grid cell colors in the factory game.
/// This allows designers to easily adjust the color scheme from the Unity Inspector.
/// </summary>
[System.Serializable]
public class GridColorConfiguration
{
    [Header("Grid Cell Colors")]
    [Tooltip("Color for the top row cells (seller area) - default pink")]
    public Color topRowColor = new Color(1f, 0.667f, 0.667f, 1f); // #FFAAAA
    
    [Tooltip("Color for the middle grid cells (main play area) - default grey")]
    public Color gridColor = new Color(0.8f, 0.8f, 0.8f, 1f); // Light grey
    
    [Tooltip("Color for the bottom row cells (spawner area) - default green")]
    public Color bottomRowColor = new Color(0.667f, 1f, 0.667f, 1f); // #AAFFAA
    
    [Header("Edit Mode Colors")]
    [Tooltip("Color for highlighting machines in edit mode - default light green")]
    public Color editModeHighlightColor = new Color(0.5f, 1f, 0.7f, 0.15f); // Light green overlay
    
    [Header("UI Interaction Colors")]
    [Tooltip("Color for UI button hover outlines - default orange")]
    public Color uiHoverColor = new Color(1f, 165f / 255f, 0f, 1f); // Orange (#FFA500)
    
    /// <summary>
    /// Convert a Unity Color to hex string format for use in machine definitions
    /// </summary>
    /// <param name="color">The Unity Color to convert</param>
    /// <returns>Hex color string in format #RRGGBB</returns>
    public static string ColorToHex(Color color)
    {
        return $"#{(int)(color.r * 255):X2}{(int)(color.g * 255):X2}{(int)(color.b * 255):X2}";
    }
    
    /// <summary>
    /// Get the hex color string for top row cells
    /// </summary>
    public string GetTopRowHexColor()
    {
        return ColorToHex(topRowColor);
    }
    
    /// <summary>
    /// Get the hex color string for grid cells (returns null for default grey)
    /// </summary>
    public string GetGridHexColor()
    {
        // For grid cells, we typically don't set a border color to keep them neutral
        // Return null to use the default sprite color
        return null;
    }
    
    /// <summary>
    /// Get the hex color string for bottom row cells
    /// </summary>
    public string GetBottomRowHexColor()
    {
        return ColorToHex(bottomRowColor);
    }
}