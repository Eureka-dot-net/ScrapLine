using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Presents either a licensed build action or a locked machine-license preview.
/// </summary>
public class MachineButton : MonoBehaviour
{
    public event Action<MachineDef, GameObject> OnButtonClicked;
    public event Action<MachineDef, GameObject> OnUnlockRequested;

    private MachineDef machineDef;
    private Button button;
    private Image statusBackdrop;
    private TextMeshProUGUI statusText;
    private bool isLicensed;
    private bool canAffordLicense;
    private bool awaitingConfirmation;
    private int availableCredits;

    public void Init(MachineDef definition, bool licensed, int credits)
    {
        machineDef = definition;
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
            // Locked, unaffordable previews remain actionable so they can explain the shortfall.
            button.interactable = true;
        }

        EnsureStatusOverlay();
        RefreshLicenseState(licensed, credits);
    }

    public void RefreshLicenseState(bool licensed, int credits)
    {
        isLicensed = licensed;
        availableCredits = credits;
        canAffordLicense = machineDef != null && credits >= machineDef.unlockCost;
        awaitingConfirmation = false;
        if (button != null && machineDef != null)
            button.interactable = !licensed || credits >= machineDef.cost;
        RenderStatus();
    }

    public void SetUnlockConfirmation(bool confirming)
    {
        if (isLicensed)
            return;
        awaitingConfirmation = confirming;
        RenderStatus();
    }

    public void ShowLicenseError(string message)
    {
        if (statusText == null)
            return;
        awaitingConfirmation = false;
        SetOverlayCoverage(true);
        statusBackdrop.color = new Color(0.38f, 0.12f, 0.12f, 0.72f);
        statusText.color = Color.white;
        int shortfall = Math.Max(0, machineDef.unlockCost - availableCredits);
        statusText.text = shortfall > 0
            ? $"NEED {shortfall}"
            : "UNAVAILABLE";
    }

    public MachineDef GetMachineDef() => machineDef;
    public bool IsLicensed() => isLicensed;
    public bool CanAffordLicense() => canAffordLicense;
    public string GetStatusText() => statusText != null ? statusText.text : string.Empty;

    private void HandleClick()
    {
        if (isLicensed)
            OnButtonClicked?.Invoke(machineDef, gameObject);
        else
            OnUnlockRequested?.Invoke(machineDef, gameObject);
    }

    private void RenderStatus()
    {
        if (machineDef == null || statusText == null)
            return;

        if (isLicensed)
        {
            SetOverlayCoverage(false);
            statusBackdrop.color = new Color(0.03f, 0.05f, 0.05f, 0.78f);
            statusText.color = Color.white;
            statusText.text = machineDef.cost.ToString();
            return;
        }

        SetOverlayCoverage(true);
        statusBackdrop.color = new Color(0.32f, 0.32f, 0.32f, 0.46f);
        statusText.color = Color.white;
        if (awaitingConfirmation)
        {
            statusText.text = $"BUY {machineDef.unlockCost}?";
        }
        else
        {
            statusText.text = machineDef.unlockCost.ToString();
        }
    }

    private void SetOverlayCoverage(bool fullCard)
    {
        if (statusBackdrop == null)
            return;
        RectTransform overlayRect = statusBackdrop.rectTransform;
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = fullCard ? Vector2.one : new Vector2(1f, 0.34f);
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
    }

    private void EnsureStatusOverlay()
    {
        Transform existing = transform.Find("LicenseStatus");
        GameObject overlay;
        if (existing != null)
        {
            overlay = existing.gameObject;
        }
        else
        {
            overlay = new GameObject("LicenseStatus", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlay.transform.SetParent(transform, false);
        }

        RectTransform overlayRect = (RectTransform)overlay.transform;
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = new Vector2(1f, 0.34f);
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        statusBackdrop = overlay.GetComponent<Image>();
        statusBackdrop.raycastTarget = false;

        Transform existingText = overlay.transform.Find("StatusText");
        GameObject textObject;
        if (existingText != null)
        {
            textObject = existingText.gameObject;
        }
        else
        {
            textObject = new GameObject("StatusText", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(overlay.transform, false);
        }

        RectTransform textRect = (RectTransform)textObject.transform;
        textRect.anchorMin = new Vector2(0.03f, 0.05f);
        textRect.anchorMax = new Vector2(0.97f, 0.95f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        statusText = textObject.GetComponent<TextMeshProUGUI>();
        statusText.raycastTarget = false;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.fontStyle = FontStyles.Bold;
        statusText.outlineColor = Color.black;
        statusText.outlineWidth = 0.15f;
        statusText.enableAutoSizing = true;
        statusText.fontSizeMin = 12f;
        statusText.fontSizeMax = 24f;
        statusText.textWrappingMode = TextWrappingModes.NoWrap;
    }
}
