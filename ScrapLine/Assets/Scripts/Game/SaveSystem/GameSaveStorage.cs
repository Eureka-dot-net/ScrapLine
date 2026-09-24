using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Durable JSON storage using a validated temporary file and one recoverable backup.
/// This class has no scene dependencies so migrations and interrupted-write recovery can be tested directly.
/// </summary>
public sealed class GameSaveStorage
{
    public GameSaveStorage(string directory, string fileName)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("A save directory is required.", nameof(directory));
        if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName)
            throw new ArgumentException("The save file name must be a simple file name.", nameof(fileName));

        PrimaryPath = Path.Combine(directory, fileName);
        TemporaryPath = PrimaryPath + ".tmp";
        BackupPath = PrimaryPath + ".bak";
        CorruptPath = PrimaryPath + ".corrupt";
    }

    public string PrimaryPath { get; }
    public string TemporaryPath { get; }
    public string BackupPath { get; }
    public string CorruptPath { get; }

    public bool AnySaveExists => File.Exists(PrimaryPath) || File.Exists(BackupPath);

    public bool TrySave(GameData data, out string error)
    {
        return TrySave(data, null, out error);
    }

    public bool TrySave(GameData data, Func<GameData, string> semanticValidator, out string error)
    {
        error = null;
        try
        {
            GameSaveMigrations.Migrate(data);
            if (!TryValidate(data, out error))
                return false;
            if (!TrySemanticValidation(data, semanticValidator, out error))
                return false;

            Directory.CreateDirectory(Path.GetDirectoryName(PrimaryPath));
            File.WriteAllText(TemporaryPath, JsonUtility.ToJson(data));
            if (!TryReadAndValidate(TemporaryPath, out GameData temporary, out _, out string temporaryError) ||
                !TrySemanticValidation(temporary, semanticValidator, out temporaryError))
                throw new InvalidDataException($"Temporary save validation failed: {temporaryError}");

            CommitTemporaryFile(semanticValidator);
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            TryDelete(TemporaryPath);
            return false;
        }
    }

    public bool TryLoad(
        out GameData data,
        out bool loadedFromBackup,
        out bool migrationApplied,
        out string error)
    {
        return TryLoad(null, out data, out loadedFromBackup, out migrationApplied, out error);
    }

    /// <summary>
    /// Selects the first structurally and semantically valid generation. The validator must not
    /// mutate game or registry state and returns null when the candidate is valid.
    /// </summary>
    public bool TryLoad(
        Func<GameData, string> semanticValidator,
        out GameData data,
        out bool loadedFromBackup,
        out bool migrationApplied,
        out string error)
    {
        loadedFromBackup = false;
        if (TryReadAndValidate(PrimaryPath, out GameData primary, out bool primaryMigrated, out string primaryError) &&
            TrySemanticValidation(primary, semanticValidator, out primaryError))
        {
            data = primary;
            migrationApplied = primaryMigrated;
            error = null;
            return true;
        }

        if (TryReadAndValidate(BackupPath, out GameData backup, out bool backupMigrated, out string backupError) &&
            TrySemanticValidation(backup, semanticValidator, out backupError))
        {
            data = backup;
            loadedFromBackup = true;
            migrationApplied = backupMigrated;
            error = primaryError;
            return true;
        }

        data = null;
        migrationApplied = false;
        error = $"Primary save: {primaryError} Backup save: {backupError}";
        return false;
    }

    private static bool TrySemanticValidation(
        GameData data,
        Func<GameData, string> semanticValidator,
        out string error)
    {
        if (semanticValidator == null)
        {
            error = null;
            return true;
        }

        try
        {
            error = semanticValidator(data);
            return string.IsNullOrEmpty(error);
        }
        catch (Exception exception)
        {
            error = $"semantic validation failed: {exception.Message}";
            return false;
        }
    }

    public bool TryDeleteAll(out string error)
    {
        error = null;
        try
        {
            TryDelete(TemporaryPath);
            TryDelete(BackupPath);
            TryDelete(PrimaryPath);
            TryDelete(CorruptPath);
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return false;
        }
    }

    private void CommitTemporaryFile(Func<GameData, string> semanticValidator)
    {
        if (!File.Exists(PrimaryPath))
        {
            File.Move(TemporaryPath, PrimaryPath);
            File.Copy(PrimaryPath, BackupPath, true);
            return;
        }

        if (!TryReadAndValidate(PrimaryPath, out GameData primary, out _, out _) ||
            !TrySemanticValidation(primary, semanticValidator, out _))
        {
            TryDelete(CorruptPath);
            File.Move(PrimaryPath, CorruptPath);
            File.Move(TemporaryPath, PrimaryPath);
            if (!File.Exists(BackupPath))
                File.Copy(PrimaryPath, BackupPath, true);
            return;
        }

        try
        {
            File.Replace(TemporaryPath, PrimaryPath, BackupPath);
        }
        catch (PlatformNotSupportedException)
        {
            CommitWithMoves();
        }
        catch (IOException)
        {
            CommitWithMoves();
        }
    }

    private void CommitWithMoves()
    {
        // A valid primary is moved to backup before the new primary is installed. An interruption at
        // either step therefore leaves at least one validated generation recoverable.
        TryDelete(BackupPath);
        File.Move(PrimaryPath, BackupPath);
        try
        {
            File.Move(TemporaryPath, PrimaryPath);
        }
        catch
        {
            if (!File.Exists(PrimaryPath) && File.Exists(BackupPath))
                File.Copy(BackupPath, PrimaryPath);
            throw;
        }
    }

    private static bool TryReadAndValidate(
        string path,
        out GameData data,
        out bool migrationApplied,
        out string error)
    {
        data = null;
        migrationApplied = false;
        if (!File.Exists(path))
        {
            error = "file does not exist.";
            return false;
        }

        try
        {
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidDataException("file is empty.");
            data = JsonUtility.FromJson<GameData>(json);
            bool hasSchemaVersion = json.Contains("\"schemaVersion\"");
            if (!hasSchemaVersion)
                data.schemaVersion = 0;
            int sourceVersion = data.schemaVersion;
            GameSaveMigrations.Migrate(data);
            migrationApplied = sourceVersion != GameSaveMigrations.CurrentSchemaVersion;
            return TryValidate(data, out error);
        }
        catch (Exception exception)
        {
            data = null;
            migrationApplied = false;
            error = exception.Message;
            return false;
        }
    }

    private static bool TryValidate(GameData data, out string error)
    {
        if (data == null)
            return Invalid("save did not contain GameData.", out error);
        if (data.schemaVersion != GameSaveMigrations.CurrentSchemaVersion)
            return Invalid($"schema version {data.schemaVersion} was not migrated.", out error);
        if (data.credits < 0)
            return Invalid("credits cannot be negative.", out error);
        if (data.grids == null || data.userMachineProgress == null || data.objectiveProgress == null)
            return Invalid("required save collections are missing.", out error);
        if (!TryValidateSchema4State(data, out error))
            return false;

        HashSet<string> itemIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> factoryIds = new HashSet<string>(StringComparer.Ordinal);
        List<string> waitingItemIds = new List<string>();
        for (int gridIndex = 0; gridIndex < data.grids.Count; gridIndex++)
        {
            GridData grid = data.grids[gridIndex];
            if (grid == null || grid.width <= 0 || grid.height <= 0 || grid.cells == null)
                return Invalid($"grid {gridIndex} is invalid.", out error);
            if (string.IsNullOrWhiteSpace(grid.factoryId))
                return Invalid($"grid {gridIndex} has no factory ID.", out error);
            if (!factoryIds.Add(grid.factoryId))
                return Invalid($"factory ID '{grid.factoryId}' is used by more than one grid.", out error);
            if (grid.cells.Count != grid.width * grid.height)
                return Invalid($"grid {gridIndex} cell count does not match its dimensions.", out error);

            HashSet<string> coordinates = new HashSet<string>(StringComparer.Ordinal);
            foreach (CellData cell in grid.cells)
            {
                if (cell == null || string.IsNullOrWhiteSpace(cell.machineDefId))
                    return Invalid($"grid {gridIndex} contains an invalid cell.", out error);
                if (cell.x < 0 || cell.x >= grid.width || cell.y < 0 || cell.y >= grid.height)
                    return Invalid($"grid {gridIndex} contains an out-of-range cell.", out error);
                if (!coordinates.Add($"{cell.x}:{cell.y}"))
                    return Invalid($"grid {gridIndex} contains duplicate cell coordinates.", out error);
                if (cell.items == null || cell.waitingItems == null || cell.sortingConfig == null ||
                    cell.wasteDeliveryQueue == null)
                    return Invalid($"grid {gridIndex} cell {cell.x}:{cell.y} has missing state.", out error);
                if (cell.wasteDeliveryQueue.Count > SpawnerMachine.DeliveryQueueCapacity)
                    return Invalid($"grid {gridIndex} cell {cell.x}:{cell.y} has an invalid waste delivery queue.", out error);
                foreach (ItemData item in cell.items)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.id) || !itemIds.Add(item.id))
                        return Invalid("cell item IDs must be present and unique.", out error);
                }
                foreach (ItemData item in cell.waitingItems)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.id))
                        return Invalid("waiting item IDs must be present.", out error);
                    waitingItemIds.Add(item.id);
                }
            }
        }

        foreach (string waitingItemId in waitingItemIds)
        {
            if (!itemIds.Contains(waitingItemId))
                return Invalid($"waiting item '{waitingItemId}' has no matching cell item.", out error);
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Structural checks for the state introduced by schema 4. Normalization repairs everything it
    /// safely can (see GameSaveMigrations), so these assertions only fire on externally damaged
    /// saves: identity collisions, which cannot be merged without guessing, and quantities that
    /// normalization should already have clamped.
    /// </summary>
    private static bool TryValidateSchema4State(GameData data, out string error)
    {
        if (data.warehouse == null || data.shipBlueprint == null || data.lifetimeStats == null ||
            data.launchResult == null)
            return Invalid("schema 4 state objects are missing.", out error);

        if (data.savedAtUtcTicks < 0 || data.savedAtUtcTicks > SaveStateLimits.MaxUtcTicks ||
            data.highWaterUtcTicks < 0 || data.highWaterUtcTicks > SaveStateLimits.MaxUtcTicks)
            return Invalid("wall-clock anchors are outside the representable range.", out error);
        if (data.highWaterUtcTicks < data.savedAtUtcTicks)
            return Invalid("the wall-clock high-water mark precedes the saved-at time.", out error);

        if (data.warehouse.slots == null)
            return Invalid("warehouse slots are missing.", out error);
        HashSet<string> warehouseItems = new HashSet<string>(StringComparer.Ordinal);
        foreach (WarehouseSlot slot in data.warehouse.slots)
        {
            if (slot == null || string.IsNullOrWhiteSpace(slot.itemId))
                return Invalid("warehouse contains an unnamed slot.", out error);
            if (!warehouseItems.Add(slot.itemId))
                return Invalid($"warehouse has more than one slot for '{slot.itemId}'.", out error);
            if (slot.count < 0)
                return Invalid($"warehouse slot '{slot.itemId}' has a negative count.", out error);
        }

        if (data.shipBlueprint.modules == null)
            return Invalid("ship blueprint modules are missing.", out error);
        HashSet<string> moduleIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (ShipModuleProgress module in data.shipBlueprint.modules)
        {
            if (module == null || string.IsNullOrWhiteSpace(module.moduleId))
                return Invalid("ship blueprint contains an unnamed module.", out error);
            if (!moduleIds.Add(module.moduleId))
                return Invalid($"ship module '{module.moduleId}' appears more than once.", out error);
            if (module.contributions == null)
                return Invalid($"ship module '{module.moduleId}' has no contribution list.", out error);
            HashSet<string> componentIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (ShipComponentContribution contribution in module.contributions)
            {
                if (contribution == null || string.IsNullOrWhiteSpace(contribution.itemId))
                    return Invalid($"ship module '{module.moduleId}' has an unnamed contribution.", out error);
                if (!componentIds.Add(contribution.itemId))
                    return Invalid(
                        $"ship module '{module.moduleId}' contributes '{contribution.itemId}' more than once.",
                        out error);
                if (contribution.contributed < 0)
                    return Invalid(
                        $"ship module '{module.moduleId}' has a negative contribution for '{contribution.itemId}'.",
                        out error);
            }
        }

        if (!TryValidateCounters(data.lifetimeStats.counters, "lifetime statistics", out error))
            return false;
        if (data.lifetimeStats.totalPlaySeconds < 0)
            return Invalid("lifetime play time cannot be negative.", out error);

        if (!TryValidateCounters(data.launchResult.statsSnapshot, "launch result", out error))
            return false;
        if (!data.launchResult.recorded && data.launchResult.completedUtcTicks != 0)
            return Invalid("an unrecorded launch result carries a completion time.", out error);
        if (data.launchResult.totalPlaySeconds < 0 || data.launchResult.creditsAtCompletion < 0 ||
            data.launchResult.factoriesOwned < 0)
            return Invalid("the launch result contains a negative quantity.", out error);

        error = null;
        return true;
    }

    private static bool TryValidateCounters(List<LifetimeCounter> counters, string owner, out string error)
    {
        if (counters == null)
            return Invalid($"{owner} counters are missing.", out error);
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (LifetimeCounter counter in counters)
        {
            if (counter == null || string.IsNullOrWhiteSpace(counter.id))
                return Invalid($"{owner} contains an unnamed counter.", out error);
            if (!ids.Add(counter.id))
                return Invalid($"{owner} counter '{counter.id}' appears more than once.", out error);
            if (counter.value < 0)
                return Invalid($"{owner} counter '{counter.id}' is negative.", out error);
        }
        error = null;
        return true;
    }

    private static bool Invalid(string message, out string error)
    {
        error = message;
        return false;
    }

    private static void TryDelete(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
