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
                if (Roster.Mode != TeamMode.Teams) return;
                if (_instance == null) _instance = new GameObject("GunGameArena_TeamTags").AddComponent<TeamTags>();
                int living = 0;
                foreach (var slot in Roster.LivingSosigSlots())
                {
                    living++;
                    _instance.EnsureTag(slot);
                }
                Plugin.Log.LogInfo("TeamTags.OnRoundStarted: " + living + " living slots, " + _instance._tags.Count + " tags created");
            }
            catch (Exception e) { Plugin.Log.LogError("TeamTags.OnRoundStarted: " + e); }
        }

        private static void OnRoundEnded()
        {
            try
            {
                if (_instance != null)
                {
                    // Belt and braces: tags are parented under the instance so destroying it should
                    // take them with it, but explicitly destroy each tag's canvas first in case a
                    // tag was ever left unparented (e.g. by future code that changes EnsureTag).
                    foreach (var text in _instance._tags.Values)
                        if (text != null && text.canvas != null) Destroy(text.canvas.gameObject);
                    _instance._tags.Clear();
                    _instance._tagRoots.Clear();
                    Destroy(_instance.gameObject);
                }
                _instance = null;
            }
            catch (Exception e) { Plugin.Log.LogError("TeamTags.OnRoundEnded: " + e); }
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

            Vector3 anchorPos = (slot.Sosig.Links != null && slot.Sosig.Links.Count > 0 && slot.Sosig.Links[0] != null)
                ? slot.Sosig.Links[0].transform.position
                : slot.Sosig.transform.position;
            Plugin.Log.LogInfo("TeamTags: tag created for " + slot.Contestant.Name + " at " + anchorPos);
        }

        private void LateUpdate()
        {
            try
            {
                var body = GM.CurrentPlayerBody;
                if (body == null || body.Head == null) return;
                Vector3 head = body.Head.position;
                float range = ArenaConfig.TagRange.Value;
                var dead = new List<Slot>();
                foreach (var kv in _tags)
                {
                    Slot slot = kv.Key; Text text = kv.Value;
                    try
                    {
                        if (text == null || text.canvas == null) { dead.Add(slot); continue; }
                        GameObject root;
                        if (!_tagRoots.TryGetValue(slot, out root) || root == null) { dead.Add(slot); continue; }
                        if (slot.IsVacant || !slot.Contestant.IsAlive) { Destroy(root); dead.Add(slot); continue; }
                        Transform anchor = (slot.Sosig.Links != null && slot.Sosig.Links.Count > 0 && slot.Sosig.Links[0] != null) ? slot.Sosig.Links[0].transform : slot.Sosig.transform;
                        Transform tag = root.transform;
                        tag.position = anchor.position + Vector3.up * HeightAboveHead;
                        Vector3 toHead = head - tag.position; toHead.y = 0f;
                        if (toHead.sqrMagnitude > 0.0001f) tag.rotation = Quaternion.LookRotation(-toHead.normalized, Vector3.up);
                        bool visible = Vector3.Distance(head, tag.position) <= range;
                        if (root.activeSelf != visible) root.SetActive(visible);
                    }
                    catch (Exception e) { Plugin.Log.LogError("TeamTags.LateUpdate (tag " + slot.Contestant.Name + "): " + e); }
                }
                for (int i = 0; i < dead.Count; i++) { _tags.Remove(dead[i]); _tagRoots.Remove(dead[i]); }

                if (Time.time >= _nextDiag)
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
                                Plugin.Log.LogInfo("TeamTags: " + _tags.Count + " tags; first: " + name + " pos=" + firstRoot.transform.position + " active=" + firstRoot.activeSelf + " dist=" + dist.ToString("0.0") + " facingDot=" + facingDot.ToString("0.00"));
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
