// Tama Senovitog hrama: igrač vidi samo krug oko sebe, a baklje i ostalo iza ivice nestaju u mraku.
// Krug je velika crna slika sa providnom sredinom koja prati lika; boss u pomračenju sužava krug.
using UnityEngine;

namespace AWizardsFight
{
    public sealed class DarknessOverlay : MonoBehaviour
    {
        public const float DefaultRadius = 5.5f;
        private static DarknessOverlay active;
        private Transform player;
        private SpriteRenderer shade;
        private float radius = DefaultRadius, targetRadius = DefaultRadius;
        private static Sprite sprite;

        public static void SetRadius(float value)
        {
            if (active != null)
                active.targetRadius = value;
        }

        private void Awake() => active = this;

        private void Start()
        {
            player = FindFirstObjectByType<PlayerHealth>()?.transform;
            if (sprite == null)
            {
                const int size = 256;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    // Udaljenost u jedinicama "poluprečnika svetla": 0 u sredini, 1 na ivici svetla.
                    float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(size / 2f, size / 2f)) / (size / 2f) * 8f;
                    float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.65f, 1.15f, d));
                    pixels[y * size + x] = new Color(0f, 0f, 0f, alpha * .96f);
                }
                texture.SetPixels32(pixels);
                texture.Apply();
                sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size / 16f);
            }
            shade = new GameObject("Darkness", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            shade.sprite = sprite;
            shade.sortingOrder = 50;
        }

        private void LateUpdate()
        {
            if (player == null || shade == null)
                return;
            // Slika ima prečnik 16 jedinica pri skali 1, a ivica svetla je na 1/8 te veličine.
            radius = Mathf.Lerp(radius, targetRadius, 1f - Mathf.Exp(-2f * Time.deltaTime));
            float flicker = 1f + Mathf.Sin(Time.time * 7f) * .015f + Mathf.Sin(Time.time * 13f) * .01f;
            shade.transform.position = player.position;
            shade.transform.localScale = Vector3.one * radius * flicker;
        }

        private void OnDestroy()
        {
            if (active == this)
                active = null;
            if (shade != null)
                Destroy(shade.gameObject);
        }
    }
}
