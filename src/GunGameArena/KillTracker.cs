using System;
using System.Collections.Generic;
using FistVR;
using GunGameArena.Core;
using UnityEngine;

namespace GunGameArena
{
    public static class KillTracker
    {
        private struct LastHit { public int Iff; public Vector3 Point; public float Time; }

        public static event Action<Contestant, Contestant> KillRegistered;

        private static readonly Dictionary<int, LastHit> _hits = new Dictionary<int, LastHit>();
        private static readonly HashSet<int> _processed = new HashSet<int>();
        private static readonly Dictionary<int, bool> _killedByPlayer = new Dictionary<int, bool>();

        public static void Install()
        {
            GunGameHooks.RoundStarting += OnRoundStarting;
            GunGameHooks.RoundEnded += OnRoundEnded;
        }

        private static void OnRoundStarting()
        {
            _hits.Clear(); _processed.Clear(); _killedByPlayer.Clear();
            var scene = GM.CurrentSceneSettings;
            if (scene != null)
            {
                scene.PlayerDeathFromIFFEvent -= OnPlayerDeath;
                scene.PlayerDeathFromIFFEvent += OnPlayerDeath;
            }
        }

        private static void OnRoundEnded()
        {
            var scene = GM.CurrentSceneSettings;
            if (scene != null) scene.PlayerDeathFromIFFEvent -= OnPlayerDeath;
        }

        public static void RecordHit(Sosig victim, Damage d)
        {
            if (victim == null || d == null || !Roster.Active) return;
            Vector3 p = d.Source_Point == Vector3.zero ? d.point : d.Source_Point;
            _hits[victim.GetInstanceID()] = new LastHit { Iff = d.Source_IFF, Point = p, Time = Time.time };

            // Grudge retaliation: whoever shoots you becomes your enemy (FFA only).
            if (ArenaConfig.Grudges.Value && Roster.Mode == TeamMode.FreeForAll
                && d.Source_IFF >= 0 && d.Source_IFF != victim.GetIFF() && victim.Priority != null)
            {
                victim.Priority.MakeEnemy(d.Source_IFF);
            }
        }

        /// <summary>Called from the SosigDies prefix, before GunGame despawns the victim.</summary>
        public static void OnSosigDying(Sosig victim)
        {
            if (victim == null || !Roster.Active) return;
            if (victim.BodyState == Sosig.SosigBodyState.Dead) return;
            int id = victim.GetInstanceID();
            if (!_processed.Add(id)) return;

            Slot slot = Roster.FindBySosig(victim);
            if (slot == null) return;

            LastHit hit;
            bool hasHit = _hits.TryGetValue(id, out hit);
            int killerIff = hasHit ? hit.Iff : victim.GetDiedFromIFF();
            Vector3 point = hasHit ? hit.Point : victim.transform.position;

            Roster.UpdatePositions();
            Contestant winner = KillAttribution.Nearest(Roster.AllContestants, killerIff, slot.Contestant.Id, Roster.ToVec(point));
            Roster.MarkDead(slot);
            _killedByPlayer[id] = winner != null && winner.IsPlayer;

            if (winner != null)
            {
                winner.AddKill(Time.time);
                Plugin.Log.LogInfo("KILL " + winner.Name + " -> " + slot.Contestant.Name + " (iff " + killerIff + ")");
            }
            else
            {
                Plugin.Log.LogInfo("DEATH " + slot.Contestant.Name + " with no credited killer (iff " + killerIff + ")");
            }
            if (KillRegistered != null) KillRegistered(slot.Contestant, winner);
            Roster.RaiseChanged();
        }

        public static bool LastKillWasByPlayer(Sosig victim)
        {
            if (victim == null) return true;
            bool b;
            if (_killedByPlayer.TryGetValue(victim.GetInstanceID(), out b)) return b;
            return victim.GetDiedFromIFF() == Roster.PlayerIff; // untracked sosig: GunGame's own rule
        }

        public static void OnPlayerDeath(bool killedSelf, int iff)
        {
            try
            {
                if (!Roster.Active || killedSelf || Roster.Player == null) return;
                Roster.UpdatePositions();
                var sosigsOnly = new List<Contestant>();
                foreach (var s in Roster.LivingSosigSlots()) sosigsOnly.Add(s.Contestant);
                Contestant winner = KillAttribution.Nearest(sosigsOnly, iff, Roster.Player.Contestant.Id, Roster.Player.Contestant.Position);
                if (winner != null)
                {
                    winner.AddKill(Time.time);
                    Plugin.Log.LogInfo("KILL " + winner.Name + " -> " + Roster.Player.Contestant.Name + " (player, iff " + iff + ")");
                    if (KillRegistered != null) KillRegistered(null, winner);
                    Roster.RaiseChanged();
                }
            }
            catch (Exception e) { Plugin.Log.LogError("KillTracker.OnPlayerDeath: " + e); }
        }
    }
}
