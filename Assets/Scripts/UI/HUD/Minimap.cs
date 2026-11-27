// Mini-mapa u gornjem desnom uglu: otkriva sobe kroz koje je igrač prošao; taster M prikazuje celu mapu.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace AWizardsFight
{
    [DisallowMultipleComponent]
    public sealed class Minimap : MonoBehaviour
    {
        private const int RevealRadius = 11;
        private const float RevealInterval = .2f;
        private const int Margin = 1;
        private static readonly Color32 Hidden = new Color32(0, 0, 0, 0);
        private static readonly Color32 FloorColor = new Color32(58, 62, 92, 235);
        private static readonly Color32 WallColor = new Color32(150, 150, 190, 255);
        private static readonly Color32 ChestColor = new Color32(255, 200, 60, 255);
        private static readonly Color32 BossColor = new Color32(220, 60, 80, 255);
        private static readonly Color32 LavaColor = new Color32(235, 95, 30, 255);

        [SerializeField]
        private RectTransform hudRoot = null;
        [SerializeField, Min(10)]
        private int zoomedCells = 34;
        [SerializeField]
        private Vector2 smallSize = new Vector2(260, 260), largeSize = new Vector2(720, 600);

        private Tilemap floor, walls, lava;
        private Transform player;
        private BoundsInt bounds;
        private Texture2D texture;
        private Color32[] pixels;
        private bool[] revealed;
        private RectTransform frame, marker;
        private RawImage image;
        private LootChest[] chests;
        private Transform boss;
        private bool expanded, dirty;
        private float nextReveal;
        private readonly Queue<Vector2Int> queue = new Queue<Vector2Int>();
        private readonly Dictionary<Vector2Int, int> distance = new Dictionary<Vector2Int, int>();

        private void Start()
        {
            foreach (var map in FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            {
                if (map.name == "Floor")
                    floor = map;
                else if (map.name == "Walls")
                    walls = map;
                else if (map.name == "Lava")
                    lava = map;
            }

            var health = FindFirstObjectByType<PlayerHealth>();
            player = health != null ? health.transform : null;
            if (floor == null || walls == null || player == null || hudRoot == null)
            {
                Debug.LogError("Minimap requires Floor and Walls tilemaps, a player and a HUD root.", this);
                enabled = false;
                return;
            }

            chests = FindObjectsByType<LootChest>(FindObjectsSortMode.None);
            var bossHealth = FindFirstObjectByType<BossEncounter>()?.BossHealth;
            boss = bossHealth != null ? bossHealth.transform : null;
            floor.CompressBounds();
            walls.CompressBounds();
            int xMin = Mathf.Min(floor.cellBounds.xMin, walls.cellBounds.xMin);
            int yMin = Mathf.Min(floor.cellBounds.yMin, walls.cellBounds.yMin);
            int xMax = Mathf.Max(floor.cellBounds.xMax, walls.cellBounds.xMax);
            int yMax = Mathf.Max(floor.cellBounds.yMax, walls.cellBounds.yMax);
            bounds = new BoundsInt(xMin - Margin, yMin - Margin, 0, xMax - xMin + Margin * 2, yMax - yMin + Margin * 2, 1);
            texture = new Texture2D(bounds.size.x, bounds.size.y, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            pixels = new Color32[bounds.size.x * bounds.size.y];
            revealed = new bool[pixels.Length];
            BuildInterface();
            Reveal();
            Redraw();
        }

        private void BuildInterface()
        {
            var frameObject = new GameObject("Minimap", typeof(RectTransform), typeof(Image));
            frame = (RectTransform)frameObject.transform;
            frame.SetParent(hudRoot, false);
            frameObject.GetComponent<Image>().color = new Color(.05f, .05f, .09f, .78f);
            frameObject.GetComponent<Image>().raycastTarget = false;
            var outline = frameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.73f, .6f, 1f, .9f);
            outline.effectDistance = new Vector2(3, -3);

            var mapObject = new GameObject("Map", typeof(RectTransform), typeof(RawImage));
            var mapRect = (RectTransform)mapObject.transform;
            mapRect.SetParent(frame, false);
            mapRect.anchorMin = Vector2.zero;
            mapRect.anchorMax = Vector2.one;
            mapRect.offsetMin = new Vector2(6, 6);
            mapRect.offsetMax = new Vector2(-6, -6);
            image = mapObject.GetComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;

            var markerObject = new GameObject("Player Marker", typeof(RectTransform), typeof(Image));
            marker = (RectTransform)markerObject.transform;
            marker.SetParent(mapRect, false);
            marker.sizeDelta = new Vector2(10, 10);
            markerObject.GetComponent<Image>().color = new Color(.55f, 1f, .6f);
            markerObject.GetComponent<Image>().raycastTarget = false;
            markerObject.AddComponent<Outline>().effectColor = Color.black;
            ApplyLayout();
        }

        private void ApplyLayout()
        {
            if (expanded)
            {
                frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(.5f, .5f);
                frame.anchoredPosition = Vector2.zero;
                float aspect = (float)bounds.size.x / bounds.size.y;
                frame.sizeDelta = aspect >= largeSize.x / largeSize.y
                    ? new Vector2(largeSize.x, largeSize.x / aspect)
                    : new Vector2(largeSize.y * aspect, largeSize.y);
            }
            else
            {
                frame.anchorMin = frame.anchorMax = frame.pivot = Vector2.one;
                frame.anchoredPosition = new Vector2(-32, -32);
                frame.sizeDelta = smallSize;
            }
        }

        private void Update()
        {
            if (Application.isFocused && Time.timeScale > 0 && (Keybinds.Pressed(GameAction.Map) || GamepadInput.MapPressed))
            {
                expanded = !expanded;
                ApplyLayout();
            }

            if (Time.time >= nextReveal)
            {
                nextReveal = Time.time + RevealInterval;
                Reveal();
                Redraw();
            }

            UpdateView();
        }

        // Otkriva polja do kojih se od igrača može doći bez prolaska kroz zid (cela soba, ne i susedne).
        private void Reveal()
        {
            var start = (Vector2Int)floor.WorldToCell(player.position);
            queue.Clear();
            distance.Clear();
            queue.Enqueue(start);
            distance[start] = 0;
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                int steps = distance[cell];
                bool isWall = walls.HasTile((Vector3Int)cell);
                MarkRevealed(cell);
                if (isWall || steps >= RevealRadius)
                    continue;
                for (int i = 0; i < 4; i++)
                {
                    var next = cell + (i == 0 ? Vector2Int.right : i == 1 ? Vector2Int.left : i == 2 ? Vector2Int.up : Vector2Int.down);
                    if (distance.ContainsKey(next) || !InBounds(next) || (!walls.HasTile((Vector3Int)next) && !floor.HasTile((Vector3Int)next)))
                        continue;
                    distance[next] = steps + 1;
                    queue.Enqueue(next);
                }
            }
        }

        private bool InBounds(Vector2Int cell) => cell.x >= bounds.xMin + Margin && cell.x < bounds.xMax - Margin && cell.y >= bounds.yMin + Margin && cell.y < bounds.yMax - Margin;

        private void MarkRevealed(Vector2Int cell)
        {
            int index = (cell.y - bounds.yMin) * bounds.size.x + cell.x - bounds.xMin;
            if (revealed[index])
                return;
            revealed[index] = true;
            pixels[index] = walls.HasTile((Vector3Int)cell) ? WallColor : lava != null && lava.HasTile((Vector3Int)cell) ? LavaColor : FloorColor;
            dirty = true;
        }

        private void Redraw()
        {
            var draw = pixels;
            bool markers = false;
            foreach (var chest in chests)
                markers |= chest != null && !chest.IsOpened && IsRevealed(chest.transform.position);
            markers |= boss != null && IsRevealed(boss.position);
            if (!dirty && !markers)
                return;
            if (markers)
            {
                draw = (Color32[])pixels.Clone();
                foreach (var chest in chests)
                    if (chest != null && !chest.IsOpened)
                        Paint(draw, chest.transform.position, ChestColor);
                if (boss != null)
                    Paint(draw, boss.position, BossColor);
            }

            texture.SetPixels32(draw);
            texture.Apply(false);
            dirty = false;
        }

        private bool IsRevealed(Vector3 position)
        {
            var cell = floor.WorldToCell(position);
            int x = cell.x - bounds.xMin, y = cell.y - bounds.yMin;
            return x >= 0 && y >= 0 && x < bounds.size.x && y < bounds.size.y && revealed[y * bounds.size.x + x];
        }

        private void Paint(Color32[] target, Vector3 position, Color32 color)
        {
            if (!IsRevealed(position))
                return;
            var cell = floor.WorldToCell(position);
            target[(cell.y - bounds.yMin) * bounds.size.x + cell.x - bounds.xMin] = color;
        }

        // Mala mapa prati igrača; velika prikazuje ceo nivo.
        private void UpdateView()
        {
            Vector2 cell = (Vector2)player.position - new Vector2(bounds.xMin, bounds.yMin);
            var size = new Vector2(bounds.size.x, bounds.size.y);
            Rect view;
            if (expanded)
            {
                view = new Rect(0, 0, 1, 1);
            }
            else
            {
                float aspect = smallSize.x / smallSize.y;
                var window = new Vector2(zoomedCells * aspect, zoomedCells);
                view = new Rect((cell - window / 2f) / size, window / size);
            }

            image.uvRect = view;
            var mapRect = (RectTransform)image.transform;
            Vector2 local = ((cell / size) - view.position) / view.size - new Vector2(.5f, .5f);
            marker.anchoredPosition = Vector2.Scale(local, mapRect.rect.size);
        }

        private void OnDestroy()
        {
            if (texture != null)
                Destroy(texture);
        }
    }
}
