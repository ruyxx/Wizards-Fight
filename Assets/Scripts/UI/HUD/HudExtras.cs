// Dodatni prikaz u HUD-u: traka iskustva, donja traka sa oružjem i sposobnostima, tamne ivice ekrana i upozorenje pri malo zdravlja.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AWizardsFight
{
    [DisallowMultipleComponent]
    public sealed class HudExtras : MonoBehaviour
    {
        private PlayerHealth health;
        private PlayerProgression progression;
        private Image experienceFill, vignette, danger;
        private TextMeshProUGUI levelText, goldText;
        private PlayerEquipment equipment;
        private HotbarView hotbar;

        private void Start()
        {
            health = FindFirstObjectByType<PlayerHealth>();
            Transform hud = null;
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (canvas.name == "HUD")
                    hud = canvas.transform;
            if (health == null || hud == null)
            {
                enabled = false;
                return;
            }

            progression = health.GetComponent<PlayerProgression>();
            equipment = health.GetComponent<PlayerEquipment>();
            goldText = UiFactory.Text("Gold", hud, "", 24, new Vector2(32, -256), new Vector2(420, 32), new Color(1f, .85f, .3f));
            SetCorner(goldText.rectTransform, new Vector2(0, 1));
            goldText.alignment = TextAlignmentOptions.Left;
            goldText.fontStyle = FontStyles.Bold;

            // Ivice ekrana su uvek blago zatamnjene; crvena ivica pulsira kada je zdravlje nisko.
            vignette = UiFactory.Fullscreen("Vignette", hud, new Color(0f, 0f, 0f, .55f));
            vignette.sprite = RuntimeSprites.Vignette;
            vignette.raycastTarget = false;
            vignette.transform.SetAsFirstSibling();
            danger = UiFactory.Fullscreen("Low Health", hud, new Color(.8f, 0f, 0f, 0f));
            danger.sprite = RuntimeSprites.Vignette;
            danger.raycastTarget = false;
            danger.transform.SetSiblingIndex(1);

            var bar = UiFactory.Panel("Experience", hud, new Vector2(32, -222), new Vector2(420, 26), new Color(.05f, .04f, .09f, .85f));
            SetCorner(bar.rectTransform, new Vector2(0, 1));
            bar.raycastTarget = false;
            experienceFill = UiFactory.Panel("Fill", bar.transform, Vector2.zero, Vector2.zero, new Color(1f, .78f, .25f));
            experienceFill.rectTransform.anchorMin = Vector2.zero;
            experienceFill.rectTransform.anchorMax = Vector2.one;
            experienceFill.rectTransform.offsetMin = new Vector2(3, 3);
            experienceFill.rectTransform.offsetMax = new Vector2(-3, -3);
            experienceFill.sprite = RuntimeSprites.Square;
            experienceFill.type = Image.Type.Filled;
            experienceFill.fillMethod = Image.FillMethod.Horizontal;
            experienceFill.raycastTarget = false;
            levelText = UiFactory.Text("Level", bar.transform, "", 22, Vector2.zero, new Vector2(420, 26), Color.white);
            levelText.rectTransform.anchorMin = Vector2.zero;
            levelText.rectTransform.anchorMax = Vector2.one;
            levelText.rectTransform.sizeDelta = Vector2.zero;
            levelText.fontStyle = FontStyles.Bold;

            // Nova donja traka zamenjuje staru listu oružja; ikonu napitka preuzima iz stare trake.
            Sprite potionIcon = null;
            var oldBar = hud.Find("Weapon Selection");
            if (oldBar != null)
            {
                foreach (var image in oldBar.GetComponentsInChildren<Image>(true))
                    if (image.sprite != null && image.sprite.name.Contains("Potion"))
                        potionIcon = image.sprite;
                oldBar.gameObject.SetActive(false);
            }

            hotbar = new HotbarView(hud, health, potionIcon);
            PetCompanion.Spawn(health.transform);
            var menus = FindFirstObjectByType<GameMenus>();
            if (menus != null && menus.GetComponent<SkillTreeMenu>() == null)
                menus.gameObject.AddComponent<SkillTreeMenu>();
            if (MobileMode.Enabled)
                TouchControls.Create(hud, health, potionIcon);
        }

        private static void SetCorner(RectTransform rect, Vector2 corner)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = corner;
        }

        private void Update()
        {
            if (health == null)
                return;
            float ratio = health.CurrentHealth / health.MaxHealth;
            float pulse = ratio < .3f && !health.IsDead ? (.35f + .25f * Mathf.Sin(Time.unscaledTime * 5f)) * (1f - ratio / .3f * .6f) : 0f;
            danger.color = new Color(.8f, 0f, 0f, pulse);

            if (progression != null)
            {
                experienceFill.fillAmount = progression.Experience / progression.RequiredExperience;
                levelText.text = "LV " + progression.Level + "   " + Mathf.FloorToInt(progression.Experience) + " / " + Mathf.CeilToInt(progression.RequiredExperience) + " XP";
            }

            hotbar?.Refresh();
            if (equipment != null)
                goldText.text = "GOLD " + equipment.Gold + "      <size=20><color=#B7B3CC>" + (MobileMode.Enabled ? "BAG" : Keybinds.KeyName(Keybinds.Get(GameAction.Inventory)) + " - INVENTORY") + " (" + equipment.Bag.Count + "/" + PlayerEquipment.BagSize + ")</color></size>";
        }
    }
}
