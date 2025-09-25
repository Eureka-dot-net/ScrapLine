using System.Collections.Generic;

[System.Serializable]
public class UpgradeMultiplier {
    public float multiplier;
    public int cost;
}

[System.Serializable]
public class MachineDef
{
    public string id;
    public string type;
    public string sprite;                  // UI icon
    public float baseProcessTime;
    public bool isMoving;                  // whether this machine has moving parts
    public string borderSprite;            // border/track sprite (can be null)
    public string buildingSprite;          // building sprite (can be null)
    public string borderColor;             // hex color for border tint (can be null)
    public int buildingDirection;          // degrees (0, 90, 180, 270)
    public string buildingIconSprite;      // icon sprite to overlay on building (can be null)
    public float buildingIconSpriteSize = 1.0f;  // size multiplier for icon sprite (0-1)
    public string buildingSpriteColour;    // hex color for building sprite tint (can be null)
    public List<UpgradeMultiplier> upgradeMultipliers;
    public List<string> gridPlacement;     // e.g., ["any"], ["bottom"]
    public int maxNumber;
    public bool displayInPanel = true;     // whether to show in machine selection panel
    public int cost = 0;                   // Credits cost to place this machine
    public List<string> spawnableItems;    // Items that this machine can spawn (for spawners)
    public bool canRotate = true;          // whether this machine can be rotated by clicking
    public string className;               // Links to the C# class for machine behavior
}

[System.Serializable]
public class RecipeItemDef {
    public string item;
    public int count;
}

[System.Serializable]
public class RecipeDef {
    public string machineId;
    public List<RecipeItemDef> inputItems;
    public List<RecipeItemDef> outputItems;
    public float processMultiplier;
    
    /// <summary>
    /// Calculated process time for this recipe (baseProcessTime * processMultiplier)
    /// </summary>
    public float processTime
    {
        get
        {
            var machineDef = FactoryRegistry.Instance.GetMachine(machineId);
            return machineDef != null ? machineDef.baseProcessTime * processMultiplier : 0f;
        }
    }
}

[System.Serializable]
public class ItemDef {
    public string id;
    public string displayName;
    public string sprite;
    public int sellValue = 0; // Credits earned when this item is sold
}

[System.Serializable]
public class WasteCrateItemDef {
    public string itemType;
    public int count;
}

[System.Serializable]
public class WasteCrateDef {
    public string id;
    public string displayName;
    public string sprite; // Sprite for UI display
    public List<WasteCrateItemDef> items;
    public int cost; // Cost to purchase this crate (calculated from items)
}