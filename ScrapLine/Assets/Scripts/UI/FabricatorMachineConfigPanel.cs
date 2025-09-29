using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Configuration panel for fabricator machines using Plan A base classes.
/// Reduces original 339 lines to ~60 lines by leveraging BaseConfigPanel.
/// 
/// Handles single recipe selection for fabricator machine configuration.
/// 
/// UNITY SETUP REQUIRED:
/// 1. Create main UI Panel GameObject with this component
/// 2. Assign configPanel, confirmButton, cancelButton (from BaseConfigPanel)
/// 3. Assign recipeConfigButton
/// 4. Create RecipeSelectionPanel and assign recipeSelectionPanel reference
/// 5. Set panels inactive by default (activated when configuration needed)
/// </summary>
public class FabricatorMachineConfigPanel : BaseConfigPanel<CellData, string>
{
    [Header("Fabricator Machine Specific")]
    [Tooltip("Button for configuring recipe (shows selected recipe output)")]
    public Button recipeConfigButton;
    
    [Tooltip("Recipe selection panel component")]
    public RecipeSelectionPanel recipeSelectionPanel;

    // Selection state
    private string selectedRecipeId = "";

     private Sprite emptySelectionSprite;

     protected override void Start()
     {
              // Load empty selection sprite
         emptySelectionSprite = recipeConfigButton?.GetComponent<Image>()?.sprite;
         if (emptySelectionSprite == null)
         {
             GameLogger.LogError(LoggingManager.LogCategory.UI, "Failed to load empty selection sprite!", ComponentId);
         }

         base.Start();
     }

    protected override void SetupCustomButtonListeners()
    {
        if (recipeConfigButton != null)
            recipeConfigButton.onClick.AddListener(ShowRecipeSelection);
    }

    protected override void LoadCurrentConfiguration()
    {
        selectedRecipeId = currentData?.selectedRecipeId ?? "";
        
        GameLogger.Log(LoggingManager.LogCategory.UI, $"Loaded fabricator config: Recipe='{selectedRecipeId}'", ComponentId);
    }

    protected override void UpdateUIFromCurrentState()
    {
        UpdateRecipeButtonVisual();
    }

    protected override string GetCurrentSelection()
    {
        return selectedRecipeId;
    }

    protected override void UpdateDataWithSelection(string selection)
    {
        if (currentData != null)
            currentData.selectedRecipeId = selection;
    }

    protected override void HideSelectionPanels()
    {
        if (recipeSelectionPanel != null)
            recipeSelectionPanel.HidePanel();
    }

    /// <summary>
    /// Show recipe selection panel
    /// </summary>
    private void ShowRecipeSelection()
    {
        if (recipeSelectionPanel != null && currentData != null)
        {
            recipeSelectionPanel.ShowPanel(currentData, OnRecipeSelected);
        }
        else
        {
            GameLogger.LogError(LoggingManager.LogCategory.UI, "RecipeSelectionPanel not assigned or no current data!", ComponentId);
        }
    }

    /// <summary>
    /// Handle recipe selection from the selection panel
    /// </summary>
    /// <param name="recipe">Selected recipe (null for "None")</param>
    private void OnRecipeSelected(RecipeDef recipe)
    {
        selectedRecipeId = RecipeSelectionPanel.GetRecipeId(recipe);
        
        GameLogger.Log(LoggingManager.LogCategory.UI, $"Selected recipe: '{selectedRecipeId}'", ComponentId);

        // Update button visual
        UpdateUIFromCurrentState();
    }

    /// <summary>
    /// Update the recipe configuration button to show selected recipe
    /// </summary>
    private void UpdateRecipeButtonVisual()
    {
        if (recipeConfigButton == null) return;

        // Get button components
        Image buttonImage = recipeConfigButton.GetComponent<Image>();
        if (buttonImage == null)
            buttonImage = recipeConfigButton.GetComponentInChildren<Image>();
        
        Text buttonText = recipeConfigButton.GetComponentInChildren<Text>();

        if (string.IsNullOrEmpty(selectedRecipeId))
        {
            
            if (buttonImage != null && emptySelectionSprite != null)
                buttonImage.sprite = emptySelectionSprite;
        }
        else
        {
            // Recipe selected - show recipe output info
            RecipeDef selectedRecipe = RecipeSelectionPanel.GetRecipeById(selectedRecipeId);
            
            if (selectedRecipe != null && selectedRecipe.outputItems != null && selectedRecipe.outputItems.Count > 0)
            {
                string outputItemId = selectedRecipe.outputItems[0].item;
                ItemDef outputItem = FactoryRegistry.Instance?.GetItem(outputItemId);
                
                if (outputItem != null)
                {
                    if (buttonText != null)
                        buttonText.text = outputItem.displayName ?? outputItemId;

                    if (buttonImage != null && !string.IsNullOrEmpty(outputItem.sprite))
                    {
                        Sprite itemSprite = Resources.Load<Sprite>($"Sprites/Items/{outputItem.sprite}");
                        if (itemSprite != null)
                        {
                            buttonImage.sprite = itemSprite;
                        }
                    }
                }
                else
                {
                    // Fallback for unknown output item
                    if (buttonText != null)
                        buttonText.text = outputItemId;
                }
            }
            else
            {
                // Fallback for invalid recipe
                if (buttonText != null)
                    buttonText.text = "Invalid Recipe";
            }
        }
    }
}