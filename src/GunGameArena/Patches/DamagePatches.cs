using System;
using FistVR;
using HarmonyLib;

namespace GunGameArena.Patches
{
    [HarmonyPatch(typeof(Sosig), "ProcessDamage", new[] { typeof(Damage), typeof(SosigLink) })]
    public static class ProcessDamagePatch
    {
        [HarmonyPostfix]
        private static void Postfix(Sosig __instance, Damage d)
        {
            try { KillTracker.RecordHit(__instance, d); }
            catch (Exception e) { Plugin.Log.LogError("ProcessDamagePatch: " + e); }
        }
    }

    [HarmonyPatch(typeof(Sosig), "SosigDies")]
    public static class SosigDiesPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Sosig __instance)
        {
            try { KillTracker.OnSosigDying(__instance); }
            catch (Exception e) { Plugin.Log.LogError("SosigDiesPatch: " + e); }
        }
    }
}
