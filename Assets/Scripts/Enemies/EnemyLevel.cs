// Pretvara nivo protivnika u množioce zdravlja, štete, kretanja i učestalosti napada.
using UnityEngine;

namespace AWizardsFight
{
    [DisallowMultipleComponent]
    public sealed class EnemyLevel : MonoBehaviour
    {
        [SerializeField, Range(1, 100)]
        private int level = 1;
        public int Level => Mathf.Clamp(level, 1, 100);
        public bool IsElite { get; private set; }
        private float eliteHealth = 1f, eliteDamage = 1f, movementBonus = 1f;
        private static int depthScene = -1, depth;

        // Dublji nivoi su opasniji: isti protivnik u lavi i ledu ima više zdravlja i jači udarac nego u tamnici.
        // Dubina se računa iz aktivne scene, nezavisno od redosleda Awake poziva.
        private static int Depth
        {
            get
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (scene.handle != depthScene)
                {
                    depthScene = scene.handle;
                    var current = GameLevels.ByScene(scene.name);
                    depth = current != null ? current.Index - 1 : 0;
                }
                return depth;
            }
        }

        public void SetLevel(int value) => level = Mathf.Clamp(value, 1, 100);

        // Šampion: mini-boss sa mnogo više zdravlja i jačim udarcem.
        public void MakeChampion(float speedBonus = 1f)
        {
            movementBonus = speedBonus;
            IsElite = true;
            eliteHealth = 5f;
            eliteDamage = 1.5f;
        }

        // Čuvar Rifta: mnogo jači od šampiona.
        public void MakeGuardian()
        {
            IsElite = true;
            eliteHealth = 18f;
            eliteDamage = 1.8f;
        }

        // Elitni protivnik ima više zdravlja i jači udarac.
        public void MakeElite()
        {
            IsElite = true;
            eliteHealth = 2.2f;
            eliteDamage = 1.3f;
        }

        // Nivo 1 daje napredak 0, a nivo 100 napredak 1.
        private float Progress => (Level - 1) / 99f;
        public float HealthMultiplier => 1.35f * Torment.EnemyHealth * (1f + 3f * Progress) * eliteHealth * (1f + .2f * Depth) * GameSettings.EnemyHealthMultiplier * LevelModifiers.EnemyHealth;
        public float DamageMultiplier => 1.4f * Torment.EnemyDamage * (1f + 1.25f * Progress) * eliteDamage * (1f + .25f * Depth) * GameSettings.EnemyDamageMultiplier * LevelModifiers.EnemyDamage;
        public float MovementMultiplier => (1f + .2f * Progress) * movementBonus * LevelModifiers.EnemySpeed;
        public float AttackRateMultiplier => 1f + .35f * Progress;
    }
}
