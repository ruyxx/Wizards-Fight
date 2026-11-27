// Dopunjuje meni podešavanja: izbor težine i ekran za promenu tastera.
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace AWizardsFight
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GameMenus))]
    public sealed class SettingsExtras : MonoBehaviour
    {
        public static bool IsCapturingKey { get; private set; }

        private TextMeshProUGUI difficultyLabel;
        private GameObject controlsPanel;
        private readonly List<TextMeshProUGUI> keyLabels = new List<TextMeshProUGUI>();
        private int capturing = -1;

        private void Awake()
        {
            var options = transform.Find("Options");
            if (options == null)
                return;
            // Proširuje pozadinu podešavanja da stanu nova dugmad.
            var contents = options.Find("Contents") as RectTransform;
            if (contents != null)
            {
                contents.sizeDelta = new Vector2(contents.sizeDelta.x, 660);
                contents.anchoredPosition = new Vector2(contents.anchoredPosition.x, -50);
            }

            var back = options.Find("BACK") as RectTransform;
            if (back != null)
                back.anchoredPosition = new Vector2(0, -330);
            var difficulty = UiFactory.Button("DIFFICULTY", options, "", new Vector2(0, -185), new Vector2(420, 60), CycleDifficulty, 28);
            difficultyLabel = difficulty.GetComponentInChildren<TextMeshProUGUI>();
            UiFactory.Button("CONTROLS", options, "CONTROLS", new Vector2(0, -255), new Vector2(420, 60), OpenControls, 26);
            // Naslov ide iznad pomerenog sadržaja, da se ne preklapa sa prvim natpisom.
            foreach (var text in options.GetComponentsInChildren<TMP_Text>(true))
                if (text.text == "OPTIONS")
                    text.rectTransform.anchoredPosition += new Vector2(0, 75);
            ShowDifficulty();
            BuildControls(options);
        }

        private void OnDisable() => IsCapturingKey = false;

        private void Update()
        {
            // Ako je meni podešavanja zatvoren (npr. Escape), zatvara se i ekran tastera.
            if (controlsPanel != null && controlsPanel.activeSelf && !controlsPanel.transform.parent.gameObject.activeInHierarchy)
                CloseControls();
        }

        private void CycleDifficulty()
        {
            GameSettings.Difficulty = (Difficulty)(((int)GameSettings.Difficulty + 1) % 3);
            ShowDifficulty();
        }

        private void ShowDifficulty()
        {
            if (difficultyLabel != null)
                difficultyLabel.text = "DIFFICULTY: " + GameSettings.Difficulty.ToString().ToUpperInvariant();
        }

        private void BuildControls(Transform options)
        {
            controlsPanel = UiFactory.Fullscreen("Controls", options, new Color(.04f, .035f, .07f, .97f)).gameObject;
            UiFactory.Text("Title", controlsPanel.transform, "CONTROLS", 56, new Vector2(0, 400), new Vector2(900, 80), UiFactory.AccentColor);
            UiFactory.Text("Hint", controlsPanel.transform, "Click an action, then press the new key.  Mouse: aim, left click cast, right click special spell, Q R F G active spells.", 22, new Vector2(0, 340), new Vector2(1400, 40), new Color(.8f, .78f, .9f));
            for (int i = 0; i < Keybinds.Count; i++)
            {
                int index = i;
                // Tri kolone po sedam radnji.
                int column = i / 7, row = i % 7;
                var button = UiFactory.Button("Bind " + (GameAction)i, controlsPanel.transform, "", new Vector2((column - 1) * 620, 260 - row * 72), new Vector2(580, 60), () => StartCapture(index), 24);
                keyLabels.Add(button.GetComponentInChildren<TextMeshProUGUI>());
            }

            UiFactory.Button("Reset", controlsPanel.transform, "RESET TO DEFAULT", new Vector2(-230, -330), new Vector2(420, 68), ResetKeys, 26);
            UiFactory.Button("Done", controlsPanel.transform, "DONE", new Vector2(230, -330), new Vector2(420, 68), CloseControls, 26);
            controlsPanel.SetActive(false);
            RefreshKeys();
        }

        private void OpenControls()
        {
            RefreshKeys();
            controlsPanel.transform.SetAsLastSibling();
            controlsPanel.SetActive(true);
        }

        private void CloseControls()
        {
            capturing = -1;
            IsCapturingKey = false;
            controlsPanel.SetActive(false);
        }

        private void ResetKeys()
        {
            Keybinds.ResetAll();
            capturing = -1;
            IsCapturingKey = false;
            RefreshKeys();
        }

        private void StartCapture(int index)
        {
            capturing = index;
            IsCapturingKey = true;
            RefreshKeys();
        }

        private void RefreshKeys()
        {
            for (int i = 0; i < keyLabels.Count; i++)
            {
                var action = (GameAction)i;
                keyLabels[i].text = Keybinds.Label(action) + ":   " + (i == capturing ? "PRESS A KEY..." : Keybinds.KeyName(Keybinds.Get(action)));
            }
        }

        // Unity šalje pritisnut taster kroz OnGUI događaj; Escape otkazuje promenu.
        private void OnGUI()
        {
            if (capturing < 0)
                return;
            var current = Event.current;
            if (current == null || current.type != EventType.KeyDown || current.keyCode == KeyCode.None)
                return;
            if (current.keyCode != KeyCode.Escape)
                Keybinds.Set((GameAction)capturing, current.keyCode);
            capturing = -1;
            current.Use();
            RefreshKeys();
            // Escape iz ovog pritiska ne sme da zatvori i podešavanja, pa se oznaka skida tek u sledećem frejmu.
            StartCoroutine(ReleaseCapture());
        }

        private System.Collections.IEnumerator ReleaseCapture()
        {
            yield return null;
            IsCapturingKey = false;
        }
    }
}
