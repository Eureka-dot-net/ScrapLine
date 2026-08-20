using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ScrapLine.Tests.EditMode
{
    public class SpawnerOwnershipTests
    {
        private Type cellType;
        private Type spawnerType;
        private object spawnerDefinition;

        [SetUp]
        public void SetUp()
        {
            Type registryType = ProductionType("FactoryRegistry");
            object registry = registryType.GetProperty("Instance").GetValue(null);
            registryType.GetMethod("LoadFromJson").Invoke(registry, new object[]
            {
                Resources.Load<TextAsset>("machines").text,
                Resources.Load<TextAsset>("recipes").text,
                Resources.Load<TextAsset>("items").text,
                Resources.Load<TextAsset>("wastecrates").text,
                null
            });

            cellType = ProductionType("CellData");
            spawnerType = ProductionType("SpawnerMachine");
            spawnerDefinition = registryType.GetMethod("GetMachine").Invoke(registry, new object[] { "spawner" });
        }

        [Test]
        public void PurchasedCrateActivatesOnlyOnSelectedSpawner()
        {
            object firstCell = CreateSpawnerCell(0);
            object selectedCell = CreateSpawnerCell(1);
            CreateSpawner(firstCell);
            object selectedSpawner = CreateSpawner(selectedCell);

            bool queued = (bool)spawnerType.GetMethod("TryEnqueueDelivery")
                .Invoke(selectedSpawner, new object[] { "starter_crate" });

            Assert.That(queued, Is.True);
            Assert.That(GetField(firstCell, "wasteCrate"), Is.Null);
            object selectedCrate = GetField(selectedCell, "wasteCrate");
            Assert.That(selectedCrate, Is.Not.Null);
            Assert.That(GetField(selectedCrate, "wasteCrateDefId"), Is.EqualTo("starter_crate"));
        }

        [Test]
        public void FollowUpDeliveryBelongsToSelectedSpawner()
        {
            object firstCell = CreateSpawnerCell(0);
            object selectedCell = CreateSpawnerCell(1);
            CreateSpawner(firstCell);
            object selectedSpawner = CreateSpawner(selectedCell);
            SetField(selectedCell, "wasteCrate", CreateActiveCrate("starter_crate"));

            bool queued = (bool)spawnerType.GetMethod("TryEnqueueDelivery")
                .Invoke(selectedSpawner, new object[] { "starter_crate" });

            Assert.That(queued, Is.True);
            Assert.That(GetField(firstCell, "wasteCrate"), Is.Null);
            Assert.That((IList)GetField(firstCell, "wasteDeliveryQueue"), Is.Empty);
            Assert.That((IList)GetField(selectedCell, "wasteDeliveryQueue"),
                Is.EqualTo(new[] { "starter_crate" }));
        }

        [Test]
        public void DeliveryQueueSurvivesJsonRoundTrip()
        {
            Type gameDataType = ProductionType("GameData");
            object data = gameDataType.GetMethod("CreateNewGame").Invoke(null, null);
            object cell = CreateSpawnerCell(0);
            ((IList)GetField(cell, "wasteDeliveryQueue")).Add("starter_crate");

            Type gridType = ProductionType("GridData");
            object grid = Activator.CreateInstance(gridType);
            SetField(grid, "width", 1);
            SetField(grid, "height", 1);
            ((IList)GetField(grid, "cells")).Add(cell);
            ((IList)GetField(data, "grids")).Add(grid);

            object loaded = JsonUtility.FromJson(JsonUtility.ToJson(data), gameDataType);
            loaded = ProductionType("GameSaveMigrations").GetMethod("Migrate")
                .Invoke(null, new[] { loaded });

            object loadedGrid = ((IList)GetField(loaded, "grids"))[0];
            object loadedCell = ((IList)GetField(loadedGrid, "cells"))[0];
            Assert.That((IList)GetField(loadedCell, "wasteDeliveryQueue"),
                Is.EqualTo(new[] { "starter_crate" }));
        }

        [Test]
        public void MixedDeliveriesActivateInPurchaseOrder()
        {
            object cell = CreateSpawnerCell(0);
            object spawner = CreateSpawner(cell);
            SetField(cell, "wasteCrate", CreateActiveCrate("mixed_bale"));

            Assert.That(Invoke<bool>(spawner, "TryEnqueueDelivery", "starter_crate"), Is.True);
            Assert.That(Invoke<bool>(spawner, "TryEnqueueDelivery", "plastic_bale"), Is.True);

            EmptyActiveCrate(cell);
            Invoke(spawner, "UpdateLogic");
            Assert.That(GetField(GetField(cell, "wasteCrate"), "wasteCrateDefId"), Is.EqualTo("starter_crate"));
            Assert.That((IList)GetField(cell, "wasteDeliveryQueue"), Is.EqualTo(new[] { "plastic_bale" }));

            EmptyActiveCrate(cell);
            Invoke(spawner, "UpdateLogic");
            Assert.That(GetField(GetField(cell, "wasteCrate"), "wasteCrateDefId"), Is.EqualTo("plastic_bale"));
            Assert.That((IList)GetField(cell, "wasteDeliveryQueue"), Is.Empty);
        }

        [Test]
        public void DeliveryQueueHoldsThreeUnopenedBales()
        {
            object cell = CreateSpawnerCell(0);
            object spawner = CreateSpawner(cell);
            SetField(cell, "wasteCrate", CreateActiveCrate("starter_crate"));

            Assert.That(Invoke<bool>(spawner, "TryEnqueueDelivery", "starter_crate"), Is.True);
            Assert.That(Invoke<bool>(spawner, "TryEnqueueDelivery", "plastic_bale"), Is.True);
            Assert.That(Invoke<bool>(spawner, "TryEnqueueDelivery", "mixed_bale"), Is.True);
            Assert.That(Invoke<bool>(spawner, "TryEnqueueDelivery", "bulk_mixed_bale"), Is.False);
        }

        [Test]
        public void SpawnerNoLongerExposesRequiredCrateConfiguration()
        {
            Assert.That(spawnerType.GetProperty("RequiredCrateId"), Is.Null);
            Assert.That(cellType.GetField("requiredCrateId"), Is.Null);
        }

        [Test]
        public void EmptySpawnerWasteCrateLookupIsSafe()
        {
            object registry = ProductionType("FactoryRegistry").GetProperty("Instance").GetValue(null);
            MethodInfo getWasteCrate = registry.GetType().GetMethod("GetWasteCrate");

            Assert.That(getWasteCrate.Invoke(registry, new object[] { null }), Is.Null);
            Assert.That(getWasteCrate.Invoke(registry, new object[] { string.Empty }), Is.Null);
        }

        [Test]
        public void DeletingSpawnerFullyRefundsUnopenedDeliveries()
        {
            GameObject owner = new GameObject("SpawnerRefundTest");
            try
            {
                Component credits = owner.AddComponent(ProductionType("CreditsManager"));
                credits.GetType().GetMethod("SetCredits", new[] { typeof(int) }).Invoke(credits, new object[] { 0 });
                Component supply = owner.AddComponent(ProductionType("WasteSupplyManager"));
                SetField(supply, "creditsManager", credits);
                object cell = CreateSpawnerCell(0);
                ((IList)GetField(cell, "wasteDeliveryQueue")).Add("starter_crate");
                ((IList)GetField(cell, "wasteDeliveryQueue")).Add("plastic_bale");

                int refund = Invoke<int>(supply, "RefundQueuedDeliveries", cell);

                Assert.That(refund, Is.EqualTo(80));
                Assert.That(credits.GetType().GetMethod("GetCredits").Invoke(credits, null), Is.EqualTo(80));
                Assert.That((IList)GetField(cell, "wasteDeliveryQueue"), Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        private object CreateSpawnerCell(int x)
        {
            object cell = Activator.CreateInstance(cellType);
            SetField(cell, "x", x);
            SetField(cell, "y", 0);
            SetField(cell, "machineDefId", "spawner");
            return cell;
        }

        private object CreateSpawner(object cell)
        {
            return Activator.CreateInstance(spawnerType, cell, spawnerDefinition);
        }

        private static object CreateActiveCrate(string crateId)
        {
            Type crateType = ProductionType("WasteCrateInstance");
            Type itemType = ProductionType("WasteCrateItemDef");
            object crate = Activator.CreateInstance(crateType);
            object item = Activator.CreateInstance(itemType);
            SetField(crate, "wasteCrateDefId", crateId);
            SetField(item, "itemType", "can");
            SetField(item, "count", 1);
            ((IList)GetField(crate, "remainingItems")).Add(item);
            return crate;
        }

        private static void EmptyActiveCrate(object cell)
        {
            object crate = GetField(cell, "wasteCrate");
            foreach (object item in (IList)GetField(crate, "remainingItems"))
                SetField(item, "count", 0);
        }

        private static void Invoke(object instance, string method)
        {
            instance.GetType().GetMethod(method).Invoke(instance, null);
        }

        private static T Invoke<T>(object instance, string method, params object[] arguments)
        {
            return (T)instance.GetType().GetMethod(method).Invoke(instance, arguments);
        }

        private static Type ProductionType(string name)
        {
            return Type.GetType($"{name}, Assembly-CSharp", true);
        }

        private static object GetField(object instance, string name)
        {
            return instance.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance).GetValue(instance);
        }

        private static void SetField(object instance, string name, object value)
        {
            instance.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance).SetValue(instance, value);
        }
    }
}
