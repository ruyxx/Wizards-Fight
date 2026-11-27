// Stablo veština: tri škole (Piromantija, Kriomantija, Arkana) sa po četiri nivoa i spojne veštine
// koje traže ulaganje u dve škole i prave kombinacije (npr. vatra po zaleđenom protivniku = Termalni šok).
// Poena ima malo (jedan po nivou lika), pa igrač mora da bira smer.
using System;
using UnityEngine;

namespace AWizardsFight
{
    public sealed class SkillNode
    {
        public readonly string Id, Title, Description;
        public readonly int Max, School, Tier, FusionWith;

        public SkillNode(string id, string title, int max, int school, int tier, string description, int fusionWith = -1)
        {
            Id = id;
            Title = title;
            Max = max;
            School = school;
            Tier = tier;
            Description = description;
            FusionWith = fusionWith;
        }

        public bool IsFusion => FusionWith >= 0;
    }

    public static class SkillTree
    {
        public const int Pyromancy = 0, Cryomancy = 1, Arcana = 2;
        // Koliko poena u školi treba za svaki nivo; spojne veštine traže po 4 u obe škole.
        public static readonly int[] TierRequirement = { 0, 3, 6, 10 };
        public const int FusionRequirement = 4;

        public static readonly (string name, Color color)[] Schools =
        {
            ("PYROMANCY", new Color(1f, .5f, .25f)),
            ("CRYOMANCY", new Color(.45f, .8f, 1f)),
            ("ARCANA", new Color(.75f, .55f, 1f))
        };

        public static readonly SkillNode[] Nodes =
        {
            // Piromantija: vatra, gorenje i eksplozije.
            new SkillNode("pyro", "PYROMANCY", 3, Pyromancy, 0, "+12% Fire Staff damage per rank."),
            new SkillNode("kindling", "KINDLING", 2, Pyromancy, 0, "Any spell hit has a 10% chance per rank to IGNITE: the target burns for 40% of the hit over 3 seconds."),
            new SkillNode("power", "SPELL POWER", 3, Pyromancy, 1, "+8% damage for every spell per rank."),
            new SkillNode("searing", "SEARING", 2, Pyromancy, 1, "Burning enemies take +15% damage per rank."),
            new SkillNode("combustion", "COMBUSTION", 1, Pyromancy, 2, "Burning enemies EXPLODE when they die, hurting everything around them."),
            new SkillNode("blazingtrail", "BLAZING TRAIL", 1, Pyromancy, 2, "Your dash leaves a trail of fire that burns enemies."),
            new SkillNode("haste", "RAPID CASTING", 2, Pyromancy, 2, "Spells recover 8% faster per rank."),
            new SkillNode("inferno", "INFERNO", 1, Pyromancy, 3, "CAPSTONE: +25% damage for every spell and +20% chance to ignite."),
            // Kriomantija: usporavanje, odbrana i pokret.
            new SkillNode("frost", "DEEP FROST", 2, Cryomancy, 0, "Chills (slows) last 40% longer per rank."),
            new SkillNode("vitality", "VITALITY", 3, Cryomancy, 0, "+20 max health per rank."),
            new SkillNode("frostbite", "FROSTBITE", 3, Cryomancy, 1, "Chilled enemies take +10% damage per rank."),
            new SkillNode("icearmor", "ICE ARMOR", 2, Cryomancy, 1, "+15 armor per rank."),
            new SkillNode("shatter", "SHATTER", 1, Cryomancy, 2, "Chilled enemies SHATTER when they die, hurting and chilling enemies nearby."),
            new SkillNode("swift", "SWIFTNESS", 2, Cryomancy, 2, "+8% movement speed per rank."),
            new SkillNode("quickstep", "QUICK STEP", 2, Cryomancy, 2, "Dash recovers 15% faster per rank."),
            new SkillNode("glacialstep", "GLACIAL STEP", 1, Cryomancy, 3, "CAPSTONE: the end of every dash releases a frost nova that chills and hurts enemies."),
            // Arkana: mana, munje, otrov i sreća.
            new SkillNode("focus", "FOCUS", 3, Arcana, 0, "+15% mana regeneration per rank."),
            new SkillNode("arcane", "ARCANE RESERVE", 3, Arcana, 0, "+25 max mana per rank."),
            new SkillNode("conduit", "STATIC CONDUIT", 2, Arcana, 1, "Storm Staff lightning jumps to 1 more enemy per rank."),
            new SkillNode("virulence", "VIRULENCE", 2, Arcana, 1, "Poison deals 30% more damage per rank."),
            new SkillNode("echo", "ARCANE ECHO", 2, Arcana, 2, "8% chance per rank that a spell is cast twice."),
            new SkillNode("scholar", "SCHOLAR", 2, Arcana, 2, "+15% experience per rank."),
            new SkillNode("potion", "POTION MASTER", 2, Arcana, 2, "+1 potion now and potions heal +10 per rank."),
            new SkillNode("manashield", "MANA SHIELD", 1, Arcana, 3, "CAPSTONE: 30% of damage you take is paid with mana instead of health."),
            // Spojne veštine: kombinacije dve škole.
            new SkillNode("thermal", "THERMAL SHOCK", 1, Pyromancy, 4, "Fire + Frost: fire hits on a chilled enemy deal +60% damage and break the chill.", Cryomancy),
            new SkillNode("plaguefire", "PLAGUE FIRE", 1, Pyromancy, 4, "Fire + Arcana: burning enemies take double poison damage, and poisoned enemies spread poison when they die.", Arcana),
            new SkillNode("frozenstorm", "FROZEN STORM", 1, Cryomancy, 4, "Frost + Arcana: lightning chills what it hits, and chilled enemies take +40% lightning damage.", Arcana)
        };

        public static SkillNode Find(string id) => Array.Find(Nodes, node => node.Id == id);
    }
}
