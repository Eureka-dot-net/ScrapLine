# Machine licenses

Machine placement capability is owned by `FactoryRegistry`. Gameplay and UI callers use
`IsMachineUnlocked`, `TryPurchaseMachineLicense`, or `TryGrantMachineLicense`; they do not infer
license state from objectives, recipes, prices, or UI state.

## Content authoring

Every machine shown in the build panel declares:

- `unlockedByDefault: true` and `unlockCost: 0` for clean-save starter tools.
- `unlockedByDefault: false` and a positive `unlockCost` for a purchasable permanent license.
- `cost` separately defines the price of constructing one licensed machine.

Current playtest prices are:

| Machine | License | Construction | First access total |
| --- | ---: | ---: | ---: |
| Shredder | 100 | 100 | 200 |
| Granulator | 125 | 125 | 250 |
| Sorter | 150 | 150 | 300 |
| Plate Press | 300 | 250 | 550 |
| Fabricator | 600 | 500 | 1,100 |

The content validator rejects missing/non-positive locked license costs, positive costs on default
licenses, and license costs on hidden machine definitions. Adding a purchasable machine requires only
content data and its normal machine implementation—not machine-specific build-bar logic.

## Purchase and grant flow

`TryPurchaseMachineLicense(machineId, creditsManager, out error)` validates the ID and content price,
rejects duplicates, checks affordability, deducts the exact license price, grants the permanent
license, emits `MachineUnlocked(machineId, "credit_purchase")`, and requests autosave. Failure leaves
both credits and license state unchanged; the transaction also includes a defensive refund if a future
grant rule fails after deduction.

`TryGrantMachineLicense(machineId, unlockSource, out error)` performs the same authoritative state
transition without charging credits. Objectives, migration/recovery tools, and developer tooling use
this path with an explicit source.

The underlying progress list is private. `UserMachines` and `FindMachineProgress` expose detached
snapshots, so callers cannot mutate license or upgrade state outside registry APIs.

## UI and enforcement

The build bar shows every buildable machine. Licensed previews show only the construction price in a
compact bottom badge. Locked previews are greyed across the full card and show only the permanent
license price; tapping once changes that amount into a concise purchase confirmation. A successful
purchase refreshes the bar immediately and replaces the grey license treatment with the construction
price badge.

Selection and every placement path independently enforce registry license state, so stale selections,
drag payloads, debug calls, and future interfaces cannot place an unlicensed machine.

## Save and migration behavior

Only licensed machines are persisted in `GameData.userMachineProgress`; absence means unlicensed.
Schema 2 adds Conveyor, Spawner, and Seller as permanent starter licenses. During migration, any
progression machine already placed by an older sandbox save is licensed so the player is not stranded.
Primary and backup save candidates both receive semantic machine-ID validation before selection.
