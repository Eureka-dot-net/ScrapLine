# Unity Frontend Integration Guide - Spawner Scrap Delivery

The main scene is already wired for the current spawner flow. This document describes the expected UI
contract for future prefab or scene work.

## Spawner panel

Selecting a spawner opens an operational panel. It displays:

- the active bale sprite;
- remaining active-bale contents;
- the three-slot delivery queue;
- a compact **Order** button;
- a **Close** button.

The panel must not expose a Config tab, scrap filter, or required scrap type. Spawners accept mixed
deliveries and process them in order.

## Delivery shop

The shop is opened for one concrete `SpawnerMachine`. Each offer shows the bale name, contents, and
credit price. An offer is disabled when the player cannot afford it or that spawner's three delivery
slots are full.

After a purchase, refresh both credits and the target spawner's queue. Closing the shop returns to the
same spawner panel.

## Scene references

- `SpawnerConfigPanel` owns the active-bale and queue display.
- `WasteCrateConfigPanel` owns the shop offers.
- `WasteSupplyManager` needs the scene's `CreditsManager`.
- The spawner panel needs a reference to the delivery shop.

Use the existing shared configuration-panel buttons: while the spawner panel is open, its confirm button
is presented as **Order** and its cancel button as **Close**. The active-bale area is a passive status
display, not a configuration button; it shows the bale name, remaining/total contents, percentage, and a
non-interactive fullness meter.

## Hands-on test

1. Start a new game with 280 credits and place a spawner.
2. Confirm it receives the one-time free Can Bale.
3. Open it and order two different bale types.
4. Confirm only that spawner shows the deliveries, in purchase order.
5. Place another spawner and confirm it starts empty.
6. Move the first spawner and confirm its active bale and queue remain attached.
7. Delete it and confirm unopened deliveries are fully refunded while its active bale is not.
8. Save and load with multiple spawners and confirm every active bale and queue remains distinct.

Automated ownership, capacity, FIFO, persistence, and refund coverage lives in
`Assets/Tests/EditMode/SpawnerOwnershipTests.cs`.
