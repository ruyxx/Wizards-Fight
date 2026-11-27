// Prikazuje brojeve štete, trese kameru pri pogocima i javlja kada je napredak sačuvan.
using TMPro;
using UnityEngine;

namespace AWizardsFight
{
    [DisallowMultipleComponent]
    public sealed class CombatFeedback : MonoBehaviour
    {
        private static readonly Color EnemyDamageColor = new Color(1f, .95f, .75f);
        private static readonly Color PlayerDamageColor = new Color(1f, .3f, .3f);
        private static readonly Color HealColor = new Color(.45f, 1f, .5f);
        private static readonly Color NoticeColor = new Color(.75f, .6f, 1f);

        private PlayerHealth player;
        private SaveSystem saveSystem;
        private float previousHealth;

        private void Awake()
        {
            player = FindFirstObjectByType<PlayerHealth>();
            saveSystem = GetComponent<SaveSystem>();
        }

        private void OnEnable()
        {
            EnemyHealth.AnyDamaged += EnemyDamaged;
            EnemyHealth.AnyDied += EnemyDied;
            if (player != null)
                player.HealthChanged += PlayerHealthChanged;
            if (saveSystem != null)
                saveSystem.Saved += ShowSaved;
        }

        private void Start()
        {
            if (player != null)
                previousHealth = player.CurrentHealth;
        }

        private void OnDisable()
        {
            EnemyHealth.AnyDamaged -= EnemyDamaged;
            EnemyHealth.AnyDied -= EnemyDied;
            if (player != null)
                player.HealthChanged -= PlayerHealthChanged;
            if (saveSystem != null)
                saveSystem.Saved -= ShowSaved;
        }

        private void EnemyDamaged(EnemyHealth enemy, float amount)
        {
            if (amount <= 0f || enemy == null)
                return;
            Spawn(Format(amount), enemy.transform.position, EnemyDamageColor, 4.2f);
            Burst(enemy.transform.position, new Color(1f, .9f, .6f), 4, 2f);
            CameraFollow.Shake(enemy.IsDead ? .14f : .05f, enemy.IsDead ? .16f : .08f);
        }

        private void EnemyDied(EnemyHealth enemy)
        {
            if (enemy == null)
                return;
            Burst(enemy.transform.position, new Color(.75f, .55f, 1f), 14, 3.2f);
            if (enemy.GetComponent<IBossBehaviour>() == null)
                BodyAnimator.SpawnCorpse(enemy.transform);
            // Kratko "zamrzavanje" pri ubijanju jakog protivnika daje udarcu težinu.
            var level = enemy.GetComponent<EnemyLevel>();
            if ((level != null && level.IsElite) || enemy.GetComponent<IBossBehaviour>() != null)
                StartCoroutine(HitStop(.08f));
        }

        private static bool stopping;

        private System.Collections.IEnumerator HitStop(float seconds)
        {
            if (stopping || Time.timeScale < .99f)
                yield break;
            stopping = true;
            Time.timeScale = .1f;
            yield return new WaitForSecondsRealtime(seconds);
            // Vraća se samo ako u međuvremenu nije otvoren meni (koji je zaustavio igru).
            if (Mathf.Approximately(Time.timeScale, .1f))
                Time.timeScale = 1f;
            stopping = false;
        }

        private void PlayerHealthChanged(float current, float maximum)
        {
            float change = current - previousHealth;
            previousHealth = current;
            if (SaveSystem.IsRestoring || Mathf.Abs(change) < .01f || player == null)
                return;
            if (change < 0f)
            {
                Spawn("-" + Format(-change), player.transform.position, PlayerDamageColor, 4.8f);
                Burst(player.transform.position, PlayerDamageColor, 5, 2f);
                CameraFollow.Shake(.22f, .2f);
            }
            else
            {
                Spawn("+" + Format(change), player.transform.position, HealColor, 4.8f);
            }
        }

        private void ShowSaved()
        {
            if (player != null)
                Spawn("PROGRESS SAVED", player.transform.position + Vector3.up * .4f, NoticeColor, 3.6f, 1.6f);
        }

        // Iskre koje se razlete iz tačke pogotka; pri smrti ih je više i šire se dalje.
        public static void Burst(Vector3 position, Color color, int count, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var spark = RuntimeSprites.Spawn("Spark", RuntimeSprites.Square, position + (Vector3)(direction * .15f), color, Random.Range(.06f, .12f), 55);
                spark.gameObject.AddComponent<FadeOut>().Play(Random.Range(.25f, .45f), direction * speed * Random.Range(.5f, 1f));
            }
        }

        private static string Format(float amount) => Mathf.CeilToInt(amount).ToString();

        // Plutajući tekst iznad mete koji se diže i nestaje.
        public static void Popup(string message, Vector3 position, Color color, float size = 4f, float duration = .9f) => Spawn(message, position, color, size, duration);

        private static void Spawn(string message, Vector3 position, Color color, float size, float duration = .75f)
        {
            var popup = new GameObject("Damage Number", typeof(TextMeshPro), typeof(FadeOut));
            popup.transform.position = position + new Vector3(Random.Range(-.25f, .25f), .7f, 0f);
            var text = popup.GetComponent<TextMeshPro>();
            text.text = message;
            text.fontSize = size;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.outlineWidth = .25f;
            text.outlineColor = new Color32(20, 16, 32, 255);
            text.sortingOrder = 60;
            text.rectTransform.sizeDelta = new Vector2(6f, 1.5f);
            popup.GetComponent<FadeOut>().Play(duration, new Vector2(0f, 1.6f));
        }
    }
}
