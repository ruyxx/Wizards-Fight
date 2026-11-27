// Pretvara izabranog protivnika u šampiona: ima ime, posebnu osobinu, mnogo zdravlja, nosi ključ trezora i ostavlja odličan plen.
using UnityEngine;

namespace AWizardsFight
{
    public enum ChampionAffix
    {
        Vampiric,
        Molten,
        Arcane,
        Swift
    }

    public sealed class ChampionEvent : MonoBehaviour
    {
        [SerializeField]
        private EnemyHealth champion = null;
        [SerializeField]
        private string championName = "GRAVEMAW";
        [SerializeField]
        private ChampionAffix affix = ChampionAffix.Vampiric;
        [SerializeField, Tooltip("Trezor čiji ključ šampion nosi (prazno: bez ključa).")]
        private string vaultId = "";
        [SerializeField]
        private Projectile arcaneBolt = null;
        private PlayerHealth player;
        private float nextAbility;
        private float lastPlayerHealth;

        public EnemyHealth Champion => champion;
        public string ChampionName => championName;

        private void Start()
        {
            player = FindFirstObjectByType<PlayerHealth>();
            if (champion == null || champion.IsDead)
                return;
            var level = champion.Level;
            level?.MakeChampion(affix == ChampionAffix.Swift ? 1.6f : 1f);
            champion.ResetToMax();
            champion.transform.localScale *= 1.45f;
            var aura = RuntimeSprites.Spawn("Champion Aura", RuntimeSprites.Glow, champion.transform.position, AffixColor(), 2.6f, 4);
            aura.transform.SetParent(champion.transform, true);
            aura.gameObject.AddComponent<TorchFlicker>();
            champion.GetComponent<HealthFeedback>()?.AddTitlePrefix(championName + " - " + affix.ToString().ToUpperInvariant() + "\n");
            champion.Died += Defeated;
            if (player != null)
            {
                lastPlayerHealth = player.CurrentHealth;
                player.HealthChanged += PlayerHurt;
            }
        }

        private void OnDestroy()
        {
            if (player != null)
                player.HealthChanged -= PlayerHurt;
        }

        private Color AffixColor()
        {
            switch (affix)
            {
                case ChampionAffix.Vampiric: return new Color(.9f, .1f, .2f, .55f);
                case ChampionAffix.Molten: return new Color(1f, .5f, .1f, .55f);
                case ChampionAffix.Arcane: return new Color(.6f, .4f, 1f, .55f);
                default: return new Color(.4f, 1f, .5f, .55f);
            }
        }

        // Vampirski šampion se leči kad povredi igrača.
        private void PlayerHurt(float current, float maximum)
        {
            float lost = lastPlayerHealth - current;
            lastPlayerHealth = current;
            if (affix != ChampionAffix.Vampiric || lost <= 0f || champion == null || champion.IsDead)
                return;
            if (Vector2.Distance(champion.transform.position, player.transform.position) < 2.5f)
                champion.Heal(lost * 2f);
        }

        private void Update()
        {
            if (champion == null || champion.IsDead || player == null || player.IsDead || Time.timeScale <= 0)
                return;
            float distance = Vector2.Distance(champion.transform.position, player.transform.position);
            if (distance > 9f || Time.time < nextAbility)
                return;
            switch (affix)
            {
                case ChampionAffix.Molten:
                    // Rastopljeni šampion ostavlja užareno tlo za sobom.
                    nextAbility = Time.time + 1.2f;
                    BurningGround.Create(champion.transform.position, .9f, 10f * (champion.Level != null ? champion.Level.DamageMultiplier : 1f), 3.5f, player);
                    break;
                case ChampionAffix.Arcane:
                    nextAbility = Time.time + 3.5f;
                    if (arcaneBolt != null)
                        for (int i = 0; i < 8; i++)
                        {
                            var shot = Instantiate(arcaneBolt, champion.transform.position, Quaternion.identity);
                            shot.Launch(Quaternion.Euler(0, 0, i * 45f + Time.time * 40f) * Vector2.right, 5f, 8f * (champion.Level != null ? champion.Level.DamageMultiplier : 1f), champion.transform);
                        }
                    break;
                case ChampionAffix.Swift:
                    nextAbility = Time.time + .5f;
                    break;
            }
        }

        private void Defeated()
        {
            Vector2 position = champion.transform.position;
            int itemLevel = champion.Level != null ? champion.Level.Level : 30;
            LootDrop.SpawnItem(ItemCatalog.CreateGear(itemLevel, 4f), position);
            LootDrop.SpawnItem(ItemCatalog.CreateGear(itemLevel, 2.5f), position);
            LootDrop.SpawnGold(Mathf.RoundToInt(60 * (1f + itemLevel / 25f)), position);
            if (!string.IsNullOrEmpty(vaultId))
            {
                SaveSystem.SetFlag(VaultChest.KeyFlag(vaultId));
                CombatFeedback.Popup("VAULT KEY FOUND!", position + Vector2.up, new Color(1f, .85f, .3f), 5f, 2f);
            }
            SaveSystem.SetFlag("champion_" + championName);
            CameraFollow.Shake(.3f, .3f);
        }
    }
}
