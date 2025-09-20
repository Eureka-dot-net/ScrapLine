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
    public bool enableSaveLoadLogs = true;

    private GridManager gridManager;
    private CreditsManager creditsManager;

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
            GameData data = new GameData
            {
                grids = gridManager.GetActiveGrids(),
                credits = creditsManager.GetCredits()
            };

            FactoryRegistry.Instance.SaveToGameData(data);

            string json = JsonUtility.ToJson(data);
            string path = GetSaveFilePath();
            File.WriteAllText(path, json);
            
            if (enableSaveLoadLogs)
                Debug.Log($"Game saved with {data.credits} credits to location {path}!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save game: {ex.Message}");
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
                Debug.LogError("Failed to deserialize save data!");
                return false;
            }

            // Load grid data
            gridManager.SetActiveGrids(data.grids);

            // Load credits
            creditsManager.SetCredits(data.credits);

            // Load factory registry data
            FactoryRegistry.Instance.LoadFromGameData(data);

            if (enableSaveLoadLogs)
                Debug.Log("Save game loaded successfully.");

            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to load game: {ex.Message}");
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
            Debug.LogError("No current grid available for machine initialization!");
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
                Debug.LogError($"Failed to delete save file: {ex.Message}");
            }
        }
    }
}