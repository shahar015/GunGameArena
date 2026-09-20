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
        public const float Height = 820f;
        private const float RowH = 52f;
        private const float FirstRowY = -110f;
        private const float BtnH = 34f;

        private Text _modeValue, _teamsValue, _alliesValue, _hunterShareValue, _pointsValue;
        private Text _teamsLabel, _alliesLabel, _pointsLabel;
        private Text _leaderboardBtn, _spreadBtn, _grudgesBtn, _huntersBtn, _tiersBtn, _friendlyFireBtn;

        public static ArenaPanel Build()
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
            try { panel.BuildRows(); }
            catch (Exception e) { Plugin.Log.LogError("ArenaPanel.BuildRows: " + e); }
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
            y -= RowH;
            // Points to win / Friendly fire (Teams only)
            _pointsLabel = UiFactory.MakeText(p, "PointsLabel", "Points to win", 26, Color.white, new Vector2(-200f, y), new Vector2(180f, RowH), TextAnchor.MiddleLeft, FontStyle.Normal);
            UiFactory.MakeButton(p, "PointsPrev", "<", new Vector2(-60f, y), new Vector2(50f, BtnH), () => Click(() => { ArenaConfig.PointsToWin.Value = PanelModel.StepPointsToWin(ArenaConfig.PointsToWin.Value, -1); }), out dummy);
            _pointsValue = UiFactory.MakeText(p, "PointsValue", "", 26, Color.white, new Vector2(95f, y), new Vector2(230f, RowH), TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.MakeButton(p, "PointsNext", ">", new Vector2(250f, y), new Vector2(50f, BtnH), () => Click(() => { ArenaConfig.PointsToWin.Value = PanelModel.StepPointsToWin(ArenaConfig.PointsToWin.Value, +1); }), out dummy);
            y -= RowH;
            UiFactory.MakeButton(p, "FriendlyFireToggle", "", new Vector2(0f, y), new Vector2(420f, BtnH), () => Click(() => { ArenaConfig.FriendlyFire.Value = !ArenaConfig.FriendlyFire.Value; }), out _friendlyFireBtn);
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
            y -= 38f;
            UiFactory.MakeText(p, "GrudgesNote", "(each sosig fights 3 rivals at a time, not everyone)", 17, new Color(1f, 1f, 1f, 0.85f), new Vector2(0f, y), new Vector2(Width - 60f, 40f), TextAnchor.UpperCenter, FontStyle.Italic);
            y -= 40f;
            UiFactory.MakeButton(p, "HuntersToggle", "", new Vector2(0f, y), new Vector2(420f, BtnH), () => Click(() => { ArenaConfig.Hunters.Value = !ArenaConfig.Hunters.Value; }), out _huntersBtn);
            y -= 38f;
            UiFactory.MakeText(p, "HuntersNote", "(every 10–20 s a few sosigs are sent toward you)", 17, new Color(1f, 1f, 1f, 0.85f), new Vector2(0f, y), new Vector2(Width - 60f, 40f), TextAnchor.UpperCenter, FontStyle.Italic);
            y -= 44f;
            // Hunter pressure
            UiFactory.MakeText(p, "HunterShareLabel", "Pressure", 26, Color.white, new Vector2(-200f, y), new Vector2(180f, RowH), TextAnchor.MiddleLeft, FontStyle.Normal);
            UiFactory.MakeButton(p, "HunterSharePrev", "<", new Vector2(-60f, y), new Vector2(50f, BtnH), () => Click(() => { ArenaConfig.HunterShare.Value = PanelModel.StepHunterShare(ArenaConfig.HunterShare.Value, -1); }), out dummy);
            _hunterShareValue = UiFactory.MakeText(p, "HunterShareValue", "", 26, Color.white, new Vector2(95f, y), new Vector2(230f, RowH), TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.MakeButton(p, "HunterShareNext", ">", new Vector2(250f, y), new Vector2(50f, BtnH), () => Click(() => { ArenaConfig.HunterShare.Value = PanelModel.StepHunterShare(ArenaConfig.HunterShare.Value, +1); }), out dummy);
            y -= RowH;
            UiFactory.MakeButton(p, "TiersToggle", "", new Vector2(0f, y), new Vector2(420f, BtnH), () => Click(() => { ArenaConfig.SkillTiers.Value = !ArenaConfig.SkillTiers.Value; }), out _tiersBtn);
            y -= 38f;
            UiFactory.MakeText(p, "TiersNote", "(each sosig gets an aim level: Rookie ^ … Elite ^^^^)", 17, new Color(1f, 1f, 1f, 0.85f), new Vector2(0f, y), new Vector2(Width - 60f, 40f), TextAnchor.UpperCenter, FontStyle.Italic);
            y -= 40f;
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
                _pointsValue.text = ArenaConfig.PointsToWin.Value.ToString();
                _friendlyFireBtn.text = PanelModel.ToggleLabel("Friendly fire", ArenaConfig.FriendlyFire.Value);
                float a = PanelModel.TeamRowsEnabled(mode) ? 1f : 0.4f;
                foreach (var t in new[] { _teamsLabel, _teamsValue, _alliesLabel, _alliesValue, _pointsLabel, _pointsValue }) t.color = new Color(1f, 1f, 1f, a);
                // _friendlyFireBtn's label sits on a white button background (UiFactory.ButtonText is
                // dark grey); dimming it toward white like the rows above would make it invisible.
                // Dim via alpha only, keeping the dark text colour.
                _friendlyFireBtn.color = new Color(UiFactory.ButtonText.r, UiFactory.ButtonText.g, UiFactory.ButtonText.b, a);
                _leaderboardBtn.text = PanelModel.ToggleLabel("Leaderboard", ArenaConfig.LeaderboardEnabled.Value);
                _spreadBtn.text = PanelModel.ToggleLabel("Spread spawns", ArenaConfig.SpreadSpawns.Value);
                _grudgesBtn.text = PanelModel.ToggleLabel("Grudges", ArenaConfig.Grudges.Value);
                _huntersBtn.text = PanelModel.ToggleLabel("Hunters", ArenaConfig.Hunters.Value);
                _hunterShareValue.text = PanelModel.PercentLabel(ArenaConfig.HunterShare.Value);
                _tiersBtn.text = PanelModel.ToggleLabel("Skill tiers", ArenaConfig.SkillTiers.Value);
            }
            catch (Exception e) { Plugin.Log.LogError("ArenaPanel.Refresh: " + e); }

            try { Behaviour.TeamMatch.ApplyWeaponCountLock(); }
            catch (Exception e) { Plugin.Log.LogError("ArenaPanel.Refresh (weapon-count lock): " + e); }
        }
    }
}
