// Otrovni projektil: pogođeni protivnik prima štetu kroz vreme; više pogodaka slaže otrov.
using UnityEngine;

namespace AWizardsFight
{
    public sealed class PoisonOnHit : MonoBehaviour
    {
        [SerializeField, Range(0f, 2f)]
        private float damagePerSecondFraction = .45f;
        [SerializeField, Min(.5f)]
        private float duration = 4f;

        public void Apply(IDamageable target, float hitDamage)
        {
            if (target is EnemyHealth enemy && !enemy.IsDead)
                EnemyPoison.Apply(enemy, hitDamage * damagePerSecondFraction, duration);
        }
    }

    // Otrov na protivniku: do pet slojeva, svaki nanosi štetu svake pola sekunde.
    public sealed class EnemyPoison : MonoBehaviour
    {
        private const int MaxStacks = 5;
        private const float Tick = .5f;
        private EnemyHealth health;
        private float damagePerSecond, expiresAt, nextTick;
        private int stacks;
        private SpriteRenderer cloud;
        public bool Active => Time.time < expiresAt && stacks > 0;

        public static void Apply(EnemyHealth enemy, float damagePerSecond, float seconds)
        {
            var poison = enemy.GetComponent<EnemyPoison>();
            if (poison == null)
                poison = enemy.gameObject.AddComponent<EnemyPoison>();
            poison.Add(damagePerSecond, seconds);
        }

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            cloud = RuntimeSprites.Spawn("Poison", RuntimeSprites.Glow, transform.position, new Color(.4f, 1f, .3f, 0f), 1.8f, 9);
            cloud.transform.SetParent(transform, true);
        }

        private void Add(float dps, float seconds)
        {
            stacks = Mathf.Min(MaxStacks, stacks + 1);
            // Slojevi se zbrajaju, a najjači pogodak određuje osnovnu jačinu.
            damagePerSecond = Mathf.Max(damagePerSecond, dps * SkillEffects.PoisonMultiplier);
            expiresAt = Time.time + seconds;
            if (nextTick <= Time.time)
                nextTick = Time.time + Tick;
        }

        private void Update()
        {
            if (health == null || health.IsDead)
                return;
            bool active = Time.time < expiresAt && stacks > 0;
            cloud.color = new Color(.4f, 1f, .3f, active ? .25f + .07f * stacks + .08f * Mathf.Sin(Time.time * 6f) : 0f);
            if (!active)
            {
                stacks = 0;
                damagePerSecond = 0f;
                return;
            }

            if (Time.time < nextTick)
                return;
            nextTick += Tick;
            health.TakeDamage(damagePerSecond * stacks * Tick);
        }
    }
}
