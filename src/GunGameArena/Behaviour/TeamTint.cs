using System;
using FistVR;
using GunGameArena.Core;
using GunGameArena.Hud;
using GunGameArena.Patches;
using UnityEngine;

namespace GunGameArena.Behaviour
{
    /// <summary>Marks a sosig GameObject as already tinted, so re-binding the same sosig
    /// (or a stray duplicate SosigBound event) never re-tints materials that already were.</summary>
    public class TeamTintMarker : MonoBehaviour { }

    /// <summary>Tints teammates' bodies blue in Team Deathmatch so you can tell them apart
    /// from the enemy team at a glance (spec: second visual cue alongside the name tags).</summary>
    public static class TeamTint
    {
        private const float TintStrength = 0.65f;
        private static bool _warnedNoColorProperty;

        public static void Install()
        {
            SpawnerPatches.SosigBound += OnSosigBound;
        }

        private static void OnSosigBound(Slot slot)
        {
            try
            {
                if (slot == null || Roster.Mode != TeamMode.Teams || !ArenaConfig.TeamTint.Value) return;
                if (slot.Contestant == null || slot.Contestant.TeamIndex != 0) return;
                if (slot.Sosig == null) return;
                if (slot.Sosig.GetComponent<TeamTintMarker>() != null) return;
                slot.Sosig.gameObject.AddComponent<TeamTintMarker>();

                Renderer[] renderers = slot.Sosig.Renderers;
                if (renderers == null || renderers.Length == 0)
                    renderers = slot.Sosig.GetComponentsInChildren<Renderer>(true);
                if (renderers == null || renderers.Length == 0)
                {
                    Plugin.Log.LogWarning("TeamTint: no renderers found on " + slot.Contestant.Name + "; tint skipped.");
                    return;
                }

                Color teamBlue = ContestantCard.ToColor(HudPalette.TeamColor(0));
                int tinted = 0;
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer r = renderers[i];
                    if (r == null) continue;
                    // MaterialPropertyBlock: tints the renderer without instancing r.materials, so the
                    // shared material (and every other renderer using it) stays untouched.
                    var block = new MaterialPropertyBlock();
                    r.GetPropertyBlock(block);
                    if (r.sharedMaterial == null || !r.sharedMaterial.HasProperty("_Color")) continue;
                    Color tintedColor = Color.Lerp(r.sharedMaterial.GetColor("_Color"), teamBlue, TintStrength);
                    block.SetColor("_Color", tintedColor);
                    r.SetPropertyBlock(block);
                    tinted++;
                }

                if (tinted > 0)
                {
                    if (ArenaConfig.DebugLogging.Value)
                        Plugin.Log.LogInfo("TeamTint: tinted " + tinted + " materials on " + slot.Contestant.Name);
                }
                else if (!_warnedNoColorProperty)
                {
                    _warnedNoColorProperty = true;
                    Plugin.Log.LogWarning("TeamTint: no materials expose _Color; tint has no effect on this sosig type.");
                }
            }
            catch (Exception e) { Plugin.Log.LogError("TeamTint.OnSosigBound: " + e); }
        }
    }
}
