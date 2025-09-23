// using System;
// using UnityEngine;

// public class ScrapPiece : MonoBehaviour
// {
//     public float conveyorSpeed = 2.0f;
//     private bool onConveyor = false;
//     private Vector2 conveyorDirection; // Default direction

//     void Update()
//     {
//         if (onConveyor)
//         {
//             // Move the scrap piece along the conveyor
//             transform.position += (Vector3)conveyorDirection * conveyorSpeed * Time.deltaTime;
//         }
//     }

//     // Detect staying on a conveyor belt
//     void OnTriggerStay2D(Collider2D other)
//     {
//         if (other.CompareTag("Conveyor"))
//         {
//             onConveyor = true;
//             // Find the conveyor center
//             Vector3 conveyorCenter = other.bounds.center;
//             float distance = Vector3.Distance(transform.position, conveyorCenter);
//             float snapThreshold = 0.1f; // Adjust based on your grid size (e.g., cellSize * 0.2f)

//             if (distance < snapThreshold || conveyorDirection == null)
//             {
                

//                 ConveyorBelt belt = other.GetComponent<ConveyorBelt>();
//                 if (belt == null)
//                 {
//                     belt = other.GetComponentInParent<ConveyorBelt>();
//                 }
//                 if (belt != null)
//                 {
//                     Vector2 direction = belt.GetDirectionVector();
//                     conveyorSpeed = belt.speed;
//                     conveyorDirection = direction;
//                     // You can also snap to center here for perfect alignment:
//                     // transform.position = conveyorCenter;
//                 }
//                 else
//                 {
//                     GameLogger.LogWarning(LoggingManager.LogCategory.Debug, "ScrapPiece: ConveyorBelt component not found on '{other.gameObject.name}'.", ComponentId);
//                 }
//             }
//             // else: do not update direction or onConveyor yet
//         }
//     }

//     // Detect leaving the conveyor belt
//     void OnTriggerExit2D(Collider2D other)
//     {
//         if (other.CompareTag("Conveyor"))
//         {
//             onConveyor = false;
//         }
//     }
// }