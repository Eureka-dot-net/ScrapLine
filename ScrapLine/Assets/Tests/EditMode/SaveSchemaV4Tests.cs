using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ScrapLine.Tests.EditMode
{
    /// <summary>
    /// Covers the schema 4 save shape: the default factory identity, wall-clock anchors, the shared
    /// warehouse, ship blueprint progress, lifetime counters, the immutable launch result, and the
    /// configurable factory site plan.
    /// </summary>
    public sealed class SaveSchemaV4Tests
    {
        private readonly List<string> temporaryDirectories = new List<string>();

        [TearDown]
        public void TearDown()
        {
            // Site configuration is process-wide static state; a test that overrides it must not
            // leak that override into the rest of the suite.
            ProductionType("FactorySiteConfiguration")
                .GetMethod("ResetToDefaults", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, null);

            foreach (string directory in temporaryDirectories)
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
            temporaryDirectories.Clear();
        }

        // -----------------------------------------------------------------------------------
        // Migration
        // -----------------------------------------------------------------------------------

        [Test]
        public void Version3SaveAdoptsDefaultFactoryIdAndSeedsEveryNewStateObject()
        {
            object storage = CreateStorage();
            File.WriteAllText(PathProperty(storage, "PrimaryPath"), Version3Json);

            Assert.That(TryLoad(storage, out object data, out string error), Is.True, error);

            Assert.That(Field<int>(data, "schemaVersion"), Is.EqualTo(CurrentSchemaVersion));
            Assert.That(Field<int>(data, "credits"), Is.EqualTo(940));

            object grid = ((IList)GetField(data, "grids"))[0];
            Assert.That(Field<string>(grid, "factoryId"), Is.EqualTo(DefaultFactoryId));
            Assert.That(Field<int>(grid, "siteIndex"), Is.EqualTo(0));
            Assert.That(Field<string>(grid, "displayName"), Is.Not.Empty);

            Assert.That(GetField(data, "warehouse"), Is.Not.Null);
            Assert.That(GetField(data, "shipBlueprint"), Is.Not.Null);
            Assert.That(GetField(data, "lifetimeStats"), Is.Not.Null);
            Assert.That(GetField(data, "launchResult"), Is.Not.Null);

            // A pre-v4 save has no trustworthy wall-clock reading, so no offline time is implied.
            Assert.That(Field<bool>(data, "hasUtcClockAnchor"), Is.False);
            Assert.That(Field<long>(data, "savedAtUtcTicks"), Is.EqualTo(0L));

            // The runtime anchor that the live simulation needs is untouched by the new fields.
            Assert.That(Field<bool>(data, "hasRuntimeClockAnchor"), Is.True);
            Assert.That(Field<float>(data, "savedAtRuntimeTime"), Is.EqualTo(120f));
        }

        [Test]
        public void ExistingGridKeepsItsFactoryIdWhenMigrationRunsAgain()
        {
            object data = ParseGameData(Version3Json);
            object once = Migrate(data);
            string firstJson = JsonUtility.ToJson(once);
            string secondJson = JsonUtility.ToJson(Migrate(once));

            Assert.That(secondJson, Is.EqualTo(firstJson));
            IList grids = (IList)GetField(once, "grids");
            Assert.That(grids.Count, Is.EqualTo(1));
            Assert.That(Field<string>(grids[0], "factoryId"), Is.EqualTo(DefaultFactoryId));
        }

        [Test]
        public void UnversionedSaveReachesSchemaFourWithAFactoryIdentity()
        {
            object storage = CreateStorage();
            File.WriteAllText(PathProperty(storage, "PrimaryPath"),
                "{\"credits\":5,\"grids\":[{\"width\":1,\"height\":1,\"cells\":[{" +
                "\"x\":0,\"y\":0,\"machineDefId\":\"blank\"}]}]}");

            Assert.That(TryLoad(storage, out object data, out string error), Is.True, error);

            Assert.That(Field<int>(data, "schemaVersion"), Is.EqualTo(CurrentSchemaVersion));
            object grid = ((IList)GetField(data, "grids"))[0];
            Assert.That(Field<string>(grid, "factoryId"), Is.EqualTo(DefaultFactoryId));
        }

        [Test]
        public void CurrentSchemaSaveWithMissingOptionalStateLoadsWithUsableDefaults()
        {
            object storage = CreateStorage();
            File.WriteAllText(PathProperty(storage, "PrimaryPath"),
                "{\"schemaVersion\":4,\"credits\":5," + MinimalGridJson + "}");

            Assert.That(TryLoad(storage, out object data, out string error), Is.True, error);
            Assert.That(((IList)GetField(GetField(data, "warehouse"), "slots")).Count, Is.Zero);
            Assert.That(Field<long>(GetField(data, "warehouse"), "capacityLimit"), Is.EqualTo(-1));
            Assert.That(((IList)GetField(GetField(data, "shipBlueprint"), "modules")).Count, Is.Zero);
            Assert.That(((IList)GetField(GetField(data, "lifetimeStats"), "counters")).Count, Is.Zero);
            Assert.That(Field<bool>(GetField(data, "launchResult"), "recorded"), Is.False);
            Assert.That(Field<bool>(data, "hasUtcClockAnchor"), Is.False);
        }

        // -----------------------------------------------------------------------------------
        // Normalization rules
        // -----------------------------------------------------------------------------------

        [Test]
        public void DuplicateQuantitiesMergeAndNegativeQuantitiesClampToZero()
        {
            object data = ParseGameData(
                "{\"schemaVersion\":4,\"credits\":1," + MinimalGridJson + "," +
                "\"warehouse\":{\"capacityLimit\":-7,\"slots\":[" +
                "{\"itemId\":\"can\",\"count\":5},{\"itemId\":\"can\",\"count\":7}," +
                "{\"itemId\":\"plate\",\"count\":-4},{\"itemId\":\"  \",\"count\":9}]}," +
                "\"lifetimeStats\":{\"counters\":[{\"id\":\"items_sold\",\"value\":-3}," +
                "{\"id\":\"items_sold\",\"value\":10}]}}");

            Migrate(data);

            object warehouse = GetField(data, "warehouse");
            IList slots = (IList)GetField(warehouse, "slots");
            Assert.That(slots.Count, Is.EqualTo(2), "the blank item ID should have been dropped");
            Assert.That(Field<long>(FindByStringField(slots, "itemId", "can"), "count"), Is.EqualTo(12L));
            Assert.That(Field<long>(FindByStringField(slots, "itemId", "plate"), "count"), Is.EqualTo(0L));

            // Every negative capacity means the same thing and collapses to one sentinel.
            Assert.That(Field<long>(warehouse, "capacityLimit"), Is.EqualTo(-1L));

            IList counters = (IList)GetField(GetField(data, "lifetimeStats"), "counters");
            Assert.That(counters.Count, Is.EqualTo(1));
            Assert.That(Field<long>(counters[0], "value"), Is.EqualTo(10L),
                "the negative reading clamps to zero before the merge");
        }

        [Test]
        public void ModuleContributionsMergeAndMalformedModulesAreDropped()
        {
            object data = ParseGameData(
                "{\"schemaVersion\":4,\"credits\":1," + MinimalGridJson + "," +
                "\"shipBlueprint\":{\"unlocked\":true,\"modules\":[" +
                "{\"moduleId\":\"hull\",\"contributions\":[" +
                "{\"itemId\":\"panel\",\"contributed\":3},{\"itemId\":\"panel\",\"contributed\":4}]}," +
                "{\"moduleId\":\"  \",\"contributions\":[]}," +
                "{\"moduleId\":\"drive\",\"completed\":false,\"completedUtcTicks\":99}]}}");

            Migrate(data);

            IList modules = (IList)GetField(GetField(data, "shipBlueprint"), "modules");
            Assert.That(modules.Count, Is.EqualTo(2), "the unnamed module should have been dropped");

            object hull = FindByStringField(modules, "moduleId", "hull");
            IList contributions = (IList)GetField(hull, "contributions");
            Assert.That(contributions.Count, Is.EqualTo(1));
            Assert.That(Field<long>(contributions[0], "contributed"), Is.EqualTo(7L));

            object drive = FindByStringField(modules, "moduleId", "drive");
            Assert.That(Field<long>(drive, "completedUtcTicks"), Is.EqualTo(0L),
                "an incomplete module must not carry a completion time");
        }

        [Test]
        public void ImpossibleTimestampsClampAndTheHighWaterMarkNeverPrecedesTheSave()
        {
            object data = ParseGameData(
                "{\"schemaVersion\":4,\"credits\":1," + MinimalGridJson + "," +
                "\"savedAtUtcTicks\":5000,\"highWaterUtcTicks\":40,\"hasUtcClockAnchor\":true}");

            Migrate(data);

            Assert.That(Field<long>(data, "highWaterUtcTicks"), Is.EqualTo(5000L));

            object negative = ParseGameData(
                "{\"schemaVersion\":4,\"credits\":1," + MinimalGridJson + "," +
                "\"savedAtUtcTicks\":-9,\"highWaterUtcTicks\":-9,\"hasUtcClockAnchor\":true}");

            Migrate(negative);

            Assert.That(Field<long>(negative, "savedAtUtcTicks"), Is.EqualTo(0L));
            Assert.That(Field<bool>(negative, "hasUtcClockAnchor"), Is.False,
                "a save with no usable anchor must re-establish one instead of crediting from the epoch");
        }

        [Test]
        public void SavingAfterClockRollbackDoesNotMoveTheOfflineAnchorBackwards()
        {
            object data = ParseGameData("{\"schemaVersion\":4}");
            long previousHighWater = DateTime.UtcNow.AddMinutes(10).Ticks;
            data.GetType().GetField("highWaterUtcTicks").SetValue(data, previousHighWater);

            ProductionType("SaveLoadManager")
                .GetMethod("StampWallClockAnchors", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new[] { data });

            Assert.That(Field<long>(data, "savedAtUtcTicks"), Is.EqualTo(previousHighWater));
            Assert.That(Field<long>(data, "highWaterUtcTicks"), Is.EqualTo(previousHighWater));
            Assert.That(Field<bool>(data, "hasUtcClockAnchor"), Is.True);
            Assert.That(Field<long>(GetField(data, "lifetimeStats"), "firstPlayedUtcTicks"),
                Is.EqualTo(previousHighWater));
        }

        [Test]
        public void AnUnrecordedLaunchResultIsBlanked()
        {
            object data = ParseGameData(
                "{\"schemaVersion\":4,\"credits\":1," + MinimalGridJson + "," +
                "\"launchResult\":{\"recorded\":false,\"completedUtcTicks\":777," +
                "\"totalPlaySeconds\":9,\"creditsAtCompletion\":50," +
                "\"statsSnapshot\":[{\"id\":\"items_sold\",\"value\":4}]}}");

            Migrate(data);

            object result = GetField(data, "launchResult");
            Assert.That(Field<long>(result, "completedUtcTicks"), Is.EqualTo(0L));
            Assert.That(Field<long>(result, "totalPlaySeconds"), Is.EqualTo(0L));
            Assert.That(Field<long>(result, "creditsAtCompletion"), Is.EqualTo(0L));
            Assert.That(((IList)GetField(result, "statsSnapshot")).Count, Is.EqualTo(0));
        }

        [Test]
        public void ARecordedLaunchResultSurvivesNormalizationAndARoundTrip()
        {
            object storage = CreateStorage();
            File.WriteAllText(PathProperty(storage, "PrimaryPath"),
                "{\"schemaVersion\":4,\"credits\":1," + MinimalGridJson + "," +
                "\"savedAtUtcTicks\":100,\"highWaterUtcTicks\":100,\"hasUtcClockAnchor\":true," +
                "\"launchResult\":{\"recorded\":true,\"completedUtcTicks\":12345," +
                "\"totalPlaySeconds\":3600,\"factoriesOwned\":2,\"creditsAtCompletion\":880," +
                "\"statsSnapshot\":[{\"id\":\"items_sold\",\"value\":4}]}}");

            Assert.That(TryLoad(storage, out object data, out string error), Is.True, error);
            Assert.That(TrySave(storage, data, out string saveError), Is.True, saveError);
            Assert.That(TryLoad(storage, out object reloaded, out string reloadError), Is.True, reloadError);

            object result = GetField(reloaded, "launchResult");
            Assert.That(Field<bool>(result, "recorded"), Is.True);
            Assert.That(Field<long>(result, "completedUtcTicks"), Is.EqualTo(12345L));
            Assert.That(Field<int>(result, "factoriesOwned"), Is.EqualTo(2));
            Assert.That(Field<long>(result, "creditsAtCompletion"), Is.EqualTo(880L));
            Assert.That(((IList)GetField(result, "statsSnapshot")).Count, Is.EqualTo(1));
        }

        // -----------------------------------------------------------------------------------
        // Validation
        // -----------------------------------------------------------------------------------

        [Test]
        public void DuplicateFactoryIdsAreRejectedRatherThanMerged()
        {
            object storage = CreateStorage();
            File.WriteAllText(PathProperty(storage, "PrimaryPath"),
                "{\"schemaVersion\":4,\"credits\":1,\"grids\":[" +
                "{\"factoryId\":\"factory_primary\",\"width\":1,\"height\":1,\"cells\":[" +
                "{\"x\":0,\"y\":0,\"machineDefId\":\"blank\"}]}," +
                "{\"factoryId\":\"factory_primary\",\"width\":1,\"height\":1,\"cells\":[" +
                "{\"x\":0,\"y\":0,\"machineDefId\":\"blank\"}]}]}");

            Assert.That(TryLoad(storage, out _, out string error), Is.False);
            Assert.That(error, Does.Contain("more than one grid"));
        }

        [Test]
        public void DuplicateShipModuleIdsAreRejected()
        {
            object data = ParseGameData(
                "{\"schemaVersion\":4,\"credits\":1," + MinimalGridJson + "}");
            Migrate(data);

            // Normalization can never produce a duplicate, so the collision is injected directly to
            // prove the validator refuses it rather than guessing which record is authoritative.
            object blueprint = GetField(data, "shipBlueprint");
            IList modules = (IList)GetField(blueprint, "modules");
            Type moduleType = ProductionType("ShipModuleProgress");
            modules.Add(CreateModule(moduleType, "hull"));
            modules.Add(CreateModule(moduleType, "hull"));

            object freshStorage = CreateStorage();
            Assert.That(TrySave(freshStorage, data, out string saveError), Is.False);
            Assert.That(saveError, Does.Contain("more than once"));
        }

        [Test]
        public void ExistingFactoriesSurviveAConfiguredSiteCountReduction()
        {
            LoadSiteConfiguration("{\"maxSiteCount\":1,\"initialWidth\":5,\"initialHeight\":7," +
                                  "\"maxWidth\":8,\"maxHeight\":10,\"sites\":[" +
                                  "{\"id\":\"factory_primary\",\"displayName\":\"Yard\",\"unlockedByDefault\":true}]}");

            object storage = CreateStorage();
            File.WriteAllText(PathProperty(storage, "PrimaryPath"),
                "{\"schemaVersion\":4,\"credits\":1,\"grids\":[" +
                "{\"factoryId\":\"a\",\"width\":1,\"height\":1,\"cells\":[" +
                "{\"x\":0,\"y\":0,\"machineDefId\":\"blank\"}]}," +
                "{\"factoryId\":\"b\",\"width\":1,\"height\":1,\"cells\":[" +
                "{\"x\":0,\"y\":0,\"machineDefId\":\"blank\"}]}]}");

            Assert.That(TryLoad(storage, out object data, out string error), Is.True, error);
            Assert.That(((IList)GetField(data, "grids")).Count, Is.EqualTo(2),
                "retuning the acquisition cap must not invalidate factories already owned by a player");
        }

        // -----------------------------------------------------------------------------------
        // Configurable site plan
        // -----------------------------------------------------------------------------------

        [Test]
        public void ShippedSiteConfigurationPlansFourSitesFromFiveBySevenToEightByTen()
        {
            TextAsset asset = Resources.Load<TextAsset>("factorysites");
            Assert.That(asset, Is.Not.Null, "factorysites.json should ship as a resource");
            LoadSiteConfiguration(asset.text);

            Assert.That(SiteConfigProperty<int>("MaxSiteCount"), Is.EqualTo(4));
            Assert.That(SiteConfigProperty<int>("InitialWidth"), Is.EqualTo(5));
            Assert.That(SiteConfigProperty<int>("InitialHeight"), Is.EqualTo(7));
            Assert.That(SiteConfigProperty<int>("MaxWidth"), Is.EqualTo(8));
            Assert.That(SiteConfigProperty<int>("MaxHeight"), Is.EqualTo(10));
            Assert.That(SiteIdAt(0), Is.EqualTo(DefaultFactoryId),
                "the first configured site must stay the migrated default factory");
        }

        [Test]
        public void SiteConfigurationIsOverridableAndBadValuesNormalize()
        {
            LoadSiteConfiguration("{\"maxSiteCount\":6,\"initialWidth\":9,\"initialHeight\":9," +
                                  "\"maxWidth\":2,\"maxHeight\":0,\"sites\":[" +
                                  "{\"id\":\"one\",\"displayName\":\"One\"}," +
                                  "{\"id\":\"one\",\"displayName\":\"Duplicate\"}," +
                                  "{\"id\":\"two\"}]}");

            Assert.That(SiteConfigProperty<int>("MaxSiteCount"), Is.EqualTo(6));
            Assert.That(SiteConfigProperty<int>("InitialWidth"), Is.EqualTo(9));
            Assert.That(SiteConfigProperty<int>("MaxWidth"), Is.EqualTo(9),
                "a maximum below the starting size is raised to it");
            Assert.That(SiteConfigProperty<int>("MaxHeight"), Is.EqualTo(9));
            Assert.That(SiteIdAt(0), Is.EqualTo("one"));
            Assert.That(SiteIdAt(1), Is.EqualTo("two"), "the duplicate ID should have been dropped");
            Assert.That(SiteIdAt(2), Is.Null);
        }

        [Test]
        public void GridManagerUsesConfiguredStartingSizeUnlessSceneOverridesIt()
        {
            LoadSiteConfiguration("{\"maxSiteCount\":4,\"initialWidth\":6,\"initialHeight\":8," +
                                  "\"maxWidth\":9,\"maxHeight\":11}");
            GameObject gameObject = new GameObject("GridSizeConfigurationTest");
            try
            {
                Component gridManager = gameObject.AddComponent(ProductionType("GridManager"));
                Type type = gridManager.GetType();
                Assert.That(type.GetProperty("StartingGridWidth").GetValue(gridManager), Is.EqualTo(6));
                Assert.That(type.GetProperty("StartingGridHeight").GetValue(gridManager), Is.EqualTo(8));

                type.GetField("defaultGridWidth").SetValue(gridManager, 7);
                Assert.That(type.GetProperty("StartingGridWidth").GetValue(gridManager), Is.EqualTo(7));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void MissingSiteConfigurationFallsBackToASingleUsableSite()
        {
            LoadSiteConfiguration("   ");

            Assert.That(SiteIdAt(0), Is.EqualTo(DefaultFactoryId));
            Assert.That(SiteConfigProperty<int>("InitialWidth"), Is.EqualTo(5));
            Assert.That(SiteConfigProperty<int>("MaxHeight"), Is.EqualTo(10));
        }

        // -----------------------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------------------

        private const string MinimalGridJson =
            "\"grids\":[{\"factoryId\":\"factory_primary\",\"width\":1,\"height\":1,\"cells\":[" +
            "{\"x\":0,\"y\":0,\"machineDefId\":\"blank\"}]}]";

        private const string Version3Json =
            "{\"schemaVersion\":3,\"credits\":940," +
            "\"hasRuntimeClockAnchor\":true,\"savedAtRuntimeTime\":120," +
            "\"userMachineProgress\":[{\"machineId\":\"shredder\",\"unlocked\":true,\"upgradeLevel\":1}]," +
            "\"objectiveProgress\":[{\"objectiveId\":\"place_first_spawner\",\"progress\":1," +
            "\"completed\":true,\"rewardClaimed\":true,\"processedEventIds\":[\"e1\"]}]," +
            "\"grids\":[{\"width\":1,\"height\":1,\"cells\":[{\"x\":0,\"y\":0," +
            "\"machineDefId\":\"shredder\",\"sortingConfig\":{}," +
            "\"items\":[{\"id\":\"item-1\",\"itemType\":\"can\"}],\"waitingItems\":[]," +
            "\"wasteDeliveryQueue\":[\"starter_crate\"]}]}]}";

        private static object CreateModule(Type moduleType, string moduleId)
        {
            object module = Activator.CreateInstance(moduleType);
            moduleType.GetField("moduleId").SetValue(module, moduleId);
            return module;
        }

        private static void LoadSiteConfiguration(string json)
        {
            ProductionType("FactorySiteConfiguration")
                .GetMethod("LoadFromJson", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object[] { json });
        }

        private static T SiteConfigProperty<T>(string name)
        {
            return (T)ProductionType("FactorySiteConfiguration")
                .GetProperty(name, BindingFlags.Public | BindingFlags.Static)
                .GetValue(null);
        }

        private static string SiteIdAt(int index)
        {
            return (string)ProductionType("FactorySiteConfiguration")
                .GetMethod("SiteIdAt", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object[] { index });
        }

        private static string DefaultFactoryId =>
            (string)ProductionType("FactorySiteConfiguration")
                .GetField("DefaultFactoryId").GetRawConstantValue();

        private static object Migrate(object data)
        {
            return ProductionType("GameSaveMigrations")
                .GetMethod("Migrate", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new[] { data });
        }

        private object CreateStorage()
        {
            string directory = Path.Combine(
                Path.GetTempPath(), "ScrapLineSchemaV4Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            temporaryDirectories.Add(directory);
            return Activator.CreateInstance(ProductionType("GameSaveStorage"), directory, "test-save.json");
        }

        private static object ParseGameData(string json)
        {
            return JsonUtility.FromJson(json, ProductionType("GameData"));
        }

        private static bool TrySave(object storage, object data, out string error)
        {
            object[] arguments = { data, null };
            MethodInfo method = storage.GetType().GetMethods()
                .Single(candidate => candidate.Name == "TrySave" && candidate.GetParameters().Length == 2);
            bool saved = (bool)method.Invoke(storage, arguments);
            error = (string)arguments[1];
            return saved;
        }

        private static bool TryLoad(object storage, out object data, out string error)
        {
            object[] arguments = { null, null, null, null };
            MethodInfo method = storage.GetType().GetMethods()
                .Single(candidate => candidate.Name == "TryLoad" && candidate.GetParameters().Length == 4);
            bool loaded = (bool)method.Invoke(storage, arguments);
            data = arguments[0];
            error = (string)arguments[3];
            return loaded;
        }

        private static string PathProperty(object storage, string name)
        {
            return (string)storage.GetType().GetProperty(name).GetValue(storage);
        }

        private static object GetField(object instance, string field)
        {
            return instance.GetType().GetField(field).GetValue(instance);
        }

        private static T Field<T>(object instance, string field)
        {
            return (T)GetField(instance, field);
        }

        private static object FindByStringField(IList records, string field, string value)
        {
            foreach (object record in records)
            {
                if (Field<string>(record, field) == value)
                    return record;
            }
            Assert.Fail($"Missing record whose {field} is '{value}'.");
            return null;
        }

        private static Type ProductionType(string name)
        {
            return Type.GetType($"{name}, Assembly-CSharp", true);
        }

        private static int CurrentSchemaVersion =>
            (int)ProductionType("GameSaveMigrations").GetField("CurrentSchemaVersion").GetRawConstantValue();
    }
}
