# Save schema

Current version: **4** (`GameSaveMigrations.CurrentSchemaVersion`).

Schema 4 is a structural reservation, not a gameplay change. It gives the offline clock, the four
factory sites, the shared warehouse, the ship blueprint, lifetime statistics and the immutable
launch result a persisted home, so each of those systems can be implemented later without another
structural migration. None of them are implemented yet.

## Ownership

| Scope | State | Lives on |
| --- | --- | --- |
| Global | Credits | `GameData.credits` |
| Global | Machine licences and upgrade levels | `GameData.userMachineProgress` |
| Global | Objective progress | `GameData.objectiveProgress` |
| Global | Shared warehouse inventory | `GameData.warehouse` |
| Global | Ship blueprint and module contributions | `GameData.shipBlueprint` |
| Global | Lifetime statistics | `GameData.lifetimeStats` |
| Global | First-completion snapshot | `GameData.launchResult` |
| Global | Session and wall clock anchors | `GameData.savedAtRuntimeTime`, `GameData.savedAtUtcTicks`, `GameData.highWaterUtcTicks` |
| Per factory | Grid dimensions, cells, placed machines, machine configuration, queued scrap, in-flight items | each `GameData.grids[n]` (`GridData`) |

Machine licences and credits are global by product decision: one balance, and buying a licence
unlocks that machine at every site.

## Factory sites

A site exists exactly when a `GridData` carrying its ID is present. There is no separate "owned"
flag. `GridData.factoryId` is stable and never reassigned; the schema 3 to 4 migration gives the
single pre-existing grid the fixed constant `FactorySiteConfiguration.DefaultFactoryId`
(`factory_primary`), deliberately **not** the first entry of the configurable plan, so retuning the
plan can never rename an existing player's factory.

`GridData.siteIndex` is derived from list position on every normalization, so it cannot desync.

### Configuration

The site plan is data, in `Assets/Resources/factorysites.json`, loaded by `ResourceManager` into
`FactorySiteConfiguration`:

| Field | Default | Meaning |
| --- | --- | --- |
| `maxSiteCount` | 4 | Planned cap for acquiring new factories; existing saved factories remain valid if this is retuned downward |
| `initialWidth` / `initialHeight` | 5 / 7 | Size a new site starts at |
| `maxWidth` / `maxHeight` | 8 / 10 | Largest size a site may be expanded to |
| `sites[]` | 4 entries | Ordered plan of `id`, `displayName`, `unlockedByDefault` |

Built-in defaults are always valid, so the save system never depends on resource load order. A
missing or unparseable file degrades to a single 5x7 site rather than blocking startup.
`GridManager.defaultGridWidth` / `defaultGridHeight` remain as a per-scene debug override and are
used only when set above zero.

Enforcing `maxWidth` / `maxHeight` during expansion belongs to the expansion pricing work;
schema 4 only records the planned ceiling and exposes
`FactorySiteConfiguration.IsWithinSiteLimits`.

## Clocks

Two anchors exist and they are not interchangeable.

`savedAtRuntimeTime` / `hasRuntimeClockAnchor` track Unity's `Time.time`, which restarts at zero
every launch. They rebase in-flight item timers across a load and are required by the live
simulation. They cannot measure offline time.

`savedAtUtcTicks` / `highWaterUtcTicks` / `hasUtcClockAnchor` are wall clock, written on every save.
`highWaterUtcTicks` is monotonic: it only ever moves forward. When the device clock reads below it,
the saved-at anchor stays at the high-water value too. Offline progress is intended to be
measured forward from `savedAtUtcTicks` and withheld whenever the device clock reads below the high
water mark, so winding the clock back cannot manufacture elapsed time. A save migrated from
schema 3 has no anchor, so the first resume credits nothing and its next save establishes one.

Offline rewards themselves are out of scope here; only the anchors they will read are persisted.

## Normalization and validation rules

Normalization runs after every migration and on every load and save, and is idempotent:
normalizing twice produces byte-identical JSON. Each rule below is applied in
`GameSaveMigrations`; the matching assertion lives in `GameSaveStorage.TryValidate`.

| Situation | Rule |
| --- | --- |
| Duplicate factory ID across grids | **Fail validation.** Two grids claiming one site is ambiguous and cannot be merged without guessing. |
| Duplicate ship module ID | **Fail validation.** Same reason; merging would over- or under-credit progress. |
| More grids than the current `maxSiteCount` | **Preserve.** The setting gates future acquisition; retuning content must not invalidate factories already owned. |
| Duplicate warehouse slot, contribution or counter for one ID | **Merge** by saturating addition, then sort by ID. |
| Null record, or blank ID | **Drop.** |
| Negative count, contribution or counter | **Clamp to zero** (before merging). |
| Negative warehouse `capacityLimit` | **Collapse** to the single sentinel `-1`, meaning unlimited. |
| UTC ticks outside `[0, DateTime.MaxValue.Ticks]` | **Clamp** into range. |
| `highWaterUtcTicks` below `savedAtUtcTicks` | **Raise** the high water mark to the saved-at value. |
| `savedAtUtcTicks` of zero | **Clear** `hasUtcClockAnchor` so the next save establishes it. |
| Completion time on an incomplete module | **Zero** the timestamp. |
| Launch result with `recorded == false` | **Blank** every field, so a partial snapshot cannot read as a completed run. |

A validation failure is not data loss: `GameSaveStorage` falls back to the validated backup
generation, and the atomic temp-file commit from issue #65 is unchanged.

### Immutability of the launch result

`LaunchResultData` is reserved to be written once, the first time the ship is completed, and then
left unchanged during sandbox play. Schema 4 validates and normalizes its persisted shape; the
future ship-completion writer must enforce that write-once transition when that gameplay is added.

### Extensibility

Lifetime statistics are a keyed list (`LifetimeCounter`), not fixed fields, so a new statistic costs
a key in `LifetimeStatKeys` rather than a migration. Unknown keys are preserved. All quantities are
`long`, because an idle game accumulates past `int` range.

`WarehouseData.capacityLimit` exists but is `-1` (unlimited) by product decision, so introducing a
limit later is a value change rather than a schema change.

Ship modules carry no ordering or prerequisite field: once the blueprint unlocks, every module is
visible and completable in any order.

## Tests

`Assets/Tests/EditMode/SaveSchemaV4Tests.cs` covers migration from schema 3 and from unversioned
saves, default factory identity, idempotency, every normalization rule above, the validation
failures, round trips, and the configurable site plan. `GameSaveSystemTests.cs` continues to cover
backup recovery, interrupted writes, autosave behaviour and runtime rehydration.

Run them with the batch command in `ScrapLine/TESTING.md`.
