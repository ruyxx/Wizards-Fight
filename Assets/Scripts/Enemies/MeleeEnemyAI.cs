// Zajedničko ponašanje protivnika koji prilaze igraču i napadaju iz blizine.
using UnityEngine;

namespace AWizardsFight
{
    public enum EnemyState
    {
        Idle,
        Chase,
        Attack,
        Dead
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyHealth), typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public abstract class MeleeEnemyAI : MonoBehaviour
    {
        [SerializeField]
        private PlayerHealth target = null;
        [SerializeField, Min(0)]
        private float movementSpeed = 2.2f;
        [SerializeField, Min(.1f)]
        private float detectionRange = 6f;
        [SerializeField, Min(.1f)]
        private float loseTargetRange = 8f;
        [SerializeField, Min(.1f)]
        private float attackRange = 1.1f;
        protected abstract float AttackDamage { get; }

        [SerializeField, Min(.01f)]
        private float attackCooldown = 1f;
        [SerializeField, Min(0)]
        private float attackWindup = .35f;
        [SerializeField]
        private LayerMask obstructionLayers = 0;
        [SerializeField, Min(0), Tooltip("Koliko sekundi protivnik traži igrača oko zidova pošto ga izgubi iz vida.")]
        private float pursuitMemory = 4f;
        private readonly EnemyNavigator navigator = new EnemyNavigator();
        private float bodyRadius, lastSeenTime;
        private EnemyHealth health;
        private Rigidbody2D body;
        private Rigidbody2D targetBody;
        private EnemySlow slow;
        private EnemyLevel level;
        private float nextAttackTime, hitAt;
        private bool windingUp;
        private bool provoked;
        public bool IsProvoked => provoked;
        public EnemyState State { get; private set; } = EnemyState.Idle;
        public bool IsWindingUp => windingUp;

        protected virtual void Awake()
        {
            health = GetComponent<EnemyHealth>();
            body = GetComponent<Rigidbody2D>();
            slow = GetComponent<EnemySlow>();
            level = GetComponent<EnemyLevel>();
            var circle = GetComponent<CircleCollider2D>();
            bodyRadius = circle.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        }

        protected virtual void OnEnable()
        {
            health.Died += Die;
            health.HealthChanged += Damaged;
        }

        protected virtual void OnDisable()
        {
            health.Died -= Die;
            health.HealthChanged -= Damaged;
            provoked = false;
            windingUp = false;
            body.linearVelocity = Vector2.zero;
        }

        protected virtual void Start()
        {
            
            SetTarget(target != null ? target : FindFirstObjectByType<PlayerHealth>());
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

        protected virtual void FixedUpdate()
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
            Vector2 difference = targetPosition - body.position;
            if (difference.sqrMagnitude > .0001f)
                FacingDirection = difference.normalized;
            float distance = difference.magnitude;
            bool visible = !Physics2D.Linecast(body.position, targetPosition, obstructionLayers);
            float allowedRange = State == EnemyState.Idle ? detectionRange : loseTargetRange;
            if (visible)
                lastSeenTime = Time.time;
            else if ((provoked || State != EnemyState.Idle) && Time.time - lastSeenTime <= pursuitMemory)
            {
                // Igrač je iza zida: protivnik ga traži zaobilazeći prepreke.
                State = EnemyState.Chase;
                windingUp = false;
                body.linearVelocity = navigator.Steer(body.position, targetPosition, bodyRadius, obstructionLayers) * CurrentSpeed;
                return;
            }

            if ((!provoked && distance > allowedRange) || !visible)
            {
                if (!visible)
                    provoked = false;
                Stop();
                return;
            }

            navigator.Reset();
            if (distance > attackRange)
            {
                State = EnemyState.Chase;
                windingUp = false;
                body.linearVelocity = difference.normalized * CurrentSpeed;
                return;
            }

            State = EnemyState.Attack;
            body.linearVelocity = Vector2.zero;
            if (windingUp)
            {
                if (Time.time < hitAt)
                    return;
                windingUp = false;
                
                if (TryGetComponent<BodyAnimator>(out var animator))
                    animator.Kick();
                PerformAttack(target, AttackDamage * (level != null ? level.DamageMultiplier : 1f));
                return;
            }

            if (Time.time < nextAttackTime)
                return;
            windingUp = true;
            hitAt = Time.time + attackWindup;
            nextAttackTime = Time.time + attackCooldown / (level != null ? level.AttackRateMultiplier : 1f);
        }

       
        protected PlayerHealth Target => target;
        protected Vector2 FacingDirection { get; private set; } = Vector2.down;

        // Podrazumevani napad iz blizine; posebni protivnici (bombaš) ga menjaju.
        protected virtual void PerformAttack(PlayerHealth player, float damage) => player.TakeDamage(damage);

        private float CurrentSpeed => movementSpeed * (slow != null ? slow.SpeedMultiplier : 1f) * (level != null ? level.MovementMultiplier : 1f);

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
