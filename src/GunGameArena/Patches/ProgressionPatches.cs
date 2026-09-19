using System;
using FistVR;
using GunGame.Scripts;
using HarmonyLib;

namespace GunGameArena.Patches
{
    /// <summary>GunGame credits any death whose source IFF equals the player's IFF. In Teams mode
    /// allies share that IFF, so this prefix lets the original run only for real player kills.</summary>
    [HarmonyPatch(typeof(Progression), "OnSosigKilledByPlayer")]
    public static class ProgressionPatches
    {
        [HarmonyPrefix]
        private static bool Prefix(Sosig killedSosig)
        {
            try
            {
                if (!Roster.Active) return true;
                bool byPlayer = KillTracker.LastKillWasByPlayer(killedSosig);
                if (!byPlayer) Plugin.Log.LogInfo("Blocked progression credit: kill was by an ally, not the player.");
                return byPlayer;
            }
            catch (Exception e) { Plugin.Log.LogError("ProgressionPatches: " + e); return true; }
        }
    }
}
