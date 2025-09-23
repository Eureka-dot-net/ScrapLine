using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// UI component for configuring fabricator machines with recipe selection.
/// This component should be attached to a UI panel that contains:
/// - One button for recipe configuration (showing selected recipe output)
/// - A secondary panel for recipe selection with recipe buttons
/// - Confirm and Cancel buttons
/// 
/// UNITY SETUP REQUIRED:
/// 1. Create a main UI Panel GameObject in the scene
/// 2. Add this component to the main panel
/// 3. Add one Button component for recipeConfigButton
/// 4. Add a secondary Panel for recipe selection (recipeSelectionPanel)
/// 5. Add Button prefab for recipe buttons (recipeButtonPrefab)
/// 6. Add two Button components for confirmButton and cancelButton
/// 7. Assign all UI components in the inspector
/// 8. Set the panels inactive by default (will be activated when configuration is needed)
/// </summary>
public class FabricatorMachineConfigUI : MonoBehaviour
{
    [Header("Main Configuration UI Components")]
    [Tooltip("Button for configuring recipe (shows selected recipe output)")]
    public Button recipeConfigButton;
    
    [Tooltip("Button to confirm configuration")]
    public Button confirmButton;
    
    [Tooltip("Button to cancel configuration")]
    public Button cancelButton;
    
    [Header("Recipe Selection Panel")]
    [Tooltip("Panel that shows when selecting recipes")]
    public GameObject recipeSelectionPanel;
    
    [Tooltip("Container for recipe selection buttons")]
    public Transform recipeButtonContainer;
    
    [Tooltip("Prefab for recipe buttons")]
    public GameObject recipeButtonPrefab;
    
    [Header("Configuration")]
    [Tooltip("Main panel to show/hide for configuration")]
    public GameObject configPanel;

    private CellData currentCellData;
    private System.Action<string> onConfigurationConfirmed;
    private string selectedRecipeId = "";

    void Start()
    {
        // Setup button listeners
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);
        
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);

        if (recipeConfigButton != null)
            recipeConfigButton.onClick.AddListener(ShowRecipeSelection);

        // Hide the configuration panels initially
        if (configPanel != null)
            configPanel.SetActive(false);
        
        if (recipeSelectionPanel != null)
            recipeSelectionPanel.SetActive(false);
    }

    /// <summary>
    /// Show the configuration UI for a fabricator machine
    /// </summary>
    /// <param name="cellData">The cell data of the fabricator machine to configure</param>
    /// <param name="onConfirmed">Callback when configuration is confirmed</param>
    public void ShowConfiguration(CellData cellData, System.Action<string> onConfirmed)
    {
        currentCellData = cellData;
        onConfigurationConfirmed = onConfirmed;

        // Load current configuration if any
        selectedRecipeId = cellData.selectedRecipeId ?? "";

        // Update button visuals with current selection
        UpdateConfigButtonVisual();

        // Show the main configuration panel
        if (configPanel != null)
            configPanel.SetActive(true);
        else
            gameObject.SetActive(true);

        GameLogger.Log(LoggingManager.LogCategory.UI, "Showing fabricator configuration UI for machine at ({cellData.x}, {cellData.y})", ComponentId);
    }

    /// <summary>
    /// Show the recipe selection panel
    /// </summary>
    private void ShowRecipeSelection()
    {
        // Populate recipe selection buttons
        PopulateRecipeSelectionButtons();
        
        // Show the recipe selection panel
        if (recipeSelectionPanel != null)
            recipeSelectionPanel.SetActive(true);
        
        GameLogger.Log(LoggingManager.LogCategory.UI, "Showing recipe selection for fabricator configuration", ComponentId);
    }

    /// <summary>
    /// Hide the recipe selection panel
    /// </summary>
    private void HideRecipeSelection()
    {
        if (recipeSelectionPanel != null)
            recipeSelectionPanel.SetActive(false);
    }

    /// <summary>
    /// Populate the recipe selection panel with recipe buttons (only craftable items)
    /// </summary>
    private void PopulateRecipeSelectionButtons()
    {
        if (recipeButtonContainer == null || recipeButtonPrefab == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.UI, "FabricatorMachineConfigUI: Recipe button container or prefab not assigned!", ComponentId);
            return;
        }

        // Clear existing buttons
        foreach (Transform child in recipeButtonContainer)
        {
            Destroy(child.gameObject);
        }

        // Add "None" option
        CreateRecipeButton("", "None", null);

        // Get available recipes from FactoryRegistry for this machine
        if (FactoryRegistry.Instance != null && currentCellData != null)
        {
            foreach (var recipe in FactoryRegistry.Instance.Recipes)
            {
                // Only show recipes that this machine can craft
                if (recipe.machineId == currentCellData.machineDefId)
                {
                    // Get the output item for display
                    if (recipe.outputItems.Count > 0)
                    {
                        string outputItemId = recipe.outputItems[0].item;
                        ItemDef outputItem = FactoryRegistry.Instance.GetItem(outputItemId);
                        
                        string displayName = outputItem?.displayName ?? outputItemId;
                        string spriteName = outputItem?.sprite ?? outputItemId;
                        
                        CreateRecipeButton(GetRecipeId(recipe), displayName, spriteName);
                    }
                }
            }
        }
        else
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.UI, "FactoryRegistry not available or currentCellData is null", ComponentId);
        }
    }

    /// <summary>
    /// Generate a unique ID for a recipe based on its inputs and outputs
    /// </summary>
    private string GetRecipeId(RecipeDef recipe)
    {
        // Create a unique ID based on machine, inputs, and outputs
        string inputs = string.Join(",", recipe.inputItems.ConvertAll(i => i.item + ":" + i.count));
        string outputs = string.Join(",", recipe.outputItems.ConvertAll(o => o.item + ":" + o.count));
        return $"{recipe.machineId}_{inputs}_{outputs}";
    }

    /// <summary>
    /// Create a recipe selection button
    /// </summary>
    private void CreateRecipeButton(string recipeId, string displayName, string spriteName)
    {
        GameObject buttonObj = Instantiate(recipeButtonPrefab, recipeButtonContainer);
        Button button = buttonObj.GetComponent<Button>();
        
        if (button != null)
        {
            // Set up button click listener
            button.onClick.AddListener(() => OnRecipeSelected(recipeId));
            
            // Try to find and set the sprite
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage == null)
                buttonImage = button.GetComponentInChildren<Image>();
            
            if (buttonImage != null && !string.IsNullOrEmpty(spriteName))
            {
                Sprite itemSprite = Resources.Load<Sprite>($"Sprites/Items/{spriteName}");
                if (itemSprite != null)
                {
                    buttonImage.sprite = itemSprite;
                }
            }
            
            // Set text if there's a Text component
            Text buttonText = button.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = displayName;
            }
        }
    }

    /// <summary>
    /// Called when a recipe is selected from the recipe selection panel
    /// </summary>
    private void OnRecipeSelected(string recipeId)
    {
        selectedRecipeId = recipeId;

        // Update the configuration button visual
        UpdateConfigButtonVisual();
        
        // Hide the recipe selection panel
        HideRecipeSelection();
        
        GameLogger.Log(LoggingManager.LogCategory.UI, "Selected recipe '{recipeId}' for fabricator configuration", ComponentId);
    }

    /// <summary>
    /// Update the recipe configuration button to show selected recipe
    /// </summary>
    private void UpdateConfigButtonVisual()
    {
        if (recipeConfigButton == null) return;

        // Get button image and text components
        Image buttonImage = recipeConfigButton.GetComponent<Image>();
        if (buttonImage == null)
            buttonImage = recipeConfigButton.GetComponentInChildren<Image>();
        
        Text buttonText = recipeConfigButton.GetComponentInChildren<Text>();

        if (string.IsNullOrEmpty(selectedRecipeId))
        {
            // No recipe selected - show default text
            if (buttonText != null)
                buttonText.text = "Select Recipe";
            
            // Reset to default button appearance
            if (buttonImage != null)
                buttonImage.sprite = null;
        }
        else
        {
            // Recipe selected - show recipe output info
            RecipeDef selectedRecipe = GetRecipeFromId(selectedRecipeId);
            if (selectedRecipe != null && selectedRecipe.outputItems.Count > 0)
            {
                string outputItemId = selectedRecipe.outputItems[0].item;
                ItemDef outputItem = FactoryRegistry.Instance?.GetItem(outputItemId);
                string displayName = outputItem?.displayName ?? outputItemId;
                string spriteName = outputItem?.sprite ?? outputItemId;

                if (buttonText != null)
                    buttonText.text = displayName;

                // Load and set the item sprite
                if (buttonImage != null && !string.IsNullOrEmpty(spriteName))
                {
                    Sprite itemSprite = Resources.Load<Sprite>($"Sprites/Items/{spriteName}");
                    if (itemSprite != null)
                    {
                        buttonImage.sprite = itemSprite;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Get recipe definition from recipe ID
    /// </summary>
    private RecipeDef GetRecipeFromId(string recipeId)
    {
        if (string.IsNullOrEmpty(recipeId) || FactoryRegistry.Instance == null) return null;
        
        foreach (var recipe in FactoryRegistry.Instance.Recipes)
        {
            if (GetRecipeId(recipe) == recipeId)
                return recipe;
        }
        return null;
    }

    private void OnConfirmClicked()
    {
        GameLogger.Log(LoggingManager.LogCategory.UI, "Fabricator configuration confirmed: Recipe={selectedRecipeId}", ComponentId);

        // Update the cell data
        if (currentCellData != null)
        {
            currentCellData.selectedRecipeId = selectedRecipeId;
        }

        // Call the callback
        onConfigurationConfirmed?.Invoke(selectedRecipeId);

        // Hide the configuration panel
        HideConfiguration();
    }

    private void OnCancelClicked()
    {
        GameLogger.Log(LoggingManager.LogCategory.UI, "Fabricator configuration cancelled", ComponentId);
        HideConfiguration();
    }

    private void HideConfiguration()
    {
        if (configPanel != null)
            configPanel.SetActive(false);
        else
            gameObject.SetActive(false);

        if (recipeSelectionPanel != null)
            recipeSelectionPanel.SetActive(false);

        currentCellData = null;
        onConfigurationConfirmed = null;
    }
}