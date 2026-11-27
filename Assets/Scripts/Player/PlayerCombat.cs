// Upravlja nišanjenjem, cenom mane, razmakom između napada, stvaranjem projektila i posebnom čarolijom na desni klik.
using UnityEngine;

namespace AWizardsFight
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerHealth), typeof(PlayerMana))]
    public sealed class PlayerCombat : MonoBehaviour
    {
        [SerializeField]
        private WeaponData equippedWeapon = null;
        [SerializeField]
        private Transform aimPivot = null;
        [SerializeField]
        private Camera aimCamera = null;
        [SerializeField]
        private LayerMask wallLayers = 1 << 9;
        private PlayerHealth health;
        private PlayerMana mana;
        private PlayerProgression progression;
        private PlayerEquipment equipment;
        private float nextCastTime, nextSpecialTime;
        public WeaponData EquippedWeapon => equippedWeapon;
        public float CooldownRemaining => Mathf.Max(0, nextCastTime - Time.time);
        public float SpecialCooldownRemaining => Mathf.Max(0, nextSpecialTime - Time.time);
        public float SpecialCooldownTotal { get; private set; } = 1f;

        public bool EquipWeapon(WeaponData weapon)
        {
            if (weapon == null || !weapon.IsValid)
                return false;
            equippedWeapon = weapon;
            return true;
        }

        private void Awake()
        {
            health = GetComponent<PlayerHealth>();
            mana = GetComponent<PlayerMana>();
            progression = GetComponent<PlayerProgression>();
            equipment = GetComponent<PlayerEquipment>();
            if (!TryGetComponent<PlayerSpells>(out _))
                gameObject.AddComponent<PlayerSpells>();
        }

        // Jačina aktivnih čarolija (Q/R/F/G): šteta izabranog oružja sa svim pojačanjima.
        public float SpellPower => equippedWeapon != null && equippedWeapon.IsValid ? WeaponDamage(equippedWeapon) : 10f;

        private void Start()
        {
            if (aimCamera == null)
                aimCamera = Camera.main;
            if (aimCamera == null || aimPivot == null || equippedWeapon == null || !equippedWeapon.IsValid)
            {
                Debug.LogError("PlayerCombat requires a camera, aim pivot and valid WeaponData.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (!Application.isFocused || health.IsDead || Time.timeScale <= 0 || aimCamera == null)
                return;
            if (MobileMode.Enabled)
            {
                AutoAim();
                return;
            }

            // Desni štap džojstika nišani i puca; RB baca posebnu čaroliju, LB menja oružje.
            var stick = GamepadInput.Aim;
            if (GamepadInput.NextWeaponPressed)
                GetComponent<PlayerWeapons>()?.SelectNext();
            if (stick.sqrMagnitude > .01f || GamepadInput.Fire || GamepadInput.SpecialPressed)
            {
                Vector2 facing = stick.sqrMagnitude > .01f ? stick.normalized : (Vector2)(aimPivot.rotation * Vector3.right);
                Vector2 aimPoint = (Vector2)transform.position + facing * 5f;
                AimAt(aimPoint);
                if (GamepadInput.Fire)
                    TryCastAt(aimPoint);
                if (GamepadInput.SpecialPressed)
                    TrySpecialAt(aimPoint);
                return;
            }

            Vector3 cursor = Input.mousePosition;
            if (!aimCamera.pixelRect.Contains(cursor))
                return;
            cursor.z = transform.position.z - aimCamera.transform.position.z;
            Vector2 target = aimCamera.ScreenToWorldPoint(cursor);
            AimAt(target);
            if (Input.GetMouseButton(0))
                TryCastAt(target);
            if (Input.GetMouseButtonDown(1))
                TrySpecialAt(target);
        }

        // ---------- Automatsko nišanjenje (telefon) ----------

        private const float AutoRange = 9f;
        private EnemyHealth autoTarget;
        private float nextTargetSearch;
        private SpriteRenderer targetMarker;

        public EnemyHealth AutoTarget => autoTarget;

        // Najbliži živ protivnik koga lik vidi (bez zida između); traži se nekoliko puta u sekundi.
        private EnemyHealth FindTarget()
        {
            EnemyHealth best = null;
            float bestDistance = AutoRange;
            foreach (var enemy in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            {
                if (enemy.IsDead || enemy.IsInvulnerable || !enemy.gameObject.activeInHierarchy)
                    continue;
                float distance = Vector2.Distance(transform.position, enemy.transform.position);
                if (distance >= bestDistance || Physics2D.Linecast(transform.position, enemy.transform.position, 1 << 9))
                    continue;
                best = enemy;
                bestDistance = distance;
            }
            return best;
        }

        private void AutoAim()
        {
            if (GamepadInput.NextWeaponPressed)
                GetComponent<PlayerWeapons>()?.SelectNext();
            if (Time.time >= nextTargetSearch || autoTarget == null || autoTarget.IsDead)
            {
                nextTargetSearch = Time.time + .2f;
                autoTarget = FindTarget();
            }

            Vector2 aimPoint;
            if (autoTarget != null)
            {
                aimPoint = autoTarget.transform.position;
                AimAt(aimPoint);
                TryCastAt(aimPoint);
            }
            else
            {
                // Bez mete lik gleda u pravcu kretanja.
                var move = GamepadInput.Move;
                aimPoint = (Vector2)transform.position + (move.sqrMagnitude > .01f ? move.normalized : (Vector2)(aimPivot.rotation * Vector3.right)) * 5f;
                AimAt(aimPoint);
            }
            if (GamepadInput.SpecialPressed)
                TrySpecialAt(aimPoint);

            // Crveni krug ispod trenutne mete.
            if (targetMarker == null)
                targetMarker = RuntimeSprites.Spawn("Target Marker", RuntimeSprites.Ring, transform.position, new Color(1f, .35f, .3f, .85f), 1.1f, 4);
            targetMarker.enabled = autoTarget != null;
            if (autoTarget != null)
            {
                targetMarker.transform.position = autoTarget.transform.position + Vector3.down * .25f;
                targetMarker.transform.localScale = Vector3.one * (1.1f + .08f * Mathf.Sin(Time.time * 8f));
            }
        }

        private void OnDisable()
        {
            if (targetMarker != null)
                targetMarker.enabled = false;
        }

        public bool AimAt(Vector2 target)
        {
            Vector2 direction = target - (Vector2)transform.position;
            if (!Finite(direction.x) || !Finite(direction.y) || direction.sqrMagnitude < 0.0001f || aimPivot == null)
                return false;
            aimPivot.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            return true;
        }

        // Šteta oružja sa pojačanjima iz nivoa lika.
        // Šteta oružja sa pojačanjima iz nivoa lika i opreme.
        private float WeaponDamage(WeaponData weapon)
        {
            float multiplier = equipment != null ? equipment.DamageMultiplier(weapon.Type) : 1f;
            if (progression != null)
            {
                multiplier *= progression.SpellDamageMultiplier;
                if (weapon.Type == WeaponType.FireStaff)
                    multiplier *= progression.FireDamageMultiplier;
            }
            return weapon.Damage * multiplier * LevelModifiers.PlayerDamage;
        }

        private float SlowDuration(WeaponData weapon) => weapon.SlowDuration * (progression != null ? progression.SlowDurationMultiplier : 1f) * (equipment != null ? equipment.Multiplier(StatType.SlowDuration) : 1f);

        private float CooldownMultiplier => (progression != null ? progression.CooldownMultiplier : 1f) * (equipment != null ? equipment.CastSpeedMultiplier : 1f);

        private bool CanAct(Vector2 target) => isActiveAndEnabled
            && !health.IsDead
            && Time.timeScale > 0
            && equippedWeapon != null
            && equippedWeapon.IsValid
            && AimAt(target);

        public bool TryCastAt(Vector2 target)
        {
            if (!CanAct(target) || Time.time < nextCastTime)
                return false;
            if (!mana.TryUseMana(equippedWeapon.ManaCost * SkillEffects.ManaCostMultiplier))
                return false;
            Fire(equippedWeapon, target - (Vector2)transform.position, WeaponDamage(equippedWeapon));
            nextCastTime = Time.time + equippedWeapon.Cooldown * CooldownMultiplier;
            if (TryGetComponent<BodyAnimator>(out var animator))
                animator.Kick();
            return true;
        }

        private void Fire(WeaponData weapon, Vector2 direction, float damage)
        {
            // Kritični pogodak nanosi višestruku štetu; krađa života leči lika za deo nanete štete.
            bool critical = equipment != null && Random.value < equipment.CritChance;
            if (critical)
                damage *= equipment.CritMultiplier;
            var shot = Instantiate(weapon.ProjectilePrefab, transform.position, Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));
            shot.Launch(direction, weapon.ProjectileSpeed, damage, transform, weapon.SlowMultiplier, SlowDuration(weapon));
            shot.SetPlayerBonuses(critical, health, equipment != null ? equipment.Lifesteal : 0f, weapon.Type);
            // Arkani eho: ponekad ista čarolija poleti još jednom, malo ukoso.
            if (SkillEffects.EchoCast)
            {
                var echo = Quaternion.Euler(0, 0, Random.Range(-9f, 9f)) * direction;
                var copy = Instantiate(weapon.ProjectilePrefab, transform.position, Quaternion.Euler(0, 0, Mathf.Atan2(echo.y, echo.x) * Mathf.Rad2Deg));
                copy.Launch(echo, weapon.ProjectileSpeed, damage, transform, weapon.SlowMultiplier, SlowDuration(weapon));
                copy.SetPlayerBonuses(critical, health, equipment != null ? equipment.Lifesteal : 0f, weapon.Type);
            }
        }

        public static string SpecialName(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.FireStaff: return "FLAME FIELD";
                case WeaponType.IceStaff: return "FROST NOVA";
                case WeaponType.StormStaff: return "THUNDERSTORM";
                case WeaponType.VenomStaff: return "PLAGUE CLOUD";
                default: return "ARCANE FAN";
            }
        }

        public static float SpecialManaCost(WeaponData weapon) => Mathf.Max(12f, weapon.ManaCost * 4f);

        private static float SpecialCooldown(WeaponType type) => type == WeaponType.FireStaff ? 4f : type == WeaponType.IceStaff ? 5f : type == WeaponType.StormStaff ? 5f : type == WeaponType.VenomStaff ? 5f : 1.6f;

        // Posebna čarolija zavisi od izabranog oružja.
        public bool TrySpecialAt(Vector2 target)
        {
            if (!CanAct(target) || Time.time < nextSpecialTime)
                return false;
            var weapon = equippedWeapon;
            if (!mana.TryUseMana(SpecialManaCost(weapon)))
            {
                CombatFeedback.Popup("NO MANA", transform.position, new Color(.5f, .7f, 1f), 3.2f, .6f);
                return false;
            }

            Vector2 origin = transform.position;
            Vector2 aim = target - origin;
            float damage = WeaponDamage(weapon);
            switch (weapon.Type)
            {
                case WeaponType.FireStaff:
                    // Polje vatre na mestu nišana, ali ne iza zida i ne dalje od 7 jedinica.
                    float distance = Mathf.Min(aim.magnitude, 7f);
                    var hit = Physics2D.Raycast(origin, aim.normalized, distance, wallLayers);
                    Vector2 point = hit.collider != null ? hit.point - aim.normalized * .4f : origin + aim.normalized * distance;
                    FlameField.Create(point, damage * 1.3f, weapon.ProjectilePrefab.GetComponent<FireExplosion>()?.VisualPrefab);
                    break;
                case WeaponType.IceStaff:
                    FrostNova.Cast(origin, damage * 1.5f, Mathf.Max(.2f, weapon.SlowMultiplier * .6f), Mathf.Max(2f, SlowDuration(weapon) * 1.5f));
                    break;
                case WeaponType.StormStaff:
                    Thunderstorm.Cast(this, origin, damage * 1.6f);
                    break;
                case WeaponType.VenomStaff:
                    float reach = Mathf.Min(aim.magnitude, 7f);
                    var blocked = Physics2D.Raycast(origin, aim.normalized, reach, wallLayers);
                    PlagueCloud.Create(blocked.collider != null ? blocked.point - aim.normalized * .4f : origin + aim.normalized * reach, damage * .5f);
                    break;
                default:
                    // Lepeza od pet projektila.
                    for (int i = -2; i <= 2; i++)
                        Fire(weapon, Quaternion.Euler(0, 0, i * 12f) * aim, damage * .8f);
                    break;
            }

            SpecialCooldownTotal = Mathf.Max(.01f, SpecialCooldown(weapon.Type) * CooldownMultiplier);
            nextSpecialTime = Time.time + SpecialCooldownTotal;
            // Posebna čarolija pokreće i animaciju i zvuk običnog napada.
            nextCastTime = Mathf.Max(nextCastTime, Time.time + .2f);
            return true;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
