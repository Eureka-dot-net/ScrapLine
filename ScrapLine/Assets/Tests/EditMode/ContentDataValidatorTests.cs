using System.Linq;
using NUnit.Framework;
using ScrapLine.Editor.ContentValidation;

namespace ScrapLine.Tests.EditMode
{
    public sealed class ContentDataValidatorTests
    {
        private const string ValidItems =
            "{\"items\":[{\"id\":\"ore\",\"displayName\":\"Ore\",\"sprite\":\"ore\",\"sellValue\":2}," +
            "{\"id\":\"refinedOre\",\"displayName\":\"Refined Ore\",\"sprite\":\"refined\",\"sellValue\":6}]}";
        private const string ValidMachines =
            "{\"machines\":[{\"id\":\"processor\",\"type\":\"Shredder\",\"baseProcessTime\":1," +
            "\"gridPlacement\":[\"grid\"],\"displayInPanel\":true,\"cost\":1," +
            "\"spawnableItems\":[\"ore\"],\"className\":\"ProcessorMachine\"}]}";
        private const string ValidRecipes =
            "[{\"machineId\":\"processor\",\"inputItems\":[{\"item\":\"ore\",\"count\":1}]," +
            "\"outputItems\":[{\"item\":\"refinedOre\",\"count\":1}],\"processMultiplier\":1}]";
        private const string ValidWasteCrates =
            "{\"wasteCrates\":[{\"id\":\"ore_crate\",\"displayName\":\"Ore Crate\",\"sprite\":\"crate\"," +
            "\"cost\":40,\"items\":[{\"itemType\":\"ore\",\"count\":25}]}]}";

        [Test]
        public void ValidDefinitionsPass()
        {
            ContentValidationResult result = Validate();

            Assert.That(result.Errors, Is.Empty,
                string.Join("\n", result.Errors.Select(error => error.ToString())));
        }

        [Test]
        public void DuplicateItemIdReportsFileAndId()
        {
            string items =
                "{\"items\":[{\"id\":\"ore\",\"displayName\":\"Ore\",\"sprite\":\"ore\",\"sellValue\":2}," +
                "{\"id\":\"ore\",\"displayName\":\"More Ore\",\"sprite\":\"ore2\",\"sellValue\":3}," +
                "{\"id\":\"refinedOre\",\"displayName\":\"Refined Ore\",\"sprite\":\"refined\",\"sellValue\":6}]}";

            ContentValidationResult result = Validate(items: items);

            Assert.That(result.Errors.Any(error =>
                error.File == ContentDataValidator.ItemsFile && error.Id == "ore" &&
                error.Message.Contains("unique")), Is.True);
        }

        [Test]
        public void InvalidMachineAndItemReferencesReportOwningFileAndId()
        {
            string machines = ValidMachines.Replace("\"ore\"]", "\"missing_spawn_item\"]");
            string recipes = ValidRecipes
                .Replace("\"processor\"", "\"missing_machine\"")
                .Replace("\"ore\",\"count\":1}],\"outputItems", "\"missing_input\",\"count\":1}],\"outputItems");

            ContentValidationResult result = Validate(machines: machines, recipes: recipes);

            Assert.That(result.Errors.Any(error =>
                error.File == ContentDataValidator.MachinesFile && error.Id == "processor" &&
                error.Message.Contains("missing_spawn_item")), Is.True);
            Assert.That(result.Errors.Any(error =>
                error.File == ContentDataValidator.RecipesFile && error.Id.Contains("missing_machine") &&
                error.Message.Contains("unknown machine")), Is.True);
            Assert.That(result.Errors.Any(error =>
                error.File == ContentDataValidator.RecipesFile && error.Id.Contains("missing_machine") &&
                error.Message.Contains("missing_input")), Is.True);
        }

        [Test]
        public void NonPositiveValuesAndEmptyCrateAreRejected()
        {
            string recipes = ValidRecipes.Replace("\"count\":1", "\"count\":0");
            string wasteCrates =
                "{\"wasteCrates\":[{\"id\":\"empty\",\"displayName\":\"Empty\",\"sprite\":\"crate\"," +
                "\"cost\":0,\"items\":[]}]}";

            ContentValidationResult result = Validate(recipes: recipes, wasteCrates: wasteCrates);

            Assert.That(result.Errors.Any(error => error.File == ContentDataValidator.RecipesFile &&
                                                    error.Message.Contains("count must be positive")), Is.True);
            Assert.That(result.Errors.Any(error => error.File == ContentDataValidator.WasteCratesFile &&
                                                    error.Id == "empty" && error.Message.Contains("cost must be positive")), Is.True);
            Assert.That(result.Errors.Any(error => error.File == ContentDataValidator.WasteCratesFile &&
                                                    error.Id == "empty" && error.Message.Contains("items must contain")), Is.True);
        }

        [Test]
        public void MissingRootCollectionIsRejected()
        {
            ContentValidationResult result = Validate(items: "{}");

            Assert.That(result.Errors.Any(error =>
                error.File == ContentDataValidator.ItemsFile && error.Message.Contains("'items'")), Is.True);
        }

        [Test]
        public void UnprofitableRecipeReportsInputAndOutputValues()
        {
            string recipes = ValidRecipes.Replace("refinedOre", "ore");

            ContentValidationResult result = Validate(recipes: recipes);

            Assert.That(result.Errors.Any(error =>
                error.File == ContentDataValidator.RecipesFile &&
                error.Message.Contains("output sale value (2)") && error.Message.Contains("input sale value (2)")), Is.True);
        }

        [Test]
        public void WasteCrateMustCostEightyPercentOfRawContents()
        {
            string wasteCrates = ValidWasteCrates.Replace("\"cost\":40", "\"cost\":20");

            ContentValidationResult result = Validate(wasteCrates: wasteCrates);

            Assert.That(result.Errors.Any(error =>
                error.File == ContentDataValidator.WasteCratesFile && error.Id == "ore_crate" &&
                error.Message.Contains("80%") && error.Message.Contains("was 20")), Is.True);
        }

        [Test]
        public void InvalidUpgradeCostsAndTimesAreRejected()
        {
            string machines = ValidMachines.Replace(
                "\"spawnableItems\":[\"ore\"]",
                "\"upgradeMultipliers\":[{\"multiplier\":0.5,\"cost\":-1,\"upgradeTime\":0}]," +
                "\"upgradeMaxNumbers\":[{\"max\":2,\"cost\":0}],\"spawnableItems\":[\"ore\"]");

            ContentValidationResult result = Validate(machines: machines);

            Assert.That(result.Errors.Any(error => error.File == ContentDataValidator.MachinesFile &&
                                                    error.Message.Contains("upgradeMultipliers[0].cost")), Is.True);
            Assert.That(result.Errors.Any(error => error.File == ContentDataValidator.MachinesFile &&
                                                    error.Message.Contains("upgradeMultipliers[0].upgradeTime")), Is.True);
            Assert.That(result.Errors.Any(error => error.File == ContentDataValidator.MachinesFile &&
                                                    error.Message.Contains("upgradeMaxNumbers[0].cost")), Is.True);
        }

        [Test]
        public void ProjectContentDefinitionsAreValid()
        {
            ContentValidationResult result = ContentDataValidator.ValidateProject();

            Assert.That(result.Errors, Is.Empty,
                "Project content is invalid:\n" + string.Join("\n", result.Errors.Select(error => error.ToString())));
        }

        private static ContentValidationResult Validate(
            string items = ValidItems,
            string machines = ValidMachines,
            string recipes = ValidRecipes,
            string wasteCrates = ValidWasteCrates)
        {
            return ContentDataValidator.Validate(new ContentDataSources(items, machines, recipes, wasteCrates));
        }
    }
}
