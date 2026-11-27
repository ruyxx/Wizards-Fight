// Efekti posebnih čarolija: vatreno polje koje peče protivnike i ledeni talas oko igrača.
using System.Collections.Generic;
using UnityEngine;

namespace AWizardsFight
{
    public sealed class FlameField : MonoBehaviour
    {
        private const float Radius = 1.7f, Duration = 3f, TickInterval = .25f;
        private static readonly LayerMask EnemyLayers = 1 << 11;
        private static readonly LayerMask WallLayers = 1 << 9;
        private float damagePerSecond, elapsed, nextTick;
        private SpriteRenderer glow;

        public static void Create(Vector2 position, float damagePerSecond, GameObject burstVisual)
        {
            var field = RuntimeSprites.Spawn("Flame Field", RuntimeSprites.Disc, position, new Color(1f, .42f, .12f, .55f), Radius * 2f, -6);
            var component = field.gameObject.AddComponent<FlameField>();
            component.damagePerSecond = damagePerSecond;
            component.glow = RuntimeSprites.Spawn("Flame Glow", RuntimeSprites.Glow, position, new Color(1f, .7f, .3f, .6f), Radius * 3f, -5);
            component.glow.transform.SetParent(field.transform, true);
            if (burstVisual != null)
            {
                var burst = Instantiate(burstVisual, position, Quaternion.identity);
                burst.transform.localScale = Vector3.one * Radius;
                Destroy(burst, FireBlastVisual.Lifetime + .05f);
            }

            Sfx.Play(SfxId.Explosion, .6f);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float fade = elapsed > Duration - .4f ? Mathf.Clamp01((Duration - elapsed) / .4f) : 1f;
            float flicker = .85f + .15f * Mathf.PerlinNoise(Time.time * 7f, transform.position.x);
            var renderer = GetComponent<SpriteRenderer>();
            renderer.color = new Color(1f, .42f, .12f, .55f * fade * flicker);
            glow.color = new Color(1f, .7f, .3f, .6f * fade * flicker);
            if (elapsed >= nextTick)
            {
                nextTick += TickInterval;
                Burn();
            }

            if (elapsed >= Duration)
                Destroy(gameObject);
        }

        private void Burn()
        {
            Vector2 center = transform.position;
            var damaged = new HashSet<EnemyHealth>();
            foreach (var collider in Physics2D.OverlapCircleAll(center, Radius, EnemyLayers))
            {
                var enemy = collider.GetComponentInParent<EnemyHealth>();
                if (enemy == null || enemy.IsDead || !damaged.Add(enemy) || Physics2D.Linecast(center, collider.ClosestPoint(center), WallLayers))
                    continue;
                enemy.TakeDamage(damagePerSecond * TickInterval);
            }
        }
    }

    public static class FrostNova
    {
        private const float Radius = 3f;
        private static readonly LayerMask EnemyLayers = 1 << 11;
        private static readonly LayerMask WallLayers = 1 << 9;

        public static void Cast(Vector2 center, float damage, float slowFactor, float slowSeconds)
        {
            var damaged = new HashSet<EnemyHealth>();
            foreach (var collider in Physics2D.OverlapCircleAll(center, Radius, EnemyLayers))
            {
                var enemy = collider.GetComponentInParent<EnemyHealth>();
                if (enemy == null || enemy.IsDead || !damaged.Add(enemy) || Physics2D.Linecast(center, collider.ClosestPoint(center), WallLayers))
                    continue;
                enemy.GetComponent<EnemySlow>()?.Apply(slowFactor, slowSeconds);
                enemy.TakeDamage(damage);
            }

            // Prsten leda se širi od igrača.
            var ring = RuntimeSprites.Spawn("Frost Nova", RuntimeSprites.Ring, center, new Color(.6f, .9f, 1f, .9f), .5f, 15);
            ring.gameObject.AddComponent<ExpandingRing>().Play(Radius * 2f, .3f);
            var glow = RuntimeSprites.Spawn("Frost Glow", RuntimeSprites.Glow, center, new Color(.55f, .85f, 1f, .7f), Radius * 2.4f, -5);
            glow.gameObject.AddComponent<FadeOut>().Play(.5f, Vector2.zero);
            CameraFollow.Shake(.12f, .15f);
            Sfx.Play(SfxId.Slam, .6f);
        }
    }

    // Prsten koji se brzo raširi do zadate veličine i nestane.
    public sealed class ExpandingRing : MonoBehaviour
    {
        private float size, duration, elapsed;
        private SpriteRenderer sprite;
        private Color color;

        public void Play(float finalSize, float seconds)
        {
            size = finalSize;
            duration = seconds;
            sprite = GetComponent<SpriteRenderer>();
            color = sprite.color;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.localScale = Vector3.one * Mathf.Lerp(.3f, size, 1f - (1f - t) * (1f - t));
            sprite.color = new Color(color.r, color.g, color.b, color.a * (1f - t));
            if (t >= 1f)
                Destroy(gameObject);
        }
    }
}
