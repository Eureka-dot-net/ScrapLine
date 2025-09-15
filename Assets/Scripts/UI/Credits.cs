using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Credits : MonoBehaviour
{
    [Header("Unity Setup Required")]
    [Tooltip("Drag a Text or TMP_Text component here from your UI hierarchy")]
    public Text creditsText; // For legacy UI.Text
    
    [Tooltip("Drag a TMP_Text component here from your UI hierarchy (recommended)")]
    public TMP_Text creditsTMPText; // For TextMeshPro
    
    [Header("Display Settings")]
    [Tooltip("Format string for displaying credits (use {0} for the credits value)")]
    public string displayFormat = "Credits: {0}";
    
    // Singleton pattern for easy access
    public static Credits Instance { get; private set; }
    
    private void Awake()
    {
        // Ensure only one Credits instance exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    private void Start()
    {
        // Update display with current credits when the script starts
        UpdateDisplay();
        
        // Validate Unity setup
        ValidateSetup();
    }
    
    private void ValidateSetup()
    {
        if (creditsText == null && creditsTMPText == null)
        {
            Debug.LogWarning("[Credits] No text component assigned! Please follow setup instructions:\n" +
                           "1. Create a UI Text or TMP_Text element in your scene\n" +
                           "2. Position it at the top of your UI\n" +
                           "3. Drag the Credits script to a GameObject\n" +
                           "4. Drag the Text/TMP_Text component to the appropriate field in the Credits script");
        }
        
        if (creditsText != null && creditsTMPText != null)
        {
            Debug.LogWarning("[Credits] Both Text and TMP_Text components assigned. TMP_Text will be used (recommended).");
        }
    }
    
    /// <summary>
    /// Updates the credits display with the current credits value from GameManager
    /// </summary>
    public void UpdateDisplay()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[Credits] GameManager.Instance is null, cannot update credits display");
            return;
        }
        
        int currentCredits = GameManager.Instance.GetCredits();
        UpdateDisplay(currentCredits);
    }
    
    /// <summary>
    /// Updates the credits display with a specific credits value
    /// </summary>
    /// <param name="credits">The credits amount to display</param>
    public void UpdateDisplay(int credits)
    {
        string displayText = string.Format(displayFormat, credits);
        
        // Use TMP_Text if available (recommended), otherwise fall back to UI.Text
        if (creditsTMPText != null)
        {
            creditsTMPText.text = displayText;
        }
        else if (creditsText != null)
        {
            creditsText.text = displayText;
        }
        else
        {
            Debug.LogWarning("[Credits] No text component available to update!");
        }
    }
    
    /// <summary>
    /// Call this method whenever credits change to automatically update the UI
    /// </summary>
    public static void RefreshDisplay()
    {
        if (Instance != null)
        {
            Instance.UpdateDisplay();
        }
    }
    
    // Optional: Subscribe to GameManager events for automatic updates
    private void OnEnable()
    {
        // If GameManager has events for credits changes, subscribe here
        // Example: GameManager.OnCreditsChanged += UpdateDisplay;
    }
    
    private void OnDisable()
    {
        // Unsubscribe from events to prevent memory leaks
        // Example: GameManager.OnCreditsChanged -= UpdateDisplay;
    }
}

/*
UNITY SETUP INSTRUCTIONS:
==========================

1. CREATE UI TEXT ELEMENT:
   - Right-click in your UI Canvas hierarchy
   - Go to UI > Text - TextMeshPro (recommended) OR UI > Legacy > Text
   - Name it "CreditsDisplay" or similar
   - Position it at the top of your UI where you want credits shown

2. CONFIGURE TEXT APPEARANCE:
   - Set font size (e.g., 24-32 for mobile)
   - Set color (e.g., white or yellow for good visibility)
   - Set alignment (typically center or left-align)
   - Anchor it to the top of the screen for consistent positioning

3. ATTACH CREDITS SCRIPT:
   - Select any GameObject in your scene (or create an empty one named "CreditsManager")
   - Add the Credits component (drag this script or use Add Component)
   - Drag your text element from step 1 into the appropriate field:
     - If using TextMeshPro: drag to "Credits TMP Text" field
     - If using legacy Text: drag to "Credits Text" field

4. OPTIONAL CUSTOMIZATION:
   - Modify "Display Format" field to change how credits are shown
   - Examples: "Credits: {0}", "${0}", "Money: {0}", "⭐ {0}"

5. TEST:
   - Enter Play mode
   - Check Console for any setup warnings
   - Verify credits display shows current value
   - Test placing machines and selling items to see credits update

The script will automatically update the display whenever credits change
through the RefreshDisplay() method called by GameManager.
*/