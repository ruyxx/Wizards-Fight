// Pri novom nivou zaustavlja igru i nudi tri pojačanja (klik ili tasteri 1, 2, 3).
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AWizardsFight
{
    [DisallowMultipleComponent]
    public sealed class PerkChoiceMenu : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }

        private PlayerProgression progression;
        private GameMenus menus;
        private GameObject panel;
        private readonly List<Perk> offered = new List<Perk>();
        private readonly List<TextMeshProUGUI> titles = new List<TextMeshProUGUI>();
        private readonly List<TextMeshProUGUI> descriptions = new List<TextMeshProUGUI>();
        private TextMeshProUGUI header;

        private void Awake()
        {
            var player = FindFirstObjectByType<PlayerHealth>();
            progression = player != null ? player.GetComponent<PlayerProgression>() : null;
            menus = FindFirstObjectByType<GameMenus>();
        }

        private void OnEnable()
        {
            if (progression != null)
                progression.LeveledUp += Open;
        }

        private void OnDisable()
        {
            if (progression != null)
                progression.LeveledUp -= Open;
            IsOpen = false;
        }

        private void Build()
        {
            var canvas = menus != null ? menus.transform : null;
            if (canvas == null)
                return;
            panel = UiFactory.Fullscreen("Level Up", canvas, new Color(0f, 0f, 0f, .72f)).gameObject;
            header = UiFactory.Text("Title", panel.transform, "LEVEL UP!", 64, new Vector2(0, 260), new Vector2(1200, 90), new Color(1f, .85f, .3f));
            UiFactory.Text("Subtitle", panel.transform, "Choose a blessing  (1, 2 or 3)", 32, new Vector2(0, 190), new Vector2(1200, 60), new Color(.85f, .82f, .95f));
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var card = UiFactory.Button("Perk " + (i + 1), panel.transform, "", new Vector2((i - 1) * 420, -20), new Vector2(380, 300), () => Pick(index));
                titles.Add(UiFactory.Text("Name", card.transform, "", 34, new Vector2(0, 80), new Vector2(350, 70), new Color(1f, .85f, .3f)));
                descriptions.Add(UiFactory.Text("Description", card.transform, "", 28, new Vector2(0, -30), new Vector2(340, 150), Color.white));
                UiFactory.Text("Key", card.transform, (i + 1).ToString(), 26, new Vector2(0, -120), new Vector2(60, 40), UiFactory.AccentColor);
            }
        }

        private void Open()
        {
            if (IsOpen || progression == null || progression.PendingChoices <= 0)
                return;
            // Poeni se sada troše u stablu veština; ovde se samo javi da je poen stigao.
            SkillTreeMenu.Announce(progression);
            return;
            if (panel == null)
                Build();
            if (panel == null)
                return;
            offered.Clear();
            offered.AddRange(progression.RollChoices());
            for (int i = 0; i < titles.Count; i++)
            {
                titles[i].text = i < offered.Count ? offered[i].Title : "";
                descriptions[i].text = i < offered.Count ? offered[i].Description : "";
            }

            header.text = "LEVEL " + progression.Level + "!";
            panel.transform.SetAsLastSibling();
            panel.SetActive(true);
            IsOpen = true;
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(panel.GetComponentInChildren<Button>().gameObject);
        }

        private void Update()
        {
            if (!IsOpen)
                return;
            Time.timeScale = 0f;
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
                Pick(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
                Pick(1);
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
                Pick(2);
        }

        private void Pick(int index)
        {
            if (!IsOpen || index < 0 || index >= offered.Count)
                return;
            progression.Choose(offered[index]);
            CombatFeedback.Popup(offered[index].Title, progression.transform.position + Vector3.up * .5f, new Color(1f, .85f, .3f), 4f, 1.2f);
            panel.SetActive(false);
            IsOpen = false;
            Time.timeScale = menus == null || menus.State == MenuState.Playing ? 1f : 0f;
            // Ako je igrač preskočio više nivoa odjednom, odmah se nudi sledeći izbor.
            if (progression.PendingChoices > 0)
                Open();
        }
    }
}
