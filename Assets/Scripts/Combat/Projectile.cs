// Pomera projektil, proverava sudare duž putanje i primenjuje pogodak.
using System.Collections.Generic;
using UnityEngine;

namespace AWizardsFight
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField, Min(0.01f)]
        private float lifetime = 2f;
        [SerializeField]
        private LayerMask hitLayers;
        private Rigidbody2D body;
        private CircleCollider2D hitbox;
        private Transform owner;
        private Vector2 direction;
        private float speed, damage, remainingLifetime;
        private float slowMultiplier = 1f, slowDuration;
        private bool launched, spent, critical;
        private PlayerHealth lifestealTarget;
        private float lifesteal;
        // Pogodak igrača nosi vrstu oružja, da bi veštine znale da li je vatra, led, munja...
        private bool fromPlayer;
        private WeaponType weaponType;
        private ContactFilter2D filter;
        private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>(8);
        public float Lifetime => lifetime;
        public bool IsSpent => spent;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            hitbox = GetComponent<CircleCollider2D>();
            filter = new ContactFilter2D
            {
                useTriggers = false
            };
            filter.SetLayerMask(hitLayers);
        }

        public void Launch(Vector2 aim, float projectileSpeed, float hitDamage, Transform source, float slowFactor = 1f, float slowSeconds = 0f)
        {
            if (launched)
                return;
            if (!Positive(projectileSpeed)
                || !Positive(hitDamage)
                || !Positive(lifetime)
                || float.IsNaN(aim.x)
                || float.IsNaN(aim.y)
                || float.IsInfinity(aim.x)
                || float.IsInfinity(aim.y)
                || aim.sqrMagnitude < .0001f)
            {
                Finish();
                return;
            }

            direction = aim.normalized;
            speed = projectileSpeed;
            damage = hitDamage;
            slowMultiplier = slowFactor;
            slowDuration = slowSeconds;
            owner = source;
            remainingLifetime = lifetime;
            launched = true;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        private void FixedUpdate()
        {
            if (!launched || spent)
                return;
            float step = Mathf.Min(Time.fixedDeltaTime, remainingLifetime);
            float distance = speed * step;

            float radius = hitbox.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            int count = Physics2D.CircleCast(body.position, radius, direction, filter, hits, distance);
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (owner != null && hit.transform.IsChildOf(owner))
                    continue;
                Impact(hit.collider, hit.centroid - direction * .01f);
                return;
            }

            body.MovePosition(body.position + direction * distance);
            remainingLifetime -= step;
            if (remainingLifetime <= 0)
                Finish();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!launched || spent || other.isTrigger || (hitLayers.value & (1 << other.gameObject.layer)) == 0)
                return;
            if (owner != null && other.transform.IsChildOf(owner))
                return;
            Impact(other, body.position);
        }

        public void SetPlayerBonuses(bool isCritical, PlayerHealth healTarget, float lifestealFraction, WeaponType type)
        {
            fromPlayer = true;
            weaponType = type;
            critical = isCritical;
            lifestealTarget = healTarget;
            lifesteal = Mathf.Max(0f, lifestealFraction);
        }

        // Kritični pogodak i krađa života važe samo za pogodak protivnika.
        private void ApplyPlayerBonuses(IDamageable target, float dealt)
        {
            if (!(target is EnemyHealth enemy))
                return;
            if (critical)
                CombatFeedback.Popup("CRIT!", enemy.transform.position + Vector3.up * .5f, new Color(1f, .85f, .2f), 3.6f, .6f);
            if (lifesteal > 0f && lifestealTarget != null)
                lifestealTarget.Heal(dealt * lifesteal);
        }

        private void Impact(Collider2D other, Vector2 position)
        {
            if (spent)
                return;

            spent = true;
            hitbox.enabled = false;
            // Štit spreda propušta samo deo štete i ne dozvoljava usporavanje ni eksploziju.
            var blocker = other.GetComponentInParent<IProjectileBlocker>();
            float factor = blocker != null ? blocker.DamageFactor(direction, position) : 1f;
            var target = other.GetComponentInParent<IDamageable>();
            if (factor < 1f)
            {
                target?.TakeDamage(damage * factor);
                ApplyPlayerBonuses(target, damage * factor);
                Destroy(gameObject);
                return;
            }

            var enemyTarget = fromPlayer ? target as EnemyHealth : null;
            if (enemyTarget != null)
                damage = SkillEffects.ModifyDamage(enemyTarget, damage, weaponType);
            target?.TakeDamage(damage);
            ApplyPlayerBonuses(target, damage);
            if (enemyTarget != null)
                SkillEffects.OnHit(enemyTarget, damage, weaponType);
            // Munja preskače na susedne protivnike, a otrov ostaje na pogođenom.
            GetComponent<ChainLightning>()?.Chain(position, damage, target);
            GetComponent<PoisonOnHit>()?.Apply(target, damage);
            GetComponent<FireExplosion>()?.Explode(position, damage, target);
            if (slowDuration > 0)
            {
                other.GetComponentInParent<EnemySlow>()?.Apply(slowMultiplier, slowDuration);
                // Ledeni projektili protivnika usporavaju i igrača (osim tokom izmicanja).
                var playerHealth = target as PlayerHealth;
                if (playerHealth != null && !playerHealth.IsInvulnerable)
                    playerHealth.GetComponent<PlayerController>()?.ApplySlow(slowMultiplier, slowDuration);
            }
            Destroy(gameObject);
        }

        private void Finish()
        {
            spent = true;
            hitbox.enabled = false;
            Destroy(gameObject);
        }

        private static bool Positive(float value) => value > 0 && !float.IsInfinity(value);
    }
}
