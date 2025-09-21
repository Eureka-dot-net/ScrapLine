using UnityEngine;
using static UICell;

/// <summary>
/// Manages machine placement, rotation, and validation.
/// Handles machine selection, placement rules, and machine interactions.
/// </summary>
public class MachineManager : MonoBehaviour
{
    [Header("Machine Configuration")]
    [Tooltip("Enable debug logs for machine operations")]
    public bool enableMachineLogs = true;

    private MachineDef selectedMachine;
    private Direction lastMachineDirection = Direction.Up;
    private CreditsManager creditsManager;
    private GridManager gridManager;
    private UIGridManager activeGridManager;

    /// <summary>
    /// Initialize the machine manager
    /// </summary>
    /// <param name="creditsManager">Reference to the credits manager</param>
    /// <param name="gridManager">Reference to the grid manager</param>
    /// <param name="activeGridManager">Reference to the UI grid manager</param>
    public void Initialize(CreditsManager creditsManager, GridManager gridManager, UIGridManager activeGridManager)
    {
        this.creditsManager = creditsManager;
        this.gridManager = gridManager;
        this.activeGridManager = activeGridManager;
    }

    /// <summary>
    /// Set the currently selected machine for placement
    /// </summary>
    /// <param name="machine">The machine definition to select</param>
    public void SetSelectedMachine(MachineDef machine)
    {
        selectedMachine = machine;
    }

    /// <summary>
    /// Get the currently selected machine
    /// </summary>
    /// <returns>The currently selected machine definition</returns>
    public MachineDef GetSelectedMachine()
    {
        return selectedMachine;
    }

    /// <summary>
    /// Handle cell click interactions for machine placement and rotation
    /// </summary>
    /// <param name="x">X coordinate of the clicked cell</param>
    /// <param name="y">Y coordinate of the clicked cell</param>
    public void OnCellClicked(int x, int y)
    {
        CellData cellData = gridManager.GetCellData(x, y);

        if (cellData == null)
        {
            Debug.LogError("Clicked cell is not in the data model! Coords: " + x + ", " + y);
            return;
        }

        if (enableMachineLogs)
            if (enableMachineLogs)
                Debug.Log($"Cell clicked at ({x}, {y}): cellType={cellData.cellType}, machineDefId={cellData.machineDefId}, selectedMachine={selectedMachine?.id ?? "null"}");

        // If clicking on an existing machine, rotate it
        if (cellData.cellType == CellType.Machine && !string.IsNullOrEmpty(cellData.machineDefId))
        {
            RotateMachine(cellData);
            return;
        }

        // If a machine is selected, try to place it
        if (selectedMachine != null)
        {
            if (IsValidMachinePlacement(cellData, selectedMachine))
            {
                if (creditsManager.CanAfford(selectedMachine.cost))
                {
                    PlaceMachine(cellData, selectedMachine);
                    return;
                }
                else
                {
                    Debug.LogWarning($"Cannot afford {selectedMachine.id} - costs {selectedMachine.cost}, have {creditsManager.GetCredits()} credits");
                    return;
                }
            }
            else
            {
                if (enableMachineLogs)
                    Debug.Log($"Cannot place {selectedMachine.id} here - invalid placement");
                return;
            }
        }

        if (enableMachineLogs)
            Debug.Log("No machine selected for placement");
    }

    // Add these methods to your existing MachineManager class

    /// <summary>
    /// Check if a cell contains a machine that can be dragged
    /// </summary>
    /// <param name="x">X coordinate of the cell</param>
    /// <param name="y">Y coordinate of the cell</param>
    /// <returns>True if the cell contains a draggable machine</returns>
    public bool CanStartDrag(int x, int y)
    {
        CellData cellData = gridManager.GetCellData(x, y);

        if (cellData == null)
        {
            if (enableMachineLogs)
                Debug.Log($"CanStartDrag({x}, {y}): No cell data found");
            return false;
        }

        bool canDrag = cellData.cellType == CellType.Machine && !string.IsNullOrEmpty(cellData.machineDefId);

        if (enableMachineLogs)
            Debug.Log($"CanStartDrag({x}, {y}): cellType={cellData.cellType}, machineDefId={cellData.machineDefId ?? "null"}, result={canDrag}");

        return canDrag;
    }

    /// <summary>
    /// Prepare a machine for dragging (called when drag starts)
    /// </summary>
    /// <param name="x">X coordinate of the machine being dragged</param>
    /// <param name="y">Y coordinate of the machine being dragged</param>
    public void StartMachineDrag(int x, int y)
    {
        if (enableMachineLogs)
            Debug.Log($"Starting machine drag from cell ({x}, {y})");

        // Could add visual feedback here if needed
        // For now, just log the action
    }

    /// <summary>
    /// Check if a machine can be dropped at the target location
    /// </summary>
    /// <param name="fromX">Source X coordinate</param>
    /// <param name="fromY">Source Y coordinate</param>
    /// <param name="toX">Target X coordinate</param>
    /// <param name="toY">Target Y coordinate</param>
    /// <returns>True if the machine can be dropped at the target location</returns>
    public bool CanDropMachine(int fromX, int fromY, int toX, int toY)
    {
        // Can't drop on same cell (this will be handled as rotation)
        if (fromX == toX && fromY == toY)
            return false;

        CellData sourceCellData = gridManager.GetCellData(fromX, fromY);
        CellData targetCellData = gridManager.GetCellData(toX, toY);

        if (sourceCellData == null || targetCellData == null)
        {
            if (enableMachineLogs)
                Debug.Log($"CanDropMachine({fromX}, {fromY}) to ({toX}, {toY}): Invalid cell data");
            return false;
        }

        // Source must have a machine
        if (sourceCellData.cellType != CellType.Machine || string.IsNullOrEmpty(sourceCellData.machineDefId))
        {
            if (enableMachineLogs)
                Debug.Log($"CanDropMachine: Source cell ({fromX}, {fromY}) has no machine to move");
            return false;
        }

        // Target must be empty (blank cell)
        if (targetCellData.cellType != CellType.Blank)
        {
            if (enableMachineLogs)
                Debug.Log($"CanDropMachine: Target cell ({toX}, {toY}) is not empty");
            return false;
        }

        // Check if machine can be placed at target location
        MachineDef machineDef = FactoryRegistry.Instance.GetMachine(sourceCellData.machineDefId);
        if (machineDef == null)
        {
            if (enableMachineLogs)
                Debug.LogError($"CanDropMachine: Cannot find machine definition for {sourceCellData.machineDefId}");
            return false;
        }

        bool canPlace = IsValidMachinePlacement(targetCellData, machineDef);

        if (enableMachineLogs)
            Debug.Log($"CanDropMachine({fromX}, {fromY}) to ({toX}, {toY}): {canPlace}");

        return canPlace;
    }

    /// <summary>
    /// Move a machine from one cell to another
    /// </summary>
    /// <param name="fromX">Source X coordinate</param>
    /// <param name="fromY">Source Y coordinate</param>
    /// <param name="toX">Target X coordinate</param>
    /// <param name="toY">Target Y coordinate</param>
    public void MoveMachine(int fromX, int fromY, int toX, int toY)
    {
        if (!CanDropMachine(fromX, fromY, toX, toY))
        {
            if (enableMachineLogs)
                Debug.LogWarning($"Cannot move machine from ({fromX}, {fromY}) to ({toX}, {toY}) - invalid move");
            return;
        }

        CellData sourceCellData = gridManager.GetCellData(fromX, fromY);
        CellData targetCellData = gridManager.GetCellData(toX, toY);

        if (enableMachineLogs)
            Debug.Log($"Moving machine {sourceCellData.machineDefId} from ({fromX}, {fromY}) to ({toX}, {toY})");

        // Copy machine data to target cell
        targetCellData.cellType = sourceCellData.cellType;
        targetCellData.machineDefId = sourceCellData.machineDefId;
        targetCellData.direction = sourceCellData.direction;
        targetCellData.machine = sourceCellData.machine;

        // Update machine's position reference if it exists
        // if (targetCellData.machine != null)
        // {
        //     // Assuming your machine objects have position references that need updating
        //     targetCellData.machine.UpdatePosition(toX, toY);
        // }

        // Clear source cell
        sourceCellData.cellType = CellType.Blank;
        sourceCellData.machineDefId = null;
        sourceCellData.direction = Direction.Up;
        sourceCellData.machine = null;

        // Update visuals for both cells
        if (activeGridManager != null)
        {
            // Update target cell with new machine
            activeGridManager.UpdateCellVisuals(toX, toY, targetCellData.cellType, targetCellData.direction, targetCellData.machineDefId);

            // Update source cell to blank
            activeGridManager.UpdateCellVisuals(fromX, fromY, sourceCellData.cellType, sourceCellData.direction, null);
        }

        if (enableMachineLogs)
            Debug.Log($"Successfully moved machine from ({fromX}, {fromY}) to ({toX}, {toY})");
    }

    /// <summary>
    /// Delete a machine (called when dragged outside grid)
    /// </summary>
    /// <param name="x">X coordinate of the machine to delete</param>
    /// <param name="y">Y coordinate of the machine to delete</param>
    public void DeleteMachine(int x, int y)
    {
        CellData cellData = gridManager.GetCellData(x, y);

        if (cellData == null || cellData.cellType != CellType.Machine)
        {
            if (enableMachineLogs)
                Debug.LogWarning($"DeleteMachine({x}, {y}): No machine to delete");
            return;
        }

        if (enableMachineLogs)
            Debug.Log($"Deleting machine {cellData.machineDefId} from cell ({x}, {y})");

        // Clean up machine object if it exists
        if (cellData.machine != null)
        {
            // Assuming your machine objects need cleanup
           // cellData.machine.Destroy();
            cellData.machine = null;
        }

        // Reset cell to blank
        cellData.cellType = CellType.Blank;
        cellData.machineDefId = null;
        cellData.direction = Direction.Up;

        // Update visuals
        if (activeGridManager != null)
        {
            activeGridManager.UpdateCellVisuals(x, y, cellData.cellType, cellData.direction, null);
        }

        // Note: We don't refund credits for deleted machines
        // This could be added as a feature if desired

        if (enableMachineLogs)
            Debug.Log($"Successfully deleted machine from cell ({x}, {y})");
    }

    /// <summary>
    /// Check if a machine can be placed at the specified cell
    /// </summary>
    /// <param name="cellData">The cell data to check</param>
    /// <param name="machineDef">The machine definition to validate</param>
    /// <returns>True if placement is valid, false otherwise</returns>
    private bool IsValidMachinePlacement(CellData cellData, MachineDef machineDef)
    {
        foreach (string placement in machineDef.gridPlacement)
        {
            switch (placement.ToLower())
            {
                case "any": return true;
                case "grid": return cellData.cellRole == CellRole.Grid;
                case "top": return cellData.cellRole == CellRole.Top;
                case "bottom": return cellData.cellRole == CellRole.Bottom;
            }
        }
        return false;
    }

    /// <summary>
    /// Place a machine at the specified cell
    /// </summary>
    /// <param name="cellData">The cell data where to place the machine</param>
    /// <param name="machineDef">The machine definition to place</param>
    private void PlaceMachine(CellData cellData, MachineDef machineDef)
    {
        if (enableMachineLogs)
            Debug.Log($"Placing machine {machineDef.id} at ({cellData.x}, {cellData.y})");

        if (!creditsManager.TrySpendCredits(machineDef.cost))
        {
            Debug.LogError($"Failed to place machine {machineDef.id} - insufficient credits!");
            return;
        }

        cellData.cellType = CellType.Machine;
        cellData.machineDefId = machineDef.id;

        if (machineDef.canRotate)
        {
            cellData.direction = lastMachineDirection;
        }
        else
        {
            cellData.direction = Direction.Up;
        }

        cellData.machine = MachineFactory.CreateMachine(cellData);
        if (cellData.machine == null)
        {
            Debug.LogError($"Failed to create machine object for {machineDef.id}");
        }

        if (activeGridManager != null)
        {
            activeGridManager.UpdateCellVisuals(cellData.x, cellData.y, cellData.cellType, cellData.direction, cellData.machineDefId);
        }
    }

    /// <summary>
    /// Rotate a machine at the specified cell
    /// </summary>
    /// <param name="cellData">The cell data containing the machine to rotate</param>
    private void RotateMachine(CellData cellData)
    {
        MachineDef machineDef = FactoryRegistry.Instance.GetMachine(cellData.machineDefId);
        if (machineDef == null)
        {
            Debug.LogError($"Cannot find machine definition for {cellData.machineDefId}");
            return;
        }

        if (!machineDef.canRotate)
        {
            if (enableMachineLogs)
                Debug.Log($"Machine {machineDef.id} cannot be rotated (canRotate = false)");
            return;
        }

        cellData.direction = (Direction)(((int)cellData.direction + 1) % 4);
        lastMachineDirection = cellData.direction;

        if (activeGridManager != null)
        {
            activeGridManager.UpdateCellVisuals(cellData.x, cellData.y, cellData.cellType, cellData.direction, cellData.machineDefId);
        }
    }

    /// <summary>
    /// Get the last machine direction used for placement
    /// </summary>
    /// <returns>The last machine direction</returns>
    public Direction GetLastMachineDirection()
    {
        return lastMachineDirection;
    }

    /// <summary>
    /// Set the last machine direction (used for maintaining direction across placements)
    /// </summary>
    /// <param name="direction">The direction to set</param>
    public void SetLastMachineDirection(Direction direction)
    {
        lastMachineDirection = direction;
    }
}