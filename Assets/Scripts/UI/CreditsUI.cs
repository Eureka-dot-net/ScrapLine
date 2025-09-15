using TMPro;
using UnityEngine;

public class CreditsUI : MonoBehaviour
{
    [SerializeField] private string displayFormat = "Credits: {0}";
    private TMP_Text creditsText;

    private void Awake()
    {
        // Automatically grabs the TMP_Text on the same GameObject
        creditsText = GetComponent<TMP_Text>();
        if (creditsText == null)
            Debug.LogError("[CreditsUI] No TMP_Text found on this GameObject!");
    }

    public void UpdateCredits(int amount)
    {
        Debug.Log($"[CreditsUI] Updating credits display to: {amount}");
        creditsText.text = string.Format(displayFormat, amount);
    }

    private void Start()
    {
        // Initialize display with current credits
        if (GameManager.Instance != null)
            UpdateCredits(GameManager.Instance.GetCredits());
    }
}
