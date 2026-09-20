using System.Collections.Generic;

namespace GunGameArena.Core
{
    public static class TeamScore
    {
        public static int Total(IEnumerable<Contestant> contestants, int teamIndex)
        {
            int sum = 0;
            foreach (var c in contestants) if (c != null && c.TeamIndex == teamIndex) sum += c.Kills;
            return sum;
        }

        /// <summary>First team (lowest index) whose total reaches pointsToWin, else -1.</summary>
        public static int Winner(IEnumerable<Contestant> contestants, int teamCount, int pointsToWin)
        {
            var list = new List<Contestant>(contestants);
            for (int t = 0; t < teamCount; t++)
                if (Total(list, t) >= pointsToWin) return t;
            return -1;
        }
    }
}
