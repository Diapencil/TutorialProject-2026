# Burger Assembly Prototype

Unity version: `6000.3.19f1` (Unity 6.3 LTS)

## Play

1. Open `Assets/@Developers/ChoiHJ/BurgerAssembly/Scenes/BurgerAssembly.unity`.
2. Enter Play Mode.
3. Drag the bottom bun from the left onto the cutting board to start.
4. Drag ingredients from the right and sauces from the bottom onto the cutting board.
5. Drag a raw patty onto the grill, press the cook button, then drag the cooked patty onto the cutting board. Raw patties are rejected by the cutting board.
6. Drag the top bun from the left onto the cutting board to finish and evaluate the burger.

The current order is a classic burger: ketchup, patty, cheese, lettuce, and tomato. Recipe matching checks exact ingredient counts but allows the player to stack the middle layers in any order.

## Implementation notes

- `BurgerAssemblyState` owns the start/add/finish/reset state machine.
- `PattyGrillState` enforces empty/raw/cooked grill transitions.
- `BurgerRecipe` compares the completed ingredient multiset with recipe definitions.
- `BurgerAssemblyController` creates the temporary shape-based UI at runtime.
- `DraggableBurgerItem` and the board/grill drop zones handle pointer drag-and-drop.
- `SimpleShapeGraphic` renders rectangles, circles, and triangles without external image assets.
- `BurgerAssemblySceneBuilder` creates the scene and runs model assertions.

## Public repositories reviewed

These repositories were reviewed for general architecture only. Their code and assets were not copied because no license file was present when checked.

- https://github.com/himanshu-nag/Unity_Pizza_Cooking_Game
- https://github.com/AyaFayed/Unity-Kitchen-Game
- https://github.com/mperez132/Simmer-Unity
