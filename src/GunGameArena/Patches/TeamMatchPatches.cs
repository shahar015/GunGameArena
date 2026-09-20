using System;
using FistVR;
using GunGame.Scripts;
using GunGameArena.Behaviour;
using HarmonyLib;

namespace GunGameArena.Patches
{
    [HarmonyPatch(typeof(Progression), "Promote")]
    public static class PromoteLoopPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Progression __instance)
        {
            try { TeamMatch.LoopRotationIfNeeded(__instance); }
            catch (Exception e) { Plugin.Log.LogError("PromoteLoopPatch: " + e); }
        }
    }

    /// <summary>Patched on <see cref="SosigLink.Damage(Damage)"/> rather than <see cref="Sosig.ProcessDamage"/>:
    /// ProcessDamage only handles stagger/stun, while SosigLink.Damage is what actually calls ProcessDamage
    /// and then applies health/limb damage via DamageIntegrity and bleeding — blocking there is what actually
    /// stops a friendly-fire hit from hurting a teammate.</summary>
    [HarmonyPatch(typeof(SosigLink), "Damage", new[] { typeof(Damage) })]
    public static class FriendlyFirePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(SosigLink __instance, Damage d)
        {
            try { return !(__instance != null && TeamMatch.ShouldBlockFriendlyFire(__instance.S, d)); }
            catch (Exception e) { Plugin.Log.LogError("FriendlyFirePatch: " + e); return true; }
        }
    }
}
