// Pamti jačinu i trajanje ledenog usporavanja protivnika.
using UnityEngine;

namespace AWizardsFight
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class EnemySlow : MonoBehaviour
    {
        private EnemyHealth health;
        private float multiplier = 1f, expiresAt;
        public float SpeedMultiplier => isActiveAndEnabled && Time.time < expiresAt ? multiplier : 1f;
        public float Remaining => Mathf.Max(0, expiresAt - Time.time);

        private void Awake() => health = GetComponent<EnemyHealth>();

        public bool Apply(float factor, float seconds)
        {
            if (!isActiveAndEnabled 
                || health.IsDead 
                || factor <= 0 
                || factor >= 1 
                || seconds <= 0 
                || float.IsNaN(factor) 
                || float.IsNaN(seconds) 
                || float.IsInfinity(seconds))
                return false;
         
            multiplier = Mathf.Min(SpeedMultiplier, factor);
            expiresAt = Mathf.Max(expiresAt, Time.time + seconds);
            return true;
        }

        // Termalni šok topi led: usporavanje odmah prestaje.
        public void Clear()
        {
            multiplier = 1f;
            expiresAt = 0f;
        }

        private void OnDisable()
        {
            multiplier = 1f;
            expiresAt = 0;
        }
    }
}
