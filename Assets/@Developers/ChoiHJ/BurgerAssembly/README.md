# SheepSheep Burger Cooking Prototype

Unity version: `6000.3.19f1` (Unity 6.3 LTS)

This module implements the cooking, assembly, temporary packaging, and cooking-result data flow defined by the v1.1 cooking prototype specification. Counter, customer simulation, day progression, debt, shop, decoration, and upgrades are intentionally outside this module.

## Play

1. Open `Assets/Scenes/BurgerAssembly.unity`.
2. Enter Play Mode. The grill zone is the default camera position.
3. Slide to the board and place any ingredients freely. Click a tray ingredient for automatic placement, or drag it to choose an exact position. A bottom bun is only required when you want to start a burger stack.
4. Choose patty, bacon, or egg from the grill tray. Click to add it to an open grill slot, or drag it to the grill for exact placement. Multiple grill items can be active at once.
5. For a patty, tap the dough to flatten it and start cooking. Bacon starts cooking on the first tap; egg cooks on one side for 3 seconds.
6. Patty and bacon cook for 3 seconds per side. Tap within the five-second flip window, then wait another 3 seconds after flipping.
7. Drag a raw, partially cooked, completed, or burnt patty, bacon, or egg to the right edge. The camera moves to the board zone and preserves its cooking state.
8. Drop ingredients near the bottom bun to add them in a top-down layout. Central drops keep their exact position, edge drops receive a small inward correction, and distant drops remain loose. Consecutive ingredients of the same type share a layer; a `1 -> 2 -> 1` sequence creates three layers. Drag grill ingredients back through the left edge to resume cooking.
9. Click ketchup or mustard to enter sauce mode. Hold the pointer on the board and move it to draw repeated strokes; click the selected sauce container again to return to the default pointer.
10. Drop the top bun to complete the burger.
11. Drag the completed burger to the right edge, keep holding it while the camera slides, and drop that same burger object inside the central packaging tray. Then use the packaging button on the right.

Drag horizontally to move continuously across the same kitchen panorama. The view follows the pointer and remains exactly where it is released instead of snapping to `grill`, `board`, or `packaging` pages. Those names now identify interaction regions only. Pointer movement below 5 pixels remains a tap, and the packaging button stays disabled until the completed burger is physically dropped inside the packaging tray.

## Confirmed rules

- Patty and bacon first-side cook time: 3 seconds
- Patty and bacon flip window after first-side cooking: 5 seconds
- Patty and bacon second-side cook time: 3 seconds
- Egg cook time: 3 seconds, no flip
- Done to overcooked: 5 unattended seconds
- Raw, partially cooked, completed, and overcooked grill ingredients can move between the grill and board
- Sauce stamp spacing: 10 screen pixels
- Sauce strokes near the completed burger are attached to its stack; distant sauce remains on the board
- Maximum toppings on board: 8
- Completion trigger: top bun drop after a bottom bun exists
- Bottom bun anchors top-down, proximity-based stacking; distant ingredients remain loose and placed ingredients remain movable
- Drops inside 65% of the bun radius keep their position; accepted edge drops move 28 pixels inward
- Consecutive duplicate ingredients share one layer, while duplicates separated by another type create a new layer
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
- `CookingSceneDataSchema` defines the cooking-only `IngredientData`, ordered `RecipeLayer`/`RecipeData`, `GradeConfig`, and `PaymentResult` records from the economy schema.
- `CookingSceneSchema` owns prototype-local ingredient costs, cooking ranges, recipe lookup, grade selection, and payment calculation without creating global day/economy state.
- `BurgerPrototypePresentation` contains the prototype theme, ingredient visuals, and shared UI factory.
- `CookingCameraSlider` handles three-zone swipe thresholds and page-strip tweening without taking over `Camera.main`.
- `CookingTrayDragSource` keeps tray originals in place and creates drag visuals.
- `CookableGrillItemView` presents patty, bacon, and egg through the timed `PattyGrillState` model.
- `PlacedIngredientView` supports repeat dragging, loose/stacked placement, cooking-state transfer, and result capture.
- `BurgerAssemblyState` enforces the bottom-bun prerequisite, topping limit, and completion lock.
- `BurgerData` and `IngredientPlacement` capture type, local board position, and layer order.
- `OnBurgerCompleted` publishes the completed `BurgerData`; `OnPaymentCalculated` publishes its grade, paid base price, tip, ingredient cost, net income, and attack flag. The latest values are also available through `LastCompletedBurger` and `LastPaymentResult`, and both records are logged as JSON.
- `BurgerPackagingController` accepts the original burger stack on its tray and enables packaging only after a valid drop.
- `BurgerAssemblySceneBuilder` safely preserves the current editor scene setup while rebuilding and verifying the prototype scene.

## Editor verification

Use `Sheep Sheep Burger > Build Unified Cooking Scene` to rebuild the unified scene and run model/interface assertions. The command asks to save modified scenes and restores the previously open scene setup after completion.

## Art integration notes

The supplied cooking art is stored in `Assets/Sprites/ProvidedArt` and is connected through serialized `BurgerSpriteCatalog` references. Runtime `Resources.Load` calls and string asset paths are not used. The grill uses the supplied patty, bacon, and egg raw/cooked/burnt images; while a patty is cooking, the six cropped frames from `IMG_0610.GIF` loop beneath the unchanged patty Sprite at the source frame timing. The board uses the supplied bottom bun, top bun, lettuce, tomato, cheese, onion, pickle, and jalapeno images. Onion, pickle, and jalapeno use their grouped top-down art in the tray and the single-piece art when placed on the board; their pile variants remain available in the Sprite catalog. Ketchup and mustard use their placement-version images in the tray, and their click-version bottles follow the pointer over the board while sauce mode is selected. Packaging displays the supplied completed-burger image. `shop_ui.png` is imported for future shop work but is not placed in the cooking scene. Custom ingredient sprites render with a white UI tint so their source pixels are used unchanged. Before a mobile build, replace the operating-system font fallback with a packaged Korean TextMeshPro font asset.

The shared kitchen-station background is stored in `Sprites/Environment/kitchen_station_reference.png`. Runtime renders this Sprite exactly once as one aspect-preserved, continuously draggable panorama. Manual dragging follows the pointer and stops at the released position without snapping to three page stops. Grill, assembly, and packaging remain logical interaction regions parented to the same panorama, so the background and controls travel together without duplicating the image.

See `OPEN_SOURCE_REFERENCES.md` for reviewed open-source projects and licensing notes.
