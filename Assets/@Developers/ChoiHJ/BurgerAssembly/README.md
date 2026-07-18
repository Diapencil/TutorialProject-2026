# SheepSheep Burger Cooking Prototype

Unity version: `6000.3.19f1` (Unity 6.3 LTS)

This module implements only the cooking part defined by the v1.1 cooking prototype specification. Counter, customer, order scoring, day progression, and rewards are intentionally outside this module.

## Play

1. Open `Assets/@Developers/ChoiHJ/BurgerAssembly/Scenes/BurgerAssembly.unity`.
2. Enter Play Mode. The grill zone is the default camera position.
3. Drag raw meat dough from the infinite tray to the grill.
4. Tap the dough to flatten it and start first-side cooking.
5. Wait 3 seconds, tap when the patty becomes flippable, then wait another 3 seconds.
6. Drag the completed patty to the right edge. The camera moves to the board zone; drop the patty anywhere on the board.
7. Drag buns and toppings from the infinite board tray. Toppings are limited to eight; buns, patties, and sauce stamps are excluded from that limit.
8. Drag a sauce container across the board. A sauce stamp is recorded every 10 screen pixels.
9. Drop the top bun anywhere on the board. Completion fires immediately, even when the bottom bun or other ingredients are missing.

Swipe at least 20% of the screen width to move between grill and board zones. The camera transition uses a same-scene smooth tween. Pointer movement below 5 pixels remains a tap.

## Confirmed rules

- First-side cook time: 3 seconds
- Second-side cook time: 3 seconds
- Done to overcooked: 5 unattended seconds
- Overcooked patties cannot be dragged to the board
- Sauce stamp spacing: 10 screen pixels
- Maximum toppings on board: 8
- Completion trigger: top bun drop
- Cooking does not reject missing ingredients or invalid assembly order

## Runtime architecture

- `BurgerAssemblyController` builds and coordinates the shape-based prototype UI.
- `CookingCameraSlider` handles swipe thresholds and same-scene camera tweening.
- `CookingTrayDragSource` keeps tray originals in place and creates drag visuals.
- `CookablePattyView` presents the timed `PattyGrillState` model.
- `PlacedIngredientView` supports free placement and repositioning before completion.
- `BurgerAssemblyState` enforces only the topping limit and completion lock.
- `BurgerData` and `IngredientPlacement` capture type, local board position, and layer order.
- `OnBurgerCompleted` publishes the completed `BurgerData`; the prototype also logs its JSON to the Console.
- `BurgerAssemblySceneBuilder` safely preserves the current editor scene setup while rebuilding and verifying the prototype scene.

## Editor verification

Use `Sheep Sheep Burger > Build Cooking Prototype Scene` to rebuild the scene and run model/interface assertions. The command asks to save modified scenes and restores the previously open scene setup after completion.

## Art integration notes

The current prototype uses `SimpleShapeGraphic` so gameplay can be verified without production assets. Replace generated shapes with prefabs/sprites while retaining the state and interaction classes. Before a mobile build, replace the operating-system font fallback with a packaged Korean TextMeshPro font asset.

See `OPEN_SOURCE_REFERENCES.md` for reviewed open-source projects and licensing notes.
