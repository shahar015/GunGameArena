using GunGameArena.Core;
using Xunit;

public class BehaviourPickersTests
{
    static Contestant C(int id, float x, bool player = false, bool alive = true)
        => new Contestant { Id = id, Iff = id, Position = new Vec3(x, 0, 0), IsPlayer = player, IsAlive = alive };

    [Fact]
    public void Rivals_never_include_self_or_dead_and_respect_count()
    {
        var self = C(1, 0);
        var all = new[] { self, C(2, 5), C(3, 6, alive: false), C(4, 7), C(5, 8), C(0, 9, player: true) };
        var rivals = RivalSelector.Pick(new Random(3), self, all, 3, 40f, 2f);
        Assert.Equal(3, rivals.Count);
        Assert.DoesNotContain(rivals, r => r.Id == 1);
        Assert.DoesNotContain(rivals, r => r.Id == 3);
        Assert.Equal(3, rivals.Select(r => r.Id).Distinct().Count());
    }

    [Fact]
    public void Rivals_prefer_inside_radius_but_fill_from_nearest_outside()
    {
        var self = C(1, 0);
        var all = new[] { self, C(2, 5), C(3, 100), C(4, 200) };
        var rivals = RivalSelector.Pick(new Random(3), self, all, 2, 10f, 1f);
        Assert.Contains(rivals, r => r.Id == 2);
        Assert.Contains(rivals, r => r.Id == 3);
    }

    [Fact]
    public void Player_is_always_eligible_and_weighted()
    {
        var self = C(1, 0);
        var all = new[] { self, C(2, 5), C(0, 500, player: true) };
        int playerPicked = 0;
        for (int seed = 0; seed < 200; seed++)
            if (RivalSelector.Pick(new Random(seed), self, all, 1, 10f, 3f).Any(r => r.IsPlayer)) playerPicked++;
        Assert.InRange(playerPicked, 120, 180); // weight 3 vs 1 → ~75%
    }

    [Fact]
    public void Hunter_count_is_ceil_of_share()
    {
        Assert.Equal(2, HunterPicker.Count(8, 0.25f));
        Assert.Equal(2, HunterPicker.Count(5, 0.25f));
        Assert.Equal(0, HunterPicker.Count(0, 0.25f));
        Assert.Equal(8, HunterPicker.Count(8, 5f));
    }

    [Fact]
    public void Hunter_pick_returns_distinct_subset()
    {
        var pool = Enumerable.Range(1, 8).Select(i => C(i, i)).ToList();
        var picked = HunterPicker.Pick(new Random(9), pool, 0.5f);
        Assert.Equal(4, picked.Count);
        Assert.Equal(4, picked.Select(p => p.Id).Distinct().Count());
    }

    [Fact]
    public void SpawnerChooser_picks_farthest_from_everyone_within_allowed_band()
    {
        // spawners on a line; player at 0; sosig at 100
        var spawners = new List<Vec3> { new Vec3(1, 0, 0), new Vec3(2, 0, 0), new Vec3(50, 0, 0), new Vec3(99, 0, 0) };
        var occupied = new List<Vec3> { new Vec3(100, 0, 0) };
        int idx = SpawnerChooser.Choose(new Random(1), spawners, ignoreNear: 2, ignoreFar: 0, new Vec3(0, 0, 0), occupied);
        Assert.Equal(2, idx); // 50 is far from player (50) and sosig (50); 99 is 1 from sosig
    }

    [Fact]
    public void SpawnerChooser_falls_back_when_band_is_empty_and_handles_no_spawners()
    {
        var spawners = new List<Vec3> { new Vec3(1, 0, 0), new Vec3(2, 0, 0) };
        int idx = SpawnerChooser.Choose(new Random(1), spawners, 5, 5, Vec3.Zero, new List<Vec3>());
        Assert.InRange(idx, 0, 1);
        Assert.Equal(-1, SpawnerChooser.Choose(new Random(1), new List<Vec3>(), 2, 0, Vec3.Zero, new List<Vec3>()));
    }
}
