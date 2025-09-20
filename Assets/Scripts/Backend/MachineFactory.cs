using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Factory class responsible for creating the correct machine instance
/// based on machine definition data. Uses a lookup dictionary to map
/// machine types to their corresponding C# classes.
/// </summary>
public static class MachineFactory
{
    /// <summary>
    /// Creates the appropriate machine instance based on the cell data's machine definition.
    /// This replaces the large switch statements in GameManager.
    /// </summary>
    /// <param name="cellData">The cell data containing machine information</param>
    /// <returns>A BaseMachine instance of the correct type, or null if invalid</returns>
    public static BaseMachine CreateMachine(CellData cellData)
    {
        if (cellData == null || string.IsNullOrEmpty(cellData.machineDefId))
        {
            return null;
        }
        
        // Get the machine definition from the registry
        MachineDef machineDef = FactoryRegistry.Instance.GetMachine(cellData.machineDefId);
        if (machineDef == null)
        {
            Debug.LogError($"Machine definition not found for ID: {cellData.machineDefId}");
            return null;
        }
        
        // Create the appropriate machine instance based on machine type/id
        // This replaces the switch statements that were in GameManager
        switch (cellData.machineDefId)
        {
            case "spawner":
                return new SpawnerMachine(cellData, machineDef);
                
            case "shredder":
                return new ProcessorMachine(cellData, machineDef);
                
            case "seller":
                return new SellerMachine(cellData, machineDef);
                
            case "blank":
            case "blank_top":
            case "blank_bottom":
                return new BlankCellMachine(cellData, machineDef);
                
            case "conveyor":
                // Conveyors don't process items, they just move them
                // They can be handled as blank cells with movement behavior
                return new BlankCellMachine(cellData, machineDef);
                
            default:
                Debug.LogWarning($"Unknown machine type: {cellData.machineDefId}, creating BlankCellMachine");
                return new BlankCellMachine(cellData, machineDef);
        }
    }
    
    /// <summary>
    /// Alternative factory method that uses the className field from MachineDef
    /// This would be used if we add the className field to the JSON definitions
    /// </summary>
    /// <param name="cellData">The cell data containing machine information</param>
    /// <returns>A BaseMachine instance of the correct type, or null if invalid</returns>
    public static BaseMachine CreateMachineByClassName(CellData cellData)
    {
        if (cellData == null || string.IsNullOrEmpty(cellData.machineDefId))
        {
            return null;
        }
        
        MachineDef machineDef = FactoryRegistry.Instance.GetMachine(cellData.machineDefId);
        if (machineDef == null)
        {
            Debug.LogError($"Machine definition not found for ID: {cellData.machineDefId}");
            return null;
        }
        
        // This would use reflection to create instances based on className
        // For now, we'll use the simpler approach above
        // TODO: Implement reflection-based creation if className field is added
        
        return CreateMachine(cellData);
    }
}