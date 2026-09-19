using System.Collections.Generic;

namespace GunGameArena.Core
{
    public static class Ranking
    {
        public static List<Contestant> Sort(IEnumerable<Contestant> all)
        {
            var list = new List<Contestant>(all);
            list.Sort(Compare);
            return list;
        }

        private static int Compare(Contestant a, Contestant b)
        {
            int c = b.Kills.CompareTo(a.Kills);
            if (c != 0) return c;
            c = a.LastKillTime.CompareTo(b.LastKillTime);
            if (c != 0) return c;
            return a.Id.CompareTo(b.Id);
        }

        /// <summary>Top N in order, then the player appended if not already shown.</summary>
        public static List<Contestant> Visible(IList<Contestant> sorted, int topCount)
        {
            var result = new List<Contestant>();
            for (int i = 0; i < sorted.Count && i < topCount; i++) result.Add(sorted[i]);
            for (int i = 0; i < sorted.Count; i++)
            {
                if (sorted[i].IsPlayer && !result.Contains(sorted[i])) { result.Add(sorted[i]); break; }
            }
            return result;
        }

        public static bool IsPinnedPlayer(IList<Contestant> visible, int topCount, Contestant c)
        {
            return c.IsPlayer && visible.IndexOf(c) >= topCount;
        }

        /// <summary>FFA/Off: rank 1 only. Teams: best of each team. Always requires Kills &gt; 0.</summary>
        public static HashSet<int> CrownedIds(IList<Contestant> sorted, TeamMode mode)
        {
            var crowned = new HashSet<int>();
            if (mode == TeamMode.Teams)
            {
                var seenTeams = new HashSet<int>();
                for (int i = 0; i < sorted.Count; i++)
                {
                    var c = sorted[i];
                    if (seenTeams.Add(c.TeamIndex) && c.Kills > 0) crowned.Add(c.Id);
                }
            }
            else if (sorted.Count > 0 && sorted[0].Kills > 0)
            {
                crowned.Add(sorted[0].Id);
            }
            return crowned;
        }
    }
}
