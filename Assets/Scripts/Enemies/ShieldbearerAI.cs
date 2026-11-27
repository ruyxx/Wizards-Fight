// Štitonoša drži štit okrenut ka igraču: projektili spreda nanose samo deo štete.
// Štit se okreće sporo i spušta se dok štitonoša napada, pa ga treba zaobići ili sačekati njegov udarac.
using UnityEngine;

namespace AWizardsFight
{
    public sealed class ShieldbearerAI : MeleeEnemyAI, IProjectileBlocker
    {
        [SerializeField, Min(0)]
        private float attackDamage = 24f;
        [SerializeField, Range(-1f, 1f)]
        private float blockCone = .3f;
        [SerializeField, Range(0f, 1f)]
        private float blockedDamageFraction = .3f;
        [SerializeField, Min(10f)]
        private float turnDegreesPerSecond = 110f;
        [SerializeField]
        private SpriteRenderer shield = null;
        private Vector2 shieldFacing = Vector2.down;
        private float lowerUntil;

        protected override float AttackDamage => attackDamage;

        // Štit je spušten tokom pripreme udarca i kratko posle njega.
        private bool ShieldRaised => !IsWindingUp && Time.time >= lowerUntil && State != EnemyState.Dead;

        private void Update()
        {
            if (IsWindingUp)
                lowerUntil = Time.time + .6f;
            shieldFacing = Vector3.RotateTowards(shieldFacing, FacingDirection, turnDegreesPerSecond * Mathf.Deg2Rad * Time.deltaTime, 0f);
            if (shieldFacing.sqrMagnitude < .0001f)
                shieldFacing = Vector2.down;
            shieldFacing.Normalize();
        }

        private void LateUpdate()
        {
            if (shield == null)
                return;
            // Štit stoji između štitonoše i igrača; spušten štit je bleđi i bliže telu.
            bool raised = ShieldRaised;
            shield.transform.position = (Vector2)transform.position + shieldFacing * (raised ? .38f : .2f) + Vector2.down * (raised ? .05f : .25f);
            shield.flipX = shieldFacing.x < 0f;
            shield.sortingOrder = shieldFacing.y > .3f ? 5 : 9;
            shield.color = raised ? Color.white : new Color(.7f, .7f, .7f, .8f);
        }

        public float DamageFactor(Vector2 projectileDirection, Vector2 impactPoint)
        {
            if (!ShieldRaised || projectileDirection.sqrMagnitude < .0001f)
                return 1f;
            if (Vector2.Dot(-projectileDirection.normalized, shieldFacing) <= blockCone)
                return 1f;
            CombatFeedback.Popup("BLOCKED", transform.position + Vector3.up * .4f, new Color(.75f, .8f, .95f), 3f, .5f);
            Sfx.Play(SfxId.Block);
            return blockedDamageFraction;
        }
    }
}
