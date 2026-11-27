// Donja traka HUD-a: tri oružja, napici, izmicanje i posebna čarolija, sa tasterima, retkošću i vremenom oporavka.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AWizardsFight
{
    public sealed class HotbarView
    {
        private const float SlotSize = 104f, Spacing = 12f, GroupGap = 34f;
        private static readonly Color SlotColor = new Color(.07f, .06f, .11f, .88f);
        private static readonly Color SelectedColor = new Color(.2f, .15f, .32f, .95f);
        private static readonly Color LockedColor = new Color(.35f, .35f, .42f, 1f);

        private sealed class Slot
        {
            public RectTransform Root;
            public Image Background, Icon, Cooldown;
            public Outline Border;
            public TextMeshProUGUI Key, Caption, Badge, Timer;
            public float BaseY;
        }

        // Mesta: pet oružja, napitak, izmicanje, posebna čarolija i četiri aktivne čarolije.
        private const int PotionSlot = WeaponTypes.Count, DashSlot = WeaponTypes.Count + 1, SpecialSlot = WeaponTypes.Count + 2, FirstSpellSlot = WeaponTypes.Count + 3;
        private readonly Slot[] slots = new Slot[WeaponTypes.Count + 3 + PlayerSpells.All.Length];
        private readonly PlayerSpells spells;
        private readonly TextMeshProUGUI title;
        private readonly PlayerWeapons weapons;
        private readonly PlayerPotions potions;
        private readonly PlayerCombat combat;
        private readonly PlayerController controller;
        private readonly Sprite potionIcon;

        public HotbarView(Transform hud, PlayerHealth player, Sprite potionIcon)
        {
            var visual = player.transform.Find("Visual");
            var playerSprite = visual != null ? visual.GetComponent<SpriteRenderer>()?.sprite : null;
            weapons = player.GetComponent<PlayerWeapons>();
            potions = player.GetComponent<PlayerPotions>();
            combat = player.GetComponent<PlayerCombat>();
            controller = player.GetComponent<PlayerController>();
            spells = player.GetComponent<PlayerSpells>();
            this.potionIcon = potionIcon;

            float width = SlotSize * slots.Length + Spacing * (slots.Length - 3) + GroupGap * 2;
            var bar = UiFactory.Rect("Hotbar", hud, new Vector2(0, 26), new Vector2(width, SlotSize + 60));
            bar.anchorMin = bar.anchorMax = new Vector2(.5f, 0f);
            bar.pivot = new Vector2(.5f, 0f);

            title = UiFactory.Text("Selected Weapon", bar, "", 26, Vector2.zero, new Vector2(width, 36), Color.white);
            var titleRect = title.rectTransform;
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(4, 0);
            title.alignment = TextAlignmentOptions.Left;
            title.fontStyle = FontStyles.Bold;

            float x = SlotSize / 2f;
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = CreateSlot(bar, new Vector2(x, SlotSize / 2f));
                x += SlotSize + (i == PotionSlot || i == SpecialSlot ? GroupGap : Spacing);
            }

            // Telefon: napitak, izmicanje i čarolija su velika dugmad desno, a oružja se biraju dodirom na traku.
            if (MobileMode.Enabled)
            {
                for (int i = PotionSlot; i < slots.Length; i++)
                    slots[i].Root.gameObject.SetActive(false);
                bar.anchoredPosition += new Vector2((SlotSize * 3 + Spacing + GroupGap) / 2f, 0);
                for (int i = 0; i < WeaponTypes.Count; i++)
                {
                    var type = (WeaponType)i;
                    slots[i].Background.raycastTarget = true;
                    slots[i].Background.gameObject.AddComponent<TouchButton>().Pressed = () => weapons?.TrySelect(type);
                }
            }

            slots[PotionSlot].Icon.sprite = potionIcon;
            slots[DashSlot].Caption.text = "DASH";
            // Ikona izmicanja je providna figura čarobnjaka, kao trag koji ostaje pri izmicanju.
            slots[DashSlot].Icon.sprite = playerSprite != null ? playerSprite : RuntimeSprites.Glow;
            slots[DashSlot].Icon.color = new Color(.7f, .55f, 1f, .8f);
            // Aktivne čarolije: sjaj u boji čarolije umesto ikone.
            for (int i = 0; i < PlayerSpells.All.Length; i++)
            {
                var slot = slots[FirstSpellSlot + i];
                slot.Icon.sprite = RuntimeSprites.Glow;
                slot.Caption.text = PlayerSpells.All[i].Name;
                if (MobileMode.Enabled)
                    slot.Root.gameObject.SetActive(false);
            }
        }

        private static Slot CreateSlot(Transform parent, Vector2 center)
        {
            var slot = new Slot();
            slot.Background = UiFactory.Panel("Slot", parent, center, new Vector2(SlotSize, SlotSize), SlotColor);
            slot.Root = slot.Background.rectTransform;
            slot.Root.anchorMin = slot.Root.anchorMax = Vector2.zero;
            slot.BaseY = center.y;
            slot.Background.raycastTarget = false;
            slot.Border = slot.Background.gameObject.AddComponent<Outline>();
            slot.Border.effectDistance = new Vector2(3, -3);

            slot.Icon = UiFactory.Panel("Icon", slot.Root, new Vector2(0, 8), new Vector2(64, 64), Color.white);
            slot.Icon.preserveAspect = true;
            slot.Icon.raycastTarget = false;

            // Tamni sektor preko ikone prikazuje preostalo vreme oporavka.
            slot.Cooldown = UiFactory.Panel("Cooldown", slot.Root, Vector2.zero, new Vector2(SlotSize - 6, SlotSize - 6), new Color(0f, 0f, 0f, .62f));
            slot.Cooldown.sprite = RuntimeSprites.Square;
            slot.Cooldown.type = Image.Type.Filled;
            slot.Cooldown.fillMethod = Image.FillMethod.Radial360;
            slot.Cooldown.fillOrigin = (int)Image.Origin360.Top;
            slot.Cooldown.fillClockwise = false;
            slot.Cooldown.fillAmount = 0f;
            slot.Cooldown.raycastTarget = false;

            // Taster u gornjem levom uglu; duža imena (SPACE) se smanjuju da stanu.
            slot.Key = UiFactory.Text("Key", slot.Root, "", 20, new Vector2(-SlotSize / 2f + 8, SlotSize / 2f - 5), new Vector2(SlotSize * .62f, 24), new Color(1f, .85f, .4f));
            slot.Key.rectTransform.pivot = new Vector2(0f, 1f);
            slot.Key.alignment = TextAlignmentOptions.Left;
            slot.Key.enableAutoSizing = true;
            slot.Key.fontSizeMin = 15;
            slot.Key.fontSizeMax = 20;
            slot.Key.fontStyle = FontStyles.Bold;
            slot.Caption = UiFactory.Text("Caption", slot.Root, "", 18, new Vector2(0, -SlotSize / 2f + 15), new Vector2(SlotSize - 6, 26), new Color(.85f, .83f, .95f));
            slot.Caption.textWrappingMode = TextWrappingModes.NoWrap;
            slot.Caption.enableAutoSizing = true;
            slot.Caption.fontSizeMin = 14;
            slot.Caption.fontSizeMax = 18;
            slot.Key.textWrappingMode = TextWrappingModes.NoWrap;
            slot.Badge = UiFactory.Text("Badge", slot.Root, "", 20, new Vector2(SlotSize / 2f - 6, SlotSize / 2f - 5), new Vector2(44, 24), Color.white);
            slot.Badge.rectTransform.pivot = new Vector2(1f, 1f);
            slot.Badge.alignment = TextAlignmentOptions.Right;
            slot.Badge.fontStyle = FontStyles.Bold;
            slot.Timer = UiFactory.Text("Timer", slot.Root, "", 26, new Vector2(0, 8), new Vector2(SlotSize, 40), Color.white);
            slot.Timer.fontStyle = FontStyles.Bold;
            return slot;
        }

        public void Refresh()
        {
            var selected = weapons != null ? weapons.Selected : null;
            if (selected != null)
                title.text = selected.WeaponName.ToUpperInvariant() + "   <size=20>" + WeaponRarityPresentation.Label(selected.Rarity).ToUpperInvariant() + "</size>";

            for (int i = 0; i < WeaponTypes.Count; i++)
            {
                var slot = slots[i];
                var weapon = weapons != null ? weapons.GetOwned((WeaponType)i) : null;
                bool active = weapon != null && weapon == selected;
                slot.Key.text = MobileMode.Enabled ? "" : Keybinds.KeyName(Keybinds.Get(WeaponKey(i)));
                slot.Caption.text = weapon != null ? weapon.WeaponName : "LOCKED";
                slot.Icon.sprite = weapon != null && weapon.Icon != null ? weapon.Icon : slot.Icon.sprite;
                slot.Icon.color = weapon != null ? Color.white : new Color(1f, 1f, 1f, .15f);
                slot.Icon.enabled = slot.Icon.sprite != null;
                slot.Border.effectColor = weapon != null ? WeaponRarityPresentation.ColorFor(weapon.Rarity) : LockedColor * new Color(1, 1, 1, .5f);
                slot.Border.effectDistance = active ? new Vector2(4, -4) : new Vector2(2, -2);
                slot.Background.color = active ? SelectedColor : SlotColor;
                // Izabrano oružje je malo podignuto.
                slot.Root.anchoredPosition = new Vector2(slot.Root.anchoredPosition.x, slot.BaseY + (active ? 10f : 0f));
                slot.Cooldown.fillAmount = 0f;
                slot.Timer.text = "";
            }

            var potion = slots[PotionSlot];
            int count = potions != null ? potions.Count : 0;
            potion.Key.text = Keybinds.KeyName(Keybinds.Get(GameAction.Potion));
            potion.Caption.text = "POTION";
            potion.Badge.text = "x" + count;
            potion.Icon.color = count > 0 ? Color.white : new Color(1f, 1f, 1f, .25f);
            potion.Icon.enabled = potionIcon != null;
            potion.Border.effectColor = new Color(1f, .4f, .45f, count > 0 ? .9f : .3f);

            if (controller != null)
                ShowCooldown(slots[DashSlot], controller.DashCooldownRemaining, controller.DashCooldownTotal, new Color(.7f, .55f, 1f, .85f));
            slots[DashSlot].Key.text = Keybinds.KeyName(Keybinds.Get(GameAction.Dash));

            var special = slots[SpecialSlot];
            special.Key.text = "RMB";
            if (combat != null && selected != null)
            {
                special.Caption.text = PlayerCombat.SpecialName(selected.Type);
                special.Icon.sprite = selected.Icon;
                special.Icon.enabled = selected.Icon != null;
                special.Badge.text = "";
                ShowCooldown(special, combat.SpecialCooldownRemaining, combat.SpecialCooldownTotal, Color.white);
                // Bez dovoljno mane ikona je bleda.
                var mana = combat.GetComponent<PlayerMana>();
                if (mana != null && mana.CurrentMana < PlayerCombat.SpecialManaCost(selected))
                    special.Icon.color = new Color(.5f, .6f, 1f, .45f);
            }

            for (int i = 0; i < PlayerSpells.All.Length && spells != null; i++)
            {
                var slot = slots[FirstSpellSlot + i];
                var info = PlayerSpells.All[i];
                slot.Key.text = Keybinds.KeyName(Keybinds.Get(PlayerSpells.KeyFor(i)));
                if (!spells.Unlocked(i))
                {
                    // Zaključana čarolija pokazuje nivo na kome se otključava.
                    slot.Cooldown.fillAmount = 0f;
                    slot.Timer.text = "LV " + info.UnlockLevel;
                    slot.Icon.color = new Color(info.Color.r, info.Color.g, info.Color.b, .12f);
                    slot.Border.effectColor = LockedColor * new Color(1, 1, 1, .5f);
                    continue;
                }
                ShowCooldown(slot, spells.Remaining(i), spells.Total(i), info.Color);
                if (spells.Remaining(i) <= 0f && !spells.EnoughMana(i))
                    slot.Icon.color = new Color(.5f, .6f, 1f, .45f);
            }
        }

        public static GameAction WeaponKey(int index) => index < 3 ? GameAction.Weapon1 + index : GameAction.Weapon4 + (index - 3);

        private static void ShowCooldown(Slot slot, float remaining, float total, Color ready)
        {
            bool cooling = remaining > 0f;
            slot.Cooldown.fillAmount = cooling ? Mathf.Clamp01(remaining / Mathf.Max(.01f, total)) : 0f;
            slot.Timer.text = cooling ? remaining.ToString(remaining < 1f ? "0.0" : "0") : "";
            slot.Icon.color = cooling ? new Color(ready.r, ready.g, ready.b, .5f) : ready;
            slot.Border.effectColor = cooling ? new Color(.3f, .3f, .38f, .8f) : ready;
            slot.Border.effectDistance = new Vector2(2, -2);
        }
    }
}
