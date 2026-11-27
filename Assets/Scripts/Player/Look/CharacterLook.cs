// Izgled čarobnjaka: šešir, boje odeće, koža, kosa i brada. Sprite se crta u kodu iz šablona,
// pa svaka kombinacija dobija svoj lik iste veličine kao originalni. Izbor se čuva u PlayerPrefs.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AWizardsFight
{
    [Serializable]
    public sealed class CharacterLook
    {
        public string name = "Arcanist";
        public int hat, hatColor, robeColor, skin, hair, hairColor, beard, trim;
        // Ljubimac pratilac (0 = bez ljubimca); stare sačuvane izgled dobijaju duha.
        public int pet = 1;

        public static readonly string[] HatStyles = { "Pointy Hat", "Hood", "Crown", "None" };
        public static readonly string[] BeardStyles = { "None", "Short", "Long" };
        public static readonly string[] HairStyles = { "Short", "Long" };

        public static readonly (string name, Color color)[] Cloth =
        {
            ("Violet", new Color32(111, 64, 182, 255)), ("Royal Blue", new Color32(48, 82, 190, 255)), ("Crimson", new Color32(170, 36, 48, 255)),
            ("Emerald", new Color32(40, 140, 76, 255)), ("Midnight", new Color32(36, 34, 62, 255)), ("Ivory", new Color32(222, 214, 196, 255)),
            ("Ash", new Color32(110, 110, 120, 255)), ("Teal", new Color32(30, 140, 150, 255)), ("Ember", new Color32(214, 100, 30, 255)),
            ("Rose", new Color32(200, 90, 150, 255))
        };

        public static readonly (string name, Color color)[] Skins =
        {
            ("Fair", new Color32(235, 213, 169, 255)), ("Tan", new Color32(204, 160, 112, 255)), ("Brown", new Color32(150, 100, 64, 255)),
            ("Dark", new Color32(96, 62, 42, 255)), ("Pale", new Color32(214, 224, 228, 255)), ("Undead", new Color32(150, 186, 140, 255))
        };

        public static readonly (string name, Color color)[] HairColors =
        {
            ("White", new Color32(236, 236, 240, 255)), ("Gray", new Color32(150, 150, 160, 255)), ("Brown", new Color32(110, 70, 40, 255)),
            ("Black", new Color32(40, 34, 40, 255)), ("Ginger", new Color32(200, 96, 40, 255)), ("Blonde", new Color32(230, 196, 100, 255))
        };

        public static readonly (string name, Color color)[] Trims =
        {
            ("Gold", new Color32(224, 166, 67, 255)), ("Silver", new Color32(190, 196, 210, 255)), ("Bronze", new Color32(176, 110, 60, 255)), ("Jade", new Color32(90, 200, 140, 255))
        };

        // Gotovi likovi koje igrač može da izabere jednim klikom.
        public static readonly CharacterLook[] Presets =
        {
            new CharacterLook { name = "Arcanist", hat = 0, hatColor = 0, robeColor = 0, skin = 0, hair = 0, hairColor = 2, beard = 0, trim = 0 },
            new CharacterLook { name = "Old Sage", hat = 0, hatColor = 1, robeColor = 1, skin = 0, hair = 1, hairColor = 0, beard = 2, trim = 1 },
            new CharacterLook { name = "Pyromancer", hat = 1, hatColor = 2, robeColor = 2, skin = 1, hair = 0, hairColor = 3, beard = 0, trim = 0, pet = 2 },
            new CharacterLook { name = "Frost Witch", hat = 0, hatColor = 7, robeColor = 5, skin = 4, hair = 1, hairColor = 0, beard = 0, trim = 1 },
            new CharacterLook { name = "Necromancer", hat = 1, hatColor = 4, robeColor = 4, skin = 5, hair = 0, hairColor = 3, beard = 0, trim = 3, pet = 3 },
            new CharacterLook { name = "Druid", hat = 3, hatColor = 3, robeColor = 3, skin = 2, hair = 1, hairColor = 2, beard = 1, trim = 2 },
            new CharacterLook { name = "Sun King", hat = 2, hatColor = 5, robeColor = 5, skin = 3, hair = 0, hairColor = 5, beard = 1, trim = 0 },
            new CharacterLook { name = "Ember Monk", hat = 3, hatColor = 8, robeColor = 8, skin = 1, hair = 0, hairColor = 4, beard = 0, trim = 2, pet = 2 }
        };

        private const string PrefKey = "CharacterLook";
        private static CharacterLook current;
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static CharacterLook Current
        {
            get
            {
                if (current == null)
                {
                    try
                    {
                        current = PlayerPrefs.HasKey(PrefKey) ? JsonUtility.FromJson<CharacterLook>(PlayerPrefs.GetString(PrefKey)) : null;
                    }
                    catch (ArgumentException)
                    {
                        current = null;
                    }
                    current = current ?? Presets[0].Copy();
                    current.Clamp();
                }
                return current;
            }
        }

        public static void Save(CharacterLook look)
        {
            current = look.Copy();
            current.Clamp();
            PlayerPrefs.SetString(PrefKey, JsonUtility.ToJson(current));
            PlayerPrefs.Save();
        }

        public CharacterLook Copy() => (CharacterLook)MemberwiseClone();

        private void Clamp()
        {
            hat = Wrap(hat, HatStyles.Length);
            hatColor = Wrap(hatColor, Cloth.Length);
            robeColor = Wrap(robeColor, Cloth.Length);
            skin = Wrap(skin, Skins.Length);
            hair = Wrap(hair, HairStyles.Length);
            hairColor = Wrap(hairColor, HairColors.Length);
            beard = Wrap(beard, BeardStyles.Length);
            trim = Wrap(trim, Trims.Length);
            pet = Wrap(pet, PetCompanion.Kinds.Length);
        }

        public static int Wrap(int value, int count) => ((value % count) + count) % count;

        private string Key => $"{hat}.{hatColor}.{robeColor}.{skin}.{hair}.{hairColor}.{beard}.{trim}";

        // Šablon 32x32 (gore je prvi red). Uloge: k obris, H šešir, h svetliji šešir, b traka, S koža, e oči,
        // R odora, r svetlija odora, T pojas, B kosa i brada.
        private static readonly string[] Body =
        {
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "..........kkSSSSSSSSSkk.........",
            "..........kkSeeSSSeeSkk.........",
            "..........kkSeeSSSeeSkk.........",
            "..........kkSSSSSSSSSkk.........",
            "........kkkkSSSSSSSSSkkkk.......",
            "........kkkkSSSSSSSSSkkkk.......",
            "........kkkkkkkkkkkkkkkkk.......",
            "........kkRrrrRRRRRRRRRkk.......",
            "........kkRrrrRRRRRRRRRkk.......",
            "........kkRrrrRRRRRRRRRkk.......",
            "........kkRrrrRRRRRRRRRkk.......",
            "........kkRrrrRRRRRRRRRkk.......",
            "........kkRrrrRRRRRRRRRkk.......",
            "........kkTTTTTTTTTTTTTkk.......",
            "........kkTTTTTTTTTTTTTkk.......",
            "........kkRrrrRRRRRRRRRkk.......",
            "........kkRrrrRRRRRRRRRkk.......",
            "........kkRrrrRRRRRRRRRkk.......",
            "........kkRrrrRRRRRRRRRkk.......",
            "........kkRrrrRRRRRRRRRkk.......",
            "........kkkkkkkkkkkkkkkkkk......",
            "........kkkkkkkkkkkkkkkkkk......",
            "........kkkkkkk....kkkkkkk......",
            "................................",
            "................................"
        };

        // Delovi koji se crtaju preko tela: (red, kolona, niz znakova; tačka ostavlja piksel kakav jeste).
        private static readonly (int row, int col, string pixels)[][] Hats =
        {
            new[]
            {
                (0, 15, "kkk"), (1, 14, "khHHk"), (2, 13, "khhHHHk"), (3, 12, "kHhhHHHHk"), (4, 11, "kHHhhHHHHHk"),
                (5, 6, "kkbbbbbbbbbbbbbbbbbbkk"), (6, 6, "kkkkkkkkkkkkkkkkkkkkkk")
            },
            new[]
            {
                (2, 13, "kkkkkkk"), (3, 12, "kHHHHHHHk"), (4, 11, "kHhhhHHHHHk"), (5, 10, "kHhHHHHHHHHHk"), (6, 10, "kHHHHHHHHHHHk"),
                (7, 11, "H.........H"), (8, 11, "H.........H"), (9, 11, "H.........H"), (10, 11, "H.........H"),
                (11, 10, "kHH.......HHk"), (12, 10, "kkH.......Hkk")
            },
            new[]
            {
                (2, 10, ".k....k....k."), (3, 10, "kbk..kbk..kbk"), (4, 10, "kbbkkbbbkkbbk"), (5, 10, "kbbbbbbbbbbbk"), (6, 10, "kkkkkkkkkkkkk")
            },
            new (int, int, string)[0]
        };

        private static readonly (int row, int col, string pixels)[][] HairShapes =
        {
            new[] { (4, 11, "kkkkkkkkkkk"), (5, 10, "kBBBBBBBBBBBk"), (6, 10, "kBBBBBBBBBBBk"), (7, 12, "B.......B") },
            new[] { (4, 11, "kkkkkkkkkkk"), (5, 10, "kBBBBBBBBBBBk"), (6, 10, "kBBBBBBBBBBBk"), (7, 11, "BB.......BB"), (8, 11, "B.........B"), (9, 11, "B.........B"), (10, 11, "B.........B"), (11, 11, "B.........B"), (12, 11, "B.........B") }
        };

        private static readonly (int row, int col, string pixels)[][] Beards =
        {
            new (int, int, string)[0],
            new[] { (10, 12, "SBBBBBBBS"), (11, 12, "BBBBBBBBB"), (12, 13, "BBBBBBB") },
            new[] { (10, 12, "SBBBBBBBS"), (11, 12, "BBBBBBBBB"), (12, 12, "BBBBBBBBB"), (13, 13, "BBBBBBB"), (14, 14, "BBBBB"), (15, 14, "BBBBB"), (16, 15, "BBB"), (17, 15, "BBB"), (18, 16, "B") }
        };

        public Sprite Sprite()
        {
            if (cache.TryGetValue(Key, out var cached) && cached != null)
                return cached;
            const int size = 32;
            var map = new char[size, size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                map[y, x] = Body[y][x];
            // Kosa se vidi samo bez šešira ili ispod kapuljače; brada je preko lica.
            if (hat == 3 || hat == 2)
                Stamp(map, HairShapes[hair]);
            Stamp(map, Hats[hat]);
            Stamp(map, Beards[beard]);

            var outline = new Color32(24, 20, 39, 255);
            var colors = new Dictionary<char, Color>
            {
                { 'k', outline }, { 'H', Cloth[hatColor].color }, { 'h', Lighter(Cloth[hatColor].color) }, { 'b', Trims[trim].color },
                { 'S', Skins[skin].color }, { 'e', outline }, { 'R', Cloth[robeColor].color }, { 'r', Lighter(Cloth[robeColor].color) },
                { 'T', Trims[trim].color }, { 'B', HairColors[hairColor].color }
            };
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                if (!colors.TryGetValue(map[y, x], out var color))
                {
                    pixels[(size - 1 - y) * size + x] = new Color32(0, 0, 0, 0);
                    continue;
                }
                // Senčenje: ivica ka obrisu gore/levo je svetlija, dole/desno tamnija, a odeća blago tamni nadole.
                if (map[y, x] != 'k' && map[y, x] != 'e')
                {
                    bool Edge(int dx, int dy) => y + dy < 0 || y + dy >= size || x + dx < 0 || x + dx >= size || map[y + dy, x + dx] == 'k' || map[y + dy, x + dx] == '.';
                    float light = (Edge(0, -1) ? .2f : 0f) + (Edge(-1, 0) ? .1f : 0f) - (Edge(0, 1) ? .22f : 0f) - (Edge(1, 0) ? .14f : 0f) + .06f - .12f * y / size;
                    color = new Color(color.r * (1f + light), color.g * (1f + light), color.b * (1f + light), 1f);
                }
                pixels[(size - 1 - y) * size + x] = color;
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            // Isti okvir i tačka oslonca kao originalni Wizard.png (24 piksela po jedinici).
            var sprite = UnityEngine.Sprite.Create(texture, new Rect(5, 1, 24, 31), new Vector2(.5f, .5f), 24f);
            sprite.name = "Wizard " + name;
            cache[Key] = sprite;
            return sprite;
        }

        private static void Stamp(char[,] map, (int row, int col, string pixels)[] parts)
        {
            foreach (var part in parts)
                for (int i = 0; i < part.pixels.Length; i++)
                    if (part.pixels[i] != '.' && part.col + i < 32)
                        map[part.row, part.col + i] = part.pixels[i];
        }

        private static Color Lighter(Color color) => Color.Lerp(color * 1.35f, Color.white, .12f);
    }
}
