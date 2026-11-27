// Posebna osobina protivnika (vidi EnemyVariants).
using UnityEngine;

namespace AWizardsFight
{
    // Ledena aura: igrač u blizini je usporen.
    public sealed class FrostAura : MonoBehaviour
    {
        private EnemyHealth self;
        private SpriteRenderer glow;
        private float nextTick;

        private void Start()
        {
            self = GetComponent<EnemyHealth>();
            glow = RuntimeSprites.Spawn("Frost Aura", RuntimeSprites.Glow, transform.position, new Color(.5f, .85f, 1f, .3f), 5.5f, 2);
            glow.transform.SetParent(transform, true);
        }

        private void Update()
        {
            if (self == null || self.IsDead)
            {
                if (glow != null)
                    glow.enabled = false;
                return;
            }
            if (Time.time < nextTick)
                return;
            nextTick = Time.time + .25f;
            var player = EnemyVariants.Player;
            if (player != null && !player.IsDead && Vector2.Distance(transform.position, player.transform.position) < 2.8f && player.TryGetComponent<PlayerController>(out var controller))
                controller.ApplySlow(.65f, .35f);
        }
    }
}
