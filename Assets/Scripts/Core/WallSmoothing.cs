// Uklanja zapinjanje uz zidove. Zidovi su sastavljeni od pločica, a svaka ima svoj kvadratni sudarač:
// ugao lika je zapinjao o spojeve susednih pločica. Pri učitavanju scene pločice se spajaju u jedan glatki
// sudarač, a lik dobija zaobljene ivice bez trenja, pa klizi duž zida kad se kreće ukoso.
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace AWizardsFight
{
    public static class WallSmoothing
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init() => SceneManager.sceneLoaded += (scene, mode) => Apply();

        public static void Apply()
        {
            foreach (var tiles in Object.FindObjectsByType<TilemapCollider2D>(FindObjectsSortMode.None))
            {
                if (tiles.isTrigger || tiles.compositeOperation != Collider2D.CompositeOperation.None)
                    continue;
                var body = tiles.GetComponent<Rigidbody2D>();
                if (body == null)
                    body = tiles.gameObject.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Static;
                var composite = tiles.GetComponent<CompositeCollider2D>();
                if (composite == null)
                    composite = tiles.gameObject.AddComponent<CompositeCollider2D>();
                composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
                composite.sharedMaterial = tiles.sharedMaterial;
                tiles.compositeOperation = Collider2D.CompositeOperation.Merge;
            }
        }

        // Kvadrat lika dobija zaobljene uglove (iste spoljne mere) i materijal bez trenja.
        public static void SmoothBody(GameObject target)
        {
            var box = target.GetComponent<BoxCollider2D>();
            if (box == null || box.isTrigger || box.edgeRadius > 0f)
                return;
            const float radius = .12f;
            box.size = new Vector2(Mathf.Max(.05f, box.size.x - radius * 2f), Mathf.Max(.05f, box.size.y - radius * 2f));
            box.edgeRadius = radius;
            if (box.sharedMaterial == null || box.sharedMaterial.friction > 0f)
                box.sharedMaterial = new PhysicsMaterial2D("No Friction") { friction = 0f, bounciness = 0f };
        }
    }
}
