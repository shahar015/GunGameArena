using GunGameArena.Core;
using Xunit;

public class TeamAssignerTests
{
    [Fact]
    public void Off_keeps_original_gungame_iff_for_every_slot()
    {
        for (int slot = 0; slot < 8; slot++)
        {
            int team = TeamAssigner.TeamIndexFor(TeamMode.Off, slot, 8, 2, -1);
            Assert.Equal(1, TeamAssigner.IffFor(TeamMode.Off, team, 0));
        }
    }

    [Fact]
    public void FreeForAll_gives_each_slot_a_unique_iff_starting_at_one()
    {
        var seen = new HashSet<int>();
        for (int slot = 0; slot < 8; slot++)
        {
            int team = TeamAssigner.TeamIndexFor(TeamMode.FreeForAll, slot, 8, 2, -1);
            int iff = TeamAssigner.IffFor(TeamMode.FreeForAll, team, 0);
            Assert.True(seen.Add(iff));
            Assert.InRange(iff, 1, 31);
        }
    }

    [Fact]
    public void FreeForAll_never_exceeds_iff_31()
    {
        int team = TeamAssigner.TeamIndexFor(TeamMode.FreeForAll, 40, 41, 2, -1);
        Assert.Equal(31, TeamAssigner.IffFor(TeamMode.FreeForAll, team, 0));
    }

    [Fact]
    public void Teams_puts_first_allies_on_player_team_and_round_robins_the_rest()
    {
        // 8 sosigs, 3 teams, 3 allies -> slots 0..2 team 0, slots 3.. alternate teams 1,2
        int[] expected = { 0, 0, 0, 1, 2, 1, 2, 1 };
        for (int slot = 0; slot < 8; slot++)
            Assert.Equal(expected[slot], TeamAssigner.TeamIndexFor(TeamMode.Teams, slot, 8, 3, 3));
    }

    [Fact]
    public void Teams_default_ally_setting_is_half_rounded_down()
    {
        Assert.Equal(4, TeamAssigner.ResolveAllyCount(-1, 8));
        Assert.Equal(3, TeamAssigner.ResolveAllyCount(-1, 7));
    }

    [Fact]
    public void Teams_always_leaves_at_least_one_enemy()
    {
        Assert.Equal(7, TeamAssigner.ResolveAllyCount(99, 8));
        Assert.Equal(0, TeamAssigner.ResolveAllyCount(0, 8));
        Assert.Equal(0, TeamAssigner.ResolveAllyCount(5, 1));
    }

    [Fact]
    public void Teams_iff_uses_player_iff_for_team_zero_and_team_index_otherwise()
    {
        Assert.Equal(0, TeamAssigner.IffFor(TeamMode.Teams, 0, 0));
        Assert.Equal(2, TeamAssigner.IffFor(TeamMode.Teams, 2, 0));
    }

    [Fact]
    public void TeamCount_is_clamped_between_2_and_4()
    {
        Assert.Equal(2, TeamAssigner.ClampTeamCount(1));
        Assert.Equal(4, TeamAssigner.ClampTeamCount(9));
        Assert.Equal(3, TeamAssigner.ClampTeamCount(3));
    }

    [Fact]
    public void FreeForAll_never_hands_a_sosig_the_player_iff()
    {
        for (int slot = 0; slot < 8; slot++)
        {
            int team = TeamAssigner.TeamIndexFor(TeamMode.FreeForAll, slot, 8, 2, -1);
            Assert.NotEqual(3, TeamAssigner.IffFor(TeamMode.FreeForAll, team, 3));
        }
    }

    [Fact]
    public void Teams_enemy_team_never_shares_the_player_iff()
    {
        Assert.Equal(2, TeamAssigner.IffFor(TeamMode.Teams, 0, 2));
        Assert.NotEqual(2, TeamAssigner.IffFor(TeamMode.Teams, 2, 2));
        Assert.Equal(1, TeamAssigner.IffFor(TeamMode.Teams, 1, 2));
    }
}
