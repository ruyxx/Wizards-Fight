// Napukao zid: posle nekoliko pogodaka se sruši i otvara tajni prolaz ka skrivenom blagu.
using UnityEngine;
using UnityEngine.Tilemaps;

namespace AWizardsFight
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CrackedWall : MonoBehaviour, IDamageable
    {
        [SerializeField]
        private string secretId = "secret";
        [SerializeField, Min(1)]
        private int hitsToBreak = 3;
        private int hits;
        private Tilemap walls;

        private string Flag => "secret_" + secretId;

        private void Start()
        {
            foreach (var map in FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
                if (map.name == "Walls")
                    walls = map;
            // Ako je prolaz već otvoren pre poslednjeg čuvanja, zid se odmah uklanja.
            if (SaveSystem.HasFlag(Flag))
                Open(false);
        }

        public void TakeDamage(float amount)
        {
            if (++hits < hitsToBreak)
            {
                CameraFollow.Shake(.06f, .08f);
                Sfx.Play(SfxId.Break, .5f);
                return;
            }

            Open(true);
        }

        private void Open(bool announce)
        {
            if (walls != null)
                walls.SetTile(walls.WorldToCell(transform.position), null);
            SaveSystem.SetFlag(Flag);
            if (announce)
            {
                for (int i = 0; i < 10; i++)
                {
                    var shard = RuntimeSprites.Spawn("Rubble", RuntimeSprites.Square, transform.position, new Color(.45f, .45f, .55f), Random.Range(.1f, .2f), 5);
                    shard.gameObject.AddComponent<FadeOut>().Play(.6f, Random.insideUnitCircle.normalized * Random.Range(2f, 5f));
                }

                CombatFeedback.Popup("SECRET PASSAGE!", transform.position, new Color(.6f, 1f, .7f), 4.5f, 1.6f);
                Sfx.Play(SfxId.Explosion, .6f);
                CameraFollow.Shake(.25f, .25f);
            }

            Destroy(gameObject);
        }
    }
}
