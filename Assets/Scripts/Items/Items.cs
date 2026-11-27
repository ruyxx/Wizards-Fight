// Predmeti opreme i rune: osobine, retkost, slotovi za rune i nasumično stvaranje plena.
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AWizardsFight
{
    public enum StatType
    {
        Armor,
        MaxHealth,
        MaxMana,
        ManaRegen,
        SpellDamage,
        FireDamage,
        IceDamage,
        ArcaneDamage,
        CritChance,
        CritDamage,
        CastSpeed,
        MoveSpeed,
        DashRecovery,
        PotionHealing,
        ExperienceGain,
        GoldFind,
        Lifesteal,
        SlowDuration,
        MaxHealthPercent,
        LightningDamage,
        PoisonDamage
    }

    public enum ItemKind
    {
        None,
        Equipment,
        Rune
    }

    // Mesta na liku; prsten postoji dva puta.
    public enum EquipSlot
    {
        Helmet,
        Amulet,
        Chest,
        Gloves,
        Ring1,
        Ring2,
        Boots
    }

    public enum GearType
    {
        Helmet,
        Amulet,
        Chest,
        Gloves,
        Ring,
        Boots
    }

    public enum RuneType
    {
        Ember,
        Frost,
        Storm,
        Vigor,
        Ward,
        Swift,
        Focus,
        Fortune,
        Fury
    }

    [Serializable]
    public sealed class StatRoll
    {
        public StatType stat;
        public float value;

        public StatRoll(StatType stat, float value)
        {
            this.stat = stat;
            this.value = value;
        }
    }

    // Runa ubačena u predmet ili oružje (bez ugnežđenih predmeta, da bi se lako čuvala).
    [Serializable]
    public sealed class SocketedRune
    {
        public RuneType rune;
        public int tier;

        public SocketedRune(RuneType rune, int tier)
        {
            this.rune = rune;
            this.tier = tier;
        }

        public StatRoll Stat => ItemCatalog.CreateRune(rune, tier).stats[0];
    }

    // Jedan predmet ili runa. Klasa je serijalizabilna da bi se čuvala u sačuvanoj partiji.
    [Serializable]
    public sealed class Item
    {
        public ItemKind kind;
        public string name;
        public GearType gear;
        public WeaponRarity rarity;
        public int itemLevel;
        public List<StatRoll> stats = new List<StatRoll>();
        public int socketCount;
        // Nadogradnja u kovačnici (+1 do +5): svaka podiže sve osobine predmeta za 10%.
        public int upgrade;
        // Jedinstveni predmet (prazno za obične); id iz UniqueItems.
        public string unique;
        public bool IsUnique => !string.IsNullOrEmpty(unique);
        public List<SocketedRune> runes = new List<SocketedRune>();
        public RuneType rune;
        public int runeTier;

        public bool IsEmpty => kind == ItemKind.None;
        public bool IsRune => kind == ItemKind.Rune;
        public int FreeSockets => Mathf.Max(0, socketCount - runes.Count);

        // Vrednost za prodaju ili rastavljanje.
        public int GoldValue => IsRune ? 15 * runeTier * runeTier : Mathf.RoundToInt((4 + itemLevel * .6f) * (1 + (int)rarity * 1.2f));

        public bool Fits(EquipSlot slot) => kind == ItemKind.Equipment && ItemCatalog.GearFor(slot) == gear;
    }

    public static class ItemCatalog
    {
        // Osnovni predmeti po vrsti: ime i osobina koju predmet uvek ima.
        private static readonly Dictionary<GearType, (string name, StatType stat, float weight)[]> Bases = new Dictionary<GearType, (string, StatType, float)[]>
        {
            { GearType.Helmet, new[] { ("Leather Cap", StatType.Armor, .7f), ("Iron Helm", StatType.Armor, 1.2f), ("Mystic Hood", StatType.MaxMana, 1f) } },
            { GearType.Chest, new[] { ("Padded Robe", StatType.MaxMana, 1.2f), ("Chainmail", StatType.Armor, 1.6f), ("Archmage Robe", StatType.SpellDamage, 1f) } },
            { GearType.Gloves, new[] { ("Cloth Gloves", StatType.CastSpeed, 1f), ("Gauntlets", StatType.Armor, .9f), ("Spellweave Wraps", StatType.CritChance, 1f) } },
            { GearType.Boots, new[] { ("Sandals", StatType.MoveSpeed, 1f), ("Iron Greaves", StatType.Armor, .9f), ("Windwalkers", StatType.DashRecovery, 1f) } },
            { GearType.Amulet, new[] { ("Bone Amulet", StatType.MaxHealth, 1f), ("Arcane Pendant", StatType.SpellDamage, 1f), ("Scholar's Charm", StatType.ExperienceGain, 1f) } },
            { GearType.Ring, new[] { ("Copper Ring", StatType.GoldFind, 1f), ("Ruby Ring", StatType.FireDamage, 1f), ("Sapphire Ring", StatType.IceDamage, 1f), ("Onyx Ring", StatType.CritDamage, 1f), ("Amethyst Ring", StatType.ArcaneDamage, 1f), ("Topaz Ring", StatType.LightningDamage, 1f), ("Jade Ring", StatType.PoisonDamage, 1f) } }
        };

        // Osnovni opseg svake osobine na nivou predmeta 1; raste sa nivoom predmeta.
        private static readonly Dictionary<StatType, (float min, float max)> Ranges = new Dictionary<StatType, (float, float)>
        {
            { StatType.Armor, (4, 8) },
            { StatType.MaxHealth, (6, 12) },
            { StatType.MaxMana, (8, 15) },
            { StatType.ManaRegen, (4, 8) },
            { StatType.SpellDamage, (3, 6) },
            { StatType.FireDamage, (5, 10) },
            { StatType.IceDamage, (5, 10) },
            { StatType.ArcaneDamage, (5, 10) },
            { StatType.CritChance, (2, 4) },
            { StatType.CritDamage, (8, 15) },
            { StatType.CastSpeed, (3, 6) },
            { StatType.MoveSpeed, (2, 4) },
            { StatType.DashRecovery, (4, 8) },
            { StatType.PotionHealing, (3, 6) },
            { StatType.ExperienceGain, (3, 6) },
            { StatType.GoldFind, (6, 12) },
            { StatType.Lifesteal, (.5f, 1.5f) },
            { StatType.SlowDuration, (6, 12) },
            { StatType.MaxHealthPercent, (2, 4) },
            { StatType.LightningDamage, (5, 10) },
            { StatType.PoisonDamage, (5, 10) }
        };

        private static readonly StatType[] AffixPool =
        {
            StatType.Armor, StatType.MaxHealth, StatType.MaxMana, StatType.ManaRegen, StatType.SpellDamage, StatType.FireDamage,
            StatType.IceDamage, StatType.ArcaneDamage, StatType.CritChance, StatType.CritDamage, StatType.CastSpeed, StatType.MoveSpeed,
            StatType.DashRecovery, StatType.PotionHealing, StatType.ExperienceGain, StatType.GoldFind, StatType.Lifesteal, StatType.SlowDuration,
            StatType.LightningDamage, StatType.PoisonDamage
        };

        private static readonly Dictionary<StatType, (string prefix, string suffix)> Words = new Dictionary<StatType, (string, string)>
        {
            { StatType.Armor, ("Sturdy", "of the Bastion") },
            { StatType.MaxHealth, ("Hale", "of Vigor") },
            { StatType.MaxMana, ("Arcane", "of the Well") },
            { StatType.ManaRegen, ("Flowing", "of Renewal") },
            { StatType.SpellDamage, ("Potent", "of Power") },
            { StatType.FireDamage, ("Blazing", "of Embers") },
            { StatType.IceDamage, ("Frozen", "of Winter") },
            { StatType.ArcaneDamage, ("Mystic", "of the Stars") },
            { StatType.CritChance, ("Keen", "of Precision") },
            { StatType.CritDamage, ("Cruel", "of Ruin") },
            { StatType.CastSpeed, ("Quick", "of Haste") },
            { StatType.MoveSpeed, ("Fleet", "of the Wind") },
            { StatType.DashRecovery, ("Nimble", "of Shadows") },
            { StatType.PotionHealing, ("Herbal", "of Mending") },
            { StatType.ExperienceGain, ("Wise", "of Insight") },
            { StatType.GoldFind, ("Gilded", "of Fortune") },
            { StatType.Lifesteal, ("Vampiric", "of the Leech") },
            { StatType.SlowDuration, ("Chilling", "of Stillness") },
            { StatType.MaxHealthPercent, ("Titan", "of Giants") },
            { StatType.LightningDamage, ("Charged", "of Storms") },
            { StatType.PoisonDamage, ("Venomous", "of the Serpent") }
        };

        public static GearType GearFor(EquipSlot slot)
        {
            switch (slot)
            {
                case EquipSlot.Helmet: return GearType.Helmet;
                case EquipSlot.Amulet: return GearType.Amulet;
                case EquipSlot.Chest: return GearType.Chest;
                case EquipSlot.Gloves: return GearType.Gloves;
                case EquipSlot.Boots: return GearType.Boots;
                default: return GearType.Ring;
            }
        }

        public static bool IsPercent(StatType stat) => stat != StatType.Armor && stat != StatType.MaxHealth && stat != StatType.MaxMana && stat != StatType.PotionHealing;

        public static string StatLabel(StatType stat)
        {
            switch (stat)
            {
                case StatType.Armor: return "Armor";
                case StatType.MaxHealth: return "Max Health";
                case StatType.MaxMana: return "Max Mana";
                case StatType.ManaRegen: return "Mana Regeneration";
                case StatType.SpellDamage: return "Spell Damage";
                case StatType.FireDamage: return "Fire Damage";
                case StatType.IceDamage: return "Ice Damage";
                case StatType.ArcaneDamage: return "Arcane Damage";
                case StatType.CritChance: return "Critical Chance";
                case StatType.CritDamage: return "Critical Damage";
                case StatType.CastSpeed: return "Cast Speed";
                case StatType.MoveSpeed: return "Movement Speed";
                case StatType.DashRecovery: return "Dash Recovery";
                case StatType.PotionHealing: return "Potion Healing";
                case StatType.ExperienceGain: return "Experience Gain";
                case StatType.GoldFind: return "Gold Find";
                case StatType.Lifesteal: return "Life Steal";
                case StatType.SlowDuration: return "Slow Duration";
                case StatType.LightningDamage: return "Lightning Damage";
                case StatType.PoisonDamage: return "Poison Damage";
                default: return "Max Health";
            }
        }

        public static string FormatStat(StatType stat, float value)
        {
            string number = IsPercent(stat) ? (value < 2f && stat == StatType.Lifesteal ? value.ToString("0.0") : Mathf.RoundToInt(value).ToString()) + "%" : Mathf.RoundToInt(value).ToString();
            return (value >= 0 ? "+" : "") + number + " " + StatLabel(stat);
        }

        public static Color RarityColor(WeaponRarity rarity) => WeaponRarityPresentation.ColorFor(rarity);
        public static Color ItemColor(Item item) => item.IsRune ? RuneColor(item.rune) : item.IsUnique ? UniqueItems.Color : RarityColor(item.rarity);

        private static float Scale(int itemLevel) => 1f + itemLevel / 45f;

        private static float RollValue(StatType stat, int itemLevel, float weight = 1f)
        {
            var range = Ranges[stat];
            // Procenti rastu sporije od ravnih vrednosti, da ne bi postali prejaki na visokim nivoima.
            float scale = IsPercent(stat) ? 1f + (Scale(itemLevel) - 1f) * .35f : Scale(itemLevel);
            float value = Random.Range(range.min, range.max) * scale * weight;
            return stat == StatType.Lifesteal ? Mathf.Round(value * 10f) / 10f : Mathf.Round(value);
        }

        // Retkost: većina predmeta je obična, legendarni su retki; elitni i kovčezi daju bolje šanse.
        public static WeaponRarity RollRarity(float luck)
        {
            float roll = Random.value / Mathf.Max(.2f, luck);
            // Legendarni 1.5%, epski 8%, retki 33% (pre sreće).
            return roll < .015f ? WeaponRarity.Legendary : roll < .08f ? WeaponRarity.Epic : roll < .33f ? WeaponRarity.Rare : WeaponRarity.Common;
        }

        public static Item CreateGear(int itemLevel, float luck = 1f)
        {
            var gear = (GearType)Random.Range(0, 6);
            var options = Bases[gear];
            var basis = options[Random.Range(0, options.Length)];
            var rarity = RollRarity(luck);
            var item = new Item
            {
                kind = ItemKind.Equipment,
                gear = gear,
                rarity = rarity,
                // Iznad 100 samo u Torment težini.
                itemLevel = Mathf.Clamp(itemLevel, 1, 160)
            };
            item.stats.Add(new StatRoll(basis.stat, RollValue(basis.stat, item.itemLevel, 1.5f * basis.weight)));

            int affixes = (int)rarity + 1;
            var pool = new List<StatType>(AffixPool);
            pool.Remove(basis.stat);
            if (rarity == WeaponRarity.Legendary)
                pool.Add(StatType.MaxHealthPercent);
            for (int i = 0; i < affixes && pool.Count > 0; i++)
            {
                int index = Random.Range(0, pool.Count);
                item.stats.Add(new StatRoll(pool[index], RollValue(pool[index], item.itemLevel)));
                pool.RemoveAt(index);
            }

            // Šlem i oklop češće imaju slot za runu, a legendarni predmeti uvek dva.
            int sockets = rarity == WeaponRarity.Legendary ? 2 : rarity == WeaponRarity.Epic ? Random.Range(1, 3) : rarity == WeaponRarity.Rare ? 1 : (Random.value < .35f ? 1 : 0);
            if ((gear == GearType.Chest || gear == GearType.Helmet) && rarity != WeaponRarity.Legendary && Random.value < .5f)
                sockets++;
            item.socketCount = Mathf.Min(3, sockets);
            item.name = NameFor(item, basis.name);
            return item;
        }

        // ---------- Kovačnica ----------

        public const int MaxUpgrade = 5, MaxSockets = 3;

        public static int UpgradeCost(Item item) => Mathf.RoundToInt((40 + item.itemLevel * 2.5f) * (item.upgrade + 1) * (1f + (int)item.rarity * .5f));
        public static int SocketCost(Item item) => 120 * (item.socketCount + 1) + item.itemLevel * 2;
        public static int ReforgeCost(Item item) => Mathf.RoundToInt((60 + item.itemLevel * 2f) * (1f + (int)item.rarity * .4f));

        public static bool Upgrade(Item item)
        {
            if (item == null || item.IsRune || item.upgrade >= MaxUpgrade)
                return false;
            item.upgrade++;
            foreach (var stat in item.stats)
                stat.value = Mathf.Round(stat.value * 1.1f * 10f) / 10f;
            item.name = System.Text.RegularExpressions.Regex.Replace(item.name, @" \+\d$", "") + " +" + item.upgrade;
            return true;
        }

        public static bool AddSocket(Item item)
        {
            if (item == null || item.IsRune || item.socketCount >= MaxSockets)
                return false;
            item.socketCount++;
            return true;
        }

        // Menja jednu dodatnu osobinu (ne osnovnu) novom nasumičnom osobinom; nadogradnja ostaje uračunata.
        public static StatType? Reforge(Item item, int index)
        {
            if (item == null || item.IsRune || index < 1 || index >= item.stats.Count)
                return null;
            var pool = new List<StatType>(AffixPool);
            foreach (var existing in item.stats)
                pool.Remove(existing.stat);
            if (pool.Count == 0)
                return null;
            var stat = pool[Random.Range(0, pool.Count)];
            float value = RollValue(stat, item.itemLevel) * Mathf.Pow(1.1f, item.upgrade);
            item.stats[index] = new StatRoll(stat, Mathf.Round(value * 10f) / 10f);
            return stat;
        }

        private static string NameFor(Item item, string baseName)
        {
            if (item.rarity == WeaponRarity.Common || item.stats.Count < 2)
                return baseName;
            string prefix = Words[item.stats[1].stat].prefix;
            if (item.rarity == WeaponRarity.Rare)
                return prefix + " " + baseName;
            string suffix = Words[item.stats[item.stats.Count - 1].stat].suffix;
            return prefix + " " + baseName + " " + suffix;
        }

        // ---------- Rune ----------

        public static StatType RuneStat(RuneType rune)
        {
            switch (rune)
            {
                case RuneType.Ember: return StatType.FireDamage;
                case RuneType.Frost: return StatType.IceDamage;
                case RuneType.Storm: return StatType.ArcaneDamage;
                case RuneType.Vigor: return StatType.MaxHealth;
                case RuneType.Ward: return StatType.Armor;
                case RuneType.Swift: return StatType.MoveSpeed;
                case RuneType.Focus: return StatType.CastSpeed;
                case RuneType.Fortune: return StatType.GoldFind;
                default: return StatType.CritChance;
            }
        }

        private static readonly Dictionary<RuneType, float[]> RuneValues = new Dictionary<RuneType, float[]>
        {
            { RuneType.Ember, new[] { 8f, 14f, 22f } },
            { RuneType.Frost, new[] { 8f, 14f, 22f } },
            { RuneType.Storm, new[] { 8f, 14f, 22f } },
            { RuneType.Vigor, new[] { 12f, 22f, 36f } },
            { RuneType.Ward, new[] { 8f, 15f, 25f } },
            { RuneType.Swift, new[] { 3f, 5f, 8f } },
            { RuneType.Focus, new[] { 4f, 7f, 11f } },
            { RuneType.Fortune, new[] { 10f, 18f, 30f } },
            { RuneType.Fury, new[] { 2f, 4f, 6f } }
        };

        public static Color RuneColor(RuneType rune)
        {
            switch (rune)
            {
                case RuneType.Ember: return new Color(1f, .45f, .15f);
                case RuneType.Frost: return new Color(.45f, .85f, 1f);
                case RuneType.Storm: return new Color(.75f, .55f, 1f);
                case RuneType.Vigor: return new Color(.95f, .3f, .35f);
                case RuneType.Ward: return new Color(.75f, .78f, .85f);
                case RuneType.Swift: return new Color(.45f, 1f, .55f);
                case RuneType.Focus: return new Color(.4f, .55f, 1f);
                case RuneType.Fortune: return new Color(1f, .85f, .25f);
                default: return new Color(1f, .2f, .2f);
            }
        }

        public static Item CreateRune(RuneType type, int tier)
        {
            tier = Mathf.Clamp(tier, 1, 3);
            var item = new Item
            {
                kind = ItemKind.Rune,
                rune = type,
                runeTier = tier,
                rarity = tier == 3 ? WeaponRarity.Epic : tier == 2 ? WeaponRarity.Rare : WeaponRarity.Common,
                itemLevel = tier * 30,
                name = type + " Rune " + new string('I', tier)
            };
            item.stats.Add(new StatRoll(RuneStat(type), RuneValues[type][tier - 1]));
            return item;
        }

        public static Item CreateRandomRune(int itemLevel)
        {
            int tier = itemLevel >= 70 && Random.value < .35f ? 3 : itemLevel >= 35 && Random.value < .45f ? 2 : 1;
            return CreateRune((RuneType)Random.Range(0, 9), tier);
        }

        // ---------- Setovi runa ----------

        public static string SetName(RuneType rune)
        {
            switch (rune)
            {
                case RuneType.Ember: return "INFERNO";
                case RuneType.Frost: return "PERMAFROST";
                case RuneType.Storm: return "OVERLOAD";
                case RuneType.Vigor: return "TITAN";
                case RuneType.Ward: return "BULWARK";
                case RuneType.Swift: return "BLINK";
                case RuneType.Focus: return "CLARITY";
                case RuneType.Fortune: return "MIDAS";
                default: return "BLOODLUST";
            }
        }

        // Tri rune iste vrste bilo gde na liku daju dodatni bonus.
        public static StatRoll[] SetBonus(RuneType rune)
        {
            switch (rune)
            {
                case RuneType.Ember: return new[] { new StatRoll(StatType.FireDamage, 25f) };
                case RuneType.Frost: return new[] { new StatRoll(StatType.SlowDuration, 60f), new StatRoll(StatType.IceDamage, 10f) };
                case RuneType.Storm: return new[] { new StatRoll(StatType.CritChance, 10f) };
                case RuneType.Vigor: return new[] { new StatRoll(StatType.MaxHealthPercent, 15f) };
                case RuneType.Ward: return new[] { new StatRoll(StatType.Armor, 40f) };
                case RuneType.Swift: return new[] { new StatRoll(StatType.DashRecovery, 30f) };
                case RuneType.Focus: return new[] { new StatRoll(StatType.ManaRegen, 40f) };
                case RuneType.Fortune: return new[] { new StatRoll(StatType.GoldFind, 50f), new StatRoll(StatType.ExperienceGain, 15f) };
                default: return new[] { new StatRoll(StatType.Lifesteal, 3f) };
            }
        }

        public static int WeaponSockets(WeaponRarity rarity) => rarity == WeaponRarity.Legendary ? 3 : rarity == WeaponRarity.Epic ? 2 : 1;

        // Ikonice predmeta su u Resources/Items, pa ih igra učitava bez posebnih referenci u sceni.
        private static readonly Dictionary<string, Sprite> Icons = new Dictionary<string, Sprite>();

        public static Sprite Icon(Item item)
        {
            if (item == null || item.IsEmpty)
                return null;
            string key = item.IsRune ? "Rune" : item.gear.ToString();
            if (!Icons.TryGetValue(key, out var sprite))
            {
                sprite = Resources.Load<Sprite>("Items/" + key);
                Icons[key] = sprite;
            }
            return sprite;
        }

        public static Sprite GoldIcon()
        {
            if (!Icons.TryGetValue("Gold", out var sprite))
            {
                sprite = Resources.Load<Sprite>("Items/Gold");
                Icons["Gold"] = sprite;
            }
            return sprite;
        }
    }
}
