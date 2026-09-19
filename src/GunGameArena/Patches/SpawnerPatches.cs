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

        private static Slot _pending;

        [HarmonyPrefix]
        private static void Prefix(CustomSosigSpawner __instance)
        {
            _pending = null;
            try
            {
                if (!Roster.Active) return;
                _pending = Roster.ClaimVacantSlot();
                if (Roster.Mode != TeamMode.Off) __instance.IFF = _pending.Contestant.Iff;
            }
            catch (Exception e) { Plugin.Log.LogError("SpawnerPatches.Prefix: " + e); }
        }

        [HarmonyPostfix]
        private static void Postfix(SpawnedSosigInfo __result)
        {
            try
            {
                if (_pending == null || __result.SpawnedSosig == null) return;
                Roster.Bind(_pending, __result.SpawnedSosig);
                Plugin.Log.LogInfo("Spawned " + _pending.Contestant + " as " + __result.SosigType
                                   + " (game IFF " + __result.SpawnedSosig.GetIFF() + ")");
                if (SosigBound != null) SosigBound(_pending);
                Roster.RaiseChanged();
            }
            catch (Exception e) { Plugin.Log.LogError("SpawnerPatches.Postfix: " + e); }
            finally { _pending = null; }
        }
    }
}
