// Bombaš juri igrača, zapali fitilj kada mu priđe i eksplodira; eksplodira i kada ga igrač pobedi.
using UnityEngine;

namespace AWizardsFight
{
    public sealed class BomberAI : MeleeEnemyAI
    {
        [SerializeField, Min(0)]
        private float attackDamage = 28f;
        [SerializeField, Min(.5f)]
        private float blastRadius = 2.2f;
        [SerializeField, Range(0f, 1f)]
        private float enemyDamageFraction = .6f;
        [SerializeField]
        private LayerMask enemyLayers = 1 << 11;
        [SerializeField]
        private LayerMask wallLayers = 1 << 9;
        [SerializeField]
        private GameObject blastVisual = null;
        private EnemyHealth health;
        private EnemyLevel enemyLevel;
        private SpriteRenderer fuse;
        private bool exploded;

        protected override float AttackDamage => attackDamage;

        protected override void Awake()
        {
            base.Awake();
            health = GetComponent<EnemyHealth>();
            enemyLevel = GetComponent<EnemyLevel>();
            fuse = RuntimeSprites.Spawn("Fuse Glow", RuntimeSprites.Glow, transform.position, new Color(1f, .45f, .15f, 0f), 1.6f, 9);
            fuse.transform.SetParent(transform, true);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            health.Died += Explode;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            health.Died -= Explode;
        }

        private void Update()
        {
            if (fuse == null)
                return;
            // Fitilj treperi sve brže dok traje priprema eksplozije.
            float alpha = IsWindingUp ? .45f + .45f * Mathf.Sin(Time.time * 30f) : 0f;
            fuse.color = new Color(1f, .45f, .15f, Mathf.Max(0f, alpha));
        }

        protected override void PerformAttack(PlayerHealth player, float damage)
        {
            health.GrantsExperience = false;
            health.Kill();
        }

        private void Explode()
        {
            if (exploded || SaveSystem.IsRestoring)
                return;
            exploded = true;
            Vector2 center = transform.position;
            float damage = attackDamage * (enemyLevel != null ? enemyLevel.DamageMultiplier : 1f);
            var player = Target;
            if (player != null && !player.IsDead
                && Vector2.Distance(center, player.transform.position) <= blastRadius
                && !Physics2D.Linecast(center, player.transform.position, wallLayers))
                player.TakeDamage(damage);

            // Eksplozija pogađa i obližnje protivnike, pa je bombaše dobro upucati u gomili.
            foreach (var collider in Physics2D.OverlapCircleAll(center, blastRadius, enemyLayers))
            {
                var enemy = collider.GetComponentInParent<EnemyHealth>();
                if (enemy == null || enemy == health || enemy.IsDead || Physics2D.Linecast(center, collider.ClosestPoint(center), wallLayers))
                    continue;
                enemy.TakeDamage(damage * enemyDamageFraction);
            }

            if (blastVisual != null)
            {
                var visual = Instantiate(blastVisual, center, Quaternion.identity);
                visual.transform.localScale = Vector3.one * blastRadius;
                Destroy(visual, FireBlastVisual.Lifetime + .05f);
            }

            var flash = RuntimeSprites.Spawn("Blast Flash", RuntimeSprites.Glow, center, new Color(1f, .6f, .2f, .9f), blastRadius * 2.4f, 20);
            flash.gameObject.AddComponent<FadeOut>().Play(.35f, Vector2.zero);
            CameraFollow.Shake(.35f, .3f);
            Sfx.Play(SfxId.Explosion);
        }
    }
}
