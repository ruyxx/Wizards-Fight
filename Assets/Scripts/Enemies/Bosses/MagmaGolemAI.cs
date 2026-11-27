// Magma Golem, boss lava arene: sporo ide ka igraču i smenjuje udar o tlo, rafal vatrenih kugli, juriš i erupcije lave.
// Faze: ispod 60% zdravlja dodaje juriš, ispod 30% je pobesneo (brži, češći napadi i erupcije pod igračem).
using System.Collections.Generic;
using UnityEngine;

namespace AWizardsFight
{
    [RequireComponent(typeof(EnemyHealth), typeof(Rigidbody2D))]
    public sealed class MagmaGolemAI : MonoBehaviour, IBossBehaviour
    {
        private enum Attack
        {
            Slam,
            Barrage,
            Charge,
            Eruption
        }

        [SerializeField]
        private Projectile projectile = null;
        [SerializeField]
        private string bossName = "MAGMA GOLEM";
        [SerializeField, Min(0)]
        private float damage = 14f, contactDamage = 18f;
        [SerializeField, Min(0)]
        private float moveSpeed = 1.1f, enragedMoveSpeed = 1.6f;
        [SerializeField, Min(.2f)]
        private float attackInterval = 2.4f, enragedAttackInterval = 1.5f;
        [SerializeField, Min(.5f)]
        private float slamRadius = 2.8f;
        [SerializeField, Min(1f)]
        private float chargeSpeed = 9f;
        [SerializeField, Min(.1f)]
        private float chargeWindup = .8f, stunDuration = 1.8f;
        [SerializeField, Min(.5f)]
        private float shotSpeed = 6f;
        private EnemyHealth health;
        private EnemyLevel level;
        private EnemySlow slow;
        private Rigidbody2D body;
        private PlayerHealth target;
        private Rect bounds;
        private bool hasBounds, engaged, charging, chargeHit;
        private float nextAttack, actionEndsAt, stunnedUntil, nextContact, nextEruption;
        private Vector2 chargeDirection;
        private Attack pendingAttack;
        private int rotation;
        private readonly List<Attack> available = new List<Attack>();

        public string BossName => bossName;
        public bool IsEngaged => engaged;
        public bool IsWindingUp { get; private set; }
        public int AttacksFired { get; private set; }
        public bool IsEnraged => HealthRatio < .3f;
        private float HealthRatio => health != null && health.MaxHealth > 0 ? health.CurrentHealth / health.MaxHealth : 1f;
        private float DamageMultiplier => level != null ? level.DamageMultiplier : 1f;
        private bool Stunned => Time.time < stunnedUntil;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            level = GetComponent<EnemyLevel>();
            slow = GetComponent<EnemySlow>();
            body = GetComponent<Rigidbody2D>();
        }

        private void OnEnable() => health.Died += Stop;

        private void OnDisable()
        {
            health.Died -= Stop;
            Stop();
        }

        private void Start()
        {
            target = FindFirstObjectByType<PlayerHealth>();
            if (projectile == null)
            {
                Debug.LogError("Magma Golem requires a projectile.", this);
                enabled = false;
            }
        }

        public void SetPatrolArena(Rect arena)
        {
            // Golem se kreće unutar arene, malo dalje od zidova.
            bounds = new Rect(arena.xMin + 1.5f, arena.yMin + 1.5f, arena.width - 3f, arena.height - 3f);
            hasBounds = bounds.width > 0 && bounds.height > 0;
        }

        public void SetEngaged(bool value)
        {
            value &= health != null && !health.IsDead && isActiveAndEnabled;
            if (value && !engaged)
            {
                nextAttack = Time.time + 1.2f;
                nextEruption = Time.time + 6f;
            }

            engaged = value;
            health.SetInvulnerable(!value);
            if (!value)
            {
                IsWindingUp = false;
                charging = false;
            }
        }

        private void FixedUpdate()
        {
            if (!engaged || health.IsDead || target == null || target.IsDead || !hasBounds)
                return;
            Vector2 position = body.position;
            Vector2 toPlayer = (Vector2)target.transform.position - position;
            float speedMultiplier = (level != null ? level.MovementMultiplier : 1f) * (slow != null ? slow.SpeedMultiplier : 1f);

            if (charging)
            {
                Vector2 next = position + chargeDirection * chargeSpeed * Time.fixedDeltaTime;
                if (!chargeHit && toPlayer.magnitude < 1.3f)
                {
                    chargeHit = true;
                    target.TakeDamage(contactDamage * 1.6f * DamageMultiplier);
                    CameraFollow.Shake(.3f, .25f);
                }

                // Juriš staje kod ivice arene; golem udari u zid i ostane ošamućen.
                if (!bounds.Contains(next) || Time.time >= actionEndsAt)
                {
                    charging = false;
                    stunnedUntil = Time.time + (bounds.Contains(next) ? .4f : stunDuration);
                    if (!bounds.Contains(next))
                    {
                        CombatFeedback.Popup("STUNNED!", transform.position + Vector3.up * 1.2f, new Color(1f, .9f, .4f), 4.5f, 1.2f);
                        CameraFollow.Shake(.4f, .3f);
                        Sfx.Play(SfxId.Slam);
                    }
                    return;
                }

                body.MovePosition(next);
                return;
            }

            if (IsWindingUp || Stunned)
                return;
            // Polako prilazi igraču, ali ne napušta arenu.
            if (toPlayer.magnitude > 1.6f)
            {
                float speed = (IsEnraged ? enragedMoveSpeed : moveSpeed) * speedMultiplier;
                Vector2 step = position + toPlayer.normalized * speed * Time.fixedDeltaTime;
                body.MovePosition(new Vector2(Mathf.Clamp(step.x, bounds.xMin, bounds.xMax), Mathf.Clamp(step.y, bounds.yMin, bounds.yMax)));
            }
            else if (Time.time >= nextContact)
            {
                nextContact = Time.time + 1f;
                target.TakeDamage(contactDamage * DamageMultiplier);
            }
        }

        private void Update()
        {
            if (!engaged || health.IsDead || target == null || target.IsDead || Time.timeScale <= 0 || charging || Stunned)
                return;

            // U pobesnelom stanju erupcije stižu i između običnih napada.
            if (IsEnraged && Time.time >= nextEruption && !IsWindingUp)
            {
                nextEruption = Time.time + 5.5f;
                Erupt();
            }

            if (IsWindingUp)
            {
                if (Time.time >= actionEndsAt)
                    Execute(pendingAttack);
                return;
            }

            if (Time.time < nextAttack)
                return;
            pendingAttack = NextAttack();
            IsWindingUp = true;
            float windup = pendingAttack == Attack.Charge ? chargeWindup : pendingAttack == Attack.Slam ? .9f : .55f;
            actionEndsAt = Time.time + windup;
            if (pendingAttack == Attack.Slam)
                HazardTelegraph.Spawn(body.position, slamRadius, windup, damage * 1.8f * DamageMultiplier, target);
            else if (pendingAttack == Attack.Charge)
                CombatFeedback.Popup("!", transform.position + Vector3.up * 1.3f, new Color(1f, .3f, .2f), 7f, .8f);
        }

        private Attack NextAttack()
        {
            available.Clear();
            available.Add(Attack.Slam);
            available.Add(Attack.Barrage);
            if (HealthRatio < .6f)
                available.Add(Attack.Charge);
            if (IsEnraged)
                available.Add(Attack.Eruption);
            return available[rotation++ % available.Count];
        }

        private void Execute(Attack attack)
        {
            IsWindingUp = false;
            AttacksFired++;
            nextAttack = Time.time + (IsEnraged ? enragedAttackInterval : attackInterval);
            switch (attack)
            {
                case Attack.Slam:
                    // Šteta udara je u upozorenju na podu; ovde je samo potres.
                    CameraFollow.Shake(.35f, .3f);
                    break;
                case Attack.Barrage:
                    FireRing(IsEnraged ? 14 : 10, AttacksFired * 9f);
                    if (IsEnraged)
                        FireRing(14, AttacksFired * 9f + 180f / 14f, .75f);
                    break;
                case Attack.Charge:
                    chargeDirection = ((Vector2)target.transform.position - body.position).normalized;
                    charging = chargeDirection.sqrMagnitude > .01f;
                    chargeHit = false;
                    actionEndsAt = Time.time + 1.4f;
                    Sfx.Play(SfxId.Dash);
                    break;
                case Attack.Eruption:
                    Erupt();
                    break;
            }
        }

        // Prsten vatrenih kugli na sve strane.
        private void FireRing(int count, float angleOffset, float speedFactor = 1f)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 direction = Quaternion.Euler(0, 0, angleOffset + i * 360f / count) * Vector2.right;
                var shot = Instantiate(projectile, body.position, Quaternion.identity);
                shot.Launch(direction, shotSpeed * speedFactor, damage * DamageMultiplier, transform);
            }

            Sfx.Play(SfxId.Explosion, .5f);
        }

        // Tri udara lave pod igračem i oko njega; posle udara tlo kratko gori.
        private void Erupt()
        {
            Vector2 center = target.transform.position;
            for (int i = 0; i < 3; i++)
            {
                Vector2 offset = i == 0 ? Vector2.zero : Random.insideUnitCircle.normalized * Random.Range(1.6f, 2.6f);
                HazardTelegraph.Spawn(center + offset, 1.2f, 1.1f, damage * 1.4f * DamageMultiplier, target, true);
            }
        }

        private void Stop()
        {
            engaged = false;
            charging = false;
            IsWindingUp = false;
            if (body != null && health.IsDead)
                body.simulated = false;
        }
    }
}
