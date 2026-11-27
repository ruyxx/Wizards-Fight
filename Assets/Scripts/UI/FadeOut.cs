// Postepeno gasi sprite ili tekst, po želji ga pomera, pa uklanja objekat.
using TMPro;
using UnityEngine;

namespace AWizardsFight
{
    public sealed class FadeOut : MonoBehaviour
    {
        private SpriteRenderer sprite;
        private TMP_Text text;
        private Vector2 velocity;
        private float duration = .3f, elapsed, startAlpha = 1f;

        public void Play(float seconds, Vector2 drift)
        {
            sprite = GetComponent<SpriteRenderer>();
            text = GetComponent<TMP_Text>();
            duration = Mathf.Max(.01f, seconds);
            velocity = drift;
            startAlpha = sprite != null ? sprite.color.a : text != null ? text.alpha : 1f;
            elapsed = 0f;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position += (Vector3)(velocity * Time.deltaTime);
            // Usporava pomeranje pred kraj, da broj "zastane" pre nestajanja.
            velocity *= 1f - Mathf.Min(1f, 3f * Time.deltaTime);
            float alpha = startAlpha * (1f - t * t);
            if (sprite != null)
            {
                var color = sprite.color;
                color.a = alpha;
                sprite.color = color;
            }

            if (text != null)
                text.alpha = alpha;
            if (t >= 1f)
                Destroy(gameObject);
        }
    }
}
