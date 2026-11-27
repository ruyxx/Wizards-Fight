// Led na podu: lik na ledu klizi, pa sporije menja pravac i sporije staje.
using UnityEngine;
using UnityEngine.Tilemaps;

namespace AWizardsFight
{
    [RequireComponent(typeof(Tilemap))]
    public sealed class IceSurface : MonoBehaviour
    {
        private static IceSurface active;
        private Tilemap ice;

        private void Awake() => ice = GetComponent<Tilemap>();

        private void OnEnable() => active = this;

        private void OnDisable()
        {
            if (active == this)
                active = null;
        }

        public static bool IsIce(Vector2 position) => active != null && active.ice.HasTile(active.ice.WorldToCell(position));

        private void Update()
        {
            // Led se blago presijava.
            float shimmer = .9f + .1f * Mathf.Sin(Time.time * 1.7f);
            ice.color = new Color(shimmer, shimmer, 1f, 1f);
        }
    }
}
