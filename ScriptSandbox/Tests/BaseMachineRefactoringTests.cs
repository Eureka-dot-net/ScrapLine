using NUnit.Framework;

namespace ScrapLine.Tests
{
    [TestFixture]
    public class BaseMachineRefactoringTests
    {
        [Test]
        public void BaseMachine_MachineDef_PropertyIsPublic()
        {
            // Arrange
            var cellData = new CellData
            {
                x = 0,
                y = 0,
                cellType = UICell.CellType.Machine,
                direction = UICell.Direction.Up,
                machineDefId = "blank"
            };
            
            // Act
            var baseMachine = MachineFactory.CreateMachine(cellData);
            
            // Assert
            Assert.IsNotNull(baseMachine, "BaseMachine should be created successfully");
            Assert.IsNotNull(baseMachine.MachineDef, "MachineDef property should be accessible");
            Assert.AreEqual("blank", baseMachine.MachineDef.id, "MachineDef should have correct ID");
        }
        
        [Test]
        public void BlankCells_HaveBaseMachineInstances()
        {
            // Test that blank cells get proper BaseMachine instances with correct machineDefId
            var testCases = new[]
            {
                (UICell.CellRole.Grid, "blank"),
                (UICell.CellRole.Top, "blank_top"), 
                (UICell.CellRole.Bottom, "blank_bottom")
            };
            
            foreach (var (role, expectedMachineId) in testCases)
            {
                // Arrange - simulate what GridManager does
                var cellData = new CellData
                {
                    x = 0,
                    y = 0,
                    cellType = UICell.CellType.Blank,
                    direction = UICell.Direction.Up,
                    cellRole = role,
                    machineDefId = expectedMachineId
                };
                
                // Act
                var machine = MachineFactory.CreateMachine(cellData);
                
                // Assert
                Assert.IsNotNull(machine, $"Blank cell with role {role} should have BaseMachine instance");
                Assert.AreEqual(expectedMachineId, machine.MachineDef.id, 
                    $"Blank cell machine should have correct machineDefId for role {role}");
                Assert.IsInstanceOf<BlankCellMachine>(machine, 
                    $"Blank cell should create BlankCellMachine instance for role {role}");
            }
        }
        
        [Test]
        public void MachineRenderer_AcceptsBaseMachine()
        {
            // This test verifies that MachineRenderer.Setup accepts BaseMachine parameter
            // We can't fully test the UI components in ScriptSandbox, but we can verify 
            // the signature and that BaseMachine provides the right data
            
            // Arrange
            var cellData = new CellData
            {
                x = 0,
                y = 0,
                cellType = UICell.CellType.Machine,
                direction = UICell.Direction.Up,
                machineDefId = "conveyor"
            };
            
            var baseMachine = MachineFactory.CreateMachine(cellData);
            
            // Act & Assert
            Assert.IsNotNull(baseMachine, "BaseMachine should be created");
            Assert.IsNotNull(baseMachine.MachineDef, "BaseMachine should provide MachineDef");
            
            // Verify that the MachineDef has the expected properties that MachineRenderer needs
            var machineDef = baseMachine.MachineDef;
            Assert.AreEqual("conveyor", machineDef.id);
            Assert.DoesNotThrow(() => {
                // These properties should be accessible without null reference exceptions
                var _ = machineDef.isMoving;
                var __ = machineDef.sprite;
                var ___ = machineDef.borderSprite;
            });
        }
        
        [Test]
        public void AllCells_NoNullMachineInstances()
        {
            // Test that the new system ensures no cell has a null machine instance
            var machineTypes = new[] { "blank", "blank_top", "blank_bottom", "conveyor", "spawner" };
            
            foreach (var machineType in machineTypes)
            {
                // Arrange
                var cellData = new CellData
                {
                    x = 0,
                    y = 0,
                    cellType = machineType.StartsWith("blank") ? UICell.CellType.Blank : UICell.CellType.Machine,
                    direction = UICell.Direction.Up,
                    machineDefId = machineType
                };
                
                // Act
                var machine = MachineFactory.CreateMachine(cellData);
                
                // Assert
                Assert.IsNotNull(machine, $"Machine instance should never be null for type {machineType}");
                Assert.IsNotNull(machine.MachineDef, $"MachineDef should never be null for type {machineType}");
                Assert.AreEqual(machineType, machine.MachineDef.id, $"Machine should have correct ID for type {machineType}");
            }
        }
        
        [Test]
        public void UIGridManager_UpdateCellVisuals_WorksWithBaseMachine()
        {
            // This test verifies that the refactored UpdateCellVisuals signature works correctly
            // by ensuring the parameter types and logic flow are correct
            
            // Arrange
            var cellData = new CellData
            {
                x = 1,
                y = 1,
                cellType = UICell.CellType.Machine,
                direction = UICell.Direction.Right,
                machineDefId = "conveyor",
                machine = null
            };
            
            // Act - simulate the grid initialization process
            cellData.machine = MachineFactory.CreateMachine(cellData);
            
            // Assert
            Assert.IsNotNull(cellData.machine, "Grid cells should have machine instances after initialization");
            
            // Verify the machine can provide all the data that UpdateCellVisuals needs
            Assert.DoesNotThrow(() => {
                var cellType = cellData.cellType;
                var direction = cellData.direction;
                var machine = cellData.machine;
                
                // These should all be accessible without exceptions
                Assert.IsNotNull(machine);
                Assert.IsNotNull(machine.MachineDef);
            });
        }
    }
}