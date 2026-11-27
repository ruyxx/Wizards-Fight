// Prozor za izgled lika u glavnom meniju: gotovi likovi i podešavanje šešira, boja, kože, kose i brade.
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AWizardsFight
{
    public sealed class CharacterMenu : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        private static CharacterMenu instance;
        private GameObject root;
        private Image preview;
        private TextMeshProUGUI nameText;
        private TextMeshProUGUI[] values;
        private CharacterLook look;

        // Jedno podešavanje: naziv, čitanje vrednosti kao teksta i pomeranje za korak levo ili desno.
        private (string label, Func<CharacterLook, string> show, Action<CharacterLook, int> step)[] options;

        public static void Open(Transform canvas)
        {
            if (instance == null)
                instance = canvas.gameObject.AddComponent<CharacterMenu>();
            instance.Show(canvas);
        }

        private void Awake()
        {
            options = new (string, Func<CharacterLook, string>, Action<CharacterLook, int>)[]
            {
                ("HEADWEAR", l => CharacterLook.HatStyles[l.hat], (l, d) => l.hat = CharacterLook.Wrap(l.hat + d, CharacterLook.HatStyles.Length)),
                ("HAT COLOR", l => CharacterLook.Cloth[l.hatColor].name, (l, d) => l.hatColor = CharacterLook.Wrap(l.hatColor + d, CharacterLook.Cloth.Length)),
                ("ROBE COLOR", l => CharacterLook.Cloth[l.robeColor].name, (l, d) => l.robeColor = CharacterLook.Wrap(l.robeColor + d, CharacterLook.Cloth.Length)),
                ("TRIM", l => CharacterLook.Trims[l.trim].name, (l, d) => l.trim = CharacterLook.Wrap(l.trim + d, CharacterLook.Trims.Length)),
                ("SKIN", l => CharacterLook.Skins[l.skin].name, (l, d) => l.skin = CharacterLook.Wrap(l.skin + d, CharacterLook.Skins.Length)),
                ("HAIR", l => CharacterLook.HairStyles[l.hair], (l, d) => l.hair = CharacterLook.Wrap(l.hair + d, CharacterLook.HairStyles.Length)),
                ("HAIR COLOR", l => CharacterLook.HairColors[l.hairColor].name, (l, d) => l.hairColor = CharacterLook.Wrap(l.hairColor + d, CharacterLook.HairColors.Length)),
                ("BEARD", l => CharacterLook.BeardStyles[l.beard], (l, d) => l.beard = CharacterLook.Wrap(l.beard + d, CharacterLook.BeardStyles.Length)),
                ("COMPANION", l => PetCompanion.Kinds[l.pet].name, (l, d) => l.pet = CharacterLook.Wrap(l.pet + d, PetCompanion.Kinds.Length))
            };
        }

        private void Show(Transform canvas)
        {
            look = CharacterLook.Current.Copy();
            if (root == null)
                Build(canvas);
            root.transform.SetAsLastSibling();
            root.SetActive(true);
            IsOpen = true;
            Refresh();
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(root.GetComponentInChildren<Button>().gameObject);
        }

        private void Update()
        {
            if (IsOpen && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1)))
                Close();
        }

        public void Close()
        {
            CharacterLook.Save(look);
            root.SetActive(false);
            IsOpen = false;
        }

        private void Build(Transform canvas)
        {
            root = UiFactory.Fullscreen("Character", canvas, new Color(.01f, .01f, .03f, .96f)).gameObject;
            var panel = UiFactory.Panel("Panel", root.transform, Vector2.zero, new Vector2(1320, 900), new Color(.09f, .08f, .14f, .98f));
            panel.sprite = RuntimeSprites.Rounded;
            panel.type = Image.Type.Sliced;
            UiFactory.Text("Title", root.transform, "YOUR WIZARD", 46, new Vector2(0, 395), new Vector2(1000, 60), UiFactory.AccentColor).fontStyle = FontStyles.Bold;

            // Levo: veliki prikaz lika na postolju.
            var stage = UiFactory.Panel("Stage", root.transform, new Vector2(-370, 120), new Vector2(380, 400), new Color(.14f, .12f, .22f, 1f));
            stage.sprite = RuntimeSprites.Rounded;
            stage.type = Image.Type.Sliced;
            var glow = UiFactory.Panel("Glow", stage.transform, new Vector2(0, -20), new Vector2(340, 340), new Color(.55f, .42f, 1f, .18f));
            glow.sprite = RuntimeSprites.Glow;
            preview = UiFactory.Panel("Preview", stage.transform, new Vector2(0, 10), new Vector2(300, 300), Color.white);
            preview.preserveAspect = true;
            nameText = UiFactory.Text("Name", stage.transform, "", 30, new Vector2(0, -165), new Vector2(360, 44), new Color(1f, .85f, .3f));

            // Desno: podešavanja sa strelicama.
            values = new TextMeshProUGUI[options.Length];
            for (int i = 0; i < options.Length; i++)
            {
                int index = i;
                float y = 290 - i * 54;
                var label = UiFactory.Text("Label", root.transform, options[i].label, 20, new Vector2(70, y), new Vector2(200, 44), new Color(.65f, .62f, .78f));
                label.alignment = TextAlignmentOptions.Left;
                UiFactory.Button("Prev", root.transform, "<", new Vector2(230, y), new Vector2(54, 48), () => Change(index, -1), 26);
                values[i] = UiFactory.Text("Value", root.transform, "", 22, new Vector2(390, y), new Vector2(250, 44), Color.white);
                UiFactory.Button("Next", root.transform, ">", new Vector2(550, y), new Vector2(54, 48), () => Change(index, 1), 26);
            }

            // Dole: gotovi likovi sa malom slikom.
            UiFactory.Text("Presets", root.transform, "READY-MADE WIZARDS", 20, new Vector2(0, -205), new Vector2(600, 30), new Color(.65f, .62f, .78f));
            for (int i = 0; i < CharacterLook.Presets.Length; i++)
            {
                var preset = CharacterLook.Presets[i];
                var button = UiFactory.Button("Preset", root.transform, "", new Vector2(-532 + i * 152, -295), new Vector2(140, 130), () => Pick(preset), 16);
                var icon = UiFactory.Panel("Icon", button.transform, new Vector2(0, 14), new Vector2(84, 84), Color.white);
                icon.sprite = preset.Sprite();
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                UiFactory.Text("Name", button.transform, preset.name, 15, new Vector2(0, -48), new Vector2(136, 26), Color.white);
            }

            UiFactory.Button("Random", root.transform, "RANDOM", new Vector2(-150, -400), new Vector2(240, 58), Randomize, 24);
            UiFactory.Button("Done", root.transform, "DONE", new Vector2(150, -400), new Vector2(240, 58), Close, 24);
        }

        private void Change(int option, int direction)
        {
            options[option].step(look, direction);
            look.name = "Custom";
            Refresh();
        }

        private void Pick(CharacterLook preset)
        {
            look = preset.Copy();
            Refresh();
        }

        private void Randomize()
        {
            foreach (var option in options)
                option.step(look, UnityEngine.Random.Range(0, 12));
            look.name = "Custom";
            Refresh();
        }

        private void Refresh()
        {
            preview.sprite = look.Sprite();
            nameText.text = look.name;
            for (int i = 0; i < options.Length; i++)
                values[i].text = options[i].show(look);
            CharacterLook.Save(look);
        }
    }
}
