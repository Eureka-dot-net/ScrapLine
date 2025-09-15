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
    public string movingPartSprite;        // moving part sprite
    public string borderSprite;            // border/track sprite (can be null)
    public string buildingSprite;          // building sprite (can be null)
    public string movingPartMaterial;      // material for moving part (can be null)
    public string borderColor;             // hex color for border tint (can be null)
    public int buildingDirection;          // degrees (0, 90, 180, 270)
    public List<UpgradeMultiplier> upgradeMultipliers;
    public List<string> gridPlacement;     // e.g., ["any"], ["bottom"]
    public int maxNumber;
    public bool displayInPanel = true;     // whether to show in machine selection panel
    public int cost = 0;                   // Credits cost to place this machine
    public List<string> spawnableItems;    // Items that this machine can spawn (for spawners)
}

[System.Serializable]
public class RecipeDef {
    public string machineId;
    public List<string> inputItems;
    public List<string> outputItems;
    public float processAdjustment;
}

[System.Serializable]
public class ItemDef {
    public string id;
    public string displayName;
    public string sprite;
    public int sellValue = 0; // Credits earned when this item is sold
}