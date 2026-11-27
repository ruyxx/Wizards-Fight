// Osvežava prikaz zdravlja i mane na osnovu događaja igrača.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AWizardsFight
{
    /// <summary>Displays resources; it never owns or changes player health or mana.</summary>
    [DisallowMultipleComponent]
    public sealed class HUDController : MonoBehaviour
    {
        [SerializeField]
        private PlayerHealth health = null;
        [SerializeField]
        private PlayerMana mana = null;
        [SerializeField]
        private Image healthFill = null;
        [SerializeField]
        private Image manaFill = null;
        [SerializeField]
        private TMP_Text healthText = null;
        [SerializeField]
        private TMP_Text manaText = null;
        private bool started;

        private void OnEnable()
        {
            if (health == null 
                || mana == null 
                || healthFill == null 
                || manaFill == null 
                || healthText == null 
                || manaText == null)
            {
                Debug.LogError("HUDController requires all player and UI references.", this);
                enabled = false;
                return;
            }

            health.HealthChanged += ShowHealth;
            mana.ManaChanged += ShowMana;
            if (started)
                Refresh();
        }

        private void Start()
        {
            started = true;
            Refresh();
        }

        private void OnDisable()
        {
            if (health != null)
                health.HealthChanged -= ShowHealth;
            if (mana != null)
                mana.ManaChanged -= ShowMana;
        }

        private void Refresh()
        {
            ShowHealth(health.CurrentHealth, health.MaxHealth);
            ShowMana(mana.CurrentMana, mana.MaxMana);
        }

        private void ShowHealth(float current, float maximum)
        {
            healthFill.fillAmount = current / maximum;
            healthText.text = $"HEALTH   {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(maximum)}";
        }

        private void ShowMana(float current, float maximum)
        {
            manaFill.fillAmount = current / maximum;
            manaText.text = $"MANA   {Mathf.FloorToInt(current)} / {Mathf.CeilToInt(maximum)}";
        }
    }
}
