// Povezuje smrt igrača sa zaustavljanjem njegovih aktivnih sposobnosti.
using UnityEngine;

namespace AWizardsFight
{
    
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerHealth), typeof(PlayerMana), typeof(PlayerController))]
    public sealed class PlayerLifeState : MonoBehaviour
    {
        private PlayerHealth health;

        private void Awake() => health = GetComponent<PlayerHealth>();

        private void OnEnable() => health.Died += OnDied;

        private void OnDisable() => health.Died -= OnDied;

        private void Start()
        {
            if (health.IsDead)
                OnDied();
        }

        private void OnDied()
        {
            GetComponent<PlayerController>().enabled = false;
            GetComponent<PlayerMana>().enabled = false;
            if (TryGetComponent<PlayerCombat>(out var combat))
                combat.enabled = false;
        }
    }
}
