// Ugovori od Starca u kampu (po uzoru na Hades): tri zadatka istovremeno, npr. "ubij 25 kostura" ili
// "otvori 4 kovčega". Napredak se broji svuda, a nagrada (zlato i predmet) se preuzima kod Starca.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AWizardsFight
{
    public enum BountyKind { Skeletons, Slimes, Archers, Bombers, Shieldbearers, Elites, Chests, Bosses }

    [Serializable]
    public sealed class Bounty
    {
        public BountyKind kind;
        public int goal, progress, gold;
        public bool Done => progress >= goal;

        public string Title
        {
            get
            {
                switch (kind)
                {
                    case BountyKind.Skeletons: return "Bone Collector";
                    case BountyKind.Slimes: return "Slime Sweeper";
                    case BountyKind.Archers: return "Silence the Bows";
                    case BountyKind.Bombers: return "Defuse the Threat";
                    case BountyKind.Shieldbearers: return "Break the Wall";
                    case BountyKind.Elites: return "Elite Hunter";
                    case BountyKind.Chests: return "Treasure Seeker";
                    default: return "Kingslayer";
                }
            }
        }

        public string Description
        {
            get
            {
                switch (kind)
                {
                    case BountyKind.Skeletons: return $"Defeat {goal} skeletons";
                    case BountyKind.Slimes: return $"Defeat {goal} slimes";
                    case BountyKind.Archers: return $"Defeat {goal} dark archers";
                    case BountyKind.Bombers: return $"Defeat {goal} bombers";
                    case BountyKind.Shieldbearers: return $"Defeat {goal} shieldbearers";
                    case BountyKind.Elites: return $"Defeat {goal} elite or champion enemies";
                    case BountyKind.Chests: return $"Open {goal} chests";
                    default: return $"Defeat {goal} level boss" + (goal > 1 ? "es" : "");
                }
            }
        }
    }

    public static class Bounties
    {
        public const int Slots = 3;
        private const string PrefKey = "Bounties";

        [Serializable]
        private sealed class Data
        {
            public List<Bounty> list = new List<Bounty>();
            public int completed;
        }

        private static Data data;

        private static Data State
        {
            get
            {
                if (data != null)
                    return data;
                try
                {
                    data = PlayerPrefs.HasKey(PrefKey) ? JsonUtility.FromJson<Data>(PlayerPrefs.GetString(PrefKey)) : null;
                }
                catch (ArgumentException)
                {
                    data = null;
                }
                data = data ?? new Data();
                while (data.list.Count < Slots)
                    data.list.Add(Create());
                return data;
            }
        }

        public static IReadOnlyList<Bounty> Active => State.list;
        public static int Completed => State.completed;

        private static void Save() => PlayerPrefs.SetString(PrefKey, JsonUtility.ToJson(State));

        // Nova igra počinje sa novim ugovorima.
        public static void Reset()
        {
            data = null;
            PlayerPrefs.DeleteKey(PrefKey);
        }

        // Težina raste sa brojem završenih ugovora i otključanih nivoa.
        private static Bounty Create()
        {
            int tier = (data != null ? data.completed : 0) / 3 + GameLevels.Unlocked;
            var kind = (BountyKind)UnityEngine.Random.Range(0, Enum.GetValues(typeof(BountyKind)).Length);
            int goal;
            switch (kind)
            {
                case BountyKind.Elites: goal = 3 + tier; break;
                case BountyKind.Chests: goal = 2 + tier; break;
                case BountyKind.Bosses: goal = 1; break;
                case BountyKind.Shieldbearers: goal = 4 + tier * 2; break;
                case BountyKind.Bombers: goal = 5 + tier * 2; break;
                default: goal = 10 + tier * 5; break;
            }
            return new Bounty { kind = kind, goal = goal, gold = 60 + goal * 12 + tier * 40 };
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            data = null;
            EnemyHealth.AnyDied += EnemyDied;
            LootChest.AnyOpened += chest => Count(BountyKind.Chests);
        }

        private static void EnemyDied(EnemyHealth enemy)
        {
            if (enemy == null || !enemy.GrantsExperience)
                return;
            if (enemy.GetComponent<IBossBehaviour>() != null)
            {
                Count(BountyKind.Bosses);
                return;
            }
            var level = enemy.GetComponent<EnemyLevel>();
            if (level != null && level.IsElite)
                Count(BountyKind.Elites);
            if (enemy.GetComponent<BomberAI>() != null)
                Count(BountyKind.Bombers);
            else if (enemy.GetComponent<ShieldbearerAI>() != null)
                Count(BountyKind.Shieldbearers);
            else if (enemy.GetComponent<DarkArcherAI>() != null)
                Count(BountyKind.Archers);
            else if (enemy.name.IndexOf("Slime", StringComparison.OrdinalIgnoreCase) >= 0)
                Count(BountyKind.Slimes);
            else if (enemy.name.IndexOf("Skeleton", StringComparison.OrdinalIgnoreCase) >= 0)
                Count(BountyKind.Skeletons);
        }

        private static void Count(BountyKind kind)
        {
            bool changed = false;
            foreach (var bounty in State.list)
            {
                if (bounty.kind != kind || bounty.Done)
                    continue;
                bounty.progress++;
                changed = true;
                var player = UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
                if (player != null && (bounty.Done || bounty.progress % Mathf.Max(1, bounty.goal / 5) == 0))
                    CombatFeedback.Popup(bounty.Done ? "CONTRACT DONE: " + bounty.Title.ToUpperInvariant() : bounty.Title.ToUpperInvariant() + " " + bounty.progress + "/" + bounty.goal,
                        player.transform.position + Vector3.up * .8f, bounty.Done ? new Color(1f, .85f, .3f) : new Color(.8f, .75f, 1f), bounty.Done ? 3.6f : 2.8f, bounty.Done ? 1.8f : 1f);
                if (bounty.Done)
                    Sfx.Play(SfxId.LevelUp, .6f);
            }
            if (changed)
                Save();
        }

        // Preuzimanje nagrade: zlato i predmet bolje retkosti, a na mesto ugovora dolazi novi.
        public static Item Claim(Bounty bounty, PlayerEquipment equipment)
        {
            var state = State;
            int index = state.list.IndexOf(bounty);
            if (index < 0 || !bounty.Done || equipment == null)
                return null;
            equipment.AddGold(bounty.gold);
            var reward = ItemCatalog.CreateGear(Mathf.Clamp(GameLevels.Unlocked * 30 - 10 + state.completed * 2, 5, 100), 2.5f);
            if (!equipment.AddItem(reward))
                LootDrop.SpawnItem(reward, equipment.transform.position);
            state.completed++;
            state.list[index] = Create();
            Save();
            return reward;
        }
    }
}
