using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FactoryRegistry
{
    // --- Singleton Pattern ---
    private static FactoryRegistry _instance;
    public static FactoryRegistry Instance => _instance ??= new FactoryRegistry();

    /// <summary>
    /// Get the component ID for logging purposes
    /// </summary>
    private string ComponentId => $"FactoryRegistry_{GetHashCode()}";

    // --- Data Members ---
    public Dictionary<string, MachineDef> Machines = new();
    public List<RecipeDef> Recipes = new();
    public Dictionary<string, ItemDef> Items = new();
    public Dictionary<string, WasteCrateDef> WasteCrates = new();

    // Per-user machine progress. Snapshots are exposed so callers cannot bypass the unlock API.
    private List<UserMachineProgress> userMachines = new();
    public IReadOnlyList<UserMachineProgress> UserMachines => userMachines
        .Where(progress => progress != null)
        .Select(CloneProgress)
        .ToList();

    /// <summary>Raised exactly once when a machine transitions from locked to unlocked.</summary>
    public event Action<string, string> MachineUnlocked;

    /// <summary>Raised after save state is loaded so unlock-dependent UI can rebuild.</summary>
    public event Action MachineUnlockStateReloaded;

    public bool IsLoaded()
    {
        // This is a simple check; you might want more robust logic
        return Machines.Count > 0 && Recipes.Count > 0 && Items.Count > 0 && WasteCrates.Count > 0;
    }

    // --- Methods ---

    /// <summary>
    /// Loads machine, recipe, item, and wastecrate definitions from JSON strings.
    /// Call this from GameManager when starting the game.
    /// </summary>
    public void LoadFromJson(string machinesJson, string recipesJson, string itemsJson, string wastecratesJson = null, GridColorConfiguration colorConfig = null)
    {
        // Load Machines
        var machinesWrapper = JsonUtility.FromJson<MachineListWrapper>(machinesJson);
        Machines.Clear();
        foreach (var m in machinesWrapper.machines)
            Machines[m.id] = m;

        // Apply color configuration to blank machine definitions
        if (colorConfig != null)
        {
            ApplyColorConfiguration(colorConfig);
        }

        // Load Recipes - handle direct array format
        try
        {
            Recipes = JsonUtility.FromJson<RecipeListWrapper>("{\"recipes\":" + recipesJson + "}").recipes ?? new List<RecipeDef>();
        }
        catch
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Debug, "Failed to load recipes from JSON, using empty list", ComponentId);
            Recipes = new List<RecipeDef>();
        }

        // Load Items
        var itemsWrapper = JsonUtility.FromJson<ItemListWrapper>(itemsJson);
        Items.Clear();
        foreach (var i in itemsWrapper.items)
            Items[i.id] = i;
            
        // Load WasteCrates
        WasteCrates.Clear();
        if (!string.IsNullOrEmpty(wastecratesJson))
        {
            try
            {
                var wastecratesWrapper = JsonUtility.FromJson<WasteCrateListWrapper>(wastecratesJson);
                foreach (var wc in wastecratesWrapper.wasteCrates)
                    WasteCrates[wc.id] = wc;
            }
            catch
            {
                GameLogger.LogWarning(LoggingManager.LogCategory.Debug, "Failed to load wastecrates from JSON, using empty dictionary", ComponentId);
            }
        }
    }

    [System.Serializable]
    private class MachineListWrapper { public List<MachineDef> machines; }
    [System.Serializable]
    private class RecipeListWrapper { public List<RecipeDef> recipes; }
    [System.Serializable]
    private class ItemListWrapper { public List<ItemDef> items; }
    [System.Serializable]
    private class WasteCrateListWrapper { public List<WasteCrateDef> wasteCrates; }

    public UserMachineProgress FindMachineProgress(string machineId)
    {
        return CloneProgress(FindMutableMachineProgress(machineId));
    }

    private UserMachineProgress FindMutableMachineProgress(string machineId)
    {
        return userMachines.Find(progress => progress != null && progress.machineId == machineId);
    }

    public MachineDef GetMachine(string machineId)
    {
        Machines.TryGetValue(machineId, out var m);
        return m;
    }

    public ItemDef GetItem(string itemId)
    {
        Items.TryGetValue(itemId, out var i);
        return i;
    }
    
    public WasteCrateDef GetWasteCrate(string wasteCrateId)
    {
        WasteCrates.TryGetValue(wasteCrateId, out var wc);
        return wc;
    }
    
    /// <summary>
    /// Get all available waste crates
    /// </summary>
    /// <returns>List of all waste crate definitions</returns>
    public List<WasteCrateDef> GetAllWasteCrates()
    {
        return new List<WasteCrateDef>(WasteCrates.Values);
    }

    public RecipeDef GetRecipe(string machineId, string inputItemId)
    {
        foreach (var recipe in Recipes)
        {
            if (recipe.machineId == machineId)
            {
                // Check if any of the input items match the provided itemId
                foreach (var inputItem in recipe.inputItems)
                {
                    if (inputItem.item == inputItemId)
                        return recipe;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Get all recipes for a specific machine that produce a specific output item
    /// </summary>
    public List<RecipeDef> GetRecipesByOutput(string machineId, string outputItemId)
    {
        var matchingRecipes = new List<RecipeDef>();
        foreach (var recipe in Recipes)
        {
            if (recipe.machineId == machineId)
            {
                foreach (var outputItem in recipe.outputItems)
                {
                    if (outputItem.item == outputItemId)
                    {
                        matchingRecipes.Add(recipe);
                        break; // Found a match for this recipe, no need to check other outputs
                    }
                }
            }
        }
        return matchingRecipes;
    }

    /// <summary>
    /// Get all recipes for a specific machine
    /// </summary>
    public List<RecipeDef> GetRecipesForMachine(string machineId)
    {
        var machineRecipes = new List<RecipeDef>();
        foreach (var recipe in Recipes)
        {
            if (recipe.machineId == machineId)
            {
                machineRecipes.Add(recipe);
            }
        }
        return machineRecipes;
    }

    public const string CreditPurchaseUnlockSource = "credit_purchase";

    /// <summary>
    /// Purchases one permanent machine license as a single validated economy transaction.
    /// </summary>
    public bool TryPurchaseMachineLicense(string machineId, CreditsManager creditsManager, out string error)
    {
        if (!TryGetLockedMachine(machineId, out MachineDef machine, out error))
            return false;
        if (creditsManager == null)
        {
            error = $"Cannot purchase '{machineId}' without an initialized credits manager.";
            return false;
        }
        if (!creditsManager.CanAfford(machine.unlockCost))
        {
            error = $"Machine license '{machineId}' costs {machine.unlockCost} credits; " +
                    $"only {creditsManager.GetCredits()} are available.";
            return false;
        }
        if (!creditsManager.TrySpendCredits(machine.unlockCost))
        {
            error = $"Could not deduct {machine.unlockCost} credits for machine license '{machineId}'.";
            return false;
        }

        if (TryGrantMachineLicenseInternal(machineId, CreditPurchaseUnlockSource, out error))
            return true;

        // The state transition is expected to be infallible after prevalidation, but preserve the
        // economy transaction if a future grant rule introduces another failure mode.
        creditsManager.AddCredits(machine.unlockCost);
        return false;
    }

    /// <summary>
    /// Grants a permanent license without charging credits (objectives, migration, recovery, tools).
    /// </summary>
    public bool TryGrantMachineLicense(string machineId, string unlockSource, out string error)
    {
        return TryGrantMachineLicenseInternal(machineId, unlockSource, out error);
    }

    private bool TryGrantMachineLicenseInternal(string machineId, string unlockSource, out string error)
    {
        if (!TryGetLockedMachine(machineId, out _, out error))
            return false;
        if (string.IsNullOrWhiteSpace(unlockSource))
        {
            error = $"Cannot grant machine license '{machineId}' without an unlock source.";
            return false;
        }

        UserMachineProgress progress = FindMutableMachineProgress(machineId);
        if (progress == null)
        {
            userMachines.Add(new UserMachineProgress
            {
                machineId = machineId,
                unlocked = true,
                upgradeLevel = 0
            });
        }
        else
            progress.unlocked = true;

        error = null;
        MachineUnlocked?.Invoke(machineId, unlockSource);
        GameManager.Instance?.RequestAutosave();
        return true;
    }

    private bool TryGetLockedMachine(string machineId, out MachineDef machine, out string error)
    {
        machine = null;
        if (string.IsNullOrWhiteSpace(machineId) || !Machines.TryGetValue(machineId, out machine))
        {
            error = $"Unknown machine ID '{machineId ?? "<null>"}'.";
            return false;
        }
        if (IsMachineUnlocked(machineId))
        {
            error = $"Machine '{machineId}' is already licensed.";
            return false;
        }
        if (machine.unlockCost <= 0)
        {
            error = $"Machine '{machineId}' has invalid license cost {machine.unlockCost}.";
            return false;
        }
        error = null;
        return true;
    }

    public void UpgradeMachine(string machineId)
    {
        var progress = FindMutableMachineProgress(machineId);
        if (progress != null && progress.unlocked)
        {
            progress.upgradeLevel++;
            GameManager.Instance?.RequestAutosave();
        }
    }

    public bool IsMachineUnlocked(string machineId)
    {
        if (string.IsNullOrWhiteSpace(machineId) || !Machines.TryGetValue(machineId, out MachineDef machine))
            return false;
        if (machine.unlockedByDefault)
            return true;
        var progress = FindMutableMachineProgress(machineId);
        return progress != null && progress.unlocked;
    }

    public IReadOnlyList<MachineDef> GetPanelMachines()
    {
        return Machines.Values
            .Where(machine => machine.displayInPanel)
            .OrderBy(machine => machine.id, StringComparer.Ordinal)
            .ToList();
    }

    public int GetMachineUpgradeLevel(string machineId)
    {
        var progress = FindMutableMachineProgress(machineId);
        return progress != null ? progress.upgradeLevel : 0;
    }

    // --- Serialization Helpers for Save/Load ---
    public void LoadFromGameData(GameData data)
    {
        if (!TryLoadFromGameData(data, out string error))
            GameLogger.LogError(LoggingManager.LogCategory.SaveLoad, error, ComponentId);
    }

    public bool TryLoadFromGameData(GameData data, out string error)
    {
        if (!ValidateMachineProgress(data, out error))
            return false;

        MachineUnlockState.Normalize(data);
        userMachines = data.userMachineProgress;
        error = null;
        MachineUnlockStateReloaded?.Invoke();
        return true;
    }

    /// <summary>Validates a save candidate without mutating registry or game state.</summary>
    public bool ValidateMachineProgress(GameData data, out string error)
    {
        if (data == null)
        {
            error = "Cannot load machine unlocks from null game data.";
            return false;
        }

        if (data.userMachineProgress != null)
        {
            foreach (UserMachineProgress progress in data.userMachineProgress)
            {
                if (progress == null || string.IsNullOrWhiteSpace(progress.machineId))
                {
                    error = "Saved machine progress contains a missing machine ID.";
                    return false;
                }
                if (!Machines.ContainsKey(progress.machineId))
                {
                    error = $"Saved machine progress references unknown machine ID '{progress.machineId}'.";
                    return false;
                }
                if (progress.upgradeLevel < 0)
                {
                    error = $"Saved machine progress for '{progress.machineId}' has a negative upgrade level.";
                    return false;
                }
            }
        }

        error = null;
        return true;
    }

    public void SaveToGameData(GameData data)
    {
        data.userMachineProgress = userMachines
            .Where(progress => progress != null)
            .Select(CloneProgress)
            .ToList();
        MachineUnlockState.Normalize(data);
    }

    private static UserMachineProgress CloneProgress(UserMachineProgress progress)
    {
        if (progress == null)
            return null;
        return new UserMachineProgress
        {
            machineId = progress.machineId,
            unlocked = progress.unlocked,
            upgradeLevel = progress.upgradeLevel
        };
    }

    /// <summary>
    /// Apply color configuration to blank machine definitions for grid cell coloring
    /// </summary>
    /// <param name="colorConfig">The color configuration to apply</param>
    private void ApplyColorConfiguration(GridColorConfiguration colorConfig)
    {
        // Apply top row color (pink/red area for sellers)
        if (Machines.TryGetValue("blank_top", out var topMachine))
        {
            topMachine.borderColor = colorConfig.GetTopRowHexColor();
            GameLogger.Log(LoggingManager.LogCategory.Grid, $"Applied top row color: {topMachine.borderColor}", ComponentId);
        }

        // Apply grid color (middle grey area) - we leave this as default by not setting borderColor
        if (Machines.TryGetValue("blank", out var gridMachine))
        {
            gridMachine.borderColor = colorConfig.GetGridHexColor(); // This returns null for default
            GameLogger.Log(LoggingManager.LogCategory.Grid, $"Applied grid color: {gridMachine.borderColor ?? "default"}", ComponentId);
        }

        // Apply bottom row color (green area for spawners)
        if (Machines.TryGetValue("blank_bottom", out var bottomMachine))
        {
            bottomMachine.borderColor = colorConfig.GetBottomRowHexColor();
            GameLogger.Log(LoggingManager.LogCategory.Grid, $"Applied bottom row color: {bottomMachine.borderColor}", ComponentId);
        }
    }
}
