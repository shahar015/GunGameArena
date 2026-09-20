using System;
using System.Collections;
using System.Collections.Generic;
using GunGameArena.Core;
using GunGameArena.Patches;
using UnityEngine;

namespace GunGameArena.Behaviour
{
    /// <summary>FFA only. Each sosig is hostile to a few rivals at a time (spec §4b.2).</summary>
    public class GrudgeDirector : MonoBehaviour
    {
        private static GrudgeDirector _instance;
        private static readonly System.Random Rng = new System.Random();

        private readonly Dictionary<Slot, float> _nextReroll = new Dictionary<Slot, float>();
        private readonly Dictionary<Slot, List<int>> _rivalIds = new Dictionary<Slot, List<int>>();
        private readonly Dictionary<Slot, List<int>> _stickyIffs = new Dictionary<Slot, List<int>>();

        private static bool Enabled
        {
            get { return ArenaConfig.Grudges.Value && Roster.Mode == TeamMode.FreeForAll && Roster.Active; }
        }

        public static void Install()
        {
            GunGameHooks.RoundStarted += OnRoundStarted;
            GunGameHooks.RoundEnded += OnRoundEnded;
            SpawnerPatches.SosigBound += OnSosigBound;
            KillTracker.KillRegistered += OnKill;
            KillTracker.RetaliationTriggered += OnRetaliation;
        }

        private static void OnRoundStarted()
        {
            try
            {
                if (!Enabled) return;
                if (_instance == null) _instance = new GameObject("GunGameArena_Grudges").AddComponent<GrudgeDirector>();
                foreach (var slot in Roster.LivingSosigSlots()) _instance.StartCoroutine(_instance.RerollNextFrame(slot));
            }
            catch (Exception e) { Plugin.Log.LogError("GrudgeDirector.OnRoundStarted: " + e); }
        }

        private static void OnRoundEnded()
        {
            try
            {
                if (_instance != null) Destroy(_instance.gameObject);
                _instance = null;
            }
            catch (Exception e) { Plugin.Log.LogError("GrudgeDirector.OnRoundEnded: " + e); }
        }

        private static void OnSosigBound(Slot slot)
        {
            try
            {
                if (!Enabled || _instance == null) return;
                _instance.StartCoroutine(_instance.RerollNextFrame(slot));
            }
            catch (Exception e) { Plugin.Log.LogError("GrudgeDirector.OnSosigBound: " + e); }
        }

        private static void OnKill(Contestant victim, Contestant killer)
        {
            try
            {
                if (!Enabled || _instance == null || victim == null) return;
                foreach (var kv in _instance._rivalIds)
                    if (kv.Value.Contains(victim.Id)) _instance._nextReroll[kv.Key] = 0f;   // re-roll on next Update
            }
            catch (Exception e) { Plugin.Log.LogError("GrudgeDirector.OnKill: " + e); }
        }

        private static void OnRetaliation(Slot slot, int attackerIff)
        {
            try
            {
                if (!Enabled || _instance == null || slot == null) return;
                List<int> list;
                if (!_instance._stickyIffs.TryGetValue(slot, out list)) { list = new List<int>(); _instance._stickyIffs[slot] = list; }
                if (!list.Contains(attackerIff)) list.Add(attackerIff);
            }
            catch (Exception e) { Plugin.Log.LogError("GrudgeDirector.OnRetaliation: " + e); }
        }

        private IEnumerator RerollNextFrame(Slot slot)
        {
            yield return null;   // Priority system is initialised in Sosig.Start / Configure
            Reroll(slot);
        }

        private void Update()
        {
            try
            {
                if (!Enabled) return;
                var due = new List<Slot>();
                foreach (var kv in _nextReroll) if (Time.time >= kv.Value) due.Add(kv.Key);
                for (int i = 0; i < due.Count; i++) Reroll(due[i]);
            }
            catch (Exception e) { Plugin.Log.LogError("GrudgeDirector.Update: " + e); }
        }

        private void Reroll(Slot slot)
        {
            try
            {
                if (slot == null || slot.IsVacant || slot.Sosig.Priority == null) { _nextReroll.Remove(slot); _rivalIds.Remove(slot); _stickyIffs.Remove(slot); return; }
                Roster.UpdatePositions();
                var rivals = RivalSelector.Pick(Rng, slot.Contestant, Roster.AllContestants,
                    ArenaConfig.RivalCount.Value, ArenaConfig.RivalRadius.Value, ArenaConfig.PlayerRivalWeight.Value);

                slot.Sosig.Priority.SetAllFriendly();
                var ids = new List<int>();
                var names = new List<string>();
                for (int i = 0; i < rivals.Count; i++)
                {
                    slot.Sosig.Priority.MakeEnemy(rivals[i].Iff);
                    ids.Add(rivals[i].Id);
                    names.Add(rivals[i].Name);
                }

                List<int> sticky;
                if (_stickyIffs.TryGetValue(slot, out sticky))
                {
                    for (int i = 0; i < sticky.Count; i++) slot.Sosig.Priority.MakeEnemy(sticky[i]);   // survives exactly one re-roll
                    names.Add("+" + sticky.Count + " grudge");
                    _stickyIffs.Remove(slot);
                }
                _rivalIds[slot] = ids;
                float min = ArenaConfig.RivalRerollMin.Value, max = Mathf.Max(min, ArenaConfig.RivalRerollMax.Value);
                _nextReroll[slot] = Time.time + UnityEngine.Random.Range(min, max);
                Plugin.Log.LogInfo("Grudges " + slot.Contestant.Name + " -> " + string.Join(", ", names.ToArray()));
            }
            catch (Exception e) { Plugin.Log.LogError("GrudgeDirector.Reroll: " + e); }
        }
    }
}
