using System.Collections;
using UnityEngine;

/// <summary>
/// Coordinates versioned, durable game saves with scene state and lifecycle events.
/// </summary>
public class SaveLoadManager : MonoBehaviour
{
    [Header("Save Configuration")]
    [Tooltip("Name of the save file")]
    public string saveFileName = "game_data.json";

    [Tooltip("Seconds to coalesce frequent state changes before writing")]
    [Min(0.1f)]
    public float autosaveDelaySeconds = 2f;

    [Tooltip("Initial delay before retrying a failed autosave")]
    [Min(0.5f)]
    public float autosaveRetryDelaySeconds = 5f;

    [Tooltip("Maximum delay between repeated autosave failures")]
    [Min(1f)]
    public float autosaveMaxRetryDelaySeconds = 60f;

    private GridManager gridManager;
    private CreditsManager creditsManager;
    private GameSaveStorage storage;
    private bool autosavePending;
    private float autosaveAtRealtime;
    private int consecutiveAutosaveFailures;

    private string ComponentId => $"SaveLoadManager_{GetEntityId()}";

    public void Initialize(GridManager gridManager, CreditsManager creditsManager)
    {
        this.gridManager = gridManager;
        this.creditsManager = creditsManager;
        storage = new GameSaveStorage(Application.persistentDataPath, saveFileName);
        creditsManager.CreditsChanged -= RequestAutosave;
        creditsManager.CreditsChanged += RequestAutosave;
    }

    private void OnDestroy()
    {
        if (creditsManager != null)
            creditsManager.CreditsChanged -= RequestAutosave;
    }

    private void Update()
    {
        if (autosavePending && Time.realtimeSinceStartup >= autosaveAtRealtime)
            SaveGame();
    }

    public bool SaveFileExists()
    {
        EnsureStorage();
        return storage.AnySaveExists;
    }

    public void RequestAutosave()
    {
        if (autosavePending)
            return;
        autosavePending = true;
        autosaveAtRealtime = Time.realtimeSinceStartup + Mathf.Max(0.1f, autosaveDelaySeconds);
    }

    public bool SaveGame()
    {
        if (gridManager == null || creditsManager == null || GameManager.Instance == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.SaveLoad,
                "Cannot save before managers are initialized.", ComponentId);
            return false;
        }

        GameData data = GameManager.Instance.gameData;
        data.grids = gridManager.GetActiveGrids();
        data.credits = creditsManager.GetCredits();
        data.hasRuntimeClockAnchor = true;
        data.savedAtRuntimeTime = SimulationClock.Time;
        FactoryRegistry.Instance.SaveToGameData(data);

        EnsureStorage();
        if (!storage.TrySave(data, ValidateMachineUnlockCandidate, out string error))
        {
            GameLogger.LogError(LoggingManager.LogCategory.SaveLoad, $"Failed to save game: {error}", ComponentId);
            ScheduleAutosaveRetry();
            return false;
        }

        autosavePending = false;
        consecutiveAutosaveFailures = 0;
        GameLogger.LogSaveLoad(
            $"Game saved at schema {data.schemaVersion}.", ComponentId);
        return true;
    }

    public bool LoadGame()
    {
        EnsureStorage();
        if (!storage.TryLoad(
                ValidateMachineUnlockCandidate,
                out GameData data,
                out bool loadedFromBackup,
                out bool migrationApplied,
                out string loadWarning))
        {
            GameLogger.LogError(LoggingManager.LogCategory.SaveLoad, $"Failed to load game: {loadWarning}", ComponentId);
            return false;
        }

        float itemMoveSpeed = GameManager.Instance.itemMovementManager != null
            ? GameManager.Instance.itemMovementManager.itemMoveSpeed
            : 1f;
        if (!GameSaveRuntimeRehydrator.TryPrepareForResume(
                data, SimulationClock.Time, itemMoveSpeed, out string rehydrationError))
        {
            GameLogger.LogError(LoggingManager.LogCategory.SaveLoad,
                $"Failed to restore runtime save state: {rehydrationError}", ComponentId);
            return false;
        }

        if (!FactoryRegistry.Instance.TryLoadFromGameData(data, out string unlockError))
        {
            GameLogger.LogError(LoggingManager.LogCategory.SaveLoad,
                $"Failed to restore machine unlock state: {unlockError}", ComponentId);
            return false;
        }

        GameManager.Instance.gameData = data;
        gridManager.SetActiveGrids(data.grids);
        creditsManager.SetCredits(data.credits, false);
        autosavePending = false;

        if (loadedFromBackup)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.SaveLoad,
                $"Primary save was invalid; recovered from backup. {loadWarning}", ComponentId);
        }

        // Preserve the previous backup during ordinary loads. Only migrations and backup recovery
        // require an immediate rewrite of the primary generation.
        if ((loadedFromBackup || migrationApplied) &&
            !storage.TrySave(data, ValidateMachineUnlockCandidate, out string rewriteError))
        {
            GameLogger.LogError(LoggingManager.LogCategory.SaveLoad,
                $"Game loaded, but the migrated/primary save could not be written: {rewriteError}", ComponentId);
            ScheduleAutosaveRetry();
        }

        GameLogger.LogSaveLoad(
            $"Game loaded at schema {data.schemaVersion}.", ComponentId);
        return true;
    }

    private static string ValidateMachineUnlockCandidate(GameData data)
    {
        return FactoryRegistry.Instance.ValidateMachineProgress(data, out string error)
            ? null
            : error;
    }

    public IEnumerator InitializeMachinesFromSave()
    {
        yield return new WaitUntil(() => FactoryRegistry.Instance.IsLoaded());

        GridData currentGrid = gridManager.GetCurrentGrid();
        if (currentGrid == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.SaveLoad,
                "No current grid available for machine initialization!", ComponentId);
            yield break;
        }

        foreach (CellData cell in currentGrid.cells)
        {
            if (cell.machine == null)
                cell.machine = MachineFactory.CreateMachine(cell);
        }
    }

    public bool DeleteSaveFile()
    {
        EnsureStorage();
        autosavePending = false;
        if (storage.TryDeleteAll(out string error))
            return true;
        GameLogger.LogError(LoggingManager.LogCategory.SaveLoad, $"Failed to delete save files: {error}", ComponentId);
        return false;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && GameManager.Instance != null)
            SaveGame();
    }

    private void OnApplicationQuit()
    {
        // Desktop quit is best-effort. Mobile pause/background is the authoritative lifecycle flush.
        if (GameManager.Instance != null)
            SaveGame();
    }

    private void EnsureStorage()
    {
        storage ??= new GameSaveStorage(Application.persistentDataPath, saveFileName);
    }

    private void ScheduleAutosaveRetry()
    {
        consecutiveAutosaveFailures++;
        float exponent = Mathf.Min(consecutiveAutosaveFailures - 1, 10);
        float delay = Mathf.Min(
            Mathf.Max(0.5f, autosaveRetryDelaySeconds) * Mathf.Pow(2f, exponent),
            Mathf.Max(1f, autosaveMaxRetryDelaySeconds));
        autosavePending = true;
        autosaveAtRealtime = Time.realtimeSinceStartup + delay;
    }
}
