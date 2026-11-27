// Meta za vežbu koja prihvata štetu preko istog ugovora kao protivnici.
using TMPro;
using UnityEngine;

namespace AWizardsFight
{
    /// <summary>Stationary Phase 3 practice target. Enemy AI is a later phase.</summary>
    public sealed class PracticeTarget : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)]
        private float maxHealth = 30f;
        [SerializeField]
        private TMP_Text label = null;
        public float CurrentHealth { get; private set; }

        private void Awake() => CurrentHealth = maxHealth;

        private void Start() => Refresh();

        public void TakeDamage(float amount)
        {
            if (CurrentHealth <= 0 || amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount))
                return;
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            Refresh();
            if (CurrentHealth <= 0)
                Destroy(gameObject);
        }

        private void Refresh()
        {
            if (label != null)
                label.text = $"PRACTICE\n{Mathf.CeilToInt(CurrentHealth)} / {Mathf.CeilToInt(maxHealth)}";
        }
    }
}
