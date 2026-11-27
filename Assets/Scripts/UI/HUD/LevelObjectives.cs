// Zadaci nivoa u HUD-u (ispod mini-mape): glavni cilj korak po korak i sporedni ciljevi (zaseda, šampion, trezor, tajne, beleške).
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AWizardsFight
{
    [DisallowMultipleComponent]
    public sealed class LevelObjectives : MonoBehaviour
    {
        private TextMeshProUGUI text;
        private CombatGate[] gates;
        private Lever lever;
        private BossEncounter encounter;
        private AmbushEvent[] ambushes;
        private ChampionEvent[] champions;
        private VaultChest[] vaults;
        private CrackedWall[] secrets;
        private LoreNote[] notes;
        private int secretTotal;
        private float nextRefresh;
        private GameLevels.Level level;

        private void Start()
        {
            level = GameLevels.ByScene(SceneManager.GetActiveScene().name);
            Transform hud = null;
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (canvas.name == "HUD")
                    hud = canvas.transform;
            if (hud == null || level == null)
            {
                enabled = false;
                return;
            }

            gates = FindObjectsByType<CombatGate>(FindObjectsSortMode.None);
            lever = FindFirstObjectByType<Lever>();
            encounter = FindFirstObjectByType<BossEncounter>();
            ambushes = FindObjectsByType<AmbushEvent>(FindObjectsSortMode.None);
            champions = FindObjectsByType<ChampionEvent>(FindObjectsSortMode.None);
            vaults = FindObjectsByType<VaultChest>(FindObjectsSortMode.None);
            secrets = FindObjectsByType<CrackedWall>(FindObjectsSortMode.None);
            secretTotal = secrets.Length + SaveSystem.CountFlags("secret_");
            notes = FindObjectsByType<LoreNote>(FindObjectsSortMode.None);

            var panel = UiFactory.Panel("Objectives", hud, new Vector2(-32, -306), new Vector2(430, 270), new Color(.05f, .04f, .09f, .72f));
            panel.rectTransform.anchorMin = panel.rectTransform.anchorMax = panel.rectTransform.pivot = Vector2.one;
            panel.raycastTarget = false;
            text = UiFactory.Text("Text", panel.transform, "", 20, Vector2.zero, new Vector2(400, 250), Color.white);
            text.enableAutoSizing = true;
            text.fontSizeMin = 17;
            text.fontSizeMax = 20;
            text.alignment = TextAlignmentOptions.TopLeft;
            Refresh();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh)
                return;
            nextRefresh = Time.unscaledTime + .5f;
            Refresh();
        }

        private static string Line(bool done, string label) => done ? "<color=#7CFF8A>[x]</color> <color=#8C8AA0>" + label + "</color>" : "<color=#FFD84D>[ ]</color> " + label;

        private void Refresh()
        {
            if (text == null)
                return;
            var builder = new StringBuilder();
            builder.AppendLine("<b><color=#BA9AFF>" + level.Title + "</color></b>");
            if (RunEvents.Current != null)
                builder.AppendLine("<color=#" + ColorUtility.ToHtmlStringRGB(RunEvents.Current.Color) + ">" + RunEvents.Current.Name + "</color>  <size=17><color=#9993B0>" + RunEvents.Current.Description + "</color></size>");
            int open = gates.Count(gate => gate != null && gate.IsOpen);
            bool gatesDone = open == gates.Length;
            bool leverDone = lever == null || lever.IsActivated;
            bool bossDone = encounter != null && encounter.State == BossEncounterState.Victory;
            builder.AppendLine(Line(gatesDone, "Clear the guarded chambers  " + open + "/" + gates.Length));
            if (lever != null)
                builder.AppendLine(Line(leverDone, "Pull the lever"));
            builder.AppendLine(Line(bossDone, "Defeat " + (encounter != null && encounter.Boss != null ? Capitalize(encounter.Boss.BossName) : "the boss")));
            builder.AppendLine("<size=18><color=#9993B0>Optional</color></size>");
            foreach (var ambush in ambushes)
                builder.AppendLine(Line(ambush.Completed, ambush.Running ? "<color=#FF7070>Survive the ambush!</color>" : "Survive the ambush"));
            foreach (var champion in champions)
                builder.AppendLine(Line(SaveSystem.HasFlag("champion_" + champion.ChampionName), "Slay the champion " + Capitalize(champion.ChampionName)));
            foreach (var vault in vaults)
                builder.AppendLine(Line(vault.IsOpened, vault.HasKey && !vault.IsOpened ? "Open the vault chest (you have the key)" : "Open the vault chest"));
            if (secretTotal > 0)
                builder.AppendLine(Line(SaveSystem.CountFlags("secret_") >= secretTotal, "Find secret passages  " + SaveSystem.CountFlags("secret_") + "/" + secretTotal));
            if (notes.Length > 0)
                builder.AppendLine(Line(SaveSystem.CountFlags("note_") >= notes.Length, "Read the lost pages  " + SaveSystem.CountFlags("note_") + "/" + notes.Length));
            text.text = builder.ToString();
        }

        private static string Capitalize(string value) => string.IsNullOrEmpty(value) ? value : value.Substring(0, 1) + value.Substring(1).ToLowerInvariant();
    }
}
