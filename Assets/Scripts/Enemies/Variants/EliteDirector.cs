// Na početku partije pretvara deo običnih protivnika u elitne: jači su, svetle zlatno i ostavljaju napitak.
using System.Collections.Generic;
using UnityEngine;

namespace AWizardsFight
{
    [DisallowMultipleComponent]
    public sealed class EliteDirector : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)]
        private float eliteChance = .12f;
        [SerializeField]
        private Pickup reward = null;
        private readonly HashSet<EnemyHealth> elites = new HashSet<EnemyHealth>();

        private void OnEnable() => EnemyHealth.AnyDied += EnemyDied;

        private void OnDisable() => EnemyHealth.AnyDied -= EnemyDied;

        private void Start()
        {
            foreach (var enemy in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.InstanceID))
            {
                var level = enemy.Level;
                if (enemy.IsDead || level == null || enemy.GetComponent<IBossBehaviour>() != null || Random.value > eliteChance)
                    continue;
                level.MakeElite();
                enemy.ResetToMax();
                enemy.transform.localScale *= 1.2f;
                var aura = RuntimeSprites.Spawn("Elite Aura", RuntimeSprites.Glow, enemy.transform.position, new Color(1f, .8f, .25f, .5f), 2.2f, 4);
                aura.transform.SetParent(enemy.transform, true);
                aura.gameObject.AddComponent<TorchFlicker>();
                enemy.GetComponent<HealthFeedback>()?.AddTitlePrefix("ELITE ");
                elites.Add(enemy);
            }
        }

        private void EnemyDied(EnemyHealth enemy)
        {
            if (!elites.Remove(enemy) || reward == null)
                return;
            Instantiate(reward, enemy.transform.position, Quaternion.identity);
        }
    }
}
