// Predmet na podu (napitak ili mana) koji igrač pokupi prolaskom preko njega.
using UnityEngine;

namespace AWizardsFight
{
    public enum PickupKind
    {
        Potion,
        Mana
    }

    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class Pickup : MonoBehaviour
    {
        [SerializeField]
        private PickupKind kind = PickupKind.Potion;
        [SerializeField, Min(1f)]
        private float manaAmount = 30f;
        [SerializeField, Min(.1f)]
        private float collectRadius = .75f;
        private static PlayerHealth player;
        private Vector3 basePosition;
        private float seed;

        private void Start()
        {
            if (player == null)
                player = FindFirstObjectByType<PlayerHealth>();
            basePosition = transform.position;
            seed = Random.value * 6f;
        }

        private void Update()
        {
            transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time * 3f + seed) * .08f);
            if (player == null || player.IsDead || Vector2.Distance(player.transform.position, basePosition) > collectRadius)
                return;
            if (kind == PickupKind.Potion)
            {
                var potions = player.GetComponent<PlayerPotions>();
                if (potions == null || !potions.Add(1))
                    return;
                CombatFeedback.Popup("+1 POTION", basePosition, new Color(1f, .45f, .5f), 3.6f);
            }
            else
            {
                var mana = player.GetComponent<PlayerMana>();
                if (mana == null)
                    return;
                mana.Restore(manaAmount);
                CombatFeedback.Popup("+" + Mathf.RoundToInt(manaAmount) + " MANA", basePosition, new Color(.45f, .7f, 1f), 3.6f);
            }

            Sfx.Play(SfxId.Pickup);
            Destroy(gameObject);
        }
    }
}
