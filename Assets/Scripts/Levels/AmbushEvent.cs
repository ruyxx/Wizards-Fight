// Zaseda: kad igrač uđe u sobu, protivnici se pojavljuju u talasima; posle poslednjeg talasa ostaje nagrada.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AWizardsFight
{
    public sealed class AmbushEvent : MonoBehaviour
    {
        [SerializeField]
        private string ambushId = "ambush";
        [SerializeField]
        private Rect area = new Rect(0, 0, 10, 8);
        [SerializeField]
        private EnemyHealth[] enemyPool = new EnemyHealth[0];
        [SerializeField, Range(1, 100)]
        private int enemyLevel = 20;
        [SerializeField, Min(1)]
        private int waves = 2, enemiesPerWave = 4;
        [SerializeField]
        private LayerMask blockingLayers = 1 << 9;
        private readonly List<EnemyHealth> alive = new List<EnemyHealth>();
        private PlayerHealth player;
        private bool running;
        private int wave;

        private string Flag => "ambush_" + ambushId;
        public bool Completed => SaveSystem.HasFlag(Flag);
        public bool Running => running;

        private void Start() => player = FindFirstObjectByType<PlayerHealth>();

        private void Update()
        {
            if (player == null || player.IsDead || Completed || enemyPool.Length == 0)
                return;
            if (!running)
            {
                if (area.Contains(player.transform.position))
                    StartCoroutine(Run());
                return;
            }
        }

        private IEnumerator Run()
        {
            running = true;
            CombatFeedback.Popup("AMBUSH!", player.transform.position + Vector3.up, new Color(1f, .35f, .3f), 6f, 1.6f);
            CameraFollow.Shake(.25f, .3f);
            Sfx.Play(SfxId.Summon);
            for (wave = 1; wave <= waves; wave++)
            {
                yield return new WaitForSeconds(wave == 1 ? .8f : 1.2f);
                var spots = new List<Vector2>();
                for (int i = 0; i < enemiesPerWave + wave - 1; i++)
                    spots.Add(FindSpot());
                // Mesta pojavljivanja prvo zasvetle, pa se tek onda pojave protivnici.
                foreach (var spot in spots)
                {
                    var flash = RuntimeSprites.Spawn("Ambush Warning", RuntimeSprites.Glow, spot, new Color(1f, .3f, .3f, .8f), 2f, 18);
                    flash.gameObject.AddComponent<FadeOut>().Play(.8f, Vector2.zero);
                }

                yield return new WaitForSeconds(.7f);
                foreach (var spot in spots)
                {
                    var enemy = Instantiate(enemyPool[Random.Range(0, enemyPool.Length)], spot, Quaternion.identity);
                    var level = enemy.Level != null ? enemy.Level : enemy.gameObject.AddComponent<EnemyLevel>();
                    level.SetLevel(enemyLevel + (wave - 1) * 3);
                    enemy.ResetToMax();
                    alive.Add(enemy);
                }

                CombatFeedback.Popup("WAVE " + wave + " / " + waves, player.transform.position + Vector3.up * 1.4f, new Color(1f, .6f, .4f), 4.5f, 1.2f);
                while (true)
                {
                    alive.RemoveAll(enemy => enemy == null || enemy.IsDead);
                    if (alive.Count == 0)
                        break;
                    yield return new WaitForSeconds(.25f);
                }
            }

            running = false;
            SaveSystem.SetFlag(Flag);
            Vector2 center = area.center;
            LootDrop.SpawnItem(ItemCatalog.CreateGear(enemyLevel, 2.5f), center);
            LootDrop.SpawnItem(ItemCatalog.CreateRandomRune(enemyLevel), center);
            LootDrop.SpawnGold(Mathf.RoundToInt(40 * (1f + enemyLevel / 25f)), center);
            CombatFeedback.Popup("AMBUSH SURVIVED!", player.transform.position + Vector3.up, new Color(.6f, 1f, .6f), 5f, 1.8f);
            Sfx.Play(SfxId.LevelUp);
            FindFirstObjectByType<SaveSystem>()?.Save();
        }

        // Slobodno mesto u sobi, ne preblizu igraču i ne u zidu.
        private Vector2 FindSpot()
        {
            for (int attempt = 0; attempt < 40; attempt++)
            {
                var spot = new Vector2(Random.Range(area.xMin + 1.2f, area.xMax - 1.2f), Random.Range(area.yMin + 1.2f, area.yMax - 1.2f));
                if (player != null && Vector2.Distance(spot, player.transform.position) < 3f)
                    continue;
                if (Physics2D.OverlapCircle(spot, .5f, blockingLayers) != null)
                    continue;
                return spot;
            }
            return area.center;
        }
    }
}
