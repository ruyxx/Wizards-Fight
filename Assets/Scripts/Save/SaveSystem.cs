// Čuva napredak posle svake očišćene sobe i vraća ga kada igrač izabere nastavak.
// Posle pobede nad bossom prvog nivoa čuva lika za silazak u sledeći nivo.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AWizardsFight
{
    // Vraćanje sačuvanog stanja mora da se desi pre Start metoda ostalih objekata (događaji nivoa čitaju oznake).
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public sealed class SaveSystem : MonoBehaviour
    {
        private const string Key = "WizardsFightSave";

        [Serializable]
        private sealed class SaveData
        {
            public string scene;
            public List<string> defeatedEnemies = new List<string>();
            public List<string> openedChests = new List<string>();
            public bool leverActivated;
            public List<string> weapons = new List<string>();
            public string selectedWeapon;
            public int potions;
            public float health;
            public bool hasPosition;
            public Vector2 position;
            public float activeSeconds;
            public int level = 1;
            public float experience;
            public int pendingChoices;
            public List<string> perks = new List<string>();
            public PlayerEquipment.Snapshot equipment;
            public List<string> flags = new List<string>();
        }

        // Oznake događaja u nivou (zaseda pređena, ključ nađen, tajni prolaz otvoren...).
        private static readonly HashSet<string> flags = new HashSet<string>();
        public static bool HasFlag(string flag) => flags.Contains(flag);
        public static void SetFlag(string flag) => flags.Add(flag);
        public static int CountFlags(string prefix) => flags.Count(flag => flag.StartsWith(prefix, StringComparison.Ordinal));


        public static bool HasSave => PlayerPrefs.HasKey(Key);
        // Meni postavlja ovu oznaku pre učitavanja scene kada igrač bira nastavak.
        public static bool ContinueRequested { get; set; }
        public static bool IsRestoring { get; private set; }
        public event Action Saved;

        private readonly Dictionary<string, EnemyHealth> enemies = new Dictionary<string, EnemyHealth>();
        private readonly Dictionary<string, LootChest> chests = new Dictionary<string, LootChest>();
        private readonly HashSet<string> defeated = new HashSet<string>();
        private PlayerHealth player;
        private PlayerWeapons weapons;
        private PlayerPotions potions;
        private PlayerProgression progression;
        private PlayerEquipment equipment;
        private RunTimer timer;
        private BossEncounter encounter;
        private Lever lever;
        private CombatGate[] gates;

        public static void Delete()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }

        // Scena iz sačuvane partije (stare partije bez oznake pripadaju prvom nivou).
        public static string SavedScene
        {
            get
            {
                if (!HasSave)
                    return "Dungeon";
                try
                {
                    var data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key));
                    return data != null && !string.IsNullOrEmpty(data.scene) ? data.scene : "Dungeon";
                }
                catch (ArgumentException)
                {
                    return "Dungeon";
                }
            }
        }

        private void Awake()
        {
            // Oznake važe samo za trenutni nivo; nastavak ih učitava iz sačuvane partije.
            flags.Clear();
            LevelModifiers.Reset();
            player = FindFirstObjectByType<PlayerHealth>();
            if (player != null)
            {
                weapons = player.GetComponent<PlayerWeapons>();
                potions = player.GetComponent<PlayerPotions>();
                progression = player.GetComponent<PlayerProgression>();
                equipment = player.GetComponent<PlayerEquipment>();
            }

            timer = FindFirstObjectByType<RunTimer>();
            encounter = FindFirstObjectByType<BossEncounter>();
            lever = FindFirstObjectByType<Lever>();
            gates = FindObjectsByType<CombatGate>(FindObjectsSortMode.None);
            // Završni protivnik se ne čuva; pobeda završava nivo.
            foreach (var enemy in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
                if (enemy.GetComponent<IBossBehaviour>() == null)
                    enemies[IdOf(enemy.transform)] = enemy;
            foreach (var chest in FindObjectsByType<LootChest>(FindObjectsSortMode.None))
                chests[IdOf(chest.transform)] = chest;
        }

        private void OnEnable()
        {
            foreach (var pair in enemies)
            {
                string id = pair.Key;
                pair.Value.Died += () => defeated.Add(id);
            }

            foreach (var gate in gates)
                gate.Opened += Save;
            if (lever != null)
                lever.Activated += Save;
            if (encounter != null)
                encounter.StateChanged += EncounterChanged;
        }

        private void OnDisable()
        {
            foreach (var gate in gates)
                if (gate != null)
                    gate.Opened -= Save;
            if (lever != null)
                lever.Activated -= Save;
            if (encounter != null)
                encounter.StateChanged -= EncounterChanged;
        }

        private void Start()
        {
            bool load = ContinueRequested;
            ContinueRequested = false;
            if (load && HasSave)
                Restore();
            // Kamp je uvek tačka nastavka: po dolasku u kamp partija se odmah čuva.
            if (SceneManager.GetActiveScene().name == GameLevels.CampScene)
                Save();
        }

        // Prelazak u drugu scenu (iz kampa u nivo ili nazad) sa celim likom i punim zdravljem.
        public void TravelTo(string scene)
        {
            if (player == null)
                return;
            var data = CaptureCharacter();
            data.scene = scene;
            data.health = player.MaxHealth;
            data.hasPosition = false;
            Write(data);
            ContinueRequested = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene);
        }

        // Ime u hijerarhiji i početni položaj zajedno daju stabilnu oznaku objekta u sceni.
        private static string IdOf(Transform target)
        {
            string path = target.name;
            for (var parent = target.parent; parent != null; parent = parent.parent)
                path = parent.name + "/" + path;
            Vector3 p = target.position;
            return path + "@" + Mathf.RoundToInt(p.x * 10) + "," + Mathf.RoundToInt(p.y * 10);
        }

        public void Save()
        {
            if (IsRestoring || player == null || player.IsDead)
                return;
            var data = CaptureCharacter();
            data.scene = SceneManager.GetActiveScene().name;
            data.leverActivated = lever != null && lever.IsActivated;
            data.hasPosition = true;
            data.position = player.transform.position;
            data.flags.AddRange(flags);
            data.defeatedEnemies.AddRange(defeated);
            foreach (var pair in chests)
                if (pair.Value != null && pair.Value.IsOpened)
                    data.openedChests.Add(pair.Key);
            Write(data);
            Saved?.Invoke();
        }

        // Stanje lika koje prelazi i u sledeći nivo: oružje, napici, nivo i pojačanja.
        private SaveData CaptureCharacter()
        {
            var data = new SaveData
            {
                potions = potions != null ? potions.Count : 0,
                health = player.CurrentHealth,
                activeSeconds = timer != null ? timer.ActiveSeconds : 0f
            };
            if (weapons != null)
            {
                foreach (WeaponType type in Enum.GetValues(typeof(WeaponType)))
                {
                    var owned = weapons.GetOwned(type);
                    if (owned != null)
                        data.weapons.Add(owned.name);
                }

                data.selectedWeapon = weapons.Selected != null ? weapons.Selected.name : null;
            }

            if (equipment != null)
                data.equipment = equipment.Capture();
            if (progression != null)
            {
                data.level = progression.Level;
                data.experience = progression.Experience;
                data.pendingChoices = progression.PendingChoices;
                data.perks.AddRange(progression.TakenPerks);
            }

            return data;
        }

        private static void Write(SaveData data)
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private void Restore()
        {
            SaveData data;
            try
            {
                data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key));
            }
            catch (ArgumentException)
            {
                Debug.LogWarning("Saved game could not be read and was removed.");
                Delete();
                return;
            }

            if (data == null)
                return;
            IsRestoring = true;
            try
            {
                // Oznake nivoa važe samo ako je sačuvana partija iz iste scene.
                if (data.scene == SceneManager.GetActiveScene().name && data.flags != null)
                    foreach (string flag in data.flags)
                        flags.Add(flag);
                foreach (string id in data.defeatedEnemies)
                    if (enemies.TryGetValue(id, out var enemy) && enemy != null)
                        enemy.RemoveForRestore();
                foreach (string id in data.openedChests)
                    if (chests.TryGetValue(id, out var chest) && chest != null)
                        chest.RestoreOpened();
                if (data.leverActivated && lever != null)
                    lever.RestoreActivated();
                RestoreWeapons(data);
                potions?.RestoreCount(data.potions);
                // Pojačanja se vraćaju pre zdravlja, jer mogu da povećaju najveće zdravlje.
                progression?.Restore(data.level, data.experience, data.perks, data.pendingChoices);
                // Oprema se vraća posle oružja (rune u oružju zavise od njegove retkosti), a pre zdravlja.
                if (data.equipment != null)
                    equipment?.Restore(data.equipment);
                player.RestoreHealth(data.health);
                // Stare partije bez oznake položaja imaju sačuvan položaj.
                if (data.hasPosition || string.IsNullOrEmpty(data.scene))
                {
                    var body = player.GetComponent<Rigidbody2D>();
                    if (body != null)
                        body.position = data.position;
                    player.transform.position = data.position;
                }

                // Vreme se meri po nivou: novi nivo počinje od nule.
                timer?.RestoreSeconds(data.scene == SceneManager.GetActiveScene().name ? data.activeSeconds : 0f);
                FindFirstObjectByType<CameraFollow>()?.SnapToTarget();
            }
            finally
            {
                IsRestoring = false;
            }
        }

        private void RestoreWeapons(SaveData data)
        {
            if (weapons == null)
                return;
            var byName = new Dictionary<string, WeaponData>();
            foreach (var weapon in Resources.FindObjectsOfTypeAll<WeaponData>())
                byName[weapon.name] = weapon;
            foreach (string name in data.weapons)
                if (byName.TryGetValue(name, out var weapon))
                    weapons.Acquire(weapon);
            if (data.selectedWeapon != null && byName.TryGetValue(data.selectedWeapon, out var selected))
                weapons.TrySelect(selected.Type);
        }

        // Lik sa svim plenom se sledeći put pojavljuje u kampu (npr. posle smrti u Riftu).
        public void SaveForCamp()
        {
            if (player == null)
                return;
            var data = CaptureCharacter();
            data.scene = GameLevels.CampScene;
            data.health = player.MaxHealth;
            data.hasPosition = false;
            Write(data);
        }

        private void EncounterChanged()
        {
            if (encounter.State != BossEncounterState.Victory)
                return;
            // Pobeda otključava sledeći nivo; lik se vraća u kamp sa svime što je skupio.
            var level = GameLevels.ByScene(SceneManager.GetActiveScene().name);
            if (level != null)
                GameLevels.Unlock(level.Index + 1);
            // Kraj priče: Kralj Senki otključava sledeći Torment stepen.
            if (level != null && level.Index == GameLevels.All.Length && Torment.UnlockNext())
                CombatFeedback.Popup(Torment.Name(Torment.Unlocked) + " UNLOCKED", player.transform.position + Vector3.up * 1.5f, new Color(1f, .4f, .3f), 5f, 3f);
            var data = CaptureCharacter();
            data.scene = GameLevels.CampScene;
            data.health = player.MaxHealth;
            data.hasPosition = false;
            Write(data);
        }
    }
}
