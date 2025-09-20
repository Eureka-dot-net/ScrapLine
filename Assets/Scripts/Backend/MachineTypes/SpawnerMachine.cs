using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles spawner machine behavior. Spawners create new items at regular intervals
/// and can spawn different item types based on their configuration.
/// </summary>
public class SpawnerMachine : BaseMachine
{
    private float lastSpawnTime;
    private float spawnInterval;
    
    public SpawnerMachine(CellData cellData, MachineDef machineDef) : base(cellData, machineDef)
    {
        // Set spawn interval from machine definition
        spawnInterval = machineDef.baseProcessTime;
        lastSpawnTime = Time.time;
    }
    
    /// <summary>
    /// Update logic for spawner - handles item spawning timer
    /// </summary>
    public override void UpdateLogic()
    {
        // Check if it's time to spawn and if the cell is empty
        if (Time.time - lastSpawnTime >= spawnInterval && cellData.items.Count == 0)
        {
            SpawnItem();
            lastSpawnTime = Time.time;
        }
    }
    
    /// <summary>
    /// Spawns a new item at this spawner's location
    /// </summary>
    private void SpawnItem()
    {
        Debug.Log($"Spawning new item at ({cellData.x}, {cellData.y})");

        // Determine what item to spawn
        string itemType = "can"; // Default item type
        
        // Use first spawnable item from spawner definition if available
        if (machineDef.spawnableItems != null && machineDef.spawnableItems.Count > 0)
        {
            itemType = machineDef.spawnableItems[0]; // Use first spawnable item for now
        }

        // Create new item with proper ItemData structure
        ItemData newItem = new ItemData
        {
            id = "item_" + System.Guid.NewGuid().ToString("N")[..8], // Generate unique ID
            itemType = itemType,
            x = cellData.x,
            y = cellData.y,
            state = ItemState.Idle,
            moveProgress = 0f,
            processingStartTime = 0f,
            processingDuration = 0f,
            waitingStartTime = 0f,
            targetMoveProgress = 0f
        };

        cellData.items.Add(newItem);

        // Tell visual manager to create visual representation
        UIGridManager gridManager = Object.FindAnyObjectByType<UIGridManager>();
        if (gridManager != null)
        {
            gridManager.CreateVisualItem(newItem.id, cellData.x, cellData.y, newItem.itemType);
        }
    }
    
    /// <summary>
    /// Spawners don't process arriving items - they create new ones
    /// </summary>
    public override void OnItemArrived(ItemData item)
    {
        // Spawners don't handle incoming items
        // Items shouldn't arrive at spawners in normal gameplay
        Debug.LogWarning($"Item {item.id} arrived at spawner - this shouldn't happen");
    }
    
    /// <summary>
    /// Spawners don't process items - they create them
    /// </summary>
    public override void ProcessItem(ItemData item)
    {
        // Spawners don't process items
        Debug.LogWarning($"Attempted to process item {item.id} at spawner - this shouldn't happen");
    }
}