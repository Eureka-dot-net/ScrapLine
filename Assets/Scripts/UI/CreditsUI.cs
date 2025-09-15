using UnityEngine;
using TMPro;

public class CreditsUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text creditsText;
    
    [Header("Display Settings")]
    [SerializeField] private string displayFormat = "Credits: {0}";
    
    /// <summary>
    /// Updates the credits display with the specified amount
    /// </summary>
    /// <param name="amount">The credits amount to display</param>
    public void UpdateCredits(int amount)
    {
        if (creditsText != null)
        {
            creditsText.text = string.Format(displayFormat, amount);
        }
        else
        {
            Debug.LogWarning("[CreditsUI] No TMP_Text component assigned! Please assign the creditsText field in the inspector.");
        }
    }
    
    private void Start()
    {
        // Initialize display with current credits from GameManager
        if (GameManager.Instance != null)
        {
            UpdateCredits(GameManager.Instance.GetCredits());
        }
        
        // Validate setup
        if (creditsText == null)
        {
            Debug.LogWarning("[CreditsUI] No TMP_Text component assigned! Please assign the creditsText field in the inspector.");
        }
    }
}

/*
UNITY SETUP INSTRUCTIONS:
==========================

1. CREATE UI TEXT ELEMENT:
   - Right-click in your UI Canvas hierarchy
   - Go to UI > Text - TextMeshPro
   - Name it "CreditsDisplay"
   - Position it at the top of your UI where you want credits shown

2. CONFIGURE TEXT APPEARANCE:
   - Set font size (e.g., 24-32 for mobile)
   - Set color (e.g., white or yellow for good visibility)
   - Set alignment (typically center or left-align)
   - Anchor it to the top of the screen for consistent positioning

3. ATTACH CREDITSUI SCRIPT:
   - Select the CreditsDisplay text object you created in step 1
   - Add the CreditsUI component (drag this script or use Add Component)
   - The script will automatically detect the TMP_Text component on the same GameObject
   - OR manually drag the TMP_Text component to the "Credits Text" field in the inspector

4. OPTIONAL CUSTOMIZATION:
   - Modify "Display Format" field to change how credits are shown
   - Examples: "Credits: {0}", "${0}", "Money: {0}", "⭐ {0}"

5. TEST:
   - Enter Play mode
   - Check Console for any setup warnings
   - Verify credits display shows current value
   - Test placing machines and selling items to see credits update

The GameManager will automatically find this component and call UpdateCredits() 
whenever credits change.
*/