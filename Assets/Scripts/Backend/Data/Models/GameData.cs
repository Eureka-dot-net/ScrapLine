// Add these classes to your data model file (or create a new one)

using System.Collections.Generic;

[System.Serializable]
public class ItemData
{
    public string id;
    public string itemType;
    
    // Movement state
    public bool isMoving;
    public int targetX;
    public int targetY;
    public float moveProgress; // 0.0 to 1.0
    public float moveStartTime;
    public bool hasCheckedMiddle; // Flag to prevent multiple middle checks per movement
    
    // Waiting state for busy machines
    public bool isWaiting; // Whether item is waiting at machine border due to busy machine
    public int waitingAtX; // X coordinate of machine being waited for
    public int waitingAtY; // Y coordinate of machine being waited for
    public int queuePosition; // Position in waiting queue (0 = first in line)
    
    // Next movement planning
    public bool shouldStopAtTarget;
    public bool hasQueuedMovement;
    public int queuedTargetX;
    public int queuedTargetY;
    
    // Timeout tracking for blank cells
    public float timeOnBlankCell; // Time spent on current blank cell
    public bool isOnBlankCell; // Whether item is currently on a blank cell
    
    // Recipe processing state
    public bool isProcessing; // Whether item is currently being processed by a recipe
    public float processingStartTime; // When processing started
    public float processingDuration; // How long processing should take
    public string processingMachineId; // Which machine is processing this item
}

[System.Serializable]
public class CellData
{
    public int x;
    public int y;
    public UICell.CellType cellType;
    public UICell.Direction direction;
    public UICell.CellRole cellRole;
    public string machineDefId; // References the specific machine definition from FactoryRegistry
    public List<ItemData> items = new List<ItemData>();
}

[System.Serializable]
public class GridData
{
    public int width;
    public int height;
    public List<CellData> cells = new List<CellData>();
}

[System.Serializable]
public class UserMachineProgress
{
    public string machineId;
    public bool unlocked;
    public int upgradeLevel;
}

[System.Serializable]
public class GameData
{
    public List<GridData> grids = new List<GridData>();
    public List<UserMachineProgress> userMachineProgress = new List<UserMachineProgress>();
    public int credits = 0; // Credits (money) system for purchasing machines
}