using System;

namespace GunGameArena.Core
{
    /// <summary>Snapshot of the config values the in-map panel edits.</summary>
    public class PanelState
    {
        public TeamMode Mode;
        public int TeamCount;
        public int AllySosigs;
        public bool Leaderboard;
        public bool SpreadSpawns;
        public bool Grudges;
        public bool Hunters;
        public bool SkillTiers;
        public float HunterShare;
    }

    /// <summary>Pure label/step logic for the in-map settings panel.</summary>
    public static class PanelModel
    {
        public const int MaxAllies = 9;
        public const float HunterStep = 0.05f;
        public const int PointsStep = 5;
        public const int MinPoints = 5;
        public const int MaxPoints = 200;

        public static string ModeLabel(TeamMode m)
        {
            switch (m)
            {
                case TeamMode.FreeForAll: return "Free For All";
                case TeamMode.Teams: return "Teams";
                default: return "Off";
            }
        }

        public static TeamMode CycleMode(TeamMode m, int dir)
        {
            int n = ((int)m + (dir >= 0 ? 1 : -1) + 3) % 3;
            return (TeamMode)n;
        }

        public static int StepTeamCount(int current, int dir)
        {
            return Math.Max(2, Math.Min(4, current + (dir >= 0 ? 1 : -1)));
        }

        public static int StepAllies(int current, int dir)
        {
            return Math.Max(-1, Math.Min(MaxAllies, current + (dir >= 0 ? 1 : -1)));
        }

        public static string AlliesLabel(int allies)
        {
            return allies < 0 ? "Auto" : allies.ToString();
        }

        public static float StepHunterShare(float current, int dir)
        {
            float v = current + (dir >= 0 ? HunterStep : -HunterStep);
            v = Math.Max(0f, Math.Min(1f, v));
            return (float)Math.Round(v, 2);
        }

        public static string PercentLabel(float share)
        {
            return ((int)Math.Round(share * 100f)).ToString() + "%";
        }

        public static string ToggleLabel(string name, bool on)
        {
            return name + ": " + (on ? "ON" : "OFF");
        }

        public static int StepPointsToWin(int current, int dir)
        {
            return Math.Max(MinPoints, Math.Min(MaxPoints, current + (dir >= 0 ? PointsStep : -PointsStep)));
        }

        public static bool TeamRowsEnabled(TeamMode m)
        {
            return m == TeamMode.Teams;
        }
    }
}
