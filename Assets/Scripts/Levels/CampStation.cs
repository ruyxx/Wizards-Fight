// Mesta u kampu sa kojima igrač razgovara pritiskom na E: trgovac, oltar runa, starac, izvor i portali nivoa.
using UnityEngine;

namespace AWizardsFight
{
    public enum CampStationKind
    {
        Merchant,
        RuneAltar,
        Elder,
        Fountain,
        Portal,
        Rift
    }

    public sealed class CampStation : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private CampStationKind kind = CampStationKind.Merchant;
        [SerializeField, Tooltip("Za portal: redni broj nivoa iz GameLevels.")]
        private int levelIndex = 1;
        [SerializeField]
        private TMPro.TMP_Text label = null;
        private SpriteRenderer[] renderers;
        private SpriteRenderer swirl;

        public CampStationKind Kind => kind;
        public int LevelIndex => levelIndex;
        private bool Locked => (kind == CampStationKind.Portal && levelIndex > GameLevels.Unlocked) || (kind == CampStationKind.Rift && GameLevels.Unlocked < 2);
        private GameLevels.Level Level => kind == CampStationKind.Portal && levelIndex >= 1 && levelIndex <= GameLevels.All.Length ? GameLevels.All[levelIndex - 1] : null;

        public string Prompt
        {
            get
            {
                switch (kind)
                {
                    case CampStationKind.Merchant: return "E - TRADE WITH THE MERCHANT";
                    case CampStationKind.RuneAltar: return "E - USE THE RUNE ALTAR";
                    case CampStationKind.Elder: return "E - TALK TO THE ELDER (CONTRACTS)";
                    case CampStationKind.Fountain: return "E - DRINK FROM THE FOUNTAIN";
                    case CampStationKind.Rift: return Locked ? "SEALED - DEFEAT THE DARK SORCERER" : "E - ENTER THE RIFT";
                    default: return Locked ? "SEALED - DEFEAT THE PREVIOUS BOSS" : "E - ENTER " + (Level != null ? Level.Title : "PORTAL");
                }
            }
        }

        private void Start()
        {
            renderers = GetComponentsInChildren<SpriteRenderer>();
            if (label != null)
                label.text = kind == CampStationKind.Portal && Level != null ? Level.Title + (Locked ? "\n<size=70%>SEALED</size>" : "") : kind == CampStationKind.Rift ? "THE RIFT" + (Locked ? "\n<size=70%>SEALED</size>" : "") : kind.ToString().ToUpperInvariant().Replace("RUNEALTAR", "RUNE ALTAR");
            if (kind == CampStationKind.Portal || kind == CampStationKind.Rift)
            {
                // Otvoren portal ima vrtlog svetla; zatvoren je siv.
                var tint = kind == CampStationKind.Rift ? new Color(1f, .3f, .3f, Locked ? 0f : .85f) : new Color(.7f, .45f, 1f, Locked ? 0f : .8f);
                swirl = RuntimeSprites.Spawn("Swirl", RuntimeSprites.Ring, transform.position + Vector3.up * .3f, tint, 1.4f, 6);
                swirl.transform.SetParent(transform, true);
                if (Locked)
                    foreach (var renderer in renderers)
                        renderer.color = new Color(.45f, .45f, .5f);
            }
        }

        private void Update()
        {
            if (swirl != null && !Locked)
            {
                swirl.transform.Rotate(0, 0, -90f * Time.deltaTime);
                float pulse = 1.3f + .15f * Mathf.Sin(Time.time * 3f);
                swirl.transform.localScale = new Vector3(pulse, pulse * 1.3f, 1f) / transform.lossyScale.x;
            }
        }

        public bool CanInteract(PlayerInteractor player) => player != null && player.CanReach(this, 2.2f);

        public bool Interact(PlayerInteractor player)
        {
            if (!CanInteract(player))
                return false;
            if (Locked)
            {
                CombatFeedback.Popup("SEALED", transform.position + Vector3.up, new Color(.7f, .7f, .8f), 4f, 1f);
                return false;
            }
            var menu = FindFirstObjectByType<CampMenu>();
            if (menu == null)
                return false;
            if (kind == CampStationKind.Fountain)
            {
                // Izvor vraća zdravlje i manu i čuva napredak.
                var health = player.GetComponent<PlayerHealth>();
                health.Heal(health.MaxHealth);
                var mana = player.GetComponent<PlayerMana>();
                mana.Restore(mana.MaxMana);
                CombatFeedback.Popup("FULLY RESTORED", transform.position + Vector3.up, new Color(.5f, .85f, 1f), 4f, 1.2f);
                Sfx.Play(SfxId.LevelUp, .6f);
                FindFirstObjectByType<SaveSystem>()?.Save();
                return true;
            }

            menu.Open(this);
            return true;
        }
    }
}
