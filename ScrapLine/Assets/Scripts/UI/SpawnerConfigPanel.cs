using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Operational panel for a spawner: current scrap, upcoming deliveries, and ordering.
/// </summary>
public class SpawnerConfigPanel : BaseConfigPanel<CellData, string>
{
    [Header("Spawner Delivery Panel")]
    public Button currentCrateButton;
    public Sprite emptySelectionSprite;
    public Color emptySelectionColor = Color.gray;
    public Slider currentCrateProgressBar;
    public TextMeshProUGUI currentCrateLabel;
    public WasteCrateQueuePanel queuePanel;
    public WasteCrateConfigPanel wasteCrateConfigPanel;

    private SpawnerMachine currentSpawnerMachine;
    private Image currentCrateIconImage;
    private TextMeshProUGUI confirmText;
    private TextMeshProUGUI cancelText;
    private string originalConfirmText;
    private string originalCancelText;
    private TextMeshProUGUI currentCrateStatusText;
    private string displayedCrateId;
    private bool currentCrateIconInitialized;
    private float nextStatusRefreshTime;

    protected override void SetupCustomButtonListeners()
    {
        if (currentCrateButton != null)
        {
            currentCrateButton.enabled = false;
            currentCrateButton.navigation = new Navigation { mode = Navigation.Mode.None };
            currentCrateButton.transition = Selectable.Transition.None;
            if (currentCrateButton.targetGraphic != null)
                currentCrateButton.targetGraphic.raycastTarget = false;

            LayoutElement layout = currentCrateButton.GetComponent<LayoutElement>() ??
                                   currentCrateButton.gameObject.AddComponent<LayoutElement>();
            // Sprite images advertise their native pixel width to layout groups. Override it so the
            // high-resolution bale art cannot consume the entire Queue/Current row.
            layout.minWidth = 0f;
            layout.preferredWidth = 0f;
            layout.flexibleWidth = 3f;
            layout.minHeight = 0f;
            layout.preferredHeight = 0f;

            RectTransform row = currentCrateButton.transform.parent as RectTransform;
            if (row != null)
                LayoutRebuilder.MarkLayoutForRebuild(row);
        }
        if (currentCrateProgressBar != null)
        {
            currentCrateProgressBar.interactable = false;
            currentCrateProgressBar.navigation = new Navigation { mode = Navigation.Mode.None };
            currentCrateProgressBar.transition = Selectable.Transition.None;
            if (currentCrateProgressBar.handleRect != null)
                currentCrateProgressBar.handleRect.gameObject.SetActive(false);
            StyleProgressBar();
        }
        CreateCurrentCrateStatus();
        if (queuePanel != null)
            queuePanel.OnQueueClicked += OnQueuePanelClicked;
    }

    private void Update()
    {
        if (currentSpawnerMachine == null || configPanel == null || !configPanel.activeInHierarchy ||
            Time.unscaledTime < nextStatusRefreshTime)
            return;

        nextStatusRefreshTime = Time.unscaledTime + 0.25f;
        UpdateCurrentCrateIcon();
        UpdateCrateProgressBar();
    }

    protected override void LoadCurrentConfiguration()
    {
        currentSpawnerMachine = currentData?.machine as SpawnerMachine;
        if (currentSpawnerMachine == null)
            GameLogger.LogWarning(LoggingManager.LogCategory.UI,
                "No SpawnerMachine found in cell data.", ComponentId);
    }

    protected override void UpdateUIFromCurrentState()
    {
        UpdateCurrentCrateIcon();
        UpdateCrateProgressBar();
        UpdateQueuePanelDisplay();
    }

    protected override string GetCurrentSelection()
    {
        return null;
    }

    protected override void UpdateDataWithSelection(string selection)
    {
        // Spawners no longer have a configurable scrap filter.
    }

    protected override void HideSelectionPanels()
    {
    }

    protected override void OnConfigurationShown()
    {
        ConfigureActionButtons();
        displayedCrateId = null;
        currentCrateIconInitialized = false;
        nextStatusRefreshTime = 0f;
        if (currentCrateLabel != null)
            currentCrateLabel.text = "Current";
    }

    protected override void OnConfigurationHidden()
    {
        RestoreActionButtons();
        currentSpawnerMachine = null;
        displayedCrateId = null;
        currentCrateIconInitialized = false;
    }

    protected override void OnConfigurationConfirmed(string selection)
    {
        OpenOrderShop(false);
    }

    private void UpdateCurrentCrateIcon()
    {
        if (currentCrateButton == null)
            return;

        currentCrateIconImage ??= currentCrateButton.GetComponent<Image>() ??
                                  currentCrateButton.GetComponentInChildren<Image>(true);
        if (currentCrateIconImage == null)
            return;

        string crateId = currentSpawnerMachine != null && currentSpawnerMachine.HasItemsInWasteCrate()
            ? currentData?.wasteCrate?.wasteCrateDefId
            : null;
        WasteCrateDef crateDef = string.IsNullOrEmpty(crateId)
            ? null
            : FactoryRegistry.Instance?.GetWasteCrate(crateId);
        if (currentCrateIconInitialized && crateId == displayedCrateId)
            return;

        currentCrateIconInitialized = true;
        displayedCrateId = crateId;
        Sprite sprite = crateDef == null || string.IsNullOrEmpty(crateDef.sprite)
            ? null
            : Resources.Load<Sprite>($"Sprites/Waste/{crateDef.sprite}");

        if (sprite != null)
        {
            currentCrateIconImage.sprite = sprite;
            currentCrateIconImage.color = Color.white;
            currentCrateIconImage.preserveAspect = true;
            currentCrateIconImage.enabled = true;
        }
        else
        {
            currentCrateIconImage.sprite = null;
            currentCrateIconImage.enabled = false;
        }
    }

    private void UpdateCrateProgressBar()
    {
        if (currentCrateProgressBar == null)
            return;

        int currentItems = currentSpawnerMachine?.GetTotalItemsInWasteCrate() ?? 0;
        int initialItems = currentSpawnerMachine?.GetInitialWasteCrateTotal() ?? 0;
        float fill = initialItems > 0 ? (float)currentItems / initialItems : 0f;
        currentCrateProgressBar.value = fill;
        currentCrateProgressBar.gameObject.SetActive(true);

        if (currentCrateStatusText == null)
            return;

        string crateId = currentData?.wasteCrate?.wasteCrateDefId;
        WasteCrateDef crateDef = currentItems > 0 && !string.IsNullOrEmpty(crateId)
            ? FactoryRegistry.Instance?.GetWasteCrate(crateId)
            : null;
        currentCrateStatusText.text = crateDef == null
            ? "Empty"
            : $"{GetCompactCrateName(crateDef.displayName)}\n<size=80%>{currentItems} / {initialItems}</size>";
    }

    private void UpdateQueuePanelDisplay()
    {
        if (queuePanel == null)
            return;

        queuePanel.ShowPanel();
        List<string> deliveries = currentSpawnerMachine?.GetDeliveryQueueStatus().queuedCrateIds ??
                                  new List<string>();
        queuePanel.UpdateQueueDisplay(deliveries);
    }

    private void OnQueuePanelClicked()
    {
        OpenOrderShop(true);
    }

    private void OpenOrderShop(bool hideSpawnerPanel)
    {
        SpawnerMachine selectedSpawner = currentSpawnerMachine;
        if (selectedSpawner == null)
            return;

        if (hideSpawnerPanel)
            HideConfiguration();

        WasteCrateConfigPanel shop = wasteCrateConfigPanel ??
            FindAnyObjectByType<WasteCrateConfigPanel>(FindObjectsInactive.Include);
        if (shop == null)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI,
                "Scrap delivery shop not found.", ComponentId);
            return;
        }

        shop.ShowPanelForSpawner(selectedSpawner, selectedSpawner.OnConfigured);
    }

    private void ConfigureActionButtons()
    {
        confirmText = confirmButton?.GetComponentInChildren<TextMeshProUGUI>(true);
        cancelText = cancelButton?.GetComponentInChildren<TextMeshProUGUI>(true);
        if (confirmText != null)
        {
            originalConfirmText = confirmText.text;
            confirmText.text = "Order";
        }
        if (cancelText != null)
        {
            originalCancelText = cancelText.text;
            cancelText.text = "Close";
        }
    }

    private void RestoreActionButtons()
    {
        if (confirmText != null && originalConfirmText != null)
            confirmText.text = originalConfirmText;
        if (cancelText != null && originalCancelText != null)
            cancelText.text = originalCancelText;
    }

    private void CreateCurrentCrateStatus()
    {
        if (currentCrateButton == null || currentCrateStatusText != null)
            return;

        GameObject status = new GameObject("CurrentScrapStatus", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        status.transform.SetParent(currentCrateButton.transform, false);
        RectTransform statusRect = status.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.03f, 0.03f);
        statusRect.anchorMax = new Vector2(0.97f, 0.28f);
        statusRect.offsetMin = Vector2.zero;
        statusRect.offsetMax = Vector2.zero;

        currentCrateStatusText = status.GetComponent<TextMeshProUGUI>();
        if (currentCrateLabel != null)
            currentCrateStatusText.font = currentCrateLabel.font;
        currentCrateStatusText.alignment = TextAlignmentOptions.Center;
        currentCrateStatusText.enableAutoSizing = true;
        currentCrateStatusText.fontSizeMin = 14f;
        currentCrateStatusText.fontSizeMax = 20f;
        currentCrateStatusText.color = new Color(1f, 0.82f, 0.48f, 1f);
        currentCrateStatusText.raycastTarget = false;
        currentCrateStatusText.text = "Empty";
    }

    private void StyleProgressBar()
    {
        Image background = currentCrateProgressBar.transform.Find("Background")?.GetComponent<Image>();
        if (background != null)
        {
            background.color = new Color(0.137f, 0.157f, 0.196f, 1f);
            background.raycastTarget = false;
        }

        Image fill = currentCrateProgressBar.fillRect?.GetComponent<Image>();
        if (fill != null)
        {
            fill.color = new Color(1f, 0.6f, 0f, 1f);
            fill.raycastTarget = false;
        }
    }

    private static string GetCompactCrateName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return "Scrap";

        const string baleSuffix = " Bale";
        return displayName.EndsWith(baleSuffix, System.StringComparison.OrdinalIgnoreCase)
            ? displayName.Substring(0, displayName.Length - baleSuffix.Length)
            : displayName;
    }

    public new void ShowConfiguration(CellData cellData, System.Action<string> onConfirmed)
    {
        base.ShowConfiguration(cellData, onConfirmed);
        UpdateQueuePanelDisplay();
    }
}
