using GunGameArena.Core;
using Xunit;

public class SkillTierTests
{
    [Fact]
    public void Default_multipliers_match_spec()
    {
        var r = TierTable.Default(SkillTier.Rookie);
        Assert.Equal(2.5f, r.Spread); Assert.Equal(2.0f, r.FireAngle); Assert.Equal(1.3f, r.Refire); Assert.Equal(0.6f, r.Reaction);
        var e = TierTable.Default(SkillTier.Elite);
        Assert.Equal(0.45f, e.Spread); Assert.Equal(0.6f, e.FireAngle); Assert.Equal(0.8f, e.Refire); Assert.Equal(1.6f, e.Reaction);
        Assert.Equal(1f, TierTable.Default(SkillTier.Regular).Spread);
    }

    [Fact]
    public void Chevrons_are_one_to_four()
    {
        Assert.Equal(1, TierTable.Chevrons(SkillTier.Rookie));
        Assert.Equal(4, TierTable.Chevrons(SkillTier.Elite));
    }

    [Fact]
    public void Roll_respects_weights_over_many_samples()
    {
        var rng = new Random(1);
        var counts = new int[4];
        for (int i = 0; i < 10000; i++) counts[(int)TierRoller.Roll(rng, new[] { 30, 40, 20, 10 })]++;
        Assert.InRange(counts[0], 2600, 3400);
        Assert.InRange(counts[1], 3600, 4400);
        Assert.InRange(counts[2], 1600, 2400);
        Assert.InRange(counts[3], 700, 1300);
    }

    [Fact]
    public void Roll_with_bad_weights_returns_regular()
    {
        Assert.Equal(SkillTier.Regular, TierRoller.Roll(new Random(1), null));
        Assert.Equal(SkillTier.Regular, TierRoller.Roll(new Random(1), new[] { 0, 0, 0, 0 }));
        Assert.Equal(SkillTier.Regular, TierRoller.Roll(new Random(1), new[] { 1, 2 }));
    }

    [Fact]
    public void ParseWeights_reads_csv_and_falls_back_to_default()
    {
        Assert.Equal(new[] { 1, 2, 3, 4 }, TierRoller.ParseWeights("1, 2,3 ,4"));
        Assert.Equal(TierTable.DefaultWeights, TierRoller.ParseWeights("garbage"));
    }
}
