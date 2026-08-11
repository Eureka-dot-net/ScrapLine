using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ScrapLine.Tests.EditMode
{
    public sealed class GridExpansionCostTests
    {
        [TestCase(7, 5, 100, 2f, 170)]
        [TestCase(1, 1, 100, 2f, 102)]
        [TestCase(10, 10, 50, 1.5f, 200)]
        public void ComputeExpansionCostUsesCurrentGridArea(
            int rows,
            int columns,
            int baseCost,
            float growthFactor,
            int expectedCost)
        {
            Type serviceType = Type.GetType("GridExpansionService, Assembly-CSharp", true);
            GameObject gameObject = new GameObject("GridExpansionCostTests");
            try
            {
                Component service = gameObject.AddComponent(serviceType);
                serviceType.GetField("baseCost").SetValue(service, baseCost);
                serviceType.GetField("growthFactor").SetValue(service, growthFactor);
                serviceType.GetField("enableExpansionLogs").SetValue(service, false);
                Type expansionType = serviceType.GetNestedType("ExpansionType");
                object insertRow = Enum.Parse(expansionType, "InsertRow");
                MethodInfo compute = serviceType.GetMethod("ComputeExpansionCost");

                int actualCost = (int)compute.Invoke(service, new[] { (object)rows, columns, insertRow });

                Assert.That(actualCost, Is.EqualTo(expectedCost));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
    }
}
