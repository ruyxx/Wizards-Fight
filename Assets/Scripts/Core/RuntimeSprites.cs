// Jednostavni oblici napravljeni u kodu: sjaj, krug, prsten, kvadrat i zatamnjenje ivica.
using UnityEngine;

namespace AWizardsFight
{
    public static class RuntimeSprites
    {
        private static Sprite glow, disc, ring, square, vignette, rounded, roundedFrame;

        // Zaobljeni pravougaonici za korisnički interfejs (9-slice): pun i samo ivica.
        public static Sprite Rounded => rounded != null ? rounded : rounded = MakeRounded(0f);
        public static Sprite RoundedFrame => roundedFrame != null ? roundedFrame : roundedFrame = MakeRounded(2.5f);

        private static Sprite MakeRounded(float thickness)
        {
            const int size = 32;
            const float radius = 9f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // Udaljenost od ivice zaobljenog pravougaonika (negativna unutra).
                float px = Mathf.Abs(x + .5f - size / 2f) - (size / 2f - radius);
                float py = Mathf.Abs(y + .5f - size / 2f) - (size / 2f - radius);
                float outside = new Vector2(Mathf.Max(px, 0f), Mathf.Max(py, 0f)).magnitude + Mathf.Min(Mathf.Max(px, py), 0f) - radius;
                float alpha = Mathf.Clamp01(.5f - outside);
                if (thickness > 0f)
                    alpha *= Mathf.Clamp01(outside + thickness + .5f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(12, 12, 12, 12));
        }

        public static Sprite Glow => glow != null ? glow : glow = Make(64, d => Mathf.Pow(Mathf.Clamp01(1f - d), 2f), FilterMode.Bilinear);
        public static Sprite Disc => disc != null ? disc : disc = Make(64, d => d <= 1f ? Mathf.Lerp(.35f, .85f, d * d) : 0f, FilterMode.Bilinear);
        public static Sprite Ring => ring != null ? ring : ring = Make(64, d => d <= 1f && d > .8f ? 1f : 0f, FilterMode.Bilinear);
        public static Sprite Square => square != null ? square : square = Make(4, d => 1f, FilterMode.Point);
        public static Sprite Vignette => vignette != null ? vignette : vignette = Make(128, d => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.6f, 1.3f, d)), FilterMode.Bilinear);

        // Bela tekstura čija providnost zavisi od udaljenosti od centra; boja se zadaje na rendereru.
        private static Sprite Make(int size, System.Func<float, float> alpha, FilterMode filter)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = filter, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            float half = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(half, half)) / half;
                pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha(d)));
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
        }

        // Pravi objekat sa spriteom u svetu igre; veličina 1 znači prečnik od jedne jedinice.
        public static SpriteRenderer Spawn(string name, Sprite sprite, Vector3 position, Color color, float scale, int order)
        {
            var item = new GameObject(name, typeof(SpriteRenderer));
            item.transform.position = position;
            item.transform.localScale = new Vector3(scale, scale, 1f);
            var renderer = item.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }
    }
}
