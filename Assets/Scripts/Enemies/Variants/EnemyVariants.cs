// Nove vrste protivnika: deo običnih protivnika u svakom nivou dobija posebnu osobinu (kao afiksi u Diablu).
// Iscelitelj leči saveznike, Senka se teleportuje iza igrača, Ledeni usporava igrača u blizini,
// a Nestabilni pri smrti ostavlja vatru. Svaki nivo ima svoju mešavinu.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AWizardsFight
{
    public enum EnemyVariant { Healer, Shade, Frostbound, Volatile }

    public static class EnemyVariants
    {
        private static readonly Dictionary<string, (EnemyVariant[] kinds, float chance)> ByScene = new Dictionary<string, (EnemyVariant[], float)>
        {
            { "Dungeon", (new[] { EnemyVariant.Healer, EnemyVariant.Volatile }, .18f) },
            { "EmberDepths", (new[] { EnemyVariant.Volatile, EnemyVariant.Healer, EnemyVariant.Shade }, .3f) },
            { "FrozenCrypt", (new[] { EnemyVariant.Frostbound, EnemyVariant.Shade, EnemyVariant.Healer }, .3f) },
            { "ShadowTemple", (new[] { EnemyVariant.Shade, EnemyVariant.Shade, EnemyVariant.Healer, EnemyVariant.Frostbound }, .38f) },
            { "Endless", (new[] { EnemyVariant.Healer, EnemyVariant.Shade, EnemyVariant.Frostbound, EnemyVariant.Volatile }, .3f) }
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init() => SceneManager.sceneLoaded += (scene, mode) =>
        {
            if (!ByScene.ContainsKey(scene.name) || scene.name == "Endless")
                return;
            // Isti izbor pri svakom ulasku u nivo (zavisi od imena protivnika), da čuvanje partije ostane dosledno.
            foreach (var enemy in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            {
                var random = new System.Random(enemy.name.GetHashCode() ^ scene.name.GetHashCode());
                Roll(enemy, scene.name, random);
            }
        };

        // Endless poziva ovo za svakog novog protivnika.
        public static void Roll(EnemyHealth enemy, string scene, System.Random random = null)
        {
            if (enemy == null || enemy.GetComponent<IBossBehaviour>() != null || !ByScene.TryGetValue(scene, out var options))
                return;
            random = random ?? new System.Random();
            if (random.NextDouble() >= options.chance)
                return;
            var kind = options.kinds[random.Next(options.kinds.Length)];
            // Bombaš se raznese pri dodiru, pa mu teleport i lečenje nemaju smisla.
            if (enemy.GetComponent<BomberAI>() != null && (kind == EnemyVariant.Shade || kind == EnemyVariant.Healer))
                kind = random.NextDouble() < .5 ? EnemyVariant.Volatile : EnemyVariant.Frostbound;
            Apply(enemy, kind);
        }

        public static void Apply(EnemyHealth enemy, EnemyVariant kind)
        {
            Color tint;
            string title;
            switch (kind)
            {
                case EnemyVariant.Healer:
                    enemy.gameObject.AddComponent<HealerAura>();
                    tint = new Color(.6f, 1f, .6f);
                    title = "MENDING ";
                    break;
                case EnemyVariant.Shade:
                    enemy.gameObject.AddComponent<ShadeBlink>();
                    tint = new Color(.7f, .55f, 1f);
                    title = "SHADE ";
                    break;
                case EnemyVariant.Frostbound:
                    enemy.gameObject.AddComponent<FrostAura>();
                    tint = new Color(.65f, .9f, 1f);
                    title = "FROSTBOUND ";
                    break;
                default:
                    enemy.gameObject.AddComponent<VolatileCore>();
                    tint = new Color(1f, .65f, .45f);
                    title = "VOLATILE ";
                    break;
            }
            var visual = enemy.transform.Find("Visual");
            if (visual != null && visual.TryGetComponent<SpriteRenderer>(out var sprite))
                sprite.color *= tint;
            if (enemy.TryGetComponent<HealthFeedback>(out var feedback))
                feedback.AddTitlePrefix(title);
        }

        public static PlayerHealth Player => Object.FindFirstObjectByType<PlayerHealth>();
    }
}
