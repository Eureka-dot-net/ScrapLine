using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MachineBarUIManager : MonoBehaviour
{
    public GameObject machineButtonPrefab;
    public Button buildTabButton;
    public Button manageTabButton;
    public GameObject buildPanel;
    public GameObject managePanel;

    public Transform machineBarPanel;

    // --- ADD THESE FIELDS ---
    public Texture conveyorPanelTexture;

    /// <summary>
    /// Get the component ID for logging purposes
    /// </summary>
    private string ComponentId => $"MachineBarUIManager_{GetEntityId()}";

    // Selection state
    private MachineDef selectedMachine;
    private GameObject selectedButtonObj;

    // Objective guidance state (see SetPlacementGuidance / SetExistingMachineGuidance). At most
    // one of these is non-null at a time; a manual selection always takes priority over guidance.
    private MachineDef guidancePlacementTarget;
    private string guidanceExistingMachineId;
    private GameObject guidanceButtonObj;

    // Both highlights are drawn as a thin frame around the button by a child object this
    // manager creates on demand -- see SetHighlightBorder for why it can't be an Outline.
    private const string HighlightBorderName = "HighlightBorder";
    private const float HighlightBorderThickness = 6f;

    // The green a manual selection gets, to distinguish it from the orange objective guidance.
    private static readonly Color SelectionBorderColor = new Color(0.30f, 0.95f, 0.40f, 1f);

    // The same orange accent used everywhere else in the UI (ObjectivePanelUI's title/eyebrow,
    // the Manage tab's inactive color, ButtonHoverOutline's hover border): #FFA500. Pulled from
    // the shared, tunable GridColorConfiguration when available, exactly like ButtonHoverOutline
    // does, so a designer changing that one value keeps every orange accent -- guidance included
    // -- in sync instead of drifting.
    private static Color GuidanceOutlineColor =>
        GameManager.Instance != null && GameManager.Instance.gridColorConfig != null
            ? GameManager.Instance.gridColorConfig.uiHoverColor
            : new Color(1f, 165f / 255f, 0f, 1f);

    // Reference to grid manager for highlighting
    private UIGridManager gridManager;

    public Button editButton;

    private bool isInEditMode = false;
    private readonly List<GameObject> generatedMachineButtons = new List<GameObject>();
    private string pendingUnlockMachineId;

    void Awake()
    {
        FactoryRegistry.Instance.MachineUnlocked -= OnMachineUnlocked;
        FactoryRegistry.Instance.MachineUnlocked += OnMachineUnlocked;
        FactoryRegistry.Instance.MachineUnlockStateReloaded -= OnMachineUnlockStateReloaded;
        FactoryRegistry.Instance.MachineUnlockStateReloaded += OnMachineUnlockStateReloaded;
    }

    private void OnDestroy()
    {
        FactoryRegistry.Instance.MachineUnlocked -= OnMachineUnlocked;
        FactoryRegistry.Instance.MachineUnlockStateReloaded -= OnMachineUnlockStateReloaded;
    }

    void Start()
    {
        // Get reference to grid manager
        gridManager = FindAnyObjectByType<UIGridManager>();
        buildTabButton.onClick.AddListener(() => OnTabSelected(buildTabButton));
        manageTabButton.onClick.AddListener(() => OnTabSelected(manageTabButton));
        OnTabSelected(buildTabButton); // Default tab
        if (editButton != null)
        {
            editButton.onClick.AddListener(OnEditModeToggled);
        }
    }

    public void OnTabSelected(Button selectedTab)
    {
        ClearSelection();
        GameManager.Instance.SetEditMode(false);

        var tabs = new[] { buildTabButton, manageTabButton };
        foreach (var tab in tabs)
        {
            var colors = tab.colors;
            colors.normalColor = (tab == selectedTab) ? Color.gray : Color.white;
            tab.colors = colors;
        }

        buildPanel.SetActive(selectedTab == buildTabButton);
        managePanel.SetActive(selectedTab == manageTabButton);

        buildTabButton.interactable = selectedTab != buildTabButton;
        manageTabButton.interactable = selectedTab != manageTabButton;
    }

    public void InitBar()
    {
        ClearSelection();
        ClearGeneratedMachineButtons();
        Canvas.ForceUpdateCanvases();

        RectTransform parentRect = machineBarPanel.GetComponent<RectTransform>();
        HorizontalLayoutGroup horizontalLayout = machineBarPanel.GetComponent<HorizontalLayoutGroup>();
        float verticalPadding = horizontalLayout != null
            ? horizontalLayout.padding.top + horizontalLayout.padding.bottom
            : 0f;
        float targetSize = Mathf.Max(1f, parentRect.rect.height - verticalPadding);

        if (horizontalLayout != null)
        {
            // Each machine owns an explicit square size. The content-size fitter
            // grows the strip horizontally and the parent ScrollRect handles overflow.
            horizontalLayout.childControlWidth = false;
            horizontalLayout.childControlHeight = false;
            horizontalLayout.childForceExpandWidth = false;
            horizontalLayout.childForceExpandHeight = false;
        }

        CreditsManager creditsManager = GetCreditsManager();
        int availableCredits = creditsManager != null ? creditsManager.GetCredits() : 0;

        // Every buildable machine is visible. MachineButton renders licensed/locked state directly
        // from content and registry data, with no machine-specific UI conditions.
        foreach (var machine in FactoryRegistry.Instance.GetPanelMachines())
        {
            GameObject buttonObj = Instantiate(machineButtonPrefab, machineBarPanel);
            generatedMachineButtons.Add(buttonObj);
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(targetSize, targetSize);

            LayoutElement layoutElement = buttonObj.GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = buttonObj.AddComponent<LayoutElement>();

            layoutElement.minWidth = targetSize;
            layoutElement.minHeight = targetSize;
            layoutElement.preferredWidth = targetSize;
            layoutElement.preferredHeight = targetSize;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;

            // Remove AspectRatioFitter to avoid conflicts
            AspectRatioFitter aspectFitter = buttonObj.GetComponent<AspectRatioFitter>();
            if (aspectFitter != null)
            {
                DestroyImmediate(aspectFitter);
            }

            var machineRenderer = buttonObj.GetComponent<MachineRenderer>();
            if (machineRenderer != null)
            {
                machineRenderer.isInMenu = true; // Prevent materials/animations in menu

                // Create temporary CellData and BaseMachine for UI menu display
                var tempCellData = new CellData
                {
                    x = 0,
                    y = 0,
                    cellType = UICell.CellType.Machine,
                    direction = UICell.Direction.Up,
                    machineDefId = machine.id
                };
                
                var tempBaseMachine = MachineFactory.CreateMachine(tempCellData);
                if (tempBaseMachine != null)
                {
                    // --- PASS TEXTURE & MATERIAL TO SETUP ---
                    machineRenderer.Setup(
                        tempBaseMachine,
                        UICell.Direction.Up,
                        null,
                        0,
                        0,
                        conveyorPanelTexture
                    );
                }
                else
                {
                    GameLogger.LogError(LoggingManager.LogCategory.UI, $"Failed to create temporary machine instance for UI button: {machine.id}", ComponentId);
                }
            }
            else
            {
                GameLogger.LogWarning(LoggingManager.LogCategory.UI, "MachineRenderer not found on prefab for machine '{machine.id}'", ComponentId);
            }

            // MachineRenderer.Setup rebuilds the button's visual children. Initialize the button
            // afterward so its license overlay survives that renderer cleanup and remains topmost.
            var machineButton = buttonObj.GetComponent<MachineButton>();
            machineButton.Init(machine, FactoryRegistry.Instance.IsMachineUnlocked(machine.id), availableCredits);
            machineButton.OnButtonClicked += OnMachinePanelClicked;
            machineButton.OnUnlockRequested += OnMachineUnlockRequested;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);

        // ClearSelection() at the top of this method re-resolved guidance against the button
        // list as it existed BEFORE the rebuild, and every one of those GameObjects has since
        // been destroyed. Drop the dangling reference and re-resolve against the new buttons,
        // so objective guidance survives an unlock-triggered rebuild instead of silently
        // pointing at a doomed object.
        guidanceButtonObj = null;
        ApplyPlacementButtonOutline();

        // Newly-generated buttons can leave the strip scrolled past its left edge
        // (or Unity may otherwise leave the ScrollRect at a stale position). Reset
        // to the leftmost position so the first machine (the Spawner) is visible
        // without the player needing to scroll, e.g. right after game launch.
        ResetBuildBarScroll();
    }

    private void ResetBuildBarScroll()
    {
        if (buildPanel == null)
            return;

        ScrollRect scrollRect = buildPanel.GetComponent<ScrollRect>();
        if (scrollRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        scrollRect.horizontalNormalizedPosition = 0f;
    }

    private void OnMachineUnlocked(string machineId, string unlockSource)
    {
        pendingUnlockMachineId = null;
        InitBar();
    }

    private void OnMachineUnlockStateReloaded()
    {
        pendingUnlockMachineId = null;
        InitBar();
    }

    private void OnMachineUnlockRequested(MachineDef machineDef, GameObject buttonObj)
    {
        if (machineDef == null || buttonObj == null)
            return;

        MachineButton machineButton = buttonObj.GetComponent<MachineButton>();
        if (pendingUnlockMachineId != machineDef.id)
        {
            ClearUnlockConfirmation();
            pendingUnlockMachineId = machineDef.id;
            machineButton.SetUnlockConfirmation(true);
            return;
        }

        pendingUnlockMachineId = null;
        CreditsManager creditsManager = GetCreditsManager();
        if (!FactoryRegistry.Instance.TryPurchaseMachineLicense(machineDef.id, creditsManager, out string error))
        {
            machineButton.ShowLicenseError(error);
            return;
        }

        GameLogger.Log(LoggingManager.LogCategory.Economy,
            $"Licensed {machineDef.type} for {machineDef.unlockCost} credits. " +
            $"Construction costs {machineDef.cost} credits.", ComponentId);
    }

    private void ClearUnlockConfirmation()
    {
        pendingUnlockMachineId = null;
        CreditsManager creditsManager = GetCreditsManager();
        int availableCredits = creditsManager != null ? creditsManager.GetCredits() : 0;
        foreach (GameObject buttonObject in generatedMachineButtons)
        {
            if (buttonObject == null)
                continue;
            MachineButton machineButton = buttonObject.GetComponent<MachineButton>();
            if (machineButton == null)
                continue;
            MachineDef machine = machineButton.GetMachineDef();
            machineButton.RefreshLicenseState(
                FactoryRegistry.Instance.IsMachineUnlocked(machine.id), availableCredits);
        }
    }

    private void ClearGeneratedMachineButtons()
    {
        foreach (GameObject button in generatedMachineButtons)
        {
            if (button == null)
                continue;
            button.SetActive(false);
            if (Application.isPlaying)
                Destroy(button);
            else
                DestroyImmediate(button);
        }
        generatedMachineButtons.Clear();
    }

    private void OnMachinePanelClicked(MachineDef machineDef, GameObject buttonObj)
    {
        ClearUnlockConfirmation();

        if (!FactoryRegistry.Instance.IsMachineUnlocked(machineDef?.id))
            return;

        // If the same machine is clicked again, clear selection
        if (selectedMachine == machineDef)
        {
            ClearSelection();
            return;
        }

        // Clear previous selection visual feedback
        ClearSelectionHighlight();

        // Drop guidance BEFORE recording the new selection, so that when the player taps exactly
        // the button guidance was pointing at, the orange frame is torn down first and the green
        // one applied a few lines below owns the button cleanly.
        ClearGuidanceButtonOutline();

        // Set new selection
        selectedMachine = machineDef;
        selectedButtonObj = buttonObj;

        // Highlight selected button
        HighlightSelectedButton(buttonObj);

        // Highlight valid placement areas on grid (keep them visible)
        if (gridManager != null)
        {
            gridManager.HighlightValidPlacements(machineDef);
        }

        // Notify GameManager about machine selection
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetSelectedMachine(machineDef);
        }

        // Re-evaluate placement guidance now that selection changed: it's only suppressed when
        // the player selected exactly the machine being suggested (nothing left to point at).
        // Placing a machine never clears selection on its own (so repeated placements, e.g. a
        // run of conveyors, don't require reselecting each time), so this is what keeps guidance
        // for the *next* objective from staying silently stuck behind a stale old selection.
        ApplyPlacementButtonOutline();
    }

    private void HighlightSelectedButton(GameObject buttonObj)
    {
        SetHighlightBorder(buttonObj, SelectionBorderColor, true);
    }

    private void ClearSelectionHighlight()
    {
        SetHighlightBorder(selectedButtonObj, SelectionBorderColor, false);


        // Clear grid highlighting
        if (gridManager != null)
        {
            gridManager.ClearHighlights();
        }
    }

    public void ClearSelection()
    {
        ClearUnlockConfirmation();
        ClearSelectionHighlight();
        selectedMachine = null;
        selectedButtonObj = null;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetSelectedMachine(null);
        }

        // Nothing is manually selected anymore, so let whatever the current objective is
        // recommending show through again. (Placement guidance -- the Build-bar outline -- isn't
        // actually gated on this; it's just no longer suppressed by matching the old selection.)
        ApplyPlacementButtonOutline();
        ApplyExistingMachineHighlight();
    }

    /// <summary>
    /// Frames the Build-bar button for a machine an objective wants placed next (e.g. the
    /// Spawner, for "Start a scrap line"), in orange. Pass null once that objective is no
    /// longer the recommendation. See <see cref="ApplyPlacementButtonOutline"/> for why this
    /// doesn't need to wait for selection to clear the way <see cref="SetExistingMachineGuidance"/>
    /// does.
    /// </summary>
    public void SetPlacementGuidance(MachineDef machineDef)
    {
        guidancePlacementTarget = machineDef;
        guidanceExistingMachineId = null;
        // A Build-bar button outline doesn't share anything with the grid's highlight layer, so
        // unlike SetExistingMachineGuidance it never needs to wait for selection to clear -- it
        // only makes sense to suppress once the player has selected exactly this machine (see
        // ApplyPlacementButtonOutline). That's also why clearing grid guidance here can't wait
        // either: it must run regardless of what's selected, just like SetExistingMachineGuidance
        // clearing this button outline unconditionally below.
        ApplyExistingMachineHighlight();
        ApplyPlacementButtonOutline();
    }

    /// <summary>
    /// Points at already-placed machines of the given definition ID (e.g. the Spawner, for
    /// "buy your first scrap delivery" -- tapping it is how that objective gets done). Pass
    /// null to clear. Unlike <see cref="SetPlacementGuidance"/>, this shares the grid's
    /// highlight layer with the player's own manual selection, so it stays suppressed until
    /// nothing is selected.
    /// </summary>
    public void SetExistingMachineGuidance(string machineDefId)
    {
        guidancePlacementTarget = null;
        guidanceExistingMachineId = machineDefId;
        ClearGuidanceButtonOutline();
        ApplyExistingMachineHighlight();
    }

    /// <summary>
    /// Frames the Build-bar button for the machine a placement objective wants placed next,
    /// in orange -- the same border a manual selection gets, just a different color -- so a new
    /// player can see which button to tap. This never fights an active selection:
    /// it's suppressed only when the player has selected exactly that machine already (nothing
    /// left to point at), not whenever anything at all is selected, since an outline on one
    /// Build-bar button doesn't visually conflict with a highlight elsewhere.
    /// </summary>
    private void ApplyPlacementButtonOutline()
    {
        ClearGuidanceButtonOutline();

        if (guidancePlacementTarget == null || guidancePlacementTarget == selectedMachine)
            return;

        GameObject button = FindButtonForMachine(guidancePlacementTarget.id);
        if (button == null)
            return;

        guidanceButtonObj = button;
        SetHighlightBorder(button, GuidanceOutlineColor, true);
    }

    /// <summary>
    /// Highlights already-placed machines matching <see cref="guidanceExistingMachineId"/> on
    /// the grid. Shares the grid's single highlight layer with the player's own manual
    /// selection (<see cref="UIGridManager.HighlightValidPlacements"/>), so it only ever runs
    /// while nothing is selected -- otherwise it would silently overwrite (or be overwritten by)
    /// whatever the player is actively placing.
    /// </summary>
    private void ApplyExistingMachineHighlight()
    {
        if (selectedMachine != null)
            return;

        // gridManager is normally cached in Start(), but objective guidance can be pushed here
        // (from ObjectivePanelUI, created and bound explicitly at the end of GameManager.Start())
        // before this component's own Start() has run and cached it. Re-resolve lazily rather
        // than trusting a possibly-premature null, so a guidance push never silently no-ops.
        if (gridManager == null)
            gridManager = FindAnyObjectByType<UIGridManager>();
        if (gridManager == null)
            return;

        if (!string.IsNullOrWhiteSpace(guidanceExistingMachineId))
            gridManager.HighlightMachinesOfType(guidanceExistingMachineId);
        else
            gridManager.ClearHighlights();
    }

    private void ClearGuidanceButtonOutline()
    {
        if (guidanceButtonObj == null)
            return;
        // Leave the selected button's own (green) frame alone -- that one belongs to
        // HighlightSelectedButton / ClearSelectionHighlight, not to guidance.
        if (guidanceButtonObj != selectedButtonObj)
            SetHighlightBorder(guidanceButtonObj, GuidanceOutlineColor, false);
        guidanceButtonObj = null;
    }

    /// <summary>
    /// Shows or hides a thin colored frame around a Build-bar button, used for both the green
    /// manual selection and the orange objective guidance. The frame is a child object with its
    /// own Graphics, created on first use.
    ///
    /// It deliberately is NOT an <see cref="Outline"/> on the button itself. Machine.prefab's
    /// Button uses a ColorTint transition whose every state color has alpha 0, so Unity drives
    /// the button's own CanvasRenderer to fully transparent. An Outline is only extra geometry
    /// inside that same CanvasRenderer, so it gets multiplied by that zero alpha and can never
    /// be seen, whatever effectColor it carries. A child object (like MachineButton's
    /// LicenseStatus overlay) gets its own CanvasRenderer and is unaffected -- and, unlike
    /// tinting the button, it draws an actual border rather than flooding the whole card.
    /// </summary>
    private static void SetHighlightBorder(GameObject buttonObj, Color color, bool on)
    {
        if (buttonObj == null)
            return;

        Transform border = buttonObj.transform.Find(HighlightBorderName);
        if (border == null)
        {
            if (!on)
                return;
            border = CreateHighlightBorder(buttonObj).transform;
        }

        if (on)
        {
            // The shared GridColorConfiguration value is only conventionally opaque, and a
            // half-transparent frame would read as a rendering bug rather than a highlight.
            color.a = 1f;
            foreach (Image edge in border.GetComponentsInChildren<Image>(true))
                edge.color = color;
            // MachineRenderer and MachineButton parent their own visuals here too; stay last so
            // the frame is never buried behind the machine icon or the cost overlay.
            border.SetAsLastSibling();
        }

        border.gameObject.SetActive(on);
    }

    /// <summary>
    /// Builds the frame as four thin edge strips stretched to the button's rect, leaving the
    /// middle untouched so the machine icon underneath stays fully visible.
    /// </summary>
    private static GameObject CreateHighlightBorder(GameObject buttonObj)
    {
        GameObject root = new GameObject(HighlightBorderName, typeof(RectTransform));
        RectTransform rootRect = (RectTransform)root.transform;
        rootRect.SetParent(buttonObj.transform, false);
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        float thickness = HighlightBorderThickness;
        CreateBorderEdge(rootRect, "Top", new Vector2(0f, 1f), Vector2.one,
            new Vector2(0f, -thickness), Vector2.zero);
        CreateBorderEdge(rootRect, "Bottom", Vector2.zero, new Vector2(1f, 0f),
            Vector2.zero, new Vector2(0f, thickness));
        CreateBorderEdge(rootRect, "Left", Vector2.zero, new Vector2(0f, 1f),
            Vector2.zero, new Vector2(thickness, 0f));
        CreateBorderEdge(rootRect, "Right", new Vector2(1f, 0f), Vector2.one,
            new Vector2(-thickness, 0f), Vector2.zero);

        return root;
    }

    private static void CreateBorderEdge(
        RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject edge = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = (RectTransform)edge.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        // Purely decorative: it covers the whole button, so it must never eat the button's taps.
        edge.GetComponent<Image>().raycastTarget = false;
    }

    private GameObject FindButtonForMachine(string machineDefId)
    {
        foreach (GameObject buttonObject in generatedMachineButtons)
        {
            if (buttonObject == null)
                continue;
            MachineButton machineButton = buttonObject.GetComponent<MachineButton>();
            MachineDef machine = machineButton != null ? machineButton.GetMachineDef() : null;
            if (machine != null && machine.id == machineDefId)
                return buttonObject;
        }
        return null;
    }

    public MachineDef GetSelectedMachine()
    {
        return selectedMachine;
    }

    /// <summary>
    /// Updates the affordability of all machine buttons based on current credits
    /// </summary>
    public void UpdateAffordability()
    {
        CreditsManager creditsManager = GetCreditsManager();
        if (creditsManager == null)
            return;
        int availableCredits = creditsManager.GetCredits();

        foreach (GameObject buttonObject in generatedMachineButtons)
        {
            if (buttonObject == null)
                continue;
            MachineButton machineButton = buttonObject.GetComponent<MachineButton>();
            MachineDef machine = machineButton?.GetMachineDef();
            if (machine == null)
                continue;
            machineButton.RefreshLicenseState(
                FactoryRegistry.Instance.IsMachineUnlocked(machine.id), availableCredits);
            if (pendingUnlockMachineId == machine.id)
                machineButton.SetUnlockConfirmation(true);
        }
    }

    private static CreditsManager GetCreditsManager()
    {
        return GameManager.Instance != null && GameManager.Instance.creditsManager != null
            ? GameManager.Instance.creditsManager
            : FindAnyObjectByType<CreditsManager>();
    }

    public void OnEditModeToggled()
    {
        // Toggle the state
        isInEditMode = !isInEditMode;

        if (isInEditMode)
        {
            HighlightSelectedButton(editButton.gameObject);
        }
        else
        {
            ClearSelectionHighlight();
            SetHighlightBorder(editButton.gameObject, SelectionBorderColor, false);
        }

        // Tell the GameManager about the state change
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetEditMode(isInEditMode);
        }
    }
}
