// Pamti broj napitaka i troši napitak kada igrač može da obnovi zdravlje.
using System;
using UnityEngine;

namespace AWizardsFight
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerHealth))]
    public sealed class PlayerPotions : MonoBehaviour
    {
        [SerializeField, Min(0)]
        private int startingCount = 2;
        [SerializeField, Min(1)]
        private float healAmount = 30f;
        private PlayerHealth health;
        private bool usingPotion;
        public int Count { get; private set; }
        public int StartingCount => startingCount;
        // Pojačanje Potion Master povećava lečenje svakog napitka.
        public float HealBonus { get; set; }

        public event Action Changed;

        private void Awake()
        {
            health = GetComponent<PlayerHealth>();
            Count = Mathf.Max(0, startingCount);
        }

        private void Update()
        {
            if (Application.isFocused && (Keybinds.Pressed(GameAction.Potion) || GamepadInput.PotionPressed))
                TryUse();
        }

        public bool TryUse()
        {
            if (usingPotion
                || !isActiveAndEnabled
                || Time.timeScale <= 0
                || Count <= 0
                || health == null
                || health.IsDead)
                return false;
            usingPotion = true;
            try
            {
                var equipment = GetComponent<PlayerEquipment>();
                if (!health.Heal(healAmount + HealBonus + (equipment != null ? equipment.Get(StatType.PotionHealing) : 0f)))
                    return false;
                Count--;
                Changed?.Invoke();
                return true;
            }
            finally
            {
                usingPotion = false;
            }
        }

        public void RestoreCount(int count)
        {
            Count = Mathf.Max(0, count);
            Changed?.Invoke();
        }

        public bool Add(int amount)
        {
            if (amount <= 0 || Count > int.MaxValue - amount)
                return false;
            Count += amount;
            Changed?.Invoke();
            return true;
        }
    }
}
