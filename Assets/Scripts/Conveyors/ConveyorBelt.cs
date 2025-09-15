using UnityEngine;

public class ConveyorBelt : MonoBehaviour
{
    //public Direction direction = Direction.Up;
    public float speed = 0.5f;

    // Returns UV offset direction for texture scrolling (inverse of visual direction)
    public Vector2 GetDirectionVector()
    {
        float zRotation = transform.eulerAngles.z;
        zRotation = (zRotation % 360 + 360) % 360;

        // UV offset directions to make texture appear to move in the visual direction
        if (Mathf.Approximately(zRotation, 0)) return Vector2.down;    // Up-facing: scroll UV down to appear moving up
        if (Mathf.Approximately(zRotation, 90)) return Vector2.left;   // Right-facing: scroll UV left to appear moving right
        if (Mathf.Approximately(zRotation, 180)) return Vector2.up;    // Down-facing: scroll UV up to appear moving down
        if (Mathf.Approximately(zRotation, 270)) return Vector2.right; // Left-facing: scroll UV right to appear moving left

        // For other angles, calculate direction vector (inverse for UV scrolling)
        float rad = zRotation * Mathf.Deg2Rad;
        Vector2 visualDirection = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)).normalized;
        return -visualDirection; // Inverse for UV scrolling
    }

    // Call this from GameManager
    public float RotateConveyor()
    {
        // Rotate 90 degrees clockwise
        float newRotation = (transform.eulerAngles.z - 90f) % 360f;
        transform.eulerAngles = new Vector3(0, 0, newRotation);
        return newRotation;
    }
}