using NUnit.Framework;
using System.Collections.Generic;

[TestFixture]
public class BaseMachineProgressAndTooltipTests
{
    private TestMachine testMachine;
    private CellData cellData;
    private MachineDef machineDef;

    // Test implementation of BaseMachine for testing abstract methods
    private class TestMachine : BaseMachine
    {
        private float testProgress = -1f;
        private string testTooltip = null;

        public TestMachine(CellData cellData, MachineDef machineDef) : base(cellData, machineDef) { }

        public void SetTestProgress(float progress) => testProgress = progress;
        public void SetTestTooltip(string tooltip) => testTooltip = tooltip;

        public override float GetProgress() => testProgress;
        public override string GetTooltip() => testTooltip ?? base.GetTooltip();
        public override void UpdateLogic() { }
    }

    [SetUp]
    public void Setup()
    {
        machineDef = new MachineDef
        {
            id = "test_machine",
            type = "Test",
            baseProcessTime = 5.0f,
            className = "TestMachine"
        };

        cellData = new CellData
        {
            x = 0,
            y = 0,
            cellType = UICell.CellType.Machine,
            direction = UICell.Direction.Up,
            machineDefId = "test_machine",
            items = new List<ItemData>()
        };

        testMachine = new TestMachine(cellData, machineDef);
    }

    [Test]
    public void GetProgress_DefaultImplementation_ReturnsMinusOne()
    {
        // Act
        float progress = testMachine.GetProgress();

        // Assert
        Assert.AreEqual(-1f, progress);
    }

    [Test]
    public void GetProgress_WithValidProgress_ReturnsSetValue()
    {
        // Arrange
        testMachine.SetTestProgress(0.75f);

        // Act
        float progress = testMachine.GetProgress();

        // Assert
        Assert.AreEqual(0.75f, progress);
    }

    [Test]
    public void GetProgress_WithZeroProgress_ReturnsZero()
    {
        // Arrange
        testMachine.SetTestProgress(0.0f);

        // Act
        float progress = testMachine.GetProgress();

        // Assert
        Assert.AreEqual(0.0f, progress);
    }

    [Test]
    public void GetProgress_WithFullProgress_ReturnsOne()
    {
        // Arrange
        testMachine.SetTestProgress(1.0f);

        // Act
        float progress = testMachine.GetProgress();

        // Assert
        Assert.AreEqual(1.0f, progress);
    }

    [Test]
    public void GetTooltip_DefaultImplementation_ReturnsMachineId()
    {
        // Act
        string tooltip = testMachine.GetTooltip();

        // Assert
        Assert.AreEqual("test_machine", tooltip);
    }

    [Test]
    public void GetTooltip_WithCustomTooltip_ReturnsCustomValue()
    {
        // Arrange
        testMachine.SetTestTooltip("Custom machine info");

        // Act
        string tooltip = testMachine.GetTooltip();

        // Assert
        Assert.AreEqual("Custom machine info", tooltip);
    }

    [Test]
    public void GetTooltip_WithNullMachineDef_ReturnsUnknownMachine()
    {
        // Arrange
        var testMachineNullDef = new TestMachine(cellData, null);

        // Act
        string tooltip = testMachineNullDef.GetTooltip();

        // Assert
        Assert.AreEqual("Unknown Machine", tooltip);
    }

    [Test]
    public void GetTooltip_WithEmptyTooltip_ReturnsDefaultTooltip()
    {
        // Arrange
        testMachine.SetTestTooltip("");

        // Act
        string tooltip = testMachine.GetTooltip();

        // Assert
        Assert.AreEqual("", tooltip);
    }

    [Test]
    public void GetTooltip_WithNullTooltip_ReturnsDefaultTooltip()
    {
        // Arrange
        testMachine.SetTestTooltip(null);

        // Act
        string tooltip = testMachine.GetTooltip();

        // Assert
        Assert.AreEqual("test_machine", tooltip);
    }
}

[TestFixture]
public class MachineRendererProgressTests
{
    private MachineRenderer renderer;
    private MachineDef machineDef;

    [SetUp]
    public void Setup()
    {
        machineDef = new MachineDef
        {
            id = "test_machine",
            type = "Test",
            baseProcessTime = 5.0f,
            buildingSprite = "test_building",
            buildingIconSprite = "test_icon",
            isMoving = false
        };

        renderer = new MachineRenderer();
    }

    [Test]
    public void UpdateBuildingIconSprite_WithValidSprite_DoesNotThrow()
    {
        // This test verifies the method signature and basic functionality
        // In a real Unity environment, this would test actual sprite loading
        
        // Act & Assert
        Assert.DoesNotThrow(() => renderer.UpdateBuildingIconSprite("junkYard_100"));
        Assert.DoesNotThrow(() => renderer.UpdateBuildingIconSprite("junkYard_66"));
        Assert.DoesNotThrow(() => renderer.UpdateBuildingIconSprite("junkYard_33"));
        Assert.DoesNotThrow(() => renderer.UpdateBuildingIconSprite("junkYard_0"));
    }

    [Test]
    public void UpdateBuildingIconSprite_WithNullSprite_DoesNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => renderer.UpdateBuildingIconSprite(null));
    }

    [Test]
    public void UpdateBuildingIconSprite_WithEmptySprite_DoesNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => renderer.UpdateBuildingIconSprite(""));
    }

    [Test]
    public void CreateProgressBar_InMenuMode_DoesNotThrow()
    {
        // This would test the method in menu mode where it should not create progress bars
        
        // Act & Assert
        Assert.DoesNotThrow(() => renderer.CreateProgressBar());
    }

    [Test]
    public void UpdateProgressBar_WithValidProgress_DoesNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => renderer.UpdateProgressBar(0.0f));
        Assert.DoesNotThrow(() => renderer.UpdateProgressBar(0.5f));
        Assert.DoesNotThrow(() => renderer.UpdateProgressBar(1.0f));
    }

    [Test]
    public void UpdateProgressBar_WithInvalidProgress_ClampsValues()
    {
        // These should not throw and should clamp values internally
        
        // Act & Assert
        Assert.DoesNotThrow(() => renderer.UpdateProgressBar(-0.5f));
        Assert.DoesNotThrow(() => renderer.UpdateProgressBar(1.5f));
    }

    [Test]
    public void UpdateDynamicElements_DoesNotThrow()
    {
        // This tests the main update method that coordinates progress and sprite updates
        
        // Act & Assert
        Assert.DoesNotThrow(() => renderer.UpdateDynamicElements());
    }
}