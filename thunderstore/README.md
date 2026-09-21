# GunGame Arena

A companion plugin for Kodeman's **GunGame**. Sosigs stop ignoring each other: they fight
you and each other in Free For All or Team Deathmatch, with an in-map panel to tune it all
and a floating Arsenal-style leaderboard on top. Works on every GunGame map and with
GunGame Progressions.

## Media

<!-- MEDIA -->

## Modes

- **Free For All** — every sosig for itself, including against you. Sosigs hold grudges
  against a handful of rivals at a time instead of brawling in one blob, and a share of
  them are periodically sent hunting for you.
- **Team Deathmatch** — blue (you plus allies) versus red, optionally green and yellow.
  First team to a points target wins. Points come from kills, counted for every
  contestant on a team, you included. The weapon rotation loops instead of ending the
  round, and GunGame's "Number of weapons" controls are disabled while Teams is active
  since the round no longer ends on the last weapon. Friendly fire is off by default, so
  your shots never hurt your own team. Teammates get a blue tint and a floating name tag
  above their head so you can pick them out at a glance. A "<TEAM> TEAM WINS" banner shows
  for 5 seconds when a team hits the points target, then the round ends normally.
- **Off** — original GunGame behaviour; sosigs only fight you.

## Match flow

- **Spread-out spawns** — new sosigs spawn at the point farthest from everyone else, not
  randomly, so fights don't all start in the same corner.
- **Grudges** — in Free For All, each sosig is hostile to only a few rivals at a time
  (picked from nearby contestants, you included) instead of every other sosig, and
  re-rolls those rivals periodically or whenever one dies.
- **Hunters** — every so often, a share of hostile sosigs are sent straight at you instead
  of wandering GunGame's usual random waypoints, so an FFA round doesn't ignore you.
- **Skill tiers** — every sosig rolls a skill level at spawn: Rookie, Regular, Veteran, or
  Elite, shown on its leaderboard card as chevrons (Rookie `^` up to Elite `^^^^`). Higher
  tiers aim tighter and react faster — nobody gets aimbot, rookies just miss more.

## Leaderboard HUD

A floating panel of cards in front of you: portrait, kill count, name, and a crown for the
leader. Sosig portraits are captured from the sosig's own head; your card uses your Steam
avatar. Crowns follow the mode — one crown for the top kill count in Free For All, or one
crown per team on that team's best contestant in Team Deathmatch (only once a contestant
or team has a kill). In Team Deathmatch the header also shows each team's running score and
a subtitle naming the points target, and the victory banner rides on this HUD.

## In-map Arena panel

A settings panel appears on every GunGame map, standing beside GunGame's own "More
options" board, so you never have to leave VR to change how the round plays. Point and
click with the same laser pointer GunGame's own buttons use.

| Control | What it does |
|---|---|
| Mode | Cycles Off → Free For All → Teams. Applies at the next Start Game. |
| Teams | Number of teams, 2–4. Teams mode only; dimmed otherwise. Applies at the next Start Game. |
| Allies | Sosigs on your team (Auto = about half the sosig count, or a fixed 0–9). Teams mode only; dimmed otherwise. Applies at the next Start Game. |
| Points to win | Team score that ends the round, 5–200 in steps of 5. Teams mode only; dimmed otherwise. Applies at the next Start Game. |
| Friendly fire | Toggles whether your shots can hurt your own team. Teams mode only. |
| Leaderboard | Shows or hides the HUD immediately, including mid-round. |
| Spread spawns | Toggles farthest-point spawn placement. |
| Grudges | Toggles the rival-based targeting described above (Free For All only). |
| Hunters | Toggles periodic hunting orders toward you. |
| Pressure | How large a share of hostile sosigs hunt you at once, 0–100% in steps of 5%. |
| Skill tiers | Toggles per-sosig aim skill rolls. Applies to new spawns only. |

Every change saves straight to the config file, so it survives a restart. Rows marked
"applies at the next Start Game" take effect the next time you start a GunGame round;
everything else applies immediately.

## Config

Settings live in `BepInEx/config/shaha.GunGameArena.cfg` after the first launch. The panel
above covers the values you'll actually want to touch mid-session; the rest (rival radius,
skill-tier multipliers, HUD placement, name seed, and so on) are config-file only by
design, to keep the panel simple.

| Section | Key | Default | Meaning |
|---|---|---|---|
| Arena | `Mode` | `FreeForAll` | Off / FreeForAll / Teams. |
| Arena | `TeamCount` | `2` | Number of teams (2–4), Teams mode only. |
| Arena | `AllySosigs` | `-1` | Sosigs on your team in Teams mode. -1 = about half the sosig count. |
| Teams | `PointsToWin` | `30` | Team score that ends a Team Deathmatch round (5–200). |
| Teams | `FriendlyFire` | `false` | When false, your shots never damage your own team. |
| Teams | `TagRange` | `0` | Hide teammate name tags beyond this many metres. 0 = always visible. |
| Teams | `TeamTint` | `true` | Tint teammates' bodies blue so you can tell them apart. |
| Panel | `Scale` | `1.0` | Size multiplier for the in-map Arena panel (0.5–2). |
| Debug | `Verbose` | `false` | Log diagnostic dumps (panel hierarchy, teammate tag status, per-sosig tint) at Info level. |
| Leaderboard | `Enabled` | `true` | Show the floating leaderboard HUD. |
| Leaderboard | `TopCount` | `5` | Cards shown before your own card is pinned at the end. |
| Leaderboard | `Scale` | `1.0` | Overall HUD size multiplier. |
| Leaderboard | `Distance` | `1.0` | Metres in front of your head. |
| Leaderboard | `Height` | `0.35` | Metres above eye line. |
| Leaderboard | `ShowNames` | `true` | Name label under each card. |
| Leaderboard | `ShowTierBadge` | `true` | Skill-tier chevrons under each sosig's name. |
| Names | `Seed` | `0` | 0 = random names every round; any other value = reproducible roster. |
| Behaviour | `SpreadSpawns` | `true` | Spawn each sosig at the spawner farthest from everyone. |
| Behaviour | `Grudges` | `true` | FFA only: each sosig hunts a few rivals at a time instead of everyone. |
| Behaviour | `RivalCount` | `3` | Rivals per sosig (1–8). |
| Behaviour | `RivalRadius` | `40` | Metres; rivals are picked from contestants within this radius. |
| Behaviour | `PlayerRivalWeight` | `2.0` | How much more likely you are to be picked as a rival than a sosig (1 = equal). |
| Behaviour | `RivalRerollSecondsMin` / `Max` | `20` / `40` | Seconds between rival re-rolls. |
| Behaviour | `Hunters` | `true` | Periodically send a share of hostile sosigs toward you. |
| Behaviour | `HunterShare` | `0.25` | Fraction of hostile sosigs sent toward you each interval (0–1). |
| Behaviour | `HunterIntervalSecondsMin` / `Max` | `10` / `20` | Seconds between hunter orders. |
| Behaviour | `SkillTiers` | `true` | Roll Rookie/Regular/Veteran/Elite per contestant; affects aim, not fire volume. |
| Behaviour | `TierWeights` | `30,40,20,10` | Relative weights Rookie,Regular,Veteran,Elite. |
| Tier.Rookie / Regular / Veteran / Elite | `Spread`, `FireAngle`, `Refire`, `Reaction` | see cfg | Per-tier multipliers on weapon spread, off-target fire tolerance, refire delay and target-recognition speed. |

## Compatibility

- Requires **BepInEx pack for H3VR** (5.4.1700) and Kodeman's **GunGame** Thunderstore
  package 1.0.2 or newer (its plugin reports version 1.0.4 internally — that's expected).
- Works on every GunGame map, and with `HLin_Mods-GunGame_Progressions`.
- Sosig count is GunGame's own setting on its in-map panel, not this plugin's.
- Not H3MP-aware; built and tested for single-player.

## Credits & source

Kodeman for GunGame, which this plugin extends without forking. Built with BepInEx and
Harmony. Source: <https://github.com/shahar015/GunGameArena>. MIT licensed.
