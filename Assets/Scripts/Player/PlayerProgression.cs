// Iskustvo, nivoi lika i pojačanja koja igrač bira pri svakom novom nivou.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AWizardsFight
{
    public sealed class Perk
    {
        public readonly string Id, Title, Description;

        public Perk(string id, string title, string description)
        {
            Id = id;
            Title = title;
            Description = description;
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerHealth), typeof(PlayerMana))]
    public sealed class PlayerProgression : MonoBehaviour
    {
        public static readonly Perk[] AllPerks =
        {
            new Perk("vitality", "VITALITY", "+20 max health\nand heal 20"),
            new Perk("arcane", "ARCANE RESERVE", "+30 max mana"),
            new Perk("focus", "FOCUS", "+25% mana\nregeneration"),
            new Perk("power", "SPELL POWER", "+15% damage\nfor every spell"),
            new Perk("haste", "RAPID CASTING", "Spells recover\n12% faster"),
            new Perk("swift", "SWIFTNESS", "+10% movement\nspeed"),
            new Perk("quickstep", "QUICK STEP", "Dash recovers\n25% faster"),
            new Perk("pyro", "PYROMANCY", "+25% Fire Staff\ndamage"),
            new Perk("frost", "DEEP FROST", "Ice slows last\n50% longer"),
            new Perk("potion", "POTION MASTER", "+1 potion now,\npotions heal +10"),
            new Perk("inferno", "INFERNO", "+30% damage\nfor every spell"),
            new Perk("scholar", "SCHOLAR", "+15% experience\nfrom enemies")
        };

        public static Perk PerkById(string id) => Array.Find(AllPerks, perk => perk.Id == id);

        public int Rank(string id) => taken.FindAll(taken => taken == id).Count;

        // Poeni uloženi u školu (spojne veštine se računaju u školu kojoj pripadaju).
        public int PointsIn(int school)
        {
            int total = 0;
            foreach (string id in taken)
            {
                var node = SkillTree.Find(id);
                if (node != null && node.School == school)
                    total++;
            }
            return total;
        }

        public int SpentPoints => taken.FindAll(id => SkillTree.Find(id) != null).Count;

        // Zašto se veština (još) ne može naučiti; prazno znači da može.
        public string Requirement(SkillNode node)
        {
            if (Rank(node.Id) >= node.Max)
                return "MAXED";
            if (node.IsFusion)
            {
                if (PointsIn(node.School) < SkillTree.FusionRequirement || PointsIn(node.FusionWith) < SkillTree.FusionRequirement)
                    return "Needs " + SkillTree.FusionRequirement + " points in " + SkillTree.Schools[node.School].name + " and " + SkillTree.Schools[node.FusionWith].name;
            }
            else if (PointsIn(node.School) < SkillTree.TierRequirement[node.Tier])
                return "Needs " + SkillTree.TierRequirement[node.Tier] + " points in " + SkillTree.Schools[node.School].name;
            return PendingChoices > 0 ? "" : "No skill points";
        }

        public bool CanLearn(SkillNode node) => node != null && Requirement(node) == "";

        // Troši poen veštine na izabranu veštinu stabla.
        public bool Learn(SkillNode node)
        {
            if (!CanLearn(node))
                return false;
            PendingChoices--;
            Apply(node.Id, false);
            ExperienceChanged?.Invoke();
            return true;
        }

        // Cena zaborava svih veština (kod Starca ili u stablu): raste sa nivoom lika.
        public int RespecCost => 40 * Level;

        // Vraća sve poene; trajni efekti veština se poništavaju.
        public void Respec()
        {
            int vitality = Rank("vitality"), arcaneRanks = Rank("arcane"), focus = Rank("focus"), potion = Rank("potion");
            int refunded = SpentPoints;
            taken.RemoveAll(id => SkillTree.Find(id) != null);
            health.AddMaximum(-20f * vitality);
            mana.AddMaximum(-25f * arcaneRanks);
            mana.RegenerationMultiplier -= .15f * focus;
            if (potions != null)
                potions.HealBonus -= 10f * potion;
            SpellDamageMultiplier = CooldownMultiplier = MoveSpeedMultiplier = DashCooldownMultiplier = FireDamageMultiplier = SlowDurationMultiplier = ExperienceMultiplier = 1f;
            BonusArmor = 0f;
            // Stara pojačanja (pre stabla) ostaju i ponovo se primenjuju.
            var old = new List<string>(taken);
            taken.Clear();
            foreach (string id in old)
                Apply(id, true);
            PendingChoices += refunded;
            GetComponent<PlayerEquipment>()?.ItemChanged();
            ExperienceChanged?.Invoke();
        }

        public float BonusArmor { get; private set; }

        public float ExperienceMultiplier { get; private set; } = 1f;

        private PlayerHealth health;
        private PlayerMana mana;
        private PlayerPotions potions;
        private readonly List<string> taken = new List<string>();

        public int Level { get; private set; } = 1;
        public float Experience { get; private set; }
        public float RequiredExperience => 50f * Level;
        public int PendingChoices { get; private set; }
        public IReadOnlyList<string> TakenPerks => taken;

        public float SpellDamageMultiplier { get; private set; } = 1f;
        public float CooldownMultiplier { get; private set; } = 1f;
        public float MoveSpeedMultiplier { get; private set; } = 1f;
        public float DashCooldownMultiplier { get; private set; } = 1f;
        public float FireDamageMultiplier { get; private set; } = 1f;
        public float SlowDurationMultiplier { get; private set; } = 1f;

        public event Action ExperienceChanged;
        public event Action LeveledUp;

        private void Awake()
        {
            health = GetComponent<PlayerHealth>();
            mana = GetComponent<PlayerMana>();
            potions = GetComponent<PlayerPotions>();
        }

        private void OnEnable() => EnemyHealth.AnyDied += EnemyDied;

        private void OnDisable() => EnemyHealth.AnyDied -= EnemyDied;

        // Jači i elitni protivnici donose više iskustva.
        private void EnemyDied(EnemyHealth enemy)
        {
            if (enemy == null || !enemy.GrantsExperience || health.IsDead)
                return;
            var level = enemy.Level;
            float amount = (6f + .6f * (level != null ? level.Level : 1)) * Torment.Rewards * LevelModifiers.Rewards;
            if (level != null && level.IsElite)
                amount *= 3f;
            var equipment = GetComponent<PlayerEquipment>();
            if (equipment != null)
                amount *= equipment.Multiplier(StatType.ExperienceGain);
            amount *= ExperienceMultiplier;
            AddExperience(amount);
        }

        public void AddExperience(float amount)
        {
            if (!(amount > 0f))
                return;
            Experience += amount;
            while (Experience >= RequiredExperience)
            {
                Experience -= RequiredExperience;
                Level++;
                PendingChoices++;
                Sfx.Play(SfxId.LevelUp);
                CombatFeedback.Popup("LEVEL UP!", transform.position + Vector3.up * .5f, new Color(1f, .85f, .3f), 5f, 1.4f);
                LeveledUp?.Invoke();
            }

            ExperienceChanged?.Invoke();
        }

        // Tri različita nasumična pojačanja za izbor.
        public List<Perk> RollChoices()
        {
            var pool = new List<Perk>(AllPerks);
            var result = new List<Perk>();
            for (int i = 0; i < 3 && pool.Count > 0; i++)
            {
                int index = UnityEngine.Random.Range(0, pool.Count);
                result.Add(pool[index]);
                pool.RemoveAt(index);
            }
            return result;
        }

        public void Choose(Perk perk)
        {
            if (perk == null || PendingChoices <= 0)
                return;
            PendingChoices--;
            Apply(perk.Id, false);
            ExperienceChanged?.Invoke();
        }

        private void Apply(string id, bool restoring)
        {
            taken.Add(id);
            switch (id)
            {
                case "vitality":
                    health.IncreaseMaximum(20f);
                    break;
                case "arcane":
                    mana.IncreaseMaximum(25f);
                    break;
                case "focus":
                    mana.RegenerationMultiplier += .15f;
                    break;
                case "power":
                    SpellDamageMultiplier += .08f;
                    break;
                case "haste":
                    CooldownMultiplier *= .92f;
                    break;
                case "swift":
                    MoveSpeedMultiplier += .08f;
                    break;
                case "quickstep":
                    DashCooldownMultiplier *= .85f;
                    break;
                case "pyro":
                    FireDamageMultiplier += .12f;
                    break;
                case "frost":
                    SlowDurationMultiplier += .4f;
                    break;
                case "inferno":
                    SpellDamageMultiplier += .25f;
                    break;
                case "icearmor":
                    BonusArmor += 15f;
                    GetComponent<PlayerEquipment>()?.ItemChanged();
                    break;
                case "scholar":
                    ExperienceMultiplier += .15f;
                    break;
                case "potion":
                    if (potions != null)
                    {
                        potions.HealBonus += 10f;
                        // Napitak dobijen ranije već je sadržan u sačuvanom broju napitaka.
                        if (!restoring)
                            potions.Add(1);
                    }
                    break;
            }
        }

        // Vraća nivo i pojačanja iz sačuvane partije.
        public void Restore(int level, float experience, IEnumerable<string> perks, int pendingChoices)
        {
            Level = Mathf.Max(1, level);
            Experience = Mathf.Max(0f, experience);
            PendingChoices = Mathf.Max(0, pendingChoices);
            if (perks != null)
                foreach (string id in perks)
                    Apply(id, true);
            ExperienceChanged?.Invoke();
            if (PendingChoices > 0)
                LeveledUp?.Invoke();
        }
    }
}
