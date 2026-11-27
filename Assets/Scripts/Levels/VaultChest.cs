// Zapečaćen kovčeg trezora: otvara se samo ključem koji nosi šampion nivoa i daje mnogo bolji plen.
using UnityEngine;

namespace AWizardsFight
{
    public sealed class VaultChest : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private string vaultId = "vault";
        [SerializeField, Range(1, 100)]
        private int itemLevel = 30;
        [SerializeField]
        private SpriteRenderer lid = null;
        private SpriteRenderer glow;

        public static string KeyFlag(string vaultId) => "key_" + vaultId;
        private string OpenedFlag => "vault_" + vaultId;
        public bool HasKey => SaveSystem.HasFlag(KeyFlag(vaultId));
        public bool IsOpened => SaveSystem.HasFlag(OpenedFlag);
        public string Prompt => HasKey ? "E - UNLOCK THE VAULT CHEST" : "SEALED - THE CHAMPION CARRIES THE KEY";

        private void Start()
        {
            glow = RuntimeSprites.Spawn("Vault Glow", RuntimeSprites.Glow, transform.position, new Color(1f, .8f, .3f, .45f), 2.6f, 3);
            glow.transform.SetParent(transform, true);
            glow.gameObject.AddComponent<TorchFlicker>();
            if (SaveSystem.HasFlag(OpenedFlag))
                ShowOpened();
        }

        public bool CanInteract(PlayerInteractor player) => player != null && !SaveSystem.HasFlag(OpenedFlag) && player.CanReach(this);

        public bool Interact(PlayerInteractor player)
        {
            if (!CanInteract(player))
                return false;
            if (!HasKey)
            {
                CombatFeedback.Popup("YOU NEED THE VAULT KEY", transform.position + Vector3.up, new Color(1f, .7f, .4f), 4f, 1.2f);
                return false;
            }

            SaveSystem.SetFlag(OpenedFlag);
            Vector2 position = (Vector2)transform.position + Vector2.down * .9f;
            for (int i = 0; i < 3; i++)
                LootDrop.SpawnItem(ItemCatalog.CreateGear(itemLevel, 3.5f), position);
            LootDrop.SpawnItem(ItemCatalog.CreateRandomRune(itemLevel + 20), position);
            LootDrop.SpawnItem(ItemCatalog.CreateRandomRune(itemLevel + 20), position);
            LootDrop.SpawnGold(Mathf.RoundToInt(80 * (1f + itemLevel / 25f)), position);
            player.GetComponent<PlayerPotions>()?.Add(1);
            CombatFeedback.Popup("VAULT OPENED!", transform.position + Vector3.up, new Color(1f, .85f, .3f), 5f, 1.6f);
            Sfx.Play(SfxId.LevelUp);
            ShowOpened();
            FindFirstObjectByType<SaveSystem>()?.Save();
            return true;
        }

        private void ShowOpened()
        {
            if (lid != null)
                lid.transform.localPosition += Vector3.up * .3f;
            if (glow != null)
                glow.color = new Color(.5f, .5f, .5f, .15f);
        }
    }
}
