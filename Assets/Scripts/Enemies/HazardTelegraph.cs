// Crveni krug na podu upozorava gde će udariti napad; posle kratkog vremena udar povređuje igrača u krugu.
using UnityEngine;

namespace AWizardsFight
{
    public sealed class HazardTelegraph : MonoBehaviour
    {
        private float radius, delay, damage, elapsed;
        private bool leavesFire;
        private float slow;
        private PlayerHealth target;
        private SpriteRenderer fill, edge;

        public static void Spawn(Vector2 position, float radius, float delay, float damage, PlayerHealth target, bool leavesFire = false, float slow = 0f)
        {
            var root = new GameObject("Hazard Warning");
            root.transform.position = position;
            var hazard = root.AddComponent<HazardTelegraph>();
            hazard.radius = radius;
            hazard.delay = delay;
            hazard.damage = damage;
            hazard.target = target;
            hazard.leavesFire = leavesFire;
            hazard.slow = slow;
            hazard.fill = RuntimeSprites.Spawn("Fill", RuntimeSprites.Disc, position, new Color(1f, .15f, .1f, .15f), .1f, -6);
            hazard.fill.transform.SetParent(root.transform, true);
            hazard.edge = RuntimeSprites.Spawn("Edge", RuntimeSprites.Ring, position, new Color(1f, .25f, .2f, .8f), radius * 2f, -6);
            hazard.edge.transform.SetParent(root.transform, true);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / delay);
            // Unutrašnji krug raste do ivice; tada nastupa udar.
            fill.transform.localScale = Vector3.one * Mathf.Lerp(.1f, radius * 2f, t);
            fill.color = new Color(1f, .15f, .1f, Mathf.Lerp(.15f, .45f, t));
            if (t < 1f)
                return;
            Vector2 center = transform.position;
            if (target != null && !target.IsDead && Vector2.Distance(center, target.transform.position) <= radius)
            {
                bool dodged = target.IsInvulnerable;
                target.TakeDamage(damage);
                if (slow > 0f && !dodged)
                    target.GetComponent<PlayerController>()?.ApplySlow(slow, 2f);
            }
            var flash = RuntimeSprites.Spawn("Impact", RuntimeSprites.Glow, center, new Color(1f, .45f, .25f, .9f), radius * 3f, 18);
            flash.gameObject.AddComponent<FadeOut>().Play(.3f, Vector2.zero);
            CameraFollow.Shake(.18f, .15f);
            Sfx.Play(SfxId.Slam, .7f);
            // Erupcija ostavlja užareno tlo koje još kratko peče.
            if (leavesFire)
                BurningGround.Create(center, radius, damage * .6f, 3f, target);
            Destroy(gameObject);
        }
    }
}
