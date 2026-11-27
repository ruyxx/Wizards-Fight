// Efekti veština u borbi: menjaju štetu pogotka prema stanju mete (zaleđena, goruća, otrovana),
// dodaju gorenje, eksplozije i lomljenje leda pri smrti, ehoe čarolija, štit od mane i tragove pri izmicanju.
using UnityEngine;

namespace AWizardsFight
{
    public static class SkillEffects
    {
        private static PlayerProgression progression;

        private static PlayerProgression Progression
        {
            get
            {
                if (progression == null)
                    progression = Object.FindFirstObjectByType<PlayerProgression>();
                return progression;
            }
        }

        public static int Rank(string id) => Progression != null ? Progression.Rank(id) : 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            progression = null;
            EnemyHealth.AnyDied += EnemyDied;
        }

        public static bool IsChilled(EnemyHealth enemy) => enemy.TryGetComponent<EnemySlow>(out var slow) && slow.SpeedMultiplier < 1f;
        public static bool IsBurning(EnemyHealth enemy) => enemy.TryGetComponent<EnemyBurn>(out var burn) && burn.Burning;
        public static bool IsPoisoned(EnemyHealth enemy) => enemy.TryGetComponent<EnemyPoison>(out var poison) && poison.Active;

        // Šteta igračevog pogotka posle bonusa veština; kombinacije prikazuju natpis iznad mete.
        public static float ModifyDamage(EnemyHealth enemy, float damage, WeaponType type)
        {
            if (Progression == null || enemy == null)
                return damage;
            if (UniqueItems.Has("bloodpact"))
                damage *= 1.3f;
            bool chilled = IsChilled(enemy);
            if (chilled)
                damage *= 1f + .1f * Rank("frostbite");
            if (IsBurning(enemy))
                damage *= 1f + .15f * Rank("searing");
            if (chilled && type == WeaponType.FireStaff && Rank("thermal") > 0)
            {
                damage *= 1.6f;
                if (enemy.TryGetComponent<EnemySlow>(out var slow))
                    slow.Clear();
                Combo(enemy, "THERMAL SHOCK!", new Color(1f, .6f, .3f));
            }
            if (chilled && type == WeaponType.StormStaff && Rank("frozenstorm") > 0)
            {
                damage *= 1.4f;
                Combo(enemy, "FROZEN STORM!", new Color(.6f, .9f, 1f));
            }
            return damage;
        }

        // Posle pogotka: paljenje i hlađenje munjom.
        public static void OnHit(EnemyHealth enemy, float damage, WeaponType type)
        {
            if (Progression == null || enemy == null || enemy.IsDead)
                return;
            float igniteChance = .1f * Rank("kindling") + (Rank("inferno") > 0 ? .2f : 0f);
            if (type == WeaponType.FireStaff)
                igniteChance += UniqueItems.Has("emberheart") ? 1f : .1f;
            if (igniteChance > 0f && Random.value < igniteChance)
                EnemyBurn.Apply(enemy, damage * .4f / 3f, 3f);
            if (type == WeaponType.StormStaff && Rank("frozenstorm") > 0 && enemy.TryGetComponent<EnemySlow>(out var slow))
                slow.Apply(.6f, 1.5f);
            // Stormcaller: munja sa neba na obližnje protivnike.
            if (UniqueItems.Has("stormcaller") && Random.value < .2f)
            {
                int struck = 0;
                Area(enemy.transform.position, 3.5f, 0f, null, other =>
                {
                    if (struck++ >= 3)
                        return;
                    other.TakeDamage(damage * .6f);
                    var bolt = RuntimeSprites.Spawn("Sky Bolt", RuntimeSprites.Square, other.transform.position + Vector3.up * 2f, new Color(1f, 1f, .6f, .9f), 1f, 19);
                    bolt.transform.localScale = new Vector3(.12f, 4f, 1f);
                    bolt.gameObject.AddComponent<FadeOut>().Play(.2f, Vector2.zero);
                });
            }
        }

        public static int ExtraChainJumps => Rank("conduit");
        public static float PoisonMultiplier => 1f + .3f * Rank("virulence");
        public static bool EchoCast => Random.value < .08f * Rank("echo") + (UniqueItems.Has("crown") ? .15f : 0f) + (UniqueItems.Has("mirror") ? .25f : 0f);
        public static float ManaCostMultiplier => UniqueItems.Has("arcanist") ? .7f : 1f;

        // Štit od mane: deo štete plaća mana, ako je ima.
        public static float AbsorbWithMana(PlayerHealth health, float amount)
        {
            if (Rank("manashield") <= 0 || !health.TryGetComponent<PlayerMana>(out var mana))
                return amount;
            float share = Mathf.Min(amount * .3f, mana.CurrentMana);
            if (share > 0f && mana.TryUseMana(share))
                return amount - share;
            return amount;
        }

        private static void EnemyDied(EnemyHealth enemy)
        {
            if (Progression == null || enemy == null)
                return;
            Vector2 position = enemy.transform.position;
            bool burning = IsBurning(enemy), chilled = IsChilled(enemy), poisoned = IsPoisoned(enemy);
            float power = 12f + 2.5f * Progression.Level;
            if (burning && Rank("combustion") > 0)
            {
                Area(position, 2.4f, power * 1.5f, enemy, other => EnemyBurn.Apply(other, power * .2f, 2f));
                CombatFeedback.Burst(position, new Color(1f, .55f, .2f), 22, 4f);
                var flash = RuntimeSprites.Spawn("Combustion", RuntimeSprites.Glow, position, new Color(1f, .5f, .15f, .9f), 4.5f, 18);
                flash.gameObject.AddComponent<FadeOut>().Play(.35f, Vector2.zero);
                CameraFollow.Shake(.12f, .15f);
                Sfx.Play(SfxId.Explosion, .6f);
            }
            if (chilled && Rank("shatter") > 0)
            {
                Area(position, 2.6f, power, enemy, other =>
                {
                    if (other.TryGetComponent<EnemySlow>(out var slow))
                        slow.Apply(.5f, 2f);
                });
                CombatFeedback.Burst(position, new Color(.7f, .9f, 1f), 18, 3.5f);
                Sfx.Play(SfxId.Block, .6f);
            }
            if (poisoned && Rank("plaguefire") > 0)
            {
                Area(position, 3f, 0f, enemy, other => EnemyPoison.Apply(other, power * .3f, 4f));
                Combo(enemy, "PLAGUE!", new Color(.5f, 1f, .4f));
            }
        }

        // Šteta i efekat svim živim protivnicima u krugu (osim izvora).
        public static void Area(Vector2 center, float radius, float damage, EnemyHealth exclude, System.Action<EnemyHealth> effect)
        {
            foreach (var collider in Physics2D.OverlapCircleAll(center, radius, 1 << 11))
            {
                var other = collider.GetComponentInParent<EnemyHealth>();
                if (other == null || other == exclude || other.IsDead)
                    continue;
                if (damage > 0f)
                    other.TakeDamage(damage);
                effect?.Invoke(other);
            }
        }

        private static void Combo(EnemyHealth enemy, string text, Color color) =>
            CombatFeedback.Popup(text, enemy.transform.position + Vector3.up * .9f, color, 3.4f, .8f);

        // Izmicanje: vatreni trag i ledena nova na kraju.
        public static void DashStep(Vector2 position)
        {
            if ((Rank("blazingtrail") > 0 || UniqueItems.Has("golemheart")) && Progression != null)
                FireTrail.Spawn(position, 6f + 1.5f * Progression.Level);
            if (UniqueItems.Has("frostweave"))
                Area(position, .9f, 0f, null, other =>
                {
                    if (other.TryGetComponent<EnemySlow>(out var slow))
                        slow.Apply(.5f, 2f);
                });
        }

        public static void DashEnded(Vector2 position)
        {
            if (Rank("glacialstep") <= 0 || Progression == null)
                return;
            Area(position, 3f, 8f + 2f * Progression.Level, null, other =>
            {
                if (other.TryGetComponent<EnemySlow>(out var slow))
                    slow.Apply(.4f, 2.5f);
            });
            var nova = RuntimeSprites.Spawn("Glacial Nova", RuntimeSprites.Ring, position, new Color(.6f, .9f, 1f, .9f), 6f, 18);
            nova.gameObject.AddComponent<FadeOut>().Play(.4f, Vector2.zero);
        }
    }

    // Gorenje protivnika: šteta na svakih pola sekunde, uz narandžasti sjaj.
    public sealed class EnemyBurn : MonoBehaviour
    {
        private EnemyHealth health;
        private float damagePerSecond, expiresAt, nextTick;
        private SpriteRenderer glow;
        public bool Burning => Time.time < expiresAt;
        public bool Active => Burning && health != null && !health.IsDead;

        public static void Apply(EnemyHealth enemy, float damagePerSecond, float seconds)
        {
            if (enemy == null || enemy.IsDead || !(damagePerSecond > 0f))
                return;
            if (!enemy.TryGetComponent<EnemyBurn>(out var burn))
                burn = enemy.gameObject.AddComponent<EnemyBurn>();
            burn.damagePerSecond = Mathf.Max(burn.Active ? burn.damagePerSecond : 0f, damagePerSecond);
            burn.expiresAt = Time.time + seconds;
        }

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            glow = RuntimeSprites.Spawn("Burn", RuntimeSprites.Glow, transform.position, new Color(1f, .45f, .1f, 0f), 1.6f, 9);
            glow.transform.SetParent(transform, true);
        }

        private void Update()
        {
            glow.color = new Color(1f, .45f, .1f, Active ? .35f + .1f * Mathf.Sin(Time.time * 12f) : 0f);
            if (!Active || Time.time < nextTick)
                return;
            nextTick = Time.time + .5f;
            // Kuga-vatra: goruć i otrovan protivnik trpi duplo od otrova (ovde dodatni deo).
            float bonus = SkillEffects.Rank("plaguefire") > 0 && SkillEffects.IsPoisoned(health) ? 2f : 1f;
            health.TakeDamage(damagePerSecond * .5f * bonus);
        }
    }

    // Vatreni trag izmicanja: kratko gori i peče protivnike koji prođu kroz njega.
    public sealed class FireTrail : MonoBehaviour
    {
        private float damage, until, nextTick;

        public static void Spawn(Vector2 position, float damagePerSecond)
        {
            var flame = RuntimeSprites.Spawn("Fire Trail", RuntimeSprites.Glow, position, new Color(1f, .5f, .15f, .7f), 1.3f, 3).gameObject.AddComponent<FireTrail>();
            flame.damage = damagePerSecond;
            flame.until = Time.time + 2.5f;
        }

        private void Update()
        {
            if (Time.time > until)
            {
                Destroy(gameObject);
                return;
            }
            var sprite = GetComponent<SpriteRenderer>();
            sprite.color = new Color(1f, .5f, .15f, .7f * (until - Time.time) / 2.5f + .1f * Mathf.Sin(Time.time * 14f));
            if (Time.time < nextTick)
                return;
            nextTick = Time.time + .5f;
            SkillEffects.Area(transform.position, .7f, damage * .5f, null, enemy => EnemyBurn.Apply(enemy, damage * .3f, 1.5f));
        }
    }
}
