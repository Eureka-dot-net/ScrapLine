# Stable recipe identities

Every recipe in `Assets/Resources/recipes.json` has an authored `id`. The ID describes the recipe
concept and does not include ingredient quantities, output quantities, values, display text, or
process timing, so normal balance changes do not invalidate saved configuration.

Current IDs are:

| Machine | Recipe ID |
| --- | --- |
| Shredder | `shred_can` |
| Granulator | `granulate_plastic_bottle` |
| Plate Press | `press_aluminum_plate` |
| Fabricator | `fabricate_reinforced_aluminum_plate` |

`FactoryRegistry.GetRecipeById` is the identity and lookup boundary. Blank, unknown, and duplicate
IDs do not resolve. `TryGetRecipeForMachine` additionally verifies that a selected recipe belongs to
the configured machine. Registry callers receive a read-only recipe collection and must not construct
identity from inputs or outputs.

`CellData.selectedRecipeId` stores the authored ID without changing the save shape or schema. When a
Fabricator is reconstructed with an old generated or otherwise unknown ID, it clears that selection,
returns to an unconfigured idle state, preserves existing and waiting item records, and logs an
actionable request to select a recipe again. It never substitutes the first recipe. A blank new
Fabricator likewise remains unconfigured until the player explicitly chooses a recipe.

The content validator rejects missing, blank, and duplicate recipe IDs in addition to its existing
machine, item, count, output, multiplier, and economy validation.
