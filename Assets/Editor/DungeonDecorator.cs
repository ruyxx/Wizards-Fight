// Alat za editor: crta piksel-art ukrase, raspoređuje ih po sobama i postavlja sisteme igre u Dungeon scenu.
// Pokretanje: meni Tools > Wizard's Fight > Decorate Dungeon (ili -executeMethod DungeonDecorator.Run).
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using AWizardsFight;

public static class DungeonDecorator
{
    private const string ArtFolder = "Assets/Art/Decorations";
    private const string ScenePath = "Assets/Scenes/Dungeon.unity";
    private const int EnvironmentLayer = 9;

    private static readonly Dictionary<char, Color32> Palette = new Dictionary<char, Color32>
    {
        { 'k', new Color32(20, 18, 30, 255) },
        { 'g', new Color32(140, 145, 165, 255) },
        { 'G', new Color32(200, 205, 225, 255) },
        { 'i', new Color32(90, 94, 108, 255) },
        { 'b', new Color32(115, 72, 38, 255) },
        { 'B', new Color32(158, 108, 56, 255) },
        { 'd', new Color32(76, 46, 26, 255) },
        { 'o', new Color32(255, 140, 38, 255) },
        { 'y', new Color32(255, 218, 76, 255) },
        { 'r', new Color32(216, 52, 38, 255) },
        { 'p', new Color32(107, 64, 166, 255) },
        { 'P', new Color32(158, 115, 230, 255) },
        { 'w', new Color32(230, 224, 204, 255) },
        { 'W', new Color32(178, 173, 153, 255) },
        { 'c', new Color32(178, 102, 64, 255) },
        { 'C', new Color32(217, 140, 89, 255) },
        { 'x', new Color32(14, 14, 24, 200) },
        { 'u', new Color32(56, 78, 128, 170) },
        { 'U', new Color32(105, 140, 200, 190) },
        { 'm', new Color32(52, 104, 58, 210) },
        { 'M', new Color32(86, 150, 80, 220) },
        { 'f', new Color32(250, 240, 200, 255) },
        { 'R', new Color32(120, 30, 40, 235) },
        { 'q', new Color32(170, 50, 58, 235) },
        { 'n', new Color32(70, 130, 245, 255) },
        { 'N', new Color32(170, 210, 255, 255) },
        { 's', new Color32(215, 215, 230, 150) }
    };

    private static readonly Dictionary<string, string[]> Drawings = new Dictionary<string, string[]>
    {
        { "Chain", new[] {
            ".....g....g.....", "....gGg..gGg....", "....g.g..g.g....", "....gGg..gGg....",
            ".....g....g.....", "....gGg..gGg....", "....g.g..g.g....", "....gGg..gGg....",
            ".....g....g.....", "....gGg..gGg....", "....g.g..g.g....", "....gGg..gGg....",
            ".....g....g.....", "....gGg..gGg....", "...gg.g..g.gg...", "...gGGg..gGGg..." } },
        { "Banner", new[] {
            "..kkkkkkkkkkkk..", "..kiiiiiiiiiik..", "...kppppppppk...", "...kpppppppPk...",
            "...kpppyypppk...", "...kppyyyyppk...", "...kpppyypppk...", "...kppppppppk...",
            "...kpPppppPpk...", "...kppppppppk...", "...kppppppppk...", "...kpppkkpppk...",
            "...kppk..kppk...", "...kpk....kpk...", "...kk......kk...", "................" } },
        { "Torch", new[] {
            "................", ".......y........", "......yoy.......", ".....yooy.......",
            ".....yory.......", "......oro.......", "......kbk.......", "......bBb.......",
            ".....kiiik......", "......bBb.......", "......bBb.......", "......bdb.......",
            "......kdk.......", "................", "................", "................" } },
        { "Barrel", new[] {
            "................", "................", "....kkkkkkkk....", "...kbBBbbBBbk...",
            "...kiiiiiiiik...", "..kbBbbbbbbBbk..", "..kbBbbbbbbBbk..", "..kiiiiiiiiiik..",
            "..kbBbbbbbbBbk..", "..kbBbbbbbbBbk..", "...kiiiiiiiik...", "...kdbbbbbbdk...",
            "....kkkkkkkk....", "................", "................", "................" } },
        { "Crate", new[] {
            "................", "................", "..kkkkkkkkkkkk..", "..kBBBBBBBBBBk..",
            "..kBdbbbbbbdBk..", "..kBbdbbbbdbBk..", "..kBbbdbbdbbBk..", "..kBbbbddbbbBk..",
            "..kBbbbddbbbBk..", "..kBbbdbbdbbBk..", "..kBbdbbbbdbBk..", "..kBdbbbbbbdBk..",
            "..kBBBBBBBBBBk..", "..kkkkkkkkkkkk..", "................", "................" } },
        { "Jar", new[] {
            "................", "................", "......kkkk......", ".....kCccCk.....",
            "......kcck......", ".....kcCcck.....", "....kcCcccck....", "...kcCcccccck...",
            "...kcCcccccck...", "...kcCrrrrcck...", "...kcccccccck...", "....kcccccck....",
            ".....kkkkkk.....", "................", "................", "................" } },
        { "Skull", new[] {
            "................", "................", "................", "................",
            "................", "......wwww......", ".....wwwwww.....", ".....wkwwkw.....",
            ".....wwwwww.....", "......wkkw......", "..w...wwww...w..", "...wW......Ww...",
            "....wWwwwwWw....", "...wW......Ww...", "..w..........w..", "................" } },
        { "Bones", new[] {
            "................", "................", "................", "................",
            "................", "................", "...w........w...", "....wW....Ww....",
            ".....wWwwWw.....", "......wWWw......", ".....wWwwWw.....", "....wW....Ww....",
            "...w........w...", "................", "................", "................" } },
        { "Cracks", new[] {
            "................", "................", "................", "......x.........",
            ".......x........", ".......xx.......", "........x.......", ".......xxx......",
            "......x...x.....", ".....x.....x....", "....x.......x...", "...........x....",
            "................", "................", "................", "................" } },
        { "Puddle", new[] {
            "................", "................", "................", "................",
            "................", "................", ".....uuuuuu.....", "...uuuUUuuuuu...",
            "..uuuuuuuUuuuu..", "..uuUuuuuuuuuu..", "...uuuuuuuuuu...", ".....uuuuuu.....",
            "................", "................", "................", "................" } },
        { "Candles", new[] {
            "................", "................", "................", "....y...........",
            "....o.....y.....", "...kfk....o.....", "...kfk...kfk....", "...kfk...kfk.y..",
            "...kfk...kfk.o..", "...kfk...kfkkfk.", "...kfk...kfkkfk.", "..kkkkk.kkkkkkk.",
            "................", "................", "................", "................" } },
        { "Moss", new[] {
            "................", "................", "................", "................",
            "................", ".....m..........", "....mMm...m.....", "...mmMmm.mMm....",
            "..mmMmmmmmmmm...", "..mMmmMmmMmmmm..", "...mmmmmMmmm....", ".....mm..mm.....",
            "................", "................", "................", "................" } },
        { "Rug", new[] {
            "kkkkkkkkkkkkkkkk", "kyyyyyyyyyyyyyyk", "kyRRRRRRRRRRRRyk", "kyRqqqqqqqqqqRyk",
            "kyRqRRRRRRRRqRyk", "kyRqRyyyyyyRqRyk", "kyRqRyRRRRyRqRyk", "kyRqRyRqqRyRqRyk",
            "kyRqRyRqqRyRqRyk", "kyRqRyRRRRyRqRyk", "kyRqRyyyyyyRqRyk", "kyRqRRRRRRRRqRyk",
            "kyRqqqqqqqqqqRyk", "kyRRRRRRRRRRRRyk", "kyyyyyyyyyyyyyyk", "kkkkkkkkkkkkkkkk" } },
        { "Cobweb", new[] {
            "ssssssssss......", "ss..s...s.......", "s.s..s..s.......", "s..s.s.s........",
            "ss..ss.s........", "s.s..sss........", "s..ss.s.........", "s...s.s.........",
            "ss.s.s..........", "s.ss............", "ss..............", "s...............",
            "................", "................", "................", "................" } }
    };

    [MenuItem("Tools/Wizard's Fight/Decorate Dungeon")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        int placed = DecorateOpenScene(false);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("DECORATOR placed " + placed + " decorations and set up gameplay systems.");
    }

    // Ukrašava trenutno otvorenu scenu; lava nivo dobija toplije boje.
    public static int DecorateOpenScene(bool ember, bool frost = false)
    {
        var sprites = CreateSprites();
        Tilemap floor = null, walls = null, lava = null;
        foreach (var map in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
        {
            if (map.name == "Floor")
                floor = map;
            else if (map.name == "Walls")
                walls = map;
            else if (map.name == "Lava" || map.name == "Ice")
                lava = map;
        }

        var old = GameObject.Find("Decorations");
        if (old != null)
            Object.DestroyImmediate(old);
        var root = new GameObject("Decorations").transform;
        frostTheme = frost;
        int placed = Decorate(root, floor, walls, lava, sprites, ember);
        SetupSystems();
        return placed;
    }

    private static Dictionary<string, Sprite> CreateSprites()
    {
        Directory.CreateDirectory(ArtFolder);
        var result = new Dictionary<string, Sprite>();
        foreach (var drawing in Drawings)
            result[drawing.Key] = WriteSprite(drawing.Key, Draw(drawing.Value));
        result["Glow"] = WriteSprite("Glow", DrawGlow());
        return result;
    }

    // Svaki znak crteža postaje kvadrat 2x2 piksela, pa sprite ima 32x32 kao ostala grafika igre.
    internal static Texture2D Draw(string[] rows)
    {
        var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        var clear = new Color32(0, 0, 0, 0);
        for (int row = 0; row < 16; row++)
        for (int column = 0; column < 16; column++)
        {
            char key = column < rows[row].Length ? rows[row][column] : '.';
            var color = Palette.TryGetValue(key, out var value) ? value : clear;
            for (int dy = 0; dy < 2; dy++)
            for (int dx = 0; dx < 2; dx++)
                texture.SetPixel(column * 2 + dx, 31 - (row * 2 + dy), color);
        }
        return texture;
    }

    private static Texture2D DrawGlow()
    {
        var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++)
        for (int x = 0; x < 32; x++)
        {
            float distance = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(16, 16)) / 16f;
            float alpha = Mathf.Clamp01(1f - distance);
            texture.SetPixel(x, y, new Color(1f, .62f, .28f, alpha * alpha));
        }
        return texture;
    }

    internal static Sprite WriteSprite(string name, Texture2D texture, string folder = ArtFolder, int pixelsPerUnit = 32)
    {
        Directory.CreateDirectory(folder);
        string path = folder + "/" + name + ".png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = name == "Glow" ? FilterMode.Bilinear : FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static int Decorate(Transform root, Tilemap floor, Tilemap walls, Tilemap lava, Dictionary<string, Sprite> sprites, bool ember)
    {
        var potion = AssetDatabase.LoadAssetAtPath<Pickup>("Assets/Prefabs/Combat/PotionPickup.prefab");
        var manaOrb = AssetDatabase.LoadAssetAtPath<Pickup>("Assets/Prefabs/Combat/ManaPickup.prefab");
        breakablePotion = potion;
        breakableMana = manaOrb;
        foreach (var jet in Object.FindObjectsByType<FireJet>(FindObjectsSortMode.None))
            keepClearExtra.Add((jet.transform.position, 1.2f));
        foreach (var station in Object.FindObjectsByType<CampStation>(FindObjectsSortMode.None))
            keepClearExtra.Add((station.transform.position, 2.4f));
        var random = new System.Random(20261013);
        var keepClear = new List<(Vector2 point, float radius)>();
        foreach (var gate in Object.FindObjectsByType<CombatGate>(FindObjectsSortMode.None))
            keepClear.Add((gate.GetComponent<Collider2D>().bounds.center, 2.6f));
        foreach (var door in Object.FindObjectsByType<Door>(FindObjectsSortMode.None))
            keepClear.Add((door.GetComponent<Collider2D>().bounds.center, 2.6f));
        foreach (var lever in Object.FindObjectsByType<Lever>(FindObjectsSortMode.None))
            keepClear.Add((lever.transform.position, 2f));
        foreach (var chest in Object.FindObjectsByType<LootChest>(FindObjectsSortMode.None))
            keepClear.Add((chest.transform.position, 1.8f));
        foreach (var target in Object.FindObjectsByType<PracticeTarget>(FindObjectsSortMode.None))
            keepClear.Add((target.transform.position, 1.8f));
        foreach (var pickup in Object.FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None))
            keepClear.Add((pickup.transform.position, 1.8f));
        foreach (var enemy in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            keepClear.Add((enemy.transform.position, 1.2f));
        foreach (var player in Object.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
            keepClear.Add((player.transform.position, 2f));
        foreach (var text in Object.FindObjectsByType<TMPro.TextMeshPro>(FindObjectsSortMode.None))
            keepClear.Add((text.transform.position, 1.2f));

        bool Wall(Vector2Int c) => walls.HasTile((Vector3Int)c);
        bool Floor(Vector2Int c) => floor.HasTile((Vector3Int)c) && !Wall(c) && (lava == null || !lava.HasTile((Vector3Int)c));
        bool Doorway(Vector2Int c) => Floor(c) && ((Wall(c + Vector2Int.left) && Wall(c + Vector2Int.right)) || (Wall(c + Vector2Int.up) && Wall(c + Vector2Int.down)));
        bool NearDoorway(Vector2Int c)
        {
            for (int dy = -2; dy <= 2; dy++)
            for (int dx = -2; dx <= 2; dx++)
                if (Doorway(c + new Vector2Int(dx, dy)))
                    return true;
            return false;
        }
        Vector2 Center(Vector2Int c) => new Vector2(c.x + .5f, c.y + .5f);
        keepClear.AddRange(keepClearExtra);
        keepClearExtra.Clear();
        bool Clear(Vector2 p)
        {
            foreach (var item in keepClear)
                if (Vector2.Distance(item.point, p) < item.radius)
                    return false;
            return true;
        }
        // Proverava da li iza ukrasa ima bar tri slobodna polja, da predmet ne bi zatvorio prolaz.
        bool Roomy(Vector2Int c, Vector2Int away)
        {
            for (int i = 1; i <= 3; i++)
                if (!Floor(c + away * i))
                    return false;
            return true;
        }

        int Run(Vector2Int c, Vector2Int step)
        {
            int length = 0;
            while (length < 30 && Floor(c + step * (length + 1)))
                length++;
            return length;
        }

        floor.CompressBounds();
        var area = floor.cellBounds;
        int count = 0;
        var wallDecor = new List<Vector2>();
        var occupied = new HashSet<Vector2Int>();
        var rugs = new List<Vector2>();
        for (int y = area.yMin; y < area.yMax; y++)
        for (int x = area.xMin; x < area.xMax; x++)
        {
            var cell = new Vector2Int(x, y);
            // Zidovi okrenuti ka sobi: baklje, zastave i lanci.
            if (Wall(cell) && Floor(cell + Vector2Int.down) && Wall(cell + Vector2Int.left) && Wall(cell + Vector2Int.right))
            {
                Vector2 p = Center(cell);
                bool spaced = true;
                foreach (var other in wallDecor)
                    if (Mathf.Abs(other.y - p.y) < .1f && Mathf.Abs(other.x - p.x) < 2.5f)
                        spaced = false;
                if (spaced && Clear(p) && !NearDoorway(cell + Vector2Int.down))
                {
                    double roll = random.NextDouble();
                    if (roll < .22)
                    {
                        Place(root, sprites["Torch"], "Torch", p + new Vector2(0, -.05f), 1);
                        var glow = Place(root, sprites["Glow"], "Torch Light", p + new Vector2(0, -.9f), -9);
                        glow.transform.localScale = new Vector3(4.5f, 3.5f, 1);
                        glow.color = new Color(1, 1, 1, .5f);
                        glow.gameObject.AddComponent<TorchFlicker>();
                        wallDecor.Add(p);
                        count += 2;
                    }
                    else if (roll < .36)
                    {
                        var banner = Place(root, sprites["Banner"], "Banner", p + new Vector2(0, -.1f), 1);
                        if (ember)
                            banner.color = new Color(1f, .55f, .45f);
                        else if (frostTheme)
                            banner.color = new Color(.55f, .75f, 1f);
                        wallDecor.Add(p);
                        count++;
                    }
                    else if (roll < .5)
                    {
                        Place(root, sprites["Chain"], "Chains", p + new Vector2(0, -.15f), 1);
                        wallDecor.Add(p);
                        count++;
                    }
                }
                continue;
            }

            if (!Floor(cell) || Doorway(cell) || NearDoorway(cell) || !Clear(Center(cell)))
                continue;
            bool up = Wall(cell + Vector2Int.up), down = Wall(cell + Vector2Int.down);
            bool left = Wall(cell + Vector2Int.left), right = Wall(cell + Vector2Int.right);
            // Uglovi: paučina i predmeti koji se ne mogu proći.
            if ((up || down) && (left || right))
            {
                var away = new Vector2Int(left ? 1 : -1, down ? 1 : -1);
                if (up && random.NextDouble() < .6)
                {
                    var web = Place(root, sprites["Cobweb"], "Cobweb", Center(cell), -7);
                    web.flipX = right;
                    count++;
                }

                if (Roomy(cell, new Vector2Int(away.x, 0)) && Roomy(cell, new Vector2Int(0, away.y)) && random.NextDouble() < .7)
                {
                    count += PlaceProp(root, sprites, random, cell, Center(cell));
                    occupied.Add(cell);
                }
                continue;
            }

            // Uz zid: povremeno ćup ili bure, samo gde soba ima dovoljno prostora.
            if (up || down || left || right)
            {
                var away = up ? Vector2Int.down : down ? Vector2Int.up : left ? Vector2Int.right : Vector2Int.left;
                bool neighbourBusy = occupied.Contains(cell + Vector2Int.left) || occupied.Contains(cell + Vector2Int.down);
                double edgeRoll = random.NextDouble();
                if (!neighbourBusy && Roomy(cell, away) && edgeRoll < .1)
                {
                    count += PlaceProp(root, sprites, random, cell, Center(cell));
                    occupied.Add(cell);
                }
                else if (edgeRoll < .16)
                {
                    string edgeName = up && random.NextDouble() < .5 ? "Candles" : "Moss";
                    var edge = Place(root, sprites[edgeName], edgeName, Center(cell), edgeName == "Candles" ? 4 : -8);
                    edge.flipX = random.NextDouble() < .5;
                    if (edgeName == "Candles")
                    {
                        var candleGlow = Place(root, sprites["Glow"], "Candle Light", Center(cell), -9);
                        candleGlow.transform.localScale = new Vector3(2.2f, 2.2f, 1);
                        candleGlow.color = new Color(1, 1, 1, .35f);
                        candleGlow.gameObject.AddComponent<TorchFlicker>();
                        count++;
                    }
                    count++;
                }
                continue;
            }

            // Tepih u sredini veće sobe, najviše jedan u okolini.
            bool open = true;
            for (int dy = -3; dy <= 3 && open; dy++)
            for (int dx = -3; dx <= 3 && open; dx++)
                open = Floor(cell + new Vector2Int(dx, dy));
            if (open && Mathf.Abs(Run(cell, Vector2Int.left) - Run(cell, Vector2Int.right)) <= 1 && Mathf.Abs(Run(cell, Vector2Int.up) - Run(cell, Vector2Int.down)) <= 1 && random.NextDouble() < .6 && !rugs.Exists(other => Vector2.Distance(other, Center(cell)) < 14f) && Clear(Center(cell) + Vector2.one))
            {
                var rug = Place(root, sprites["Rug"], "Rug", Center(cell), -8);
                rug.transform.localScale = new Vector3(3f, 2f, 1f);
                rugs.Add(Center(cell));
                count++;
                continue;
            }

            // Pod: pukotine, kosti i barice.
            if (random.NextDouble() < .08)
            {
                double roll = random.NextDouble();
                string name = roll < .35 ? "Cracks" : roll < .55 ? "Bones" : roll < .7 ? "Skull" : "Puddle";
                var decal = Place(root, sprites[name], name, Center(cell) + new Vector2((float)random.NextDouble() * .4f - .2f, (float)random.NextDouble() * .4f - .2f), -8);
                // U lava nivou barice postaju užareni ugljevlje, a pukotine svetle.
                if (frostTheme && (name == "Puddle" || name == "Cracks"))
                    decal.color = new Color(.75f, .9f, 1f, .85f);
                else if (ember && name == "Puddle")
                    decal.color = new Color(1f, .45f, .2f, .9f);
                else if (ember && name == "Cracks")
                    decal.color = new Color(1f, .5f, .2f, .8f);
                decal.flipX = random.NextDouble() < .5;
                if (name == "Cracks" || name == "Puddle")
                    decal.flipY = random.NextDouble() < .5;
                count++;
            }
        }
        return count;
    }

    private static int PlaceProp(Transform root, Dictionary<string, Sprite> sprites, System.Random random, Vector2Int cell, Vector2 position)
    {
        double roll = random.NextDouble();
        string name = roll < .35 ? "Barrel" : roll < .62 ? "Crate" : "Jar";
        var prop = Place(root, sprites[name], name, position, 4);
        prop.flipX = random.NextDouble() < .5;
        prop.gameObject.layer = EnvironmentLayer;
        var box = prop.gameObject.AddComponent<BoxCollider2D>();
        box.size = name == "Jar" ? new Vector2(.6f, .65f) : new Vector2(.8f, .7f);
        box.offset = new Vector2(0, -.05f);
        // Predmet se razbija pogotkom i ponekad ostavi napitak ili manu.
        var breakable = prop.gameObject.AddComponent<Breakable>();
        var serialized = new SerializedObject(breakable);
        serialized.FindProperty("potion").objectReferenceValue = breakablePotion;
        serialized.FindProperty("mana").objectReferenceValue = breakableMana;
        serialized.FindProperty("shardColor").colorValue = name == "Jar" ? new Color(.78f, .45f, .28f) : new Color(.55f, .36f, .2f);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return 1;
    }

    private static SpriteRenderer Place(Transform root, Sprite sprite, string name, Vector2 position, int order)
    {
        var item = new GameObject(name, typeof(SpriteRenderer));
        item.transform.SetParent(root, false);
        item.transform.position = position;
        var renderer = item.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        return renderer;
    }

    private static Pickup breakablePotion, breakableMana;
    private static bool frostTheme;
    private static readonly List<(Vector2 point, float radius)> keepClearExtra = new List<(Vector2 point, float radius)>();

    // Jedan objekat u sceni nosi čuvanje napretka, prikaz štete i mini-mapu.
    internal static GameObject SetupSystems()
    {
        var systems = GameObject.Find("Gameplay Systems") ?? new GameObject("Gameplay Systems");
        foreach (var type in new[] { typeof(SaveSystem), typeof(CombatFeedback), typeof(Minimap) })
            if (systems.GetComponent(type) == null)
                systems.AddComponent(type);
        GameObject hud = null;
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (canvas.name == "HUD")
                hud = canvas.gameObject;
        var minimap = new SerializedObject(systems.GetComponent<Minimap>());
        minimap.FindProperty("hudRoot").objectReferenceValue = hud != null ? hud.transform : null;
        minimap.ApplyModifiedPropertiesWithoutUndo();
        return systems;
    }
}
