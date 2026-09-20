using System;
using System.Collections;
using System.Text;
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
        private const float NearestCanvasMaxDistance = 3f;
        private const float FallbackNoRendererOffset = 0.7f;
        private const float FallbackNoRendererHeight = 0.35f;
        private const float FallbackScale = 0.01f;
        private const int MaxPolls = 20;
        private const int MaxCanvasDumpCount = 15;
        private const int MaxDumpLineLength = 200;

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
            LogPollTimeout();
        }

        /// <summary>Logs why the 20 s poll gave up. Kept out of WaitAndBuild so no try/catch wraps a yield.</summary>
        private static void LogPollTimeout()
        {
            try
            {
                if (MonoBehaviourSingleton<GameSettings>.Instance == null)
                {
                    Plugin.Log.LogInfo("GameSettings.Instance never appeared in 20 s; Arena panel not shown.");
                }
                // else: TryBuild always builds exactly once GameSettings.Instance exists, so if we
                // ever get here with a non-null Instance, TryBuild's own catch already logged why.
            }
            catch (Exception e) { Plugin.Log.LogError("PanelInstaller.LogPollTimeout: " + e); }
        }

        /// <summary>Returns false only while GameSettings.Instance has not appeared yet. Once it exists we
        /// always attempt exactly one build (via some anchor strategy) and never retry after that.</summary>
        private static bool TryBuild()
        {
            try
            {
                if (_panel != null) return true;

                var settings = MonoBehaviourSingleton<GameSettings>.Instance;
                if (settings == null) return false;

                BuildPanel(settings);
                DumpHierarchy(settings);
                return true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("PanelInstaller.TryBuild: " + e);
                return true;   // don't keep retrying a failing build
            }
        }

        private static void BuildPanel(GameSettings settings)
        {
            try
            {
                Transform anchor = settings.transform;
                string strategy;
                Canvas canvas = settings.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    strategy = "canvas-parent";
                }
                else
                {
                    canvas = settings.GetComponentInChildren<Canvas>(true);
                    if (canvas != null)
                    {
                        strategy = "canvas-child";
                    }
                    else
                    {
                        canvas = NearestCanvas(anchor.position, NearestCanvasMaxDistance);
                        strategy = canvas != null ? "canvas-nearest" : null;
                    }
                }

                if (canvas != null)
                {
                    PlaceUsingCanvas(canvas);
                }
                else if (PlaceUsingRendererBounds(anchor))
                {
                    strategy = "renderer-bounds";
                }
                else
                {
                    PlaceUsingFallbackOffset(anchor);
                    strategy = "fallback-offset";
                }

                Plugin.Log.LogInfo("Arena panel placed via " + strategy + ".");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("PanelInstaller.BuildPanel: " + e);
                throw;
            }
        }

        /// <summary>Nearest Canvas to a world position within maxDistance metres, or null if none is close enough.</summary>
        private static Canvas NearestCanvas(Vector3 position, float maxDistance)
        {
            try
            {
                Canvas[] canvases = UnityEngine.Object.FindObjectsOfType<Canvas>();
                Canvas nearest = null;
                float nearestDist = maxDistance;
                for (int i = 0; i < canvases.Length; i++)
                {
                    Canvas c = canvases[i];
                    if (c == null) continue;
                    float dist = Vector3.Distance(c.transform.position, position);
                    if (dist <= nearestDist)
                    {
                        nearest = c;
                        nearestDist = dist;
                    }
                }
                return nearest;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("PanelInstaller.NearestCanvas: " + e);
                return null;
            }
        }

        /// <summary>Existing, reviewed canvas-based placement: left edge via rect.xMin, top via rect.yMax,
        /// using the host canvas's own rotation and lossyScale.</summary>
        private static void PlaceUsingCanvas(Canvas host)
        {
            try
            {
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
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("PanelInstaller.PlaceUsingCanvas: " + e);
                throw;
            }
        }

        /// <summary>No Canvas anywhere near the anchor: build a world Bounds from every Renderer under it and
        /// place our panel off its left edge, top-aligned. Returns false (and builds nothing) if there are no
        /// renderers to measure.</summary>
        private static bool PlaceUsingRendererBounds(Transform anchor)
        {
            try
            {
                Renderer[] renderers = anchor.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) return false;

                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }

                Vector3 c = bounds.center;
                Vector3 e = bounds.extents;
                var corners = new Vector3[8];
                corners[0] = c + new Vector3(e.x, e.y, e.z);
                corners[1] = c + new Vector3(e.x, e.y, -e.z);
                corners[2] = c + new Vector3(e.x, -e.y, e.z);
                corners[3] = c + new Vector3(e.x, -e.y, -e.z);
                corners[4] = c + new Vector3(-e.x, e.y, e.z);
                corners[5] = c + new Vector3(-e.x, e.y, -e.z);
                corners[6] = c + new Vector3(-e.x, -e.y, e.z);
                corners[7] = c + new Vector3(-e.x, -e.y, -e.z);

                float minX = float.MaxValue;
                float maxY = float.MinValue;
                for (int i = 0; i < corners.Length; i++)
                {
                    Vector3 local = anchor.InverseTransformPoint(corners[i]);
                    if (local.x < minX) minX = local.x;
                    if (local.y > maxY) maxY = local.y;
                }

                _panel = ArenaPanel.Build();
                Transform t = _panel.transform;
                t.rotation = anchor.rotation;
                t.localScale = Vector3.one * FallbackScale;
                float ourHalfWidth = ArenaPanel.Width * t.localScale.x * 0.5f;

                Vector3 leftEdgeWorld = anchor.TransformPoint(new Vector3(minX, 0f, 0f));
                Vector3 topWorld = anchor.TransformPoint(new Vector3(0f, maxY, 0f));

                t.position = leftEdgeWorld - anchor.right * (GapMetres + ourHalfWidth);

                Vector3 delta = Vector3.Project(topWorld - t.position, anchor.up);
                t.position += delta - anchor.up * (ArenaPanel.Height * t.localScale.y * 0.5f);

                return true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("PanelInstaller.PlaceUsingRendererBounds: " + e);
                throw;
            }
        }

        /// <summary>Last resort: no canvas, no renderers to measure. Just sit a fixed offset to the anchor's
        /// left, roughly chest height above it.</summary>
        private static void PlaceUsingFallbackOffset(Transform anchor)
        {
            try
            {
                _panel = ArenaPanel.Build();
                Transform t = _panel.transform;
                t.rotation = anchor.rotation;
                t.localScale = Vector3.one * FallbackScale;
                float ourHalfWidth = ArenaPanel.Width * t.localScale.x * 0.5f;

                Vector3 position = anchor.position - anchor.right * (FallbackNoRendererOffset + GapMetres + ourHalfWidth);
                position += anchor.up * FallbackNoRendererHeight;
                t.position = position;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("PanelInstaller.PlaceUsingFallbackOffset: " + e);
                throw;
            }
        }

        /// <summary>Diagnostic dump of the real GunGame panel hierarchy, so the next iteration can be exact
        /// about where the panel actually lives on maps where our anchor guesses are wrong. Every line is
        /// prefixed "[Panel dump] " per the morning checklist so it's easy to grep out of a pasted log.</summary>
        private static void DumpHierarchy(GameSettings settings)
        {
            try
            {
                Plugin.Log.LogInfo("[Panel dump] Path from settings.transform to root:");
                Transform current = settings.transform;
                while (current != null)
                {
                    Plugin.Log.LogInfo("[Panel dump]   " + DescribeTransform(current));
                    current = current.parent;
                }

                Plugin.Log.LogInfo("[Panel dump] Children of settings.transform:");
                Transform parent = settings.transform;
                for (int i = 0; i < parent.childCount; i++)
                {
                    Plugin.Log.LogInfo("[Panel dump]   " + DescribeChild(parent.GetChild(i)));
                }

                Plugin.Log.LogInfo("[Panel dump] Canvases in scene (capped at " + MaxCanvasDumpCount + "):");
                Canvas[] canvases = UnityEngine.Object.FindObjectsOfType<Canvas>();
                int dumped = 0;
                for (int i = 0; i < canvases.Length && dumped < MaxCanvasDumpCount; i++)
                {
                    Canvas c = canvases[i];
                    if (c == null) continue;
                    Plugin.Log.LogInfo("[Panel dump]   " + DescribeCanvas(c, settings.transform.position));
                    dumped++;
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("PanelInstaller.DumpHierarchy: " + e);
            }
        }

        private static string DescribeTransform(Transform t)
        {
            try
            {
                string compNames = JoinComponentNames(t);
                RectTransform rt = t as RectTransform;
                string rectStr = rt != null
                    ? rt.rect.width.ToString("0.0") + "x" + rt.rect.height.ToString("0.0")
                    : "n/a";
                Vector3 s = t.lossyScale;
                string scaleStr = s.x.ToString("0.###") + "," + s.y.ToString("0.###") + "," + s.z.ToString("0.###");

                string line = t.name + " [components: " + compNames + "] rect=(" + rectStr + ") lossyScale=(" + scaleStr + ")";
                return Truncate(line);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("PanelInstaller.DescribeTransform: " + e);
                return "<describe error>";
            }
        }

        private static string DescribeChild(Transform t)
        {
            try
            {
                string line = t.name + " [" + JoinComponentNames(t) + "]";
                return Truncate(line);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("PanelInstaller.DescribeChild: " + e);
                return "<describe error>";
            }
        }

        private static string DescribeCanvas(Canvas c, Vector3 referencePosition)
        {
            try
            {
                RectTransform rt = c.GetComponent<RectTransform>();
                string rectStr = rt != null
                    ? rt.rect.width.ToString("0.0") + "x" + rt.rect.height.ToString("0.0")
                    : "n/a";
                float dist = Vector3.Distance(c.transform.position, referencePosition);
                string line = c.name + " | " + GetPath(c.transform) + " | " + c.renderMode +
                    " | rect " + rectStr + " | lossyScale.x " + c.transform.lossyScale.x.ToString("0.###") +
                    " | dist " + dist.ToString("0.00") + "m";
                return Truncate(line);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("PanelInstaller.DescribeCanvas: " + e);
                return "<describe error>";
            }
        }

        private static string JoinComponentNames(Transform t)
        {
            try
            {
                Component[] components = t.GetComponents<Component>();
                var sb = new StringBuilder();
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null) continue;
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append(components[i].GetType().Name);
                }
                return sb.ToString();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("PanelInstaller.JoinComponentNames: " + e);
                return "?";
            }
        }

        private static string GetPath(Transform t)
        {
            try
            {
                var sb = new StringBuilder(t.name);
                Transform p = t.parent;
                while (p != null)
                {
                    sb.Insert(0, p.name + "/");
                    p = p.parent;
                }
                return sb.ToString();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("PanelInstaller.GetPath: " + e);
                return "?";
            }
        }

        private static string Truncate(string line)
        {
            return line.Length > MaxDumpLineLength ? line.Substring(0, MaxDumpLineLength) : line;
        }
    }
}
