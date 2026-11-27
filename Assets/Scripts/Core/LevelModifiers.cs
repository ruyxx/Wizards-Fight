// Privremene izmene koje važe do kraja nivoa (npr. kletva oltara pohlepe).
namespace AWizardsFight
{
    public static class LevelModifiers
    {
        public static float EnemyDamage { get; set; } = 1f;
        public static float EnemyHealth { get; set; } = 1f;
        public static float EnemySpeed { get; set; } = 1f;
        public static float PlayerDamage { get; set; } = 1f;
        // Nagrade (zlato i iskustvo) i sreća za bolje predmete, npr. od osobine nivoa.
        public static float Rewards { get; set; } = 1f;
        public static float Luck { get; set; } = 1f;

        public static void Reset()
        {
            EnemyDamage = 1f;
            EnemyHealth = 1f;
            EnemySpeed = 1f;
            PlayerDamage = 1f;
            Rewards = 1f;
            Luck = 1f;
        }
    }
}
