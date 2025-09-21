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