// Frost Lich, boss ledene kripte: teleportuje se po areni i smenjuje ledene kristale, ledeni talas i niz šiljaka.
// Ispod 60% zdravlja priziva ledene kosture, a ispod 30% diže mećavu: cela arena usporava igrača i ledenice padaju oko njega.
using System.Collections.Generic;
using UnityEngine;

namespace AWizardsFight
{
    [RequireComponent(typeof(EnemyHealth), typeof(Rigidbody2D))]
    public sealed class FrostLichAI : MonoBehaviour, IBossBehaviour
    {
        private enum Attack
        {
            Shards,
            Nova,
            Spikes,
            Summon
        }

        [SerializeField]
        private Projectile shard = null;
        [SerializeField]
        private EnemyHealth minionPrefab = null;
        [SerializeField]
        private string bossName = "FROST LICH";
        [SerializeField, Min(0)]
        private float damage = 13f;
        [SerializeField, Min(.2f)]
        private float attackInterval = 2.2f, enragedAttackInterval = 1.4f;
        [SerializeField, Min(1f)]
        private float teleportInterval = 6f;
        private EnemyHealth health;
        private EnemyLevel level;
        private Rigidbody2D body;
        private PlayerHealth target;
        private Rect bounds;
        private bool hasBounds, engaged;
        private float nextAttack, actionEndsAt, nextTeleport, nextIcicles, lastHealth;
        private int rotation;
        private Attack pending;
        private SpriteRenderer[] renderers;
        private readonly List<Attack> available = new List<Attack>();
        private readonly List<EnemyHealth> minions = new List<EnemyHealth>();

        public string BossName => bossName;
        public bool IsEngaged => engaged;
        public bool IsWindingUp { get; private set; }
        public int AttacksFired { get; private set; }
        public bool IsEnraged => Ratio < .3f;
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

        private void Start()
        {
            target = FindFirstObjectByType<PlayerHealth>();
            renderers = GetComponentsInChildren<SpriteRenderer>();
            lastHealth = health.CurrentHealth;
            if (shard == null)
            {
                Debug.LogError("Frost Lich requires a shard projectile.", this);
                enabled = false;
            }
        }

        public void SetPatrolArena(Rect arena)
        {
            bounds = new Rect(arena.xMin + 2f, arena.yMin + 2f, arena.width - 4f, arena.height - 4f);
            hasBounds = bounds.width > 0 && bounds.height > 0;
        }

        public void SetEngaged(bool value)
        {
            value &= health != null && !health.IsDead && isActiveAndEnabled;
            if (value && !engaged)
            {
                nextAttack = Time.time + 1f;
                nextTeleport = Time.time + teleportInterval;
                nextIcicles = Time.time + 3f;
            }

            engaged = value;
            health.SetInvulnerable(!value);
            if (!value)
                IsWindingUp = false;
        }

        private void Update()
        {
            if (!engaged || health.IsDead || target == null || target.IsDead || Time.timeScale <= 0)
                return;

            // Lebdi gore-dole, a dok se sprema za napad svetli.
            float bob = Mathf.Sin(Time.time * 2.5f) * .06f;
            foreach (var renderer in renderers)
                if (renderer != null && renderer.name == "Visual")
                    renderer.transform.localPosition = new Vector3(0, .2f + bob, 0);

            // Mećava: igrač je usporen i ledenice padaju oko njega.
            if (IsEnraged)
            {
                target.GetComponent<PlayerController>()?.ApplySlow(.8f, .3f);
                if (Time.time >= nextIcicles)
                {
                    nextIcicles = Time.time + 2.2f;
                    for (int i = 0; i < 3; i++)
                        HazardTelegraph.Spawn((Vector2)target.transform.position + Random.insideUnitCircle * 2.5f, .9f, 1f, damage * 1.2f * DamageMultiplier, target, false, .5f);
                }
            }

            // Posle većeg gubitka zdravlja ili posle nekog vremena lich nestaje i pojavljuje se drugde.
            bool hurt = lastHealth - health.CurrentHealth > health.MaxHealth * .08f;
            if (!IsWindingUp && (Time.time >= nextTeleport || hurt))
            {
                Teleport();
                return;
            }

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
            float windup = pending == Attack.Nova ? 1f : pending == Attack.Spikes ? .5f : .6f;
            actionEndsAt = Time.time + windup;
            if (pending == Attack.Nova)
                HazardTelegraph.Spawn(body.position, 3.2f, windup, damage * 1.8f * DamageMultiplier, target, false, .4f);
        }

        private Attack NextAttack()
        {
            available.Clear();
            available.Add(Attack.Shards);
            available.Add(Vector2.Distance(body.position, target.transform.position) < 4f ? Attack.Nova : Attack.Spikes);
            available.Add(Attack.Shards);
            if (Ratio < .6f && minionPrefab != null)
                available.Add(Attack.Summon);
            return available[rotation++ % available.Count];
        }

        private void Execute(Attack attack)
        {
            IsWindingUp = false;
            AttacksFired++;
            nextAttack = Time.time + (IsEnraged ? enragedAttackInterval : attackInterval);
            Vector2 aim = (Vector2)target.transform.position - body.position;
            switch (attack)
            {
                case Attack.Shards:
                    // Lepeza ledenih kristala; kad je pobesneo, dve lepeze zaredom.
                    int count = IsEnraged ? 7 : 5;
                    for (int i = 0; i < count; i++)
                    {
                        float angle = (i - (count - 1) / 2f) * 11f;
                        var shot = Instantiate(shard, body.position, Quaternion.identity);
                        shot.Launch(Quaternion.Euler(0, 0, angle) * aim, 7f, damage * DamageMultiplier, transform, .5f, 1.5f);
                    }
                    break;
                case Attack.Nova:
                    CameraFollow.Shake(.25f, .2f);
                    break;
                case Attack.Spikes:
                    // Niz šiljaka od licha prema igraču.
                    Vector2 direction = aim.normalized;
                    for (int i = 1; i <= 7; i++)
                        HazardTelegraph.Spawn(body.position + direction * (i * 1.3f), .75f, .45f + i * .12f, damage * 1.4f * DamageMultiplier, target, false, .5f);
                    break;
                case Attack.Summon:
                    minions.RemoveAll(minion => minion == null || minion.IsDead);
                    for (int i = 0; i < 2 && minions.Count < 3; i++)
                    {
                        Vector2 position = ClampToArena(body.position + Random.insideUnitCircle.normalized * 2f);
                        var minion = Instantiate(minionPrefab, position, Quaternion.identity);
                        minion.Level?.SetLevel(85);
                        minion.ResetToMax();
                        minion.GrantsExperience = false;
                        foreach (var renderer in minion.GetComponentsInChildren<SpriteRenderer>())
                            renderer.color *= new Color(.6f, .85f, 1f);
                        minions.Add(minion);
                        var flash = RuntimeSprites.Spawn("Summon", RuntimeSprites.Glow, position, new Color(.5f, .8f, 1f, .9f), 2.5f, 18);
                        flash.gameObject.AddComponent<FadeOut>().Play(.5f, Vector2.zero);
                    }
                    Sfx.Play(SfxId.Summon);
                    break;
            }
        }

        private Vector2 ClampToArena(Vector2 position) => hasBounds ? new Vector2(Mathf.Clamp(position.x, bounds.xMin, bounds.xMax), Mathf.Clamp(position.y, bounds.yMin, bounds.yMax)) : position;

        // Nestaje u oblaku leda i pojavljuje se na mestu udaljenom od igrača.
        private void Teleport()
        {
            nextTeleport = Time.time + teleportInterval * (IsEnraged ? .6f : 1f);
            lastHealth = health.CurrentHealth;
            if (!hasBounds)
                return;
            Vector2 best = body.position;
            float bestDistance = -1f;
            for (int i = 0; i < 12; i++)
            {
                var candidate = new Vector2(Random.Range(bounds.xMin, bounds.xMax), Random.Range(bounds.yMin, bounds.yMax));
                float distance = Vector2.Distance(candidate, target.transform.position);
                if (distance > 3.5f && distance < 9f && distance > bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            var puff = RuntimeSprites.Spawn("Frost Puff", RuntimeSprites.Glow, body.position, new Color(.6f, .9f, 1f, .9f), 3f, 18);
            puff.gameObject.AddComponent<FadeOut>().Play(.5f, Vector2.zero);
            body.position = best;
            transform.position = best;
            var arrive = RuntimeSprites.Spawn("Frost Puff", RuntimeSprites.Glow, best, new Color(.6f, .9f, 1f, .9f), 3f, 18);
            arrive.gameObject.AddComponent<FadeOut>().Play(.5f, Vector2.zero);
            Sfx.Play(SfxId.Dash, .7f);
            nextAttack = Mathf.Max(nextAttack, Time.time + .5f);
        }

        private void Stop()
        {
            engaged = false;
            IsWindingUp = false;
            if (body != null && health.IsDead)
                body.simulated = false;
        }
    }
}
