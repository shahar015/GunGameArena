# Team Deathmatch Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make Teams mode a real Team Deathmatch: points-to-win victory with a HUD banner and GunGame's end area, looping weapon rotation, dimmed "Number of weapons" controls, team scores and subtitle on the HUD, blue dot + name tags over teammates, friendly fire off by default; plus panel note polish and two new panel rows.

**Architecture:** Core gains `TeamScore` (pure scoring/winner), `HudPalette.TeamName`, and `PanelModel.StepPointsToWin`. Plugin gains `Behaviour/TeamMatch.cs` (victory watcher, `Promote` loop prefix, friendly-fire prefix, weapon-count lock), HUD additions in `LeaderboardHud` (scores, subtitle, banner), and `Hud/TeamTags.cs` (floating teammate tags). Panel gets two Teams-only rows and shorter notes.

**Tech Stack:** C# net35, Unity 5.6 UI, HarmonyLib, BepInEx config, xunit (Core, net10.0).

**Spec:** `docs/superpowers/specs/2026-09-20-team-deathmatch-addendum.md`

## Global Constraints

- Plugin `net35`; Core `net35;net8.0`, no Unity/game refs, no LINQ/records/ValueTuple in Core. No `?.` anywhere in `src/`.
- Every Harmony patch body, event handler, coroutine step and `LateUpdate` wrapped in try/catch that logs via `Plugin.Log.LogError` and never rethrows; no `yield` inside a try that has a catch.
- `Progression.CurrentWeaponId` has a **private setter**: set it with `HarmonyLib.AccessTools.Property(typeof(Progression), "CurrentWeaponId").SetValue(instance, value, null)`.
- `WeaponCountOption._counterText` is a private serialized `Text`: read via `AccessTools.Field`. Its arrow buttons are found by scanning `UnityEngine.UI.Button`s whose `onClick` persistent target is the `WeaponCountOption` instance (`onClick.GetPersistentEventCount()` / `GetPersistentTarget(i)`).
- Config section `Teams`: `PointsToWin` int 30 (range 5..200), `FriendlyFire` bool false, `TagRange` float 30.
- Team names: 0 BLUE, 1 RED, 2 GREEN, 3+ YELLOW. Colours from existing `HudPalette.TeamColor`.
- Commit per task, conventional messages, no `Co-Authored-By` trailer; never commit `.superpowers/` or `dist/`.
- Build `dotnet build GunGameArena.slnx -c Release`; tests `dotnet test tests\GunGameArena.Core.Tests -c Release` (50 before this plan).

---

### Task 1: Core — TeamScore, TeamName, StepPointsToWin

**Files:**
- Create: `src/GunGameArena.Core/TeamScore.cs`
- Modify: `src/GunGameArena.Core/HudPalette.cs` (add `TeamName`), `src/GunGameArena.Core/PanelModel.cs` (add `StepPointsToWin`, constants)
- Test: `tests/GunGameArena.Core.Tests/TeamScoreTests.cs`

**Interfaces:**
- Produces: `static class TeamScore { int Total(IEnumerable<Contestant>, int teamIndex); int Winner(IEnumerable<Contestant>, int teamCount, int pointsToWin) /* team index or -1; lowest index on ties */ }`
- Produces: `HudPalette.TeamName(int teamIndex)` → "BLUE" | "RED" | "GREEN" | "YELLOW".
- Produces: `PanelModel.StepPointsToWin(int current, int dir)` (±5, clamp 5..200), constants `PointsStep = 5`, `MinPoints = 5`, `MaxPoints = 200`.

- [ ] **Step 1: Failing tests** — `tests/GunGameArena.Core.Tests/TeamScoreTests.cs`:
```csharp
using GunGameArena.Core;
using Xunit;

public class TeamScoreTests
{
    static Contestant C(int id, int team, int kills, bool player = false)
        => new Contestant { Id = id, TeamIndex = team, Kills = kills, IsPlayer = player, IsAlive = true };

    [Fact]
    public void Total_sums_kills_of_a_team_including_the_player()
    {
        var all = new[] { C(0, 0, 4, player: true), C(1, 0, 3), C(2, 1, 9), C(3, 2, 1) };
        Assert.Equal(7, TeamScore.Total(all, 0));
        Assert.Equal(9, TeamScore.Total(all, 1));
        Assert.Equal(0, TeamScore.Total(all, 3));
    }

    [Fact]
    public void Winner_is_minus_one_until_a_team_reaches_the_target()
    {
        var all = new[] { C(0, 0, 10, player: true), C(1, 1, 12) };
        Assert.Equal(-1, TeamScore.Winner(all, 2, 30));
        Assert.Equal(1, TeamScore.Winner(all, 2, 12));
    }

    [Fact]
    public void Winner_prefers_lowest_team_index_on_ties()
    {
        var all = new[] { C(0, 0, 20, player: true), C(1, 1, 20) };
        Assert.Equal(0, TeamScore.Winner(all, 2, 20));
    }

    [Fact]
    public void Team_names_follow_palette_order()
    {
        Assert.Equal("BLUE", HudPalette.TeamName(0));
        Assert.Equal("RED", HudPalette.TeamName(1));
        Assert.Equal("GREEN", HudPalette.TeamName(2));
        Assert.Equal("YELLOW", HudPalette.TeamName(3));
        Assert.Equal("YELLOW", HudPalette.TeamName(7));
    }

    [Fact]
    public void Points_to_win_steps_by_five_and_clamps()
    {
        Assert.Equal(35, PanelModel.StepPointsToWin(30, +1));
        Assert.Equal(5, PanelModel.StepPointsToWin(5, -1));
        Assert.Equal(200, PanelModel.StepPointsToWin(200, +1));
    }
}
```

- [ ] **Step 2: Run** `dotnet test tests\GunGameArena.Core.Tests -c Release` → compile errors (RED).

- [ ] **Step 3: Implement**

`src/GunGameArena.Core/TeamScore.cs`:
```csharp
using System.Collections.Generic;

namespace GunGameArena.Core
{
    public static class TeamScore
    {
        public static int Total(IEnumerable<Contestant> contestants, int teamIndex)
        {
            int sum = 0;
            foreach (var c in contestants) if (c != null && c.TeamIndex == teamIndex) sum += c.Kills;
            return sum;
        }

        /// <summary>First team (lowest index) whose total reaches pointsToWin, else -1.</summary>
        public static int Winner(IEnumerable<Contestant> contestants, int teamCount, int pointsToWin)
        {
            var list = new List<Contestant>(contestants);
            for (int t = 0; t < teamCount; t++)
                if (Total(list, t) >= pointsToWin) return t;
            return -1;
        }
    }
}
```

Add to `HudPalette` (inside the class):
```csharp
        public static string TeamName(int teamIndex)
        {
            switch (teamIndex)
            {
                case 0: return "BLUE";
                case 1: return "RED";
                case 2: return "GREEN";
                default: return "YELLOW";
            }
        }
```

Add to `PanelModel`:
```csharp
        public const int PointsStep = 5;
        public const int MinPoints = 5;
        public const int MaxPoints = 200;

        public static int StepPointsToWin(int current, int dir)
        {
            return Math.Max(MinPoints, Math.Min(MaxPoints, current + (dir >= 0 ? PointsStep : -PointsStep)));
        }
```

- [ ] **Step 4: Run tests** → 55 passing. Then `dotnet build GunGameArena.slnx -c Release` clean.

- [ ] **Step 5: Commit** `feat(core): team scoring, team names and points-to-win stepping`

---

### Task 2: Plugin — config, panel rows/notes, TeamMatch rules, deferred panel fixes

**Files:**
- Modify: `src/GunGameArena/ArenaConfig.cs`, `src/GunGameArena/Panel/ArenaPanel.cs`, `src/GunGameArena/Panel/PanelInstaller.cs`, `src/GunGameArena/Plugin.cs`, `docs/MORNING-CHECKLIST.md`
- Create: `src/GunGameArena/Behaviour/TeamMatch.cs`

**Interfaces:**
- Produces: `ArenaConfig.PointsToWin`, `ArenaConfig.FriendlyFire`, `ArenaConfig.TagRange` (ConfigEntry<int>/<bool>/<float>).
- Produces: `static class TeamMatch { void Install(); void ApplyWeaponCountLock(); event Action<int /*teamIndex*/> TeamWon; }`. Task 3 subscribes to `TeamWon` to show the banner and reads scores itself via `TeamScore`.

- [ ] **Step 1: Config** — in `ArenaConfig.Bind` add (declare fields too):
```csharp
            PointsToWin = cfg.Bind("Teams", "PointsToWin", 30, new ConfigDescription("Team Deathmatch: team score that ends the round.", new AcceptableValueRange<int>(5, 200)));
            FriendlyFire = cfg.Bind("Teams", "FriendlyFire", false, "Team Deathmatch: when false your shots never damage your own team.");
            TagRange = cfg.Bind("Teams", "TagRange", 30f, "Team Deathmatch: teammate name tags are hidden beyond this distance (metres).");
```

- [ ] **Step 2: Panel** — in `ArenaPanel.BuildRows`:
  - Replace the two notes with `(each sosig fights 3 rivals at a time, not everyone)` and `(every 10–20 s a few sosigs are sent toward you)`.
  - After the Skill tiers toggle add `y -= 38f;` and a note `(each sosig gets an aim level: Rookie ^ … Elite ^^^^)` (same style as the other notes), then `y -= 40f;` before the footer.
  - After the Allies row add two Teams-only rows (same column layout as Mode; fields `_pointsLabel`, `_pointsValue`, `_friendlyFireBtn`):
```csharp
            _pointsLabel = UiFactory.MakeText(p, "PointsLabel", "Points to win", 26, Color.white, new Vector2(-200f, y), new Vector2(180f, RowH), TextAnchor.MiddleLeft, FontStyle.Normal);
            UiFactory.MakeButton(p, "PointsPrev", "<", new Vector2(-60f, y), new Vector2(50f, BtnH), () => Click(() => { ArenaConfig.PointsToWin.Value = PanelModel.StepPointsToWin(ArenaConfig.PointsToWin.Value, -1); }), out dummy);
            _pointsValue = UiFactory.MakeText(p, "PointsValue", "", 26, Color.white, new Vector2(95f, y), new Vector2(230f, RowH), TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.MakeButton(p, "PointsNext", ">", new Vector2(250f, y), new Vector2(50f, BtnH), () => Click(() => { ArenaConfig.PointsToWin.Value = PanelModel.StepPointsToWin(ArenaConfig.PointsToWin.Value, +1); }), out dummy);
            y -= RowH;
            UiFactory.MakeButton(p, "FriendlyFireToggle", "", new Vector2(0f, y), new Vector2(420f, BtnH), () => Click(() => { ArenaConfig.FriendlyFire.Value = !ArenaConfig.FriendlyFire.Value; }), out _friendlyFireBtn);
            y -= RowH + 10f;
```
    (and remove the previous `y -= RowH + 10f;` that followed Allies so spacing stays consistent).
  - In `Refresh`: `_pointsValue.text = ArenaConfig.PointsToWin.Value.ToString();` `_friendlyFireBtn.text = PanelModel.ToggleLabel("Friendly fire", ArenaConfig.FriendlyFire.Value);` include `_pointsLabel`, `_pointsValue`, `_friendlyFireBtn` in the dim set (alpha 0.4 when not Teams). After refreshing, call `Behaviour.TeamMatch.ApplyWeaponCountLock();` inside its own try/catch.
  - `Height` → `820f` (verify footer bottom > −Height; adjust to 840f if not).

- [ ] **Step 3: Deferred panel fixes** — in `PanelInstaller.PlaceUsingMoreOptionsBoard`'s catch: `if (_panel != null) { UnityEngine.Object.Destroy(_panel.gameObject); _panel = null; }` before `return false;`. Wrap `EncapsulateLocal`'s body in try/catch (log, no rethrow). After a successful build in `TryBuild`, call `Behaviour.TeamMatch.ApplyWeaponCountLock();` (try/caught).

- [ ] **Step 4: TeamMatch** — `src/GunGameArena/Behaviour/TeamMatch.cs`:
```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FistVR;
using GunGame.Scripts;
using GunGame.Scripts.Options;
using GunGameArena.Core;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace GunGameArena.Behaviour
{
    /// <summary>Team Deathmatch rules: points victory, looping rotation, friendly fire, weapon-count lock.</summary>
    public static class TeamMatch
    {
        public static event Action<int> TeamWon;

        private const float EndDelaySeconds = 5f;
        private static bool _victoryDeclared;
        private static TeamMatchRunner _runner;

        private static readonly PropertyInfo CurrentWeaponIdProp = AccessTools.Property(typeof(Progression), "CurrentWeaponId");
        private static readonly FieldInfo CounterTextField = AccessTools.Field(typeof(WeaponCountOption), "_counterText");

        // weapon-count lock state
        private static readonly List<FVRPointableButton> _lockedPointables = new List<FVRPointableButton>();
        private static readonly Dictionary<Graphic, Color> _originalColors = new Dictionary<Graphic, Color>();
        private static bool _locked;

        public static bool IsTeams { get { return Roster.Active && Roster.Mode == TeamMode.Teams; } }

        public static void Install()
        {
            GunGameHooks.RoundStarting += OnRoundStarting;
            GunGameHooks.RoundEnded += OnRoundEnded;
            Roster.Changed += OnRosterChanged;
        }

        private static void OnRoundStarting()
        {
            try { _victoryDeclared = false; ApplyWeaponCountLock(); }
            catch (Exception e) { Plugin.Log.LogError("TeamMatch.OnRoundStarting: " + e); }
        }

        private static void OnRoundEnded()
        {
            try { _victoryDeclared = false; _lockedPointables.Clear(); _originalColors.Clear(); _locked = false; if (_runner != null) { UnityEngine.Object.Destroy(_runner.gameObject); _runner = null; } }
            catch (Exception e) { Plugin.Log.LogError("TeamMatch.OnRoundEnded: " + e); }
        }

        private static void OnRosterChanged()
        {
            try
            {
                if (!IsTeams || _victoryDeclared) return;
                var gm = MonoBehaviourSingleton<GameManager>.Instance;
                if (gm == null || gm.GameEnded) return;
                int winner = TeamScore.Winner(Roster.AllContestants, TeamAssigner.ClampTeamCount(ArenaConfig.TeamCount.Value), ArenaConfig.PointsToWin.Value);
                if (winner < 0) return;
                _victoryDeclared = true;
                Plugin.Log.LogInfo("TEAM VICTORY: " + HudPalette.TeamName(winner) + " reached " + ArenaConfig.PointsToWin.Value + " points.");
                if (TeamWon != null) TeamWon(winner);
                Runner().StartCoroutine(Runner().EndAfterDelay());
            }
            catch (Exception e) { Plugin.Log.LogError("TeamMatch.OnRosterChanged: " + e); }
        }

        private static TeamMatchRunner Runner()
        {
            if (_runner == null) _runner = new GameObject("GunGameArena_TeamMatch").AddComponent<TeamMatchRunner>();
            return _runner;
        }

        /// <summary>Called by the Promote prefix: loop the rotation instead of ending the round.</summary>
        public static void LoopRotationIfNeeded(Progression progression)
        {
            if (!IsTeams || progression == null || CurrentWeaponIdProp == null) return;
            int poolCount = GameSettings.CurrentPool != null ? GameSettings.CurrentPool.GetWeaponCount() : int.MaxValue;
            int limit = Math.Min(poolCount, WeaponCountOption.WeaponCount);
            if (progression.CurrentWeaponId + 1 >= limit)
            {
                CurrentWeaponIdProp.SetValue(progression, -1, null);
                Plugin.Log.LogInfo("Team Deathmatch: weapon rotation looped.");
            }
        }

        /// <summary>Called by the ProcessDamage prefix. True = block this damage.</summary>
        public static bool ShouldBlockFriendlyFire(Sosig victim, Damage d)
        {
            if (!IsTeams || ArenaConfig.FriendlyFire.Value || d == null || victim == null) return false;
            if (d.Source_IFF != Roster.PlayerIff) return false;
            Slot slot = Roster.FindBySosig(victim);
            return slot != null && slot.Contestant.TeamIndex == 0;
        }

        public static void ApplyWeaponCountLock()
        {
            try
            {
                bool want = ArenaConfig.Mode.Value == TeamMode.Teams;
                var option = UnityEngine.Object.FindObjectOfType<WeaponCountOption>();
                if (option == null) return;
                if (want && !_locked) Lock(option);
                else if (!want && _locked) Unlock();
            }
            catch (Exception e) { Plugin.Log.LogError("TeamMatch.ApplyWeaponCountLock: " + e); }
        }

        private static void Lock(WeaponCountOption option)
        {
            _lockedPointables.Clear(); _originalColors.Clear();
            var buttons = UnityEngine.Object.FindObjectsOfType<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                Button b = buttons[i];
                bool targetsOption = false;
                for (int k = 0; k < b.onClick.GetPersistentEventCount(); k++)
                    if ((object)b.onClick.GetPersistentTarget(k) == (object)option) targetsOption = true;
                if (!targetsOption) continue;
                var p = b.GetComponent<FVRPointableButton>();
                if (p != null && p.enabled) { p.enabled = false; _lockedPointables.Add(p); }
                Dim(b.GetComponent<Graphic>());
                foreach (var g in b.GetComponentsInChildren<Graphic>(true)) Dim(g);
            }
            var counter = CounterTextField != null ? CounterTextField.GetValue(option) as Text : null;
            if (counter != null)
            {
                Dim(counter);
                if (counter.transform.parent != null)
                    foreach (var g in counter.transform.parent.GetComponentsInChildren<Graphic>(true)) Dim(g);
            }
            _locked = true;
            Plugin.Log.LogInfo("Team Deathmatch: 'Number of weapons' controls locked (" + _lockedPointables.Count + " buttons).");
        }

        private static void Dim(Graphic g)
        {
            if (g == null || _originalColors.ContainsKey(g)) return;
            _originalColors[g] = g.color;
            Color c = g.color; c.a *= 0.4f; g.color = c;
        }

        private static void Unlock()
        {
            for (int i = 0; i < _lockedPointables.Count; i++) if (_lockedPointables[i] != null) _lockedPointables[i].enabled = true;
            foreach (var kv in _originalColors) if (kv.Key != null) kv.Key.color = kv.Value;
            _lockedPointables.Clear(); _originalColors.Clear();
            _locked = false;
            Plugin.Log.LogInfo("'Number of weapons' controls restored.");
        }

        public class TeamMatchRunner : MonoBehaviour
        {
            public IEnumerator EndAfterDelay()
            {
                yield return new WaitForSeconds(EndDelaySeconds);
                EndRound();
            }

            private static void EndRound()
            {
                try
                {
                    var gm = MonoBehaviourSingleton<GameManager>.Instance;
                    if (gm != null && !gm.GameEnded) gm.EndGame();
                }
                catch (Exception e) { Plugin.Log.LogError("TeamMatch.EndRound: " + e); }
            }
        }
    }
}
```

- [ ] **Step 5: Patches** — create `src/GunGameArena/Patches/TeamMatchPatches.cs`:
```csharp
using System;
using FistVR;
using GunGame.Scripts;
using GunGameArena.Behaviour;
using HarmonyLib;

namespace GunGameArena.Patches
{
    [HarmonyPatch(typeof(Progression), "Promote")]
    public static class PromoteLoopPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Progression __instance)
        {
            try { TeamMatch.LoopRotationIfNeeded(__instance); }
            catch (Exception e) { Plugin.Log.LogError("PromoteLoopPatch: " + e); }
        }
    }

    [HarmonyPatch(typeof(Sosig), "ProcessDamage", new[] { typeof(Damage), typeof(SosigLink) })]
    public static class FriendlyFirePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Sosig __instance, Damage d)
        {
            try { return !TeamMatch.ShouldBlockFriendlyFire(__instance, d); }
            catch (Exception e) { Plugin.Log.LogError("FriendlyFirePatch: " + e); return true; }
        }
    }
}
```
Note: Harmony runs all prefixes before postfixes; a `false` return skips the original AND the postfixes' view of a hit is irrelevant because `KillTracker.RecordHit` runs in a postfix that Harmony still calls — so in `KillTracker.RecordHit` add an early return when `TeamMatch.ShouldBlockFriendlyFire(victim, d)` is true (so blocked hits don't record grudges either).

- [ ] **Step 6: Wire** — `Plugin.Awake`: after `Panel.PanelInstaller.Install();` add `Behaviour.TeamMatch.Install();`.

- [ ] **Step 7: Checklist** — Session 2 (Teams) add bullets: "Number of weapons controls on GunGame's board are dimmed and unclickable; log `'Number of weapons' controls locked`." / "Reaching the last weapon loops back to the first (`weapon rotation looped`)." / "Shooting a blue sosig does nothing (Friendly fire OFF)." / "Panel shows Points to win and Friendly fire rows when Mode = Teams."

- [ ] **Step 8: Build** clean, tests 55. **Commit** `feat: team deathmatch rules — points victory, looping rotation, friendly fire, weapon-count lock; panel notes and rows`

---

### Task 3: HUD — team scores, subtitle, victory banner, teammate tags

**Files:**
- Modify: `src/GunGameArena/Hud/LeaderboardHud.cs`, `src/GunGameArena/Plugin.cs`, `docs/MORNING-CHECKLIST.md`
- Create: `src/GunGameArena/Hud/TeamTags.cs`

**Interfaces:**
- Consumes: `TeamMatch.TeamWon`, `TeamScore.Total`, `HudPalette.TeamName/TeamColor`, `ArenaConfig.PointsToWin/TagRange`, `Roster.LivingSosigSlots()`, `SpawnerPatches.SosigBound`.
- Produces: `LeaderboardHud.ShowBanner(string text, Color color, float seconds)` (public static, no-op if HUD absent).

- [ ] **Step 1: LeaderboardHud additions**
  - Fields: `Text _leftScore, _rightScore, _subtitle, _banner;`
  - In `Build()`: create `_leftScore` (RectTransform anchored top-centre at `(-330, -22)` size `(300, 44)`, 26 pt bold, right-aligned), `_rightScore` at `(330, -22)` size `(300, 44)` left-aligned, `_subtitle` at `(0, -60)` size `(600, 30)` 18 pt alpha 0.85 centred, `_banner` at `(0, -420)` size `(900, 90)` 64 pt bold centred with `Outline` (black, 3 px), initially `gameObject.SetActive(false)`. Use the same manual RectTransform/Text creation style as the header. Move the row down by 20 px (`anchoredPosition (0, -170)`) so the subtitle fits.
  - In `Rebuild()`: `bool teams = Roster.Mode == TeamMode.Teams;` set `_leftScore/_rightScore/_subtitle` active only when `teams`; when teams: `_leftScore.text = "BLUE TEAM: " + TeamScore.Total(all, 0)` coloured `TeamColor(0)`; `_rightScore.text` = for t in 1..teamCount−1 join `HudPalette.TeamName(t) + " " + TeamScore.Total(all, t)` with " · " (2 teams → `"RED TEAM: n"`), colour `TeamColor(1)`; `_subtitle.text = "First team to reach " + ArenaConfig.PointsToWin.Value + " points wins"`. `all` is `new List<Contestant>(Roster.AllContestants)` computed once.
  - `public static void ShowBanner(string text, Color color, float seconds)`: try/catch; if `_instance == null` return; set text/colour, activate, `_instance.StartCoroutine(_instance.HideBannerAfter(seconds))` (coroutine: `yield return new WaitForSeconds(seconds); if (_banner != null) _banner.gameObject.SetActive(false);` — no try/catch around the yield).
  - `Install()`: also `TeamMatch.TeamWon += OnTeamWon;` with `private static void OnTeamWon(int team) { try { ShowBanner(HudPalette.TeamName(team) + " TEAM WINS", ContestantCard.ToColor(HudPalette.TeamColor(team)), 5f); } catch (Exception e) { Plugin.Log.LogError("LeaderboardHud.OnTeamWon: " + e); } }`.

- [ ] **Step 2: TeamTags** — `src/GunGameArena/Hud/TeamTags.cs`:
```csharp
using System;
using System.Collections.Generic;
using FistVR;
using GunGameArena.Core;
using GunGameArena.Patches;
using UnityEngine;
using UnityEngine.UI;

namespace GunGameArena.Hud
{
    /// <summary>Blue dot + name floating above each teammate in Team Deathmatch.</summary>
    public class TeamTags : MonoBehaviour
    {
        private const float HeightAboveHead = 0.35f;
        private const float TagScale = 0.003f;
        private static TeamTags _instance;
        private readonly Dictionary<Slot, Text> _tags = new Dictionary<Slot, Text>();
        private Font _font;

        public static void Install()
        {
            GunGameHooks.RoundStarted += OnRoundStarted;
            GunGameHooks.RoundEnded += OnRoundEnded;
            SpawnerPatches.SosigBound += OnSosigBound;
        }

        private static void OnRoundStarted()
        {
            try
            {
                if (Roster.Mode != TeamMode.Teams) return;
                if (_instance == null) _instance = new GameObject("GunGameArena_TeamTags").AddComponent<TeamTags>();
                foreach (var slot in Roster.LivingSosigSlots()) _instance.EnsureTag(slot);
            }
            catch (Exception e) { Plugin.Log.LogError("TeamTags.OnRoundStarted: " + e); }
        }

        private static void OnRoundEnded()
        {
            try { if (_instance != null) Destroy(_instance.gameObject); _instance = null; }
            catch (Exception e) { Plugin.Log.LogError("TeamTags.OnRoundEnded: " + e); }
        }

        private static void OnSosigBound(Slot slot)
        {
            try { if (_instance != null && Roster.Mode == TeamMode.Teams) _instance.EnsureTag(slot); }
            catch (Exception e) { Plugin.Log.LogError("TeamTags.OnSosigBound: " + e); }
        }

        private void EnsureTag(Slot slot)
        {
            if (slot == null || slot.Contestant.TeamIndex != 0 || slot.Sosig == null) return;
            Text existing;
            if (_tags.TryGetValue(slot, out existing) && existing != null) return;
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var go = new GameObject("TeamTag_" + slot.Contestant.Name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(600f, 80f);
            go.transform.localScale = Vector3.one * TagScale;

            var text = new GameObject("Text", typeof(RectTransform)).AddComponent<Text>();
            text.transform.SetParent(go.transform, false);
            var rt = text.rectTransform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            text.font = _font; text.fontSize = 40; text.fontStyle = FontStyle.Bold; text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white; text.supportRichText = true; text.raycastTarget = false;
            Color blue = ContestantCard.ToColor(HudPalette.TeamColor(0));
            string hex = ColorUtility.ToHtmlStringRGB(blue);
            text.text = "<color=#" + hex + ">●</color> " + slot.Contestant.Name;
            var outline = text.gameObject.AddComponent<Outline>(); outline.effectColor = Color.black; outline.effectDistance = new Vector2(2f, -2f);
            _tags[slot] = text;
        }

        private void LateUpdate()
        {
            try
            {
                var body = GM.CurrentPlayerBody;
                if (body == null || body.Head == null) return;
                Vector3 head = body.Head.position;
                float range = ArenaConfig.TagRange.Value;
                var dead = new List<Slot>();
                foreach (var kv in _tags)
                {
                    Slot slot = kv.Key; Text text = kv.Value;
                    if (text == null) { dead.Add(slot); continue; }
                    if (slot.IsVacant || !slot.Contestant.IsAlive) { Destroy(text.canvas.gameObject); dead.Add(slot); continue; }
                    Transform anchor = (slot.Sosig.Links != null && slot.Sosig.Links.Count > 0 && slot.Sosig.Links[0] != null) ? slot.Sosig.Links[0].transform : slot.Sosig.transform;
                    Transform tag = text.canvas.transform;
                    tag.position = anchor.position + Vector3.up * HeightAboveHead;
                    Vector3 toHead = head - tag.position; toHead.y = 0f;
                    if (toHead.sqrMagnitude > 0.0001f) tag.rotation = Quaternion.LookRotation(-toHead.normalized, Vector3.up);
                    bool visible = Vector3.Distance(head, tag.position) <= range;
                    if (text.canvas.enabled != visible) text.canvas.enabled = visible;
                }
                for (int i = 0; i < dead.Count; i++) _tags.Remove(dead[i]);
            }
            catch (Exception e) { Plugin.Log.LogError("TeamTags.LateUpdate: " + e); }
        }
    }
}
```
Note: `ColorUtility` exists in Unity 5.6. If `Quaternion.LookRotation(-toHead)` renders the text mirrored in-game, flip the sign (`toHead.normalized`) — record whichever is right in the checklist.

- [ ] **Step 3: Wire** — `Plugin.Awake`: after `Behaviour.TeamMatch.Install();` add `Hud.TeamTags.Install();`.

- [ ] **Step 4: Checklist** — Session 2 add: "HUD shows `BLUE TEAM: n` left and `RED TEAM: n` right of the title, subtitle `First team to reach 30 points wins`." / "Blue ● name tags float above your teammates, facing you, disappearing beyond 30 m." / "When a team reaches the target: `<TEAM> TEAM WINS` banner for 5 s then GunGame's end area; log `TEAM VICTORY`."

- [ ] **Step 5: Build** clean, tests 55. **Commit** `feat: team scores, subtitle, victory banner and teammate tags on the HUD`

---

## Self-Review
- Spec coverage: config (T2 S1), panel notes/rows (T2 S2), rules (T2 S4–5: victory, loop, friendly fire, lock), HUD scores/subtitle/banner (T3 S1), tags (T3 S2), deferred fixes (T2 S3). ✔
- Type consistency: `TeamScore.Total/Winner`, `HudPalette.TeamName`, `PanelModel.StepPointsToWin`, `TeamMatch.TeamWon/ApplyWeaponCountLock/ShouldBlockFriendlyFire/LoopRotationIfNeeded`, `LeaderboardHud.ShowBanner`, `ContestantCard.ToColor` used consistently.
- Private-setter and private-field access documented in Global Constraints and used in T2 S4.
