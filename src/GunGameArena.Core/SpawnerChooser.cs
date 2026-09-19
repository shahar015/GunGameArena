using System;
using System.Collections.Generic;

namespace GunGameArena.Core
{
    public static class SpawnerChooser
    {
        /// <summary>Among spawners allowed by GunGame's near/far exclusion (ordered by distance to
        /// the player), choose the one maximising the minimum distance to the player and every
        /// occupied position. Ties broken randomly. -1 when there are no spawners.</summary>
        public static int Choose(Random rng, IList<Vec3> spawners, int ignoreNear, int ignoreFar, Vec3 player, IList<Vec3> occupied)
        {
            int n = spawners.Count;
            if (n == 0) return -1;

            var order = new List<int>(n);
            for (int i = 0; i < n; i++) order.Add(i);
            order.Sort((a, b) => Vec3.Distance(spawners[a], player).CompareTo(Vec3.Distance(spawners[b], player)));

            int start = Math.Max(0, ignoreNear);
            int end = n - Math.Max(0, ignoreFar);
            if (end - start <= 0) { start = 0; end = n; }

            float bestScore = -1f;
            var best = new List<int>();
            for (int k = start; k < end; k++)
            {
                int idx = order[k];
                float score = Vec3.Distance(spawners[idx], player);
                for (int j = 0; j < occupied.Count; j++)
                    score = Math.Min(score, Vec3.Distance(spawners[idx], occupied[j]));
                if (score > bestScore + 0.0001f) { bestScore = score; best.Clear(); best.Add(idx); }
                else if (Math.Abs(score - bestScore) <= 0.0001f) best.Add(idx);
            }
            return best[rng.Next(best.Count)];
        }
    }
}
