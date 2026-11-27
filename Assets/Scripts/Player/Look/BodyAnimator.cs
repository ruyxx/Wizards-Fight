// Animacija lika bez dodatnih sličica: pri hodu se lik ljulja i poskakuje, u mestu blago "diše",
// pri napadu se kratko napne, a pri smrti pada i nestaje. Radi nad detetom "Visual".
using UnityEngine;

namespace AWizardsFight
{
    [DisallowMultipleComponent]
    public sealed class BodyAnimator : MonoBehaviour
    {
        private Transform visual;
        private Rigidbody2D body;
        private Vector3 basePosition, baseScale;
        private float phase, kickUntil, speedSmoothed;
        private const float KickTime = .14f;

        private void Awake()
        {
            visual = transform.Find("Visual");
            body = GetComponent<Rigidbody2D>();
            if (visual != null)
            {
                basePosition = visual.localPosition;
                baseScale = visual.localScale;
            }
            phase = Random.value * 10f;
        }

        // Kratak "udarac" pri napadu ili bacanju čarolije.
        public void Kick() => kickUntil = Time.time + KickTime;

        private void LateUpdate()
        {
            if (visual == null || Time.timeScale <= 0f)
                return;
            float speed = body != null && body.simulated ? body.linearVelocity.magnitude : 0f;
            speedSmoothed = Mathf.Lerp(speedSmoothed, Mathf.Clamp01(speed / 3f), 1f - Mathf.Exp(-12f * Time.deltaTime));
            phase += Time.deltaTime * (2f + 10f * speedSmoothed);
            // Hod: poskakivanje i ljuljanje; u mestu: disanje.
            float hop = Mathf.Abs(Mathf.Sin(phase)) * .07f * speedSmoothed;
            float tilt = Mathf.Sin(phase) * 5f * speedSmoothed;
            float breathe = 1f + Mathf.Sin(phase * .5f) * .025f * (1f - speedSmoothed);
            float kick = Time.time < kickUntil ? Mathf.Sin((kickUntil - Time.time) / KickTime * Mathf.PI) : 0f;
            visual.localPosition = basePosition + Vector3.up * hop;
            visual.localRotation = Quaternion.Euler(0f, 0f, tilt);
            visual.localScale = new Vector3(baseScale.x * (1f + .12f * kick), baseScale.y * breathe * (1f - .08f * kick), baseScale.z);
        }

        // Pri smrti ostaje kopija lika koja pada na stranu i nestaje.
        public static void SpawnCorpse(Transform owner)
        {
            var visual = owner.Find("Visual");
            var renderer = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
            if (renderer == null || renderer.sprite == null)
                return;
            var corpse = new GameObject("Corpse", typeof(SpriteRenderer)).AddComponent<CorpseFall>();
            corpse.transform.SetPositionAndRotation(visual.position, visual.rotation);
            corpse.transform.localScale = visual.lossyScale;
            var copy = corpse.GetComponent<SpriteRenderer>();
            copy.sprite = renderer.sprite;
            copy.flipX = renderer.flipX;
            copy.sortingLayerID = renderer.sortingLayerID;
            copy.sortingOrder = renderer.sortingOrder;
            corpse.direction = Random.value < .5f ? -1f : 1f;
        }
    }

    public sealed class CorpseFall : MonoBehaviour
    {
        public float direction = 1f;
        private float elapsed;
        private SpriteRenderer sprite;
        private Vector3 start;

        private void Start()
        {
            sprite = GetComponent<SpriteRenderer>();
            start = transform.position;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float fall = Mathf.Clamp01(elapsed / .3f);
            transform.rotation = Quaternion.Euler(0f, 0f, -90f * direction * fall * fall);
            transform.position = start + Vector3.down * .2f * fall;
            var color = sprite.color;
            color = Color.Lerp(Color.white, new Color(.45f, .4f, .55f), fall);
            color.a = 1f - Mathf.Clamp01((elapsed - .8f) / .6f);
            sprite.color = color;
            if (elapsed > 1.4f)
                Destroy(gameObject);
        }
    }
}
