using GunGameArena.Core;
using Xunit;

public class TeamScoreTests
{
    static Contestant C(int id, int team, int kills, bool player = false)
        => new Contestant { Id = id, TeamIndex = team, Kills = kills, IsPlayer = player, IsAlive = true };

    [Fact]
    public void Total_sums_kills_of_a_team_including_the_player()
    {
        var all = new[] { C(0, 0, 4, player: true), C(1, 0, 3), C(2, 1, 9), C(3, 2, 1) };
        Assert.Equal(7, TeamScore.Total(all, 0));
        Assert.Equal(9, TeamScore.Total(all, 1));
        Assert.Equal(0, TeamScore.Total(all, 3));
    }

    [Fact]
    public void Winner_is_minus_one_until_a_team_reaches_the_target()
    {
        var all = new[] { C(0, 0, 10, player: true), C(1, 1, 12) };
        Assert.Equal(-1, TeamScore.Winner(all, 2, 30));
        Assert.Equal(1, TeamScore.Winner(all, 2, 12));
    }

    [Fact]
    public void Winner_prefers_lowest_team_index_on_ties()
    {
        var all = new[] { C(0, 0, 20, player: true), C(1, 1, 20) };
        Assert.Equal(0, TeamScore.Winner(all, 2, 20));
    }

    [Fact]
    public void Team_names_follow_palette_order()
    {
        Assert.Equal("BLUE", HudPalette.TeamName(0));
        Assert.Equal("RED", HudPalette.TeamName(1));
        Assert.Equal("GREEN", HudPalette.TeamName(2));
        Assert.Equal("YELLOW", HudPalette.TeamName(3));
        Assert.Equal("YELLOW", HudPalette.TeamName(7));
    }

    [Fact]
    public void Points_to_win_steps_by_five_and_clamps()
    {
        Assert.Equal(35, PanelModel.StepPointsToWin(30, +1));
        Assert.Equal(5, PanelModel.StepPointsToWin(5, -1));
        Assert.Equal(200, PanelModel.StepPointsToWin(200, +1));
    }
}
