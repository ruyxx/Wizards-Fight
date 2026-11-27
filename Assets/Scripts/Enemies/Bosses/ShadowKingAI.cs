// Kralj Senki, boss Senovitog hrama: polako ide ka igraču, ispaljuje spiralu senki, juriša kroz arenu
// i priziva senke-kosture. Ispod 40% zdravlja nastupa pomračenje: svetlo oko igrača se smanjuje, a spirala ubrzava.
using System.Collections.Generic;
using UnityEngine;

namespace AWizardsFight
{
    [RequireComponent(typeof(EnemyHealth), typeof(Rigidbody2D))]
    public sealed class ShadowKingAI : MonoBehaviour, IBossBehaviour
    {
        private enum Attack { Spiral, Charge, Summon, Rain }

        [SerializeField]
        private Projectile bolt = null;
        [SerializeField]
        private EnemyHealth minionPrefab = null;
        [SerializeField]
        private string bossName = "SHADOW KING";
        [SerializeField, Min(0)]
        private float damage = 14f;
        [SerializeField, Min(.2f)]
        private float attackInterval = 2.4f, enragedAttackInterval = 1.5f, moveSpeed = 1.6f;
        private EnemyHealth health;
        private EnemyLevel level;
        private Rigidbody2D body;
        private PlayerHealth target;
        private Rect bounds;
        private bool hasBounds, engaged, eclipse;
        private float nextAttack, actionEndsAt, chargeUntil;
        private Vector2 chargeDirection;
        private int rotation;
        private Attack pending;
        private readonly List<EnemyHealth> minions = new List<EnemyHealth>();

        public string BossName => bossName;
        public bool IsEngaged => engaged;
        public bool IsWindingUp { get; private set; }
        public int AttacksFired { get; private set; }
        public bool IsEnraged => Ratio < .4f;
        private float Ratio => health != null && health.MaxHealth > 0 ? health.CurrentHealth / health.MaxHealth : 1f;
        private float DamageMultiplier => level != null ? level.DamageMultiplier : 1f;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            level = GetComponent<EnemyLevel>();
            body = GetComponent<Rigidbody2D>();
        }

        private void OnEnable() => health.Died += Stop;

        private void OnDisable()
        {
            health.Died -= Stop;
            Stop();
        }

        private void Start() => target = FindFirstObjectByType<PlayerHealth>();

        public void SetPatrolArena(Rect arena)
        {
            bounds = new Rect(arena.xMin + 2f, arena.yMin + 2f, arena.width - 4f, arena.height - 4f);
            hasBounds = bounds.width > 0 && bounds.height > 0;
        }

        public void SetEngaged(bool value)
        {
            value &= health != null && !health.IsDead && isActiveAndEnabled;
            if (value && !engaged)
                nextAttack = Time.time + 1.2f;
            engaged = value;
            health.SetInvulnerable(!value);
            if (!value)
            {
                IsWindingUp = false;
                body.linearVelocity = Vector2.zero;
            }
        }

        private void Update()
        {
            if (!engaged || health.IsDead || target == null || target.IsDead || Time.timeScale <= 0)
                return;
            if (IsEnraged && !eclipse)
            {
                // Pomračenje: tama se zatvara oko igrača.
                eclipse = true;
                DarknessOverlay.SetRadius(3.2f);
                CameraFollow.Shake(.3f, .5f);
                Sfx.Play(SfxId.Summon, 1f);
            }

            Vector2 toPlayer = (Vector2)target.transform.position - body.position;
            if (Time.time < chargeUntil)
            {
                body.linearVelocity = chargeDirection * 11f;
                return;
            }
            body.linearVelocity = IsWindingUp || toPlayer.magnitude < 2.5f ? Vector2.zero : toPlayer.normalized * moveSpeed;
            if (hasBounds)
                body.position = new Vector2(Mathf.Clamp(body.position.x, bounds.xMin, bounds.xMax), Mathf.Clamp(body.position.y, bounds.yMin, bounds.yMax));

            if (IsWindingUp)
            {
                if (Time.time >= actionEndsAt)
                    Execute(pending);
                return;
            }
            if (Time.time < nextAttack)
                return;
            pending = NextAttack();
            IsWindingUp = true;
            float windup = pending == Attack.Charge ? .8f : .6f;
            actionEndsAt = Time.time + windup;
            if (pending == Attack.Charge)
            {
                // Linija na podu pokazuje gde će jurišati.
                chargeDirection = toPlayer.normalized;
                for (int i = 1; i <= 6; i++)
                    HazardTelegraph.Spawn(body.position + chargeDirection * (i * 1.2f), .6f, windup, 0f, target, false, .3f);
            }
        }

        private Attack NextAttack()
        {
            var options = new List<Attack> { Attack.Spiral, Attack.Charge, Attack.Rain };
            if (Ratio < .7f && minionPrefab != null)
                options.Add(Attack.Summon);
            return options[rotation++ % options.Count];
        }

        private void Execute(Attack attack)
        {
            IsWindingUp = false;
            AttacksFired++;
            nextAttack = Time.time + (IsEnraged ? enragedAttackInterval : attackInterval);
            switch (attack)
            {
                case Attack.Spiral:
                    // Spirala kugli senke na sve strane; u pomračenju gušća.
                    int count = IsEnraged ? 16 : 12;
                    float offset = Random.value * 360f;
                    for (int i = 0; i < count; i++)
                    {
                        var direction = Quaternion.Euler(0, 0, offset + i * 360f / count) * Vector2.right;
                        var shot = Instantiate(bolt, body.position, Quaternion.identity);
                        shot.Launch(direction, 5.5f, damage * DamageMultiplier, transform, 1f, 0f);
                    }
                    break;
                case Attack.Charge:
                    chargeUntil = Time.time + .45f;
                    CameraFollow.Shake(.2f, .3f);
                    Sfx.Play(SfxId.Dash, 1f);
                    break;
                case Attack.Rain:
                    // Senke padaju oko igrača.
                    for (int i = 0; i < (IsEnraged ? 5 : 3); i++)
                        HazardTelegraph.Spawn((Vector2)target.transform.position + Random.insideUnitCircle * 2.8f, 1f, 1f, damage * 1.3f * DamageMultiplier, target, false, 0f);
                    break;
                case Attack.Summon:
                    minions.RemoveAll(minion => minion == null || minion.IsDead);
                    for (int i = 0; i < 2 && minions.Count < 3; i++)
                    {
                        Vector2 position = body.position + Random.insideUnitCircle.normalized * 2f;
                        var minion = Instantiate(minionPrefab, position, Quaternion.identity);
                        minion.Level?.SetLevel(95);
                        minion.ResetToMax();
                        minion.GrantsExperience = false;
                        foreach (var renderer in minion.GetComponentsInChildren<SpriteRenderer>())
                            renderer.color *= new Color(.45f, .35f, .7f);
                        minions.Add(minion);
                        var flash = RuntimeSprites.Spawn("Summon", RuntimeSprites.Glow, position, new Color(.5f, .3f, .9f, .9f), 2.5f, 18);
                        flash.gameObject.AddComponent<FadeOut>().Play(.5f, Vector2.zero);
                    }
                    Sfx.Play(SfxId.Summon);
                    break;
            }
        }

        // Kada ga udari u zid, juriš se prekida.
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (Time.time < chargeUntil && collision.gameObject.layer == 9)
            {
                chargeUntil = 0f;
                CameraFollow.Shake(.3f, .25f);
            }
            if (Time.time < chargeUntil && collision.collider.GetComponentInParent<PlayerHealth>() is PlayerHealth player)
            {
                player.TakeDamage(damage * 1.5f * DamageMultiplier);
                chargeUntil = 0f;
            }
        }

        private void Stop()
        {
            engaged = false;
            IsWindingUp = false;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                if (health != null && health.IsDead)
                {
                    body.simulated = false;
                    DarknessOverlay.SetRadius(DarknessOverlay.DefaultRadius);
                }
            }
        }
    }
}
