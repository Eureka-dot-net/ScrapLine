using System;
using UnityEngine;

/// <summary>
/// Manages the credits and economy system for the game.
/// Handles credits tracking, spending, earning, and UI updates.
/// </summary>
public class CreditsManager : MonoBehaviour
{
    public event Action CreditsChanged;

    [Header("Credits Configuration")]
    [Tooltip("Starting credits amount for new games")]
    public int startingCredits = 280;
    
    [Tooltip("Percentage of machine cost refunded when dropped outside grid (0.0 to 1.0)")]
    [UnityEngine.Range(0f, 1f)]
    public float machineRefundPercentage = 0.8f;
    
    [Header("Debug")]
    [Tooltip("Enable debug logs for credits operations")]
    public bool enableCreditsLogs = true;

    /// <summary>
    /// Get the component ID for logging purposes
    /// </summary>
    private string ComponentId => $"CreditsManager_{GetEntityId()}";

    private int currentCredits = 0;
    private CreditsUI creditsUI;
    private MachineBarUIManager machineBarManager;

    /// <summary>
    /// Initialize the credits system
    /// </summary>
    /// <param name="creditsUI">Reference to the credits UI</param>
    /// <param name="machineBarManager">Reference to the machine bar manager</param>
    public void Initialize(CreditsUI creditsUI, MachineBarUIManager machineBarManager)
    {
        this.creditsUI = creditsUI;
        this.machineBarManager = machineBarManager;
    }

    /// <summary>
    /// Initialize with starting credits for a new game
    /// </summary>
    public void InitializeNewGame()
    {
        currentCredits = startingCredits;
        UpdateCreditsDisplay();
        CreditsChanged?.Invoke();
    }

    /// <summary>
    /// Set the current credits amount (used when loading saves)
    /// </summary>
    /// <param name="credits">The credits amount to set</param>
    public void SetCredits(int credits)
    {
        SetCredits(credits, true);
    }

    public void SetCredits(int credits, bool notifyChange)
    {
        currentCredits = credits;
        UpdateCreditsDisplay();
        if (notifyChange)
            CreditsChanged?.Invoke();
    }

    /// <summary>
    /// Get the current credits amount
    /// </summary>
    /// <returns>Current credits</returns>
    public int GetCredits()
    {
        return currentCredits;
    }

    /// <summary>
    /// Add credits to the current amount
    /// </summary>
    /// <param name="amount">Amount to add</param>
    public void AddCredits(int amount)
    {
        currentCredits += amount;
        UpdateCreditsDisplay();
        CreditsChanged?.Invoke();
    }

    /// <summary>
    /// Try to spend credits if sufficient funds are available
    /// </summary>
    /// <param name="amount">Amount to spend</param>
    /// <returns>True if credits were spent, false if insufficient funds</returns>
    public bool TrySpendCredits(int amount)
    {
        if (amount < 0)
            return false;
        if (currentCredits >= amount)
        {
            currentCredits -= amount;
            UpdateCreditsDisplay();
            if (amount > 0)
                CreditsChanged?.Invoke();
            return true;
        }
        else
        {
            if (enableCreditsLogs)
                GameLogger.LogWarning(LoggingManager.LogCategory.Economy, $"Insufficient credits! Need {amount}, have {currentCredits}", ComponentId);
            return false;
        }
    }

    /// <summary>
    /// Check if the player can afford a specific amount
    /// </summary>
    /// <param name="amount">Amount to check</param>
    /// <returns>True if affordable, false otherwise</returns>
    public bool CanAfford(int amount)
    {
        return currentCredits >= amount;
    }

    /// <summary>
    /// Refund credits for a machine that was dropped outside the grid
    /// </summary>
    /// <param name="machineCost">Original cost of the machine</param>
    /// <returns>Amount refunded</returns>
    public int RefundMachine(int machineCost)
    {
        int refundAmount = Mathf.RoundToInt(machineCost * machineRefundPercentage);
        currentCredits += refundAmount;
        UpdateCreditsDisplay();
        CreditsChanged?.Invoke();
        return refundAmount;
    }

    /// <summary>
    /// Update the credits display in the UI
    /// </summary>
    public void UpdateCreditsDisplay()
    {
        if (creditsUI != null)
        {
            creditsUI.UpdateCredits(currentCredits);
        }
        
        if (machineBarManager != null)
        {
            machineBarManager.UpdateAffordability();
        }
    }
}
