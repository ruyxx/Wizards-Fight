// Upravlja patroliranjem bossa, pripremom napada i promenom borbene faze.
using UnityEngine;

namespace AWizardsFight
{
    [RequireComponent(typeof(EnemyHealth), typeof(Rigidbody2D))]
    public sealed class DarkSorcererAI : MonoBehaviour, IBossBehaviour
    {
        [SerializeField]
        private PlayerHealth target = null;
        [SerializeField]
        private Projectile projectile = null;
        [SerializeField]
        private LayerMask obstructionLayers = 1 << 9;
        [SerializeField]
        private float damage = 12f;
        [SerializeField]
        private float normalCooldown = 1.6f, enragedCooldown = .9f;
        [SerializeField]
        private float normalSpeed = 6f, enragedSpeed = 8f;
        [SerializeField]
        private float windup = .4f, spreadAngle = 18f;
        [SerializeField, Min(0)]
        private float movementSpeed = 1.8f, enragedMovementSpeed = 2.4f;
        [Header("Faza 3 i udari po podu")]
        [SerializeField]
        private EnemyHealth minionPrefab = null;
        [SerializeField, Range(1, 100)]
        private int minionLevel = 60;
        [SerializeField, Min(1)]
        private int maxMinions = 3;
        [SerializeField, Min(1f)]
        private float summonInterval = 9f;
        [SerializeField, Min(1f)]
        private float phaseTwoSlamInterval = 7f, phaseThreeSlamInterval = 4.5f;
        [SerializeField, Min(.2f)]
        private float slamRadius = 1.3f, slamDelay = 1.1f;
        [SerializeField]
        private Color tint = Color.white;
        [SerializeField]
        private string bossName = "DARK SORCERER";
        private readonly System.Collections.Generic.List<EnemyHealth> minions = new System.Collections.Generic.List<EnemyHealth>();
        private float nextSummon, nextSlam;
        private bool phaseThreeAnnounced;
        private EnemyHealth health;
        private EnemyLevel level;
        private Rigidbody2D body, targetBody;
        private bool engaged, windingUp;
        private float nextAttack, fireAt;
        private Rect patrolBounds;
        private bool hasPatrolBounds;
        private int patrolCorner;
        private EnemySlow slow;
        public bool IsPhaseTwo => health != null && health.CurrentHealth < health.MaxHealth * .5f;
        // Treća faza: boss priziva pomoćnike i češće udara po podu.
        public bool IsPhaseThree => health != null && health.CurrentHealth < health.MaxHealth * .25f;
        public bool IsEngaged => engaged;
        public string BossName => bossName;
        public bool IsEnraged => IsPhaseTwo;
        public bool IsWindingUp => windingUp;
        public int AttacksFired { get; private set; }
        public int LastShotCount { get; private set; }
        public float AttackCooldown => (IsPhaseTwo ? enragedCooldown : normalCooldown) / (level != null ? level.AttackRateMultiplier : 1f);
        public float ShotSpeed => IsPhaseTwo ? enragedSpeed : normalSpeed;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            body = GetComponent<Rigidbody2D>();
            level = GetComponent<EnemyLevel>();
            slow = GetComponent<EnemySlow>();
        }

        private void OnEnable() => health.Died += Stop;

        private void OnDisable()
        {
            health.Died -= Stop;
            Stop();
        }

        private void Start()
        {
            if (target == null)
                target = FindFirstObjectByType<PlayerHealth>();
            if (target != null)
                targetBody = target.GetComponent<Rigidbody2D>();
            if (projectile == null)
            {
                Debug.LogError("Dark Sorcerer requires a projectile.", this);
                enabled = false;
            }

            if (tint != Color.white)
                foreach (var renderer in GetComponentsInChildren<SpriteRenderer>())
                    renderer.color *= tint;
        }

        public void SetEngaged(bool value)
        {
            value &= health != null && !health.IsDead && isActiveAndEnabled;
            if (value && !engaged)
            {
                nextAttack = Mathf.Max(nextAttack, Time.time + .6f);
                nextSlam = Mathf.Max(nextSlam, Time.time + 3f);
            }
            engaged = value;
            health.SetInvulnerable(!value);
            if (!value)
            {
                windingUp = false;
                body.linearVelocity = Vector2.zero;
            }
        }

        public void SetPatrolArena(Rect arena)
        {
            float inset = Mathf.Min(4f, Mathf.Min(arena.width, arena.height) * .3f);
            patrolBounds = new Rect(arena.xMin + inset, arena.yMin + inset, arena.width - inset * 2, arena.height - inset * 2);
            hasPatrolBounds = patrolBounds.width > 0 && patrolBounds.height > 0;
            patrolCorner = 0;
        }

        private void FixedUpdate()
        {
            if (!engaged || !hasPatrolBounds || health.IsDead || target == null || target.IsDead)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 destination = new Vector2(patrolCorner == 0
                || patrolCorner == 3 ? patrolBounds.xMin : patrolBounds.xMax, patrolCorner < 2 ? patrolBounds.yMin : patrolBounds.yMax);
            float speed = (IsPhaseTwo ? enragedMovementSpeed : movementSpeed) * (level != null ? level.MovementMultiplier : 1f) * (slow != null ? slow.SpeedMultiplier : 1f);
            body.MovePosition(Vector2.MoveTowards(body.position, destination, speed * Time.fixedDeltaTime));
            if (Vector2.Distance(body.position, destination) < .1f)
                patrolCorner = (patrolCorner + 1) % 4;
        }

        private void Update()
        {
            if (!engaged || health.IsDead || target == null || target.IsDead || Time.timeScale <= 0)
                return;
            UpdateSpecialAttacks();
            Vector2 aim = (targetBody != null ? targetBody.position : (Vector2)target.transform.position) - body.position;
            if (Physics2D.Linecast(body.position, body.position + aim, obstructionLayers))
            {
                windingUp = false;
                return;
            }

            if (!windingUp)
            {
                if (Time.time < nextAttack)
                    return;
                windingUp = true;
                fireAt = Time.time + windup;
                return;
            }

            if (Time.time < fireAt)
                return;
            windingUp = false;
            LastShotCount = AttacksFired % 2 == 0 ? 1 : 3;
            for (int i = 0; i < LastShotCount; i++)
            {
                float angle = LastShotCount == 1 ? 0 : (i - 1) * spreadAngle;
                Vector2 direction = Quaternion.Euler(0, 0, angle) * aim;
                var shot = Instantiate(projectile, body.position, Quaternion.identity);
                shot.Launch(direction, ShotSpeed, damage * (level != null ? level.DamageMultiplier : 1f), transform);
            }

            AttacksFired++;
            nextAttack = Time.time + AttackCooldown;
        }

        private void UpdateSpecialAttacks()
        {
            float damageMultiplier = level != null ? level.DamageMultiplier : 1f;
            if (IsPhaseTwo && Time.time >= nextSlam)
            {
                // U drugoj fazi jedan udar ispod igrača, u trećoj tri udara oko njega.
                int count = IsPhaseThree ? 3 : 1;
                Vector2 center = target.transform.position;
                for (int i = 0; i < count; i++)
                {
                    Vector2 offset = i == 0 ? Vector2.zero : Random.insideUnitCircle.normalized * Random.Range(1.4f, 2.4f);
                    HazardTelegraph.Spawn(center + offset, slamRadius, slamDelay, damage * 1.5f * damageMultiplier, target);
                }

                nextSlam = Time.time + (IsPhaseThree ? phaseThreeSlamInterval : phaseTwoSlamInterval);
            }

            if (!IsPhaseThree)
                return;
            if (!phaseThreeAnnounced)
            {
                phaseThreeAnnounced = true;
                nextSummon = Time.time + .5f;
                CombatFeedback.Popup("THE SORCERER CALLS FOR AID!", transform.position + Vector3.up, new Color(1f, .35f, .35f), 4.5f, 2f);
                CameraFollow.Shake(.3f, .4f);
            }

            minions.RemoveAll(minion => minion == null || minion.IsDead);
            if (minionPrefab == null || Time.time < nextSummon || minions.Count >= maxMinions)
                return;
            nextSummon = Time.time + summonInterval;
            for (int i = 0; i < 2 && minions.Count < maxMinions; i++)
            {
                Vector2 position = body.position + Random.insideUnitCircle.normalized * 1.8f;
                if (hasPatrolBounds)
                    position = new Vector2(Mathf.Clamp(position.x, patrolBounds.xMin, patrolBounds.xMax), Mathf.Clamp(position.y, patrolBounds.yMin, patrolBounds.yMax));
                var minion = Instantiate(minionPrefab, position, Quaternion.identity);
                minion.Level?.SetLevel(minionLevel);
                minion.ResetToMax();
                minion.GrantsExperience = false;
                minions.Add(minion);
                var flash = RuntimeSprites.Spawn("Summon", RuntimeSprites.Glow, position, new Color(.8f, .3f, 1f, .9f), 2.5f, 18);
                flash.gameObject.AddComponent<FadeOut>().Play(.5f, Vector2.zero);
            }

            Sfx.Play(SfxId.Summon);
        }

        private void Stop()
        {
            engaged = false;
            windingUp = false;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                if (health.IsDead)
                    body.simulated = false;
            }
        }
    }
}
