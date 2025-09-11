using UnityEngine;

public class InputSquare : MonoBehaviour
{
    [HideInInspector] public GameObject canPrefab;
    [HideInInspector] public float spawnInterval = 2f;
    [HideInInspector] public float cellSize = 1f;

    private float timer = 0f;

    void Update()
    {
        if (canPrefab == null) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;

            // Find the center of the InputSquare (works for non-centered pivots)
            Vector3 centerPos = transform.position;
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                centerPos = sr.bounds.center;
                sr.sortingOrder = 15; // Ensure InputSquare is rendered above conveyor rail
            }

            // Spawn the can just above the center of the input square
            Vector3 spawnPos = centerPos + new Vector3(0, cellSize, 0);
            
            GameObject can = Instantiate(canPrefab, spawnPos, Quaternion.identity);
    
            // Set the sortingOrder for the can's SpriteRenderer
            SpriteRenderer canSR = can.GetComponent<SpriteRenderer>();
            if (canSR != null)
            {
                canSR.sortingOrder = 20; // Make sure this is above conveyor and input square
            }
        }
    }
}