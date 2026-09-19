using System;

namespace GunGameArena.Core
{
    public enum SkillTier { Rookie = 0, Regular = 1, Veteran = 2, Elite = 3 }

    public struct TierMultipliers
    {
        public float Spread;     // SosigWeapon.ProjectileSpread
        public float FireAngle;  // SosigWeapon.MaxAngularFireRange
        public float Refire;     // SosigWeapon.Usage_RefireRange
        public float Reaction;   // Sosig recognition / identification speed
        public TierMultipliers(float spread, float fireAngle, float refire, float reaction)
        { Spread = spread; FireAngle = fireAngle; Refire = refire; Reaction = reaction; }
    }

    public static class TierTable
    {
        public static readonly int[] DefaultWeights = { 30, 40, 20, 10 };

        public static TierMultipliers Default(SkillTier tier)
        {
            switch (tier)
            {
                case SkillTier.Rookie: return new TierMultipliers(2.5f, 2.0f, 1.3f, 0.6f);
                case SkillTier.Veteran: return new TierMultipliers(0.7f, 0.8f, 0.9f, 1.3f);
                case SkillTier.Elite: return new TierMultipliers(0.45f, 0.6f, 0.8f, 1.6f);
                default: return new TierMultipliers(1f, 1f, 1f, 1f);
            }
        }

        public static int Chevrons(SkillTier tier) { return (int)tier + 1; }
    }

    public static class TierRoller
    {
        public static SkillTier Roll(Random rng, int[] weights)
        {
            if (weights == null || weights.Length != 4) return SkillTier.Regular;
            int sum = 0;
            for (int i = 0; i < 4; i++) sum += Math.Max(0, weights[i]);
            if (sum <= 0) return SkillTier.Regular;
            int r = rng.Next(sum);
            int acc = 0;
            for (int i = 0; i < 4; i++)
            {
                acc += Math.Max(0, weights[i]);
                if (r < acc) return (SkillTier)i;
            }
            return SkillTier.Regular;
        }

        /// <summary>"30,40,20,10" → int[4]; anything malformed → TierTable.DefaultWeights.</summary>
        public static int[] ParseWeights(string csv)
        {
            if (string.IsNullOrEmpty(csv)) return TierTable.DefaultWeights;
            string[] parts = csv.Split(',');
            if (parts.Length != 4) return TierTable.DefaultWeights;
            var result = new int[4];
            for (int i = 0; i < 4; i++)
            {
                int v;
                if (!int.TryParse(parts[i].Trim(), out v)) return TierTable.DefaultWeights;
                result[i] = v;
            }
            return result;
        }
    }
}
