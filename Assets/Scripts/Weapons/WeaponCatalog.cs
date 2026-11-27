// Pronalazi definiciju oružja prema njegovoj vrsti i retkosti.
using UnityEngine;

namespace AWizardsFight
{
    [CreateAssetMenu(menuName = "A Wizard's Fight/Weapon Catalog", fileName = "WeaponCatalog")]
    public sealed class WeaponCatalog : ScriptableObject
    {
        [SerializeField]
        private WeaponData[] weapons = new WeaponData[0];
        public int Count => weapons.Length;

        public WeaponData Find(WeaponType type, WeaponRarity rarity)
        {
            foreach (var weapon in weapons)
                if (weapon != null && weapon.Type == type && weapon.Rarity == rarity)
                    return weapon;
            return null;
        }

        public bool IsComplete
        {
            get
            {
                if (weapons.Length != WeaponTypes.Count * 4)
                    return false;
                var seen = new bool[WeaponTypes.Count * 4];
                foreach (var weapon in weapons)
                {
                    if (weapon == null || !weapon.IsValid)
                        return false;
                    int key = (int)weapon.Type * 4 + (int)weapon.Rarity;
                    if (seen[key])
                        return false;
                    seen[key] = true;
                }

                return true;
            }
        }
    }
}
