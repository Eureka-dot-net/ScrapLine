using UnityEngine;

public class GridCell : MonoBehaviour
{
    public GameObject conveyorPrefab;
    public Material conveyorSharedMaterial; // Shared for all directions

    public void DrawConveyor(float conveyorRotation)
    {
        if (transform.childCount == 0)
        {
             
            GameObject conveyor = Instantiate(conveyorPrefab, transform.position, Quaternion.identity, transform);
            conveyor.tag = "Conveyor"; // Set tag to "Conveyor"
            conveyor.transform.localEulerAngles = new Vector3(0, 0, conveyorRotation);
            Transform insideTransform = conveyor.transform.Find("Conveyor_Inside");
            if (insideTransform != null)
            {
                SpriteRenderer sr = insideTransform.GetComponent<SpriteRenderer>();
                if (sr != null && conveyorSharedMaterial != null)
                {
                    sr.sharedMaterial = conveyorSharedMaterial; // Always use shared material!
                }
                // Always set rotation on the child (visual)
                //insideTransform.localEulerAngles = new Vector3(0, 0, conveyorRotation);
            }
        }
    }
}