using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Factory class responsible for creating the correct machine instance
/// based on machine definition data. Uses a lookup dictionary to map
/// machine types to their corresponding C# classes via className field.
/// </summary>
public static class MachineFactory
{
    /// <summary>
    /// Static dictionary that maps class names to their corresponding Type objects
    /// for efficient machine instantiation
    /// </summary>
    private static readonly Dictionary<string, Type> MachineTypeMap = new Dictionary<string, Type>
    {
        { "SpawnerMachine", typeof(SpawnerMachine) },
        { "ProcessorMachine", typeof(ProcessorMachine) },
        { "SellerMachine", typeof(SellerMachine) },
        { "BlankCellMachine", typeof(BlankCellMachine) },
        { "ConveyorMachine", typeof(ConveyorMachine) },
        { "SortingMachine", typeof(SortingMachine) },
        { "FabricatorMachine", typeof(FabricatorMachine) }
    };

    /// <summary>
    /// Creates the appropriate machine instance based on the machine definition's className field.
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

        // Check if className is specified in the machine definition
        if (string.IsNullOrEmpty(machineDef.className))
        {
            Debug.LogError($"Machine definition {cellData.machineDefId} is missing className field");
            return null;
        }

        // Look up the class type from our dictionary
        if (!MachineTypeMap.TryGetValue(machineDef.className, out Type machineType))
        {
            Debug.LogError($"Unknown machine class name: {machineDef.className}");
            return null;
        }

        try
        {
            // Create instance using reflection with constructor parameters
            BaseMachine machineInstance = (BaseMachine)Activator.CreateInstance(machineType, cellData, machineDef);
            return machineInstance;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to create machine instance for {machineDef.className}: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Alternative factory method that uses the className field from MachineDef.
    /// This method is now the primary implementation since we use className field.
    /// </summary>
    /// <param name="cellData">The cell data containing machine information</param>
    /// <returns>A BaseMachine instance of the correct type, or null if invalid</returns>
    public static BaseMachine CreateMachineByClassName(CellData cellData)
    {
        // This method now calls the main CreateMachine method since it's className-based
        return CreateMachine(cellData);
    }
}