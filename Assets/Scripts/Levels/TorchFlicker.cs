// Blago menja jačinu i veličinu sjaja baklje, da svetlo deluje živo.
using UnityEngine;

namespace AWizardsFight
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class TorchFlicker : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)]
        private float baseAlpha = .55f;
        [SerializeField, Min(0f)]
        private float speed = 6f;
        private SpriteRenderer glow;
        private Vector3 baseScale;
        private float seed;

        private void Awake()
        {
            glow = GetComponent<SpriteRenderer>();
            baseScale = transform.localScale;
            seed = Random.value * 100f;
        }

        private void Update()
        {
            float noise = Mathf.PerlinNoise(seed, Time.time * speed);
            var color = glow.color;
            color.a = baseAlpha * (.75f + .35f * noise);
            glow.color = color;
            transform.localScale = baseScale * (.95f + .1f * noise);
        }
    }
}
