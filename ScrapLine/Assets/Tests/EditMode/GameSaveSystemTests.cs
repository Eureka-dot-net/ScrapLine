using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ScrapLine.Tests.EditMode
{
    public sealed class GameSaveSystemTests
    {
        private readonly List<string> temporaryDirectories = new List<string>();

        [TearDown]
        public void TearDown()
        {
            foreach (string directory in temporaryDirectories)
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
            temporaryDirectories.Clear();
        }

        [Test]
        public void NewGameUsesCurrentSchemaAndStarterSupply()
        {
            Type gameDataType = ProductionType("GameData");
            object data = gameDataType.GetMethod("CreateNewGame", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, null);
            object storage = CreateStorage();

            Assert.That(Field<int>(data, "schemaVersion"), Is.EqualTo(CurrentSchemaVersion));
            Assert.That(Field<bool>(data, "starterDeliveryAvailable"), Is.True);
            Assert.That(TrySave(storage, data, out string saveError), Is.True, saveError);
            Assert.That(File.Exists(PathProperty(storage, "PrimaryPath")), Is.True);
            Assert.That(File.Exists(PathProperty(storage, "BackupPath")), Is.True);
            Assert.That(TryLoad(storage, out object loaded, out _, out string loadError), Is.True, loadError);
            Assert.That(Field<bool>(loaded, "starterDeliveryAvailable"), Is.True);
        }

        [Test]
        public void UnversionedFactoryMigratesWithoutLosingState()
        {
            object storage = CreateStorage();
            File.WriteAllText(PathProperty(storage, "PrimaryPath"), LegacyFactoryJson);

            Assert.That(TryLoad(storage, out object data, out bool fromBackup, out string error), Is.True, error);

            Assert.That(fromBackup, Is.False);
            Assert.That(Field<int>(data, "schemaVersion"), Is.EqualTo(CurrentSchemaVersion));
            Assert.That(Field<int>(data, "credits"), Is.EqualTo(321));

            IList grids = (IList)GetField(data, "grids");
            IList cells = (IList)GetField(grids[0], "cells");
            object cell = cells[0];
            Assert.That(Field<string>(cell, "machineDefId"), Is.EqualTo("fabricator"));
            Assert.That(Field<string>(cell, "selectedRecipeId"), Is.EqualTo("fabricator_recipe"));
            Assert.That(((IList)GetField(cell, "items")).Count, Is.EqualTo(1));
            object sorting = GetField(cell, "sortingConfig");
            Assert.That(Field<string>(sorting, "leftItemType"), Is.EqualTo("can"));

            IList progress = (IList)GetField(data, "userMachineProgress");
            object fabricatorProgress = FindByStringField(progress, "machineId", "fabricator");
            Assert.That(Field<int>(fabricatorProgress, "upgradeLevel"), Is.EqualTo(2));
        }

        [Test]
        public void MissingOptionalFieldsAreNormalized()
        {
            object storage = CreateStorage();
            File.WriteAllText(PathProperty(storage, "PrimaryPath"),
                "{\"credits\":10,\"grids\":[{\"width\":1,\"height\":1,\"cells\":[{" +
                "\"x\":0,\"y\":0,\"machineDefId\":\"blank\"}]}]}");

            Assert.That(TryLoad(storage, out object data, out _, out string error), Is.True, error);

            Assert.That(GetField(data, "userMachineProgress"), Is.Not.Null);
            object cell = ((IList)GetField(((IList)GetField(data, "grids"))[0], "cells"))[0];
            Assert.That(GetField(cell, "items"), Is.Not.Null);
            Assert.That(GetField(cell, "waitingItems"), Is.Not.Null);
            Assert.That(GetField(cell, "sortingConfig"), Is.Not.Null);
            Assert.That(GetField(cell, "wasteDeliveryQueue"), Is.Not.Null);
        }

        [Test]
        public void ResumeReconnectsWaitingItemsAndRebasesAnchoredTimers()
        {
            object storage = CreateStorage();
            File.WriteAllText(PathProperty(storage, "PrimaryPath"), RuntimeStateJson(true));
            Assert.That(TryLoad(storage, out object data, out _, out string loadError), Is.True, loadError);

            const float freshRuntimeTime = 0.25f;
            Assert.That(TryPrepareForResume(data, freshRuntimeTime, 1f, out string resumeError), Is.True, resumeError);

            IList cells = (IList)GetField(((IList)GetField(data, "grids"))[0], "cells");
            IList sourceItems = (IList)GetField(cells[0], "items");
            IList waitingItems = (IList)GetField(cells[1], "waitingItems");
            object waitingCanonical = FindItem(sourceItems, "wait-1");
            Assert.That(ReferenceEquals(waitingCanonical, waitingItems[0]), Is.True,
                "The processor queue must point at the source cell's canonical item instance.");

            object moving = FindItem(sourceItems, "move-1");
            object processing = FindItem((IList)GetField(cells[1], "items"), "process-1");
            Assert.That(freshRuntimeTime - Field<float>(moving, "moveStartTime"), Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(freshRuntimeTime - Field<float>(processing, "processingStartTime"), Is.EqualTo(2f).Within(0.0001f));
            Assert.That(freshRuntimeTime - Field<float>(waitingCanonical, "waitingStartTime"), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(Field<float>(processing, "processingStartTime"), Is.LessThan(0f),
                "A fresh runtime clock can legitimately rebase an active timer below zero.");
        }

        [Test]
        public void UnanchoredLegacyTimersResumeSafelyAtFreshRuntimeClock()
        {
            object data = ParseGameData(RuntimeStateJson(false));

            const float freshRuntimeTime = 0.25f;
            Assert.That(TryPrepareForResume(data, freshRuntimeTime, 2f, out string error), Is.True, error);

            IList cells = (IList)GetField(((IList)GetField(data, "grids"))[0], "cells");
            object moving = FindItem((IList)GetField(cells[0], "items"), "move-1");
            object waiting = FindItem((IList)GetField(cells[0], "items"), "wait-1");
            object processing = FindItem((IList)GetField(cells[1], "items"), "process-1");
            Assert.That(Field<float>(moving, "moveStartTime"), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(Field<float>(processing, "processingStartTime"), Is.EqualTo(freshRuntimeTime));
            Assert.That(Field<float>(waiting, "waitingStartTime"), Is.EqualTo(freshRuntimeTime));
        }

        [Test]
        public void CorruptedPrimaryLoadsValidBackupAndCanRepairPrimary()
        {
            object storage = CreateStorage();
            object first = ParseGameData(VersionedFactoryJson(100));
            object second = ParseGameData(VersionedFactoryJson(200));
            Assert.That(TrySave(storage, first, out string firstError), Is.True, firstError);
            Assert.That(TrySave(storage, second, out string secondError), Is.True, secondError);
            File.WriteAllText(PathProperty(storage, "PrimaryPath"), VersionedFactoryJson(-1));

            Assert.That(TryLoad(storage, out object recovered, out bool fromBackup, out string loadError), Is.True,
                loadError);
            Assert.That(fromBackup, Is.True);
            Assert.That(Field<int>(recovered, "credits"), Is.EqualTo(100));

            Assert.That(TrySave(storage, recovered, out string repairError), Is.True, repairError);
            Assert.That(TryLoad(storage, out object repaired, out bool repairedFromBackup, out string reloadError),
                Is.True, reloadError);
            Assert.That(repairedFromBackup, Is.False);
            Assert.That(Field<int>(repaired, "credits"), Is.EqualTo(100));
            Assert.That(File.Exists(PathProperty(storage, "CorruptPath")), Is.True);
        }

        [Test]
        public void InterruptedTemporaryWriteLeavesLastPrimaryLoadable()
        {
            object storage = CreateStorage();
            Assert.That(TrySave(storage, ParseGameData(VersionedFactoryJson(100)), out string saveError),
                Is.True, saveError);
            File.WriteAllText(PathProperty(storage, "TemporaryPath"), VersionedFactoryJson(999));

            Assert.That(TryLoad(storage, out object loaded, out bool fromBackup, out string loadError),
                Is.True, loadError);
            Assert.That(fromBackup, Is.False);
            Assert.That(Field<int>(loaded, "credits"), Is.EqualTo(100));
        }

        [Test]
        public void CreditChangesScheduleOneDebouncedAutosaveWithoutExtendingDeadline()
        {
            Type creditsType = ProductionType("CreditsManager");
            Type gridType = ProductionType("GridManager");
            Type saveManagerType = ProductionType("SaveLoadManager");
            GameObject gameObject = new GameObject("GameSaveDebounceTests");
            try
            {
                Component credits = gameObject.AddComponent(creditsType);
                Component grid = gameObject.AddComponent(gridType);
                Component saveManager = gameObject.AddComponent(saveManagerType);
                saveManagerType.GetMethod("Initialize").Invoke(saveManager, new object[] { grid, credits });

                creditsType.GetMethod("AddCredits").Invoke(credits, new object[] { 1 });
                float firstDeadline = PrivateField<float>(saveManager, "autosaveAtRealtime");
                Assert.That(PrivateField<bool>(saveManager, "autosavePending"), Is.True);

                creditsType.GetMethod("AddCredits").Invoke(credits, new object[] { 1 });
                Assert.That(PrivateField<float>(saveManager, "autosaveAtRealtime"), Is.EqualTo(firstDeadline));
                Assert.That(saveManagerType.GetMethod("OnApplicationPause", BindingFlags.NonPublic | BindingFlags.Instance),
                    Is.Not.Null, "Mobile background handling must remain wired.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void FailedAutosaveUsesBackoffInsteadOfRetryingEveryUpdate()
        {
            using (RuntimeSaveHarness harness = new RuntimeSaveHarness(ParseGameData(VersionedFactoryJson(50))))
            {
                string blockerRoot = NewTemporaryDirectory();
                string blocker = Path.Combine(blockerRoot, "not-a-directory");
                File.WriteAllText(blocker, "blocks Directory.CreateDirectory");
                object failingStorage = Activator.CreateInstance(
                    ProductionType("GameSaveStorage"), blocker, "autosave.json");
                SetPrivateField(harness.SaveManager, "storage", failingStorage);

                harness.SaveManager.GetType().GetMethod("RequestAutosave").Invoke(harness.SaveManager, null);
                SetPrivateField(harness.SaveManager, "autosaveAtRealtime", Time.realtimeSinceStartup - 1f);

                LogAssert.ignoreFailingMessages = true;
                try
                {
                    InvokePrivate(harness.SaveManager, "Update");
                    int failuresAfterFirstAttempt = PrivateField<int>(harness.SaveManager, "consecutiveAutosaveFailures");
                    float retryDeadline = PrivateField<float>(harness.SaveManager, "autosaveAtRealtime");
                    Assert.That(failuresAfterFirstAttempt, Is.EqualTo(1));
                    Assert.That(retryDeadline, Is.GreaterThan(Time.realtimeSinceStartup));
                    Assert.That(PrivateField<bool>(harness.SaveManager, "autosavePending"), Is.True);

                    InvokePrivate(harness.SaveManager, "Update");
                    Assert.That(PrivateField<int>(harness.SaveManager, "consecutiveAutosaveFailures"),
                        Is.EqualTo(failuresAfterFirstAttempt), "A second frame must not retry the write.");
                    Assert.That(PrivateField<float>(harness.SaveManager, "autosaveAtRealtime"),
                        Is.EqualTo(retryDeadline));
                }
                finally
                {
                    LogAssert.ignoreFailingMessages = false;
                    SetPrivateField(harness.SaveManager, "storage", harness.Storage);
                }
            }
        }

        [Test]
        public void OrdinaryManagerLoadPreservesPreviousBackupGeneration()
        {
            using (RuntimeSaveHarness harness = new RuntimeSaveHarness(ParseGameData(VersionedFactoryJson(0))))
            {
                Assert.That(TrySave(harness.Storage, ParseGameData(VersionedFactoryJson(100)), out string firstError),
                    Is.True, firstError);
                Assert.That(TrySave(harness.Storage, ParseGameData(VersionedFactoryJson(200)), out string secondError),
                    Is.True, secondError);
                string primaryBefore = File.ReadAllText(PathProperty(harness.Storage, "PrimaryPath"));
                string backupBefore = File.ReadAllText(PathProperty(harness.Storage, "BackupPath"));

                bool loaded = (bool)harness.SaveManager.GetType().GetMethod("LoadGame")
                    .Invoke(harness.SaveManager, null);

                Assert.That(loaded, Is.True);
                Assert.That(File.ReadAllText(PathProperty(harness.Storage, "PrimaryPath")), Is.EqualTo(primaryBefore));
                Assert.That(File.ReadAllText(PathProperty(harness.Storage, "BackupPath")), Is.EqualTo(backupBefore));
                Assert.That(backupBefore, Is.Not.EqualTo(primaryBefore),
                    "The fixture must contain a genuinely previous backup generation.");
            }
        }

        [Test]
        public void SemanticUnlockFailureInPrimaryRecoversValidBackup()
        {
            using (RuntimeSaveHarness harness = new RuntimeSaveHarness(ParseGameData(VersionedFactoryJson(0))))
            {
                Assert.That(TrySave(harness.Storage, ParseGameData(VersionedFactoryJson(111)), out string validError),
                    Is.True, validError);
                string invalidJson = VersionedFactoryJson(222).Replace(
                    "\"userMachineProgress\":[]",
                    "\"userMachineProgress\":[{\"machineId\":\"retired_machine\"," +
                    "\"unlocked\":true,\"upgradeLevel\":0}]");
                Assert.That(TrySave(harness.Storage, ParseGameData(invalidJson), out string invalidError),
                    Is.True, invalidError);

                bool loaded = (bool)harness.SaveManager.GetType().GetMethod("LoadGame")
                    .Invoke(harness.SaveManager, null);

                Assert.That(loaded, Is.True);
                object loadedData = harness.GameManager.GetType().GetProperty("gameData")
                    .GetValue(harness.GameManager);
                Assert.That(Field<int>(loadedData, "credits"), Is.EqualTo(111));
                StringAssert.DoesNotContain("retired_machine",
                    File.ReadAllText(PathProperty(harness.Storage, "PrimaryPath")));
                StringAssert.DoesNotContain("retired_machine",
                    File.ReadAllText(PathProperty(harness.Storage, "BackupPath")),
                    "Semantic recovery must not rotate the invalid primary over the good backup.");
                StringAssert.Contains("retired_machine",
                    File.ReadAllText(PathProperty(harness.Storage, "CorruptPath")));
            }
        }

        [Test]
        public void MobilePauseCallbackWritesCurrentStateImmediately()
        {
            using (RuntimeSaveHarness harness = new RuntimeSaveHarness(ParseGameData(VersionedFactoryJson(77))))
            {
                Assert.That(File.Exists(PathProperty(harness.Storage, "PrimaryPath")), Is.False);

                InvokePrivate(harness.SaveManager, "OnApplicationPause", true);

                Assert.That(File.Exists(PathProperty(harness.Storage, "PrimaryPath")), Is.True);
                Assert.That(TryLoad(harness.Storage, out object loaded, out _, out string loadError),
                    Is.True, loadError);
                Assert.That(Field<int>(loaded, "credits"), Is.EqualTo(77));
            }
        }

        [Test]
        public void MigrationIsIdempotentAndRoundTripDoesNotDuplicateFactoryState()
        {
            object storage = CreateStorage();
            File.WriteAllText(PathProperty(storage, "PrimaryPath"), LegacyFactoryJson);
            Assert.That(TryLoad(storage, out object migrated, out _, out string loadError), Is.True, loadError);

            Type migrations = ProductionType("GameSaveMigrations");
            MethodInfo migrate = migrations.GetMethod("Migrate", BindingFlags.Public | BindingFlags.Static);
            string once = JsonUtility.ToJson(migrate.Invoke(null, new[] { migrated }));
            string twice = JsonUtility.ToJson(migrate.Invoke(null, new[] { migrated }));
            Assert.That(twice, Is.EqualTo(once));

            Assert.That(TrySave(storage, migrated, out string saveError), Is.True, saveError);
            Assert.That(TryLoad(storage, out object roundTripped, out _, out string reloadError), Is.True, reloadError);
            Assert.That(Field<int>(roundTripped, "credits"), Is.EqualTo(321));
            Assert.That(((IList)GetField(roundTripped, "grids")).Count, Is.EqualTo(1));
            object grid = ((IList)GetField(roundTripped, "grids"))[0];
            Assert.That(((IList)GetField(grid, "cells")).Count, Is.EqualTo(1));
            object cell = ((IList)GetField(grid, "cells"))[0];
            Assert.That(((IList)GetField(cell, "items")).Count, Is.EqualTo(1));
            IList progress = (IList)GetField(roundTripped, "userMachineProgress");
            Assert.That(progress.Count, Is.EqualTo(4));
            Assert.That(Field<int>(FindByStringField(progress, "machineId", "fabricator"), "upgradeLevel"),
                Is.EqualTo(2));
        }

        private object CreateStorage()
        {
            string directory = NewTemporaryDirectory();
            return Activator.CreateInstance(ProductionType("GameSaveStorage"), directory, "test-save.json");
        }

        private string NewTemporaryDirectory()
        {
            string directory = Path.Combine(Path.GetTempPath(), "ScrapLineSaveTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            temporaryDirectories.Add(directory);
            return directory;
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

        private static bool TryLoad(object storage, out object data, out bool fromBackup, out string error)
        {
            object[] arguments = { null, false, false, null };
            MethodInfo method = storage.GetType().GetMethods()
                .Single(candidate => candidate.Name == "TryLoad" && candidate.GetParameters().Length == 4);
            bool loaded = (bool)method.Invoke(storage, arguments);
            data = arguments[0];
            fromBackup = (bool)arguments[1];
            error = (string)arguments[3];
            return loaded;
        }

        private static bool TryPrepareForResume(
            object data,
            float currentRuntimeTime,
            float moveSpeed,
            out string error)
        {
            object[] arguments = { data, currentRuntimeTime, moveSpeed, null };
            bool prepared = (bool)ProductionType("GameSaveRuntimeRehydrator")
                .GetMethod("TryPrepareForResume", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, arguments);
            error = (string)arguments[3];
            return prepared;
        }

        private static string PathProperty(object storage, string property)
        {
            return (string)storage.GetType().GetProperty(property).GetValue(storage);
        }

        private static object GetField(object owner, string field)
        {
            return owner.GetType().GetField(field).GetValue(owner);
        }

        private static T Field<T>(object owner, string field)
        {
            return (T)GetField(owner, field);
        }

        private static T PrivateField<T>(object owner, string field)
        {
            return (T)owner.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(owner);
        }

        private static void SetPrivateField(object owner, string field, object value)
        {
            owner.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(owner, value);
        }

        private static object InvokePrivate(object owner, string method, params object[] arguments)
        {
            return owner.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(owner, arguments);
        }

        private static object FindItem(IList items, string id)
        {
            foreach (object item in items)
            {
                if (Field<string>(item, "id") == id)
                    return item;
            }
            Assert.Fail($"Missing item '{id}'.");
            return null;
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

        private const string LegacyFactoryJson =
            "{\"credits\":321," +
            "\"userMachineProgress\":[{\"machineId\":\"fabricator\",\"unlocked\":true,\"upgradeLevel\":2}]," +
            "\"grids\":[{\"width\":1,\"height\":1,\"cells\":[{\"x\":0,\"y\":0," +
            "\"machineDefId\":\"fabricator\",\"selectedRecipeId\":\"fabricator_recipe\"," +
            "\"sortingConfig\":{\"leftItemType\":\"can\",\"rightItemType\":\"plasticBottle\"}," +
            "\"items\":[{\"id\":\"item-1\",\"itemType\":\"can\",\"x\":0,\"y\":0}]," +
            "\"waitingItems\":[]}]}]}";

        private static string VersionedFactoryJson(int credits)
        {
            return $"{{\"schemaVersion\":1,\"credits\":{credits}," +
                   "\"userMachineProgress\":[],\"grids\":[{\"width\":1,\"height\":1,\"cells\":[{" +
                   "\"x\":0,\"y\":0,\"machineDefId\":\"blank\",\"items\":[],\"waitingItems\":[]," +
                   "\"sortingConfig\":{}}]}]}";
        }

        private static string RuntimeStateJson(bool anchored)
        {
            string anchor = anchored
                ? "\"hasRuntimeClockAnchor\":true,\"savedAtRuntimeTime\":1000,"
                : "\"hasRuntimeClockAnchor\":false,\"savedAtRuntimeTime\":0,";
            return "{\"schemaVersion\":1," + anchor + "\"credits\":50," +
                   "\"userMachineProgress\":[],\"grids\":[{\"width\":2,\"height\":1," +
                   "\"cells\":[{\"x\":0,\"y\":0,\"machineDefId\":\"conveyor\",\"sortingConfig\":{}," +
                   "\"items\":[{\"id\":\"move-1\",\"itemType\":\"can\",\"state\":1," +
                   "\"moveStartTime\":999.5,\"moveProgress\":0.5},{\"id\":\"wait-1\"," +
                   "\"itemType\":\"can\",\"state\":2,\"waitingStartTime\":999,\"moveProgress\":0.5}]," +
                   "\"waitingItems\":[]},{\"x\":1,\"y\":0,\"machineDefId\":\"shredder\"," +
                   "\"sortingConfig\":{},\"items\":[{\"id\":\"process-1\",\"itemType\":\"can\"," +
                   "\"state\":3,\"processingStartTime\":998,\"processingDuration\":5}]," +
                   "\"waitingItems\":[{\"id\":\"wait-1\",\"itemType\":\"can\",\"state\":2," +
                   "\"waitingStartTime\":999,\"moveProgress\":0.5}]}]}]}";
        }

        private sealed class RuntimeSaveHarness : IDisposable
        {
            private readonly GameObject gameObject;

            public RuntimeSaveHarness(object data)
            {
                gameObject = new GameObject("RuntimeSaveHarness");
                object registry = ProductionType("FactoryRegistry").GetProperty("Instance").GetValue(null);
                registry.GetType().GetMethod("LoadFromJson").Invoke(registry, new object[]
                {
                    Resources.Load<TextAsset>("machines").text,
                    Resources.Load<TextAsset>("recipes").text,
                    Resources.Load<TextAsset>("items").text,
                    Resources.Load<TextAsset>("wastecrates").text,
                    null
                });
                Type gameManagerType = ProductionType("GameManager");
                Type creditsType = ProductionType("CreditsManager");
                Type gridType = ProductionType("GridManager");
                Type movementType = ProductionType("ItemMovementManager");
                Type saveManagerType = ProductionType("SaveLoadManager");

                GameManager = gameObject.AddComponent(gameManagerType);
                gameManagerType.GetField("<Instance>k__BackingField",
                        BindingFlags.NonPublic | BindingFlags.Static)
                    .SetValue(null, GameManager);
                Credits = gameObject.AddComponent(creditsType);
                Grid = gameObject.AddComponent(gridType);
                ItemMovement = gameObject.AddComponent(movementType);
                SaveManager = gameObject.AddComponent(saveManagerType);

                gameManagerType.GetField("creditsManager").SetValue(GameManager, Credits);
                gameManagerType.GetField("gridManager").SetValue(GameManager, Grid);
                gameManagerType.GetField("itemMovementManager").SetValue(GameManager, ItemMovement);
                gameManagerType.GetField("saveLoadManager").SetValue(GameManager, SaveManager);
                gameManagerType.GetProperty("gameData").SetValue(GameManager, data);
                Grid.GetType().GetMethod("SetActiveGrids").Invoke(Grid, new[] { GetField(data, "grids") });
                Credits.GetType().GetMethod("SetCredits", new[] { typeof(int), typeof(bool) })
                    .Invoke(Credits, new object[] { Field<int>(data, "credits"), false });

                SaveManager.GetType().GetField("saveFileName").SetValue(
                    SaveManager, $"save-test-{Guid.NewGuid():N}.json");
                SaveManager.GetType().GetMethod("Initialize").Invoke(SaveManager, new[] { Grid, Credits });
                Storage = SaveManager.GetType().GetField("storage", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(SaveManager);
            }

            public object GameManager { get; }
            public object Credits { get; }
            public object Grid { get; }
            public object ItemMovement { get; }
            public object SaveManager { get; }
            public object Storage { get; }

            public void Dispose()
            {
                SetPrivateField(SaveManager, "storage", Storage);
                SaveManager.GetType().GetMethod("DeleteSaveFile").Invoke(SaveManager, null);
                GameManager.GetType().GetField("<Instance>k__BackingField",
                        BindingFlags.NonPublic | BindingFlags.Static)
                    .SetValue(null, null);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
    }
}
