// Endless režim: poseban način igre u areni. Talasi protivnika postaju sve jači, svaki peti talas
// donosi šampiona, a posle talasa stižu zlato, oprema i oružja. Ništa se ne čuva u priči; pamti se samo najdalji talas.
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace AWizardsFight
{
    public sealed class EndlessDirector : MonoBehaviour
    {
        public const string SceneName = "Endless";

        [SerializeField]
        private GameObject[] enemyPrefabs = new GameObject[0];
        [SerializeField, Tooltip("Od kog talasa se pojavljuje odgovarajući protivnik iz enemyPrefabs.")]
        private int[] unlockWave = new int[0];
        [SerializeField]
        private WeaponCatalog catalog = null;
        [SerializeField]
        private Vector2 arenaMin = new Vector2(1, 1), arenaMax = new Vector2(35, 25);

        public static bool Active { get; private set; }
        public static int Wave { get; private set; }
        public static int BestWave => Leaderboard.Waves(DailyChallenge.Active ? DailyChallenge.Key : Leaderboard.EndlessKey).DefaultIfEmpty(0).Max();

        private readonly List<EnemyHealth> alive = new List<EnemyHealth>();
        private PlayerHealth player;
        private PlayerEquipment equipment;
        private PlayerWeapons weapons;
        private PlayerPotions potions;
        private TextMeshProUGUI waveText, bannerText;
        private bool spawning, recorded;
        private float bannerUntil;
        private Transform enemyRoot;

        private void Awake()
        {
            Active = true;
            Wave = 0;
            LevelModifiers.Reset();
            player = FindFirstObjectByType<PlayerHealth>();
            equipment = player.GetComponent<PlayerEquipment>();
            weapons = player.GetComponent<PlayerWeapons>();
            potions = player.GetComponent<PlayerPotions>();
            enemyRoot = new GameObject("Endless Enemies").transform;
        }

        private void OnEnable()
        {
            EnemyHealth.AnyDied += EnemyDied;
            if (player != null)
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
            waveText = UiFactory.Text("Wave", canvas, "", 30, new Vector2(0, 500), new Vector2(900, 50), new Color(1f, .85f, .4f));
            waveText.rectTransform.SetAsFirstSibling();
            waveText.fontStyle = FontStyles.Bold;
            bannerText = UiFactory.Text("Banner", canvas, "", 64, new Vector2(0, 180), new Vector2(1400, 200), Color.white);
            bannerText.rectTransform.SetAsFirstSibling();
            bannerText.fontStyle = FontStyles.Bold;
            bannerText.outlineWidth = .2f;
            // Dnevni izazov: isti raspored za ceo dan i dva posebna pravila.
            if (DailyChallenge.Active)
            {
                Random.InitState(DailyChallenge.Seed);
                if (DailyChallenge.Has(DailyRule.Frenzy))
                    LevelModifiers.EnemySpeed = 1.3f;
                if (DailyChallenge.Has(DailyRule.GlassCannon))
                {
                    LevelModifiers.PlayerDamage = 1.5f;
                    player.MultiplyMaximum(.5f);
                }
                Banner("DAILY CHALLENGE\n<size=45%><color=#FF9A5C>" + string.Join("  ·  ", System.Array.ConvertAll(DailyChallenge.Rules, rule => DailyChallenge.Info[rule].title)) + "</color></size>", 3.5f);
            }
            StartCoroutine(NextWave(DailyChallenge.Active ? 4f : 2f));
        }

        private void Update()
        {
            alive.RemoveAll(enemy => enemy == null || enemy.IsDead);
            if (waveText != null)
                waveText.text = Wave == 0 ? "ENDLESS  ·  GET READY" : $"WAVE {Wave}   ·   ENEMIES {alive.Count}   ·   BEST {Mathf.Max(BestWave, 0)}";
            if (bannerText != null)
            {
                float left = bannerUntil - Time.time;
                bannerText.alpha = Mathf.Clamp01(left * 1.5f);
            }
            // Nagrade se dele tek kad igra teče (ne dok je otvoren izbor pojačanja ili inventar).
            if (!spawning && Wave > 0 && alive.Count == 0 && player != null && !player.IsDead && Time.timeScale > 0f)
                WaveCleared();
        }

        private void Banner(string text, float seconds = 2.5f)
        {
            bannerText.text = text;
            bannerUntil = Time.time + seconds;
        }

        // ---------- Talasi ----------

        public static int EnemyLevelFor(int wave) => Mathf.Clamp(2 + wave * 6, 1, 100);
        public static int CountFor(int wave) => Mathf.Min(24, 3 + wave + wave / 3);

        private IEnumerator NextWave(float delay)
        {
            spawning = true;
            yield return new WaitForSeconds(delay);
            Wave++;
            // Posle 16. talasa nivo je na vrhu, pa protivnici dalje rastu kroz dodatno zdravlje i štetu.
            int extra = Mathf.Max(0, Wave - 12);
            LevelModifiers.EnemyHealth = 1f + .15f * extra;
            LevelModifiers.EnemyDamage = 1f + .08f * extra;
            bool champion = Wave % 5 == 0;
            Banner(champion ? $"WAVE {Wave}\n<size=50%><color=#FF9A5C>A CHAMPION APPROACHES</color></size>" : $"WAVE {Wave}");
            Sfx.Play(champion ? SfxId.Summon : SfxId.Pickup, champion ? 1f : .6f);

            var pool = Enumerable.Range(0, enemyPrefabs.Length).Where(i => enemyPrefabs[i] != null && Wave >= (i < unlockWave.Length ? unlockWave[i] : 1)).ToList();
            int count = CountFor(Wave);
            float eliteChance = Mathf.Min(.4f, Wave * .03f);
            if (DailyChallenge.Has(DailyRule.Horde))
                count = Mathf.RoundToInt(count * 1.5f);
            if (DailyChallenge.Has(DailyRule.EliteRush))
                eliteChance = Mathf.Min(.8f, eliteChance * 3f);
            if (DailyChallenge.Has(DailyRule.Juggernauts))
                LevelModifiers.EnemyHealth *= 1.4f;
            for (int i = 0; i < count; i++)
            {
                var prefab = enemyPrefabs[pool[Random.Range(0, pool.Count)]];
                StartCoroutine(SpawnAfter(prefab, SpawnPoint(), Random.value < eliteChance, false, i * .25f));
            }
            if (champion)
                StartCoroutine(SpawnAfter(enemyPrefabs[pool[Random.Range(0, pool.Count)]], SpawnPoint(), false, true, .5f));
            yield return new WaitForSeconds(count * .25f + 1.2f);
            spawning = false;
        }

        private Vector2 SpawnPoint()
        {
            Vector2 center = player != null ? (Vector2)player.transform.position : (arenaMin + arenaMax) / 2f;
            for (int attempt = 0; attempt < 40; attempt++)
            {
                var point = new Vector2(Random.Range(arenaMin.x + 1.5f, arenaMax.x - 1.5f), Random.Range(arenaMin.y + 1.5f, arenaMax.y - 1.5f));
                if (Vector2.Distance(point, center) > 6f && Physics2D.OverlapCircle(point, .6f, 1 << 9) == null)
                    return point;
            }
            return (arenaMin + arenaMax) / 2f;
        }

        // Kratko upozorenje (krug na podu), pa se protivnik pojavi.
        private IEnumerator SpawnAfter(GameObject prefab, Vector2 position, bool elite, bool champion, float delay)
        {
            yield return new WaitForSeconds(delay);
            var color = champion ? new Color(1f, .5f, .2f, .8f) : elite ? new Color(1f, .85f, .3f, .7f) : new Color(.7f, .45f, 1f, .7f);
            var ring = RuntimeSprites.Spawn("Spawn Mark", RuntimeSprites.Ring, position, color, champion ? 2.2f : 1.3f, 3);
            var glow = RuntimeSprites.Spawn("Spawn Glow", RuntimeSprites.Glow, position, new Color(color.r, color.g, color.b, .4f), champion ? 3f : 1.8f, 2);
            yield return new WaitForSeconds(.9f);
            Destroy(ring.gameObject);
            glow.gameObject.AddComponent<FadeOut>().Play(.4f, Vector2.zero);
            if (player == null || player.IsDead)
                yield break;
            // Protivnik se pravi neaktivan, da nivo bude podešen pre nego što izračuna svoje zdravlje.
            var holder = new GameObject("Spawn Holder");
            holder.SetActive(false);
            var enemy = Instantiate(prefab, position, Quaternion.identity, holder.transform);
            var level = enemy.GetComponent<EnemyLevel>();
            if (level == null)
                level = enemy.AddComponent<EnemyLevel>();
            level.SetLevel(EnemyLevelFor(Wave));
            if (champion)
                level.MakeChampion();
            else if (elite)
                level.MakeElite();
            enemy.transform.SetParent(enemyRoot, true);
            Destroy(holder);
            var health = enemy.GetComponent<EnemyHealth>();
            health.ResetToMax();
            enemy.GetComponent<MeleeEnemyAI>()?.SetHunting();
            enemy.GetComponent<DarkArcherAI>()?.SetHunting();
            EnemyVariants.Roll(health, SceneName);
            if (champion)
                enemy.name += " Champion";
            alive.Add(health);
            CombatFeedback.Burst(position, color, 8, 2.5f);
        }

        private void EnemyDied(EnemyHealth enemy)
        {
            if (enemy == null || !alive.Contains(enemy))
                return;
            // Malo zlata iz svakog protivnika, više iz elitnih.
            var level = enemy.GetComponent<EnemyLevel>();
            int gold = Mathf.RoundToInt((3 + Wave * 1.5f) * (level != null && level.IsElite ? 3f : 1f));
            LootDrop.SpawnGold(gold, enemy.transform.position);
        }

        // ---------- Nagrade ----------

        private void WaveCleared()
        {
            spawning = true;
            Sfx.Play(SfxId.LevelUp, .7f);
            var reward = new List<string>();
            player.RestoreHealth(Mathf.Min(player.MaxHealth, player.CurrentHealth + player.MaxHealth * .25f));
            reward.Add("+25% health");
            Vector2 drop = player.transform.position + Vector3.up * 1.5f;
            // Svaki talas daje predmet, svaki drugi napitak, svaki treći runu, a svaki četvrti novo ili bolje oružje.
            LootDrop.SpawnItem(ItemCatalog.CreateGear(Mathf.Clamp(Wave * 6, 1, 100), 1f + Wave * .08f), drop);
            reward.Add("gear");
            if (Wave % 2 == 0 && potions != null && !DailyChallenge.Has(DailyRule.NoPotions))
            {
                potions.Add(1);
                reward.Add("a potion");
            }
            if (Wave % 3 == 0)
            {
                LootDrop.SpawnItem(ItemCatalog.CreateRandomRune(Mathf.Clamp(Wave * 6, 1, 100)), drop + Vector2.right);
                reward.Add("a rune");
            }
            if (Wave % 4 == 0 && catalog != null && weapons != null)
            {
                var weapon = RewardWeapon();
                if (weapon != null && weapons.Acquire(weapon) != WeaponAcquisition.Rejected)
                {
                    reward.Add(weapon.WeaponName + " (" + weapon.Rarity + ")");
                }
            }
            Banner($"WAVE {Wave} CLEARED\n<size=45%><color=#B8F0B8>{string.Join("  ·  ", reward)}</color></size>", 3f);
            StartCoroutine(NextWave(5f));
        }

        private WeaponData RewardWeapon()
        {
            var rarity = Wave >= 16 ? WeaponRarity.Legendary : Wave >= 10 ? WeaponRarity.Epic : WeaponRarity.Rare;
            // Prvo nova vrsta oružja, a kad su sve otključane, bolja retkost postojećeg.
            var all = Enumerable.Range(0, WeaponTypes.Count).Select(i => (WeaponType)i).ToList();
            var types = all.Where(type => weapons.GetOwned(type) == null).ToList();
            if (types.Count == 0)
                types = all.Where(type => weapons.GetOwned(type).Rarity < rarity).ToList();
            if (types.Count == 0)
                return null;
            return catalog.Find(types[Random.Range(0, types.Count)], rarity);
        }

        private void PlayerDied()
        {
            if (recorded)
                return;
            recorded = true;
            Leaderboard.RecordWave(Mathf.Max(0, Wave - 1), DailyChallenge.Active ? DailyChallenge.Key : Leaderboard.EndlessKey);
        }

        public static string GameOverLine()
        {
            int reached = Mathf.Max(0, Wave - 1);
            int best = BestWave;
            return $"<color=#FFD966>Waves survived: {reached}</color>" + (reached >= best && reached > 0 ? "   <color=#FFD966>NEW BEST!</color>" : $"   Best: {best}");
        }
    }
}
