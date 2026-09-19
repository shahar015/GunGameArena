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
