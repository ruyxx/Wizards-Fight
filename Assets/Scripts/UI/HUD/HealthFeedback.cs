// Prikazuje kratak bljesak pri pogotku i oznaku zdravlja protivnika.
using UnityEngine;

namespace AWizardsFight
{
    public sealed class HealthFeedback : MonoBehaviour
    {
        private PlayerHealth player;
        private EnemyHealth enemy;
        private SpriteRenderer visual;
        private TMPro.TMP_Text label;
        private float previous, flashUntil;
        private string originalLabel;

        private void Awake()
        {
            player = GetComponent<PlayerHealth>();
            enemy = GetComponent<EnemyHealth>();
            visual = transform.Find("Visual").GetComponent<SpriteRenderer>();
            label = GetComponentInChildren<TMPro.TMP_Text>();
            if (label != null)
                originalLabel = label.text;
            // Kopija protivnika već nosi oznaku nivoa u natpisu; ona se dopisuje ponovo na startu.
            if (originalLabel != null && originalLabel.Contains("  LV. "))
                originalLabel = originalLabel.Substring(0, originalLabel.IndexOf("  LV. ", System.StringComparison.Ordinal));
        }

        // Dodaje oznaku ispred imena, npr. ELITE.
        public void AddTitlePrefix(string prefix)
        {
            if (label == null || originalLabel == null || originalLabel.StartsWith(prefix))
                return;
            originalLabel = prefix + originalLabel;
            if (enemy != null)
                label.text = originalLabel;
        }

        private void OnEnable()
        {
            if (player != null)
                player.HealthChanged += Changed;
            if (enemy != null)
                enemy.HealthChanged += Changed;
        }

        private void Start()
        {
            // Nivo se dopisuje tek na startu, jer ga prizvani protivnici dobijaju posle stvaranja.
            var level = GetComponent<EnemyLevel>();
            if (label != null && level != null)
                originalLabel += "  LV. " + level.Level;
            previous = player != null ? player.CurrentHealth : enemy.CurrentHealth;
            if (enemy != null && label != null)
            {
                label.text = originalLabel;
                BuildBar();
            }
        }

        // ---------- Traka zdravlja iznad protivnika ----------

        private Transform bar, fill, chip;
        private float chipRatio = 1f, barVisibleUntil;
        private const float BarWidth = 1.1f, BarHeight = .13f;

        // Tanka traka ispod imena: crvena je trenutno zdravlje, a bela pokazuje upravo izgubljeni deo i polako se smanjuje.
        private void BuildBar()
        {
            bar = new GameObject("Health Bar").transform;
            bar.SetParent(transform, false);
            bar.position = label.transform.position + Vector3.down * .22f;
            Part("Back", new Color(.05f, .03f, .08f, .85f), BarWidth + .06f, BarHeight + .06f, 70);
            chip = Part("Chip", new Color(1f, .95f, .85f, .9f), BarWidth, BarHeight, 71);
            fill = Part("Fill", new Color(.9f, .2f, .25f, 1f), BarWidth, BarHeight, 72);
            bar.gameObject.SetActive(false);
        }

        private Transform Part(string name, Color color, float width, float height, int order)
        {
            var part = RuntimeSprites.Spawn(name, RuntimeSprites.Square, bar.position, color, 1f, order);
            part.transform.SetParent(bar, true);
            part.transform.localScale = new Vector3(width, height, 1f);
            return part.transform;
        }

        private void SetWidth(Transform part, float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            part.localScale = new Vector3(BarWidth * ratio, BarHeight, 1f);
            part.localPosition = new Vector3(-BarWidth * (1f - ratio) / 2f, 0f, 0f);
        }

        private void UpdateBar()
        {
            if (bar == null)
                return;
            float ratio = enemy.MaxHealth > 0f ? enemy.CurrentHealth / enemy.MaxHealth : 0f;
            // Elitni protivnici uvek pokazuju traku; obični tek kad budu pogođeni.
            bool elite = GetComponent<EnemyLevel>() is EnemyLevel level && level.IsElite;
            bool show = !enemy.IsDead && (elite || ratio < .999f || Time.time < barVisibleUntil);
            if (bar.gameObject.activeSelf != show)
                bar.gameObject.SetActive(show);
            if (!show)
                return;
            chipRatio = Mathf.MoveTowards(chipRatio, ratio, Time.deltaTime * (chipRatio > ratio + .3f ? 1.2f : .6f));
            if (chipRatio < ratio)
                chipRatio = ratio;
            SetWidth(fill, ratio);
            SetWidth(chip, chipRatio);
        }

        private void OnDisable()
        {
            if (player != null)
                player.HealthChanged -= Changed;
            if (enemy != null)
                enemy.HealthChanged -= Changed;
        }

        private void Changed(float current, float max)
        {
            if (current < previous)
            {
                flashUntil = Time.time + .15f;
                barVisibleUntil = Time.time + 4f;
            }

            previous = current;
        }

        private void LateUpdate()
        {
            if (enemy != null)
                UpdateBar();
            var color = Time.time < flashUntil ? new Color(1, .25f, .3f) : Color.white;
            color.a = visual.color.a;
            visual.color = color;
        }
    }
}
