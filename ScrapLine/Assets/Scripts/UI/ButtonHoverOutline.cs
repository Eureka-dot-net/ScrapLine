using UnityEngine;
using UnityEngine.UI; // Needed for the Outline component
using UnityEngine.EventSystems; // Needed for IPointerEnterHandler/IPointerExitHandler

/// <summary>
/// Attaches to a UI Button and controls the visibility of an attached Outline component
/// based on mouse hover state, simulating the active border from the HTML mockup.
/// It also handles changing the mouse cursor appearance on hover.
/// </summary>
public class ButtonHoverOutline : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Outline outline;
    
    // The color we want for the border (Orange/Accent: #FFA500)
    // Note: Colors in Unity C# are normalized from 0 to 1
    public Color hoverColor = new Color(1f, 165f / 255f, 0f, 1f); 
    
    // Optional: Reference to a custom cursor texture if you use one.
    // public Texture2D customCursor; 
    // public Vector2 hotSpot = Vector2.zero; // Hotspot for the custom cursor

    void Start()
    {
        // Get the Outline component attached to this GameObject
        outline = GetComponent<Outline>();

        if (outline == null)
        {
            // If it doesn't exist, we add one automatically
            outline = gameObject.AddComponent<Outline>();
            Debug.Log($"Added Outline component to {gameObject.name}.");
        }

        // Initialize the outline settings
        outline.effectColor = hoverColor;
        // Adjust the distance to create a thin, tight border effect (you may tune this in the Inspector)
        outline.effectDistance = new Vector2(10, -10); 
        
        // Ensure it starts disabled
        outline.enabled = false;
    }

    /// <summary>
    /// Called when the pointer enters the object (mouseover).
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (outline != null)
        {
            // Enable the orange outline border
            outline.enabled = true;
            
            // 1. Change cursor to the hand pointer (Pointer)
            // If you had a custom cursor texture, you would use:
            // Cursor.SetCursor(customCursor, hotSpot, CursorMode.Auto);
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); // Set to default hardware cursor (null)
            Cursor.visible = true;
            
            // On Windows/Mac, this is usually enough to default to the hand icon for buttons.
            // If you need explicit control, you must import a custom cursor texture.
        }
    }

    /// <summary>
    /// Called when the pointer exits the object (mouseout).
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (outline != null)
        {
            // Disable the orange outline border
            outline.enabled = false;
            
            // 2. Revert cursor back to the default arrow
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); 
        }
    }
}
