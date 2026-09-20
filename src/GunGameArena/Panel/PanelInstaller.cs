using System;
using System.Collections;
using GunGame.Scripts;
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
                var settings = MonoBehaviourSingleton<GameSettings>.Instance;
                if (settings == null) return false;
                Canvas host = settings.GetComponentInParent<Canvas>();
                if (host == null) return false;

                var hostRt = host.GetComponent<RectTransform>();
                float widthMetres = hostRt != null ? hostRt.rect.width * host.transform.lossyScale.x : 0f;
                if (widthMetres <= 0.05f || float.IsNaN(widthMetres)) widthMetres = FallbackWidthMetres;

                _panel = ArenaPanel.Build(null);
                Transform t = _panel.transform;
                t.rotation = host.transform.rotation;
                t.localScale = host.transform.lossyScale;
                float ourHalfWidth = ArenaPanel.Width * t.localScale.x * 0.5f;
                t.position = host.transform.position - host.transform.right * (widthMetres * 0.5f + GapMetres + ourHalfWidth);
                // Align our top edge with the host's top: host pivot is unknown, so match the host's top in its local space.
                if (hostRt != null)
                {
                    float hostTopLocal = hostRt.rect.yMax;
                    Vector3 hostTopWorld = host.transform.TransformPoint(new Vector3(0f, hostTopLocal, 0f));
                    Vector3 delta = Vector3.Project(hostTopWorld - t.position, host.transform.up);
                    t.position += delta;
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
