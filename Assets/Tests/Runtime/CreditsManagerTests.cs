using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ScrapLine.Tests
{
    /// <summary>
    /// Unit tests for CreditsManager.
    /// Tests credits tracking, spending, earning, initialization, and UI updates.
    /// </summary>
    [TestFixture]
    public class CreditsManagerTests
    {
        private GameObject gameObject;
        private CreditsManager creditsManager;
        private MockCreditsUI mockCreditsUI;
        private MockMachineBarUIManager mockMachineBarManager;

        [SetUp]
        public void SetUp()
        {
            // Arrange - Create test objects
            gameObject = new GameObject("TestCreditsManager");
            creditsManager = gameObject.AddComponent<CreditsManager>();
            
            // Create mock UI components
            mockCreditsUI = new MockCreditsUI();
            mockMachineBarManager = new MockMachineBarUIManager();
            
            // Initialize with mocks
            creditsManager.Initialize(mockCreditsUI, mockMachineBarManager);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up
            if (gameObject != null)
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        #region Initialization Tests

        [Test]
        public void Initialize_WithValidComponents_SetsReferences()
        {
            // Arrange
            var newCreditsUI = new MockCreditsUI();
            var newMachineBarManager = new MockMachineBarUIManager();

            // Act
            creditsManager.Initialize(newCreditsUI, newMachineBarManager);

            // Assert
            // We can't directly test private fields, but we can test the behavior
            creditsManager.UpdateCreditsDisplay();
            Assert.IsTrue(newCreditsUI.UpdateCreditsCalled, "CreditsUI should be set and called");
            Assert.IsTrue(newMachineBarManager.UpdateAffordabilityCalled, "MachineBarManager should be set and called");
        }

        [Test]
        public void Initialize_WithNullComponents_DoesNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => creditsManager.Initialize(null, null), "Should handle null components gracefully");
        }

        [Test]
        public void InitializeNewGame_SetsStartingCredits()
        {
            // Arrange
            creditsManager.startingCredits = 1500;

            // Act
            creditsManager.InitializeNewGame();

            // Assert
            Assert.AreEqual(1500, creditsManager.GetCredits(), "Should set starting credits");
            Assert.IsTrue(mockCreditsUI.UpdateCreditsCalled, "Should update UI");
            Assert.AreEqual(1500, mockCreditsUI.LastCreditsValue, "Should update UI with correct value");
        }

        [Test]
        public void InitializeNewGame_CallsUpdateCreditsDisplay()
        {
            // Arrange
            mockCreditsUI.Reset();
            mockMachineBarManager.Reset();

            // Act
            creditsManager.InitializeNewGame();

            // Assert
            Assert.IsTrue(mockCreditsUI.UpdateCreditsCalled, "Should call UpdateCredits on UI");
            Assert.IsTrue(mockMachineBarManager.UpdateAffordabilityCalled, "Should call UpdateAffordability on machine bar");
        }

        #endregion

        #region Credits Get/Set Tests

        [Test]
        public void GetCredits_InitialState_ReturnsZero()
        {
            // Arrange & Act
            int credits = creditsManager.GetCredits();

            // Assert
            Assert.AreEqual(0, credits, "Initial credits should be 0");
        }

        [Test]
        public void SetCredits_ValidAmount_SetsCorrectly()
        {
            // Arrange
            mockCreditsUI.Reset();

            // Act
            creditsManager.SetCredits(2500);

            // Assert
            Assert.AreEqual(2500, creditsManager.GetCredits(), "Should set credits correctly");
            Assert.IsTrue(mockCreditsUI.UpdateCreditsCalled, "Should update UI");
            Assert.AreEqual(2500, mockCreditsUI.LastCreditsValue, "Should update UI with correct value");
        }

        [Test]
        public void SetCredits_NegativeAmount_SetsNegativeCredits()
        {
            // Arrange & Act
            creditsManager.SetCredits(-100);

            // Assert
            Assert.AreEqual(-100, creditsManager.GetCredits(), "Should allow negative credits");
        }

        [Test]
        public void SetCredits_Zero_SetsZero()
        {
            // Arrange
            creditsManager.SetCredits(1000); // First set to non-zero

            // Act
            creditsManager.SetCredits(0);

            // Assert
            Assert.AreEqual(0, creditsManager.GetCredits(), "Should set credits to zero");
        }

        #endregion

        #region Add Credits Tests

        [Test]
        public void AddCredits_PositiveAmount_IncreasesCredits()
        {
            // Arrange
            creditsManager.SetCredits(1000);
            mockCreditsUI.Reset();

            // Act
            creditsManager.AddCredits(500);

            // Assert
            Assert.AreEqual(1500, creditsManager.GetCredits(), "Should add credits correctly");
            Assert.IsTrue(mockCreditsUI.UpdateCreditsCalled, "Should update UI");
            Assert.AreEqual(1500, mockCreditsUI.LastCreditsValue, "Should update UI with correct value");
        }

        [Test]
        public void AddCredits_NegativeAmount_DecreasesCredits()
        {
            // Arrange
            creditsManager.SetCredits(1000);

            // Act
            creditsManager.AddCredits(-300);

            // Assert
            Assert.AreEqual(700, creditsManager.GetCredits(), "Should subtract credits correctly");
        }

        [Test]
        public void AddCredits_Zero_DoesNotChangeCredits()
        {
            // Arrange
            creditsManager.SetCredits(1000);
            mockCreditsUI.Reset();

            // Act
            creditsManager.AddCredits(0);

            // Assert
            Assert.AreEqual(1000, creditsManager.GetCredits(), "Should not change credits when adding zero");
            Assert.IsTrue(mockCreditsUI.UpdateCreditsCalled, "Should still update UI");
        }

        [Test]
        public void AddCredits_MultipleOperations_AccumulatesCorrectly()
        {
            // Arrange
            creditsManager.SetCredits(500);

            // Act
            creditsManager.AddCredits(200);
            creditsManager.AddCredits(100);
            creditsManager.AddCredits(-50);

            // Assert
            Assert.AreEqual(750, creditsManager.GetCredits(), "Should accumulate multiple add operations correctly");
        }

        #endregion

        #region Spend Credits Tests

        [Test]
        public void TrySpendCredits_SufficientFunds_Spendsuccessfully()
        {
            // Arrange
            creditsManager.SetCredits(1000);
            mockCreditsUI.Reset();

            // Act
            bool result = creditsManager.TrySpendCredits(300);

            // Assert
            Assert.IsTrue(result, "Should return true for successful spending");
            Assert.AreEqual(700, creditsManager.GetCredits(), "Should deduct spent amount");
            Assert.IsTrue(mockCreditsUI.UpdateCreditsCalled, "Should update UI");
            Assert.AreEqual(700, mockCreditsUI.LastCreditsValue, "Should update UI with correct value");
        }

        [Test]
        public void TrySpendCredits_InsufficientFunds_FailsToSpend()
        {
            // Arrange
            creditsManager.SetCredits(100);
            mockCreditsUI.Reset();

            // Act
            bool result = creditsManager.TrySpendCredits(300);

            // Assert
            Assert.IsFalse(result, "Should return false for insufficient funds");
            Assert.AreEqual(100, creditsManager.GetCredits(), "Should not change credits when spend fails");
            Assert.IsFalse(mockCreditsUI.UpdateCreditsCalled, "Should not update UI when spend fails");
        }

        [Test]
        public void TrySpendCredits_ExactAmount_Spendsuccessfully()
        {
            // Arrange
            creditsManager.SetCredits(500);

            // Act
            bool result = creditsManager.TrySpendCredits(500);

            // Assert
            Assert.IsTrue(result, "Should return true when spending exact amount");
            Assert.AreEqual(0, creditsManager.GetCredits(), "Should have zero credits after spending all");
        }

        [Test]
        public void TrySpendCredits_ZeroAmount_AlwaysSucceeds()
        {
            // Arrange
            creditsManager.SetCredits(0);

            // Act
            bool result = creditsManager.TrySpendCredits(0);

            // Assert
            Assert.IsTrue(result, "Should return true when spending zero");
            Assert.AreEqual(0, creditsManager.GetCredits(), "Should not change credits when spending zero");
        }

        [Test]
        public void TrySpendCredits_NegativeAmount_IncreasesCredits()
        {
            // Arrange
            creditsManager.SetCredits(500);

            // Act
            bool result = creditsManager.TrySpendCredits(-100);

            // Assert
            Assert.IsTrue(result, "Should return true for negative spending (essentially adding)");
            Assert.AreEqual(600, creditsManager.GetCredits(), "Should increase credits when spending negative amount");
        }

        [UnityTest]
        public System.Collections.IEnumerator TrySpendCredits_InsufficientFunds_LogsWarning()
        {
            // Arrange
            creditsManager.enableCreditsLogs = true;
            creditsManager.SetCredits(50);

            // Act
            LogAssert.Expect(LogType.Warning, "Insufficient credits! Need 100, have 50");
            creditsManager.TrySpendCredits(100);

            // Assert
            yield return null; // Wait one frame for log assertion
        }

        [Test]
        public void TrySpendCredits_InsufficientFundsWithLogsDisabled_DoesNotLog()
        {
            // Arrange
            creditsManager.enableCreditsLogs = false;
            creditsManager.SetCredits(50);

            // Act & Assert
            LogAssert.NoUnexpectedReceived();
            creditsManager.TrySpendCredits(100);
        }

        #endregion

        #region Can Afford Tests

        [Test]
        public void CanAfford_SufficientFunds_ReturnsTrue()
        {
            // Arrange
            creditsManager.SetCredits(1000);

            // Act
            bool canAfford = creditsManager.CanAfford(500);

            // Assert
            Assert.IsTrue(canAfford, "Should return true when credits are sufficient");
        }

        [Test]
        public void CanAfford_InsufficientFunds_ReturnsFalse()
        {
            // Arrange
            creditsManager.SetCredits(300);

            // Act
            bool canAfford = creditsManager.CanAfford(500);

            // Assert
            Assert.IsFalse(canAfford, "Should return false when credits are insufficient");
        }

        [Test]
        public void CanAfford_ExactAmount_ReturnsTrue()
        {
            // Arrange
            creditsManager.SetCredits(750);

            // Act
            bool canAfford = creditsManager.CanAfford(750);

            // Assert
            Assert.IsTrue(canAfford, "Should return true when credits exactly match amount");
        }

        [Test]
        public void CanAfford_ZeroAmount_AlwaysReturnsTrue()
        {
            // Arrange
            creditsManager.SetCredits(0);

            // Act
            bool canAfford = creditsManager.CanAfford(0);

            // Assert
            Assert.IsTrue(canAfford, "Should return true for zero amount even with no credits");
        }

        [Test]
        public void CanAfford_NegativeAmount_AlwaysReturnsTrue()
        {
            // Arrange
            creditsManager.SetCredits(100);

            // Act
            bool canAfford = creditsManager.CanAfford(-50);

            // Assert
            Assert.IsTrue(canAfford, "Should return true for negative amounts");
        }

        [Test]
        public void CanAfford_DoesNotModifyCredits()
        {
            // Arrange
            creditsManager.SetCredits(1000);

            // Act
            creditsManager.CanAfford(500);

            // Assert
            Assert.AreEqual(1000, creditsManager.GetCredits(), "CanAfford should not modify credits");
        }

        #endregion

        #region Update Credits Display Tests

        [Test]
        public void UpdateCreditsDisplay_WithBothUIComponents_UpdatesBoth()
        {
            // Arrange
            creditsManager.SetCredits(1500);
            mockCreditsUI.Reset();
            mockMachineBarManager.Reset();

            // Act
            creditsManager.UpdateCreditsDisplay();

            // Assert
            Assert.IsTrue(mockCreditsUI.UpdateCreditsCalled, "Should update credits UI");
            Assert.AreEqual(1500, mockCreditsUI.LastCreditsValue, "Should pass correct credits value to UI");
            Assert.IsTrue(mockMachineBarManager.UpdateAffordabilityCalled, "Should update machine bar affordability");
        }

        [Test]
        public void UpdateCreditsDisplay_WithNullCreditsUI_DoesNotThrow()
        {
            // Arrange
            creditsManager.Initialize(null, mockMachineBarManager);
            mockMachineBarManager.Reset();

            // Act & Assert
            Assert.DoesNotThrow(() => creditsManager.UpdateCreditsDisplay(), "Should handle null credits UI gracefully");
            Assert.IsTrue(mockMachineBarManager.UpdateAffordabilityCalled, "Should still update machine bar");
        }

        [Test]
        public void UpdateCreditsDisplay_WithNullMachineBarManager_DoesNotThrow()
        {
            // Arrange
            creditsManager.Initialize(mockCreditsUI, null);
            mockCreditsUI.Reset();

            // Act & Assert
            Assert.DoesNotThrow(() => creditsManager.UpdateCreditsDisplay(), "Should handle null machine bar manager gracefully");
            Assert.IsTrue(mockCreditsUI.UpdateCreditsCalled, "Should still update credits UI");
        }

        [Test]
        public void UpdateCreditsDisplay_WithBothUIComponentsNull_DoesNotThrow()
        {
            // Arrange
            creditsManager.Initialize(null, null);

            // Act & Assert
            Assert.DoesNotThrow(() => creditsManager.UpdateCreditsDisplay(), "Should handle both null UI components gracefully");
        }

        #endregion

        #region Integration Tests

        [Test]
        public void CompleteWorkflow_InitializeSpendAndEarn_WorksCorrectly()
        {
            // Arrange
            creditsManager.startingCredits = 1000;

            // Act & Assert - Initialize new game
            creditsManager.InitializeNewGame();
            Assert.AreEqual(1000, creditsManager.GetCredits(), "Should start with correct credits");

            // Spend some credits
            bool spendResult = creditsManager.TrySpendCredits(300);
            Assert.IsTrue(spendResult, "Should successfully spend credits");
            Assert.AreEqual(700, creditsManager.GetCredits(), "Should have correct amount after spending");

            // Earn some credits
            creditsManager.AddCredits(150);
            Assert.AreEqual(850, creditsManager.GetCredits(), "Should have correct amount after earning");

            // Try to overspend
            bool overspendResult = creditsManager.TrySpendCredits(1000);
            Assert.IsFalse(overspendResult, "Should fail to overspend");
            Assert.AreEqual(850, creditsManager.GetCredits(), "Credits should be unchanged after failed spend");
        }

        #endregion

        #region Mock Classes

        /// <summary>
        /// Mock implementation of CreditsUI for testing
        /// </summary>
        private class MockCreditsUI : CreditsUI
        {
            public bool UpdateCreditsCalled { get; private set; }
            public int LastCreditsValue { get; private set; }

            public void UpdateCredits(int credits)
            {
                UpdateCreditsCalled = true;
                LastCreditsValue = credits;
            }

            public void Reset()
            {
                UpdateCreditsCalled = false;
                LastCreditsValue = 0;
            }
        }

        /// <summary>
        /// Mock implementation of MachineBarUIManager for testing
        /// </summary>
        private class MockMachineBarUIManager : MachineBarUIManager
        {
            public bool UpdateAffordabilityCalled { get; private set; }

            public void UpdateAffordability()
            {
                UpdateAffordabilityCalled = true;
            }

            public void Reset()
            {
                UpdateAffordabilityCalled = false;
            }
        }

        #endregion
    }
}