# GunGame Arena — Addendum: in-map settings panel

Date: 2026-09-20 (approved in discussion)
Extends: `2026-09-19-gungame-arena-design.md` (which listed an in-game UI as out of scope; this addendum brings it in).

## Goal

A settings panel in every GunGame map, standing beside GunGame's own "More options" panel, that edits the plugin's most-used config values with the VR laser pointer. No map edits: the panel is built at runtime and anchored to GunGame's panel, so it appears on every GunGame map automatically.

## Runtime facts (verified 2026-09-20)

- GunGame's option panel scripts are `MonoBehaviourSingleton<T>` singletons; `MonoBehaviourSingleton<GunGame.Scripts.Options.GameSettings>.Instance` lives on the "More options" panel. `Instance.GetComponentInParent<Canvas>()` is that panel's world-space canvas.
- H3VR pointer: `FVRViveHand` raycasts (`PointingLayerMask`, triggers included) and requires an `FVRPointable` on the collider's GameObject. GunGame's buttons are: `RectTransform` 230×30, `Image` (built-in `UISprite`, white), `Button` (ColorTint: normal white, highlighted 0.96, pressed 0.78), `FVRPointableButton` (`MaxPointingRange` 600, `ColorUnselected` white, `ColorSelected` 0.96), `BoxCollider` size 230×30×1, layer Default, child `Text` (Arial, size 23, colour 0.196 grey). `FVRPointableButton.OnPoint` invokes `Button.onClick` on trigger down.
- GunGame canvases: `RenderMode.WorldSpace`, local scale 0.01 (1 unit = 1 cm).

## Design

### Placement
`PanelInstaller` listens to `SceneManager.sceneLoaded`, then polls once per second (up to 20 s) for `GameSettings.Instance`. When found: `Canvas host = Instance.GetComponentInParent<Canvas>()`. Our canvas gets `host.transform.rotation`, `host.transform.lossyScale`, and position `host.transform.position - host.transform.right * (hostWidth + 0.12f)` where `hostWidth = host RectTransform.rect.width × lossyScale.x` (metres). Falls back to 0.7 m when the rect is degenerate. If `GameSettings.Instance` never appears (not a GunGame map) nothing is built. Destroyed with the scene.

### Look
Canvas 620×760 units (cm), scale 0.01. Background `Image` colour `#2F5FD6` at 0.92 alpha (GunGame blue). Title "Arena" 44 pt bold white. Rows 52 units tall from y = −90 downward. Fonts: Arial built-in; button text 23 pt grey 0.196 (GunGame style); labels 26 pt white; helper notes 17 pt white at 0.85 alpha, wrapped under their row.

### Rows (top to bottom)
| Row | Left label | Controls | Notes |
|---|---|---|---|
| Mode | `Mode` | `<` `[Free For All]` `>` cycles Off → Free For All → Teams | applies at next Start Game |
| Teams | `Teams` | `<` `2` `>` (2..4) | Teams mode only; dimmed otherwise |
| Allies | `Allies` | `<` `Auto` `>` (Auto = −1, then 0..9) | Teams mode only; dimmed otherwise |
| Leaderboard | toggle button `Leaderboard: ON/OFF` | | applies immediately (HUD shown/hidden if a round is running) |
| Spread spawns | toggle | | immediate |
| Grudges | toggle | note: "(each sosig fights 3 rivals at a time, not everyone — keeps FFA from being one blob)" | immediate for new re-rolls |
| Hunters | toggle | note: "(every 10–20 s a few sosigs are sent toward you — without this an FFA mostly ignores you)" | immediate |
| Hunter pressure | `<` `25%` `>` steps of 5 % in 0..100 | | immediate |
| Skill tiers | toggle | | new spawns only |
| Footer | text: "Mode, Teams and Allies apply at the next Start Game. Everything saves to the config file." | | |

Toggle buttons show state in their label. Dimmed rows set text alpha 0.4 and keep buttons working.

### Behaviour
- Every control writes straight to the `ArenaConfig` entry (`ConfigEntry.Value = …`), which BepInEx persists to `shaha.GunGameArena.cfg` immediately.
- Pure logic (labels, cycling, stepping, clamping, which rows are dimmed) lives in Core `PanelModel` and is unit-tested. The plugin `ArenaPanel` only builds UI and forwards clicks.
- `LeaderboardHud` gains `public static void SetEnabledLive(bool)`: hides the HUD if visible and disabling; shows it if a round is active and enabling.
- All click handlers and the installer poll are try/caught and log; a failure never throws into the game.

### Out of scope
Tier multipliers, HUD geometry, names seed, reroll/hunter intervals: config-file only.
