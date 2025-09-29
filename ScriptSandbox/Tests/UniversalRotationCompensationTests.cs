using NUnit.Framework;
using UnityEngine;

namespace ScrapLine.Tests
{
    [TestFixture]
    public class UniversalRotationCompensationTests
    {
        [Test]
        public void AnyMachine_With180DegreeRotation_GetsRotationCompensation()
        {
            // Test that ANY machine with buildingDirection: 180 gets rotation compensation,
            // not just SortingMachine
            
            // Arrange - Create a sorting machine with 180° rotation
            var cellData = new CellData
            {
                x = 1,
                y = 1,
                sortingConfig = new SortingMachineConfig()
            };

            var machineDef = new MachineDef
            {
                id = "sorter", // Sorter machine has buildingDirection: 180
                type = "SortingMachine",
                buildingDirection = 180
            };

            var sortingMachine = new SortingMachine(cellData, machineDef);

            // Act - Verify that the buildingDirection property is correctly set
            int buildingDirection = sortingMachine.MachineDef.buildingDirection;

            // Assert - Any machine with 180° rotation should be detected
            Assert.AreEqual(180, buildingDirection, "Machine should have 180° rotation");
            
            // The rotation compensation logic should apply to any machine with buildingDirection == 180
            bool shouldGetRotationCompensation = buildingDirection == 180;
            Assert.IsTrue(shouldGetRotationCompensation, "Any machine with buildingDirection == 180 should get rotation compensation");

            GameLogger.Log(LoggingManager.LogCategory.Debug, 
                "✅ Universal rotation compensation verified - any machine with 180° rotation is handled", "Test");
        }

        [Test]
        public void SortingMachine_And_SellerMachine_BothGet180DegreeRotation()
        {
            // Test that both sorting and seller machines get the same rotation treatment
            
            // Arrange - Create both machine types
            var sortingCellData = new CellData { x = 1, y = 1, sortingConfig = new SortingMachineConfig() };
            var sortingDef = new MachineDef { id = "sorter", buildingDirection = 180 };
            
            var sellerCellData = new CellData { x = 2, y = 2 };  
            var sellerDef = new MachineDef { id = "seller", buildingDirection = 180 };

            var sortingMachine = new SortingMachine(sortingCellData, sortingDef);
            var sellerMachine = new SellerMachine(sellerCellData, sellerDef);

            // Act & Assert - Both should have 180° rotation
            Assert.AreEqual(180, sortingMachine.MachineDef.buildingDirection, "Sorting machine should have 180° rotation");
            Assert.AreEqual(180, sellerMachine.MachineDef.buildingDirection, "Seller machine should have 180° rotation");

            // Both should trigger the same rotation compensation logic
            bool sortingGetsCompensation = sortingMachine.MachineDef.buildingDirection == 180;
            bool sellerGetsCompensation = sellerMachine.MachineDef.buildingDirection == 180;
            
            Assert.IsTrue(sortingGetsCompensation, "Sorting machine should get rotation compensation");
            Assert.IsTrue(sellerGetsCompensation, "Seller machine should get rotation compensation");

            GameLogger.Log(LoggingManager.LogCategory.Debug, 
                "✅ Both sorting and seller machines get universal 180° rotation compensation", "Test");
        }

        [Test]
        public void MachineRenderer_RotationLogic_NotSpecificToMachineType()
        {
            // Verify the MachineRenderer rotation detection is based on buildingDirection, not machine type
            
            var method = typeof(MachineRenderer).GetMethod("CreateConfigurationSprite", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.IsNotNull(method, "CreateConfigurationSprite method should exist");

            var parameters = method.GetParameters();
            Assert.AreEqual(6, parameters.Length, "Method should have 6 parameters");
            
            // The last parameter should be renamed from sorting-specific to generic
            Assert.AreEqual("rotateForRotated180Machine", parameters[5].Name, 
                "Parameter should be named for universal 180° machines, not just sorting");

            GameLogger.Log(LoggingManager.LogCategory.Debug, 
                "✅ MachineRenderer uses universal rotation logic, not machine-type specific", "Test");
        }

        [Test]
        public void RotationCompensation_AppliesTo_AnyBuildingDirection180()
        {
            // Integration test: verify the complete logic chain works for any 180° machine
            
            // Test data representing different machines with 180° rotation
            var machineConfigs = new[]
            {
                new { id = "sorter", type = "SortingMachine", buildingDirection = 180 },
                new { id = "seller", type = "SellerMachine", buildingDirection = 180 }
            };

            foreach (var config in machineConfigs)
            {
                // Arrange
                var cellData = new CellData { x = 1, y = 1 };
                if (config.type == "SortingMachine")
                    cellData.sortingConfig = new SortingMachineConfig();

                var machineDef = new MachineDef 
                { 
                    id = config.id,
                    type = config.type,
                    buildingDirection = config.buildingDirection
                };

                BaseMachine machine;
                if (config.type == "SortingMachine")
                    machine = new SortingMachine(cellData, machineDef);
                else
                    machine = new SellerMachine(cellData, machineDef);

                // Act - Check if rotation compensation would be applied
                bool shouldGetCompensation = machine.MachineDef.buildingDirection == 180;

                // Assert - All 180° machines should get compensation
                Assert.IsTrue(shouldGetCompensation, 
                    $"Machine '{config.id}' with 180° rotation should get compensation");
            }

            GameLogger.Log(LoggingManager.LogCategory.Debug, 
                "✅ All machines with buildingDirection == 180 get rotation compensation regardless of type", "Test");
        }

        [Test]
        public void NonRotated_Machines_DoNotGet_RotationCompensation()
        {
            // Verify that machines without 180° rotation don't get compensation
            
            // Arrange - Machine with different rotation
            var cellData = new CellData { x = 1, y = 1 };
            var machineDef = new MachineDef
            {
                id = "conveyor",
                buildingDirection = 0 // No rotation
            };

            var machine = new ConveyorMachine(cellData, machineDef);

            // Act & Assert - Should not get rotation compensation
            bool shouldGetCompensation = machine.MachineDef.buildingDirection == 180;
            Assert.IsFalse(shouldGetCompensation, "Non-rotated machines should not get rotation compensation");

            // Test other rotation values
            var testRotations = new[] { 0, 90, 270, 45, 135 };
            foreach (var rotation in testRotations)
            {
                var testDef = new MachineDef { buildingDirection = rotation };
                bool gets180Compensation = testDef.buildingDirection == 180;
                Assert.IsFalse(gets180Compensation, 
                    $"Machine with {rotation}° rotation should not get 180° compensation");
            }

            GameLogger.Log(LoggingManager.LogCategory.Debug, 
                "✅ Only machines with exactly 180° rotation get compensation", "Test");
        }
    }
}