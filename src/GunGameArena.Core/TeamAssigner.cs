using System;

namespace GunGameArena.Core
{
    /// <summary>Maps a roster slot to a team index and an H3VR IFF code.
    /// Team index 0 is always the player's team; 1.. are enemy teams.</summary>
    public static class TeamAssigner
    {
        public const int MaxIff = 31;               // IFFChart is bool[32]
        public const int OriginalGunGameIff = 1;    // value on every GunGame spawner prefab

        public static int ResolveAllyCount(int allySetting, int sosigCount)
        {
            if (sosigCount <= 0) return 0;
            int allies = allySetting < 0 ? sosigCount / 2 : allySetting;
            return Math.Max(0, Math.Min(allies, sosigCount - 1));
        }

        public static int ClampTeamCount(int teamCount)
        {
            return Math.Max(2, Math.Min(4, teamCount));
        }

        public static int TeamIndexFor(TeamMode mode, int slot, int sosigCount, int teamCount, int allySetting)
        {
            switch (mode)
            {
                case TeamMode.FreeForAll:
                    return slot + 1;
                case TeamMode.Teams:
                {
                    int allies = ResolveAllyCount(allySetting, sosigCount);
                    if (slot < allies) return 0;
                    int enemyTeams = ClampTeamCount(teamCount) - 1;
                    return 1 + (slot - allies) % enemyTeams;
                }
                default:
                    return 1;
            }
        }

        public static int IffFor(TeamMode mode, int teamIndex, int playerIff)
        {
            switch (mode)
            {
                case TeamMode.FreeForAll:
                {
                    int iff = Math.Max(1, teamIndex);
                    if (playerIff >= 1 && iff >= playerIff) iff += 1;   // skip the player's IFF, keep all sosig IFFs distinct
                    return Math.Min(iff, MaxIff);
                }
                case TeamMode.Teams:
                {
                    if (teamIndex == 0) return playerIff;
                    int iff = teamIndex;
                    if (iff == playerIff) iff = MaxIff;
                    return iff;
                }
                default:
                    return OriginalGunGameIff;
            }
        }
    }
}
