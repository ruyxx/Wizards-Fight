// Prikazuje trenutno izabrano oružje i promene izbora.
using TMPro;
using UnityEngine;

namespace AWizardsFight
{
    public sealed class WeaponSelectionHUD : MonoBehaviour
    {
        [SerializeField]
        private PlayerWeapons weapons = null;
        [SerializeField]
        private TMP_Text label = null;

        private void OnEnable()
        {
            if (weapons != null)
                weapons.Changed += Refresh;
            Refresh();
        }

        private void Start() => Refresh();

        private void OnDisable()
        {
            if (weapons != null)
                weapons.Changed -= Refresh;
        }

        private void Refresh()
        {
            if (weapons == null || label == null)
                return;
            var selected = weapons.Selected;
            label.text = selected != null ? selected.WeaponName + "  /  " + WeaponRarityPresentation.Label(selected.Rarity) + "\n" : "";
            for (int slot = 0; slot < 3; slot++)
            {
                var weapon = weapons.GetOwned((WeaponType)slot);
                label.text += (slot == 0 ? "" : "    ") + (slot + 1) + ": " + (weapon == null ? "LOCKED" : (weapon == selected ? "> " : "") + weapon.WeaponName);
            }
        }
    }
}
