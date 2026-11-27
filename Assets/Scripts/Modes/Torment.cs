// Torment (kao u Diablu): posle pobede nad Kraljem Senki otključava se viša težina, stepen po stepen.
// Svaki stepen čini protivnike jačim, a nagrade (iskustvo, zlato, nivo i retkost predmeta) boljim.
using UnityEngine;

namespace AWizardsFight
{
    public static class Torment
    {
        public const int Max = 6;
        private const string UnlockedKey = "TormentUnlocked", LevelKey = "TormentLevel";

        // Najviši otključani stepen (0 = još nije otključan).
        public static int Unlocked
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(UnlockedKey, 0), 0, Max);
            private set => PlayerPrefs.SetInt(UnlockedKey, Mathf.Clamp(value, 0, Max));
        }

        // Izabrani stepen; važi za nivoe priče i Riftove (ne za Endless i dnevni izazov).
        public static int Level
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(LevelKey, 0), 0, Unlocked);
            set
            {
                PlayerPrefs.SetInt(LevelKey, Mathf.Clamp(value, 0, Unlocked));
                PlayerPrefs.Save();
            }
        }

        // Endless i dnevni izazov imaju svoja pravila, pa Torment tamo ne važi.
        public static int Active => EndlessDirector.Active && !RiftDirector.Active ? 0 : Level;

        public static float EnemyHealth => 1f + .6f * Active;
        public static float EnemyDamage => 1f + .35f * Active;
        public static float Rewards => 1f + .5f * Active;
        public static float Luck => 1f + .45f * Active;
        public static int ItemLevelBonus => 10 * Active;

        // Pobeda nad Kraljem Senki na najvišem stepenu otključava sledeći.
        public static bool UnlockNext()
        {
            if (Level < Unlocked || Unlocked >= Max)
                return false;
            Unlocked = Unlocked + 1;
            PlayerPrefs.Save();
            return true;
        }

        public static void Reset()
        {
            PlayerPrefs.DeleteKey(UnlockedKey);
            PlayerPrefs.DeleteKey(LevelKey);
        }

        public static string Name(int level) => level == 0 ? "NORMAL" : "TORMENT " + level;

        public static string Describe(int level) => level == 0
            ? "Normal difficulty."
            : $"Enemies: +{60 * level}% health, +{35 * level}% damage.  Rewards: +{50 * level}% experience and gold, item level +{10 * level}, better rarity, more unique items.";
    }
}
