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
            try
            {
                if (Roster.Player == null) return;
                Ensure().StartCoroutine(SteamAvatar.Load(sprite =>
                {
                    if (Roster.Player == null) return;
                    if (Roster.Player.Portrait != null && Roster.Player.Portrait != Sprites.FallbackAvatar && Roster.Player.Portrait.texture != null)
                        Destroy(Roster.Player.Portrait.texture);
                    Roster.Player.Portrait = sprite;
                    Roster.RaiseChanged();
                }));
            }
            catch (Exception e) { Plugin.Log.LogError("PortraitRenderer.OnRoundStarted: " + e); }
        }

        public static void Capture(Slot slot)
        {
            try { var r = Ensure(); r.StartCoroutine(r.CaptureRoutine(slot)); }
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
