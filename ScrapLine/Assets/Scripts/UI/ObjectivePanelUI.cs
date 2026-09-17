using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Compact, advisory objective display. It reports progress and offers reward claims,
/// but never controls whether gameplay actions are available.
///
/// Visually this follows the same "card" language as the machine configuration panels
/// (BaseConfigPanel / SpawnerConfigPanel etc.): a rounded, dark navy panel with an
/// orange title and a thin divider, built from the shared RoundedCorners sprite and the
/// same Orbitron font the rest of the UI uses, so it reads as part of the same UI system
/// rather than a debug overlay. Unlike those panels it is not modal — it starts collapsed
/// to a slim header that sits above the grid, and only expands (like a popover, briefly
/// covering the top of the grid the same way a config panel would) when the player taps it.
/// </summary>
public sealed class ObjectivePanelUI : MonoBehaviour
{
    private const string RoundedCornerSpritePath = "Sprites/UI/RoundedCorners/Rounded40px";
    private const string OrbitronSemiBoldFontPath = "Fonts/Orbitron-SemiBold SDF";
    private const string CollapsedPrefsKey = "ScrapLine.ObjectivePanel.Collapsed";

    // Orbitron is wide and geometric — great for the big header title, but it eats a lot of
    // horizontal space per character, which was making the body copy wrap early and clip.
    // Body text intentionally stays on TMP's default font (denser, more legible at small sizes);
    // only the header/labels use Orbitron, matching how the rest of the UI mostly uses it for
    // headings rather than paragraphs.

    private static readonly Color32 FrameColor = new Color32(58, 74, 94, 255);
    private static readonly Color32 FillColor = new Color32(23, 34, 46, 240);
    private static readonly Color32 TitleColor = new Color32(255, 165, 0, 255);
    private static readonly Color32 EyebrowColor = new Color32(255, 190, 110, 210);
    private static readonly Color32 DividerColor = new Color32(255, 165, 0, 120);
    private static readonly Color32 BodyTextColor = new Color32(238, 243, 247, 255);
    private static readonly Color32 RecommendationColor = new Color32(220, 230, 238, 255);
    private static readonly Color32 FeedbackColor = new Color32(255, 190, 80, 255);
    private static readonly Color32 ButtonColor = new Color32(64, 75, 90, 255);
    private static readonly Color32 ProgressTrackColor = new Color32(15, 22, 30, 255);
    private static readonly Color32 ProgressFillColor = new Color32(255, 165, 0, 255);
    private static readonly Color32 BadgeColor = new Color32(255, 165, 0, 255);
    private static readonly Color32 BadgeTextColor = new Color32(35, 25, 10, 255);

    [Header("Layout anchors (tune in Inspector if the HUD layout changes)")]
    [Tooltip("Distance from the top-right corner of the canvas to the panel's top-right corner. " +
             "Reference resolution is 1080x1920. Nudge this up (less negative) if the header still " +
             "touches the top button row, or down if it clips the grid.")]
    public Vector2 anchoredOffset = new Vector2(-24f, -136f);
    [Tooltip("Panel width in canvas units (reference resolution 1080x1920).")]
    public float panelWidth = 720f;
    [Tooltip("Header height (always visible: an 'OPTIONAL' eyebrow tag, the objective title, and the collapse toggle).")]
    public float headerHeight = 132f;
    [Tooltip("Body height when expanded.")]
    public float bodyHeight = 460f;

    /// <summary>Small, muted "OPTIONAL" tag above the objective title. Always static text.</summary>
    public TMP_Text titleText;
    /// <summary>The large, readable objective name (e.g. "Start a scrap line").</summary>
    public TMP_Text objectiveTitleText;
    public TMP_Text detailsText;
    public TMP_Text recommendationText;
    public TMP_Text feedbackText;
    public Button claimButton;
    public Button collapseToggleButton;
    public TMP_Text collapseToggleLabel;

    private RectTransform panelRect;
    private RectTransform bodyRect;
    private GameObject rewardBadge;
    private Image progressFillImage;
    private ProgressionManager progressionManager;
    private CreditsManager creditsManager;
    private string displayedObjectiveId;
    private bool isCollapsed;
    private bool rewardReady;

    public void Bind(ProgressionManager manager)
    {
        Bind(manager, GameManager.Instance?.creditsManager);
    }

    public void Bind(ProgressionManager manager, CreditsManager credits)
    {
        Unsubscribe();
        progressionManager = manager;
        creditsManager = credits;
        if (progressionManager != null)
        {
            progressionManager.ObjectiveProgressed += HandleStateChanged;
            progressionManager.ObjectiveCompleted += HandleCompleted;
            progressionManager.ObjectiveRewardClaimed += HandleClaimed;
            progressionManager.ObjectiveStateReloaded += Refresh;
        }
        if (creditsManager != null)
            creditsManager.CreditsChanged += HandleRelatedStateChanged;
        if (FactoryRegistry.Instance != null)
            FactoryRegistry.Instance.MachineUnlocked += HandleMachineUnlocked;

        if (claimButton != null)
        {
            claimButton.onClick.RemoveListener(ClaimDisplayedReward);
            claimButton.onClick.AddListener(ClaimDisplayedReward);
        }
        if (collapseToggleButton != null)
        {
            collapseToggleButton.onClick.RemoveListener(ToggleCollapsed);
            collapseToggleButton.onClick.AddListener(ToggleCollapsed);
        }
        SetText(titleText, "OPTIONAL");
        Refresh();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Unsubscribe()
    {
        if (progressionManager != null)
        {
            progressionManager.ObjectiveProgressed -= HandleStateChanged;
            progressionManager.ObjectiveCompleted -= HandleCompleted;
            progressionManager.ObjectiveRewardClaimed -= HandleClaimed;
            progressionManager.ObjectiveStateReloaded -= Refresh;
        }
        if (creditsManager != null)
            creditsManager.CreditsChanged -= HandleRelatedStateChanged;
        if (FactoryRegistry.Instance != null)
            FactoryRegistry.Instance.MachineUnlocked -= HandleMachineUnlocked;
        creditsManager = null;
    }

    private void HandleRelatedStateChanged()
    {
        Refresh(false);
    }

    private void HandleMachineUnlocked(string machineId, string source)
    {
        Refresh(false);
    }

    private void HandleStateChanged(ObjectiveStateSnapshot state)
    {
        Refresh();
    }

    private void HandleCompleted(ObjectiveStateSnapshot state)
    {
        if (feedbackText != null)
            feedbackText.text = "Objective complete — reward ready";
        Refresh(false);
    }

    private void HandleClaimed(ObjectiveStateSnapshot state)
    {
        if (feedbackText != null)
            feedbackText.text = "Reward claimed";
        Refresh(false);
    }

    public void Refresh()
    {
        Refresh(true);
    }

    private void Refresh(bool clearFeedback)
    {
        if (clearFeedback && feedbackText != null)
            feedbackText.text = string.Empty;

        // The "OPTIONAL" eyebrow tag never changes — it is a fixed label for the whole panel,
        // not a per-objective flag, so it stays legible instead of wrapping into the title.
        SetText(titleText, "OPTIONAL");

        ObjectiveDefinition definition = progressionManager?.GetCurrentRecommendation();
        if (definition == null)
        {
            displayedObjectiveId = null;
            SetText(objectiveTitleText, "All objectives complete");
            SetText(detailsText, "Keep building the factory your way — nothing here was ever required.");
            SetText(recommendationText, string.Empty);
            if (progressFillImage != null)
                progressFillImage.fillAmount = 1f;
            SetRewardReady(false);
            return;
        }

        displayedObjectiveId = definition.id;
        ObjectiveStateSnapshot state = progressionManager.GetObjectiveState(definition.id);
        int progress = state?.Progress ?? 0;
        int target = Mathf.Max(1, definition.targetValue);
        bool isCompleted = state?.IsCompleted == true;

        // Recommendation and feedback share one slot on screen (see Create()), so only one of
        // them may have text at a time — otherwise a lingering "Reward claimed" from the
        // objective we just finished renders on top of the next objective's recommendation.
        bool hasFeedback = feedbackText != null && !string.IsNullOrEmpty(feedbackText.text);

        SetText(objectiveTitleText, definition.title);
        SetText(detailsText,
            $"{definition.description}\n{progress}/{definition.targetValue}  •  {RewardLabel(definition.reward)}");
        // Once complete, the "suggested next" line is no longer useful and just competes for
        // space with the completion message — drop it so the claim can't get crowded out.
        SetText(recommendationText,
            isCompleted || hasFeedback || string.IsNullOrEmpty(definition.recommendedNextCapability)
                ? string.Empty
                : $"Suggested next: {definition.recommendedNextCapability}");
        if (progressFillImage != null)
            progressFillImage.fillAmount = Mathf.Clamp01((float)progress / target);
        SetRewardReady(isCompleted && state.IsRewardClaimed == false);
    }

    private void SetRewardReady(bool ready)
    {
        rewardReady = ready;
        if (claimButton != null)
            claimButton.gameObject.SetActive(ready);
        if (rewardBadge != null)
            rewardBadge.SetActive(ready && isCollapsed);
    }

    private void ClaimDisplayedReward()
    {
        if (progressionManager == null || string.IsNullOrWhiteSpace(displayedObjectiveId))
            return;
        if (!progressionManager.TryClaimReward(displayedObjectiveId, out string error) && feedbackText != null)
            feedbackText.text = error;
    }

    private void ToggleCollapsed()
    {
        SetCollapsed(!isCollapsed);
        PlayerPrefs.SetInt(CollapsedPrefsKey, isCollapsed ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void SetCollapsed(bool collapsed)
    {
        isCollapsed = collapsed;
        if (bodyRect != null)
            bodyRect.gameObject.SetActive(!collapsed);
        if (panelRect != null)
            panelRect.sizeDelta = new Vector2(panelWidth, headerHeight + (collapsed ? 0f : bodyHeight));
        if (collapseToggleLabel != null)
            collapseToggleLabel.text = collapsed ? "▸" : "▾";
        if (rewardBadge != null)
            rewardBadge.SetActive(collapsed && rewardReady);
        // Re-pull a clean snapshot whenever the player opens the panel, rather than trusting
        // whatever was last drawn onto it — this also clears out any stale feedback message
        // ("Reward claimed" etc.) left over from before it was collapsed.
        if (!collapsed)
            Refresh(true);
    }

    public static string RewardLabel(ObjectiveRewardDefinition reward)
    {
        if (reward == null)
            return "No reward";
        return reward.type == ObjectiveRewardTypes.Credits
            ? $"Reward: {reward.amount} credits"
            : $"Reward: {reward.targetId} license";
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }

    public static ObjectivePanelUI Create(ProgressionManager manager)
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return null;

        Sprite roundedSprite = Resources.Load<Sprite>(RoundedCornerSpritePath);
        TMP_FontAsset headingFont = Resources.Load<TMP_FontAsset>(OrbitronSemiBoldFontPath);

        // --- Root panel: anchored to the top-right of the HUD, rounded card frame ---
        GameObject panelObject = new GameObject("ObjectivePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);

        Image frame = panelObject.GetComponent<Image>();
        frame.sprite = roundedSprite;
        frame.type = roundedSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        frame.color = FrameColor;

        ObjectivePanelUI panel = panelObject.AddComponent<ObjectivePanelUI>();
        panel.panelRect = panelRect;
        panelRect.anchoredPosition = panel.anchoredOffset;
        panelRect.sizeDelta = new Vector2(panel.panelWidth, panel.headerHeight + panel.bodyHeight);

        // Inner fill, inset from the frame so the rounded border reads clearly (same trick
        // the config panels use for their two-tone frame/fill look).
        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillObject.transform.SetParent(panelRect, false);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(6f, 6f);
        fillRect.offsetMax = new Vector2(-6f, -6f);
        Image fill = fillObject.GetComponent<Image>();
        fill.sprite = roundedSprite;
        fill.type = roundedSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        fill.color = FillColor;

        // --- Header: small "OPTIONAL" eyebrow tag, the objective title, and the collapse toggle ---
        panel.titleText = CreateText(fillRect, "Eyebrow", 20f, new Vector2(20f, -8f), new Vector2(300f, 24f),
            EyebrowColor, FontStyles.Bold, headingFont);
        panel.titleText.characterSpacing = 4f;

        panel.objectiveTitleText = CreateText(fillRect, "Title", 34f, new Vector2(20f, -32f), new Vector2(panel.panelWidth - 120f, 84f),
            TitleColor, FontStyles.Bold, headingFont);

        CreateBar(fillRect, "Divider", new Vector2(20f, -panel.headerHeight + 10f), new Vector2(panel.panelWidth - 40f, 3f), DividerColor);

        panel.collapseToggleButton = CreateIconButton(
            fillRect, "CollapseToggle", new Vector2(panel.panelWidth - 80f, -8f), headingFont, out TMP_Text toggleLabel);
        panel.collapseToggleLabel = toggleLabel;

        // Small "reward ready" badge that stays visible on the collapsed header so a claimable
        // reward is never hidden away.
        GameObject badgeObject = new GameObject("RewardBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        badgeObject.transform.SetParent(panelRect, false);
        RectTransform badgeRect = badgeObject.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0f, 1f);
        badgeRect.anchorMax = new Vector2(0f, 1f);
        badgeRect.pivot = new Vector2(0.5f, 0.5f);
        badgeRect.anchoredPosition = new Vector2(-6f, 6f);
        badgeRect.sizeDelta = new Vector2(36f, 36f);
        badgeObject.GetComponent<Image>().color = BadgeColor;
        TMP_Text badgeLabel = CreateText(badgeRect, "Label", 24f, Vector2.zero, badgeRect.sizeDelta, BadgeTextColor, FontStyles.Bold, headingFont);
        badgeLabel.alignment = TextAlignmentOptions.Center;
        badgeLabel.text = "!";
        badgeObject.SetActive(false);
        panel.rewardBadge = badgeObject;

        // --- Body: recommendation, progress, feedback, claim. Collapsible as a whole. ---
        GameObject bodyObject = new GameObject("Body", typeof(RectTransform));
        bodyObject.transform.SetParent(fillRect, false);
        RectTransform bodyRect = bodyObject.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 1f);
        bodyRect.anchorMax = new Vector2(0f, 1f);
        bodyRect.pivot = new Vector2(0f, 1f);
        bodyRect.anchoredPosition = new Vector2(0f, -panel.headerHeight);
        bodyRect.sizeDelta = new Vector2(panel.panelWidth, panel.bodyHeight);
        panel.bodyRect = bodyRect;

        // Description + progress/reward line. Generously tall so it can never clip — this is
        // the text that was getting silently ellipsis-truncated before.
        panel.detailsText = CreateText(bodyRect, "Details", 34f, new Vector2(20f, -16f), new Vector2(panel.panelWidth - 40f, 170f),
            BodyTextColor, FontStyles.Normal, null);
        panel.detailsText.lineSpacing = 10f;

        RectTransform trackRect = CreateBar(bodyRect, "ProgressTrack", new Vector2(20f, -196f), new Vector2(panel.panelWidth - 40f, 22f), ProgressTrackColor);
        GameObject fillBarObject = new GameObject("ProgressFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillBarObject.transform.SetParent(trackRect, false);
        RectTransform fillBarRect = fillBarObject.GetComponent<RectTransform>();
        fillBarRect.anchorMin = Vector2.zero;
        fillBarRect.anchorMax = Vector2.one;
        fillBarRect.offsetMin = Vector2.zero;
        fillBarRect.offsetMax = Vector2.zero;
        Image progressFill = fillBarObject.GetComponent<Image>();
        progressFill.color = ProgressFillColor;
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        progressFill.fillAmount = 0f;
        panel.progressFillImage = progressFill;

        // Recommendation and feedback share the same slot (only one is ever showing text at a
        // time — see Refresh()) so a completed objective's message doesn't have to squeeze in
        // beside anything else.
        Vector2 statusPosition = new Vector2(20f, -234f);
        Vector2 statusSize = new Vector2(panel.panelWidth - 40f, 110f);
        panel.recommendationText = CreateText(bodyRect, "Recommendation", 28f, statusPosition, statusSize,
            RecommendationColor, FontStyles.Normal, null);
        panel.feedbackText = CreateText(bodyRect, "Feedback", 30f, statusPosition, statusSize,
            FeedbackColor, FontStyles.Bold, null);

        Vector2 claimButtonSize = new Vector2(240f, 68f);
        Vector2 claimButtonPosition = new Vector2((panel.panelWidth - claimButtonSize.x) * 0.5f, -358f);
        panel.claimButton = CreateClaimButton(bodyRect, claimButtonPosition, claimButtonSize, headingFont);

        bool startCollapsed = PlayerPrefs.GetInt(CollapsedPrefsKey, 1) == 1;
        panel.SetCollapsed(startCollapsed);

        panel.Bind(manager, GameManager.Instance?.creditsManager);
        return panel;
    }

    /// <summary>
    /// Creates a plain-color rectangle anchored to the parent's top-left corner
    /// (used for the header divider and the progress-bar track).
    /// </summary>
    private static RectTransform CreateBar(RectTransform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        go.GetComponent<Image>().color = color;
        return rect;
    }

    private static TMP_Text CreateText(
        RectTransform parent,
        string name,
        float fontSize,
        Vector2 position,
        Vector2 size,
        Color color,
        FontStyles style,
        TMP_FontAsset font)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        if (font != null)
            text.font = font;
        text.fontSize = fontSize;
        text.color = color;
        text.fontStyle = style;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static Button CreateIconButton(RectTransform parent, string name, Vector2 position, TMP_FontAsset font, out TMP_Text label)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(60f, 60f);
        buttonObject.GetComponent<Image>().color = ButtonColor;
        label = CreateText(rect, "Label", 28f, Vector2.zero, rect.sizeDelta, TitleColor, FontStyles.Bold, font);
        label.alignment = TextAlignmentOptions.Center;
        label.text = "▾";
        return buttonObject.GetComponent<Button>();
    }

    /// <summary>
    /// The reward-claim button, back to the same neutral panel-button styling as the rest of
    /// the UI (config panel confirm/order buttons etc.) rather than a standalone accent color.
    /// </summary>
    private static Button CreateClaimButton(RectTransform parent, Vector2 position, Vector2 size, TMP_FontAsset font)
    {
        GameObject buttonObject = new GameObject(
            "ClaimButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        buttonObject.GetComponent<Image>().color = ButtonColor;
        TMP_Text label = CreateText(rect, "Label", 32f, Vector2.zero, rect.sizeDelta, TitleColor, FontStyles.Bold, font);
        label.alignment = TextAlignmentOptions.Center;
        label.text = "CLAIM";
        return buttonObject.GetComponent<Button>();
    }
}
