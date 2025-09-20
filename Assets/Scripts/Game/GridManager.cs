using UnityEngine;
using System.Collections.Generic;
using static UICell;

/// <summary>
/// Manages grid operations and cell data access.
/// Handles grid creation, clearing, and cell data retrieval.
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("Grid Configuration")]
    [Tooltip("Default grid width for new games")]
    public int defaultGridWidth = 5;
    
    [Tooltip("Default grid height for new games")]
    public int defaultGridHeight = 7;
    
    [Header("Debug")]
    [Tooltip("Enable debug logs for grid operations")]
    public bool enableGridLogs = true;

    private List<GridData> activeGrids = new List<GridData>();
    private UIGridManager activeGridManager;

    /// <summary>
    /// Initialize the grid manager
    /// </summary>
    /// <param name="gridManager">Reference to the UI grid manager</param>
    public void Initialize(UIGridManager gridManager)
    {
        this.activeGridManager = gridManager;
        
        if (enableGridLogs)
            Debug.Log("GridManager initialized.");
    }

    /// <summary>
    /// Create a new default grid for a new game
    /// </summary>
    /// <returns>The created grid data</returns>
    public GridData CreateDefaultGrid()
    {
        if (enableGridLogs)
            Debug.Log("Creating default grid...");

        GridData defaultGrid = new GridData();
        defaultGrid.width = defaultGridWidth;
        defaultGrid.height = defaultGridHeight;

        for (int y = 0; y < defaultGrid.height; y++)
        {
            for (int x = 0; x < defaultGrid.width; x++)
            {
                CellRole role = CellRole.Grid;
                if (y == 0)
                    role = CellRole.Top;
                else if (y == defaultGrid.height - 1)
                    role = CellRole.Bottom;

                var cell = new CellData
                {
                    x = x,
                    y = y,
                    cellType = CellType.Blank,
                    direction = Direction.Up,
                    cellRole = role,
                };
                defaultGrid.cells.Add(cell);
                cell.machine = MachineFactory.CreateMachine(cell);
            }
        }

        activeGrids.Add(defaultGrid);
        
        if (enableGridLogs)
            Debug.Log($"Created default grid: {defaultGrid.width}x{defaultGrid.height}");

        return defaultGrid;
    }

    /// <summary>
    /// Set the active grids (used when loading saves)
    /// </summary>
    /// <param name="grids">The grids to set as active</param>
    public void SetActiveGrids(List<GridData> grids)
    {
        activeGrids = grids;
        
        if (enableGridLogs)
            Debug.Log($"Loaded {grids.Count} grids from save file");
    }

    /// <summary>
    /// Get the current grid. Currently returns the first grid but can be extended
    /// for multiple grid support in the future.
    /// </summary>
    /// <returns>The current grid data</returns>
    public GridData GetCurrentGrid()
    {
        return activeGrids.Count > 0 ? activeGrids[0] : null;
    }

    /// <summary>
    /// Get all active grids
    /// </summary>
    /// <returns>List of all active grids</returns>
    public List<GridData> GetActiveGrids()
    {
        return activeGrids;
    }

    /// <summary>
    /// Get cell data at specific coordinates
    /// </summary>
    /// <param name="grid">The grid to search in</param>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <returns>Cell data if found, null otherwise</returns>
    public CellData GetCellData(GridData grid, int x, int y)
    {
        foreach (var cell in grid.cells)
        {
            if (cell.x == x && cell.y == y)
            {
                return cell;
            }
        }
        return null;
    }

    /// <summary>
    /// Get cell data at specific coordinates in the current grid
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <returns>Cell data if found, null otherwise</returns>
    public CellData GetCellData(int x, int y)
    {
        GridData currentGrid = GetCurrentGrid();
        if (currentGrid == null)
        {
            Debug.LogError("No current grid available!");
            return null;
        }
        
        return GetCellData(currentGrid, x, y);
    }

    /// <summary>
    /// Clear the current grid
    /// </summary>
    public void ClearGrid()
    {
        if (enableGridLogs)
            Debug.Log("Clearing grid...");
            
        GridData gridData = GetCurrentGrid();
        if (gridData == null)
        {
            Debug.LogError("No current grid to clear!");
            return;
        }

        if (activeGridManager == null)
        {
            Debug.LogError("No active grid manager available!");
            return;
        }

        foreach (var cell in gridData.cells)
        {
            // Destroy visual items
            foreach (var item in cell.items)
            {
                activeGridManager.DestroyVisualItem(item.id);
            }

            // Reset cell state
            cell.cellType = CellType.Blank;
            cell.direction = Direction.Up;
            cell.machineDefId = null;
            cell.items.Clear();
            cell.waitingItems.Clear();
        }

        activeGridManager.UpdateAllVisuals();
        
        if (enableGridLogs)
            Debug.Log("Grid cleared successfully.");
    }

    /// <summary>
    /// Initialize the UI grid with the current grid data
    /// </summary>
    public void InitializeUIGrid()
    {
        if (activeGridManager != null)
        {
            GridData currentGrid = GetCurrentGrid();
            if (currentGrid != null)
            {
                activeGridManager.InitGrid(currentGrid);
                if (enableGridLogs)
                    Debug.Log("UI grid initialized with current grid data.");
            }
            else
            {
                Debug.LogError("No current grid available for UI initialization!");
            }
        }
        else
        {
            Debug.LogError("No active grid manager available for UI initialization!");
        }
    }
}