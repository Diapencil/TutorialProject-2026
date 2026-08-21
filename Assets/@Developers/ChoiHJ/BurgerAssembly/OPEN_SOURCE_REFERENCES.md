# Open-source references

The cooking prototype was implemented from the SheepSheep Burger planning documents. No external source code or art assets were copied into this project.

## BurgerPanic

- Repository: https://github.com/Sergueille/BurgerPanic
- License: MIT
- Relevance: Unity 2D burger cooking, generated ingredients, draggable physical objects, independent steak-side cooking progress, sauce drops, and coordinate-based burger evaluation.
- Patterns reviewed: create a new ingredient when interacting with a persistent generator; keep cooking state on the patty; represent sauce as individual placed records; evaluate a finished burger from the placed objects rather than blocking invalid cooking actions.
- Adaptation: this project uses original UI/event-system code, deterministic 3-second state transitions, an eight-topping cap, 10-pixel sauce stamps, and a top-bun completion event required by the v1.1 specification.

## VRChef

- Repository: https://github.com/dyanikoglu/VRChef
- License: Apache-2.0
- Relevance: separates food manipulation/cooking actions from a broader recipe/progress system.
- Pattern reviewed: keep cooking interactions independently reusable so later counter/order logic can consume the result without controlling the cooking step.

## Burger Builder

- Repository: https://github.com/lewdev/burger-builder
- License: MIT
- Relevance: touch-friendly burger-building loop across mobile and desktop resolutions.
- Pattern reviewed: keep the assembly interaction simple and input-first while allowing scoring/progression to remain a separate layer.

Repositories without an explicit reusable license were not used as implementation sources.
