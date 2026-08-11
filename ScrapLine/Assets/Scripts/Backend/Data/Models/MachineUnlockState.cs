using System;
using System.Collections.Generic;

/// <summary>
/// Defines the clean-save machine licenses and normalizes persisted license state.
/// Only licensed machines are stored; an absent entry is unlicensed.
/// </summary>
public static class MachineUnlockState
{
    public const string ConveyorId = "conveyor";
    public const string SpawnerId = "spawner";
    public const string SellerId = "seller";

    private static readonly string[] CleanSaveMachineIds =
    {
        ConveyorId,
        SpawnerId,
        SellerId
    };

    public static List<UserMachineProgress> CreateCleanSaveProgress()
    {
        List<UserMachineProgress> progress = new List<UserMachineProgress>(CleanSaveMachineIds.Length);
        foreach (string machineId in CleanSaveMachineIds)
            progress.Add(CreateUnlocked(machineId));
        return progress;
    }

    public static void Normalize(GameData data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        Dictionary<string, UserMachineProgress> progressById =
            new Dictionary<string, UserMachineProgress>(StringComparer.Ordinal);
        if (data.userMachineProgress != null)
        {
            foreach (UserMachineProgress progress in data.userMachineProgress)
            {
                if (progress == null || string.IsNullOrWhiteSpace(progress.machineId))
                    continue;

                if (!progressById.TryGetValue(progress.machineId, out UserMachineProgress existing))
                {
                    progressById.Add(progress.machineId, new UserMachineProgress
                    {
                        machineId = progress.machineId,
                        unlocked = progress.unlocked,
                        upgradeLevel = Math.Max(0, progress.upgradeLevel)
                    });
                    continue;
                }

                existing.unlocked |= progress.unlocked;
                existing.upgradeLevel = Math.Max(existing.upgradeLevel, Math.Max(0, progress.upgradeLevel));
            }
        }

        foreach (string machineId in CleanSaveMachineIds)
            EnsureUnlocked(progressById, machineId);

        // Older sandbox saves may already contain progression machines. Preserve access so loading
        // cannot strand a placed machine that the player was previously allowed to use.
        if (data.grids != null)
        {
            foreach (GridData grid in data.grids)
            {
                if (grid?.cells == null)
                    continue;
                foreach (CellData cell in grid.cells)
                {
                    if (cell == null || cell.cellType != UICell.CellType.Machine ||
                        string.IsNullOrWhiteSpace(cell.machineDefId))
                        continue;
                    EnsureUnlocked(progressById, cell.machineDefId);
                }
            }
        }

        List<string> machineIds = new List<string>();
        foreach (KeyValuePair<string, UserMachineProgress> entry in progressById)
        {
            if (entry.Value.unlocked)
                machineIds.Add(entry.Key);
        }
        machineIds.Sort(StringComparer.Ordinal);
        data.userMachineProgress = new List<UserMachineProgress>(machineIds.Count);
        foreach (string machineId in machineIds)
            data.userMachineProgress.Add(progressById[machineId]);
    }

    private static void EnsureUnlocked(
        IDictionary<string, UserMachineProgress> progressById,
        string machineId)
    {
        if (!progressById.TryGetValue(machineId, out UserMachineProgress progress))
        {
            progressById.Add(machineId, CreateUnlocked(machineId));
            return;
        }
        progress.unlocked = true;
    }

    private static UserMachineProgress CreateUnlocked(string machineId)
    {
        return new UserMachineProgress
        {
            machineId = machineId,
            unlocked = true,
            upgradeLevel = 0
        };
    }
}
