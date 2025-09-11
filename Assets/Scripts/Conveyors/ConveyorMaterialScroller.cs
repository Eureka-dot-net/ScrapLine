using UnityEngine;

public class ConveyorMaterialScroller : MonoBehaviour
{
    public Material conveyorMaterial; // Assign your shared mat in Inspector

    public float scrollSpeed = 0.5f;

    void Update()
    {
        Vector2 offset = conveyorMaterial.GetTextureOffset("_BaseMap");
        offset += Vector2.down * scrollSpeed * Time.deltaTime;
        conveyorMaterial.SetTextureOffset("_BaseMap", offset);
    }
}