// Rift (kao Greater Rift u Diablu): igra se sa svojim likom iz priče. Svaki sprat ima drugi izgled
// (tamnica, lava, led, hram) i traži određen broj ubistava; tada se pojavi Čuvar Rifta. Posle čuvara
// igrač bira: dublje (jače i bolje nagrade) ili nazad u kamp sa svim plenom. Pamti se najdublji sprat.
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace AWizardsFight
{
    public sealed class RiftDirector : MonoBehaviour
    {
        public const string SceneName = "Rift", RecordKey = "Rift";

        [SerializeField]
        private GameObject[] enemyPrefabs = new GameObject[0];
        [SerializeField, Tooltip("Pločice poda i zida za svaku temu: tamnica, lava, led, hram.")]
        private TileBase[] floorTiles = new TileBase[0], wallTiles = new TileBase[0];
        [SerializeField]
        private Vector2 arenaMin = new Vector2(1, 1), arenaMax = new Vector2(35, 25);

        public static bool Active { get; private set; }
        public static int Floor { get; private set; }
        public static int BestFloor => Leaderboard.Waves(RecordKey).DefaultIfEmpty(0).Max();
        private static readonly string[] Themes = { "CRYPT", "EMBER", "FROST", "SHADOW" };

        private readonly List<EnemyHealth> alive = new List<EnemyHealth>();
        private PlayerHealth player;
        private PlayerEquipment equipment;
        private TextMeshProUGUI headerText, bannerText;
        private Image progressFill;
        private int kills, goal;
        private bool guardianSpawned, floorDone, recorded;
        private EnemyHealth guardian;
        private float nextSpawn, bannerUntil;
        private Transform enemyRoot;
        private DarknessOverlay darkness;

        private void Awake()
        {
            Active = true;
            Floor = 0;
            player = FindFirstObjectByType<PlayerHealth>();
            equipment = player.GetComponent<PlayerEquipment>();
            enemyRoot = new GameObject("Rift Enemies").transform;
        }

        private void OnEnable()
        {
            EnemyHealth.AnyDied += EnemyDied;
            player.Died += PlayerDied;
        }

        private void OnDisable()
        {
            EnemyHealth.AnyDied -= EnemyDied;
            if (player != null)
                player.Died -= PlayerDied;
            Active = false;
            LevelModifiers.Reset();
        }

        private void Start()
        {
            var canvas = FindFirstObjectByType<GameMenus>().transform;
            headerText = UiFactory.Text("Rift Header", canvas, "", 26, new Vector2(0, 505), new Vector2(1000, 40), new Color(1f, .5f, .45f));
            headerText.fontStyle = FontStyles.Bold;
            headerText.rectTransform.SetAsFirstSibling();
            var bar = UiFactory.Panel("Rift Progress", canvas, new Vector2(0, 470), new Vector2(520, 16), new Color(.08f, .05f, .1f, .9f));
            bar.rectTransform.SetAsFirstSibling();
            progressFill = UiFactory.Panel("Fill", bar.transform, Vector2.zero, new Vector2(520, 16), new Color(.9f, .3f, .35f));
            progressFill.sprite = RuntimeSprites.Square;
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            bannerText = UiFactory.Text("Rift Banner", canvas, "", 60, new Vector2(0, 180), new Vector2(1400, 200), Color.white);
            bannerText.fontStyle = FontStyles.Bold;
            bannerText.outlineWidth = .2f;
            bannerText.rectTransform.SetAsFirstSibling();
            StartFloor();
        }

        // ---------- Spratovi ----------

        private void StartFloor()
        {
            Floor++;
            kills = 0;
            goal = 18 + Floor * 4;
            guardianSpawned = floorDone = false;
            guardian = null;
            // Svaki sprat jači: nivo protivnika raste do 100, a posle toga zdravlje i šteta.
            LevelModifiers.EnemyHealth = 1f + .15f * Mathf.Max(0, Floor - 1);
            LevelModifiers.EnemyDamage = 1f + .08f * Mathf.Max(0, Floor - 1);
            int theme = (Floor - 1) % Themes.Length;
            Retheme(theme);
            // Hramski spratovi su u mraku.
            if (theme == 3 && darkness == null)
                darkness = gameObject.AddComponent<DarknessOverlay>();
            else if (theme != 3 && darkness != null)
            {
                Destroy(darkness);
                darkness = null;
            }
            Banner($"RIFT  ·  FLOOR {Floor}\n<size=45%><color=#FF8A80>{Themes[theme]}  ·  {Torment.Name(Torment.Level)}</color></size>", 3f);
            nextSpawn = Time.time + 2f;
        }

        private int EnemyLevel => Mathf.Clamp(40 + Floor * 6, 1, 100);

        private void Retheme(int theme)
        {
            if (theme >= floorTiles.Length || theme >= wallTiles.Length)
                return;
            foreach (var map in FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            {
                var tile = map.name == "Floor" ? floorTiles[theme] : map.name == "Walls" ? wallTiles[theme] : null;
                if (tile == null)
                    continue;
                foreach (var cell in map.cellBounds.allPositionsWithin)
                    if (map.HasTile(cell))
                        map.SetTile(cell, tile);
            }
            TileVariety.Apply();
        }

        private void Update()
        {
            alive.RemoveAll(enemy => enemy == null || enemy.IsDead);
            float ratio = guardianSpawned ? 1f : Mathf.Clamp01(kills / (float)goal);
            progressFill.fillAmount = ratio;
            headerText.text = $"RIFT FLOOR {Floor}   ·   {(guardianSpawned ? (floorDone ? "CLEARED" : "DEFEAT THE GUARDIAN") : kills + " / " + goal)}   ·   BEST {BestFloor}";
            bannerText.alpha = Mathf.Clamp01((bannerUntil - Time.time) * 1.5f);
            if (floorDone || player.IsDead || Time.timeScale <= 0f)
                return;
            int cap = Mathf.Min(14, 6 + Floor);
            if (!guardianSpawned && Time.time >= nextSpawn && alive.Count < cap)
            {
                nextSpawn = Time.time + Mathf.Max(.35f, 1.2f - Floor * .05f);
                var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
                StartCoroutine(Spawn(prefab, SpawnPoint(), Random.value < Mathf.Min(.35f, .08f + Floor * .02f), false));
            }
        }

        private void EnemyDied(EnemyHealth enemy)
        {
            if (enemy == null || !alive.Contains(enemy) && enemy != guardian)
                return;
            if (enemy == guardian)
            {
                FloorCleared();
                return;
            }
            kills++;
            if (!guardianSpawned && kills >= goal)
            {
                guardianSpawned = true;
                Banner("<color=#FF6A60>RIFT GUARDIAN</color>", 2.5f);
                Sfx.Play(SfxId.Summon, 1f);
                // Bombaš bi se kao čuvar sam razneo, pa čuvar nikad nije bombaš.
                var candidates = enemyPrefabs.Where(prefab => prefab.GetComponent<BomberAI>() == null).ToArray();
                StartCoroutine(Spawn(candidates[Random.Range(0, candidates.Length)], SpawnPoint(), false, true));
            }
        }

        private void FloorCleared()
        {
            floorDone = true;
            foreach (var enemy in alive.ToArray())
                if (enemy != null && !enemy.IsDead)
                    enemy.Kill();
            Sfx.Play(SfxId.LevelUp, .9f);
            // Nagrade rastu sa spratom i Torment stepenom.
            Vector2 at = player.transform.position + Vector3.up * 1.5f;
            int itemLevel = EnemyLevel + Torment.ItemLevelBonus + Floor * 2;
            for (int i = 0; i < 2; i++)
                LootDrop.SpawnItem(ItemCatalog.CreateGear(itemLevel, (1.5f + Floor * .15f) * Torment.Luck), at + Random.insideUnitCircle);
            LootDrop.SpawnItem(ItemCatalog.CreateRandomRune(itemLevel), at + Vector2.left);
            if (Random.value < .12f + .03f * Floor + .05f * Torment.Active)
                UniqueItems.DropRandom(at + Vector2.right);
            equipment.AddGold(Mathf.RoundToInt((60 + Floor * 35) * Torment.Rewards));
            player.RestoreHealth(Mathf.Min(player.MaxHealth, player.CurrentHealth + player.MaxHealth * .3f));
            Record();
            StartCoroutine(AskNext());
        }

        private IEnumerator AskNext()
        {
            // Malo vremena da se pokupi plen pre izbora.
            yield return new WaitForSeconds(4f);
            if (player.IsDead)
                yield break;
            MessagePanel.Show("FLOOR " + Floor + " CLEARED",
                "The rift trembles. Deeper floors are stronger and pay better.\n\nYour best floor: " + BestFloor + "    " + Torment.Name(Torment.Level),
                ("DESCEND", StartFloor),
                ("RETURN TO CAMP", () =>
                {
                    var save = FindFirstObjectByType<SaveSystem>();
                    if (save != null)
                        save.TravelTo(GameLevels.CampScene);
                }));
        }

        private void Record()
        {
            Leaderboard.RecordWave(Floor, RecordKey);
        }

        // Smrt u Riftu: plen ostaje, lik se budi u kampu.
        private void PlayerDied()
        {
            if (recorded)
                return;
            recorded = true;
            var save = FindFirstObjectByType<SaveSystem>();
            if (save != null)
                save.SaveForCamp();
        }

        public static string GameOverLine() => $"<color=#FF8A80>Rift floor reached: {Floor}</color>   Best: {BestFloor}\nYour loot is safe: you wake up in the camp.";

        private void Banner(string text, float seconds)
        {
            bannerText.text = text;
            bannerUntil = Time.time + seconds;
        }

        // ---------- Stvaranje protivnika ----------

        private Vector2 SpawnPoint()
        {
            Vector2 center = player.transform.position;
            for (int attempt = 0; attempt < 40; attempt++)
            {
                var point = new Vector2(Random.Range(arenaMin.x + 1.5f, arenaMax.x - 1.5f), Random.Range(arenaMin.y + 1.5f, arenaMax.y - 1.5f));
                if (Vector2.Distance(point, center) > 6f && Physics2D.OverlapCircle(point, .6f, 1 << 9) == null)
                    return point;
            }
            return (arenaMin + arenaMax) / 2f;
        }

        private IEnumerator Spawn(GameObject prefab, Vector2 position, bool elite, bool isGuardian)
        {
            var color = isGuardian ? new Color(1f, .3f, .3f, .85f) : elite ? new Color(1f, .85f, .3f, .7f) : new Color(.9f, .4f, .45f, .6f);
            var ring = RuntimeSprites.Spawn("Spawn Mark", RuntimeSprites.Ring, position, color, isGuardian ? 2.6f : 1.3f, 3);
            yield return new WaitForSeconds(isGuardian ? 1.5f : .8f);
            Destroy(ring.gameObject);
            if (player == null || player.IsDead || floorDone)
                yield break;
            var holder = new GameObject("Spawn Holder");
            holder.SetActive(false);
            var enemy = Instantiate(prefab, position, Quaternion.identity, holder.transform);
            var level = enemy.GetComponent<EnemyLevel>();
            if (level == null)
                level = enemy.AddComponent<EnemyLevel>();
            level.SetLevel(EnemyLevel);
            if (isGuardian)
                level.MakeGuardian();
            else if (elite)
                level.MakeElite();
            enemy.transform.SetParent(enemyRoot, true);
            Destroy(holder);
            var health = enemy.GetComponent<EnemyHealth>();
            if (isGuardian)
            {
                // Čuvar je veći i ima četiri puta više zdravlja od šampiona.
                enemy.transform.localScale *= 1.6f;
                enemy.name = "Rift Guardian";
                guardian = health;
            }
            health.ResetToMax();
            if (enemy.TryGetComponent<MeleeEnemyAI>(out var melee))
                melee.SetHunting();
            if (enemy.TryGetComponent<DarkArcherAI>(out var archer))
                archer.SetHunting();
            if (!isGuardian)
                EnemyVariants.Roll(health, EndlessDirector.SceneName);
            else
            {
                EnemyVariants.Apply(health, (EnemyVariant)Random.Range(0, 4));
                if (health.TryGetComponent<HealthFeedback>(out var feedback))
                    feedback.AddTitlePrefix("GUARDIAN ");
            }
            alive.Add(health);
            CombatFeedback.Burst(position, color, isGuardian ? 20 : 6, 2.5f);
        }
    }
}
