# Objective progression

Objectives are optional guidance and rewards. They observe completed gameplay actions; they never authorize or block machine placement, licenses, recipes, scrap delivery, or grid expansion.

## Authored data

`Assets/Resources/objectives.json` owns the representative objective chain. Every definition has a stable `id`, display copy, an event `type`, stable `targetId`, positive `targetValue`, one reward, and optional presentation metadata. `prerequisiteObjectiveId` controls which suggestion is shown next, not which objectives can record progress. This means a player who chooses a later strategy early does not lose that progress.

Supported event types are item count/value sold, machine placed, machine licensed, recipe completed, scrap delivery ordered, and grid expanded. Supported rewards are credits and a free machine license. The editor content validator checks IDs, references, values, rewards, missing prerequisites, and prerequisite cycles.

## Runtime authority and events

`ProgressionManager` is the sole writer for objective progress and reward-claim state. It exposes read-only snapshots plus progress, completion, claim, and reload notifications. `GameplayDomainEvents` is a reporting bridge used after authoritative domain operations succeed:

- Seller sale and credit award
- Paid machine creation (not drag/move)
- Machine license transition through `FactoryRegistry`
- Processor/fabricator recipe completion
- Accepted paid or starter scrap delivery
- Successful grid expansion mutation

Each action supplies a stable event identity. The manager remembers identities per objective so a replay cannot count twice. Rewards are manually claimed and idempotent. Free licenses use `FactoryRegistry.TryGrantMachineLicense` with source `objective_reward:<objective-id>`.
Advisory subscribers are isolated from one another so a UI/tutorial/analytics listener failure cannot interrupt the gameplay action that published the event or prevent the progression save request.

## Save and recovery

Schema version 3 adds `objectiveProgress`, including progress, completed/claimed state, and processed event IDs. Older development saves migrate with clean objective state. Objective state participates in the same semantic candidate selection as machine licenses: an invalid primary generation is rejected and a valid backup is tried before a new game is considered. Both `GameManager.ResetGame()` (full save wipe) and `GameManager.ResetGrid()` (the in-game Reset button, which clears the grid, credits, and purchased/granted machine licenses) now call `ProgressionManager.ResetForNewGame` so objective state resets alongside the grid instead of surviving stale from the previous run.

A `machine_license` objective tracks persistent game state (owning a license) rather than a one-time transaction, so it cannot rely on replaying a domain event alone: re-purchasing an already-owned license is rejected outright, and licenses are not reverted by a plain progression reset. `ProgressionManager` reconciles every freshly created objective state against the current license state, so it self-corrects in both directions: an objective for a machine the player already owns (a migrated save predating the objective, or licenses that survive a caller that resets progression without also revoking them) completes immediately and becomes claimable without a new purchase, while `GameManager.ResetGrid()` revoking licenses before resetting progression correctly returns the objective to incomplete so it can be earned again.

## UI

The compact runtime panel shows the current recommendation, progress and reward, announces completion, and presents a Claim button. Completing or claiming an objective refreshes it immediately. It uses the same rounded-card visual language and Orbitron typeface as the machine configuration panels (dark navy fill, lighter frame, orange title, Orbitron SemiBold for headings/labels and Orbitron Regular for body copy — both moved to `Assets/Resources/Fonts/` so they can be loaded at runtime) and sits anchored to the top-right of the HUD, below the credits/pause row. A small muted 'OPTIONAL' eyebrow tag sits above the objective's own name, rather than being prefixed onto it, so the title itself stays short and readable. It can be collapsed down to just its header via a toggle in the top-right corner; the collapsed/expanded preference is remembered locally (PlayerPrefs) across sessions, and it starts collapsed by default so it never covers the grid until the player opens it. A small badge stays visible on the collapsed header when a reward is ready to claim. The Claim button uses the same neutral panel-button styling as the rest of the UI (e.g. the config panel confirm/order buttons).

## Deliberate scope decisions

The representative chain grants credits rather than machines so rewards do not remove player choice. The framework supports free machine-license rewards. A generic free-scrap reward is not included because scrap ownership is per spawner; choosing a target spawner silently would violate that ownership model. It can be added later with an explicit player target flow.
