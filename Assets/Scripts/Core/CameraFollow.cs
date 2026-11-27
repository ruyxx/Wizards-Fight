// Prati položaj igrača i glatko pomera kameru posle pomeranja ostalih objekata.
using UnityEngine;

namespace AWizardsFight
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField]
        private Transform target = null;
        [SerializeField, Min(0.01f)]
        private float smoothTime = 0.15f;
        [SerializeField]
        private Vector3 offset = new Vector3(0f, 0f, -10f);
        private Vector3 smoothingVelocity, basePosition;
        private float shakeStrength, shakeUntil, shakeDuration;
        private static CameraFollow active;
        private Transform aim;
        private Vector3 lookAhead;
        private const float LookAheadDistance = 1.1f;

        // Kratko trese kameru; jači potres zamenjuje slabiji.
        public static void Shake(float strength, float duration)
        {
            if (active == null || duration <= 0f)
                return;
            float remaining = Mathf.Max(0f, active.shakeUntil - Time.time);
            float current = active.shakeDuration > 0f ? active.shakeStrength * remaining / active.shakeDuration : 0f;
            if (strength < current)
                return;
            active.shakeStrength = strength;
            active.shakeDuration = duration;
            active.shakeUntil = Time.time + duration;
        }

        // Odmah postavlja kameru na lika, npr. posle nastavka sa tačke čuvanja.
        public void SnapToTarget()
        {
            if (target == null)
                return;
            basePosition = target.position + offset;
            transform.position = basePosition;
            smoothingVelocity = Vector3.zero;
        }

        private void OnEnable() => active = this;

        private void Start()
        {
            if (target == null)
            {
                Debug.LogError("CameraFollow requires a Player target in the Inspector.", this);
                enabled = false;
                return;
            }

            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (target == null)
                return;
            // Kamera je malo pomerena u pravcu nišana, da se vidi više prostora ispred lika.
            if (aim == null)
                aim = target.Find("Aim Pivot") ?? target.Find("AimPivot");
            var wanted = aim != null && Time.timeScale > 0f ? aim.right * LookAheadDistance : Vector3.zero;
            lookAhead = Vector3.Lerp(lookAhead, new Vector3(wanted.x, wanted.y, 0f), 1f - Mathf.Exp(-4f * Time.deltaTime));
            basePosition = Vector3.SmoothDamp(basePosition, target.position + offset + lookAhead, ref smoothingVelocity, smoothTime);
            Vector3 shake = Vector3.zero;
            if (Time.time < shakeUntil && Time.timeScale > 0f)
            {
                float strength = shakeStrength * (shakeUntil - Time.time) / shakeDuration;
                shake = (Vector3)(Random.insideUnitCircle * strength);
            }
            transform.position = basePosition + shake;
        }

        private void OnDisable()
        {
            if (active == this)
                active = null;
            smoothingVelocity = Vector3.zero;
        }
    }
}
