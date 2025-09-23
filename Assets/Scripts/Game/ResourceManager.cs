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
            GameLogger.LogError(LoggingManager.LogCategory.Debug, "Failed to load machines.json resource!", ComponentId);
            return;
        }
        string machinesJson = machinesAsset.text;

        TextAsset recipesAsset = Resources.Load<TextAsset>("recipes");
        if (recipesAsset == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.Debug, "Failed to load recipes.json resource!", ComponentId);
            return;
        }
        string recipesJson = recipesAsset.text;

        TextAsset itemsAsset = Resources.Load<TextAsset>("items");
        if (itemsAsset == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.Debug, "Failed to load items.json resource!", ComponentId);
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
                GameLogger.LogWarning(LoggingManager.LogCategory.Debug, "wastecrates.json resource not found - WasteCrates will be disabled", ComponentId);
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
            GameLogger.LogWarning(LoggingManager.LogCategory.Debug, "MachineBarUIManager not found in scene!", ComponentId);
        }

        creditsUI = FindFirstObjectByType<CreditsUI>();
        if (creditsUI == null)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Debug, "CreditsUI not found in scene!", ComponentId);
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