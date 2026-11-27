// Raznovrsnost poda i zidova: pri učitavanju scene svaka pločica dobija jednu od nekoliko varijanti
// (Resources/Tiles/<ime>_0..3), izabranu po položaju, pa nivo ne izgleda kao ista slika ponovljena.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace AWizardsFight
{
    public static class TileVariety
    {
        private static readonly Dictionary<string, Tile[]> variants = new Dictionary<string, Tile[]>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init() => SceneManager.sceneLoaded += (scene, mode) => Apply();

        private static Tile[] Variants(Tile original)
        {
            string name = original.sprite != null ? original.sprite.texture.name : "";
            if (variants.TryGetValue(name, out var cached))
                return cached;
            var list = new List<Tile>();
            for (int i = 0; i < 4; i++)
            {
                var texture = Resources.Load<Texture2D>("Tiles/" + name + "_" + i);
                if (texture == null)
                    break;
                texture.filterMode = FilterMode.Point;
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), original.sprite.pixelsPerUnit);
                tile.colliderType = original.colliderType;
                tile.color = original.color;
                list.Add(tile);
            }
            variants[name] = list.Count > 0 ? list.ToArray() : null;
            return variants[name];
        }

        public static void Apply()
        {
            foreach (var map in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            {
                var bounds = map.cellBounds;
                foreach (var cell in bounds.allPositionsWithin)
                {
                    if (!(map.GetTile(cell) is Tile tile) || tile.sprite == null)
                        continue;
                    var options = Variants(tile);
                    if (options == null)
                        continue;
                    // Polovina pločica ostaje osnovna, ostale dobijaju varijantu; izbor zavisi samo od položaja.
                    int hash = (cell.x * 73856093) ^ (cell.y * 19349663);
                    int pick = ((hash % 8) + 8) % 8;
                    if (pick < options.Length)
                        map.SetTile(cell, options[pick]);
                }
            }
        }
    }
}
