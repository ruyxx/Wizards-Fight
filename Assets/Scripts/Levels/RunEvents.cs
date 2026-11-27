// Svaki ulazak u nivo priče je drugačiji (po uzoru na Diablo): nivo dobija nasumičnu osobinu (npr. Krvavi
// mesec: jači protivnici, više zlata), a po podu se nasumično pojave svetilišta moći, ukleti kovčeg sa talasom
// protivnika i Vrag blaga koji beži i ispušta mnogo plena ako ga stigneš.
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace AWizardsFight
{
    public sealed class LevelAffix
    {
        public readonly string Name, Description;
        public readonly Color Color;
        public readonly System.Action Apply;

        public LevelAffix(string name, string description, Color color, System.Action apply)
        {
            Name = name;
            Description = description;
            Color = color;
            Apply = apply;
        }
    }

    public static class RunEvents
    {
        public static readonly LevelAffix[] Affixes =
        {
            new LevelAffix("BLOOD MOON", "Enemies hit 25% harder. +50% gold and experience.", new Color(1f, .35f, .35f),
                () => { LevelModifiers.EnemyDamage *= 1.25f; LevelModifiers.Rewards *= 1.5f; }),
            new LevelAffix("FRENZY", "Enemies move 25% faster. +35% gold and experience.", new Color(1f, .65f, .3f),
                () => { LevelModifiers.EnemySpeed *= 1.25f; LevelModifiers.Rewards *= 1.35f; }),
            new LevelAffix("IRON HIDE", "Enemies have 40% more health. Much better item drops.", new Color(.7f, .75f, .85f),
                () => { LevelModifiers.EnemyHealth *= 1.4f; LevelModifiers.Luck *= 1.7f; }),
            new LevelAffix("ARCANE SURGE", "Your spells deal 25% more damage.", new Color(.7f, .55f, 1f),
                () => LevelModifiers.PlayerDamage *= 1.25f),
            new LevelAffix("GLASS CANNON", "You and the enemies deal 40% more damage. +25% rewards.", new Color(1f, .5f, .8f),
                () => { LevelModifiers.PlayerDamage *= 1.4f; LevelModifiers.EnemyDamage *= 1.4f; LevelModifiers.Rewards *= 1.25f; }),
            new LevelAffix("TREASURE FEVER", "Better loot, and treasure imps roam the halls.", new Color(1f, .85f, .3f),
                () => LevelModifiers.Luck *= 1.5f),
        };

        public static LevelAffix Current { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init() => SceneManager.sceneLoaded += (scene, mode) => Roll(scene);

        private static void Roll(Scene scene)
        {
            Current = null;
            if (GameLevels.ByScene(scene.name) == null || FindPlayer() == null)
                return;
            // Tri od četiri prolaza imaju posebnu osobinu.
            if (Random.value < .75f)
            {
                Current = Affixes[Random.Range(0, Affixes.Length)];
                Current.Apply();
                // Već stvoreni protivnici kreću od novog maksimuma zdravlja.
                foreach (var enemy in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
                    enemy.ResetToMax();
            }
            new GameObject("Run Events").AddComponent<RunEventSpawner>();
        }

        private static PlayerHealth FindPlayer() => Object.FindFirstObjectByType<PlayerHealth>();
    }

    // Raspoređuje nasumične događaje po podu nivoa, daleko od početka i od zidova.
    public sealed class RunEventSpawner : MonoBehaviour
    {
        private static readonly LayerMask Blocking = (1 << 9) | (1 << 11);
        private readonly List<Vector2> used = new List<Vector2>();
        private TextMeshProUGUI banner;
        private float bannerUntil;

        private IEnumerator Start()
        {
            // Sačekaj da se scena i lik postave (nastavak sa tačke čuvanja pomera lika).
            yield return null;
            var player = FindFirstObjectByType<PlayerHealth>();
            if (player == null)
                yield break;
            var points = FloorPoints(player.transform.position);
            var templates = FindObjectsByType<EnemyHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(enemy => enemy.GetComponent<MeleeEnemyAI>() != null && enemy.GetComponent<BomberAI>() == null && enemy.GetComponent<IBossBehaviour>() == null && !enemy.IsDead)
                .ToList();
            // Protivnik iz istog dela nivoa, da bi događaj bio iste težine kao okolina.
            GameObject TemplateNear(Vector2 point) => templates.Count == 0 ? null
                : templates.OrderBy(enemy => Vector2.Distance(enemy.transform.position, point)).First().gameObject;

            int shrines = Random.Range(2, 4);
            for (int i = 0; i < shrines; i++)
                if (TryTake(points, out var point))
                    PowerShrine.Create(point, (PowerShrine.Kind)Random.Range(0, 4));
            if (templates.Count > 0 && Random.value < .7f && TryTake(points, out var chestPoint))
                CursedChest.Create(chestPoint, TemplateNear(chestPoint));
            bool fever = RunEvents.Current != null && RunEvents.Current.Name == "TREASURE FEVER";
            int imps = fever ? 2 : Random.value < .5f ? 1 : 0;
            for (int i = 0; i < imps && templates.Count > 0; i++)
                if (TryTake(points, out var impPoint))
                    TreasureImp.Create(impPoint, TemplateNear(impPoint));

            if (RunEvents.Current != null)
                ShowBanner(RunEvents.Current);
        }

        private void Update()
        {
            if (banner != null)
                banner.alpha = Mathf.Clamp01((bannerUntil - Time.time) * 1.2f);
        }

        private void ShowBanner(LevelAffix affix)
        {
            var menus = FindFirstObjectByType<GameMenus>();
            if (menus == null)
                return;
            string color = ColorUtility.ToHtmlStringRGB(affix.Color);
            banner = UiFactory.Text("Affix Banner", menus.transform, $"<color=#{color}>{affix.Name}</color>\n<size=50%>{affix.Description}</size>", 64, new Vector2(0, 230), new Vector2(1500, 200), Color.white);
            banner.fontStyle = FontStyles.Bold;
            banner.outlineWidth = .2f;
            banner.rectTransform.SetAsFirstSibling();
            bannerUntil = Time.time + 5f;
        }

        // Ćelije poda bez zida u blizini, dalje od 9 jedinica od lika i od bosa.
        private static List<Vector2> FloorPoints(Vector2 start)
        {
            var result = new List<Vector2>();
            var floor = FindObjectsByType<Tilemap>(FindObjectsSortMode.None).FirstOrDefault(map => map.name == "Floor");
            if (floor == null)
                return result;
            var boss = FindFirstObjectByType<BossEncounter>();
            foreach (var cell in floor.cellBounds.allPositionsWithin)
            {
                if (!floor.HasTile(cell))
                    continue;
                Vector2 point = floor.GetCellCenterWorld(cell);
                if (Vector2.Distance(point, start) < 9f || (boss != null && Vector2.Distance(point, boss.transform.position) < 12f))
                    continue;
                if (Physics2D.OverlapCircle(point, 1.1f, Blocking) == null)
                    result.Add(point);
            }
            return result;
        }

        // Nasumična tačka koja nije preblizu već postavljenom događaju.
        private bool TryTake(List<Vector2> points, out Vector2 point)
        {
            for (int attempt = 0; attempt < 60 && points.Count > 0; attempt++)
            {
                point = points[Random.Range(0, points.Count)];
                var candidate = point;
                if (used.All(other => Vector2.Distance(other, candidate) > 8f))
                {
                    used.Add(point);
                    return true;
                }
            }
            point = default;
            return false;
        }
    }

    // Svetilište moći: kratak, snažan blagoslov (kao Pylon u Diablu). Svako se koristi jednom.
    public sealed class PowerShrine : MonoBehaviour, IInteractable
    {
        public enum Kind { Frenzy, Fortune, Protection, Arcana }
        private Kind kind;
        private bool used;
        private SpriteRenderer glow, core;

        private static readonly string[] Names = { "SHRINE OF FRENZY", "SHRINE OF FORTUNE", "SHRINE OF PROTECTION", "SHRINE OF ARCANA" };
        private static readonly Color[] Colors = { new Color(1f, .45f, .3f), new Color(1f, .85f, .3f), new Color(.45f, .8f, 1f), new Color(.75f, .55f, 1f) };

        public string Prompt => "E - " + Names[(int)kind];

        public static void Create(Vector2 position, Kind kind)
        {
            var root = new GameObject("Power Shrine " + kind);
            root.transform.position = position;
            root.layer = 9;
            var trigger = root.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = .7f;
            var shrine = root.AddComponent<PowerShrine>();
            shrine.kind = kind;
            var color = Colors[(int)kind];
            shrine.glow = RuntimeSprites.Spawn("Glow", RuntimeSprites.Glow, position, new Color(color.r, color.g, color.b, .55f), 3f, -8);
            shrine.glow.transform.SetParent(root.transform, true);
            shrine.glow.gameObject.AddComponent<TorchFlicker>();
            var pillar = RuntimeSprites.Spawn("Pillar", RuntimeSprites.Square, position + Vector2.down * .1f, new Color(.5f, .46f, .6f), 1f, 3);
            pillar.transform.localScale = new Vector3(.55f, .9f, 1f);
            pillar.transform.SetParent(root.transform, true);
            shrine.core = RuntimeSprites.Spawn("Orb", RuntimeSprites.Disc, position + Vector2.up * .55f, color, .55f, 4);
            shrine.core.transform.SetParent(root.transform, true);
        }

        private void Update()
        {
            if (core != null && !used)
                core.transform.localPosition = new Vector3(0f, .55f + .08f * Mathf.Sin(Time.time * 3f), 0f);
        }

        public bool CanInteract(PlayerInteractor player) => !used && player != null && player.CanReach(this);

        public bool Interact(PlayerInteractor player)
        {
            if (!CanInteract(player))
                return false;
            used = true;
            var buffs = player.GetComponent<PlayerBuffs>();
            const float seconds = 30f;
            string text;
            switch (kind)
            {
                case Kind.Frenzy:
                    buffs.Add("Frenzy", StatType.CastSpeed, 50f, seconds);
                    buffs.Add("Frenzy", StatType.MoveSpeed, 20f, seconds);
                    text = "FRENZY! +50% cast speed";
                    break;
                case Kind.Fortune:
                    buffs.Add("Fortune", StatType.GoldFind, 100f, seconds * 2f);
                    buffs.Add("Fortune", StatType.ExperienceGain, 50f, seconds * 2f);
                    text = "FORTUNE! +100% gold, +50% XP";
                    break;
                case Kind.Protection:
                    buffs.Add("Protection", StatType.Armor, 150f, seconds);
                    player.GetComponent<PlayerHealth>().Heal(9999f);
                    text = "PROTECTION! +150 armor, full health";
                    break;
                default:
                    buffs.Add("Arcana", StatType.SpellDamage, 40f, seconds);
                    player.GetComponent<PlayerMana>().Restore(9999f);
                    text = "ARCANA! +40% spell damage, full mana";
                    break;
            }
            var color = Colors[(int)kind];
            CombatFeedback.Popup(text, transform.position + Vector3.up, color, 4.5f, 2f);
            CombatFeedback.Burst(transform.position, color, 22, 5f);
            var ring = RuntimeSprites.Spawn("Shrine Ring", RuntimeSprites.Ring, transform.position, color, .5f, 15);
            ring.gameObject.AddComponent<ExpandingRing>().Play(6f, .4f);
            Sfx.Play(SfxId.LevelUp, .8f);
            glow.color = new Color(.4f, .4f, .45f, .2f);
            core.color = new Color(.4f, .4f, .45f, .6f);
            return true;
        }
    }

    // Ukleti kovčeg: otvaranje priziva tri talasa ojačanih protivnika. Ko preživi, dobija bogat plen.
    public sealed class CursedChest : MonoBehaviour, IInteractable
    {
        private GameObject template;
        private bool started, finished;
        private readonly List<EnemyHealth> alive = new List<EnemyHealth>();
        private SpriteRenderer glow;

        public string Prompt => "E - OPEN THE CURSED CHEST (AMBUSH!)";

        public static void Create(Vector2 position, GameObject template)
        {
            var root = new GameObject("Cursed Chest");
            root.transform.position = position;
            root.layer = 9;
            var trigger = root.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = .7f;
            var chest = root.AddComponent<CursedChest>();
            chest.template = template;
            var source = FindFirstObjectByType<LootChest>();
            SpriteRenderer body = null;
            if (source != null && source.GetComponentInChildren<SpriteRenderer>() is SpriteRenderer original && original != null)
                body = RuntimeSprites.Spawn("Body", original.sprite, position, new Color(.75f, .45f, .9f), 1f, 3);
            else
                body = RuntimeSprites.Spawn("Body", RuntimeSprites.Square, position, new Color(.4f, .2f, .5f), .8f, 3);
            if (source != null)
                body.transform.localScale = source.GetComponentInChildren<SpriteRenderer>().transform.lossyScale;
            body.transform.SetParent(root.transform, true);
            chest.glow = RuntimeSprites.Spawn("Curse", RuntimeSprites.Glow, position, new Color(.6f, .2f, .9f, .6f), 2.8f, -8);
            chest.glow.transform.SetParent(root.transform, true);
            chest.glow.gameObject.AddComponent<TorchFlicker>();
        }

        public bool CanInteract(PlayerInteractor player) => !started && player != null && player.CanReach(this);

        public bool Interact(PlayerInteractor player)
        {
            if (!CanInteract(player))
                return false;
            started = true;
            StartCoroutine(Waves(player.GetComponent<PlayerHealth>()));
            return true;
        }

        private IEnumerator Waves(PlayerHealth player)
        {
            CombatFeedback.Popup("THE CHEST WAS CURSED!", transform.position + Vector3.up, new Color(.8f, .4f, 1f), 4.5f, 2f);
            Sfx.Play(SfxId.Summon, 1f);
            CameraFollow.Shake(.2f, .3f);
            for (int wave = 0; wave < 3; wave++)
            {
                int count = 3 + wave;
                for (int i = 0; i < count; i++)
                {
                    Vector2 point = (Vector2)transform.position + Random.insideUnitCircle.normalized * Random.Range(2.5f, 4f);
                    if (Physics2D.OverlapCircle(point, .5f, 1 << 9) != null)
                        point = (Vector2)transform.position + Random.insideUnitCircle;
                    var enemy = SpawnCopy(template, point, i == 0 && wave == 2);
                    if (enemy != null)
                        alive.Add(enemy);
                }
                // Sledeći talas kreće kad padne ovaj (ili kad igrač umre).
                while (alive.Any(enemy => enemy != null && !enemy.IsDead))
                {
                    if (player == null || player.IsDead)
                        yield break;
                    yield return new WaitForSeconds(.3f);
                }
                alive.Clear();
                yield return new WaitForSeconds(1f);
            }

            finished = true;
            glow.color = new Color(1f, .85f, .3f, .6f);
            int itemLevel = EnemyLevelAt();
            for (int i = 0; i < 3; i++)
                LootDrop.SpawnItem(ItemCatalog.CreateGear(itemLevel + Torment.ItemLevelBonus, 2.2f * Torment.Luck * LevelModifiers.Luck), (Vector2)transform.position + Random.insideUnitCircle * 1.2f);
            LootDrop.SpawnGold(Mathf.RoundToInt((40 + itemLevel * 3) * Torment.Rewards * LevelModifiers.Rewards), (Vector2)transform.position + Vector2.down);
            if (Random.value < .25f)
                UniqueItems.DropRandom((Vector2)transform.position + Vector2.up);
            CombatFeedback.Popup("CURSE LIFTED!", transform.position + Vector3.up, new Color(1f, .85f, .3f), 4.5f, 2f);
            Sfx.Play(SfxId.LevelUp, .9f);
        }

        private int EnemyLevelAt()
        {
            var level = template != null ? template.GetComponent<EnemyLevel>() : null;
            return Mathf.Max(5, level != null ? level.Level + 4 : 10);
        }

        // Kopija protivnika iz nivoa, odmah u lovu na igrača.
        public static EnemyHealth SpawnCopy(GameObject template, Vector2 position, bool elite)
        {
            if (template == null)
                return null;
            var holder = new GameObject("Spawn Holder");
            holder.SetActive(false);
            var copy = Instantiate(template, position, Quaternion.identity, holder.transform);
            copy.transform.SetParent(null, true);
            Destroy(holder);
            copy.SetActive(true);
            var health = copy.GetComponent<EnemyHealth>();
            var level = copy.GetComponent<EnemyLevel>();
            if (level != null && elite)
                level.MakeElite();
            health.ResetToMax();
            if (copy.TryGetComponent<MeleeEnemyAI>(out var melee))
                melee.SetHunting();
            var ring = RuntimeSprites.Spawn("Spawn Mark", RuntimeSprites.Ring, position, new Color(.8f, .4f, 1f, .8f), 1.3f, 3);
            ring.gameObject.AddComponent<FadeOut>().Play(.6f, Vector2.zero);
            return health;
        }
    }

    // Vrag blaga: beži od igrača i posle 20 sekundi nestane kroz portal. Ako ga stigneš, ispadne gomila plena.
    public sealed class TreasureImp : MonoBehaviour
    {
        private const float Lifetime = 20f, Speed = 3.4f;
        private Rigidbody2D body;
        private Transform player;
        private EnemyHealth health;
        private float despawnAt;
        private bool noticed;
        private Vector2 wander;

        public static void Create(Vector2 position, GameObject template)
        {
            var copy = CursedChest.SpawnCopy(template, position, false);
            if (copy == null)
                return;
            copy.gameObject.name = "Treasure Imp";
            foreach (var ai in copy.GetComponents<MonoBehaviour>())
                if (ai is MeleeEnemyAI || ai is DarkArcherAI)
                    ai.enabled = false;
            foreach (var sprite in copy.GetComponentsInChildren<SpriteRenderer>())
                sprite.color = new Color(1f, .85f, .3f);
            copy.transform.localScale *= .8f;
            var glow = RuntimeSprites.Spawn("Gold Glow", RuntimeSprites.Glow, position, new Color(1f, .8f, .3f, .5f), 2.2f, -5);
            glow.transform.SetParent(copy.transform, true);
            if (copy.TryGetComponent<HealthFeedback>(out var feedback))
                feedback.AddTitlePrefix("TREASURE IMP ");
            copy.gameObject.AddComponent<TreasureImp>();
        }

        private void Start()
        {
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<EnemyHealth>();
            var target = FindFirstObjectByType<PlayerHealth>();
            player = target != null ? target.transform : null;
            health.Died += Drop;
            despawnAt = float.PositiveInfinity;
        }

        private void FixedUpdate()
        {
            if (health == null || health.IsDead || player == null || body == null)
                return;
            Vector2 away = (Vector2)transform.position - (Vector2)player.position;
            // Mirno luta dok ga igrač ne primeti, a tada počinje bekstvo i odbrojavanje.
            if (!noticed && away.magnitude < 7f)
            {
                noticed = true;
                despawnAt = Time.time + Lifetime;
                CombatFeedback.Popup("HEHEHE!", transform.position + Vector3.up, new Color(1f, .85f, .3f), 3.5f, 1f);
            }
            if (!noticed)
            {
                if (Random.value < .02f)
                    wander = Random.insideUnitCircle;
                body.linearVelocity = wander * 1.2f;
                return;
            }
            if (Time.time >= despawnAt)
            {
                CombatFeedback.Popup("ESCAPED!", transform.position + Vector3.up, new Color(1f, .6f, .3f), 3.5f, 1.2f);
                CombatFeedback.Burst(transform.position, new Color(1f, .85f, .3f), 20, 5f);
                health.Died -= Drop;
                Destroy(gameObject);
                return;
            }
            // Beži od igrača, a kad udari u zid, skrene u stranu.
            Vector2 direction = away.normalized;
            var hit = Physics2D.Raycast(transform.position, direction, 1.2f, 1 << 9);
            if (hit.collider != null)
                direction = Vector2.Perpendicular(direction) * (Vector2.Dot(Vector2.Perpendicular(direction), hit.normal) >= 0f ? 1f : -1f);
            float slow = TryGetComponent<EnemySlow>(out var effect) ? effect.SpeedMultiplier : 1f;
            body.linearVelocity = direction * Speed * slow;
            if (Random.value < .05f)
                LootDrop.SpawnGold(Random.Range(2, 6), transform.position);
        }

        private void Drop()
        {
            Vector2 at = transform.position;
            int itemLevel = TryGetComponent<EnemyLevel>(out var level) ? level.Level + 6 : 12;
            for (int i = 0; i < 4; i++)
                LootDrop.SpawnItem(ItemCatalog.CreateGear(itemLevel + Torment.ItemLevelBonus, 2.5f * Torment.Luck * LevelModifiers.Luck), at + Random.insideUnitCircle * 1.5f);
            for (int i = 0; i < 6; i++)
                LootDrop.SpawnGold(Mathf.RoundToInt((15 + itemLevel) * Torment.Rewards * LevelModifiers.Rewards), at + Random.insideUnitCircle * 1.5f);
            if (Random.value < .35f)
                UniqueItems.DropRandom(at + Vector2.up);
            CombatFeedback.Popup("JACKPOT!", at + Vector2.up, new Color(1f, .85f, .3f), 5f, 1.6f);
        }
    }
}
