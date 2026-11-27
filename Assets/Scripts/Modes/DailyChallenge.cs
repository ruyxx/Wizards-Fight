// Dnevni izazov: Endless sa dva posebna pravila koja zavise od datuma, pa su ista za ceo dan.
// Svaki dan ima svoju rang listu najdaljih talasa.
using System;
using System.Collections.Generic;

namespace AWizardsFight
{
    public enum DailyRule { Frenzy, GlassCannon, Horde, EliteRush, NoPotions, Juggernauts }

    public static class DailyChallenge
    {
        public static bool Active { get; set; }

        public static int Seed => int.Parse(DateTime.Now.ToString("yyyyMMdd"));
        public static string Key => "Daily" + Seed;

        public static readonly Dictionary<DailyRule, (string title, string text)> Info = new Dictionary<DailyRule, (string, string)>
        {
            { DailyRule.Frenzy, ("FRENZY", "Enemies move 30% faster") },
            { DailyRule.GlassCannon, ("GLASS CANNON", "You deal +50% damage but have half health") },
            { DailyRule.Horde, ("HORDE", "Every wave has 50% more enemies") },
            { DailyRule.EliteRush, ("ELITE RUSH", "Elite enemies are three times as common") },
            { DailyRule.NoPotions, ("DRY SPELL", "Waves never reward potions") },
            { DailyRule.Juggernauts, ("JUGGERNAUTS", "Enemies have 40% more health") }
        };

        // Dva različita pravila izabrana iz datuma.
        public static DailyRule[] Rules
        {
            get
            {
                var random = new Random(Seed);
                int count = Enum.GetValues(typeof(DailyRule)).Length;
                int first = random.Next(count), second = (first + 1 + random.Next(count - 1)) % count;
                return new[] { (DailyRule)first, (DailyRule)second };
            }
        }

        public static bool Has(DailyRule rule) => Active && Array.IndexOf(Rules, rule) >= 0;

        public static string Description()
        {
            var lines = new List<string> { "<color=#FFD966>" + DateTime.Now.ToString("dd.MM.yyyy") + "</color>  -  the same challenge for everyone today", "" };
            foreach (var rule in Rules)
                lines.Add("<color=#FF9A5C><b>" + Info[rule].title + "</b></color>   " + Info[rule].text);
            var best = Leaderboard.Waves(Key);
            lines.Add("");
            lines.Add(best.Count > 0 ? "Your best today: wave " + best[0] : "You have not played today's challenge yet.");
            return string.Join("\n", lines);
        }
    }
}
