# Sailor's Rest — working notes for Claude

Unity 6.5 (6000.5), URP 2D, uGUI, new Input System. Cosy pixel-art fishing + merge game for Steam (landscape, mouse + controller, Steam Deck-ready). Code lives in `Assets/SailorsRest/`; see its `README.md` for the folder map and where tuning data lives.

## Principles

**KISS — keep it simple.**
- Prefer the plain solution that works today: a `switch`, a small table, a `MonoBehaviour` with three fields. No frameworks, DI containers, event buses or service locators unless a real problem demands one.
- One class, one job. If a method needs a comment to explain its flow, split it into named steps instead.
- Build UI in code with `UiKit` the way the existing screens do; don't introduce UI Toolkit or prefabs for one screen.

**DRY — don't repeat yourself.**
- Before writing a helper, search for one: `UiKit` (UI elements, chips, title plates), `UiStyle` (margins, font sizes), `SortOrder`, `Palette`, `PixelArtImport`, `GameBalance` getters (`MaxCastDistance`, `Pricing`, `ScaleFor`…).
- Repeated knowledge goes in one place: tuning numbers in `GameBalance` / `Species_*` / `WorldArt`, UI metrics in `UiStyle` or the screen's `Layout` block, sprite file names in `[SpriteFile]` attributes.
- Repetition of *code shape* is fine when the meaning differs; don't merge two things just because they look alike.

**YAGNI — you aren't gonna need it.**
- Build what the current task needs. No hooks, abstract base classes, config options or "future-proof" parameters for features nobody asked for.
- Don't keep fallbacks for states that can't happen (the art is in the project; missing sprites log a warning, they don't need placeholder code).
- Delete dead code instead of commenting it out.

## Project rules

- **No magic numbers.** Gameplay values → `GameBalance` (or `FishSpecies` per tier) with a `[Tooltip]`. Scenery placement → `WorldArt`. UI sizes → the view's nested `static class Layout` or shared `UiStyle`. Rule constants → named `const` in the rule class. `0`, `1`, loop bounds and halves for centring are fine.
- **Rules stay pure.** `Scripts/Rules` (assembly `SailorsRest.Rules`) has no `UnityEngine` references and is covered by `Tests/EditMode`. New game logic goes there first, with a test.
- **Don't rename serialized fields** on `GameBalance`, `FishSpecies`, `WorldArt`, `UiSkin` without saying so — tuned values in the `.asset` files are lost.
- **Scenes are generated.** After changing scene wiring, run **Sailor's Rest → Build All Scenes**; don't hand-edit `Scenes/*.unity`.
- **Save compatibility.** `PlayerProgress` is saved with `JsonUtility`; adding fields is fine, renaming or removing breaks existing saves.
- **Art** comes from SpriteCook (style guide asset `a4727edc-b54e-4234-8f25-0844753165a9`, project "Sailor's Rest — Steam landscape fishing UI"). World art is listed in `Art/SpriteCook/World/world-art.json` and pulled by `WorldArtDownloader`.
- **No per-frame allocations in `Update`.** Screens format text only when the value changes (see `HudView`'s `shown*` fields and depth label table); fish loops use `List` `foreach`, no LINQ or lambdas. Events (a sale, a catch, a toast) may allocate.
- **Verify before handing back:** code compiles with zero warnings, EditMode tests pass, and the changed scene works in Play mode.

## Skills to use

Load the matching skill before starting work in its area.

| Area | Skill |
| --- | --- |
| Code review, naming, cohesion, design patterns | `unity-perf:code-standards` |
| Frame drops, GC spikes, profiling, draw calls | `unity-perf:unity-performance` |
| Making a hot path allocation-free (fish updates, HUD `Update`) | `unity-perf:csharp-zero-gc` |
| Pixel-perfect camera, sprite import, blurry or shimmering pixels | `unity:2d-pixel-perfect` |
| HUD / Map / Market work (this project uses uGUI) | `unity:ui` → `unity:ui-ugui` |
| Sprite pivots, borders, slicing | `unity:sprite-editor` |
| 9-slice borders for UI panels and frames (`UiSkin` art) | `unity:sprite-segment-3x3grid` |
| Importing SpriteCook sprites, sheets, animations or AI audio into the project | `ai-assets-to-unity` |
| Sprite atlases before shipping | `unity:manage-sprite-atlas` |
| Tile-based rooms for house decorating (only once that feature starts) | `unity:tilemap-palette-create`, `unity:tilemap-ruletile-createfromsegment` |
| Finding assets or objects in the editor by type, label or name | `unity:generate-editor-search-query` |
| Post-processing (bloom, colour grading for golden hour) | `unity:urp-postprocessing` |
| Driving the editor, builds, tests and logs from the command line | `unity:unity-cli` |
| Adding or upgrading packages | `unity:unity-package-management` |
| Audio (when added): mixer routing, import settings | `unity:audio-setup-mixers`, `unity:optimize-audio` |
| Translating the game | `unity:localization` |
| Generating sprites, animations, tilesets, UI kits | `spritecook:spritecook-workflow-essentials` plus `spritecook:spritecook-generate-sprites`, `-animate-assets`, `-generate-tilesets` or `-build-ui-kits` |
