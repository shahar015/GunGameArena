using System;
using System.Collections.Generic;
using FistVR;
using GunGame.Scripts.Options;
using GunGameArena.Core;
using UnityEngine;

namespace GunGameArena
{
    public class Slot
    {
        public Contestant Contestant;
        public Sosig Sosig;
        public Sprite Portrait;

        /// <summary>Engine-driven "alive": true once the bound sosig is gone or the game itself
        /// reports it dead. Decides whether the slot is free for the spawner to reuse. This is
        /// independent of <see cref="Contestant.IsAlive"/>, which is roster-driven.</summary>
        public bool IsVacant
        {
            get { return Sosig == null || Sosig.BodyState == Sosig.SosigBodyState.Dead; }
        }
    }

    /// <summary>The round's contestants. Sosig slots persist; dead sosigs' replacements
    /// re-bind to the same slot (respawn semantics).</summary>
    public static class Roster
    {
        public static event Action Changed;

        public static bool Active;
        public static TeamMode Mode = TeamMode.Off;
        public static int PlayerIff;
        public static Slot Player;
        public static readonly List<Slot> SosigSlots = new List<Slot>();

        private static int _nextId;
        private static NameGenerator _names;
        private static System.Random _rng;

        public static IEnumerable<Contestant> AllContestants
        {
            get
            {
                if (Player != null) yield return Player.Contestant;
                for (int i = 0; i < SosigSlots.Count; i++) yield return SosigSlots[i].Contestant;
            }
        }

        public static void Install()
        {
            GunGameHooks.RoundStarting += OnRoundStarting;
            GunGameHooks.RoundEnded += OnRoundEnded;
        }

        private static void OnRoundStarting()
        {
            int playerIff = GM.CurrentPlayerBody != null ? GM.CurrentPlayerBody.GetPlayerIFF() : 0;
            Reset(GameSettings.MaxSosigCount, ArenaConfig.Mode.Value, playerIff);
        }

        private static void OnRoundEnded()
        {
            Active = false;
            SosigSlots.Clear();
            Player = null;
            RaiseChanged();
        }

        public static void Reset(int sosigCount, TeamMode mode, int playerIff)
        {
            Mode = mode;
            PlayerIff = playerIff;
            _nextId = 1;
            int seed = ArenaConfig.NameSeed.Value;
            _names = new NameGenerator(seed);
            _rng = seed == 0 ? new System.Random() : new System.Random(seed);
            int[] weights = TierRoller.ParseWeights(ArenaConfig.TierWeights.Value);

            SosigSlots.Clear();
            for (int slot = 0; slot < sosigCount; slot++) SosigSlots.Add(NewSlot(slot, sosigCount, weights));

            string playerName = "You";
            try { if (!string.IsNullOrEmpty(GM.PlayerName)) playerName = GM.PlayerName; } catch { }
            Player = new Slot
            {
                Contestant = new Contestant { Id = 0, Name = playerName, TeamIndex = 0, Iff = playerIff, IsPlayer = true, IsAlive = true }
            };

            Active = true;
            Plugin.Log.LogInfo("Roster reset: " + sosigCount + " sosigs, mode " + mode + ", player IFF " + playerIff);
            for (int i = 0; i < SosigSlots.Count; i++) Plugin.Log.LogInfo("  slot " + i + ": " + SosigSlots[i].Contestant + " tier " + SosigSlots[i].Contestant.Tier);
            RaiseChanged();
        }

        private static Slot NewSlot(int slotIndex, int sosigCount, int[] weights)
        {
            int team = TeamAssigner.TeamIndexFor(Mode, slotIndex, sosigCount, ArenaConfig.TeamCount.Value, ArenaConfig.AllySosigs.Value);
            var c = new Contestant
            {
                Id = _nextId++,
                Name = _names.Next(),
                TeamIndex = team,
                Iff = TeamAssigner.IffFor(Mode, team, PlayerIff),
                IsAlive = false,
                Tier = ArenaConfig.SkillTiers.Value ? TierRoller.Roll(_rng, weights) : SkillTier.Regular
            };
            return new Slot { Contestant = c };
        }

        public static Slot ClaimVacantSlot()
        {
            for (int i = 0; i < SosigSlots.Count; i++) if (SosigSlots[i].IsVacant) return SosigSlots[i];
            var extra = NewSlot(SosigSlots.Count, SosigSlots.Count + 1, TierRoller.ParseWeights(ArenaConfig.TierWeights.Value));
            SosigSlots.Add(extra);
            Plugin.Log.LogWarning("More sosigs than roster slots; added " + extra.Contestant);
            return extra;
        }

        public static void Bind(Slot slot, Sosig sosig)
        {
            slot.Sosig = sosig;
            slot.Contestant.IsAlive = sosig != null;
        }

        public static Slot FindBySosig(Sosig sosig)
        {
            if (sosig == null) return null;
            for (int i = 0; i < SosigSlots.Count; i++) if (SosigSlots[i].Sosig == sosig) return SosigSlots[i];
            return null;
        }

        /// <summary>Roster-driven "alive": <see cref="Contestant.IsAlive"/> is set true by
        /// <see cref="Bind"/> and false here by MarkDead, which the kill tracker's SosigDies prefix
        /// calls before GunGame spawns the replacement sosig. It can go false slightly ahead of the
        /// engine-driven <see cref="Slot.IsVacant"/>, so <see cref="LivingSosigSlots"/> requires both.</summary>
        public static void MarkDead(Slot slot)
        {
            slot.Contestant.IsAlive = false;
        }

        public static IEnumerable<Slot> LivingSosigSlots()
        {
            for (int i = 0; i < SosigSlots.Count; i++)
            {
                var s = SosigSlots[i];
                if (!s.IsVacant && s.Contestant.IsAlive) yield return s;
            }
        }

        public static Vector3 PlayerHeadPosition()
        {
            var body = GM.CurrentPlayerBody;
            if (body == null || body.Head == null) return Vector3.zero;
            return body.Head.position;
        }

        public static void UpdatePositions()
        {
            if (Player != null) Player.Contestant.Position = ToVec(PlayerHeadPosition());
            for (int i = 0; i < SosigSlots.Count; i++)
            {
                var s = SosigSlots[i];
                if (s.Sosig == null) continue;
                Transform t = (s.Sosig.Links != null && s.Sosig.Links.Count > 0 && s.Sosig.Links[0] != null)
                    ? s.Sosig.Links[0].transform : s.Sosig.transform;
                s.Contestant.Position = ToVec(t.position);
            }
        }

        public static Vec3 ToVec(Vector3 v) { return new Vec3(v.x, v.y, v.z); }

        public static void RaiseChanged()
        {
            if (Changed == null) return;
            foreach (Delegate d in Changed.GetInvocationList())
            {
                try { ((Action)d)(); }
                catch (Exception e) { Plugin.Log.LogError("Roster.Changed handler failed: " + e); }
            }
        }
    }
}
