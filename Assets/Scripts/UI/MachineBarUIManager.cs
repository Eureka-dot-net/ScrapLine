using UnityEngine;
using UnityEngine.UI;

public class MachineBarUIManager : MonoBehaviour
{
    public GameObject machineButtonPrefab;
    public Transform machineBarPanel;

    // --- ADD THESE FIELDS ---
    public Texture conveyorPanelTexture;
    
    // Selection state
    private MachineDef selectedMachine;
    private GameObject selectedButtonObj;
    
    // Reference to grid manager for highlighting
    private UIGridManager gridManager;

    void Awake()
    {
        Debug.Log("Awake called on MachineBarUIManager");
    }

    void Start()
    {
        // Get reference to grid manager
        gridManager = FindAnyObjectByType<UIGridManager>();
    }

    public void InitBar()
    {
        Debug.Log("Initializing Machine Bar UI");
        foreach (var machine in FactoryRegistry.Instance.Machines.Values)
        {
            // Skip machines that shouldn't be displayed in panel
            if (!machine.displayInPanel)
            {
                Debug.Log($"Skipping machine '{machine.id}' - displayInPanel is false");
                continue;
            }
            
            Debug.Log($"Creating machine: {machine.id}");
            GameObject buttonObj = Instantiate(machineButtonPrefab, machineBarPanel);

            RectTransform parentRect = machineBarPanel.GetComponent<RectTransform>();
            float targetSize = parentRect.rect.height; // Use parent height for square size

            // Set fixed size instead of flexible + aspect ratio
            LayoutElement layoutElement = buttonObj.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = buttonObj.AddComponent<LayoutElement>();
            }
            layoutElement.preferredWidth = targetSize;
            layoutElement.preferredHeight = targetSize;

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
                Debug.LogWarning($"MachineRenderer not found on prefab for machine '{machine.id}'");
            }
        }
    }

    private void OnMachinePanelClicked(MachineDef machineDef, GameObject buttonObj)
    {
        Debug.Log($"Selected machine: {machineDef.id}");
        
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
            Debug.LogWarning("GameManager.Instance is null, cannot update machine affordability");
            return;
        }
        
        // Find all machine buttons in the panel
        MachineButton[] machineButtons = machineBarPanel.GetComponentsInChildren<MachineButton>();
        
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
                    Debug.Log($"Machine {machineDef.id} disabled - costs {machineDef.cost}, player has {GameManager.Instance.GetCredits()} credits");
                }
            }
        }
    }
}