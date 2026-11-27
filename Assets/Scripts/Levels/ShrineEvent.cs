// Oltar sudbine: igrač bira jedan od tri dara, a svaki ima cenu ili rizik. Može se upotrebiti jednom po nivou.
using UnityEngine;

namespace AWizardsFight
{
    public sealed class ShrineEvent : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private string shrineId = "shrine";
        private SpriteRenderer glow;

        private string Flag => "shrine_" + shrineId;
        public string Prompt => "E - PRAY AT THE SHRINE OF FATE";

        private void Start()
        {
            // Posle nastavka sa tačke čuvanja izabrani dar ponovo važi.
            var player = FindFirstObjectByType<PlayerHealth>();
            if (player != null && SaveSystem.HasFlag(Flag + "_power"))
                ApplyPower(player.GetComponent<PlayerBuffs>());
            if (player != null && SaveSystem.HasFlag(Flag + "_greed"))
                ApplyGreed(player.GetComponent<PlayerBuffs>());
            glow = RuntimeSprites.Spawn("Shrine Glow", RuntimeSprites.Glow, transform.position, new Color(.7f, .5f, 1f, .5f), 3f, -8);
            glow.transform.SetParent(transform, true);
            glow.gameObject.AddComponent<TorchFlicker>();
        }

        private void Update()
        {
            if (glow != null && SaveSystem.HasFlag(Flag))
                glow.color = new Color(.4f, .4f, .45f, .2f);
        }

        public bool CanInteract(PlayerInteractor player) => player != null && !SaveSystem.HasFlag(Flag) && player.CanReach(this);

        public bool Interact(PlayerInteractor player)
        {
            if (!CanInteract(player))
                return false;
            var health = player.GetComponent<PlayerHealth>();
            var buffs = player.GetComponent<PlayerBuffs>();
            var equipment = player.GetComponent<PlayerEquipment>();
            MessagePanel.Show("SHRINE OF FATE",
                "The shrine hums with old magic. It offers one gift, and every gift has a price.\n\n" +
                "<color=#FF9A6A>POWER</color>: +30% spell damage for the rest of this level, but enemies also hit 15% harder.\n" +
                "<color=#FFD84D>GREED</color>: +80% gold find and +20% experience for this level, but enemies hit 25% harder.\n" +
                "<color=#FF6A80>BLOOD</color>: sacrifice 40% of your current health for a rare rune.",
                ("POWER", () =>
                {
                    ApplyPower(buffs);
                    SaveSystem.SetFlag(Flag + "_power");
                    Use(player, "POWER FLOWS THROUGH YOU");
                }),
                ("GREED", () =>
                {
                    ApplyGreed(buffs);
                    SaveSystem.SetFlag(Flag + "_greed");
                    Use(player, "YOUR POCKETS GROW HEAVY");
                }),
                ("BLOOD", () =>
                {
                    float cost = health.CurrentHealth * .4f;
                    if (cost > 0f)
                        health.TakeDamageIgnoringArmor(cost);
                    if (equipment != null && !equipment.AddItem(ItemCatalog.CreateRune((RuneType)Random.Range(0, 9), 2)))
                        LootDrop.SpawnItem(ItemCatalog.CreateRune((RuneType)Random.Range(0, 9), 2), transform.position + Vector3.down);
                    Use(player, "THE SHRINE DRINKS YOUR BLOOD");
                }),
                ("LEAVE", null));
            return true;
        }

        private static void ApplyPower(PlayerBuffs buffs)
        {
            buffs?.Add("Power", StatType.SpellDamage, 30f, 0f);
            LevelModifiers.EnemyDamage *= 1.15f;
        }

        private static void ApplyGreed(PlayerBuffs buffs)
        {
            buffs?.Add("Greed", StatType.GoldFind, 80f, 0f);
            buffs?.Add("Greed", StatType.ExperienceGain, 20f, 0f);
            LevelModifiers.EnemyDamage *= 1.25f;
        }

        private void Use(PlayerInteractor player, string message)
        {
            SaveSystem.SetFlag(Flag);
            CombatFeedback.Popup(message, player.transform.position + Vector3.up, new Color(.8f, .6f, 1f), 4f, 1.6f);
            Sfx.Play(SfxId.Summon, .7f);
        }
    }
}
