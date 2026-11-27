// Posebna osobina protivnika (vidi EnemyVariants).
using UnityEngine;

namespace AWizardsFight
{
    // Leči obližnje saveznike na svakih nekoliko sekundi (zeleni talas) - ubij ga prvog.
    public sealed class HealerAura : MonoBehaviour
    {
        private EnemyHealth self;
        private float nextPulse;

        private void Start()
        {
            self = GetComponent<EnemyHealth>();
            nextPulse = Time.time + Random.Range(2f, 4f);
        }

        private void Update()
        {
            if (self == null || self.IsDead || Time.time < nextPulse)
                return;
            nextPulse = Time.time + 3.5f;
            bool healed = false;
            foreach (var collider in Physics2D.OverlapCircleAll(transform.position, 4.5f, 1 << 11))
            {
                var ally = collider.GetComponentInParent<EnemyHealth>();
                if (ally == null || ally == self || ally.IsDead || ally.CurrentHealth >= ally.MaxHealth)
                    continue;
                ally.Heal(ally.MaxHealth * .12f);
                CombatFeedback.Burst(ally.transform.position, new Color(.4f, 1f, .5f), 4, 1.5f);
                healed = true;
            }
            if (!healed)
                return;
            var ring = RuntimeSprites.Spawn("Heal Pulse", RuntimeSprites.Ring, transform.position, new Color(.4f, 1f, .5f, .8f), 9f, 6);
            ring.gameObject.AddComponent<FadeOut>().Play(.5f, Vector2.zero);
        }
    }
}
