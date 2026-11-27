// Primenjuje bočnu vatrenu štetu okolnim protivnicima i pokreće vizuelni efekat.
using System.Collections.Generic;
using UnityEngine;

namespace AWizardsFight
{
    
    public sealed class FireExplosion : MonoBehaviour
    {
        [SerializeField, Min(.1f)]
        private float radius = 1.8f;
        [SerializeField, Range(0, 1)]
        private float splashFraction = .6f;
        [SerializeField]
        private LayerMask enemyLayers = 1 << 11;
        [SerializeField]
        private LayerMask wallLayers = 1 << 9;
        [SerializeField]
        private GameObject visualPrefab = null;
        public float Radius => radius;
        public GameObject VisualPrefab => visualPrefab;

        public void Explode(Vector2 position, float directDamage, IDamageable directTarget)
        {
            // Jedan protivnik može imati više sudarača, ali štetu dobija samo jednom.
            var damaged = new HashSet<EnemyHealth>();
            foreach (var collider in Physics2D.OverlapCircleAll(position, radius, enemyLayers))
            {
                var enemy = collider.GetComponentInParent<EnemyHealth>();
                // Glavna meta već prima direktnu štetu iz projektila.
                if (enemy == null || enemy.IsDead || ReferenceEquals(enemy, directTarget) || !damaged.Add(enemy))
                    continue;
               
                if (Physics2D.Linecast(position, collider.ClosestPoint(position), wallLayers))
                    continue;
                enemy.TakeDamage(directDamage * splashFraction);
            }

            if (visualPrefab == null)
                return;
            var visual = Instantiate(visualPrefab, position, Quaternion.identity);
            visual.transform.localScale = Vector3.one * radius;
            
            Destroy(visual, FireBlastVisual.Lifetime + .05f);
        }
    }
}
