// Pravi jednostavne UI elemente u kodu (panel, tekst, dugme) u stilu postojećih menija.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AWizardsFight
{
    public static class UiFactory
    {
        public static readonly Color PanelColor = new Color(.07f, .06f, .12f, .94f);
        public static readonly Color ButtonColor = new Color(.2f, .14f, .34f, 1f);
        public static readonly Color AccentColor = new Color(.73f, .6f, 1f, 1f);
        // Najmanja veličina slova (za 1920x1080). Manja slova su u manjem prozoru ili na telefonu nečitljiva.
        public const float MinFontSize = 20f;

        public static RectTransform Rect(string name, Transform parent, Vector2 anchoredPosition, Vector2 size)
        {
            var item = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)item.transform;
            rect.SetParent(parent, false);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return rect;
        }

        public static Image Panel(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var image = Rect(name, parent, anchoredPosition, size).gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        // Panel preko celog roditelja (npr. tamna pozadina iza menija).
        public static Image Fullscreen(string name, Transform parent, Color color)
        {
            var image = Panel(name, parent, Vector2.zero, Vector2.zero, color);
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.anchorMax = Vector2.one;
            return image;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, string value, float size, Vector2 anchoredPosition, Vector2 box, Color color)
        {
            var text = Rect(name, parent, anchoredPosition, box).gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = Mathf.Max(size, MinFontSize);
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        public static Button Button(string name, Transform parent, string label, Vector2 anchoredPosition, Vector2 size, UnityEngine.Events.UnityAction action, float fontSize = 30f)
        {
            var image = Panel(name, parent, anchoredPosition, size, ButtonColor);
            var outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = AccentColor;
            outline.effectDistance = new Vector2(2, -2);
            var button = image.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(1.35f, 1.35f, 1.35f, 1f);
            colors.pressedColor = new Color(.8f, .8f, .8f, 1f);
            button.colors = colors;
            button.onClick.AddListener(action);
            var text = Text("Label", image.transform, label, fontSize, Vector2.zero, size, Color.white);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.sizeDelta = new Vector2(-16, -8);
            return button;
        }
    }
}
