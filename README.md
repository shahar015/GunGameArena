# GunGame Arena

A BepInEx companion plugin for Kodeman's **GunGame** (H3VR) that makes sosigs fight each
other, not just you — Free For All or Team Deathmatch, spread spawns, grudges, hunters,
skill tiers, an in-map settings panel, and a floating Arsenal-style leaderboard with head
portraits. It patches GunGame's own spawner and death hooks at runtime rather than forking
it, so it works on every GunGame map and with GunGame Progressions.

## Media

<!-- MEDIA -->

## Install

- **Thunderstore Mod Manager / r2modman**: search for "GunGame Arena" and install it
  alongside the BepInEx pack for H3VR and Kodeman's GunGame (1.0.2 or newer).
- Manual: drop `GunGameArena.dll` and `GunGameArena.Core.dll` into
  `BepInEx/plugins/GunGameArena/` in your H3VR install.

See `thunderstore/README.md` for the full feature and config reference (this is the same
text published on the Thunderstore mod page).

## Build from source

Requires the **.NET 10 SDK** (`dotnet`). Reference assemblies for H3VR's Unity 5.6 / net35
runtime are resolved from your H3VR install and BepInEx profile via two MSBuild properties
in `Directory.Build.props`, overridable as environment variables:

- `H3VR_DIR` — path to the H3VR install (default: the standard Steam location).
- `H3VR_PROFILE_DIR` — path to the BepInEx profile to copy the built plugin into (default:
  the Thunderstore Mod Manager "Default" profile).

```powershell
dotnet build GunGameArena.slnx -c Release
```

This builds both projects and copies `GunGameArena.dll` + `GunGameArena.Core.dll` into
`$(H3VR_PROFILE_DIR)\plugins\GunGameArena\`, so a Release build is immediately live in your
mod profile.

To package a Thunderstore zip:

```powershell
powershell -ExecutionPolicy Bypass -File tools\pack.ps1
```

This reads the version from `thunderstore/manifest.json` and writes
`dist\GunGameArena-<version>.zip`.

## Project layout

```
src/
  GunGameArena.Core/   Unity-free, unit-testable logic: team assignment, ranking, crowns,
                        kill attribution, name generation, rival/hunter/tier picking,
                        panel label/step logic, team scoring.
  GunGameArena/         BepInEx plugin: Harmony patches, roster, kill tracking, portraits,
                        leaderboard HUD, in-map Arena panel, Team Deathmatch rules.
tests/
  GunGameArena.Core.Tests/   xunit tests for GunGameArena.Core.
thunderstore/           manifest.json, README.md, CHANGELOG.md, icon.png — packaged as-is.
docs/superpowers/specs/ Design docs: original design spec plus the Arena panel and Team
                         Deathmatch addenda. Read these for the "why" behind the plugin's
                         behaviour.
```

## License

MIT — see `LICENSE`.
