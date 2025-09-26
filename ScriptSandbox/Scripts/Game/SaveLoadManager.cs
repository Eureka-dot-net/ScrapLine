using UnityEngine;
using System.IO;
using System.Collections;

/// <summary>
/// Manages game state serialization and persistence.
/// Handles saving and loading game data to/from JSON files.
/// </summary>
public class SaveLoadManager : MonoBehaviour
{
    [Header("Save Configuration")]
    [Tooltip("Name of the save file")]
    public string saveFileName = "game_data.json";
    
    [Header("Debug")]
    [Tooltip("Enable debug logs for save/load operations")]

    private GridManager gridManager;
    private CreditsManager creditsManager;
    
    /// <summary>
    /// Get the component ID for logging purposes
    /// </summary>
    private string ComponentId => $"SaveLoadManager_{GetInstanceID()}";

    /// <summary>
    /// Initialize the save/load manager
    /// </summary>
    /// <param name="gridManager">Reference to the grid manager</param>
    /// <param name="creditsManager">Reference to the credits manager</param>
    public void Initialize(GridManager gridManager, CreditsManager creditsManager)
    {
        this.gridManager = gridManager;
        this.creditsManager = creditsManager;
    }

    /// <summary>
    /// Check if a save file exists
    /// </summary>
    /// <returns>True if save file exists, false otherwise</returns>
    public bool SaveFileExists()
    {
        string path = GetSaveFilePath();
        return File.Exists(path);
    }

    /// <summary>
    /// Get the full path to the save file
    /// </summary>
    /// <returns>Full path to save file</returns>
    private string GetSaveFilePath()
    {
        return Application.persistentDataPath + "/" + saveFileName;
    }

    /// <summary>
    /// Save the current game state
    /// </summary>
    public void SaveGame()
    {
        try
        {
            // Get current game data from GameManager
            GameData data = GameManager.Instance.gameData;
            
            // Update with current state
            data.grids = gridManager.GetActiveGrids();
            data.credits = creditsManager.GetCredits();
            
            // Save WasteSupplyManager data
            var wasteSupplyManager = WasteSupplyManager.Instance;
            if (wasteSupplyManager != null)
            {
                data.machineWasteQueues = wasteSupplyManager.SerializeQueues();
                GameLogger.LogSaveLoad($"Saved queues for {data.machineWasteQueues.Count} machines", ComponentId);
            }
            
            // Save spawner configuration data
            SaveSpawnerConfigurations(data);
            
            FactoryRegistry.Instance.SaveToGameData(data);

            string json = JsonUtility.ToJson(data);
            string path = GetSaveFilePath();
            File.WriteAllText(path, json);
            
            GameLogger.LogSaveLoad($"Game saved successfully. Machines with queues: {data.machineWasteQueues?.Count ?? 0}", ComponentId);
        }
        catch (System.Exception ex)
        {
            GameLogger.LogError(LoggingManager.LogCategory.SaveLoad, $"Failed to save game: {ex.Message}", ComponentId);
        }
    }

    /// <summary>
    /// Load the game state from file
    /// </summary>
    /// <returns>True if loaded successfully, false otherwise</returns>
    public bool LoadGame()
    {
        string path = GetSaveFilePath();
        
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(path);
            GameData data = JsonUtility.FromJson<GameData>(json);
            
            if (data == null)
            {
                GameLogger.LogError(LoggingManager.LogCategory.SaveLoad, $"Failed to deserialize save data!", ComponentId);
                return false;
            }

            // Store loaded data in GameManager
            GameManager.Instance.gameData = data;

            // Load grid data
            gridManager.SetActiveGrids(data.grids);

            // Load credits
            creditsManager.SetCredits(data.credits);

            // Load WasteSupplyManager data
            var wasteSupplyManager = WasteSupplyManager.Instance;
            if (wasteSupplyManager != null)
            {
                wasteSupplyManager.DeserializeQueues(data.machineWasteQueues);
                GameLogger.LogSaveLoad($"Loaded queues for {data.machineWasteQueues?.Count ?? 0} machines", ComponentId);
            }
            
            // Load spawner configuration data
            LoadSpawnerConfigurations(data);

            // Load factory registry data
            FactoryRegistry.Instance.LoadFromGameData(data);
            
            GameLogger.LogSaveLoad($"Game loaded successfully. Machines with queues: {data.machineWasteQueues?.Count ?? 0}", ComponentId);

            return true;
        }
        catch (System.Exception ex)
        {
            GameLogger.LogError(LoggingManager.LogCategory.SaveLoad, $"Failed to load game: {ex.Message}", ComponentId);
            return false;
        }
    }

    /// <summary>
    /// Initialize machines from saved data after FactoryRegistry is loaded
    /// </summary>
    public IEnumerator InitializeMachinesFromSave()
    {
        yield return new WaitUntil(() => FactoryRegistry.Instance.IsLoaded());

        GridData currentGrid = gridManager.GetCurrentGrid();
        if (currentGrid == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.SaveLoad, $"No current grid available for machine initialization!", ComponentId);
            yield break;
        }

        foreach (var cell in currentGrid.cells)
        {
            if (cell.machine == null && !string.IsNullOrEmpty(cell.machineDefId))
            {
                cell.machine = MachineFactory.CreateMachine(cell);
            }
            else if (cell.machine == null)
            {
                cell.machine = MachineFactory.CreateMachine(cell);
            }
        }
    }

    /// <summary>
    /// Delete the save file
    /// </summary>
    public void DeleteSaveFile()
    {
        string path = GetSaveFilePath();
        
        if (File.Exists(path))
        {
            try
            {
                File.Delete(path);
            }
            catch (System.Exception ex)
            {
                GameLogger.LogError(LoggingManager.LogCategory.SaveLoad, $"Failed to delete save file: {ex.Message}", ComponentId);
            }
        }
    }

    /// <summary>
    /// Save spawner configuration data to GameData
    /// </summary>
    /// <param name="data">GameData to save to</param>
    private void SaveSpawnerConfigurations(GameData data)
    {
        data.spawnerRequiredCrateIds = new System.Collections.Generic.Dictionary<string, string>();
        
        // Iterate through all grids and collect spawner configurations
        foreach (var grid in data.grids)
        {
            foreach (var cell in grid.cells)
            {
                if (cell.machine is SpawnerMachine spawner)
                {
                    string machineId = $"Spawner_{cell.x}_{cell.y}";
                    data.spawnerRequiredCrateIds[machineId] = spawner.RequiredCrateId;
                }
            }
        }
        
        GameLogger.LogSaveLoad($"Saved configuration for {data.spawnerRequiredCrateIds.Count} spawners", ComponentId);
    }

    /// <summary>
    /// Load spawner configuration data from GameData
    /// </summary>
    /// <param name="data">GameData to load from</param>
    private void LoadSpawnerConfigurations(GameData data)
    {
        if (data.spawnerRequiredCrateIds == null) return;
        
        int configuredSpawners = 0;
        
        // Iterate through all grids and apply spawner configurations
        foreach (var grid in data.grids)
        {
            foreach (var cell in grid.cells)
            {
                if (cell.machine is SpawnerMachine spawner)
                {
                    string machineId = $"Spawner_{cell.x}_{cell.y}";
                    if (data.spawnerRequiredCrateIds.TryGetValue(machineId, out string requiredCrateId))
                    {
                        spawner.RequiredCrateId = requiredCrateId;
                        configuredSpawners++;
                    }
                }
            }
        }
        
        GameLogger.LogSaveLoad($"Loaded configuration for {configuredSpawners} spawners", ComponentId);
    }
}