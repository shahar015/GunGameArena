using System;
using FistVR;
using GunGameArena.Behaviour;
using HarmonyLib;

namespace GunGameArena.Patches
{
    /// <summary>Re-applies the owner's tier whenever a tracked sosig picks up a weapon.</summary>
    [HarmonyPatch(typeof(SosigWeapon), "BotPickup")]
    public static class WeaponPatches
    {
        [HarmonyPostfix]
        private static void Postfix(SosigWeapon __instance, Sosig S)
        {
            try
            {
                if (!Roster.Active || !ArenaConfig.SkillTiers.Value) return;
                Slot slot = Roster.FindBySosig(S);
                if (slot != null) SkillApplier.ApplyWeapon(__instance, slot.Contestant.Tier);
            }
            catch (Exception e) { Plugin.Log.LogError("WeaponPatches: " + e); }
        }
    }
}
