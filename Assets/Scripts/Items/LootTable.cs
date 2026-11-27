// Bira nagradu koristeći odvojene težine za vrstu i retkost oružja.
using UnityEngine;

namespace AWizardsFight
{
    [CreateAssetMenu(menuName = "A Wizard's Fight/Loot Table", fileName = "LootTable")]
    public sealed class LootTable : ScriptableObject
    {
        [SerializeField]
        private WeaponCatalog catalog = null;
        [SerializeField]
        private float[] typeWeights =
        {
            32,
            22,
            18,
            14,
            14
        };
        [SerializeField]
        private float[] rarityWeights =
        {
            60,
            25,
            11,
            4
        };
        public bool IsValid => catalog != null
            && catalog.IsComplete
            && typeWeights != null
            && rarityWeights != null
            && typeWeights.Length == WeaponTypes.Count
            && rarityWeights.Length == 4
            && ValidWeights(typeWeights)
            && ValidWeights(rarityWeights);

        public WeaponData Roll(float typeRoll, float rarityRoll)
        {
            if (!IsValid)
                return null;
            int type = PickIndex(typeWeights, typeRoll), rarity = PickIndex(rarityWeights, rarityRoll);
            return type < 0 || rarity < 0 ? null : catalog.Find((WeaponType)type, (WeaponRarity)rarity);
        }

        public static int PickIndex(float[] weights, float unitRoll)
        {
            if (!ValidWeights(weights) || float.IsNaN(unitRoll) || unitRoll < 0 || unitRoll > 1)
                return -1;
            double total = 0;
            foreach (float weight in weights)
                total += weight;
            // Izvučeni broj predstavlja mesto u zbiru težina, a ne redni broj nagrade.
            double target = unitRoll * total, cumulative = 0;
            int lastPositive = -1;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] <= 0)
                    continue;
                lastPositive = i;
                cumulative += weights[i];
                if (target < cumulative)
                    return i;
            }

            return lastPositive; // Unity's inclusive Random.value may return exactly 1.
        }

        private static bool ValidWeights(float[] weights)
        {
            if (weights == null || weights.Length == 0)
                return false;
            bool positive = false;
            foreach (float weight in weights)
            {
                if (weight < 0 || float.IsNaN(weight) || float.IsInfinity(weight))
                    return false;
                positive |= weight > 0;
            }

            return positive;
        }
    }
}
