// Prikazuje stanje i zdravlje završnog protivnika tokom susreta.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AWizardsFight
{
    public sealed class BossHUD : MonoBehaviour
    {
        [SerializeField]
        private BossEncounter encounter = null;
        [SerializeField]
        private EnemyHealth health = null;
        [SerializeField]
        private GameObject healthPanel = null, victoryPanel = null;
        [SerializeField]
        private Image fill = null;
        [SerializeField]
        private TMP_Text label = null;

        private void OnEnable()
        {
            if (encounter != null)
                encounter.StateChanged += Refresh;
            if (health != null)
                health.HealthChanged += HealthChanged;
        }

        private void Start() => Refresh();

        private void OnDisable()
        {
            if (encounter != null)
                encounter.StateChanged -= Refresh;
            if (health != null)
                health.HealthChanged -= HealthChanged;
        }

        private void HealthChanged(float current, float maximum) => Refresh();

        private void Refresh()
        {
            if (encounter == null)
                return;
            healthPanel.SetActive(encounter.State == BossEncounterState.Fighting);
            victoryPanel.SetActive(encounter.State == BossEncounterState.Victory);
            if (health == null)
                return;
            fill.fillAmount = health.CurrentHealth / health.MaxHealth;
            var level = health.GetComponent<EnemyLevel>();
            var boss = encounter.Boss;
            label.text = (boss != null ? boss.BossName : "BOSS") + (level != null ? "  LV. " + level.Level : "") + "  " + Mathf.CeilToInt(health.CurrentHealth) + " / " + Mathf.CeilToInt(health.MaxHealth) + (boss != null && boss.IsEnraged ? "  -  ENRAGED" : "");
        }
    }
}
