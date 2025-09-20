using System.Collections.Generic;
using UnityEngine;

public class FactoryRegistry
{
    // --- Singleton Pattern ---
    private static FactoryRegistry _instance;
    public static FactoryRegistry Instance => _instance ??= new FactoryRegistry();

    // --- Data Members ---
    public Dictionary<string, MachineDef> Machines = new();
    public List<RecipeDef> Recipes = new();
    public Dictionary<string, ItemDef> Items = new();

    // Per-user machine progress
    public List<UserMachineProgress> UserMachines = new();

    public bool IsLoaded()
    {
        // This is a simple check; you might want more robust logic
        return Machines.Count > 0 && Recipes.Count > 0 && Items.Count > 0;
    }

    // --- Methods ---

    /// <summary>
    /// Loads machine, recipe, and item definitions from JSON strings.
    /// Call this from GameManager when starting the game.
    /// </summary>
    public void LoadFromJson(string machinesJson, string recipesJson, string itemsJson)
    {
        // Load Machines
        var machinesWrapper = JsonUtility.FromJson<MachineListWrapper>(machinesJson);
        Machines.Clear();
        foreach (var m in machinesWrapper.machines)
            Machines[m.id] = m;

        // Load Recipes - handle direct array format
        try
        {
            Recipes = JsonUtility.FromJson<RecipeListWrapper>("{\"recipes\":" + recipesJson + "}").recipes ?? new List<RecipeDef>();
        }
        catch
        {
            Debug.LogWarning("Failed to load recipes from JSON, using empty list");
            Recipes = new List<RecipeDef>();
        }

        // Load Items
        var itemsWrapper = JsonUtility.FromJson<ItemListWrapper>(itemsJson);
        Items.Clear();
        foreach (var i in itemsWrapper.items)
            Items[i.id] = i;
    }

    [System.Serializable]
    private class MachineListWrapper { public List<MachineDef> machines; }
    [System.Serializable]
    private class RecipeListWrapper { public List<RecipeDef> recipes; }
    [System.Serializable]
    private class ItemListWrapper { public List<ItemDef> items; }

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
}