using GunGameArena.Core;
using Xunit;

public class KillAttributionTests
{
    static Contestant C(int id, int iff, float x, bool alive = true, bool player = false)
        => new Contestant { Id = id, Iff = iff, Position = new Vec3(x, 0, 0), IsAlive = alive, IsPlayer = player };

    [Fact]
    public void Picks_nearest_living_candidate_on_killer_team()
    {
        var all = new[] { C(1, 3, 10f), C(2, 3, 2f), C(3, 4, 1f) };
        var winner = KillAttribution.Nearest(all, 3, victimId: 99, new Vec3(0, 0, 0));
        Assert.Equal(2, winner.Id);
    }

    [Fact]
    public void Ignores_dead_candidates()
    {
        var all = new[] { C(1, 3, 10f), C(2, 3, 1f, alive: false) };
        Assert.Equal(1, KillAttribution.Nearest(all, 3, 99, Vec3.Zero).Id);
    }

    [Fact]
    public void Never_credits_the_victim()
    {
        var all = new[] { C(7, 3, 0f), C(1, 3, 10f) };
        Assert.Equal(1, KillAttribution.Nearest(all, 3, victimId: 7, Vec3.Zero).Id);
    }

    [Fact]
    public void Returns_null_for_negative_iff_or_no_candidates()
    {
        var all = new[] { C(1, 3, 10f) };
        Assert.Null(KillAttribution.Nearest(all, -1, 99, Vec3.Zero));
        Assert.Null(KillAttribution.Nearest(all, 5, 99, Vec3.Zero));
    }

    [Fact]
    public void Player_can_win_attribution()
    {
        var all = new[] { C(0, 0, 1f, player: true), C(1, 0, 5f) };
        Assert.True(KillAttribution.Nearest(all, 0, 99, Vec3.Zero).IsPlayer);
    }
}
