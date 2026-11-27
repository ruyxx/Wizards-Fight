// Stablo veština (taster K): tri škole sa po četiri nivoa i spojne veštine za kombinacije.
// Klik bira veštinu (opis dole), drugi klik ili LEARN je uči. Poeni se mogu vratiti za zlato.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AWizardsFight
{
    public sealed class SkillTreeMenu : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        private static SkillTreeMenu instance;
        private GameMenus menus;
        private PlayerProgression progression;
        private GameObject root;
        private TextMeshProUGUI pointsText, detailTitle, detailText, detailRequirement;
        private readonly TextMeshProUGUI[] schoolPoints = new TextMeshProUGUI[3];
        private Button learnButton, resetButton;
        private readonly Dictionary<SkillNode, (Image card, Image frame, TextMeshProUGUI rank)> views = new Dictionary<SkillNode, (Image, Image, TextMeshProUGUI)>();
        private SkillNode selected;

        private void Awake()
        {
            instance = this;
            menus = GetComponent<GameMenus>();
            progression = FindFirstObjectByType<PlayerProgression>();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
                IsOpen = false;
            }
        }

        // Poruka pri novom nivou.
        public static void Announce(PlayerProgression progression)
        {
            string key = MobileMode.Enabled ? "tap SKILLS" : "press " + Keybinds.KeyName(Keybinds.Get(GameAction.Skills));
            CombatFeedback.Popup("SKILL POINT!  " + key, progression.transform.position + Vector3.up * 1.1f, new Color(1f, .85f, .3f), 4f, 2.2f);
        }

        private void Update()
        {
            if (progression == null || menus == null)
                return;
            bool pressed = Keybinds.Pressed(GameAction.Skills) || TouchInput.Pressed(TouchInput.Button.Skills);
            if (IsOpen)
            {
                Time.timeScale = 0f;
                if (pressed || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1))
                    Close();
                return;
            }
            if (pressed && menus.State == MenuState.Playing && !InventoryMenu.IsOpen && !CampMenu.IsOpen && !MessagePanel.IsOpen)
                Open();
        }

        public void Open()
        {
            if (root == null)
                Build();
            root.transform.SetAsLastSibling();
            root.SetActive(true);
            IsOpen = true;
            Time.timeScale = 0f;
            Select(selected ?? SkillTree.Nodes[0]);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(learnButton.gameObject);
        }

        public void Close()
        {
            root.SetActive(false);
            IsOpen = false;
            Time.timeScale = menus.State == MenuState.Playing ? 1f : 0f;
        }

        // ---------- Izgled ----------

        private static Image Rounded(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var image = UiFactory.Panel(name, parent, position, size, color);
            image.sprite = RuntimeSprites.Rounded;
            image.type = Image.Type.Sliced;
            return image;
        }

        private void Build()
        {
            root = UiFactory.Fullscreen("Skill Tree", menus.transform, new Color(.01f, .01f, .03f, .97f)).gameObject;
            var title = UiFactory.Text("Title", root.transform, "SKILL TREE", 46, new Vector2(0, 490), new Vector2(900, 60), UiFactory.AccentColor);
            title.fontStyle = FontStyles.Bold;
            pointsText = UiFactory.Text("Points", root.transform, "", 24, new Vector2(0, 445), new Vector2(1200, 36), new Color(1f, .85f, .3f));

            for (int school = 0; school < 3; school++)
            {
                var (name, color) = SkillTree.Schools[school];
                float x = (school - 1) * 610f;
                Rounded("School", root.transform, new Vector2(x, 80), new Vector2(590, 680), new Color(color.r * .1f, color.g * .1f, color.b * .13f, .95f));
                var header = UiFactory.Text("Header", root.transform, name, 30, new Vector2(x, 390), new Vector2(560, 40), color);
                header.fontStyle = FontStyles.Bold;
                schoolPoints[school] = UiFactory.Text("Spent", root.transform, "", 17, new Vector2(x, 358), new Vector2(560, 26), new Color(.75f, .72f, .85f));
                for (int tier = 0; tier < 4; tier++)
                {
                    float y = 270 - tier * 150;
                    var label = UiFactory.Text("Tier", root.transform, tier == 3 ? "CAPSTONE  ·  " + SkillTree.TierRequirement[tier] + " pts" : "TIER " + (tier + 1) + (tier > 0 ? "  ·  " + SkillTree.TierRequirement[tier] + " pts" : ""), 17, new Vector2(x - 165, y + 64), new Vector2(240, 26), new Color(color.r, color.g, color.b, .8f));
                    label.alignment = TextAlignmentOptions.Left;
                    var row = System.Array.FindAll(SkillTree.Nodes, node => !node.IsFusion && node.School == school && node.Tier == tier);
                    for (int i = 0; i < row.Length; i++)
                        BuildNode(row[i], new Vector2(x + (i - (row.Length - 1) / 2f) * 190f, y), color);
                }
            }

            // Spojne veštine, između dve škole.
            UiFactory.Text("Fusion", root.transform, "COMBINATIONS  ·  " + SkillTree.FusionRequirement + " points in both schools", 18, new Vector2(0, -312), new Vector2(900, 28), new Color(1f, .85f, .3f));
            foreach (var node in SkillTree.Nodes)
            {
                if (!node.IsFusion)
                    continue;
                float x = (node.School + node.FusionWith - 2) * 305f;
                var mix = Color.Lerp(SkillTree.Schools[node.School].color, SkillTree.Schools[node.FusionWith].color, .5f);
                BuildNode(node, new Vector2(x, -375), mix);
            }

            // Opis izabrane veštine i dugmad.
            var detail = Rounded("Detail", root.transform, new Vector2(-150, -478), new Vector2(1240, 104), new Color(.08f, .07f, .12f, 1f));
            detailTitle = UiFactory.Text("Name", detail.transform, "", 22, new Vector2(-300, 26), new Vector2(560, 32), Color.white);
            detailTitle.alignment = TextAlignmentOptions.Left;
            detailTitle.fontStyle = FontStyles.Bold;
            detailText = UiFactory.Text("Text", detail.transform, "", 21, new Vector2(60, -16), new Vector2(1180, 54), new Color(.85f, .82f, .95f));
            detailText.alignment = TextAlignmentOptions.Left;
            detailRequirement = UiFactory.Text("Requirement", detail.transform, "", 17, new Vector2(320, 26), new Vector2(560, 30), new Color(1f, .6f, .5f));
            detailRequirement.alignment = TextAlignmentOptions.Right;
            learnButton = UiFactory.Button("Learn", root.transform, "LEARN", new Vector2(600, -455), new Vector2(240, 54), () => Learn(selected), 24);
            resetButton = UiFactory.Button("Reset", root.transform, "", new Vector2(790, -455), new Vector2(120, 54), Reset, 15);
            UiFactory.Button("Close", root.transform, "CLOSE", new Vector2(700, -510), new Vector2(340, 40), Close, 20);
            root.SetActive(false);
        }

        private void BuildNode(SkillNode node, Vector2 position, Color color)
        {
            var button = UiFactory.Button("Skill " + node.Id, root.transform, "", position, new Vector2(176, 100), () =>
            {
                // Drugi klik na izabranu veštinu je uči.
                if (selected == node)
                    Learn(node);
                else
                    Select(node);
            }, 16);
            var card = button.GetComponent<Image>();
            card.sprite = RuntimeSprites.Rounded;
            card.type = Image.Type.Sliced;
            Destroy(button.GetComponent<Outline>());
            var frame = UiFactory.Panel("Frame", button.transform, Vector2.zero, new Vector2(176, 100), color);
            frame.sprite = RuntimeSprites.RoundedFrame;
            frame.type = Image.Type.Sliced;
            frame.raycastTarget = false;
            var name = UiFactory.Text("Name", button.transform, node.Title, 20, new Vector2(0, 14), new Vector2(166, 44), Color.white);
            name.enableAutoSizing = true;
            name.fontSizeMin = 16;
            name.fontSizeMax = 20;
            name.fontStyle = FontStyles.Bold;
            var rank = UiFactory.Text("Rank", button.transform, "", 18, new Vector2(0, -28), new Vector2(166, 26), color);
            views[node] = (card, frame, rank);
        }

        // ---------- Radnje ----------

        private void Select(SkillNode node)
        {
            selected = node;
            Refresh();
        }

        private void Learn(SkillNode node)
        {
            if (node != null && progression.Learn(node))
            {
                Sfx.Play(SfxId.LevelUp, .7f);
                CombatFeedback.Burst(progression.transform.position, SkillTree.Schools[node.School].color, 10, 2.5f);
            }
            Refresh();
        }

        private void Reset()
        {
            var equipment = progression.GetComponent<PlayerEquipment>();
            if (progression.SpentPoints == 0 || equipment == null || !equipment.SpendGold(progression.RespecCost))
                return;
            progression.Respec();
            Sfx.Play(SfxId.Summon, .6f);
            Refresh();
        }

        private void Refresh()
        {
            pointsText.text = progression.PendingChoices > 0
                ? progression.PendingChoices + " SKILL POINT" + (progression.PendingChoices > 1 ? "S" : "") + " TO SPEND   ·   one point per level"
                : "No points left. Level up to earn more.   (" + progression.SpentPoints + " spent)";
            for (int school = 0; school < 3; school++)
                schoolPoints[school].text = progression.PointsIn(school) + " points in this school";
            foreach (var pair in views)
            {
                var node = pair.Key;
                var (card, frame, rank) = pair.Value;
                int ranks = progression.Rank(node.Id);
                string requirement = progression.Requirement(node);
                bool locked = requirement.StartsWith("Needs");
                bool can = requirement == "";
                rank.text = locked ? "LOCKED" : "RANK " + ranks + " / " + node.Max;
                var color = node.IsFusion ? Color.Lerp(SkillTree.Schools[node.School].color, SkillTree.Schools[node.FusionWith].color, .5f) : SkillTree.Schools[node.School].color;
                card.color = ranks > 0 ? new Color(color.r * .38f, color.g * .38f, color.b * .45f, 1f) : locked ? new Color(.06f, .06f, .08f, 1f) : new Color(.13f, .11f, .19f, 1f);
                frame.color = node == selected ? Color.white : can ? new Color(1f, .85f, .3f) : locked ? new Color(.22f, .22f, .26f) : color;
                frame.rectTransform.sizeDelta = node == selected ? new Vector2(184, 108) : new Vector2(176, 100);
            }

            if (selected != null)
            {
                var color = SkillTree.Schools[selected.School].color;
                detailTitle.text = "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + selected.Title + "</color>  <size=20><color=#9993B0>rank " + progression.Rank(selected.Id) + " / " + selected.Max + "</color></size>";
                detailText.text = selected.Description;
                string requirement = progression.Requirement(selected);
                detailRequirement.text = requirement == "" ? "<color=#7CFF8A>Ready to learn (click again)</color>" : requirement;
                learnButton.interactable = requirement == "";
            }
            resetButton.GetComponentInChildren<TextMeshProUGUI>().text = "RESET\n" + progression.RespecCost + " G";
            var equipment = progression.GetComponent<PlayerEquipment>();
            resetButton.interactable = progression.SpentPoints > 0 && equipment != null && equipment.Gold >= progression.RespecCost;
        }
    }
}
