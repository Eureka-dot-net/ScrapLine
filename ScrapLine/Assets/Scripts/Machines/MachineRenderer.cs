using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders a machine using sprites from definition and creates its own RawImage for moving parts.
/// Other images (border, building, main) are created automatically as children.
/// </summary>
public class MachineRenderer : MonoBehaviour
{
    [Header("Context")]
    public bool isInMenu = false; // Set to true when used in UI panels to disable materials

    /// <summary>
    /// Get the component ID for logging purposes
    /// </summary>
    private string ComponentId => $"MachineRenderer_{GetInstanceID()}";

    // For separated building rendering
    private UIGridManager gridManager;
    private int cellX, cellY;
    private GameObject buildingSprite; // Track building sprite separately
    private GameObject borderSprite; // Track border sprite separately
    private GameObject movingPartSprite; // Track moving part sprite separately

    // Progress bar components
    private GameObject progressBarContainer; // Container for progress bar UI
    private UnityEngine.UI.Image progressBarBackground; // Background of progress bar
    private UnityEngine.UI.Image progressBarFill; // Fill of progress bar
    private BaseMachine associatedMachine; // Reference to machine for progress updates
    private float lastProgressUpdate; // Track last update time for 1-second intervals

    // Moving part visual references (created internally)
    [NonSerialized] private RawImage movingPartRawImage;
    [NonSerialized] private Texture movingPartTexture;
    [NonSerialized] private Material movingPartMaterial;

    /// <summary>
    /// Setup the renderer. Pass in Texture and Material for moving part if needed.
    /// </summary>
    /// <param name="baseMachine">The BaseMachine instance containing the definition and behavior</param>
    /// <param name="cellDirection"></param>
    /// <param name="gridManager"></param>
    /// <param name="cellX"></param>
    /// <param name="cellY"></param>
    /// <param name="movingPartTexture">Optional: assign this if using moving part</param>
    /// <param name="movingPartMaterial">Optional: assign this if using moving part</param>
    public void Setup(
        BaseMachine baseMachine,
        UICell.Direction cellDirection = UICell.Direction.Up,
        UIGridManager gridManager = null,
        int cellX = 0,
        int cellY = 0,
        Texture movingPartTexture = null,
        Material movingPartMaterial = null
    )
    {
        this.gridManager = gridManager;
        this.cellX = cellX;
        this.cellY = cellY;
        this.movingPartTexture = movingPartTexture;
        this.movingPartMaterial = movingPartMaterial;
        this.associatedMachine = baseMachine; // Store reference for progress bar updates

        // Get the machine definition for easier access
        MachineDef def = baseMachine.MachineDef;

        foreach (Transform child in transform) Destroy(child.gameObject);

        // Clean up any existing building sprite in separate container
        if (buildingSprite != null)
        {
            Destroy(buildingSprite);
            buildingSprite = null;
        }

        // Clean up any existing border sprite in separate container
        if (borderSprite != null)
        {
            Destroy(borderSprite);
            borderSprite = null;
        }

        // Clean up any existing moving part sprite in separate container
        if (movingPartSprite != null)
        {
            Destroy(movingPartSprite);
            movingPartSprite = null;
        }

        // --- Moving Part: create RawImage in BordersContainer ---
        if (def.isMoving && movingPartTexture != null && !isInMenu && gridManager != null)
        {
            CreateSeparatedMovingPart(def, cellDirection);
        }
        else if (def.isMoving && movingPartTexture != null)
        {
            // For menu context, keep moving parts in local renderer
            GameObject rawImageObj = new GameObject("MovingPartRawImage");
            rawImageObj.transform.SetParent(this.transform, false);
            movingPartRawImage = rawImageObj.AddComponent<RawImage>();
            movingPartRawImage.texture = movingPartTexture;

            // Only assign the material if NOT in menu
            if (!isInMenu && movingPartMaterial != null)
                movingPartRawImage.material = movingPartMaterial;

            // Stretch RawImage to fill parent
            RectTransform rt = movingPartRawImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            movingPartRawImage.transform.SetSiblingIndex(0);
        }


        // --- Border: create in BordersContainer ---
        if (!string.IsNullOrEmpty(def.borderSprite) && !isInMenu && gridManager != null)
        {
            CreateSeparatedBorder(def, cellDirection);
        }
        else if (!string.IsNullOrEmpty(def.borderSprite))
        {
            // For menu context, keep border in local renderer
            var border = CreateImageChild("Border", def.borderSprite);

            // Apply border color tinting if specified
            if (!string.IsNullOrEmpty(def.borderColor))
            {
                Color borderColor;
                if (ColorUtility.TryParseHtmlString(def.borderColor, out borderColor))
                {
                    border.color = borderColor;
                }
                else
                {
                    GameLogger.LogWarning(LoggingManager.LogCategory.Machine, $"Failed to parse border color '{def.borderColor}' for machine '{def.id}'", ComponentId);
                }
            }
            border.transform.SetSiblingIndex(1);
        }

        // --- Building: separated sprite in BuildingsContainer ---
        if (!string.IsNullOrEmpty(def.buildingSprite) && !isInMenu && gridManager != null)
        {
            CreateSeparatedBuildingSprite(baseMachine, cellDirection);
        }
        else if (!string.IsNullOrEmpty(def.buildingSprite) && isInMenu)
        {
            // For menu context, keep building sprites in local renderer
            var building = CreateImageChild("Building", def.buildingSprite);
            building.rectTransform.rotation = Quaternion.Euler(0, 0, def.buildingDirection);

            // Apply building sprite color tinting if specified
            if (!string.IsNullOrEmpty(def.buildingSpriteColour))
            {
                Color buildingColor;
                if (ColorUtility.TryParseHtmlString(def.buildingSpriteColour, out buildingColor))
                {
                    building.color = buildingColor;
                }
                else
                {
                    GameLogger.LogWarning(LoggingManager.LogCategory.Machine, $"Failed to parse building sprite color '{def.buildingSpriteColour}' for machine '{def.id}'", ComponentId);
                }
            }

            building.transform.SetSiblingIndex(3);

            string buildingIconSprite = def.buildingIconSprite;
            // Add icon sprite if specified
            if (!string.IsNullOrEmpty(buildingIconSprite))
            {
                var icon = CreateImageChild("BuildingIcon", buildingIconSprite);
                icon.rectTransform.rotation = Quaternion.Euler(0, 0, def.buildingDirection);

                // Apply icon size scaling - for menu context, use responsive anchors
                RectTransform iconRT = icon.rectTransform;

                // For responsive UI, use anchors to size the icon relative to its parent
                float iconSizeRatio = def.buildingIconSpriteSize;
                float margin = (1.0f - iconSizeRatio) * 0.5f; // Calculate margins to center the icon

                iconRT.anchorMin = new Vector2(margin, margin);
                iconRT.anchorMax = new Vector2(1.0f - margin, 1.0f - margin);
                iconRT.offsetMin = Vector2.zero;
                iconRT.offsetMax = Vector2.zero;
                iconRT.anchoredPosition = Vector2.zero;
                icon.transform.SetSiblingIndex(4);
            }
        }

        // --- Main sprite stays in local renderer if needed ---
        if (!string.IsNullOrEmpty(def.sprite))
        {
            var mainSprite = CreateImageChild("Main", def.sprite);
            mainSprite.transform.SetSiblingIndex(4);
        }

        // --- Rotation ---
        ConveyorBelt existingBelt = GetComponent<ConveyorBelt>();
        if (existingBelt == null)
        {
            float cellRotation = GetCellDirectionRotation(cellDirection);
            transform.rotation = Quaternion.Euler(0, 0, cellRotation);
        }
    }

    private void CreateSeparatedBuildingSprite(BaseMachine baseMachine, UICell.Direction cellDirection)
    {
        MachineDef def = baseMachine.MachineDef;
        RectTransform buildingsContainer = gridManager?.GetBuildingsContainer();
        if (buildingsContainer == null)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Machine, $"BuildingsContainer not found, falling back to local building sprite", ComponentId);
            var building = CreateImageChild("Building", def.buildingSprite);
            building.rectTransform.rotation = Quaternion.Euler(0, 0, def.buildingDirection);

            // Apply building sprite color tinting if specified
            if (!string.IsNullOrEmpty(def.buildingSpriteColour))
            {
                Color buildingColor;
                if (ColorUtility.TryParseHtmlString(def.buildingSpriteColour, out buildingColor))
                {
                    building.color = buildingColor;
                }
                else
                {
                    GameLogger.LogWarning(LoggingManager.LogCategory.Machine, $"Failed to parse building sprite color '{def.buildingSpriteColour}' for machine '{def.id}'", ComponentId);
                }
            }

            building.transform.SetSiblingIndex(3);

            // Add icon sprite if specified
            if (!string.IsNullOrEmpty(def.buildingIconSprite))
            {
                var icon = CreateImageChild("BuildingIcon", def.buildingIconSprite);
                icon.rectTransform.rotation = Quaternion.Euler(0, 0, def.buildingDirection);

                // Apply icon size scaling - for fallback context, use responsive anchors
                RectTransform iconRT = icon.rectTransform;

                // For responsive UI, use anchors to size the icon relative to its parent
                float iconSizeRatio = def.buildingIconSpriteSize;
                float margin = (1.0f - iconSizeRatio) * 0.5f; // Calculate margins to center the icon

                iconRT.anchorMin = new Vector2(margin, margin);
                iconRT.anchorMax = new Vector2(1.0f - margin, 1.0f - margin);
                iconRT.offsetMin = Vector2.zero;
                iconRT.offsetMax = Vector2.zero;
                iconRT.anchoredPosition = Vector2.zero;
                icon.transform.SetSiblingIndex(4);
            }
            return;
        }

        // Create building sprite in separate container
        buildingSprite = new GameObject($"Building_{cellX}_{cellY}");
        buildingSprite.transform.SetParent(buildingsContainer, false);

        Image buildingImage = buildingSprite.AddComponent<Image>();
        string spritePath = "Sprites/Machines/" + def.buildingSprite;
        buildingImage.sprite = Resources.Load<Sprite>(spritePath);

        // Make building sprite non-interactive to avoid blocking cell button clicks
        CanvasGroup buildingCanvasGroup = buildingSprite.AddComponent<CanvasGroup>();
        buildingCanvasGroup.blocksRaycasts = false;
        buildingCanvasGroup.interactable = false;

        if (buildingImage.sprite == null)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Machine, $"Building sprite not found! Tried to load: {spritePath}", ComponentId);
            buildingImage.color = Color.blue; // Fallback color
        }
        else
        {
            buildingImage.color = Color.white;
        }

        // Apply building sprite color tinting if specified
        if (!string.IsNullOrEmpty(def.buildingSpriteColour))
        {
            Color buildingColor;
            if (ColorUtility.TryParseHtmlString(def.buildingSpriteColour, out buildingColor))
            {
                buildingImage.color = buildingColor;
            }
            else
            {
                GameLogger.LogWarning(LoggingManager.LogCategory.Machine, $"Failed to parse building sprite color '{def.buildingSpriteColour}' for machine '{def.id}'", ComponentId);
            }
        }

        // Position and size the building sprite to match this cell
        RectTransform buildingRT = buildingSprite.GetComponent<RectTransform>();
        Vector3 cellPosition = gridManager.GetCellWorldPosition(cellX, cellY);
        Vector2 cellSize = gridManager.GetCellSize();

        buildingRT.position = cellPosition;
        buildingRT.sizeDelta = cellSize;

        // Apply rotations: building direction + cell direction
        float totalRotation = def.buildingDirection + GetCellDirectionRotation(cellDirection);
        buildingRT.rotation = Quaternion.Euler(0, 0, totalRotation);

        // Create icon sprite if specified (as a child of the building sprite)
        string buildingIconSprite = baseMachine.GetBuildingIconSprite();

        if (!string.IsNullOrEmpty(buildingIconSprite))
        {
            GameObject iconSprite = new GameObject($"BuildingIcon_{cellX}_{cellY}");
            iconSprite.transform.SetParent(buildingSprite.transform, false);

            Image iconImage = iconSprite.AddComponent<Image>();

            // Try loading icon sprite from multiple possible locations
            Sprite iconSpriteAsset = null;
            string[] possiblePaths = {
                "Sprites/Machines/" + buildingIconSprite,
                "Sprites/Items/" + buildingIconSprite,
                "Sprites/" + buildingIconSprite
            };

            foreach (string iconPath in possiblePaths)
            {
                iconSpriteAsset = Resources.Load<Sprite>(iconPath);
                if (iconSpriteAsset != null)
                {
                    break;
                }
            }

            iconImage.sprite = iconSpriteAsset;

            // Make icon non-interactive
            CanvasGroup iconCanvasGroup = iconSprite.AddComponent<CanvasGroup>();
            iconCanvasGroup.blocksRaycasts = false;
            iconCanvasGroup.interactable = false;

            if (iconImage.sprite == null)
            {
                GameLogger.LogWarning(LoggingManager.LogCategory.Machine, "Building icon sprite " + def.buildingIconSprite + " not found! Tried paths: " + string.Join(", ", possiblePaths), ComponentId);
                iconImage.color = Color.yellow; // Fallback color
            }
            else
            {
                iconImage.color = Color.white;
            }

            // Position and size the icon sprite
            RectTransform iconRT = iconSprite.GetComponent<RectTransform>();

            // For responsive UI, use anchors to size the icon relative to its parent
            float iconSizeRatio = def.buildingIconSpriteSize;
            float margin = (1.0f - iconSizeRatio) * 0.5f; // Calculate margins to center the icon

            iconRT.anchorMin = new Vector2(margin, margin);
            iconRT.anchorMax = new Vector2(1.0f - margin, 1.0f - margin);
            iconRT.offsetMin = Vector2.zero;
            iconRT.offsetMax = Vector2.zero;
            iconRT.anchoredPosition = Vector2.zero;

            // Create configuration sprites for non-menu machines after the icon sprite
            CreateConfigurationSprites(baseMachine, buildingSprite);

            // Create progress bar if machine supports it and not in menu
            CreateProgressBar();
        }
    }

    private void CreateSeparatedBorder(MachineDef def, UICell.Direction cellDirection)
    {
        RectTransform bordersContainer = gridManager?.GetBordersContainer();
        if (bordersContainer == null)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Machine, $"BordersContainer not found, falling back to local border sprite", ComponentId);
            var border = CreateImageChild("Border", def.borderSprite);

            // Apply border color tinting if specified
            if (!string.IsNullOrEmpty(def.borderColor))
            {
                Color borderColor;
                if (ColorUtility.TryParseHtmlString(def.borderColor, out borderColor))
                {
                    border.color = borderColor;
                }
                else
                {
                    GameLogger.LogWarning(LoggingManager.LogCategory.Machine, $"Failed to parse border color '{def.borderColor}' for machine '{def.id}'", ComponentId);
                }
            }
            border.transform.SetSiblingIndex(1);
            return;
        }

        // Create border sprite in separate container
        borderSprite = new GameObject($"Border_{cellX}_{cellY}");
        borderSprite.transform.SetParent(bordersContainer, false);

        Image borderImage = borderSprite.AddComponent<Image>();
        string spritePath = "Sprites/Machines/" + def.borderSprite;
        borderImage.sprite = Resources.Load<Sprite>(spritePath);

        // Make border non-interactive to avoid blocking cell button clicks
        CanvasGroup borderCanvasGroup = borderSprite.AddComponent<CanvasGroup>();
        borderCanvasGroup.blocksRaycasts = false;
        borderCanvasGroup.interactable = false;

        if (borderImage.sprite == null)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Machine, $"Border sprite not found! Tried to load: {spritePath}", ComponentId);
            borderImage.color = Color.red; // Fallback color
        }
        else
        {
            borderImage.color = Color.white;
        }

        // Apply border color tinting if specified
        if (!string.IsNullOrEmpty(def.borderColor))
        {
            Color borderColor;
            if (ColorUtility.TryParseHtmlString(def.borderColor, out borderColor))
            {
                borderImage.color = borderColor;
            }
            else
            {
                GameLogger.LogWarning(LoggingManager.LogCategory.Machine, $"Failed to parse border color '{def.borderColor}' for machine '{def.id}'", ComponentId);
            }
        }

        // Position and size the border sprite to match this cell
        RectTransform borderRT = borderSprite.GetComponent<RectTransform>();
        Vector3 cellPosition = gridManager.GetCellWorldPosition(cellX, cellY);
        Vector2 cellSize = gridManager.GetCellSize();

        // Use same positioning approach as building sprites (which work)
        borderRT.position = cellPosition;
        borderRT.sizeDelta = cellSize;

        // Apply cell direction rotation
        float cellRotation = GetCellDirectionRotation(cellDirection);
        borderRT.rotation = Quaternion.Euler(0, 0, cellRotation);
    }

    private void CreateSeparatedMovingPart(MachineDef def, UICell.Direction cellDirection)
    {
        RectTransform bordersContainer = gridManager?.GetBordersContainer();
        if (bordersContainer == null)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Machine, "BordersContainer not found, falling back to local moving part", ComponentId);
            GameObject rawImageObj = new GameObject("MovingPartRawImage");
            rawImageObj.transform.SetParent(this.transform, false);
            movingPartRawImage = rawImageObj.AddComponent<RawImage>();
            movingPartRawImage.texture = movingPartTexture;

            if (!isInMenu && movingPartMaterial != null)
                movingPartRawImage.material = movingPartMaterial;

            RectTransform rt = movingPartRawImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            movingPartRawImage.transform.SetSiblingIndex(0);
            return;
        }

        // Create moving part RawImage in separate container
        movingPartSprite = new GameObject($"MovingPart_{cellX}_{cellY}");
        movingPartSprite.transform.SetParent(bordersContainer, false);

        movingPartRawImage = movingPartSprite.AddComponent<RawImage>();
        movingPartRawImage.texture = movingPartTexture;

        // Make moving part non-interactive to avoid blocking cell button clicks
        CanvasGroup movingPartCanvasGroup = movingPartSprite.AddComponent<CanvasGroup>();
        movingPartCanvasGroup.blocksRaycasts = false;
        movingPartCanvasGroup.interactable = false;

        // Assign material for conveyor animation
        if (movingPartMaterial != null)
        {
            movingPartRawImage.material = movingPartMaterial;
        }
        else
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Machine, "MovingPart material is NULL", ComponentId);
        }

        // Position and size the moving part to match this cell
        RectTransform movingPartRT = movingPartSprite.GetComponent<RectTransform>();
        Vector3 cellPosition = gridManager.GetCellWorldPosition(cellX, cellY);
        Vector2 cellSize = gridManager.GetCellSize();

        // Use same positioning approach as building sprites (which work)
        movingPartRT.position = cellPosition;
        movingPartRT.sizeDelta = cellSize;

        // Apply cell direction rotation
        float cellRotation = GetCellDirectionRotation(cellDirection);
        movingPartRT.rotation = Quaternion.Euler(0, 0, cellRotation);
    }

    private Image CreateImageChild(string name, string spriteResource)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(this.transform, false);
        Image img = go.AddComponent<Image>();

        Sprite spriteAsset = null;

        // For BuildingIcon, try multiple paths
        if (name == "BuildingIcon")
        {
            string[] possiblePaths = {
                "Sprites/Machines/" + spriteResource,
                "Sprites/Items/" + spriteResource,
                "Sprites/" + spriteResource
            };

            foreach (string spritePath in possiblePaths)
            {
                spriteAsset = Resources.Load<Sprite>(spritePath);
                if (spriteAsset != null)
                {
                    break;
                }
            }

            if (spriteAsset == null)
            {
                GameLogger.LogWarning(LoggingManager.LogCategory.Machine, $"Building icon sprite '{spriteResource}' not found! Tried paths: {string.Join(", ", possiblePaths)}", ComponentId);
            }
        }
        else
        {
            // For other sprites, use the default Machines path
            string spritePath = "Sprites/Machines/" + spriteResource;
            spriteAsset = Resources.Load<Sprite>(spritePath);

            if (spriteAsset == null)
            {
                GameLogger.LogWarning(LoggingManager.LogCategory.Machine, $"Sprite not found! Tried to load: {spritePath} for '{name}'", ComponentId);
            }
        }

        img.sprite = spriteAsset;

        if (img.sprite == null)
        {
            img.color = name == "Border" ? Color.red :
                       name == "Building" ? Color.blue :
                       name == "MovingPart" ? Color.green : Color.yellow;
        }
        else
        {
            img.color = Color.white;
        }

        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;

        return img;
    }

    void OnDestroy()
    {
        if (buildingSprite != null)
        {
            Destroy(buildingSprite);
            buildingSprite = null;
        }

        if (borderSprite != null)
        {
            Destroy(borderSprite);
            borderSprite = null;
        }

        if (movingPartSprite != null)
        {
            Destroy(movingPartSprite);
            movingPartSprite = null;
        }

        // Clean up progress bar
        if (progressBarContainer != null)
        {
            Destroy(progressBarContainer);
            progressBarContainer = null;
        }
    }

    private float GetCellDirectionRotation(UICell.Direction direction)
    {
        switch (direction)
        {
            case UICell.Direction.Up: return 0f;
            case UICell.Direction.Right: return -90f;
            case UICell.Direction.Down: return -180f;
            case UICell.Direction.Left: return -270f;
            default: return 0f;
        }
    }

    // Progress Bar System
    // ===================

    /// <summary>
    /// Creates a progress bar UI underneath the building icon sprite.
    /// Should only be called for non-menu machines that support progress tracking.
    /// </summary>
    private void CreateProgressBar()
    {
        // Only create progress bar if not in menu and machine supports progress
        if (isInMenu || associatedMachine == null || !associatedMachine.ShouldShowProgressBar(associatedMachine.GetProgress()))
        {
            return;
        }

        // Get the buildings container from grid manager
        RectTransform buildingsContainer = gridManager?.GetBuildingsContainer();
        if (buildingsContainer == null)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Machine, "Cannot create progress bar - BuildingsContainer not found", ComponentId);
            return;
        }

        // Create progress bar container as a child of the building sprite
        progressBarContainer = new GameObject($"ProgressBar_{cellX}_{cellY}");
        progressBarContainer.transform.SetParent(buildingSprite.transform, false);

        // Add RectTransform for UI positioning
        RectTransform progressRT = progressBarContainer.AddComponent<RectTransform>();

        // Position progress bar below the icon sprite
        // Use anchors to position at bottom of parent with some margin
        progressRT.anchorMin = new Vector2(0.1f, 0.05f); // Left and bottom margins
        progressRT.anchorMax = new Vector2(0.9f, 0.15f);  // Right margin and height
        progressRT.offsetMin = Vector2.zero;
        progressRT.offsetMax = Vector2.zero;
        progressRT.anchoredPosition = Vector2.zero;

        // Create background image
        GameObject backgroundObj = new GameObject("Background");
        backgroundObj.transform.SetParent(progressBarContainer.transform, false);

        progressBarBackground = backgroundObj.AddComponent<UnityEngine.UI.Image>();
        progressBarBackground.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Dark gray background

        RectTransform backgroundRT = backgroundObj.GetComponent<RectTransform>();
        backgroundRT.anchorMin = Vector2.zero;
        backgroundRT.anchorMax = Vector2.one;
        backgroundRT.offsetMin = Vector2.zero;
        backgroundRT.offsetMax = Vector2.zero;

        // Create fill image
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(progressBarContainer.transform, false);

        progressBarFill = fillObj.AddComponent<UnityEngine.UI.Image>();
        progressBarFill.color = new Color(0.2f, 0.8f, 0.2f, 0.8f); // Green fill
        progressBarFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        progressBarFill.fillOrigin = 0;
        progressBarFill.sprite = Resources.Load<Sprite>("Sprites/Machines/UICellSprite");
        progressBarFill.type = UnityEngine.UI.Image.Type.Filled;

        RectTransform fillRT = fillObj.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        // Make progress bar non-interactive
        CanvasGroup progressCanvasGroup = progressBarContainer.AddComponent<CanvasGroup>();
        progressCanvasGroup.blocksRaycasts = false;
        progressCanvasGroup.interactable = false;

        

        GameLogger.LogMachine($"Created progress bar for machine at ({cellX}, {cellY})", ComponentId);
    }

    /// <summary>
    /// Updates the progress bar with current machine progress.
    /// Called every second to avoid performance issues.
    /// </summary>
    private void UpdateProgressBar()
    {
        // Only update if we have a progress bar and associated machine
        if (progressBarContainer == null || progressBarFill == null || associatedMachine == null)
        {
            return;
        }
        
        float progress = associatedMachine.GetProgress();

        // Check if machine should still show progress bar
        bool shouldShow = associatedMachine.ShouldShowProgressBar(progress);
        
        if (!shouldShow)
        {
            // Hide progress bar if no longer needed
            progressBarContainer.SetActive(false);
            return;
        }

        // Show progress bar if it was hidden
        if (!progressBarContainer.activeInHierarchy)
        {
            progressBarContainer.SetActive(true);
        }

        // Get current progress and update fill amount
        if (progress >= 0f)
        {
            GameLogger.LogMachine($"Updating progress bar: {progress} for machine at ({cellX}, {cellY})", ComponentId);
            // Clamp to 1.0 to handle timing delays that might cause progress to exceed 100%
            float normalizedProgress = Mathf.Clamp01(progress / 0.8f); // Normalize to 0-1 range based on 0-80% logic
            progressBarFill.fillAmount = normalizedProgress;
            GameLogger.LogMachine($"fillAmount is now actually {progressBarFill.fillAmount}", ComponentId);
        }
    }

    /// <summary>
    /// MonoBehaviour Update method to handle progress bar updates every second
    /// </summary>
    void Update()
    {
        // Only process progress bar updates if not in menu
        if (isInMenu || associatedMachine == null)
        {
            return;
        }

        // Update progress bar every 1 second to avoid performance issues
        if (Time.time - lastProgressUpdate >= 1.0f)
        {
            // Create progress bar if needed and machine supports it
            if (progressBarContainer == null)
            {
                CreateProgressBar();
            }

            // Update existing progress bar
            UpdateProgressBar();

            lastProgressUpdate = Time.time;
        }
    }

    /// <summary>
    /// Creates left and right configuration sprites for machines that support configuration
    /// </summary>
    private void CreateConfigurationSprites(BaseMachine baseMachine, GameObject parentSprite)
    {
        if (baseMachine == null || parentSprite == null)
            return;

        // Only create configuration sprites for non-menu machines
        if (isInMenu)
            return;

        string leftSprite = baseMachine.GetLeftConfigurationSprite();
        string rightSprite = baseMachine.GetRightConfigurationSprite();

        // Create left configuration sprite if available
        if (!string.IsNullOrEmpty(leftSprite))
        {
            CreateConfigurationSprite(leftSprite, "LeftConfig", parentSprite, -0.35f, 0.0f); // Left position
        }

        // Create right configuration sprite if available
        if (!string.IsNullOrEmpty(rightSprite))
        {
            CreateConfigurationSprite(rightSprite, "RightConfig", parentSprite, 0.35f, 0.0f); // Right position
        }
    }

    /// <summary>
    /// Updates/refreshes the configuration sprites for machines when configuration changes
    /// </summary>
    public void RefreshConfigurationSprites()
    {
        if (isInMenu || associatedMachine == null)
            return;

        // Find the building sprite container to update configuration sprites
        GameObject buildingContainer = null;
        if (buildingSprite != null)
        {
            buildingContainer = buildingSprite;
        }
        else if (gridManager != null)
        {
            // For local building sprites, find in this renderer's children
            Transform buildingTransform = transform.Find("Building");
            if (buildingTransform != null)
                buildingContainer = buildingTransform.gameObject;
        }

        if (buildingContainer == null)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Machine, 
                "Cannot refresh configuration sprites - no building container found", ComponentId);
            return;
        }

        // Remove existing configuration sprites
        Transform leftConfig = buildingContainer.transform.Find($"LeftConfig_{cellX}_{cellY}");
        if (leftConfig != null)
            DestroyImmediate(leftConfig.gameObject);

        Transform rightConfig = buildingContainer.transform.Find($"RightConfig_{cellX}_{cellY}");
        if (rightConfig != null)
            DestroyImmediate(rightConfig.gameObject);

        // Recreate configuration sprites with updated data
        CreateConfigurationSprites(associatedMachine, buildingContainer);
        
        GameLogger.LogMachine($"Refreshed configuration sprites for machine at ({cellX}, {cellY})", ComponentId);
    }

    /// <summary>
    /// Creates a single configuration sprite at the specified position
    /// </summary>
    private void CreateConfigurationSprite(string spriteName, string gameObjectName, GameObject parentSprite, float offsetX, float offsetY)
    {
        GameObject configSprite = new GameObject($"{gameObjectName}_{cellX}_{cellY}");
        configSprite.transform.SetParent(parentSprite.transform, false);

        Image configImage = configSprite.AddComponent<Image>();

        // Try loading config sprite from multiple possible locations  
        Sprite configSpriteAsset = null;
        string[] possiblePaths = {
            "Sprites/Items/" + spriteName,
            "Sprites/Machines/" + spriteName,
            "Sprites/" + spriteName
        };

        foreach (string configPath in possiblePaths)
        {
            configSpriteAsset = Resources.Load<Sprite>(configPath);
            if (configSpriteAsset != null)
            {
                break;
            }
        }

        configImage.sprite = configSpriteAsset;

        // Make config sprite non-interactive
        CanvasGroup configCanvasGroup = configSprite.AddComponent<CanvasGroup>();
        configCanvasGroup.blocksRaycasts = false;
        configCanvasGroup.interactable = false;

        if (configImage.sprite == null)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Machine, 
                $"Configuration sprite '{spriteName}' not found! Tried paths: {string.Join(", ", possiblePaths)}", ComponentId);
            configImage.color = Color.cyan; // Fallback color for missing config sprites
        }
        else
        {
            configImage.color = Color.white;
        }

        // Position and size the configuration sprite
        RectTransform configRT = configSprite.GetComponent<RectTransform>();

        // Use 0.3 relative size as requested
        float configSizeRatio = 0.3f;
        float margin = (1.0f - configSizeRatio) * 0.5f;

        configRT.anchorMin = new Vector2(margin, margin);
        configRT.anchorMax = new Vector2(1.0f - margin, 1.0f - margin);
        configRT.offsetMin = Vector2.zero;
        configRT.offsetMax = Vector2.zero;

        // Apply offset position (relative to parent)
        configRT.anchoredPosition = new Vector2(offsetX * parentSprite.GetComponent<RectTransform>().sizeDelta.x, 
                                                offsetY * parentSprite.GetComponent<RectTransform>().sizeDelta.y);
    }
}