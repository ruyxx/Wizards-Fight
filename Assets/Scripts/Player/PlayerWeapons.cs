// Pamti najbolje pronađeno oružje svake vrste i obrađuje izbor aktivnog oružja.
using System;
using UnityEngine;

namespace AWizardsFight
{
    public enum WeaponAcquisition
    {
        Rejected,
        NewWeapon,
        Upgrade,
        Duplicate
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerCombat), typeof(PlayerHealth))]
    public sealed class PlayerWeapons : MonoBehaviour
    {
        [SerializeField]
        private WeaponData startingWeapon = null;
        private readonly WeaponData[] owned = new WeaponData[WeaponTypes.Count];
        private PlayerCombat combat;
        private PlayerHealth health;
        public event Action Changed;
        public WeaponData Selected => combat != null ? combat.EquippedWeapon : null;

        private void Awake()
        {
            combat = GetComponent<PlayerCombat>();
            health = GetComponent<PlayerHealth>();
            if (startingWeapon == null || !startingWeapon.IsValid)
            {
                Debug.LogError("PlayerWeapons requires a valid starting weapon.", this);
                enabled = false;
                return;
            }

            owned[(int)startingWeapon.Type] = startingWeapon;
            combat.EquipWeapon(startingWeapon);
        }

        private void Update()
        {
            if (!Application.isFocused)
                return;
            if (Keybinds.Pressed(GameAction.Weapon1))
                TrySelect(WeaponType.BasicWand);
            if (Keybinds.Pressed(GameAction.Weapon2))
                TrySelect(WeaponType.FireStaff);
            if (Keybinds.Pressed(GameAction.Weapon3))
                TrySelect(WeaponType.IceStaff);
            if (Keybinds.Pressed(GameAction.Weapon4))
                TrySelect(WeaponType.StormStaff);
            if (Keybinds.Pressed(GameAction.Weapon5))
                TrySelect(WeaponType.VenomStaff);
        }

        // Prelazi na sledeće posedovano oružje (džojstik LB).
        public bool SelectNext()
        {
            var current = Selected != null ? (int)Selected.Type : 0;
            for (int step = 1; step <= WeaponTypes.Count; step++)
            {
                var type = (WeaponType)((current + step) % WeaponTypes.Count);
                if (owned[(int)type] != null)
                    return TrySelect(type);
            }
            return false;
        }

        public WeaponData GetOwned(WeaponType type) => ValidType(type) ? owned[(int)type] : null;

        public bool TrySelect(WeaponType type)
        {
            if (!CanChange || !ValidType(type))
                return false;
            var weapon = owned[(int)type];
            if (weapon == null || !combat.EquipWeapon(weapon))
                return false;
            Changed?.Invoke();
            return true;
        }

        public WeaponAcquisition Acquire(WeaponData weapon)
        {
            if (!CanChange || weapon == null || !weapon.IsValid)
                return WeaponAcquisition.Rejected;
            // Svaka vrsta ima jedno mesto; bolja retkost zamenjuje prethodno oružje.
            int slot = (int)weapon.Type;
            var previous = owned[slot];
            if (previous != null && previous.Rarity >= weapon.Rarity)
                return WeaponAcquisition.Duplicate;
            owned[slot] = weapon;
            if (Selected != null && Selected.Type == weapon.Type)
                combat.EquipWeapon(weapon);
            Changed?.Invoke();
            return previous == null ? WeaponAcquisition.NewWeapon : WeaponAcquisition.Upgrade;
        }

        private bool CanChange => isActiveAndEnabled && health != null && !health.IsDead && Time.timeScale > 0;

        private static bool ValidType(WeaponType type) => (int)type >= 0 && (int)type < WeaponTypes.Count;
    }
}
