using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ScrapLine.Tests.EditMode
{
    public sealed class BaselineEconomyTests
    {
        [Test]
        public void MachineAndItemPricesMatchPlaytestBaseline()
        {
            object registry = LoadProductionRegistry();
            IDictionary machines = GetDictionary(registry, "Machines");
            IDictionary items = GetDictionary(registry, "Items");

            AssertFieldValues(machines, "cost", new Hashtable
            {
                { "conveyor", 10 }, { "spawner", 50 }, { "seller", 50 }, { "shredder", 100 },
                { "granulator", 125 }, { "sorter", 150 }, { "plate_press", 250 }, { "fabricator", 500 }
            });
            AssertFieldValues(items, "sellValue", new Hashtable
            {
                { "can", 2 }, { "plasticBottle", 2 }, { "shreddedAluminum", 6 },
                { "granulatedPlastic", 6 }, { "aluminumPlate", 16 },
                { "reinforcedAluminumPlate", 75 }
            });
        }

        [Test]
        public void NewGameCanAffordStarterLineAndReceivesFreeCanBale()
        {
            object registry = LoadProductionRegistry();
            IDictionary machines = GetDictionary(registry, "Machines");
            int starterLineCost = GetIntField(machines["spawner"], "cost") +
                                  GetIntField(machines["seller"], "cost") +
                                  (5 * GetIntField(machines["conveyor"], "cost"));

            Type creditsType = Type.GetType("CreditsManager, Assembly-CSharp", true);
            Type gameDataType = Type.GetType("GameData, Assembly-CSharp", true);
            GameObject gameObject = new GameObject("BaselineEconomyTests");
            try
            {
                Component creditsManager = gameObject.AddComponent(creditsType);
                int startingCredits = GetIntField(creditsManager, "startingCredits");
                object gameData = Activator.CreateInstance(gameDataType);
                IList wasteQueue = (IList)gameDataType.GetField("wasteQueue").GetValue(gameData);

                Assert.That(startingCredits, Is.EqualTo(250));
                Assert.That(starterLineCost, Is.EqualTo(150));
                Assert.That(startingCredits - starterLineCost, Is.EqualTo(100),
                    "The opening budget must retain the documented mistake buffer.");
                Assert.That(wasteQueue, Is.EqualTo(new[] { "starter_crate" }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }

            string scene = File.ReadAllText(Path.Combine(Application.dataPath, "Scenes/MobileGridScene.unity"));
            StringAssert.Contains("startingCredits: 250", scene,
                "MobileGridScene must agree with the CreditsManager code default.");
        }

        [Test]
        public void FallbackCratePricingMatchesDataRule()
        {
            object registry = LoadProductionRegistry();
            IDictionary crates = GetDictionary(registry, "WasteCrates");
            object canBale = crates["starter_crate"];
            Type spawnerType = Type.GetType("SpawnerMachine, Assembly-CSharp", true);
            MethodInfo calculate = spawnerType.GetMethod("CalculateWasteCrateCost", BindingFlags.Public | BindingFlags.Static);

            int calculatedCost = (int)calculate.Invoke(null, new[] { canBale });

            Assert.That(calculatedCost, Is.EqualTo(GetIntField(canBale, "cost")));
            Assert.That(calculatedCost, Is.EqualTo(40));
        }

        private static object LoadProductionRegistry()
        {
            Type registryType = Type.GetType("FactoryRegistry, Assembly-CSharp", true);
            object registry = registryType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)
                .GetValue(null);
            MethodInfo load = registryType.GetMethod("LoadFromJson", BindingFlags.Public | BindingFlags.Instance);
            load.Invoke(registry, new object[]
            {
                Resources.Load<TextAsset>("machines").text,
                Resources.Load<TextAsset>("recipes").text,
                Resources.Load<TextAsset>("items").text,
                Resources.Load<TextAsset>("wastecrates").text,
                null
            });
            return registry;
        }

        private static IDictionary GetDictionary(object owner, string field)
        {
            return (IDictionary)owner.GetType().GetField(field).GetValue(owner);
        }

        private static int GetIntField(object owner, string field)
        {
            return (int)owner.GetType().GetField(field).GetValue(owner);
        }

        private static void AssertFieldValues(IDictionary definitions, string field, IDictionary expected)
        {
            foreach (DictionaryEntry entry in expected)
            {
                Assert.That(definitions.Contains(entry.Key), Is.True, $"Missing definition '{entry.Key}'.");
                Assert.That(GetIntField(definitions[entry.Key], field), Is.EqualTo(entry.Value),
                    $"Unexpected {field} for '{entry.Key}'.");
            }
        }
    }
}
