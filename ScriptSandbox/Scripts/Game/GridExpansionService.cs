using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Pure logic service for grid expansion operations.
/// Handles cost calculation and data model manipulation for inserting rows/columns.
/// Does NOT handle rendering or UI - that's the responsibility of GridExpandAnimator and GridMarkersView.
/// </summary>
public class GridExpansionService : MonoBehaviour
{
    [Header("Cost Configuration")]
    [Tooltip("Base cost for any expansion operation")]
    public int baseCost = 100;

    [Tooltip("Cost growth factor based on current grid area")]
    public float growthFactor = 2f;

    [Header("Debug")]
    [Tooltip("Enable debug logs for expansion operations")]
    public bool enableExpansionLogs = true;

    /// <summary>
    /// Types of expansion operations
    /// </summary>
    public enum ExpansionType
    {
        InsertRow,        // Insert row at any internal index
        InsertColumn,     // Insert column at any internal index
        InsertColumnLeft, // Insert column at left edge
        InsertColumnRight // Insert column at right edge
    }

    /// <summary>
    /// Edge positions for column expansion
    /// </summary>
    public enum Edge
    {
        Left,
        Right
    }

    private string ComponentId => $"GridExpansionService_{GetInstanceID()}";

    /// <summary>
    /// Compute the cost for an expansion operation
    /// </summary>
    /// <param name="rows">Current number of rows</param>
    /// <param name="cols">Current number of columns</param>
    /// <param name="type">Type of expansion</param>
    /// <returns>Cost in credits</returns>
    public int ComputeExpansionCost(int rows, int cols, ExpansionType type)
    {
        // Cost grows with grid area
        int currentArea = rows * cols;
        int cost = baseCost + Mathf.RoundToInt(currentArea * growthFactor);

        if (enableExpansionLogs)
            GameLogger.LogGrid($"Expansion cost calculated: {cost} (area={currentArea}, base={baseCost}, factor={growthFactor})", ComponentId);

        return cost;
    }

    /// <summary>
    /// Insert a new row at the specified index
    /// </summary>
    /// <param name="gridData">Grid data to modify</param>
    /// <param name="rowIndex">Index where the row should be inserted (0 = top, height = bottom)</param>
    public void InsertRow(GridData gridData, int rowIndex)
    {
        if (gridData == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.Grid, "Cannot insert row - gridData is null", ComponentId);
            return;
        }

        // Validate row index (must be internal, not at edges)
        if (rowIndex <= 0 || rowIndex >= gridData.height)
        {
            GameLogger.LogError(LoggingManager.LogCategory.Grid, 
                $"Cannot insert row at index {rowIndex} - must be between 1 and {gridData.height - 1} (internal rows only)", 
                ComponentId);
            return;
        }

        if (enableExpansionLogs)
            GameLogger.LogGrid($"Inserting row at index {rowIndex} in {gridData.width}x{gridData.height} grid", ComponentId);

        // Step 1: Shift all cells at rowIndex and below down by 1
        foreach (var cell in gridData.cells)
        {
            if (cell.y >= rowIndex)
            {
                cell.y++;
            }
        }

        // Step 2: Create new cells for the inserted row
        for (int x = 0; x < gridData.width; x++)
        {
            var newCell = new CellData
            {
                x = x,
                y = rowIndex,
                cellType = UICell.CellType.Blank,
                direction = UICell.Direction.Up,
                cellRole = UICell.CellRole.Grid,
                machineDefId = "blank"
            };
            newCell.machine = MachineFactory.CreateMachine(newCell);
            gridData.cells.Add(newCell);
        }

        // Step 3: Update grid height
        gridData.height++;

        if (enableExpansionLogs)
            GameLogger.LogGrid($"Row inserted successfully. New grid size: {gridData.width}x{gridData.height}", ComponentId);
    }

    /// <summary>
    /// Insert a new column at the specified index
    /// </summary>
    /// <param name="gridData">Grid data to modify</param>
    /// <param name="colIndex">Index where the column should be inserted (0 = left edge, width = right edge)</param>
    public void InsertColumn(GridData gridData, int colIndex)
    {
        if (gridData == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.Grid, "Cannot insert column - gridData is null", ComponentId);
            return;
        }

        // Validate column index (0 to width inclusive for columns)
        if (colIndex < 0 || colIndex > gridData.width)
        {
            GameLogger.LogError(LoggingManager.LogCategory.Grid, 
                $"Cannot insert column at index {colIndex} - must be between 0 and {gridData.width}", 
                ComponentId);
            return;
        }

        if (enableExpansionLogs)
            GameLogger.LogGrid($"Inserting column at index {colIndex} in {gridData.width}x{gridData.height} grid", ComponentId);

        // Step 1: Shift all cells at colIndex and right by 1
        foreach (var cell in gridData.cells)
        {
            if (cell.x >= colIndex)
            {
                cell.x++;
            }
        }

        // Step 2: Create new cells for the inserted column
        for (int y = 0; y < gridData.height; y++)
        {
            // Determine cell role based on row position
            UICell.CellRole role = UICell.CellRole.Grid;
            string machineDefId = "blank";

            if (y == 0)
            {
                role = UICell.CellRole.Top;
                machineDefId = "blank_top";
            }
            else if (y == gridData.height - 1)
            {
                role = UICell.CellRole.Bottom;
                machineDefId = "blank_bottom";
            }

            var newCell = new CellData
            {
                x = colIndex,
                y = y,
                cellType = UICell.CellType.Blank,
                direction = UICell.Direction.Up,
                cellRole = role,
                machineDefId = machineDefId
            };
            newCell.machine = MachineFactory.CreateMachine(newCell);
            gridData.cells.Add(newCell);
        }

        // Step 3: Update grid width
        gridData.width++;

        if (enableExpansionLogs)
            GameLogger.LogGrid($"Column inserted successfully. New grid size: {gridData.width}x{gridData.height}", ComponentId);
    }

    /// <summary>
    /// Insert a column at the left or right edge
    /// </summary>
    /// <param name="gridData">Grid data to modify</param>
    /// <param name="edge">Which edge to insert at</param>
    public void InsertColumnAtEdge(GridData gridData, Edge edge)
    {
        if (gridData == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.Grid, "Cannot insert column at edge - gridData is null", ComponentId);
            return;
        }

        int colIndex = edge == Edge.Left ? 0 : gridData.width;

        if (enableExpansionLogs)
            GameLogger.LogGrid($"Inserting column at {edge} edge (index {colIndex})", ComponentId);

        InsertColumn(gridData, colIndex);
    }

    /// <summary>
    /// Get the indices where rows can be inserted (internal positions only)
    /// </summary>
    /// <param name="gridData">Grid data</param>
    /// <returns>List of valid row insertion indices</returns>
    public List<int> GetValidRowInsertionIndices(GridData gridData)
    {
        List<int> indices = new List<int>();
        if (gridData == null) return indices;

        // Rows can only be inserted internally (not at top or bottom edges)
        for (int i = 1; i < gridData.height; i++)
        {
            indices.Add(i);
        }

        return indices;
    }

    /// <summary>
    /// Get the indices where columns can be inserted (including edges)
    /// </summary>
    /// <param name="gridData">Grid data</param>
    /// <returns>List of valid column insertion indices</returns>
    public List<int> GetValidColumnInsertionIndices(GridData gridData)
    {
        List<int> indices = new List<int>();
        if (gridData == null) return indices;

        // Columns can be inserted anywhere, including edges
        for (int i = 0; i <= gridData.width; i++)
        {
            indices.Add(i);
        }

        return indices;
    }
}

/*
 * UNITY WIRING INSTRUCTIONS:
 * 
 * 1. Create an empty GameObject named "ExpandModeSystem" in the scene hierarchy (root level)
 * 2. Add this GridExpansionService component to it
 * 3. Configure in Inspector:
 *    - Base Cost: 100 (adjustable)
 *    - Growth Factor: 2.0 (adjustable)
 *    - Enable Expansion Logs: true (for debugging)
 * 4. This component provides pure logic - no UI references needed here
 * 5. Other components (GridMarkersView, ExpandModeController) will reference this service
 */
