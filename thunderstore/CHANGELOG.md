# Changelog

## 1.0.0

Initial public release.

**Modes**
- Free For All: every sosig fights every other sosig and the player.
- Team Deathmatch: blue (player + allies) vs. red/green/yellow, won by team points
  (`PointsToWin`, panel-adjustable); weapon rotation loops instead of ending the round;
  GunGame's "Number of weapons" controls are disabled while Teams is active; friendly fire
  off by default; blue team tint and floating name tags on teammates; "<TEAM> TEAM WINS"
  victory banner on the leaderboard HUD.
- Off: original GunGame behaviour, unchanged.

**Match flow**
- Spread-out spawns (farthest-point placement instead of random).
- Grudges: FFA sosigs target a handful of rivals at a time instead of everyone at once.
- Hunters: a share of hostile sosigs are periodically sent after the player.
- Skill tiers: Rookie/Regular/Veteran/Elite aim and reaction rolls per sosig, shown as
  chevrons on the leaderboard card.

**Leaderboard**
- Floating Arsenal-style HUD: sosig head portraits, generated usernames, kill counts,
  crowns, team colours; player card uses the Steam avatar.
- Team Deathmatch header shows live team scores and a points-to-win subtitle.

**In-map Arena panel**
- Settings panel built at runtime beside GunGame's own "More options" board on every
  GunGame map; edits Mode, Teams, Allies, Points to win, Friendly fire, Leaderboard,
  Spread spawns, Grudges, Hunters, Hunter pressure, and Skill tiers with the VR laser
  pointer; every change saves straight to the config file.

**Fixes found during testing**
- Sodalite's spawn API randomises any requested IFF ≥ 5; the spawn postfix now re-applies
  the intended team IFF after every spawn.
- Team tint now applies via `MaterialPropertyBlock` instead of mutating renderer
  materials, so shared materials aren't instanced or altered for other sosigs.
- Friendly-fire blocking moved to `SosigLink.Damage` (where health/limb damage is actually
  applied), not just `Sosig.ProcessDamage`, so it reliably stops teammate damage.
- Weapon-count buttons are made inert by disabling their colliders, not just their
  pointable component, since the hand's raycast still finds a disabled `FVRPointable`.
- Teammate name tags stay alive out of tag range (hidden via canvas, not destroyed) and
  are torn down cleanly on round end or a mode switch away from Teams.
- Diagnostic logging (panel hierarchy, tag status, tint counts) is gated behind
  `Debug.Verbose` so normal runs stay quiet; errors and warnings are always logged.
