using GunGameArena.Core;
using Xunit;

public class NameGeneratorTests
{
    [Fact]
    public void Generates_unique_names_within_length()
    {
        var gen = new NameGenerator(1234);
        var seen = new HashSet<string>();
        for (int i = 0; i < 32; i++)
        {
            string n = gen.Next();
            Assert.False(string.IsNullOrEmpty(n));
            Assert.True(n.Length <= NameGenerator.MaxLength, n);
            Assert.True(seen.Add(n), "duplicate " + n);
        }
    }

    [Fact]
    public void Same_seed_gives_same_sequence()
    {
        var a = new NameGenerator(42);
        var b = new NameGenerator(42);
        for (int i = 0; i < 10; i++) Assert.Equal(a.Next(), b.Next());
    }

    [Fact]
    public void Keeps_producing_after_many_names()
    {
        var gen = new NameGenerator(7);
        var seen = new HashSet<string>();
        for (int i = 0; i < 500; i++) Assert.True(seen.Add(gen.Next()));
    }
}
