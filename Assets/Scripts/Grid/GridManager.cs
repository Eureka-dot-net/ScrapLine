using UnityEngine;

public class GridManager : MonoBehaviour
{
    public int width = 5;
    public int height = 5;
    public GameObject cellPrefab;
    public Material conveyorSharedMaterial;
    public GameObject inputSquarePrefab;
    public GameObject[] allPrefabs;
    public float canSpawnInterval = 2f;
    public float lastConveyorDirection = 0f; // 0=Up, 90=Right, 180=Down, 270=Left

    void Start()
    {
        // Get camera size in world units
        float camHeight = Camera.main.orthographicSize * 2f;
        float camWidth = camHeight * Camera.main.aspect;
        float margin = 0.9f; // Use 90% of screen for grid

        float usableWidth = camWidth * margin;
        float usableHeight = camHeight * margin;

        // Find maximum cell size that fits the grid in the camera view
        float cellSizeX = usableWidth / width;
        float cellSizeY = usableHeight / height;
        float cellSize = Mathf.Min(cellSizeX, cellSizeY);

        // Center grid in camera
        Vector3 startPos = new Vector3(
            -((width - 1) * cellSize) / 2f,
            -((height - 1) * cellSize) / 2f,
            0f);

        // Create grid cells
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = startPos + new Vector3(x * cellSize, y * cellSize, 0);
                GameObject cellObj = Instantiate(cellPrefab, pos, Quaternion.identity, transform);

                // Scale the cell so sprite fits cellSize
                SpriteRenderer sr = cellObj.GetComponent<SpriteRenderer>();
                float originalSpriteSize = Mathf.Max(sr.bounds.size.x, sr.bounds.size.y);
                float scale = cellSize / originalSpriteSize;
                cellObj.transform.localScale = new Vector3(scale, scale, 1);

                GridCell cell = cellObj.GetComponent<GridCell>();
                if (cell != null)
                {
                    cell.conveyorSharedMaterial = conveyorSharedMaterial;
                }
            }
        }

        // --- Spawn the input square below the grid, centered horizontally ---
        Vector3 inputSquarePos = new Vector3(0, startPos.y - cellSize, 0); // One cell below grid
        GameObject inputSquareObj = Instantiate(inputSquarePrefab, inputSquarePos, Quaternion.identity, transform);

        // Assign can prefab and spawn interval to input square
        InputSquare inputSquare = inputSquareObj.GetComponent<InputSquare>();
        if (inputSquare != null)
        {
            inputSquare.spawnPrefabs = allPrefabs;
            inputSquare.spawnInterval = canSpawnInterval;
            inputSquare.cellSize = cellSize;
        }
    }

    void Update()
    {
        HandleClickInput();
    }

    void HandleClickInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D[] hits = Physics2D.RaycastAll(mouseWorldPos, Vector2.zero);

            foreach (var hit in hits)
            {
                if (hit.collider != null)
                {
                    if (hit.collider.CompareTag("Conveyor"))
                    {
                        ConveyorBelt belt = hit.collider.GetComponent<ConveyorBelt>();
                        if (belt != null)
                        {
                            lastConveyorDirection = belt.RotateConveyor();
                            break;
                        }
                    }
                    else
                    {
                        GridCell cell = hit.collider.GetComponent<GridCell>();
                        if (cell != null)
                        {
                            cell.DrawConveyor(lastConveyorDirection);
                            break;
                        }
                    }
                }
            }
        }
    }
}