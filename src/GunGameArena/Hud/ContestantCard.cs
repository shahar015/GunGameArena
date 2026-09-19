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
