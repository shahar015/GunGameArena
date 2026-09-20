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
            try
            {
                if (_instance != null) Destroy(_instance.gameObject);
                _instance = null;
            }
            catch (Exception e) { Plugin.Log.LogError("LeaderboardHud.Hide: " + e); }
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
