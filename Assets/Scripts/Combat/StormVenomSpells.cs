// Posebne čarolije novih štapova: oluja munja po obližnjim protivnicima i otrovni oblak.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AWizardsFight
{
    public static class Thunderstorm
    {
        private const float Radius = 7f;
        private static readonly LayerMask EnemyLayers = 1 << 11;
        private static readonly LayerMask WallLayers = 1 << 9;

        // Šest udara munje u nasumične protivnike u blizini, jedan za drugim.
        public static void Cast(MonoBehaviour caster, Vector2 center, float damage)
        {
            caster.StartCoroutine(Strikes(center, damage));
            CameraFollow.Shake(.15f, .2f);
        }

        private static IEnumerator Strikes(Vector2 center, float damage)
        {
            for (int i = 0; i < 6; i++)
            {
                var targets = new List<EnemyHealth>();
                foreach (var collider in Physics2D.OverlapCircleAll(center, Radius, EnemyLayers))
                {
                    var enemy = collider.GetComponentInParent<EnemyHealth>();
                    if (enemy != null && !enemy.IsDead && !targets.Contains(enemy) && !Physics2D.Linecast(center, enemy.transform.position, WallLayers))
                        targets.Add(enemy);
                }

                if (targets.Count > 0)
                {
                    var target = targets[Random.Range(0, targets.Count)];
                    Vector2 point = target.transform.position;
                    ChainLightning.Bolt(point + new Vector2(Random.Range(-.6f, .6f), 6f), point);
                    var flash = RuntimeSprites.Spawn("Strike", RuntimeSprites.Glow, point, new Color(.8f, .9f, 1f, .9f), 2.4f, 18);
                    flash.gameObject.AddComponent<FadeOut>().Play(.25f, Vector2.zero);
                    target.TakeDamage(damage);
                    Sfx.Play(SfxId.Slam, .5f);
                }

                yield return new WaitForSeconds(.25f);
            }
        }
    }

    // Otrovni oblak: protivnici u njemu dobijaju sloj otrova svake pola sekunde.
    public sealed class PlagueCloud : MonoBehaviour
    {
        private const float Radius = 2.2f, Duration = 4f, TickInterval = .5f;
        private static readonly LayerMask EnemyLayers = 1 << 11;
        private float damagePerSecond, elapsed, nextTick;
        private SpriteRenderer fill;

        public static void Create(Vector2 position, float damagePerSecond)
        {
            var fill = RuntimeSprites.Spawn("Plague Cloud", RuntimeSprites.Glow, position, new Color(.45f, 1f, .35f, .6f), Radius * 2.6f, -5);
            var cloud = fill.gameObject.AddComponent<PlagueCloud>();
            cloud.damagePerSecond = damagePerSecond;
            cloud.fill = fill;
            Sfx.Play(SfxId.Burn, .7f);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float fade = elapsed > Duration - .5f ? Mathf.Clamp01((Duration - elapsed) / .5f) : 1f;
            fill.color = new Color(.45f, 1f, .35f, (.45f + .1f * Mathf.Sin(Time.time * 4f)) * fade);
            transform.localScale = Vector3.one * Radius * 2.6f * (1f + .05f * Mathf.Sin(Time.time * 2f));
            if (elapsed >= nextTick)
            {
                nextTick += TickInterval;
                foreach (var collider in Physics2D.OverlapCircleAll(transform.position, Radius, EnemyLayers))
                {
                    var enemy = collider.GetComponentInParent<EnemyHealth>();
                    if (enemy != null && !enemy.IsDead)
                        EnemyPoison.Apply(enemy, damagePerSecond, 3f);
                }
            }

            if (elapsed >= Duration)
                Destroy(gameObject);
        }
    }
}
