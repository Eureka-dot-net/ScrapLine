using System;
using UnityEngine;
using UnityEngine.UI;

public class MachineRenderer : MonoBehaviour
{
    [Header("Context")]
    public bool isInMenu = false; // Set to true when used in UI panels to disable materials

    // Separate rendering: building sprites in dedicated container
    private UIGridManager gridManager;
    private int cellX, cellY;
    private GameObject buildingSprite; // Track building sprite separately

    public void Setup(MachineDef def, UICell.Direction cellDirection = UICell.Direction.Up, UIGridManager gridManager = null, int cellX = 0, int cellY = 0)
    {
        this.gridManager = gridManager;
        this.cellX = cellX;
        this.cellY = cellY;
        
        foreach (Transform child in transform) Destroy(child.gameObject);
        
        // Clean up any existing building sprite in separate container
        if (buildingSprite != null)
        {
            Destroy(buildingSprite);
            buildingSprite = null;
        }

        Debug.Log("Creating machine with separated rendering: " +
                  $"Main='{def.sprite}', Border='{def.borderSprite}', " +
                  $"MovingPart='{def.movingPartSprite}', Building='{def.buildingSprite}'" +
                  $" BorderColor='{def.borderColor}', IsInMenu={isInMenu}");

        // Solution 1: Create border and moving parts in this renderer (will be in BordersContainer layer)
        if (!string.IsNullOrEmpty(def.movingPartSprite))
        {
            var movingPart = CreateImageChild("MovingPart", def.movingPartSprite);
            // Only apply materials if NOT in menu to prevent unwanted animations
            if (!isInMenu && !string.IsNullOrEmpty(def.movingPartMaterial))
            {
                string movingPartMatPath = "Materials/" + def.movingPartMaterial;
                Debug.Log($"Attempting to load material from path: '{movingPartMatPath}' for machine '{def.id}'");
                var mat = Resources.Load<Material>(movingPartMatPath);
                if (mat != null)
                {
                    movingPart.material = mat;
                    Debug.Log($"Successfully applied material '{movingPartMatPath}' to MovingPart for machine '{def.id}'");
                    
                    // Add UITextureScroller component for UI Image material animation
                    var uiTextureScroller = movingPart.GetComponent<UITextureScroller>();
                    if (uiTextureScroller != null)
                    {
                        Debug.Log($"UITextureScroller component found on MovingPart for machine '{def.id}'");
                    }
                    else
                    {
                        Debug.Log($"No UITextureScroller component found on MovingPart for machine '{def.id}' - adding one");
                        movingPart.gameObject.AddComponent<UITextureScroller>();
                    }
                }
                else
                {
                    Debug.LogWarning($"Material '{movingPartMatPath}' could not be found in Resources folder for machine '{def.id}'");
                    Debug.Log($"Trying alternative path: 'Materials/Conveyors/{def.movingPartMaterial}'");
                    string altPath = "Materials/Conveyors/" + def.movingPartMaterial;
                    var altMat = Resources.Load<Material>(altPath);
                    if (altMat != null)
                    {
                        movingPart.material = altMat;
                        Debug.Log($"Successfully applied alternative material '{altPath}' to MovingPart for machine '{def.id}'");
                        movingPart.gameObject.AddComponent<UITextureScroller>();
                    }
                    else
                    {
                        Debug.LogError($"Both material paths failed for machine '{def.id}': '{movingPartMatPath}' and '{altPath}'");
                    }
                }
            }
            else if (isInMenu)
            {
                Debug.Log($"Skipping material application for '{def.id}' - in menu context");
            }
            movingPart.transform.SetSiblingIndex(0);
        }

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

        // Solution 1: Create building sprite in separate BuildingsContainer (unless in menu)
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

        // Main sprite stays in local renderer if needed
        if (!string.IsNullOrEmpty(def.sprite))
        {
            var mainSprite = CreateImageChild("Main", def.sprite);
            mainSprite.transform.SetSiblingIndex(4);
        }

        // Apply cell direction rotation to entire renderer (only if ConveyorBelt isn't handling it)
        ConveyorBelt existingBelt = GetComponentInParent<ConveyorBelt>();
        if (existingBelt == null)
        {
            float cellRotation = GetCellDirectionRotation(cellDirection);
            transform.rotation = Quaternion.Euler(0, 0, cellRotation);
            Debug.Log($"MachineRenderer applying rotation {cellRotation} for cell direction {cellDirection}");
        }
        else
        {
            Debug.Log($"ConveyorBelt component found in parent - skipping MachineRenderer rotation for {def.id}");
        }
    }
    
    private void CreateSeparatedBuildingSprite(MachineDef def, UICell.Direction cellDirection)
    {
        RectTransform buildingsContainer = gridManager.GetBuildingsContainer();
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

    // This is the helper method you need:
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
            // Create a colored rectangle as fallback so you can see something
            img.color = name == "Border" ? Color.red :
                       name == "Building" ? Color.blue :
                       name == "MovingPart" ? Color.green : Color.yellow;
        }
        else
        {
            Debug.Log($"Successfully loaded sprite: {spritePath} for '{name}'");
            img.color = Color.white;
        }

        // Stretch to fill parent
        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Ensure the RectTransform is properly reset
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;

        return img;
    }

    void OnDestroy()
    {
        // Clean up separated building sprite when this renderer is destroyed
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