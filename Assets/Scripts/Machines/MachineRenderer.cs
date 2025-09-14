using System;
using UnityEngine;
using UnityEngine.UI;

public class MachineRenderer : MonoBehaviour
{
    public void Setup(MachineDef def)
    {
        foreach (Transform child in transform) Destroy(child.gameObject);

        Debug.Log("Creating machine with sprites: " +
                  $"Main='{def.sprite}', Border='{def.borderSprite}', " +
                  $"MovingPart='{def.movingPartSprite}', Building='{def.buildingSprite}'");

        // Create images in back-to-front order (first created = background)

        if (!string.IsNullOrEmpty(def.movingPartSprite))
        {
            var movingPart = CreateImageChild("MovingPart", def.movingPartSprite);
            if (!string.IsNullOrEmpty(def.movingPartMaterial))
            {
                var mat = Resources.Load<Material>(def.movingPartMaterial);
                if (mat != null)
                    movingPart.material = mat;
            }
            movingPart.transform.SetSiblingIndex(0);
        }

        if (!string.IsNullOrEmpty(def.borderSprite))
        {
            var border = CreateImageChild("Border", def.borderSprite);
            border.transform.SetSiblingIndex(1); // Ensure it's in back
        }

        if (!string.IsNullOrEmpty(def.buildingSprite))
        {
            var building = CreateImageChild("Building", def.buildingSprite);
            building.rectTransform.rotation = Quaternion.Euler(0, 0, def.buildingDirection);
            building.transform.SetSiblingIndex(2);
        }

        // 4. If there's a main sprite, create it on top
        if (!string.IsNullOrEmpty(def.sprite))
        {
            var mainSprite = CreateImageChild("Main", def.sprite);
            mainSprite.transform.SetSiblingIndex(3);
        }

        // Create spawn point for items between border and building
        CreateItemSpawnPoint();
    }

    private void CreateItemSpawnPoint()
    {
        // Find the UICell parent to set the spawn point
        UICell parentCell = GetComponentInParent<UICell>();
        if (parentCell != null)
        {
            // Create spawn point GameObject
            GameObject spawnPointObj = new GameObject("ItemSpawnPoint");
            spawnPointObj.transform.SetParent(this.transform, false);
            
            RectTransform spawnPointRT = spawnPointObj.AddComponent<RectTransform>();
            
            // Position it in the center, between border and building layers
            spawnPointRT.anchorMin = new Vector2(0.5f, 0.5f);
            spawnPointRT.anchorMax = new Vector2(0.5f, 0.5f);
            spawnPointRT.anchoredPosition = Vector2.zero;
            spawnPointRT.sizeDelta = Vector2.zero;
            
            // Set the sibling index to be between border (1) and building (2)
            spawnPointObj.transform.SetSiblingIndex(1);
            
            // Assign to the parent UICell
            parentCell.itemSpawnPoint = spawnPointRT;
            
            Debug.Log("Created item spawn point for machine");
        }
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
}