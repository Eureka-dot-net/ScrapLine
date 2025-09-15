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
            // Use direction directly (no flip needed since GetDirectionVector returns UV offset direction)
            Vector2 dir = belt.GetDirectionVector();
            offset += dir * belt.speed * Time.deltaTime;
            
            // Debug logging to understand the issue
            if (Time.frameCount % 60 == 0) // Log once per second
            {
                Debug.Log($"UITextureScroller: {gameObject.name} - Rotation: {belt.transform.eulerAngles.z}, UV Direction: {dir}");
            }
        }
        else
        {
            offset += Vector2.down * 0.5f * Time.deltaTime; // fallback direction for up-facing
        }

        mat.SetTextureOffset("_BaseMap", offset);
        
        // Also try _MainTex in case the material uses legacy properties
        if (mat.HasProperty("_MainTex"))
        {
            mat.SetTextureOffset("_MainTex", offset);
        }
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