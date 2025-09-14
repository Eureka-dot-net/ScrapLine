using UnityEngine;
using UnityEngine.UI;

public class MachineBarUIManager : MonoBehaviour
{
    public GameObject machineButtonPrefab;
    public Transform machineBarPanel;

    void Awake()
    {
        Debug.Log("Awake called on MachineBarUIManager");
    }

    public void InitBar()
    {
        Debug.Log("Initializing Machine Bar UI");
        foreach (var machine in FactoryRegistry.Instance.Machines.Values)
        {
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

            // Set explicit preferred sizes (square)
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
                machineRenderer.Setup(machine);
            }
            else
            {
                Debug.LogWarning($"MachineRenderer not found on prefab for machine '{machine.id}'");
            }
        }
    }

    private void OnMachinePanelClicked(MachineDef machineDef, GameObject buttonObj)
    {
        // Highlight allowed placement area
        // Store selected machine for placement
        // Update UI accordingly
        Debug.Log($"Selected machine: {machineDef.id}");
    }
}