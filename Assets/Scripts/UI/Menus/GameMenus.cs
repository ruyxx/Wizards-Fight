// Upravlja menijima, pauzom, podešavanjima i ponovnim pokretanjem igre.
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AWizardsFight
{
    public enum MenuState
    {
        Playing,
        Main,
        Paused,
        Options,
        GameOver,
        Victory
    }


    public sealed class GameMenus : MonoBehaviour
    {
        [SerializeField]
        private GameObject mainPanel = null, pausePanel = null, optionsPanel = null;
        [SerializeField]
        private GameObject gameOverPanel = null, victoryPanel = null;
        [SerializeField]
        private Button playButton = null, quitButton = null, resumeButton = null;
        [SerializeField]
        private Button pauseOptionsButton = null, mainOptionsButton = null, backButton = null;
        [SerializeField]
        private Button retryButton = null, gameOverMenuButton = null;
        [SerializeField]
        private Button victoryRetryButton = null, victoryMenuButton = null;
        [SerializeField]
        private Button pauseMenuButton = null, pauseRestartButton = null;
        [SerializeField]
        private Slider volumeSlider = null, musicSlider = null, effectsSlider = null;
        [SerializeField]
        private PlayerHealth player = null;
        [SerializeField]
        private BossEncounter encounter = null;
        private MenuState optionsReturn;
        public MenuState State { get; private set; }

        private void Awake()
        {
            Time.timeScale = 1;
            AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", .8f);
            State = SceneManager.GetActiveScene().name == "MainMenu" ? MenuState.Main : MenuState.Playing;
            Bind(playButton, Play);
            Bind(quitButton, Quit);
            Bind(resumeButton, Resume);
            Bind(pauseOptionsButton, OpenOptions);
            Bind(mainOptionsButton, OpenOptions);
            Bind(backButton, Back);
            Bind(retryButton, RetryFromCheckpoint);
            Bind(victoryRetryButton, Play);
            Bind(gameOverMenuButton, MainMenu);
            Bind(victoryMenuButton, MainMenu);
            Bind(pauseMenuButton, MainMenu);
            Bind(pauseRestartButton, RetryFromCheckpoint);
            if (State == MenuState.Main && SaveSystem.HasSave)
                AddContinueButton();
            if (State == MenuState.Main)
                AddRecordsButton();
            if (State == MenuState.Playing)
                AddDescendButton();
            volumeSlider.SetValueWithoutNotify(AudioListener.volume);
            volumeSlider.onValueChanged.AddListener(SetVolume);
            if (musicSlider != null)
            {
                musicSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("MusicVolume", .8f));
                musicSlider.onValueChanged.AddListener(SetMusicVolume);
            }

            if (effectsSlider != null)
            {
                effectsSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("EffectsVolume", .8f));
                effectsSlider.onValueChanged.AddListener(SetEffectsVolume);
            }
        }

        private void OnEnable()
        {
            if (player != null)
                player.Died += Lose;
            if (encounter != null)
                encounter.StateChanged += EncounterChanged;
        }

        private void Start() => Refresh();

        private void OnDisable()
        {
            if (player != null)
                player.Died -= Lose;
            if (encounter != null)
                encounter.StateChanged -= EncounterChanged;
        }

        private void Update()
        {
            if (!Application.isFocused || !(Input.GetKeyDown(KeyCode.Escape) || GamepadInput.PausePressed) || PerkChoiceMenu.IsOpen || InventoryMenu.IsOpen || CampMenu.IsOpen || MessagePanel.IsOpen || CharacterMenu.IsOpen || SkillTreeMenu.IsOpen || SettingsExtras.IsCapturingKey)
                return;
            if (State == MenuState.Playing)
                Pause();
            else if (State == MenuState.Paused)
                Resume();
            else if (State == MenuState.Options)
                Back();
        }

        // Dodaje dugme za nastavak iznad dugmeta za novu igru i pomera ostala dugmad naniže.
        private void AddContinueButton()
        {
            if (playButton == null)
                return;
            var continueButton = Instantiate(playButton, playButton.transform.parent);
            continueButton.name = "CONTINUE";
            continueButton.onClick = new Button.ButtonClickedEvent();
            Bind(continueButton, Continue);
            var label = continueButton.GetComponentInChildren<TMPro.TMP_Text>();
            if (label != null)
                label.text = "CONTINUE";
            var playLabel = playButton.GetComponentInChildren<TMPro.TMP_Text>();
            if (playLabel != null)
                playLabel.text = "NEW GAME";
            continueButton.transform.SetSiblingIndex(playButton.transform.GetSiblingIndex());
            var continueRect = (RectTransform)continueButton.transform;
            continueRect.anchoredPosition = ((RectTransform)playButton.transform).anchoredPosition + new Vector2(0, 50);
            foreach (var button in new[] { playButton, mainOptionsButton, quitButton })
            {
                if (button == null)
                    continue;
                var rect = (RectTransform)button.transform;
                rect.anchoredPosition -= new Vector2(0, 40);
            }

            foreach (var rect in new[] { continueRect, (RectTransform)playButton.transform, mainOptionsButton != null ? (RectTransform)mainOptionsButton.transform : null, quitButton != null ? (RectTransform)quitButton.transform : null })
                if (rect != null)
                    rect.sizeDelta = new Vector2(rect.sizeDelta.x, 62);
        }

        // Glavni meni dobija ENDLESS, CHARACTER i RECORDS, pa se sva dugmad slažu u jednu urednu kolonu.
        private void AddRecordsButton()
        {
            if (mainOptionsButton == null || quitButton == null || playButton == null)
                return;
            var endless = AddMainButton("ENDLESS", playButton.transform.GetSiblingIndex() + 1, () =>
            {
                DailyChallenge.Active = false;
                Time.timeScale = 1;
                SceneManager.LoadScene(EndlessDirector.SceneName);
            });
            var daily = AddMainButton("DAILY CHALLENGE", endless.transform.GetSiblingIndex() + 1, () => MessagePanel.Show("DAILY CHALLENGE", DailyChallenge.Description(), ("PLAY", () =>
            {
                DailyChallenge.Active = true;
                Time.timeScale = 1;
                SceneManager.LoadScene(EndlessDirector.SceneName);
            }), ("BACK", null)));
            var character = AddMainButton("CHARACTER", daily.transform.GetSiblingIndex() + 1, () => CharacterMenu.Open(transform));
            AddMainButton("RECORDS", quitButton.transform.GetSiblingIndex(), () => MessagePanel.Show("RECORDS", Leaderboard.Summary(), ("BACK", null)));
            var column = mainOptionsButton.transform.parent.GetComponentsInChildren<Button>(true)
                .Where(button => button.transform.parent == mainOptionsButton.transform.parent)
                .OrderBy(button => button.transform.GetSiblingIndex()).ToList();
            float top = column.Max(button => ((RectTransform)button.transform).anchoredPosition.y);
            for (int i = 0; i < column.Count; i++)
            {
                var rect = (RectTransform)column[i].transform;
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, top - i * 58);
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, 52);
            }
        }

        private Button AddMainButton(string text, int siblingIndex, UnityEngine.Events.UnityAction action)
        {
            var button = Instantiate(mainOptionsButton, mainOptionsButton.transform.parent);
            button.name = text;
            button.onClick = new Button.ButtonClickedEvent();
            Bind(button, action);
            var label = button.GetComponentInChildren<TMPro.TMP_Text>();
            if (label != null)
                label.text = text;
            button.transform.SetSiblingIndex(siblingIndex);
            return button;
        }

        // Za džojstik: prvo dugme otvorenog menija dobija fokus, pa se njime upravlja štapom i dugmetom A.
        private void SelectFirstButton()
        {
            var events = UnityEngine.EventSystems.EventSystem.current;
            if (events == null)
                return;
            GameObject panel = State == MenuState.Main ? mainPanel : State == MenuState.Paused ? pausePanel : State == MenuState.Options ? optionsPanel : State == MenuState.GameOver ? gameOverPanel : State == MenuState.Victory ? victoryPanel : null;
            Button first = panel != null ? panel.GetComponentsInChildren<Button>().FirstOrDefault(button => button.interactable) : null;
            events.SetSelectedGameObject(first != null ? first.gameObject : null);
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
                button.onClick.AddListener(action);
        }

        public void Pause()
        {
            if (State != MenuState.Playing)
                return;
            State = MenuState.Paused;
            Refresh();
        }

        public void Resume()
        {
            if (State != MenuState.Paused)
                return;
            State = MenuState.Playing;
            Refresh();
        }

        public void OpenOptions()
        {
            if (State != MenuState.Main && State != MenuState.Paused)
                return;
            optionsReturn = State;
            State = MenuState.Options;
            Refresh();
        }

        public void Back()
        {
            if (State != MenuState.Options)
                return;
            PlayerPrefs.Save();
            State = optionsReturn;
            Refresh();
        }

        public void SetVolume(float value)
        {
            AudioListener.volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("MasterVolume", AudioListener.volume);
        }

        public void SetMusicVolume(float value)
        {
            PlayerPrefs.SetFloat("MusicVolume", Mathf.Clamp01(value));
            FindFirstObjectByType<GameAudio>()?.ApplyVolumes();
        }

        public void SetEffectsVolume(float value)
        {
            PlayerPrefs.SetFloat("EffectsVolume", Mathf.Clamp01(value));
            FindFirstObjectByType<GameAudio>()?.ApplyVolumes();
        }

        public void Play()
        {
            // Nova partija briše prethodno sačuvan napredak i počinje u kampu.
            SaveSystem.Delete();
            Bounties.Reset();
            SaveSystem.ContinueRequested = false;
            GameLevels.Unlocked = 1;
            Time.timeScale = 1;
            SceneManager.LoadScene(GameLevels.CampScene);
        }

        public void Continue()
        {
            SaveSystem.ContinueRequested = true;
            Time.timeScale = 1;
            SceneManager.LoadScene(SaveSystem.SavedScene);
        }

        // Posle pobede nad bossom nivoa igrač se vraća u kamp, gde je otključan sledeći portal.
        private void AddDescendButton()
        {
            if (victoryRetryButton == null || GameLevels.ByScene(SceneManager.GetActiveScene().name) == null)
                return;
            var descend = Instantiate(victoryRetryButton, victoryRetryButton.transform.parent);
            descend.name = "RETURN TO CAMP";
            descend.onClick = new Button.ButtonClickedEvent();
            Bind(descend, Continue);
            var label = descend.GetComponentInChildren<TMPro.TMP_Text>();
            if (label != null)
                label.text = "RETURN TO CAMP";
            var rect = (RectTransform)descend.transform;
            rect.anchoredPosition = ((RectTransform)victoryRetryButton.transform).anchoredPosition;
            foreach (var button in new[] { victoryRetryButton, victoryMenuButton })
                if (button != null)
                    ((RectTransform)button.transform).anchoredPosition -= new Vector2(0, 85);
        }

        // Posle poraza igra se nastavlja od poslednje očišćene sobe, ako postoji sačuvan napredak.
        public void RetryFromCheckpoint()
        {
            // Endless se uvek ponovo igra od prvog talasa i ne dira sačuvanu priču.
            if (SceneManager.GetActiveScene().name == EndlessDirector.SceneName)
            {
                Time.timeScale = 1;
                SceneManager.LoadScene(EndlessDirector.SceneName);
                return;
            }

            if (SaveSystem.HasSave)
                Continue();
            else
                Play();
        }

        public void MainMenu()
        {
            Time.timeScale = 1;
            SceneManager.LoadScene("MainMenu");
        }

        public void Quit()
        {
            PlayerPrefs.Save();
            Application.Quit();
        }

        private void Lose()
        {
            if (State == MenuState.Victory)
                return;
            State = MenuState.GameOver;
            Refresh();
        }

        private void EncounterChanged()
        {
            if (encounter.State != BossEncounterState.Victory)
                return;
            State = MenuState.Victory;
            Refresh();
        }

        private void Refresh()
        {
            mainPanel.SetActive(State == MenuState.Main);
            pausePanel.SetActive(State == MenuState.Paused);
            optionsPanel.SetActive(State == MenuState.Options);
            gameOverPanel.SetActive(State == MenuState.GameOver);
            victoryPanel.SetActive(State == MenuState.Victory);
            Time.timeScale = State == MenuState.Playing || State == MenuState.Main ? 1 : 0;
            SelectFirstButton();
        }
    }
}
