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
    public bool enableMachineLogs = false;

    [Header("UI References")]
    [Tooltip("Reference to the waste crate UI manager")]
    public WasteCrateConfigPanel wasteCrateUI;

    private MachineDef selectedMachine;
    private Direction lastMachineDirection = Direction.Up;
    private CreditsManager creditsManager;
    private GridManager gridManager;
    private UIGridManager activeGridManager;
    
    /// <summary>
    /// Get the component ID for logging purposes
    /// </summary>
    private string ComponentId => $"MachineManager_{GetInstanceID()}";

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
            GameLogger.LogError(LoggingManager.LogCategory.Machine, $"Clicked cell is not in the data model! Coords: " + x + ", " + y, ComponentId);
            return;
        }

        // Check if we're in edit mode
        if (GameManager.Instance != null && GameManager.Instance.IsInEditMode())
        {
            // In edit mode, only handle configuration of machines that can be configured
            if (cellData.cellType == CellType.Machine && cellData.machine != null && cellData.machine.CanConfigure)
            {
                cellData.machine.OnConfigured();
                return;
            }
            else
            {
                return;
            }
        }

        // Normal mode behavior (not in edit mode)
        // If clicking on an existing machine, handle special actions
        if (cellData.cellType == CellType.Machine && !string.IsNullOrEmpty(cellData.machineDefId))
        {
            // Special handling for spawner machines - show waste crate menu
            if (cellData.machineDefId == "spawner" && cellData.machine is SpawnerMachine)
            {
                ShowSpawnerWasteCrateMenu(x, y);
                return;
            }
            
            // For other machines, rotate them
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
                    GameLogger.LogWarning(LoggingManager.LogCategory.Machine, $"Cannot afford {selectedMachine.id} - costs {selectedMachine.cost}, have {creditsManager.GetCredits()} credits", ComponentId);
                    return;
                }
            }
            else
            {
                return;
            }
        }

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
            return false;
        }

        bool canDrag = cellData.cellType == CellType.Machine && !string.IsNullOrEmpty(cellData.machineDefId);

        return canDrag;
    }

    /// <summary>
    /// Prepare a machine for dragging (called when drag starts)
    /// This immediately blanks the original cell to prevent phantom machines
    /// </summary>
    /// <param name="x">X coordinate of the machine being dragged</param>
    /// <param name="y">Y coordinate of the machine being dragged</param>
    public void StartMachineDrag(int x, int y)
    {

        CellData cellData = gridManager.GetCellData(x, y);
        if (cellData == null || cellData.cellType != CellType.Machine)
        {
            if (enableMachineLogs)
                GameLogger.LogWarning(LoggingManager.LogCategory.Machine, $"StartMachineDrag({x}, {y}): No machine to drag", ComponentId);
            return;
        }

        // Immediately blank the original cell (both logic and visuals)
        // This prevents phantom machine effects during drag
        cellData.cellType = CellType.Blank;
        cellData.machineDefId = null;
        cellData.direction = Direction.Up;
        cellData.machine = null;

        // Update visuals to show cell as blank
        if (activeGridManager != null)
        {
            activeGridManager.UpdateCellVisuals(x, y, cellData.cellType, cellData.direction, null);
        }

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
            return false;
        }

        // Source must have a machine
        if (sourceCellData.cellType != CellType.Machine || string.IsNullOrEmpty(sourceCellData.machineDefId))
        {
            return false;
        }

        // Target must be empty (blank cell)
        if (targetCellData.cellType != CellType.Blank)
        {
            return false;
        }

        // Check if machine can be placed at target location
        MachineDef machineDef = FactoryRegistry.Instance.GetMachine(sourceCellData.machineDefId);
        if (machineDef == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.Machine, $"CanDropMachine: Cannot find machine definition for {sourceCellData.machineDefId}", ComponentId);
            return false;
        }

        bool canPlace = IsValidMachinePlacement(targetCellData, machineDef);

        return canPlace;
    }

    /// <summary>
    /// Check if a machine can be placed at the target location using machine definition ID
    /// Used for drag-and-drop operations where source cell is already blanked
    /// </summary>
    /// <param name="toX">Target X coordinate</param>
    /// <param name="toY">Target Y coordinate</param>
    /// <param name="machineDefId">Machine definition ID to check</param>
    /// <returns>True if the machine can be dropped at the target location</returns>
    public bool CanDropMachineWithDefId(int toX, int toY, string machineDefId)
    {
        CellData targetCellData = gridManager.GetCellData(toX, toY);

        if (targetCellData == null)
        {
            if (enableMachineLogs)
                GameLogger.LogError(LoggingManager.LogCategory.Machine, $"CanDropMachineWithDefId: Invalid target coordinates ({toX}, {toY})", ComponentId);
            return false;
        }

        // Check if target cell is empty
        if (targetCellData.cellType != CellType.Blank)
        {
            return false;
        }

        // Check if machine can be placed at target location
        MachineDef machineDef = FactoryRegistry.Instance.GetMachine(machineDefId);
        if (machineDef == null)
        {
            if (enableMachineLogs)
                GameLogger.LogError(LoggingManager.LogCategory.Machine, $"CanDropMachineWithDefId: Cannot find machine definition for {machineDefId}", ComponentId);
            return false;
        }

        bool canPlace = IsValidMachinePlacement(targetCellData, machineDef);

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
                GameLogger.LogWarning(LoggingManager.LogCategory.Machine, $"Cannot move machine from ({fromX}, {fromY}) to ({toX}, {toY}) - invalid move", ComponentId);
            return;
        }

        CellData sourceCellData = gridManager.GetCellData(fromX, fromY);
        CellData targetCellData = gridManager.GetCellData(toX, toY);

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
            activeGridManager.UpdateCellVisuals(toX, toY, targetCellData.cellType, targetCellData.direction);

            // Update source cell to blank
            activeGridManager.UpdateCellVisuals(fromX, fromY, sourceCellData.cellType, sourceCellData.direction);
        }

    }

    /// <summary>
    /// Place a dragged machine at the target location using stored machine data
    /// Used when completing a successful drag-and-drop operation
    /// </summary>
    /// <param name="x">Target X coordinate</param>
    /// <param name="y">Target Y coordinate</param>
    /// <param name="machineDefId">Machine definition ID to place</param>
    /// <param name="direction">Direction of the machine</param>
    /// <returns>True if placement was successful, false otherwise</returns>
    public bool PlaceDraggedMachine(int x, int y, string machineDefId, Direction direction)
    {
        // Use the new validation method that doesn't require source cell data
        if (!CanDropMachineWithDefId(x, y, machineDefId))
        {
            if (enableMachineLogs)
                GameLogger.LogWarning(LoggingManager.LogCategory.Machine, $"PlaceDraggedMachine({x}, {y}): Cannot place {machineDefId} at target location", ComponentId);
            return false;
        }

        CellData targetCellData = gridManager.GetCellData(x, y);
        if (targetCellData == null)
        {
            if (enableMachineLogs)
                GameLogger.LogError(LoggingManager.LogCategory.Machine, $"PlaceDraggedMachine({x}, {y}): No cell data found", ComponentId);
            return false;
        }

        // Place the machine (no cost since it's being moved, not newly placed)
        targetCellData.cellType = CellType.Machine;
        targetCellData.machineDefId = machineDefId;
        targetCellData.direction = direction;
        targetCellData.machine = MachineFactory.CreateMachine(targetCellData);

        if (targetCellData.machine == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.Machine, $"Failed to create machine object for dragged {machineDefId}", ComponentId);
        }

        // Update visuals
        if (activeGridManager != null)
        {
            activeGridManager.UpdateCellVisuals(x, y, targetCellData.cellType, targetCellData.direction);
        }

        return true;
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
                GameLogger.LogWarning(LoggingManager.LogCategory.Machine, $"DeleteMachine({x}, {y}): No machine to delete", ComponentId);
            return;
        }

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
        if (!creditsManager.TrySpendCredits(machineDef.cost))
        {
            GameLogger.LogError(LoggingManager.LogCategory.Machine, $"Failed to place machine {machineDef.id} - insufficient credits!", ComponentId);
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
            GameLogger.LogError(LoggingManager.LogCategory.Machine, $"Failed to create machine object for {machineDef.id}", ComponentId);
        }

        if (activeGridManager != null)
        {
            activeGridManager.UpdateCellVisuals(cellData.x, cellData.y, cellData.cellType, cellData.direction);
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
            GameLogger.LogError(LoggingManager.LogCategory.Machine, $"Cannot find machine definition for {cellData.machineDefId}", ComponentId);
            return;
        }

        if (!machineDef.canRotate)
        {
            return;
        }

        cellData.direction = (Direction)(((int)cellData.direction + 1) % 4);
        lastMachineDirection = cellData.direction;

        if (activeGridManager != null)
        {
            activeGridManager.UpdateCellVisuals(cellData.x, cellData.y, cellData.cellType, cellData.direction);
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
    
    /// <summary>
    /// Show the waste crate menu for a spawner machine
    /// </summary>
    /// <param name="spawnerX">X coordinate of spawner</param>
    /// <param name="spawnerY">Y coordinate of spawner</param>
    private void ShowSpawnerWasteCrateMenu(int spawnerX, int spawnerY)
    {
        GameLogger.LogUI($"Showing waste crate menu for spawner at ({spawnerX}, {spawnerY})", ComponentId);
        
        // Get the spawner machine
        var cellData = gridManager.GetCellData(spawnerX, spawnerY);
        var spawner = cellData?.machine as SpawnerMachine;
        
        if (spawner == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.UI, $"No spawner machine found at ({spawnerX}, {spawnerY})", ComponentId);
            return;
        }
        
        // Show UI if available
        if (wasteCrateUI != null)
        {
           // wasteCrateUI.ShowConfiguration(spawnerX, spawnerY, OnConfigurationConfirmed);
        }
        else
        {
            // Fallback: Log global queue information using WasteSupplyManager
            var wasteSupplyManager = GameManager.Instance?.wasteSupplyManager;
            if (wasteSupplyManager != null)
            {
                var queueStatus = wasteSupplyManager.GetGlobalQueueStatus();
                GameLogger.LogUI($"Current crate: {queueStatus.currentCrateId ?? "None"}", ComponentId);
                GameLogger.LogUI($"Global queue size: {queueStatus.queuedCrateIds.Count}/{queueStatus.maxQueueSize}", ComponentId);
                GameLogger.LogUI($"Can add to global queue: {queueStatus.canAddToQueue}", ComponentId);
            }
            else
            {
                GameLogger.LogWarning(LoggingManager.LogCategory.UI, "WasteSupplyManager not found", ComponentId);
            }
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, "WasteCrateUI not assigned to MachineManager", ComponentId);
        }
    }
}