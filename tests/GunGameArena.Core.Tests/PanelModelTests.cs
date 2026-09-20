using GunGameArena.Core;
using Xunit;

public class PanelModelTests
{
    [Fact]
    public void Mode_cycles_forward_and_backward_with_wrap()
    {
        Assert.Equal(TeamMode.FreeForAll, PanelModel.CycleMode(TeamMode.Off, +1));
        Assert.Equal(TeamMode.Teams, PanelModel.CycleMode(TeamMode.FreeForAll, +1));
        Assert.Equal(TeamMode.Off, PanelModel.CycleMode(TeamMode.Teams, +1));
        Assert.Equal(TeamMode.Teams, PanelModel.CycleMode(TeamMode.Off, -1));
    }

    [Fact]
    public void Mode_labels_are_human_readable()
    {
        Assert.Equal("Free For All", PanelModel.ModeLabel(TeamMode.FreeForAll));
        Assert.Equal("Teams", PanelModel.ModeLabel(TeamMode.Teams));
        Assert.Equal("Off", PanelModel.ModeLabel(TeamMode.Off));
    }

    [Fact]
    public void Team_count_clamps_between_2_and_4()
    {
        Assert.Equal(3, PanelModel.StepTeamCount(2, +1));
        Assert.Equal(4, PanelModel.StepTeamCount(4, +1));
        Assert.Equal(2, PanelModel.StepTeamCount(2, -1));
    }

    [Fact]
    public void Allies_step_from_auto_through_numbers_and_clamp()
    {
        Assert.Equal(0, PanelModel.StepAllies(-1, +1));
        Assert.Equal(-1, PanelModel.StepAllies(0, -1));
        Assert.Equal(-1, PanelModel.StepAllies(-1, -1));
        Assert.Equal(PanelModel.MaxAllies, PanelModel.StepAllies(PanelModel.MaxAllies, +1));
        Assert.Equal("Auto", PanelModel.AlliesLabel(-1));
        Assert.Equal("4", PanelModel.AlliesLabel(4));
    }

    [Fact]
    public void Hunter_share_steps_by_five_percent_and_clamps()
    {
        Assert.Equal(0.30f, PanelModel.StepHunterShare(0.25f, +1), 3);
        Assert.Equal(0f, PanelModel.StepHunterShare(0.03f, -1), 3);
        Assert.Equal(1f, PanelModel.StepHunterShare(0.98f, +1), 3);
        Assert.Equal("25%", PanelModel.PercentLabel(0.25f));
        Assert.Equal("100%", PanelModel.PercentLabel(1f));
    }

    [Fact]
    public void Toggle_label_and_team_rows()
    {
        Assert.Equal("Hunters: ON", PanelModel.ToggleLabel("Hunters", true));
        Assert.Equal("Grudges: OFF", PanelModel.ToggleLabel("Grudges", false));
        Assert.True(PanelModel.TeamRowsEnabled(TeamMode.Teams));
        Assert.False(PanelModel.TeamRowsEnabled(TeamMode.FreeForAll));
    }
}
