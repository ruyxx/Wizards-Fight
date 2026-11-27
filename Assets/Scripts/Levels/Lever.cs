// Jednokratna poluga koja aktivira povezano otvaranje vrata.
using UnityEngine;

namespace AWizardsFight
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class Lever : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private Door door = null;
        [SerializeField]
        private Transform handle = null;
        [SerializeField]
        private TMPro.TMP_Text label = null;
        public bool IsActivated { get; private set; }
        public event System.Action Activated;
        public string Prompt => "E - ACTIVATE LEVER";

        public bool CanInteract(PlayerInteractor player) => !IsActivated 
            && door != null 
            && player != null 
            && player.CanReach(this);

        public bool Interact(PlayerInteractor player)
        {
            if (!CanInteract(player))
                return false;
            Activate();
            Activated?.Invoke();
            return true;
        }

        // Vraća polugu u aktivirano stanje pri nastavku partije.
        public void RestoreActivated()
        {
            if (!IsActivated && door != null)
                Activate();
        }

        private void Activate()
        {
            IsActivated = true;
            door.Open();
            if (handle != null)
                handle.localRotation = Quaternion.Euler(0, 0, -40);
            if (label != null)
                label.text = "LEVER ACTIVE - BOSS DOOR OPEN";
        }
    }
}
