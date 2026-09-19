using System;
using System.Collections.Generic;

namespace GunGameArena.Core
{
    public static class RivalSelector
    {
        public static List<Contestant> Pick(Random rng, Contestant self, IEnumerable<Contestant> all, int count, float radius, float playerWeight)
        {
            var pool = new List<Contestant>();
            var far = new List<Contestant>();
            foreach (var c in all)
            {
                if (c == null || c == self || c.Id == self.Id || !c.IsAlive) continue;
                if (c.IsPlayer || Vec3.Distance(c.Position, self.Position) <= radius) pool.Add(c);
                else far.Add(c);
            }
            if (pool.Count < count && far.Count > 0)
            {
                far.Sort((a, b) => Vec3.Distance(a.Position, self.Position).CompareTo(Vec3.Distance(b.Position, self.Position)));
                for (int i = 0; i < far.Count && pool.Count < count; i++) pool.Add(far[i]);
            }

            var result = new List<Contestant>();
            while (result.Count < count && pool.Count > 0)
            {
                float total = 0f;
                for (int i = 0; i < pool.Count; i++) total += pool[i].IsPlayer ? playerWeight : 1f;
                double r = rng.NextDouble() * total;
                float acc = 0f;
                int chosen = pool.Count - 1;
                for (int i = 0; i < pool.Count; i++)
                {
                    acc += pool[i].IsPlayer ? playerWeight : 1f;
                    if (r < acc) { chosen = i; break; }
                }
                result.Add(pool[chosen]);
                pool.RemoveAt(chosen);
            }
            return result;
        }
    }
}
