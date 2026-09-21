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
        private static bool _spriteTried;
        private static bool _usingFallbackSprite;

        public static Font Font
        {
            get { if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); return _font; }
        }

        public static Sprite UiSprite
        {
            get
            {
                if (!_spriteTried)
                {
                    // Unity 5.6's Resources has no GetBuiltinExtraResource; GetBuiltinResource is the only lookup available here.
                    try { _uiSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd"); } catch { _uiSprite = null; }
                    if (_uiSprite == null)
                    {
                        _uiSprite = GunGameArena.Portraits.Sprites.Solid;
                        _usingFallbackSprite = true;
                    }
                    _spriteTried = true;
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
            img.sprite = UiSprite; // accessing the property resolves _usingFallbackSprite before we read it below
            img.type = _usingFallbackSprite ? Image.Type.Simple : Image.Type.Sliced;
            img.color = Color.white;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
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
