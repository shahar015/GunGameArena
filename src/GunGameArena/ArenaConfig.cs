using System.Collections.Generic;
using BepInEx.Configuration;
using GunGameArena.Core;

namespace GunGameArena
{
    public static class ArenaConfig
    {
        // Arena
        public static ConfigEntry<TeamMode> Mode;
        public static ConfigEntry<int> TeamCount;
        public static ConfigEntry<int> AllySosigs;
        // Leaderboard
        public static ConfigEntry<bool> LeaderboardEnabled;
        public static ConfigEntry<int> TopCount;
        public static ConfigEntry<float> Scale;
        public static ConfigEntry<float> Distance;
        public static ConfigEntry<float> Height;
        public static ConfigEntry<bool> ShowNames;
        public static ConfigEntry<bool> ShowTierBadge;
        // Names
        public static ConfigEntry<int> NameSeed;
        // Behaviour
        public static ConfigEntry<bool> SpreadSpawns;
        public static ConfigEntry<bool> Grudges;
        public static ConfigEntry<int> RivalCount;
        public static ConfigEntry<float> RivalRadius;
        public static ConfigEntry<float> PlayerRivalWeight;
        public static ConfigEntry<float> RivalRerollMin;
        public static ConfigEntry<float> RivalRerollMax;
        public static ConfigEntry<bool> Hunters;
        public static ConfigEntry<float> HunterShare;
        public static ConfigEntry<float> HunterIntervalMin;
        public static ConfigEntry<float> HunterIntervalMax;
        public static ConfigEntry<bool> SkillTiers;
        public static ConfigEntry<string> TierWeights;

        private static readonly Dictionary<SkillTier, ConfigEntry<float>[]> TierEntries = new Dictionary<SkillTier, ConfigEntry<float>[]>();

        public static void Bind(ConfigFile cfg)
        {
            Mode = cfg.Bind("Arena", "Mode", TeamMode.FreeForAll, "Off = original GunGame. FreeForAll = every sosig for itself. Teams = blue (you + allies) vs red (+ green/yellow).");
            TeamCount = cfg.Bind("Arena", "TeamCount", 2, new ConfigDescription("Teams mode only.", new AcceptableValueRange<int>(2, 4)));
            AllySosigs = cfg.Bind("Arena", "AllySosigs", -1, "Sosigs on your team in Teams mode. -1 = half of the sosig count.");

            LeaderboardEnabled = cfg.Bind("Leaderboard", "Enabled", true, "Show the floating leaderboard HUD.");
            TopCount = cfg.Bind("Leaderboard", "TopCount", 5, "Cards shown before your own card is pinned at the end.");
            Scale = cfg.Bind("Leaderboard", "Scale", 1.0f, "Overall HUD size multiplier.");
            Distance = cfg.Bind("Leaderboard", "Distance", 1.0f, "Metres in front of your head.");
            Height = cfg.Bind("Leaderboard", "Height", 0.35f, "Metres above eye line.");
            ShowNames = cfg.Bind("Leaderboard", "ShowNames", true, "Name label under each card.");
            ShowTierBadge = cfg.Bind("Leaderboard", "ShowTierBadge", true, "Skill tier chevrons under each sosig's name.");

            NameSeed = cfg.Bind("Names", "Seed", 0, "0 = random names every round; any other value = reproducible roster.");

            SpreadSpawns = cfg.Bind("Behaviour", "SpreadSpawns", true, "Spawn each sosig at the spawner farthest from everyone.");
            Grudges = cfg.Bind("Behaviour", "Grudges", true, "FFA only: each sosig hunts a few rivals at a time instead of everyone.");
            RivalCount = cfg.Bind("Behaviour", "RivalCount", 3, "Rivals per sosig.");
            RivalRadius = cfg.Bind("Behaviour", "RivalRadius", 40f, "Metres; rivals are picked from contestants within this radius.");
            PlayerRivalWeight = cfg.Bind("Behaviour", "PlayerRivalWeight", 2.0f, "How much more likely you are to be picked as a rival than a sosig (1 = equal).");
            RivalRerollMin = cfg.Bind("Behaviour", "RivalRerollSecondsMin", 20f, "Seconds between rival re-rolls (min).");
            RivalRerollMax = cfg.Bind("Behaviour", "RivalRerollSecondsMax", 40f, "Seconds between rival re-rolls (max).");
            Hunters = cfg.Bind("Behaviour", "Hunters", true, "Periodically send a share of hostile sosigs toward you.");
            HunterShare = cfg.Bind("Behaviour", "HunterShare", 0.25f, "Fraction of hostile sosigs sent toward you each interval.");
            HunterIntervalMin = cfg.Bind("Behaviour", "HunterIntervalSecondsMin", 10f, "Seconds between hunter orders (min).");
            HunterIntervalMax = cfg.Bind("Behaviour", "HunterIntervalSecondsMax", 20f, "Seconds between hunter orders (max).");
            SkillTiers = cfg.Bind("Behaviour", "SkillTiers", true, "Roll Rookie/Regular/Veteran/Elite per contestant; affects aim, not fire volume.");
            TierWeights = cfg.Bind("Behaviour", "TierWeights", "30,40,20,10", "Relative weights Rookie,Regular,Veteran,Elite.");

            TierEntries.Clear();
            foreach (SkillTier tier in new[] { SkillTier.Rookie, SkillTier.Regular, SkillTier.Veteran, SkillTier.Elite })
            {
                var d = TierTable.Default(tier);
                string sec = "Tier." + tier;
                TierEntries[tier] = new[]
                {
                    cfg.Bind(sec, "Spread", d.Spread, "Multiplier on weapon projectile spread (bigger = misses more)."),
                    cfg.Bind(sec, "FireAngle", d.FireAngle, "Multiplier on how far off-target the sosig will still fire."),
                    cfg.Bind(sec, "Refire", d.Refire, "Multiplier on delay between shots (bigger = slower)."),
                    cfg.Bind(sec, "Reaction", d.Reaction, "Multiplier on target recognition speed (bigger = faster).")
                };
            }
        }

        public static TierMultipliers MultipliersFor(SkillTier tier)
        {
            ConfigEntry<float>[] e;
            if (!TierEntries.TryGetValue(tier, out e)) return TierTable.Default(tier);
            return new TierMultipliers(e[0].Value, e[1].Value, e[2].Value, e[3].Value);
        }
    }
}
