// Aktivne čarolije na tasterima Q, R, F i G. Otključavaju se redom kako lik raste u nivou, troše manu i imaju
// svoje vreme oporavka. Jačina svake zavisi od štete trenutnog oružja (sa opremom i veštinama).
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AWizardsFight
{
    public sealed class SpellInfo
    {
        public readonly string Name, Description;
        public readonly int UnlockLevel;
        public readonly float ManaCost, Cooldown;
        public readonly Color Color;

        public SpellInfo(string name, string description, int unlockLevel, float manaCost, float cooldown, Color color)
        {
            Name = name;
            Description = description;
            UnlockLevel = unlockLevel;
            ManaCost = manaCost;
            Cooldown = cooldown;
            Color = color;
        }
    }

    [DisallowMultipleComponent]
    public sealed class PlayerSpells : MonoBehaviour
    {
        public const int Meteor = 0, Blink = 1, Wisps = 2, TimeWarp = 3;
        private static readonly LayerMask EnemyLayers = 1 << 11, WallLayers = 1 << 9;

        public static readonly SpellInfo[] All =
        {
            new SpellInfo("METEOR", "A meteor falls on the cursor: huge fire damage in an area and sets enemies on fire.", 2, 30f, 9f, new Color(1f, .5f, .2f)),
            new SpellInfo("BLINK", "Teleport toward the cursor. Arcane blasts hit enemies where you left and where you land.", 4, 18f, 6f, new Color(.75f, .55f, 1f)),
            new SpellInfo("SPIRIT WISPS", "Two spirits circle you for 14 seconds and shoot the nearest enemies.", 7, 40f, 22f, new Color(.45f, 1f, .9f)),
            new SpellInfo("TIME WARP", "Enemies around you slow to a crawl for 4 seconds while you move faster.", 10, 35f, 18f, new Color(.95f, .85f, .4f)),
        };

        private readonly float[] readyAt = new float[All.Length], totals = new float[All.Length];
        private PlayerHealth health;
        private PlayerMana mana;
        private PlayerCombat combat;
        private PlayerProgression progression;
        private PlayerBuffs buffs;
        private int knownLevel;

        public static GameAction KeyFor(int spell) => GameAction.Spell1 + spell;

        private void Awake()
        {
            health = GetComponent<PlayerHealth>();
            mana = GetComponent<PlayerMana>();
            combat = GetComponent<PlayerCombat>();
            progression = GetComponent<PlayerProgression>();
            buffs = GetComponent<PlayerBuffs>();
            if (buffs == null)
                buffs = gameObject.AddComponent<PlayerBuffs>();
        }

        private void Start() => knownLevel = Level;

        private int Level => progression != null ? progression.Level : 1;
        public bool Unlocked(int spell) => Level >= All[spell].UnlockLevel;
        public float Remaining(int spell) => Mathf.Max(0f, readyAt[spell] - Time.time);
        public float Total(int spell) => Mathf.Max(.01f, totals[spell]);
        public bool EnoughMana(int spell) => mana != null && mana.CurrentMana >= All[spell].ManaCost;

        // Jačina čarolija prati štetu izabranog oružja.
        private float Power => combat != null ? combat.SpellPower : 10f;

        private void Update()
        {
            // Novi nivo može da otključa čaroliju; javi to igraču.
            if (Level != knownLevel)
            {
                for (int i = 0; i < All.Length; i++)
                    if (All[i].UnlockLevel > knownLevel && All[i].UnlockLevel <= Level)
                        Announce(i);
                knownLevel = Level;
            }

            if (!Application.isFocused || health == null || health.IsDead || Time.timeScale <= 0f || MobileMode.Enabled || Camera.main == null)
                return;
            for (int i = 0; i < All.Length; i++)
                if (Keybinds.Pressed(KeyFor(i)))
                {
                    Vector3 cursor = Input.mousePosition;
                    cursor.z = transform.position.z - Camera.main.transform.position.z;
                    TryCast(i, Camera.main.ScreenToWorldPoint(cursor));
                }
        }

        private void Announce(int spell)
        {
            var info = All[spell];
            CombatFeedback.Popup("NEW SPELL: " + info.Name + " [" + Keybinds.KeyName(Keybinds.Get(KeyFor(spell))) + "]", transform.position + Vector3.up * 1.2f, info.Color, 4.5f, 2.6f);
            Sfx.Play(SfxId.LevelUp, .7f);
        }

        public bool TryCast(int spell, Vector2 target)
        {
            var info = All[spell];
            if (!Unlocked(spell))
            {
                CombatFeedback.Popup("UNLOCKS AT LEVEL " + info.UnlockLevel, transform.position, new Color(.8f, .78f, .9f), 3.2f, .8f);
                return false;
            }
            if (Time.time < readyAt[spell])
                return false;
            if (!mana.TryUseMana(info.ManaCost * SkillEffects.ManaCostMultiplier))
            {
                CombatFeedback.Popup("NO MANA", transform.position, new Color(.5f, .7f, 1f), 3.2f, .6f);
                return false;
            }

            Vector2 origin = transform.position;
            switch (spell)
            {
                case Meteor:
                    StartCoroutine(CastMeteor(ReachablePoint(origin, target, 9f)));
                    break;
                case Blink:
                    CastBlink(origin, target);
                    break;
                case Wisps:
                    for (int i = 0; i < 2; i++)
                        SpiritWisp.Summon(transform, i * Mathf.PI, Power * .55f, 14f);
                    Sfx.Play(SfxId.Summon, .7f);
                    break;
                default:
                    CastTimeWarp(origin);
                    break;
            }

            float cooldown = info.Cooldown * (progression != null ? progression.CooldownMultiplier : 1f);
            totals[spell] = cooldown;
            readyAt[spell] = Time.time + cooldown;
            if (TryGetComponent<BodyAnimator>(out var animator))
                animator.Kick();
            return true;
        }

        // Tačka do koje čarolija može da stigne: ne dalje od dometa i ne iza zida.
        private static Vector2 ReachablePoint(Vector2 origin, Vector2 target, float range)
        {
            Vector2 aim = target - origin;
            if (aim.sqrMagnitude < .0001f)
                return origin;
            float distance = Mathf.Min(aim.magnitude, range);
            var hit = Physics2D.Raycast(origin, aim.normalized, distance, WallLayers);
            return hit.collider != null ? hit.point - aim.normalized * .4f : origin + aim.normalized * distance;
        }

        // Šteta svim protivnicima u krugu koje zid ne zaklanja.
        private static List<EnemyHealth> HitArea(Vector2 center, float radius, float damage)
        {
            var hit = new List<EnemyHealth>();
            foreach (var collider in Physics2D.OverlapCircleAll(center, radius, EnemyLayers))
            {
                var enemy = collider.GetComponentInParent<EnemyHealth>();
                if (enemy == null || enemy.IsDead || hit.Contains(enemy) || Physics2D.Linecast(center, collider.ClosestPoint(center), WallLayers))
                    continue;
                hit.Add(enemy);
                if (damage > 0f)
                    enemy.TakeDamage(damage);
            }
            return hit;
        }

        // ---------- Meteor ----------

        private IEnumerator CastMeteor(Vector2 point)
        {
            const float radius = 2.4f;
            var color = All[Meteor].Color;
            var warning = RuntimeSprites.Spawn("Meteor Warning", RuntimeSprites.Ring, point, new Color(1f, .35f, .2f, .9f), radius * 2f, 4);
            var shadow = RuntimeSprites.Spawn("Meteor Shadow", RuntimeSprites.Disc, point, new Color(1f, .3f, .1f, .25f), radius * 2f, -6);
            var rock = RuntimeSprites.Spawn("Meteor", RuntimeSprites.Glow, point + Vector2.up * 7f + Vector2.left * 2f, color, 2.2f, 30);
            const float fall = .6f;
            for (float t = 0f; t < fall; t += Time.deltaTime)
            {
                float k = t / fall;
                rock.transform.position = Vector2.Lerp(point + Vector2.up * 7f + Vector2.left * 2f, point, k * k);
                shadow.color = new Color(1f, .3f, .1f, .2f + .35f * k);
                yield return null;
            }
            Destroy(warning.gameObject);
            Destroy(shadow.gameObject);
            Destroy(rock.gameObject);

            float damage = Power * 3.5f;
            foreach (var enemy in HitArea(point, radius, damage))
                EnemyBurn.Apply(enemy, Power * .5f, 3f);
            var ring = RuntimeSprites.Spawn("Meteor Blast", RuntimeSprites.Ring, point, new Color(1f, .6f, .25f, 1f), .5f, 15);
            ring.gameObject.AddComponent<ExpandingRing>().Play(radius * 2.4f, .35f);
            var glow = RuntimeSprites.Spawn("Meteor Glow", RuntimeSprites.Glow, point, new Color(1f, .55f, .2f, .9f), radius * 3f, -5);
            glow.gameObject.AddComponent<FadeOut>().Play(.6f, Vector2.zero);
            // Ugljenisano tlo ostaje kratko posle udara.
            var scorch = RuntimeSprites.Spawn("Scorch", RuntimeSprites.Disc, point, new Color(.15f, .06f, .04f, .55f), radius * 1.8f, -7);
            scorch.gameObject.AddComponent<FadeOut>().Play(3f, Vector2.zero);
            CombatFeedback.Burst(point, color, 26, 6f);
            CameraFollow.Shake(.3f, .3f);
            Sfx.Play(SfxId.Explosion, 1f);
        }

        // ---------- Blink ----------

        private void CastBlink(Vector2 origin, Vector2 target)
        {
            const float range = 7f, bodyRadius = .4f;
            Vector2 aim = target - origin;
            if (aim.sqrMagnitude < .01f)
                aim = Vector2.right;
            float distance = Mathf.Min(aim.magnitude, range);
            var hit = Physics2D.CircleCast(origin, bodyRadius, aim.normalized, distance, WallLayers);
            Vector2 destination = origin + aim.normalized * (hit.collider != null ? Mathf.Max(0f, hit.distance - .05f) : distance);

            AfterImage(origin);
            ArcaneBurst(origin);
            if (TryGetComponent<Rigidbody2D>(out var body))
            {
                body.position = destination;
                body.linearVelocity = Vector2.zero;
            }
            transform.position = destination;
            ArcaneBurst(destination);
            StartCoroutine(BriefInvulnerability(.35f));
            CameraFollow.Shake(.1f, .12f);
            Sfx.Play(SfxId.Dash, 1f);
        }

        private void ArcaneBurst(Vector2 center)
        {
            HitArea(center, 1.8f, Power * 1.4f);
            var ring = RuntimeSprites.Spawn("Blink Burst", RuntimeSprites.Ring, center, new Color(.8f, .6f, 1f, .95f), .4f, 15);
            ring.gameObject.AddComponent<ExpandingRing>().Play(3.6f, .25f);
            CombatFeedback.Burst(center, All[Blink].Color, 12, 4f);
        }

        private void AfterImage(Vector2 position)
        {
            var visual = transform.Find("Visual");
            if (visual == null || !visual.TryGetComponent<SpriteRenderer>(out var source))
                return;
            var ghost = RuntimeSprites.Spawn("Blink Ghost", source.sprite, position, new Color(.7f, .55f, 1f, .6f), 1f, source.sortingOrder - 1);
            ghost.transform.localScale = visual.lossyScale;
            ghost.flipX = source.flipX;
            ghost.gameObject.AddComponent<FadeOut>().Play(.45f, Vector2.zero);
        }

        private IEnumerator BriefInvulnerability(float seconds)
        {
            if (health.IsInvulnerable)
                yield break;
            health.SetInvulnerable(true);
            yield return new WaitForSeconds(seconds);
            health.SetInvulnerable(false);
        }

        // ---------- Time Warp ----------

        private void CastTimeWarp(Vector2 center)
        {
            const float radius = 5.5f, seconds = 4f;
            foreach (var enemy in HitArea(center, radius, 0f))
                if (enemy.TryGetComponent<EnemySlow>(out var slow))
                    slow.Apply(.25f, seconds);
            buffs.Add("Time Warp", StatType.MoveSpeed, 30f, seconds);
            buffs.Add("Time Warp", StatType.CastSpeed, 25f, seconds);
            var color = All[TimeWarp].Color;
            var ring = RuntimeSprites.Spawn("Time Warp", RuntimeSprites.Ring, center, new Color(color.r, color.g, color.b, .9f), .5f, 15);
            ring.gameObject.AddComponent<ExpandingRing>().Play(radius * 2f, .5f);
            var field = RuntimeSprites.Spawn("Time Field", RuntimeSprites.Disc, center, new Color(color.r, color.g, color.b, .18f), radius * 2f, -6);
            field.gameObject.AddComponent<FadeOut>().Play(seconds, Vector2.zero);
            Sfx.Play(SfxId.Slam, .5f);
        }
    }

    // Duh koji kruži oko igrača i gađa najbližeg protivnika svetlosnim zrakom.
    public sealed class SpiritWisp : MonoBehaviour
    {
        private const float Range = 7.5f, Interval = .6f;
        private static Material beamMaterial;
        private static readonly LayerMask WallLayers = 1 << 9;
        private Transform owner;
        private float angle, damage, expiresAt, nextShot;

        public static void Summon(Transform owner, float startAngle, float damage, float seconds)
        {
            var sprite = RuntimeSprites.Spawn("Spirit Wisp", RuntimeSprites.Glow, owner.position, new Color(.45f, 1f, .9f, .95f), .9f, 25);
            var core = RuntimeSprites.Spawn("Core", RuntimeSprites.Disc, owner.position, new Color(.85f, 1f, 1f, 1f), .3f, 26);
            core.transform.SetParent(sprite.transform, true);
            var wisp = sprite.gameObject.AddComponent<SpiritWisp>();
            wisp.owner = owner;
            wisp.angle = startAngle;
            wisp.damage = damage;
            wisp.expiresAt = Time.time + seconds;
            wisp.nextShot = Time.time + .3f + startAngle * .1f;
        }

        private void Update()
        {
            if (owner == null || Time.time >= expiresAt)
            {
                CombatFeedback.Burst(transform.position, new Color(.45f, 1f, .9f), 8, 3f);
                Destroy(gameObject);
                return;
            }
            angle += Time.deltaTime * 2.4f;
            transform.position = owner.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * .8f + .4f, 0f) * 1.3f;
            if (Time.time < nextShot)
                return;
            var target = Nearest();
            if (target == null)
                return;
            nextShot = Time.time + Interval;
            target.TakeDamage(damage);
            Beam(transform.position, target.transform.position);
        }

        private EnemyHealth Nearest()
        {
            EnemyHealth best = null;
            float bestDistance = Range;
            foreach (var collider in Physics2D.OverlapCircleAll(transform.position, Range, 1 << 11))
            {
                var enemy = collider.GetComponentInParent<EnemyHealth>();
                if (enemy == null || enemy.IsDead || enemy.IsInvulnerable)
                    continue;
                float distance = Vector2.Distance(transform.position, enemy.transform.position);
                if (distance < bestDistance && !Physics2D.Linecast(transform.position, enemy.transform.position, WallLayers))
                {
                    best = enemy;
                    bestDistance = distance;
                }
            }
            return best;
        }

        private static void Beam(Vector3 from, Vector3 to)
        {
            var line = new GameObject("Wisp Beam").AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            line.startWidth = .12f;
            line.endWidth = .05f;
            if (beamMaterial == null)
                beamMaterial = new Material(Shader.Find("Sprites/Default"));
            line.sharedMaterial = beamMaterial;
            line.startColor = new Color(.85f, 1f, 1f, 1f);
            line.endColor = new Color(.45f, 1f, .9f, .8f);
            line.sortingOrder = 40;
            Destroy(line.gameObject, .12f);
            CombatFeedback.Burst(to, new Color(.45f, 1f, .9f), 4, 2f);
        }
    }
}
