using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

/// <summary>
/// Generic base class for selection panels that display lists of selectable items.
/// Handles dynamic button creation, sprite loading, and selection workflow.
/// 
/// TItem = Type of item being selected (e.g., ItemDef, RecipeDef, WasteCrateDef)
/// 
/// UNITY SETUP REQUIRED:
/// 1. Add this component to a selection panel GameObject
/// 2. Assign selectionPanel, buttonContainer, buttonPrefab in inspector
/// 3. Override abstract methods in derived classes
/// 4. Call ShowPanel() to display selection options
/// </summary>
public abstract class BaseSelectionPanel<TItem> : MonoBehaviour
{
    [Header("Base Selection Panel Components")]
    [Tooltip("Panel that shows when selecting items")]
    public GameObject selectionPanel;
    
    [Tooltip("Container for selection buttons (should have Grid or Horizontal/Vertical Layout Group)")]
    public Transform buttonContainer;
    
    [Tooltip("Prefab for selection buttons (must have Button component)")]
    public GameObject buttonPrefab;

    // Protected state available to derived classes
    protected Action<TItem> onItemSelected;
    protected string ComponentId => $"{GetType().Name}_{GetInstanceID()}";

    /// <summary>
    /// Show the selection panel with available options
    /// </summary>
    /// <param name="onSelected">Callback when item is selected</param>
    public virtual void ShowPanel(Action<TItem> onSelected)
    {
        onItemSelected = onSelected;
        
        // Populate the selection buttons
        PopulateButtons();
        
        // Show the selection panel
        if (selectionPanel != null)
            selectionPanel.SetActive(true);
            
        GameLogger.Log(LoggingManager.LogCategory.UI, $"Selection panel shown for {GetType().Name}", ComponentId);
    }

    /// <summary>
    /// Hide the selection panel
    /// </summary>
    public virtual void HidePanel()
    {
        if (selectionPanel != null)
            selectionPanel.SetActive(false);
            
        GameLogger.Log(LoggingManager.LogCategory.UI, $"Selection panel hidden for {GetType().Name}", ComponentId);
    }

    /// <summary>
    /// Get the button prefab to use for creating selection buttons.
    /// Can be overridden by derived classes to use a different prefab.
    /// </summary>
    /// <returns>Button prefab to instantiate</returns>
    protected virtual GameObject GetButtonPrefabToUse()
    {
        return buttonPrefab;
    }

    /// <summary>
    /// Populate the selection panel with available options
    /// </summary>
    protected virtual void PopulateButtons()
    {
        GameObject prefabToUse = GetButtonPrefabToUse();
        
        if (buttonContainer == null || prefabToUse == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.UI, $"{GetType().Name}: Button container or prefab not assigned!", ComponentId);
            return;
        }

        // Clear existing buttons
        ClearExistingButtons();
        
        // Add "None" option if supported
        if (SupportsNoneOption())
        {
            CreateSelectionButton(GetNoneOption(), GetNoneDisplayName());
        }

        // Add all available items
        var availableItems = GetAvailableItems();
        if (availableItems != null)
        {
            foreach (var item in availableItems)
            {
                string displayName = GetDisplayName(item);
                CreateSelectionButton(item, displayName);
            }
        }
        
        GameLogger.Log(LoggingManager.LogCategory.UI, $"Populated {GetType().Name} with {availableItems?.Count ?? 0} items", ComponentId);
    }

    /// <summary>
    /// Clear all existing selection buttons
    /// </summary>
    protected virtual void ClearExistingButtons()
    {
        if (buttonContainer == null) return;
        
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Create a selection button for an item
    /// </summary>
    /// <param name="item">Item to create button for</param>
    /// <param name="displayName">Display name for the button</param>
    protected virtual void CreateSelectionButton(TItem item, string displayName)
    {
        GameObject prefabToUse = GetButtonPrefabToUse();
        
        if (buttonContainer == null || prefabToUse == null) return;

        GameObject buttonObj = Instantiate(prefabToUse, buttonContainer);
        Button button = buttonObj.GetComponent<Button>();
        
        if (button != null)
        {
            // Set up button click listener
            button.onClick.AddListener(() => OnItemSelected(item));
            
            // Setup button visuals using derived class implementation
            SetupButtonVisuals(buttonObj, item, displayName);
        }
        else
        {
            GameLogger.LogError(LoggingManager.LogCategory.UI, $"{GetType().Name}: Button prefab missing Button component!", ComponentId);
        }
    }

    /// <summary>
    /// Handle item selection
    /// </summary>
    /// <param name="item">Selected item</param>
    protected virtual void OnItemSelected(TItem item)
    {
        // Notify callback
        onItemSelected?.Invoke(item);
        
        // Notify derived classes
        OnItemSelectionMade(item);
        
        GameLogger.Log(LoggingManager.LogCategory.UI, $"Item selected in {GetType().Name}: {GetDisplayName(item)}", ComponentId);
        
        // Hide the selection panel
        HidePanel();
    }

    /// <summary>
    /// Standard sprite loading utility for derived classes
    /// </summary>
    /// <param name="spritePath">Resource path to sprite</param>
    /// <returns>Loaded sprite or null</returns>
    protected Sprite LoadSprite(string spritePath)
    {
        if (string.IsNullOrEmpty(spritePath)) return null;
        
        Sprite sprite = Resources.Load<Sprite>(spritePath);
        if (sprite == null)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, $"Could not load sprite at path: {spritePath}", ComponentId);
        }
        return sprite;
    }

    /// <summary>
    /// Standard button text setup utility
    /// </summary>
    /// <param name="buttonObj">Button GameObject</param>
    /// <param name="text">Text to set</param>
    protected void SetButtonText(GameObject buttonObj, string text)
    {
        if (buttonObj == null || string.IsNullOrEmpty(text)) return;
        
        Text buttonText = buttonObj.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.text = text;
        }
        else
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, $"Button prefab missing Text component for: {text}", ComponentId);
        }
    }

    /// <summary>
    /// Standard button image setup utility
    /// </summary>
    /// <param name="buttonObj">Button GameObject</param>
    /// <param name="sprite">Sprite to set</param>
    protected void SetButtonImage(GameObject buttonObj, Sprite sprite)
    {
        if (buttonObj == null) return;

        Image buttonImage = buttonObj.GetComponent<Image>();
        if (buttonImage == null)
            buttonImage = buttonObj.GetComponentInChildren<Image>();

        if (buttonImage != null && sprite != null)
        {
            buttonImage.sprite = sprite;
            buttonImage.color = Color.white;
        }
    }

    // Abstract methods that derived classes must implement
    
    /// <summary>
    /// Get all available items for selection
    /// </summary>
    /// <returns>List of available items</returns>
    protected abstract List<TItem> GetAvailableItems();
    
    /// <summary>
    /// Get display name for an item
    /// </summary>
    /// <param name="item">Item to get display name for</param>
    /// <returns>Display name</returns>
    protected abstract string GetDisplayName(TItem item);
    
    /// <summary>
    /// Setup the visual appearance of a selection button
    /// </summary>
    /// <param name="buttonObj">Button GameObject to setup</param>
    /// <param name="item">Item this button represents</param>
    /// <param name="displayName">Display name for the item</param>
    protected abstract void SetupButtonVisuals(GameObject buttonObj, TItem item, string displayName);

    // Virtual methods with default implementations that can be overridden
    
    /// <summary>
    /// Whether this selection panel supports a "None" option
    /// </summary>
    /// <returns>True if "None" option should be included</returns>
    protected virtual bool SupportsNoneOption() => true;
    
    /// <summary>
    /// Get the "None" option item
    /// </summary>
    /// <returns>Item representing "None" selection</returns>
    protected virtual TItem GetNoneOption() => default(TItem);
    
    /// <summary>
    /// Get display name for "None" option
    /// </summary>
    /// <returns>Display text for "None" option</returns>
    protected virtual string GetNoneDisplayName() => "None";
    
    /// <summary>
    /// Called when an item selection is made
    /// </summary>
    /// <param name="item">Selected item</param>
    protected virtual void OnItemSelectionMade(TItem item) { }
}