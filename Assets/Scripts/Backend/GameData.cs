using UnityEngine;
using System.Collections.Generic;

// We must add the [System.Serializable] attribute for Unity's JsonUtility to work
[System.Serializable]
public class GameData
{
    public List<GridData> grids = new List<GridData>();
}

[System.Serializable]
public class GridData
{
    public int width;
    public int height;
    public List<CellData> cells = new List<CellData>();
}

[System.Serializable]
public class CellData
{
    public int x;
    public int y;
    public UICell.CellType cellType;
    public UICell.Direction direction;

    public UICell.MachineType machineType;

    public UICell.CellRole cellRole;

    public List<ItemData> items = new List<ItemData>();
}

[System.Serializable]
public class ItemData
{
    public string itemType;
}