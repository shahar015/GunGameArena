using System;
using System.Globalization;

namespace GunGameArena.Core
{
    public struct Rgba
    {
        public float R, G, B, A;
        public Rgba(float r, float g, float b, float a) { R = r; G = g; B = b; A = a; }

        public static Rgba Hex(string rrggbb, float a = 1f)
        {
            int v = int.Parse(rrggbb.TrimStart('#'), NumberStyles.HexNumber);
            return new Rgba(((v >> 16) & 0xFF) / 255f, ((v >> 8) & 0xFF) / 255f, (v & 0xFF) / 255f, a);
        }

        public Rgba Darken(float factor) { return new Rgba(R * factor, G * factor, B * factor, A); }

        public override bool Equals(object obj)
        {
            if (!(obj is Rgba)) return false;
            var o = (Rgba)obj;
            return R == o.R && G == o.G && B == o.B && A == o.A;
        }
        public override int GetHashCode() { return R.GetHashCode() ^ G.GetHashCode() ^ B.GetHashCode() ^ A.GetHashCode(); }
        public static bool operator ==(Rgba a, Rgba b) { return a.Equals(b); }
        public static bool operator !=(Rgba a, Rgba b) { return !a.Equals(b); }
    }

    public static class HudPalette
    {
        public static readonly Rgba FfaCard = Rgba.Hex("2B2B2B");
        public static readonly Rgba FfaBorder = Rgba.Hex("555555");
        public static readonly Rgba Gold = Rgba.Hex("F5C542");
        public static readonly Rgba White = new Rgba(1, 1, 1, 1);
        public static readonly Rgba Black = new Rgba(0, 0, 0, 1);
        public static readonly Rgba HeaderBg = Rgba.Hex("1A1A1A", 0.85f);
        public static readonly Rgba TeamBlue = Rgba.Hex("1E3FA8");
        public static readonly Rgba TeamRed = Rgba.Hex("A81E1E");
        public static readonly Rgba TeamGreen = Rgba.Hex("1E8A3A");
        public static readonly Rgba TeamYellow = Rgba.Hex("C9A400");

        public static Rgba TeamColor(int teamIndex)
        {
            switch (teamIndex)
            {
                case 0: return TeamBlue;
                case 1: return TeamRed;
                case 2: return TeamGreen;
                default: return TeamYellow;
            }
        }

        public static Rgba CardBorder(TeamMode mode, int teamIndex, bool isRankOne)
        {
            if (mode == TeamMode.Teams) return TeamColor(teamIndex);
            return isRankOne ? Gold : FfaBorder;
        }

        public static Rgba CardBackground(TeamMode mode, int teamIndex)
        {
            return mode == TeamMode.Teams ? TeamColor(teamIndex).Darken(0.55f) : FfaCard;
        }
    }
}
