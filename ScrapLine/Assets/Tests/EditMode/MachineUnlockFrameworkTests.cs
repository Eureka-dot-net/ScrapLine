using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ScrapLine.Tests.EditMode
{
    public sealed class MachineUnlockFrameworkTests
    {
        private object registry;

        [SetUp]
        public void SetUp()
        {
            registry = ProductionType("FactoryRegistry").GetProperty("Instance").GetValue(null);
            registry.GetType().GetMethod("LoadFromJson").Invoke(registry, new object[]
            {
                Resources.Load<TextAsset>("machines").text,
                Resources.Load<TextAsset>("recipes").text,
                Resources.Load<TextAsset>("items").text,
                Resources.Load<TextAsset>("wastecrates").text,
                null
            });
            Assert.That(TryLoadProgress(NewGameData(), out string error), Is.True, error);
        }

        [Test]
        public void CleanSaveLicensesStarterToolsAndListsEveryBuildableMachine()
        {
            Assert.That(UnlockedMachineIds(),
                Is.EqualTo(new[] { "conveyor", "seller", "spawner" }));
            Assert.That(PanelMachineIds(), Is.EqualTo(new[]
            {
                "conveyor", "spawner", "seller", "shredder",
                "granulator", "sorter", "plate_press", "fabricator"
            }));

            Assert.That(MachineInt("shredder", "unlockCost"), Is.EqualTo(100));
            Assert.That(MachineInt("granulator", "unlockCost"), Is.EqualTo(125));
            Assert.That(MachineInt("sorter", "unlockCost"), Is.EqualTo(150));
            Assert.That(MachineInt("plate_press", "unlockCost"), Is.EqualTo(300));
            Assert.That(MachineInt("fabricator", "unlockCost"), Is.EqualTo(600));
        }

        [Test]
        public void UnlockingMachineDoesNotReorderBuildMenu()
        {
            Assert.That(TryGrant("plate_press", "test", out string error), Is.True, error);

            Assert.That(PanelMachineIds(), Is.EqualTo(new[]
            {
                "conveyor", "spawner", "seller", "shredder",
                "granulator", "sorter", "plate_press", "fabricator"
            }));
        }

        [Test]
        public void PaidLicenseDeductsExactCostAndEmitsPurchaseSource()
        {
            using (CreditHarness credits = new CreditHarness(500))
            {
                List<string> events = CaptureUnlockEvents(out EventInfo unlockedEvent, out Action<string, string> handler);
                try
                {
                    Assert.That(TryPurchase("shredder", credits.Component, out string error), Is.True, error);
                }
                finally
                {
                    unlockedEvent.RemoveEventHandler(registry, handler);
                }

                Assert.That(credits.Balance, Is.EqualTo(400));
                Assert.That(IsUnlocked("shredder"), Is.True);
                Assert.That(events, Is.EqualTo(new[] { "shredder:credit_purchase" }));
            }
        }

        [Test]
        public void InsufficientCreditsLeaveBalanceAndLicenseUnchanged()
        {
            using (CreditHarness credits = new CreditHarness(99))
            {
                Assert.That(TryPurchase("shredder", credits.Component, out string error), Is.False);
                StringAssert.Contains("costs 100 credits", error);
                Assert.That(credits.Balance, Is.EqualTo(99));
                Assert.That(IsUnlocked("shredder"), Is.False);
            }
        }

        [Test]
        public void DuplicatePurchaseDoesNotChargeOrEmitTwice()
        {
            using (CreditHarness credits = new CreditHarness(500))
            {
                List<string> events = CaptureUnlockEvents(out EventInfo unlockedEvent, out Action<string, string> handler);
                try
                {
                    Assert.That(TryPurchase("shredder", credits.Component, out string firstError), Is.True, firstError);
                    Assert.That(TryPurchase("shredder", credits.Component, out string secondError), Is.False);
                    StringAssert.Contains("already licensed", secondError);
                }
                finally
                {
                    unlockedEvent.RemoveEventHandler(registry, handler);
                }

                Assert.That(credits.Balance, Is.EqualTo(400));
                Assert.That(events, Is.EqualTo(new[] { "shredder:credit_purchase" }));
            }
        }

        [Test]
        public void FreeObjectiveGrantUsesAuthoritativeTransitionWithoutCharging()
        {
            using (CreditHarness credits = new CreditHarness(25))
            {
                List<string> events = CaptureUnlockEvents(out EventInfo unlockedEvent, out Action<string, string> handler);
                try
                {
                    Assert.That(TryGrant("sorter", "objective_reward", out string error), Is.True, error);
                }
                finally
                {
                    unlockedEvent.RemoveEventHandler(registry, handler);
                }

                Assert.That(credits.Balance, Is.EqualTo(25));
                Assert.That(IsUnlocked("sorter"), Is.True);
                Assert.That(events, Is.EqualTo(new[] { "sorter:objective_reward" }));
            }
        }

        [Test]
        public void PurchasedLicenseAndBalanceSurviveSerializationAndReload()
        {
            using (CreditHarness credits = new CreditHarness(500))
            {
                Assert.That(TryPurchase("shredder", credits.Component, out string purchaseError), Is.True,
                    purchaseError);
                object data = NewGameData();
                Field(data, "credits", credits.Balance);
                registry.GetType().GetMethod("SaveToGameData").Invoke(registry, new[] { data });

                object restored = JsonUtility.FromJson(JsonUtility.ToJson(data), ProductionType("GameData"));
                ProductionType("GameSaveMigrations").GetMethod("Migrate").Invoke(null, new[] { restored });
                Assert.That(TryLoadProgress(restored, out string loadError), Is.True, loadError);

                Assert.That(IsUnlocked("shredder"), Is.True);
                Assert.That((int)Field(restored, "credits"), Is.EqualTo(400));
            }
        }

        [Test]
        public void SchemaOneMigrationAddsStarterLicensesAndRecoversPlacedMachineIdempotently()
        {
            object data = JsonUtility.FromJson(
                "{\"schemaVersion\":1,\"credits\":100," +
                "\"userMachineProgress\":[],\"grids\":[{\"width\":1,\"height\":1,\"cells\":[{" +
                "\"x\":0,\"y\":0,\"cellType\":1,\"cellRole\":0,\"machineDefId\":\"sorter\"," +
                "\"items\":[],\"waitingItems\":[],\"sortingConfig\":{}}]}]}",
                ProductionType("GameData"));
            MethodInfo migrate = ProductionType("GameSaveMigrations").GetMethod("Migrate");

            string once = JsonUtility.ToJson(migrate.Invoke(null, new[] { data }));
            string twice = JsonUtility.ToJson(migrate.Invoke(null, new[] { data }));

            Assert.That(twice, Is.EqualTo(once));
            Assert.That(TryLoadProgress(data, out string error), Is.True, error);
            Assert.That(UnlockedMachineIds(),
                Is.EqualTo(new[] { "conveyor", "seller", "sorter", "spawner" }));
            Assert.That(IsUnlocked("shredder"), Is.False);
        }

        [Test]
        public void InvalidMachineIdCannotChargeOrGrant()
        {
            using (CreditHarness credits = new CreditHarness(500))
            {
                Assert.That(TryPurchase("missing_machine", credits.Component, out string purchaseError), Is.False);
                StringAssert.Contains("Unknown machine ID 'missing_machine'", purchaseError);
                Assert.That(TryGrant("missing_machine", "developer_tool", out string grantError), Is.False);
                StringAssert.Contains("Unknown machine ID 'missing_machine'", grantError);
                Assert.That(credits.Balance, Is.EqualTo(500));
            }
        }

        [Test]
        public void ExposedMachineProgressIsReadOnlySnapshot()
        {
            Assert.That(TryGrant("shredder", "test_grant", out string error), Is.True, error);
            Assert.That(registry.GetType().GetField("UserMachines"), Is.Null);

            IEnumerable exposed = (IEnumerable)registry.GetType().GetProperty("UserMachines").GetValue(registry);
            object shredder = exposed.Cast<object>()
                .Single(progress => (string)Field(progress, "machineId") == "shredder");
            Field(shredder, "unlocked", false);
            object found = registry.GetType().GetMethod("FindMachineProgress")
                .Invoke(registry, new object[] { "shredder" });
            Field(found, "unlocked", false);

            Assert.That(IsUnlocked("shredder"), Is.True);
        }

        [Test]
        public void DirectPlacementRejectsUnlicensedMachineAndAcceptsLicensedMachine()
        {
            object data = ParseOneCellGameData();
            Assert.That(TryLoadProgress(data, out string loadError), Is.True, loadError);

            GameObject owner = new GameObject("MachineLicensePlacementTest");
            try
            {
                Component grid = owner.AddComponent(ProductionType("GridManager"));
                Component credits = owner.AddComponent(ProductionType("CreditsManager"));
                Component machines = owner.AddComponent(ProductionType("MachineManager"));
                grid.GetType().GetMethod("SetActiveGrids").Invoke(grid, new[] { Field(data, "grids") });
                credits.GetType().GetMethod("SetCredits", new[] { typeof(int), typeof(bool) })
                    .Invoke(credits, new object[] { 1000, false });
                machines.GetType().GetMethod("Initialize").Invoke(machines, new object[] { credits, grid, null });

                object up = Enum.ToObject(ProductionType("UICell+Direction"), 0);
                Assert.That((bool)machines.GetType().GetMethod("PlaceDraggedMachine")
                    .Invoke(machines, new[] { (object)0, 0, "shredder", up }), Is.False);

                Assert.That(TryGrant("shredder", "developer_tool", out string grantError), Is.True, grantError);
                Assert.That((bool)machines.GetType().GetMethod("PlaceDraggedMachine")
                    .Invoke(machines, new[] { (object)0, 0, "shredder", up }), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [UnityTest]
        public IEnumerator MachineBarShowsLockedPricesAndRefreshesImmediatelyAfterConfirmedPurchase()
        {
            GameObject panel = new GameObject("MachineBarPanel", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            GameObject prefab = new GameObject(
                "MachineButtonPrefab", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            prefab.AddComponent(ProductionType("MachineButton"));
            prefab.AddComponent(ProductionType("MachineRenderer"));
            GameObject owner = new GameObject("MachineLicenseUiTest");
            Component manager = null;
            Type gameManagerType = ProductionType("GameManager");
            FieldInfo instanceField = gameManagerType.GetField("<Instance>k__BackingField",
                BindingFlags.NonPublic | BindingFlags.Static);
            try
            {
                Component gameManager = owner.AddComponent(gameManagerType);
                instanceField.SetValue(null, gameManager);
                Component credits = owner.AddComponent(ProductionType("CreditsManager"));
                credits.GetType().GetMethod("SetCredits", new[] { typeof(int), typeof(bool) })
                    .Invoke(credits, new object[] { 500, false });
                gameManagerType.GetField("creditsManager").SetValue(gameManager, credits);
                Component machineManager = owner.AddComponent(ProductionType("MachineManager"));
                gameManagerType.GetField("machineManager").SetValue(gameManager, machineManager);

                manager = owner.AddComponent(ProductionType("MachineBarUIManager"));
                manager.GetType().GetField("machineButtonPrefab").SetValue(manager, prefab);
                manager.GetType().GetField("machineBarPanel").SetValue(manager, panel.transform);
                InvokePrivate(manager, "Awake");
                manager.GetType().GetMethod("InitBar").Invoke(manager, null);

                // MachineRenderer uses deferred destruction. Wait a frame so this assertion would
                // catch the old initialization order where Setup deleted LicenseStatus.
                yield return null;

                Assert.That(PanelMachineIds(panel.transform), Is.EqualTo(PanelMachineIds()));
                Component platePress = FindPanelButton(panel.transform, "plate_press");
                Transform lockedOverlay = platePress.transform.Find("LicenseStatus");
                Assert.That(lockedOverlay, Is.Not.Null,
                    "MachineRenderer.Setup must not remove the license overlay.");
                Assert.That(((RectTransform)lockedOverlay).anchorMax, Is.EqualTo(Vector2.one),
                    "Locked previews should be greyed across the full card.");
                Assert.That((bool)platePress.GetType().GetMethod("IsLicensed").Invoke(platePress, null), Is.False);
                string lockedStatus = (string)platePress.GetType().GetMethod("GetStatusText").Invoke(platePress, null);
                Assert.That(lockedStatus, Is.EqualTo("300"));

                platePress.GetComponent<Button>().onClick.Invoke();
                string confirmationStatus =
                    (string)platePress.GetType().GetMethod("GetStatusText").Invoke(platePress, null);
                Assert.That(confirmationStatus, Is.EqualTo("BUY 300?"));
                Assert.That((int)credits.GetType().GetMethod("GetCredits").Invoke(credits, null), Is.EqualTo(500));

                platePress.GetComponent<Button>().onClick.Invoke();
                yield return null;
                Component refreshed = FindPanelButton(panel.transform, "plate_press");
                Transform licensedOverlay = refreshed.transform.Find("LicenseStatus");
                Assert.That(licensedOverlay, Is.Not.Null,
                    "The renderer-backed button must retain its overlay after the purchase refresh.");
                Assert.That(((RectTransform)licensedOverlay).anchorMax.y, Is.EqualTo(0.34f).Within(0.001f),
                    "Licensed previews should show only a compact placement-price badge.");
                Assert.That((bool)refreshed.GetType().GetMethod("IsLicensed").Invoke(refreshed, null), Is.True);
                Assert.That((string)refreshed.GetType().GetMethod("GetStatusText").Invoke(refreshed, null),
                    Is.EqualTo("250"));
                Assert.That((int)credits.GetType().GetMethod("GetCredits").Invoke(credits, null), Is.EqualTo(200));
            }
            finally
            {
                if (manager != null)
                    InvokePrivate(manager, "OnDestroy");
                instanceField.SetValue(null, null);
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(panel);
            }
        }

        private bool TryPurchase(string machineId, Component credits, out string error)
        {
            object[] arguments = { machineId, credits, null };
            bool purchased = (bool)registry.GetType().GetMethod("TryPurchaseMachineLicense")
                .Invoke(registry, arguments);
            error = (string)arguments[2];
            return purchased;
        }

        private bool TryGrant(string machineId, string source, out string error)
        {
            object[] arguments = { machineId, source, null };
            bool granted = (bool)registry.GetType().GetMethod("TryGrantMachineLicense")
                .Invoke(registry, arguments);
            error = (string)arguments[2];
            return granted;
        }

        private List<string> CaptureUnlockEvents(out EventInfo unlockedEvent, out Action<string, string> handler)
        {
            List<string> events = new List<string>();
            handler = (machineId, source) => events.Add($"{machineId}:{source}");
            unlockedEvent = registry.GetType().GetEvent("MachineUnlocked");
            unlockedEvent.AddEventHandler(registry, handler);
            return events;
        }

        private object NewGameData() => ProductionType("GameData").GetMethod("CreateNewGame").Invoke(null, null);

        private bool TryLoadProgress(object data, out string error)
        {
            object[] arguments = { data, null };
            bool loaded = (bool)registry.GetType().GetMethod("TryLoadFromGameData").Invoke(registry, arguments);
            error = (string)arguments[1];
            return loaded;
        }

        private bool IsUnlocked(string machineId) =>
            (bool)registry.GetType().GetMethod("IsMachineUnlocked").Invoke(registry, new object[] { machineId });

        private string[] UnlockedMachineIds()
        {
            IEnumerable progress = (IEnumerable)registry.GetType().GetProperty("UserMachines").GetValue(registry);
            return progress.Cast<object>()
                .Where(item => (bool)Field(item, "unlocked"))
                .Select(item => (string)Field(item, "machineId"))
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();
        }

        private string[] PanelMachineIds()
        {
            IEnumerable machines = (IEnumerable)registry.GetType().GetMethod("GetPanelMachines")
                .Invoke(registry, null);
            return machines.Cast<object>().Select(machine => (string)Field(machine, "id")).ToArray();
        }

        private int MachineInt(string machineId, string field)
        {
            object machine = registry.GetType().GetMethod("GetMachine").Invoke(registry, new object[] { machineId });
            return (int)Field(machine, field);
        }

        private static object ParseOneCellGameData()
        {
            return JsonUtility.FromJson(
                "{\"schemaVersion\":2,\"credits\":1000," +
                "\"userMachineProgress\":[{\"machineId\":\"conveyor\",\"unlocked\":true}," +
                "{\"machineId\":\"seller\",\"unlocked\":true},{\"machineId\":\"spawner\",\"unlocked\":true}]," +
                "\"grids\":[{\"width\":1,\"height\":1,\"cells\":[{\"x\":0,\"y\":0," +
                "\"cellType\":0,\"cellRole\":0,\"machineDefId\":\"blank\",\"items\":[]," +
                "\"waitingItems\":[],\"sortingConfig\":{}}]}]}", ProductionType("GameData"));
        }

        private static string[] PanelMachineIds(Transform panel)
        {
            List<string> ids = new List<string>();
            for (int index = 0; index < panel.childCount; index++)
            {
                Component button = panel.GetChild(index).GetComponent(ProductionType("MachineButton"));
                object machine = button.GetType().GetMethod("GetMachineDef").Invoke(button, null);
                ids.Add((string)Field(machine, "id"));
            }
            return ids.ToArray();
        }

        private static Component FindPanelButton(Transform panel, string machineId)
        {
            for (int index = 0; index < panel.childCount; index++)
            {
                Component button = panel.GetChild(index).GetComponent(ProductionType("MachineButton"));
                object machine = button.GetType().GetMethod("GetMachineDef").Invoke(button, null);
                if ((string)Field(machine, "id") == machineId)
                    return button;
            }
            Assert.Fail($"Missing panel button for '{machineId}'.");
            return null;
        }

        private static object Field(object owner, string name) => owner.GetType().GetField(name).GetValue(owner);
        private static void Field(object owner, string name, object value) =>
            owner.GetType().GetField(name).SetValue(owner, value);
        private static object InvokePrivate(object owner, string method) =>
            owner.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(owner, null);
        private static Type ProductionType(string name) => Type.GetType($"{name}, Assembly-CSharp", true);

        private sealed class CreditHarness : IDisposable
        {
            private readonly GameObject owner;
            public CreditHarness(int credits)
            {
                owner = new GameObject("MachineLicenseCreditTest");
                Component = owner.AddComponent(ProductionType("CreditsManager"));
                Component.GetType().GetMethod("SetCredits", new[] { typeof(int), typeof(bool) })
                    .Invoke(Component, new object[] { credits, false });
            }
            public Component Component { get; }
            public int Balance => (int)Component.GetType().GetMethod("GetCredits").Invoke(Component, null);
            public void Dispose() => UnityEngine.Object.DestroyImmediate(owner);
        }
    }
}
