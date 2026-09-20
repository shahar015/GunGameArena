using UnityEngine;

namespace GunGameArena.Portraits
{
    /// <summary>Procedural sprites so the plugin ships as a single DLL with no asset bundle.</summary>
    public static class Sprites
    {
        private static Sprite _crown, _fallback, _solid;

        public static Sprite Solid { get { if (_solid == null) _solid = MakeSolid(); return _solid; } }
        public static Sprite Crown { get { if (_crown == null) _crown = MakeCrown(); return _crown; } }
        public static Sprite FallbackAvatar { get { if (_fallback == null) _fallback = MakeFallbackAvatar(); return _fallback; } }

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
