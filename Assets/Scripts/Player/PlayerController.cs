// Čita komande za kretanje i primenjuje brzinu igrača
using UnityEngine;

namespace AWizardsFight
{

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField, Min(0f)]
        private float movementSpeed = 5f;
        [SerializeField, Min(0f)]
        private float dashSpeed = 15f;
        [SerializeField, Min(.01f)]
        private float dashDuration = .16f;
        [SerializeField, Min(0f)]
        private float dashCooldown = 1.6f;
        private Rigidbody2D body;
        private PlayerHealth health;
        private SpriteRenderer visual;
        private Vector2 movementInput;
        private Vector2 lastDirection = Vector2.right, dashDirection;
        private float dashEndTime, nextDashTime, nextGhostTime;
        private bool dashInvulnerable;
        private float nextStepTime, nextDustTime;
        private bool wasDashing;
        private float slowFactor = 1f, slowUntil, nextSlowNotice;
        // Na ledu lik ubrzava i usporava postepeno (jedinica brzine u sekundi).
        private const float IceAcceleration = 7f;
        private PlayerProgression progression;
        private PlayerEquipment equipment;
        public float MovementSpeed => movementSpeed;
        public bool IsDashing => Time.time < dashEndTime;
        public float DashCooldownRemaining => Mathf.Max(0f, nextDashTime - Time.time);
        public float DashCooldownTotal { get; private set; } = 1f;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<PlayerHealth>();
            progression = GetComponent<PlayerProgression>();
            equipment = GetComponent<PlayerEquipment>();
            var visualTransform = transform.Find("Visual");
            visual = visualTransform != null ? visualTransform.GetComponent<SpriteRenderer>() : null;
            WallSmoothing.SmoothBody(gameObject);
            if (GetComponent<BodyAnimator>() == null)
                gameObject.AddComponent<BodyAnimator>();
            // Izgled koji je igrač izabrao u glavnom meniju.
            if (visual != null)
                visual.sprite = CharacterLook.Current.Sprite();
        }

        private void Update()
        {
            float horizontal = (Keybinds.Held(GameAction.MoveRight) ? 1f : 0f) - (Keybinds.Held(GameAction.MoveLeft) ? 1f : 0f);
            float vertical = (Keybinds.Held(GameAction.MoveUp) ? 1f : 0f) - (Keybinds.Held(GameAction.MoveDown) ? 1f : 0f);
            var input = new Vector2(horizontal, vertical);
            // Džojstik se koristi kada tastatura ne daje pravac.
            if (input.sqrMagnitude < .01f)
                input = GamepadInput.Move;
            SetMovementInput(input);
            if (Keybinds.Pressed(GameAction.Dash) || GamepadInput.DashPressed)
                TryDash();
            if (movementInput.sqrMagnitude > .01f && Time.timeScale > 0 && Time.time >= nextDustTime && !IsDashing)
            {
                // Mali oblačići prašine iza lika dok trči.
                nextDustTime = Time.time + .18f;
                var dust = RuntimeSprites.Spawn("Dust", RuntimeSprites.Glow, transform.position + Vector3.down * .4f, new Color(.7f, .68f, .75f, .35f), .45f, 4);
                dust.gameObject.AddComponent<FadeOut>().Play(.35f, -movementInput * .6f + Vector2.up * .2f);
            }
            if (!IsDashing && movementInput.sqrMagnitude > .01f && Time.timeScale > 0 && Time.time >= nextStepTime)
            {
                nextStepTime = Time.time + .36f;
                Sfx.Play(SfxId.Footstep, .12f);
            }
            if (IsDashing && Time.time >= nextGhostTime)
            {
                nextGhostTime = Time.time + .04f;
                SpawnGhost();
                SkillEffects.DashStep(transform.position);
            }
            // Kraj izmicanja (za veštinu Ledeni korak).
            if (wasDashing && !IsDashing)
                SkillEffects.DashEnded(transform.position);
            wasDashing = IsDashing;
        }

        // Mraz usporava lika na kratko; jače usporenje zamenjuje slabije.
        public void ApplySlow(float factor, float seconds)
        {
            factor = Mathf.Clamp(factor, .2f, 1f);
            if (Time.time >= slowUntil || factor < slowFactor)
                slowFactor = factor;
            slowUntil = Mathf.Max(slowUntil, Time.time + seconds);
            if (Time.time >= nextSlowNotice)
            {
                nextSlowNotice = Time.time + 1.5f;
                CombatFeedback.Popup("CHILLED", transform.position + Vector3.up * .4f, new Color(.6f, .85f, 1f), 3f, .6f);
            }
        }

        public bool IsSlowed => Time.time < slowUntil;

        // Jedinstveni predmet: dash je odmah spreman.
        public void ResetDash() => nextDashTime = Time.time;

        // Kratak trzaj u pravcu kretanja; tokom njega lik ne prima štetu.
        public bool TryDash()
        {
            if (!isActiveAndEnabled || Time.timeScale <= 0 || Time.time < nextDashTime || (health != null && health.IsDead))
                return false;
            dashDirection = movementInput.sqrMagnitude > .01f ? movementInput.normalized : lastDirection;
            dashEndTime = Time.time + dashDuration;
            DashCooldownTotal = Mathf.Max(.01f, dashCooldown * (progression != null ? progression.DashCooldownMultiplier : 1f) * (equipment != null ? equipment.DashRecoveryMultiplier : 1f));
            nextDashTime = Time.time + DashCooldownTotal;
            Sfx.Play(SfxId.Dash);
            nextGhostTime = Time.time;
            if (health != null)
            {
                health.SetInvulnerable(true);
                dashInvulnerable = true;
            }
            return true;
        }

        // Providna kopija figure ostaje iza lika i brzo nestaje.
        private void SpawnGhost()
        {
            if (visual == null || visual.sprite == null)
                return;
            var ghost = new GameObject("Dash Ghost", typeof(SpriteRenderer), typeof(FadeOut));
            ghost.transform.SetPositionAndRotation(visual.transform.position, visual.transform.rotation);
            ghost.transform.localScale = visual.transform.lossyScale;
            var renderer = ghost.GetComponent<SpriteRenderer>();
            renderer.sprite = visual.sprite;
            renderer.flipX = visual.flipX;
            renderer.color = new Color(.7f, .55f, 1f, .55f);
            renderer.sortingLayerID = visual.sortingLayerID;
            renderer.sortingOrder = visual.sortingOrder - 1;
            ghost.GetComponent<FadeOut>().Play(.22f, Vector2.zero);
        }



        public void SetMovementInput(Vector2 input)
        {

            movementInput = Vector2.ClampMagnitude(input, 1f);
            if (movementInput.sqrMagnitude > .01f)
                lastDirection = movementInput.normalized;
        }

        private void FixedUpdate()
        {

            float speedMultiplier = (progression != null ? progression.MoveSpeedMultiplier : 1f) * (equipment != null ? equipment.MoveSpeedMultiplier : 1f) * (Time.time < slowUntil ? slowFactor : 1f);
            Vector2 desired = IsDashing ? dashDirection * dashSpeed : movementInput * movementSpeed * speedMultiplier;
            body.linearVelocity = !IsDashing && IceSurface.IsIce(body.position)
                ? Vector2.MoveTowards(body.linearVelocity, desired, IceAcceleration * Time.fixedDeltaTime)
                : desired;
            if (!IsDashing)
                EndDashInvulnerability();
        }

        private void EndDashInvulnerability()
        {
            if (!dashInvulnerable)
                return;
            dashInvulnerable = false;
            health.SetInvulnerable(false);
        }

        private void OnDisable()
        {
            dashEndTime = 0f;
            EndDashInvulnerability();
            movementInput = Vector2.zero;
            if (body != null)
                body.linearVelocity = Vector2.zero;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                movementInput = Vector2.zero;
                if (body != null)
                    body.linearVelocity = Vector2.zero;
            }
        }
    }
}
