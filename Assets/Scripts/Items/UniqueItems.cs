// Jedinstveni predmeti (kao u Diablu): svaki boss ispušta svoj predmet sa imenom i posebnim efektom.
// Efekti deluju dok je predmet obučen; proverava ih PlayerEquipment.HasUnique.
using System.Collections.Generic;
using UnityEngine;

namespace AWizardsFight
{
    public static class UniqueItems
    {
        public sealed class Definition
        {
            public string Id, Name, Effect;
            public GearType Gear;
            public bool World;
            public (StatType stat, float value)[] Stats;
        }

        public static readonly Definition[] All =
        {
            new Definition { Id = "crown", Name = "Crown of the Dark Sorcerer", Gear = GearType.Helmet, Effect = "+15% chance that a spell is cast twice.",
                Stats = new[] { (StatType.SpellDamage, 14f), (StatType.MaxMana, 30f), (StatType.Armor, 12f) } },
            new Definition { Id = "golemheart", Name = "Heart of the Magma Golem", Gear = GearType.Chest, Effect = "Your dash leaves a trail of burning lava.",
                Stats = new[] { (StatType.Armor, 40f), (StatType.FireDamage, 20f), (StatType.MaxHealth, 30f) } },
            new Definition { Id = "phylactery", Name = "Phylactery of the Frost Lich", Gear = GearType.Amulet, Effect = "Killing a chilled enemy restores 8 mana. Chills last longer.",
                Stats = new[] { (StatType.IceDamage, 25f), (StatType.ManaRegen, 20f), (StatType.SlowDuration, 30f) } },
            new Definition { Id = "shadowband", Name = "Band of the Shadow King", Gear = GearType.Ring, Effect = "Every kill heals you for 3 health.",
                Stats = new[] { (StatType.CritChance, 10f), (StatType.CritDamage, 40f), (StatType.MoveSpeed, 8f) } },
            // Svetski jedinstveni predmeti: padaju retko bilo gde (češće na Torment težini).
            new Definition { Id = "emberheart", Name = "Emberheart Gloves", Gear = GearType.Gloves, Effect = "Fire Staff hits always ignite.", World = true,
                Stats = new[] { (StatType.FireDamage, 25f), (StatType.CastSpeed, 8f) } },
            new Definition { Id = "frostweave", Name = "Frostweave Boots", Gear = GearType.Boots, Effect = "Dashing through enemies chills them.", World = true,
                Stats = new[] { (StatType.MoveSpeed, 12f), (StatType.DashRecovery, 20f), (StatType.IceDamage, 15f) } },
            new Definition { Id = "stormcaller", Name = "Stormcaller Pendant", Gear = GearType.Amulet, Effect = "20% chance on hit to call lightning on up to 3 nearby enemies.", World = true,
                Stats = new[] { (StatType.LightningDamage, 25f), (StatType.MaxMana, 25f) } },
            new Definition { Id = "venomfang", Name = "Venomfang Ring", Gear = GearType.Ring, Effect = "Poisoned enemies spread their poison when they die.", World = true,
                Stats = new[] { (StatType.PoisonDamage, 30f), (StatType.CritChance, 5f) } },
            new Definition { Id = "mirror", Name = "Mirror of Echoes", Gear = GearType.Helmet, Effect = "+25% chance that a spell is cast twice.", World = true,
                Stats = new[] { (StatType.SpellDamage, 8f), (StatType.Armor, 15f) } },
            new Definition { Id = "bloodpact", Name = "Bloodpact Robe", Gear = GearType.Chest, Effect = "Your spells deal +30% damage, but you have less health.", World = true,
                Stats = new[] { (StatType.SpellDamage, 15f), (StatType.MaxHealth, -30f), (StatType.Lifesteal, 3f) } },
            new Definition { Id = "gravewalker", Name = "Gravewalker Boots", Gear = GearType.Boots, Effect = "Every kill makes your dash ready again.", World = true,
                Stats = new[] { (StatType.MoveSpeed, 15f), (StatType.Armor, 10f) } },
            new Definition { Id = "arcanist", Name = "Arcanist's Grips", Gear = GearType.Gloves, Effect = "Spells cost 30% less mana.", World = true,
                Stats = new[] { (StatType.ArcaneDamage, 20f), (StatType.ManaRegen, 25f) } },
            new Definition { Id = "phoenix", Name = "Ring of the Phoenix", Gear = GearType.Ring, Effect = "Once per level, a killing blow heals you to half health instead.", World = true,
                Stats = new[] { (StatType.MaxHealth, 30f), (StatType.FireDamage, 10f) } },
            new Definition { Id = "seeker", Name = "Seeker's Charm", Gear = GearType.Amulet, Effect = "Double gold and better items from every kill.", World = true,
                Stats = new[] { (StatType.GoldFind, 50f), (StatType.ExperienceGain, 15f) } }
        };

        // Nasumični svetski jedinstveni predmet na podu.
        public static void DropRandom(Vector2 position)
        {
            var pool = System.Array.FindAll(All, d => d.World);
            var item = Create(pool[Random.Range(0, pool.Length)].Id);
            LootDrop.SpawnItem(item, position);
            CombatFeedback.Popup("UNIQUE!", (Vector3)position + Vector3.up * 1.2f, Color, 4.5f, 1.8f);
        }

        // Feniks: jednom po nivou.
        private static bool phoenixUsed;

        public static bool TryPhoenix(PlayerHealth health)
        {
            if (phoenixUsed || !Has("phoenix"))
                return false;
            phoenixUsed = true;
            CombatFeedback.Burst(health.transform.position, new Color(1f, .6f, .2f), 30, 5f);
            CombatFeedback.Popup("PHOENIX!", health.transform.position + Vector3.up, new Color(1f, .6f, .2f), 5f, 2f);
            Sfx.Play(SfxId.LevelUp, 1f);
            return true;
        }

        public static readonly Color Color = new Color(1f, .62f, .2f);

        public static Definition Find(string id) => System.Array.Find(All, d => d.Id == id);

        public static Item Create(string id)
        {
            var definition = Find(id);
            if (definition == null)
                return null;
            var item = new Item
            {
                kind = ItemKind.Equipment,
                gear = definition.Gear,
                rarity = WeaponRarity.Legendary,
                itemLevel = 100,
                name = definition.Name,
                unique = id,
                socketCount = 2
            };
            foreach (var (stat, value) in definition.Stats)
                item.stats.Add(new StatRoll(stat, value));
            return item;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            EnemyHealth.AnyDied += EnemyDied;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) => phoenixUsed = false;
        }

        private static PlayerEquipment Player => Object.FindFirstObjectByType<PlayerEquipment>();

        private static void EnemyDied(EnemyHealth enemy)
        {
            if (enemy == null)
                return;
            // Boss ispušta svoj jedinstveni predmet.
            string drop = enemy.GetComponent<DarkSorcererAI>() != null ? "crown"
                : enemy.GetComponent<MagmaGolemAI>() != null ? "golemheart"
                : enemy.GetComponent<FrostLichAI>() != null ? "phylactery"
                : enemy.GetComponent<ShadowKingAI>() != null ? "shadowband" : null;
            if (drop != null)
            {
                LootDrop.SpawnItem(Create(drop), enemy.transform.position);
                CombatFeedback.Popup("UNIQUE ITEM!", enemy.transform.position + Vector3.up * 1.4f, Color, 5f, 2f);
            }

            var equipment = Player;
            if (equipment == null)
                return;
            if (equipment.HasUnique("shadowband"))
                equipment.GetComponent<PlayerHealth>().Heal(3f);
            if (equipment.HasUnique("phylactery") && SkillEffects.IsChilled(enemy))
                equipment.GetComponent<PlayerMana>().Restore(8f);
            if (equipment.HasUnique("gravewalker"))
                equipment.GetComponent<PlayerController>().ResetDash();
            if (equipment.HasUnique("venomfang") && SkillEffects.IsPoisoned(enemy))
                SkillEffects.Area(enemy.transform.position, 3f, 0f, enemy, other => EnemyPoison.Apply(other, 4f + 1.5f * enemy.Level?.Level ?? 1, 4f));
        }

        public static bool Has(string id)
        {
            var equipment = Player;
            return equipment != null && equipment.HasUnique(id);
        }
    }
}
