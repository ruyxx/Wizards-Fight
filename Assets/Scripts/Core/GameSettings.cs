// Težina igre: menja zdravlje i štetu protivnika.
using UnityEngine;

namespace AWizardsFight
{
    public enum Difficulty
    {
        Easy,
        Normal,
        Hard
    }

    public static class GameSettings
    {
        private const string Key = "Difficulty";

        public static Difficulty Difficulty
        {
            get => (Difficulty)Mathf.Clamp(PlayerPrefs.GetInt(Key, (int)Difficulty.Normal), 0, 2);
            set
            {
                PlayerPrefs.SetInt(Key, (int)value);
                PlayerPrefs.Save();
            }
        }

        public static float EnemyHealthMultiplier => Difficulty == Difficulty.Easy ? .75f : Difficulty == Difficulty.Hard ? 1.3f : 1f;

        public static float EnemyDamageMultiplier => Difficulty == Difficulty.Easy ? .7f : Difficulty == Difficulty.Hard ? 1.35f : 1f;
    }
}
