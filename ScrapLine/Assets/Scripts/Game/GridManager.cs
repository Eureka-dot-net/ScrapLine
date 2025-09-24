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
    /// Get the component ID for logging purposes
    /// </summary>
    private string ComponentId => $"GridManager_{GetInstanceID()}";

    /// <summary>
    /// Initialize the grid manager
    /// </summary>
    /// <param name="gridManager">Reference to the UI grid manager</param>
    public void Initialize(UIGridManager gridManager)
    {
        this.activeGridManager = gridManager;
        GameLogger.LogGrid("GridManager initialized", ComponentId);
    }

    /// <summary>
    /// Create a new default grid for a new game
    /// </summary>
    /// <returns>The created grid data</returns>
    public GridData CreateDefaultGrid()
    {
        GameLogger.LogGrid($"Creating default grid {defaultGridWidth}x{defaultGridHeight}", ComponentId);
        
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

                // Set appropriate machineDefId for blank cells based on role
                string machineDefId;
                switch (role)
                {
                    case CellRole.Top:
                        machineDefId = "blank_top";
                        break;
                    case CellRole.Bottom:
                        machineDefId = "blank_bottom";
                        break;
                    default:
                        machineDefId = "blank";
                        break;
                }

                var cell = new CellData
                {
                    x = x,
                    y = y,
                    cellType = CellType.Blank,
                    direction = Direction.Up,
                    cellRole = role,
                    machineDefId = machineDefId
                };
                defaultGrid.cells.Add(cell);
                cell.machine = MachineFactory.CreateMachine(cell);
            }
        }

        activeGrids.Add(defaultGrid);
        GameLogger.LogGrid($"Default grid created with {defaultGrid.cells.Count} cells", ComponentId);
        return defaultGrid;
    }

    /// <summary>
    /// Set the active grids (used when loading saves)
    /// </summary>
    /// <param name="grids">The grids to set as active</param>
    public void SetActiveGrids(List<GridData> grids)
    {
        activeGrids = grids;
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
            GameLogger.LogError(LoggingManager.LogCategory.Grid, "No current grid available!", ComponentId);
            return null;
        }
        
        return GetCellData(currentGrid, x, y);
    }

    /// <summary>
    /// Clear the current grid
    /// </summary>
    public void ClearGrid()
    {
        GameLogger.LogGrid("Starting grid clear operation", ComponentId);
        
        GridData gridData = GetCurrentGrid();
        if (gridData == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.Grid, "No current grid to clear!", ComponentId);
            return;
        }

        if (activeGridManager == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.Grid, "No active grid manager available!", ComponentId);
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
        GameLogger.LogGrid($"Grid cleared - reset {gridData.cells.Count} cells", ComponentId);
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
                GameLogger.LogGrid("Grid UI initialized successfully", ComponentId);
            }
            else
            {
                GameLogger.LogError(LoggingManager.LogCategory.Grid, $"No current grid available for UI initialization!", ComponentId);
            }
        }
        else
        {
            GameLogger.LogError(LoggingManager.LogCategory.Grid, $"No active grid manager available for UI initialization!", ComponentId);
        }
    }
}