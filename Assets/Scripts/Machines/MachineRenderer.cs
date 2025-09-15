using System;
using UnityEngine;
using UnityEngine.UI;

public class MachineRenderer : MonoBehaviour
{
    [Header("Context")]
    public bool isInMenu = false; // Set to true when used in UI panels to disable materials

    public void Setup(MachineDef def, UICell.Direction cellDirection = UICell.Direction.Up)
    {
        foreach (Transform child in transform) Destroy(child.gameObject);

        Debug.Log("Creating machine with sprites: " +
                  $"Main='{def.sprite}', Border='{def.borderSprite}', " +
                  $"MovingPart='{def.movingPartSprite}', Building='{def.buildingSprite}'" +
                  $" BorderColor='{def.borderColor}', IsInMenu={isInMenu}");

        // Create images in back-to-front order (first created = background)


        if (!string.IsNullOrEmpty(def.movingPartSprite))
        {
            var movingPart = CreateImageChild("MovingPart", def.movingPartSprite);
            // Only apply materials if NOT in menu to prevent unwanted animations
            if (!isInMenu && !string.IsNullOrEmpty(def.movingPartMaterial))
            {
                string movingPartMatPath = "Materials/" + def.movingPartMaterial;
                var mat = Resources.Load<Material>(movingPartMatPath);
                if (mat != null)
                {
                    movingPart.material = mat;
                    Debug.Log($"Successfully applied material '{movingPartMatPath}' to MovingPart");
                }
                else
                {
                    Debug.LogWarning($"Material '{movingPartMatPath}' could not be found in Resources folder for machine '{def.id}'");
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
            border.transform.SetSiblingIndex(1); // Ensure it's in back

        }

        CreateItemSpawnPoint();

        if (!string.IsNullOrEmpty(def.buildingSprite))
        {
            var building = CreateImageChild("Building", def.buildingSprite);
            building.rectTransform.rotation = Quaternion.Euler(0, 0, def.buildingDirection);
            building.transform.SetSiblingIndex(3);
        }

        // 4. If there's a main sprite, create it on top
        if (!string.IsNullOrEmpty(def.sprite))
        {
            var mainSprite = CreateImageChild("Main", def.sprite);
            mainSprite.transform.SetSiblingIndex(4);
        }

        // Apply cell direction rotation to entire renderer
        float cellRotation = GetCellDirectionRotation(cellDirection);
        transform.rotation = Quaternion.Euler(0, 0, cellRotation);

        // Create spawn point for items between border and building
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