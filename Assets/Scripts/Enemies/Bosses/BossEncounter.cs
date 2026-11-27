// Povezuje ulazak u arenu, aktiviranje bossa i završetak susreta.
using System;
using UnityEngine;

namespace AWizardsFight
{
    // Zajedničko ponašanje završnih protivnika koje koristi susret u areni i prikaz u HUD-u.
    public interface IBossBehaviour
    {
        string BossName { get; }
        bool IsEngaged { get; }
        bool IsWindingUp { get; }
        bool IsEnraged { get; }
        int AttacksFired { get; }
        void SetEngaged(bool value);
        void SetPatrolArena(Rect arena);
    }

    public enum BossEncounterState
    {
        Dormant,
        Fighting,
        Victory,
        PlayerDead
    }

    public sealed class BossEncounter : MonoBehaviour
    {
        [SerializeField, Tooltip("Komponenta završnog protivnika (DarkSorcererAI ili MagmaGolemAI).")]
        private MonoBehaviour boss = null;
        [SerializeField]
        private PlayerHealth player = null;
        [SerializeField]
        private Door door = null;
        [SerializeField]
        private Rect arena = new Rect(16, -5, 9, 11);
        private EnemyHealth bossHealth;
        public BossEncounterState State { get; private set; }
        public IBossBehaviour Boss => boss as IBossBehaviour;
        public EnemyHealth BossHealth => bossHealth;

        public event Action StateChanged;

        private void Awake()
        {
            if (Boss != null)
            {
                bossHealth = boss.GetComponent<EnemyHealth>();
                Boss.SetPatrolArena(arena);
            }
        }

        private void OnEnable()
        {
            if (bossHealth != null)
                bossHealth.Died += Win;
            if (player != null)
                player.Died += Lose;
        }

        private void OnDisable()
        {
            if (bossHealth != null)
                bossHealth.Died -= Win;
            if (player != null)
                player.Died -= Lose;
        }

        private void Update()
        {
            if (State == BossEncounterState.Victory
                || State == BossEncounterState.PlayerDead
                || Boss == null
                || player == null)
                return;
            bool inside = door != null && door.IsOpen && !player.IsDead && arena.Contains(player.transform.position);
            if (inside && State == BossEncounterState.Dormant)
            {
                State = BossEncounterState.Fighting;
                StateChanged?.Invoke();
            }

            Boss.SetEngaged(inside);
        }

        private void Win()
        {
            if (State != BossEncounterState.Fighting || player == null || player.IsDead)
                return;
            State = BossEncounterState.Victory;
            foreach (var shot in FindObjectsByType<Projectile>(FindObjectsSortMode.None))
            {
                shot.gameObject.SetActive(false);
                Destroy(shot.gameObject);
            }

            foreach (var ai in FindObjectsByType<MeleeEnemyAI>(FindObjectsSortMode.None))
                ai.enabled = false;
            foreach (var ai in FindObjectsByType<DarkArcherAI>(FindObjectsSortMode.None))
                ai.enabled = false;
            foreach (var hazard in FindObjectsByType<HazardTelegraph>(FindObjectsSortMode.None))
                Destroy(hazard.gameObject);
            player.GetComponent<PlayerController>().enabled = false;
            player.GetComponent<PlayerCombat>().enabled = false;
            player.GetComponent<PlayerMana>().enabled = false;
            player.GetComponent<PlayerWeapons>().enabled = false;
            player.GetComponent<PlayerPotions>().enabled = false;
            player.GetComponent<PlayerInteractor>().enabled = false;
            StateChanged?.Invoke();
        }

        private void Lose()
        {
            if (State == BossEncounterState.Victory)
                return;
            State = BossEncounterState.PlayerDead;
            Boss?.SetEngaged(false);
            StateChanged?.Invoke();
        }
    }
}
