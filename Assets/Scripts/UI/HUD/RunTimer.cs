// Meri aktivno vreme partije i prikazuje proteklo vreme.
using TMPro;
using UnityEngine;

namespace AWizardsFight
{
    // Measures active gameplay only; pause menus contribute zero scaled delta time.
    public sealed class RunTimer : MonoBehaviour
    {
        [SerializeField]
        private PlayerHealth player = null;
        [SerializeField]
        private BossEncounter encounter = null;
        [SerializeField]
        private TMP_Text victoryDetails = null;
        [SerializeField]
        private TMP_Text gameOverDetails = null;
        [SerializeField]
        private string victoryMessage = "The Dark Sorcerer is defeated.";
        public float ActiveSeconds { get; private set; }

        public void RestoreSeconds(float seconds) => ActiveSeconds = Mathf.Max(0f, seconds);

        private void OnEnable()
        {
            player.Died += ShowGameOver;
            encounter.StateChanged += EncounterChanged;
        }

        private void OnDisable()
        {
            if (player != null)
                player.Died -= ShowGameOver;
            if (encounter != null)
                encounter.StateChanged -= EncounterChanged;
        }

        private void Update()
        {
            if (!player.IsDead && encounter.State != BossEncounterState.Victory)
                ActiveSeconds += Time.deltaTime;
        }

        private string Duration()
        {
            int seconds = Mathf.FloorToInt(ActiveSeconds);
            return (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        }

        private void ShowGameOver() => gameOverDetails.text = (RiftDirector.Active ? RiftDirector.GameOverLine() + "\n" : EndlessDirector.Active ? EndlessDirector.GameOverLine() + "\n" : "") + "Active time: " + Duration();

        private void EncounterChanged()
        {
            if (encounter.State != BossEncounterState.Victory)
                return;
            string level = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            int place = Leaderboard.Record(level, ActiveSeconds);
            string record = place == 1 ? "\n<color=#FFD966>NEW RECORD!</color>" : place > 0 ? "\nRank #" + place + " on the records" : "";
            var times = Leaderboard.Times(level);
            victoryDetails.text = victoryMessage + "\nActive time: " + Duration() + "   Best: " + Leaderboard.Format(times[0]) + record;
        }
    }
}
