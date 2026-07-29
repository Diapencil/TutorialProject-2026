# SheepSheep Burger Cooking Prototype

Unity version: `6000.3.19f1` (Unity 6.3 LTS)

This module implements the cooking, assembly, and temporary packaging flow defined by the v1.1 cooking prototype specification. Counter, customer, order scoring, day progression, and rewards are intentionally outside this module.

## Play

1. Open `Assets/@Developers/ChoiHJ/BurgerAssembly/Scenes/BurgerAssembly.unity`.
2. Enter Play Mode. The grill zone is the default camera position.
3. Slide to the board, place the bottom bun at the desired stack position, then return to the grill.
4. Choose patty, bacon, or egg from the grill tray and drag it to the grill. Only one grill item is active at a time.
5. For a patty, tap the dough to flatten it and start cooking. Bacon starts cooking on the first tap; egg cooks on one side for 3 seconds.
6. Patty and bacon cook for 3 seconds per side. Tap within the five-second flip window, then wait another 3 seconds after flipping.
7. Drag the completed patty, bacon, or egg to the right edge. The camera moves to the board zone, where it snaps above the bottom bun.
8. Drop toppings onto the board; each one snaps above the current burger stack.
9. Click ketchup or mustard to enter sauce mode. Hold the pointer on the board and move it to draw repeated strokes; click the selected sauce container again to return to the default pointer.
10. Drop the top bun to complete the burger.
11. Drag the completed burger to the right edge, keep holding it while the camera slides, and drop that same burger object inside the central packaging tray. Then use the packaging button on the right.

Swipe at least 20% of the screen width to move through the same-scene `grill ↔ board ↔ packaging` zones at any time. The page strip uses a smooth tween without moving or reconfiguring the scene camera. Pointer movement below 5 pixels remains a tap. The packaging button stays disabled until the completed burger is physically dropped inside the packaging tray.

## Confirmed rules

- Patty and bacon first-side cook time: 3 seconds
- Patty and bacon flip window after first-side cooking: 5 seconds
- Patty and bacon second-side cook time: 3 seconds
- Egg cook time: 3 seconds, no flip
- Done to overcooked: 5 unattended seconds
- Overcooked patties, bacon, and eggs cannot be dragged to the board
- Sauce stamp spacing: 10 screen pixels
- Sauce strokes near the completed burger are attached to its stack; distant sauce remains on the board
- Maximum toppings on board: 8
- Completion trigger: top bun drop after a bottom bun exists
- Bottom bun anchors automatic ingredient stacking; placed ingredients are locked
- Completed burger transfer target: the same-scene packaging page
- Packaging page access: always available
- Packaging transfer preserves and reparents the original burger stack; it does not create a preview copy

## Runtime architecture

- `BurgerAssemblyController` coordinates cooking, drag transfer, page navigation, and completion flow.
- `BurgerSauceDrawingController` owns sauce-tool selection, batched stroke drawing, and near-burger attachment.
- `SauceStrokeGraphic` batches every point in one stroke into a single UI mesh.
- `BurgerAssemblyViewBuilder` creates the three-zone runtime UI and returns its typed references.
- `BurgerStackAssembler` owns ingredient stacking, board bounds, assembly state, and result snapshots.
- `BurgerCompletionPublisher` stores the latest result, logs JSON, and preserves the public completion event.
- `BurgerPrototypePresentation` contains the prototype theme, ingredient visuals, and shared UI factory.
- `CookingCameraSlider` handles three-zone swipe thresholds and page-strip tweening without taking over `Camera.main`.
- `CookingTrayDragSource` keeps tray originals in place and creates drag visuals.
- `CookableGrillItemView` presents patty, bacon, and egg through the timed `PattyGrillState` model.
- `PlacedIngredientView` captures locked, automatically stacked ingredient positions.
- `BurgerAssemblyState` enforces the bottom-bun prerequisite, topping limit, and completion lock.
- `BurgerData` and `IngredientPlacement` capture type, local board position, and layer order.
- `OnBurgerCompleted` publishes the completed `BurgerData`; the prototype also logs its JSON to the Console.
- `BurgerPackagingController` accepts the original burger stack on its tray and enables packaging only after a valid drop.
- `BurgerAssemblySceneBuilder` safely preserves the current editor scene setup while rebuilding and verifying the prototype scene.

## Editor verification

Use `Sheep Sheep Burger > Build Unified Cooking Scene` to rebuild the unified scene and run model/interface assertions. The command asks to save modified scenes and restores the previously open scene setup after completion.

## Art integration notes

The supplied cooking art is stored in `Assets/@Developers/ChoiHJ/BurgerAssembly/Sprites/ProvidedArt` and is connected through serialized `BurgerSpriteCatalog` references. Runtime `Resources.Load` calls and string asset paths are not used. The grill uses the supplied patty, bacon, and egg raw/cooked/burnt images; the board uses the supplied top bun, lettuce, tomato, onion, pickle, and jalapeno images and their pile variants; packaging displays the supplied completed-burger image. Cheese, the bottom bun, ketchup, and mustard keep the existing project art because no replacement image was supplied. `shop_ui.png` is imported for future shop work but is not placed in the cooking scene. Custom ingredient sprites render with a white UI tint so their source pixels are used unchanged. Before a mobile build, replace the operating-system font fallback with a packaged Korean TextMeshPro font asset.

See `OPEN_SOURCE_REFERENCES.md` for reviewed open-source projects and licensing notes.
