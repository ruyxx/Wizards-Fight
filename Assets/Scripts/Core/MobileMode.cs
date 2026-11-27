// Režim za telefon: uključen je sam na telefonu, a na računaru može da se uključi u opcijama (za probu).
// Pri svakom učitavanju scene prilagođava ekran: korisnički interfejs se skalira po visini, 60 sličica u sekundi.
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AWizardsFight
{
    public static class MobileMode
    {
        private const string PrefKey = "TouchControls";

        // 0 = automatski (samo na telefonu), 1 = uvek uključeno, 2 = isključeno.
        public static int Setting
        {
            get => PlayerPrefs.GetInt(PrefKey, 0);
            set
            {
                PlayerPrefs.SetInt(PrefKey, Mathf.Clamp(value, 0, 2));
                PlayerPrefs.Save();
            }
        }

        // Kontrole na dodir postoje samo u verziji za telefon; na računaru (i u Unity editoru) nikad.
        public static bool Enabled => Application.isMobilePlatform;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            if (Application.isMobilePlatform)
            {
                Application.targetFrameRate = 60;
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
                Input.multiTouchEnabled = true;
            }
        }

        // Široki telefoni (20:9) bi smanjili meni po širini; skaliranje po visini drži dugmad iste veličine.
        public static void Apply()
        {
            if (!Enabled)
                return;
            foreach (var scaler in Object.FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None))
                if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                    scaler.matchWidthOrHeight = 1f;
        }
    }

    // Stanje dodirnih kontrola za tekući frejm; čita ga GamepadInput, pa ga sve postojeće kontrole same prepoznaju.
    public static class TouchInput
    {
        public static Vector2 Move;
        private static readonly float[] pressedAt = { -1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f };

        public enum Button { Dash, Potion, Interact, Inventory, NextWeapon, Special, Map, Pause, Skills }

        public static void Press(Button button) => pressedAt[(int)button] = Time.unscaledTime;

        // Svako dugme čita tačno jedan deo igre, pa se dodir troši pri čitanju;
        // stari dodir (npr. dok je igra bila pauzirana) se zanemaruje.
        public static bool Pressed(Button button)
        {
            float at = pressedAt[(int)button];
            if (at < 0f)
                return false;
            pressedAt[(int)button] = -1f;
            return Time.unscaledTime - at < .4f;
        }
    }
}
