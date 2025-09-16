// Add these classes to your data model file (or create a new one)

using System.Collections.Generic;

public enum ItemState
{
    Idle,      // Item is stationary and ready to move
    Moving,    // Item is moving between cells
    Waiting,   // Item is waiting because target machine is busy
    Processing // Item is being processed by a machine
}

public enum MachineState
{
    Idle,       // Machine is ready to receive items
    Receiving,  // Item is moving to machine (prevents race conditions)
    Processing  // Machine is actively processing an item
}

[System.Serializable]
public class ItemData
{
    public string id;
    public string itemType;
    
    // Simple state machine
    public ItemState state = ItemState.Idle;
    
    // Movement data (reuse existing fields)
    public int targetX;
    public int targetY;
    public float moveProgress; // 0.0 to 1.0
    public float moveStartTime;
    
    // Processing data (simplified)
    public float processingStartTime;
    public float processingDuration;
    public string processingMachineId;
    
    // Waiting timeout
    public float waitingStartTime;
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
    public List<ItemData> waitingItems = new List<ItemData>(); // List for items waiting to enter this machine
    public MachineState machineState = MachineState.Idle; // Current state of the machine
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