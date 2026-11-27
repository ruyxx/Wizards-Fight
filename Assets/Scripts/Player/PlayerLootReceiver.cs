// Primenjuje osvojenu nagradu i povezuje oružje, napitke i obaveštenje o plenu.
using System;
using UnityEngine;

namespace AWizardsFight
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerWeapons), typeof(PlayerPotions))]
    public sealed class PlayerLootReceiver : MonoBehaviour
    {
        private PlayerWeapons weapons;
        private PlayerPotions potions;
        public event Action<WeaponData, WeaponAcquisition> Rewarded;

        private void Awake()
        {
            weapons = GetComponent<PlayerWeapons>();
            potions = GetComponent<PlayerPotions>();
        }

        public WeaponAcquisition Receive(WeaponData weapon)
        {
            if (!isActiveAndEnabled)
                return WeaponAcquisition.Rejected;
            var result = weapons.Acquire(weapon);
            if (result == WeaponAcquisition.Rejected)
                return result;
            if (result == WeaponAcquisition.Duplicate && !potions.Add(1))
                return WeaponAcquisition.Rejected;
            Rewarded?.Invoke(weapon, result);
            return result;
        }
    }
}
