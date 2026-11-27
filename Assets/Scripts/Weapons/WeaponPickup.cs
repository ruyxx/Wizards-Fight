// Omogućava preuzimanje oružja sa poda pri ulasku igrača u okidač.
using UnityEngine;

namespace AWizardsFight
{
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class WeaponPickup : MonoBehaviour
    {
        [SerializeField]
        private WeaponData weapon = null;
        private bool collected;
        public WeaponData Weapon => weapon;

        private void OnTriggerEnter2D(Collider2D other)
        {
            var collection = other.GetComponentInParent<PlayerWeapons>();
            if (collection != null)
                TryCollect(collection);
        }

        public bool TryCollect(PlayerWeapons collection)
        {
            if (collected || collection == null)
                return false;
            var result = collection.Acquire(weapon);
            
            if (result != WeaponAcquisition.NewWeapon && result != WeaponAcquisition.Upgrade)
                return false;
            collected = true;
            GetComponent<Collider2D>().enabled = false;
            Destroy(gameObject);
            return true;
        }
    }
}
