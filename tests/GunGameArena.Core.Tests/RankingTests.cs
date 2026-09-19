using GunGameArena.Core;
using Xunit;

public class RankingTests
{
    static Contestant C(int id, int kills, float lastKill = 0f, bool player = false, int team = 1)
        => new Contestant { Id = id, Name = "c" + id, Kills = kills, LastKillTime = lastKill, IsPlayer = player, TeamIndex = team, IsAlive = true };

    [Fact]
    public void Sort_orders_by_kills_desc_then_earliest_kill_then_id()
    {
        var sorted = Ranking.Sort(new[] { C(1, 2, 30f), C(2, 5), C(3, 2, 10f), C(4, 2, 10f) });
        Assert.Equal(new[] { 2, 3, 4, 1 }, sorted.Select(c => c.Id).ToArray());
    }

    [Fact]
    public void Visible_returns_top_n_when_player_is_inside()
    {
        var sorted = Ranking.Sort(new[] { C(1, 9), C(2, 8, player: true), C(3, 7), C(4, 6), C(5, 5), C(6, 4), C(7, 3) });
        var visible = Ranking.Visible(sorted, 5);
        Assert.Equal(5, visible.Count);
        Assert.Contains(visible, c => c.IsPlayer);
    }

    [Fact]
    public void Visible_pins_player_at_end_when_outside_top_n()
    {
        var sorted = Ranking.Sort(new[] { C(1, 9), C(2, 8), C(3, 7), C(4, 6), C(5, 5), C(6, 4), C(7, 0, player: true) });
        var visible = Ranking.Visible(sorted, 5);
        Assert.Equal(6, visible.Count);
        Assert.True(visible[5].IsPlayer);
        Assert.True(Ranking.IsPinnedPlayer(visible, 5, visible[5]));
        Assert.False(Ranking.IsPinnedPlayer(visible, 5, visible[0]));
    }

    [Fact]
    public void FFA_crowns_only_rank_one_and_only_with_kills()
    {
        var sorted = Ranking.Sort(new[] { C(1, 3), C(2, 3, 5f), C(3, 1) });
        var crowned = Ranking.CrownedIds(sorted, TeamMode.FreeForAll);
        Assert.Single(crowned);
        Assert.Contains(sorted[0].Id, crowned);

        var none = Ranking.CrownedIds(Ranking.Sort(new[] { C(1, 0), C(2, 0) }), TeamMode.FreeForAll);
        Assert.Empty(none);
    }

    [Fact]
    public void Teams_crowns_best_of_each_team_with_kills_only()
    {
        var sorted = Ranking.Sort(new[] { C(1, 4, team: 1), C(2, 3, team: 0, player: true), C(3, 2, team: 1), C(4, 0, team: 2) });
        var crowned = Ranking.CrownedIds(sorted, TeamMode.Teams);
        Assert.Equal(2, crowned.Count);
        Assert.Contains(1, crowned);
        Assert.Contains(2, crowned);
        Assert.DoesNotContain(4, crowned);
    }
}
