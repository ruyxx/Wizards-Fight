// Čuva trenutnu manu, obnavlja je kroz vreme i proverava cenu bacanja čarolije.
using System;
using UnityEngine;

namespace AWizardsFight
{
    [DisallowMultipleComponent]
    public sealed class PlayerMana : MonoBehaviour
    {
        [SerializeField, Min(1f)]
        private float maxMana = 100f;
        [SerializeField, Min(0f)]
        private float regenerationPerSecond = 8f;
        private float gearMana, gearRegen;
        public float MaxMana => maxMana + gearMana;
        public float CurrentMana { get; private set; }
        public float RegenerationPerSecond => regenerationPerSecond;

        public event Action<float, float> ManaChanged;

        private void Awake() => CurrentMana = MaxMana;

        // Oprema dodaje manu i procenat brže obnove.
        public void SetGearBonus(float maximum, float regenPercent)
        {
            gearMana = Mathf.Max(0f, maximum);
            gearRegen = Mathf.Max(0f, regenPercent);
            CurrentMana = Mathf.Min(CurrentMana, MaxMana);
            ManaChanged?.Invoke(CurrentMana, MaxMana);
        }

        private void Update()
        {
            if (CurrentMana >= MaxMana || regenerationPerSecond <= 0f || Time.deltaTime <= 0f)
                return;
            CurrentMana = Mathf.Min(MaxMana, CurrentMana + regenerationPerSecond * (RegenerationMultiplier + gearRegen / 100f) * Time.deltaTime);
            ManaChanged?.Invoke(CurrentMana, MaxMana);
        }

        // Množilac obnove koji menjaju pojačanja.
        public float RegenerationMultiplier { get; set; } = 1f;

        public void Restore(float amount)
        {
            if (!(amount > 0f) || float.IsInfinity(amount))
                return;
            CurrentMana = Mathf.Min(MaxMana, CurrentMana + amount);
            ManaChanged?.Invoke(CurrentMana, MaxMana);
        }

        public void AddMaximum(float amount)
        {
            if (amount >= 0f)
            {
                IncreaseMaximum(amount);
                return;
            }
            maxMana = Mathf.Max(1f, maxMana + amount);
            CurrentMana = Mathf.Min(CurrentMana, MaxMana);
            ManaChanged?.Invoke(CurrentMana, MaxMana);
        }

        public void IncreaseMaximum(float amount)
        {
            if (!(amount > 0f) || float.IsInfinity(amount))
                return;
            maxMana += amount;
            CurrentMana = Mathf.Min(MaxMana, CurrentMana + amount);
            ManaChanged?.Invoke(CurrentMana, MaxMana);
        }

        public bool TryUseMana(float amount)
        {
            if (!isActiveAndEnabled 
                || float.IsNaN(amount) 
                || float.IsInfinity(amount) 
                || amount < 0f 
                || amount > CurrentMana)
                return false;
            if (amount == 0f)
                return true;
            CurrentMana -= amount;
            ManaChanged?.Invoke(CurrentMana, MaxMana);
            return true;
        }

        private void OnValidate()
        {
            if (float.IsNaN(maxMana) || float.IsInfinity(maxMana))
                maxMana = 100f;
            maxMana = Mathf.Max(1f, maxMana);
            if (float.IsNaN(regenerationPerSecond) || float.IsInfinity(regenerationPerSecond))
                regenerationPerSecond = 8f;
            regenerationPerSecond = Mathf.Max(0f, regenerationPerSecond);
        }
    }
}
