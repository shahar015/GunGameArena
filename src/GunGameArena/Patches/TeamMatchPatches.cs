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

    [HarmonyPatch(typeof(Sosig), "ProcessDamage", new[] { typeof(Damage), typeof(SosigLink) })]
    public static class FriendlyFirePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Sosig __instance, Damage d)
        {
            try { return !TeamMatch.ShouldBlockFriendlyFire(__instance, d); }
            catch (Exception e) { Plugin.Log.LogError("FriendlyFirePatch: " + e); return true; }
        }
    }
}
