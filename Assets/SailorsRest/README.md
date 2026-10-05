# Sailor's Rest — code layout

Press **Sailor's Rest → Build All Scenes** once, then press Play. The game starts on the Map.

Loop: **Map → Shore Pond** (fish until sunset; catches go into your creel) **→ Harbour Market** (sell the creel, upgrade rod / hook / line / reel) **→ Map**. Progress is saved to `sailors-rest-save.json` in `Application.persistentDataPath`; **Sailor's Rest → Reset Save** wipes it.

| Folder | What lives there |
| --- | --- |
| `Scripts/Rules` | Pure game rules, no Unity types: merging, tier drops and prices (`MergeRules`), line tension, the hooking ring, the power bar, the day, the save data (`PlayerProgress`), gear (`GearCatalog`) and selling (`MarketRules`). Own assembly `SailorsRest.Rules`. |
| `Scripts/Runtime` | Unity side. Gameplay: `FishingController` (state machine), `FishingInput`, `FishSpawner`/`FishAgent`, `QteRingView`, `CameraFollow`, `SceneDressing`/`Parallax`. Screens: `HudView`, `MapView`, `MarketView`, built with the `UiKit` builder and `UiStyle` metrics. `GameSession` owns the save and scene changes. |
| `Scripts/Editor` | Scene builder (`SailorsRestSetup`), world-art downloader, UI-kit importer and their shared import settings. |
| `Tests/EditMode` | Unit tests for the rules. Window → General → Test Runner. |
| `Data` | Created by the builder the first time. Tune here, not in code: rebuilding scenes keeps your values. |

Where the numbers live:

- **`GameBalance`** — every gameplay number: lake size, casting and power-bar speed, hook movement, hooking ring, reeling and tension, wild-fish movement, gear effects, the golden-fish price bonus.
- **`Species_*`** — per-species depth, spawn weight, price per kg, and per-tier ring arcs, needle speed, surges and weights.
- **`GearCatalog`** (code, Rules) — gear names, effects and upgrade prices, as one table.
- **`WorldArt`** — Shore Pond scenery placement (backdrops, waterline, weeds, dock, fisherman, rod).
- **`UiSkin`** — the UI-kit sprites. Fields carry `[SpriteFile]`, so **Refresh Sprites From SpriteCook** re-links them by file name.
- **`UiStyle`** (code) — shared UI metrics: margins, font sizes, chip sizes, border thinning. Each screen keeps its own layout in a nested `Layout` class.

Controls: hold left mouse / A / Space to charge, release to cast; move the hook with the mouse, WASD or the left stick; click / A on green to hook; hold to reel and steer into same-species, same-tier fish to merge; right mouse / B / R reels back. Esc / M / Start goes back to the map while aiming (Esc is swallowed by the editor; use M there). At sunset: click / A for the market, right mouse / B for the map. In the market, Esc / B goes back to the map. The map has no Esc shortcut; use its Quit button to leave the game.

## Idle animations — everything moves

**SpriteCook loops** (`Art/SpriteCook/Anim`, listed in `anim-art.json`): fisherman + cat, fishmonger, perch / carp / catfish swim, lake-bed weeds, the pier (lantern, rope, sign), and the two map icons. They download and slice themselves when Unity reloads scripts, then get wired into `WorldArt` and the `Species_*` assets — just press Play. Menu: **Sailor's Rest → Download Idle Animations** (force re-download) and **Re-slice Idle Animations**. Frame 0 of each loop is lined up with its still sprite, so the size and pivot match. A loop that is missing falls back to the still sprite.

**Free code idles** (no art, tune counts in `WorldArt → Code idles`):

| Where | What | Script |
| --- | --- | --- |
| Shore Pond sky | warm motes / fireflies drifting over the treeline | `PixelParticles` |
| Under water | swaying, fading sunbeams | `LightRays` |
| Under water | bubbles rising from the lake bed | `PixelParticles` |
| Waterline | glints winking along the surface + slow alpha shimmer of the band | `PixelParticles`, `AlphaPulse` |
| Bobber | gentle swing (rotation only — the controller owns its position) | `IdleMotion` |
| Market | dust motes in the golden-hour light; gear icons bob and twinkle | `UiIdle`, `IdleMotion` |
| Map | glints on the sea; locked spots bob | `UiIdle`, `IdleMotion` |

`SpriteFlipbook` plays any `Sprite[]` on a SpriteRenderer or UI Image; `IdleMotion` adds sway / bob / breathe to any transform; `PixelParticles` works in the world or on a canvas.
