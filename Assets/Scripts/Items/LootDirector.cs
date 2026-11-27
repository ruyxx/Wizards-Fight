// Odlučuje šta pada iz protivnika i kovčega: zlato uvek, predmeti i rune povremeno; elitni i bossovi daju više.
using UnityEngine;

namespace AWizardsFight
{
    [DisallowMultipleComponent]
    public sealed class LootDirector : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)]
        private float itemChance = .09f, runeChance = .045f;
        private int highestLevel = 1;

        private void OnEnable()
        {
            EnemyHealth.AnyDied += EnemyDied;
            LootChest.AnyOpened += ChestOpened;
        }

        private void OnDisable()
        {
            EnemyHealth.AnyDied -= EnemyDied;
            LootChest.AnyOpened -= ChestOpened;
        }

        private void EnemyDied(EnemyHealth enemy)
        {
            if (enemy == null || !enemy.GrantsExperience)
                return;
            var level = enemy.Level;
            int itemLevel = (level != null ? level.Level : 1) + Torment.ItemLevelBonus;
            highestLevel = Mathf.Max(highestLevel, itemLevel);
            bool elite = level != null && level.IsElite;
            bool boss = enemy.GetComponent<IBossBehaviour>() != null;
            Vector2 position = enemy.transform.position;
            LootDrop.SpawnGold(Mathf.RoundToInt(Random.Range(3f, 7f) * (1f + itemLevel / 25f) * (elite ? 3f : 1f) * (boss ? 15f : 1f)), position);
            // Boss uvek ostavlja tri predmeta i runu, elitni jedan predmet.
            int items = boss ? 3 : elite ? 1 : Random.value < itemChance ? 1 : 0;
            float luck = Torment.Luck * LevelModifiers.Luck * (UniqueItems.Has("seeker") ? 1.5f : 1f);
            for (int i = 0; i < items; i++)
                LootDrop.SpawnItem(ItemCatalog.CreateGear(itemLevel, (boss ? 3f : elite ? 1.8f : 1f) * luck), position);
            // Retki jedinstveni predmeti bilo gde; u Torment-u češći, a boss na Torment-u uvek daje jedan.
            float uniqueChance = (.003f + .004f * Torment.Active) * (elite ? 6f : 1f) * luck;
            if ((boss && Torment.Active > 0) || Random.value < uniqueChance)
                UniqueItems.DropRandom(position);
            if (boss || Random.value < (elite ? runeChance * 4f : runeChance))
                LootDrop.SpawnItem(ItemCatalog.CreateRandomRune(itemLevel), position);
        }

        // Kovčeg daje siguran predmet, zlato i ponekad runu, uz oružje ili napitak kao i ranije.
        private void ChestOpened(LootChest chest)
        {
            Vector2 position = (Vector2)chest.transform.position + Vector2.down * .8f;
            int itemLevel = Mathf.Max(highestLevel, LevelNear(chest.transform.position));
            LootDrop.SpawnItem(ItemCatalog.CreateGear(itemLevel + Torment.ItemLevelBonus, 1.5f * Torment.Luck), position);
            if (Random.value < .02f + .02f * Torment.Active)
                UniqueItems.DropRandom(position);
            LootDrop.SpawnGold(Mathf.RoundToInt(Random.Range(10f, 20f) * (1f + itemLevel / 25f)), position);
            if (Random.value < .3f)
                LootDrop.SpawnItem(ItemCatalog.CreateRandomRune(itemLevel), position);
        }

        // Nivo kovčega prati jačinu najbližih protivnika.
        private static int LevelNear(Vector2 position)
        {
            float best = float.MaxValue;
            int result = 1;
            foreach (var level in FindObjectsByType<EnemyLevel>(FindObjectsSortMode.None))
            {
                float distance = Vector2.Distance(position, level.transform.position);
                if (distance < best)
                {
                    best = distance;
                    result = level.Level;
                }
            }
            return result;
        }
    }
}
