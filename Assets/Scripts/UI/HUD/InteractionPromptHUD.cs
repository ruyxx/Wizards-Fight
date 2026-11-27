// Prikazuje poruku za trenutno dostupnu interakciju.
using TMPro;
using UnityEngine;

namespace AWizardsFight
{
    public sealed class InteractionPromptHUD : MonoBehaviour
    {
        [SerializeField]
        private PlayerInteractor player = null;
        [SerializeField]
        private TMP_Text label = null;

        private void OnEnable()
        {
            if (player != null)
            {
                player.PromptChanged += Show;
                Show(player.CurrentPrompt);
            }
        }

        private void OnDisable()
        {
            if (player != null)
                player.PromptChanged -= Show;
            Show("");
        }

        private void Show(string text)
        {
            // Na telefonu poruku prikazuje dugme za interakciju.
            if (label != null)
                label.text = MobileMode.Enabled ? "" : text;
        }
    }
}
