// Definicija jednog oružja: šteta, mana, oporavak, projektil i dodatni efekti.
using UnityEngine;

namespace AWizardsFight
{
    public enum WeaponType
    {
        BasicWand,
        FireStaff,
        IceStaff,
        StormStaff,
        VenomStaff
    }

    public static class WeaponTypes
    {
        public const int Count = 5;
    }

    public enum WeaponRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    [CreateAssetMenu(menuName = "A Wizard's Fight/Weapon", fileName = "Weapon")]
    public sealed class WeaponData : ScriptableObject
    {
        [SerializeField]
        private string weaponName = "Basic Wand";
        [SerializeField]
        private WeaponType weaponType = WeaponType.BasicWand;
        [SerializeField]
        private WeaponRarity rarity = WeaponRarity.Common;
        [SerializeField, TextArea]
        private string description = "A reliable single-target magic bolt.";
        [SerializeField]
        private Sprite icon = null;
        [SerializeField, Range(0.01f, 1f)]
        private float slowMultiplier = 1f;
        [SerializeField, Min(0)]
        private float slowDuration = 0f;
        [SerializeField, Min(0.01f)]
        private float damage = 10f;
        [SerializeField, Min(0.01f)]
        private float cooldown = 0.35f;
        [SerializeField, Min(0f)]
        private float manaCost = 5f;
        [SerializeField, Min(0.01f)]
        private float projectileSpeed = 12f;
        [SerializeField]
        private Projectile projectilePrefab = null;
        public string WeaponName => weaponName;
        public WeaponType Type => weaponType;
        public WeaponRarity Rarity => rarity;
        public string Description => description;
        public Sprite Icon => icon;
        public float SlowMultiplier => slowMultiplier;
        public float SlowDuration => slowDuration;
        public float Damage => damage;
        public float Cooldown => cooldown;
        public float ManaCost => manaCost;
        public float ProjectileSpeed => projectileSpeed;
        public Projectile ProjectilePrefab => projectilePrefab;
        public bool IsValid => (int)weaponType >= 0
            && (int)weaponType < WeaponTypes.Count
            && (int)rarity >= 0
            && (int)rarity < 4
            && Positive(slowMultiplier)
            && slowMultiplier <= 1
            && !float.IsNaN(slowDuration)
            && !float.IsInfinity(slowDuration)
            && slowDuration >= 0
            && projectilePrefab != null
            && Positive(damage)
            && Positive(cooldown)
            && Positive(projectileSpeed)
            && !float.IsNaN(manaCost)
            && !float.IsInfinity(manaCost)
            && manaCost >= 0;

        private static bool Positive(float value) => value > 0 && !float.IsInfinity(value);
    }
}
