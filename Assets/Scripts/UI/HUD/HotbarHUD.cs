// Prikazuje izbor oružja i napitke na donjoj traci interfejsa.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AWizardsFight
{
    public sealed class HotbarHUD : MonoBehaviour
    {
        [SerializeField]
        private PlayerWeapons weapons = null;
        [SerializeField]
        private PlayerPotions potions = null;
        [SerializeField]
        private Image[] icons = new Image[4];
        [SerializeField]
        private Image[] rarityStrips = new Image[3];
        [SerializeField]
        private Outline[] selection = new Outline[3];
        [SerializeField]
        private TMP_Text[] labels = new TMP_Text[4];

        private void OnEnable()
        {
            if (weapons != null)
                weapons.Changed += Refresh;
            if (potions != null)
                potions.Changed += Refresh;
            Refresh();
        }

        private void Start() => Refresh();

        private void OnDisable()
        {
            if (weapons != null)
                weapons.Changed -= Refresh;
            if (potions != null)
                potions.Changed -= Refresh;
        }

        private void Refresh()
        {
            if (weapons == null || potions == null)
                return;
            for (int i = 0; i < 3; i++)
            {
                var weapon = weapons.GetOwned((WeaponType)i);
                icons[i].color = weapon != null ? Color.white : new Color(1, 1, 1, .2f);
                if (weapon != null && weapon.Icon != null)
                    icons[i].sprite = weapon.Icon;
                labels[i].text = (i + 1) + "  " + (weapon != null ? weapon.WeaponName : "LOCKED");
                rarityStrips[i].color = weapon != null ? WeaponRarityPresentation.ColorFor(weapon.Rarity) : new Color(.16f, .18f, .23f);
                selection[i].enabled = weapon != null && weapons.Selected == weapon;
            }

            labels[3].text = "4  POTION x" + potions.Count;
            icons[3].color = potions.Count > 0 ? Color.white : new Color(1, 1, 1, .2f);
        }
    }
}
