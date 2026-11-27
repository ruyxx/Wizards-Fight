// Kovčeg koji proverava mogućnost otvaranja i dodeljuje izabranu nagradu.
using UnityEngine;

namespace AWizardsFight
{
    public sealed class LootChest : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private LootTable lootTable = null;
        [SerializeField]
        private PlayerLootReceiver player = null;
        [SerializeField, Min(.1f)]
        private float interactionRange = 1.5f;
        [SerializeField]
        private SpriteRenderer lid = null;
        [SerializeField]
        private TMPro.TMP_Text label = null;
        public bool IsOpened { get; private set; }
        // Javlja da je igrač otvorio kovčeg (za dodatni plen opreme).
        public static event System.Action<LootChest> AnyOpened;
        public string Prompt => "E - OPEN CHEST";

        public bool CanInteract(PlayerInteractor actor) => !IsOpened 
            && lootTable != null 
            && lootTable.IsValid 
            && actor != null 
            && actor.GetComponent<PlayerLootReceiver>() != null 
            && actor.CanReach(this, interactionRange);

        public bool Interact(PlayerInteractor actor) => CanInteract(actor) 
            && TryOpen(actor.GetComponent<PlayerLootReceiver>());

        public bool TryOpen(PlayerLootReceiver receiver)
        {
            if (!isActiveAndEnabled 
                || IsOpened 
                || receiver == null 
                || Time.timeScale <= 0 
                || lootTable == null 
                || !lootTable.IsValid)
                return false;
            var health = receiver.GetComponent<PlayerHealth>();
            if (health == null 
                || health.IsDead 
                || Vector2.Distance(transform.position, receiver.transform.position) > interactionRange)
                return false;
            var actor = receiver.GetComponent<PlayerInteractor>();
            if (actor == null || !actor.CanReach(this, interactionRange) || (player != null && player != receiver))
                return false;
            IsOpened = true;
            var reward = lootTable.Roll(Random.value, Random.value);
            if (receiver.Receive(reward) == WeaponAcquisition.Rejected)
            {
                IsOpened = false;
                return false;
            }

            ShowOpened();
            AnyOpened?.Invoke(this);
            return true;
        }

        // Označava kovčeg kao već otvoren pri nastavku partije, bez nove nagrade.
        public void RestoreOpened()
        {
            if (IsOpened)
                return;
            IsOpened = true;
            ShowOpened();
        }

        private void ShowOpened()
        {
            if (lid != null)
                lid.transform.localPosition += Vector3.up * .3f;
            if (label != null)
                label.text = "OPENED";
        }
    }
}
