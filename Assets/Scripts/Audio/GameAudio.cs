// Povezuje događaje igre sa zvučnim efektima, muzikom i podešavanjem glasnoće.
using UnityEngine;

namespace AWizardsFight
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class GameAudio : MonoBehaviour
    {
        [SerializeField]
        private AudioClip music = null, cast = null, hit = null, heal = null, loot = null, victory = null;
        [SerializeField]
        private AudioClip fire = null, ice = null, enemyDeath = null, gate = null, gameOver = null, bossMusic = null;
        [SerializeField]
        private PlayerHealth player = null;
        [SerializeField]
        private BossEncounter encounter = null;
        private AudioSource effects, soundtrack;
        private PlayerCombat combat;
        private PlayerLootReceiver receiver;
        private float previousHealth, previousCooldown;
        private EnemyHealth[] enemies;
        private Door door;
        private IBossBehaviour boss;
        private int previousBossAttacks;

        private void Awake()
        {
            effects = GetComponent<AudioSource>();
            effects.playOnAwake = false;
            effects.volume = .35f;
            soundtrack = gameObject.AddComponent<AudioSource>();
            // Svaki nivo može da ima svoju muziku (Resources/Music/<scena>), inače ide zajednička.
            var levelMusic = Resources.Load<AudioClip>("Music/" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            soundtrack.clip = levelMusic != null ? levelMusic : music;
            soundtrack.loop = true;
            soundtrack.volume = .12f;
            soundtrack.Play();
            if (player != null)
            {
                combat = player.GetComponent<PlayerCombat>();
                receiver = player.GetComponent<PlayerLootReceiver>();
            }

            // The dungeon has fixed scene actors; cache them once, not every frame.
            enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            door = FindFirstObjectByType<Door>();
            ApplyVolumes();
            boss = FindFirstObjectByType<BossEncounter>()?.Boss;
        }

        private void OnEnable()
        {
            if (player != null)
            {
                player.HealthChanged += Health;
                player.Died += Lose;
            }

            if (receiver != null)
                receiver.Rewarded += Reward;
            if (encounter != null)
                encounter.StateChanged += Encounter;
            if (door != null)
                door.Opened += Gate;
            foreach (var enemy in enemies)
            {
                enemy.HealthChanged += EnemyHit;
                enemy.Died += EnemyDied;
            }
        }

        private void Start()
        {
            if (player != null)
                previousHealth = player.CurrentHealth;
        }

        private void OnDisable()
        {
            if (player != null)
            {
                player.HealthChanged -= Health;
                player.Died -= Lose;
            }

            if (receiver != null)
                receiver.Rewarded -= Reward;
            if (encounter != null)
                encounter.StateChanged -= Encounter;
            if (door != null)
                door.Opened -= Gate;
            foreach (var enemy in enemies)
            {
                if (enemy == null)
                    continue;
                enemy.HealthChanged -= EnemyHit;
                enemy.Died -= EnemyDied;
            }
        }

        private void Update()
        {
            if (combat == null)
                return;
            float cooldown = combat.CooldownRemaining;
            if (cooldown > previousCooldown + .01f)
            {
                Play(combat.EquippedWeapon.Type == WeaponType.FireStaff ? fire : combat.EquippedWeapon.Type == WeaponType.IceStaff ? ice : cast);
            }

            previousCooldown = cooldown;
            if (boss != null && boss.AttacksFired > previousBossAttacks)
            {
                previousBossAttacks = boss.AttacksFired;
                Play(cast);
            }
        }

        private void Health(float current, float maximum)
        {
            Play(current < previousHealth ? hit : heal);
            previousHealth = current;
        }

        private void Reward(WeaponData weapon, WeaponAcquisition acquisition) => Play(loot);

        private void EnemyHit(float current, float max) => Play(hit);

        private void EnemyDied() => Play(enemyDeath);

        private void Gate() => Play(gate);

        private void Encounter()
        {
            if (encounter.State == BossEncounterState.Victory)
            {
                soundtrack.Stop();
                Play(victory);
            }
            else if (encounter.State == BossEncounterState.Fighting && bossMusic != null)
            {
                // Svaki boss može da ima svoju muziku (Resources/Music/<scena>Boss).
                var levelBoss = Resources.Load<AudioClip>("Music/" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "Boss");
                soundtrack.clip = levelBoss != null ? levelBoss : bossMusic;
                soundtrack.Play();
            }
        }

        public void ApplyVolumes()
        {
            if (soundtrack != null)
                soundtrack.volume = .15f * PlayerPrefs.GetFloat("MusicVolume", .8f);
            if (effects != null)
                effects.volume = .44f * PlayerPrefs.GetFloat("EffectsVolume", .8f);
        }

        private void Lose()
        {
            soundtrack.Stop();
            Play(gameOver);
        }

        private void Play(AudioClip clip)
        {
            // Pri vraćanju sačuvanog stanja ne puštaju se zvuci za već poražene protivnike.
            if (clip != null && !SaveSystem.IsRestoring)
                effects.PlayOneShot(clip);
        }
    }
}
