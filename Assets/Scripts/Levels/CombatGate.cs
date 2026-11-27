// Prati svoju grupu neprijatelja i otvara prolaz kada su svi čuvari poraženi.
using TMPro;
using UnityEngine;

namespace AWizardsFight
{
   
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class CombatGate : MonoBehaviour
    {
        [SerializeField]
        private EnemyHealth[] guards = new EnemyHealth[0];
        [SerializeField]
        private SpriteRenderer panel = null;
        [SerializeField]
        private TMP_Text label = null;
        public bool IsOpen { get; private set; }
        public int Remaining { get; private set; }
        public event System.Action Opened;
        private bool refreshed;

        private void OnEnable()
        {
            foreach (var guard in guards)
                if (guard != null)
                    guard.Died += Refresh;
        }

        private void Start() => Refresh();

        private void OnDisable()
        {
            foreach (var guard in guards)
                if (guard != null)
                    guard.Died -= Refresh;
        }

        private void Refresh()
        {
            Remaining = 0;
            foreach (var guard in guards)
                if (guard != null && !guard.IsDead)
                    Remaining++;
            bool wasOpen = IsOpen;
            IsOpen = Remaining == 0;
            if (refreshed && IsOpen && !wasOpen)
                Opened?.Invoke();
            refreshed = true;
            GetComponent<BoxCollider2D>().enabled = !IsOpen;
            if (panel != null)
                panel.enabled = !IsOpen;
            if (label != null)
                label.text = IsOpen ? "ROOM CLEARED" : "DEFEAT GUARDS: " + Remaining;
        }
    }
}
