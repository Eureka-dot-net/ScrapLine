using UnityEngine;

public class ConveyorBelt : MonoBehaviour
{
    //public Direction direction = Direction.Up;
    public float speed = 0.5f;

    // Returns normalized vector for movement based on rotation
    public Vector2 GetDirectionVector()
    {
        float zRotation = transform.eulerAngles.z;
        zRotation = (zRotation % 360 + 360) % 360;

        if (Mathf.Approximately(zRotation, 0)) return Vector2.up;
        if (Mathf.Approximately(zRotation, 90)) return Vector2.left; //invert
        if (Mathf.Approximately(zRotation, 180)) return Vector2.down;
        if (Mathf.Approximately(zRotation, 270)) return Vector2.right;

        // For other angles, calculate direction vector
        float rad = zRotation * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)).normalized;
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