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

    // For separated building rendering
    private UIGridManager gridManager;
    private int cellX, cellY;
    private GameObject buildingSprite; // Track building sprite separately

    // Moving part visual references (created internally)
    [NonSerialized] private RawImage movingPartRawImage;
    [NonSerialized] private Texture movingPartTexture;
    [NonSerialized] private Material movingPartMaterial;

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

        // --- Moving Part: create RawImage, assign Texture and Material ---
        if (def.isMoving && movingPartTexture != null)
        {
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

        // --- Border ---
        if (!string.IsNullOrEmpty(def.borderSprite))
        {
            var border = CreateImageChild("Border", def.borderSprite);

            // Apply border color tinting if specified
            if (!string.IsNullOrEmpty(def.borderColor))
            {
                Color borderColor;
                if (ColorUtility.TryParseHtmlString(def.borderColor, out borderColor))
                {
                    border.color = borderColor;
                    Debug.Log($"Applied border color '{def.borderColor}' to border image");
                }
                else
                {
                    Debug.LogWarning($"Failed to parse border color '{def.borderColor}' for machine '{def.id}'");
                }
            }
            border.transform.SetSiblingIndex(1);
        }

        CreateItemSpawnPoint();

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
            building.transform.SetSiblingIndex(3);
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
            Debug.Log($"MachineRenderer applying rotation {cellRotation} for cell direction {cellDirection}");
        }
        else
        {
            Debug.Log($"ConveyorBelt component found on same GameObject - skipping MachineRenderer rotation for {def.id}");
        }
    }

    private void CreateSeparatedBuildingSprite(MachineDef def, UICell.Direction cellDirection)
    {
        RectTransform buildingsContainer = gridManager?.GetBuildingsContainer();
        if (buildingsContainer == null)
        {
            Debug.LogWarning("BuildingsContainer not found, falling back to local building sprite");
            var building = CreateImageChild("Building", def.buildingSprite);
            building.rectTransform.rotation = Quaternion.Euler(0, 0, def.buildingDirection);
            building.transform.SetSiblingIndex(3);
            return;
        }

        // Create building sprite in separate container
        buildingSprite = new GameObject($"Building_{cellX}_{cellY}");
        buildingSprite.transform.SetParent(buildingsContainer, false);

        Image buildingImage = buildingSprite.AddComponent<Image>();
        string spritePath = "Sprites/Machines/" + def.buildingSprite;
        buildingImage.sprite = Resources.Load<Sprite>(spritePath);

        if (buildingImage.sprite == null)
        {
            Debug.LogWarning($"Building sprite not found! Tried to load: {spritePath}");
            buildingImage.color = Color.blue; // Fallback color
        }
        else
        {
            Debug.Log($"Successfully loaded building sprite: {spritePath}");
            buildingImage.color = Color.white;
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

        Debug.Log($"Created separated building sprite for cell ({cellX}, {cellY}) in BuildingsContainer");
    }

    private void CreateItemSpawnPoint()
    {
        // Create spawn point GameObject as a child of this MachineRenderer
        GameObject spawnPointObj = new GameObject("ItemSpawnPoint");
        spawnPointObj.transform.SetParent(this.transform, false);

        RectTransform spawnPointRT = spawnPointObj.AddComponent<RectTransform>();

        // Position it in the center, between border and building layers
        spawnPointRT.anchorMin = new Vector2(0.5f, 0.5f);
        spawnPointRT.anchorMax = new Vector2(0.5f, 0.5f);
        spawnPointRT.anchoredPosition = Vector2.zero;
        spawnPointRT.sizeDelta = Vector2.zero;

        // Set the sibling index to be between border (1) and building (3)
        spawnPointObj.transform.SetSiblingIndex(2);

        Debug.Log("Created item spawn point for machine");
    }

    private Image CreateImageChild(string name, string spriteResource)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(this.transform, false);
        Image img = go.AddComponent<Image>();

        string spritePath = "Sprites/Machines/" + spriteResource;
        img.sprite = Resources.Load<Sprite>(spritePath);

        if (img.sprite == null)
        {
            Debug.LogWarning($"Sprite not found! Tried to load: {spritePath} for '{name}'");
            img.color = name == "Border" ? Color.red :
                       name == "Building" ? Color.blue :
                       name == "MovingPart" ? Color.green : Color.yellow;
        }
        else
        {
            Debug.Log($"Successfully loaded sprite: {spritePath} for '{name}'");
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