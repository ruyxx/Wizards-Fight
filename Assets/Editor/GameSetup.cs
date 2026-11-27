// Alat za editor: pravi nove resurse (grafika, zvuk, prefabi), povezuje nove sisteme u scene i gradi lava nivo Ember Depths.
// Pokretanje: meni Tools > Wizard's Fight > Build Content Update (ili -executeMethod GameSetup.Run).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using AWizardsFight;
using Object = UnityEngine.Object;
using Random = System.Random;

public static class GameSetup
{
    private const int EnvironmentLayer = 9;
    private const string EnemyFolder = "Assets/Prefabs/Enemies";
    private const string CombatFolder = "Assets/Prefabs/Combat";
    private const string ArtFolder = "Assets/Art/Ember";
    private const string AudioFolder = "Assets/Audio/Original";
    private const string DungeonPath = "Assets/Scenes/Dungeon.unity";
    private const string EmberPath = "Assets/Scenes/EmberDepths.unity";
    private const string MenuPath = "Assets/Scenes/MainMenu.unity";
    private const string CampPath = "Assets/Scenes/Camp.unity";
    private const string FrozenPath = "Assets/Scenes/FrozenCrypt.unity";
    private const string EndlessPath = "Assets/Scenes/Endless.unity";
    private const string ShadowPath = "Assets/Scenes/ShadowTemple.unity";
    private const string RiftPath = "Assets/Scenes/Rift.unity";
    private static bool shadowTheme;

    private static readonly Dictionary<string, string[]> Drawings = new Dictionary<string, string[]>
    {
        { "Bomber", new[] {
            "................", "..........yy....", ".........yo.....", "........kk......",
            ".......kbk......", "......kkkk......", "....kkkkkkkk....", "...kiGiiiiiik...",
            "..kiGiiiiiiiik..", "..kiirriiirrik..", "..kiirriiirrik..", "..kiiiiiiiiiik..",
            "..kiiiikkkiiik..", "...kiiiiiiiik...", "....kkkkkkkk....", "................" } },
        { "Shieldbearer", new[] {
            "................", "......kkkk......", ".....kggggk.....", ".....kgkkgk.....",
            ".....kgrrgk.....", ".....kggggk.....", "....kkiiiikk....", "...kiigGgiiik...",
            "...kiigGgiiik...", "...kiiiiiiiik...", "....kiiiiiik....", "....kiikkiik....",
            "....kiik.kiik...", "....kkk..kkk....", "................", "................" } },
        { "Shield", new[] {
            "................", "................", ".....kkkkkk.....", "....kBBBBBBk....",
            "....kBbbbbBk....", "....kBbyybBk....", "....kByyyyBk....", "....kBbyybBk....",
            "....kBbbbbBk....", ".....kBbbBk.....", "......kBBk......", ".......kk.......",
            "................", "................", "................", "................" } },
        { "ManaOrb", new[] {
            "................", "................", "................", "................",
            "......kkkk......", ".....knnnnk.....", "....knNnnnnk....", "....knNnnnnk....",
            "....knnnnnnk....", "....knnnnnnk....", ".....knnnnk.....", "......kkkk......",
            "................", "................", "................", "................" } },
        { "MagmaGolem", new[] {
            "................", ".....kkkkkk.....", "....kddddddk....", "...kdyddddydk...",
            "...kdddoodddk...", ".kkkdddddddddkk.", "kddokddddddkoddk", "kdddkdoorodkdddk",
            "kddokdorrodkoddk", "kddkkdddoddkkddk", ".kk.kddddddk.kk.", "....kdddkdddk...",
            "....kddk.kddk...", "...kddk...kddk..", "...kkkk...kkkk..", "................" } },
        { "Merchant", new[] {
            "................", "......kkkk......", ".....kbbbbk.....", "....kbbbbbbk....",
            "....kbkCCkbk....", "....kbCkkCbk....", ".....kbCCbk.....", "...kkbbbbbbkk...",
            "..kBbbbyybbbBk..", "..kBbbbbbbbbBk..", "..kkbbbbbbbbkk..", "...kbbbbbbbbk...",
            "...kbbbkkbbbk...", "...kkkk..kkkk...", "................", "................" } },
        { "Elder", new[] {
            ".......kk.......", "......knnk......", ".....knnnnk.....", "....knnnnnnk....",
            "...kkkkkkkkkk...", "....kCCCCCCk....", "....kCkCCkCk....", "....kwwwwwwk....",
            "...knwwwwwwnk...", "...knnwwwwnnk...", "...knnnwwnnnk...", "...knnnnnnnnk...",
            "...knnnnnnnnk...", "...kkkkkkkkkk...", "................", "................" } },
        { "Altar", new[] {
            "................", "................", "................", "......kkkk......",
            ".....kPPPPk.....", ".....kPyyPk.....", ".....kPPPPk.....", "......kkkk......",
            "...kkkkkkkkkk...", "...kgggggggGk...", "....kgggggGk....", "....kgggggGk....",
            "...kgggggggGk...", "..kkkkkkkkkkkk..", "................", "................" } },
        { "Fountain", new[] {
            "................", "................", "................", "................",
            ".......nn.......", "......nNNn......", ".......nn.......", "...kkkkkkkkkk...",
            "..kgnNnnnnNngk..", "..kgnnnnnnnngk..", "..kgggggggggGk..", "...kgggggggGk...",
            "...kkkkkkkkkk...", "................", "................", "................" } },
        { "Portal", new[] {
            "................", ".....kkkkkk.....", "...kkggggggkk...", "..kggkkkkkkggk..",
            "..kgkppPPppkgk..", ".kgkpPPppPPpkgk.", ".kgkpPpPPpPpkgk.", ".kgkppPPPPppkgk.",
            ".kgkpPpPPpPpkgk.", ".kgkpPPppPPpkgk.", ".kgkppppppppkgk.", ".kgkpPPPPPPpkgk.",
            ".kgkppppppppkgk.", "kkgkkkkkkkkkkgkk", "kgggggggggggggGk", "................" } },
        { "Shrine", new[] {
            "................", ".......kk.......", "......kggk......", ".....kgGgGk.....",
            ".....kgPPgk.....", ".....kPyyPk.....", ".....kgPPgk.....", ".....kgggGk.....",
            ".....kgggGk.....", "....kggggggk....", "....kggggggk....", "...kkkkkkkkkk...",
            "..kggggggggggk..", "..kkkkkkkkkkkk..", "................", "................" } },
        { "Note", new[] {
            "................", "................", "................", "................",
            "....kkkkkkk.....", "....kwwwwwwk....", "....kwWWWwwk....", "....kwwwwwwk....",
            "....kwWWWWwk....", "....kwwwwwwk....", "....kwWWwwwk....", "....kkkkkkkk....",
            "................", "................", "................", "................" } },
        { "Crack", new[] {
            "................", ".......x........", "......xx........", "......x..x......",
            ".....xx.xx......", ".....x..x.......", "....xx.xx.......", "...x..x.xx......",
            "..x..xx...x.....", "....x..x...x....", "...x....x...x...", "..x......x......",
            ".x........x.....", "................", "................", "................" } },
        { "Lich", new[] {
            "....y..y..y.....", "....yyyyyyy.....", "....kkkkkkk.....", "...kwwwwwwwk....",
            "...kwkkwkkwk....", "...kwknwknwk....", "...kwwwkwwwk....", "....kwwwwwk.....",
            "...knnkkknnk....", "..knNnnnnnNnk...", "..knnnnnnnnnk...", "..knNnnnnnNnk...",
            "...knnnnnnnk....", "...knnnnnnnk....", "..kkkkkkkkkkk...", "................" } },
        { "FireVent", new[] {
            "................", "................", "................", "................",
            "................", "....kkkkkkkk....", "...kiiiiiiiik...", "...kikokokoik...",
            "...kiokokokik...", "...kiiiiiiiik...", "....kkkkkkkk....", "................",
            "................", "................", "................", "................" } },
        { "Flame", new[] {
            ".......y........", "......yy........", "......yoy.......", ".....yooy.......",
            ".....yoory......", "....yoorroy.....", "....yorrroy.....", "....yorrrooy....",
            "...yoorrrroy....", "...yorrrrrroy...", "...yorrrrrroy...", "...yoorrrrooy...",
            "....yoorrooy....", ".....yoooooy....", "......yyyyy.....", "................" } }
    };

    // Ikonice opreme idu u Resources/Items, odakle ih igra učitava po imenu.
    private static readonly Dictionary<string, string[]> ItemIcons = new Dictionary<string, string[]>
    {
        { "Helmet", new[] {
            "................", "................", ".....kkkkkk.....", "....kGGggggk....",
            "...kGgggggggk...", "...kGgggggggk...", "...kggggggggk...", "...kkkkkkkkkk...",
            "...kgkk..kkgk...", "...kgk....kgk...", "...kk......kk...", "................",
            "................", "................", "................", "................" } },
        { "Chest", new[] {
            "................", "................", "...kk......kk...", "..kggk....kggk..",
            "..kgggkkkkgggk..", "..kgGggggggGgk..", "...kggggggggk...", "...kgggyygggk...",
            "...kggggggggk...", "...kggggggggk...", "...kgGggggGgk...", "....kkkkkkkk....",
            "................", "................", "................", "................" } },
        { "Gloves", new[] {
            "................", "................", "................", ".....k.k.k......",
            "....kbkbkbk.....", "....kbbbbbkk....", "....kbbbbbbbk...", "....kbbbbbbk....",
            "....kbbbbbk.....", "....kBBBBBk.....", "....kkkkkkk.....", "................",
            "................", "................", "................", "................" } },
        { "Boots", new[] {
            "................", "................", "................", ".....kkkk.......",
            ".....kbbk.......", ".....kbbk.......", ".....kbbk.......", ".....kbbbkk.....",
            ".....kbbbbbk....", ".....kbbbbbbk...", ".....kdddddddk..", ".....kkkkkkkkk..",
            "................", "................", "................", "................" } },
        { "Amulet", new[] {
            "................", "................", "....k......k....", ".....k....k.....",
            "......k..k......", ".......kk.......", "......kyyk......", ".....kyrryk.....",
            ".....kyrryk.....", "......kyyk......", ".......kk.......", "................",
            "................", "................", "................", "................" } },
        { "Ring", new[] {
            "................", "................", "................", "................",
            "......kkkk......", ".....krrrrk.....", "......kkkk......", ".....kyyyyk.....",
            "....ky....yk....", "....ky....yk....", "....ky....yk....", ".....kyyyyk.....",
            "......kkkk......", "................", "................", "................" } },
        { "Rune", new[] {
            "................", "................", ".....kkkkkk.....", "....kwwwwwwk....",
            "...kwwwkkwwwk...", "...kwwkwwkwwk...", "...kwwwkkwwwk...", "...kwwwkwwwwk...",
            "...kwwwkwwwwk...", "...kwwkkkwwwk...", "...kWwwwwwwWk...", "....kWWWWWWk....",
            ".....kkkkkk.....", "................", "................", "................" } },
        { "Gold", new[] {
            "................", "................", "................", "................",
            "......kkkk......", "....kkyyyykk....", "...kyyoyyyyyk...", "...kyoyyyyyyk...",
            "...kyyyyyyoyk...", "...kyyyyyyyyk...", "....kkyyyykk....", "......kkkk......",
            "................", "................", "................", "................" } }
    };

    [MenuItem("Tools/Wizard's Fight/Build Content Update")]
    public static void Run()
    {
        foreach (var drawing in Drawings)
            DungeonDecorator.WriteSprite(drawing.Key, DungeonDecorator.Draw(drawing.Value), ArtFolder);
        foreach (var icon in ItemIcons)
            DungeonDecorator.WriteSprite(icon.Key, DungeonDecorator.Draw(icon.Value), "Assets/Resources/Items");
        CreateTiles();
        CreateAudio();
        CreatePrefabs(LoadSprites());
        CreateNewWeapons();
        GenerateMusic();
        AddProgressionToPlayer();

        // Otvaranje scene oslobađa nekorišćene resurse, pa se posle svakog otvaranja ponovo učitavaju sa diska.
        BuildEmberDepths();
        BuildFrozenCrypt();
        BuildCamp();
        SetupGameplayScene(DungeonPath, false);
        SetupGameplayScene(EmberPath, true);
        SetupGameplayScene(FrozenPath, false, false, true);
        SetupGameplayScene(CampPath, false, true);
        BuildEndless();
        BuildShadowContent();
        BuildRift();
        var menu = EditorSceneManager.OpenScene(MenuPath);
        AddSettingsExtras();
        EditorSceneManager.SaveScene(menu);

        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(scene => scene.path == EmberPath))
            scenes.Add(new EditorBuildSettingsScene(EmberPath, true));
        if (!scenes.Any(scene => scene.path == FrozenPath))
            scenes.Add(new EditorBuildSettingsScene(FrozenPath, true));
        if (!scenes.Any(scene => scene.path == CampPath))
            scenes.Insert(1, new EditorBuildSettingsScene(CampPath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("SETUP content update finished");
    }

    // ---------- Grafika i pločice ----------

    private static Dictionary<string, Sprite> LoadSprites()
    {
        var result = new Dictionary<string, Sprite>();
        foreach (var name in Drawings.Keys)
            result[name] = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/" + name + ".png");
        return result;
    }

    private static Dictionary<string, TileBase> LoadTiles() => new Dictionary<string, TileBase>
    {
        { "Floor", AssetDatabase.LoadAssetAtPath<TileBase>(ArtFolder + "/EmberFloor.asset") },
        { "Wall", AssetDatabase.LoadAssetAtPath<TileBase>(ArtFolder + "/EmberWall.asset") },
        { "Lava", AssetDatabase.LoadAssetAtPath<TileBase>(ArtFolder + "/Lava.asset") }
    };

    private static readonly string[] ClipNames = { "dash", "explosion", "break", "levelup", "pickup", "block", "slam", "summon", "footstep", "burn" };

    private static AudioClip[] LoadClips() => ClipNames.Select(name => AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + "/" + name + ".wav")).ToArray();

    private static Prefabs LoadPrefabs() => new Prefabs
    {
        Skeleton = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyFolder + "/Skeleton.prefab"),
        Slime = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyFolder + "/DungeonSlime.prefab"),
        Archer = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyFolder + "/DarkArcher.prefab"),
        Bomber = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyFolder + "/Bomber.prefab"),
        Shieldbearer = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyFolder + "/Shieldbearer.prefab"),
        Potion = AssetDatabase.LoadAssetAtPath<Pickup>(CombatFolder + "/PotionPickup.prefab"),
        Mana = AssetDatabase.LoadAssetAtPath<Pickup>(CombatFolder + "/ManaPickup.prefab")
    };

    private static Dictionary<string, TileBase> CreateTiles()
    {
        var result = new Dictionary<string, TileBase>();
        result["Floor"] = WriteTile("EmberFloor", DrawNoiseTile(1, new Color32(46, 34, 36, 255), new Color32(60, 44, 44, 255), new Color32(30, 22, 24, 255), true), Tile.ColliderType.None);
        result["Wall"] = WriteTile("EmberWall", DrawBricks(), Tile.ColliderType.Grid);
        result["Lava"] = WriteTile("Lava", DrawLava(), Tile.ColliderType.None);
        result["FrostFloor"] = WriteTile("FrostFloor", DrawNoiseTile(5, new Color32(40, 50, 70, 255), new Color32(70, 88, 115, 255), new Color32(26, 32, 48, 255), false), Tile.ColliderType.None);
        result["FrostWall"] = WriteTile("FrostWall", DrawFrostBricks(), Tile.ColliderType.Grid);
        result["Ice"] = WriteTile("Ice", DrawIce(), Tile.ColliderType.None);
        return result;
    }

    private static TileBase WriteTile(string name, Texture2D texture, Tile.ColliderType collider)
    {
        // Postojeće (ručno doterane) slike pločica se ne prepisuju.
        string png = ArtFolder + "/" + name + ".png";
        var sprite = File.Exists(png) ? AssetDatabase.LoadAssetAtPath<Sprite>(png) : DungeonDecorator.WriteSprite(name, texture, ArtFolder);
        string path = ArtFolder + "/" + name + ".asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }

        tile.sprite = sprite;
        tile.colliderType = collider;
        tile.color = Color.white;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    // Tamni bazalt sa mrljama i retkim užarenim pukotinama.
    private static Texture2D DrawNoiseTile(int seed, Color32 baseColor, Color32 light, Color32 dark, bool embers)
    {
        var random = new Random(seed);
        var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++)
        for (int x = 0; x < 32; x++)
        {
            double roll = random.NextDouble();
            Color32 color = roll < .12 ? light : roll < .24 ? dark : baseColor;
            if (x == 0 || y == 0)
                color = dark;
            texture.SetPixel(x, y, color);
        }

        if (embers)
            for (int i = 0; i < 6; i++)
            {
                int x = 4 + i * 4, y = 6 + (i * 7) % 18;
                texture.SetPixel(x, y, new Color32(200, 80, 30, 255));
                texture.SetPixel(x + 1, y, new Color32(150, 50, 20, 255));
            }
        return texture;
    }

    // Zid od tamnocrvene opeke.
    private static Texture2D DrawBricks()
    {
        var random = new Random(7);
        var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        var mortar = new Color32(22, 12, 12, 255);
        for (int y = 0; y < 32; y++)
        for (int x = 0; x < 32; x++)
        {
            int row = y / 8;
            int offset = row % 2 == 0 ? 0 : 8;
            bool line = y % 8 == 0 || (x + offset) % 16 == 0;
            byte shade = (byte)(78 + random.Next(-10, 11));
            Color32 brick = new Color32(shade, (byte)(shade * .5f), (byte)(shade * .45f), 255);
            if (y % 8 == 7)
                brick = new Color32((byte)(shade + 22), (byte)(shade * .62f), (byte)(shade * .5f), 255);
            texture.SetPixel(x, y, line ? mortar : brick);
        }
        return texture;
    }

    // Ledeni zid: plavosiva opeka sa injem na vrhu.
    private static Texture2D DrawFrostBricks()
    {
        var random = new Random(8);
        var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        var mortar = new Color32(18, 22, 34, 255);
        for (int y = 0; y < 32; y++)
        for (int x = 0; x < 32; x++)
        {
            int offset = (y / 8) % 2 == 0 ? 0 : 8;
            bool line = y % 8 == 0 || (x + offset) % 16 == 0;
            byte shade = (byte)(70 + random.Next(-8, 9));
            Color32 brick = new Color32((byte)(shade * .8f), (byte)(shade * .95f), (byte)(shade * 1.35f), 255);
            if (y % 8 == 7)
                brick = new Color32(170, 200, 230, 255);
            texture.SetPixel(x, y, line ? mortar : brick);
        }
        return texture;
    }

    // Led: providno plava površina sa belim odsjajima.
    private static Texture2D DrawIce()
    {
        var random = new Random(4);
        var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++)
        for (int x = 0; x < 32; x++)
        {
            float wave = Mathf.Sin((x + y) * .25f) * .5f + .5f;
            Color color = Color.Lerp(new Color(.55f, .75f, .95f, .82f), new Color(.75f, .9f, 1f, .85f), wave);
            if ((x + 2 * y) % 23 == 0 || random.NextDouble() < .02)
                color = new Color(1f, 1f, 1f, .95f);
            texture.SetPixel(x, y, color);
        }
        return texture;
    }

    // Lava: narandžasta osnova sa žutim mehurićima i tamnom korom.
    private static Texture2D DrawLava()
    {
        var random = new Random(3);
        var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++)
        for (int x = 0; x < 32; x++)
        {
            float wave = Mathf.Sin(x * .45f + Mathf.Sin(y * .3f) * 2f) * .5f + .5f;
            Color color = Color.Lerp(new Color(.85f, .28f, .05f), new Color(1f, .62f, .15f), wave);
            if (random.NextDouble() < .06)
                color = new Color(1f, .85f, .35f);
            else if (random.NextDouble() < .05)
                color = new Color(.45f, .12f, .05f);
            texture.SetPixel(x, y, color);
        }
        return texture;
    }

    // ---------- Zvuk ----------

    private static float footstepFilter;

    [MenuItem("Tools/Wizard's Fight/Regenerate Sounds")]
    public static void RegenerateAudio() => CreateAudio();

    // ---------- Muzika nivoa ----------

    // Proceduralne petlje za nivoe: lava (teški ritam u d-molu), led (spori zvonki arpeđo u a-molu) i kamp (mirno).
    public static void GenerateMusic()
    {
        const int rate = 22050;
        Directory.CreateDirectory("Assets/Resources/Music");
        WriteMusic("EmberDepths", rate, 96f, 32, new[] { 50, 50, 53, 48 }, new[] { 0, 3, 7, 10, 7, 3, 0, 5 }, 1f, .9f, 31);
        WriteMusic("FrozenCrypt", rate, 70f, 24, new[] { 45, 41, 43, 40 }, new[] { 12, 7, 3, 7, 15, 12, 7, 3 }, .45f, .25f, 47);
        WriteMusic("Camp", rate, 64f, 16, new[] { 48, 45, 41, 43 }, new[] { 7, 4, 0, 4, 12, 7, 4, 0 }, .25f, 0f, 5);
        // Glavni meni: tajanstveno i sporo, u e-molu.
        WriteMusic("MainMenu", rate, 58f, 16, new[] { 40, 36, 38, 35 }, new[] { 12, 19, 15, 24, 19, 15, 12, 7 }, .35f, 0f, 9);
        // Bosovi: brži, teži ritam i napetiji arpeđo.
        WriteMusic("EmberDepthsBoss", rate, 138f, 32, new[] { 38, 38, 41, 37 }, new[] { 0, 12, 3, 12, 6, 12, 3, 12 }, 1f, 1.1f, 77);
        WriteMusic("FrozenCryptBoss", rate, 112f, 32, new[] { 45, 46, 45, 41 }, new[] { 12, 15, 19, 24, 23, 19, 15, 11 }, .8f, .8f, 91);
        // Endless: uporan ritam koji ne staje, u c-molu.
        // Senoviti hram: spor i mračan, u h-molu; boss sa teškim ritmom.
        WriteMusic("ShadowTemple", rate, 62f, 24, new[] { 47, 43, 45, 42 }, new[] { 12, 15, 19, 18, 15, 12, 10, 7 }, .5f, .3f, 71);
        WriteMusic("ShadowTempleBoss", rate, 132f, 32, new[] { 35, 35, 38, 34 }, new[] { 0, 12, 1, 12, 7, 13, 6, 12 }, 1f, 1.2f, 83);
        // Rift: brz, mračan i nemiran, u f-molu.
        WriteMusic("Rift", rate, 140f, 32, new[] { 41, 44, 40, 39 }, new[] { 0, 12, 3, 15, 7, 12, 3, 10 }, .9f, 1.1f, 97);
        WriteMusic("Endless", rate, 124f, 32, new[] { 36, 39, 34, 31 }, new[] { 0, 7, 12, 15, 12, 7, 3, 7 }, .9f, 1f, 63);
        AssetDatabase.Refresh();
        foreach (var name in new[] { "EmberDepths", "FrozenCrypt", "Camp", "MainMenu", "EmberDepthsBoss", "FrozenCryptBoss", "Endless", "ShadowTemple", "ShadowTempleBoss", "Rift" })
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath("Assets/Resources/Music/" + name + ".wav");
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = .5f;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }
        Debug.Log("SETUP music generated");
    }

    private static float Note(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

    // Četiri takta basa ponavljaju se kroz zadati broj taktova; arpeđo svira osmine, a bubanj daje ritam.
    private static void WriteMusic(string name, int rate, float bpm, int bars, int[] roots, int[] arpeggio, float arpVolume, float drums, int seed)
    {
        var random = new Random(seed);
        float beat = 60f / bpm;
        int count = (int)(bars * 4 * beat * rate);
        var samples = new float[count];
        float lowpass = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            int beatIndex = (int)(t / beat);
            float inBeat = t - beatIndex * beat;
            int bar = beatIndex / 4;
            int root = roots[bar % roots.Length];
            // Bas: meki trougaoni talas sa sporim gašenjem po dobi.
            float bassPhase = t * Note(root - 12);
            float bass = (Mathf.Abs((bassPhase % 1f) * 4f - 2f) - 1f) * .32f * Mathf.Exp(-inBeat * 1.4f);
            // Pad: dva blago raštimovana sinusa za atmosferu.
            float pad = (Mathf.Sin(2 * Mathf.PI * Note(root + 12) * t) + Mathf.Sin(2 * Mathf.PI * Note(root + 19) * 1.003f * t)) * .05f;
            // Arpeđo na osmine.
            float eighth = beat / 2f;
            int step = (int)(t / eighth);
            float inStep = t - step * eighth;
            float arp = Mathf.Sin(2 * Mathf.PI * Note(root + 12 + arpeggio[step % arpeggio.Length]) * t) * .13f * arpVolume * Mathf.Exp(-inStep * 6f);
            // Bubanj: duboki udarac na 1 i 3, šuškava doboš na 2 i 4.
            float drum = 0f;
            if (drums > 0f)
            {
                lowpass += ((float)(random.NextDouble() * 2 - 1) - lowpass) * .35f;
                if (beatIndex % 2 == 0)
                    drum = Mathf.Sin(2 * Mathf.PI * (55f + 90f * Mathf.Exp(-inBeat * 30f)) * inBeat) * .45f * Mathf.Exp(-inBeat * 9f);
                else
                    drum = lowpass * .25f * Mathf.Exp(-inBeat * 18f);
                drum *= drums;
            }
            // Mekan ulaz i izlaz da se petlja ne čuje kao klik.
            float edge = Mathf.Clamp01(t * 20f) * Mathf.Clamp01((count - i) / (float)rate * 20f);
            samples[i] = Mathf.Clamp((bass + pad + arp + drum) * edge, -1f, 1f);
        }
        WriteWav("Assets/Resources/Music/" + name + ".wav", samples, rate);
    }

    private static AudioClip[] CreateAudio()
    {
        footstepFilter = 0f;
        const int rate = 44100;
        var random = new Random(11);
        float Noise() => (float)(random.NextDouble() * 2 - 1);
        var defs = new (string name, float seconds, Func<float, float, float> sample)[]
        {
            ("dash", .22f, (t, d) => Noise() * Mathf.Sin(Mathf.PI * t / d) * .5f),
            ("explosion", .7f, (t, d) => (Noise() * .8f + Mathf.Sin(2 * Mathf.PI * 55 * t) * .5f) * Mathf.Exp(-t * 6f)),
            ("break", .18f, (t, d) => Noise() * Mathf.Exp(-t * 22f) * (Mathf.Sin(2 * Mathf.PI * 30 * t) > 0 ? 1f : .4f)),
            ("levelup", .48f, (t, d) => Mathf.Sin(2 * Mathf.PI * new[] { 523f, 659f, 784f, 1047f }[Mathf.Min(3, (int)(t / .12f))] * t) * .4f * (1f - t / d * .5f)),
            ("pickup", .16f, (t, d) => Mathf.Sin(2 * Mathf.PI * (t < .07f ? 880f : 1320f) * t) * .4f * (1f - t / d)),
            ("block", .3f, (t, d) => (Mathf.Sin(2 * Mathf.PI * 900 * t) + Mathf.Sin(2 * Mathf.PI * 1350 * t) * .6f + Mathf.Sin(2 * Mathf.PI * 2100 * t) * .3f) * .25f * Mathf.Exp(-t * 12f)),
            ("slam", .4f, (t, d) => (Mathf.Sin(2 * Mathf.PI * (90f - 60f * t / d) * t) * .7f + Noise() * .3f) * Mathf.Exp(-t * 8f)),
            ("summon", .55f, (t, d) => Mathf.Sin(2 * Mathf.PI * (200f + 600f * t / d) * t) * .35f * (.6f + .4f * Mathf.Sin(2 * Mathf.PI * 18 * t)) * Mathf.Sin(Mathf.PI * t / d)),
            // Mekan, tih korak: niskopropusno filtriran šum i kratak dubok ton.
            ("footstep", .09f, (t, d) => { footstepFilter += (Noise() - footstepFilter) * .06f; return (footstepFilter * .9f + Mathf.Sin(2 * Mathf.PI * 95f * t) * .12f) * Mathf.Exp(-t * 45f); }),
            ("burn", .25f, (t, d) => (Noise() - Noise() * .5f) * .35f * Mathf.Sin(Mathf.PI * t / d))
        };

        var clips = new AudioClip[defs.Length];
        for (int i = 0; i < defs.Length; i++)
        {
            var def = defs[i];
            int count = (int)(def.seconds * rate);
            var samples = new float[count];
            for (int s = 0; s < count; s++)
                samples[s] = Mathf.Clamp(def.sample(s / (float)rate, def.seconds), -1f, 1f);
            string path = AudioFolder + "/" + def.name + ".wav";
            WriteWav(path, samples, rate);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        return clips;
    }

    private static void WriteWav(string path, float[] samples, int rate)
    {
        using (var stream = new FileStream(path, FileMode.Create))
        using (var writer = new BinaryWriter(stream))
        {
            int bytes = samples.Length * 2;
            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + bytes);
            writer.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(rate);
            writer.Write(rate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(bytes);
            foreach (float sample in samples)
                writer.Write((short)(sample * 32000f));
        }
    }

    // ---------- Senoviti hram (četvrti nivo) ----------

    private static readonly string[] ShadowKingDrawing =
    {
        "...y..y..y......", "...yyyyyyy......", "...kkkkkkk......", "..kxxxxxxxk.....",
        "..kxrxxxrxk.....", "..kxxxxxxxk.....", "...kxxxxxk......", "..kpPpppPpk.....",
        ".kppPpppPppk....", ".kppppppppppk...", "kpPpppppppPpk...", "kppppPpPppppk...",
        ".kpppppppppk....", ".kppppppppppk...", "kkkkkkkkkkkkkk..", "................"
    };

    // Pločice, boss, nivo, portal u kampu i muzika za Senoviti hram.
    public static void BuildShadowContent()
    {
        foreach (var name in new[] { "ShadowFloor", "ShadowWall", "Void" })
        {
            string png = ArtFolder + "/" + name + ".png";
            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(png));
            DungeonDecorator.WriteSprite(name, texture, ArtFolder);
            WriteTile(name, null, name == "ShadowWall" ? Tile.ColliderType.Grid : Tile.ColliderType.None);
        }
        // Varijante pločica se učitavaju u igri; moraju biti oštre i bez kompresije.
        foreach (var file in Directory.GetFiles("Assets/Resources/Tiles", "*.png"))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(file.Replace('\\', '/'));
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        var king = DungeonDecorator.WriteSprite("ShadowKing", DungeonDecorator.Draw(ShadowKingDrawing), ArtFolder);
        MakeShadowKing(king);
        BuildGeneratedLevel(new LevelSpec
        {
            Path = ShadowPath, Prefix = "Shadow", BossPrefab = "ShadowKing", BossName = "Shadow King",
            VictoryMessage = "The Shadow King falls.\nLight returns to the temple.",
            Floor = "ShadowFloor", Wall = "ShadowWall", Hazard = "Void", Frost = false,
            Background = new Color(.02f, .015f, .04f), Seed = 6161, LeverRoom = 6, ChestRooms = new[] { 1, 3, 5 },
            Rooms = new List<Room>
            {
                new Room { X = 0, Y = 0, Width = 15, Height = 13, Level = 1, Enemies = 0, Title = "1 / 8 - TEMPLE STEPS\nCONTINUE NORTH" },
                new Room { X = 0, Y = 20, Width = 15, Height = 13, Level = 85, Enemies = 5, Title = "2 / 8 - HALL OF ECHOES\nCONTINUE EAST" },
                new Room { X = 22, Y = 20, Width = 15, Height = 13, Level = 88, Enemies = 6, Title = "3 / 8 - BLIND CLOISTER\nCONTINUE EAST" },
                new Room { X = 44, Y = 20, Width = 15, Height = 13, Level = 91, Enemies = 6, Title = "4 / 8 - NAVE OF NIGHT\nCONTINUE NORTH" },
                new Room { X = 44, Y = 40, Width = 15, Height = 13, Level = 94, Enemies = 7, Title = "5 / 8 - STARLESS CHOIR\nCONTINUE WEST" },
                new Room { X = 22, Y = 40, Width = 15, Height = 13, Level = 97, Enemies = 7, Title = "6 / 8 - ALTAR OF VOID\nCONTINUE WEST" },
                new Room { X = 0, Y = 40, Width = 15, Height = 13, Level = 99, Enemies = 7, Title = "7 / 8 - LEVER OF NIGHT" },
                new Room { X = 0, Y = 60, Width = 17, Height = 16, Level = 100, Enemies = 0, Title = "8 / 8 - SHADOW KING" }
            }
        });
        SetupGameplayScene(ShadowPath, true, false, false, true);

        // Kamp dobija četvrti portal, iznad postojećih.
        var camp = EditorSceneManager.OpenScene(CampPath);
        var portals = Object.FindObjectsByType<CampStation>(FindObjectsSortMode.None).Where(station => station.Kind == CampStationKind.Portal).ToList();
        if (!portals.Any(portal => portal.LevelIndex == 4))
        {
            var source = portals.First(portal => portal.LevelIndex == 3);
            var copy = Object.Instantiate(source.gameObject, source.transform.parent);
            copy.name = "Portal 4";
            copy.transform.position = source.transform.position + new Vector3(0f, 3.6f, 0f);
            var data = new SerializedObject(copy.GetComponent<CampStation>());
            data.FindProperty("levelIndex").intValue = 4;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(camp);
            EditorSceneManager.SaveScene(camp);
        }

        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(entry => entry.path == ShadowPath))
            scenes.Add(new EditorBuildSettingsScene(ShadowPath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("SETUP shadow temple built");
    }

    // Rift: Endless arena, ali sa čuvanjem lika (igra se svojim likom iz priče) i spratovima umesto talasa.
    public static void BuildRift()
    {
        AssetDatabase.DeleteAsset(RiftPath);
        AssetDatabase.CopyAsset(EndlessPath, RiftPath);
        var scene = EditorSceneManager.OpenScene(RiftPath);
        var systems = GameObject.Find("Gameplay Systems");
        var endless = systems.GetComponent<EndlessDirector>();
        var endlessData = new SerializedObject(endless);
        var prefabs = endlessData.FindProperty("enemyPrefabs");
        var arenaMin = endlessData.FindProperty("arenaMin").vector2Value;
        var arenaMax = endlessData.FindProperty("arenaMax").vector2Value;
        var director = Ensure<RiftDirector>(systems);
        var data = new SerializedObject(director);
        var list = data.FindProperty("enemyPrefabs");
        list.arraySize = prefabs.arraySize;
        for (int i = 0; i < prefabs.arraySize; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = prefabs.GetArrayElementAtIndex(i).objectReferenceValue;
        string[] floors = { "Assets/Art/Placeholders/Floor.asset", ArtFolder + "/EmberFloor.asset", ArtFolder + "/FrostFloor.asset", ArtFolder + "/ShadowFloor.asset" };
        string[] walls = { "Assets/Art/Placeholders/Wall.asset", ArtFolder + "/EmberWall.asset", ArtFolder + "/FrostWall.asset", ArtFolder + "/ShadowWall.asset" };
        var floorList = data.FindProperty("floorTiles");
        var wallList = data.FindProperty("wallTiles");
        floorList.arraySize = wallList.arraySize = floors.Length;
        for (int i = 0; i < floors.Length; i++)
        {
            floorList.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<TileBase>(floors[i]);
            wallList.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<TileBase>(walls[i]);
        }
        data.FindProperty("arenaMin").vector2Value = arenaMin;
        data.FindProperty("arenaMax").vector2Value = arenaMax;
        data.ApplyModifiedPropertiesWithoutUndo();
        Object.DestroyImmediate(endless);
        Ensure<SaveSystem>(systems);
        Camera.main.backgroundColor = new Color(.07f, .02f, .03f);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // Kamp dobija crveni portal Rifta, levo dole, blizu mesta gde lik ulazi.
        var camp = EditorSceneManager.OpenScene(CampPath);
        var stations = Object.FindObjectsByType<CampStation>(FindObjectsSortMode.None);
        if (!stations.Any(station => station.Kind == CampStationKind.Rift))
        {
            var source = stations.First(station => station.Kind == CampStationKind.Portal && station.LevelIndex == 1);
            var copy = Object.Instantiate(source.gameObject, source.transform.parent);
            copy.name = "Rift Portal";
            copy.transform.position = new Vector3(2.6f, 9.4f, 0f);
            var stationData = new SerializedObject(copy.GetComponent<CampStation>());
            stationData.FindProperty("kind").enumValueIndex = (int)CampStationKind.Rift;
            stationData.ApplyModifiedPropertiesWithoutUndo();
            foreach (var renderer in copy.GetComponentsInChildren<SpriteRenderer>())
                renderer.color = new Color(1f, .55f, .55f);
            EditorSceneManager.MarkSceneDirty(camp);
            EditorSceneManager.SaveScene(camp);
        }

        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(entry => entry.path == RiftPath))
            scenes.Add(new EditorBuildSettingsScene(RiftPath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("SETUP rift built");
    }

    private static void MakeShadowKing(Sprite sprite)
    {
        var bolt = MakeTintedBolt("ShadowBolt", new Color(.65f, .35f, 1f));
        var root = PrefabUtility.LoadPrefabContents(EnemyFolder + "/DarkSorcerer.prefab");
        root.name = "ShadowKing";
        Object.DestroyImmediate(root.GetComponent<DarkSorcererAI>(), true);
        var ai = root.AddComponent<ShadowKingAI>();
        SetObject(ai, "bolt", bolt);
        SetObject(ai, "minionPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(EnemyFolder + "/Skeleton.prefab").GetComponent<EnemyHealth>());
        var visual = root.transform.Find("Visual");
        foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            if (renderer.transform != visual)
                renderer.enabled = false;
        var visualRenderer = visual.GetComponent<SpriteRenderer>();
        visualRenderer.sprite = sprite;
        visualRenderer.color = Color.white;
        const float scale = 2f;
        root.transform.localScale = Vector3.one * scale;
        visual.localPosition = new Vector3(0, .2f, 0);
        var text = root.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.text = "SHADOW KING";
            text.transform.localScale = text.transform.localScale / scale;
            text.transform.localPosition = new Vector3(0, .95f, 0);
        }
        Set(root.GetComponent<EnemyHealth>(), "maxHealth", 800f);
        Ensure<EnemyLevel>(root);
        Set(root.GetComponent<EnemyLevel>(), "level", 100);
        var glow = new GameObject("Shadow Glow", typeof(SpriteRenderer));
        glow.transform.SetParent(root.transform, false);
        glow.transform.localScale = Vector3.one * 4f / scale;
        var glowRenderer = glow.GetComponent<SpriteRenderer>();
        glowRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Decorations/Glow.png");
        glowRenderer.color = new Color(.6f, .3f, 1f, .5f);
        glowRenderer.sortingOrder = -7;
        glow.AddComponent<TorchFlicker>();
        PrefabUtility.SaveAsPrefabAsset(root, EnemyFolder + "/ShadowKing.prefab");
        PrefabUtility.UnloadPrefabContents(root);
    }

    // ---------- Nova oružja ----------

    private static readonly Dictionary<string, string[]> WeaponIcons = new Dictionary<string, string[]>
    {
        { "StormStaff", new[] {
            "................", "..........y.....", ".........yNy....", "........yNNNy...",
            ".........yNy....", "........kBk.....", ".......kBk......", "......kBk.......",
            ".....kBk........", "....kBk.........", "...kBk..........", "..kBk...........",
            "..kk............", "................", "................", "................" } },
        { "VenomStaff", new[] {
            "................", "..........m.....", ".........mMm....", "........mMMMm...",
            ".........mMm....", "........kdk.....", ".......kdk......", "......kdk.......",
            ".....kdk........", "....kdk.........", "...kdk..........", "..kdk...........",
            "..kk............", "................", "................", "................" } }
    };

    // Storm i Venom štapovi u četiri retkosti, njihovi projektili, katalog oružja i kovčezi.
    private static void CreateNewWeapons()
    {
        var stormIcon = DungeonDecorator.WriteSprite("StormStaff", DungeonDecorator.Draw(WeaponIcons["StormStaff"]), "Assets/Art/Placeholders/Icons");
        var venomIcon = DungeonDecorator.WriteSprite("VenomStaff", DungeonDecorator.Draw(WeaponIcons["VenomStaff"]), "Assets/Art/Placeholders/Icons");
        var stormBolt = MakeBolt("StormBolt", "Assets/Prefabs/Projectiles/BasicBolt.prefab", new Color(.95f, .95f, .55f), root => root.AddComponent<ChainLightning>());
        var venomBolt = MakeBolt("VenomBolt", "Assets/Prefabs/Projectiles/BasicBolt.prefab", new Color(.45f, 1f, .35f), root => root.AddComponent<PoisonOnHit>());
        var created = new List<WeaponData>();
        foreach (WeaponRarity rarity in System.Enum.GetValues(typeof(WeaponRarity)))
        {
            var basic = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/ScriptableObjects/Weapons/BasicWand/BasicWand_" + rarity + ".asset");
            created.Add(MakeWeapon("StormStaff", "Storm Staff", WeaponType.StormStaff, rarity, "Lightning jumps from the target to nearby enemies.", stormIcon, stormBolt, basic.Damage * .85f, basic.Cooldown * 1.3f, basic.ManaCost * 1.5f, 15f));
            created.Add(MakeWeapon("VenomStaff", "Venom Staff", WeaponType.VenomStaff, rarity, "Poison stacks on the target and hurts over time.", venomIcon, venomBolt, basic.Damage * .6f, basic.Cooldown * .9f, basic.ManaCost * 1.1f, 11f));
        }

        // Katalog dobija nova oružja, a kovčezi težine za pet vrsta.
        var catalog = AssetDatabase.LoadAssetAtPath<WeaponCatalog>("Assets/ScriptableObjects/Weapons/WeaponCatalog.asset");
        var catalogData = new SerializedObject(catalog);
        var list = catalogData.FindProperty("weapons");
        var existing = new List<Object>();
        for (int i = 0; i < list.arraySize; i++)
            existing.Add(list.GetArrayElementAtIndex(i).objectReferenceValue);
        foreach (var weapon in created)
            if (!existing.Contains(weapon))
            {
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = weapon;
            }
        catalogData.ApplyModifiedPropertiesWithoutUndo();
        foreach (string guid in AssetDatabase.FindAssets("t:LootTable"))
        {
            var table = AssetDatabase.LoadAssetAtPath<LootTable>(AssetDatabase.GUIDToAssetPath(guid));
            var tableData = new SerializedObject(table);
            var weights = tableData.FindProperty("typeWeights");
            var values = new[] { 32f, 22f, 18f, 14f, 14f };
            weights.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                weights.GetArrayElementAtIndex(i).floatValue = values[i];
            tableData.ApplyModifiedPropertiesWithoutUndo();
        }

        AssetDatabase.SaveAssets();
        Debug.Log("SETUP weapons: catalog " + catalog.Count + " complete=" + catalog.IsComplete);
    }

    private static Projectile MakeBolt(string name, string source, Color color, System.Action<GameObject> configure)
    {
        var root = PrefabUtility.LoadPrefabContents(source);
        root.name = name;
        foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            renderer.color = color;
        if (root.GetComponent<ChainLightning>() == null && root.GetComponent<PoisonOnHit>() == null)
            configure(root);
        string path = "Assets/Prefabs/Projectiles/" + name + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        return AssetDatabase.LoadAssetAtPath<Projectile>(path);
    }

    private static WeaponData MakeWeapon(string folder, string title, WeaponType type, WeaponRarity rarity, string description, Sprite icon, Projectile projectile, float damage, float cooldown, float mana, float speed)
    {
        string directory = "Assets/ScriptableObjects/Weapons/" + folder;
        Directory.CreateDirectory(directory);
        string path = directory + "/" + folder + "_" + rarity + ".asset";
        var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
        if (weapon == null)
        {
            weapon = ScriptableObject.CreateInstance<WeaponData>();
            AssetDatabase.CreateAsset(weapon, path);
        }

        var data = new SerializedObject(weapon);
        data.FindProperty("weaponName").stringValue = title;
        data.FindProperty("weaponType").enumValueIndex = (int)type;
        data.FindProperty("rarity").enumValueIndex = (int)rarity;
        data.FindProperty("description").stringValue = description;
        data.FindProperty("icon").objectReferenceValue = icon;
        data.FindProperty("slowMultiplier").floatValue = 1f;
        data.FindProperty("slowDuration").floatValue = 0f;
        data.FindProperty("damage").floatValue = Mathf.Round(damage * 10f) / 10f;
        data.FindProperty("cooldown").floatValue = Mathf.Round(cooldown * 100f) / 100f;
        data.FindProperty("manaCost").floatValue = Mathf.Round(mana * 10f) / 10f;
        data.FindProperty("projectileSpeed").floatValue = speed;
        data.FindProperty("projectilePrefab").objectReferenceValue = projectile;
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(weapon);
        return weapon;
    }

    // ---------- Prefabi ----------

    private sealed class Prefabs
    {
        public GameObject Skeleton, Slime, Archer, Bomber, Shieldbearer;
        public Pickup Potion, Mana;
    }

    private static Prefabs CreatePrefabs(Dictionary<string, Sprite> sprites)
    {
        var result = new Prefabs
        {
            Skeleton = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyFolder + "/Skeleton.prefab"),
            Slime = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyFolder + "/DungeonSlime.prefab"),
            Archer = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyFolder + "/DarkArcher.prefab")
        };
        var fireBlast = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/FireBlast.prefab");

        result.Bomber = MakeEnemy("Bomber", "BOMBER", sprites["Bomber"], 26f, root =>
        {
            var ai = Replace<BomberAI>(root);
            Set(ai, "movementSpeed", 3.1f);
            Set(ai, "attackRange", 1.1f);
            Set(ai, "attackWindup", .75f);
            Set(ai, "attackCooldown", 1f);
            Set(ai, "detectionRange", 7f);
            Set(ai, "loseTargetRange", 10f);
            SetObject(ai, "blastVisual", fireBlast);
        });
        result.Shieldbearer = MakeEnemy("Shieldbearer", "SHIELDBEARER", sprites["Shieldbearer"], 70f, root =>
        {
            var ai = Replace<ShieldbearerAI>(root);
            Set(ai, "movementSpeed", 1.7f);
            Set(ai, "attackCooldown", 1.3f);
            Set(ai, "attackWindup", .45f);
            var shield = new GameObject("Shield", typeof(SpriteRenderer));
            shield.transform.SetParent(root.transform, false);
            shield.transform.localPosition = new Vector3(0, -.05f, 0);
            shield.transform.localScale = Vector3.one * .85f;
            var renderer = shield.GetComponent<SpriteRenderer>();
            renderer.sprite = sprites["Shield"];
            renderer.sortingOrder = 9;
            SetObject(ai, "shield", renderer);
        });

        MakeGolem(sprites["MagmaGolem"]);
        MakeLich(sprites["Lich"]);
        result.Potion = MakePickup("PotionPickup", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Placeholders/Icons/HealingPotion.png"), PickupKind.Potion);
        result.Mana = MakePickup("ManaPickup", sprites["ManaOrb"], PickupKind.Mana);
        return result;
    }

    // Novi neprijatelj nastaje od kostura: zadržava fiziku, animaciju i prikaz zdravlja, a dobija novo ponašanje i izgled.
    private static GameObject MakeEnemy(string name, string label, Sprite sprite, float maxHealth, Action<GameObject> configure)
    {
        var root = PrefabUtility.LoadPrefabContents(EnemyFolder + "/Skeleton.prefab");
        root.name = name;
        var visual = root.transform.Find("Visual").GetComponent<SpriteRenderer>();
        visual.sprite = sprite;
        visual.color = Color.white;
        var text = root.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
            text.text = label;
        Set(root.GetComponent<EnemyHealth>(), "maxHealth", maxHealth);
        configure(root);
        string path = EnemyFolder + "/" + name + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    // Menja ponašanje kostura novim, uz isti domet napada, slojeve zidova i ostala zajednička podešavanja.
    private static T Replace<T>(GameObject root) where T : MeleeEnemyAI
    {
        var old = root.GetComponent<MeleeEnemyAI>();
        var oldSerialized = new SerializedObject(old);
        var shared = new[] { "movementSpeed", "detectionRange", "loseTargetRange", "attackRange", "attackCooldown", "attackWindup", "obstructionLayers", "pursuitMemory" };
        var values = new Dictionary<string, object>();
        foreach (string field in shared)
        {
            var property = oldSerialized.FindProperty(field);
            if (property == null)
                continue;
            values[field] = property.propertyType == SerializedPropertyType.LayerMask ? (object)property.intValue : property.floatValue;
        }

        Object.DestroyImmediate(old, true);
        var added = root.AddComponent<T>();
        var serialized = new SerializedObject(added);
        foreach (var pair in values)
        {
            var property = serialized.FindProperty(pair.Key);
            if (property == null)
                continue;
            if (pair.Value is int mask)
                property.intValue = mask;
            else
                property.floatValue = (float)pair.Value;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        return added;
    }

    // Magma Golem nastaje od prefaba čarobnjaka: zadržava fiziku, prikaz zdravlja i animaciju, a dobija novo ponašanje i izgled.
    private static void MakeGolem(Sprite sprite)
    {
        var bolt = MakeEmberBolt();
        var root = PrefabUtility.LoadPrefabContents(EnemyFolder + "/DarkSorcerer.prefab");
        root.name = "MagmaGolem";
        Object.DestroyImmediate(root.GetComponent<DarkSorcererAI>(), true);
        var ai = root.AddComponent<MagmaGolemAI>();
        SetObject(ai, "projectile", bolt);
        var visual = root.transform.Find("Visual");
        foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            if (renderer.transform != visual)
                renderer.enabled = false;
        var visualRenderer = visual.GetComponent<SpriteRenderer>();
        visualRenderer.sprite = sprite;
        visualRenderer.color = Color.white;
        // Animator menja razmeru same figure, pa se uvećava ceo objekat, a natpis i sudarač se prilagođavaju.
        const float scale = 2.3f;
        root.transform.localScale = Vector3.one * scale;
        visual.localPosition = new Vector3(0, .2f, 0);
        var circle = root.GetComponent<CircleCollider2D>();
        if (circle != null)
            circle.radius = .45f;
        var text = root.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.text = "MAGMA GOLEM";
            text.transform.localScale = text.transform.localScale / scale;
            text.transform.localPosition = new Vector3(0, .95f, 0);
        }

        Set(root.GetComponent<EnemyHealth>(), "maxHealth", 650f);
        Ensure<EnemyLevel>(root);
        Set(root.GetComponent<EnemyLevel>(), "level", 100);
        // Užareni sjaj ispod golema.
        var glow = new GameObject("Magma Glow", typeof(SpriteRenderer));
        glow.transform.SetParent(root.transform, false);
        glow.transform.localScale = Vector3.one * 4f / scale;
        var glowRenderer = glow.GetComponent<SpriteRenderer>();
        glowRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Decorations/Glow.png");
        glowRenderer.color = new Color(1f, .5f, .2f, .45f);
        glowRenderer.sortingOrder = -7;
        glow.AddComponent<TorchFlicker>();
        PrefabUtility.SaveAsPrefabAsset(root, EnemyFolder + "/MagmaGolem.prefab");
        PrefabUtility.UnloadPrefabContents(root);
    }

    // Frost Lich nastaje od prefaba čarobnjaka, kao i golem.
    private static void MakeLich(Sprite sprite)
    {
        var shard = MakeTintedBolt("IceShard", new Color(.55f, .9f, 1f));
        var root = PrefabUtility.LoadPrefabContents(EnemyFolder + "/DarkSorcerer.prefab");
        root.name = "FrostLich";
        Object.DestroyImmediate(root.GetComponent<DarkSorcererAI>(), true);
        var ai = root.AddComponent<FrostLichAI>();
        SetObject(ai, "shard", shard);
        SetObject(ai, "minionPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(EnemyFolder + "/Skeleton.prefab").GetComponent<EnemyHealth>());
        var visual = root.transform.Find("Visual");
        foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            if (renderer.transform != visual)
                renderer.enabled = false;
        var visualRenderer = visual.GetComponent<SpriteRenderer>();
        visualRenderer.sprite = sprite;
        visualRenderer.color = Color.white;
        const float scale = 1.9f;
        root.transform.localScale = Vector3.one * scale;
        visual.localPosition = new Vector3(0, .2f, 0);
        var text = root.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.text = "FROST LICH";
            text.transform.localScale = text.transform.localScale / scale;
            text.transform.localPosition = new Vector3(0, .95f, 0);
        }

        Set(root.GetComponent<EnemyHealth>(), "maxHealth", 750f);
        Ensure<EnemyLevel>(root);
        Set(root.GetComponent<EnemyLevel>(), "level", 100);
        var glow = new GameObject("Frost Glow", typeof(SpriteRenderer));
        glow.transform.SetParent(root.transform, false);
        glow.transform.localScale = Vector3.one * 4f / scale;
        var glowRenderer = glow.GetComponent<SpriteRenderer>();
        glowRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Decorations/Glow.png");
        glowRenderer.color = new Color(.5f, .8f, 1f, .45f);
        glowRenderer.sortingOrder = -7;
        glow.AddComponent<TorchFlicker>();
        PrefabUtility.SaveAsPrefabAsset(root, EnemyFolder + "/FrostLich.prefab");
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static Projectile MakeTintedBolt(string name, Color color)
    {
        var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Projectiles/SorcererBolt.prefab");
        root.name = name;
        foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            renderer.color = color;
        string path = "Assets/Prefabs/Projectiles/" + name + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        return AssetDatabase.LoadAssetAtPath<Projectile>(path);
    }

    // Narandžasta varijanta projektila čarobnjaka za golema.
    private static Projectile MakeEmberBolt()
    {
        var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Projectiles/SorcererBolt.prefab");
        root.name = "EmberBolt";
        foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            renderer.color = new Color(1f, .55f, .15f);
        const string path = "Assets/Prefabs/Projectiles/EmberBolt.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        return AssetDatabase.LoadAssetAtPath<Projectile>(path);
    }

    private static Pickup MakePickup(string name, Sprite sprite, PickupKind kind)
    {
        var root = new GameObject(name, typeof(SpriteRenderer), typeof(Pickup));
        var renderer = root.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 8;
        root.transform.localScale = Vector3.one * .75f;
        Set(root.GetComponent<Pickup>(), "kind", (int)kind);
        string path = CombatFolder + "/" + name + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return AssetDatabase.LoadAssetAtPath<Pickup>(path);
    }

    private static void AddProgressionToPlayer()
    {
        const string path = "Assets/Prefabs/Player/Player.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        if (root.GetComponent<PlayerProgression>() == null)
            root.AddComponent<PlayerProgression>();
        if (root.GetComponent<PlayerEquipment>() == null)
            root.AddComponent<PlayerEquipment>();
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    // ---------- Scene ----------

    private static void SetupGameplayScene(string path, bool ember, bool camp = false, bool frost = false, bool shadow = false)
    {
        shadowTheme = shadow;
        var scene = EditorSceneManager.OpenScene(path);
        var clips = LoadClips();
        var prefabs = LoadPrefabs();
        var player = Object.FindFirstObjectByType<PlayerHealth>();
        if (player.GetComponent<PlayerProgression>() == null)
            player.gameObject.AddComponent<PlayerProgression>();

        var systems = DungeonDecorator.SetupSystems();
        var sfx = Ensure<Sfx>(systems);
        var sfxSerialized = new SerializedObject(sfx);
        var array = sfxSerialized.FindProperty("clips");
        array.arraySize = clips.Length;
        for (int i = 0; i < clips.Length; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
        sfxSerialized.ApplyModifiedPropertiesWithoutUndo();
        SetObject(Ensure<EliteDirector>(systems), "reward", prefabs.Potion);
        Ensure<PerkChoiceMenu>(systems);
        Ensure<HudExtras>(systems);
        Ensure<LootDirector>(systems);
        Ensure<InventoryMenu>(systems);
        if (camp)
            Ensure<CampMenu>(systems);
        if (player.GetComponent<PlayerEquipment>() == null)
            player.gameObject.AddComponent<PlayerEquipment>();
        AddSettingsExtras();

        var boss = Object.FindFirstObjectByType<DarkSorcererAI>();
        if (boss != null)
            SetObject(boss, "minionPrefab", prefabs.Skeleton.GetComponent<EnemyHealth>());
        if (!ember && !camp && !frost)
            AddNewEnemiesToDungeon(prefabs);
        if (!camp)
        {
            Ensure<LevelObjectives>(systems);
            AddLevelEvents(ember, prefabs, frost);
        }
        DungeonDecorator.DecorateOpenScene(ember && !shadow, frost);
        if (shadow)
            Ensure<DarknessOverlay>(systems);
        shadowTheme = false;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("SETUP scene " + path);
    }

    private static void AddSettingsExtras()
    {
        var menus = Object.FindFirstObjectByType<GameMenus>();
        if (menus != null && menus.GetComponent<SettingsExtras>() == null)
            menus.gameObject.AddComponent<SettingsExtras>();
    }

    // U prvi nivo dodaje bombaše od četvrte i štitonoše od osme sobe, kao čuvare postojećih kapija.
    private static void AddNewEnemiesToDungeon(Prefabs prefabs)
    {
        var enemiesRoot = GameObject.Find("Enemies").transform;
        if (enemiesRoot.GetComponentsInChildren<BomberAI>(true).Length > 0)
            return;
        var gates = Object.FindObjectsByType<CombatGate>(FindObjectsSortMode.None);
        var walls = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None).First(map => map.name == "Walls");
        int added = 0;
        foreach (Transform room in enemiesRoot)
        {
            if (!room.name.StartsWith("Room ") || !int.TryParse(room.name.Substring(5, 2), out int number) || number < 4 || number > 13)
                continue;
            var members = room.GetComponentsInChildren<EnemyHealth>();
            if (members.Length == 0)
                continue;
            int level = members[0].GetComponent<EnemyLevel>().Level;
            Vector2 center = Vector2.zero;
            foreach (var member in members)
                center += (Vector2)member.transform.position;
            center /= members.Length;
            var gate = gates.FirstOrDefault(candidate => Guards(candidate).Intersect(members).Any());
            var spawned = new List<EnemyHealth> { Spawn(prefabs.Bomber, room, FreeSpot(center, members, walls, 0), level, "Bomber Lv. " + level) };
            if (number >= 8)
                spawned.Add(Spawn(prefabs.Shieldbearer, room, FreeSpot(center, members, walls, 1), level, "Shieldbearer Lv. " + level));
            added += spawned.Count;
            if (gate != null)
                SetGuards(gate, Guards(gate).Concat(spawned).ToArray());
        }
        Debug.Log("SETUP added " + added + " new enemies to Dungeon");
    }

    private static Vector2 FreeSpot(Vector2 center, EnemyHealth[] occupied, Tilemap walls, int attempt)
    {
        for (int ring = 1; ring < 6; ring++)
        for (int step = 0; step < 16; step++)
        {
            float angle = (step + attempt * 8) * Mathf.PI / 8f;
            var cell = new Vector2Int(Mathf.FloorToInt(center.x + Mathf.Cos(angle) * ring), Mathf.FloorToInt(center.y + Mathf.Sin(angle) * ring));
            var point = new Vector2(cell.x + .5f, cell.y + .5f);
            bool blocked = false;
            for (int dy = -1; dy <= 1 && !blocked; dy++)
            for (int dx = -1; dx <= 1 && !blocked; dx++)
                blocked = walls.HasTile(new Vector3Int(cell.x + dx, cell.y + dy, 0));
            if (blocked || occupied.Any(member => Vector2.Distance(member.transform.position, point) < 1.5f))
                continue;
            return point;
        }
        return center;
    }

    private static EnemyHealth Spawn(GameObject prefab, Transform parent, Vector2 position, int level, string name)
    {
        var enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        enemy.name = name;
        enemy.transform.position = position;
        Set(Ensure<EnemyLevel>(enemy), "level", level);
        return enemy.GetComponent<EnemyHealth>();
    }

    private static EnemyHealth[] Guards(CombatGate gate)
    {
        var property = new SerializedObject(gate).FindProperty("guards");
        var result = new EnemyHealth[property.arraySize];
        for (int i = 0; i < result.Length; i++)
            result[i] = property.GetArrayElementAtIndex(i).objectReferenceValue as EnemyHealth;
        return result.Where(guard => guard != null).ToArray();
    }

    private static void SetGuards(CombatGate gate, EnemyHealth[] guards)
    {
        var serialized = new SerializedObject(gate);
        var property = serialized.FindProperty("guards");
        property.arraySize = guards.Length;
        for (int i = 0; i < guards.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = guards[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- Ember Depths ----------

    private struct Room
    {
        public int X, Y, Width, Height, Level, Enemies;
        public string Title;
        public Vector2 Center => new Vector2(X + Width / 2f, Y + Height / 2f);
        public Vector2Int CenterCell => new Vector2Int(X + Width / 2, Y + Height / 2);
    }

    // Opis generisanog nivoa: raspored soba, pločice, opasnost na podu i boss.
    private sealed class LevelSpec
    {
        public string Path, Prefix, BossPrefab, BossName, VictoryMessage, Floor, Wall, Hazard;
        public bool Frost;
        public Color Background;
        public int Seed, LeverRoom;
        public int[] ChestRooms;
        public List<Room> Rooms;
    }

    private static void BuildEmberDepths()
    {
        BuildGeneratedLevel(new LevelSpec
        {
            Path = EmberPath, Prefix = "Ember", BossPrefab = "MagmaGolem", BossName = "Magma Golem",
            VictoryMessage = "The Magma Golem is destroyed.\nThe forge falls silent.",
            Floor = "EmberFloor", Wall = "EmberWall", Hazard = "Lava", Frost = false,
            Background = new Color(.09f, .035f, .03f), Seed = 4242, LeverRoom = 6, ChestRooms = new[] { 1, 3, 5 },
            Rooms = new List<Room>
        {
            new Room { X = 0, Y = 0, Width = 15, Height = 13, Level = 1, Enemies = 0, Title = "1 / 8 - SCORCHED GATE\nCONTINUE EAST" },
            new Room { X = 22, Y = 0, Width = 15, Height = 13, Level = 40, Enemies = 4, Title = "2 / 8 - ASH HALL" },
            new Room { X = 44, Y = 0, Width = 15, Height = 13, Level = 50, Enemies = 5, Title = "3 / 8 - MOLTEN FORGE\nCONTINUE NORTH" },
            new Room { X = 44, Y = 20, Width = 15, Height = 13, Level = 60, Enemies = 5, Title = "4 / 8 - CINDER CRYPT\nCONTINUE WEST" },
            new Room { X = 22, Y = 20, Width = 15, Height = 13, Level = 70, Enemies = 6, Title = "5 / 8 - OBSIDIAN WATCH" },
            new Room { X = 0, Y = 20, Width = 15, Height = 13, Level = 80, Enemies = 6, Title = "6 / 8 - INFERNO BRIDGE\nCONTINUE NORTH" },
            new Room { X = 0, Y = 40, Width = 15, Height = 13, Level = 90, Enemies = 7, Title = "7 / 8 - LEVER OF FLAME" },
            new Room { X = 0, Y = 60, Width = 17, Height = 16, Level = 100, Enemies = 0, Title = "8 / 8 - MAGMA GOLEM" }
        }
        });
    }

    private static void BuildFrozenCrypt()
    {
        BuildGeneratedLevel(new LevelSpec
        {
            Path = FrozenPath, Prefix = "Frozen", BossPrefab = "FrostLich", BossName = "Frost Lich",
            VictoryMessage = "The Frost Lich is shattered.\nThe crypt is finally quiet.",
            Floor = "FrostFloor", Wall = "FrostWall", Hazard = "Ice", Frost = true,
            Background = new Color(.03f, .05f, .09f), Seed = 9191, LeverRoom = 6, ChestRooms = new[] { 1, 3, 5 },
            Rooms = new List<Room>
            {
                new Room { X = 0, Y = 0, Width = 15, Height = 13, Level = 1, Enemies = 0, Title = "1 / 8 - FROZEN STAIRS\nCONTINUE EAST" },
                new Room { X = 22, Y = 0, Width = 15, Height = 13, Level = 70, Enemies = 5, Title = "2 / 8 - RIME HALL\nCONTINUE NORTH" },
                new Room { X = 22, Y = 20, Width = 15, Height = 13, Level = 76, Enemies = 5, Title = "3 / 8 - SHIVERING GALLERY\nCONTINUE WEST" },
                new Room { X = 0, Y = 20, Width = 15, Height = 13, Level = 82, Enemies = 6, Title = "4 / 8 - TOMB OF THE COLD\nCONTINUE NORTH" },
                new Room { X = 0, Y = 40, Width = 15, Height = 13, Level = 88, Enemies = 6, Title = "5 / 8 - GLACIER CHAPEL\nCONTINUE EAST" },
                new Room { X = 22, Y = 40, Width = 15, Height = 13, Level = 94, Enemies = 7, Title = "6 / 8 - HALL OF ICICLES\nCONTINUE EAST" },
                new Room { X = 44, Y = 40, Width = 15, Height = 13, Level = 98, Enemies = 7, Title = "7 / 8 - LEVER OF RIME" },
                new Room { X = 44, Y = 60, Width = 17, Height = 16, Level = 100, Enemies = 0, Title = "8 / 8 - FROST LICH" }
            }
        });
    }

    private static void BuildGeneratedLevel(LevelSpec spec)
    {
        AssetDatabase.DeleteAsset(spec.Path);
        AssetDatabase.CopyAsset(DungeonPath, spec.Path);
        var scene = EditorSceneManager.OpenScene(spec.Path);
        var tiles = new Dictionary<string, TileBase>
        {
            { "Floor", AssetDatabase.LoadAssetAtPath<TileBase>(ArtFolder + "/" + spec.Floor + ".asset") },
            { "Wall", AssetDatabase.LoadAssetAtPath<TileBase>(ArtFolder + "/" + spec.Wall + ".asset") },
            { "Lava", AssetDatabase.LoadAssetAtPath<TileBase>(ArtFolder + "/" + spec.Hazard + ".asset") }
        };
        var sprites = LoadSprites();
        var random = new Random(spec.Seed);
        var rooms = spec.Rooms;

        // Pod: sobe i hodnici širine 3 između uzastopnih soba.
        var floorCells = new HashSet<Vector2Int>();
        foreach (var room in rooms)
            for (int y = room.Y; y < room.Y + room.Height; y++)
            for (int x = room.X; x < room.X + room.Width; x++)
                floorCells.Add(new Vector2Int(x, y));
        var corridorCenters = new List<(Vector2 center, bool horizontal)>();
        for (int i = 0; i + 1 < rooms.Count; i++)
            corridorCenters.Add(Carve(rooms[i], rooms[i + 1], floorCells));

        Tilemap floor = null, walls = null;
        foreach (var map in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
        {
            if (map.name == "Floor")
                floor = map;
            else if (map.name == "Walls")
                walls = map;
        }

        floor.ClearAllTiles();
        walls.ClearAllTiles();
        var wallCells = new HashSet<Vector2Int>();
        foreach (var cell in floorCells)
            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                var neighbour = cell + new Vector2Int(dx, dy);
                if (!floorCells.Contains(neighbour))
                    wallCells.Add(neighbour);
            }
        foreach (var cell in floorCells)
            floor.SetTile((Vector3Int)cell, tiles["Floor"]);
        foreach (var cell in wallCells)
            walls.SetTile((Vector3Int)cell, tiles["Wall"]);

        // Lava ili led je poseban sloj iznad poda; igra ga proverava u LavaHazard ili IceSurface.
        var lavaObject = new GameObject(spec.Hazard, typeof(Tilemap), typeof(TilemapRenderer));
        if (spec.Frost)
            lavaObject.AddComponent<IceSurface>();
        else
            lavaObject.AddComponent<LavaHazard>();
        lavaObject.transform.SetParent(floor.transform.parent, false);
        lavaObject.GetComponent<TilemapRenderer>().sortingOrder = -9;
        var lava = lavaObject.GetComponent<Tilemap>();

        // Šabloni iz prvog nivoa koji se kopiraju pre brisanja starog sadržaja.
        var roots = scene.GetRootGameObjects();
        var horizontalGate = roots.First(root => root.name == "Combat Gate 2");
        var verticalGate = roots.First(root => root.name == "Combat Gate 13");
        var signTemplate = roots.First(root => root.name == "Room Sign" && root.GetComponent<TMP_Text>() != null);
        var chestTemplate = roots.First(root => root.GetComponent<LootChest>() != null);
        var templates = new GameObject("Templates");
        foreach (var template in new[] { horizontalGate, verticalGate, signTemplate, chestTemplate })
            template.transform.SetParent(templates.transform, true);

        var keep = new HashSet<string> { "Main Camera", "Player", "HUD", "Game Menus", "Event System", "Game Audio", "Boss Encounter", "BossDoor", "Lever", "Gameplay Systems", "Dungeon Rooms", "Enemies", "Templates" };
        foreach (var root in roots)
            if (root != null && root.transform.parent == null && !keep.Contains(root.name))
                Object.DestroyImmediate(root);
        var enemiesRoot = GameObject.Find("Enemies").transform;
        var boss = Object.FindFirstObjectByType<DarkSorcererAI>();
        for (int i = enemiesRoot.childCount - 1; i >= 0; i--)
        {
            var child = enemiesRoot.GetChild(i);
            if (!boss.transform.IsChildOf(child))
                Object.DestroyImmediate(child.gameObject);
        }

        // Svaki generisani nivo dobija svog bossa umesto čarobnjaka iz prvog nivoa.
        var bossRoom = rooms[rooms.Count - 1];
        var bossGroup = boss.transform.parent;
        bossGroup.name = spec.Prefix + " Room 8 - Boss";
        Object.DestroyImmediate(boss.gameObject);
        var golem = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(EnemyFolder + "/" + spec.BossPrefab + ".prefab"), bossGroup);
        golem.name = spec.BossName;
        golem.transform.position = new Vector3(bossRoom.X + bossRoom.Width / 2f, bossRoom.Y + 10f, 0);
        var encounter = Object.FindFirstObjectByType<BossEncounter>();
        var encounterSerialized = new SerializedObject(encounter);
        encounterSerialized.FindProperty("arena").rectValue = new Rect(bossRoom.X, bossRoom.Y, bossRoom.Width, bossRoom.Height);
        encounterSerialized.FindProperty("boss").objectReferenceValue = golem.GetComponent<IBossBehaviour>() as MonoBehaviour;
        encounterSerialized.ApplyModifiedPropertiesWithoutUndo();
        SetObject(Object.FindFirstObjectByType<BossHUD>(FindObjectsInactive.Include), "health", golem.GetComponent<EnemyHealth>());

        // Vrata bossa stoje u hodniku ispod arene, a poluga u sedmoj sobi.
        var door = Object.FindFirstObjectByType<Door>();
        var bossCorridor = corridorCenters[corridorCenters.Count - 1].center;
        MoveByCollider(door.gameObject, door.GetComponent<Collider2D>(), bossCorridor);
        var lever = Object.FindFirstObjectByType<Lever>();
        lever.transform.position = new Vector3(rooms[spec.LeverRoom].X + 12f, rooms[spec.LeverRoom].Y + 10f, 0);

        var player = Object.FindFirstObjectByType<PlayerHealth>();
        player.transform.position = rooms[0].Center;
        Camera.main.transform.position = new Vector3(rooms[0].Center.x, rooms[0].Center.y, -10);
        Camera.main.backgroundColor = spec.Background;
        var timer = Object.FindFirstObjectByType<RunTimer>();
        SetString(timer, "victoryMessage", spec.VictoryMessage);

        var prefabs = new[]
        {
            AssetDatabase.LoadAssetAtPath<GameObject>(EnemyFolder + "/Skeleton.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>(EnemyFolder + "/DungeonSlime.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>(EnemyFolder + "/DarkArcher.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>(EnemyFolder + "/Bomber.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>(EnemyFolder + "/Shieldbearer.prefab")
        };
        var hazards = new GameObject("Hazards").transform;
        var signs = new GameObject("Room Signs").transform;
        var chests = new GameObject("Chests").transform;
        var gates = new GameObject("Gates").transform;
        for (int index = 0; index < rooms.Count; index++)
        {
            var room = rooms[index];
            var sign = Object.Instantiate(signTemplate, signs);
            sign.name = "Room Sign";
            sign.GetComponent<TMP_Text>().text = room.Title;
            // Natpis je RectTransform pod običnim objektom, pa se položaj zadaje preko anchoredPosition.
            var signRect = sign.GetComponent<RectTransform>();
            signRect.anchorMin = signRect.anchorMax = signRect.pivot = new Vector2(.5f, .5f);
            signRect.anchoredPosition = new Vector2(room.Center.x, room.Y + room.Height - 2.3f);

            var reserved = new HashSet<Vector2Int>();
            PaintLava(room, index, lava, tiles["Lava"], random, reserved, spec.Frost);
            if (index >= 2 && index <= 6)
                for (int j = 0; j < 2 + index % 2; j++)
                {
                    var cell = RandomFreeCell(room, spec.Frost ? null : lava, reserved, random, 2);
                    AddFireJet(hazards, new Vector2(cell.x + .5f, cell.y + .5f), sprites, spec.Frost);
                    reserved.Add(cell);
                }

            if (System.Array.IndexOf(spec.ChestRooms, index) >= 0)
            {
                var chest = Object.Instantiate(chestTemplate, chests);
                chest.name = spec.Prefix + " Chest " + index;
                chest.transform.position = new Vector3(room.X + 2f, room.Y + room.Height - 2f, 0);
                reserved.Add(new Vector2Int(room.X + 2, room.Y + room.Height - 2));
            }

            if (room.Enemies == 0)
                continue;
            var group = new GameObject(spec.Prefix + " Room " + (index + 1) + " - Lv. " + room.Level).transform;
            group.SetParent(enemiesRoot, false);
            var members = new List<EnemyHealth>();
            for (int j = 0; j < room.Enemies; j++)
            {
                // Svaka soba ima mešavinu: kosture i sluzi, strelce, bombaše i štitonoše.
                int kind = j % 5 == 4 ? 4 : j % 5 == 3 ? 3 : j % 5 == 2 ? 2 : random.Next(2);
                var cell = RandomFreeCell(room, spec.Frost ? null : lava, reserved, random, 3);
                reserved.Add(cell);
                var enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[kind], group);
                enemy.name = prefabs[kind].name + " Lv. " + room.Level + " (" + (j + 1).ToString("00") + ")";
                enemy.transform.position = new Vector3(cell.x + .5f, cell.y + .5f, 0);
                Set(Ensure<EnemyLevel>(enemy), "level", room.Level);
                members.Add(enemy.GetComponent<EnemyHealth>());
            }

            // Kapija na izlazu iz sobe otvara se kada su svi čuvari sobe poraženi.
            if (index < spec.LeverRoom)
            {
                var corridor = corridorCenters[index];
                var template = corridor.horizontal ? verticalGate : horizontalGate;
                var gate = Object.Instantiate(template, gates);
                gate.name = spec.Prefix + " Gate " + (index + 1);
                MoveByCollider(gate, gate.GetComponent<Collider2D>(), corridor.center);
                SetGuards(gate.GetComponent<CombatGate>(), members.ToArray());
            }
        }

        ClearLava(lava, lever.transform.position);
        foreach (Transform chest in chests)
            ClearLava(lava, chest.position);
        Object.DestroyImmediate(templates);
        var decorations = GameObject.Find("Decorations");
        if (decorations != null)
            Object.DestroyImmediate(decorations);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("EMBER built " + spec.Prefix + ": " + floorCells.Count + " floor cells, hazard tiles used: " + lava.GetUsedTilesCount());
    }

    // ---------- Događaji u nivoima ----------

    private sealed class NoteText
    {
        public string Room, Id, Title, Text;
    }

    // Sobe se prepoznaju po natpisu sa imenom sobe; događaji se postavljaju na slobodna polja u njima.
    private static void AddLevelEvents(bool ember, Prefabs prefabs, bool frost = false)
    {
        var old = GameObject.Find("Level Events");
        if (old != null)
            Object.DestroyImmediate(old);
        foreach (var champion in Object.FindObjectsByType<ChampionEvent>(FindObjectsSortMode.None))
            Object.DestroyImmediate(champion.gameObject);
        var root = new GameObject("Level Events").transform;
        var sprites = LoadSprites();
        Tilemap floor = null, walls = null, lava = null;
        foreach (var map in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
        {
            if (map.name == "Floor") floor = map;
            else if (map.name == "Walls") walls = map;
            else if (map.name == "Lava") lava = map;
        }

        var signs = Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Where(text => text.name == "Room Sign" && text.text.Contains(" / ")).ToList();
        Vector2 RoomPoint(string number)
        {
            var sign = signs.First(text => text.text.StartsWith(number + " / "));
            return (Vector2)sign.transform.position + Vector2.down * 4.5f;
        }

        var used = new List<Vector2>();
        foreach (var blocker in Object.FindObjectsByType<LootChest>(FindObjectsSortMode.None)) used.Add(blocker.transform.position);
        foreach (var blocker in Object.FindObjectsByType<CombatGate>(FindObjectsSortMode.None)) used.Add(blocker.transform.position);
        foreach (var blocker in Object.FindObjectsByType<Lever>(FindObjectsSortMode.None)) used.Add(blocker.transform.position);
        foreach (var blocker in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None)) used.Add(blocker.transform.position);
        foreach (var blocker in Object.FindObjectsByType<FireJet>(FindObjectsSortMode.None)) used.Add(blocker.transform.position);
        bool Free(Vector2Int cell)
        {
            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                var c = new Vector3Int(cell.x + dx, cell.y + dy, 0);
                if (walls.HasTile(c) || !floor.HasTile(c) || (lava != null && lava.HasTile(c)))
                    return false;
            }
            var center = new Vector2(cell.x + .5f, cell.y + .5f);
            return used.All(point => Vector2.Distance(point, center) > 1.8f);
        }
        Vector2 Spot(Vector2 near)
        {
            var start = new Vector2Int(Mathf.FloorToInt(near.x), Mathf.FloorToInt(near.y));
            for (int radius = 0; radius < 8; radius++)
            for (int dy = -radius; dy <= radius; dy++)
            for (int dx = -radius; dx <= radius; dx++)
            {
                if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != radius)
                    continue;
                var cell = start + new Vector2Int(dx, dy);
                if (!Free(cell))
                    continue;
                var point = new Vector2(cell.x + .5f, cell.y + .5f);
                used.Add(point);
                return point;
            }
            return near;
        }

        // Granice sobe: polja poda dostižna od tačke bez prolaska kroz uske prolaze.
        Rect RoomRect(Vector2 point)
        {
            var start = new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y));
            var seen = new HashSet<Vector2Int> { start };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            int xMin = start.x, xMax = start.x, yMin = start.y, yMax = start.y;
            bool Doorway(Vector2Int c) => (walls.HasTile(new Vector3Int(c.x - 1, c.y, 0)) && walls.HasTile(new Vector3Int(c.x + 1, c.y, 0))) || (walls.HasTile(new Vector3Int(c.x, c.y - 1, 0)) && walls.HasTile(new Vector3Int(c.x, c.y + 1, 0)));
            // Pretraga ostaje u okviru jedne sobe (soba je oko 15 x 13 polja).
            while (queue.Count > 0 && seen.Count < 400)
            {
                var c = queue.Dequeue();
                xMin = Mathf.Min(xMin, c.x); xMax = Mathf.Max(xMax, c.x); yMin = Mathf.Min(yMin, c.y); yMax = Mathf.Max(yMax, c.y);
                foreach (var step in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
                {
                    var next = c + step;
                    var cell = new Vector3Int(next.x, next.y, 0);
                    if (seen.Contains(next) || walls.HasTile(cell) || !floor.HasTile(cell) || Doorway(next) || Mathf.Abs(next.x - start.x) > 9 || Mathf.Abs(next.y - start.y) > 8)
                        continue;
                    seen.Add(next);
                    queue.Enqueue(next);
                }
            }
            return new Rect(xMin, yMin, xMax - xMin + 1, yMax - yMin + 1);
        }

        GameObject Make(string name, Sprite sprite, Vector2 position, float scale, bool solid)
        {
            var item = new GameObject(name, typeof(SpriteRenderer));
            item.transform.SetParent(root, false);
            item.transform.position = position;
            item.transform.localScale = Vector3.one * scale;
            item.GetComponent<SpriteRenderer>().sprite = sprite;
            item.GetComponent<SpriteRenderer>().sortingOrder = 4;
            item.layer = EnvironmentLayer;
            var box = item.AddComponent<BoxCollider2D>();
            box.size = new Vector2(.7f, .5f);
            box.isTrigger = !solid;
            return item;
        }

        string ambushRoom = frost ? "4" : ember ? "4" : "7", championRoom = frost ? "5" : ember ? "6" : "9", vaultRoom = frost ? "7" : ember ? "7" : "10", shrineRoom = frost ? "3" : ember ? "3" : "8";
        string vaultId = shadowTheme ? "shadow" : frost ? "frozen" : ember ? "ember" : "dungeon";
        int roomLevel = shadowTheme ? 95 : frost ? 85 : ember ? 65 : 35;

        // Zaseda.
        var ambushRect = RoomRect(RoomPoint(ambushRoom));
        var ambush = new GameObject("Ambush", typeof(AmbushEvent));
        ambush.transform.SetParent(root, false);
        var ambushData = new SerializedObject(ambush.GetComponent<AmbushEvent>());
        ambushData.FindProperty("ambushId").stringValue = vaultId;
        ambushData.FindProperty("area").rectValue = ambushRect;
        ambushData.FindProperty("enemyLevel").intValue = roomLevel;
        var pool = ambushData.FindProperty("enemyPool");
        var poolPrefabs = new[] { prefabs.Skeleton, prefabs.Slime, prefabs.Archer, prefabs.Bomber };
        pool.arraySize = poolPrefabs.Length;
        for (int i = 0; i < poolPrefabs.Length; i++)
            pool.GetArrayElementAtIndex(i).objectReferenceValue = poolPrefabs[i].GetComponent<EnemyHealth>();
        ambushData.ApplyModifiedPropertiesWithoutUndo();
        if (!ember)
        {
            var sanctuary = signs.First(text => text.text.StartsWith("7 / "));
            sanctuary.text = "7 / 15 - FALSE SANCTUARY";
        }

        // Šampion nosi ključ trezora.
        var championPoint = RoomPoint(championRoom);
        var championEnemy = Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None)
            .Where(enemy => enemy.GetComponent<SkeletonAI>() != null || enemy.GetComponent<SlimeAI>() != null)
            .OrderBy(enemy => Vector2.Distance(enemy.transform.position, championPoint)).First();
        var championObject = new GameObject("Champion", typeof(ChampionEvent));
        championObject.transform.SetParent(root, false);
        var championData = new SerializedObject(championObject.GetComponent<ChampionEvent>());
        championData.FindProperty("champion").objectReferenceValue = championEnemy;
        championData.FindProperty("championName").stringValue = shadowTheme ? "VOIDCLAW" : frost ? "RIMEFANG" : ember ? "CINDERJAW" : "GRAVEMAW";
        championData.FindProperty("affix").enumValueIndex = (int)(frost ? ChampionAffix.Arcane : ember ? ChampionAffix.Molten : ChampionAffix.Vampiric);
        championData.FindProperty("vaultId").stringValue = vaultId;
        championData.FindProperty("arcaneBolt").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Projectile>("Assets/Prefabs/Projectiles/SorcererBolt.prefab");
        championData.ApplyModifiedPropertiesWithoutUndo();

        // Trezor: zlatni kovčeg.
        var vault = Make("Vault Chest", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/OriginalPixel/ChestBody.png"), Spot(RoomPoint(vaultRoom) + Vector2.up * 1.5f), 1.3f, true);
        vault.GetComponent<SpriteRenderer>().color = new Color(1f, .85f, .35f);
        var lid = new GameObject("Lid", typeof(SpriteRenderer));
        lid.transform.SetParent(vault.transform, false);
        lid.transform.localPosition = new Vector3(0, .3f, 0);
        lid.GetComponent<SpriteRenderer>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/OriginalPixel/ChestLid.png");
        lid.GetComponent<SpriteRenderer>().color = new Color(1f, .85f, .35f);
        lid.GetComponent<SpriteRenderer>().sortingOrder = 5;
        var vaultComponent = vault.AddComponent<VaultChest>();
        var vaultData = new SerializedObject(vaultComponent);
        vaultData.FindProperty("vaultId").stringValue = vaultId;
        vaultData.FindProperty("itemLevel").intValue = roomLevel + 10;
        vaultData.FindProperty("lid").objectReferenceValue = lid.GetComponent<SpriteRenderer>();
        vaultData.ApplyModifiedPropertiesWithoutUndo();

        // Oltar sudbine.
        var shrine = Make("Shrine of Fate", sprites["Shrine"], Spot(RoomPoint(shrineRoom) + new Vector2(3f, 0)), 1.6f, true);
        var shrineData = new SerializedObject(shrine.AddComponent<ShrineEvent>());
        shrineData.FindProperty("shrineId").stringValue = vaultId;
        shrineData.ApplyModifiedPropertiesWithoutUndo();

        // Beleške sa pričom.
        var notes = shadowTheme
            ? new[]
            {
                new NoteText { Room = "1", Id = "shadow1", Title = "TEMPLE INSCRIPTION", Text = "Here the old kings worshipped the dark between the stars. They built no windows, so that no light would ever reach their god.\n\nCarry your own light, traveller." },
                new NoteText { Room = "4", Id = "shadow2", Title = "MONK'S LAST PRAYER", Text = "The King of Shadows walks the inner hall. When he grows weak he swallows the light itself.\n\nWhen the dark closes in, keep your back to a wall." },
                new NoteText { Room = "7", Id = "shadow3", Title = "THE FINAL SEAL", Text = "The Sorcerer, the Golem and the Lich were only his keys. Now every lock is broken.\n\nEnd it, wizard. End it here." }
            }
            : frost
            ? new[]
            {
                new NoteText { Room = "1", Id = "frozen1", Title = "FROST-BITTEN DIARY", Text = "The crypt was sealed to keep the Lich asleep. The Golem kept the heat below so the ice above would never melt.\n\nWith the Golem gone, the cold is waking." },
                new NoteText { Room = "4", Id = "frozen2", Title = "PRIEST'S WARNING", Text = "The Lich does not stand and fight. It steps through the ice and appears where you least expect it.\n\nKeep moving, and never stand still in the frost." },
                new NoteText { Room = "6", Id = "frozen3", Title = "THE LICH'S OATH", Text = "I froze my own heart so it could never be broken.\n\nCome, little wizard. Your fire will not last long down here." }
            }
            : ember
            ? new[]
            {
                new NoteText { Room = "1", Id = "ember1", Title = "SMITH'S LEDGER", Text = "The old kings forged their crowns in these halls. When the last king fell, the fires were left burning.\n\nThe forge never cooled. It only waited." },
                new NoteText { Room = "5", Id = "ember2", Title = "BURNT LETTER", Text = "The Golem was built to guard the forge, not to rule it. Something woke it with a voice from above.\n\nIf you read this, do not stand in the fire for long." },
                new NoteText { Room = "7", Id = "ember3", Title = "THE LAST PAGE", Text = "Strike the Golem when it charges into the wall. Even living stone needs a moment to remember where it is." }
            }
            : new[]
            {
                new NoteText { Room = "1", Id = "dungeon1", Title = "GUARD'S JOURNAL", Text = "Day 12. The master stopped sleeping. He talks to the walls of the lowest chamber, and the walls talk back.\n\nWe were told to guard the lever chamber day and night." },
                new NoteText { Room = "6", Id = "dungeon2", Title = "TORN ORDERS", Text = "All wardens: the Sorcerer's door opens only from the lever chamber. No one pulls the lever without his word.\n\nGravemaw keeps the key to the cellar vault. Do not ask him for it." },
                new NoteText { Room = "12", Id = "dungeon3", Title = "A PRISONER'S SCRAWL", Text = "He is not hiding down here. He is waiting.\n\nSomething under the dungeon is cracking open, and he wants to be the first to drink from it." }
            };
        foreach (var note in notes)
        {
            var page = Make("Lost Page", sprites["Note"], Spot(RoomPoint(note.Room) + new Vector2(-3f, -1f)), .9f, false);
            var noteData = new SerializedObject(page.AddComponent<LoreNote>());
            noteData.FindProperty("noteId").stringValue = note.Id;
            noteData.FindProperty("title").stringValue = note.Title;
            noteData.FindProperty("text").stringValue = note.Text;
            noteData.ApplyModifiedPropertiesWithoutUndo();
        }

        AddSecrets(root, floor, walls, frost ? null : lava, sprites["Crack"], ember || frost ? 2 : 3, used);
        Debug.Log("SETUP level events added (ambush area " + ambushRect + ", champion " + championEnemy.name + ")");
    }

    // Tajni prolaz: napukao zid na ivici sobe vodi u mali skriveni džep sa kovčegom.
    private static void AddSecrets(Transform root, Tilemap floor, Tilemap walls, Tilemap lava, Sprite crack, int count, List<Vector2> used)
    {
        var chestTemplate = Object.FindObjectsByType<LootChest>(FindObjectsSortMode.None).FirstOrDefault();
        bool Empty(Vector3Int c) => !walls.HasTile(c) && !floor.HasTile(c);
        floor.CompressBounds();
        var bounds = floor.cellBounds;
        var steps = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        var random = new Random(77);
        var candidates = new List<(Vector2Int wall, Vector2Int direction)>();
        for (int y = bounds.yMin; y < bounds.yMax; y++)
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            var wall = new Vector2Int(x, y);
            if (!walls.HasTile((Vector3Int)wall))
                continue;
            foreach (var direction in steps)
            {
                // Pod ispred zida, a iza zida 6 polja praznine za hodnik i džep 3x3.
                var inside = wall - direction;
                if (!floor.HasTile((Vector3Int)inside) || walls.HasTile((Vector3Int)inside))
                    continue;
                var side = new Vector2Int(direction.y, direction.x);
                if (!walls.HasTile((Vector3Int)(wall + side)) || !walls.HasTile((Vector3Int)(wall - side)))
                    continue;
                bool clear = true;
                for (int k = 1; k <= 6 && clear; k++)
                for (int s = -2; s <= 2 && clear; s++)
                    clear = Empty((Vector3Int)(wall + direction * k + side * s));
                var insidePoint = new Vector2(inside.x + .5f, inside.y + .5f);
                if (clear && used.All(point => Vector2.Distance(point, insidePoint) > 3f))
                    candidates.Add((wall, direction));
            }
        }

        var chosen = new List<Vector2Int>();
        while (chosen.Count < count && candidates.Count > 0)
        {
            var pick = candidates[random.Next(candidates.Count)];
            candidates.Remove(pick);
            if (chosen.Any(other => Vector2Int.Distance(other, pick.wall) < 15f))
                continue;
            chosen.Add(pick.wall);
            var side = new Vector2Int(pick.direction.y, pick.direction.x);
            var pocket = new HashSet<Vector2Int>();
            pocket.Add(pick.wall + pick.direction);
            for (int k = 2; k <= 4; k++)
            for (int s = -1; s <= 1; s++)
                pocket.Add(pick.wall + pick.direction * k + side * s);
            foreach (var cell in pocket)
                floor.SetTile((Vector3Int)cell, floor.GetTile((Vector3Int)(pick.wall - pick.direction)));
            var wallTile = walls.GetTile((Vector3Int)pick.wall);
            foreach (var cell in pocket)
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    var around = cell + new Vector2Int(dx, dy);
                    if (!pocket.Contains(around) && around != pick.wall && !floor.HasTile((Vector3Int)around))
                        walls.SetTile((Vector3Int)around, wallTile);
                }

            var crackObject = new GameObject("Cracked Wall", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(CrackedWall));
            crackObject.transform.SetParent(root, false);
            crackObject.transform.position = new Vector3(pick.wall.x + .5f, pick.wall.y + .5f, 0);
            crackObject.layer = EnvironmentLayer;
            crackObject.GetComponent<SpriteRenderer>().sprite = crack;
            crackObject.GetComponent<SpriteRenderer>().sortingOrder = 1;
            crackObject.GetComponent<BoxCollider2D>().size = new Vector2(1.1f, 1.1f);
            var crackData = new SerializedObject(crackObject.GetComponent<CrackedWall>());
            crackData.FindProperty("secretId").stringValue = SceneManagerName() + "_" + chosen.Count;
            crackData.ApplyModifiedPropertiesWithoutUndo();

            if (chestTemplate != null)
            {
                var chest = Object.Instantiate(chestTemplate.gameObject, root);
                chest.name = "Secret Cache";
                var center = pick.wall + pick.direction * 3;
                chest.transform.position = new Vector3(center.x + .5f, center.y + .5f, 0);
            }
        }
        Debug.Log("SETUP secrets carved: " + chosen.Count + " of " + count);
    }

    private static string SceneManagerName() => EditorSceneManager.GetActiveScene().name;

    // ---------- Kamp ----------

    // Kamp je bezbedna soba između nivoa: trgovac, oltar runa, starac, izvor i portali za otključane nivoe.
    private static void BuildCamp()
    {
        AssetDatabase.DeleteAsset(CampPath);
        AssetDatabase.CopyAsset(DungeonPath, CampPath);
        var scene = EditorSceneManager.OpenScene(CampPath);
        var sprites = LoadSprites();
        var floorTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Art/Placeholders/Floor.asset");
        var wallTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Art/Placeholders/Wall.asset");
        Tilemap floor = null, walls = null;
        foreach (var map in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
        {
            if (map.name == "Floor")
                floor = map;
            else if (map.name == "Walls")
                walls = map;
        }

        floor.ClearAllTiles();
        walls.ClearAllTiles();
        const int width = 21, height = 15;
        for (int y = -1; y <= height; y++)
        for (int x = -1; x <= width; x++)
        {
            var cell = new Vector3Int(x, y, 0);
            if (x < 0 || y < 0 || x >= width || y >= height)
                walls.SetTile(cell, wallTile);
            else
                floor.SetTile(cell, floorTile);
        }

        var roots = scene.GetRootGameObjects();
        var signTemplate = roots.First(root => root.name == "Room Sign" && root.GetComponent<TMP_Text>() != null);
        var keep = new HashSet<string> { "Main Camera", "Player", "HUD", "Game Menus", "Event System", "Game Audio", "Boss Encounter", "Gameplay Systems", "Dungeon Rooms", "Enemies" };
        signTemplate.transform.SetParent(null);
        var template = Object.Instantiate(signTemplate);
        foreach (var root in roots)
            if (root != null && root.transform.parent == null && !keep.Contains(root.name))
                Object.DestroyImmediate(root);
        var enemiesRoot = GameObject.Find("Enemies").transform;
        for (int i = enemiesRoot.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(enemiesRoot.GetChild(i).gameObject);

        var stations = new GameObject("Camp").transform;
        var title = Object.Instantiate(template, stations);
        title.name = "Camp Title";
        title.GetComponent<TMP_Text>().text = "WIZARD'S CAMP\n<size=60%>Talk to the Elder, trade, then step through a portal</size>";
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = titleRect.anchorMax = titleRect.pivot = new Vector2(.5f, .5f);
        titleRect.anchoredPosition = new Vector2(width / 2f, height - 1.4f);
        AddStation(stations, template, sprites["Merchant"], CampStationKind.Merchant, 0, new Vector2(4.5f, 10.5f), 1.4f);
        AddStation(stations, template, sprites["Elder"], CampStationKind.Elder, 0, new Vector2(10.5f, 10.5f), 1.4f);
        AddStation(stations, template, sprites["Altar"], CampStationKind.RuneAltar, 0, new Vector2(16.5f, 10.5f), 1.4f);
        AddStation(stations, template, sprites["Fountain"], CampStationKind.Fountain, 0, new Vector2(10.5f, 5.5f), 1.6f);
        for (int i = 0; i < GameLevels.All.Length; i++)
            AddStation(stations, template, sprites["Portal"], CampStationKind.Portal, i + 1, new Vector2(19f, 2.6f + i * 3.6f), 1.8f);
        Object.DestroyImmediate(template);

        var player = Object.FindFirstObjectByType<PlayerHealth>();
        player.transform.position = new Vector3(3.5f, 4.5f, 0);
        Camera.main.transform.position = new Vector3(3.5f, 4.5f, -10);
        Camera.main.backgroundColor = new Color(.04f, .045f, .07f);
        var decorations = GameObject.Find("Decorations");
        if (decorations != null)
            Object.DestroyImmediate(decorations);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("SETUP camp built");
    }

    // Endless arena: pravi se od kampa (lik, meniji, sistemi), bez čuvanja partije, sa stubovima za zaklon.
    public static void BuildEndless()
    {
        AssetDatabase.DeleteAsset(EndlessPath);
        AssetDatabase.CopyAsset(CampPath, EndlessPath);
        var scene = EditorSceneManager.OpenScene(EndlessPath);
        var tiles = LoadTiles();
        Tilemap floor = null, walls = null;
        foreach (var map in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
        {
            if (map.name == "Floor")
                floor = map;
            else if (map.name == "Walls")
                walls = map;
        }

        floor.ClearAllTiles();
        walls.ClearAllTiles();
        const int width = 36, height = 26;
        var pillars = new[] { new Vector2Int(8, 7), new Vector2Int(26, 7), new Vector2Int(8, 17), new Vector2Int(26, 17), new Vector2Int(17, 12), new Vector2Int(17, 4), new Vector2Int(17, 20) };
        for (int y = -1; y <= height; y++)
        for (int x = -1; x <= width; x++)
        {
            var cell = new Vector3Int(x, y, 0);
            bool pillar = pillars.Any(p => x >= p.x && x <= p.x + 1 && y >= p.y && y <= p.y + 1 && !(p.x == 17 && p.y == 12));
            if (x < 0 || y < 0 || x >= width || y >= height || pillar)
                walls.SetTile(cell, tiles["Wall"]);
            else
                floor.SetTile(cell, tiles["Floor"]);
        }

        var camp = GameObject.Find("Camp");
        if (camp != null)
            Object.DestroyImmediate(camp);
        var systems = GameObject.Find("Gameplay Systems");
        foreach (var type in new[] { typeof(SaveSystem), typeof(CampMenu) })
        {
            var component = systems.GetComponent(type);
            if (component != null)
                Object.DestroyImmediate(component);
        }

        var prefabs = LoadPrefabs();
        var director = Ensure<EndlessDirector>(systems);
        var data = new SerializedObject(director);
        var list = data.FindProperty("enemyPrefabs");
        var enemies = new[] { prefabs.Skeleton, prefabs.Slime, prefabs.Archer, prefabs.Bomber, prefabs.Shieldbearer };
        list.arraySize = enemies.Length;
        for (int i = 0; i < enemies.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = enemies[i];
        var unlocks = data.FindProperty("unlockWave");
        var waves = new[] { 1, 1, 3, 5, 7 };
        unlocks.arraySize = waves.Length;
        for (int i = 0; i < waves.Length; i++)
            unlocks.GetArrayElementAtIndex(i).intValue = waves[i];
        data.FindProperty("catalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<WeaponCatalog>("Assets/ScriptableObjects/Weapons/WeaponCatalog.asset");
        data.FindProperty("arenaMin").vector2Value = new Vector2(0, 0);
        data.FindProperty("arenaMax").vector2Value = new Vector2(width, height);
        data.ApplyModifiedPropertiesWithoutUndo();

        var player = Object.FindFirstObjectByType<PlayerHealth>();
        player.transform.position = new Vector3(width / 2f, 9.5f, 0);
        Camera.main.transform.position = new Vector3(width / 2f, 9.5f, -10);
        Camera.main.backgroundColor = new Color(.06f, .03f, .03f);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(entry => entry.path == EndlessPath))
            scenes.Add(new EditorBuildSettingsScene(EndlessPath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("SETUP endless arena built");
    }

    private static void AddStation(Transform parent, GameObject labelTemplate, Sprite sprite, CampStationKind kind, int levelIndex, Vector2 position, float scale)
    {
        var station = new GameObject(kind + (levelIndex > 0 ? " " + levelIndex : ""), typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(CampStation));
        station.transform.SetParent(parent, false);
        station.transform.position = position;
        station.transform.localScale = Vector3.one * scale;
        station.layer = EnvironmentLayer;
        var renderer = station.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 5;
        var box = station.GetComponent<BoxCollider2D>();
        box.size = new Vector2(.6f, .45f);
        box.offset = new Vector2(0, -.25f);
        var label = Object.Instantiate(labelTemplate, station.transform);
        label.name = "Label";
        // Natpis je dete stanice, pa se postavlja lokalno (razmera stanice se poništava).
        label.transform.localScale = Vector3.one / scale;
        var rect = label.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(0, .55f + .35f / scale);
        var serialized = new SerializedObject(station.GetComponent<CampStation>());
        serialized.FindProperty("kind").enumValueIndex = (int)kind;
        serialized.FindProperty("levelIndex").intValue = Mathf.Max(1, levelIndex);
        serialized.FindProperty("label").objectReferenceValue = label.GetComponent<TMP_Text>();
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // Iskopava hodnik širine 3 između dve susedne sobe i vraća njegovo središte.
    private static (Vector2 center, bool horizontal) Carve(Room a, Room b, HashSet<Vector2Int> cells)
    {
        if (a.Y == b.Y)
        {
            var left = a.X < b.X ? a : b;
            var right = a.X < b.X ? b : a;
            int cy = left.Y + left.Height / 2;
            for (int x = left.X + left.Width; x < right.X; x++)
            for (int y = cy - 1; y <= cy + 1; y++)
                cells.Add(new Vector2Int(x, y));
            return (new Vector2((left.X + left.Width + right.X) / 2f, cy + .5f), true);
        }

        var lower = a.Y < b.Y ? a : b;
        var upper = a.Y < b.Y ? b : a;
        int cx = lower.X + lower.Width / 2;
        for (int y = lower.Y + lower.Height; y < upper.Y; y++)
        for (int x = cx - 1; x <= cx + 1; x++)
            cells.Add(new Vector2Int(x, y));
        return (new Vector2(cx + .5f, (lower.Y + lower.Height + upper.Y) / 2f), false);
    }

    // Lava se ne stavlja na krst kroz sredinu sobe, pa uvek postoji siguran prolaz između vrata.
    private static void PaintLava(Room room, int index, Tilemap lava, TileBase tile, Random random, HashSet<Vector2Int> reserved, bool ice = false)
    {
        int pools = index == 0 ? 1 : index == 7 ? 4 : 2 + random.Next(2);
        var center = room.CenterCell;
        for (int p = 0; p < pools; p++)
        {
            int qx = p % 2 == 0 ? -1 : 1, qy = p < 2 ? -1 : 1;
            if (index != 7)
            {
                qx = random.Next(2) == 0 ? -1 : 1;
                qy = random.Next(2) == 0 ? -1 : 1;
            }

            var poolCenter = new Vector2(center.x + qx * (3.5f + (float)random.NextDouble() * 1.5f), center.y + qy * (2.5f + (float)random.NextDouble()));
            // Led je bezopasan, pa su njegove površine veće i smeju da pređu i preko prolaza.
            float rx = (ice ? 2.2f : 1.2f) + (float)random.NextDouble() * (ice ? 1.8f : 1f), ry = (ice ? 1.6f : 1f) + (float)random.NextDouble() * (ice ? 1.2f : .6f);
            for (int y = room.Y + 2; y < room.Y + room.Height - 2; y++)
            for (int x = room.X + 2; x < room.X + room.Width - 2; x++)
            {
                if (!ice && (Mathf.Abs(x - center.x) <= 1 || Mathf.Abs(y - center.y) <= 1))
                    continue;
                float dx = (x + .5f - poolCenter.x) / rx, dy = (y + .5f - poolCenter.y) / ry;
                if (dx * dx + dy * dy > 1f)
                    continue;
                var cell = new Vector2Int(x, y);
                lava.SetTile((Vector3Int)cell, tile);
                if (!ice)
                    reserved.Add(cell);
            }
        }
    }

    private static Vector2Int RandomFreeCell(Room room, Tilemap lava, HashSet<Vector2Int> reserved, Random random, int margin)
    {
        for (int attempt = 0; attempt < 200; attempt++)
        {
            var cell = new Vector2Int(room.X + margin + random.Next(room.Width - margin * 2), room.Y + margin + random.Next(room.Height - margin * 2));
            if ((lava != null && lava.HasTile((Vector3Int)cell)) || reserved.Any(other => Mathf.Abs(other.x - cell.x) + Mathf.Abs(other.y - cell.y) < 2))
                continue;
            return cell;
        }
        return room.CenterCell;
    }

    private static void AddFireJet(Transform parent, Vector2 position, Dictionary<string, Sprite> sprites, bool frost = false)
    {
        var jet = new GameObject("Fire Jet", typeof(SpriteRenderer), typeof(FireJet));
        jet.transform.SetParent(parent, false);
        jet.transform.position = position;
        var vent = jet.GetComponent<SpriteRenderer>();
        vent.sprite = sprites["FireVent"];
        vent.sortingOrder = -8;
        var flame = new GameObject("Flame", typeof(SpriteRenderer));
        flame.transform.SetParent(jet.transform, false);
        flame.transform.localPosition = new Vector3(0, .45f, 0);
        var flameRenderer = flame.GetComponent<SpriteRenderer>();
        flameRenderer.sprite = sprites["Flame"];
        flameRenderer.sortingOrder = 9;
        flameRenderer.enabled = false;
        SetObject(jet.GetComponent<FireJet>(), "flame", flameRenderer);
        if (frost)
        {
            // Ledeni otvor: plavi plamen koji usporava.
            jet.name = "Frost Vent";
            vent.color = new Color(.7f, .85f, 1f);
            flameRenderer.color = new Color(.55f, .85f, 1f);
            var serialized = new SerializedObject(jet.GetComponent<FireJet>());
            serialized.FindProperty("frost").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    // Kapije i vrata imaju sudarač u središtu korena (provereno u prvom nivou), pa se koren postavlja u sredinu hodnika.
    private static void MoveByCollider(GameObject target, Collider2D collider, Vector2 center)
    {
        var box = collider as BoxCollider2D;
        Vector2 offset = box != null ? box.offset : Vector2.zero;
        target.transform.position = (Vector3)(center - offset);
    }

    private static void ClearLava(Tilemap lava, Vector2 position)
    {
        var cell = lava.WorldToCell(position);
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
            lava.SetTile(cell + new Vector3Int(dx, dy, 0), null);
    }

    // ---------- Pomoćne funkcije za serijalizovana polja ----------

    private static T Ensure<T>(GameObject target) where T : Component => target.GetComponent<T>() ?? target.AddComponent<T>();

    private static void Set(Object target, string field, float value)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(field);
        if (property == null)
        {
            Debug.LogError("SETUP missing field " + field + " on " + target);
            return;
        }

        if (property.propertyType == SerializedPropertyType.Integer || property.propertyType == SerializedPropertyType.Enum)
            property.intValue = (int)value;
        else
            property.floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObject(Object target, string field, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetString(Object target, string field, string value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).stringValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetColor(Object target, string field, Color value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).colorValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
