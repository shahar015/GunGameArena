using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FistVR;
using GunGame.Scripts;
using GunGame.Scripts.Options;
using GunGameArena.Core;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace GunGameArena.Behaviour
{
    /// <summary>Team Deathmatch rules: points victory, looping rotation, friendly fire, weapon-count lock.</summary>
    public static class TeamMatch
    {
        public static event Action<int> TeamWon;

        private const float EndDelaySeconds = 5f;
        private static bool _victoryDeclared;
        private static TeamMatchRunner _runner;

        private static readonly PropertyInfo CurrentWeaponIdProp = AccessTools.Property(typeof(Progression), "CurrentWeaponId");
        private static readonly FieldInfo CounterTextField = AccessTools.Field(typeof(WeaponCountOption), "_counterText");

        // weapon-count lock state
        private static readonly List<FVRPointableButton> _lockedPointables = new List<FVRPointableButton>();
        private static readonly Dictionary<Graphic, Color> _originalColors = new Dictionary<Graphic, Color>();
        private static bool _locked;

        public static bool IsTeams { get { return Roster.Active && Roster.Mode == TeamMode.Teams; } }

        public static void Install()
        {
            GunGameHooks.RoundStarting += OnRoundStarting;
            GunGameHooks.RoundEnded += OnRoundEnded;
            Roster.Changed += OnRosterChanged;
        }

        private static void OnRoundStarting()
        {
            try { _victoryDeclared = false; ApplyWeaponCountLock(); }
            catch (Exception e) { Plugin.Log.LogError("TeamMatch.OnRoundStarting: " + e); }
        }

        private static void OnRoundEnded()
        {
            try { _victoryDeclared = false; _lockedPointables.Clear(); _originalColors.Clear(); _locked = false; if (_runner != null) { UnityEngine.Object.Destroy(_runner.gameObject); _runner = null; } }
            catch (Exception e) { Plugin.Log.LogError("TeamMatch.OnRoundEnded: " + e); }
        }

        private static void OnRosterChanged()
        {
            try
            {
                if (!IsTeams || _victoryDeclared) return;
                var gm = MonoBehaviourSingleton<GameManager>.Instance;
                if (gm == null || gm.GameEnded) return;
                int winner = TeamScore.Winner(Roster.AllContestants, TeamAssigner.ClampTeamCount(ArenaConfig.TeamCount.Value), ArenaConfig.PointsToWin.Value);
                if (winner < 0) return;
                _victoryDeclared = true;
                Plugin.Log.LogInfo("TEAM VICTORY: " + HudPalette.TeamName(winner) + " reached " + ArenaConfig.PointsToWin.Value + " points.");
                if (TeamWon != null) TeamWon(winner);
                Runner().StartCoroutine(Runner().EndAfterDelay());
            }
            catch (Exception e) { Plugin.Log.LogError("TeamMatch.OnRosterChanged: " + e); }
        }

        private static TeamMatchRunner Runner()
        {
            if (_runner == null) _runner = new GameObject("GunGameArena_TeamMatch").AddComponent<TeamMatchRunner>();
            return _runner;
        }

        /// <summary>Called by the Promote prefix: loop the rotation instead of ending the round.</summary>
        public static void LoopRotationIfNeeded(Progression progression)
        {
            if (!IsTeams || progression == null || CurrentWeaponIdProp == null) return;
            int poolCount = GameSettings.CurrentPool != null ? GameSettings.CurrentPool.GetWeaponCount() : int.MaxValue;
            int limit = Math.Min(poolCount, WeaponCountOption.WeaponCount);
            if (progression.CurrentWeaponId + 1 >= limit)
            {
                CurrentWeaponIdProp.SetValue(progression, -1, null);
                Plugin.Log.LogInfo("Team Deathmatch: weapon rotation looped.");
            }
        }

        /// <summary>Called by the ProcessDamage prefix. True = block this damage.</summary>
        public static bool ShouldBlockFriendlyFire(Sosig victim, Damage d)
        {
            if (!IsTeams || ArenaConfig.FriendlyFire.Value || d == null || victim == null) return false;
            if (d.Source_IFF != Roster.PlayerIff) return false;
            Slot slot = Roster.FindBySosig(victim);
            return slot != null && slot.Contestant.TeamIndex == 0;
        }

        public static void ApplyWeaponCountLock()
        {
            try
            {
                bool want = ArenaConfig.Mode.Value == TeamMode.Teams;
                var option = UnityEngine.Object.FindObjectOfType<WeaponCountOption>();
                if (option == null) return;
                if (want && !_locked) Lock(option);
                else if (!want && _locked) Unlock();
            }
            catch (Exception e) { Plugin.Log.LogError("TeamMatch.ApplyWeaponCountLock: " + e); }
        }

        private static void Lock(WeaponCountOption option)
        {
            _lockedPointables.Clear(); _originalColors.Clear();
            var buttons = UnityEngine.Object.FindObjectsOfType<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                Button b = buttons[i];
                bool targetsOption = false;
                for (int k = 0; k < b.onClick.GetPersistentEventCount(); k++)
                    if ((object)b.onClick.GetPersistentTarget(k) == (object)option) targetsOption = true;
                if (!targetsOption) continue;
                var p = b.GetComponent<FVRPointableButton>();
                if (p != null && p.enabled) { p.enabled = false; _lockedPointables.Add(p); }
                Dim(b.GetComponent<Graphic>());
                foreach (var g in b.GetComponentsInChildren<Graphic>(true)) Dim(g);
            }
            var counter = CounterTextField != null ? CounterTextField.GetValue(option) as Text : null;
            if (counter != null)
            {
                Dim(counter);
                if (counter.transform.parent != null)
                    foreach (var g in counter.transform.parent.GetComponentsInChildren<Graphic>(true)) Dim(g);
            }
            _locked = true;
            Plugin.Log.LogInfo("Team Deathmatch: 'Number of weapons' controls locked (" + _lockedPointables.Count + " buttons).");
        }

        private static void Dim(Graphic g)
        {
            if (g == null || _originalColors.ContainsKey(g)) return;
            _originalColors[g] = g.color;
            Color c = g.color; c.a *= 0.4f; g.color = c;
        }

        private static void Unlock()
        {
            for (int i = 0; i < _lockedPointables.Count; i++) if (_lockedPointables[i] != null) _lockedPointables[i].enabled = true;
            foreach (var kv in _originalColors) if (kv.Key != null) kv.Key.color = kv.Value;
            _lockedPointables.Clear(); _originalColors.Clear();
            _locked = false;
            Plugin.Log.LogInfo("'Number of weapons' controls restored.");
        }

        public class TeamMatchRunner : MonoBehaviour
        {
            public IEnumerator EndAfterDelay()
            {
                yield return new WaitForSeconds(EndDelaySeconds);
                EndRound();
            }

            private static void EndRound()
            {
                try
                {
                    var gm = MonoBehaviourSingleton<GameManager>.Instance;
                    if (gm != null && !gm.GameEnded) gm.EndGame();
                }
                catch (Exception e) { Plugin.Log.LogError("TeamMatch.EndRound: " + e); }
            }
        }
    }
}
