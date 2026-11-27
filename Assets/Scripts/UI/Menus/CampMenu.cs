// Prozori u kampu: trgovac (kupovina opreme, runa i napitaka), oltar za spajanje runa, razgovor sa starcem i ulazak u portal.
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AWizardsFight
{
    [DisallowMultipleComponent]
    public sealed class CampMenu : MonoBehaviour
    {
        private const int PotionPrice = 40, RerollPrice = 60;
        public static bool IsOpen { get; private set; }

        private PlayerEquipment equipment;
        private PlayerProgression progression;
        private PlayerPotions potions;
        private GameMenus menus;
        private GameObject root;
        private Transform content;
        private TextMeshProUGUI title, goldText, footer;
        private bool forgeTab, storyTab;
        private Item forgeItem;
        private readonly List<Item> stock = new List<Item>();
        private CampStation station;

        private void Awake()
        {
            var player = FindFirstObjectByType<PlayerHealth>();
            equipment = player != null ? player.GetComponent<PlayerEquipment>() : null;
            progression = player != null ? player.GetComponent<PlayerProgression>() : null;
            potions = player != null ? player.GetComponent<PlayerPotions>() : null;
            menus = FindFirstObjectByType<GameMenus>();
        }

        private void Start() => RollStock();

        private void OnDisable() => IsOpen = false;

        // Ponuda prati najteži otključani nivo i nivo lika.
        private int ShopItemLevel => Mathf.Clamp(Mathf.Max(GameLevels.Unlocked * 30 - 20, (progression != null ? progression.Level : 1) * 4), 5, 100);

        private void RollStock()
        {
            stock.Clear();
            for (int i = 0; i < 8; i++)
                stock.Add(ItemCatalog.CreateGear(ShopItemLevel, 1.6f));
            for (int i = 0; i < 2; i++)
                stock.Add(ItemCatalog.CreateRandomRune(ShopItemLevel));
            // Bolji predmeti prvi, rune na kraju.
            stock.Sort((a, b) => a.IsRune != b.IsRune ? (a.IsRune ? 1 : -1) : b.rarity.CompareTo(a.rarity));
        }

        private static int Price(Item item) => item.GoldValue * 3 + 10;

        private void Update()
        {
            if (!IsOpen)
                return;
            Time.timeScale = 0f;
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1))
                Close();
        }

        public void Open(CampStation source)
        {
            if (menus == null || equipment == null || menus.State != MenuState.Playing)
                return;
            station = source;
            if (root == null)
                Build();
            root.transform.SetAsLastSibling();
            root.SetActive(true);
            IsOpen = true;
            Time.timeScale = 0f;
            Show();
            var firstButton = content.GetComponentInChildren<Button>();
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(firstButton != null ? firstButton.gameObject : root.GetComponentInChildren<Button>().gameObject);
        }

        public void Close()
        {
            if (root != null)
                root.SetActive(false);
            IsOpen = false;
            Time.timeScale = menus != null && menus.State == MenuState.Playing ? 1f : 0f;
        }

        private void Build()
        {
            root = UiFactory.Fullscreen("Camp Menu", menus.transform, new Color(0f, 0f, 0f, .9f)).gameObject;
            var panel = UiFactory.Panel("Panel", root.transform, new Vector2(0, 0), new Vector2(1660, 980), new Color(.07f, .06f, .11f, .98f));
            panel.sprite = RuntimeSprites.Rounded;
            panel.type = Image.Type.Sliced;
            title = UiFactory.Text("Title", root.transform, "", 46, new Vector2(0, 438), new Vector2(1200, 60), UiFactory.AccentColor);
            goldText = UiFactory.Text("Gold", root.transform, "", 28, new Vector2(0, 392), new Vector2(1200, 36), new Color(1f, .85f, .3f));
            content = UiFactory.Rect("Content", root.transform, new Vector2(0, -10), new Vector2(1580, 760));
            footer = UiFactory.Text("Footer", root.transform, "", 20, new Vector2(-120, -440), new Vector2(1200, 40), new Color(.75f, .72f, .85f));
            UiFactory.Button("Close", root.transform, "CLOSE", new Vector2(690, -440), new Vector2(200, 54), Close, 24);
        }

        private void Show()
        {
            foreach (Transform child in content)
                Destroy(child.gameObject);
            goldText.text = "GOLD: " + equipment.Gold;
            footer.text = "";
            switch (station.Kind)
            {
                case CampStationKind.Merchant:
                    ShowShop();
                    break;
                case CampStationKind.RuneAltar:
                    ShowAltar();
                    break;
                case CampStationKind.Elder:
                    ShowElder();
                    break;
                case CampStationKind.Rift:
                    ShowRift();
                    break;
                default:
                    ShowPortal();
                    break;
            }
        }

        // ---------- Trgovac ----------

        private void ShowShop()
        {
            title.text = "MERCHANT";
            footer.text = "Green stats beat what you wear now, red ones are worse. Sell old gear in your bag (salvage) for gold.";
            // Kartice: velika ikona, ime u boji retkosti, osobine i poređenje sa onim što lik nosi, cena dole.
            const float width = 296f, height = 340f, gap = 12f;
            for (int i = 0; i < stock.Count; i++)
            {
                var item = stock[i];
                int column = i % 5, row = i / 5;
                var position = new Vector2(-2 * (width + gap) + column * (width + gap), 190 - row * (height + gap));
                Color rarity = ItemCatalog.ItemColor(item);
                var card = UiFactory.Panel("Offer", content, position, new Vector2(width, height), new Color(.1f, .09f, .15f, 1f));
                card.sprite = RuntimeSprites.Rounded;
                card.type = Image.Type.Sliced;
                var frame = UiFactory.Panel("Frame", card.transform, Vector2.zero, new Vector2(width, height), new Color(rarity.r, rarity.g, rarity.b, .9f));
                frame.sprite = RuntimeSprites.RoundedFrame;
                frame.type = Image.Type.Sliced;
                frame.raycastTarget = false;
                // Gornji deo kartice obojen retkošću, sa ikonom u sredini.
                var top = UiFactory.Panel("Top", card.transform, new Vector2(0, 113), new Vector2(width - 10, 104), Color.Lerp(new Color(.1f, .09f, .15f), rarity, .28f));
                top.sprite = RuntimeSprites.Rounded;
                top.type = Image.Type.Sliced;
                var glow = UiFactory.Panel("Glow", top.transform, Vector2.zero, new Vector2(140, 140), new Color(rarity.r, rarity.g, rarity.b, .35f));
                glow.sprite = RuntimeSprites.Glow;
                glow.raycastTarget = false;
                var icon = UiFactory.Panel("Icon", top.transform, Vector2.zero, new Vector2(84, 84), item.IsRune ? rarity : Color.white);
                icon.sprite = ItemCatalog.Icon(item);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                string kind = item.IsRune ? "Rune  ·  tier " + item.runeTier : item.rarity + " " + item.gear;
                string sockets = !item.IsRune && item.socketCount > 0 ? "  ·  " + new string('\u25CF', item.socketCount) : "";
                var name = UiFactory.Text("Name", card.transform, $"<b>{item.name}</b>\n<size=19><color=#B3AEC8>{kind}{sockets}</color></size>", 22, new Vector2(0, 30), new Vector2(width - 16, 60), rarity);
                name.overflowMode = TextOverflowModes.Ellipsis;
                var stats = UiFactory.Text("Stats", card.transform, ShopLines(item), 21, new Vector2(0, -61), new Vector2(width - 20, 120), Color.white);
                stats.alignment = TextAlignmentOptions.Top;
                stats.overflowMode = TextOverflowModes.Ellipsis;
                stats.enableAutoSizing = true;
                stats.fontSizeMin = 17;
                stats.fontSizeMax = 21;
                int price = Price(item);
                var offer = item;
                var buy = UiFactory.Button("Buy", card.transform, price + " G", new Vector2(0, -height / 2f + 28), new Vector2(width - 30, 44), () => Buy(offer), 24);
                buy.interactable = equipment.Gold >= price && !equipment.BagFull;
                if (equipment.Gold < price)
                    buy.GetComponentInChildren<TextMeshProUGUI>().color = new Color(1f, .45f, .45f);
            }

            var potionRow = UiFactory.Button("Potion", content, "HEALING POTION  -  " + PotionPrice + " G   (you have " + (potions != null ? potions.Count : 0) + ")", new Vector2(-390, -368), new Vector2(740, 50), BuyPotion, 22);
            potionRow.interactable = equipment.Gold >= PotionPrice;
            var reroll = UiFactory.Button("Reroll", content, "NEW WARES  -  " + RerollPrice + " G", new Vector2(390, -368), new Vector2(740, 50), Reroll, 22);
            reroll.interactable = equipment.Gold >= RerollPrice;
        }

        // Osobine predmeta sa razlikom u odnosu na opremljeni predmet iste vrste (zeleno bolje, crveno lošije).
        private string ShopLines(Item item)
        {
            if (item.IsRune)
                return string.Join("\n", item.stats.Select(stat => ItemCatalog.FormatStat(stat.stat, stat.value))) + "\n<color=#9993B0>Socket it into gear or a weapon</color>";
            Item current = null;
            foreach (EquipSlot slot in System.Enum.GetValues(typeof(EquipSlot)))
                if (item.Fits(slot))
                {
                    var worn = equipment.Equipped(slot);
                    // Za prstenove poredi sa slabijim (ili praznim) mestom.
                    if (current == null || worn == null || worn.GoldValue < current.GoldValue)
                        current = worn;
                    if (worn == null)
                        break;
                }
            var difference = new Dictionary<StatType, float>();
            foreach (var stat in item.stats)
                difference[stat.stat] = difference.TryGetValue(stat.stat, out float value) ? value + stat.value : stat.value;
            if (current != null)
                foreach (var stat in current.stats)
                    difference[stat.stat] = (difference.TryGetValue(stat.stat, out float value) ? value : 0f) - stat.value;
            var lines = new List<string>();
            foreach (var stat in item.stats)
            {
                float change = difference[stat.stat];
                string line = ItemCatalog.FormatStat(stat.stat, stat.value);
                lines.Add(current == null || Mathf.Abs(change) < .01f ? line : (change > 0 ? "<color=#7CFF8A>" : "<color=#FF7070>") + line + "</color>");
            }
            // Osobine koje bi se izgubile zamenom.
            foreach (var pair in difference.Where(pair => pair.Value < -.01f && item.stats.All(stat => stat.stat != pair.Key)))
                lines.Add("<color=#FF7070>" + ItemCatalog.FormatStat(pair.Key, pair.Value) + "</color>");
            if (current == null)
                lines.Add("<color=#7CFF8A>empty slot - pure upgrade</color>");
            return string.Join("\n", lines);
        }

        private void Buy(Item item)
        {
            int price = Price(item);
            if (equipment.BagFull || !equipment.SpendGold(price))
                return;
            stock.Remove(item);
            equipment.AddItem(item);
            Sfx.Play(SfxId.Pickup);
            Show();
            footer.text = "Bought " + item.name + ".";
        }

        private void BuyPotion()
        {
            if (potions == null || !equipment.SpendGold(PotionPrice))
                return;
            potions.Add(1);
            Sfx.Play(SfxId.Pickup);
            Show();
        }

        private void Reroll()
        {
            if (!equipment.SpendGold(RerollPrice))
                return;
            RollStock();
            Show();
        }

        // ---------- Oltar runa ----------

        private void ShowAltar()
        {
            title.text = "RUNE ALTAR & FORGE";
            UiFactory.Button("Runes Tab", content, (forgeTab ? "" : "> ") + "MERGE RUNES", new Vector2(-170, 300), new Vector2(320, 44), () => { forgeTab = false; Show(); }, 22);
            UiFactory.Button("Forge Tab", content, (forgeTab ? "> " : "") + "FORGE GEAR", new Vector2(170, 300), new Vector2(320, 44), () => { forgeTab = true; Show(); }, 22);
            if (forgeTab)
            {
                ShowForge();
                return;
            }

            var groups = equipment.Bag.Where(item => item.IsRune && item.runeTier < 3)
                .GroupBy(item => (item.rune, item.runeTier))
                .Where(group => group.Count() >= 3)
                .ToList();
            var intro = UiFactory.Text("Intro", content, "Place three identical runes on the altar to forge one rune of the next tier.\nRunes must be in your bag (take them out of gear first).", 22, new Vector2(0, 250), new Vector2(1100, 70), new Color(.85f, .82f, .95f));
            if (groups.Count == 0)
            {
                UiFactory.Text("Empty", content, "You have no three identical runes of tier I or II.", 24, new Vector2(0, 120), new Vector2(1100, 40), new Color(.6f, .58f, .7f));
                return;
            }

            for (int i = 0; i < groups.Count && i < 8; i++)
            {
                var group = groups[i];
                int cost = 30 * group.Key.runeTier;
                var result = ItemCatalog.CreateRune(group.Key.rune, group.Key.runeTier + 1);
                string color = ColorUtility.ToHtmlStringRGB(ItemCatalog.RuneColor(group.Key.rune));
                var button = UiFactory.Button("Forge", content,
                    $"3x <color=#{color}>{group.Key.rune} Rune {new string('I', group.Key.runeTier)}</color>  ->  <color=#{color}>{result.name}</color>  ({ItemCatalog.FormatStat(result.stats[0].stat, result.stats[0].value)})   -   {cost} G",
                    new Vector2(0, 160 - i * 66), new Vector2(1000, 58), () => Forge(group.Key.rune, group.Key.runeTier), 21);
                button.interactable = equipment.Gold >= cost;
            }
        }

        private void Forge(RuneType rune, int tier)
        {
            int cost = 30 * tier;
            var parts = equipment.Bag.Where(item => item.IsRune && item.rune == rune && item.runeTier == tier).Take(3).ToList();
            if (parts.Count < 3 || !equipment.SpendGold(cost))
                return;
            // Rastavljanje bez zlata: rune se uklanjaju iz torbe, a nova se dodaje.
            foreach (var part in parts)
                equipment.RemoveFromBag(part);
            var forged = ItemCatalog.CreateRune(rune, tier + 1);
            equipment.AddItem(forged);
            Sfx.Play(SfxId.LevelUp, .7f);
            Show();
            footer.text = "Forged " + forged.name + ".";
        }

        // ---------- Kovačnica: nadogradnja, novi slot za runu i prekivanje osobine ----------

        private void ShowForge()
        {
            var gear = System.Enum.GetValues(typeof(EquipSlot)).Cast<EquipSlot>().Select(slot => equipment.Equipped(slot)).Where(item => item != null)
                .Concat(equipment.Bag.Where(item => !item.IsRune)).ToList();
            if (gear.Count == 0)
            {
                UiFactory.Text("Empty", content, "You have no gear to work on.", 24, new Vector2(0, 150), new Vector2(1100, 40), new Color(.6f, .58f, .7f));
                return;
            }
            if (forgeItem == null || !gear.Contains(forgeItem))
                forgeItem = gear[0];

            // Levo: spisak opreme (prvo ono što nosiš), desno: izabrani predmet i radnje.
            UiFactory.Text("List Title", content, "YOUR GEAR", 20, new Vector2(-400, 236), new Vector2(400, 30), new Color(.6f, .58f, .72f));
            for (int i = 0; i < gear.Count && i < 9; i++)
            {
                var item = gear[i];
                bool worn = equipment.IsEquipped(item);
                string color = ColorUtility.ToHtmlStringRGB(ItemCatalog.ItemColor(item));
                var row = UiFactory.Button("Gear", content, $"<color=#{color}>{item.name}</color>{(worn ? "  <size=17><color=#9993B0>worn</color></size>" : "")}", new Vector2(-400, 196 - i * 56), new Vector2(420, 50), () => { forgeItem = item; Show(); }, 18);
                if (item == forgeItem)
                    row.GetComponent<Outline>().effectColor = new Color(1f, .85f, .3f);
            }
            if (gear.Count > 9)
                UiFactory.Text("More", content, "+" + (gear.Count - 9) + " more in your bag", 16, new Vector2(-400, -290), new Vector2(420, 26), new Color(.6f, .58f, .72f));

            var selected = forgeItem;
            string title = ColorUtility.ToHtmlStringRGB(ItemCatalog.ItemColor(selected));
            var details = new StringBuilder();
            details.AppendLine($"<size=28><b><color=#{title}>{selected.name}</color></b></size>");
            details.AppendLine($"<color=#9993B0>{selected.rarity} {selected.gear}  ·  item level {selected.itemLevel}  ·  upgrade {selected.upgrade}/{ItemCatalog.MaxUpgrade}  ·  sockets {selected.socketCount}/{ItemCatalog.MaxSockets}</color>");
            details.AppendLine();
            for (int i = 0; i < selected.stats.Count; i++)
                details.AppendLine((i == 0 ? "<color=#FFD84D>base</color>  " : "") + ItemCatalog.FormatStat(selected.stats[i].stat, selected.stats[i].value));
            var text = UiFactory.Text("Details", content, details.ToString(), 21, new Vector2(230, 80), new Vector2(620, 300), Color.white);
            text.alignment = TextAlignmentOptions.TopLeft;

            int upgradeCost = ItemCatalog.UpgradeCost(selected);
            var upgrade = UiFactory.Button("Upgrade", content, selected.upgrade >= ItemCatalog.MaxUpgrade ? "FULLY UPGRADED" : $"UPGRADE TO +{selected.upgrade + 1}  (all stats +10%)  -  {upgradeCost} G", new Vector2(230, -80), new Vector2(620, 56), () => Work(selected, "upgrade"), 20);
            upgrade.interactable = selected.upgrade < ItemCatalog.MaxUpgrade && equipment.Gold >= upgradeCost;
            int socketCost = ItemCatalog.SocketCost(selected);
            var socket = UiFactory.Button("Socket", content, selected.socketCount >= ItemCatalog.MaxSockets ? "ALL SOCKETS OPEN" : $"ADD A RUNE SOCKET  -  {socketCost} G", new Vector2(230, -146), new Vector2(620, 56), () => Work(selected, "socket"), 20);
            socket.interactable = selected.socketCount < ItemCatalog.MaxSockets && equipment.Gold >= socketCost;
            int reforgeCost = ItemCatalog.ReforgeCost(selected);
            var reforge = UiFactory.Button("Reforge", content, selected.stats.Count < 2 ? "NOTHING TO REFORGE" : $"REFORGE THE LAST STAT  (new random stat)  -  {reforgeCost} G", new Vector2(230, -212), new Vector2(620, 56), () => Work(selected, "reforge"), 20);
            reforge.interactable = selected.stats.Count >= 2 && equipment.Gold >= reforgeCost;
            footer.text = "Upgrades keep their bonus when you reforge. Runes stay in their sockets.";
        }

        private void Work(Item item, string action)
        {
            int cost = action == "upgrade" ? ItemCatalog.UpgradeCost(item) : action == "socket" ? ItemCatalog.SocketCost(item) : ItemCatalog.ReforgeCost(item);
            if (equipment.Gold < cost)
                return;
            string result;
            if (action == "upgrade" && ItemCatalog.Upgrade(item))
                result = item.name + " is now stronger.";
            else if (action == "socket" && ItemCatalog.AddSocket(item))
                result = item.name + " has a new rune socket.";
            else if (action == "reforge" && ItemCatalog.Reforge(item, item.stats.Count - 1) is StatType stat)
                result = "New stat: " + ItemCatalog.FormatStat(stat, item.stats[item.stats.Count - 1].value) + ".";
            else
                return;
            equipment.SpendGold(cost);
            equipment.ItemChanged();
            Sfx.Play(SfxId.Block, .8f);
            Show();
            footer.text = result;
        }

        // ---------- Starac ----------

        private void ShowElder()
        {
            title.text = "THE ELDER";
            UiFactory.Button("Contracts Tab", content, (storyTab ? "" : "> ") + "CONTRACTS", new Vector2(-170, 300), new Vector2(320, 44), () => { storyTab = false; Show(); }, 22);
            UiFactory.Button("Story Tab", content, (storyTab ? "> " : "") + "THE ROAD AHEAD", new Vector2(170, 300), new Vector2(320, 44), () => { storyTab = true; Show(); }, 22);
            if (!storyTab)
            {
                ShowContracts();
                return;
            }
            int next = Mathf.Min(GameLevels.Unlocked, GameLevels.All.Length);
            var level = GameLevels.All[next - 1];
            var text = new StringBuilder();
            text.AppendLine("<size=30><color=#FFD84D>" + level.Title + "</color></size>");
            text.AppendLine();
            text.AppendLine(level.Story);
            text.AppendLine();
            text.AppendLine("<color=#B8F0B8>Goal: " + level.Goal + "</color>");
            text.AppendLine("<color=#9993B0>Recommended character level: " + level.RecommendedLevel + "</color>");
            text.AppendLine();
            text.AppendLine("<color=#9993B0>Tips: equip better gear with B, socket runes for set bonuses, and drink from the fountain to heal before you leave. Every portal you have opened stays open, so you can return to hunt for better loot.</color>");
            var story = UiFactory.Text("Story", content, text.ToString(), 24, new Vector2(0, 10), new Vector2(1100, 540), Color.white);
        }

        // Tri ugovora jedan pored drugog: naslov, zadatak, traka napretka, nagrada i dugme za preuzimanje.
        private void ShowContracts()
        {
            var bounties = Bounties.Active;
            for (int i = 0; i < bounties.Count; i++)
            {
                var bounty = bounties[i];
                var position = new Vector2((i - 1) * 390, 40);
                var card = UiFactory.Panel("Contract", content, position, new Vector2(370, 420), new Color(.1f, .09f, .15f, 1f));
                card.sprite = RuntimeSprites.Rounded;
                card.type = Image.Type.Sliced;
                var accent = bounty.Done ? new Color(1f, .85f, .3f) : new Color(.6f, .5f, .9f);
                var frame = UiFactory.Panel("Frame", card.transform, Vector2.zero, new Vector2(370, 420), accent);
                frame.sprite = RuntimeSprites.RoundedFrame;
                frame.type = Image.Type.Sliced;
                frame.raycastTarget = false;
                var seal = UiFactory.Panel("Seal", card.transform, new Vector2(0, 140), new Vector2(110, 110), new Color(accent.r, accent.g, accent.b, .45f));
                seal.sprite = RuntimeSprites.Glow;
                var core = UiFactory.Panel("Core", seal.transform, Vector2.zero, new Vector2(44, 44), accent);
                core.sprite = RuntimeSprites.Disc;
                UiFactory.Text("Mark", core.transform, bounty.Done ? "!" : (i + 1).ToString(), 26, Vector2.zero, new Vector2(44, 44), new Color(.08f, .06f, .12f)).fontStyle = FontStyles.Bold;
                UiFactory.Text("Title", card.transform, "<b>" + bounty.Title.ToUpperInvariant() + "</b>", 24, new Vector2(0, 75), new Vector2(340, 40), Color.white);
                UiFactory.Text("Task", card.transform, bounty.Description, 19, new Vector2(0, 25), new Vector2(330, 60), new Color(.8f, .78f, .9f));
                var back = UiFactory.Panel("Progress", card.transform, new Vector2(0, -40), new Vector2(300, 22), new Color(.05f, .04f, .08f, 1f));
                back.sprite = RuntimeSprites.Rounded;
                back.type = Image.Type.Sliced;
                float ratio = Mathf.Clamp01(bounty.progress / (float)bounty.goal);
                var fill = UiFactory.Panel("Fill", back.transform, new Vector2(-150 + 150 * ratio, 0), new Vector2(300 * ratio, 22), accent);
                fill.sprite = RuntimeSprites.Rounded;
                fill.type = Image.Type.Sliced;
                UiFactory.Text("Count", card.transform, Mathf.Min(bounty.progress, bounty.goal) + " / " + bounty.goal, 18, new Vector2(0, -70), new Vector2(300, 30), Color.white);
                UiFactory.Text("Reward", card.transform, "<color=#FFD84D>" + bounty.gold + " gold</color>  +  <color=#C77DFF>rare gear</color>", 18, new Vector2(0, -110), new Vector2(330, 30), Color.white);
                var claim = UiFactory.Button("Claim", card.transform, bounty.Done ? "CLAIM REWARD" : "IN PROGRESS", new Vector2(0, -170), new Vector2(300, 54), () =>
                {
                    var reward = Bounties.Claim(bounty, equipment);
                    if (reward == null)
                        return;
                    Sfx.Play(SfxId.LevelUp, .8f);
                    Show();
                    footer.text = "Reward: " + bounty.gold + " gold and " + reward.name + ". A new contract waits.";
                }, 22);
                claim.interactable = bounty.Done;
            }
            footer.text = "Contracts count everywhere you fight. Completed: " + Bounties.Completed + ".";
        }

        // ---------- Portal ----------

        private void ShowPortal()
        {
            var level = GameLevels.All[Mathf.Clamp(station.LevelIndex, 1, GameLevels.All.Length) - 1];
            title.text = level.Title;
            var text = UiFactory.Text("Story", content, level.Story + "\n\n<color=#B8F0B8>Goal: " + level.Goal + "</color>\n<color=#9993B0>Recommended character level: " + level.RecommendedLevel + "   (you are level " + (progression != null ? progression.Level : 1) + ")</color>", 24, new Vector2(0, 110), new Vector2(1100, 420), Color.white);
            TormentPicker(-110);
            UiFactory.Button("Enter", content, "ENTER " + level.Title, new Vector2(0, -230), new Vector2(520, 80), () => Travel(level), 30);
        }

        // Izbor Torment težine (kad je otključana): strelice levo i desno.
        private void TormentPicker(float y)
        {
            if (Torment.Unlocked == 0)
            {
                UiFactory.Text("Torment Locked", content, "<color=#9993B0>Defeat the Shadow King to unlock TORMENT difficulty.</color>", 18, new Vector2(0, y), new Vector2(1000, 30), Color.white);
                return;
            }
            UiFactory.Button("Torment Down", content, "<", new Vector2(-230, y), new Vector2(60, 50), () => { Torment.Level--; Show(); }, 26).interactable = Torment.Level > 0;
            UiFactory.Text("Torment", content, "<b>" + Torment.Name(Torment.Level) + "</b>", 26, new Vector2(0, y), new Vector2(380, 40), Torment.Level > 0 ? new Color(1f, .45f, .4f) : Color.white);
            UiFactory.Button("Torment Up", content, ">", new Vector2(230, y), new Vector2(60, 50), () => { Torment.Level++; Show(); }, 26).interactable = Torment.Level < Torment.Unlocked;
            UiFactory.Text("Torment Info", content, Torment.Describe(Torment.Level), 17, new Vector2(0, y - 42), new Vector2(1100, 50), new Color(.8f, .78f, .9f));
        }

        private void ShowRift()
        {
            title.text = "THE RIFT";
            var text = "A tear in the world, deeper than any dungeon. Every floor wears a different face: crypt, fire, ice or darkness.\n" +
                "Slay enough enemies and the RIFT GUARDIAN appears. Defeat it, take your reward, then go deeper or return.\n\n" +
                "<color=#B8F0B8>You keep your character, gear and everything you find. Dying in the rift is not the end: you wake up in the camp.</color>\n\n" +
                "<color=#FFD966>Your deepest floor: " + RiftDirector.BestFloor + "</color>";
            UiFactory.Text("Story", content, text, 22, new Vector2(0, 150), new Vector2(1100, 300), Color.white);
            TormentPicker(-60);
            UiFactory.Button("Enter", content, "ENTER THE RIFT", new Vector2(0, -210), new Vector2(520, 80), () =>
            {
                Close();
                var save = FindFirstObjectByType<SaveSystem>();
                if (save != null)
                    save.TravelTo(RiftDirector.SceneName);
            }, 30);
        }

        private void Travel(GameLevels.Level level)
        {
            Close();
            FindFirstObjectByType<SaveSystem>()?.TravelTo(level.Scene);
        }
    }
}
