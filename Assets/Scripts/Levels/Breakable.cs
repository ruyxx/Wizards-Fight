// Bure, sanduk ili ćup koji se razbije pogotkom i ponekad ostavi napitak ili manu.
using UnityEngine;

namespace AWizardsFight
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class Breakable : MonoBehaviour, IDamageable
    {
        [SerializeField]
        private Pickup potion = null, mana = null;
        // Napitak je redak, mana nešto češća.
        [SerializeField, Range(0f, 1f)]
        private float potionChance = .08f, manaChance = .22f;
        [SerializeField]
        private Color shardColor = new Color(.6f, .4f, .22f);
        private bool broken;

        public void TakeDamage(float amount)
        {
            if (broken)
                return;
            broken = true;
            foreach (var collider in GetComponents<Collider2D>())
                collider.enabled = false;
            // Krhotine se razlete i nestanu.
            for (int i = 0; i < 7; i++)
            {
                var shard = RuntimeSprites.Spawn("Shard", RuntimeSprites.Square, transform.position, shardColor, Random.Range(.08f, .16f), 5);
                shard.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 90f));
                shard.gameObject.AddComponent<FadeOut>().Play(.45f, Random.insideUnitCircle.normalized * Random.Range(2f, 4f));
            }

            float roll = Random.value;
            // Napici su retki (60% osnovne šanse), da isceljenje ima cenu.
            float potionRoll = potionChance * .6f;
            var drop = roll < potionRoll ? potion : roll < potionRoll + manaChance ? mana : null;
            if (drop != null)
                Instantiate(drop, transform.position, Quaternion.identity);

            Sfx.Play(SfxId.Break);
            Destroy(gameObject);
        }
    }
}
