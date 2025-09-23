using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles fabricator machine behavior. These machines take input items,
/// process them according to recipes, and output transformed items.
/// </summary>
public class FabricatorMachine : ProcessorMachine
{
    public FabricatorMachine(CellData cellData, MachineDef machineDef) : base(cellData, machineDef)
    {
    }
}
