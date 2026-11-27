// Privremeno prikazuje poruku o dobijenoj nagradi.
using TMPro;
using UnityEngine;

namespace AWizardsFight
{
    public sealed class LootPopup : MonoBehaviour
    {
        [SerializeField]
        private PlayerLootReceiver player = null;
        [SerializeField]
        private TMP_Text label = null;
        [SerializeField, Min(.1f)]
        private float duration = 3f;
        private float hideAt;

        private void OnEnable()
        {
            if (player != null)
                player.Rewarded += Show;
        }

        private void OnDisable()
        {
            if (player != null)
                player.Rewarded -= Show;
            if (label != null)
                label.text = "";
        }

        private void Update()
        {
            if (label != null && Time.time >= hideAt)
                label.text = "";
        }

        private void Show(WeaponData weapon, WeaponAcquisition result)
        {
            if (label == null)
                return;
            label.text = result == WeaponAcquisition.Duplicate ? "DUPLICATE  /  +1 HEALING POTION" : (result == WeaponAcquisition.Upgrade ? "UPGRADE!" : "NEW WEAPON") + "\n" + weapon.WeaponName + "  /  " + WeaponRarityPresentation.Label(weapon.Rarity);
            hideAt = Time.time + duration;
        }
    }
}
