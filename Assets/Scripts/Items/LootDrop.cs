// Plen na podu: zlato, predmet ili runa. Igrač ga kupi prolaskom preko njega.
using UnityEngine;

namespace AWizardsFight
{
    public sealed class LootDrop : MonoBehaviour
    {
        private const float CollectRadius = .8f;
        private Item item;
        private int gold;
        private SpriteRenderer icon, glow;
        private Vector3 basePosition;
        private float seed, nextFullWarning;
        private static PlayerEquipment player;

        public static void SpawnItem(Item item, Vector2 position)
        {
            if (item == null)
                return;
            var drop = Create(position + Random.insideUnitCircle * .5f, ItemCatalog.Icon(item), ItemCatalog.ItemColor(item));
            drop.item = item;
            if (item.IsRune)
                drop.icon.color = ItemCatalog.RuneColor(item.rune);
            // Epski i legendarni predmeti dobijaju stub svetlosti koji se vidi izdaleka (kao u Diablu).
            if (!item.IsRune && item.rarity >= WeaponRarity.Epic)
            {
                drop.AddBeam(ItemCatalog.ItemColor(item));
                Sfx.Play(item.rarity == WeaponRarity.Legendary ? SfxId.LevelUp : SfxId.Summon, .45f);
            }
        }

        public static void SpawnGold(int amount, Vector2 position)
        {
            if (amount <= 0)
                return;
            var drop = Create(position + Random.insideUnitCircle * .4f, ItemCatalog.GoldIcon(), new Color(1f, .85f, .3f));
            drop.gold = amount;
            drop.transform.localScale = Vector3.one * .6f;
        }

        private SpriteRenderer beam, beamCore;

        // Ljubimac donosi plen liku.
        public void MoveToward(Vector3 target, float step) => basePosition = Vector3.MoveTowards(basePosition, target, step);

        private void AddBeam(Color color)
        {
            beam = RuntimeSprites.Spawn("Beam", RuntimeSprites.Glow, transform.position + Vector3.up * 2.6f, new Color(color.r, color.g, color.b, .35f), 1f, 6);
            beam.transform.SetParent(transform, true);
            beam.transform.localScale = new Vector3(.9f, 6.5f, 1f);
            beamCore = RuntimeSprites.Spawn("Beam Core", RuntimeSprites.Square, transform.position + Vector3.up * 2.6f, new Color(Mathf.Lerp(color.r, 1f, .5f), Mathf.Lerp(color.g, 1f, .5f), Mathf.Lerp(color.b, 1f, .5f), .45f), 1f, 6);
            beamCore.transform.SetParent(transform, true);
            beamCore.transform.localScale = new Vector3(.07f, 5.2f, 1f);
        }

        private static LootDrop Create(Vector2 position, Sprite sprite, Color glowColor)
        {
            var root = new GameObject("Loot");
            root.transform.position = position;
            var drop = root.AddComponent<LootDrop>();
            drop.glow = RuntimeSprites.Spawn("Glow", RuntimeSprites.Glow, position, new Color(glowColor.r, glowColor.g, glowColor.b, .55f), 1.4f, 7);
            drop.glow.transform.SetParent(root.transform, true);
            drop.icon = RuntimeSprites.Spawn("Icon", sprite != null ? sprite : RuntimeSprites.Disc, position, Color.white, 1f, 8);
            drop.icon.transform.SetParent(root.transform, true);
            drop.icon.transform.localScale = Vector3.one * (sprite != null ? .8f : .3f);
            return drop;
        }

        private void Start()
        {
            if (player == null)
                player = FindFirstObjectByType<PlayerEquipment>();
            basePosition = transform.position;
            seed = Random.value * 6f;
        }

        private void Update()
        {
            transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time * 3f + seed) * .07f);
            if (beam != null)
            {
                float pulse = .75f + .25f * Mathf.Sin(Time.time * 4f + seed);
                var color = beam.color;
                color.a = .35f * pulse;
                beam.color = color;
            }
            if (player == null || Vector2.Distance(player.transform.position, basePosition) > CollectRadius)
                return;
            var health = player.GetComponent<PlayerHealth>();
            if (health != null && health.IsDead)
                return;
            if (gold > 0)
            {
                // Pojačanje opreme „Gold Find” povećava pokupljeno zlato.
                int amount = Mathf.RoundToInt(gold * player.Multiplier(StatType.GoldFind) * Torment.Rewards * LevelModifiers.Rewards * (player.HasUnique("seeker") ? 2f : 1f));
                player.AddGold(amount);
                CombatFeedback.Popup("+" + amount + " GOLD", basePosition, new Color(1f, .85f, .3f), 3f, .7f);
                Sfx.Play(SfxId.Pickup, .5f);
                Destroy(gameObject);
                return;
            }

            if (!player.AddItem(item))
            {
                if (Time.time >= nextFullWarning)
                {
                    nextFullWarning = Time.time + 2f;
                    CombatFeedback.Popup("BAG FULL", basePosition, new Color(1f, .4f, .4f), 3.2f, .8f);
                }
                return;
            }

            CombatFeedback.Popup(item.name.ToUpperInvariant(), basePosition, ItemCatalog.ItemColor(item), 3.2f, 1.3f);
            Sfx.Play(SfxId.Pickup);
            Destroy(gameObject);
        }
    }
}
