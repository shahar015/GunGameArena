using GunGameArena.Core;
using Xunit;

public class Vec3Tests
{
    [Fact]
    public void Distance_is_euclidean()
    {
        var a = new Vec3(0, 0, 0);
        var b = new Vec3(3, 4, 0);
        Assert.Equal(5f, Vec3.Distance(a, b), 4);
    }

    [Fact]
    public void Zero_reports_IsZero()
    {
        Assert.True(Vec3.Zero.IsZero);
        Assert.False(new Vec3(0, 1, 0).IsZero);
    }
}
