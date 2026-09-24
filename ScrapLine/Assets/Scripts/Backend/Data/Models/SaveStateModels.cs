using System;
using System.Collections.Generic;

/// <summary>
/// Serializable state objects introduced by save schema 4. They reserve structure for the offline
/// clock, the shared warehouse, the ship blueprint, lifetime statistics and the immutable launch
/// result, so those systems can be implemented later without another structural migration.
///
/// Ownership: every type in this file is GLOBAL (one instance per save, alongside credits and
/// machine licences). Per-factory state lives on <see cref="GridData"/>.
///
/// All quantities are <c>long</c> because an idle game accumulates well past <c>int</c> range.
/// </summary>
public static class SaveStateLimits
{
    /// <summary>Largest representable UTC tick value; anything beyond this is not a real date.</summary>
    public static readonly long MaxUtcTicks = DateTime.MaxValue.Ticks;
}

// ---------------------------------------------------------------------------------------------
// Warehouse
// ---------------------------------------------------------------------------------------------

/// <summary>One item type held in the shared warehouse.</summary>
[Serializable]
public sealed class WarehouseSlot
{
    public string itemId;
    public long count;
}

/// <summary>
/// The shared, cross-factory warehouse. Unlimited for now by product decision, but the capacity
/// field exists so a limit can be introduced later as a value change rather than a schema change.
/// </summary>
[Serializable]
public sealed class WarehouseData
{
    /// <summary>Sentinel for "no limit". Any value &gt;= 0 is a real capacity.</summary>
    public const long UnlimitedCapacity = -1;

    public long capacityLimit = UnlimitedCapacity;
    public List<WarehouseSlot> slots = new List<WarehouseSlot>();

    public bool IsUnlimited => capacityLimit < 0;
}

// ---------------------------------------------------------------------------------------------
// Ship blueprint
// ---------------------------------------------------------------------------------------------

/// <summary>Quantity of one component contributed toward a module.</summary>
[Serializable]
public sealed class ShipComponentContribution
{
    public string itemId;
    public long contributed;
}

/// <summary>
/// Progress toward one ship module. Modules carry no ordering or prerequisite field: once the
/// blueprint is unlocked every module is visible and may be completed in any order.
/// </summary>
[Serializable]
public sealed class ShipModuleProgress
{
    public string moduleId;
    public List<ShipComponentContribution> contributions = new List<ShipComponentContribution>();
    public bool completed;
    public long completedUtcTicks;
}

/// <summary>
/// The ship blueprint. It replaces the objective chain as the guidance surface once unlocked;
/// requirements themselves are content, so only player progress is persisted here.
/// </summary>
[Serializable]
public sealed class ShipBlueprintData
{
    public bool unlocked;
    public long unlockedUtcTicks;
    public List<ShipModuleProgress> modules = new List<ShipModuleProgress>();
}

// ---------------------------------------------------------------------------------------------
// Lifetime statistics
// ---------------------------------------------------------------------------------------------

/// <summary>
/// One named lifetime counter. Statistics are a keyed list rather than fixed fields so a new
/// statistic costs a key, not a migration.
/// </summary>
[Serializable]
public sealed class LifetimeCounter
{
    public string id;
    public long value;
}

/// <summary>Well-known <see cref="LifetimeCounter.id"/> values. Unknown keys are preserved.</summary>
public static class LifetimeStatKeys
{
    public const string ItemsProduced = "items_produced";
    public const string ItemsSold = "items_sold";
    public const string CreditsEarned = "credits_earned";
    public const string CreditsSpent = "credits_spent";
    public const string ScrapItemsPurchased = "scrap_items_purchased";
    public const string MachinesPlaced = "machines_placed";
    public const string OfflineSecondsCredited = "offline_seconds_credited";
    public const string ShipComponentsInstalled = "ship_components_installed";
}

/// <summary>Lifetime totals that survive for the whole save and never decrease.</summary>
[Serializable]
public sealed class LifetimeStatsData
{
    public long firstPlayedUtcTicks;
    public long totalPlaySeconds;
    public List<LifetimeCounter> counters = new List<LifetimeCounter>();
}

// ---------------------------------------------------------------------------------------------
// Launch result
// ---------------------------------------------------------------------------------------------

/// <summary>
/// Snapshot reserved for the first ship completion. The future completion writer must populate it
/// only while <see cref="recorded"/> is false, then leave it unchanged during later sandbox play.
/// Schema validation guarantees a complete, normalized shape; write-once lifecycle enforcement is
/// intentionally deferred with the ship-completion gameplay that owns the transition.
/// </summary>
[Serializable]
public sealed class LaunchResultData
{
    public bool recorded;
    public long completedUtcTicks;
    public long totalPlaySeconds;
    public int factoriesOwned;
    public long creditsAtCompletion;
    public List<LifetimeCounter> statsSnapshot = new List<LifetimeCounter>();
}
