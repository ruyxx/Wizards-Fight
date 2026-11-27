// Menja stanje vrata i njihov fizički prikaz kada se vrata otvore.
using UnityEngine;

namespace AWizardsFight
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class Door : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer panel = null;
        [SerializeField]
        private TMPro.TMP_Text label = null;
        public bool IsOpen { get; private set; }

        public event System.Action Opened;

        public bool Open()
        {
            if (IsOpen)
                return false;
            IsOpen = true;
            GetComponent<BoxCollider2D>().enabled = false;
            if (panel != null)
                panel.enabled = false;
            if (label != null)
                label.text = "BOSS DOOR OPEN";
            Opened?.Invoke();
            return true;
        }
    }
}
