using System;
using System.Collections;
using System.Collections.Generic;
using FistVR;
using GunGameArena.Core;
using UnityEngine;
using UnityEngine.AI;

namespace GunGameArena.Behaviour
{
    /// <summary>Every 10–20 s sends a share of player-hostile sosigs to a point near the player.</summary>
    public class HunterDirector : MonoBehaviour
    {
        private static HunterDirector _instance;
        private static readonly System.Random Rng = new System.Random();

        public static void Install()
        {
            GunGameHooks.RoundStarted += OnRoundStarted;
            GunGameHooks.RoundEnded += OnRoundEnded;
        }

        private static void OnRoundStarted()
        {
            try
            {
                if (!ArenaConfig.Hunters.Value || Roster.Mode == TeamMode.Off) return;
                if (_instance == null) _instance = new GameObject("GunGameArena_Hunters").AddComponent<HunterDirector>();
                _instance.StartCoroutine(_instance.Loop());
            }
            catch (Exception e) { Plugin.Log.LogError("HunterDirector.OnRoundStarted: " + e); }
        }

        private static void OnRoundEnded()
        {
            try
            {
                if (_instance != null) Destroy(_instance.gameObject);
                _instance = null;
            }
            catch (Exception e) { Plugin.Log.LogError("HunterDirector.OnRoundEnded: " + e); }
        }

        private IEnumerator Loop()
        {
            while (Roster.Active)
            {
                yield return new WaitForSeconds(NextInterval());
                IssueOrders();
            }
        }

        private static float NextInterval()
        {
            try
            {
                float min = ArenaConfig.HunterIntervalMin.Value, max = Mathf.Max(min, ArenaConfig.HunterIntervalMax.Value);
                return UnityEngine.Random.Range(min, max);
            }
            catch (Exception e) { Plugin.Log.LogError("HunterDirector.NextInterval: " + e); return 15f; }
        }

        private void IssueOrders()
        {
            try
            {
                var hostile = new List<Contestant>();
                var bySlot = new Dictionary<Contestant, Slot>();
                foreach (var slot in Roster.LivingSosigSlots())
                {
                    if (slot.Contestant.Iff == Roster.PlayerIff) continue;   // allies never hunt the player
                    hostile.Add(slot.Contestant);
                    bySlot[slot.Contestant] = slot;
                }
                var hunters = HunterPicker.Pick(Rng, hostile, ArenaConfig.HunterShare.Value);
                if (hunters.Count == 0) return;

                Vector3 playerPos = Roster.PlayerHeadPosition();
                var names = new List<string>();
                for (int i = 0; i < hunters.Count; i++)
                {
                    Sosig s = bySlot[hunters[i]].Sosig;
                    if (s == null) continue;
                    Vector3 target = PointNear(playerPos);
                    s.SetCurrentOrder(Sosig.SosigOrder.Assault);
                    s.CommandAssaultPoint(target);
                    names.Add(hunters[i].Name);
                }
                Plugin.Log.LogInfo("Hunters: " + string.Join(", ", names.ToArray()));
            }
            catch (Exception e) { Plugin.Log.LogError("HunterDirector.IssueOrders: " + e); }
        }

        private static Vector3 PointNear(Vector3 playerPos)
        {
            Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
            float dist = UnityEngine.Random.Range(8f, 15f);
            Vector3 candidate = playerPos + new Vector3(dir.x, 0f, dir.y) * dist;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(candidate, out hit, 5f, NavMesh.AllAreas)) return hit.position;
            return playerPos;
        }
    }
}
