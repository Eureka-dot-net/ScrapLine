using UnityEngine;

public class GridManager : MonoBehaviour
{
    public int width = 5;
    public int height = 5;
    public GameObject cellPrefab;
    public Material conveyorSharedMaterial; // Shared material for conveyors

    public GameObject inputSquarePrefab; // Assign in Inspector
    public GameObject canPrefab;         // Assign in Inspector

    public float canSpawnInterval = 2f;  // Seconds between spawns

    public float lastConveyorDirection = 0f; // 0=Up, 90=Right, 180=Down, 270=Left

    void Start()
    {
        SpriteRenderer sr = cellPrefab.GetComponent<SpriteRenderer>();
        float cellSize = Mathf.Max(sr.bounds.size.x, sr.bounds.size.y);

        float gridWidth = width * cellSize;
        float gridHeight = height * cellSize;

        Vector3 startPos = new Vector3(-gridWidth / 2 + cellSize / 2, -gridHeight / 2 + cellSize / 2, 0);

        // Create grid cells
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = startPos + new Vector3(x * cellSize, y * cellSize, 0);
                GameObject cellObj = Instantiate(cellPrefab, pos, Quaternion.identity, transform);

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

        // Assign the can prefab and spawn interval to the input square
        InputSquare inputSquare = inputSquareObj.GetComponent<InputSquare>();
        if (inputSquare != null)
        {
            inputSquare.canPrefab = canPrefab;
            inputSquare.spawnInterval = canSpawnInterval;
            inputSquare.cellSize = cellSize;
        }
    }

    void Update()
    {
        // Handle global input, e.g. conveyor clicks
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