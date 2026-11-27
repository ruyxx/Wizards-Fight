// Inventar, oprema, rune u predmetima i oružjima, zlato i zbir svih osobina koje oprema daje liku.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AWizardsFight
{
    [DisallowMultipleComponent]
    public sealed class PlayerEquipment : MonoBehaviour
    {
        public const int BagSize = 30;
        public const int SlotCount = 7;

        // Stanje opreme za čuvanje partije.
        [Serializable]
        public sealed class Snapshot
        {
            public int gold;
            public List<Item> bag = new List<Item>();
            public List<Item> equipped = new List<Item>();
            public List<SocketedRune> basicRunes = new List<SocketedRune>();
            public List<SocketedRune> fireRunes = new List<SocketedRune>();
            public List<SocketedRune> iceRunes = new List<SocketedRune>();
            public List<SocketedRune> stormRunes = new List<SocketedRune>();
            public List<SocketedRune> venomRunes = new List<SocketedRune>();
        }

        private readonly List<Item> bag = new List<Item>();
        private readonly Item[] equipped = new Item[SlotCount];
        private readonly List<SocketedRune>[] weaponRunes = { new List<SocketedRune>(), new List<SocketedRune>(), new List<SocketedRune>(), new List<SocketedRune>(), new List<SocketedRune>() };
        private readonly float[] totals = new float[Enum.GetValues(typeof(StatType)).Length];
        private readonly List<RuneType> activeSets = new List<RuneType>();
        private PlayerHealth health;
        private PlayerMana mana;
        private PlayerWeapons weapons;
        private PlayerBuffs buffs;

        public int Gold { get; private set; }
        public IReadOnlyList<Item> Bag => bag;
        public IReadOnlyList<RuneType> ActiveSets => activeSets;
        public event Action Changed;

        private void Awake()
        {
            health = GetComponent<PlayerHealth>();
            mana = GetComponent<PlayerMana>();
            weapons = GetComponent<PlayerWeapons>();
            buffs = GetComponent<PlayerBuffs>();
            if (buffs == null)
                buffs = gameObject.AddComponent<PlayerBuffs>();
        }

        private void Start()
        {
            if (weapons != null)
                weapons.Changed += Recalculate;
            Recalculate();
        }

        private void OnDestroy()
        {
            if (weapons != null)
                weapons.Changed -= Recalculate;
        }

        // Ukupna osobina: oprema, rune i privremeni blagoslovi oltara.
        public float Get(StatType stat) => totals[(int)stat] + (buffs != null ? buffs.Bonus(stat) : 0f);

        // Procentualna osobina kao množilac, npr. +15% daje 1,15.
        public float Multiplier(StatType stat) => 1f + Get(stat) / 100f;

        public Item Equipped(EquipSlot slot) => equipped[(int)slot];

        public IReadOnlyList<SocketedRune> WeaponRunes(WeaponType type) => weaponRunes[(int)type];

        public int WeaponSocketCount(WeaponType type)
        {
            var weapon = weapons != null ? weapons.GetOwned(type) : null;
            return weapon != null ? ItemCatalog.WeaponSockets(weapon.Rarity) : 0;
        }

        public bool BagFull => bag.Count >= BagSize;

        public bool AddItem(Item item)
        {
            if (item == null || item.IsEmpty || BagFull)
                return false;
            bag.Add(item);
            Changed?.Invoke();
            return true;
        }

        public bool RemoveFromBag(Item item)
        {
            if (!bag.Remove(item))
                return false;
            Changed?.Invoke();
            return true;
        }

        public void AddGold(int amount)
        {
            if (amount <= 0)
                return;
            Gold += amount;
            Changed?.Invoke();
        }

        public bool SpendGold(int amount)
        {
            if (amount < 0 || Gold < amount)
                return false;
            Gold -= amount;
            Changed?.Invoke();
            return true;
        }

        // Stavlja predmet iz torbe na odgovarajuće mesto; prsten ide na slobodno mesto ili menja prvi.
        // Bez zadatog mesta bira prvo slobodno odgovarajuće; prevlačenjem se bira tačno mesto.
        public bool Equip(Item item, EquipSlot? target = null)
        {
            if (item == null || item.kind != ItemKind.Equipment || !bag.Contains(item))
                return false;
            if (target.HasValue && !item.Fits(target.Value))
                return false;
            EquipSlot slot;
            if (target.HasValue)
                slot = target.Value;
            else if (item.gear == GearType.Ring)
                slot = equipped[(int)EquipSlot.Ring1] == null ? EquipSlot.Ring1 : equipped[(int)EquipSlot.Ring2] == null ? EquipSlot.Ring2 : EquipSlot.Ring1;
            else
                slot = Enum.GetValues(typeof(EquipSlot)).Cast<EquipSlot>().First(candidate => item.Fits(candidate));
            bag.Remove(item);
            var previous = equipped[(int)slot];
            equipped[(int)slot] = item;
            if (previous != null)
                bag.Add(previous);
            Recalculate();
            return true;
        }

        // Zamena mesta dva opremljena predmeta iste vrste (npr. dva prstena).
        public bool SwapEquipped(EquipSlot a, EquipSlot b)
        {
            var first = equipped[(int)a];
            var second = equipped[(int)b];
            if (a == b || (first != null && !first.Fits(b)) || (second != null && !second.Fits(a)))
                return false;
            equipped[(int)a] = second;
            equipped[(int)b] = first;
            Recalculate();
            return true;
        }

        // Posle izmene predmeta u kovačnici: zbirovi se ponovo računaju i prozori osvežavaju.
        public void ItemChanged() => Recalculate();

        public bool IsEquipped(Item item) => item != null && equipped.Contains(item);

        public bool HasUnique(string id)
        {
            foreach (var item in equipped)
                if (item != null && item.unique == id)
                    return true;
            return false;
        }

        public bool Unequip(EquipSlot slot)
        {
            var item = equipped[(int)slot];
            if (item == null || BagFull)
                return false;
            equipped[(int)slot] = null;
            bag.Add(item);
            Recalculate();
            return true;
        }

        // Rastavljanje predmeta daje zlato.
        public int Salvage(Item item)
        {
            if (item == null || !bag.Remove(item))
                return 0;
            int value = item.GoldValue;
            foreach (var rune in item.runes)
                bag.Add(ItemCatalog.CreateRune(rune.rune, rune.tier));
            Gold += value;
            Changed?.Invoke();
            return value;
        }

        public bool InsertRune(Item rune, EquipSlot slot)
        {
            var target = equipped[(int)slot];
            if (rune == null || !rune.IsRune || target == null || target.FreeSockets <= 0 || !bag.Remove(rune))
                return false;
            target.runes.Add(new SocketedRune(rune.rune, rune.runeTier));
            Recalculate();
            return true;
        }

        public bool InsertRune(Item rune, WeaponType weapon)
        {
            var list = weaponRunes[(int)weapon];
            if (rune == null || !rune.IsRune || list.Count >= WeaponSocketCount(weapon) || !bag.Remove(rune))
                return false;
            list.Add(new SocketedRune(rune.rune, rune.runeTier));
            Recalculate();
            return true;
        }

        // Vađenje rune je besplatno; runa se vraća u torbu.
        public bool RemoveRune(EquipSlot slot, int index)
        {
            var target = equipped[(int)slot];
            if (target == null || index < 0 || index >= target.runes.Count || BagFull)
                return false;
            var rune = target.runes[index];
            target.runes.RemoveAt(index);
            bag.Add(ItemCatalog.CreateRune(rune.rune, rune.tier));
            Recalculate();
            return true;
        }

        public bool RemoveRune(WeaponType weapon, int index)
        {
            var list = weaponRunes[(int)weapon];
            if (index < 0 || index >= list.Count || BagFull)
                return false;
            var rune = list[index];
            list.RemoveAt(index);
            bag.Add(ItemCatalog.CreateRune(rune.rune, rune.tier));
            Recalculate();
            return true;
        }

        // Zbir osobina sa opreme i runa; tri iste rune uključuju set bonus.
        public void Recalculate()
        {
            Array.Clear(totals, 0, totals.Length);
            var runeCounts = new Dictionary<RuneType, int>();
            void CountRune(SocketedRune rune)
            {
                Add(rune.Stat);
                runeCounts.TryGetValue(rune.rune, out int count);
                runeCounts[rune.rune] = count + 1;
            }

            foreach (var item in equipped)
            {
                if (item == null)
                    continue;
                foreach (var stat in item.stats)
                    Add(stat);
                foreach (var rune in item.runes)
                    CountRune(rune);
            }

            for (int type = 0; type < weaponRunes.Length; type++)
            {
                // Rune u oružju koje više nema dovoljno slotova (npr. posle zamene) ostaju, ali ne rade iznad broja slotova.
                int sockets = WeaponSocketCount((WeaponType)type);
                for (int i = 0; i < weaponRunes[type].Count && i < sockets; i++)
                    CountRune(weaponRunes[type][i]);
            }

            activeSets.Clear();
            foreach (var pair in runeCounts)
                if (pair.Value >= 3)
                {
                    activeSets.Add(pair.Key);
                    foreach (var bonus in ItemCatalog.SetBonus(pair.Key))
                        Add(bonus);
                }

            if (health != null)
                health.SetGearBonus(Get(StatType.MaxHealth), Get(StatType.MaxHealthPercent), Get(StatType.Armor) + (TryGetComponent<PlayerProgression>(out var progression) ? progression.BonusArmor : 0f));
            if (mana != null)
                mana.SetGearBonus(Get(StatType.MaxMana), Get(StatType.ManaRegen));
            Changed?.Invoke();
        }

        private void Add(StatRoll roll) => totals[(int)roll.stat] += roll.value;

        // Ograničenja da oprema ne napravi lik neranjivim ili prebrzim.
        public float CritChance => Mathf.Min(60f, Get(StatType.CritChance)) / 100f;
        public float CritMultiplier => 1.5f + Get(StatType.CritDamage) / 100f;
        public float CastSpeedMultiplier => 1f / (1f + Mathf.Min(60f, Get(StatType.CastSpeed)) / 100f);
        public float MoveSpeedMultiplier => 1f + Mathf.Min(40f, Get(StatType.MoveSpeed)) / 100f;
        public float DashRecoveryMultiplier => 1f / (1f + Mathf.Min(80f, Get(StatType.DashRecovery)) / 100f);
        public float Lifesteal => Mathf.Min(10f, Get(StatType.Lifesteal)) / 100f;

        public float DamageMultiplier(WeaponType type)
        {
            float element = type == WeaponType.FireStaff ? Get(StatType.FireDamage)
                : type == WeaponType.IceStaff ? Get(StatType.IceDamage)
                : type == WeaponType.StormStaff ? Get(StatType.LightningDamage)
                : type == WeaponType.VenomStaff ? Get(StatType.PoisonDamage)
                : Get(StatType.ArcaneDamage);
            return 1f + (Get(StatType.SpellDamage) + element) / 100f;
        }

        public Snapshot Capture()
        {
            var snapshot = new Snapshot { gold = Gold };
            snapshot.bag.AddRange(bag);
            foreach (var item in equipped)
                snapshot.equipped.Add(item ?? new Item());
            snapshot.basicRunes.AddRange(weaponRunes[0]);
            snapshot.fireRunes.AddRange(weaponRunes[1]);
            snapshot.iceRunes.AddRange(weaponRunes[2]);
            snapshot.stormRunes.AddRange(weaponRunes[3]);
            snapshot.venomRunes.AddRange(weaponRunes[4]);
            return snapshot;
        }

        public void Restore(Snapshot snapshot)
        {
            if (snapshot == null)
                return;
            Gold = Mathf.Max(0, snapshot.gold);
            bag.Clear();
            bag.AddRange(snapshot.bag.Where(item => item != null && !item.IsEmpty).Take(BagSize));
            for (int i = 0; i < SlotCount; i++)
            {
                var item = i < snapshot.equipped.Count ? snapshot.equipped[i] : null;
                equipped[i] = item != null && !item.IsEmpty ? item : null;
            }

            weaponRunes[0] = new List<SocketedRune>(snapshot.basicRunes);
            weaponRunes[1] = new List<SocketedRune>(snapshot.fireRunes);
            weaponRunes[2] = new List<SocketedRune>(snapshot.iceRunes);
            weaponRunes[3] = new List<SocketedRune>(snapshot.stormRunes ?? new List<SocketedRune>());
            weaponRunes[4] = new List<SocketedRune>(snapshot.venomRunes ?? new List<SocketedRune>());
            Recalculate();
        }
    }
}
