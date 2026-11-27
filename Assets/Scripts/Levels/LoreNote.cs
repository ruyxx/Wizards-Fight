// Stara beleška na podu: igrač je čita pritiskom na E i saznaje deo priče nivoa.
using UnityEngine;

namespace AWizardsFight
{
    public sealed class LoreNote : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private string noteId = "note";
        [SerializeField]
        private string title = "TORN PAGE";
        [SerializeField, TextArea(3, 8)]
        private string text = "";
        private SpriteRenderer glow;

        public string Prompt => "E - READ " + title;

        private void Start()
        {
            glow = RuntimeSprites.Spawn("Note Glow", RuntimeSprites.Glow, transform.position, new Color(1f, .9f, .6f, .35f), 1.4f, 3);
            glow.transform.SetParent(transform, true);
            glow.gameObject.AddComponent<TorchFlicker>();
        }

        public bool CanInteract(PlayerInteractor player) => player != null && player.CanReach(this);

        public bool Interact(PlayerInteractor player)
        {
            if (!CanInteract(player))
                return false;
            bool first = !SaveSystem.HasFlag("note_" + noteId);
            SaveSystem.SetFlag("note_" + noteId);
            MessagePanel.Show(title, text);
            if (first)
                Sfx.Play(SfxId.Pickup, .6f);
            return true;
        }
    }
}
