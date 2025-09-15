using UnityEngine;
using UnityEngine.UI;

public class UITextureScroller : MonoBehaviour
{
    private Material mat;
    private Image imageComponent;
    private ConveyorBelt belt; // Reference to parent's ConveyorBelt
    
    void Start()
    {
        imageComponent = GetComponent<Image>();
        if (imageComponent == null)
        {
            Debug.LogError("UITextureScroller requires an Image component!");
            return;
        }
        
        // For UI Images, we need to create an instance of the material to avoid affecting shared materials
        if (imageComponent.material != null)
        {
            mat = new Material(imageComponent.material);
            imageComponent.material = mat;
            Debug.Log($"UITextureScroller: Created material instance for {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"UITextureScroller: No material found on Image component of {gameObject.name}");
            return;
        }
        
        belt = GetComponentInParent<ConveyorBelt>();
        if (belt == null)
        {
            Debug.LogWarning("No ConveyorBelt component found in parent. Using default direction (Up) and speed (0.5).");
        }
    }

    void Update()
    {
        if (mat == null) return;
        
        Vector2 offset = mat.GetTextureOffset("_BaseMap"); // For URP Unlit shader

        if (belt != null)
        {
            // Get the visual scrolling direction based on machine orientation
            Vector2 dir = GetVisualScrollDirection();
            offset += dir * belt.speed * Time.deltaTime;
            
            // Debug logging to understand the texture scrolling behavior
            if (Time.frameCount % 60 == 0) // Log every 60 frames to avoid spam
            {
                float rotation = belt.transform.eulerAngles.z;
                Vector2 beltDir = belt.GetDirectionVector();
                Debug.Log($"UITextureScroller Debug - Rotation: {rotation:F1}°, BeltDir: {beltDir}, VisualDir: {dir}, Object: {gameObject.name}");
            }
        }
        else
        {
            offset += Vector2.down * 0.5f * Time.deltaTime; // fallback flips direction
        }

        mat.SetTextureOffset("_BaseMap", offset);
        
        // Also try _MainTex in case the material uses legacy properties
        if (mat.HasProperty("_MainTex"))
        {
            mat.SetTextureOffset("_MainTex", offset);
        }
    }
    
    private Vector2 GetVisualScrollDirection()
    {
        float zRotation = belt.transform.eulerAngles.z;
        // Normalize to 0-360 range (Unity's eulerAngles.z is already in this range)
        zRotation = (zRotation % 360 + 360) % 360;
        
        // Map rotation angles to appropriate texture scroll directions
        // The goal is to make the texture appear to flow in the direction the conveyor is facing
        if (Mathf.Approximately(zRotation, 0)) 
        {
            // Facing Up: scroll texture downward (so material appears to move up)
            return Vector2.down;
        }
        else if (Mathf.Approximately(zRotation, 270))
        {
            // Facing Right: scroll texture leftward (so material appears to move right)
            // Note: -90° becomes 270° in Unity's 0-360 range
            return Vector2.left;
        }
        else if (Mathf.Approximately(zRotation, 180))
        {
            // Facing Down: scroll texture upward (so material appears to move down)
            return Vector2.up;
        }
        else if (Mathf.Approximately(zRotation, 90))
        {
            // Facing Left: scroll texture rightward (so material appears to move left)
            // Note: -270° becomes 90° in Unity's 0-360 range
            return Vector2.right;
        }
        
        // For other angles, calculate appropriate direction
        float rad = zRotation * Mathf.Deg2Rad;
        // Rotate the base direction (Vector2.down for upward-facing) by the rotation amount
        return new Vector2(-Mathf.Sin(rad), -Mathf.Cos(rad)).normalized;
    }
    
    void OnDestroy()
    {
        // Clean up the material instance when the component is destroyed
        if (mat != null)
        {
            DestroyImmediate(mat);
        }
    }
}