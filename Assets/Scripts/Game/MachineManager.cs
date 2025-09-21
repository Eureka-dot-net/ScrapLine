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

    /// <summary>
    /// Check if a machine can be placed at the specified cell
    /// </summary>
    /// <param name="cellData">The cell data to check</param>
    /// <param name="machineDef">The machine definition to validate</param>
    /// <returns>True if placement is valid, false otherwise</returns>
    private bool IsValidMachinePlacement(CellData cellData, MachineDef machineDef)
    {
        // Cell must be empty to place a machine
        if (cellData.cellType != CellType.Blank) return false;

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