// Održava rastojanje od igrača i priprema napade projektilima.
using UnityEngine;

namespace AWizardsFight
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyHealth), typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class DarkArcherAI : MonoBehaviour
    {
        [SerializeField]
        private PlayerHealth target = null;
        [SerializeField]
        private Projectile projectile = null;
        [SerializeField, Min(.1f)]
        private float movementSpeed = 1.8f;
        [SerializeField, Min(.1f)]
        private float detectionRange = 8f;
        [SerializeField, Min(.1f)]
        private float loseTargetRange = 10f;
        [SerializeField, Min(.1f)]
        private float firingRange = 5.5f;
        [SerializeField, Min(.1f)]
        private float retreatRange = 2.5f;
        [SerializeField, Min(.1f)]
        private float attackDamage = 8f;
        [SerializeField, Min(.1f)]
        private float projectileSpeed = 7f;
        [SerializeField, Min(.1f)]
        private float cooldown = 1.4f;
        [SerializeField, Min(0)]
        private float windup = .3f;
        [SerializeField]
        private LayerMask obstructionLayers = 0;
        [SerializeField, Min(0), Tooltip("Koliko sekundi strelac traži igrača oko zidova pošto ga izgubi iz vida.")]
        private float pursuitMemory = 4f;
        private readonly EnemyNavigator navigator = new EnemyNavigator();
        private float bodyRadius, lastSeenTime;
        private EnemyHealth health;
        private EnemySlow slow;
        private EnemyLevel level;
        private Rigidbody2D body, targetBody;
        private float fireAt, nextAttack;
        private bool windingUp;
        private bool provoked;
        public bool IsProvoked => provoked;
        public EnemyState State { get; private set; } = EnemyState.Idle;
        public bool IsWindingUp => windingUp;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            slow = GetComponent<EnemySlow>();
            body = GetComponent<Rigidbody2D>();
            level = GetComponent<EnemyLevel>();
            var circle = GetComponent<CircleCollider2D>();
            bodyRadius = circle.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        }

        private void OnEnable()
        {
            health.Died += Die;
            health.HealthChanged += Damaged;
        }

        private void OnDisable()
        {
            health.Died -= Die;
            health.HealthChanged -= Damaged;
            provoked = false;
            windingUp = false;
            body.linearVelocity = Vector2.zero;
        }

        private void Start()
        {
            SetTarget(target != null ? target : FindFirstObjectByType<PlayerHealth>());
            if (projectile == null)
            {
                Debug.LogError("DarkArcherAI requires an enemy projectile prefab.", this);
                enabled = false;
            }
        }

        // Endless: protivnik odmah lovi igrača preko cele arene i ne odustaje.
        public void SetHunting()
        {
            detectionRange = loseTargetRange = 80f;
            pursuitMemory = 9999f;
            provoked = true;
        }

        public void SetTarget(PlayerHealth player)
        {
            target = player;
            targetBody = player != null ? player.GetComponent<Rigidbody2D>() : null;
            Stop();
        }

        private void FixedUpdate()
        {
            if (health.IsDead)
            {
                Die();
                return;
            }

            if (target == null || target.IsDead || !target.gameObject.activeInHierarchy)
            {
                provoked = false;
                Stop();
                return;
            }

            Vector2 targetPosition = targetBody != null ? targetBody.position : (Vector2)target.transform.position;
            Vector2 direction = targetPosition - body.position;
            float distance = direction.magnitude;
            bool visible = !Physics2D.Linecast(body.position, targetPosition, obstructionLayers);
            if (visible)
                lastSeenTime = Time.time;
            else if ((provoked || State != EnemyState.Idle) && Time.time - lastSeenTime <= pursuitMemory)
            {
                // Igrač je iza zida: strelac zaobilazi prepreke da bi ponovo imao čist pogled.
                State = EnemyState.Chase;
                windingUp = false;
                body.linearVelocity = navigator.Steer(body.position, targetPosition, bodyRadius, obstructionLayers) * CurrentSpeed;
                return;
            }

            if ((!provoked 
                && distance > (State == EnemyState.Idle ? detectionRange : loseTargetRange)) 
                || !visible)
            {
                if (!visible)
                    provoked = false;
                Stop();
                return;
            }

            navigator.Reset();
            if (distance > firingRange || distance < retreatRange)
            {
                State = EnemyState.Chase;
                windingUp = false;
                body.linearVelocity = direction.normalized * (distance < retreatRange ? -1 : 1) * CurrentSpeed;
                return;
            }

            State = EnemyState.Attack;
            body.linearVelocity = Vector2.zero;
            if (windingUp)
            {
                if (Time.time < fireAt)
                    return;
                windingUp = false;
                var arrow = Instantiate(projectile, body.position, Quaternion.identity);
                arrow.Launch(direction, projectileSpeed, attackDamage * (level != null ? level.DamageMultiplier : 1f), transform);
                return;
            }

            if (Time.time < nextAttack)
                return;
            windingUp = true;
            fireAt = Time.time + windup;
            nextAttack = Time.time + cooldown / (level != null ? level.AttackRateMultiplier : 1f);
        }

        private float CurrentSpeed => movementSpeed * (slow != null ? slow.SpeedMultiplier : 1) * (level != null ? level.MovementMultiplier : 1f);

        private void Damaged(float current, float maximum)
        {
            if (current <= 0)
                return;
            provoked = true;
            lastSeenTime = Time.time;
        }

        private void Stop()
        {
            if (State != EnemyState.Dead)
                State = EnemyState.Idle;
            navigator.Reset();
            windingUp = false;
            body.linearVelocity = Vector2.zero;
        }

        private void Die()
        {
            State = EnemyState.Dead;
            windingUp = false;
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
        }
    }
}
