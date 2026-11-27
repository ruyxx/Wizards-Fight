// Munja posle pogotka preskače na obližnje protivnike; svaki skok nanosi nešto manje štete.
using System.Collections.Generic;
using UnityEngine;

namespace AWizardsFight
{
    public sealed class ChainLightning : MonoBehaviour
    {
        [SerializeField, Min(0)]
        private int jumps = 3;
        [SerializeField, Min(.5f)]
        private float range = 4f;
        [SerializeField, Range(.1f, 1f)]
        private float falloff = .75f;
        private static readonly LayerMask EnemyLayers = 1 << 11;
        private static readonly LayerMask WallLayers = 1 << 9;

        public void Chain(Vector2 from, float damage, IDamageable first)
        {
            var hit = new HashSet<EnemyHealth>();
            if (first is EnemyHealth firstEnemy)
                hit.Add(firstEnemy);
            Vector2 position = from;
            for (int jump = 0; jump < jumps + SkillEffects.ExtraChainJumps; jump++)
            {
                damage *= falloff;
                EnemyHealth next = null;
                float best = range;
                foreach (var collider in Physics2D.OverlapCircleAll(position, range, EnemyLayers))
                {
                    var enemy = collider.GetComponentInParent<EnemyHealth>();
                    if (enemy == null || enemy.IsDead || hit.Contains(enemy))
                        continue;
                    float distance = Vector2.Distance(position, enemy.transform.position);
                    if (distance < best && !Physics2D.Linecast(position, enemy.transform.position, WallLayers))
                    {
                        best = distance;
                        next = enemy;
                    }
                }

                if (next == null)
                    break;
                hit.Add(next);
                Vector2 target = next.transform.position;
                Bolt(position, target);
                next.TakeDamage(damage);
                position = target;
            }
        }

        // Izlomljena linija munje koja brzo nestane.
        public static void Bolt(Vector2 from, Vector2 to)
        {
            var line = new GameObject("Lightning", typeof(LineRenderer)).GetComponent<LineRenderer>();
            const int points = 8;
            line.positionCount = points;
            Vector2 normal = Vector2.Perpendicular(to - from).normalized;
            for (int i = 0; i < points; i++)
            {
                float t = i / (float)(points - 1);
                float jitter = i == 0 || i == points - 1 ? 0f : Random.Range(-.25f, .25f);
                line.SetPosition(i, Vector2.Lerp(from, to, t) + normal * jitter);
            }

            line.widthMultiplier = .12f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = line.endColor = new Color(.85f, .9f, 1f, 1f);
            line.sortingOrder = 20;
            line.gameObject.AddComponent<LineFade>();
        }
    }

    // Linija munje bledi i nestaje.
    public sealed class LineFade : MonoBehaviour
    {
        private LineRenderer line;
        private float elapsed;

        private void Awake() => line = GetComponent<LineRenderer>();

        private void Update()
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - elapsed / .2f;
            var color = new Color(.85f, .9f, 1f, Mathf.Clamp01(alpha));
            line.startColor = line.endColor = color;
            if (alpha <= 0f)
            {
                Destroy(line.material);
                Destroy(gameObject);
            }
        }
    }
}
