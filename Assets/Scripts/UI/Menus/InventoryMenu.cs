// Inventar (taster B): oprema na liku, torba, rune u predmetima i oružjima, statistike i opis predmeta sa poređenjem.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AWizardsFight
{
    // Jedno polje inventara: prima klik levim i desnim tasterom i prelazak mišem.
    // Prevlačenje levim tasterom: predmet se nosi kao senka i spušta na željeno polje.
    public sealed class InventorySlotView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        public Action<bool> Clicked;
        public Action<bool> Hovered;
        public Func<bool> BeginDrag;
        public Action<Vector2> Dragging;
        public Action EndDrag;
        public Action Dropped;

        public void OnPointerClick(PointerEventData data) => Clicked?.Invoke(data.button == PointerEventData.InputButton.Right);
        public void OnPointerEnter(PointerEventData data) => Hovered?.Invoke(true);
        public void OnPointerExit(PointerEventData data) => Hovered?.Invoke(false);

        public void OnBeginDrag(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || BeginDrag == null || !BeginDrag())
                data.pointerDrag = null;
        }

        public void OnDrag(PointerEventData data) => Dragging?.Invoke(data.position);
        public void OnEndDrag(PointerEventData data) => EndDrag?.Invoke();
        public void OnDrop(PointerEventData data) => Dropped?.Invoke();
    }

    [DisallowMultipleComponent]
    public sealed class InventoryMenu : MonoBehaviour
    {
        private const float BagSlot = 80f, BagGap = 10f, DollSlot = 100f;
        private static readonly Color PanelColor = new Color(.09f, .08f, .14f, .97f);
        private static readonly Color EmptyColor = new Color(.15f, .14f, .22f, 1f);
        private static readonly Color FrameColor = new Color(.28f, .26f, .38f);
        private static readonly Color TargetColor = new Color(1f, .85f, .3f);
        private static readonly Color MutedText = new Color(.6f, .58f, .72f);

        public static bool IsOpen { get; private set; }

        private sealed class SlotUi
        {
            public Image Background, Icon, Frame, Hover;
            public TextMeshProUGUI Text, Placeholder;
            public Image[] Sockets;
        }

        private PlayerEquipment equipment;
        private PlayerWeapons weapons;
        private GameMenus menus;
        private GameObject root;
        private readonly List<SlotUi> bagSlots = new List<SlotUi>();
        private readonly SlotUi[] dollSlots = new SlotUi[PlayerEquipment.SlotCount];
        private readonly SlotUi[] weaponIcons = new SlotUi[WeaponTypes.Count];
        private readonly SlotUi[,] weaponSockets = new SlotUi[WeaponTypes.Count, 3];
        private TextMeshProUGUI goldText, statsText, tooltipText, hintText, bagTitle;
        private Item pendingRune;
        private string notice = "";
        // Prevlačenje: predmet koji se nosi, odakle je uzet i senka koja prati miš.
        private Item dragItem;
        private EquipSlot? dragFrom;
        private Image ghost;
        // Telefon: izabrani predmet i dugmad sa radnjama za njega.
        private Item touchItem;
        private EquipSlot? touchSlot;
        private Button primaryAction, secondaryAction;
        private bool touchConfirm;
        private RectTransform rootRect;

        private void Awake()
        {
            var player = FindFirstObjectByType<PlayerHealth>();
            equipment = player != null ? player.GetComponent<PlayerEquipment>() : null;
            weapons = player != null ? player.GetComponent<PlayerWeapons>() : null;
            menus = FindFirstObjectByType<GameMenus>();
        }

        private void OnEnable()
        {
            if (equipment != null)
                equipment.Changed += Refresh;
        }

        private void OnDisable()
        {
            if (equipment != null)
                equipment.Changed -= Refresh;
            IsOpen = false;
        }

        private void Update()
        {
            if (equipment == null || menus == null || !Application.isFocused)
                return;
            if (IsOpen)
            {
                Time.timeScale = 0f;
                if (Keybinds.Pressed(GameAction.Inventory) || Input.GetKeyDown(KeyCode.Escape) || GamepadInput.InventoryPressed || Input.GetKeyDown(KeyCode.JoystickButton1))
                    Close();
                return;
            }

            if ((Keybinds.Pressed(GameAction.Inventory) || GamepadInput.InventoryPressed) && menus.State == MenuState.Playing && !PerkChoiceMenu.IsOpen && !SkillTreeMenu.IsOpen)
                Open();
        }

        private void Open()
        {
            if (root == null)
                Build();
            pendingRune = null;
            notice = "";
            root.transform.SetAsLastSibling();
            root.SetActive(true);
            IsOpen = true;
            Time.timeScale = 0f;
            Refresh();
            ShowTooltip(null);
        }

        private void Close()
        {
            root.SetActive(false);
            IsOpen = false;
            pendingRune = null;
            dragItem = null;
            dragFrom = null;
            Time.timeScale = menus.State == MenuState.Playing && !PerkChoiceMenu.IsOpen ? 1f : 0f;
        }

        // ---------- Izgradnja prikaza ----------

        private void Build()
        {
            root = UiFactory.Fullscreen("Inventory", menus.transform, new Color(.01f, .01f, .03f, .97f)).gameObject;
            rootRect = (RectTransform)root.transform;

            // Gornja traka: naslov levo, zlato i taster za zatvaranje desno.
            var bar = Card("Top Bar", root.transform, new Vector2(0, 470), new Vector2(1700, 76), new Color(.11f, .09f, .18f, .98f));
            var title = UiFactory.Text("Title", bar, "INVENTORY", 40, new Vector2(-590, 0), new Vector2(480, 60), UiFactory.AccentColor);
            title.alignment = TextAlignmentOptions.Left;
            title.fontStyle = FontStyles.Bold;
            goldText = UiFactory.Text("Gold", bar, "", 30, new Vector2(330, 0), new Vector2(400, 50), new Color(1f, .85f, .3f));
            goldText.alignment = TextAlignmentOptions.Right;
            if (MobileMode.Enabled)
                UiFactory.Button("Close", bar, "CLOSE", new Vector2(730, 0), new Vector2(180, 56), Close, 24);
            else
            {
                var close = UiFactory.Text("Close", bar, Chip(Keybinds.Get(GameAction.Inventory).ToString()) + " close", 20, new Vector2(700, 0), new Vector2(260, 50), MutedText);
                close.alignment = TextAlignmentOptions.Right;
            }
            hintText = UiFactory.Text("Hint", root.transform, "", 20, new Vector2(0, -478), new Vector2(1700, 56), new Color(.82f, .8f, .92f));

            var doll = Card("Equipment", root.transform, new Vector2(-590, -25), new Vector2(520, 840), PanelColor);
            Header(doll, "EQUIPMENT", "what you wear", 520);
            var positions = new Dictionary<EquipSlot, Vector2>
            {
                { EquipSlot.Helmet, new Vector2(0, 280) }, { EquipSlot.Amulet, new Vector2(140, 280) },
                { EquipSlot.Gloves, new Vector2(-140, 155) }, { EquipSlot.Chest, new Vector2(0, 155) },
                { EquipSlot.Ring1, new Vector2(140, 155) }, { EquipSlot.Boots, new Vector2(0, 30) },
                { EquipSlot.Ring2, new Vector2(140, 30) }
            };
            // Silueta lika iza mesta za opremu, da se odmah vidi šta ide gde.
            var figure = UiFactory.Panel("Figure", doll, new Vector2(0, 155), new Vector2(130, 370), new Color(.22f, .18f, .36f, .35f));
            figure.sprite = RuntimeSprites.Rounded;
            figure.type = Image.Type.Sliced;
            foreach (var pair in positions)
            {
                var slot = pair.Key;
                string name = slot.ToString().ToUpperInvariant().Replace("1", " 1").Replace("2", " 2");
                var ui = CreateSlot(doll, pair.Value, DollSlot, true, name);
                var view = ui.Background.gameObject.AddComponent<InventorySlotView>();
                view.Clicked = right => EquipSlotClicked(slot, right);
                view.Hovered = over => { ui.Hover.enabled = over; ShowTooltip(over ? equipment.Equipped(slot) : null); };
                view.BeginDrag = () => StartDrag(equipment.Equipped(slot), slot);
                view.Dragging = MoveGhost;
                view.EndDrag = FinishDrag;
                view.Dropped = () => DropOnSlot(slot);
                dollSlots[(int)slot] = ui;
            }

            var weaponHeader = UiFactory.Text("Weapons", doll, "WEAPON RUNES", 22, new Vector2(-30, -70), new Vector2(420, 36), Color.white);
            weaponHeader.alignment = TextAlignmentOptions.Left;
            weaponHeader.fontStyle = FontStyles.Bold;
            UiFactory.Panel("Divider", doll, new Vector2(0, -92), new Vector2(470, 2), new Color(.3f, .26f, .45f));
            for (int w = 0; w < WeaponTypes.Count; w++)
            {
                int weapon = w;
                float y = -128 - w * 60;
                weaponIcons[w] = CreateSlot(doll, new Vector2(-200, y), 56, false, null);
                weaponIcons[w].Text = UiFactory.Text("Name", doll, "", 20, new Vector2(-80, y), new Vector2(150, 60), Color.white);
                weaponIcons[w].Text.enableAutoSizing = true;
                weaponIcons[w].Text.fontSizeMin = 15;
                weaponIcons[w].Text.fontSizeMax = 20;
                weaponIcons[w].Text.alignment = TextAlignmentOptions.Left;
                for (int s = 0; s < 3; s++)
                {
                    int socket = s;
                    var ui = CreateSlot(doll, new Vector2(60 + s * 66, y), 54, false, null);
                    var view = ui.Background.gameObject.AddComponent<InventorySlotView>();
                    view.Clicked = right => WeaponSocketClicked((WeaponType)weapon, socket);
                    view.Hovered = over => { ui.Hover.enabled = over; ShowWeaponSocket(over, (WeaponType)weapon, socket); };
                    view.Dropped = () => DropOnWeapon((WeaponType)weapon);
                    weaponSockets[w, s] = ui;
                }
            }

            var bag = Card("Bag", root.transform, new Vector2(0, -25), new Vector2(600, 840), PanelColor);
            bagTitle = Header(bag, "BAG", "", 600);
            float width = BagSlot * 6 + BagGap * 5;
            for (int i = 0; i < PlayerEquipment.BagSize; i++)
            {
                int index = i;
                var position = new Vector2(-width / 2f + BagSlot / 2f + (i % 6) * (BagSlot + BagGap), 285 - (i / 6) * (BagSlot + BagGap));
                var ui = CreateSlot(bag, position, BagSlot, true, null);
                var view = ui.Background.gameObject.AddComponent<InventorySlotView>();
                view.Clicked = right => BagClicked(index, right);
                view.Hovered = over => { ui.Hover.enabled = over; ShowTooltip(over && index < equipment.Bag.Count ? equipment.Bag[index] : null, true); };
                view.BeginDrag = () => StartDrag(index < equipment.Bag.Count ? equipment.Bag[index] : null, null);
                view.Dragging = MoveGhost;
                view.EndDrag = FinishDrag;
                view.Dropped = DropOnBag;
                bagSlots.Add(ui);
            }

            // Legenda boja retkosti ispod torbe.
            string legend = string.Join("     ", Enum.GetValues(typeof(WeaponRarity)).Cast<WeaponRarity>().Select(r => "<color=#" + ColorUtility.ToHtmlStringRGB(ItemCatalog.RarityColor(r)) + ">■ " + r + "</color>"));
            UiFactory.Text("Legend", bag, legend, 18, new Vector2(0, -320), new Vector2(560, 34), MutedText);
            UiFactory.Text("Sockets", bag, "<color=#8C88A0>●</color> dots under an item are rune sockets", 16, new Vector2(0, -355), new Vector2(560, 30), MutedText);

            var info = Card("Info", root.transform, new Vector2(600, -25), new Vector2(500, 840), PanelColor);
            Header(info, "CHARACTER", "your totals", 500);
            statsText = UiFactory.Text("Stats", info, "", 22, new Vector2(0, 175), new Vector2(440, 320), Color.white);
            statsText.enableAutoSizing = true;
            statsText.fontSizeMin = 18;
            statsText.fontSizeMax = 22;
            statsText.alignment = TextAlignmentOptions.TopLeft;
            var tooltipCard = Card("Tooltip Card", info, new Vector2(0, -210), new Vector2(460, 400), new Color(.06f, .05f, .1f, 1f));
            tooltipText = UiFactory.Text("Tooltip", tooltipCard, "", 21, Vector2.zero, new Vector2(420, 370), Color.white);
            tooltipText.enableAutoSizing = true;
            tooltipText.fontSizeMin = 18;
            tooltipText.fontSizeMax = 21;
            if (MobileMode.Enabled)
            {
                // Telefon: dodir bira predmet, a radnje su dugmad ispod opisa (nema desnog klika ni prelaska mišem).
                tooltipCard.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -170);
                tooltipCard.GetComponent<RectTransform>().sizeDelta = new Vector2(460, 320);
                tooltipText.rectTransform.sizeDelta = new Vector2(420, 300);
                tooltipText.fontSizeMax = 20;
                primaryAction = UiFactory.Button("Primary", info, "", new Vector2(-117, -375), new Vector2(226, 60), PrimaryAction, 20);
                secondaryAction = UiFactory.Button("Secondary", info, "", new Vector2(117, -375), new Vector2(226, 60), SecondaryAction, 20);
            }
            tooltipText.alignment = TextAlignmentOptions.TopLeft;

            ghost = UiFactory.Panel("Drag Ghost", root.transform, Vector2.zero, new Vector2(72, 72), new Color(1f, 1f, 1f, .9f));
            ghost.preserveAspect = true;
            ghost.raycastTarget = false;
            ghost.gameObject.SetActive(false);
            root.SetActive(false);
        }

        private static string Chip(string key) => "<color=#FFD84D><b>[" + key + "]</b></color>";

        // Zaobljena kartica sa tankom ivicom.
        private static Transform Card(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var card = UiFactory.Panel(name, parent, position, size, color);
            card.sprite = RuntimeSprites.Rounded;
            card.type = Image.Type.Sliced;
            var frame = UiFactory.Panel("Frame", card.transform, Vector2.zero, size, new Color(.3f, .26f, .46f, .9f));
            frame.sprite = RuntimeSprites.RoundedFrame;
            frame.type = Image.Type.Sliced;
            frame.raycastTarget = false;
            return card.transform;
        }

        // Naslov kartice: ljubičasta crtica levo, veliki naslov i sitan opis desno.
        private static TextMeshProUGUI Header(Transform card, string title, string subtitle, float width)
        {
            float top = 420f - 44f;
            UiFactory.Panel("Accent", card, new Vector2(-width / 2f + 26, top), new Vector2(5, 30), UiFactory.AccentColor);
            var text = UiFactory.Text("Header", card, title, 26, new Vector2(20, top), new Vector2(width - 80, 40), Color.white);
            text.alignment = TextAlignmentOptions.Left;
            text.fontStyle = FontStyles.Bold;
            if (subtitle.Length > 0)
            {
                var note = UiFactory.Text("Sub", card, subtitle, 16, new Vector2(10, top), new Vector2(width - 60, 40), MutedText);
                note.alignment = TextAlignmentOptions.Right;
            }
            UiFactory.Panel("Divider", card, new Vector2(0, top - 30), new Vector2(width - 40, 2), new Color(.3f, .26f, .45f));
            return text;
        }

        private static SlotUi CreateSlot(Transform parent, Vector2 position, float size, bool sockets, string placeholder)
        {
            var ui = new SlotUi { Background = UiFactory.Panel("Slot", parent, position, new Vector2(size, size), EmptyColor) };
            ui.Background.sprite = RuntimeSprites.Rounded;
            ui.Background.type = Image.Type.Sliced;
            ui.Background.raycastTarget = true;
            if (placeholder != null)
            {
                ui.Placeholder = UiFactory.Text("Placeholder", ui.Background.transform, placeholder, 14, Vector2.zero, new Vector2(size - 8, size - 8), new Color(.45f, .43f, .58f));
                ui.Placeholder.fontStyle = FontStyles.Bold;
            }
            ui.Icon = UiFactory.Panel("Icon", ui.Background.transform, new Vector2(0, sockets ? 6 : 0), new Vector2(size * .66f, size * .66f), Color.white);
            ui.Icon.preserveAspect = true;
            ui.Icon.raycastTarget = false;
            ui.Hover = UiFactory.Panel("Hover", ui.Background.transform, Vector2.zero, new Vector2(size, size), new Color(1f, 1f, 1f, .09f));
            ui.Hover.sprite = RuntimeSprites.Rounded;
            ui.Hover.type = Image.Type.Sliced;
            ui.Hover.raycastTarget = false;
            ui.Hover.enabled = false;
            ui.Frame = UiFactory.Panel("Frame", ui.Background.transform, Vector2.zero, new Vector2(size, size), FrameColor);
            ui.Frame.sprite = RuntimeSprites.RoundedFrame;
            ui.Frame.type = Image.Type.Sliced;
            ui.Frame.raycastTarget = false;
            if (sockets)
            {
                ui.Sockets = new Image[3];
                for (int i = 0; i < 3; i++)
                {
                    ui.Sockets[i] = UiFactory.Panel("Socket", ui.Background.transform, new Vector2((i - 1) * 16, -size / 2f + 11), new Vector2(11, 11), Color.clear);
                    ui.Sockets[i].sprite = RuntimeSprites.Disc;
                    ui.Sockets[i].raycastTarget = false;
                }
            }
            return ui;
        }

        // Okvir polja: deblji i zlatan kada je meta, inače boja retkosti ili prigušen.
        private static void SetFrame(SlotUi ui, Color color, bool strong)
        {
            ui.Frame.color = color;
            ui.Frame.rectTransform.sizeDelta = ui.Background.rectTransform.sizeDelta + (strong ? new Vector2(8, 8) : Vector2.zero);
        }

        // ---------- Prevlačenje ----------

        private bool StartDrag(Item item, EquipSlot? from)
        {
            if (item == null)
                return false;
            dragItem = item;
            dragFrom = from;
            pendingRune = null;
            ghost.sprite = ItemCatalog.Icon(item);
            ghost.color = item.IsRune ? ItemCatalog.RuneColor(item.rune) : Color.white;
            ghost.gameObject.SetActive(true);
            ghost.transform.SetAsLastSibling();
            MoveGhost(Input.mousePosition);
            Refresh();
            return true;
        }

        private void MoveGhost(Vector2 screen)
        {
            var canvas = root.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRect, screen, camera, out Vector2 local))
                ghost.rectTransform.anchoredPosition = local;
        }

        private void FinishDrag()
        {
            ghost.gameObject.SetActive(false);
            dragItem = null;
            dragFrom = null;
            Refresh();
        }

        private void DropOnSlot(EquipSlot slot)
        {
            if (dragItem == null)
                return;
            var target = equipment.Equipped(slot);
            if (dragFrom.HasValue)
            {
                if (dragFrom.Value != slot)
                    notice = equipment.SwapEquipped(dragFrom.Value, slot) ? "Swapped places." : "<color=#FF7070>" + dragItem.name + " does not go there.</color>";
            }
            else if (dragItem.IsRune)
            {
                if (target != null && equipment.InsertRune(dragItem, slot))
                {
                    notice = dragItem.name + " socketed into " + target.name + ".";
                    Sfx.Play(SfxId.Block, .6f);
                }
                else
                    notice = "<color=#FF7070>" + (target == null ? "Nothing to socket there." : target.name + " has no free socket.") + "</color>";
            }
            else if (equipment.Equip(dragItem, slot))
            {
                notice = "Equipped " + dragItem.name + ".";
                Sfx.Play(SfxId.Pickup, .7f);
            }
            else
                notice = "<color=#FF7070>" + dragItem.name + " is a " + dragItem.gear + ", it does not fit the " + slot + " slot.</color>";
        }

        private void DropOnWeapon(WeaponType weapon)
        {
            if (dragItem == null)
                return;
            if (dragItem.IsRune && !dragFrom.HasValue && equipment.InsertRune(dragItem, weapon))
            {
                notice = dragItem.name + " socketed into the " + weapon + ".";
                Sfx.Play(SfxId.Block, .6f);
            }
            else
                notice = "<color=#FF7070>Only runes go into weapon sockets, and the socket must be free.</color>";
        }

        private void DropOnBag()
        {
            if (dragItem == null || !dragFrom.HasValue)
                return;
            notice = equipment.Unequip(dragFrom.Value) ? "Took off " + dragItem.name + "." : "<color=#FF7070>Bag is full.</color>";
        }

        // Da li je polje dobra meta za predmet koji se trenutno prevlači.
        private bool CanDrop(EquipSlot slot)
        {
            if (dragItem == null)
                return false;
            if (dragItem.IsRune)
                return !dragFrom.HasValue && equipment.Equipped(slot) != null && equipment.Equipped(slot).FreeSockets > 0;
            return dragItem.Fits(slot) && dragFrom != slot;
        }

        // Test i džojstik: isto što i prevlačenje mišem, bez miša.
        public bool DragForTest(Item item, EquipSlot? from, EquipSlot? toSlot, WeaponType? toWeapon, bool toBag)
        {
            if (!StartDrag(item, from))
                return false;
            if (toSlot.HasValue)
                DropOnSlot(toSlot.Value);
            if (toWeapon.HasValue)
                DropOnWeapon(toWeapon.Value);
            if (toBag)
                DropOnBag();
            FinishDrag();
            return true;
        }

        // ---------- Osvežavanje ----------

        // ---------- Telefon: izbor i radnje ----------

        private void SelectForTouch(Item item, EquipSlot? slot)
        {
            touchItem = item;
            touchSlot = item != null ? slot : null;
            ShowTooltip(item, slot == null);
            Refresh();
        }

        private void PrimaryAction()
        {
            if (touchItem == null)
                return;
            var item = touchItem;
            touchItem = null;
            touchConfirm = true;
            if (touchSlot.HasValue)
                EquipSlotClicked(touchSlot.Value, false);
            else
            {
                int index = equipment.Bag.ToList().IndexOf(item);
                if (index >= 0)
                {
                    if (item.IsRune)
                        pendingRune = item;
                    else
                        BagClicked(index, false);
                }
            }
            touchSlot = null;
            touchConfirm = false;
            Refresh();
        }

        private void SecondaryAction()
        {
            if (touchItem == null)
                return;
            touchConfirm = true;
            if (touchSlot.HasValue)
                EquipSlotClicked(touchSlot.Value, true);
            else
            {
                int index = equipment.Bag.ToList().IndexOf(touchItem);
                if (index >= 0)
                    BagClicked(index, true);
            }
            touchItem = null;
            touchSlot = null;
            touchConfirm = false;
            Refresh();
        }

        private void RefreshTouchActions()
        {
            if (primaryAction == null)
                return;
            if (touchItem != null && !equipment.Bag.Contains(touchItem) && !(touchSlot.HasValue && equipment.Equipped(touchSlot.Value) == touchItem))
            {
                touchItem = null;
                touchSlot = null;
            }
            bool any = touchItem != null;
            primaryAction.gameObject.SetActive(any);
            secondaryAction.gameObject.SetActive(any && (!touchSlot.HasValue || touchItem.runes.Count > 0));
            if (!any)
                return;
            primaryAction.GetComponentInChildren<TextMeshProUGUI>().text = touchSlot.HasValue ? "TAKE OFF" : touchItem.IsRune ? "SOCKET" : "EQUIP";
            secondaryAction.GetComponentInChildren<TextMeshProUGUI>().text = touchSlot.HasValue ? "REMOVE RUNE" : "SALVAGE " + touchItem.GoldValue + " G";
        }

        private void Refresh()
        {
            if (root == null || !root.activeSelf)
                return;
            goldText.text = equipment.Gold + " <size=20>GOLD</size>";
            bagTitle.text = "BAG  <color=#9993B0><size=20>" + equipment.Bag.Count + " / " + PlayerEquipment.BagSize + "</size></color>";
            for (int i = 0; i < bagSlots.Count; i++)
            {
                var item = i < equipment.Bag.Count ? equipment.Bag[i] : null;
                ShowItem(bagSlots[i], item, item != null && (item == pendingRune || item == touchItem));
                if (item != null && item == dragItem)
                    bagSlots[i].Icon.color = new Color(1f, 1f, 1f, .3f);
            }

            foreach (EquipSlot slot in Enum.GetValues(typeof(EquipSlot)))
            {
                var item = equipment.Equipped(slot);
                bool target = (pendingRune != null && item != null && item.FreeSockets > 0) || CanDrop(slot);
                ShowItem(dollSlots[(int)slot], item, target);
                dollSlots[(int)slot].Placeholder.enabled = item == null;
                if (item != null && item == dragItem)
                    dollSlots[(int)slot].Icon.color = new Color(1f, 1f, 1f, .3f);
            }

            for (int w = 0; w < WeaponTypes.Count; w++)
            {
                var weapon = weapons != null ? weapons.GetOwned((WeaponType)w) : null;
                weaponIcons[w].Icon.sprite = weapon != null ? weapon.Icon : null;
                weaponIcons[w].Icon.enabled = weapon != null && weapon.Icon != null;
                SetFrame(weaponIcons[w], weapon != null ? ItemCatalog.RarityColor(weapon.Rarity) : FrameColor, false);
                weaponIcons[w].Text.text = weapon != null ? weapon.WeaponName + "\n<size=17>" + WeaponRarityPresentation.Label(weapon.Rarity) + "</size>" : "LOCKED";
                int sockets = equipment.WeaponSocketCount((WeaponType)w);
                var runes = equipment.WeaponRunes((WeaponType)w);
                for (int s = 0; s < 3; s++)
                {
                    var ui = weaponSockets[w, s];
                    bool available = s < sockets;
                    var rune = s < runes.Count ? runes[s] : null;
                    ui.Background.color = available ? EmptyColor : new Color(.05f, .05f, .07f, .6f);
                    ui.Icon.enabled = rune != null;
                    ui.Icon.sprite = ItemCatalog.Icon(rune != null ? ItemCatalog.CreateRune(rune.rune, rune.tier) : null);
                    ui.Icon.color = rune != null ? ItemCatalog.RuneColor(rune.rune) : Color.white;
                    var carried = pendingRune ?? (dragItem != null && dragItem.IsRune && !dragFrom.HasValue ? dragItem : null);
                    bool target = carried != null && available && rune == null;
                    SetFrame(ui, target ? TargetColor : available ? FrameColor : new Color(.15f, .15f, .18f), target);
                }
            }

            statsText.text = StatsSummary();
            RefreshTouchActions();
            hintText.text = notice + (notice.Length > 0 ? "\n" : "") + (pendingRune != null
                ? "Choose a <color=#FFD84D>highlighted</color> item or weapon socket for " + pendingRune.name + ".   " + (MobileMode.Enabled ? "Tap an empty bag slot to cancel." : Chip("Right click") + " cancel")
                : dragItem != null
                ? "Drop on a <color=#FFD84D>highlighted</color> slot.  Drop on the bag to take it off."
                : MobileMode.Enabled
                ? "Tap an item to see it, then use the buttons under its details.  Hold and drag an item onto a slot to equip it."
                : Chip("Click") + " equip / pick rune    " + Chip("Hold & drag") + " move to a slot    " + Chip("Right click") + " salvage    " + Chip("Right click worn") + " remove rune");
        }

        private void ShowItem(SlotUi ui, Item item, bool highlighted)
        {
            ui.Icon.enabled = item != null;
            ui.Icon.sprite = ItemCatalog.Icon(item);
            ui.Icon.color = item != null && item.IsRune ? ItemCatalog.RuneColor(item.rune) : Color.white;
            var rarity = item != null ? (ItemCatalog.ItemColor(item)) : FrameColor;
            // Pozadina blago obojena retkošću, da se dobri predmeti vide na prvi pogled.
            ui.Background.color = item != null ? Color.Lerp(EmptyColor, rarity, .22f) : EmptyColor;
            SetFrame(ui, highlighted ? TargetColor : rarity, highlighted);
            if (ui.Sockets == null)
                return;
            for (int i = 0; i < ui.Sockets.Length; i++)
            {
                bool exists = item != null && !item.IsRune && i < item.socketCount;
                ui.Sockets[i].color = !exists ? Color.clear : i < item.runes.Count ? ItemCatalog.RuneColor(item.runes[i].rune) : new Color(.55f, .53f, .62f);
            }
        }

        private string StatsSummary()
        {
            var health = equipment.GetComponent<PlayerHealth>();
            var mana = equipment.GetComponent<PlayerMana>();
            var text = new StringBuilder();
            text.AppendLine($"<color=#FF8080>Health</color><pos=40%><b>{Mathf.CeilToInt(health.CurrentHealth)} / {Mathf.CeilToInt(health.MaxHealth)}</b>");
            text.AppendLine($"<color=#80B0FF>Mana</color><pos=40%><b>{Mathf.FloorToInt(mana.MaxMana)}</b>");
            text.AppendLine($"<color=#D0C8A0>Armor</color><pos=40%><b>{Mathf.RoundToInt(health.Armor)}</b>  <color=#9993B0>blocks {Mathf.RoundToInt(health.DamageReduction * 100)}%</color>");
            text.AppendLine($"<color=#FFB060>Critical</color><pos=40%><b>{Mathf.RoundToInt(equipment.CritChance * 100)}%</b>  <color=#9993B0>x{equipment.CritMultiplier:0.0#} damage</color>");
            text.AppendLine();
            foreach (StatType stat in Enum.GetValues(typeof(StatType)))
            {
                if (stat == StatType.Armor || stat == StatType.MaxHealth || stat == StatType.MaxMana || stat == StatType.CritChance || stat == StatType.CritDamage)
                    continue;
                float value = equipment.Get(stat);
                if (Mathf.Abs(value) > .01f)
                    text.AppendLine("<color=#B8F0B8>" + ItemCatalog.FormatStat(stat, value) + "</color>");
            }

            if (equipment.ActiveSets.Count > 0)
            {
                text.AppendLine();
                foreach (var set in equipment.ActiveSets)
                    text.AppendLine("<color=#" + ColorUtility.ToHtmlStringRGB(ItemCatalog.RuneColor(set)) + ">RUNE SET " + ItemCatalog.SetName(set) + "</color>: " + string.Join(", ", ItemCatalog.SetBonus(set).Select(bonus => ItemCatalog.FormatStat(bonus.stat, bonus.value))));
            }
            return text.ToString();
        }

        // ---------- Opis predmeta ----------

        private void ShowTooltip(Item item, bool compare = false)
        {
            if (tooltipText == null)
                return;
            if (item == null)
            {
                tooltipText.text = "<color=#9993B0>Hover an item to see its details.\n\nRunes: three runes of the same kind anywhere on your gear or weapons activate a rune set bonus.</color>";
                return;
            }

            var text = new StringBuilder();
            string color = ColorUtility.ToHtmlStringRGB(ItemCatalog.ItemColor(item));
            text.AppendLine($"<size=26><b><color=#{color}>{item.name}</color></b></size>");
            text.AppendLine(item.IsRune
                ? $"<color=#9993B0>Rune  ·  Tier {item.runeTier}  ·  set: {ItemCatalog.SetName(item.rune)}</color>"
                : $"<color=#9993B0>{item.rarity} {item.gear}  ·  Item level {item.itemLevel}</color>");
            foreach (var stat in item.stats)
                text.AppendLine(ItemCatalog.FormatStat(stat.stat, stat.value));
            if (!item.IsRune && item.socketCount > 0)
            {
                text.Append("Sockets: ");
                for (int i = 0; i < item.socketCount; i++)
                {
                    if (i < item.runes.Count)
                        text.Append($"<color=#{ColorUtility.ToHtmlStringRGB(ItemCatalog.RuneColor(item.runes[i].rune))}>[{item.runes[i].rune} {new string('I', item.runes[i].tier)}]</color> ");
                    else
                        text.Append("[empty] ");
                }
                text.AppendLine();
                foreach (var rune in item.runes)
                    text.AppendLine("  " + ItemCatalog.FormatStat(rune.Stat.stat, rune.Stat.value));
            }

            if (item.IsUnique)
                text.AppendLine("<color=#FF9E33><b>UNIQUE:</b> " + UniqueItems.Find(item.unique)?.Effect + "</color>");
            text.AppendLine($"<color=#FFD84D>Salvage: {item.GoldValue} gold</color>");
            if (compare && !item.IsRune)
                AppendComparison(text, item);
            tooltipText.text = text.ToString();
        }

        // Razlika u odnosu na trenutno opremljeni predmet iste vrste.
        private void AppendComparison(StringBuilder text, Item item)
        {
            var slot = item.gear == GearType.Ring
                ? (equipment.Equipped(EquipSlot.Ring1) == null ? EquipSlot.Ring1 : equipment.Equipped(EquipSlot.Ring2) == null ? EquipSlot.Ring2 : EquipSlot.Ring1)
                : Enum.GetValues(typeof(EquipSlot)).Cast<EquipSlot>().First(candidate => item.Fits(candidate));
            var current = equipment.Equipped(slot);
            text.AppendLine();
            text.AppendLine(current == null ? "<color=#9993B0>Nothing equipped in this slot.</color>" : "<color=#9993B0>Compared to " + current.name + ":</color>");
            var difference = new Dictionary<StatType, float>();
            foreach (var stat in item.stats)
                difference[stat.stat] = difference.TryGetValue(stat.stat, out float value) ? value + stat.value : stat.value;
            if (current != null)
                foreach (var stat in current.stats)
                    difference[stat.stat] = (difference.TryGetValue(stat.stat, out float value) ? value : 0f) - stat.value;
            foreach (var pair in difference.Where(pair => Mathf.Abs(pair.Value) > .01f))
                text.AppendLine((pair.Value > 0 ? "<color=#7CFF8A>" : "<color=#FF7070>") + ItemCatalog.FormatStat(pair.Key, pair.Value) + "</color>");
        }

        private void ShowWeaponSocket(bool over, WeaponType weapon, int socket)
        {
            if (!over)
            {
                ShowTooltip(null);
                return;
            }

            var runes = equipment.WeaponRunes(weapon);
            ShowTooltip(socket < runes.Count ? ItemCatalog.CreateRune(runes[socket].rune, runes[socket].tier) : null);
        }

        // ---------- Klikovi ----------

        private void BagClicked(int index, bool right)
        {
            if (MobileMode.Enabled && !right && !touchConfirm)
            {
                if (pendingRune != null && index >= equipment.Bag.Count)
                    pendingRune = null;
                else if (pendingRune == null)
                {
                    SelectForTouch(index < equipment.Bag.Count ? equipment.Bag[index] : null, null);
                    return;
                }
            }
            if (index >= equipment.Bag.Count)
            {
                if (right)
                    pendingRune = null;
                Refresh();
                return;
            }

            var item = equipment.Bag[index];
            if (right)
            {
                if (pendingRune != null)
                {
                    pendingRune = null;
                    Refresh();
                    return;
                }

                int gold = equipment.Salvage(item);
                notice = "<color=#FFD84D>Salvaged " + item.name + " for " + gold + " gold.</color>";
                Sfx.Play(SfxId.Pickup);
                ShowTooltip(null);
                return;
            }

            if (item.IsRune)
            {
                pendingRune = pendingRune == item ? null : item;
                Refresh();
                return;
            }

            equipment.Equip(item);
            notice = "Equipped " + item.name + ".";
            ShowTooltip(item);
            Refresh();
        }

        private void EquipSlotClicked(EquipSlot slot, bool right)
        {
            if (MobileMode.Enabled && !right && pendingRune == null && !touchConfirm)
            {
                SelectForTouch(equipment.Equipped(slot), slot);
                return;
            }
            var item = equipment.Equipped(slot);
            if (pendingRune != null)
            {
                if (item != null && equipment.InsertRune(pendingRune, slot))
                {
                    notice = pendingRune.name + " socketed into " + item.name + ".";
                    Sfx.Play(SfxId.Block, .6f);
                    pendingRune = null;
                }
                Refresh();
                return;
            }

            if (item == null)
                return;
            if (right)
            {
                if (item.runes.Count > 0 && equipment.RemoveRune(slot, item.runes.Count - 1))
                    notice = "Rune removed from " + item.name + ".";
            }
            else if (!equipment.Unequip(slot))
            {
                notice = "<color=#FF7070>Bag is full.</color>";
            }

            ShowTooltip(null);
            Refresh();
        }

        private void WeaponSocketClicked(WeaponType weapon, int socket)
        {
            if (pendingRune != null)
            {
                if (equipment.InsertRune(pendingRune, weapon))
                {
                    notice = pendingRune.name + " socketed into the " + weapon + ".";
                    Sfx.Play(SfxId.Block, .6f);
                    pendingRune = null;
                }
                Refresh();
                return;
            }

            if (socket < equipment.WeaponRunes(weapon).Count && equipment.RemoveRune(weapon, socket))
                notice = "Rune removed from the " + weapon + ".";
            Refresh();
        }
    }
}
