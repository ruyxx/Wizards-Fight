// Podešava osobine sluzi koristeći zajedničko ponašanje MeleeEnemyAI.
using UnityEngine;

namespace AWizardsFight
{
    public sealed class SlimeAI : MeleeEnemyAI
    {
        [SerializeField, Min(0)]
        private float attackDamage = 12f;
        protected override float AttackDamage => attackDamage;
    }
}
