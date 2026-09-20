# Arena Panel Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** An in-map VR settings panel for GunGame Arena, built at runtime beside GunGame's "More options" panel on every GunGame map, editing the plugin's main config values with the laser pointer.

**Architecture:** `PanelModel` (Core, pure, tested) turns config values into row labels and applies cycle/step/toggle operations with clamping. `ArenaPanel` (plugin) builds a world-space Unity UI canvas using GunGame's button recipe (Image + Button + FVRPointableButton + BoxCollider) and forwards clicks to config entries. `PanelInstaller` finds GunGame's panel after scene load and positions ours next to it.

**Tech Stack:** C# net35, Unity 5.6 UI, H3VR `FistVR.FVRPointableButton`, BepInEx `ConfigEntry<T>`, xunit (Core tests, net10.0).

**Spec:** `docs/superpowers/specs/2026-09-20-arena-panel-addendum.md` (plus the main spec `docs/superpowers/specs/2026-09-19-gungame-arena-design.md`).

## Global Constraints

- Plugin is `net35`; Core is `net35;net8.0` with no Unity/game references. No `record`, `init`, `Span<T>`, `ValueTuple`, LINQ in Core, `string.Join(string, IEnumerable)`.
- Every click handler, coroutine step and scene hook body is wrapped in try/catch that logs via `Plugin.Log.LogError` and never rethrows. `yield` never sits inside a try block that has a catch.
- Config keys are the existing `ArenaConfig` entries; the panel never introduces new config keys.
- Button recipe (verbatim from GunGame's prefabs): RectTransform 230×30 (we vary width), `Image` sprite `UISprite` white, `Button` ColorTint normal (1,1,1) highlighted (0.96,0.96,0.96) pressed (0.784,0.784,0.784), `FVRPointableButton` MaxPointingRange 600 ColorUnselected white ColorSelected 0.96, `BoxCollider` size (w, h, 1), layer 0, child `Text` Arial 23 colour (0.196,0.196,0.196) centred.
- Canvas: `RenderMode.WorldSpace`, size 620×760, local scale 0.01, background `#2F5FD6` alpha 0.92.
- Commit after each task, conventional messages, no `Co-Authored-By` trailer. Never commit `.superpowers/` or `dist/`.
- Build: `dotnet build GunGameArena.slnx -c Release` (prints `GunGameArena copied to …`). Tests: `dotnet test tests\GunGameArena.Core.Tests -c Release` (44 before this plan).

---

### Task 1: PanelModel (Core)

**Files:**
- Create: `src/GunGameArena.Core/PanelModel.cs`
- Test: `tests/GunGameArena.Core.Tests/PanelModelTests.cs`

**Interfaces:**
- Produces:
```csharp
public class PanelState { public TeamMode Mode; public int TeamCount; public int AllySosigs; public bool Leaderboard, SpreadSpawns, Grudges, Hunters, SkillTiers; public float HunterShare; }
public static class PanelModel {
  public const int MaxAllies = 9; public const float HunterStep = 0.05f;
  public static string ModeLabel(TeamMode m);            // "Off" | "Free For All" | "Teams"
  public static TeamMode CycleMode(TeamMode m, int dir);  // dir ±1, wraps
  public static int StepTeamCount(int current, int dir);  // clamp 2..4
  public static int StepAllies(int current, int dir);     // -1 (Auto) .. MaxAllies, clamp
  public static string AlliesLabel(int allies);           // "Auto" or number
  public static float StepHunterShare(float current, int dir); // ±0.05, clamp 0..1, rounded to 2 dp
  public static string PercentLabel(float share);         // "25%"
  public static string ToggleLabel(string name, bool on); // "Hunters: ON"
  public static bool TeamRowsEnabled(TeamMode m);         // only Teams
}
```

- [ ] **Step 1: Write the failing tests**

`tests/GunGameArena.Core.Tests/PanelModelTests.cs`:
```csharp
using GunGameArena.Core;
using Xunit;

public class PanelModelTests
{
    [Fact]
    public void Mode_cycles_forward_and_backward_with_wrap()
    {
        Assert.Equal(TeamMode.FreeForAll, PanelModel.CycleMode(TeamMode.Off, +1));
        Assert.Equal(TeamMode.Teams, PanelModel.CycleMode(TeamMode.FreeForAll, +1));
        Assert.Equal(TeamMode.Off, PanelModel.CycleMode(TeamMode.Teams, +1));
        Assert.Equal(TeamMode.Teams, PanelModel.CycleMode(TeamMode.Off, -1));
    }

    [Fact]
    public void Mode_labels_are_human_readable()
    {
        Assert.Equal("Free For All", PanelModel.ModeLabel(TeamMode.FreeForAll));
        Assert.Equal("Teams", PanelModel.ModeLabel(TeamMode.Teams));
        Assert.Equal("Off", PanelModel.ModeLabel(TeamMode.Off));
    }

    [Fact]
    public void Team_count_clamps_between_2_and_4()
    {
        Assert.Equal(3, PanelModel.StepTeamCount(2, +1));
        Assert.Equal(4, PanelModel.StepTeamCount(4, +1));
        Assert.Equal(2, PanelModel.StepTeamCount(2, -1));
    }

    [Fact]
    public void Allies_step_from_auto_through_numbers_and_clamp()
    {
        Assert.Equal(0, PanelModel.StepAllies(-1, +1));
        Assert.Equal(-1, PanelModel.StepAllies(0, -1));
        Assert.Equal(-1, PanelModel.StepAllies(-1, -1));
        Assert.Equal(PanelModel.MaxAllies, PanelModel.StepAllies(PanelModel.MaxAllies, +1));
        Assert.Equal("Auto", PanelModel.AlliesLabel(-1));
        Assert.Equal("4", PanelModel.AlliesLabel(4));
    }

    [Fact]
    public void Hunter_share_steps_by_five_percent_and_clamps()
    {
        Assert.Equal(0.30f, PanelModel.StepHunterShare(0.25f, +1), 3);
        Assert.Equal(0f, PanelModel.StepHunterShare(0.03f, -1), 3);
        Assert.Equal(1f, PanelModel.StepHunterShare(0.98f, +1), 3);
        Assert.Equal("25%", PanelModel.PercentLabel(0.25f));
        Assert.Equal("100%", PanelModel.PercentLabel(1f));
    }

    [Fact]
    public void Toggle_label_and_team_rows()
    {
        Assert.Equal("Hunters: ON", PanelModel.ToggleLabel("Hunters", true));
        Assert.Equal("Grudges: OFF", PanelModel.ToggleLabel("Grudges", false));
        Assert.True(PanelModel.TeamRowsEnabled(TeamMode.Teams));
        Assert.False(PanelModel.TeamRowsEnabled(TeamMode.FreeForAll));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests\GunGameArena.Core.Tests -c Release` — expected: compile error `PanelModel` not found.

- [ ] **Step 3: Implement**

`src/GunGameArena.Core/PanelModel.cs`:
```csharp
using System;

namespace GunGameArena.Core
{
    /// <summary>Snapshot of the config values the in-map panel edits.</summary>
    public class PanelState
    {
        public TeamMode Mode;
        public int TeamCount;
        public int AllySosigs;
        public bool Leaderboard;
        public bool SpreadSpawns;
        public bool Grudges;
        public bool Hunters;
        public bool SkillTiers;
        public float HunterShare;
    }

    /// <summary>Pure label/step logic for the in-map settings panel.</summary>
    public static class PanelModel
    {
        public const int MaxAllies = 9;
        public const float HunterStep = 0.05f;

        public static string ModeLabel(TeamMode m)
        {
            switch (m)
            {
                case TeamMode.FreeForAll: return "Free For All";
                case TeamMode.Teams: return "Teams";
                default: return "Off";
            }
        }

        public static TeamMode CycleMode(TeamMode m, int dir)
        {
            int n = ((int)m + (dir >= 0 ? 1 : -1) + 3) % 3;
            return (TeamMode)n;
        }

        public static int StepTeamCount(int current, int dir)
        {
            return Math.Max(2, Math.Min(4, current + (dir >= 0 ? 1 : -1)));
        }

        public static int StepAllies(int current, int dir)
        {
            return Math.Max(-1, Math.Min(MaxAllies, current + (dir >= 0 ? 1 : -1)));
        }

        public static string AlliesLabel(int allies)
        {
            return allies < 0 ? "Auto" : allies.ToString();
        }

        public static float StepHunterShare(float current, int dir)
        {
            float v = current + (dir >= 0 ? HunterStep : -HunterStep);
            v = Math.Max(0f, Math.Min(1f, v));
            return (float)Math.Round(v, 2);
        }

        public static string PercentLabel(float share)
        {
            return ((int)Math.Round(share * 100f)).ToString() + "%";
        }

        public static string ToggleLabel(string name, bool on)
        {
            return name + ": " + (on ? "ON" : "OFF");
        }

        public static bool TeamRowsEnabled(TeamMode m)
        {
            return m == TeamMode.Teams;
        }
    }
}
```

- [ ] **Step 4: Run tests** — expected all pass (50 total).

- [ ] **Step 5: Commit**

```bash
git add src/GunGameArena.Core/PanelModel.cs tests/GunGameArena.Core.Tests/PanelModelTests.cs
git commit -m "feat(core): panel model for in-map settings"
```

---

### Task 2: ArenaPanel UI, installer and live HUD toggle (plugin)

**Files:**
- Create: `src/GunGameArena/Panel/UiFactory.cs`, `src/GunGameArena/Panel/ArenaPanel.cs`, `src/GunGameArena/Panel/PanelInstaller.cs`
- Modify: `src/GunGameArena/Hud/LeaderboardHud.cs` (add `SetEnabledLive`), `src/GunGameArena/Plugin.cs` (wire `Panel.PanelInstaller.Install()` after `Behaviour.SkillApplier.Install();`), `docs/MORNING-CHECKLIST.md` (add a Panel section)

**Interfaces:**
- Consumes: `ArenaConfig.*` entries, `PanelModel`, `GunGameHooks.RoundActive`, `Roster.Active`, `MonoBehaviourSingleton<GunGame.Scripts.Options.GameSettings>.Instance`, `FistVR.FVRPointableButton`.
- Produces: `LeaderboardHud.SetEnabledLive(bool)`.

- [ ] **Step 1: Add `SetEnabledLive` to LeaderboardHud**

In `src/GunGameArena/Hud/LeaderboardHud.cs` add inside the class (after `Hide()`):
```csharp
        /// <summary>Panel toggle: hide immediately when disabling; show now if a round is active when enabling.</summary>
        public static void SetEnabledLive(bool enabled)
        {
            try
            {
                if (!enabled) { Hide(); return; }
                if (Roster.Active) Show();
            }
            catch (Exception e) { Plugin.Log.LogError("LeaderboardHud.SetEnabledLive: " + e); }
        }
```
(`Show`/`Hide` are the existing private static methods; `Show` already checks `ArenaConfig.LeaderboardEnabled`, so the caller must set the config value before calling.)

- [ ] **Step 2: Write UiFactory**

`src/GunGameArena/Panel/UiFactory.cs`:
```csharp
using FistVR;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GunGameArena.Panel
{
    /// <summary>Builds UI pieces in GunGame's own style (verbatim values from its prefabs).</summary>
    public static class UiFactory
    {
        public static readonly Color ButtonText = new Color(0.196f, 0.196f, 0.196f, 1f);
        public static readonly Color Highlight = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f);
        public static readonly Color Pressed = new Color(0.78431374f, 0.78431374f, 0.78431374f, 1f);
        public static readonly Color PanelBlue = new Color(0.184f, 0.373f, 0.839f, 0.92f);

        private static Font _font;
        private static Sprite _uiSprite;

        public static Font Font
        {
            get { if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); return _font; }
        }

        public static Sprite UiSprite
        {
            get
            {
                if (_uiSprite == null)
                {
                    try { _uiSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd"); } catch { _uiSprite = null; }
                }
                return _uiSprite;
            }
        }

        public static RectTransform MakeRect(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 0;
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Text MakeText(Transform parent, string name, string text, int size, Color color, Vector2 pos, Vector2 box, TextAnchor anchor, FontStyle style)
        {
            var rt = MakeRect(parent, name, pos, box);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = anchor;
            t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>GunGame-style pointer button: Image + Button + FVRPointableButton + BoxCollider + Text child.</summary>
        public static Button MakeButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, UnityAction onClick, out Text labelText)
        {
            var rt = MakeRect(parent, name, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UiSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Highlight;
            colors.pressedColor = Pressed;
            btn.colors = colors;
            btn.onClick.AddListener(onClick);

            var col = rt.gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(size.x, size.y, 1f);
            col.isTrigger = true;

            var pointable = rt.gameObject.AddComponent<FVRPointableButton>();
            pointable.MaxPointingRange = 600f;
            pointable.ColorUnselected = Color.white;
            pointable.ColorSelected = Highlight;
            pointable.Button = btn;
            pointable.Image = img;

            labelText = MakeText(rt, "Text", label, 23, ButtonText, Vector2.zero, size, TextAnchor.MiddleCenter, FontStyle.Normal);
            var lrt = labelText.rectTransform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            return btn;
        }
    }
}
```

- [ ] **Step 3: Write ArenaPanel**

`src/GunGameArena/Panel/ArenaPanel.cs`:
```csharp
using System;
using GunGameArena.Core;
using GunGameArena.Hud;
using UnityEngine;
using UnityEngine.UI;

namespace GunGameArena.Panel
{
    /// <summary>World-space settings panel. Pure logic in PanelModel; this class only builds UI and forwards clicks.</summary>
    public class ArenaPanel : MonoBehaviour
    {
        public const float Width = 620f;
        public const float Height = 760f;
        private const float RowH = 52f;
        private const float FirstRowY = -110f;
        private const float BtnH = 34f;

        private Text _modeValue, _teamsValue, _alliesValue, _hunterShareValue;
        private Text _teamsLabel, _alliesLabel;
        private Text _leaderboardBtn, _spreadBtn, _grudgesBtn, _huntersBtn, _tiersBtn;

        public static ArenaPanel Build(Transform parentless)
        {
            var go = new GameObject("GunGameArena_Panel", typeof(RectTransform));
            go.layer = 0;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(Width, Height);
            go.transform.localScale = Vector3.one * 0.01f;

            var bg = UiFactory.MakeRect(go.transform, "Background", new Vector2(0f, -Height / 2f), new Vector2(Width, Height));
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.sprite = UiFactory.UiSprite; bgImg.type = Image.Type.Sliced; bgImg.color = UiFactory.PanelBlue; bgImg.raycastTarget = false;

            var panel = go.AddComponent<ArenaPanel>();
            panel.BuildRows();
            panel.Refresh();
            return panel;
        }

        private void BuildRows()
        {
            Transform p = transform;
            UiFactory.MakeText(p, "Title", "Arena", 44, Color.white, new Vector2(0f, -50f), new Vector2(Width - 40f, 60f), TextAnchor.MiddleCenter, FontStyle.Bold);

            float y = FirstRowY;
            // Mode
            UiFactory.MakeText(p, "ModeLabel", "Mode", 26, Color.white, new Vector2(-200f, y), new Vector2(180f, RowH), TextAnchor.MiddleLeft, FontStyle.Normal);
            Text dummy;
            UiFactory.MakeButton(p, "ModePrev", "<", new Vector2(-60f, y), new Vector2(50f, BtnH), () => Click(() => { ArenaConfig.Mode.Value = PanelModel.CycleMode(ArenaConfig.Mode.Value, -1); }), out dummy);
            _modeValue = UiFactory.MakeText(p, "ModeValue", "", 26, Color.white, new Vector2(95f, y), new Vector2(230f, RowH), TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.MakeButton(p, "ModeNext", ">", new Vector2(250f, y), new Vector2(50f, BtnH), () => Click(() => { ArenaConfig.Mode.Value = PanelModel.CycleMode(ArenaConfig.Mode.Value, +1); }), out dummy);
            y -= RowH;
            // Teams
            _teamsLabel = UiFactory.MakeText(p, "TeamsLabel", "Teams", 26, Color.white, new Vector2(-200f, y), new Vector2(180f, RowH), TextAnchor.MiddleLeft, FontStyle.Normal);
            UiFactory.MakeButton(p, "TeamsPrev", "<", new Vector2(-60f, y), new Vector2(50f, BtnH), () => Click(() => { ArenaConfig.TeamCount.Value = PanelModel.StepTeamCount(ArenaConfig.TeamCount.Value, -1); }), out dummy);
            _teamsValue = UiFactory.MakeText(p, "TeamsValue", "", 26, Color.white, new Vector2(95f, y), new Vector2(230f, RowH), TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.MakeButton(p, "TeamsNext", ">", new Vector2(250f, y), new Vector2(50f, BtnH), () => Click(() => { ArenaConfig.TeamCount.Value = PanelModel.StepTeamCount(ArenaConfig.TeamCount.Value, +1); }), out dummy);
            y -= RowH;
            // Allies
            _alliesLabel = UiFactory.MakeText(p, "AlliesLabel", "Allies", 26, Color.white, new Vector2(-200f, y), new Vector2(180f, RowH), TextAnchor.MiddleLeft, FontStyle.Normal);
            UiFactory.MakeButton(p, "AlliesPrev", "<", new Vector2(-60f, y), new Vector2(50f, BtnH), () => Click(() => { ArenaConfig.AllySosigs.Value = PanelModel.StepAllies(ArenaConfig.AllySosigs.Value, -1); }), out dummy);
            _alliesValue = UiFactory.MakeText(p, "AlliesValue", "", 26, Color.white, new Vector2(95f, y), new Vector2(230f, RowH), TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.MakeButton(p, "AlliesNext", ">", new Vector2(250f, y), new Vector2(50f, BtnH), () => Click(() => { ArenaConfig.AllySosigs.Value = PanelModel.StepAllies(ArenaConfig.AllySosigs.Value, +1); }), out dummy);
            y -= RowH + 10f;
            // Toggles
            UiFactory.MakeButton(p, "LeaderboardToggle", "", new Vector2(0f, y), new Vector2(420f, BtnH), () => Click(() =>
            {
                ArenaConfig.LeaderboardEnabled.Value = !ArenaConfig.LeaderboardEnabled.Value;
                LeaderboardHud.SetEnabledLive(ArenaConfig.LeaderboardEnabled.Value);
            }), out _leaderboardBtn);
            y -= RowH;
            UiFactory.MakeButton(p, "SpreadToggle", "", new Vector2(0f, y), new Vector2(420f, BtnH), () => Click(() => { ArenaConfig.SpreadSpawns.Value = !ArenaConfig.SpreadSpawns.Value; }), out _spreadBtn);
            y -= RowH;
            UiFactory.MakeButton(p, "GrudgesToggle", "", new Vector2(0f, y), new Vector2(420f, BtnH), () => Click(() => { ArenaConfig.Grudges.Value = !ArenaConfig.Grudges.Value; }), out _grudgesBtn);
            y -= 34f;
            UiFactory.MakeText(p, "GrudgesNote", "(each sosig fights 3 rivals at a time, not everyone — keeps FFA from being one blob)", 17, new Color(1f, 1f, 1f, 0.85f), new Vector2(0f, y), new Vector2(Width - 60f, 40f), TextAnchor.UpperCenter, FontStyle.Italic);
            y -= 40f;
            UiFactory.MakeButton(p, "HuntersToggle", "", new Vector2(0f, y), new Vector2(420f, BtnH), () => Click(() => { ArenaConfig.Hunters.Value = !ArenaConfig.Hunters.Value; }), out _huntersBtn);
            y -= 34f;
            UiFactory.MakeText(p, "HuntersNote", "(every 10–20 s a few sosigs are sent toward you — without this an FFA mostly ignores you)", 17, new Color(1f, 1f, 1f, 0.85f), new Vector2(0f, y), new Vector2(Width - 60f, 40f), TextAnchor.UpperCenter, FontStyle.Italic);
            y -= 44f;
            // Hunter pressure
            UiFactory.MakeText(p, "HunterShareLabel", "Hunter pressure", 26, Color.white, new Vector2(-170f, y), new Vector2(240f, RowH), TextAnchor.MiddleLeft, FontStyle.Normal);
            UiFactory.MakeButton(p, "HunterSharePrev", "<", new Vector2(20f, y), new Vector2(50f, BtnH), () => Click(() => { ArenaConfig.HunterShare.Value = PanelModel.StepHunterShare(ArenaConfig.HunterShare.Value, -1); }), out dummy);
            _hunterShareValue = UiFactory.MakeText(p, "HunterShareValue", "", 26, Color.white, new Vector2(130f, y), new Vector2(150f, RowH), TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.MakeButton(p, "HunterShareNext", ">", new Vector2(250f, y), new Vector2(50f, BtnH), () => Click(() => { ArenaConfig.HunterShare.Value = PanelModel.StepHunterShare(ArenaConfig.HunterShare.Value, +1); }), out dummy);
            y -= RowH;
            UiFactory.MakeButton(p, "TiersToggle", "", new Vector2(0f, y), new Vector2(420f, BtnH), () => Click(() => { ArenaConfig.SkillTiers.Value = !ArenaConfig.SkillTiers.Value; }), out _tiersBtn);
            y -= RowH + 6f;
            UiFactory.MakeText(p, "Footer", "Mode, Teams and Allies apply at the next Start Game. Everything saves to the config file.", 17, new Color(1f, 1f, 1f, 0.85f), new Vector2(0f, y), new Vector2(Width - 60f, 50f), TextAnchor.UpperCenter, FontStyle.Normal);
        }

        private void Click(Action change)
        {
            try { change(); Refresh(); }
            catch (Exception e) { Plugin.Log.LogError("ArenaPanel click: " + e); }
        }

        public void Refresh()
        {
            try
            {
                TeamMode mode = ArenaConfig.Mode.Value;
                _modeValue.text = PanelModel.ModeLabel(mode);
                _teamsValue.text = ArenaConfig.TeamCount.Value.ToString();
                _alliesValue.text = PanelModel.AlliesLabel(ArenaConfig.AllySosigs.Value);
                float a = PanelModel.TeamRowsEnabled(mode) ? 1f : 0.4f;
                foreach (var t in new[] { _teamsLabel, _teamsValue, _alliesLabel, _alliesValue }) t.color = new Color(1f, 1f, 1f, a);
                _leaderboardBtn.text = PanelModel.ToggleLabel("Leaderboard", ArenaConfig.LeaderboardEnabled.Value);
                _spreadBtn.text = PanelModel.ToggleLabel("Spread spawns", ArenaConfig.SpreadSpawns.Value);
                _grudgesBtn.text = PanelModel.ToggleLabel("Grudges", ArenaConfig.Grudges.Value);
                _huntersBtn.text = PanelModel.ToggleLabel("Hunters", ArenaConfig.Hunters.Value);
                _hunterShareValue.text = PanelModel.PercentLabel(ArenaConfig.HunterShare.Value);
                _tiersBtn.text = PanelModel.ToggleLabel("Skill tiers", ArenaConfig.SkillTiers.Value);
            }
            catch (Exception e) { Plugin.Log.LogError("ArenaPanel.Refresh: " + e); }
        }
    }
}
```

- [ ] **Step 4: Write PanelInstaller**

`src/GunGameArena/Panel/PanelInstaller.cs`:
```csharp
using System;
using System.Collections;
using GunGame.Scripts.Options;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GunGameArena.Panel
{
    /// <summary>After each scene load, waits for GunGame's settings panel and builds ours beside it.</summary>
    public class PanelInstaller : MonoBehaviour
    {
        private const float GapMetres = 0.12f;
        private const float FallbackWidthMetres = 0.7f;
        private const int MaxPolls = 20;

        private static PanelInstaller _runner;
        private static ArenaPanel _panel;

        public static void Install()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try
            {
                if (_panel != null) { Destroy(_panel.gameObject); }
                _panel = null;   // previous panel died with its scene
                if (_runner == null)
                {
                    var go = new GameObject("GunGameArena_PanelInstaller");
                    DontDestroyOnLoad(go);
                    _runner = go.AddComponent<PanelInstaller>();
                }
                _runner.StopAllCoroutines();
                _runner.StartCoroutine(_runner.WaitAndBuild());
            }
            catch (Exception e) { Plugin.Log.LogError("PanelInstaller.OnSceneLoaded: " + e); }
        }

        private IEnumerator WaitAndBuild()
        {
            for (int i = 0; i < MaxPolls; i++)
            {
                yield return new WaitForSeconds(1f);
                if (TryBuild()) yield break;
            }
            Plugin.Log.LogInfo("No GunGame settings panel found in this scene; Arena panel not shown.");
        }

        private static bool TryBuild()
        {
            try
            {
                if (_panel != null) return true;

                var settings = MonoBehaviourSingleton<GameSettings>.Instance;
                if (settings == null) return false;
                Canvas host = settings.GetComponentInParent<Canvas>();
                if (host == null) return false;

                var hostRt = host.GetComponent<RectTransform>();
                float widthMetres = hostRt != null ? hostRt.rect.width * host.transform.lossyScale.x : 0f;
                bool hostRectDegenerate = hostRt == null || widthMetres <= 0.05f || float.IsNaN(widthMetres);
                if (hostRectDegenerate) widthMetres = FallbackWidthMetres;

                _panel = ArenaPanel.Build();
                Transform t = _panel.transform;
                t.rotation = host.transform.rotation;
                t.localScale = host.transform.lossyScale;
                float ourHalfWidth = ArenaPanel.Width * t.localScale.x * 0.5f;

                if (!hostRectDegenerate)
                {
                    // Pivot-agnostic: measure from the host's actual left edge instead of assuming a centred pivot.
                    Vector3 hostLeftWorld = host.transform.TransformPoint(new Vector3(hostRt.rect.xMin, 0f, 0f));
                    t.position = hostLeftWorld - host.transform.right * (GapMetres + ourHalfWidth);
                }
                else
                {
                    t.position = host.transform.position - host.transform.right * (widthMetres * 0.5f + GapMetres + ourHalfWidth);
                }

                // Align our top edge with the host's top: host pivot is unknown, so match the host's top in its local space.
                // Our own canvas root pivot is its centre, so also drop by half our height to line up the tops.
                if (hostRt != null)
                {
                    float hostTopLocal = hostRt.rect.yMax;
                    Vector3 hostTopWorld = host.transform.TransformPoint(new Vector3(0f, hostTopLocal, 0f));
                    Vector3 delta = Vector3.Project(hostTopWorld - t.position, host.transform.up);
                    t.position += delta - host.transform.up * (ArenaPanel.Height * t.localScale.y * 0.5f);
                }
                Plugin.Log.LogInfo("Arena panel placed beside GunGame's settings panel (host width " + widthMetres.ToString("0.00") + " m).");
                return true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("PanelInstaller.TryBuild: " + e);
                return true;   // don't keep retrying a failing build
            }
        }
    }
}
```
Note: `MonoBehaviourSingleton<T>` is `GunGame.Scripts.MonoBehaviourSingleton<T>`; add `using GunGame.Scripts;`.

- [ ] **Step 5: Wire and document**

`Plugin.cs`: after `Behaviour.SkillApplier.Install();` add `Panel.PanelInstaller.Install();`.

`docs/MORNING-CHECKLIST.md`: add a section before "Things you can tune":
```markdown
## Session 4 — In-map Arena panel

- [ ] On any GunGame map a blue **Arena** panel stands to the left of GunGame's "More options" panel, same height. Log: `Arena panel placed beside GunGame's settings panel`.
- [ ] Laser pointer + trigger works on every button; toggles flip their ON/OFF text; `<`/`>` step values.
- [ ] Toggling **Leaderboard** mid-round hides/shows the HUD immediately.
- [ ] Set Mode to Teams; Teams and Allies rows brighten; Start Game → header "Team Deathmatch".
- [ ] Values persist: quit, relaunch, panel shows what you set (they are in `shaha.GunGameArena.cfg`).
```

- [ ] **Step 6: Build**

Run: `dotnet build GunGameArena.slnx -c Release` — expected clean. `UnityEngine.Events.UnityAction` and `Button.onClick.AddListener` exist in Unity 5.6. If `Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd")` throws at runtime the factory falls back to a null sprite (solid rectangle), which is acceptable.

- [ ] **Step 7: Commit**

```bash
git add src/GunGameArena docs/MORNING-CHECKLIST.md
git commit -m "feat: in-map Arena settings panel beside GunGame's options panel"
```

---

## Self-Review

- Spec coverage: placement (Task 2 installer), look (UiFactory constants), rows (BuildRows in the spec's order incl. both notes), behaviour (config writes, live HUD toggle, dimming, try/catch), out of scope respected.
- Placeholder scan: none.
- Type consistency: `PanelModel` names used in `ArenaPanel` match Task 1 (`CycleMode`, `StepTeamCount`, `StepAllies`, `AlliesLabel`, `StepHunterShare`, `PercentLabel`, `ToggleLabel`, `TeamRowsEnabled`, `ModeLabel`); `LeaderboardHud.SetEnabledLive` defined in Task 2 Step 1 before use in Step 3.
