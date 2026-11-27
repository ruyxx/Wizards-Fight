// Jednostavan prozor preko igre: naslov, tekst i dugmad za izbor (beleške, oltari...). Igra stoji dok je otvoren.
using System;
using TMPro;
using UnityEngine;

namespace AWizardsFight
{
    public sealed class MessagePanel : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        private static MessagePanel instance;
        private GameMenus menus;
        private GameObject root;
        private TextMeshProUGUI title, body;
        private Transform buttons;

        public static void Show(string heading, string text, params (string label, Action action)[] options)
        {
            var menus = FindFirstObjectByType<GameMenus>();
            if (menus == null)
                return;
            if (instance == null)
                instance = menus.gameObject.AddComponent<MessagePanel>();
            instance.menus = menus;
            instance.Open(heading, text, options);
        }

        private void Build()
        {
            root = UiFactory.Fullscreen("Message", menus.transform, new Color(0f, 0f, 0f, .82f)).gameObject;
            UiFactory.Panel("Panel", root.transform, Vector2.zero, new Vector2(1000, 620), new Color(.07f, .06f, .11f, .97f));
            title = UiFactory.Text("Title", root.transform, "", 42, new Vector2(0, 245), new Vector2(940, 60), new Color(1f, .85f, .35f));
            body = UiFactory.Text("Body", root.transform, "", 24, new Vector2(0, 40), new Vector2(900, 330), Color.white);
            buttons = UiFactory.Rect("Buttons", root.transform, new Vector2(0, -230), new Vector2(940, 80));
        }

        private void Open(string heading, string text, (string label, Action action)[] options)
        {
            if (root == null)
                Build();
            title.text = heading;
            body.text = text;
            foreach (Transform child in buttons)
                Destroy(child.gameObject);
            if (options == null || options.Length == 0)
                options = new (string, Action)[] { ("CONTINUE", null) };
            float width = Mathf.Min(300f, 920f / options.Length - 20f);
            for (int i = 0; i < options.Length; i++)
            {
                var option = options[i];
                UiFactory.Button("Option", buttons, option.label, new Vector2((i - (options.Length - 1) / 2f) * (width + 20f), 0), new Vector2(width, 70), () =>
                {
                    Close();
                    option.action?.Invoke();
                }, 22);
            }

            root.transform.SetAsLastSibling();
            root.SetActive(true);
            IsOpen = true;
            var firstButton = buttons.GetComponentInChildren<UnityEngine.UI.Button>();
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(firstButton != null ? firstButton.gameObject : null);
            Time.timeScale = 0f;
        }

        private void Update()
        {
            if (IsOpen)
                Time.timeScale = 0f;
        }

        private void Close()
        {
            root.SetActive(false);
            IsOpen = false;
            Time.timeScale = menus != null && menus.State == MenuState.Playing ? 1f : 0f;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
                IsOpen = false;
            }
        }
    }
}
