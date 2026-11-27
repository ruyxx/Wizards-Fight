// Čuva tastere koje igrač može da promeni u podešavanjima.
using System;
using UnityEngine;

namespace AWizardsFight
{
    public enum GameAction
    {
        MoveUp,
        MoveDown,
        MoveLeft,
        MoveRight,
        Dash,
        Interact,
        Potion,
        Weapon1,
        Weapon2,
        Weapon3,
        Map,
        Inventory,
        Weapon4,
        Weapon5,
        Skills,
        Spell1,
        Spell2,
        Spell3,
        Spell4
    }

    public static class Keybinds
    {
        private static readonly KeyCode[] Defaults =
        {
            KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D, KeyCode.Space, KeyCode.E,
            KeyCode.Alpha4, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.M, KeyCode.B, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.K,
            KeyCode.Q, KeyCode.R, KeyCode.F, KeyCode.G
        };

        private static KeyCode[] current;

        public static int Count => Defaults.Length;

        public static KeyCode Get(GameAction action)
        {
            Load();
            return current[(int)action];
        }

        public static bool Held(GameAction action) => Input.GetKey(Get(action));

        public static bool Pressed(GameAction action) => Input.GetKeyDown(Get(action));

        public static void Set(GameAction action, KeyCode key)
        {
            Load();
            // Ako je taster već zauzet, druga radnja dobija stari taster ove radnje.
            for (int i = 0; i < current.Length; i++)
                if (current[i] == key && i != (int)action)
                {
                    current[i] = current[(int)action];
                    PlayerPrefs.SetInt(PrefKey((GameAction)i), (int)current[i]);
                }

            current[(int)action] = key;
            PlayerPrefs.SetInt(PrefKey(action), (int)key);
            PlayerPrefs.Save();
        }

        public static void ResetAll()
        {
            Load();
            for (int i = 0; i < current.Length; i++)
            {
                current[i] = Defaults[i];
                PlayerPrefs.DeleteKey(PrefKey((GameAction)i));
            }

            PlayerPrefs.Save();
        }

        public static string Label(GameAction action)
        {
            switch (action)
            {
                case GameAction.MoveUp: return "MOVE UP";
                case GameAction.MoveDown: return "MOVE DOWN";
                case GameAction.MoveLeft: return "MOVE LEFT";
                case GameAction.MoveRight: return "MOVE RIGHT";
                case GameAction.Dash: return "DASH";
                case GameAction.Interact: return "INTERACT";
                case GameAction.Potion: return "POTION";
                case GameAction.Weapon1: return "BASIC WAND";
                case GameAction.Weapon2: return "FIRE STAFF";
                case GameAction.Weapon3: return "ICE STAFF";
                case GameAction.Map: return "MAP";
                case GameAction.Weapon4: return "STORM STAFF";
                case GameAction.Weapon5: return "VENOM STAFF";
                case GameAction.Skills: return "SKILL TREE";
                case GameAction.Spell1:
                case GameAction.Spell2:
                case GameAction.Spell3:
                case GameAction.Spell4: return PlayerSpells.All[action - GameAction.Spell1].Name;
                default: return "INVENTORY";
            }
        }

        public static string KeyName(KeyCode key)
        {
            string name = key.ToString();
            return name.StartsWith("Alpha", StringComparison.Ordinal) ? name.Substring(5) : name.ToUpperInvariant();
        }

        private static string PrefKey(GameAction action) => "Key_" + action;

        private static void Load()
        {
            if (current != null)
                return;
            current = new KeyCode[Defaults.Length];
            for (int i = 0; i < Defaults.Length; i++)
                current[i] = (KeyCode)PlayerPrefs.GetInt(PrefKey((GameAction)i), (int)Defaults[i]);
            // Inventar je ranije bio na I; stari sačuvan podrazumevani taster prelazi na novi B.
            if (PlayerPrefs.GetInt("KeysInventoryB", 0) == 0)
            {
                PlayerPrefs.SetInt("KeysInventoryB", 1);
                if (current[(int)GameAction.Inventory] == KeyCode.I)
                {
                    current[(int)GameAction.Inventory] = KeyCode.B;
                    PlayerPrefs.SetInt(PrefKey(GameAction.Inventory), (int)KeyCode.B);
                }
            }
        }
    }
}
