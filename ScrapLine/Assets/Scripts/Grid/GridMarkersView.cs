using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages visual markers for grid expansion.
/// Spawns inline markers between rows/columns and edge markers for columns.
/// Handles marker interactions (hover, click) and provides callbacks.
/// </summary>
public class GridMarkersView : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the expand mode controller")]
    public ExpandModeController expandModeController;

    [Tooltip("Reference to the grid expansion service")]
    public GridExpansionService gridExpansionService;

    [Tooltip("Reference to the grid manager")]
    public GridManager gridManager;

    [Tooltip("Reference to the UI grid manager")]
    public UIGridManager uiGridManager;

    [Header("Marker Prefabs")]
    [Tooltip("Prefab for inline + markers (between rows/columns)")]
    public GameObject inlinePlusMarkerPrefab;

    [Tooltip("Prefab for edge + markers (left/right edges for columns)")]
    public GameObject edgePlusMarkerPrefab;

    [Header("Marker Containers")]
    [Tooltip("Container for row markers (horizontal lines between rows)")]
    public RectTransform rowMarkersContainer;

    [Tooltip("Container for column markers (vertical lines between columns)")]
    public RectTransform columnMarkersContainer;

    [Tooltip("Container for edge markers (left and right edges)")]
    public RectTransform edgeMarkersContainer;

    [Header("Marker Settings")]
    [Tooltip("Size of inline markers")]
    public Vector2 inlineMarkerSize = new Vector2(40f, 40f);

    [Tooltip("Size of edge markers")]
    public Vector2 edgeMarkerSize = new Vector2(48f, 48f);

    [Tooltip("Hover scale multiplier")]
    [UnityEngine.Range(1.0f, 2.0f)]
    public float hoverScaleMultiplier = 1.2f;

    [Tooltip("Scale animation duration")]
    public float scaleAnimationDuration = 0.15f;

    [Header("Debug")]
    [Tooltip("Enable debug logs for marker operations")]
    public bool enableMarkerLogs = true;

    // Callbacks for marker clicks
    public event Action<int> OnRowMarkerClicked;
    public event Action<int> OnColumnMarkerClicked;
    public event Action<GridExpansionService.Edge> OnEdgeColumnMarkerClicked;

    // Active markers tracking
    private List<GameObject> activeMarkers = new List<GameObject>();
    private Dictionary<GameObject, Vector3> originalMarkerScales = new Dictionary<GameObject, Vector3>();

    private string ComponentId => $"GridMarkersView_{GetInstanceID()}";

    private void Awake()
    {
        // Subscribe to expand mode events
        if (expandModeController != null)
        {
            expandModeController.OnExpandModeEnabled += ShowMarkers;
            expandModeController.OnExpandModeDisabled += HideMarkers;
        }
    }

    /// <summary>
    /// Show all expansion markers
    /// </summary>
    private void ShowMarkers()
    {
        if (enableMarkerLogs)
            GameLogger.LogGrid("Showing expansion markers", ComponentId);

        ClearMarkers();

        GridData gridData = gridManager?.GetCurrentGrid();
        if (gridData == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.Grid, "Cannot show markers - no grid data available", ComponentId);
            return;
        }

        // Spawn row markers (internal positions only)
        SpawnRowMarkers(gridData);

        // Spawn column markers (including internal positions)
        SpawnColumnMarkers(gridData);

        // Spawn edge column markers (left and right edges)
        SpawnEdgeColumnMarkers(gridData);

        if (enableMarkerLogs)
            GameLogger.LogGrid($"Spawned {activeMarkers.Count} expansion markers", ComponentId);
    }

    /// <summary>
    /// Hide all expansion markers
    /// </summary>
    private void HideMarkers()
    {
        if (enableMarkerLogs)
            GameLogger.LogGrid("Hiding expansion markers", ComponentId);

        ClearMarkers();
    }

    /// <summary>
    /// Spawn markers between rows (internal only, not at top/bottom edges)
    /// </summary>
    private void SpawnRowMarkers(GridData gridData)
    {
        if (rowMarkersContainer == null || inlinePlusMarkerPrefab == null)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Grid, "Row markers container or prefab not assigned", ComponentId);
            return;
        }

        // Get valid row insertion indices (internal only)
        List<int> rowIndices = gridExpansionService?.GetValidRowInsertionIndices(gridData);
        if (rowIndices == null || rowIndices.Count == 0)
        {
            if (enableMarkerLogs)
                GameLogger.LogGrid("No valid row insertion positions", ComponentId);
            return;
        }

        foreach (int rowIndex in rowIndices)
        {
            // Create marker between row (rowIndex-1) and row (rowIndex)
            GameObject marker = Instantiate(inlinePlusMarkerPrefab, rowMarkersContainer);
            RectTransform markerRect = marker.GetComponent<RectTransform>();
            markerRect.sizeDelta = inlineMarkerSize;

            // Position marker between the two rows
            Vector3 upperCellPos = uiGridManager.GetCellWorldPosition(gridData.width / 2, rowIndex - 1);
            Vector3 lowerCellPos = uiGridManager.GetCellWorldPosition(gridData.width / 2, rowIndex);
            Vector3 markerPos = (upperCellPos + lowerCellPos) / 2f;
            markerRect.position = markerPos;

            // Store original scale
            originalMarkerScales[marker] = marker.transform.localScale;

            // Setup click handler
            Button markerButton = marker.GetComponent<Button>();
            if (markerButton == null)
                markerButton = marker.AddComponent<Button>();

            int capturedIndex = rowIndex;
            markerButton.onClick.AddListener(() => HandleRowMarkerClick(capturedIndex));

            // Setup hover events
            SetupMarkerHoverEvents(marker);

            activeMarkers.Add(marker);
        }

        if (enableMarkerLogs)
            GameLogger.LogGrid($"Spawned {rowIndices.Count} row markers", ComponentId);
    }

    /// <summary>
    /// Spawn markers between columns (internal positions)
    /// </summary>
    private void SpawnColumnMarkers(GridData gridData)
    {
        if (columnMarkersContainer == null || inlinePlusMarkerPrefab == null)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Grid, "Column markers container or prefab not assigned", ComponentId);
            return;
        }

        // Spawn markers between internal columns (not at edges - those use edge markers)
        for (int colIndex = 1; colIndex < gridData.width; colIndex++)
        {
            GameObject marker = Instantiate(inlinePlusMarkerPrefab, columnMarkersContainer);
            RectTransform markerRect = marker.GetComponent<RectTransform>();
            markerRect.sizeDelta = inlineMarkerSize;

            // Position marker between the two columns
            Vector3 leftCellPos = uiGridManager.GetCellWorldPosition(colIndex - 1, gridData.height / 2);
            Vector3 rightCellPos = uiGridManager.GetCellWorldPosition(colIndex, gridData.height / 2);
            Vector3 markerPos = (leftCellPos + rightCellPos) / 2f;
            markerRect.position = markerPos;

            // Store original scale
            originalMarkerScales[marker] = marker.transform.localScale;

            // Setup click handler
            Button markerButton = marker.GetComponent<Button>();
            if (markerButton == null)
                markerButton = marker.AddComponent<Button>();

            int capturedIndex = colIndex;
            markerButton.onClick.AddListener(() => HandleColumnMarkerClick(capturedIndex));

            // Setup hover events
            SetupMarkerHoverEvents(marker);

            activeMarkers.Add(marker);
        }

        if (enableMarkerLogs)
            GameLogger.LogGrid($"Spawned {gridData.width - 1} column markers", ComponentId);
    }

    /// <summary>
    /// Spawn edge markers for columns (left and right edges only)
    /// </summary>
    private void SpawnEdgeColumnMarkers(GridData gridData)
    {
        if (edgeMarkersContainer == null || edgePlusMarkerPrefab == null)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Grid, "Edge markers container or prefab not assigned", ComponentId);
            return;
        }

        Vector2 cellSize = uiGridManager.GetCellSize();

        // Left edge marker
        GameObject leftMarker = Instantiate(edgePlusMarkerPrefab, edgeMarkersContainer);
        RectTransform leftMarkerRect = leftMarker.GetComponent<RectTransform>();
        leftMarkerRect.sizeDelta = edgeMarkerSize;

        Vector3 leftEdgePos = uiGridManager.GetCellWorldPosition(0, gridData.height / 2);
        leftEdgePos.x -= cellSize.x / 2f + edgeMarkerSize.x / 2f + 10f; // Offset to the left
        leftMarkerRect.position = leftEdgePos;

        originalMarkerScales[leftMarker] = leftMarker.transform.localScale;

        Button leftButton = leftMarker.GetComponent<Button>();
        if (leftButton == null)
            leftButton = leftMarker.AddComponent<Button>();
        leftButton.onClick.AddListener(() => HandleEdgeColumnMarkerClick(GridExpansionService.Edge.Left));

        SetupMarkerHoverEvents(leftMarker);
        activeMarkers.Add(leftMarker);

        // Right edge marker
        GameObject rightMarker = Instantiate(edgePlusMarkerPrefab, edgeMarkersContainer);
        RectTransform rightMarkerRect = rightMarker.GetComponent<RectTransform>();
        rightMarkerRect.sizeDelta = edgeMarkerSize;

        Vector3 rightEdgePos = uiGridManager.GetCellWorldPosition(gridData.width - 1, gridData.height / 2);
        rightEdgePos.x += cellSize.x / 2f + edgeMarkerSize.x / 2f + 10f; // Offset to the right
        rightMarkerRect.position = rightEdgePos;

        originalMarkerScales[rightMarker] = rightMarker.transform.localScale;

        Button rightButton = rightMarker.GetComponent<Button>();
        if (rightButton == null)
            rightButton = rightMarker.AddComponent<Button>();
        rightButton.onClick.AddListener(() => HandleEdgeColumnMarkerClick(GridExpansionService.Edge.Right));

        SetupMarkerHoverEvents(rightMarker);
        activeMarkers.Add(rightMarker);

        if (enableMarkerLogs)
            GameLogger.LogGrid("Spawned 2 edge column markers (left and right)", ComponentId);
    }

    /// <summary>
    /// Setup hover events for a marker
    /// </summary>
    private void SetupMarkerHoverEvents(GameObject marker)
    {
        EventTrigger trigger = marker.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = marker.AddComponent<EventTrigger>();

        // Pointer enter
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => OnMarkerHoverEnter(marker));
        trigger.triggers.Add(enterEntry);

        // Pointer exit
        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => OnMarkerHoverExit(marker));
        trigger.triggers.Add(exitEntry);
    }

    /// <summary>
    /// Handle marker hover enter
    /// </summary>
    private void OnMarkerHoverEnter(GameObject marker)
    {
        if (!originalMarkerScales.ContainsKey(marker)) return;

        // Animate scale up
        Vector3 targetScale = originalMarkerScales[marker] * hoverScaleMultiplier;
        StartCoroutine(AnimateMarkerScale(marker, targetScale, scaleAnimationDuration));
    }

    /// <summary>
    /// Handle marker hover exit
    /// </summary>
    private void OnMarkerHoverExit(GameObject marker)
    {
        if (!originalMarkerScales.ContainsKey(marker)) return;

        // Animate scale back to original
        Vector3 targetScale = originalMarkerScales[marker];
        StartCoroutine(AnimateMarkerScale(marker, targetScale, scaleAnimationDuration));
    }

    /// <summary>
    /// Animate marker scale
    /// </summary>
    private System.Collections.IEnumerator AnimateMarkerScale(GameObject marker, Vector3 targetScale, float duration)
    {
        if (marker == null) yield break;

        Vector3 startScale = marker.transform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (marker == null) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            marker.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        if (marker != null)
            marker.transform.localScale = targetScale;
    }

    /// <summary>
    /// Handle row marker click
    /// </summary>
    private void HandleRowMarkerClick(int rowIndex)
    {
        if (enableMarkerLogs)
            GameLogger.LogGrid($"Row marker clicked at index {rowIndex}", ComponentId);

        OnRowMarkerClicked?.Invoke(rowIndex);
    }

    /// <summary>
    /// Handle column marker click
    /// </summary>
    private void HandleColumnMarkerClick(int colIndex)
    {
        if (enableMarkerLogs)
            GameLogger.LogGrid($"Column marker clicked at index {colIndex}", ComponentId);

        OnColumnMarkerClicked?.Invoke(colIndex);
    }

    /// <summary>
    /// Handle edge column marker click
    /// </summary>
    private void HandleEdgeColumnMarkerClick(GridExpansionService.Edge edge)
    {
        if (enableMarkerLogs)
            GameLogger.LogGrid($"Edge column marker clicked: {edge}", ComponentId);

        OnEdgeColumnMarkerClicked?.Invoke(edge);
    }

    /// <summary>
    /// Clear all active markers
    /// </summary>
    private void ClearMarkers()
    {
        foreach (GameObject marker in activeMarkers)
        {
            if (marker != null)
                Destroy(marker);
        }

        activeMarkers.Clear();
        originalMarkerScales.Clear();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (expandModeController != null)
        {
            expandModeController.OnExpandModeEnabled -= ShowMarkers;
            expandModeController.OnExpandModeDisabled -= HideMarkers;
        }

        // Clear callbacks
        OnRowMarkerClicked = null;
        OnColumnMarkerClicked = null;
        OnEdgeColumnMarkerClicked = null;

        // Clean up markers
        ClearMarkers();
    }
}

/*
 * UNITY WIRING INSTRUCTIONS:
 * 
 * 1. Add this component to the "ExpandModeSystem" GameObject
 * 2. Assign references:
 *    - Expand Mode Controller: ExpandModeController component (same GameObject)
 *    - Grid Expansion Service: GridExpansionService component (same GameObject)
 *    - Grid Manager: Find GameManager's GridManager component
 *    - UI Grid Manager: Find the UIGridManager in the scene
 * 3. Create marker container GameObjects (empty with RectTransform):
 *    - In the Canvas, under the grid container, create:
 *      a) "RowMarkersContainer" (RectTransform, stretch-stretch, offsets 0)
 *      b) "ColumnMarkersContainer" (RectTransform, stretch-stretch, offsets 0)
 *      c) "EdgeMarkersContainer" (RectTransform, stretch-stretch, offsets 0)
 *    - Place these ABOVE the DimOverlay so markers are visible
 *    - Drag each container into the corresponding field in Inspector
 * 4. Create marker prefabs (see separate prefab creation guide)
 * 5. Configure marker settings:
 *    - Inline Marker Size: (40, 40)
 *    - Edge Marker Size: (48, 48)
 *    - Hover Scale Multiplier: 1.2
 *    - Scale Animation Duration: 0.15
 *    - Enable Marker Logs: true (for debugging)
 */
