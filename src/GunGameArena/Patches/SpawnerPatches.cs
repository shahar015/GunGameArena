using System;
using GunGame.Scripts;
using GunGameArena.Core;
using HarmonyLib;

namespace GunGameArena.Patches
{
    [HarmonyPatch(typeof(CustomSosigSpawner), "Spawn")]
    public static class SpawnerPatches
    {
        /// <summary>Raised after a freshly spawned sosig is bound to its roster slot.</summary>
        public static event Action<Slot> SosigBound;

        [HarmonyPrefix]
        private static void Prefix(CustomSosigSpawner __instance, ref Slot __state)
        {
            __state = null;
            try
            {
                if (!Roster.Active) return;
                __state = Roster.ClaimVacantSlot();
                if (Roster.Mode != TeamMode.Off) __instance.IFF = __state.Contestant.Iff;
            }
            catch (Exception e) { Plugin.Log.LogError("SpawnerPatches.Prefix: " + e); }
        }

        [HarmonyPostfix]
        private static void Postfix(SpawnedSosigInfo __result, Slot __state)
        {
            try
            {
                if (__state == null || __result.SpawnedSosig == null) return;
                Roster.Bind(__state, __result.SpawnedSosig);
                Plugin.Log.LogInfo("Spawned " + __state.Contestant + " as " + __result.SosigType
                                   + " (game IFF " + __result.SpawnedSosig.GetIFF() + ")");
                if (SosigBound != null) SosigBound(__state);
                Roster.RaiseChanged();
            }
            catch (Exception e) { Plugin.Log.LogError("SpawnerPatches.Postfix: " + e); }
        }
    }
}
