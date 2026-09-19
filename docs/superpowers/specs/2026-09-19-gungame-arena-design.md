# GunGame Arena — Design Spec

Date: 2026-09-19
Status: approved in discussion, pending written review

## 1. Goal

A BepInEx companion plugin for Kodeman's **GunGame** (H3VR, Thunderstore package
`Kodeman-GunGame` 1.0.2) that adds two features to every GunGame map:

1. **Arena teams** — sosigs fight each other (free-for-all or teams), not only the player.
2. **Leaderboard** — a Roblox Arsenal-style HUD with head portraits, generated names,
   kill counts, ranks, crowns and team colours.

It does not fork GunGame. The public GunGame source is stale (1.0.0, March 2023) and
needs the full Unity 5.6 + MeatKit project; the shipped 1.0.2 DLL still exposes the same
spawner and death hook, so runtime patching is safe and works with every GunGame map
(Nuketown, Sandpit, Soul Silo, PG3D maps) and with `HLin_Mods-GunGame_Progressions`.

Out of scope: H3MP multiplayer, stats persisted across rounds, custom fonts, in-game
settings UI (config file only).

## 2. Environment facts (verified 2026-09-19)

| Item | Value |
|---|---|
| Game | `C:\Program Files (x86)\Steam\steamapps\common\H3VR` |
| Managed DLLs | `h3vr_Data\Managed\` — `Assembly-CSharp.dll`, `Assembly-CSharp-firstpass.dll` (contains `Steamworks.*`), `UnityEngine.dll` (Unity 5.x monolithic), `UnityEngine.UI.dll` |
| Mod manager | Thunderstore Mod Manager, profile **Default**: `%APPDATA%\Thunderstore Mod Manager\DataFolder\H3VR\profiles\Default\BepInEx\` |
| BepInEx | 5.4.1700 (`core\BepInEx.dll`, `core\0Harmony.dll`) |
| GunGame DLL | `plugins\Kodeman-GunGame\GunGame.dll` (1.0.2, net35, references Sodalite, Atlas) |
| .NET SDK | 10.0.201 (`dotnet`) |
| Target framework | `net35` (BepInEx 5 / Unity 5.6 mono) |

Relevant game API (from decompile):

- `FistVR.Damage`: `Source_IFF`, `Source_Point`, `Source_Transform`, `point`, `Class`.
- `FistVR.Sosig.ProcessDamage(Damage d, SosigLink link)` — every hit passes through here.
- `FistVR.Sosig.SosigDies(Damage.DamageClass, SosigDeathType)` — guarded by `BodyState == Dead`; sets `E.IFFCode = -3` at the end.
- `Sosig.GetIFF()`, `Sosig.SetIFF(int)`, `Sosig.Links[0]` = head `SosigLink`, `Sosig.BodyState`.
- `SosigTargetPrioritySystem.IFFChart` is `bool[32]` → valid team ids 0..31; sosigs treat any different id as hostile, negative as friendly.
- `FVRPlayerBody.GetPlayerIFF()` (0), `FVRPlayerBody.Head` transform.
- `FVRSceneSettings.PlayerDeathFromIFFEvent(bool killedSelf, int iff)`, `SosigKillEvent(Sosig)`.
- `SteamManager.Initialized`, `SteamFriends.GetPersonaName()`, `SteamFriends.GetLargeFriendAvatar(CSteamID)`, `SteamUser.GetSteamID()`, `SteamUtils.GetImageSize/GetImageRGBA`.

Relevant GunGame 1.0.2 API (namespace `GunGame.Scripts`):

- `CustomSosigSpawner { SosigOrder SpawnState; int IFF; SpawnedSosigInfo Spawn(SosigEnemyID) }` — IFF is `1` on every spawner prefab; this is why sosigs never fight each other.
- `SosigBehavior.Instance.Sosigs : Dictionary<Sosig, SosigEnemyID>`, `SpawnSosigRandomPlace`, `ClearSosigs()`.
- `Progression.OnSosigDied` (Harmony postfix on `Sosig.SosigDies`): credits the player when `GetDiedFromIFF() == player IFF`, despawns, respawns a replacement.
- `Progression.OnSosigKilledByPlayer(Sosig)` — private static; advances weapon progression.
- `GameManager.BeforeGameStartedEvent`, `GameManager.GameStartedEvent` (static `Action`), `GameManager.Instance.Kills/Deaths`.
- `GameSettings.MaxSosigCount` (static int).

## 3. Architecture

Single plugin assembly `GunGameArena.dll` plus a Unity-free core library so logic is unit-testable.

```
GunGameArena/
  src/
    GunGameArena.Core/          net35;net8.0 — no Unity/game refs, pure logic
      Vec3.cs                   tiny float3 struct (distance only)
      TeamMode.cs               enum Off | FreeForAll | Teams
      TeamAssigner.cs           slot index → team index → IFF
      Contestant.cs             Id, Name, TeamIndex, Iff, Kills, LastKillTime, IsPlayer, IsAlive
      Ranking.cs                sort, top-N-plus-you, crown selection
      KillAttribution.cs        nearest candidate on a team to a point
      NameGenerator.cs          Roblox-style unique usernames
      HudPalette.cs             team/rank colours as RGBA floats
    GunGameArena/               net35 — BepInEx plugin
      Plugin.cs                 BaseUnityPlugin entry, Harmony.PatchAll, wiring
      ArenaConfig.cs            BepInEx ConfigEntry definitions
      Patches/SpawnerPatches.cs prefix/postfix on CustomSosigSpawner.Spawn
      Patches/DamagePatches.cs  postfix Sosig.ProcessDamage, prefix Sosig.SosigDies
      Patches/ProgressionPatches.cs prefix Progression.OnSosigKilledByPlayer
      Roster.cs                 runtime roster: contestants ↔ live Sosig instances
      KillTracker.cs            last-hit memory per sosig, attribution, player-death hook
      Portraits/PortraitRenderer.cs  camera snapshot of a sosig head → Sprite
      Portraits/SteamAvatar.cs  Steam avatar → Sprite, generic fallback
      Hud/LeaderboardHud.cs     canvas, header, card row, rebuild on change
      Hud/HudFollower.cs        yaw-only smoothed head follower
      Hud/ContestantCard.cs     one card (portrait, kills, name, crown, border)
      Hud/Sprites.cs            procedural crown + fallback avatar sprites
  tests/
    GunGameArena.Core.Tests/    net8.0, xunit
  thunderstore/
    manifest.json, README.md, icon.png (256x256), CHANGELOG.md
  Directory.Build.props         H3VR_DIR, H3VR_PROFILE_DIR, copy-to-profile step
  docs/superpowers/specs/…
```

Data flow:

```
GameManager.BeforeGameStartedEvent ──► Roster.Reset(MaxSosigCount, mode)
CustomSosigSpawner.Spawn  ─prefix──► Roster.ClaimVacantSlot() → sets spawner.IFF
                          ─postfix─► Roster.Bind(slot, sosig) → PortraitRenderer.Capture(sosig)
Sosig.ProcessDamage       ─postfix─► KillTracker.RecordHit(sosig, d.Source_IFF, point)
Sosig.SosigDies           ─prefix──► KillTracker.Attribute(victim) → Contestant.Kills++ → Roster.Changed
Progression.OnSosigKilledByPlayer ─prefix─► return KillTracker.LastKillWasByPlayer(sosig)
PlayerDeathFromIFFEvent   ──────────► KillTracker.AttributePlayerDeath(iff)
Roster.Changed            ──────────► LeaderboardHud.Rebuild()
```

## 4. Feature: Arena teams

### Config (`BepInEx/config/gungamearena.cfg`, section `Arena`)

| Key | Type / default | Meaning |
|---|---|---|
| `Mode` | `FreeForAll` (Off / FreeForAll / Teams) | Team layout |
| `TeamCount` | int 2, range 2..4 | Teams mode only |
| `AllySosigs` | int -1 | Sosigs on the player's team. -1 = floor(MaxSosigCount / 2). Clamped to 0..MaxSosigCount-1 so at least one enemy exists |

### Team ids

- Player IFF is read at runtime (`GM.CurrentPlayerBody.GetPlayerIFF()`, normally 0).
- **Off**: every spawn keeps IFF 1 (original behaviour).
- **FreeForAll**: contestant slot `i` (0-based) gets IFF `i + 1`. Slots ≥ 31 wrap to 31 (shared) with a warning; in practice MaxSosigCount ≤ 30.
- **Teams**: team index 0 = player's team → player IFF. Team index `t ≥ 1` → IFF `t`. The first `AllySosigs` slots are team 0; remaining slots round-robin over teams 1..TeamCount-1.

### Spawner patch

Harmony **prefix** on `CustomSosigSpawner.Spawn(SosigEnemyID)`: if mode ≠ Off and a round is active, `__instance.IFF = Roster.ClaimVacantSlot().Iff`. **Postfix**: bind `__result.SpawnedSosig` to that slot and request a portrait capture. If no roster exists yet (spawn outside a GunGame round, e.g. debug), leave IFF untouched.

Slot vacancy: a slot is vacant when its sosig reference is null (Unity-destroyed), `BodyState == Dead`, or the sosig is no longer in `SosigBehavior.Instance.Sosigs`. This covers all three GunGame despawn paths: death respawn, distance despawn, and `ClearSosigs()` after a tiered-progression player death.

### Behaviour notes

- Sosigs pick the nearest visible hostile, so they will engage each other and the player. In FFA pressure on the player drops; the config `AllySosigs`/Teams and GunGame's own sosig-count setting are the tuning knobs. No extra "min hostile" knob (YAGNI).
- GunGame's waypoint loop (random assault points every 12–25 s) is left alone; it keeps everyone roaming into each other.

## 5. Feature: Kill tracking & attribution

### Recording hits

Postfix on `Sosig.ProcessDamage(Damage d, SosigLink link)`: store per victim `LastHit { Iff = d.Source_IFF, Point = d.Source_Point != zero ? d.Source_Point : d.point, Time }`.

### Attributing a sosig death

Prefix on `Sosig.SosigDies` (runs before GunGame's postfix; guard `BodyState != Dead` and `Roster.Contains(victim)` and de-dupe by instance id):

1. `iff = LastHit.Iff` (fallback `victim.GetDiedFromIFF()`). If `iff < 0` → no credit (environment, self).
2. Candidates = living contestants with that IFF, excluding the victim. The player is a candidate when `iff == playerIff` (position = `Head.position`); sosig candidates use `Links[0].transform.position`.
3. Credit the candidate nearest to `LastHit.Point` (`KillAttribution.Nearest`). No candidates → no credit.
4. Record `LastKillWasByPlayer[victim] = (winner.IsPlayer)`.
5. Raise `Roster.Changed`.

### Guarding GunGame progression

Prefix on `Progression.OnSosigKilledByPlayer(Sosig)`: return `KillTracker.LastKillWasByPlayer(sosig)`. Without this, in Teams mode any kill by a blue ally (same IFF as the player) would advance the player's weapon. In Off/FFA mode this returns true whenever GunGame would have credited the player anyway, so behaviour is unchanged.

### Player deaths

Subscribe to `GM.CurrentSceneSettings.PlayerDeathFromIFFEvent` on round start (unsubscribe on scene unload). Credit the living sosig contestant with that IFF nearest to the player's head. Deaths are not displayed (kills only), matching the reference screenshot.

## 6. Feature: Roster

`Roster.Reset(count, mode)` on `GameManager.BeforeGameStartedEvent`:

- Creates `count = GameSettings.MaxSosigCount` sosig contestants with unique generated names, team index and IFF from `TeamAssigner`, kills 0, no sosig bound.
- Creates the player contestant: name = `SteamFriends.GetPersonaName()` when `SteamManager.Initialized`, else `"You"`; team 0; IFF = player IFF; portrait from `SteamAvatar`.
- Contestants persist for the whole round; a replacement sosig inherits the dead one's slot, name, team and kills (respawn semantics). Portrait is re-captured on each respawn because the sosig type may change.
- If more spawns than slots occur, extra contestants are appended (defensive).

## 7. Feature: Names

`NameGenerator(seed)` produces Roblox-style usernames, unique within a round:

- Word lists (≈40 adjectives, ≈40 nouns) with a sausage/H3VR flavour mixed with generic gamer words: `Glizzy`, `Wiener`, `Sosig`, `Mustard`, `Brat`, `Sniper`, `Ninja`, `Gamer`, `Toaster`…
- Decorators, picked randomly: `{Adj}{Noun}`, `{adj}_{noun}`, `xX_{Noun}_Xx`, `{Noun}{2–4 digits}`, `{Adj}{Noun}{2 digits}`, `iL{Noun}`, `{Noun}YT`.
- Max 16 chars. Uniqueness via a used-set with retry; after 50 collisions append digits.
- Config `Names.Seed` (int, 0 = random per round) for reproducible rosters.

## 8. Feature: Portraits

### Sosig heads

`PortraitRenderer` owns one hidden `Camera` (disabled, manual `Render()`), a 128×128 ARGB32 `RenderTexture`, `clearFlags = SolidColor`, `backgroundColor = Color.clear`, `fieldOfView = 30`, near 0.05, far 1.2, `cullingMask = everything`.

Capture (coroutine, one frame after spawn so wearables are attached):

1. `head = sosig.Links[0].transform`. Camera placed at `head.position + facing * 0.45 + up * 0.03`, looking at `head.position`, where `facing = sosig.transform.forward` (head link orientation is not trusted; verify in-game and switch to head forward if it looks better).
2. `camera.Render()`; `ReadPixels` into a `Texture2D`; `Sprite.Create`.
3. On failure (null links, destroyed sosig) fall back to the generic sprite; never throw into the game loop.
4. Old portrait texture is destroyed when replaced to avoid leaks.

### Player avatar

`SteamAvatar.Load(callback)`: if `SteamManager.Initialized`, `h = GetLargeFriendAvatar(GetSteamID())`. `h > 0` → `GetImageSize` + `GetImageRGBA` → `Texture2D RGBA32`, flip rows vertically, sprite. `h == -1` (not loaded yet) → poll once per second up to 10 s. `h == 0` or timeout or Steam offline → generic fallback sprite.

## 9. Feature: Leaderboard HUD

### Config (section `Leaderboard`)

| Key | Default | Meaning |
|---|---|---|
| `Enabled` | true | |
| `TopCount` | 5 | Cards shown before pinning the player |
| `Scale` | 1.0 | Overall size multiplier |
| `Distance` | 1.0 | Metres ahead of the head |
| `Height` | 0.35 | Metres above eye line |
| `ShowNames` | true | Name label under each card |

### Placement

World-space `Canvas` (scale 0.001, i.e. 1 unit = 1 mm). `HudFollower` in `LateUpdate`: `yaw = head yaw`; target position = `head.position + yawForward * Distance + Vector3.up * Height`; rotation = yaw with a 10° downward tilt so it faces the player; position and yaw lerped (`1 - exp(-6·dt)`) so it drifts rather than snaps. Hidden until `GameStartedEvent`; stays visible after the round ends.

### Layout

- Header text (mode name: `Free For All` / `Team Deathmatch` / `Gun Game`) centred above the row, 28 px bold, dark rounded background.
- `HorizontalLayoutGroup` of `ContestantCard`s, spacing 6 px. Card 120×120 px: 4 px border (`Image`), portrait (`Image`, `preserveAspect`), kill number (`Text`, bold 48 px, white, black `Outline`, anchored bottom-right), crown (`Image`, 28 px, anchored top-left), optional name (`Text`, 18 px, below card).
- Font: `Resources.GetBuiltinResource<Font>("Arial.ttf")`.
- The player's card always gets an additional 3 px white outline.
- If the player is outside the top `TopCount`, an 18 px gap then the player's card is appended.

### Ranking & crowns (`Ranking`, pure)

- Sort by `Kills` desc, then `LastKillTime` asc (reached the score first wins ties), then `Id` asc.
- `Visible = top TopCount ∪ {player}` in that order (player appended if not already present).
- **Crowns** (per user decision 2026-09-19):
  - FreeForAll / Off: exactly one crown, on rank 1, only if `Kills > 0`.
  - Teams: one crown per team, on that team's best-ranked contestant, only if `Kills > 0`. A team whose best has 0 kills shows no crown.

### Colours (`HudPalette`)

| Context | Border/background |
|---|---|
| FFA / Off card | `#2B2B2B` bg, `#555555` border; rank 1 border `#F5C542` (gold) |
| Teams: team 0 (player) | `#1E3FA8` blue |
| Teams: team 1 | `#A81E1E` red |
| Teams: team 2 | `#1E8A3A` green |
| Teams: team 3 | `#C9A400` yellow |
| Kill text | white with black outline |

### Refresh

`LeaderboardHud.Rebuild()` runs on `Roster.Changed` (kills, respawn portrait ready, round reset) — never per frame. ≤ 6 cards, so full rebuild is cheap; cards are pooled to avoid GC churn.

## 10. Error handling

- Every Harmony patch body is wrapped in try/catch that logs once per message via `ManualLogSource` and never rethrows into the game.
- Missing GunGame types at load → plugin logs an error and disables itself (soft dependency check on `Kodeman.GunGame` plugin GUID / type presence).
- Portrait/avatar failures fall back to generic sprites.
- Roster access from patches tolerates null `Roster` (no active round) by doing nothing.

## 11. Testing

Unit tests (`tests/GunGameArena.Core.Tests`, xunit, net8.0) on the Core project:

- `TeamAssigner`: Off keeps IFF 1; FFA gives unique ids 1..n; Teams with allies=k gives first k slots player IFF and round-robins the rest; clamps allies to leave ≥1 enemy; TeamCount bounds.
- `NameGenerator`: uniqueness for 32 names; max length; deterministic with seed; digits-suffix fallback after collisions.
- `Ranking`: sort order and tie-break; top-N-plus-player pinning both when player is inside and outside top N; crown rules for FFA (one crown, none at 0 kills) and Teams (one per team, none for 0-kill teams).
- `KillAttribution`: nearest candidate wins; excludes victim; empty candidates → null; negative IFF → null.
- `HudPalette`: team index → colour, rank-1 gold in FFA.

In-game verification (manual, via Thunderstore profile Default): build copies `GunGameArena.dll` into `BepInEx\plugins\GunGameArena\`; launch H3VR through the mod manager; on Nuketown GunGame confirm in `BepInEx\LogOutput.log`: roster created with N names, IFFs assigned, hits recorded, kills attributed with winner names, HUD created; visually confirm sosigs shooting each other, cards/crowns/colours, and that ally kills in Teams mode do not advance the weapon.

## 12. Build & shipping

- `Directory.Build.props`: `H3VR_DIR` (default the Steam path above), `H3VR_PROFILE_DIR` (default the Thunderstore Default profile), both overridable via environment. References resolved from those paths with `Private=false`. Post-build target copies the plugin DLL (+ Core DLL) into `$(H3VR_PROFILE_DIR)\plugins\GunGameArena\`.
- `Microsoft.NETFramework.ReferenceAssemblies` NuGet provides net35 reference assemblies on the .NET 10 SDK.
- Thunderstore package: `manifest.json` (name `GunGameArena`, dependencies `BepInEx-BepInExPack_H3VR-5.4.1700`, `Kodeman-GunGame-1.0.2`), `README.md`, `icon.png` 256×256, `CHANGELOG.md`; a `pack` script zips `plugins/GunGameArena/*.dll` with those files.
- Repo: `D:\Projects\gungamesosig\GunGameArena`, MIT licence, conventional commits.

## 13. Risks & open verification points

- Head-link orientation for portrait framing → verify in-game; fallback documented in §8.
- `Damage.Source_Point` may be zero for melee/explosive damage → fallback to hit point; attribution may be approximate for grenades.
- GunGame's own `SosigDies` postfix despawns the victim immediately; our prefix must finish attribution before that (Harmony runs prefixes first — fine).
- GunGame Progressions "Mixed Enemy" pools spawn through the same spawner → compatible; custom modded sosigs with odd prefabs may have no `Links[0]` renderer → generic portrait.
