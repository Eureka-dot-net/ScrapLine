using UnityEngine;
using System.Collections;

/// <summary>
/// Orchestrates the complete grid expansion workflow.
/// Coordinates between markers, cost prompt, expansion service, animator, and credits.
/// This is the main coordinator that ties all the expansion systems together.
/// </summary>
public class GridExpansionOrchestrator : MonoBehaviour
{
    [Header("System References")]
    [Tooltip("Reference to the expand mode controller")]
    public ExpandModeController expandModeController;

    [Tooltip("Reference to the grid expansion service")]
    public GridExpansionService gridExpansionService;

    [Tooltip("Reference to the grid markers view")]
    public GridMarkersView gridMarkersView;

    [Tooltip("Reference to the grid expand animator")]
    public GridExpandAnimator gridExpandAnimator;

    [Tooltip("Reference to the expansion cost prompt")]
    public ExpansionCostPrompt expansionCostPrompt;

    [Tooltip("Reference to the grid manager")]
    public GridManager gridManager;

    [Tooltip("Reference to the UI grid manager")]
    public UIGridManager uiGridManager;

    [Tooltip("Reference to the credits manager")]
    public CreditsManager creditsManager;

    [Header("Debug")]
    [Tooltip("Enable debug logs for orchestration operations")]
    public bool enableOrchestrationLogs = true;

    private string ComponentId => $"GridExpansionOrchestrator_{GetInstanceID()}";

    private void Awake()
    {
        // Subscribe to marker click events
        if (gridMarkersView != null)
        {
            gridMarkersView.OnRowMarkerClicked += HandleRowMarkerClicked;
            gridMarkersView.OnColumnMarkerClicked += HandleColumnMarkerClicked;
            gridMarkersView.OnEdgeColumnMarkerClicked += HandleEdgeColumnMarkerClicked;
        }
        else
        {
            GameLogger.LogError(LoggingManager.LogCategory.Grid, "Grid markers view not assigned", ComponentId);
        }
    }

    /// <summary>
    /// Handle row marker click
    /// </summary>
    private void HandleRowMarkerClicked(int rowIndex)
    {
        if (enableOrchestrationLogs)
            GameLogger.LogGrid($"Orchestrating row insertion at index {rowIndex}", ComponentId);

        GridData gridData = gridManager?.GetCurrentGrid();
        if (gridData == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.Grid, "Cannot expand - no grid data", ComponentId);
            return;
        }

        // Calculate cost
        int cost = gridExpansionService.ComputeExpansionCost(
            gridData.height, 
            gridData.width, 
            GridExpansionService.ExpansionType.InsertRow
        );

        // Show cost prompt
        if (expansionCostPrompt != null)
        {
            expansionCostPrompt.Show(
                cost,
                () => ConfirmRowExpansion(rowIndex, cost),
                () => CancelExpansion()
            );
        }
        else
        {
            GameLogger.LogError(LoggingManager.LogCategory.Grid, "Expansion cost prompt not assigned", ComponentId);
        }
    }

    /// <summary>
    /// Handle column marker click
    /// </summary>
    private void HandleColumnMarkerClicked(int colIndex)
    {
        if (enableOrchestrationLogs)
            GameLogger.LogGrid($"Orchestrating column insertion at index {colIndex}", ComponentId);

        GridData gridData = gridManager?.GetCurrentGrid();
        if (gridData == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.Grid, "Cannot expand - no grid data", ComponentId);
            return;
        }

        // Calculate cost
        int cost = gridExpansionService.ComputeExpansionCost(
            gridData.height, 
            gridData.width, 
            GridExpansionService.ExpansionType.InsertColumn
        );

        // Show cost prompt
        if (expansionCostPrompt != null)
        {
            expansionCostPrompt.Show(
                cost,
                () => ConfirmColumnExpansion(colIndex, cost),
                () => CancelExpansion()
            );
        }
        else
        {
            GameLogger.LogError(LoggingManager.LogCategory.Grid, "Expansion cost prompt not assigned", ComponentId);
        }
    }

    /// <summary>
    /// Handle edge column marker click
    /// </summary>
    private void HandleEdgeColumnMarkerClicked(GridExpansionService.Edge edge)
    {
        if (enableOrchestrationLogs)
            GameLogger.LogGrid($"Orchestrating edge column insertion at {edge} edge", ComponentId);

        GridData gridData = gridManager?.GetCurrentGrid();
        if (gridData == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.Grid, "Cannot expand - no grid data", ComponentId);
            return;
        }

        // Calculate cost (use appropriate expansion type)
        GridExpansionService.ExpansionType expansionType = edge == GridExpansionService.Edge.Left
            ? GridExpansionService.ExpansionType.InsertColumnLeft
            : GridExpansionService.ExpansionType.InsertColumnRight;

        int cost = gridExpansionService.ComputeExpansionCost(
            gridData.height, 
            gridData.width, 
            expansionType
        );

        // Show cost prompt
        if (expansionCostPrompt != null)
        {
            expansionCostPrompt.Show(
                cost,
                () => ConfirmEdgeColumnExpansion(edge, cost),
                () => CancelExpansion()
            );
        }
        else
        {
            GameLogger.LogError(LoggingManager.LogCategory.Grid, "Expansion cost prompt not assigned", ComponentId);
        }
    }

    /// <summary>
    /// Confirm row expansion
    /// </summary>
    private void ConfirmRowExpansion(int rowIndex, int cost)
    {
        if (enableOrchestrationLogs)
            GameLogger.LogGrid($"Confirming row expansion at index {rowIndex} for {cost} credits", ComponentId);

        // Check if player can afford
        if (!creditsManager.CanAfford(cost))
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Economy, 
                $"Cannot afford row expansion - need {cost} credits", ComponentId);
            CancelExpansion();
            return;
        }

        // Deduct credits
        creditsManager.TrySpendCredits(cost);

        // Start expansion process
        StartCoroutine(ExecuteRowExpansion(rowIndex));
    }

    /// <summary>
    /// Confirm column expansion
    /// </summary>
    private void ConfirmColumnExpansion(int colIndex, int cost)
    {
        if (enableOrchestrationLogs)
            GameLogger.LogGrid($"Confirming column expansion at index {colIndex} for {cost} credits", ComponentId);

        // Check if player can afford
        if (!creditsManager.CanAfford(cost))
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Economy, 
                $"Cannot afford column expansion - need {cost} credits", ComponentId);
            CancelExpansion();
            return;
        }

        // Deduct credits
        creditsManager.TrySpendCredits(cost);

        // Start expansion process
        StartCoroutine(ExecuteColumnExpansion(colIndex));
    }

    /// <summary>
    /// Confirm edge column expansion
    /// </summary>
    private void ConfirmEdgeColumnExpansion(GridExpansionService.Edge edge, int cost)
    {
        if (enableOrchestrationLogs)
            GameLogger.LogGrid($"Confirming edge column expansion at {edge} for {cost} credits", ComponentId);

        // Check if player can afford
        if (!creditsManager.CanAfford(cost))
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Economy, 
                $"Cannot afford edge column expansion - need {cost} credits", ComponentId);
            CancelExpansion();
            return;
        }

        // Deduct credits
        creditsManager.TrySpendCredits(cost);

        // Start expansion process
        StartCoroutine(ExecuteEdgeColumnExpansion(edge));
    }

    /// <summary>
    /// Cancel expansion
    /// </summary>
    private void CancelExpansion()
    {
        if (enableOrchestrationLogs)
            GameLogger.LogGrid("Expansion cancelled", ComponentId);

        // Nothing to do - stay in expand mode
    }

    /// <summary>
    /// Execute row expansion with animation
    /// </summary>
    private IEnumerator ExecuteRowExpansion(int rowIndex)
    {
        if (enableOrchestrationLogs)
            GameLogger.LogGrid($"Executing row expansion at index {rowIndex}", ComponentId);

        GridData gridData = gridManager?.GetCurrentGrid();
        if (gridData == null) yield break;

        // Play animation
        if (gridExpandAnimator != null)
        {
            yield return StartCoroutine(gridExpandAnimator.PlayInsertRow(rowIndex));
        }

        // Mutate data
        gridExpansionService.InsertRow(gridData, rowIndex);

        // Refresh UI grid
        if (uiGridManager != null)
        {
            uiGridManager.InitGrid(gridData);
        }

        // Exit expand mode
        ExitExpandMode();

        if (enableOrchestrationLogs)
            GameLogger.LogGrid("Row expansion completed", ComponentId);
    }

    /// <summary>
    /// Execute column expansion with animation
    /// </summary>
    private IEnumerator ExecuteColumnExpansion(int colIndex)
    {
        if (enableOrchestrationLogs)
            GameLogger.LogGrid($"Executing column expansion at index {colIndex}", ComponentId);

        GridData gridData = gridManager?.GetCurrentGrid();
        if (gridData == null) yield break;

        // Play animation
        if (gridExpandAnimator != null)
        {
            yield return StartCoroutine(gridExpandAnimator.PlayInsertColumn(colIndex));
        }

        // Mutate data
        gridExpansionService.InsertColumn(gridData, colIndex);

        // Refresh UI grid
        if (uiGridManager != null)
        {
            uiGridManager.InitGrid(gridData);
        }

        // Exit expand mode
        ExitExpandMode();

        if (enableOrchestrationLogs)
            GameLogger.LogGrid("Column expansion completed", ComponentId);
    }

    /// <summary>
    /// Execute edge column expansion with animation
    /// </summary>
    private IEnumerator ExecuteEdgeColumnExpansion(GridExpansionService.Edge edge)
    {
        if (enableOrchestrationLogs)
            GameLogger.LogGrid($"Executing edge column expansion at {edge}", ComponentId);

        GridData gridData = gridManager?.GetCurrentGrid();
        if (gridData == null) yield break;

        // Play animation
        if (gridExpandAnimator != null)
        {
            yield return StartCoroutine(gridExpandAnimator.PlayInsertEdgeColumn(edge));
        }

        // Mutate data
        gridExpansionService.InsertColumnAtEdge(gridData, edge);

        // Refresh UI grid
        if (uiGridManager != null)
        {
            uiGridManager.InitGrid(gridData);
        }

        // Exit expand mode
        ExitExpandMode();

        if (enableOrchestrationLogs)
            GameLogger.LogGrid("Edge column expansion completed", ComponentId);
    }

    /// <summary>
    /// Exit expand mode after successful expansion
    /// </summary>
    private void ExitExpandMode()
    {
        if (expandModeController != null)
        {
            expandModeController.DisableExpandMode();
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from marker events
        if (gridMarkersView != null)
        {
            gridMarkersView.OnRowMarkerClicked -= HandleRowMarkerClicked;
            gridMarkersView.OnColumnMarkerClicked -= HandleColumnMarkerClicked;
            gridMarkersView.OnEdgeColumnMarkerClicked -= HandleEdgeColumnMarkerClicked;
        }
    }
}

/*
 * UNITY WIRING INSTRUCTIONS:
 * 
 * 1. Add this component to the "ExpandModeSystem" GameObject
 * 2. Assign ALL system references in Inspector:
 *    - Expand Mode Controller: ExpandModeController component (same GameObject)
 *    - Grid Expansion Service: GridExpansionService component (same GameObject)
 *    - Grid Markers View: GridMarkersView component (same GameObject)
 *    - Grid Expand Animator: GridExpandAnimator component (same GameObject)
 *    - Expansion Cost Prompt: ExpansionCostPrompt component (from prompt panel)
 *    - Grid Manager: GameManager's GridManager component
 *    - UI Grid Manager: UIGridManager in scene
 *    - Credits Manager: GameManager's CreditsManager component
 * 3. Configure debug:
 *    - Enable Orchestration Logs: true (for debugging)
 * 4. This is the main coordinator - ensure all components are properly wired
 * 5. Testing sequence:
 *    - Enable expand mode
 *    - Click a marker
 *    - Verify cost prompt appears
 *    - Confirm purchase
 *    - Verify credits deducted
 *    - Verify animation plays
 *    - Verify grid expands
 *    - Verify expand mode exits
 */
