// Užareno tlo posle erupcije: kratko peče igrača koji stoji u krugu.
using UnityEngine;

namespace AWizardsFight
{
    public sealed class BurningGround : MonoBehaviour
    {
        private const float TickInterval = .25f;
        private float radius, damagePerSecond, duration, elapsed, nextTick;
        private PlayerHealth target;
        private SpriteRenderer fill;

        public static void Create(Vector2 position, float radius, float damagePerSecond, float duration, PlayerHealth target)
        {
            var fill = RuntimeSprites.Spawn("Burning Ground", RuntimeSprites.Disc, position, new Color(1f, .4f, .1f, .55f), radius * 2f, -7);
            var ground = fill.gameObject.AddComponent<BurningGround>();
            ground.radius = radius;
            ground.damagePerSecond = damagePerSecond;
            ground.duration = duration;
            ground.target = target;
            ground.fill = fill;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float fade = Mathf.Clamp01((duration - elapsed) / .5f);
            fill.color = new Color(1f, .35f + .1f * Mathf.Sin(Time.time * 9f), .1f, .55f * fade);
            if (elapsed >= nextTick)
            {
                nextTick += TickInterval;
                if (target != null && !target.IsDead && Vector2.Distance(target.transform.position, transform.position) <= radius)
                    target.TakeDamage(damagePerSecond * TickInterval);
            }

            if (elapsed >= duration)
                Destroy(gameObject);
        }
    }
}
