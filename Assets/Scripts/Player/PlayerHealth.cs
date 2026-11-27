// Čuva zdravlje igrača i obaveštava druge komponente o promeni zdravlja i smrti.
using System;
using UnityEngine;

namespace AWizardsFight
{
    [DisallowMultipleComponent]
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)]
        private float maxHealth = 100f;
        private float gearHealth, gearHealthPercent;
        // Najveće zdravlje: osnovno (sa pojačanjima nivoa) plus oprema.
        public float MaxHealth => (maxHealth + gearHealth) * (1f + gearHealthPercent / 100f);
        public float Armor { get; private set; }
        // Oklop smanjuje svaku štetu; što više oklopa, to manji dodatni efekat (najviše 70%).
        public float DamageReduction => Mathf.Min(.7f, Armor / (Armor + 150f));
        public float CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0f;
        // Tokom izmicanja lik ne prima štetu.
        public bool IsInvulnerable { get; private set; }

        public event Action<float, float> HealthChanged;
        public event Action Died;

        private void Awake() => CurrentHealth = MaxHealth;

        public void SetGearBonus(float health, float healthPercent, float armor)
        {
            gearHealth = Mathf.Max(0f, health);
            gearHealthPercent = Mathf.Max(0f, healthPercent);
            Armor = Mathf.Max(0f, armor);
            if (CurrentHealth > MaxHealth)
                CurrentHealth = MaxHealth;
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void SetInvulnerable(bool value) => IsInvulnerable = value;

        public void TakeDamage(float amount)
        {
            if (IsDead || IsInvulnerable || !IsPositiveFinite(amount))
                return;
            amount = SkillEffects.AbsorbWithMana(this, amount);
            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount * (1f - DamageReduction));
            // Prsten feniksa spasava od smrtnog udarca jednom po nivou.
            if (CurrentHealth <= 0f && UniqueItems.TryPhoenix(this))
                CurrentHealth = MaxHealth * .5f;
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
            if (IsDead)
                Died?.Invoke();
        }

        // Cena koju igrač sam plaća (oltar krvi): oklop je ne smanjuje i ne može da ubije lika.
        public void TakeDamageIgnoringArmor(float amount)
        {
            if (IsDead || !IsPositiveFinite(amount))
                return;
            CurrentHealth = Mathf.Max(1f, CurrentHealth - amount);
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public bool Heal(float amount)
        {
            if (IsDead || CurrentHealth >= MaxHealth || !IsPositiveFinite(amount))
                return false;
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
            return true;
        }

        // Vraća sačuvano zdravlje pri nastavku partije sa poslednje tačke čuvanja.
        public void RestoreHealth(float value)
        {
            if (IsDead || !IsPositiveFinite(value))
                return;
            CurrentHealth = Mathf.Min(MaxHealth, value);
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        // Menja osnovno najveće zdravlje (npr. dnevni izazov sa pola zdravlja).
        public void MultiplyMaximum(float factor)
        {
            if (!IsPositiveFinite(factor))
                return;
            maxHealth *= factor;
            CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        // Vraćanje veština može i da smanji najveće zdravlje.
        public void AddMaximum(float amount)
        {
            if (amount >= 0f)
            {
                IncreaseMaximum(amount);
                return;
            }
            maxHealth = Mathf.Max(1f, maxHealth + amount);
            CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        // Trajno povećava najveće zdravlje i odmah leči za isti iznos.
        public void IncreaseMaximum(float amount)
        {
            if (!IsPositiveFinite(amount))
                return;
            maxHealth += amount;
            if (!IsDead)
                CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        private static bool IsPositiveFinite(float value) => value > 0f && !float.IsInfinity(value);

        private void OnValidate()
        {
            if (!IsPositiveFinite(maxHealth))
                maxHealth = 100f;
            maxHealth = Mathf.Max(1f, maxHealth);
        }
    }
}
