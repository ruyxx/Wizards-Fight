// Spisak nivoa, njihov redosled, otključavanje i kratka priča koju starac u kampu priča pre ulaska.
using UnityEngine;

namespace AWizardsFight
{
    public static class GameLevels
    {
        public const string CampScene = "Camp";
        private const string UnlockedKey = "UnlockedLevel";

        public sealed class Level
        {
            public readonly int Index;
            public readonly string Scene, Title, Story, Goal;
            public readonly int RecommendedLevel;

            public Level(int index, string scene, string title, int recommended, string goal, string story)
            {
                Index = index;
                Scene = scene;
                Title = title;
                RecommendedLevel = recommended;
                Goal = goal;
                Story = story;
            }
        }

        public static readonly Level[] All =
        {
            new Level(1, "Dungeon", "THE DUNGEON", 1,
                "Clear the chambers, pull the lever in chamber 14 and defeat the Dark Sorcerer.",
                "The Dark Sorcerer has sealed himself in the old dungeon beneath the tower. His guards grow stronger the deeper you go. Fight through the chambers, find the lever that opens his door and break his hold on the land."),
            new Level(2, "EmberDepths", "EMBER DEPTHS", 8,
                "Survive the lava halls, pull the lever of flame and destroy the Magma Golem.",
                "When the Sorcerer fell, the seal beneath the dungeon cracked. Heat rises from below: the old forges woke up and the Magma Golem guards them. Lava burns while you stand in it, but a quick dash carries you across. Watch the vents in the floor."),
            new Level(3, "FrozenCrypt", "FROZEN CRYPT", 16,
                "Cross the frozen halls, pull the lever of rime and shatter the Frost Lich.",
                "With the Golem destroyed, the forge stopped warming the crypt above it. The ice is spreading and the Frost Lich has woken. Ice is slippery, so plan your steps, and the blue vents chill you. The Lich teleports around its tomb: keep moving and strike when it appears."),
            new Level(4, "ShadowTemple", "SHADOW TEMPLE", 22,
                "Find your way through the dark temple, pull the lever of night and defeat the Shadow King.",
                "Every seal is broken and the last door is open. Above the crypt stands a temple built without windows, for a god who hated light. Here you see only what your own light reaches. The Shadow King charges, fills the air with spirals of darkness and, when hurt, swallows the light around you.")
        };

        public static Level ByScene(string scene)
        {
            foreach (var level in All)
                if (level.Scene == scene)
                    return level;
            return null;
        }

        // Najviši otključani nivo; nova igra počinje samo sa prvim.
        public static int Unlocked
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(UnlockedKey, 1), 1, All.Length);
            set
            {
                PlayerPrefs.SetInt(UnlockedKey, Mathf.Clamp(value, 1, All.Length));
                PlayerPrefs.Save();
            }
        }

        public static void Unlock(int index)
        {
            if (index > Unlocked)
                Unlocked = index;
        }
    }
}
