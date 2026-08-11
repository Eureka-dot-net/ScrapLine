using System;
using System.Collections.Generic;

/// <summary>
/// Ordered, idempotent migrations for persisted GameData. Add one method and one switch case for each
/// future schema version; never rewrite or remove an older migration.
/// </summary>
public static class GameSaveMigrations
{
    public const int CurrentSchemaVersion = 1;

    public static GameData Migrate(GameData data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        if (data.schemaVersion < 0)
            throw new InvalidOperationException($"Save schema version {data.schemaVersion} is invalid.");
        if (data.schemaVersion > CurrentSchemaVersion)
            throw new InvalidOperationException(
                $"Save schema version {data.schemaVersion} is newer than supported version {CurrentSchemaVersion}.");

        while (data.schemaVersion < CurrentSchemaVersion)
        {
            switch (data.schemaVersion)
            {
                case 0:
                    MigrateUnversionedToVersion1(data);
                    break;
                default:
                    throw new InvalidOperationException($"No migration exists for schema version {data.schemaVersion}.");
            }
        }

        NormalizeOptionalFields(data);
        return data;
    }

    private static void MigrateUnversionedToVersion1(GameData data)
    {
        // Version 0 is the original unversioned format. It already contains the factory state; this
        // migration only supplies fields that old JsonUtility payloads may omit.
        NormalizeOptionalFields(data);
        data.schemaVersion = 1;
    }

    private static void NormalizeOptionalFields(GameData data)
    {
        data.grids ??= new List<GridData>();
        data.userMachineProgress ??= new List<UserMachineProgress>();
        data.wasteQueue ??= new List<string>();
        if (data.wasteQueueLimit <= 0)
            data.wasteQueueLimit = 3;

        foreach (GridData grid in data.grids)
        {
            if (grid == null)
                continue;
            grid.cells ??= new List<CellData>();
            foreach (CellData cell in grid.cells)
            {
                if (cell == null)
                    continue;
                cell.items ??= new List<ItemData>();
                cell.waitingItems ??= new List<ItemData>();
                cell.sortingConfig ??= new SortingMachineConfig();
                cell.requiredCrateId ??= "starter_crate";
                if (string.IsNullOrWhiteSpace(cell.machineDefId) && cell.cellType == UICell.CellType.Blank)
                {
                    cell.machineDefId = cell.cellRole == UICell.CellRole.Top
                        ? "blank_top"
                        : cell.cellRole == UICell.CellRole.Bottom ? "blank_bottom" : "blank";
                }
                if (cell.wasteCrate != null)
                    cell.wasteCrate.remainingItems ??= new List<WasteCrateItemDef>();
            }
        }
    }
}
