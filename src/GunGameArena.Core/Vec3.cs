using System;

namespace GunGameArena.Core
{
    /// <summary>Minimal float3 so Core never depends on UnityEngine.</summary>
    public struct Vec3
    {
        public float X;
        public float Y;
        public float Z;

        public Vec3(float x, float y, float z) { X = x; Y = y; Z = z; }

        public static readonly Vec3 Zero = new Vec3(0f, 0f, 0f);

        public bool IsZero { get { return X == 0f && Y == 0f && Z == 0f; } }

        public static float Distance(Vec3 a, Vec3 b)
        {
            float dx = a.X - b.X, dy = a.Y - b.Y, dz = a.Z - b.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public override string ToString() { return "(" + X + ", " + Y + ", " + Z + ")"; }
    }
}
