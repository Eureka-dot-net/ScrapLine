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
    //public string id;
    public string id;
    public string itemType;
    public int x;
    public int y;

    public ItemState state = ItemState.Idle;
    public float moveStartTime;
    public float moveProgress;
    public int sourceX;
    public int sourceY;
    public int targetX;
    public int targetY;

    public float processingStartTime;
    public float processingDuration;

    public float waitingStartTime;

    public bool isHalfway = false; // Flag to indicate if the item is halfway in its movement
    
    public int stackIndex = 0; // Index in the waiting stack (0 = center, 1 = left, 2 = right, 3 = left2, etc.)

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
    public BaseMachine machine; // Runtime machine object that handles behavior
    public string selectedRecipeId; // Player's configuration choice for this machine
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