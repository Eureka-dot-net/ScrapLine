using UnityEngine;

public class InputSquare : MonoBehaviour
{
    [HideInInspector] public GameObject[] spawnPrefabs;   // Shows up in Inspector as "Spawn Prefabs"
    [HideInInspector] public float spawnInterval = 2f;
    [HideInInspector] public float cellSize = 1f;

    private float timer = 0f;
    private int spawnIndex = 0;

    void Update()
    {
        if (spawnPrefabs == null || spawnPrefabs.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;

            Vector3 centerPos = transform.position;
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                centerPos = sr.bounds.center;
                sr.sortingOrder = 15;
            }

            Vector3 spawnPos = centerPos + new Vector3(0, cellSize, 0);

            GameObject prefabToSpawn = spawnPrefabs[spawnIndex];
            GameObject spawnedObj = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

            SpriteRenderer spawnedSR = spawnedObj.GetComponent<SpriteRenderer>();
            if (spawnedSR != null)
            {
                spawnedSR.sortingOrder = 20;
            }

            spawnIndex = (spawnIndex + 1) % spawnPrefabs.Length;
        }
    }
}