using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ScrapLine.Editor.ContentValidation
{
    public sealed class ContentDataSources
    {
        public ContentDataSources(string items, string machines, string recipes, string wasteCrates)
        {
            Items = items;
            Machines = machines;
            Recipes = recipes;
            WasteCrates = wasteCrates;
        }

        public string Items { get; }
        public string Machines { get; }
        public string Recipes { get; }
        public string WasteCrates { get; }
    }

    public sealed class ContentValidationError
    {
        public ContentValidationError(string file, string id, string message)
        {
            File = file;
            Id = id;
            Message = message;
        }

        public string File { get; }
        public string Id { get; }
        public string Message { get; }

        public override string ToString()
        {
            string location = string.IsNullOrWhiteSpace(Id) ? File : $"{File} [id: {Id}]";
            return $"{location}: {Message}";
        }
    }

    public sealed class ContentValidationResult
    {
        private readonly List<ContentValidationError> errors = new List<ContentValidationError>();

        public IReadOnlyList<ContentValidationError> Errors => errors;
        public bool IsValid => errors.Count == 0;

        internal void Add(string file, string id, string message)
        {
            errors.Add(new ContentValidationError(file, id, message));
        }
    }

    public static class ContentDataValidator
    {
        public const string ItemsFile = "Assets/Resources/items.json";
        public const string MachinesFile = "Assets/Resources/machines.json";
        public const string RecipesFile = "Assets/Resources/recipes.json";
        public const string WasteCratesFile = "Assets/Resources/wastecrates.json";

        public static ContentValidationResult ValidateProject()
        {
            string assetsPath = Application.dataPath;
            return Validate(new ContentDataSources(
                ReadProjectFile(assetsPath, "Resources/items.json"),
                ReadProjectFile(assetsPath, "Resources/machines.json"),
                ReadProjectFile(assetsPath, "Resources/recipes.json"),
                ReadProjectFile(assetsPath, "Resources/wastecrates.json")));
        }

        public static ContentValidationResult Validate(ContentDataSources sources)
        {
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));

            ContentValidationResult result = new ContentValidationResult();
            ItemList items = Parse<ItemList>(sources.Items, ItemsFile, result);
            MachineList machines = Parse<MachineList>(sources.Machines, MachinesFile, result);
            RecipeList recipes = Parse<RecipeList>(WrapArray("recipes", sources.Recipes), RecipesFile, result);
            WasteCrateList wasteCrates = Parse<WasteCrateList>(sources.WasteCrates, WasteCratesFile, result);

            RequireCollection(items?.items, "items", ItemsFile, items != null, result);
            RequireCollection(machines?.machines, "machines", MachinesFile, machines != null, result);
            RequireCollection(recipes?.recipes, "recipes", RecipesFile, recipes != null, result);
            RequireCollection(wasteCrates?.wasteCrates, "wasteCrates", WasteCratesFile, wasteCrates != null, result);

            List<ItemData> itemData = items?.items ?? new List<ItemData>();
            List<MachineData> machineData = machines?.machines ?? new List<MachineData>();
            List<RecipeData> recipeData = recipes?.recipes ?? new List<RecipeData>();
            List<WasteCrateData> wasteCrateData = wasteCrates?.wasteCrates ?? new List<WasteCrateData>();

            ValidateItems(itemData, result);
            ValidateMachines(machineData, result);
            ValidateSpawnableItems(machineData, itemData, result);
            ValidateRecipes(recipeData, machineData, itemData, result);
            ValidateWasteCrates(wasteCrateData, itemData, result);
            ValidateRecipeEconomy(recipeData, itemData, result);
            ValidateWasteCrateEconomy(wasteCrateData, itemData, result);
            return result;
        }

        private static void ValidateItems(IReadOnlyList<ItemData> items, ContentValidationResult result)
        {
            ValidateUniqueIds(items.Select(item => item?.id), ItemsFile, result);
            for (int index = 0; index < items.Count; index++)
            {
                ItemData item = items[index];
                string id = RecordId(item?.id, index);
                if (item == null)
                {
                    result.Add(ItemsFile, id, "item definition is null.");
                    continue;
                }

                Require(item.id, "id", ItemsFile, id, result);
                Require(item.displayName, "displayName", ItemsFile, id, result);
                Require(item.sprite, "sprite", ItemsFile, id, result);
                Positive(item.sellValue, "sellValue", ItemsFile, id, result);
            }
        }

        private static void ValidateMachines(IReadOnlyList<MachineData> machines, ContentValidationResult result)
        {
            ValidateUniqueIds(machines.Select(machine => machine?.id), MachinesFile, result);
            for (int index = 0; index < machines.Count; index++)
            {
                MachineData machine = machines[index];
                string id = RecordId(machine?.id, index);
                if (machine == null)
                {
                    result.Add(MachinesFile, id, "machine definition is null.");
                    continue;
                }

                Require(machine.id, "id", MachinesFile, id, result);
                Require(machine.type, "type", MachinesFile, id, result);
                Require(machine.className, "className", MachinesFile, id, result);
                if (machine.gridPlacement == null || machine.gridPlacement.Count == 0 ||
                    machine.gridPlacement.Any(string.IsNullOrWhiteSpace))
                    result.Add(MachinesFile, id, "gridPlacement must contain at least one non-empty value.");

                if (machine.displayInPanel)
                {
                    Positive(machine.cost, "cost", MachinesFile, id, result);
                    if (machine.unlockedByDefault && machine.unlockCost != 0)
                        result.Add(MachinesFile, id, "unlockCost must be zero when unlockedByDefault is true.");
                    else if (!machine.unlockedByDefault)
                        Positive(machine.unlockCost, "unlockCost", MachinesFile, id, result);
                }
                else if (machine.cost < 0)
                    result.Add(MachinesFile, id, "cost must not be negative.");
                else if (machine.unlockCost != 0)
                    result.Add(MachinesFile, id, "hidden machines must have an unlockCost of zero.");

                if (RequiresProcessTime(machine.type))
                    Positive(machine.baseProcessTime, "baseProcessTime", MachinesFile, id, result);
                else if (machine.baseProcessTime < 0f)
                    result.Add(MachinesFile, id, "baseProcessTime must not be negative.");

                ValidateMachineUpgrades(machine, id, result);
            }
        }

        private static void ValidateMachineUpgrades(
            MachineData machine,
            string machineId,
            ContentValidationResult result)
        {
            if (machine.upgradeMultipliers != null)
            {
                for (int index = 0; index < machine.upgradeMultipliers.Count; index++)
                {
                    UpgradeMultiplierData upgrade = machine.upgradeMultipliers[index];
                    if (upgrade == null)
                    {
                        result.Add(MachinesFile, machineId, $"upgradeMultipliers[{index}] is null.");
                        continue;
                    }
                    Positive(upgrade.multiplier, $"upgradeMultipliers[{index}].multiplier", MachinesFile, machineId, result);
                    Positive(upgrade.cost, $"upgradeMultipliers[{index}].cost", MachinesFile, machineId, result);
                    if (upgrade.upgradeTime >= 0f)
                        Positive(upgrade.upgradeTime, $"upgradeMultipliers[{index}].upgradeTime", MachinesFile, machineId, result);
                }
            }

            if (machine.upgradeMaxNumbers != null)
            {
                for (int index = 0; index < machine.upgradeMaxNumbers.Count; index++)
                {
                    UpgradeMaxNumberData upgrade = machine.upgradeMaxNumbers[index];
                    if (upgrade == null)
                    {
                        result.Add(MachinesFile, machineId, $"upgradeMaxNumbers[{index}] is null.");
                        continue;
                    }
                    Positive(upgrade.max, $"upgradeMaxNumbers[{index}].max", MachinesFile, machineId, result);
                    Positive(upgrade.cost, $"upgradeMaxNumbers[{index}].cost", MachinesFile, machineId, result);
                }
            }
        }

        private static void ValidateRecipes(
            IReadOnlyList<RecipeData> recipes,
            IReadOnlyList<MachineData> machines,
            IReadOnlyList<ItemData> items,
            ContentValidationResult result)
        {
            HashSet<string> machineIds = IdSet(machines.Select(machine => machine?.id));
            HashSet<string> itemIds = IdSet(items.Select(item => item?.id));
            ValidateUniqueIds(recipes.Select(recipe => recipe?.id), RecipesFile, result);

            for (int index = 0; index < recipes.Count; index++)
            {
                RecipeData recipe = recipes[index];
                string id = recipe == null ? $"index {index}" : RecipeId(recipe, index);
                if (recipe == null)
                {
                    result.Add(RecipesFile, id, "recipe definition is null.");
                    continue;
                }

                Require(recipe.id, "id", RecipesFile, id, result);
                Require(recipe.machineId, "machineId", RecipesFile, id, result);
                if (!string.IsNullOrWhiteSpace(recipe.machineId) && !machineIds.Contains(recipe.machineId))
                    result.Add(RecipesFile, id, $"machineId references unknown machine '{recipe.machineId}'.");
                Positive(recipe.processMultiplier, "processMultiplier", RecipesFile, id, result);
                ValidateRecipeItems(recipe.inputItems, "inputItems", id, itemIds, result);
                ValidateRecipeItems(recipe.outputItems, "outputItems", id, itemIds, result);
            }
        }

        private static void ValidateWasteCrates(
            IReadOnlyList<WasteCrateData> wasteCrates,
            IReadOnlyList<ItemData> items,
            ContentValidationResult result)
        {
            ValidateUniqueIds(wasteCrates.Select(crate => crate?.id), WasteCratesFile, result);
            HashSet<string> itemIds = IdSet(items.Select(item => item?.id));

            for (int index = 0; index < wasteCrates.Count; index++)
            {
                WasteCrateData crate = wasteCrates[index];
                string id = RecordId(crate?.id, index);
                if (crate == null)
                {
                    result.Add(WasteCratesFile, id, "waste crate definition is null.");
                    continue;
                }

                Require(crate.id, "id", WasteCratesFile, id, result);
                Require(crate.displayName, "displayName", WasteCratesFile, id, result);
                Require(crate.sprite, "sprite", WasteCratesFile, id, result);
                Positive(crate.cost, "cost", WasteCratesFile, id, result);
                if (crate.items == null || crate.items.Count == 0)
                {
                    result.Add(WasteCratesFile, id, "items must contain at least one entry.");
                    continue;
                }

                for (int itemIndex = 0; itemIndex < crate.items.Count; itemIndex++)
                {
                    WasteCrateItemData item = crate.items[itemIndex];
                    if (item == null)
                    {
                        result.Add(WasteCratesFile, id, $"items[{itemIndex}] is null.");
                        continue;
                    }

                    Require(item.itemType, $"items[{itemIndex}].itemType", WasteCratesFile, id, result);
                    if (!string.IsNullOrWhiteSpace(item.itemType) && !itemIds.Contains(item.itemType))
                        result.Add(WasteCratesFile, id,
                            $"items[{itemIndex}].itemType references unknown item '{item.itemType}'.");
                    Positive(item.count, $"items[{itemIndex}].count", WasteCratesFile, id, result);
                }
            }
        }

        private static void ValidateRecipeItems(
            IReadOnlyList<RecipeItemData> entries,
            string field,
            string recipeId,
            HashSet<string> itemIds,
            ContentValidationResult result)
        {
            if (entries == null || entries.Count == 0)
            {
                result.Add(RecipesFile, recipeId, $"{field} must contain at least one entry.");
                return;
            }

            for (int index = 0; index < entries.Count; index++)
            {
                RecipeItemData entry = entries[index];
                if (entry == null)
                {
                    result.Add(RecipesFile, recipeId, $"{field}[{index}] is null.");
                    continue;
                }

                Require(entry.item, $"{field}[{index}].item", RecipesFile, recipeId, result);
                if (!string.IsNullOrWhiteSpace(entry.item) && !itemIds.Contains(entry.item))
                    result.Add(RecipesFile, recipeId,
                        $"{field}[{index}].item references unknown item '{entry.item}'.");
                Positive(entry.count, $"{field}[{index}].count", RecipesFile, recipeId, result);
            }
        }

        private static void ValidateSpawnableItems(
            IReadOnlyList<MachineData> machines,
            IReadOnlyList<ItemData> items,
            ContentValidationResult result)
        {
            HashSet<string> itemIds = IdSet(items.Select(item => item?.id));
            foreach (MachineData machine in machines.Where(machine => machine != null))
            {
                if (machine.spawnableItems == null)
                    continue;
                for (int index = 0; index < machine.spawnableItems.Count; index++)
                {
                    string itemId = machine.spawnableItems[index];
                    if (string.IsNullOrWhiteSpace(itemId))
                        result.Add(MachinesFile, machine.id, $"spawnableItems[{index}] is required.");
                    else if (!itemIds.Contains(itemId))
                        result.Add(MachinesFile, machine.id,
                            $"spawnableItems[{index}] references unknown item '{itemId}'.");
                }
            }
        }

        private static void ValidateRecipeEconomy(
            IReadOnlyList<RecipeData> recipes,
            IReadOnlyList<ItemData> items,
            ContentValidationResult result)
        {
            Dictionary<string, int> itemValues = ItemValueMap(items);
            for (int index = 0; index < recipes.Count; index++)
            {
                RecipeData recipe = recipes[index];
                if (recipe == null ||
                    !TryCalculateRecipeValue(recipe.inputItems, itemValues, out int inputValue) ||
                    !TryCalculateRecipeValue(recipe.outputItems, itemValues, out int outputValue))
                    continue;

                if (outputValue <= inputValue)
                {
                    result.Add(RecipesFile, RecipeId(recipe, index),
                        $"output sale value ({outputValue}) must exceed input sale value ({inputValue}).");
                }
            }
        }

        private static void ValidateWasteCrateEconomy(
            IReadOnlyList<WasteCrateData> wasteCrates,
            IReadOnlyList<ItemData> items,
            ContentValidationResult result)
        {
            Dictionary<string, int> itemValues = ItemValueMap(items);
            foreach (WasteCrateData crate in wasteCrates.Where(crate => crate != null))
            {
                if (!TryCalculateCrateValue(crate.items, itemValues, out int rawSaleValue))
                    continue;

                int expectedCost = Mathf.RoundToInt(rawSaleValue * 0.8f);
                if (crate.cost != expectedCost)
                {
                    result.Add(WasteCratesFile, crate.id,
                        $"cost must be 80% of raw contents value ({expectedCost} for contents worth {rawSaleValue}, was {crate.cost}).");
                }
            }
        }

        private static Dictionary<string, int> ItemValueMap(IEnumerable<ItemData> items)
        {
            Dictionary<string, int> values = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (ItemData item in items.Where(item => item != null && !string.IsNullOrWhiteSpace(item.id)))
            {
                if (!values.ContainsKey(item.id))
                    values.Add(item.id, item.sellValue);
            }
            return values;
        }

        private static bool TryCalculateRecipeValue(
            IReadOnlyList<RecipeItemData> entries,
            IReadOnlyDictionary<string, int> itemValues,
            out int value)
        {
            value = 0;
            if (entries == null || entries.Count == 0)
                return false;
            foreach (RecipeItemData entry in entries)
            {
                if (entry == null || entry.count <= 0 || !itemValues.TryGetValue(entry.item ?? "", out int itemValue))
                    return false;
                value += itemValue * entry.count;
            }
            return true;
        }

        private static bool TryCalculateCrateValue(
            IReadOnlyList<WasteCrateItemData> entries,
            IReadOnlyDictionary<string, int> itemValues,
            out int value)
        {
            value = 0;
            if (entries == null || entries.Count == 0)
                return false;
            foreach (WasteCrateItemData entry in entries)
            {
                if (entry == null || entry.count <= 0 ||
                    !itemValues.TryGetValue(entry.itemType ?? "", out int itemValue))
                    return false;
                value += itemValue * entry.count;
            }
            return true;
        }

        private static void ValidateUniqueIds(
            IEnumerable<string> ids,
            string file,
            ContentValidationResult result)
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in ids.Where(id => !string.IsNullOrWhiteSpace(id)))
            {
                if (!seen.Add(id))
                    result.Add(file, id, "id must be unique.");
            }
        }

        private static void RequireCollection<T>(
            IReadOnlyCollection<T> collection,
            string field,
            string file,
            bool parsed,
            ContentValidationResult result)
        {
            if (!parsed)
                return;
            if (collection == null)
                result.Add(file, null, $"root object must contain a '{field}' array.");
            else if (collection.Count == 0)
                result.Add(file, null, $"'{field}' must contain at least one definition.");
        }

        private static T Parse<T>(string json, string file, ContentValidationResult result) where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                result.Add(file, null, "file is empty.");
                return null;
            }

            try
            {
                T parsed = JsonUtility.FromJson<T>(json);
                if (parsed == null)
                    result.Add(file, null, "JSON did not contain the expected root object.");
                return parsed;
            }
            catch (Exception exception)
            {
                result.Add(file, null, $"invalid JSON ({exception.Message}).");
                return null;
            }
        }

        private static string ReadProjectFile(string assetsPath, string relativePath)
        {
            string path = Path.Combine(assetsPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        private static string WrapArray(string property, string json)
        {
            return string.IsNullOrWhiteSpace(json) ? json : $"{{\"{property}\":{json}}}";
        }

        private static HashSet<string> IdSet(IEnumerable<string> ids)
        {
            return new HashSet<string>(ids.Where(id => !string.IsNullOrWhiteSpace(id)), StringComparer.Ordinal);
        }

        private static bool RequiresProcessTime(string machineType)
        {
            return string.Equals(machineType, "Spawner", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(machineType, "Shredder", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(machineType, "PlatePress", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(machineType, "Granulator", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(machineType, "Fabricator", StringComparison.OrdinalIgnoreCase);
        }

        private static string RecordId(string id, int index)
        {
            return string.IsNullOrWhiteSpace(id) ? $"index {index}" : id;
        }

        private static string RecipeId(RecipeData recipe, int index)
        {
            return RecordId(recipe.id, index);
        }

        private static void Require(
            string value,
            string field,
            string file,
            string id,
            ContentValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(value))
                result.Add(file, id, $"{field} is required.");
        }

        private static void Positive(int value, string field, string file, string id, ContentValidationResult result)
        {
            if (value <= 0)
                result.Add(file, id, $"{field} must be positive (was {value}).");
        }

        private static void Positive(float value, string field, string file, string id, ContentValidationResult result)
        {
            if (value <= 0f || float.IsNaN(value) || float.IsInfinity(value))
                result.Add(file, id, $"{field} must be a finite positive number (was {value}).");
        }

        [Serializable]
        private sealed class ItemList { public List<ItemData> items; }
        [Serializable]
        private sealed class ItemData
        {
            public string id;
            public string displayName;
            public string sprite;
            public int sellValue;
        }

        [Serializable]
        private sealed class MachineList { public List<MachineData> machines; }
        [Serializable]
        private sealed class MachineData
        {
            public string id;
            public string type;
            public float baseProcessTime;
            public List<UpgradeMultiplierData> upgradeMultipliers;
            public List<UpgradeMaxNumberData> upgradeMaxNumbers;
            public List<string> gridPlacement;
            public bool displayInPanel = true;
            public bool unlockedByDefault;
            public int unlockCost;
            public int cost;
            public List<string> spawnableItems;
            public string className;
        }
        [Serializable]
        private sealed class UpgradeMultiplierData
        {
            public float multiplier;
            public int cost;
            public float upgradeTime = -1f;
        }
        [Serializable]
        private sealed class UpgradeMaxNumberData
        {
            public int max;
            public int cost;
        }

        [Serializable]
        private sealed class RecipeList { public List<RecipeData> recipes; }
        [Serializable]
        private sealed class RecipeData
        {
            public string id;
            public string machineId;
            public List<RecipeItemData> inputItems;
            public List<RecipeItemData> outputItems;
            public float processMultiplier;
        }
        [Serializable]
        private sealed class RecipeItemData
        {
            public string item;
            public int count;
        }

        [Serializable]
        private sealed class WasteCrateList { public List<WasteCrateData> wasteCrates; }
        [Serializable]
        private sealed class WasteCrateData
        {
            public string id;
            public string displayName;
            public string sprite;
            public List<WasteCrateItemData> items;
            public int cost;
        }
        [Serializable]
        private sealed class WasteCrateItemData
        {
            public string itemType;
            public int count;
        }
    }
}
