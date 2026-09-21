# Spawner Scrap Delivery System

## Player flow

- A spawner has no scrap-type configuration or filter.
- Selecting a spawner shows its active bale and upcoming deliveries.
- **Order Scrap** opens the delivery shop for that specific spawner.
- An order is paid for immediately and belongs only to the selected spawner.
- Each spawner can hold one active bale and three unopened deliveries.
- Mixed bale types are allowed and activate in first-in, first-out order.
- New spawners begin empty; the player deliberately purchases their first delivery.

Moving a spawner preserves its active bale and delivery queue. Deleting a spawner fully refunds its
unopened deliveries; any partially consumed active bale is discarded without a refund.

## Data ownership

Delivery state is stored on the spawner's `CellData`:

```csharp
public WasteCrateData wasteCrate;
public List<string> wasteDeliveryQueue;
```

There is intentionally no factory-wide scrap queue and no required-crate configuration.

## Runtime responsibilities

- `SpawnerMachine` activates deliveries and emits items from the active bale.
- `WasteSupplyManager` validates targeted orders, charges credits, and refunds
  unopened deliveries when a spawner is deleted.
- `SpawnerConfigPanel` is an operational status panel with an **Order Scrap** action.
- `WasteCrateConfigPanel` is the shop and always requires a target spawner.
- The normal save system persists the active bale and each spawner's queue.

Crate definitions and prices live in `Assets/Resources/wastecrates.json`. The authoritative starting
economy and current crate catalog are documented in `BALANCE_BASELINE.md`.

## Verification checklist

- Orders appear only on the selected spawner.
- Every newly placed spawner begins empty until scrap is ordered.
- Purchasing a Can Bale for the first spawner charges credits and adds it to that spawner only.
- Three unopened deliveries are accepted; a fourth is rejected.
- Mixed deliveries activate in purchase order.
- Save/load preserves active contents and queue order.
- Moving preserves deliveries.
- Deleting refunds unopened deliveries but not the active bale.
