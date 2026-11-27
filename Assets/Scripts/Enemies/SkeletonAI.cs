// Podešava osobine kostura koristeći zajedničko ponašanje MeleeEnemyAI.
using UnityEngine;

namespace AWizardsFight
{
    public sealed class SkeletonAI : MeleeEnemyAI
    {
       
        [SerializeField, Min(0)]
        private float attackDamage = 20f;
        protected override float AttackDamage => attackDamage;
    }
}
