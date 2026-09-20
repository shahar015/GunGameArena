using System;
using System.Collections.Generic;
using FistVR;
using GunGame.Scripts;
using GunGameArena.Core;
using HarmonyLib;
using UnityEngine;

namespace GunGameArena.Patches
{
    /// <summary>Replaces GunGame's random spawner pick with the spawner farthest from everyone.
    /// Re-implements the tail of SpawnSosigRandomPlace (Spawn + register) identically.</summary>
    [HarmonyPatch(typeof(SosigBehavior), "SpawnSosigRandomPlace")]
    public static class SpawnPlacementPatches
    {
        private static readonly System.Random Rng = new System.Random();

        [HarmonyPrefix]
        private static bool Prefix(SosigBehavior __instance, SosigEnemyID sosigtype)
        {
            try
            {
                if (!Roster.Active || !ArenaConfig.SpreadSpawns.Value) return true;
                var spawners = __instance.SosigSpawners;
                if (spawners == null || spawners.Count == 0 || GM.CurrentPlayerBody == null) return true;

                var positions = new List<Vec3>(spawners.Count);
                for (int i = 0; i < spawners.Count; i++) positions.Add(Roster.ToVec(spawners[i].transform.position));

                var occupied = new List<Vec3>();
                foreach (var kv in __instance.Sosigs)
                {
                    Sosig s = kv.Key;
                    if (s != null && s.BodyState != Sosig.SosigBodyState.Dead) occupied.Add(Roster.ToVec(s.transform.position));
                }

                int idx = SpawnerChooser.Choose(Rng, positions, __instance.IgnoredSpawnersCloseToPlayer,
                    __instance.IgnoredSpawnersFarFromPlayer, Roster.ToVec(GM.CurrentPlayerBody.transform.position), occupied);
                if (idx < 0) return true;

                SpawnedSosigInfo info = spawners[idx].Spawn(sosigtype);   // SpawnerPatches run inside this call
                if (info.SpawnedSosig != null && !__instance.Sosigs.ContainsKey(info.SpawnedSosig))
                    __instance.Sosigs.Add(info.SpawnedSosig, info.SosigType);
                return false;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("SpawnPlacementPatches: " + e);
                return true;
            }
        }
    }
}
