using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Generic base class for all configuration panels in ScrapLine.
/// Handles common UI patterns: main config panel, confirm/cancel buttons, lifecycle management.
/// 
/// TData = Type of data being configured (e.g., CellData)
/// TSelection = Type of selection result (e.g., string, Tuple<string,string>)
/// 
/// UNITY SETUP REQUIRED:
/// 1. Add this component to a main UI Panel GameObject
/// 2. Assign configPanel, confirmButton, cancelButton in inspector
/// 3. Override abstract methods in derived classes
/// 4. Call base.Start() in derived Start() methods
/// </summary>
public abstract class BaseConfigPanel<TData, TSelection> : MonoBehaviour
{
    [Header("Base Config Panel Components")]
    [Tooltip("Main panel to show/hide for configuration")]
    public GameObject configPanel;

    [Header("Detail Config Panel Components")]
    [Tooltip("Detail panel to show/hide for configuration")]
    public GameObject detailConfigPanel;

    [Tooltip("Panel containing the title bar")]
    public GameObject titlePanel;

    [Tooltip("Text to display in the title bar")]
    public string titleText;

    [Tooltip("Button to confirm configuration")]
    public Button confirmButton;

    [Tooltip("Button to cancel configuration")]
    public Button cancelButton;

    // Protected state available to derived classes
    protected TData currentData;
    protected Action<TSelection> onConfigurationConfirmed;
    protected string ComponentId => $"{GetType().Name}_{GetInstanceID()}";

    /// <summary>
    /// Initialize the base panel - call this from derived Start() methods
    /// </summary>
    protected virtual void Start()
    {
        SetupBaseButtonListeners();
        SetupCustomButtonListeners();
        HideConfiguration();

        GameLogger.Log(LoggingManager.LogCategory.UI, $"Initialized {GetType().Name}", ComponentId);
    }

    /// <summary>
    /// Setup confirm/cancel button listeners
    /// </summary>
    private void SetupBaseButtonListeners()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);
    }

    /// <summary>
    /// Show the configuration UI for the given data
    /// </summary>
    /// <param name="data">Data to configure</param>
    /// <param name="onConfirmed">Callback when configuration is confirmed</param>
    public virtual void ShowConfiguration(TData data, Action<TSelection> onConfirmed)
    {
        currentData = data;
        onConfigurationConfirmed = onConfirmed;

        if (detailConfigPanel != null)
            detailConfigPanel.SetActive(true);

        if (titlePanel != null)
        {
            titlePanel.SetActive(true);
            var textComp = titlePanel.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (textComp != null)
                textComp.text = titleText;
        }

        // Load current configuration from data
        LoadCurrentConfiguration();

        // Update UI to reflect current state
        UpdateUIFromCurrentState();

        // Show the main configuration panel
        if (configPanel != null)
            configPanel.SetActive(true);
        else
            gameObject.SetActive(true);

        // Notify derived classes
        OnConfigurationShown();

        GameLogger.Log(LoggingManager.LogCategory.UI, $"Configuration shown for {GetType().Name}", ComponentId);
    }

    /// <summary>
    /// Hide the configuration UI
    /// </summary>
    protected virtual void HideConfiguration()
    {
        if (configPanel != null)
            configPanel.SetActive(false);
        else
            gameObject.SetActive(false);

        if (detailConfigPanel != null)
            detailConfigPanel.SetActive(false);

        // Hide any selection panels
        HideSelectionPanels();

        // Notify derived classes
        OnConfigurationHidden();

        // Clear state
        currentData = default(TData);
        onConfigurationConfirmed = null;

        GameLogger.Log(LoggingManager.LogCategory.UI, $"Configuration hidden for {GetType().Name}", ComponentId);
    }

    /// <summary>
    /// Handle confirm button click
    /// </summary>
    private void OnConfirmClicked()
    {
        // Get the current selection from derived class
        TSelection selection = GetCurrentSelection();

        // Update the data with the selection
        UpdateDataWithSelection(selection);

        // Call the callback
        onConfigurationConfirmed?.Invoke(selection);

        // Notify derived classes
        OnConfigurationConfirmed(selection);

        GameLogger.Log(LoggingManager.LogCategory.UI, $"Configuration confirmed for {GetType().Name}: {selection}", ComponentId);

        // Hide the configuration panel
        HideConfiguration();
    }

    /// <summary>
    /// Handle cancel button click
    /// </summary>
    private void OnCancelClicked()
    {
        OnConfigurationCancelled();

        GameLogger.Log(LoggingManager.LogCategory.UI, $"Configuration cancelled for {GetType().Name}", ComponentId);

        HideConfiguration();
    }

    // Abstract methods that derived classes must implement

    /// <summary>
    /// Setup custom button listeners specific to this panel type
    /// </summary>
    protected abstract void SetupCustomButtonListeners();

    /// <summary>
    /// Load the current configuration from the data object
    /// </summary>
    protected abstract void LoadCurrentConfiguration();

    /// <summary>
    /// Update the UI to reflect the current configuration state
    /// </summary>
    protected abstract void UpdateUIFromCurrentState();

    /// <summary>
    /// Get the current selection from the UI
    /// </summary>
    /// <returns>Current selection result</returns>
    protected abstract TSelection GetCurrentSelection();

    /// <summary>
    /// Update the data object with the confirmed selection
    /// </summary>
    /// <param name="selection">Selection to apply to data</param>
    protected abstract void UpdateDataWithSelection(TSelection selection);

    /// <summary>
    /// Hide any selection panels specific to this panel type
    /// </summary>
    protected abstract void HideSelectionPanels();

    // Virtual methods with default implementations that can be overridden

    /// <summary>
    /// Called when configuration is first shown
    /// </summary>
    protected virtual void OnConfigurationShown() { }

    /// <summary>
    /// Called when configuration is hidden
    /// </summary>
    protected virtual void OnConfigurationHidden() { }

    /// <summary>
    /// Called when configuration is confirmed
    /// </summary>
    /// <param name="selection">The confirmed selection</param>
    protected virtual void OnConfigurationConfirmed(TSelection selection) { }

    /// <summary>
    /// Called when configuration is cancelled
    /// </summary>
    protected virtual void OnConfigurationCancelled() { }
}