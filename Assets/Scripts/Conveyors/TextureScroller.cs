// using UnityEngine;

// public class TextureScroller : MonoBehaviour
// {
//     private Material mat;
//     private ConveyorBelt belt; // Reference to parent’s ConveyorBelt

//     void Start()
//     {
//         mat = GetComponent<Renderer>().sharedMaterial;
//         belt = GetComponentInParent<ConveyorBelt>();
//         if (belt == null)
//         {
//             Debug.LogWarning("No ConveyorBelt component found in parent. Using default direction (Up) and speed (0.5).");
//         }
//     }

//     void Update()
//     {
//         Vector2 offset = mat.GetTextureOffset("_BaseMap"); // For URP Unlit shader

//         if (belt != null)
//         {
//             // Flip the direction for visual effect!
//             Vector2 dir = -belt.GetDirectionVector();
//             offset += dir * belt.speed * Time.deltaTime;
//         }
//         else
//         {
//             offset += Vector2.down * 0.5f * Time.deltaTime; // fallback flips direction
//         }

//         mat.SetTextureOffset("_BaseMap", offset);
//     }
// }