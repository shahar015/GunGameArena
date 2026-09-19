using System;
using System.Collections.Generic;

namespace GunGameArena.Core
{
    public static class HunterPicker
    {
        public static int Count(int hostileCount, float share)
        {
            if (hostileCount <= 0 || share <= 0f) return 0;
            return Math.Min(hostileCount, (int)Math.Ceiling(share * hostileCount));
        }

        public static List<Contestant> Pick(Random rng, IList<Contestant> hostileToPlayer, float share)
        {
            var result = new List<Contestant>();
            int count = Count(hostileToPlayer.Count, share);
            var pool = new List<Contestant>(hostileToPlayer);
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int idx = rng.Next(pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            return result;
        }
    }
}
