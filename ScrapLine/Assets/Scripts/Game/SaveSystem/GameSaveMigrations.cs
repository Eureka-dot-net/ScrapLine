using System;
using System.Collections.Generic;

/// <summary>
/// Ordered, idempotent migrations for persisted GameData. Add one method and one switch case for each
/// future schema version; never rewrite or remove an older migration.
///
/// NORMALIZATION CONTRACT
/// ----------------------
/// Normalization runs after every migration and on every load and save, and must be idempotent:
/// normalizing twice produces byte-identical JSON. The documented rules are
///
///   Identity collisions FAIL validation. Two factories claiming one site ID, or two module progress
///   records claiming one module ID, are ambiguous and are rejected by GameSaveStorage rather than
///   silently merged. Normalization never creates a duplicate, so this only fires on external damage.
///
///   Quantity collisions MERGE by saturating addition. Repeated warehouse slots, repeated component
///   contributions and repeated lifetime counters for the same ID are summed, then the collection is
///   sorted by ID so the result is stable.
///
///   Malformed records are DROPPED. Null entries, and entries with a missing or blank ID, are removed.
///
///   Negative quantities CLAMP to zero. Counters, contributions and warehouse counts never go below
///   zero. Warehouse capacity is the one exception: any negative value normalizes to the single
///   "unlimited" sentinel, <see cref="WarehouseData.UnlimitedCapacity"/>.
///
///   Impossible timestamps CLAMP into range. UTC tick fields are held within
///   [0, <see cref="SaveStateLimits.MaxUtcTicks"/>], the wall-clock high-water mark is raised to at
///   least the saved-at value so it can never run backwards, and a zero saved-at clears the anchor
///   flag so the next resume re-establishes it instead of crediting time from the epoch.
///
///   An unrecorded launch result is BLANKED, so a partially written snapshot cannot masquerade as a
///   completed run. Recorded snapshots are normalized without changing their recorded flag; the
///   future ship-completion writer owns the write-once lifecycle rule.
/// </summary>
public static class GameSaveMigrations
{
    public const int CurrentSchemaVersion = 4;

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
                case 1:
                    MigrateVersion1ToVersion2(data);
                    break;
                case 2:
                    MigrateVersion2ToVersion3(data);
                    break;
                case 3:
                    MigrateVersion3ToVersion4(data);
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

    private static void MigrateVersion1ToVersion2(GameData data)
    {
        MachineUnlockState.Normalize(data);
        data.schemaVersion = 2;
    }

    private static void MigrateVersion2ToVersion3(GameData data)
    {
        data.objectiveProgress ??= new List<ObjectiveProgressData>();
        data.schemaVersion = 3;
    }

    /// <summary>
    /// Schema 4 reserves structure for the offline clock, four factory sites, the shared warehouse,
    /// the ship blueprint, lifetime statistics and the immutable launch result. No gameplay changes:
    /// the single existing grid simply becomes the default factory and every new state object is
    /// seeded empty.
    /// </summary>
    private static void MigrateVersion3ToVersion4(GameData data)
    {
        data.grids ??= new List<GridData>();

        // The pre-multi-site save has exactly one factory. Its ID is the fixed constant rather than
        // the first entry of the configurable site plan, so retuning that plan can never rename an
        // existing player's factory.
        for (int index = 0; index < data.grids.Count; index++)
        {
            GridData grid = data.grids[index];
            if (grid == null || !string.IsNullOrWhiteSpace(grid.factoryId))
                continue;
            grid.factoryId = index == 0
                ? FactorySiteConfiguration.DefaultFactoryId
                : SiteIdForIndex(index);
        }

        data.warehouse ??= new WarehouseData();
        data.shipBlueprint ??= new ShipBlueprintData();
        data.lifetimeStats ??= new LifetimeStatsData();
        data.launchResult ??= new LaunchResultData();

        // A pre-v4 save has no trustworthy wall-clock reading. Leaving the anchor unset means the
        // first resume establishes it and credits no offline time, rather than crediting everything
        // since the epoch.
        data.hasUtcClockAnchor = false;
        data.savedAtUtcTicks = 0;
        data.highWaterUtcTicks = 0;

        data.schemaVersion = 4;
    }

    private static void NormalizeOptionalFields(GameData data)
    {
        data.grids ??= new List<GridData>();
        data.userMachineProgress ??= new List<UserMachineProgress>();
        data.objectiveProgress ??= new List<ObjectiveProgressData>();
        foreach (ObjectiveProgressData objective in data.objectiveProgress)
        {
            if (objective != null)
                objective.processedEventIds ??= new List<string>();
        }
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
                cell.wasteDeliveryQueue ??= new List<string>();
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

        if (data.schemaVersion >= 2)
            MachineUnlockState.Normalize(data);

        if (data.schemaVersion >= 4)
        {
            NormalizeFactoryIdentity(data);
            NormalizeClockAnchors(data);
            NormalizeWarehouse(data);
            NormalizeShipBlueprint(data);
            NormalizeLifetimeStats(data);
            NormalizeLaunchResult(data);
        }
    }

    // -----------------------------------------------------------------------------------------
    // Factory identity
    // -----------------------------------------------------------------------------------------

    private static void NormalizeFactoryIdentity(GameData data)
    {
        for (int index = 0; index < data.grids.Count; index++)
        {
            GridData grid = data.grids[index];
            if (grid == null)
                continue;

            if (string.IsNullOrWhiteSpace(grid.factoryId))
            {
                grid.factoryId = index == 0
                    ? FactorySiteConfiguration.DefaultFactoryId
                    : SiteIdForIndex(index);
            }

            // Position in the list is the authority for site index, so reordering cannot desync it.
            grid.siteIndex = index;

            if (string.IsNullOrWhiteSpace(grid.displayName))
            {
                FactorySiteDef site = FactorySiteConfiguration.FindSite(grid.factoryId);
                grid.displayName = site != null ? site.displayName : grid.factoryId;
            }
        }
    }

    /// <summary>
    /// Site ID for a grid position, falling back to a synthesized stable ID when the configured plan
    /// is shorter than the number of factories actually present in the save.
    /// </summary>
    private static string SiteIdForIndex(int index)
    {
        return FactorySiteConfiguration.SiteIdAt(index) ?? $"factory_site_{index}";
    }

    // -----------------------------------------------------------------------------------------
    // Clock anchors
    // -----------------------------------------------------------------------------------------

    private static void NormalizeClockAnchors(GameData data)
    {
        data.savedAtUtcTicks = ClampTicks(data.savedAtUtcTicks);
        data.highWaterUtcTicks = ClampTicks(data.highWaterUtcTicks);

        // The high-water mark is monotonic by definition; a save claiming otherwise is repaired
        // upward so a rolled-back device clock can never be read as elapsed time.
        if (data.highWaterUtcTicks < data.savedAtUtcTicks)
            data.highWaterUtcTicks = data.savedAtUtcTicks;

        if (data.savedAtUtcTicks == 0)
            data.hasUtcClockAnchor = false;
    }

    private static long ClampTicks(long ticks)
    {
        if (ticks < 0)
            return 0;
        return ticks > SaveStateLimits.MaxUtcTicks ? SaveStateLimits.MaxUtcTicks : ticks;
    }

    // -----------------------------------------------------------------------------------------
    // Warehouse
    // -----------------------------------------------------------------------------------------

    private static void NormalizeWarehouse(GameData data)
    {
        data.warehouse ??= new WarehouseData();
        WarehouseData warehouse = data.warehouse;

        // Any negative value means the same thing; collapse to the single sentinel so the field
        // round-trips identically.
        if (warehouse.capacityLimit < 0)
            warehouse.capacityLimit = WarehouseData.UnlimitedCapacity;

        warehouse.slots ??= new List<WarehouseSlot>();
        Dictionary<string, long> totals = new Dictionary<string, long>(StringComparer.Ordinal);
        List<string> order = new List<string>();
        foreach (WarehouseSlot slot in warehouse.slots)
        {
            if (slot == null || string.IsNullOrWhiteSpace(slot.itemId))
                continue;
            long count = Math.Max(0L, slot.count);
            if (totals.TryGetValue(slot.itemId, out long existing))
            {
                totals[slot.itemId] = SaturatingAdd(existing, count);
                continue;
            }
            totals.Add(slot.itemId, count);
            order.Add(slot.itemId);
        }

        order.Sort(StringComparer.Ordinal);
        List<WarehouseSlot> normalized = new List<WarehouseSlot>(order.Count);
        foreach (string itemId in order)
            normalized.Add(new WarehouseSlot { itemId = itemId, count = totals[itemId] });
        warehouse.slots = normalized;
    }

    // -----------------------------------------------------------------------------------------
    // Ship blueprint
    // -----------------------------------------------------------------------------------------

    private static void NormalizeShipBlueprint(GameData data)
    {
        data.shipBlueprint ??= new ShipBlueprintData();
        ShipBlueprintData blueprint = data.shipBlueprint;

        blueprint.unlockedUtcTicks = ClampTicks(blueprint.unlockedUtcTicks);
        if (!blueprint.unlocked)
            blueprint.unlockedUtcTicks = 0;

        blueprint.modules ??= new List<ShipModuleProgress>();
        List<ShipModuleProgress> modules = new List<ShipModuleProgress>(blueprint.modules.Count);
        foreach (ShipModuleProgress module in blueprint.modules)
        {
            if (module == null || string.IsNullOrWhiteSpace(module.moduleId))
                continue;

            module.contributions = NormalizeContributions(module.contributions);
            module.completedUtcTicks = ClampTicks(module.completedUtcTicks);
            if (!module.completed)
                module.completedUtcTicks = 0;
            modules.Add(module);
        }

        // Modules are a set, not a sequence: every module is freely completable in any order, so a
        // stable sort by ID keeps the persisted form deterministic without implying progression.
        modules.Sort((left, right) => string.CompareOrdinal(left.moduleId, right.moduleId));
        blueprint.modules = modules;
    }

    private static List<ShipComponentContribution> NormalizeContributions(
        List<ShipComponentContribution> contributions)
    {
        Dictionary<string, long> totals = new Dictionary<string, long>(StringComparer.Ordinal);
        List<string> order = new List<string>();
        if (contributions != null)
        {
            foreach (ShipComponentContribution contribution in contributions)
            {
                if (contribution == null || string.IsNullOrWhiteSpace(contribution.itemId))
                    continue;
                long amount = Math.Max(0L, contribution.contributed);
                if (totals.TryGetValue(contribution.itemId, out long existing))
                {
                    totals[contribution.itemId] = SaturatingAdd(existing, amount);
                    continue;
                }
                totals.Add(contribution.itemId, amount);
                order.Add(contribution.itemId);
            }
        }

        order.Sort(StringComparer.Ordinal);
        List<ShipComponentContribution> normalized = new List<ShipComponentContribution>(order.Count);
        foreach (string itemId in order)
            normalized.Add(new ShipComponentContribution { itemId = itemId, contributed = totals[itemId] });
        return normalized;
    }

    // -----------------------------------------------------------------------------------------
    // Lifetime statistics and launch result
    // -----------------------------------------------------------------------------------------

    private static void NormalizeLifetimeStats(GameData data)
    {
        data.lifetimeStats ??= new LifetimeStatsData();
        LifetimeStatsData stats = data.lifetimeStats;
        stats.firstPlayedUtcTicks = ClampTicks(stats.firstPlayedUtcTicks);
        stats.totalPlaySeconds = Math.Max(0L, stats.totalPlaySeconds);
        stats.counters = NormalizeCounters(stats.counters);
    }

    private static void NormalizeLaunchResult(GameData data)
    {
        data.launchResult ??= new LaunchResultData();
        LaunchResultData result = data.launchResult;

        if (!result.recorded)
        {
            // Never leave a half-written snapshot that could later read as a completed run.
            result.completedUtcTicks = 0;
            result.totalPlaySeconds = 0;
            result.factoriesOwned = 0;
            result.creditsAtCompletion = 0;
            result.statsSnapshot = new List<LifetimeCounter>();
            return;
        }

        result.completedUtcTicks = ClampTicks(result.completedUtcTicks);
        result.totalPlaySeconds = Math.Max(0L, result.totalPlaySeconds);
        result.factoriesOwned = Math.Max(0, result.factoriesOwned);
        result.creditsAtCompletion = Math.Max(0L, result.creditsAtCompletion);
        result.statsSnapshot = NormalizeCounters(result.statsSnapshot);
    }

    private static List<LifetimeCounter> NormalizeCounters(List<LifetimeCounter> counters)
    {
        Dictionary<string, long> totals = new Dictionary<string, long>(StringComparer.Ordinal);
        List<string> order = new List<string>();
        if (counters != null)
        {
            foreach (LifetimeCounter counter in counters)
            {
                if (counter == null || string.IsNullOrWhiteSpace(counter.id))
                    continue;
                long value = Math.Max(0L, counter.value);
                if (totals.TryGetValue(counter.id, out long existing))
                {
                    totals[counter.id] = SaturatingAdd(existing, value);
                    continue;
                }
                totals.Add(counter.id, value);
                order.Add(counter.id);
            }
        }

        order.Sort(StringComparer.Ordinal);
        List<LifetimeCounter> normalized = new List<LifetimeCounter>(order.Count);
        foreach (string id in order)
            normalized.Add(new LifetimeCounter { id = id, value = totals[id] });
        return normalized;
    }

    /// <summary>Adds without wrapping; an idle game's totals must saturate rather than go negative.</summary>
    private static long SaturatingAdd(long left, long right)
    {
        long sum = unchecked(left + right);
        if (((left ^ sum) & (right ^ sum)) < 0)
            return long.MaxValue;
        return sum;
    }
}
