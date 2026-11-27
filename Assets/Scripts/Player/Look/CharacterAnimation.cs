// Povezuje kretanje, pripremu napada i smrt sa vizuelnom animacijom lika.
using UnityEngine;

namespace AWizardsFight
{
    public sealed class CharacterAnimation : MonoBehaviour
    {
        private Animator animator;
        private SpriteRenderer visual;
        private Rigidbody2D body;
        private PlayerHealth player;
        private EnemyHealth enemy;
        private PlayerCombat combat;
        private MeleeEnemyAI melee;
        private DarkArcherAI archer;
        private IBossBehaviour boss;
        private float previousCooldown, castUntil;
        private bool dead;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            visual = animator.GetComponent<SpriteRenderer>();
            body = GetComponent<Rigidbody2D>();
            player = GetComponent<PlayerHealth>();
            enemy = GetComponent<EnemyHealth>();
            combat = GetComponent<PlayerCombat>();
            melee = GetComponent<MeleeEnemyAI>();
            archer = GetComponent<DarkArcherAI>();
            boss = GetComponent<IBossBehaviour>();
        }

        private void OnEnable()
        {
            if (player != null)
                player.Died += Die;
            if (enemy != null)
                enemy.Died += Die;
        }

        private void OnDisable()
        {
            if (player != null)
                player.Died -= Die;
            if (enemy != null)
                enemy.Died -= Die;
        }

        private void Update()
        {
            if (dead)
                return;
            if (combat != null)
            {
                float cooldown = combat.CooldownRemaining;
                // Novi skok oporavka znači da je upravo bačena čarolija.
                if (cooldown > previousCooldown + .01f)
                    castUntil = Time.time + .18f;
                previousCooldown = cooldown;
            }

            // Animacija napada važi za igrača ili za tip neprijatelja prisutan na ovom objektu.
            bool attack = Time.time < castUntil
                || melee != null
                && melee.IsWindingUp
                || archer != null
                && archer.IsWindingUp
                || boss != null
                && boss.IsWindingUp;
            animator.SetBool("Moving", body != null && body.linearVelocity.sqrMagnitude > .01f);
            animator.SetBool("Attacking", attack);
            if (body != null && Mathf.Abs(body.linearVelocity.x) > .05f)
                visual.flipX = body.linearVelocity.x < 0;
        }

        private void Die()
        {
            dead = true;

            var remnant = new GameObject("Death Visual", typeof(SpriteRenderer), typeof(Animator), typeof(DeathVisual));
            remnant.transform.position = visual.transform.position;
            var renderer = remnant.GetComponent<SpriteRenderer>();
            renderer.sprite = visual.sprite;
            renderer.sortingOrder = visual.sortingOrder;
            renderer.flipX = visual.flipX;
            var animation = remnant.GetComponent<Animator>();
            animation.runtimeAnimatorController = animator.runtimeAnimatorController;
            animation.updateMode = AnimatorUpdateMode.UnscaledTime;
            animation.SetBool("Dead", true);
            visual.enabled = false;
        }
    }

    public sealed class DeathVisual : MonoBehaviour
    {
        private float remaining = .6f;

        private void Update()
        {
            remaining -= Time.unscaledDeltaTime;
            if (remaining <= 0)
                Destroy(gameObject);
        }
    }
}
