// Posebna osobina protivnika (vidi EnemyVariants).
using UnityEngine;

namespace AWizardsFight
{
    // Nestabilni: pri smrti ostavlja krug vatre - ne stoj blizu kad padne.
    public sealed class VolatileCore : MonoBehaviour
    {
        private EnemyHealth self;
        private EnemyLevel level;

        private void Start()
        {
            self = GetComponent<EnemyHealth>();
            level = GetComponent<EnemyLevel>();
            self.Died += Burst;
        }

        private void OnDestroy()
        {
            if (self != null)
                self.Died -= Burst;
        }

        private void Burst()
        {
            var player = EnemyVariants.Player;
            BurningGround.Create(transform.position, 1.7f, 9f * (level != null ? level.DamageMultiplier : 1f), 3f, player);
            CombatFeedback.Burst(transform.position, new Color(1f, .5f, .2f), 12, 3f);
            Sfx.Play(SfxId.Burn, .7f);
        }
    }
}
