// Čuva zdravlje protivnika i emituje obaveštenja o oštećenju i smrti.
using System;
using UnityEngine;

namespace AWizardsFight
{
    [DisallowMultipleComponent]
    public sealed class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)]
        private float maxHealth = 40f;
        [SerializeField]
        private bool startsInvulnerable = false;
        public bool IsInvulnerable { get; private set; }

        private EnemyLevel level;
        public float MaxHealth => maxHealth * (level != null ? level.HealthMultiplier : 1f);
        public float CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0;

        public event Action<float, float> HealthChanged;
        public event Action Died;
        // Javlja svaku stvarno nanetu štetu bilo kom protivniku (za brojeve štete i potres kamere).
        public static event Action<EnemyHealth, float> AnyDamaged;
        // Javlja smrt bilo kog protivnika (za iskustvo i nagrade elitnih protivnika).
        public static event Action<EnemyHealth> AnyDied;
        // Bombaš koji se sam raznese ne daje iskustvo.
        public bool GrantsExperience { get; set; } = true;
        public EnemyLevel Level => level;

        private void Awake()
        {
            level = GetComponent<EnemyLevel>();
            if (GetComponent<BodyAnimator>() == null && transform.Find("Visual") != null)
                gameObject.AddComponent<BodyAnimator>();
            CurrentHealth = MaxHealth;
            IsInvulnerable = startsInvulnerable;
        }

        public void SetInvulnerable(bool value) => IsInvulnerable = value;

        // Posle promene nivoa ili elitnog statusa zdravlje kreće od novog maksimuma.
        public void ResetToMax()
        {
            if (IsDead)
                return;
            CurrentHealth = MaxHealth;
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void Heal(float amount)
        {
            if (IsDead || !(amount > 0f) || float.IsInfinity(amount))
                return;
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        // Odmah uklanja protivnika bez broja štete (npr. bombaš posle eksplozije).
        public void Kill()
        {
            if (IsDead)
                return;
            CurrentHealth = 0;
            Die();
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || IsInvulnerable || amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount))
                return;
            float previous = CurrentHealth;
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
            AnyDamaged?.Invoke(this, previous - CurrentHealth);
            if (IsDead)
                Die();
        }

        // Uklanja protivnika koji je već bio poražen pre poslednjeg čuvanja.
        public void RemoveForRestore()
        {
            if (IsDead)
                return;
            CurrentHealth = 0;
            Die();
        }

        private void Die()
        {
            Died?.Invoke();
            if (!SaveSystem.IsRestoring)
                AnyDied?.Invoke(this);
            foreach (var collider in GetComponentsInChildren<Collider2D>())
                collider.enabled = false;
            Destroy(gameObject);
        }
    }
}
