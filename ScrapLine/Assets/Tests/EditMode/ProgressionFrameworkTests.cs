using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace ScrapLine.Tests.EditMode
{
    public sealed class ProgressionFrameworkTests
    {
        private readonly List<GameObject> owners = new List<GameObject>();
        private readonly List<string> temporaryDirectories = new List<string>();
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
            object data = NewGameData();
            object[] loadArguments = { data, null };
            Assert.That((bool)registry.GetType().GetMethod("TryLoadFromGameData")
                .Invoke(registry, loadArguments), Is.True, loadArguments[1] as string);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject owner in owners.Where(owner => owner != null))
                UnityEngine.Object.DestroyImmediate(owner);
            owners.Clear();
            foreach (string directory in temporaryDirectories)
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
            temporaryDirectories.Clear();
        }

        [Test]
        public void EverySupportedDomainEventAdvancesItsMatchingObjective()
        {
            string definitions = Objectives(
                Objective("sold_count", "item_sold_count", "can", 1),
                Objective("sold_value", "item_sold_value", "can", 5),
                Objective("placed", "machine_placed", "spawner", 1),
                Objective("licensed", "machine_license", "shredder", 1),
                Objective("recipe", "recipe_completed", "shred_can", 2),
                Objective("delivery", "scrap_delivery_ordered", "starter_crate", 1),
                Objective("expansion", "grid_expanded", "any", 1));
            Harness harness = CreateHarness(definitions, 100);

            InvokeStatic("GameplayDomainEvents", "PublishItemSold", "item-1", "can", 1, 5, "seller");
            InvokeStatic("GameplayDomainEvents", "PublishMachinePlaced", "place-1", "spawner");
            InvokeStatic("GameplayDomainEvents", "PublishMachineLicense", "shredder", "credit_purchase");
            InvokeStatic("GameplayDomainEvents", "PublishRecipeCompleted", "process-1", "shred_can", 4);
            InvokeStatic("GameplayDomainEvents", "PublishScrapDelivery", "delivery-1", "starter_crate", "credit_purchase");
            InvokeStatic("GameplayDomainEvents", "PublishGridExpanded", "expand-1", "row", 5, 7, 5, 8, 100);

            Assert.That(Progress(harness.Manager, "sold_count"), Is.EqualTo(1));
            Assert.That(Progress(harness.Manager, "sold_value"), Is.EqualTo(5));
            Assert.That(Progress(harness.Manager, "placed"), Is.EqualTo(1));
            Assert.That(Progress(harness.Manager, "licensed"), Is.EqualTo(1));
            Assert.That(Progress(harness.Manager, "recipe"), Is.EqualTo(1),
                "One recipe operation counts once even when it produces multiple outputs.");
            Assert.That(Progress(harness.Manager, "delivery"), Is.EqualTo(1));
            Assert.That(Progress(harness.Manager, "expansion"), Is.EqualTo(1));
        }

        [Test]
        public void ProgressAccumulatesToBoundaryAndIgnoresUnrelatedOrDuplicateEvents()
        {
            Harness harness = CreateHarness(Objectives(Objective("sell_three", "item_sold_count", "can", 3)), 0);

            Assert.That(Record(harness.Manager, "wrong", "item_sold_count", "plasticBottle", 2, 0), Is.False);
            Assert.That(Record(harness.Manager, "sale-1", "item_sold_count", "can", 2, 0), Is.True);
            Assert.That(Record(harness.Manager, "sale-1", "item_sold_count", "can", 2, 0), Is.False);
            Assert.That(Progress(harness.Manager, "sell_three"), Is.EqualTo(2));
            Assert.That(Record(harness.Manager, "sale-2", "item_sold_count", "can", 50, 0), Is.True);

            object state = State(harness.Manager, "sell_three");
            Assert.That(Property<int>(state, "Progress"), Is.EqualTo(3));
            Assert.That(Property<bool>(state, "IsCompleted"), Is.True);
        }

        [Test]
        public void CreditRewardIsExactAndClaimIsIdempotent()
        {
            Harness harness = CreateHarness(
                Objectives(Objective("credit_reward", "machine_placed", "spawner", 1, "credits", 25)), 100);
            Record(harness.Manager, "place", "machine_placed", "spawner", 1, 0);

            Assert.That(Claim(harness.Manager, "credit_reward", out string error), Is.True, error);
            Assert.That(Balance(harness.Credits), Is.EqualTo(125));
            Assert.That(Claim(harness.Manager, "credit_reward", out _), Is.False);
            Assert.That(Balance(harness.Credits), Is.EqualTo(125));
            Assert.That(Property<bool>(State(harness.Manager, "credit_reward"), "IsRewardClaimed"), Is.True);
        }

        [Test]
        public void FreeLicenseRewardUsesAuthoritativeGrantWithObjectiveSource()
        {
            Harness harness = CreateHarness(
                Objectives(Objective("free_sorter", "machine_placed", "spawner", 1,
                    "machine_license", 0, "sorter")), 30);
            List<string> events = new List<string>();
            Action<string, string> handler = (machineId, source) => events.Add($"{machineId}:{source}");
            EventInfo unlocked = registry.GetType().GetEvent("MachineUnlocked");
            unlocked.AddEventHandler(registry, handler);
            try
            {
                Record(harness.Manager, "place", "machine_placed", "spawner", 1, 0);
                Assert.That(Claim(harness.Manager, "free_sorter", out string error), Is.True, error);
            }
            finally
            {
                unlocked.RemoveEventHandler(registry, handler);
            }

            Assert.That(Balance(harness.Credits), Is.EqualTo(30));
            Assert.That((bool)registry.GetType().GetMethod("IsMachineUnlocked")
                .Invoke(registry, new object[] { "sorter" }), Is.True);
            Assert.That(events, Is.EqualTo(new[] { "sorter:objective_reward:free_sorter" }));
        }

        [Test]
        public void FailedRewardApplicationLeavesClaimStateUnchanged()
        {
            Harness harness = CreateHarness(
                Objectives(Objective("needs_credits", "machine_placed", "spawner", 1, "credits", 10)),
                0, initializeCredits: false);
            Record(harness.Manager, "place", "machine_placed", "spawner", 1, 0);

            Assert.That(Claim(harness.Manager, "needs_credits", out string error), Is.False);
            StringAssert.Contains("CreditsManager", error);
            Assert.That(Property<bool>(State(harness.Manager, "needs_credits"), "IsRewardClaimed"), Is.False);
        }

        [Test]
        public void PartialCompletedAndClaimedStateRoundTripsAndCleanResetClearsIt()
        {
            string definitions = Objectives(
                Objective("claimed", "item_sold_count", "can", 1),
                Objective("completed", "item_sold_count", "can", 2),
                Objective("partial", "item_sold_count", "can", 3));
            Harness original = CreateHarness(definitions, 0);
            Record(original.Manager, "batch", "item_sold_count", "can", 2, 0);
            Assert.That(Claim(original.Manager, "claimed", out string error), Is.True, error);

            object restoredData = JsonUtility.FromJson(JsonUtility.ToJson(original.Data), ProductionType("GameData"));
            Harness restored = CreateHarness(definitions, 0, data: restoredData, reset: false);

            Assert.That(Property<bool>(State(restored.Manager, "claimed"), "IsRewardClaimed"), Is.True);
            Assert.That(Property<bool>(State(restored.Manager, "completed"), "IsCompleted"), Is.True);
            Assert.That(Progress(restored.Manager, "partial"), Is.EqualTo(2));

            object[] resetArguments = { restored.Data, null };
            Assert.That((bool)restored.Manager.GetType().GetMethod("ResetForNewGame")
                .Invoke(restored.Manager, resetArguments), Is.True, resetArguments[1] as string);
            Assert.That(Progress(restored.Manager, "claimed"), Is.Zero);
            Assert.That(Progress(restored.Manager, "completed"), Is.Zero);
            Assert.That(Progress(restored.Manager, "partial"), Is.Zero);
        }

        [Test]
        public void MachineLicenseObjectiveAutoCompletesForASaveThatAlreadyOwnsTheLicense()
        {
            // Simulates both a migrated save (predates the objective, so it has no matching
            // objectiveProgress entry) and a save loaded after a license was granted by other
            // means. Either way, re-purchasing an already-owned license is impossible, so no
            // future domain event can ever complete the objective -- it must reconcile immediately.
            Assert.That(GrantLicense("shredder", out string grantError), Is.True, grantError);
            string definitions = Objectives(Objective("licensed", "machine_license", "shredder", 1));

            Harness harness = CreateHarness(definitions, 0, reset: false);

            object state = State(harness.Manager, "licensed");
            Assert.That(Property<bool>(state, "IsCompleted"), Is.True,
                "A machine already licensed before the objective existed must not require a new purchase.");
            Assert.That(Property<int>(state, "Progress"), Is.EqualTo(1));
            Assert.That(Property<bool>(state, "IsRewardClaimed"), Is.False,
                "Auto-completion must not auto-claim the reward.");
            Assert.That(Claim(harness.Manager, "licensed", out string claimError), Is.True, claimError);
        }

        [Test]
        public void ResetForNewGameTracksWhetherTheUnderlyingLicenseWasAlsoRevoked()
        {
            string definitions = Objectives(Objective("licensed", "machine_license", "shredder", 1));
            Harness harness = CreateHarness(definitions, 0);
            Assert.That(GrantLicense("shredder", out string grantError), Is.True, grantError);
            Record(harness.Manager, "grant-event", "machine_license", "shredder", 1, 0);
            Assert.That(Claim(harness.Manager, "licensed", out string claimError), Is.True, claimError);

            // A reset that clears progression without also revoking the license (the bug this
            // guards against) must not strand the objective incomplete forever: it re-completes
            // immediately so it stays claimable rather than requiring an impossible re-purchase.
            object[] resetArguments = { harness.Data, null };
            Assert.That((bool)harness.Manager.GetType().GetMethod("ResetForNewGame")
                .Invoke(harness.Manager, resetArguments), Is.True, resetArguments[1] as string);
            Assert.That(Property<bool>(State(harness.Manager, "licensed"), "IsCompleted"), Is.True);
            Assert.That(Property<bool>(State(harness.Manager, "licensed"), "IsRewardClaimed"), Is.False,
                "Reset must still clear a previously claimed reward.");

            // Once the license itself is also reverted to its default (locked) state -- what
            // GameManager.ResetGrid now does before resetting progression -- the objective
            // correctly returns to incomplete and can be earned again.
            ResetLicenses();
            Assert.That((bool)harness.Manager.GetType().GetMethod("ResetForNewGame")
                .Invoke(harness.Manager, resetArguments), Is.True, resetArguments[1] as string);
            Assert.That(Property<bool>(State(harness.Manager, "licensed"), "IsCompleted"), Is.False);
            Assert.That(Progress(harness.Manager, "licensed"), Is.Zero);
        }

        [Test]
        public void SemanticObjectiveCorruptionFallsBackToValidBackup()
        {
            Harness harness = CreateHarness(Objectives(Objective("valid", "machine_placed", "spawner", 1)), 0);
            string directory = Path.Combine(Path.GetTempPath(), "ScrapLineObjectiveTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            temporaryDirectories.Add(directory);
            object storage = Activator.CreateInstance(ProductionType("GameSaveStorage"), directory, "objective-save.json");
            Delegate validator = CreateValidator(harness.Manager);

            Assert.That(Save(storage, harness.Data, validator, out string firstError), Is.True, firstError);
            Field(harness.Data, "credits", 7);
            Assert.That(Save(storage, harness.Data, validator, out string secondError), Is.True, secondError);

            string primary = (string)storage.GetType().GetProperty("PrimaryPath").GetValue(storage);
            File.WriteAllText(primary, File.ReadAllText(primary).Replace("\"objectiveId\":\"valid\"", "\"objectiveId\":\"unknown\""));

            object[] loadArguments = { validator, null, false, false, null };
            MethodInfo load = storage.GetType().GetMethods()
                .Single(method => method.Name == "TryLoad" && method.GetParameters().Length == 5);
            Assert.That((bool)load.Invoke(storage, loadArguments), Is.True, loadArguments[4] as string);
            Assert.That((bool)loadArguments[2], Is.True, "A semantically invalid primary must select the backup.");
            IList states = (IList)Field(loadArguments[1], "objectiveProgress");
            Assert.That((string)Field(states[0], "objectiveId"), Is.EqualTo("valid"));
        }

        [Test]
        public void ObjectivePanelRefreshesAtCompletionAndClaimsFromItsButton()
        {
            Harness harness = CreateHarness(Objectives(Objective("visible", "machine_placed", "spawner", 1)), 10);
            GameObject panelOwner = Owner("ObjectivePanelTest");
            Component panel = panelOwner.AddComponent(ProductionType("ObjectivePanelUI"));
            Type textType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", true);
            Component title = Owner("Title", typeof(RectTransform), textType).GetComponent(textType);
            Component details = Owner("Details", typeof(RectTransform), textType).GetComponent(textType);
            Component recommendation = Owner("Recommendation", typeof(RectTransform), textType).GetComponent(textType);
            Component feedback = Owner("Feedback", typeof(RectTransform), textType).GetComponent(textType);
            Button claimButton = Owner("Claim", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
            SetPublic(panel, "titleText", title);
            SetPublic(panel, "detailsText", details);
            SetPublic(panel, "recommendationText", recommendation);
            SetPublic(panel, "feedbackText", feedback);
            SetPublic(panel, "claimButton", claimButton);
            panel.GetType().GetMethods().Single(method =>
                    method.Name == "Bind" && method.GetParameters().Length == 1)
                .Invoke(panel, new[] { harness.Manager });

            Assert.That(Text(title), Does.Contain("OPTIONAL"));
            Assert.That(Text(details), Does.Contain("0/1"));
            Assert.That(claimButton.gameObject.activeSelf, Is.False);

            Record(harness.Manager, "place", "machine_placed", "spawner", 1, 0);
            Assert.That(claimButton.gameObject.activeSelf, Is.True);
            Assert.That(Text(feedback), Does.Contain("complete"));
            claimButton.onClick.Invoke();
            Assert.That(Balance(harness.Credits), Is.EqualTo(11));
            Assert.That(Text(feedback), Does.Contain("claimed"));
        }

        private Harness CreateHarness(
            string definitions,
            int credits,
            bool initializeCredits = true,
            object data = null,
            bool reset = true)
        {
            GameObject owner = Owner("ProgressionHarness");
            Component creditComponent = owner.AddComponent(ProductionType("CreditsManager"));
            creditComponent.GetType().GetMethod("SetCredits", new[] { typeof(int), typeof(bool) })
                .Invoke(creditComponent, new object[] { credits, false });
            Component manager = owner.AddComponent(ProductionType("ProgressionManager"));
            Assert.That((bool)manager.GetType().GetMethod("Initialize")
                .Invoke(manager, new object[] { initializeCredits ? creditComponent : null, definitions }), Is.True);
            data ??= NewGameData();
            object[] arguments = { data, null };
            string method = reset ? "ResetForNewGame" : "LoadFromGameData";
            Assert.That((bool)manager.GetType().GetMethod(method).Invoke(manager, arguments), Is.True, arguments[1] as string);
            return new Harness(manager, creditComponent, data);
        }

        private GameObject Owner(string name, params Type[] components)
        {
            GameObject owner = components.Length == 0 ? new GameObject(name) : new GameObject(name, components);
            owners.Add(owner);
            return owner;
        }

        private static bool Record(object manager, string id, string type, string target, int count, int value)
        {
            MethodInfo method = manager.GetType().GetMethods().Single(candidate =>
                candidate.Name == "RecordEvent" && candidate.GetParameters().Length == 6);
            return (bool)method.Invoke(manager, new object[] { id, type, target, count, value, "test" });
        }

        private static bool Claim(object manager, string id, out string error)
        {
            object[] arguments = { id, null };
            bool result = (bool)manager.GetType().GetMethod("TryClaimReward").Invoke(manager, arguments);
            error = arguments[1] as string;
            return result;
        }

        private bool GrantLicense(string machineId, out string error)
        {
            object[] arguments = { machineId, "test_grant", null };
            bool granted = (bool)registry.GetType().GetMethod("TryGrantMachineLicense")
                .Invoke(registry, arguments);
            error = arguments[2] as string;
            return granted;
        }

        private void ResetLicenses()
        {
            object[] arguments = { NewGameData(), null };
            Assert.That((bool)registry.GetType().GetMethod("TryLoadFromGameData")
                .Invoke(registry, arguments), Is.True, arguments[1] as string);
        }

        private static int Progress(object manager, string id) => Property<int>(State(manager, id), "Progress");
        private static object State(object manager, string id) =>
            manager.GetType().GetMethod("GetObjectiveState").Invoke(manager, new object[] { id });
        private static int Balance(object credits) =>
            (int)credits.GetType().GetMethod("GetCredits").Invoke(credits, null);
        private static T Property<T>(object owner, string name) =>
            (T)owner.GetType().GetProperty(name).GetValue(owner);
        private static object Field(object owner, string name) => owner.GetType().GetField(name).GetValue(owner);
        private static void Field(object owner, string name, object value) => owner.GetType().GetField(name).SetValue(owner, value);
        private static void SetPublic(object owner, string name, object value) => owner.GetType().GetField(name).SetValue(owner, value);
        private static string Text(object text) => (string)text.GetType().GetProperty("text").GetValue(text);
        private static object NewGameData() => ProductionType("GameData").GetMethod("CreateNewGame").Invoke(null, null);
        private static Type ProductionType(string name) => Type.GetType($"{name}, Assembly-CSharp", true);
        private static object InvokeStatic(string type, string method, params object[] arguments) =>
            ProductionType(type).GetMethod(method).Invoke(null, arguments);

        private static Delegate CreateValidator(object manager)
        {
            Type funcType = typeof(Func<,>).MakeGenericType(ProductionType("GameData"), typeof(string));
            return Delegate.CreateDelegate(funcType, manager, manager.GetType().GetMethod("ValidateSaveCandidate"));
        }

        private static bool Save(object storage, object data, Delegate validator, out string error)
        {
            object[] arguments = { data, validator, null };
            MethodInfo method = storage.GetType().GetMethods()
                .Single(candidate => candidate.Name == "TrySave" && candidate.GetParameters().Length == 3);
            bool result = (bool)method.Invoke(storage, arguments);
            error = arguments[2] as string;
            return result;
        }

        private static string Objectives(params string[] objectives) =>
            $"{{\"objectives\":[{string.Join(",", objectives)}]}}";

        private static string Objective(
            string id,
            string type,
            string target,
            int targetValue,
            string rewardType = "credits",
            int rewardAmount = 1,
            string rewardTarget = null)
        {
            string reward = rewardType == "credits"
                ? $"{{\"type\":\"credits\",\"amount\":{rewardAmount}}}"
                : $"{{\"type\":\"machine_license\",\"targetId\":\"{rewardTarget}\"}}";
            return $"{{\"id\":\"{id}\",\"title\":\"{id}\",\"description\":\"test\"," +
                   $"\"type\":\"{type}\",\"targetId\":\"{target}\",\"targetValue\":{targetValue}," +
                   $"\"reward\":{reward}}}";
        }

        private sealed class Harness
        {
            public Harness(object manager, object credits, object data)
            {
                Manager = manager;
                Credits = credits;
                Data = data;
            }
            public object Manager { get; }
            public object Credits { get; }
            public object Data { get; }
        }
    }
}
