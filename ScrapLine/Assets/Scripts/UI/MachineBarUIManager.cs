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
    }

    private void HighlightSelectedButton(GameObject buttonObj)
    {
        // Add visual feedback to show this button is selected
        var button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            var colors = button.colors;
            colors.selectedColor = new Color(0.8f, 1f, 0.8f, 1f); // Light green
            button.colors = colors;
        }

        // Add outline or border effect if desired
        var outline = buttonObj.GetComponent<Outline>();
        if (outline == null)
        {
            outline = buttonObj.AddComponent<Outline>();
        }
        outline.effectColor = Color.green;
        outline.effectDistance = new Vector2(2, 2);
        outline.enabled = true;
    }

    private void ClearSelectionHighlight()
    {
        if (selectedButtonObj != null)
        {
            // Remove outline
            var outline = selectedButtonObj.GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = false;
            }
        }


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
            // reset button visual
            
            // Remove outline
            var outline = editButton.GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = false;
            }
        }

        // Tell the GameManager about the state change
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetEditMode(isInEditMode);
        }
    }
}
