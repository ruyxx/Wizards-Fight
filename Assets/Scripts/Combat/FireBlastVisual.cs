// Prikazuje širenje, bleđenje i iskre vatrene eksplozije; ne obračunava štetu.
using UnityEngine;

namespace AWizardsFight
{
   
    public sealed class FireBlastVisual : MonoBehaviour
    {
        [SerializeField]
        private LineRenderer ring = null;
        [SerializeField]
        private SpriteRenderer core = null;
        [SerializeField]
        private SpriteRenderer[] sparks = new SpriteRenderer[0];
        public const float Lifetime = .42f;
        private float age;

        private void Awake() => Show(0);

        private void Update()
        {
            age += Time.deltaTime;
            Show(age / Lifetime);
            if (age >= Lifetime)
                Destroy(gameObject);
        }

        public void Show(float progress)
        {
            float t = Mathf.Clamp01(progress), fade = 1 - t;
            if (ring != null)
            {
                ring.transform.localScale = Vector3.one * Mathf.Lerp(.15f, 1f, Mathf.Min(1, t * 2));
                ring.startColor = ring.endColor = new Color(1, .3f + .5f * fade, .04f, fade * .85f);
                ring.widthMultiplier = Mathf.Lerp(.16f, .025f, t);
            }

            if (core != null)
            {
                core.transform.localScale = Vector3.one * Mathf.Lerp(.2f, 1.9f, Mathf.Sqrt(t));
                core.color = new Color(1, 1, 1, fade * fade * .85f);
                core.transform.localRotation = Quaternion.Euler(0, 0, t * 45);
            }

            for (int i = 0; i < sparks.Length; i++)
            {
                float angle = (i * 137.5f) * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                sparks[i].transform.localPosition = direction * Mathf.Lerp(.12f, .7f + (i % 3) * .12f, t);
                sparks[i].transform.localScale = new Vector3(.06f, .13f, 1) * Mathf.Lerp(1, .3f, t);
                sparks[i].color = new Color(1, .3f + .6f * fade, .06f, fade);
            }
        }
    }
}
