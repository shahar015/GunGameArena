using System;
using System.Collections.Generic;

namespace GunGameArena.Core
{
    /// <summary>Roblox-style usernames, unique per instance. Words are kept to 7 chars so
    /// every decorator fits in MaxLength.</summary>
    public class NameGenerator
    {
        public const int MaxLength = 16;

        private static readonly string[] Adjectives =
        {
            "Spicy", "Crispy", "Smoky", "Salty", "Angry", "Sneaky", "Turbo", "Mega", "Tiny", "Chunky",
            "Greasy", "Soggy", "Frozen", "Rusty", "Shiny", "Lucky", "Silent", "Loud", "Toxic", "Cursed",
            "Epic", "Dank", "Sus", "Cool", "Evil", "Happy", "Fried", "Grilled", "Juicy", "Pickled",
            "Rapid", "Sleepy", "Ghost", "Iron", "Neon", "Pixel", "Retro", "Wild", "Zesty", "Ultra"
        };

        private static readonly string[] Nouns =
        {
            "Glizzy", "Wiener", "Sosig", "Mustard", "Brat", "Hotdog", "Ketchup", "Relish", "Bun", "Kebab",
            "Sniper", "Ninja", "Gamer", "Toaster", "Goblin", "Wizard", "Pirate", "Knight", "Robot", "Duck",
            "Potato", "Pickle", "Noodle", "Waffle", "Nugget", "Bacon", "Salami", "Chorizo", "Frank", "Dog",
            "Slayer", "Hunter", "Camper", "Rusher", "Boomer", "Zoomer", "Gremlin", "Meatman", "Tank", "Yeet"
        };

        private readonly Random _rng;
        private readonly HashSet<string> _used = new HashSet<string>();

        public NameGenerator(int seed)
        {
            _rng = seed == 0 ? new Random() : new Random(seed);
        }

        public string Next()
        {
            for (int attempt = 0; attempt < 50; attempt++)
            {
                string n = Compose();
                if (n.Length <= MaxLength && _used.Add(n)) return n;
            }
            while (true)
            {
                string noun = Pick(Nouns);
                if (noun.Length > MaxLength - 4) noun = noun.Substring(0, MaxLength - 4);
                string n = noun + _rng.Next(1000, 9999);
                if (_used.Add(n)) return n;
            }
        }

        private string Compose()
        {
            string adj = Pick(Adjectives);
            string noun = Pick(Nouns);
            switch (_rng.Next(7))
            {
                case 0: return adj + noun;
                case 1: return adj.ToLowerInvariant() + "_" + noun.ToLowerInvariant();
                case 2: return "xX_" + noun + "_Xx";
                case 3: return noun + _rng.Next(10, 9999);
                case 4: return adj + noun + _rng.Next(10, 99);
                case 5: return "iL" + noun;
                default: return noun + "YT";
            }
        }

        private string Pick(string[] words) { return words[_rng.Next(words.Length)]; }
    }
}
