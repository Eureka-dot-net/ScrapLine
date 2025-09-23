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

    // Selection state
    private MachineDef selectedMachine;
    private GameObject selectedButtonObj;

    // Reference to grid manager for highlighting
    private UIGridManager gridManager;

    public Button editButton;

    private bool isInEditMode = false;

    void Awake()
    {
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
        foreach (var machine in FactoryRegistry.Instance.Machines.Values)
        {
            // Skip machines that shouldn't be displayed in panel
            if (!machine.displayInPanel)
            {
                continue;
            }

            GameObject buttonObj = Instantiate(machineButtonPrefab, machineBarPanel);

            RectTransform parentRect = machineBarPanel.GetComponent<RectTransform>();
            float unscaledHeight = parentRect.rect.height;
            float scale = parentRect.lossyScale.y; // Get the actual scale factor
            float targetSize = unscaledHeight * scale;

            // targetSize = 200;
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(targetSize, targetSize);

            // Set fixed size instead of flexible + aspect ratio
            // LayoutElement layoutElement = buttonObj.GetComponent<LayoutElement>();
            // if (layoutElement == null)
            // {
            //     layoutElement = buttonObj.AddComponent<LayoutElement>();
            // }
            // targetSize = 200;
            // layoutElement.preferredWidth = targetSize;
            // layoutElement.preferredHeight = targetSize;
            // layoutElement.minHeight = targetSize;
            // layoutElement.minWidth = targetSize;
            // LayoutRebuilder.MarkLayoutForRebuild(buttonObj.GetComponent<RectTransform>());

            // Remove AspectRatioFitter to avoid conflicts
            AspectRatioFitter aspectFitter = buttonObj.GetComponent<AspectRatioFitter>();
            if (aspectFitter != null)
            {
                DestroyImmediate(aspectFitter);
            }

            var machineButton = buttonObj.GetComponent<MachineButton>();
            machineButton.Init(machine);
            machineButton.OnButtonClicked += OnMachinePanelClicked;

            var machineRenderer = buttonObj.GetComponent<MachineRenderer>();
            if (machineRenderer != null)
            {
                machineRenderer.isInMenu = true; // Prevent materials/animations in menu

                // --- PASS TEXTURE & MATERIAL TO SETUP ---
                machineRenderer.Setup(
                    machine,
                    UICell.Direction.Up,
                    null,
                    0,
                    0,
                    conveyorPanelTexture
                );
            }
            else
            {
                GameLogger.LogWarning(LoggingManager.LogCategory.UI, "MachineRenderer not found on prefab for machine '{machine.id}'", ComponentId);
            }
        }
    }

    private void OnMachinePanelClicked(MachineDef machineDef, GameObject buttonObj)
    {

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
        if (GameManager.Instance == null)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, "GameManager.Instance is null, cannot update machine affordability", ComponentId);
            return;
        }

        // Find all machine buttons in the panel
        MachineButton[] machineButtons = managePanel.GetComponentsInChildren<MachineButton>();

        foreach (var machineButton in machineButtons)
        {
            var button = machineButton.GetComponent<Button>();
            var machineDef = machineButton.GetMachineDef(); // We'll need to add this method to MachineButton

            if (button != null && machineDef != null)
            {
                bool canAfford = GameManager.Instance.CanAfford(machineDef.cost);

                // Enable/disable the button based on affordability
                button.interactable = canAfford;

                // Visual feedback for unaffordable machines
                var canvasGroup = machineButton.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = machineButton.gameObject.AddComponent<CanvasGroup>();
                }

                // Reduce opacity for unaffordable machines
                canvasGroup.alpha = canAfford ? 1.0f : 0.5f;

                // Log affordability status
                if (!canAfford)
                {
                }
            }
        }
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