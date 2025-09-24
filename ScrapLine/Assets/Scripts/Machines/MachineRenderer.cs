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