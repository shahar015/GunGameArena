using System.Collections.Generic;

namespace GunGameArena.Core
{
    public static class KillAttribution
    {
        /// <summary>The living contestant with the killer's IFF nearest to where the fatal
        /// shot came from. Returns null when nobody qualifies.</summary>
        public static Contestant Nearest(IEnumerable<Contestant> candidates, int killerIff, int victimId, Vec3 point)
        {
            if (killerIff < 0) return null;
            Contestant best = null;
            float bestDist = float.MaxValue;
            foreach (var c in candidates)
            {
                if (c == null || !c.IsAlive || c.Iff != killerIff || c.Id == victimId) continue;
                float d = Vec3.Distance(c.Position, point);
                if (d < bestDist) { bestDist = d; best = c; }
            }
            return best;
        }
    }
}
