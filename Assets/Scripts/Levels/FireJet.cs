// Otvor u podu koji povremeno izbaci plamen: prvo zasvetli kao upozorenje, pa peče igrača koji stoji iznad njega.
using UnityEngine;

namespace AWizardsFight
{
    public sealed class FireJet : MonoBehaviour
    {
        [SerializeField, Min(.5f)]
        private float period = 3.2f;
        [SerializeField, Min(.1f)]
        private float warning = .8f, burning = .9f;
        [SerializeField, Min(0f)]
        private float damage = 18f;
        [SerializeField, Min(.1f)]
        private float radius = .65f;
        [SerializeField]
        private SpriteRenderer flame = null;
        [SerializeField, Tooltip("Ledeni otvor: manja šteta, ali usporava igrača.")]
        private bool frost = false;
        private SpriteRenderer glow;
        private PlayerHealth player;
        private float offset;
        private bool hitThisBurst;

        private void Start()
        {
            player = FindFirstObjectByType<PlayerHealth>();
            // Mlazevi nisu svi u istom ritmu.
            offset = (transform.position.x * .37f + transform.position.y * .61f) % period;
            glow = RuntimeSprites.Spawn("Jet Glow", RuntimeSprites.Glow, transform.position, frost ? new Color(.5f, .8f, 1f, 0f) : new Color(1f, .5f, .15f, 0f), 2.2f, -9);
            glow.transform.SetParent(transform, true);
        }

        private void Update()
        {
            float t = (Time.time + offset) % period;
            bool warn = t < warning;
            bool burn = t >= warning && t < warning + burning;
            if (flame != null)
            {
                flame.enabled = burn;
                if (burn)
                    flame.transform.localScale = new Vector3(1f, .9f + .2f * Mathf.Sin(Time.time * 30f), 1f);
            }

            var tint = frost ? new Color(.5f, .8f, 1f) : new Color(1f, .5f, .15f);
            glow.color = new Color(tint.r, tint.g, tint.b, burn ? .75f : warn ? .25f + .25f * Mathf.Sin(Time.time * 20f) : 0f);
            if (!burn)
            {
                hitThisBurst = false;
                return;
            }

            // Jedan plamen pogađa igrača najviše jednom.
            if (hitThisBurst || player == null || player.IsDead || Vector2.Distance(player.transform.position, transform.position) > radius)
                return;
            hitThisBurst = true;
            player.TakeDamage(frost ? damage * .5f : damage);
            if (frost && !player.IsInvulnerable)
                player.GetComponent<PlayerController>()?.ApplySlow(.45f, 2f);
            Sfx.Play(SfxId.Burn);
        }
    }
}
