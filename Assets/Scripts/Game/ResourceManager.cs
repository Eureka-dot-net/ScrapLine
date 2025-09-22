using UnityEngine;
using System.Collections;

/// <summary>
/// Manages resource loading and initialization for the game.
/// Handles JSON loading, FactoryRegistry initialization, and UI manager setup.
/// </summary>
public class ResourceManager : MonoBehaviour
{
    [Header("Resource Loading")]
    [Tooltip("Enable debug logs for resource loading")]
    public bool enableResourceLogs = true;

    private MachineBarUIManager machineBarManager;
    private CreditsUI creditsUI;

    /// <summary>
    /// Initialize all resources and UI managers
    /// </summary>
    public void Initialize()
    {
        LoadFactoryData();
        InitializeUIManagers();
    }

    /// <summary>
    /// Load factory data from JSON resources
    /// </summary>
    private void LoadFactoryData()
    {
        TextAsset machinesAsset = Resources.Load<TextAsset>("machines");
        if (machinesAsset == null)
        {
            Debug.LogError("Failed to load machines.json resource!");
            return;
        }
        string machinesJson = machinesAsset.text;

        TextAsset recipesAsset = Resources.Load<TextAsset>("recipes");
        if (recipesAsset == null)
        {
            Debug.LogError("Failed to load recipes.json resource!");
            return;
        }
        string recipesJson = recipesAsset.text;

        TextAsset itemsAsset = Resources.Load<TextAsset>("items");
        if (itemsAsset == null)
        {
            Debug.LogError("Failed to load items.json resource!");
            return;
        }
        string itemsJson = itemsAsset.text;

        TextAsset wastecratesAsset = Resources.Load<TextAsset>("wastecrates");
        string wastecratesJson = null;
        if (wastecratesAsset != null)
        {
            wastecratesJson = wastecratesAsset.text;
        }
        else
        {
            if (enableResourceLogs)
                Debug.LogWarning("wastecrates.json resource not found - WasteCrates will be disabled");
        }

        FactoryRegistry.Instance.LoadFromJson(machinesJson, recipesJson, itemsJson, wastecratesJson);
    }

    /// <summary>
    /// Initialize UI managers
    /// </summary>
    private void InitializeUIManagers()
    {
        machineBarManager = FindFirstObjectByType<MachineBarUIManager>();
        if (machineBarManager != null)
        {
            machineBarManager.InitBar();
        }
        else
        {
            Debug.LogWarning("MachineBarUIManager not found in scene!");
        }

        creditsUI = FindFirstObjectByType<CreditsUI>();
        if (creditsUI == null)
        {
            Debug.LogWarning("CreditsUI not found in scene!");
        }
    }

    /// <summary>
    /// Wait for the FactoryRegistry to be fully loaded
    /// </summary>
    public IEnumerator WaitForFactoryRegistryLoaded()
    {
        yield return new WaitUntil(() => FactoryRegistry.Instance.IsLoaded());
    }

    /// <summary>
    /// Get the machine bar manager reference
    /// </summary>
    public MachineBarUIManager GetMachineBarManager()
    {
        return machineBarManager;
    }

    /// <summary>
    /// Get the credits UI reference
    /// </summary>
    public CreditsUI GetCreditsUI()
    {
        return creditsUI;
    }
}