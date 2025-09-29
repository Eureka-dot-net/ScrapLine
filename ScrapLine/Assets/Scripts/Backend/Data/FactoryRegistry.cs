using System.Collections.Generic;
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

    // Per-user machine progress
    public List<UserMachineProgress> UserMachines = new();

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
        return UserMachines.Find(mp => mp.machineId == machineId);
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

    public void UnlockMachine(string machineId)
    {
        var progress = FindMachineProgress(machineId);
        if (progress == null)
        {
            UserMachines.Add(new UserMachineProgress { machineId = machineId, unlocked = true, upgradeLevel = 0 });
        }
        else
        {
            progress.unlocked = true;
        }
    }

    public void UpgradeMachine(string machineId)
    {
        var progress = FindMachineProgress(machineId);
        if (progress != null && progress.unlocked)
            progress.upgradeLevel++;
    }

    public bool IsMachineUnlocked(string machineId)
    {
        var progress = FindMachineProgress(machineId);
        return progress != null && progress.unlocked;
    }

    public int GetMachineUpgradeLevel(string machineId)
    {
        var progress = FindMachineProgress(machineId);
        return progress != null ? progress.upgradeLevel : 0;
    }

    // --- Serialization Helpers for Save/Load ---
    public void LoadFromGameData(GameData data)
    {
        UserMachines = data.userMachineProgress ?? new List<UserMachineProgress>();
    }

    public void SaveToGameData(GameData data)
    {
        data.userMachineProgress = UserMachines;
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