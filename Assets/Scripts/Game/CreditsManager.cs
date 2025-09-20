using UnityEngine;

/// <summary>
/// Manages the credits and economy system for the game.
/// Handles credits tracking, spending, earning, and UI updates.
/// </summary>
public class CreditsManager : MonoBehaviour
{
    [Header("Credits Configuration")]
    [Tooltip("Starting credits amount for new games")]
    public int startingCredits = 2000;
    
    [Header("Debug")]
    [Tooltip("Enable debug logs for credits operations")]
    public bool enableCreditsLogs = true;

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
    }

    /// <summary>
    /// Set the current credits amount (used when loading saves)
    /// </summary>
    /// <param name="credits">The credits amount to set</param>
    public void SetCredits(int credits)
    {
        currentCredits = credits;
        UpdateCreditsDisplay();
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
    }

    /// <summary>
    /// Try to spend credits if sufficient funds are available
    /// </summary>
    /// <param name="amount">Amount to spend</param>
    /// <returns>True if credits were spent, false if insufficient funds</returns>
    public bool TrySpendCredits(int amount)
    {
        if (currentCredits >= amount)
        {
            currentCredits -= amount;
            UpdateCreditsDisplay();
            return true;
        }
        else
        {
            if (enableCreditsLogs)
                Debug.LogWarning($"Insufficient credits! Need {amount}, have {currentCredits}");
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