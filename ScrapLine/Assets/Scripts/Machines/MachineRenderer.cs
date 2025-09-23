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
    private GameObject progressBarSprite; // Track progress bar sprite separately

    // Moving part visual references (created internally)
    [NonSerialized] private RawImage movingPartRawImage;
    [NonSerialized] private Texture movingPartTexture;
    [NonSerialized] private Material movingPartMaterial;
    
    // Progress bar references
    [NonSerialized] private Image progressBarFill;
    [NonSerialized] private BaseMachine cachedMachine;
    [NonSerialized] private string lastSpriteName = ""; // Cache last sprite name to avoid constant updates
    [NonSerialized] private float lastUpdateTime = 0f; // Track when we last updated to reduce frequency

    /// <summary>
    /// Setup the renderer. Pass in Texture and Material for moving part if needed.
    /// </summary>
    /// <param name="def"></param>
    /// <param name="cellDirection"></param>
    /// <param name="gridManager"></param>
    /// <param name="cellX"></param>
    /// <param name="cellY"></param>
    /// <param name="movingPartTexture">Optional: assign this if using moving part</param>
    /// <param name="movingPartMaterial">Optional: assign this if using moving part</param>
    public void Setup(
        MachineDef def,
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
            CreateSeparatedBuildingSprite(def, cellDirection);
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
            
            // Add icon sprite if specified
            if (!string.IsNullOrEmpty(def.buildingIconSprite))
            {
                var icon = CreateImageChild("BuildingIcon", def.buildingIconSprite);
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

    private void CreateSeparatedBuildingSprite(MachineDef def, UICell.Direction cellDirection)
    {
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
        if (!string.IsNullOrEmpty(def.buildingIconSprite))
        {
            GameObject iconSprite = new GameObject($"BuildingIcon_{cellX}_{cellY}");
            iconSprite.transform.SetParent(buildingSprite.transform, false);

            Image iconImage = iconSprite.AddComponent<Image>();
            
            // Try loading icon sprite from multiple possible locations
            Sprite iconSpriteAsset = null;
            string[] possiblePaths = {
                "Sprites/Machines/" + def.buildingIconSprite,
                "Sprites/Items/" + def.buildingIconSprite,
                "Sprites/" + def.buildingIconSprite
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
    
    /// <summary>
    /// Update the building icon sprite dynamically (e.g., for spawner junkyard levels)
    /// </summary>
    /// <param name="newIconSprite">New icon sprite name</param>
    public void UpdateBuildingIconSprite(string newIconSprite)
    {
        if (buildingSprite == null || string.IsNullOrEmpty(newIconSprite))
            return;
            
        // Find the icon child of the building sprite
        Transform iconTransform = buildingSprite.transform.Find($"BuildingIcon_{cellX}_{cellY}");
        if (iconTransform != null)
        {
            Image iconImage = iconTransform.GetComponent<Image>();
            if (iconImage != null)
            {
                // Try loading new icon sprite from multiple possible locations
                Sprite iconSpriteAsset = null;
                string[] possiblePaths = {
                    "Sprites/Machines/" + newIconSprite,
                    "Sprites/Items/" + newIconSprite,
                    "Sprites/" + newIconSprite
                };
                
                foreach (string iconPath in possiblePaths)
                {
                    iconSpriteAsset = Resources.Load<Sprite>(iconPath);
                    if (iconSpriteAsset != null)
                    {
                        break;
                    }
                }
                
                if (iconSpriteAsset != null)
                {
                    iconImage.sprite = iconSpriteAsset;
                    iconImage.color = Color.white;
                    GameLogger.LogMachine($"Updated building icon to: {newIconSprite}", ComponentId);
                }
                else
                {
                    GameLogger.LogWarning(LoggingManager.LogCategory.Machine, $"Failed to load new icon sprite '{newIconSprite}'. Tried paths: {string.Join(", ", possiblePaths)}", ComponentId);
                }
            }
        }
    }
    
    /// <summary>
    /// Create a progress bar below the border sprite or building sprite
    /// </summary>
    public void CreateProgressBar()
    {
        if (isInMenu || gridManager == null)
            return;
        
        // Use border sprite if available, otherwise use building sprite
        GameObject parentSprite = borderSprite ?? buildingSprite;
        if (parentSprite == null)
            return;
            
        // Create progress bar container
        progressBarSprite = new GameObject($"ProgressBar_{cellX}_{cellY}");
        progressBarSprite.transform.SetParent(parentSprite.transform, false);
        
        // Position at the bottom of the parent sprite
        RectTransform progressRT = progressBarSprite.AddComponent<RectTransform>();
        progressRT.anchorMin = new Vector2(0.1f, -0.15f);
        progressRT.anchorMax = new Vector2(0.9f, -0.05f);
        progressRT.offsetMin = Vector2.zero;
        progressRT.offsetMax = Vector2.zero;
        progressRT.anchoredPosition = Vector2.zero;
        
        // Create background
        Image progressBackground = progressBarSprite.AddComponent<Image>();
        progressBackground.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        // Create fill (as child)
        GameObject fillObject = new GameObject("ProgressFill");
        fillObject.transform.SetParent(progressBarSprite.transform, false);
        
        progressBarFill = fillObject.AddComponent<Image>();
        progressBarFill.color = new Color(0.0f, 0.7f, 0.0f, 0.9f); // Green fill
        progressBarFill.type = Image.Type.Filled;
        progressBarFill.fillMethod = Image.FillMethod.Horizontal;
        
        RectTransform fillRT = fillObject.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        fillRT.anchoredPosition = Vector2.zero;
        
        // Make non-interactive
        CanvasGroup progressCanvasGroup = progressBarSprite.AddComponent<CanvasGroup>();
        progressCanvasGroup.blocksRaycasts = false;
        progressCanvasGroup.interactable = false;
        
        GameLogger.LogMachine($"Created progress bar for machine at ({cellX}, {cellY}) - parent: {parentSprite.name}", ComponentId);
    }
    
    /// <summary>
    /// Update the progress bar fill amount
    /// </summary>
    /// <param name="progress">Progress value between 0 and 1</param>
    public void UpdateProgressBar(float progress)
    {
        if (progressBarFill != null)
        {
            float clampedProgress = Mathf.Clamp01(progress);
            progressBarFill.fillAmount = clampedProgress;
        }
    }
    
    /// <summary>
    /// Force an immediate sprite update on the next update cycle (called when items are consumed)
    /// </summary>
    public void ForceUpdateSprite()
    {
        lastUpdateTime = 0f; // Reset timer to force immediate update
        lastSpriteName = ""; // Clear cached sprite name to force update
    }
    
    /// <summary>
    /// Update dynamic elements based on machine state (called from Update loop)
    /// </summary>
    public void UpdateDynamicElements()
    {
        if (isInMenu) return;
        
        // Only update at most once per second to reduce performance impact
        if (Time.time - lastUpdateTime < 1.0f) return;
        lastUpdateTime = Time.time;
        
        GameLogger.LogMachine($"UpdateDynamicElements called for cell ({cellX}, {cellY})", ComponentId);
        
        // Update machine reference if needed
        if (cachedMachine == null && gridManager != null)
        {
            GameLogger.LogMachine($"Attempting to get cell data for ({cellX}, {cellY})", ComponentId);
            var cellData = gridManager.GetCellData(cellX, cellY);
            if (cellData != null)
            {
                cachedMachine = cellData.machine;
                if (cachedMachine != null)
                {
                    GameLogger.LogMachine($"Machine found: {cachedMachine.GetType().Name} at ({cellX}, {cellY})", ComponentId);
                }
                else
                {
                    GameLogger.LogMachine($"Cell data found but machine is null at ({cellX}, {cellY})", ComponentId);
                }
            }
            else
            {
                GameLogger.LogMachine($"Cell data is null for ({cellX}, {cellY})", ComponentId);
            }
        }
        else if (cachedMachine == null && gridManager == null)
        {
            GameLogger.LogMachine($"GridManager is null for cell ({cellX}, {cellY})", ComponentId);
        }
        
        if (cachedMachine == null) 
        {
            GameLogger.LogMachine($"No machine available for cell ({cellX}, {cellY})", ComponentId);
            return;
        }
        
        // Update progress bar
        float progress = cachedMachine.GetProgress();
        
        if (progress >= 0)
        {
            // Create progress bar if it doesn't exist
            if (progressBarSprite == null)
            {
                CreateProgressBar();
            }
            
            // Update progress bar if it exists
            if (progressBarFill != null)
            {
                progressBarSprite.SetActive(true);
                UpdateProgressBar(progress);
            }
        }
        else if (progressBarSprite != null)
        {
            // Hide progress bar if no progress to show
            progressBarSprite.SetActive(false);
        }
        
        // Update dynamic sprites for spawner machines (only once per second)
        if (cachedMachine is SpawnerMachine spawner)
        {
            string newSprite = spawner.GetJunkyardSpriteName();
            // Only update if sprite has changed (to avoid unnecessary resource loading)
            if (newSprite != lastSpriteName)
            {
                UpdateBuildingIconSprite(newSprite);
                lastSpriteName = newSprite;
            }
        }
    }
    
    /// <summary>
    /// Update method called by Unity every frame
    /// </summary>
    void Update()
    {
        if (!isInMenu)
        {
            // Log once every 60 frames to avoid spam but confirm Update is running
            if (Time.frameCount % 60 == 0)
            {
                GameLogger.LogMachine($"Update() running for MachineRenderer at ({cellX}, {cellY})", ComponentId);
            }
            
            // Update progress bar every frame for smooth animation
            UpdateProgressBarFrequently();
            
            // Update expensive operations (sprites, machine detection) less frequently
            UpdateDynamicElements();
        }
    }
    
    /// <summary>
    /// Update progress bar every frame for smooth progress animation
    /// </summary>
    private void UpdateProgressBarFrequently()
    {
        if (cachedMachine != null && progressBarFill != null)
        {
            float progress = cachedMachine.GetProgress();
            if (progress >= 0)
            {
                progressBarSprite.SetActive(true);
                UpdateProgressBar(progress);
            }
        }
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
        
        if (progressBarSprite != null)
        {
            Destroy(progressBarSprite);
            progressBarSprite = null;
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
}