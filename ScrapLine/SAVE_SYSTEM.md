# Save system

ScrapLine saves `GameData` as versioned JSON in `Application.persistentDataPath`. The current schema is
defined by `GameSaveMigrations.CurrentSchemaVersion`; new games are created with
`GameData.CreateNewGame()`.

## File safety

Each save uses three generations in the same directory:

- `game_data.json.tmp` is written and deserialized again before it can become primary.
- `game_data.json` is the current validated save.
- `game_data.json.bak` is the previous validated generation (or a copy of the first generation).

The temporary file atomically replaces the primary where the platform supports `File.Replace`. The
fallback moves the valid primary to backup before installing the temporary file, so an interruption
still leaves a loadable generation. An invalid primary is quarantined as `game_data.json.corrupt` and
a valid backup is used and repaired automatically. An ordinary primary load does not rewrite the save,
so it also does not rotate an identical primary over the previous backup generation. Rewrites happen
only after migration or backup recovery.

Candidate selection includes machine-unlock semantic validation as well as structural JSON validation.
An unknown machine in the primary therefore falls through to a valid backup, and the semantic-invalid
primary is quarantined rather than rotated over that good backup during repair.

## Migrations

Unversioned saves are schema 0. `GameSaveMigrations` upgrades them to schema 1 by normalizing optional
collections and per-cell configuration, then to schema 2 by normalizing machine unlock state. Schema 2
adds the three starter capabilities and preserves access to machines already placed by older sandbox
saves. Grids, machines, items, credits, queues, upgrades, and existing unlocks are retained. Migrations
are idempotent.

For a future schema change:

1. Increment `CurrentSchemaVersion`.
2. Add exactly one migration from the previous version.
3. Add its switch case without changing historical migrations.
4. Add fixtures proving preservation and repeated-migration safety.

Saves from a newer unsupported schema are rejected rather than partially loaded.

## Runtime restoration

Items are persisted with stable IDs. After JSON deserialization, processor waiting queues are rebound
to the canonical item objects owned by grid cells. This preserves the shared object identity expected
by processing and movement code and rejects saves whose waiting queues reference missing items.

Runtime timers carry a saved clock anchor. Loading rebases movement, processing, and waiting timestamps
onto the current `Time.time` clock, preserving elapsed durations across an application restart. Legacy
saves without an anchor reconstruct movement from saved progress and safely restart processing/waiting
intervals from the current runtime clock.

## Autosave and lifecycle

Meaningful changes request a debounced autosave. The first dirty event schedules a write after two
seconds; further events are coalesced rather than extending the deadline, so continuous production is
saved periodically without writing for every item.

A failed autosave is rescheduled with bounded exponential backoff (five seconds initially, up to one
minute by default). It therefore cannot retry serialization, disk I/O, and logging once per frame when
storage is unavailable.

`OnApplicationPause(true)` flushes immediately and is the authoritative mobile background behavior.
`OnApplicationQuit` performs a best-effort desktop save, but mobile platforms are not expected to
deliver a reliable quit callback.

`GameManager.ResetGame()` is the controlled reset path: it removes primary, temporary, backup, and
quarantined files, rebuilds the default factory state, restores the opening budget/supply, and writes a
new validated save immediately.
