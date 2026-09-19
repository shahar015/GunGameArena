# GunGame Arena Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A BepInEx companion plugin for Kodeman's H3VR GunGame mod that makes sosigs fight each other (FFA or teams), makes the match feel natural (spread spawns, grudges, hunters, skill tiers), and shows a Roblox Arsenal-style leaderboard HUD with portraits, generated names, kills, crowns and team colours.

**Architecture:** Two assemblies. `GunGameArena.Core` (net35 + net8.0, no Unity references) holds all decision logic and is unit-tested with xunit. `GunGameArena` (net35 BepInEx plugin) holds Harmony patches on GunGame 1.0.2 and the game, a roster that maps contestants to live sosigs, kill attribution, portrait rendering, and the world-space HUD. Nothing in GunGame is forked.

**Tech Stack:** C# (LangVersion 9, no records/init), .NET SDK 10 building `net35` via `Microsoft.NETFramework.ReferenceAssemblies.net35`, BepInEx 5.4.1700, HarmonyLib 2 (`0Harmony.dll`), Unity 5.6 (`UnityEngine.dll`, `UnityEngine.UI.dll`), xunit 2.9 on net8.0.

**Spec:** `docs/superpowers/specs/2026-09-19-gungame-arena-design.md`

## Global Constraints

- Plugin target framework is exactly `net35`. Core multi-targets `net35;net8.0`. No `record`, `init`, `Span<T>`, `ValueTuple`, `string.Join(string, IEnumerable)` or other post-3.5 BCL APIs in either project.
- Core project must not reference UnityEngine, BepInEx, Harmony or game assemblies. All game-facing code lives in the plugin project.
- Every Harmony patch body is wrapped in `try { … } catch (Exception e) { Plugin.Log.LogError(...) }` and never rethrows into the game.
- Game paths: `H3VR_DIR` = `C:\Program Files (x86)\Steam\steamapps\common\H3VR`; `H3VR_PROFILE_DIR` = `%APPDATA%\Thunderstore Mod Manager\DataFolder\H3VR\profiles\Default\BepInEx`. Both overridable by environment variable of the same name.
- GunGame plugin GUID is `Kodeman.GunGame`; our GUID is `shaha.GunGameArena`, name `GunGame Arena`, version `0.1.0`.
- Valid team ids (IFF) are 0..31. Player IFF is read at runtime, never assumed.
- Config keys and defaults must match the spec tables exactly (§4, §4b, §9).
- Commit after every task with conventional-commit messages (`feat:`, `test:`, `chore:`, `docs:`). Repo: `D:\Projects\gungamesosig\GunGameArena`, branch `main`.
- Steps marked **USER CHECKPOINT** need the user to launch H3VR (VR headset). An agent cannot do these; stop and ask the user to run the check and paste the relevant `LogOutput.log` lines.

## File Structure

| Path | Responsibility |
|---|---|
| `Directory.Build.props` | Shared MSBuild props: game paths, LangVersion |
| `GunGameArena.sln` | Solution |
| `src/GunGameArena.Core/GunGameArena.Core.csproj` | Pure logic library |
| `src/GunGameArena.Core/Vec3.cs` | float3 + distance |
| `src/GunGameArena.Core/TeamMode.cs` | enum |
| `src/GunGameArena.Core/TeamAssigner.cs` | slot → team index → IFF |
| `src/GunGameArena.Core/Contestant.cs` | leaderboard row data |
| `src/GunGameArena.Core/Ranking.cs` | sort, visible set, crowns |
| `src/GunGameArena.Core/KillAttribution.cs` | nearest candidate on a team |
| `src/GunGameArena.Core/NameGenerator.cs` | Roblox-style names |
| `src/GunGameArena.Core/HudPalette.cs` | colours |
| `src/GunGameArena.Core/SkillTier.cs` | tiers, roller, multipliers |
| `src/GunGameArena.Core/RivalSelector.cs` | grudge rivals |
| `src/GunGameArena.Core/HunterPicker.cs` | hunter subset |
| `src/GunGameArena.Core/SpawnerChooser.cs` | max-min-distance spawner |
| `src/GunGameArena/GunGameArena.csproj` | BepInEx plugin, copies to profile |
| `src/GunGameArena/Plugin.cs` | entry, Harmony, event wiring |
| `src/GunGameArena/ArenaConfig.cs` | config entries |
| `src/GunGameArena/GunGameHooks.cs` | GunGame static event subscriptions, round lifecycle |
| `src/GunGameArena/Roster.cs` | contestants ↔ live sosigs |
| `src/GunGameArena/KillTracker.cs` | last hits, attribution, player death |
| `src/GunGameArena/Patches/SpawnerPatches.cs` | IFF on spawn, bind slot |
| `src/GunGameArena/Patches/DamagePatches.cs` | ProcessDamage / SosigDies |
| `src/GunGameArena/Patches/ProgressionPatches.cs` | block ally kills from promoting |
| `src/GunGameArena/Patches/SpawnPlacementPatches.cs` | spread spawns |
| `src/GunGameArena/Patches/WeaponPatches.cs` | re-apply tier on pickup |
| `src/GunGameArena/Behaviour/GrudgeDirector.cs` | rivals / IFF chart |
| `src/GunGameArena/Behaviour/HunterDirector.cs` | assault orders near player |
| `src/GunGameArena/Behaviour/SkillApplier.cs` | tier multipliers |
| `src/GunGameArena/Portraits/Sprites.cs` | procedural crown + fallback |
| `src/GunGameArena/Portraits/SteamAvatar.cs` | Steam avatar |
| `src/GunGameArena/Portraits/PortraitRenderer.cs` | head snapshots |
| `src/GunGameArena/Hud/HudFollower.cs` | follow head |
| `src/GunGameArena/Hud/ContestantCard.cs` | one card |
| `src/GunGameArena/Hud/LeaderboardHud.cs` | canvas + rebuild |
| `tests/GunGameArena.Core.Tests/*.cs` | xunit tests for Core |
| `thunderstore/manifest.json`, `README.md`, `icon.png`, `CHANGELOG.md` | package |
| `tools/make-icon.js`, `tools/pack.ps1` | icon generator, zip packer |

---

### Task 1: Solution scaffold and build pipeline

**Files:**
- Create: `Directory.Build.props`, `GunGameArena.sln`, `.gitignore`, `LICENSE`
- Create: `src/GunGameArena.Core/GunGameArena.Core.csproj`, `src/GunGameArena.Core/Vec3.cs`
- Create: `src/GunGameArena/GunGameArena.csproj`, `src/GunGameArena/Plugin.cs` (minimal)
- Create: `tests/GunGameArena.Core.Tests/GunGameArena.Core.Tests.csproj`, `tests/GunGameArena.Core.Tests/Vec3Tests.cs`

**Interfaces:**
- Produces: `GunGameArena.Core.Vec3 { float X,Y,Z; Vec3(x,y,z); static Vec3 Zero; static float Distance(Vec3,Vec3); bool IsZero }`
- Produces: `GunGameArena.Plugin` with `public static ManualLogSource Log` and constants `Guid`, `Name`, `Version`.

- [ ] **Step 1: Create Directory.Build.props and .gitignore**

`Directory.Build.props`:
```xml
<Project>
  <PropertyGroup>
    <H3VR_DIR Condition="'$(H3VR_DIR)' == ''">C:\Program Files (x86)\Steam\steamapps\common\H3VR</H3VR_DIR>
    <H3VR_PROFILE_DIR Condition="'$(H3VR_PROFILE_DIR)' == ''">$(APPDATA)\Thunderstore Mod Manager\DataFolder\H3VR\profiles\Default\BepInEx</H3VR_PROFILE_DIR>
    <H3VR_MANAGED>$(H3VR_DIR)\h3vr_Data\Managed</H3VR_MANAGED>
    <LangVersion>9.0</LangVersion>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);CS1591</NoWarn>
  </PropertyGroup>
</Project>
```

`.gitignore`:
```
bin/
obj/
dist/
*.user
.vs/
TestResults/
```

`LICENSE`: MIT text with `Copyright (c) 2026 shaha`.

- [ ] **Step 2: Create the Core project and Vec3**

`src/GunGameArena.Core/GunGameArena.Core.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net35;net8.0</TargetFrameworks>
    <AssemblyName>GunGameArena.Core</AssemblyName>
    <RootNamespace>GunGameArena.Core</RootNamespace>
  </PropertyGroup>
  <ItemGroup Condition="'$(TargetFramework)' == 'net35'">
    <PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies.net35" Version="1.0.3" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

`src/GunGameArena.Core/Vec3.cs`:
```csharp
using System;

namespace GunGameArena.Core
{
    /// <summary>Minimal float3 so Core never depends on UnityEngine.</summary>
    public struct Vec3
    {
        public float X;
        public float Y;
        public float Z;

        public Vec3(float x, float y, float z) { X = x; Y = y; Z = z; }

        public static readonly Vec3 Zero = new Vec3(0f, 0f, 0f);

        public bool IsZero { get { return X == 0f && Y == 0f && Z == 0f; } }

        public static float Distance(Vec3 a, Vec3 b)
        {
            float dx = a.X - b.X, dy = a.Y - b.Y, dz = a.Z - b.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public override string ToString() { return "(" + X + ", " + Y + ", " + Z + ")"; }
    }
}
```

- [ ] **Step 3: Create the test project with a failing Vec3 test**

`tests/GunGameArena.Core.Tests/GunGameArena.Core.Tests.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <LangVersion>latest</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" PrivateAssets="all" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\GunGameArena.Core\GunGameArena.Core.csproj" />
  </ItemGroup>
</Project>
```

`tests/GunGameArena.Core.Tests/Vec3Tests.cs`:
```csharp
using GunGameArena.Core;
using Xunit;

public class Vec3Tests
{
    [Fact]
    public void Distance_is_euclidean()
    {
        var a = new Vec3(0, 0, 0);
        var b = new Vec3(3, 4, 0);
        Assert.Equal(5f, Vec3.Distance(a, b), 4);
    }

    [Fact]
    public void Zero_reports_IsZero()
    {
        Assert.True(Vec3.Zero.IsZero);
        Assert.False(new Vec3(0, 1, 0).IsZero);
    }
}
```

- [ ] **Step 4: Create the plugin project and minimal Plugin.cs**

`src/GunGameArena/GunGameArena.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net35</TargetFramework>
    <AssemblyName>GunGameArena</AssemblyName>
    <RootNamespace>GunGameArena</RootNamespace>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <CopyLocalLockFileAssemblies>false</CopyLocalLockFileAssemblies>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies.net35" Version="1.0.3" PrivateAssets="all" />
    <ProjectReference Include="..\GunGameArena.Core\GunGameArena.Core.csproj" />
  </ItemGroup>
  <ItemGroup>
    <Reference Include="BepInEx"><HintPath>$(H3VR_PROFILE_DIR)\core\BepInEx.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="0Harmony"><HintPath>$(H3VR_PROFILE_DIR)\core\0Harmony.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="GunGame"><HintPath>$(H3VR_PROFILE_DIR)\plugins\Kodeman-GunGame\GunGame.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="Sodalite" Condition="Exists('$(H3VR_PROFILE_DIR)\plugins\nrgill28-Sodalite\plugins\Sodalite.dll')"><HintPath>$(H3VR_PROFILE_DIR)\plugins\nrgill28-Sodalite\plugins\Sodalite.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="Atlas" Condition="Exists('$(H3VR_PROFILE_DIR)\plugins\nrgill28-Atlas\Atlas.dll')"><HintPath>$(H3VR_PROFILE_DIR)\plugins\nrgill28-Atlas\Atlas.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="Assembly-CSharp"><HintPath>$(H3VR_MANAGED)\Assembly-CSharp.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="Assembly-CSharp-firstpass"><HintPath>$(H3VR_MANAGED)\Assembly-CSharp-firstpass.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="UnityEngine"><HintPath>$(H3VR_MANAGED)\UnityEngine.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="UnityEngine.UI"><HintPath>$(H3VR_MANAGED)\UnityEngine.UI.dll</HintPath><Private>false</Private></Reference>
  </ItemGroup>
  <Target Name="CopyToProfile" AfterTargets="Build" Condition="Exists('$(H3VR_PROFILE_DIR)\plugins')">
    <ItemGroup>
      <PluginFiles Include="$(OutDir)GunGameArena.dll;$(OutDir)GunGameArena.Core.dll" />
    </ItemGroup>
    <Copy SourceFiles="@(PluginFiles)" DestinationFolder="$(H3VR_PROFILE_DIR)\plugins\GunGameArena" />
    <Message Importance="high" Text="GunGameArena copied to $(H3VR_PROFILE_DIR)\plugins\GunGameArena" />
  </Target>
</Project>
```

`src/GunGameArena/Plugin.cs` (minimal, extended in Task 8):
```csharp
using BepInEx;
using BepInEx.Logging;

namespace GunGameArena
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency("Kodeman.GunGame", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "shaha.GunGameArena";
        public const string Name = "GunGame Arena";
        public const string Version = "0.1.0";

        public static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo(Name + " " + Version + " loaded (scaffold).");
        }
    }
}
```

- [ ] **Step 5: Create the solution and run the build and tests**

Run (from repo root):
```powershell
dotnet new sln -n GunGameArena --force
dotnet sln GunGameArena.sln add src\GunGameArena.Core\GunGameArena.Core.csproj src\GunGameArena\GunGameArena.csproj tests\GunGameArena.Core.Tests\GunGameArena.Core.Tests.csproj
dotnet build GunGameArena.sln -c Release
dotnet test tests\GunGameArena.Core.Tests -c Release
```
Expected: build succeeds for net35 and net8.0; message `GunGameArena copied to …\plugins\GunGameArena`; 2 tests pass. If `dotnet new sln` produces `GunGameArena.slnx`, use that filename in later commands.

If the plugin build fails with missing Sodalite types, check the actual Sodalite path with `Get-ChildItem "$env:APPDATA\Thunderstore Mod Manager\DataFolder\H3VR\profiles\Default\BepInEx\plugins" -Recurse -Filter Sodalite.dll` and fix the HintPath.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "chore: scaffold Core, plugin and test projects with game-referencing build"
```

---

### Task 2: TeamMode and TeamAssigner (Core)

**Files:**
- Create: `src/GunGameArena.Core/TeamMode.cs`, `src/GunGameArena.Core/TeamAssigner.cs`
- Test: `tests/GunGameArena.Core.Tests/TeamAssignerTests.cs`

**Interfaces:**
- Produces: `enum TeamMode { Off, FreeForAll, Teams }`
- Produces: `static class TeamAssigner { const int MaxIff = 31; const int OriginalGunGameIff = 1; int ResolveAllyCount(int allySetting, int sosigCount); int ClampTeamCount(int teamCount); int TeamIndexFor(TeamMode mode, int slot, int sosigCount, int teamCount, int allySetting); int IffFor(TeamMode mode, int teamIndex, int playerIff) }`
- Team index semantics used everywhere later: `0` = player's team, `>=1` = enemy teams. In FFA each sosig has its own team index `slot + 1`.

- [ ] **Step 1: Write the failing tests**

`tests/GunGameArena.Core.Tests/TeamAssignerTests.cs`:
```csharp
using GunGameArena.Core;
using Xunit;

public class TeamAssignerTests
{
    [Fact]
    public void Off_keeps_original_gungame_iff_for_every_slot()
    {
        for (int slot = 0; slot < 8; slot++)
        {
            int team = TeamAssigner.TeamIndexFor(TeamMode.Off, slot, 8, 2, -1);
            Assert.Equal(1, TeamAssigner.IffFor(TeamMode.Off, team, 0));
        }
    }

    [Fact]
    public void FreeForAll_gives_each_slot_a_unique_iff_starting_at_one()
    {
        var seen = new HashSet<int>();
        for (int slot = 0; slot < 8; slot++)
        {
            int team = TeamAssigner.TeamIndexFor(TeamMode.FreeForAll, slot, 8, 2, -1);
            int iff = TeamAssigner.IffFor(TeamMode.FreeForAll, team, 0);
            Assert.True(seen.Add(iff));
            Assert.InRange(iff, 1, 31);
        }
    }

    [Fact]
    public void FreeForAll_never_exceeds_iff_31()
    {
        int team = TeamAssigner.TeamIndexFor(TeamMode.FreeForAll, 40, 41, 2, -1);
        Assert.Equal(31, TeamAssigner.IffFor(TeamMode.FreeForAll, team, 0));
    }

    [Fact]
    public void Teams_puts_first_allies_on_player_team_and_round_robins_the_rest()
    {
        // 8 sosigs, 3 teams, 3 allies -> slots 0..2 team 0, slots 3.. alternate teams 1,2
        int[] expected = { 0, 0, 0, 1, 2, 1, 2, 1 };
        for (int slot = 0; slot < 8; slot++)
            Assert.Equal(expected[slot], TeamAssigner.TeamIndexFor(TeamMode.Teams, slot, 8, 3, 3));
    }

    [Fact]
    public void Teams_default_ally_setting_is_half_rounded_down()
    {
        Assert.Equal(4, TeamAssigner.ResolveAllyCount(-1, 8));
        Assert.Equal(3, TeamAssigner.ResolveAllyCount(-1, 7));
    }

    [Fact]
    public void Teams_always_leaves_at_least_one_enemy()
    {
        Assert.Equal(7, TeamAssigner.ResolveAllyCount(99, 8));
        Assert.Equal(0, TeamAssigner.ResolveAllyCount(0, 8));
        Assert.Equal(0, TeamAssigner.ResolveAllyCount(5, 1));
    }

    [Fact]
    public void Teams_iff_uses_player_iff_for_team_zero_and_team_index_otherwise()
    {
        Assert.Equal(0, TeamAssigner.IffFor(TeamMode.Teams, 0, 0));
        Assert.Equal(2, TeamAssigner.IffFor(TeamMode.Teams, 2, 0));
    }

    [Fact]
    public void TeamCount_is_clamped_between_2_and_4()
    {
        Assert.Equal(2, TeamAssigner.ClampTeamCount(1));
        Assert.Equal(4, TeamAssigner.ClampTeamCount(9));
        Assert.Equal(3, TeamAssigner.ClampTeamCount(3));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests\GunGameArena.Core.Tests -c Release`
Expected: compile errors `TeamMode`/`TeamAssigner` not found.

- [ ] **Step 3: Implement TeamMode and TeamAssigner**

`src/GunGameArena.Core/TeamMode.cs`:
```csharp
namespace GunGameArena.Core
{
    public enum TeamMode
    {
        Off = 0,
        FreeForAll = 1,
        Teams = 2
    }
}
```

`src/GunGameArena.Core/TeamAssigner.cs`:
```csharp
using System;

namespace GunGameArena.Core
{
    /// <summary>Maps a roster slot to a team index and an H3VR IFF code.
    /// Team index 0 is always the player's team; 1.. are enemy teams.</summary>
    public static class TeamAssigner
    {
        public const int MaxIff = 31;               // IFFChart is bool[32]
        public const int OriginalGunGameIff = 1;    // value on every GunGame spawner prefab

        public static int ResolveAllyCount(int allySetting, int sosigCount)
        {
            if (sosigCount <= 0) return 0;
            int allies = allySetting < 0 ? sosigCount / 2 : allySetting;
            return Math.Max(0, Math.Min(allies, sosigCount - 1));
        }

        public static int ClampTeamCount(int teamCount)
        {
            return Math.Max(2, Math.Min(4, teamCount));
        }

        public static int TeamIndexFor(TeamMode mode, int slot, int sosigCount, int teamCount, int allySetting)
        {
            switch (mode)
            {
                case TeamMode.FreeForAll:
                    return slot + 1;
                case TeamMode.Teams:
                {
                    int allies = ResolveAllyCount(allySetting, sosigCount);
                    if (slot < allies) return 0;
                    int enemyTeams = ClampTeamCount(teamCount) - 1;
                    return 1 + (slot - allies) % enemyTeams;
                }
                default:
                    return 1;
            }
        }

        public static int IffFor(TeamMode mode, int teamIndex, int playerIff)
        {
            switch (mode)
            {
                case TeamMode.FreeForAll:
                    return Math.Min(Math.Max(1, teamIndex), MaxIff);
                case TeamMode.Teams:
                    return teamIndex == 0 ? playerIff : teamIndex;
                default:
                    return OriginalGunGameIff;
            }
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests\GunGameArena.Core.Tests -c Release`
Expected: all TeamAssigner tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/GunGameArena.Core/TeamMode.cs src/GunGameArena.Core/TeamAssigner.cs tests/GunGameArena.Core.Tests/TeamAssignerTests.cs
git commit -m "feat(core): team mode and slot-to-IFF assignment"
```

---

### Task 3: Contestant and Ranking (Core)

**Files:**
- Create: `src/GunGameArena.Core/Contestant.cs`, `src/GunGameArena.Core/Ranking.cs`
- Test: `tests/GunGameArena.Core.Tests/RankingTests.cs`

**Interfaces:**
- Produces: `class Contestant { int Id; string Name; int TeamIndex; int Iff; int Kills; float LastKillTime; bool IsPlayer; bool IsAlive; Vec3 Position; SkillTier Tier; void AddKill(float time) }` (`SkillTier` is defined in Task 6; until then declare it in this task as shown below so the project compiles.)
- Produces: `static class Ranking { List<Contestant> Sort(IEnumerable<Contestant>); List<Contestant> Visible(IList<Contestant> sorted, int topCount); HashSet<int> CrownedIds(IList<Contestant> sorted, TeamMode mode); bool IsPinnedPlayer(IList<Contestant> visible, int topCount, Contestant c) }`

- [ ] **Step 1: Write the failing tests**

`tests/GunGameArena.Core.Tests/RankingTests.cs`:
```csharp
using GunGameArena.Core;
using Xunit;

public class RankingTests
{
    static Contestant C(int id, int kills, float lastKill = 0f, bool player = false, int team = 1)
        => new Contestant { Id = id, Name = "c" + id, Kills = kills, LastKillTime = lastKill, IsPlayer = player, TeamIndex = team, IsAlive = true };

    [Fact]
    public void Sort_orders_by_kills_desc_then_earliest_kill_then_id()
    {
        var sorted = Ranking.Sort(new[] { C(1, 2, 30f), C(2, 5), C(3, 2, 10f), C(4, 2, 10f) });
        Assert.Equal(new[] { 2, 3, 4, 1 }, sorted.Select(c => c.Id).ToArray());
    }

    [Fact]
    public void Visible_returns_top_n_when_player_is_inside()
    {
        var sorted = Ranking.Sort(new[] { C(1, 9), C(2, 8, player: true), C(3, 7), C(4, 6), C(5, 5), C(6, 4), C(7, 3) });
        var visible = Ranking.Visible(sorted, 5);
        Assert.Equal(5, visible.Count);
        Assert.Contains(visible, c => c.IsPlayer);
    }

    [Fact]
    public void Visible_pins_player_at_end_when_outside_top_n()
    {
        var sorted = Ranking.Sort(new[] { C(1, 9), C(2, 8), C(3, 7), C(4, 6), C(5, 5), C(6, 4), C(7, 0, player: true) });
        var visible = Ranking.Visible(sorted, 5);
        Assert.Equal(6, visible.Count);
        Assert.True(visible[5].IsPlayer);
        Assert.True(Ranking.IsPinnedPlayer(visible, 5, visible[5]));
        Assert.False(Ranking.IsPinnedPlayer(visible, 5, visible[0]));
    }

    [Fact]
    public void FFA_crowns_only_rank_one_and_only_with_kills()
    {
        var sorted = Ranking.Sort(new[] { C(1, 3), C(2, 3, 5f), C(3, 1) });
        var crowned = Ranking.CrownedIds(sorted, TeamMode.FreeForAll);
        Assert.Single(crowned);
        Assert.Contains(sorted[0].Id, crowned);

        var none = Ranking.CrownedIds(Ranking.Sort(new[] { C(1, 0), C(2, 0) }), TeamMode.FreeForAll);
        Assert.Empty(none);
    }

    [Fact]
    public void Teams_crowns_best_of_each_team_with_kills_only()
    {
        var sorted = Ranking.Sort(new[] { C(1, 4, team: 1), C(2, 3, team: 0, player: true), C(3, 2, team: 1), C(4, 0, team: 2) });
        var crowned = Ranking.CrownedIds(sorted, TeamMode.Teams);
        Assert.Equal(2, crowned.Count);
        Assert.Contains(1, crowned);
        Assert.Contains(2, crowned);
        Assert.DoesNotContain(4, crowned);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests\GunGameArena.Core.Tests -c Release`
Expected: compile errors for `Contestant`, `Ranking`.

- [ ] **Step 3: Implement Contestant and Ranking**

`src/GunGameArena.Core/Contestant.cs`:
```csharp
namespace GunGameArena.Core
{
    /// <summary>One leaderboard row. Persists for the whole round; a respawned sosig
    /// re-binds to the same contestant.</summary>
    public class Contestant
    {
        public int Id;
        public string Name;
        public int TeamIndex;      // 0 = player's team
        public int Iff;
        public int Kills;
        public float LastKillTime; // game time of most recent kill; tie-break
        public bool IsPlayer;
        public bool IsAlive;
        public Vec3 Position;      // refreshed by the plugin before attribution
        public SkillTier Tier = SkillTier.Regular;

        public void AddKill(float time)
        {
            Kills++;
            LastKillTime = time;
        }

        public override string ToString() { return Name + "#" + Id + " t" + TeamIndex + " iff" + Iff + " k" + Kills; }
    }
}
```

Temporary `src/GunGameArena.Core/SkillTier.cs` (replaced with the full version in Task 6):
```csharp
namespace GunGameArena.Core
{
    public enum SkillTier { Rookie = 0, Regular = 1, Veteran = 2, Elite = 3 }
}
```

`src/GunGameArena.Core/Ranking.cs`:
```csharp
using System.Collections.Generic;

namespace GunGameArena.Core
{
    public static class Ranking
    {
        public static List<Contestant> Sort(IEnumerable<Contestant> all)
        {
            var list = new List<Contestant>(all);
            list.Sort(Compare);
            return list;
        }

        private static int Compare(Contestant a, Contestant b)
        {
            int c = b.Kills.CompareTo(a.Kills);
            if (c != 0) return c;
            c = a.LastKillTime.CompareTo(b.LastKillTime);
            if (c != 0) return c;
            return a.Id.CompareTo(b.Id);
        }

        /// <summary>Top N in order, then the player appended if not already shown.</summary>
        public static List<Contestant> Visible(IList<Contestant> sorted, int topCount)
        {
            var result = new List<Contestant>();
            for (int i = 0; i < sorted.Count && i < topCount; i++) result.Add(sorted[i]);
            for (int i = 0; i < sorted.Count; i++)
            {
                if (sorted[i].IsPlayer && !result.Contains(sorted[i])) { result.Add(sorted[i]); break; }
            }
            return result;
        }

        public static bool IsPinnedPlayer(IList<Contestant> visible, int topCount, Contestant c)
        {
            return c.IsPlayer && visible.IndexOf(c) >= topCount;
        }

        /// <summary>FFA/Off: rank 1 only. Teams: best of each team. Always requires Kills &gt; 0.</summary>
        public static HashSet<int> CrownedIds(IList<Contestant> sorted, TeamMode mode)
        {
            var crowned = new HashSet<int>();
            if (mode == TeamMode.Teams)
            {
                var seenTeams = new HashSet<int>();
                for (int i = 0; i < sorted.Count; i++)
                {
                    var c = sorted[i];
                    if (seenTeams.Add(c.TeamIndex) && c.Kills > 0) crowned.Add(c.Id);
                }
            }
            else if (sorted.Count > 0 && sorted[0].Kills > 0)
            {
                crowned.Add(sorted[0].Id);
            }
            return crowned;
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests\GunGameArena.Core.Tests -c Release`
Expected: all PASS.

- [ ] **Step 5: Commit**

```bash
git add src/GunGameArena.Core tests/GunGameArena.Core.Tests/RankingTests.cs
git commit -m "feat(core): contestant model, ranking, visible set and crown rules"
```

---
### Task 4: KillAttribution (Core)

**Files:**
- Create: `src/GunGameArena.Core/KillAttribution.cs`
- Test: `tests/GunGameArena.Core.Tests/KillAttributionTests.cs`

**Interfaces:**
- Produces: `static class KillAttribution { Contestant Nearest(IEnumerable<Contestant> candidates, int killerIff, int victimId, Vec3 point) }` — returns `null` when `killerIff < 0` or no living candidate with that IFF other than the victim exists.

- [ ] **Step 1: Write the failing tests**

`tests/GunGameArena.Core.Tests/KillAttributionTests.cs`:
```csharp
using GunGameArena.Core;
using Xunit;

public class KillAttributionTests
{
    static Contestant C(int id, int iff, float x, bool alive = true, bool player = false)
        => new Contestant { Id = id, Iff = iff, Position = new Vec3(x, 0, 0), IsAlive = alive, IsPlayer = player };

    [Fact]
    public void Picks_nearest_living_candidate_on_killer_team()
    {
        var all = new[] { C(1, 3, 10f), C(2, 3, 2f), C(3, 4, 1f) };
        var winner = KillAttribution.Nearest(all, 3, victimId: 99, new Vec3(0, 0, 0));
        Assert.Equal(2, winner.Id);
    }

    [Fact]
    public void Ignores_dead_candidates()
    {
        var all = new[] { C(1, 3, 10f), C(2, 3, 1f, alive: false) };
        Assert.Equal(1, KillAttribution.Nearest(all, 3, 99, Vec3.Zero).Id);
    }

    [Fact]
    public void Never_credits_the_victim()
    {
        var all = new[] { C(7, 3, 0f), C(1, 3, 10f) };
        Assert.Equal(1, KillAttribution.Nearest(all, 3, victimId: 7, Vec3.Zero).Id);
    }

    [Fact]
    public void Returns_null_for_negative_iff_or_no_candidates()
    {
        var all = new[] { C(1, 3, 10f) };
        Assert.Null(KillAttribution.Nearest(all, -1, 99, Vec3.Zero));
        Assert.Null(KillAttribution.Nearest(all, 5, 99, Vec3.Zero));
    }

    [Fact]
    public void Player_can_win_attribution()
    {
        var all = new[] { C(0, 0, 1f, player: true), C(1, 0, 5f) };
        Assert.True(KillAttribution.Nearest(all, 0, 99, Vec3.Zero).IsPlayer);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests\GunGameArena.Core.Tests -c Release`
Expected: compile error `KillAttribution` not found.

- [ ] **Step 3: Implement**

`src/GunGameArena.Core/KillAttribution.cs`:
```csharp
using System.Collections.Generic;

namespace GunGameArena.Core
{
    public static class KillAttribution
    {
        /// <summary>The living contestant with the killer's IFF nearest to where the fatal
        /// shot came from. Returns null when nobody qualifies.</summary>
        public static Contestant Nearest(IEnumerable<Contestant> candidates, int killerIff, int victimId, Vec3 point)
        {
            if (killerIff < 0) return null;
            Contestant best = null;
            float bestDist = float.MaxValue;
            foreach (var c in candidates)
            {
                if (c == null || !c.IsAlive || c.Iff != killerIff || c.Id == victimId) continue;
                float d = Vec3.Distance(c.Position, point);
                if (d < bestDist) { bestDist = d; best = c; }
            }
            return best;
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests\GunGameArena.Core.Tests -c Release`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/GunGameArena.Core/KillAttribution.cs tests/GunGameArena.Core.Tests/KillAttributionTests.cs
git commit -m "feat(core): nearest-candidate kill attribution"
```

---

### Task 5: NameGenerator (Core)

**Files:**
- Create: `src/GunGameArena.Core/NameGenerator.cs`
- Test: `tests/GunGameArena.Core.Tests/NameGeneratorTests.cs`

**Interfaces:**
- Produces: `class NameGenerator { const int MaxLength = 16; NameGenerator(int seed /*0 = random*/); string Next(); }` — names unique per instance.

- [ ] **Step 1: Write the failing tests**

`tests/GunGameArena.Core.Tests/NameGeneratorTests.cs`:
```csharp
using GunGameArena.Core;
using Xunit;

public class NameGeneratorTests
{
    [Fact]
    public void Generates_unique_names_within_length()
    {
        var gen = new NameGenerator(1234);
        var seen = new HashSet<string>();
        for (int i = 0; i < 32; i++)
        {
            string n = gen.Next();
            Assert.False(string.IsNullOrEmpty(n));
            Assert.True(n.Length <= NameGenerator.MaxLength, n);
            Assert.True(seen.Add(n), "duplicate " + n);
        }
    }

    [Fact]
    public void Same_seed_gives_same_sequence()
    {
        var a = new NameGenerator(42);
        var b = new NameGenerator(42);
        for (int i = 0; i < 10; i++) Assert.Equal(a.Next(), b.Next());
    }

    [Fact]
    public void Keeps_producing_after_many_names()
    {
        var gen = new NameGenerator(7);
        var seen = new HashSet<string>();
        for (int i = 0; i < 500; i++) Assert.True(seen.Add(gen.Next()));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests\GunGameArena.Core.Tests -c Release`
Expected: compile error.

- [ ] **Step 3: Implement**

`src/GunGameArena.Core/NameGenerator.cs`:
```csharp
using System;
using System.Collections.Generic;

namespace GunGameArena.Core
{
    /// <summary>Roblox-style usernames, unique per instance. Words are kept to 7 chars so
    /// every decorator fits in MaxLength.</summary>
    public class NameGenerator
    {
        public const int MaxLength = 16;

        private static readonly string[] Adjectives =
        {
            "Spicy", "Crispy", "Smoky", "Salty", "Angry", "Sneaky", "Turbo", "Mega", "Tiny", "Chunky",
            "Greasy", "Soggy", "Frozen", "Rusty", "Shiny", "Lucky", "Silent", "Loud", "Toxic", "Cursed",
            "Epic", "Dank", "Sus", "Cool", "Evil", "Happy", "Fried", "Grilled", "Juicy", "Pickled",
            "Rapid", "Sleepy", "Ghost", "Iron", "Neon", "Pixel", "Retro", "Wild", "Zesty", "Ultra"
        };

        private static readonly string[] Nouns =
        {
            "Glizzy", "Wiener", "Sosig", "Mustard", "Brat", "Hotdog", "Ketchup", "Relish", "Bun", "Kebab",
            "Sniper", "Ninja", "Gamer", "Toaster", "Goblin", "Wizard", "Pirate", "Knight", "Robot", "Duck",
            "Potato", "Pickle", "Noodle", "Waffle", "Nugget", "Bacon", "Salami", "Chorizo", "Frank", "Dog",
            "Slayer", "Hunter", "Camper", "Rusher", "Boomer", "Zoomer", "Gremlin", "Meatman", "Tank", "Yeet"
        };

        private readonly Random _rng;
        private readonly HashSet<string> _used = new HashSet<string>();

        public NameGenerator(int seed)
        {
            _rng = seed == 0 ? new Random() : new Random(seed);
        }

        public string Next()
        {
            for (int attempt = 0; attempt < 50; attempt++)
            {
                string n = Compose();
                if (n.Length <= MaxLength && _used.Add(n)) return n;
            }
            while (true)
            {
                string noun = Pick(Nouns);
                if (noun.Length > MaxLength - 4) noun = noun.Substring(0, MaxLength - 4);
                string n = noun + _rng.Next(1000, 9999);
                if (_used.Add(n)) return n;
            }
        }

        private string Compose()
        {
            string adj = Pick(Adjectives);
            string noun = Pick(Nouns);
            switch (_rng.Next(7))
            {
                case 0: return adj + noun;
                case 1: return adj.ToLowerInvariant() + "_" + noun.ToLowerInvariant();
                case 2: return "xX_" + noun + "_Xx";
                case 3: return noun + _rng.Next(10, 9999);
                case 4: return adj + noun + _rng.Next(10, 99);
                case 5: return "iL" + noun;
                default: return noun + "YT";
            }
        }

        private string Pick(string[] words) { return words[_rng.Next(words.Length)]; }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests\GunGameArena.Core.Tests -c Release`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/GunGameArena.Core/NameGenerator.cs tests/GunGameArena.Core.Tests/NameGeneratorTests.cs
git commit -m "feat(core): roblox-style unique name generator"
```

---

### Task 6: HudPalette and SkillTier (Core)

**Files:**
- Create: `src/GunGameArena.Core/HudPalette.cs`
- Modify: `src/GunGameArena.Core/SkillTier.cs` (replace the Task 3 stub)
- Test: `tests/GunGameArena.Core.Tests/HudPaletteTests.cs`, `tests/GunGameArena.Core.Tests/SkillTierTests.cs`

**Interfaces:**
- Produces: `struct Rgba { float R,G,B,A; Rgba(r,g,b,a); static Rgba Hex(string rrggbb, float a = 1f); Rgba Darken(float factor) }`
- Produces: `static class HudPalette { Rgba FfaCard, FfaBorder, Gold, White, Black, HeaderBg, TeamBlue, TeamRed, TeamGreen, TeamYellow; Rgba TeamColor(int teamIndex); Rgba CardBorder(TeamMode mode, int teamIndex, bool isRankOne); Rgba CardBackground(TeamMode mode, int teamIndex) }`
- Produces: `enum SkillTier { Rookie, Regular, Veteran, Elite }`, `struct TierMultipliers { float Spread, FireAngle, Refire, Reaction }`, `static class TierTable { int[] DefaultWeights; TierMultipliers Default(SkillTier); int Chevrons(SkillTier) }`, `static class TierRoller { SkillTier Roll(Random rng, int[] weights); int[] ParseWeights(string csv) }`

- [ ] **Step 1: Write the failing tests**

`tests/GunGameArena.Core.Tests/HudPaletteTests.cs`:
```csharp
using GunGameArena.Core;
using Xunit;

public class HudPaletteTests
{
    [Fact]
    public void Hex_parses_rrggbb()
    {
        var c = Rgba.Hex("F5C542");
        Assert.Equal(0xF5 / 255f, c.R, 3);
        Assert.Equal(0xC5 / 255f, c.G, 3);
        Assert.Equal(0x42 / 255f, c.B, 3);
        Assert.Equal(1f, c.A);
    }

    [Fact]
    public void Team_colours_follow_spec_order()
    {
        Assert.Equal(HudPalette.TeamBlue, HudPalette.TeamColor(0));
        Assert.Equal(HudPalette.TeamRed, HudPalette.TeamColor(1));
        Assert.Equal(HudPalette.TeamGreen, HudPalette.TeamColor(2));
        Assert.Equal(HudPalette.TeamYellow, HudPalette.TeamColor(3));
        Assert.Equal(HudPalette.TeamYellow, HudPalette.TeamColor(9));
    }

    [Fact]
    public void Ffa_border_is_gold_only_for_rank_one()
    {
        Assert.Equal(HudPalette.Gold, HudPalette.CardBorder(TeamMode.FreeForAll, 3, true));
        Assert.Equal(HudPalette.FfaBorder, HudPalette.CardBorder(TeamMode.FreeForAll, 3, false));
        Assert.Equal(HudPalette.TeamRed, HudPalette.CardBorder(TeamMode.Teams, 1, true));
    }
}
```

`tests/GunGameArena.Core.Tests/SkillTierTests.cs`:
```csharp
using GunGameArena.Core;
using Xunit;

public class SkillTierTests
{
    [Fact]
    public void Default_multipliers_match_spec()
    {
        var r = TierTable.Default(SkillTier.Rookie);
        Assert.Equal(2.5f, r.Spread); Assert.Equal(2.0f, r.FireAngle); Assert.Equal(1.3f, r.Refire); Assert.Equal(0.6f, r.Reaction);
        var e = TierTable.Default(SkillTier.Elite);
        Assert.Equal(0.45f, e.Spread); Assert.Equal(0.6f, e.FireAngle); Assert.Equal(0.8f, e.Refire); Assert.Equal(1.6f, e.Reaction);
        Assert.Equal(1f, TierTable.Default(SkillTier.Regular).Spread);
    }

    [Fact]
    public void Chevrons_are_one_to_four()
    {
        Assert.Equal(1, TierTable.Chevrons(SkillTier.Rookie));
        Assert.Equal(4, TierTable.Chevrons(SkillTier.Elite));
    }

    [Fact]
    public void Roll_respects_weights_over_many_samples()
    {
        var rng = new Random(1);
        var counts = new int[4];
        for (int i = 0; i < 10000; i++) counts[(int)TierRoller.Roll(rng, new[] { 30, 40, 20, 10 })]++;
        Assert.InRange(counts[0], 2600, 3400);
        Assert.InRange(counts[1], 3600, 4400);
        Assert.InRange(counts[2], 1600, 2400);
        Assert.InRange(counts[3], 700, 1300);
    }

    [Fact]
    public void Roll_with_bad_weights_returns_regular()
    {
        Assert.Equal(SkillTier.Regular, TierRoller.Roll(new Random(1), null));
        Assert.Equal(SkillTier.Regular, TierRoller.Roll(new Random(1), new[] { 0, 0, 0, 0 }));
        Assert.Equal(SkillTier.Regular, TierRoller.Roll(new Random(1), new[] { 1, 2 }));
    }

    [Fact]
    public void ParseWeights_reads_csv_and_falls_back_to_default()
    {
        Assert.Equal(new[] { 1, 2, 3, 4 }, TierRoller.ParseWeights("1, 2,3 ,4"));
        Assert.Equal(TierTable.DefaultWeights, TierRoller.ParseWeights("garbage"));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests\GunGameArena.Core.Tests -c Release`
Expected: compile errors.

- [ ] **Step 3: Implement**

`src/GunGameArena.Core/HudPalette.cs`:
```csharp
using System;
using System.Globalization;

namespace GunGameArena.Core
{
    public struct Rgba
    {
        public float R, G, B, A;
        public Rgba(float r, float g, float b, float a) { R = r; G = g; B = b; A = a; }

        public static Rgba Hex(string rrggbb, float a = 1f)
        {
            int v = int.Parse(rrggbb.TrimStart('#'), NumberStyles.HexNumber);
            return new Rgba(((v >> 16) & 0xFF) / 255f, ((v >> 8) & 0xFF) / 255f, (v & 0xFF) / 255f, a);
        }

        public Rgba Darken(float factor) { return new Rgba(R * factor, G * factor, B * factor, A); }

        public override bool Equals(object obj)
        {
            if (!(obj is Rgba)) return false;
            var o = (Rgba)obj;
            return R == o.R && G == o.G && B == o.B && A == o.A;
        }
        public override int GetHashCode() { return R.GetHashCode() ^ G.GetHashCode() ^ B.GetHashCode() ^ A.GetHashCode(); }
        public static bool operator ==(Rgba a, Rgba b) { return a.Equals(b); }
        public static bool operator !=(Rgba a, Rgba b) { return !a.Equals(b); }
    }

    public static class HudPalette
    {
        public static readonly Rgba FfaCard = Rgba.Hex("2B2B2B");
        public static readonly Rgba FfaBorder = Rgba.Hex("555555");
        public static readonly Rgba Gold = Rgba.Hex("F5C542");
        public static readonly Rgba White = new Rgba(1, 1, 1, 1);
        public static readonly Rgba Black = new Rgba(0, 0, 0, 1);
        public static readonly Rgba HeaderBg = Rgba.Hex("1A1A1A", 0.85f);
        public static readonly Rgba TeamBlue = Rgba.Hex("1E3FA8");
        public static readonly Rgba TeamRed = Rgba.Hex("A81E1E");
        public static readonly Rgba TeamGreen = Rgba.Hex("1E8A3A");
        public static readonly Rgba TeamYellow = Rgba.Hex("C9A400");

        public static Rgba TeamColor(int teamIndex)
        {
            switch (teamIndex)
            {
                case 0: return TeamBlue;
                case 1: return TeamRed;
                case 2: return TeamGreen;
                default: return TeamYellow;
            }
        }

        public static Rgba CardBorder(TeamMode mode, int teamIndex, bool isRankOne)
        {
            if (mode == TeamMode.Teams) return TeamColor(teamIndex);
            return isRankOne ? Gold : FfaBorder;
        }

        public static Rgba CardBackground(TeamMode mode, int teamIndex)
        {
            return mode == TeamMode.Teams ? TeamColor(teamIndex).Darken(0.55f) : FfaCard;
        }
    }
}
```

`src/GunGameArena.Core/SkillTier.cs` (full version):
```csharp
using System;

namespace GunGameArena.Core
{
    public enum SkillTier { Rookie = 0, Regular = 1, Veteran = 2, Elite = 3 }

    public struct TierMultipliers
    {
        public float Spread;     // SosigWeapon.ProjectileSpread
        public float FireAngle;  // SosigWeapon.MaxAngularFireRange
        public float Refire;     // SosigWeapon.Usage_RefireRange
        public float Reaction;   // Sosig recognition / identification speed
        public TierMultipliers(float spread, float fireAngle, float refire, float reaction)
        { Spread = spread; FireAngle = fireAngle; Refire = refire; Reaction = reaction; }
    }

    public static class TierTable
    {
        public static readonly int[] DefaultWeights = { 30, 40, 20, 10 };

        public static TierMultipliers Default(SkillTier tier)
        {
            switch (tier)
            {
                case SkillTier.Rookie: return new TierMultipliers(2.5f, 2.0f, 1.3f, 0.6f);
                case SkillTier.Veteran: return new TierMultipliers(0.7f, 0.8f, 0.9f, 1.3f);
                case SkillTier.Elite: return new TierMultipliers(0.45f, 0.6f, 0.8f, 1.6f);
                default: return new TierMultipliers(1f, 1f, 1f, 1f);
            }
        }

        public static int Chevrons(SkillTier tier) { return (int)tier + 1; }
    }

    public static class TierRoller
    {
        public static SkillTier Roll(Random rng, int[] weights)
        {
            if (weights == null || weights.Length != 4) return SkillTier.Regular;
            int sum = 0;
            for (int i = 0; i < 4; i++) sum += Math.Max(0, weights[i]);
            if (sum <= 0) return SkillTier.Regular;
            int r = rng.Next(sum);
            int acc = 0;
            for (int i = 0; i < 4; i++)
            {
                acc += Math.Max(0, weights[i]);
                if (r < acc) return (SkillTier)i;
            }
            return SkillTier.Regular;
        }

        /// <summary>"30,40,20,10" → int[4]; anything malformed → TierTable.DefaultWeights.</summary>
        public static int[] ParseWeights(string csv)
        {
            if (string.IsNullOrEmpty(csv)) return TierTable.DefaultWeights;
            string[] parts = csv.Split(',');
            if (parts.Length != 4) return TierTable.DefaultWeights;
            var result = new int[4];
            for (int i = 0; i < 4; i++)
            {
                int v;
                if (!int.TryParse(parts[i].Trim(), out v)) return TierTable.DefaultWeights;
                result[i] = v;
            }
            return result;
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests\GunGameArena.Core.Tests -c Release`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/GunGameArena.Core/HudPalette.cs src/GunGameArena.Core/SkillTier.cs tests/GunGameArena.Core.Tests/HudPaletteTests.cs tests/GunGameArena.Core.Tests/SkillTierTests.cs
git commit -m "feat(core): HUD palette and skill tiers with weighted roller"
```

---

### Task 7: RivalSelector, HunterPicker, SpawnerChooser (Core)

**Files:**
- Create: `src/GunGameArena.Core/RivalSelector.cs`, `src/GunGameArena.Core/HunterPicker.cs`, `src/GunGameArena.Core/SpawnerChooser.cs`
- Test: `tests/GunGameArena.Core.Tests/BehaviourPickersTests.cs`

**Interfaces:**
- Produces: `static class RivalSelector { List<Contestant> Pick(Random rng, Contestant self, IEnumerable<Contestant> all, int count, float radius, float playerWeight) }`
- Produces: `static class HunterPicker { int Count(int hostileCount, float share); List<Contestant> Pick(Random rng, IList<Contestant> hostileToPlayer, float share) }`
- Produces: `static class SpawnerChooser { int Choose(Random rng, IList<Vec3> spawners, int ignoreNear, int ignoreFar, Vec3 player, IList<Vec3> occupied) }` — returns index into `spawners`, or -1 if empty.

- [ ] **Step 1: Write the failing tests**

`tests/GunGameArena.Core.Tests/BehaviourPickersTests.cs`:
```csharp
using GunGameArena.Core;
using Xunit;

public class BehaviourPickersTests
{
    static Contestant C(int id, float x, bool player = false, bool alive = true)
        => new Contestant { Id = id, Iff = id, Position = new Vec3(x, 0, 0), IsPlayer = player, IsAlive = alive };

    [Fact]
    public void Rivals_never_include_self_or_dead_and_respect_count()
    {
        var self = C(1, 0);
        var all = new[] { self, C(2, 5), C(3, 6, alive: false), C(4, 7), C(5, 8), C(0, 9, player: true) };
        var rivals = RivalSelector.Pick(new Random(3), self, all, 3, 40f, 2f);
        Assert.Equal(3, rivals.Count);
        Assert.DoesNotContain(rivals, r => r.Id == 1);
        Assert.DoesNotContain(rivals, r => r.Id == 3);
        Assert.Equal(3, rivals.Select(r => r.Id).Distinct().Count());
    }

    [Fact]
    public void Rivals_prefer_inside_radius_but_fill_from_nearest_outside()
    {
        var self = C(1, 0);
        var all = new[] { self, C(2, 5), C(3, 100), C(4, 200) };
        var rivals = RivalSelector.Pick(new Random(3), self, all, 2, 10f, 1f);
        Assert.Contains(rivals, r => r.Id == 2);
        Assert.Contains(rivals, r => r.Id == 3);
    }

    [Fact]
    public void Player_is_always_eligible_and_weighted()
    {
        var self = C(1, 0);
        var all = new[] { self, C(2, 5), C(0, 500, player: true) };
        int playerPicked = 0;
        for (int seed = 0; seed < 200; seed++)
            if (RivalSelector.Pick(new Random(seed), self, all, 1, 10f, 3f).Any(r => r.IsPlayer)) playerPicked++;
        Assert.InRange(playerPicked, 120, 180); // weight 3 vs 1 → ~75%
    }

    [Fact]
    public void Hunter_count_is_ceil_of_share()
    {
        Assert.Equal(2, HunterPicker.Count(8, 0.25f));
        Assert.Equal(2, HunterPicker.Count(5, 0.25f));
        Assert.Equal(0, HunterPicker.Count(0, 0.25f));
        Assert.Equal(8, HunterPicker.Count(8, 5f));
    }

    [Fact]
    public void Hunter_pick_returns_distinct_subset()
    {
        var pool = Enumerable.Range(1, 8).Select(i => C(i, i)).ToList();
        var picked = HunterPicker.Pick(new Random(9), pool, 0.5f);
        Assert.Equal(4, picked.Count);
        Assert.Equal(4, picked.Select(p => p.Id).Distinct().Count());
    }

    [Fact]
    public void SpawnerChooser_picks_farthest_from_everyone_within_allowed_band()
    {
        // spawners on a line; player at 0; sosig at 100
        var spawners = new List<Vec3> { new Vec3(1, 0, 0), new Vec3(2, 0, 0), new Vec3(50, 0, 0), new Vec3(99, 0, 0) };
        var occupied = new List<Vec3> { new Vec3(100, 0, 0) };
        int idx = SpawnerChooser.Choose(new Random(1), spawners, ignoreNear: 2, ignoreFar: 0, new Vec3(0, 0, 0), occupied);
        Assert.Equal(2, idx); // 50 is far from player (50) and sosig (50); 99 is 1 from sosig
    }

    [Fact]
    public void SpawnerChooser_falls_back_when_band_is_empty_and_handles_no_spawners()
    {
        var spawners = new List<Vec3> { new Vec3(1, 0, 0), new Vec3(2, 0, 0) };
        int idx = SpawnerChooser.Choose(new Random(1), spawners, 5, 5, Vec3.Zero, new List<Vec3>());
        Assert.InRange(idx, 0, 1);
        Assert.Equal(-1, SpawnerChooser.Choose(new Random(1), new List<Vec3>(), 2, 0, Vec3.Zero, new List<Vec3>()));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests\GunGameArena.Core.Tests -c Release`
Expected: compile errors.

- [ ] **Step 3: Implement the three pickers**

`src/GunGameArena.Core/RivalSelector.cs`:
```csharp
using System;
using System.Collections.Generic;

namespace GunGameArena.Core
{
    public static class RivalSelector
    {
        public static List<Contestant> Pick(Random rng, Contestant self, IEnumerable<Contestant> all, int count, float radius, float playerWeight)
        {
            var pool = new List<Contestant>();
            var far = new List<Contestant>();
            foreach (var c in all)
            {
                if (c == null || c == self || c.Id == self.Id || !c.IsAlive) continue;
                if (c.IsPlayer || Vec3.Distance(c.Position, self.Position) <= radius) pool.Add(c);
                else far.Add(c);
            }
            if (pool.Count < count && far.Count > 0)
            {
                far.Sort((a, b) => Vec3.Distance(a.Position, self.Position).CompareTo(Vec3.Distance(b.Position, self.Position)));
                for (int i = 0; i < far.Count && pool.Count < count; i++) pool.Add(far[i]);
            }

            var result = new List<Contestant>();
            while (result.Count < count && pool.Count > 0)
            {
                float total = 0f;
                for (int i = 0; i < pool.Count; i++) total += pool[i].IsPlayer ? playerWeight : 1f;
                double r = rng.NextDouble() * total;
                float acc = 0f;
                int chosen = pool.Count - 1;
                for (int i = 0; i < pool.Count; i++)
                {
                    acc += pool[i].IsPlayer ? playerWeight : 1f;
                    if (r < acc) { chosen = i; break; }
                }
                result.Add(pool[chosen]);
                pool.RemoveAt(chosen);
            }
            return result;
        }
    }
}
```

`src/GunGameArena.Core/HunterPicker.cs`:
```csharp
using System;
using System.Collections.Generic;

namespace GunGameArena.Core
{
    public static class HunterPicker
    {
        public static int Count(int hostileCount, float share)
        {
            if (hostileCount <= 0 || share <= 0f) return 0;
            return Math.Min(hostileCount, (int)Math.Ceiling(share * hostileCount));
        }

        public static List<Contestant> Pick(Random rng, IList<Contestant> hostileToPlayer, float share)
        {
            var result = new List<Contestant>();
            int count = Count(hostileToPlayer.Count, share);
            var pool = new List<Contestant>(hostileToPlayer);
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int idx = rng.Next(pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            return result;
        }
    }
}
```

`src/GunGameArena.Core/SpawnerChooser.cs`:
```csharp
using System;
using System.Collections.Generic;

namespace GunGameArena.Core
{
    public static class SpawnerChooser
    {
        /// <summary>Among spawners allowed by GunGame's near/far exclusion (ordered by distance to
        /// the player), choose the one maximising the minimum distance to the player and every
        /// occupied position. Ties broken randomly. -1 when there are no spawners.</summary>
        public static int Choose(Random rng, IList<Vec3> spawners, int ignoreNear, int ignoreFar, Vec3 player, IList<Vec3> occupied)
        {
            int n = spawners.Count;
            if (n == 0) return -1;

            var order = new List<int>(n);
            for (int i = 0; i < n; i++) order.Add(i);
            order.Sort((a, b) => Vec3.Distance(spawners[a], player).CompareTo(Vec3.Distance(spawners[b], player)));

            int start = Math.Max(0, ignoreNear);
            int end = n - Math.Max(0, ignoreFar);
            if (end - start <= 0) { start = 0; end = n; }

            float bestScore = -1f;
            var best = new List<int>();
            for (int k = start; k < end; k++)
            {
                int idx = order[k];
                float score = Vec3.Distance(spawners[idx], player);
                for (int j = 0; j < occupied.Count; j++)
                    score = Math.Min(score, Vec3.Distance(spawners[idx], occupied[j]));
                if (score > bestScore + 0.0001f) { bestScore = score; best.Clear(); best.Add(idx); }
                else if (Math.Abs(score - bestScore) <= 0.0001f) best.Add(idx);
            }
            return best[rng.Next(best.Count)];
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests\GunGameArena.Core.Tests -c Release`
Expected: PASS (all Core tests, Tasks 1–7).

- [ ] **Step 5: Commit**

```bash
git add src/GunGameArena.Core tests/GunGameArena.Core.Tests/BehaviourPickersTests.cs
git commit -m "feat(core): rival, hunter and spawner selection logic"
```

---
### Task 8: Plugin entry, config and GunGame lifecycle hooks

**Files:**
- Modify: `src/GunGameArena/Plugin.cs`
- Create: `src/GunGameArena/ArenaConfig.cs`, `src/GunGameArena/GunGameHooks.cs`

**Interfaces:**
- Produces: `ArenaConfig` static class with `Bind(ConfigFile)` and `ConfigEntry<T>` fields named exactly: `Mode`, `TeamCount`, `AllySosigs`, `LeaderboardEnabled`, `TopCount`, `Scale`, `Distance`, `Height`, `ShowNames`, `ShowTierBadge`, `NameSeed`, `SpreadSpawns`, `Grudges`, `RivalCount`, `RivalRadius`, `PlayerRivalWeight`, `RivalRerollMin`, `RivalRerollMax`, `Hunters`, `HunterShare`, `HunterIntervalMin`, `HunterIntervalMax`, `SkillTiers`, `TierWeights`; plus `TierMultipliers MultipliersFor(SkillTier)`.
- Produces: `GunGameHooks.Install()/Uninstall()`; static events `GunGameHooks.RoundStarting` (before GunGame spawns) and `GunGameHooks.RoundStarted` (after) that later tasks subscribe to; `GunGameHooks.RoundActive` bool.
- Consumes: GunGame statics `GunGame.Scripts.GameManager.BeforeGameStartedEvent`, `GameStartedEvent` (type `System.Action`).

- [ ] **Step 1: Write ArenaConfig**

`src/GunGameArena/ArenaConfig.cs`:
```csharp
using System.Collections.Generic;
using BepInEx.Configuration;
using GunGameArena.Core;

namespace GunGameArena
{
    public static class ArenaConfig
    {
        // Arena
        public static ConfigEntry<TeamMode> Mode;
        public static ConfigEntry<int> TeamCount;
        public static ConfigEntry<int> AllySosigs;
        // Leaderboard
        public static ConfigEntry<bool> LeaderboardEnabled;
        public static ConfigEntry<int> TopCount;
        public static ConfigEntry<float> Scale;
        public static ConfigEntry<float> Distance;
        public static ConfigEntry<float> Height;
        public static ConfigEntry<bool> ShowNames;
        public static ConfigEntry<bool> ShowTierBadge;
        // Names
        public static ConfigEntry<int> NameSeed;
        // Behaviour
        public static ConfigEntry<bool> SpreadSpawns;
        public static ConfigEntry<bool> Grudges;
        public static ConfigEntry<int> RivalCount;
        public static ConfigEntry<float> RivalRadius;
        public static ConfigEntry<float> PlayerRivalWeight;
        public static ConfigEntry<float> RivalRerollMin;
        public static ConfigEntry<float> RivalRerollMax;
        public static ConfigEntry<bool> Hunters;
        public static ConfigEntry<float> HunterShare;
        public static ConfigEntry<float> HunterIntervalMin;
        public static ConfigEntry<float> HunterIntervalMax;
        public static ConfigEntry<bool> SkillTiers;
        public static ConfigEntry<string> TierWeights;

        private static readonly Dictionary<SkillTier, ConfigEntry<float>[]> TierEntries = new Dictionary<SkillTier, ConfigEntry<float>[]>();

        public static void Bind(ConfigFile cfg)
        {
            Mode = cfg.Bind("Arena", "Mode", TeamMode.FreeForAll, "Off = original GunGame. FreeForAll = every sosig for itself. Teams = blue (you + allies) vs red (+ green/yellow).");
            TeamCount = cfg.Bind("Arena", "TeamCount", 2, new ConfigDescription("Teams mode only.", new AcceptableValueRange<int>(2, 4)));
            AllySosigs = cfg.Bind("Arena", "AllySosigs", -1, "Sosigs on your team in Teams mode. -1 = half of the sosig count.");

            LeaderboardEnabled = cfg.Bind("Leaderboard", "Enabled", true, "Show the floating leaderboard HUD.");
            TopCount = cfg.Bind("Leaderboard", "TopCount", 5, "Cards shown before your own card is pinned at the end.");
            Scale = cfg.Bind("Leaderboard", "Scale", 1.0f, "Overall HUD size multiplier.");
            Distance = cfg.Bind("Leaderboard", "Distance", 1.0f, "Metres in front of your head.");
            Height = cfg.Bind("Leaderboard", "Height", 0.35f, "Metres above eye line.");
            ShowNames = cfg.Bind("Leaderboard", "ShowNames", true, "Name label under each card.");
            ShowTierBadge = cfg.Bind("Leaderboard", "ShowTierBadge", true, "Skill tier chevrons under each sosig's name.");

            NameSeed = cfg.Bind("Names", "Seed", 0, "0 = random names every round; any other value = reproducible roster.");

            SpreadSpawns = cfg.Bind("Behaviour", "SpreadSpawns", true, "Spawn each sosig at the spawner farthest from everyone.");
            Grudges = cfg.Bind("Behaviour", "Grudges", true, "FFA only: each sosig hunts a few rivals at a time instead of everyone.");
            RivalCount = cfg.Bind("Behaviour", "RivalCount", 3, "Rivals per sosig.");
            RivalRadius = cfg.Bind("Behaviour", "RivalRadius", 40f, "Metres; rivals are picked from contestants within this radius.");
            PlayerRivalWeight = cfg.Bind("Behaviour", "PlayerRivalWeight", 2.0f, "How much more likely you are to be picked as a rival than a sosig (1 = equal).");
            RivalRerollMin = cfg.Bind("Behaviour", "RivalRerollSecondsMin", 20f, "Seconds between rival re-rolls (min).");
            RivalRerollMax = cfg.Bind("Behaviour", "RivalRerollSecondsMax", 40f, "Seconds between rival re-rolls (max).");
            Hunters = cfg.Bind("Behaviour", "Hunters", true, "Periodically send a share of hostile sosigs toward you.");
            HunterShare = cfg.Bind("Behaviour", "HunterShare", 0.25f, "Fraction of hostile sosigs sent toward you each interval.");
            HunterIntervalMin = cfg.Bind("Behaviour", "HunterIntervalSecondsMin", 10f, "Seconds between hunter orders (min).");
            HunterIntervalMax = cfg.Bind("Behaviour", "HunterIntervalSecondsMax", 20f, "Seconds between hunter orders (max).");
            SkillTiers = cfg.Bind("Behaviour", "SkillTiers", true, "Roll Rookie/Regular/Veteran/Elite per contestant; affects aim, not fire volume.");
            TierWeights = cfg.Bind("Behaviour", "TierWeights", "30,40,20,10", "Relative weights Rookie,Regular,Veteran,Elite.");

            TierEntries.Clear();
            foreach (SkillTier tier in new[] { SkillTier.Rookie, SkillTier.Regular, SkillTier.Veteran, SkillTier.Elite })
            {
                var d = TierTable.Default(tier);
                string sec = "Tier." + tier;
                TierEntries[tier] = new[]
                {
                    cfg.Bind(sec, "Spread", d.Spread, "Multiplier on weapon projectile spread (bigger = misses more)."),
                    cfg.Bind(sec, "FireAngle", d.FireAngle, "Multiplier on how far off-target the sosig will still fire."),
                    cfg.Bind(sec, "Refire", d.Refire, "Multiplier on delay between shots (bigger = slower)."),
                    cfg.Bind(sec, "Reaction", d.Reaction, "Multiplier on target recognition speed (bigger = faster).")
                };
            }
        }

        public static TierMultipliers MultipliersFor(SkillTier tier)
        {
            ConfigEntry<float>[] e;
            if (!TierEntries.TryGetValue(tier, out e)) return TierTable.Default(tier);
            return new TierMultipliers(e[0].Value, e[1].Value, e[2].Value, e[3].Value);
        }
    }
}
```

- [ ] **Step 2: Write GunGameHooks**

`src/GunGameArena/GunGameHooks.cs`:
```csharp
using System;
using GunGame.Scripts;
using UnityEngine.SceneManagement;

namespace GunGameArena
{
    /// <summary>Bridges GunGame's static round events to our own. Everything else subscribes here,
    /// never to GunGame directly, so the ordering is controlled in one place.</summary>
    public static class GunGameHooks
    {
        public static event Action RoundStarting;  // fired on GunGame BeforeGameStartedEvent (before sosigs spawn)
        public static event Action RoundStarted;   // fired on GunGame GameStartedEvent (after initial spawns)
        public static event Action RoundEnded;     // fired on scene change
        public static bool RoundActive { get; private set; }

        public static void Install()
        {
            GameManager.BeforeGameStartedEvent += OnBefore;
            GameManager.GameStartedEvent += OnStarted;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public static void Uninstall()
        {
            GameManager.BeforeGameStartedEvent -= OnBefore;
            GameManager.GameStartedEvent -= OnStarted;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private static void OnBefore()
        {
            try
            {
                RoundActive = true;
                Plugin.Log.LogInfo("Round starting.");
                if (RoundStarting != null) RoundStarting();
            }
            catch (Exception e) { Plugin.Log.LogError("RoundStarting handler failed: " + e); }
        }

        private static void OnStarted()
        {
            try
            {
                Plugin.Log.LogInfo("Round started.");
                if (RoundStarted != null) RoundStarted();
            }
            catch (Exception e) { Plugin.Log.LogError("RoundStarted handler failed: " + e); }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try
            {
                if (!RoundActive) return;
                RoundActive = false;
                Plugin.Log.LogInfo("Scene changed, round ended.");
                if (RoundEnded != null) RoundEnded();
            }
            catch (Exception e) { Plugin.Log.LogError("RoundEnded handler failed: " + e); }
        }
    }
}
```

- [ ] **Step 3: Extend Plugin.cs**

Replace `src/GunGameArena/Plugin.cs` with:
```csharp
using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace GunGameArena
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency("Kodeman.GunGame", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "shaha.GunGameArena";
        public const string Name = "GunGame Arena";
        public const string Version = "0.1.0";

        public static ManualLogSource Log;
        public static Plugin Instance;

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            try
            {
                ArenaConfig.Bind(Config);
                _harmony = new Harmony(Guid);
                _harmony.PatchAll(typeof(Plugin).Assembly);
                GunGameHooks.Install();
                Log.LogInfo(Name + " " + Version + " loaded. Mode=" + ArenaConfig.Mode.Value
                            + " Leaderboard=" + ArenaConfig.LeaderboardEnabled.Value);
            }
            catch (Exception e)
            {
                Log.LogError(Name + " failed to initialise and is disabled: " + e);
            }
        }

        private void OnDestroy()
        {
            GunGameHooks.Uninstall();
            if (_harmony != null) _harmony.UnpatchSelf();
        }
    }
}
```

- [ ] **Step 4: Build**

Run: `dotnet build GunGameArena.sln -c Release`
Expected: success; DLLs copied to the profile. (`SceneManager` lives in `UnityEngine.dll` for Unity 5.6; if the compiler complains, confirm with `ilspycmd UnityEngine.dll -l c | Select-String SceneManager`.)

- [ ] **Step 5: USER CHECKPOINT — plugin loads**

Ask the user to launch H3VR from Thunderstore Mod Manager (profile Default), load any GunGame map, press the start lever, then close the game. Check `%APPDATA%\Thunderstore Mod Manager\DataFolder\H3VR\profiles\Default\BepInEx\LogOutput.log` contains:
```
[Info   :GunGame Arena] GunGame Arena 0.1.0 loaded. Mode=FreeForAll Leaderboard=True
[Info   :GunGame Arena] Round starting.
[Info   :GunGame Arena] Round started.
```
and no `[Error  :GunGame Arena]` lines. A config file `BepInEx\config\shaha.GunGameArena.cfg` must now exist with the sections `Arena`, `Leaderboard`, `Names`, `Behaviour`, `Tier.Rookie` … `Tier.Elite`.

- [ ] **Step 6: Commit**

```bash
git add src/GunGameArena
git commit -m "feat: plugin entry, config schema and GunGame round lifecycle hooks"
```

---

### Task 9: Roster and spawner IFF patch (sosigs fight each other)

**Files:**
- Create: `src/GunGameArena/Roster.cs`, `src/GunGameArena/Patches/SpawnerPatches.cs`
- Modify: `src/GunGameArena/Plugin.cs` (wire `Roster.Install()`)

**Interfaces:**
- Produces: `class Slot { Contestant Contestant; Sosig Sosig; Sprite Portrait; bool IsVacant }`
- Produces: `static class Roster { event Action Changed; bool Active; TeamMode Mode; int PlayerIff; Slot Player; List<Slot> SosigSlots; IEnumerable<Contestant> AllContestants; void Install(); void Reset(int sosigCount, TeamMode mode, int playerIff); Slot ClaimVacantSlot(); void Bind(Slot, Sosig); Slot FindBySosig(Sosig); void MarkDead(Slot); void UpdatePositions(); void RaiseChanged(); IEnumerable<Slot> LivingSosigSlots(); Vector3 PlayerHeadPosition(); }`
- Produces: `static class SpawnerPatches` with a static event `SosigBound(Slot)` fired after a sosig is bound to a slot (later tasks hook portraits, tiers, grudges here).
- Consumes: `GunGame.Scripts.CustomSosigSpawner` (`int IFF`, `SpawnedSosigInfo Spawn(SosigEnemyID)`), `GunGame.Scripts.SpawnedSosigInfo { SosigEnemyID SosigType; Sosig SpawnedSosig }`, `GunGame.Scripts.Options.GameSettings.MaxSosigCount`, `FistVR.GM.CurrentPlayerBody.GetPlayerIFF()`, `FistVR.GM.PlayerName`.

- [ ] **Step 1: Write Roster**

`src/GunGameArena/Roster.cs`:
```csharp
using System;
using System.Collections.Generic;
using FistVR;
using GunGame.Scripts.Options;
using GunGameArena.Core;
using UnityEngine;

namespace GunGameArena
{
    public class Slot
    {
        public Contestant Contestant;
        public Sosig Sosig;
        public Sprite Portrait;

        public bool IsVacant
        {
            get { return Sosig == null || Sosig.BodyState == Sosig.SosigBodyState.Dead; }
        }
    }

    /// <summary>The round's contestants. Sosig slots persist; dead sosigs' replacements
    /// re-bind to the same slot (respawn semantics).</summary>
    public static class Roster
    {
        public static event Action Changed;

        public static bool Active;
        public static TeamMode Mode = TeamMode.Off;
        public static int PlayerIff;
        public static Slot Player;
        public static readonly List<Slot> SosigSlots = new List<Slot>();

        private static int _nextId;
        private static NameGenerator _names;
        private static System.Random _rng;

        public static IEnumerable<Contestant> AllContestants
        {
            get
            {
                if (Player != null) yield return Player.Contestant;
                for (int i = 0; i < SosigSlots.Count; i++) yield return SosigSlots[i].Contestant;
            }
        }

        public static void Install()
        {
            GunGameHooks.RoundStarting += OnRoundStarting;
            GunGameHooks.RoundEnded += OnRoundEnded;
        }

        private static void OnRoundStarting()
        {
            int playerIff = GM.CurrentPlayerBody != null ? GM.CurrentPlayerBody.GetPlayerIFF() : 0;
            Reset(GameSettings.MaxSosigCount, ArenaConfig.Mode.Value, playerIff);
        }

        private static void OnRoundEnded()
        {
            Active = false;
            SosigSlots.Clear();
            Player = null;
            RaiseChanged();
        }

        public static void Reset(int sosigCount, TeamMode mode, int playerIff)
        {
            Mode = mode;
            PlayerIff = playerIff;
            _nextId = 1;
            int seed = ArenaConfig.NameSeed.Value;
            _names = new NameGenerator(seed);
            _rng = seed == 0 ? new System.Random() : new System.Random(seed);
            int[] weights = TierRoller.ParseWeights(ArenaConfig.TierWeights.Value);

            SosigSlots.Clear();
            for (int slot = 0; slot < sosigCount; slot++) SosigSlots.Add(NewSlot(slot, sosigCount, weights));

            string playerName = "You";
            try { if (!string.IsNullOrEmpty(GM.PlayerName)) playerName = GM.PlayerName; } catch { }
            Player = new Slot
            {
                Contestant = new Contestant { Id = 0, Name = playerName, TeamIndex = 0, Iff = playerIff, IsPlayer = true, IsAlive = true }
            };

            Active = true;
            Plugin.Log.LogInfo("Roster reset: " + sosigCount + " sosigs, mode " + mode + ", player IFF " + playerIff);
            for (int i = 0; i < SosigSlots.Count; i++) Plugin.Log.LogInfo("  slot " + i + ": " + SosigSlots[i].Contestant + " tier " + SosigSlots[i].Contestant.Tier);
            RaiseChanged();
        }

        private static Slot NewSlot(int slotIndex, int sosigCount, int[] weights)
        {
            int team = TeamAssigner.TeamIndexFor(Mode, slotIndex, sosigCount, ArenaConfig.TeamCount.Value, ArenaConfig.AllySosigs.Value);
            var c = new Contestant
            {
                Id = _nextId++,
                Name = _names.Next(),
                TeamIndex = team,
                Iff = TeamAssigner.IffFor(Mode, team, PlayerIff),
                IsAlive = false,
                Tier = ArenaConfig.SkillTiers.Value ? TierRoller.Roll(_rng, weights) : SkillTier.Regular
            };
            return new Slot { Contestant = c };
        }

        public static Slot ClaimVacantSlot()
        {
            for (int i = 0; i < SosigSlots.Count; i++) if (SosigSlots[i].IsVacant) return SosigSlots[i];
            var extra = NewSlot(SosigSlots.Count, SosigSlots.Count + 1, TierRoller.ParseWeights(ArenaConfig.TierWeights.Value));
            SosigSlots.Add(extra);
            Plugin.Log.LogWarning("More sosigs than roster slots; added " + extra.Contestant);
            return extra;
        }

        public static void Bind(Slot slot, Sosig sosig)
        {
            slot.Sosig = sosig;
            slot.Contestant.IsAlive = sosig != null;
        }

        public static Slot FindBySosig(Sosig sosig)
        {
            if (sosig == null) return null;
            for (int i = 0; i < SosigSlots.Count; i++) if (SosigSlots[i].Sosig == sosig) return SosigSlots[i];
            return null;
        }

        public static void MarkDead(Slot slot)
        {
            slot.Contestant.IsAlive = false;
        }

        public static IEnumerable<Slot> LivingSosigSlots()
        {
            for (int i = 0; i < SosigSlots.Count; i++)
            {
                var s = SosigSlots[i];
                if (!s.IsVacant && s.Contestant.IsAlive) yield return s;
            }
        }

        public static Vector3 PlayerHeadPosition()
        {
            var body = GM.CurrentPlayerBody;
            if (body == null || body.Head == null) return Vector3.zero;
            return body.Head.position;
        }

        public static void UpdatePositions()
        {
            if (Player != null) Player.Contestant.Position = ToVec(PlayerHeadPosition());
            for (int i = 0; i < SosigSlots.Count; i++)
            {
                var s = SosigSlots[i];
                if (s.Sosig == null) continue;
                Transform t = (s.Sosig.Links != null && s.Sosig.Links.Count > 0 && s.Sosig.Links[0] != null)
                    ? s.Sosig.Links[0].transform : s.Sosig.transform;
                s.Contestant.Position = ToVec(t.position);
            }
        }

        public static Vec3 ToVec(Vector3 v) { return new Vec3(v.x, v.y, v.z); }

        public static void RaiseChanged()
        {
            try { if (Changed != null) Changed(); }
            catch (Exception e) { Plugin.Log.LogError("Roster.Changed handler failed: " + e); }
        }
    }
}
```

- [ ] **Step 2: Write SpawnerPatches**

`src/GunGameArena/Patches/SpawnerPatches.cs`:
```csharp
using System;
using GunGame.Scripts;
using GunGameArena.Core;
using HarmonyLib;

namespace GunGameArena.Patches
{
    [HarmonyPatch(typeof(CustomSosigSpawner), "Spawn")]
    public static class SpawnerPatches
    {
        /// <summary>Raised after a freshly spawned sosig is bound to its roster slot.</summary>
        public static event Action<Slot> SosigBound;

        private static Slot _pending;

        [HarmonyPrefix]
        private static void Prefix(CustomSosigSpawner __instance)
        {
            _pending = null;
            try
            {
                if (!Roster.Active) return;
                _pending = Roster.ClaimVacantSlot();
                if (Roster.Mode != TeamMode.Off) __instance.IFF = _pending.Contestant.Iff;
            }
            catch (Exception e) { Plugin.Log.LogError("SpawnerPatches.Prefix: " + e); }
        }

        [HarmonyPostfix]
        private static void Postfix(SpawnedSosigInfo __result)
        {
            try
            {
                if (_pending == null || __result.SpawnedSosig == null) return;
                Roster.Bind(_pending, __result.SpawnedSosig);
                Plugin.Log.LogInfo("Spawned " + _pending.Contestant + " as " + __result.SosigType
                                   + " (game IFF " + __result.SpawnedSosig.GetIFF() + ")");
                if (SosigBound != null) SosigBound(_pending);
                Roster.RaiseChanged();
            }
            catch (Exception e) { Plugin.Log.LogError("SpawnerPatches.Postfix: " + e); }
            finally { _pending = null; }
        }
    }
}
```

- [ ] **Step 3: Wire Roster into Plugin.Awake**

In `Plugin.Awake`, after `GunGameHooks.Install();` add:
```csharp
                Roster.Install();
```

- [ ] **Step 4: Build**

Run: `dotnet build GunGameArena.sln -c Release`
Expected: success.

- [ ] **Step 5: USER CHECKPOINT — sosigs fight each other**

Ask the user to launch a GunGame map (Nuketown recommended), start a round, watch for ~1 minute, quit. Expected in the log: one `Roster reset: 8 sosigs, mode FreeForAll…` line, 8 `slot N:` lines with distinct names and IFFs 1..8, `Spawned … (game IFF n)` lines where `n` matches the slot's IFF. Expected in play: sosigs shooting each other, not only the player. If sosigs still only target the player, paste the `Spawned` lines; the IFF printed must differ between sosigs.

- [ ] **Step 6: Commit**

```bash
git add src/GunGameArena
git commit -m "feat: roster of contestants and per-slot IFF assignment on spawn"
```

---

### Task 10: Kill tracking, attribution and progression guard

**Files:**
- Create: `src/GunGameArena/KillTracker.cs`, `src/GunGameArena/Patches/DamagePatches.cs`, `src/GunGameArena/Patches/ProgressionPatches.cs`
- Modify: `src/GunGameArena/Plugin.cs` (wire `KillTracker.Install()`)

**Interfaces:**
- Produces: `static class KillTracker { void Install(); void RecordHit(Sosig victim, Damage d); void OnSosigDying(Sosig victim); bool LastKillWasByPlayer(Sosig victim); void OnPlayerDeath(bool killedSelf, int iff); event Action<Contestant /*victim, null for player*/, Contestant /*killer*/> KillRegistered }`
- Consumes: `Roster` (Task 9), `KillAttribution.Nearest` (Task 4). Game: `Sosig.ProcessDamage(Damage, SosigLink)`, `Sosig.SosigDies`, `Sosig.GetDiedFromIFF()`, `Sosig.BodyState`, `Sosig.Priority.MakeEnemy(int)`, `FVRSceneSettings.PlayerDeathFromIFFEvent`, `GM.CurrentSceneSettings`. GunGame: `GunGame.Scripts.Progression` private static `OnSosigKilledByPlayer(Sosig)`.

- [ ] **Step 1: Write KillTracker**

`src/GunGameArena/KillTracker.cs`:
```csharp
using System;
using System.Collections.Generic;
using FistVR;
using GunGameArena.Core;
using UnityEngine;

namespace GunGameArena
{
    public static class KillTracker
    {
        private struct LastHit { public int Iff; public Vector3 Point; public float Time; }

        public static event Action<Contestant, Contestant> KillRegistered;

        private static readonly Dictionary<int, LastHit> _hits = new Dictionary<int, LastHit>();
        private static readonly HashSet<int> _processed = new HashSet<int>();
        private static readonly Dictionary<int, bool> _killedByPlayer = new Dictionary<int, bool>();

        public static void Install()
        {
            GunGameHooks.RoundStarting += OnRoundStarting;
            GunGameHooks.RoundEnded += OnRoundEnded;
        }

        private static void OnRoundStarting()
        {
            _hits.Clear(); _processed.Clear(); _killedByPlayer.Clear();
            var scene = GM.CurrentSceneSettings;
            if (scene != null)
            {
                scene.PlayerDeathFromIFFEvent -= OnPlayerDeath;
                scene.PlayerDeathFromIFFEvent += OnPlayerDeath;
            }
        }

        private static void OnRoundEnded()
        {
            var scene = GM.CurrentSceneSettings;
            if (scene != null) scene.PlayerDeathFromIFFEvent -= OnPlayerDeath;
        }

        public static void RecordHit(Sosig victim, Damage d)
        {
            if (victim == null || d == null || !Roster.Active) return;
            Vector3 p = d.Source_Point == Vector3.zero ? d.point : d.Source_Point;
            _hits[victim.GetInstanceID()] = new LastHit { Iff = d.Source_IFF, Point = p, Time = Time.time };

            // Grudge retaliation: whoever shoots you becomes your enemy (FFA only).
            if (ArenaConfig.Grudges.Value && Roster.Mode == TeamMode.FreeForAll
                && d.Source_IFF >= 0 && d.Source_IFF != victim.GetIFF() && victim.Priority != null)
            {
                victim.Priority.MakeEnemy(d.Source_IFF);
            }
        }

        /// <summary>Called from the SosigDies prefix, before GunGame despawns the victim.</summary>
        public static void OnSosigDying(Sosig victim)
        {
            if (victim == null || !Roster.Active) return;
            if (victim.BodyState == Sosig.SosigBodyState.Dead) return;
            int id = victim.GetInstanceID();
            if (!_processed.Add(id)) return;

            Slot slot = Roster.FindBySosig(victim);
            if (slot == null) return;

            LastHit hit;
            bool hasHit = _hits.TryGetValue(id, out hit);
            int killerIff = hasHit ? hit.Iff : victim.GetDiedFromIFF();
            Vector3 point = hasHit ? hit.Point : victim.transform.position;

            Roster.UpdatePositions();
            Contestant winner = KillAttribution.Nearest(Roster.AllContestants, killerIff, slot.Contestant.Id, Roster.ToVec(point));
            Roster.MarkDead(slot);
            _killedByPlayer[id] = winner != null && winner.IsPlayer;

            if (winner != null)
            {
                winner.AddKill(Time.time);
                Plugin.Log.LogInfo("KILL " + winner.Name + " -> " + slot.Contestant.Name + " (iff " + killerIff + ")");
            }
            else
            {
                Plugin.Log.LogInfo("DEATH " + slot.Contestant.Name + " with no credited killer (iff " + killerIff + ")");
            }
            if (KillRegistered != null) KillRegistered(slot.Contestant, winner);
            Roster.RaiseChanged();
        }

        public static bool LastKillWasByPlayer(Sosig victim)
        {
            if (victim == null) return true;
            bool b;
            if (_killedByPlayer.TryGetValue(victim.GetInstanceID(), out b)) return b;
            return victim.GetDiedFromIFF() == Roster.PlayerIff; // untracked sosig: GunGame's own rule
        }

        public static void OnPlayerDeath(bool killedSelf, int iff)
        {
            try
            {
                if (!Roster.Active || killedSelf || Roster.Player == null) return;
                Roster.UpdatePositions();
                var sosigsOnly = new List<Contestant>();
                foreach (var s in Roster.LivingSosigSlots()) sosigsOnly.Add(s.Contestant);
                Contestant winner = KillAttribution.Nearest(sosigsOnly, iff, Roster.Player.Contestant.Id, Roster.Player.Contestant.Position);
                if (winner != null)
                {
                    winner.AddKill(Time.time);
                    Plugin.Log.LogInfo("KILL " + winner.Name + " -> " + Roster.Player.Contestant.Name + " (player, iff " + iff + ")");
                    if (KillRegistered != null) KillRegistered(null, winner);
                    Roster.RaiseChanged();
                }
            }
            catch (Exception e) { Plugin.Log.LogError("KillTracker.OnPlayerDeath: " + e); }
        }
    }
}
```

- [ ] **Step 2: Write DamagePatches and ProgressionPatches**

`src/GunGameArena/Patches/DamagePatches.cs`:
```csharp
using System;
using FistVR;
using HarmonyLib;

namespace GunGameArena.Patches
{
    [HarmonyPatch(typeof(Sosig), "ProcessDamage", new[] { typeof(Damage), typeof(SosigLink) })]
    public static class ProcessDamagePatch
    {
        [HarmonyPostfix]
        private static void Postfix(Sosig __instance, Damage d)
        {
            try { KillTracker.RecordHit(__instance, d); }
            catch (Exception e) { Plugin.Log.LogError("ProcessDamagePatch: " + e); }
        }
    }

    [HarmonyPatch(typeof(Sosig), "SosigDies")]
    public static class SosigDiesPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Sosig __instance)
        {
            try { KillTracker.OnSosigDying(__instance); }
            catch (Exception e) { Plugin.Log.LogError("SosigDiesPatch: " + e); }
        }
    }
}
```

`src/GunGameArena/Patches/ProgressionPatches.cs`:
```csharp
using System;
using FistVR;
using GunGame.Scripts;
using HarmonyLib;

namespace GunGameArena.Patches
{
    /// <summary>GunGame credits any death whose source IFF equals the player's IFF. In Teams mode
    /// allies share that IFF, so this prefix lets the original run only for real player kills.</summary>
    [HarmonyPatch(typeof(Progression), "OnSosigKilledByPlayer")]
    public static class ProgressionPatches
    {
        [HarmonyPrefix]
        private static bool Prefix(Sosig killedSosig)
        {
            try
            {
                if (!Roster.Active) return true;
                bool byPlayer = KillTracker.LastKillWasByPlayer(killedSosig);
                if (!byPlayer) Plugin.Log.LogInfo("Blocked progression credit: kill was by an ally, not the player.");
                return byPlayer;
            }
            catch (Exception e) { Plugin.Log.LogError("ProgressionPatches: " + e); return true; }
        }
    }
}
```

- [ ] **Step 3: Wire into Plugin.Awake**

After `Roster.Install();` add:
```csharp
                KillTracker.Install();
```

- [ ] **Step 4: Build**

Run: `dotnet build GunGameArena.sln -c Release`
Expected: success. If Harmony cannot resolve `OnSosigKilledByPlayer` at runtime it logs at load; that is checked next.

- [ ] **Step 5: USER CHECKPOINT — kills are attributed**

User plays a round for a few minutes in FFA, kills at least two sosigs, and dies once. Expected log lines: `KILL <sosigName> -> <sosigName> (iff n)` for sosig-on-sosig kills, `KILL <SteamName> -> <sosigName>` for the user's kills, `KILL <sosigName> -> <SteamName> (player, iff n)` for the death. The weapon must advance only on the user's own kills. Then set `Mode = Teams` in the config, play again: at least one `Blocked progression credit` line should appear when a blue ally kills someone, and the weapon must not advance on it.

- [ ] **Step 6: Commit**

```bash
git add src/GunGameArena
git commit -m "feat: kill attribution to nearest contestant and ally-kill progression guard"
```

---
### Task 11: Portraits — procedural sprites, Steam avatar, sosig head snapshots

**Files:**
- Create: `src/GunGameArena/Portraits/Sprites.cs`, `src/GunGameArena/Portraits/SteamAvatar.cs`, `src/GunGameArena/Portraits/PortraitRenderer.cs`
- Modify: `src/GunGameArena/Plugin.cs` (wire `PortraitRenderer.Install()`)

**Interfaces:**
- Produces: `static class Sprites { Sprite Crown; Sprite FallbackAvatar; Sprite Solid; Sprite FromTexture(Texture2D) }` (lazy singletons)
- Produces: `static class SteamAvatar { IEnumerator Load(Action<Sprite> onDone) }` — always calls `onDone`, with the fallback sprite on failure.
- Produces: `class PortraitRenderer : MonoBehaviour { static void Install(); static void Capture(Slot slot) }` — sets `slot.Portrait` and calls `Roster.RaiseChanged()` when done.
- Consumes: `SpawnerPatches.SosigBound` (Task 9), `Roster.Player` (Task 9). Game: `SteamManager.Initialized`, `Steamworks.SteamUser.GetSteamID()`, `Steamworks.SteamFriends.GetLargeFriendAvatar`, `Steamworks.SteamUtils.GetImageSize/GetImageRGBA`, `Sosig.Links[0].transform`.

- [ ] **Step 1: Write Sprites**

`src/GunGameArena/Portraits/Sprites.cs`:
```csharp
using UnityEngine;

namespace GunGameArena.Portraits
{
    /// <summary>Procedural sprites so the plugin ships as a single DLL with no asset bundle.</summary>
    public static class Sprites
    {
        private static Sprite _crown, _fallback, _solid;

        public static Sprite Solid { get { return _solid ?? (_solid = MakeSolid()); } }
        public static Sprite Crown { get { return _crown ?? (_crown = MakeCrown()); } }
        public static Sprite FallbackAvatar { get { return _fallback ?? (_fallback = MakeFallbackAvatar()); } }

        public static Sprite FromTexture(Texture2D tex)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }

        private static Sprite MakeSolid()
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px); tex.Apply();
            return FromTexture(tex);
        }

        /// <summary>32x32 gold crown: three spikes on a base, transparent elsewhere.</summary>
        private static Sprite MakeCrown()
        {
            const int S = 32;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            var clear = new Color32(0, 0, 0, 0);
            var gold = new Color32(245, 197, 66, 255);
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                bool on = false;
                if (y >= 4 && y < 12) on = x >= 3 && x < 29;                         // base
                else if (y >= 12 && y < 28)
                {
                    int h = y - 12;                                                   // spikes narrow upward
                    int half = 5 - h / 4;
                    if (half < 1) half = 1;
                    on = Mathf.Abs(x - 6) <= half || Mathf.Abs(x - 16) <= half + 1 || Mathf.Abs(x - 25) <= half;
                    if (Mathf.Abs(x - 16) <= half + 1 && y < 30) on = true;           // centre spike taller
                }
                px[y * S + x] = on ? gold : clear;
            }
            tex.SetPixels32(px); tex.Apply();
            return FromTexture(tex);
        }

        /// <summary>128x128 dark disc with a lighter head-and-shoulders silhouette.</summary>
        private static Sprite MakeFallbackAvatar()
        {
            const int S = 128;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            var px = new Color32[S * S];
            var bg = new Color32(60, 60, 60, 255);
            var fg = new Color32(170, 170, 170, 255);
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = x - 64f, dy = y - 64f;
                bool disc = dx * dx + dy * dy <= 62f * 62f;
                float hx = x - 64f, hy = y - 78f;
                bool head = hx * hx + hy * hy <= 22f * 22f;
                float sx = x - 64f, sy = y - 30f;
                bool shoulders = sx * sx / (38f * 38f) + sy * sy / (24f * 24f) <= 1f && y < 50;
                px[y * S + x] = !disc ? new Color32(0, 0, 0, 0) : (head || shoulders ? fg : bg);
            }
            tex.SetPixels32(px); tex.Apply();
            return FromTexture(tex);
        }
    }
}
```

- [ ] **Step 2: Write SteamAvatar**

`src/GunGameArena/Portraits/SteamAvatar.cs`:
```csharp
using System;
using System.Collections;
using Steamworks;
using UnityEngine;

namespace GunGameArena.Portraits
{
    public static class SteamAvatar
    {
        private const float TimeoutSeconds = 10f;

        /// <summary>Coroutine. Calls onDone exactly once with the avatar sprite or the fallback.</summary>
        public static IEnumerator Load(Action<Sprite> onDone)
        {
            Sprite result = null;
            bool steamOk = SafeIsSteamReady();
            if (steamOk)
            {
                float deadline = Time.time + TimeoutSeconds;
                int handle = SafeGetHandle();
                while (handle == -1 && Time.time < deadline)          // -1 = not yet downloaded
                {
                    yield return new WaitForSeconds(1f);
                    handle = SafeGetHandle();
                }
                if (handle > 0) result = SafeToSprite(handle);
            }
            if (result == null)
            {
                Plugin.Log.LogInfo("Steam avatar unavailable, using fallback portrait.");
                result = Sprites.FallbackAvatar;
            }
            onDone(result);
        }

        private static bool SafeIsSteamReady()
        {
            try { return SteamManager.Initialized; }
            catch (Exception e) { Plugin.Log.LogWarning("SteamManager check failed: " + e.Message); return false; }
        }

        private static int SafeGetHandle()
        {
            try { return SteamFriends.GetLargeFriendAvatar(SteamUser.GetSteamID()); }
            catch (Exception e) { Plugin.Log.LogWarning("GetLargeFriendAvatar failed: " + e.Message); return 0; }
        }

        private static Sprite SafeToSprite(int handle)
        {
            try
            {
                uint w, h;
                if (!SteamUtils.GetImageSize(handle, out w, out h) || w == 0 || h == 0) return null;
                var raw = new byte[w * h * 4];
                if (!SteamUtils.GetImageRGBA(handle, raw, raw.Length)) return null;

                // Steam rows are top-down; Unity expects bottom-up.
                var flipped = new byte[raw.Length];
                int stride = (int)w * 4;
                for (int row = 0; row < h; row++)
                    Buffer.BlockCopy(raw, row * stride, flipped, ((int)h - 1 - row) * stride, stride);

                var tex = new Texture2D((int)w, (int)h, TextureFormat.RGBA32, false);
                tex.LoadRawTextureData(flipped);
                tex.Apply();
                return Sprites.FromTexture(tex);
            }
            catch (Exception e) { Plugin.Log.LogWarning("Steam avatar decode failed: " + e.Message); return null; }
        }
    }
}
```

- [ ] **Step 3: Write PortraitRenderer**

`src/GunGameArena/Portraits/PortraitRenderer.cs`:
```csharp
using System;
using System.Collections;
using FistVR;
using GunGameArena.Patches;
using UnityEngine;

namespace GunGameArena.Portraits
{
    /// <summary>Photographs a sosig's head onto a 128x128 texture right after it spawns.</summary>
    public class PortraitRenderer : MonoBehaviour
    {
        private const int Size = 128;
        private static PortraitRenderer _instance;

        private Camera _cam;
        private RenderTexture _rt;

        public static void Install()
        {
            SpawnerPatches.SosigBound += Capture;
            GunGameHooks.RoundStarted += OnRoundStarted;
        }

        private static void OnRoundStarted()
        {
            if (Roster.Player == null) return;
            Ensure().StartCoroutine(SteamAvatar.Load(sprite =>
            {
                if (Roster.Player == null) return;
                Roster.Player.Portrait = sprite;
                Roster.RaiseChanged();
            }));
        }

        public static void Capture(Slot slot)
        {
            try { Ensure().StartCoroutine(Ensure().CaptureRoutine(slot)); }
            catch (Exception e) { Plugin.Log.LogError("PortraitRenderer.Capture: " + e); }
        }

        private static PortraitRenderer Ensure()
        {
            if (_instance != null) return _instance;
            var go = new GameObject("GunGameArena_PortraitCam");
            _instance = go.AddComponent<PortraitRenderer>();
            _instance._cam = go.AddComponent<Camera>();
            _instance._cam.enabled = false;
            _instance._cam.clearFlags = CameraClearFlags.SolidColor;
            _instance._cam.backgroundColor = Color.clear;
            _instance._cam.fieldOfView = 30f;
            _instance._cam.nearClipPlane = 0.05f;
            _instance._cam.farClipPlane = 1.2f;
            _instance._cam.cullingMask = ~0;
            _instance._rt = new RenderTexture(Size, Size, 16, RenderTextureFormat.ARGB32);
            _instance._cam.targetTexture = _instance._rt;
            return _instance;
        }

        private IEnumerator CaptureRoutine(Slot slot)
        {
            yield return null;                      // let Sodalite finish outfitting
            yield return new WaitForEndOfFrame();
            Sosig sosig = slot.Sosig;
            Sprite sprite = TryRender(sosig);
            if (sprite == null) sprite = Sprites.FallbackAvatar;
            if (slot.Portrait != null && slot.Portrait != Sprites.FallbackAvatar && slot.Portrait.texture != null)
                Destroy(slot.Portrait.texture);
            slot.Portrait = sprite;
            Roster.RaiseChanged();
        }

        private Sprite TryRender(Sosig sosig)
        {
            try
            {
                if (sosig == null || sosig.Links == null || sosig.Links.Count == 0 || sosig.Links[0] == null) return null;
                Transform head = sosig.Links[0].transform;
                Vector3 facing = sosig.transform.forward;             // head link orientation is not trusted (spec §8)
                _cam.transform.position = head.position + facing * 0.45f + Vector3.up * 0.03f;
                _cam.transform.LookAt(head.position);
                _cam.Render();

                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = _rt;
                var tex = new Texture2D(Size, Size, TextureFormat.ARGB32, false);
                tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                return Sprites.FromTexture(tex);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Portrait render failed: " + e.Message);
                return null;
            }
        }

        private void OnDestroy()
        {
            if (_rt != null) _rt.Release();
            if (_instance == this) _instance = null;
        }
    }
}
```

- [ ] **Step 4: Wire into Plugin.Awake**

After `KillTracker.Install();` add:
```csharp
                Portraits.PortraitRenderer.Install();
```

- [ ] **Step 5: Build**

Run: `dotnet build GunGameArena.sln -c Release`
Expected: success. If `Steamworks` types do not resolve, the `Assembly-CSharp-firstpass` reference is missing (Task 1 csproj).

- [ ] **Step 6: Commit** (visual verification happens with the HUD in Task 12)

```bash
git add src/GunGameArena
git commit -m "feat: procedural sprites, Steam avatar loader and sosig head portrait renderer"
```

---

### Task 12: Leaderboard HUD

**Files:**
- Create: `src/GunGameArena/Hud/HudFollower.cs`, `src/GunGameArena/Hud/ContestantCard.cs`, `src/GunGameArena/Hud/LeaderboardHud.cs`
- Modify: `src/GunGameArena/Plugin.cs` (wire `LeaderboardHud.Install()`)

**Interfaces:**
- Produces: `class HudFollower : MonoBehaviour` (uses `ArenaConfig.Distance/Height`)
- Produces: `class ContestantCard : MonoBehaviour { static ContestantCard Create(Transform parent, Font font); void Bind(Contestant c, Sprite portrait, bool crowned, bool isRankOne, TeamMode mode, bool showName, bool showTier) }`
- Produces: `class LeaderboardHud : MonoBehaviour { static void Install(); void Rebuild() }`
- Consumes: `Roster` (Task 9), `Ranking`, `HudPalette`, `TierTable.Chevrons` (Core), `Sprites` (Task 11).

- [ ] **Step 1: Write HudFollower**

`src/GunGameArena/Hud/HudFollower.cs`:
```csharp
using FistVR;
using UnityEngine;

namespace GunGameArena.Hud
{
    /// <summary>Keeps the canvas in front of and above the player's head, following yaw only,
    /// with exponential smoothing so it drifts instead of snapping.</summary>
    public class HudFollower : MonoBehaviour
    {
        private const float Smoothing = 6f;
        private const float TiltDegrees = 10f;

        private void LateUpdate()
        {
            var body = GM.CurrentPlayerBody;
            if (body == null || body.Head == null) return;
            Transform head = body.Head;

            float yaw = head.rotation.eulerAngles.y;
            Quaternion yawRot = Quaternion.Euler(0f, yaw, 0f);
            Vector3 target = head.position + yawRot * Vector3.forward * ArenaConfig.Distance.Value
                             + Vector3.up * ArenaConfig.Height.Value;
            Quaternion targetRot = Quaternion.Euler(-TiltDegrees, yaw, 0f);

            float t = 1f - Mathf.Exp(-Smoothing * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, target, t);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
        }
    }
}
```

- [ ] **Step 2: Write ContestantCard**

`src/GunGameArena/Hud/ContestantCard.cs`:
```csharp
using GunGameArena.Core;
using GunGameArena.Portraits;
using UnityEngine;
using UnityEngine.UI;

namespace GunGameArena.Hud
{
    public class ContestantCard : MonoBehaviour
    {
        public const float CardSize = 120f;
        public const float BorderPx = 4f;
        public const float SelfOutlinePx = 3f;

        private Image _selfOutline, _border, _background, _portrait, _crown;
        private Text _kills, _name, _tier;

        public static ContestantCard Create(Transform parent, Font font)
        {
            var root = new GameObject("Card", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var le = root.AddComponent<LayoutElement>();
            le.preferredWidth = CardSize + 2 * SelfOutlinePx;
            le.preferredHeight = CardSize + 2 * SelfOutlinePx + 44f;   // room for name + tier
            var card = root.AddComponent<ContestantCard>();

            // Square area anchored to the top of the root
            var square = MakeRect(root.transform, "Square", 0f, 1f, 1f, 1f, new Vector2(0, -(CardSize + 2 * SelfOutlinePx)), Vector2.zero);

            card._selfOutline = MakeImage(square, "SelfOutline", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            card._border = MakeImage(square, "Border", Vector2.zero, Vector2.one, new Vector2(SelfOutlinePx, SelfOutlinePx), new Vector2(-SelfOutlinePx, -SelfOutlinePx));
            float inset = SelfOutlinePx + BorderPx;
            card._background = MakeImage(square, "Background", Vector2.zero, Vector2.one, new Vector2(inset, inset), new Vector2(-inset, -inset));
            card._portrait = MakeImage(square, "Portrait", Vector2.zero, Vector2.one, new Vector2(inset, inset), new Vector2(-inset, -inset));
            card._portrait.preserveAspect = true;

            card._crown = MakeImage(square, "Crown", new Vector2(0, 1), new Vector2(0, 1), new Vector2(inset + 2, -inset - 30), new Vector2(inset + 30, -inset - 2));
            card._crown.sprite = Sprites.Crown;
            card._crown.color = Color.white;

            card._kills = MakeText(square, "Kills", font, 48, TextAnchor.LowerRight, new Vector2(0, 0), new Vector2(1, 0), new Vector2(inset, inset - 6), new Vector2(-inset - 4, inset + 52));
            card._kills.fontStyle = FontStyle.Bold;
            var outline = card._kills.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2);

            card._name = MakeText(root.transform, "Name", font, 18, TextAnchor.UpperCenter, new Vector2(0, 0), new Vector2(1, 0), new Vector2(-10, 20), new Vector2(10, 44));
            card._tier = MakeText(root.transform, "Tier", font, 14, TextAnchor.UpperCenter, new Vector2(0, 0), new Vector2(1, 0), new Vector2(-10, 2), new Vector2(10, 20));
            card._tier.color = new Color(1f, 0.85f, 0.4f);
            return card;
        }

        public void Bind(Contestant c, Sprite portrait, bool crowned, bool isRankOne, TeamMode mode, bool showName, bool showTier)
        {
            _border.color = ToColor(HudPalette.CardBorder(mode, c.TeamIndex, isRankOne));
            _background.color = ToColor(HudPalette.CardBackground(mode, c.TeamIndex));
            _selfOutline.color = c.IsPlayer ? Color.white : Color.clear;
            _portrait.sprite = portrait != null ? portrait : Sprites.FallbackAvatar;
            _portrait.color = Color.white;
            _crown.enabled = crowned;
            _kills.text = c.Kills.ToString();
            _name.text = showName ? c.Name : "";
            _tier.text = (showTier && !c.IsPlayer) ? new string('^', TierTable.Chevrons(c.Tier)) : "";
        }

        public static Color ToColor(Rgba c) { return new Color(c.R, c.G, c.B, c.A); }

        private static RectTransform MakeRect(Transform parent, string name, float minX, float minY, float maxX, float maxY, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        private static Image MakeImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rt = MakeRect(parent, name, anchorMin.x, anchorMin.y, anchorMax.x, anchorMax.y, offsetMin, offsetMax);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Sprites.Solid;
            img.raycastTarget = false;
            return img;
        }

        private static Text MakeText(Transform parent, string name, Font font, int size, TextAnchor anchor, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rt = MakeRect(parent, name, anchorMin.x, anchorMin.y, anchorMax.x, anchorMax.y, offsetMin, offsetMax);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }
    }
}
```

- [ ] **Step 3: Write LeaderboardHud**

`src/GunGameArena/Hud/LeaderboardHud.cs`:
```csharp
using System;
using System.Collections.Generic;
using GunGameArena.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GunGameArena.Hud
{
    public class LeaderboardHud : MonoBehaviour
    {
        private static LeaderboardHud _instance;

        private Font _font;
        private Text _header;
        private RectTransform _row;
        private LayoutElement _pinGap;
        private readonly List<ContestantCard> _cards = new List<ContestantCard>();

        public static void Install()
        {
            GunGameHooks.RoundStarted += Show;
            GunGameHooks.RoundEnded += Hide;
        }

        private static void Show()
        {
            try
            {
                if (!ArenaConfig.LeaderboardEnabled.Value) return;
                if (_instance == null) _instance = Build();
                _instance.gameObject.SetActive(true);
                _instance.Rebuild();
                Plugin.Log.LogInfo("Leaderboard HUD shown.");
            }
            catch (Exception e) { Plugin.Log.LogError("LeaderboardHud.Show: " + e); }
        }

        private static void Hide()
        {
            if (_instance != null) Destroy(_instance.gameObject);
            _instance = null;
        }

        private static LeaderboardHud Build()
        {
            var go = new GameObject("GunGameArena_HUD", typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1000f, 260f);
            go.transform.localScale = Vector3.one * 0.001f * ArenaConfig.Scale.Value;
            go.AddComponent<HudFollower>();

            var hud = go.AddComponent<LeaderboardHud>();
            hud._font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Header
            var headerBg = new GameObject("HeaderBg", typeof(RectTransform)).GetComponent<RectTransform>();
            headerBg.SetParent(go.transform, false);
            headerBg.anchorMin = new Vector2(0.5f, 1f); headerBg.anchorMax = new Vector2(0.5f, 1f);
            headerBg.sizeDelta = new Vector2(320f, 44f); headerBg.anchoredPosition = new Vector2(0f, -22f);
            var bgImg = headerBg.gameObject.AddComponent<Image>();
            bgImg.sprite = Portraits.Sprites.Solid; bgImg.color = ContestantCard.ToColor(HudPalette.HeaderBg); bgImg.raycastTarget = false;
            var headerText = new GameObject("Header", typeof(RectTransform)).AddComponent<Text>();
            headerText.transform.SetParent(headerBg, false);
            var htr = headerText.GetComponent<RectTransform>(); htr.anchorMin = Vector2.zero; htr.anchorMax = Vector2.one; htr.offsetMin = Vector2.zero; htr.offsetMax = Vector2.zero;
            headerText.font = hud._font; headerText.fontSize = 28; headerText.fontStyle = FontStyle.Bold;
            headerText.alignment = TextAnchor.MiddleCenter; headerText.color = Color.white; headerText.raycastTarget = false;
            hud._header = headerText;

            // Card row
            var row = new GameObject("Row", typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(go.transform, false);
            row.anchorMin = new Vector2(0.5f, 1f); row.anchorMax = new Vector2(0.5f, 1f);
            row.sizeDelta = new Vector2(1000f, 180f); row.anchoredPosition = new Vector2(0f, -150f);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f; layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;
            hud._row = row;

            var gap = new GameObject("PinGap", typeof(RectTransform));
            gap.transform.SetParent(row, false);
            hud._pinGap = gap.AddComponent<LayoutElement>();
            hud._pinGap.preferredWidth = 18f;
            gap.SetActive(false);

            Roster.Changed += hud.Rebuild;
            return hud;
        }

        public void Rebuild()
        {
            try
            {
                if (!Roster.Active) return;
                var sorted = Ranking.Sort(Roster.AllContestants);
                var visible = Ranking.Visible(sorted, ArenaConfig.TopCount.Value);
                var crowned = Ranking.CrownedIds(sorted, Roster.Mode);
                Contestant rankOne = sorted.Count > 0 ? sorted[0] : null;

                _header.text = ModeTitle(Roster.Mode);

                while (_cards.Count < visible.Count) _cards.Add(ContestantCard.Create(_row, _font));

                _pinGap.gameObject.SetActive(false);
                for (int i = 0; i < _cards.Count; i++)
                {
                    bool show = i < visible.Count;
                    _cards[i].gameObject.SetActive(show);
                    if (!show) continue;
                    Contestant c = visible[i];
                    bool pinned = Ranking.IsPinnedPlayer(visible, ArenaConfig.TopCount.Value, c);
                    if (pinned)
                    {
                        _pinGap.gameObject.SetActive(true);
                        _pinGap.transform.SetSiblingIndex(_cards[i].transform.GetSiblingIndex());
                    }
                    _cards[i].transform.SetAsLastSibling();
                    _cards[i].Bind(c, PortraitFor(c), crowned.Contains(c.Id), c == rankOne, Roster.Mode,
                                   ArenaConfig.ShowNames.Value, ArenaConfig.ShowTierBadge.Value);
                }
            }
            catch (Exception e) { Plugin.Log.LogError("LeaderboardHud.Rebuild: " + e); }
        }

        private static Sprite PortraitFor(Contestant c)
        {
            if (c.IsPlayer) return Roster.Player != null ? Roster.Player.Portrait : null;
            for (int i = 0; i < Roster.SosigSlots.Count; i++)
                if (Roster.SosigSlots[i].Contestant == c) return Roster.SosigSlots[i].Portrait;
            return null;
        }

        private static string ModeTitle(TeamMode mode)
        {
            switch (mode)
            {
                case TeamMode.FreeForAll: return "Free For All";
                case TeamMode.Teams: return "Team Deathmatch";
                default: return "Gun Game";
            }
        }

        private void OnDestroy()
        {
            Roster.Changed -= Rebuild;
            if (_instance == this) _instance = null;
        }
    }
}
```

- [ ] **Step 4: Wire into Plugin.Awake**

After `Portraits.PortraitRenderer.Install();` add:
```csharp
                Hud.LeaderboardHud.Install();
```

- [ ] **Step 5: Build**

Run: `dotnet build GunGameArena.sln -c Release`
Expected: success.

- [ ] **Step 6: USER CHECKPOINT — HUD looks right**

User starts a round and reports (a screenshot via SteamVR's screenshot key helps):
- Header "Free For All" with up to six cards below, following head yaw smoothly at about 1 m ahead and 0.35 m up.
- Each sosig card shows a head render (hat visible) or the grey silhouette; the user's card shows their Steam avatar and has a white outline.
- Kill numbers update on kills; the leader has a gold border and a crown; nobody has a crown before the first kill.
- Names below cards; chevrons `^`..`^^^^` under sosig names.
- Switch config to `Mode = Teams`: blue and red card backgrounds, one crown per team, header "Team Deathmatch".

If portraits are back-of-head or empty, change `facing` in `PortraitRenderer.TryRender` to `-sosig.transform.forward` or to `head.forward` and rebuild; record the working choice in the spec §8.

- [ ] **Step 7: Commit**

```bash
git add src/GunGameArena
git commit -m "feat: world-space leaderboard HUD with cards, crowns, team colours and head follower"
```

---
### Task 13: Spread-out spawns and grudges

**Files:**
- Create: `src/GunGameArena/Patches/SpawnPlacementPatches.cs`, `src/GunGameArena/Behaviour/GrudgeDirector.cs`
- Modify: `src/GunGameArena/Plugin.cs` (wire `GrudgeDirector.Install()`)

**Interfaces:**
- Produces: `class GrudgeDirector : MonoBehaviour { static void Install(); }` — reacts to `SpawnerPatches.SosigBound` and `KillTracker.KillRegistered`.
- Consumes: `SpawnerChooser.Choose`, `RivalSelector.Pick` (Core). GunGame: `SosigBehavior` fields `SosigSpawners`, `Sosigs`, `IgnoredSpawnersCloseToPlayer`, `IgnoredSpawnersFarFromPlayer`; `CustomSosigSpawner.Spawn`. Game: `Sosig.Priority.SetAllFriendly()`, `Sosig.Priority.MakeEnemy(int)`.

- [ ] **Step 1: Write SpawnPlacementPatches**

`src/GunGameArena/Patches/SpawnPlacementPatches.cs`:
```csharp
using System;
using System.Collections.Generic;
using FistVR;
using GunGame.Scripts;
using GunGameArena.Core;
using HarmonyLib;
using UnityEngine;

namespace GunGameArena.Patches
{
    /// <summary>Replaces GunGame's random spawner pick with the spawner farthest from everyone.
    /// Re-implements the tail of SpawnSosigRandomPlace (Spawn + register) identically.</summary>
    [HarmonyPatch(typeof(SosigBehavior), "SpawnSosigRandomPlace")]
    public static class SpawnPlacementPatches
    {
        private static readonly System.Random Rng = new System.Random();

        [HarmonyPrefix]
        private static bool Prefix(SosigBehavior __instance, SosigEnemyID sosigtype)
        {
            try
            {
                if (!ArenaConfig.SpreadSpawns.Value || !Roster.Active) return true;
                var spawners = __instance.SosigSpawners;
                if (spawners == null || spawners.Count == 0 || GM.CurrentPlayerBody == null) return true;

                var positions = new List<Vec3>(spawners.Count);
                for (int i = 0; i < spawners.Count; i++) positions.Add(Roster.ToVec(spawners[i].transform.position));

                var occupied = new List<Vec3>();
                foreach (var kv in __instance.Sosigs)
                {
                    Sosig s = kv.Key;
                    if (s != null && s.BodyState != Sosig.SosigBodyState.Dead) occupied.Add(Roster.ToVec(s.transform.position));
                }

                int idx = SpawnerChooser.Choose(Rng, positions, __instance.IgnoredSpawnersCloseToPlayer,
                    __instance.IgnoredSpawnersFarFromPlayer, Roster.ToVec(GM.CurrentPlayerBody.transform.position), occupied);
                if (idx < 0) return true;

                SpawnedSosigInfo info = spawners[idx].Spawn(sosigtype);   // SpawnerPatches run inside this call
                if (info.SpawnedSosig != null && !__instance.Sosigs.ContainsKey(info.SpawnedSosig))
                    __instance.Sosigs.Add(info.SpawnedSosig, info.SosigType);
                return false;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("SpawnPlacementPatches: " + e);
                return true;
            }
        }
    }
}
```

- [ ] **Step 2: Write GrudgeDirector**

`src/GunGameArena/Behaviour/GrudgeDirector.cs`:
```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using GunGameArena.Core;
using GunGameArena.Patches;
using UnityEngine;

namespace GunGameArena.Behaviour
{
    /// <summary>FFA only. Each sosig is hostile to a few rivals at a time (spec §4b.2).</summary>
    public class GrudgeDirector : MonoBehaviour
    {
        private static GrudgeDirector _instance;
        private static readonly System.Random Rng = new System.Random();

        private readonly Dictionary<Slot, float> _nextReroll = new Dictionary<Slot, float>();
        private readonly Dictionary<Slot, List<int>> _rivalIds = new Dictionary<Slot, List<int>>();

        private static bool Enabled
        {
            get { return ArenaConfig.Grudges.Value && Roster.Mode == TeamMode.FreeForAll && Roster.Active; }
        }

        public static void Install()
        {
            GunGameHooks.RoundStarted += OnRoundStarted;
            GunGameHooks.RoundEnded += OnRoundEnded;
            SpawnerPatches.SosigBound += OnSosigBound;
            KillTracker.KillRegistered += OnKill;
        }

        private static void OnRoundStarted()
        {
            if (!Enabled) return;
            if (_instance == null) _instance = new GameObject("GunGameArena_Grudges").AddComponent<GrudgeDirector>();
            foreach (var slot in Roster.LivingSosigSlots()) _instance.StartCoroutine(_instance.RerollNextFrame(slot));
        }

        private static void OnRoundEnded()
        {
            if (_instance != null) Destroy(_instance.gameObject);
            _instance = null;
        }

        private static void OnSosigBound(Slot slot)
        {
            if (!Enabled || _instance == null) return;
            _instance.StartCoroutine(_instance.RerollNextFrame(slot));
        }

        private static void OnKill(Contestant victim, Contestant killer)
        {
            if (!Enabled || _instance == null || victim == null) return;
            foreach (var kv in _instance._rivalIds)
                if (kv.Value.Contains(victim.Id)) _instance._nextReroll[kv.Key] = 0f;   // re-roll on next Update
        }

        private IEnumerator RerollNextFrame(Slot slot)
        {
            yield return null;   // Priority system is initialised in Sosig.Start / Configure
            Reroll(slot);
        }

        private void Update()
        {
            if (!Enabled) return;
            var due = new List<Slot>();
            foreach (var kv in _nextReroll) if (Time.time >= kv.Value) due.Add(kv.Key);
            for (int i = 0; i < due.Count; i++) Reroll(due[i]);
        }

        private void Reroll(Slot slot)
        {
            try
            {
                if (slot == null || slot.IsVacant || slot.Sosig.Priority == null) { _nextReroll.Remove(slot); _rivalIds.Remove(slot); return; }
                Roster.UpdatePositions();
                var rivals = RivalSelector.Pick(Rng, slot.Contestant, Roster.AllContestants,
                    ArenaConfig.RivalCount.Value, ArenaConfig.RivalRadius.Value, ArenaConfig.PlayerRivalWeight.Value);

                slot.Sosig.Priority.SetAllFriendly();
                var ids = new List<int>();
                var names = new List<string>();
                for (int i = 0; i < rivals.Count; i++)
                {
                    slot.Sosig.Priority.MakeEnemy(rivals[i].Iff);
                    ids.Add(rivals[i].Id);
                    names.Add(rivals[i].Name);
                }
                _rivalIds[slot] = ids;
                float min = ArenaConfig.RivalRerollMin.Value, max = Mathf.Max(min, ArenaConfig.RivalRerollMax.Value);
                _nextReroll[slot] = Time.time + UnityEngine.Random.Range(min, max);
                Plugin.Log.LogInfo("Grudges " + slot.Contestant.Name + " -> " + string.Join(", ", names.ToArray()));
            }
            catch (Exception e) { Plugin.Log.LogError("GrudgeDirector.Reroll: " + e); }
        }
    }
}
```

- [ ] **Step 3: Wire into Plugin.Awake**

After `Hud.LeaderboardHud.Install();` add:
```csharp
                Behaviour.GrudgeDirector.Install();
```

- [ ] **Step 4: Build**

Run: `dotnet build GunGameArena.sln -c Release`
Expected: success.

- [ ] **Step 5: USER CHECKPOINT — spread and grudges**

User plays FFA for a few minutes. Expected: initial sosigs appear spread around the map rather than clustered; log shows `Grudges <name> -> <a>, <b>, <c>` lines at round start and every 20–40 s, with the user's Steam name appearing in roughly a quarter of them; sosigs that the user shoots turn and fight back immediately; a sosig walking past another it has no grudge against does not open fire until shot.

- [ ] **Step 6: Commit**

```bash
git add src/GunGameArena
git commit -m "feat: spread-out spawn placement and FFA grudge targeting"
```

---

### Task 14: Hunters and skill tiers

**Files:**
- Create: `src/GunGameArena/Behaviour/HunterDirector.cs`, `src/GunGameArena/Behaviour/SkillApplier.cs`, `src/GunGameArena/Patches/WeaponPatches.cs`
- Modify: `src/GunGameArena/Plugin.cs` (wire `HunterDirector.Install()`, `SkillApplier.Install()`)

**Interfaces:**
- Produces: `class HunterDirector : MonoBehaviour { static void Install(); }`
- Produces: `static class SkillApplier { static void Install(); static void Apply(Slot slot); static void ApplyWeapon(SosigWeapon w, SkillTier tier) }`
- Consumes: `HunterPicker.Pick`, `TierMultipliers`, `ArenaConfig.MultipliersFor`. Game: `Sosig.SetCurrentOrder(Sosig.SosigOrder)`, `Sosig.CommandAssaultPoint(Vector3)`, `Sosig.Hands[i].IsHoldingObject/HeldObject`, `SosigWeapon.ProjectileSpread` (float), `SosigWeapon.MaxAngularFireRange` (float), `SosigWeapon.Usage_RefireRange` (Vector2), `SosigWeapon.BotPickup(Sosig S)`, private `Sosig.m_entityRecognitionMultiplier`, `Sosig.m_combatTargetIdentificationSpeedMultiplier`; `UnityEngine.AI.NavMesh.SamplePosition`.

- [ ] **Step 1: Write HunterDirector**

`src/GunGameArena/Behaviour/HunterDirector.cs`:
```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using FistVR;
using GunGameArena.Core;
using UnityEngine;
using UnityEngine.AI;

namespace GunGameArena.Behaviour
{
    /// <summary>Every 10–20 s sends a share of player-hostile sosigs to a point near the player.</summary>
    public class HunterDirector : MonoBehaviour
    {
        private static HunterDirector _instance;
        private static readonly System.Random Rng = new System.Random();

        public static void Install()
        {
            GunGameHooks.RoundStarted += OnRoundStarted;
            GunGameHooks.RoundEnded += OnRoundEnded;
        }

        private static void OnRoundStarted()
        {
            if (!ArenaConfig.Hunters.Value || Roster.Mode == TeamMode.Off) return;
            if (_instance == null) _instance = new GameObject("GunGameArena_Hunters").AddComponent<HunterDirector>();
            _instance.StartCoroutine(_instance.Loop());
        }

        private static void OnRoundEnded()
        {
            if (_instance != null) Destroy(_instance.gameObject);
            _instance = null;
        }

        private IEnumerator Loop()
        {
            while (Roster.Active)
            {
                float min = ArenaConfig.HunterIntervalMin.Value, max = Mathf.Max(min, ArenaConfig.HunterIntervalMax.Value);
                yield return new WaitForSeconds(UnityEngine.Random.Range(min, max));
                IssueOrders();
            }
        }

        private void IssueOrders()
        {
            try
            {
                var hostile = new List<Contestant>();
                var bySlot = new Dictionary<Contestant, Slot>();
                foreach (var slot in Roster.LivingSosigSlots())
                {
                    if (slot.Contestant.Iff == Roster.PlayerIff) continue;   // allies never hunt the player
                    hostile.Add(slot.Contestant);
                    bySlot[slot.Contestant] = slot;
                }
                var hunters = HunterPicker.Pick(Rng, hostile, ArenaConfig.HunterShare.Value);
                if (hunters.Count == 0) return;

                Vector3 playerPos = Roster.PlayerHeadPosition();
                var names = new List<string>();
                for (int i = 0; i < hunters.Count; i++)
                {
                    Sosig s = bySlot[hunters[i]].Sosig;
                    if (s == null) continue;
                    Vector3 target = PointNear(playerPos);
                    s.SetCurrentOrder(Sosig.SosigOrder.Assault);
                    s.CommandAssaultPoint(target);
                    names.Add(hunters[i].Name);
                }
                Plugin.Log.LogInfo("Hunters: " + string.Join(", ", names.ToArray()));
            }
            catch (Exception e) { Plugin.Log.LogError("HunterDirector.IssueOrders: " + e); }
        }

        private static Vector3 PointNear(Vector3 playerPos)
        {
            Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
            float dist = UnityEngine.Random.Range(8f, 15f);
            Vector3 candidate = playerPos + new Vector3(dir.x, 0f, dir.y) * dist;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(candidate, out hit, 5f, NavMesh.AllAreas)) return hit.position;
            return playerPos;
        }
    }
}
```

- [ ] **Step 2: Write SkillApplier and WeaponPatches**

`src/GunGameArena/Behaviour/SkillApplier.cs`:
```csharp
using System;
using System.Reflection;
using FistVR;
using GunGameArena.Core;
using GunGameArena.Patches;
using HarmonyLib;
using UnityEngine;

namespace GunGameArena.Behaviour
{
    /// <summary>Remembers a weapon's original values so re-applying a tier is idempotent.</summary>
    public class TierAppliedMarker : MonoBehaviour
    {
        public float Spread, FireAngle;
        public Vector2 Refire;
    }

    /// <summary>Applies skill-tier multipliers (spec §4b.4). Rookies miss more; nobody fires more.</summary>
    public static class SkillApplier
    {
        private static readonly FieldInfo RecognitionField = AccessTools.Field(typeof(Sosig), "m_entityRecognitionMultiplier");
        private static readonly FieldInfo IdentificationField = AccessTools.Field(typeof(Sosig), "m_combatTargetIdentificationSpeedMultiplier");
        private static bool _warnedMissingFields;

        public static void Install()
        {
            SpawnerPatches.SosigBound += Apply;
        }

        public static void Apply(Slot slot)
        {
            try
            {
                if (!ArenaConfig.SkillTiers.Value || slot == null || slot.Sosig == null) return;
                Sosig s = slot.Sosig;
                TierMultipliers m = ArenaConfig.MultipliersFor(slot.Contestant.Tier);

                if (s.GetComponent<TierAppliedMarker>() == null)
                {
                    s.gameObject.AddComponent<TierAppliedMarker>();
                    ScaleFloatField(s, RecognitionField, m.Reaction);
                    ScaleFloatField(s, IdentificationField, m.Reaction);
                }
                for (int i = 0; i < s.Hands.Count; i++)
                {
                    if (s.Hands[i] != null && s.Hands[i].IsHoldingObject && s.Hands[i].HeldObject != null)
                        ApplyWeapon(s.Hands[i].HeldObject, slot.Contestant.Tier);
                }
                Plugin.Log.LogInfo("Tier " + slot.Contestant.Tier + " applied to " + slot.Contestant.Name);
            }
            catch (Exception e) { Plugin.Log.LogError("SkillApplier.Apply: " + e); }
        }

        public static void ApplyWeapon(SosigWeapon w, SkillTier tier)
        {
            if (w == null) return;
            TierMultipliers m = ArenaConfig.MultipliersFor(tier);
            var marker = w.GetComponent<TierAppliedMarker>();
            if (marker == null)
            {
                marker = w.gameObject.AddComponent<TierAppliedMarker>();
                marker.Spread = w.ProjectileSpread;
                marker.FireAngle = w.MaxAngularFireRange;
                marker.Refire = w.Usage_RefireRange;
            }
            w.ProjectileSpread = marker.Spread * m.Spread;
            w.MaxAngularFireRange = marker.FireAngle * m.FireAngle;
            w.Usage_RefireRange = marker.Refire * m.Refire;
        }

        private static void ScaleFloatField(Sosig s, FieldInfo field, float factor)
        {
            if (field == null)
            {
                if (!_warnedMissingFields) { Plugin.Log.LogWarning("Sosig reaction fields not found in this game build; tier reaction skipped."); _warnedMissingFields = true; }
                return;
            }
            float current = (float)field.GetValue(s);
            field.SetValue(s, current * factor);
        }
    }
}
```

`src/GunGameArena/Patches/WeaponPatches.cs`:
```csharp
using System;
using FistVR;
using GunGameArena.Behaviour;
using HarmonyLib;

namespace GunGameArena.Patches
{
    /// <summary>Re-applies the owner's tier whenever a tracked sosig picks up a weapon.</summary>
    [HarmonyPatch(typeof(SosigWeapon), "BotPickup")]
    public static class WeaponPatches
    {
        [HarmonyPostfix]
        private static void Postfix(SosigWeapon __instance, Sosig S)
        {
            try
            {
                if (!ArenaConfig.SkillTiers.Value || !Roster.Active) return;
                Slot slot = Roster.FindBySosig(S);
                if (slot != null) SkillApplier.ApplyWeapon(__instance, slot.Contestant.Tier);
            }
            catch (Exception e) { Plugin.Log.LogError("WeaponPatches: " + e); }
        }
    }
}
```

- [ ] **Step 3: Wire into Plugin.Awake**

After `Behaviour.GrudgeDirector.Install();` add:
```csharp
                Behaviour.HunterDirector.Install();
                Behaviour.SkillApplier.Install();
```

- [ ] **Step 4: Build**

Run: `dotnet build GunGameArena.sln -c Release`
Expected: success. `UnityEngine.AI.NavMesh` is in `UnityEngine.dll` for Unity 5.6 (verified in the spec research).

- [ ] **Step 5: USER CHECKPOINT — hunters and tiers**

User plays FFA. Expected log: `Hunters: <names>` every 10–20 s naming about 2 of 8 sosigs, and those sosigs visibly move toward the user; `Tier <X> applied to <name>` per spawn with a spread of tiers; no `reaction fields not found` warning. In play, `^` sosigs miss noticeably more than `^^^^` sosigs. Then set `Mode = Teams` and confirm no ally (blue) is ever listed under `Hunters:`.

- [ ] **Step 6: Commit**

```bash
git add src/GunGameArena
git commit -m "feat: hunter orders toward the player and per-contestant skill tiers"
```

---

### Task 15: Thunderstore packaging

**Files:**
- Create: `thunderstore/manifest.json`, `thunderstore/README.md`, `thunderstore/CHANGELOG.md`, `tools/make-icon.js`, `tools/pack.ps1`
- Generated: `thunderstore/icon.png`, `dist/GunGameArena-0.1.0.zip`

**Interfaces:**
- Consumes: built `GunGameArena.dll` and `GunGameArena.Core.dll` from `src/GunGameArena/bin/Release/`.

- [ ] **Step 1: Write manifest, README, CHANGELOG**

`thunderstore/manifest.json`:
```json
{
  "name": "GunGameArena",
  "version_number": "0.1.0",
  "website_url": "https://github.com/shaha/GunGameArena",
  "description": "GunGame companion: sosigs fight each other (FFA or teams), smarter match flow, and a Roblox-style leaderboard with portraits.",
  "dependencies": [
    "BepInEx-BepInExPack_H3VR-5.4.1700",
    "Kodeman-GunGame-1.0.2"
  ]
}
```

`thunderstore/README.md`:
```markdown
# GunGame Arena

A companion plugin for Kodeman's **GunGame**. Works on every GunGame map and with GunGame Progressions.

## What it does

- **Free For All** (default): every sosig fights every other sosig, and you.
- **Teams**: blue (you plus allies) versus red, optionally green and yellow. Ally kills never advance your weapon.
- **Natural match flow**: spread-out spawns, sosigs hold grudges against a few rivals at a time, a share of them hunt you, and each has a skill tier (Rookie to Elite) that changes aim, not fire rate.
- **Leaderboard HUD**: floating Arsenal-style cards with sosig head portraits, generated usernames, kill counts, crowns and team colours. Your card uses your Steam avatar.

## Config

`BepInEx/config/shaha.GunGameArena.cfg` after first launch. Sections: `Arena` (Mode, TeamCount, AllySosigs), `Leaderboard` (size, distance, names), `Behaviour` (spawns, grudges, hunters, tiers), `Tier.*` multipliers.

Sosig count is GunGame's own setting on the in-map panel.

## Credits

Kodeman for GunGame. Built with BepInEx and Harmony.
```

`thunderstore/CHANGELOG.md`:
```markdown
## 0.1.0
- Initial release: FFA/Teams sosig combat, spread spawns, grudges, hunters, skill tiers, leaderboard HUD.
```

- [ ] **Step 2: Write the icon generator**

`tools/make-icon.js` (pure Node, writes a 256×256 PNG: dark background, gold crown, red/blue split bar):
```javascript
const fs = require('fs');
const zlib = require('zlib');
const S = 256;
const px = Buffer.alloc(S * S * 4);
function set(x, y, r, g, b) { const i = (y * S + x) * 4; px[i] = r; px[i + 1] = g; px[i + 2] = b; px[i + 3] = 255; }
for (let y = 0; y < S; y++) for (let x = 0; x < S; x++) set(x, y, 30, 30, 34);
for (let y = 200; y < 232; y++) for (let x = 24; x < S - 24; x++) x < S / 2 ? set(x, y, 30, 63, 168) : set(x, y, 168, 30, 30);
for (let y = 60; y < 180; y++) for (let x = 48; x < 208; x++) {
  const base = y >= 130;
  const h = y - 60;
  const half = Math.max(6, 30 - Math.floor(h / 3));
  const spike = Math.abs(x - 80) <= half || Math.abs(x - 128) <= half + 8 || Math.abs(x - 176) <= half;
  if (base || spike) set(x, y, 245, 197, 66);
}
function crc32(buf) { let c, crc = 0xffffffff; for (let n = 0; n < buf.length; n++) { c = (crc ^ buf[n]) & 0xff; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; crc = (crc >>> 8) ^ c; } return (crc ^ 0xffffffff) >>> 0; }
function chunk(type, data) { const len = Buffer.alloc(4); len.writeUInt32BE(data.length); const td = Buffer.concat([Buffer.from(type), data]); const crc = Buffer.alloc(4); crc.writeUInt32BE(crc32(td)); return Buffer.concat([len, td, crc]); }
const raw = Buffer.alloc((S * 4 + 1) * S);
for (let y = 0; y < S; y++) { raw[y * (S * 4 + 1)] = 0; px.copy(raw, y * (S * 4 + 1) + 1, y * S * 4, (y + 1) * S * 4); }
const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(S, 0); ihdr.writeUInt32BE(S, 4); ihdr[8] = 8; ihdr[9] = 6; ihdr[10] = 0; ihdr[11] = 0; ihdr[12] = 0;
const png = Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk('IHDR', ihdr), chunk('IDAT', zlib.deflateSync(raw)), chunk('IEND', Buffer.alloc(0))]);
fs.writeFileSync(process.argv[2] || 'thunderstore/icon.png', png);
console.log('icon written', png.length, 'bytes');
```

Run: `node tools/make-icon.js thunderstore/icon.png`
Expected: `icon written … bytes`; open the PNG and confirm 256×256.

- [ ] **Step 3: Write the pack script**

`tools/pack.ps1`:
```powershell
param([string]$Configuration = "Release")
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$manifest = Get-Content "$root\thunderstore\manifest.json" | ConvertFrom-Json
$version = $manifest.version_number
$bin = "$root\src\GunGameArena\bin\$Configuration"
$stage = "$root\dist\stage"
if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }
New-Item -ItemType Directory -Force "$stage\plugins\GunGameArena" | Out-Null
Copy-Item "$bin\GunGameArena.dll", "$bin\GunGameArena.Core.dll" "$stage\plugins\GunGameArena\"
Copy-Item "$root\thunderstore\manifest.json", "$root\thunderstore\README.md", "$root\thunderstore\CHANGELOG.md", "$root\thunderstore\icon.png" $stage
$zip = "$root\dist\GunGameArena-$version.zip"
if (Test-Path $zip) { Remove-Item -Force $zip }
Compress-Archive -Path "$stage\*" -DestinationPath $zip
Remove-Item -Recurse -Force $stage
Write-Host "Packed $zip"
```

Run:
```powershell
dotnet build GunGameArena.sln -c Release
powershell -ExecutionPolicy Bypass -File tools\pack.ps1
```
Expected: `Packed …\dist\GunGameArena-0.1.0.zip`; the zip root contains `manifest.json`, `README.md`, `CHANGELOG.md`, `icon.png`, `plugins/GunGameArena/GunGameArena.dll`, `plugins/GunGameArena/GunGameArena.Core.dll`.

- [ ] **Step 4: Verify the zip installs through the mod manager**

USER CHECKPOINT (optional, recommended before publishing): in Thunderstore Mod Manager, Settings → Import local mod → select the zip. It must install into `plugins\GunGameArena\` and the game must log `GunGame Arena 0.1.0 loaded`. Remove the manually copied build first to avoid duplicate plugin GUIDs.

- [ ] **Step 5: Commit**

```bash
git add thunderstore tools .gitignore
git commit -m "chore: thunderstore packaging, icon generator and pack script"
```

---

## Self-Review

**Spec coverage**

| Spec section | Task |
|---|---|
| §4 Arena teams config, team ids, spawner patch, vacancy rule | 8 (config), 2 (ids), 9 (patch, `Slot.IsVacant`) |
| §4b.1 Spread-out spawns | 7 (chooser), 13 (patch) |
| §4b.2 Grudges incl. retaliation via ProcessDamage | 7 (selector), 13 (director), 10 (`RecordHit` MakeEnemy) |
| §4b.3 Hunters | 7 (picker), 14 |
| §4b.4 Skill tiers incl. chevrons on card, BotPickup re-apply | 6 (table/roller), 14 (applier, patch), 12 (chevrons) |
| §5 Kill tracking, attribution, progression guard, player deaths | 4, 10 |
| §6 Roster, persistence, player contestant | 9 |
| §7 Names | 5 |
| §8 Portraits, Steam avatar, fallbacks | 11 |
| §9 HUD config, placement, layout, ranking, crowns, colours, refresh | 8, 3, 6, 12 |
| §10 Error handling | every patch/handler wrapped; plugin disables on init failure (8) |
| §11 Tests | 1–7; in-game checkpoints in 8–14 |
| §12 Build & shipping | 1, 15 |

Gaps found and resolved during review: the spec's "GunGame types missing → disable" is covered by the hard `BepInDependency` (BepInEx refuses to load us without GunGame) plus the try/catch in `Plugin.Awake`. `RoundEnded` on scene change was added (Task 8) so HUD/directors are cleaned up between maps.

**Placeholder scan:** none. Every code step contains the full file.

**Type consistency check:** `Slot`, `Roster.AllContestants`, `Roster.LivingSosigSlots()`, `Roster.ToVec`, `SpawnerPatches.SosigBound`, `KillTracker.KillRegistered`, `ArenaConfig.MultipliersFor`, `TierTable.Chevrons`, `Ranking.IsPinnedPlayer`, `Sprites.Solid/Crown/FallbackAvatar` are used with the same names and signatures in every task that references them. `Contestant.Tier` is declared in Task 3 and typed by the `SkillTier` stub that Task 6 replaces in place.
