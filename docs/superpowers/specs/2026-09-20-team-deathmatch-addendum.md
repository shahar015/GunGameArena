# GunGame Arena — Addendum 2: Team Deathmatch rules, HUD and panel polish

Date: 2026-09-20 (approved in discussion)
Extends: `2026-09-19-gungame-arena-design.md`, `2026-09-20-arena-panel-addendum.md`.

## Decisions (user, 2026-09-20)
- Panel notes explain only *what* an option is; no "— why" clause. Skill tiers gets a note too.
- Team Deathmatch (Mode = Teams) is won by **team points**, not by finishing the weapon rotation. Points to win is a panel option (Teams only).
- GunGame's "Number of weapons" controls are dimmed and inert while Teams is selected; the rotation loops instead of ending the round.
- HUD in Teams: subtitle under the title "First team to reach N points wins"; team scores left/right of the title in team colours.
- Teammates get a floating blue dot + name tag above the head. Friendly fire is off by default (panel toggle).
- Victory: HUD banner "<TEAM> TEAM WINS" for 5 s, then GunGame's normal end area.

## Verified GunGame facts
- `Progression.Promote()` increments `CurrentWeaponId` (public field on the `MonoBehaviourSingleton<Progression>` instance) and calls `GameManager.Instance.EndGame()` when `CurrentWeaponId >= min(GameSettings.CurrentPool.GetWeaponCount(), WeaponCountOption.WeaponCount)`; otherwise `SpawnAndEquip()`.
- `WeaponCountOption` is a plain MonoBehaviour on the "Gun Game" board (static `int WeaponCount`; arrow buttons are `FVRPointableButton`s whose `OnPoint` invokes `Button.onClick` directly, bypassing `Button.interactable` — so disabling the *pointable* component is what makes a button inert).
- `GameManager.Instance.GameEnded` (public bool), `GameManager.Instance.EndGame()` teleports the player to the end area and shows kills/deaths/time.

## Config (new section `Teams`)
| Key | Default | Range | Meaning |
|---|---|---|---|
| `PointsToWin` | 30 | 5..200, panel steps of 5 | Team score that ends the round |
| `FriendlyFire` | false | | When false, damage whose `Source_IFF` is the player's IFF never reaches a team-0 sosig |
| `TagRange` | 30 | metres | Teammate tags hidden beyond this distance |

## Rules (plugin `Behaviour/TeamMatch.cs`)
- **Score** = sum of `Kills` of all contestants on a team (player included). Core `TeamScore.Total(contestants, teamIndex)` and `TeamScore.Winner(contestants, teamCount, pointsToWin)` → team index or −1 (lowest index wins ties at the same tick).
- **Victory**: on every `Roster.Changed`, if Mode = Teams, round active, `!GameManager.Instance.GameEnded`, and `Winner >= 0` (once per round): `LeaderboardHud.ShowBanner(TeamName + " TEAM WINS", teamColor, 5 s)`, then after 5 s `GameManager.Instance.EndGame()`.
- **Rotation loop**: Harmony prefix on `Progression.Promote`: when Mode = Teams and round active, if `CurrentWeaponId + 1 >= min(pool count, WeaponCountOption.WeaponCount)` set `CurrentWeaponId = -1` before the original runs, so the original increments to 0 and equips the first weapon. Never calls EndGame in Teams.
- **Friendly fire**: Harmony prefix on `Sosig.ProcessDamage(Damage, SosigLink)` returning `false` (skip) when Mode = Teams, `FriendlyFire` false, victim is a tracked team-0 sosig, and `d.Source_IFF == Roster.PlayerIff`. Runs before the existing postfix, so no hit is recorded either.
- **Weapon-count lock**: `TeamMatch.ApplyWeaponCountLock()` finds `Object.FindObjectOfType<WeaponCountOption>()`; when Mode = Teams disables every `FVRPointableButton` under it and sets alpha 0.4 on every `Text`/`Image` under it; otherwise restores (originals remembered). Called after the Arena panel is built, on every panel Mode change, and on `RoundStarting`.

## HUD (Teams mode only; hidden otherwise)
- Header row: `BLUE TEAM: n` left of the title in `HudPalette.TeamColor(0)`, `RED TEAM: n` right of the title in `TeamColor(1)`; with 3–4 teams the right label lists `RED n · GREEN n · YELLOW n`. 26 pt bold.
- Subtitle under the title: `First team to reach N points wins` 18 pt, 0.85 alpha.
- Banner: centred under the cards, 64 pt bold in team colour with black outline, text `BLUE TEAM WINS`; shown for 5 s.
- Team names from Core `HudPalette.TeamName(int)`: BLUE, RED, GREEN, YELLOW.

## Teammate tags (`Hud/TeamTags.cs`)
- For each living contestant with `TeamIndex == 0` while Mode = Teams: a world-space canvas (scale 0.003) 0.35 m above `Links[0]`, billboarded to the player head each `LateUpdate`, text `● Name` (dot in team blue, name white, 40 pt, black outline). Hidden beyond `TagRange`. Created on `SpawnerPatches.SosigBound`, destroyed when the slot vacates or on `RoundEnded`. No tags in FFA/Off.

## Panel changes
- Notes: Grudges `(each sosig fights 3 rivals at a time, not everyone)`; Hunters `(every 10–20 s a few sosigs are sent toward you)`; Skill tiers `(each sosig gets an aim level: Rookie ^ … Elite ^^^^)`.
- New rows after Allies, Teams-only (dimmed otherwise): `Points to win` `<` `30` `>`; toggle `Friendly fire: OFF`.
- Height grows to fit (~800).

## Deferred fixes folded in
- `PanelInstaller.PlaceUsingMoreOptionsBoard` catch destroys a half-built `_panel` before returning false; `EncapsulateLocal` gets try/catch.
