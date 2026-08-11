using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GridExpansionInstaller
{
    private const string ScenePath = "Assets/Scenes/MobileGridScene.unity";
    private const string InlinePrefabPath = "Assets/Prefabs/UI/InlinePlusMarker.prefab";
    private const string EdgePrefabPath = "Assets/Prefabs/UI/EdgePlusMarker.prefab";

    [MenuItem("ScrapLine/Grid Expansion/Install or Repair")]
    public static void Install()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        UIGridManager uiGrid = FindSceneComponent<UIGridManager>();
        GridManager gridManager = FindSceneComponent<GridManager>();
        CreditsManager creditsManager = FindSceneComponent<CreditsManager>();
        Canvas canvas = FindSceneComponent<Canvas>();

        Require(uiGrid, "UIGridManager");
        Require(uiGrid.gridPanel, "UIGridManager.gridPanel");
        Require(gridManager, "GridManager");
        Require(creditsManager, "CreditsManager");
        Require(canvas, "Canvas");

        EnsureDirectory("Assets/Prefabs/UI");
        GameObject inlinePrefab = CreateMarkerPrefab(InlinePrefabPath, new Vector2(40f, 40f),
            new Color32(255, 153, 51, 255));
        GameObject edgePrefab = CreateMarkerPrefab(EdgePrefabPath, new Vector2(48f, 48f),
            new Color32(51, 204, 204, 255));

        DestroyExisting("ExpandModeSystem");
        DestroyExisting("GridExpansionOverlay");
        DestroyExisting("ExpansionCostPrompt");
        DestroyExisting("ExpandToggleButton");

        RectTransform overlayRoot = CreateRect("GridExpansionOverlay", uiGrid.gridPanel.parent);
        CopyRect(uiGrid.gridPanel, overlayRoot);
        LayoutElement overlayLayout = overlayRoot.gameObject.AddComponent<LayoutElement>();
        overlayLayout.ignoreLayout = true;
        overlayRoot.SetAsLastSibling();

        Image dimOverlay = CreateImage("DimOverlay", overlayRoot, new Color(0f, 0f, 0f, 0.4f));
        Stretch(dimOverlay.rectTransform);
        dimOverlay.raycastTarget = true;
        dimOverlay.gameObject.SetActive(false);

        RectTransform rowMarkers = CreateStretchedContainer("RowMarkersContainer", overlayRoot);
        RectTransform columnMarkers = CreateStretchedContainer("ColumnMarkersContainer", overlayRoot);
        RectTransform edgeMarkers = CreateStretchedContainer("EdgeMarkersContainer", overlayRoot);

        ExpansionCostPrompt prompt = CreatePrompt(canvas.transform);
        ManageTabButtonBinder buttonBinder = CreateToggleButton(canvas.transform);

        GameObject systemObject = new GameObject("ExpandModeSystem");
        ExpandModeController mode = systemObject.AddComponent<ExpandModeController>();
        GridExpansionService service = systemObject.AddComponent<GridExpansionService>();
        GridMarkersView markers = systemObject.AddComponent<GridMarkersView>();
        GridExpandAnimator animator = systemObject.AddComponent<GridExpandAnimator>();
        GridExpansionOrchestrator orchestrator = systemObject.AddComponent<GridExpansionOrchestrator>();

        mode.dimOverlay = dimOverlay;
        mode.dimAlpha = 0.4f;
        mode.enableExpandModeLogs = false;

        service.baseCost = 100;
        service.growthFactor = 2f;
        service.enableExpansionLogs = false;

        markers.expandModeController = mode;
        markers.gridExpansionService = service;
        markers.gridManager = gridManager;
        markers.uiGridManager = uiGrid;
        markers.inlinePlusMarkerPrefab = inlinePrefab;
        markers.edgePlusMarkerPrefab = edgePrefab;
        markers.rowMarkersContainer = rowMarkers;
        markers.columnMarkersContainer = columnMarkers;
        markers.edgeMarkersContainer = edgeMarkers;
        markers.enableMarkerLogs = false;

        animator.uiGridManager = uiGrid;
        animator.slideDuration = 0.15f;
        animator.slideEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        animator.enableAnimationLogs = false;

        orchestrator.expandModeController = mode;
        orchestrator.gridExpansionService = service;
        orchestrator.gridMarkersView = markers;
        orchestrator.gridExpandAnimator = animator;
        orchestrator.expansionCostPrompt = prompt;
        orchestrator.gridManager = gridManager;
        orchestrator.uiGridManager = uiGrid;
        orchestrator.creditsManager = creditsManager;
        orchestrator.enableOrchestrationLogs = false;

        buttonBinder.expandModeController = mode;
        buttonBinder.enableButtonLogs = false;
        prompt.enablePromptLogs = false;

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("Grid expansion installation completed successfully.");
    }

    [MenuItem("ScrapLine/Grid Expansion/Validate")]
    public static void Validate()
    {
        if (SceneManager.GetActiveScene().path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GridExpansionOrchestrator orchestrator = FindSceneComponent<GridExpansionOrchestrator>();
        ManageTabButtonBinder binder = FindSceneComponent<ManageTabButtonBinder>();
        Require(orchestrator, "GridExpansionOrchestrator");
        Require(orchestrator.expandModeController, "orchestrator.expandModeController");
        Require(orchestrator.gridExpansionService, "orchestrator.gridExpansionService");
        Require(orchestrator.gridMarkersView, "orchestrator.gridMarkersView");
        Require(orchestrator.gridExpandAnimator, "orchestrator.gridExpandAnimator");
        Require(orchestrator.expansionCostPrompt, "orchestrator.expansionCostPrompt");
        Require(orchestrator.gridManager, "orchestrator.gridManager");
        Require(orchestrator.uiGridManager, "orchestrator.uiGridManager");
        Require(orchestrator.creditsManager, "orchestrator.creditsManager");
        Require(binder, "ManageTabButtonBinder");
        Require(binder.expandToggleButton, "binder.expandToggleButton");
        Require(binder.expandModeController, "binder.expandModeController");

        GridMarkersView markers = orchestrator.gridMarkersView;
        Require(markers.inlinePlusMarkerPrefab, "markers.inlinePlusMarkerPrefab");
        Require(markers.edgePlusMarkerPrefab, "markers.edgePlusMarkerPrefab");
        Require(markers.rowMarkersContainer, "markers.rowMarkersContainer");
        Require(markers.columnMarkersContainer, "markers.columnMarkersContainer");
        Require(markers.edgeMarkersContainer, "markers.edgeMarkersContainer");

        if (orchestrator.gridExpansionService.ComputeExpansionCost(7, 5,
                GridExpansionService.ExpansionType.InsertRow) != 170)
            throw new InvalidOperationException("Expansion cost validation failed.");

        if (!EditorBuildSettings.scenes.Any(s => s.enabled && s.path == ScenePath))
            throw new InvalidOperationException("MobileGridScene is not enabled in Build Settings.");

        Debug.Log("GRID_EXPANSION_VALIDATION_PASSED");
    }

    private static GameObject CreateMarkerPrefab(string path, Vector2 size, Color color)
    {
        GameObject root = new GameObject(Path.GetFileNameWithoutExtension(path),
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(CanvasGroup));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        Image image = root.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        Button button = root.GetComponent<Button>();
        button.targetGraphic = image;

        TextMeshProUGUI plus = CreateText("Plus", root.transform, "+", 34f, Color.white);
        Stretch(plus.rectTransform);
        plus.raycastTarget = false;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    private static ExpansionCostPrompt CreatePrompt(Transform canvas)
    {
        Image panel = CreateImage("ExpansionCostPrompt", canvas, new Color32(25, 29, 36, 245));
        RectTransform rect = panel.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(380f, 190f);
        rect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI title = CreateText("CostText", rect, "Expand here?", 23f, Color.white);
        title.rectTransform.anchorMin = new Vector2(0f, 0.48f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.offsetMin = new Vector2(20f, 0f);
        title.rectTransform.offsetMax = new Vector2(-20f, -10f);

        Button confirm = CreateTextButton("ConfirmButton", rect, "Confirm",
            new Color32(76, 175, 80, 255), new Vector2(-75f, -55f));
        Button cancel = CreateTextButton("CancelButton", rect, "Cancel",
            new Color32(244, 67, 54, 255), new Vector2(75f, -55f));

        ExpansionCostPrompt prompt = panel.gameObject.AddComponent<ExpansionCostPrompt>();
        prompt.promptPanel = panel.gameObject;
        prompt.costText = title;
        prompt.confirmButton = confirm;
        prompt.cancelButton = cancel;
        panel.gameObject.SetActive(false);
        panel.transform.SetAsLastSibling();
        return prompt;
    }

    private static ManageTabButtonBinder CreateToggleButton(Transform canvas)
    {
        Transform managePanel = FindByName("ManagePanel")?.transform ?? canvas;
        ScrollRect manageScroll = managePanel.GetComponent<ScrollRect>();
        Transform buttonParent = manageScroll != null && manageScroll.content != null
            ? manageScroll.content
            : managePanel;
        Image image = CreateImage("ExpandToggleButton", buttonParent, new Color32(255, 153, 51, 255));
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(180f, 0f);
        rect.anchoredPosition = Vector2.zero;
        LayoutElement layout = image.gameObject.AddComponent<LayoutElement>();
        layout.ignoreLayout = false;
        layout.minWidth = 180f;
        layout.preferredWidth = 180f;
        layout.flexibleWidth = 0f;

        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        TextMeshProUGUI label = CreateText("Label", rect, "Expand Grid", 24f, Color.white);
        Stretch(label.rectTransform);
        label.enableAutoSizing = true;
        label.fontSizeMin = 14f;
        label.fontSizeMax = 28f;
        label.raycastTarget = false;

        ManageTabButtonBinder binder = image.gameObject.AddComponent<ManageTabButtonBinder>();
        binder.expandToggleButton = button;
        binder.buttonImage = image;
        return binder;
    }

    private static Button CreateTextButton(string name, Transform parent, string label, Color color,
        Vector2 anchoredPosition)
    {
        Image image = CreateImage(name, parent, color);
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(130f, 48f);
        rect.anchoredPosition = anchoredPosition;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        TextMeshProUGUI text = CreateText("Label", rect, label, 19f, Color.white);
        Stretch(text.rectTransform);
        text.raycastTarget = false;
        return button;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string value, float size, Color color)
    {
        GameObject child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        child.transform.SetParent(parent, false);
        TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        return text;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        child.transform.SetParent(parent, false);
        Image image = child.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static RectTransform CreateStretchedContainer(string name, Transform parent)
    {
        RectTransform rect = CreateRect(name, parent);
        Stretch(rect);
        return rect;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void CopyRect(RectTransform source, RectTransform target)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.localScale = source.localScale;
    }

    private static T FindSceneComponent<T>() where T : Component
    {
        return UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(component => component.gameObject.scene.IsValid());
    }

    private static GameObject FindByName(string name)
    {
        return UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(transform => transform.gameObject.scene.IsValid() && transform.name == name)?.gameObject;
    }

    private static void DestroyExisting(string name)
    {
        GameObject existing = FindByName(name);
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing);
    }

    private static void EnsureDirectory(string path)
    {
        string current = "";
        foreach (string part in path.Split('/'))
        {
            string next = string.IsNullOrEmpty(current) ? part : current + "/" + part;
            if (!AssetDatabase.IsValidFolder(next) && !string.IsNullOrEmpty(current))
                AssetDatabase.CreateFolder(current, part);
            current = next;
        }
    }

    private static void Require(UnityEngine.Object value, string label)
    {
        if (value == null)
            throw new InvalidOperationException($"Missing required grid expansion reference: {label}");
    }
}
