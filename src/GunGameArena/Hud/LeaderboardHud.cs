using System;
using System.Collections;
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
        private Text _leftScore, _rightScore, _subtitle, _banner;
        private RectTransform _row;
        private LayoutElement _pinGap;
        private readonly List<ContestantCard> _cards = new List<ContestantCard>();

        public static void Install()
        {
            GunGameHooks.RoundStarted += Show;
            GunGameHooks.RoundEnded += Hide;
            Behaviour.TeamMatch.TeamWon += OnTeamWon;
        }

        private static void OnTeamWon(int team)
        {
            try { ShowBanner(HudPalette.TeamName(team) + " TEAM WINS", ContestantCard.ToColor(HudPalette.TeamColor(team)), 5f); }
            catch (Exception e) { Plugin.Log.LogError("LeaderboardHud.OnTeamWon: " + e); }
        }

        /// <summary>Shows the victory banner for a few seconds. No-op if the HUD isn't up.</summary>
        public static void ShowBanner(string text, Color color, float seconds)
        {
            try
            {
                if (_instance == null) return;
                _instance._banner.text = text;
                _instance._banner.color = color;
                _instance._banner.gameObject.SetActive(true);
                _instance.StartCoroutine(_instance.HideBannerAfter(seconds));
            }
            catch (Exception e) { Plugin.Log.LogError("LeaderboardHud.ShowBanner: " + e); }
        }

        private IEnumerator HideBannerAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (_banner != null) _banner.gameObject.SetActive(false);
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
            try
            {
                if (_instance != null) Destroy(_instance.gameObject);
                _instance = null;
            }
            catch (Exception e) { Plugin.Log.LogError("LeaderboardHud.Hide: " + e); }
        }

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

        private static LeaderboardHud Build()
        {
            var go = new GameObject("GunGameArena_HUD", typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1000f, 260f);
            go.transform.localScale = Vector3.one * 0.001f * ArenaConfig.Scale.Value;
            go.AddComponent<HudFollower>();

            var body = FistVR.GM.CurrentPlayerBody;
            if (body != null && body.Head != null)
            {
                float yaw = body.Head.rotation.eulerAngles.y;
                go.transform.position = body.Head.position + Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * ArenaConfig.Distance.Value + Vector3.up * ArenaConfig.Height.Value;
                go.transform.rotation = Quaternion.Euler(-10f, yaw, 0f);
            }

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

            // Team scores (left/right of the header)
            var leftScoreGo = new GameObject("LeftScore", typeof(RectTransform));
            leftScoreGo.transform.SetParent(go.transform, false);
            var leftScoreRt = leftScoreGo.GetComponent<RectTransform>();
            leftScoreRt.anchorMin = new Vector2(0.5f, 1f); leftScoreRt.anchorMax = new Vector2(0.5f, 1f);
            leftScoreRt.sizeDelta = new Vector2(300f, 44f); leftScoreRt.anchoredPosition = new Vector2(-330f, -22f);
            var leftScoreText = leftScoreGo.AddComponent<Text>();
            leftScoreText.font = hud._font; leftScoreText.fontSize = 26; leftScoreText.fontStyle = FontStyle.Bold;
            leftScoreText.alignment = TextAnchor.MiddleRight; leftScoreText.color = Color.white; leftScoreText.raycastTarget = false;
            hud._leftScore = leftScoreText;

            var rightScoreGo = new GameObject("RightScore", typeof(RectTransform));
            rightScoreGo.transform.SetParent(go.transform, false);
            var rightScoreRt = rightScoreGo.GetComponent<RectTransform>();
            rightScoreRt.anchorMin = new Vector2(0.5f, 1f); rightScoreRt.anchorMax = new Vector2(0.5f, 1f);
            rightScoreRt.sizeDelta = new Vector2(300f, 44f); rightScoreRt.anchoredPosition = new Vector2(330f, -22f);
            var rightScoreText = rightScoreGo.AddComponent<Text>();
            rightScoreText.font = hud._font; rightScoreText.fontSize = 26; rightScoreText.fontStyle = FontStyle.Bold;
            rightScoreText.alignment = TextAnchor.MiddleLeft; rightScoreText.color = Color.white; rightScoreText.raycastTarget = false;
            hud._rightScore = rightScoreText;

            // Subtitle
            var subtitleGo = new GameObject("Subtitle", typeof(RectTransform));
            subtitleGo.transform.SetParent(go.transform, false);
            var subtitleRt = subtitleGo.GetComponent<RectTransform>();
            subtitleRt.anchorMin = new Vector2(0.5f, 1f); subtitleRt.anchorMax = new Vector2(0.5f, 1f);
            subtitleRt.sizeDelta = new Vector2(600f, 30f); subtitleRt.anchoredPosition = new Vector2(0f, -60f);
            var subtitleText = subtitleGo.AddComponent<Text>();
            subtitleText.font = hud._font; subtitleText.fontSize = 18; subtitleText.fontStyle = FontStyle.Normal;
            subtitleText.alignment = TextAnchor.MiddleCenter; subtitleText.color = new Color(1f, 1f, 1f, 0.85f); subtitleText.raycastTarget = false;
            hud._subtitle = subtitleText;

            // Victory banner (hidden until ShowBanner is called)
            var bannerGo = new GameObject("Banner", typeof(RectTransform));
            bannerGo.transform.SetParent(go.transform, false);
            var bannerRt = bannerGo.GetComponent<RectTransform>();
            bannerRt.anchorMin = new Vector2(0.5f, 1f); bannerRt.anchorMax = new Vector2(0.5f, 1f);
            bannerRt.sizeDelta = new Vector2(900f, 90f); bannerRt.anchoredPosition = new Vector2(0f, -420f);
            var bannerText = bannerGo.AddComponent<Text>();
            bannerText.font = hud._font; bannerText.fontSize = 64; bannerText.fontStyle = FontStyle.Bold;
            bannerText.alignment = TextAnchor.MiddleCenter; bannerText.color = Color.white; bannerText.raycastTarget = false;
            var bannerOutline = bannerGo.AddComponent<Outline>();
            bannerOutline.effectColor = Color.black; bannerOutline.effectDistance = new Vector2(3f, -3f);
            hud._banner = bannerText;
            bannerGo.SetActive(false);

            // Card row
            var row = new GameObject("Row", typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(go.transform, false);
            row.anchorMin = new Vector2(0.5f, 1f); row.anchorMax = new Vector2(0.5f, 1f);
            row.sizeDelta = new Vector2(1000f, 180f); row.anchoredPosition = new Vector2(0f, -170f);
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

                bool teams = Roster.Mode == TeamMode.Teams;
                _leftScore.gameObject.SetActive(teams);
                _rightScore.gameObject.SetActive(teams);
                _subtitle.gameObject.SetActive(teams);
                if (teams)
                {
                    var all = new List<Contestant>(Roster.AllContestants);

                    _leftScore.text = "BLUE TEAM: " + TeamScore.Total(all, 0);
                    _leftScore.color = ContestantCard.ToColor(HudPalette.TeamColor(0));

                    int teamCount = TeamAssigner.ClampTeamCount(ArenaConfig.TeamCount.Value);
                    string right = "";
                    for (int t = 1; t < teamCount; t++)
                    {
                        if (t > 1) right += " · ";
                        right += HudPalette.TeamName(t) + " TEAM: " + TeamScore.Total(all, t);
                    }
                    _rightScore.text = right;
                    _rightScore.color = ContestantCard.ToColor(HudPalette.TeamColor(1));

                    _subtitle.text = "First team to reach " + ArenaConfig.PointsToWin.Value + " points wins";
                }

                while (_cards.Count < visible.Count) _cards.Add(ContestantCard.Create(_row, _font));

                // First lay out every visible card (pushing each to the end of the row in order),
                // remembering which one (if any) is the pinned player. Only then position the gap
                // relative to that card's final sibling index — doing it inside the loop races the
                // later SetAsLastSibling calls and leaves the gap at the front of the row.
                ContestantCard pinnedCard = null;
                for (int i = 0; i < _cards.Count; i++)
                {
                    bool show = i < visible.Count;
                    _cards[i].gameObject.SetActive(show);
                    if (!show) continue;
                    Contestant c = visible[i];
                    _cards[i].transform.SetAsLastSibling();
                    _cards[i].Bind(c, PortraitFor(c), crowned.Contains(c.Id), c == rankOne, Roster.Mode,
                                   ArenaConfig.ShowNames.Value, ArenaConfig.ShowTierBadge.Value);
                    if (Ranking.IsPinnedPlayer(visible, ArenaConfig.TopCount.Value, c)) pinnedCard = _cards[i];
                }

                // Pass 2: gap then pinned card moved to the end in that order → [..., lastTopCard, gap, pinnedCard]
                _pinGap.gameObject.SetActive(pinnedCard != null);
                if (pinnedCard != null)
                {
                    _pinGap.transform.SetAsLastSibling();
                    pinnedCard.transform.SetAsLastSibling();
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
