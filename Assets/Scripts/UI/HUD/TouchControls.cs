// Kontrole na dodir za telefon: plutajući džojstik levo, a desno veliko dugme za izmicanje,
// posebna čarolija, napitak, promena oružja i dugme za interakciju koje se pojavi samo kad ima šta da se uradi.
// Lik sam nišani i puca na najbližeg protivnika (PlayerCombat.AutoAim).
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AWizardsFight
{
    // Dugme koje reaguje odmah na dodir (ne tek kad se prst podigne).
    public sealed class TouchButton : MonoBehaviour, IPointerDownHandler
    {
        public Action Pressed;

        public void OnPointerDown(PointerEventData data) => Pressed?.Invoke();
    }

    // Džojstik se pojavljuje tamo gde prst dodirne levu stranu ekrana.
    public sealed class TouchJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform Area, Base, Knob;
        public float Radius = 110f;
        private Vector2 home;
        private int pointer = int.MinValue;

        private void Start() => home = Base.anchoredPosition;

        public void OnPointerDown(PointerEventData data)
        {
            if (pointer != int.MinValue)
                return;
            pointer = data.pointerId;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(Area, data.position, data.pressEventCamera, out Vector2 local))
                Base.anchoredPosition = local - Area.rect.min;
            Base.GetComponent<Image>().color = new Color(1f, 1f, 1f, .22f);
            OnDrag(data);
        }

        public void OnDrag(PointerEventData data)
        {
            if (data.pointerId != pointer)
                return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(Base, data.position, data.pressEventCamera, out Vector2 local))
                return;
            var offset = Vector2.ClampMagnitude(local, Radius);
            Knob.anchoredPosition = offset;
            // Mala mrtva zona, a dalje brzina raste sa otklonom.
            float amount = Mathf.InverseLerp(.12f, 1f, offset.magnitude / Radius);
            TouchInput.Move = offset.sqrMagnitude > .01f ? offset.normalized * amount : Vector2.zero;
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (data.pointerId != pointer)
                return;
            Release();
        }

        public void Release()
        {
            pointer = int.MinValue;
            TouchInput.Move = Vector2.zero;
            Knob.anchoredPosition = Vector2.zero;
            Base.anchoredPosition = home;
            Base.GetComponent<Image>().color = new Color(1f, 1f, 1f, .1f);
        }

        private void OnDisable()
        {
            if (Base != null)
                Release();
        }
    }

    public sealed class TouchControls : MonoBehaviour
    {
        private PlayerHealth player;
        private PlayerController controller;
        private PlayerCombat combat;
        private PlayerPotions potions;
        private PlayerInteractor interactor;
        private GameObject root;
        private Image dashCooldown, specialCooldown, specialIcon;
        private TextMeshProUGUI potionCount, interactLabel, specialLabel;
        private GameObject interactButton;
        private TouchJoystick joystick;

        private Sprite potionSprite;

        public static TouchControls Create(Transform hud, PlayerHealth player, Sprite potionIcon)
        {
            var controls = hud.gameObject.AddComponent<TouchControls>();
            controls.player = player;
            controls.potionSprite = potionIcon;
            controls.Build(hud);
            return controls;
        }

        private void Build(Transform hud)
        {
            controller = player.GetComponent<PlayerController>();
            combat = player.GetComponent<PlayerCombat>();
            potions = player.GetComponent<PlayerPotions>();
            interactor = player.GetComponent<PlayerInteractor>();
            root = UiFactory.Rect("Touch Controls", hud, Vector2.zero, Vector2.zero).gameObject;
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;

            // Leva strana: nevidljiva površina za džojstik i bledi krug na mestu gde se obično nalazi.
            var area = UiFactory.Panel("Joystick Area", root.transform, Vector2.zero, new Vector2(760, 640), new Color(0, 0, 0, 0));
            Corner(area.rectTransform, Vector2.zero, Vector2.zero);
            var stickBase = UiFactory.Panel("Base", area.transform, new Vector2(240, 230), new Vector2(240, 240), new Color(1f, 1f, 1f, .1f));
            stickBase.sprite = RuntimeSprites.Disc;
            stickBase.raycastTarget = false;
            stickBase.rectTransform.anchorMin = stickBase.rectTransform.anchorMax = Vector2.zero;
            var ring = UiFactory.Panel("Ring", stickBase.transform, Vector2.zero, new Vector2(240, 240), new Color(1f, 1f, 1f, .35f));
            ring.sprite = RuntimeSprites.Ring;
            ring.raycastTarget = false;
            var knob = UiFactory.Panel("Knob", stickBase.transform, Vector2.zero, new Vector2(110, 110), new Color(.8f, .7f, 1f, .75f));
            knob.sprite = RuntimeSprites.Disc;
            knob.raycastTarget = false;
            joystick = area.gameObject.AddComponent<TouchJoystick>();
            joystick.Area = area.rectTransform;
            joystick.Base = stickBase.rectTransform;
            joystick.Knob = knob.rectTransform;

            // Desna strana: dugmad za palac.
            var dash = RoundButton("Dash", new Vector2(-190, 190), 190, "DASH", new Color(.55f, .4f, 1f), TouchInput.Button.Dash);
            dashCooldown = Cooldown(dash, 190);
            var special = RoundButton("Special", new Vector2(-420, 130), 150, "", new Color(1f, .75f, .3f), TouchInput.Button.Special);
            specialIcon = UiFactory.Panel("Icon", special, Vector2.zero, new Vector2(70, 70), Color.white);
            specialIcon.preserveAspect = true;
            specialIcon.raycastTarget = false;
            // Natpisi idu ispod krugova, ne preko njih.
            specialLabel = Label(special, "", 17, new Vector2(0, -98));
            specialCooldown = Cooldown(special, 150);
            var potion = RoundButton("Potion", new Vector2(-380, 330), 120, "", new Color(1f, .4f, .45f), TouchInput.Button.Potion);
            var potionIcon = UiFactory.Panel("Icon", potion, Vector2.zero, new Vector2(58, 58), Color.white);
            potionIcon.sprite = potionSprite != null ? potionSprite : RuntimeSprites.Disc;
            potionIcon.preserveAspect = true;
            potionIcon.raycastTarget = false;
            potionCount = Label(potion, "", 20, new Vector2(0, -82));
            RoundButton("Swap", new Vector2(-170, 420), 110, "SWAP", new Color(.6f, .85f, 1f), TouchInput.Button.NextWeapon);

            // Interakcija: pojavljuje se iznad dugmadi sa tekstom šta će se desiti.
            var interact = UiFactory.Panel("Interact", root.transform, new Vector2(-330, 560), new Vector2(420, 96), new Color(.2f, .15f, .34f, .92f));
            interact.sprite = RuntimeSprites.Rounded;
            interact.type = Image.Type.Sliced;
            Corner(interact.rectTransform, new Vector2(1, 0), new Vector2(.5f, .5f));
            interact.gameObject.AddComponent<TouchButton>().Pressed = () => TouchInput.Press(TouchInput.Button.Interact);
            interactLabel = Label(interact.transform, "", 24, Vector2.zero);
            interactLabel.rectTransform.sizeDelta = new Vector2(400, 90);
            interactButton = interact.gameObject;

            // Gore desno, ispod mape: torba, mapa i pauza.
            SmallButton("Inventory", new Vector2(-350, -70), "BAG", TouchInput.Button.Inventory);
            SmallButton("Map", new Vector2(-460, -70), "MAP", TouchInput.Button.Map);
            SmallButton("Pause", new Vector2(-570, -70), "II", TouchInput.Button.Pause);
            SmallButton("Skills", new Vector2(-680, -70), "SKILLS", TouchInput.Button.Skills);
        }

        private static void Corner(RectTransform rect, Vector2 corner, Vector2 pivot)
        {
            rect.anchorMin = rect.anchorMax = corner;
            rect.pivot = pivot;
        }

        private Transform RoundButton(string name, Vector2 position, float size, string text, Color color, TouchInput.Button action)
        {
            var image = UiFactory.Panel(name, root.transform, position, new Vector2(size, size), new Color(.08f, .06f, .14f, .7f));
            image.sprite = RuntimeSprites.Disc;
            Corner(image.rectTransform, new Vector2(1, 0), new Vector2(.5f, .5f));
            var rim = UiFactory.Panel("Rim", image.transform, Vector2.zero, new Vector2(size, size), new Color(color.r, color.g, color.b, .85f));
            rim.sprite = RuntimeSprites.Ring;
            rim.raycastTarget = false;
            image.gameObject.AddComponent<TouchButton>().Pressed = () => TouchInput.Press(action);
            if (text.Length > 0)
                Label(image.transform, text, size * .17f, Vector2.zero).color = color;
            return image.transform;
        }

        private void SmallButton(string name, Vector2 position, string text, TouchInput.Button action)
        {
            var image = UiFactory.Panel(name, root.transform, position, new Vector2(96, 76), new Color(.08f, .06f, .14f, .8f));
            image.sprite = RuntimeSprites.Rounded;
            image.type = Image.Type.Sliced;
            Corner(image.rectTransform, Vector2.one, new Vector2(.5f, .5f));
            image.gameObject.AddComponent<TouchButton>().Pressed = () => TouchInput.Press(action);
            Label(image.transform, text, 22, Vector2.zero).color = new Color(.85f, .8f, 1f);
        }

        private static TextMeshProUGUI Label(Transform parent, string text, float size, Vector2 position)
        {
            var label = UiFactory.Text("Label", parent, text, size, position, new Vector2(200, 40), Color.white);
            label.fontStyle = FontStyles.Bold;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }

        private static Image Cooldown(Transform parent, float size)
        {
            var image = UiFactory.Panel("Cooldown", parent, Vector2.zero, new Vector2(size, size), new Color(0f, 0f, 0f, .6f));
            image.sprite = RuntimeSprites.Disc;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = (int)Image.Origin360.Top;
            image.fillClockwise = false;
            image.fillAmount = 0f;
            image.raycastTarget = false;
            return image;
        }

        private void Update()
        {
            if (player == null)
                return;
            // Kontrole se skrivaju dok je otvoren neki meni preko igre.
            bool show = Time.timeScale > 0f && !player.IsDead && !InventoryMenu.IsOpen && !CampMenu.IsOpen && !MessagePanel.IsOpen && !PerkChoiceMenu.IsOpen && !SkillTreeMenu.IsOpen;
            if (root.activeSelf != show)
            {
                root.SetActive(show);
                if (!show)
                    joystick.Release();
            }
            if (!show)
                return;
            // Dugmad uvek iznad ostatka HUD-a (i uvećane mape), da bi dodir stigao do njih.
            if (root.transform.GetSiblingIndex() != root.transform.parent.childCount - 1)
                root.transform.SetAsLastSibling();

            if (controller != null)
                dashCooldown.fillAmount = controller.DashCooldownRemaining > 0f ? controller.DashCooldownRemaining / Mathf.Max(.01f, controller.DashCooldownTotal) : 0f;
            var weapon = player.GetComponent<PlayerWeapons>()?.Selected;
            if (combat != null && weapon != null)
            {
                specialIcon.sprite = weapon.Icon;
                specialLabel.text = PlayerCombat.SpecialName(weapon.Type);
                specialCooldown.fillAmount = combat.SpecialCooldownRemaining > 0f ? combat.SpecialCooldownRemaining / Mathf.Max(.01f, combat.SpecialCooldownTotal) : 0f;
            }
            if (potions != null)
                potionCount.text = "x" + potions.Count;
            string prompt = interactor != null ? interactor.CurrentPrompt : "";
            interactButton.SetActive(!string.IsNullOrEmpty(prompt));
            if (interactButton.activeSelf)
                interactLabel.text = TouchPrompt(prompt);
        }

        // "E - OPEN CHEST" postaje "OPEN CHEST" (na telefonu nema tastera E).
        public static string TouchPrompt(string prompt) => string.IsNullOrEmpty(prompt) ? "" : prompt.StartsWith("E - ") ? prompt.Substring(4) : prompt;
    }
}
