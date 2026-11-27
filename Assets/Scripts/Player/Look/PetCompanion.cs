// Ljubimac pratilac (kao u Diablu i Torchlight-u): lebdi pored lika, donosi mu plen sa poda
// i povremeno ispali malu kuglu na najbližeg protivnika. Vrsta se bira u ekranu za lika.
using System.Collections.Generic;
using UnityEngine;

namespace AWizardsFight
{
    public sealed class PetCompanion : MonoBehaviour
    {
        public static readonly (string name, Color body, Color glow)[] Kinds =
        {
            ("None", Color.clear, Color.clear),
            ("Wisp", new Color(.7f, .9f, 1f), new Color(.4f, .75f, 1f)),
            ("Ember Drake", new Color(1f, .55f, .25f), new Color(1f, .45f, .1f)),
            ("Shadow Owl", new Color(.7f, .55f, 1f), new Color(.55f, .3f, 1f))
        };

        private const float FetchRange = 7f, AttackRange = 8f, AttackCooldown = 1.4f;
        private Transform owner;
        private PlayerProgression progression;
        private SpriteRenderer body, glow;
        private Vector3 velocity;
        private float nextAttack, bob;
        private LootDrop carrying;

        public static void Spawn(Transform player)
        {
            int kind = CharacterLook.Current.pet;
            if (kind <= 0 || kind >= Kinds.Length)
                return;
            var pet = new GameObject("Pet " + Kinds[kind].name).AddComponent<PetCompanion>();
            pet.owner = player;
            pet.progression = player.GetComponent<PlayerProgression>();
            pet.transform.position = player.position + new Vector3(-.8f, .6f, 0f);
            pet.glow = RuntimeSprites.Spawn("Glow", RuntimeSprites.Glow, pet.transform.position, new Color(Kinds[kind].glow.r, Kinds[kind].glow.g, Kinds[kind].glow.b, .45f), 1.1f, 9);
            pet.glow.transform.SetParent(pet.transform, true);
            pet.body = RuntimeSprites.Spawn("Body", Sprite(kind), pet.transform.position, Color.white, 1f, 10);
            pet.body.transform.SetParent(pet.transform, true);
        }

        private static readonly Dictionary<int, Sprite> sprites = new Dictionary<int, Sprite>();

        // Mali sprajt (16x16) nacrtan u kodu: duh, zmaj ili sova.
        private static Sprite Sprite(int kind)
        {
            if (sprites.TryGetValue(kind, out var cached))
                return cached;
            string[][] shapes =
            {
                null,
                new[] { "................", "......kkkk......", ".....kbbbbk.....", "....kbwbbwbk....", "....kbkbbkbk....", "....kbbbbbbk....", "....kbbbbbbk....", ".....kbbbbk.....", "......kbbk......", ".......kbk......", "........k.......", "................", "................", "................", "................", "................" },
                new[] { "................", "..k........k....", "..kbk..kk.kbk...", "...kbkkbbkbk....", "....kbbbbbbk....", "...kbwkbbbbbk...", "...kbkkbbbbbbk..", "....kbbbbbbbbk..", ".....kbbbbbbk...", "....kbk..kbk....", "....kk....kk....", "................", "................", "................", "................", "................" },
                new[] { "................", "....k......k....", "....kk....kk....", "....kbkkkkbk....", "...kbbbbbbbbk...", "...kbwwbbwwbk...", "...kbwkbbkwbk...", "...kbbbyybbbk...", "...kbbbbbbbbk...", "....kbbbbbbk....", ".....kbbbbk.....", "......kyyk......", "................", "................", "................", "................" }
            };
            var color = Kinds[kind].body;
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                char c = shapes[kind][y][x];
                Color pixel = c == 'k' ? new Color(.09f, .07f, .14f) : c == 'b' ? color * (1.05f - .25f * y / 16f) : c == 'w' ? Color.white : c == 'y' ? new Color(1f, .8f, .3f) : Color.clear;
                pixel.a = c == '.' ? 0f : 1f;
                texture.SetPixel(x, 15 - y, pixel);
            }
            texture.Apply();
            return sprites[kind] = UnityEngine.Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(.5f, .5f), 22f);
        }

        private void Update()
        {
            if (owner == null)
            {
                Destroy(gameObject);
                return;
            }
            bob += Time.deltaTime * 3f;
            // Prati lika, osim kad nosi plen ka njemu.
            Vector3 target = owner.position + new Vector3(-.9f, .7f + Mathf.Sin(bob) * .12f, 0f);
            if (carrying == null && Time.timeScale > 0f)
                carrying = NearestLoot();
            if (carrying != null)
            {
                target = carrying.transform.position;
                if (Vector2.Distance(transform.position, target) < .4f)
                {
                    // Plen se pomera do lika, gde ga on sam pokupi.
                    carrying.MoveToward(owner.position, 7f * Time.deltaTime);
                    target = carrying.transform.position + Vector3.up * .3f;
                }
            }
            transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, carrying != null ? .12f : .25f);
            body.flipX = velocity.x < -.05f;
            if (Time.time >= nextAttack && Time.timeScale > 0f)
                Attack();
        }

        private LootDrop NearestLoot()
        {
            LootDrop best = null;
            float bestDistance = FetchRange;
            foreach (var loot in FindObjectsByType<LootDrop>(FindObjectsSortMode.None))
            {
                float distance = Vector2.Distance(loot.transform.position, owner.position);
                if (distance < bestDistance && distance > .7f)
                {
                    best = loot;
                    bestDistance = distance;
                }
            }
            return best;
        }

        private void Attack()
        {
            EnemyHealth best = null;
            float bestDistance = AttackRange;
            foreach (var enemy in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            {
                if (enemy.IsDead || enemy.IsInvulnerable)
                    continue;
                float distance = Vector2.Distance(transform.position, enemy.transform.position);
                if (distance < bestDistance && !Physics2D.Linecast(transform.position, enemy.transform.position, 1 << 9))
                {
                    best = enemy;
                    bestDistance = distance;
                }
            }
            nextAttack = Time.time + (best != null ? AttackCooldown : .3f);
            if (best == null)
                return;
            float damage = 4f + 1.2f * (progression != null ? progression.Level : 1);
            PetBolt.Fire(transform.position, best, damage, glow.color);
        }
    }

    // Mala kugla ljubimca: leti do mete i nanese štetu.
    public sealed class PetBolt : MonoBehaviour
    {
        private EnemyHealth target;
        private float damage;

        public static void Fire(Vector3 from, EnemyHealth target, float damage, Color color)
        {
            var bolt = RuntimeSprites.Spawn("Pet Bolt", RuntimeSprites.Glow, from, new Color(color.r, color.g, color.b, .95f), .45f, 12).gameObject.AddComponent<PetBolt>();
            bolt.target = target;
            bolt.damage = damage;
        }

        private void Update()
        {
            if (target == null || target.IsDead)
            {
                Destroy(gameObject);
                return;
            }
            transform.position = Vector3.MoveTowards(transform.position, target.transform.position, 11f * Time.deltaTime);
            if (Vector2.Distance(transform.position, target.transform.position) < .25f)
            {
                target.TakeDamage(damage);
                CombatFeedback.Burst(transform.position, GetComponent<SpriteRenderer>().color, 3, 1.5f);
                Destroy(gameObject);
            }
        }
    }
}
