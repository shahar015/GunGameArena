using GunGameArena.Core;
using Xunit;

public class HudPaletteTests
{
    [Fact]
    public void Hex_parses_rrggbb()
    {
        var c = Rgba.Hex("F5C542");
        Assert.Equal(0xF5 / 255f, c.R, 3);
        Assert.Equal(0xC5 / 255f, c.G, 3);
        Assert.Equal(0x42 / 255f, c.B, 3);
        Assert.Equal(1f, c.A);
    }

    [Fact]
    public void Team_colours_follow_spec_order()
    {
        Assert.Equal(HudPalette.TeamBlue, HudPalette.TeamColor(0));
        Assert.Equal(HudPalette.TeamRed, HudPalette.TeamColor(1));
        Assert.Equal(HudPalette.TeamGreen, HudPalette.TeamColor(2));
        Assert.Equal(HudPalette.TeamYellow, HudPalette.TeamColor(3));
        Assert.Equal(HudPalette.TeamYellow, HudPalette.TeamColor(9));
    }

    [Fact]
    public void Ffa_border_is_gold_only_for_rank_one()
    {
        Assert.Equal(HudPalette.Gold, HudPalette.CardBorder(TeamMode.FreeForAll, 3, true));
        Assert.Equal(HudPalette.FfaBorder, HudPalette.CardBorder(TeamMode.FreeForAll, 3, false));
        Assert.Equal(HudPalette.TeamRed, HudPalette.CardBorder(TeamMode.Teams, 1, true));
    }
}
