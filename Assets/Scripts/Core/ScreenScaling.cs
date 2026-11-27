// Interfejs je napravljen za 1920x1080. Na širem prozoru se skalira po visini, a na užem (4:3, uzan prozor)
// po širini, pa meniji i HUD uvek staju u prozor bilo koje veličine. Prati i promenu veličine prozora tokom igre.
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AWizardsFight
{
    public sealed class ScreenScaling : MonoBehaviour
    {
        private const float ReferenceAspect = 1920f / 1080f;
        private int width, height;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            var keeper = new GameObject("Screen Scaling");
            DontDestroyOnLoad(keeper);
            keeper.AddComponent<ScreenScaling>();
            SceneManager.sceneLoaded += (scene, mode) => Apply();
        }

        private void Update()
        {
            if (Screen.width == width && Screen.height == height)
                return;
            width = Screen.width;
            height = Screen.height;
            Apply();
        }

        public static void Apply()
        {
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : ReferenceAspect;
            float match = aspect >= ReferenceAspect ? 1f : 0f;
            foreach (var scaler in Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                    scaler.matchWidthOrHeight = match;
        }
    }
}
