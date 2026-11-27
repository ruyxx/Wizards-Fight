// Lava peče igrača dok stoji na njoj; izmicanjem se može preskočiti kraći deo lave.
using UnityEngine;
using UnityEngine.Tilemaps;

namespace AWizardsFight
{
    [RequireComponent(typeof(Tilemap))]
    public sealed class LavaHazard : MonoBehaviour
    {
        [SerializeField, Min(0f)]
        private float damagePerSecond = 26f;
        [SerializeField, Min(.05f)]
        private float tickInterval = .25f;
        private Tilemap lava;
        private PlayerHealth player;
        private float nextTick;

        private void Awake() => lava = GetComponent<Tilemap>();

        private void Start() => player = FindFirstObjectByType<PlayerHealth>();

        private void Update()
        {
            // Blago pulsiranje boje daje utisak žive lave.
            float glow = .88f + .12f * Mathf.Sin(Time.time * 2.2f);
            lava.color = new Color(1f, glow, glow * .9f, 1f);
            if (player == null || player.IsDead || Time.time < nextTick)
                return;
            if (!lava.HasTile(lava.WorldToCell(player.transform.position)))
                return;
            nextTick = Time.time + tickInterval;
            if (player.IsInvulnerable)
                return;
            player.TakeDamage(damagePerSecond * tickInterval);
            Sfx.Play(SfxId.Burn, .6f);
        }
    }
}
