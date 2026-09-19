namespace GunGameArena.Core
{
    /// <summary>One leaderboard row. Persists for the whole round; a respawned sosig
    /// re-binds to the same contestant.</summary>
    public class Contestant
    {
        public int Id;
        public string Name;
        public int TeamIndex;      // 0 = player's team
        public int Iff;
        public int Kills;
        public float LastKillTime; // game time of most recent kill; tie-break
        public bool IsPlayer;
        public bool IsAlive;
        public Vec3 Position;      // refreshed by the plugin before attribution
        public SkillTier Tier = SkillTier.Regular;

        public void AddKill(float time)
        {
            Kills++;
            LastKillTime = time;
        }

        public override string ToString() { return Name + "#" + Id + " t" + TeamIndex + " iff" + Iff + " k" + Kills; }
    }
}
