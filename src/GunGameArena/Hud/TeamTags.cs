using System;
using System.Collections.Generic;
using FistVR;
using GunGameArena.Core;
using GunGameArena.Patches;
using UnityEngine;
using UnityEngine.UI;

namespace GunGameArena.Hud
{
    /// <summary>Blue dot + name floating above each teammate in Team Deathmatch.</summary>
    public class TeamTags : MonoBehaviour
    {
        private const float HeightAboveHead = 0.35f;
        private const float TagScale = 0.003f;
        private const float DiagIntervalSeconds = 10f;
        private static TeamTags _instance;
        private readonly Dictionary<Slot, Text> _tags = new Dictionary<Slot, Text>();
        private readonly Dictionary<Slot, GameObject> _tagRoots = new Dictionary<Slot, GameObject>();
        private readonly Dictionary<Slot, Canvas> _tagCanvases = new Dictionary<Slot, Canvas>();
        private readonly List<Slot> _dead = new List<Slot>();
        private Font _font;
        private float _nextDiag;

        public static void Install()
        {
            GunGameHooks.RoundStarted += OnRoundStarted;
            GunGameHooks.RoundEnded += OnRoundEnded;
            SpawnerPatches.SosigBound += OnSosigBound;
        }

        private static void OnRoundStarted()
        {
            try
            {
                if (Roster.Mode != TeamMode.Teams) { DestroyInstance(); return; }
                if (_instance == null) _instance = new GameObject("GunGameArena_TeamTags").AddComponent<TeamTags>();
                int living = 0;
                foreach (var slot in Roster.LivingSosigSlots())
                {
                    living++;
                    _instance.EnsureTag(slot);
                }
                if (ArenaConfig.DebugLogging.Value)
                    Plugin.Log.LogInfo("TeamTags.OnRoundStarted: " + living + " living slots, " + _instance._tags.Count + " tags created");
            }
            catch (Exception e) { Plugin.Log.LogError("TeamTags.OnRoundStarted: " + e); }
        }

        private static void OnRoundEnded()
        {
            try { DestroyInstance(); }
            catch (Exception e) { Plugin.Log.LogError("TeamTags.OnRoundEnded: " + e); }
        }

        /// <summary>Tears down the singleton instance and every tag it owns. Shared by OnRoundEnded
        /// (round over) and OnRoundStarted (a round starting in a non-Teams mode) so neither path can
        /// leave a stale instance or tags behind.</summary>
        private static void DestroyInstance()
        {
            if (_instance == null) return;
            // Belt and braces: tags are parented under the instance so destroying it should
            // take them with it, but explicitly destroy each tag's canvas first in case a
            // tag was ever left unparented (e.g. by future code that changes EnsureTag).
            foreach (var text in _instance._tags.Values)
                if (text != null && text.canvas != null) Destroy(text.canvas.gameObject);
            _instance._tags.Clear();
            _instance._tagRoots.Clear();
            _instance._tagCanvases.Clear();
            Destroy(_instance.gameObject);
            _instance = null;
        }

        private static void OnSosigBound(Slot slot)
        {
            try { if (_instance != null && Roster.Mode == TeamMode.Teams) _instance.EnsureTag(slot); }
            catch (Exception e) { Plugin.Log.LogError("TeamTags.OnSosigBound: " + e); }
        }

        private void EnsureTag(Slot slot)
        {
            if (slot == null || slot.Contestant.TeamIndex != 0 || slot.Sosig == null) return;
            Text existing;
            if (_tags.TryGetValue(slot, out existing) && existing != null) return;
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var go = new GameObject("TeamTag_" + slot.Contestant.Name, typeof(RectTransform));
            go.transform.SetParent(transform, true); // worldPositionStays: LateUpdate re-positions it in world space anyway
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(700f, 100f);
            go.transform.localScale = Vector3.one * TagScale;

            // Background, created before the Text so the label renders on top of it.
            var bg = new GameObject("Bg", typeof(RectTransform));
            bg.transform.SetParent(go.transform, false);
            var bgRt = (RectTransform)bg.transform;
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.sprite = GunGameArena.Portraits.Sprites.Solid;
            bgImg.color = new Color(0f, 0f, 0f, 0.55f);
            bgImg.raycastTarget = false;

            var text = new GameObject("Text", typeof(RectTransform)).AddComponent<Text>();
            text.transform.SetParent(go.transform, false);
            var rt = text.rectTransform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            text.font = _font; text.fontSize = 48; text.fontStyle = FontStyle.Bold; text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white; text.supportRichText = true; text.raycastTarget = false;
            Color blue = ContestantCard.ToColor(HudPalette.TeamColor(0));
            string hex = ColorUtility.ToHtmlStringRGB(blue);
            text.text = "<color=#" + hex + ">●</color> " + slot.Contestant.Name;
            var outline = text.gameObject.AddComponent<Outline>(); outline.effectColor = Color.black; outline.effectDistance = new Vector2(2f, -2f);
            _tags[slot] = text;
            _tagRoots[slot] = go;
            _tagCanvases[slot] = canvas;

            if (ArenaConfig.DebugLogging.Value)
            {
                Vector3 anchorPos = (slot.Sosig.Links != null && slot.Sosig.Links.Count > 0 && slot.Sosig.Links[0] != null)
                    ? slot.Sosig.Links[0].transform.position
                    : slot.Sosig.transform.position;
                Plugin.Log.LogInfo("TeamTags: tag created for " + slot.Contestant.Name + " at " + anchorPos);
            }
        }

        private void LateUpdate()
        {
            try
            {
                var body = GM.CurrentPlayerBody;
                if (body == null || body.Head == null) return;
                Vector3 head = body.Head.position;
                float range = ArenaConfig.TagRange.Value;
                _dead.Clear();
                foreach (var kv in _tags)
                {
                    Slot slot = kv.Key; Text text = kv.Value;
                    try
                    {
                        GameObject root;
                        bool hasRoot = _tagRoots.TryGetValue(slot, out root);
                        // Do NOT test text.canvas here: a tag hidden via canvas.enabled = false still
                        // reports text.canvas == null on some Unity versions once disabled, which used
                        // to make LateUpdate mistake a merely-hidden tag for a dead one and destroy it.
                        if (text == null || !hasRoot || root == null) { _dead.Add(slot); continue; }
                        if (slot.IsVacant || !slot.Contestant.IsAlive) { Destroy(root); _dead.Add(slot); continue; }
                        Transform anchor = (slot.Sosig.Links != null && slot.Sosig.Links.Count > 0 && slot.Sosig.Links[0] != null) ? slot.Sosig.Links[0].transform : slot.Sosig.transform;
                        Transform tag = root.transform;
                        tag.position = anchor.position + Vector3.up * HeightAboveHead;
                        Vector3 toHead = head - tag.position; toHead.y = 0f;
                        if (toHead.sqrMagnitude > 0.0001f) tag.rotation = Quaternion.LookRotation(-toHead.normalized, Vector3.up);
                        // Keep the root active at all times; toggle the canvas instead. An inactive
                        // hierarchy makes Text.canvas return null, which used to trip the dead-tag test
                        // above and get the tag destroyed the very next frame it went out of range.
                        bool visible = range <= 0f || Vector3.Distance(head, tag.position) <= range;
                        Canvas canvas;
                        if (_tagCanvases.TryGetValue(slot, out canvas) && canvas != null)
                        {
                            if (canvas.enabled != visible) canvas.enabled = visible;
                        }
                        // else: no canvas reference on record — leave the tag visible rather than risk
                        // hiding it with no way to bring it back.
                    }
                    catch (Exception e)
                    {
                        string who = slot != null && slot.Contestant != null ? slot.Contestant.Name : "?";
                        Plugin.Log.LogError("TeamTags.LateUpdate (tag " + who + "): " + e);
                    }
                }
                for (int i = 0; i < _dead.Count; i++) { _tags.Remove(_dead[i]); _tagRoots.Remove(_dead[i]); _tagCanvases.Remove(_dead[i]); }

                if (ArenaConfig.DebugLogging.Value && Time.time >= _nextDiag)
                {
                    _nextDiag = Time.time + DiagIntervalSeconds;
                    try
                    {
                        if (_tags.Count == 0)
                        {
                            int livingCount = 0; int team0Count = 0;
                            foreach (var s in Roster.LivingSosigSlots())
                            {
                                livingCount++;
                                if (s.Contestant.TeamIndex == 0) team0Count++;
                            }
                            Plugin.Log.LogInfo("TeamTags: 0 tags (mode=" + Roster.Mode + ", living slots=" + livingCount + ", team0 slots=" + team0Count + ")");
                        }
                        else
                        {
                            Slot firstSlot = null; GameObject firstRoot = null;
                            foreach (var kv in _tagRoots) { firstSlot = kv.Key; firstRoot = kv.Value; break; }
                            if (firstRoot != null && firstSlot != null)
                            {
                                string name = firstSlot.Contestant.Name;
                                float dist = Vector3.Distance(head, firstRoot.transform.position);
                                float facingDot = Vector3.Dot(firstRoot.transform.forward, (firstRoot.transform.position - head).normalized);
                                Canvas diagCanvas;
                                bool visible = !_tagCanvases.TryGetValue(firstSlot, out diagCanvas) || diagCanvas == null || diagCanvas.enabled;
                                Plugin.Log.LogInfo("TeamTags: " + _tags.Count + " tags; first: " + name + " pos=" + firstRoot.transform.position + " visible=" + visible + " dist=" + dist.ToString("0.0") + " facingDot=" + facingDot.ToString("0.00"));
                            }
                        }
                    }
                    catch (Exception e) { Plugin.Log.LogError("TeamTags.LateUpdate (diag): " + e); }
                }
            }
            catch (Exception e) { Plugin.Log.LogError("TeamTags.LateUpdate: " + e); }
        }
    }
}
