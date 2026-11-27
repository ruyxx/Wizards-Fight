// Posebna osobina protivnika (vidi EnemyVariants).
using UnityEngine;

namespace AWizardsFight
{
    // Senka: kad je igrač blizu, povremeno nestane i pojavi se iza njega.
    public sealed class ShadeBlink : MonoBehaviour
    {
        private EnemyHealth self;
        private Rigidbody2D body;
        private float nextBlink;

        private void Start()
        {
            self = GetComponent<EnemyHealth>();
            body = GetComponent<Rigidbody2D>();
            nextBlink = Time.time + Random.Range(3f, 5f);
        }

        private void Update()
        {
            var player = EnemyVariants.Player;
            if (self == null || self.IsDead || player == null || player.IsDead || Time.time < nextBlink)
                return;
            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (distance > 9f || Physics2D.Linecast(transform.position, player.transform.position, 1 << 9))
                return;
            nextBlink = Time.time + Random.Range(4.5f, 6.5f);
            // Mesto iza igrača (suprotno od pravca u kom gleda), ako nije u zidu.
            var away = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
            for (int attempt = 0; attempt < 6; attempt++)
            {
                var spot = (Vector2)player.transform.position + (Vector2)(Quaternion.Euler(0, 0, Random.Range(-50f, 50f)) * away) * 1.6f;
                if (Physics2D.OverlapCircle(spot, .45f, 1 << 9) != null)
                    continue;
                Puff(transform.position);
                if (body != null)
                    body.position = spot;
                transform.position = spot;
                Puff(spot);
                Sfx.Play(SfxId.Dash, .4f);
                return;
            }
        }

        private static void Puff(Vector2 at)
        {
            var puff = RuntimeSprites.Spawn("Shade Puff", RuntimeSprites.Glow, at, new Color(.5f, .3f, .9f, .85f), 2f, 18);
            puff.gameObject.AddComponent<FadeOut>().Play(.4f, Vector2.zero);
        }
    }
}
