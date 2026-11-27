// Pronalazi put oko zidova kada protivnik izgubi igrača iz vida (A* po mreži polja veličine 1).
using System.Collections.Generic;
using UnityEngine;

namespace AWizardsFight
{
    public sealed class EnemyNavigator
    {
        private const float RepathInterval = .3f;
        private const float CellClearance = .4f;
        private const int SearchBudget = 2500;
        private const int LookAhead = 8;

        private static readonly Vector2Int[] Steps =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };
        private static readonly Collider2D[] OverlapResults = new Collider2D[1];
        private static readonly RaycastHit2D[] CastResults = new RaycastHit2D[1];

        private readonly List<Vector2Int> path = new List<Vector2Int>();
        private readonly Dictionary<Vector2Int, bool> walkable = new Dictionary<Vector2Int, bool>();
        private readonly Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        private readonly Dictionary<Vector2Int, float> cost = new Dictionary<Vector2Int, float>();
        private readonly List<(float priority, Vector2Int cell)> open = new List<(float, Vector2Int)>();
        private float nextRepathTime;
        private Vector2 waypoint;
        private bool hasWaypoint;

        // Vraća pravac kretanja ka sledećoj tački puta ili nulu ako put ne postoji.
        public Vector2 Steer(Vector2 position, Vector2 target, float bodyRadius, LayerMask walls)
        {
            if (Time.time >= nextRepathTime)
            {
                nextRepathTime = Time.time + RepathInterval * Random.Range(.8f, 1.2f);
                hasWaypoint = FindPath(position, target, walls) && PickWaypoint(position, bodyRadius, walls, out waypoint);
            }

            if (!hasWaypoint)
                return Vector2.zero;
            Vector2 toWaypoint = waypoint - position;
            // Blizu tačke puta odmah se traži sledeća, da protivnik ne bi prebacio cilj.
            if (toWaypoint.sqrMagnitude < .25f)
                nextRepathTime = 0;
            return toWaypoint.sqrMagnitude < .0001f ? Vector2.zero : toWaypoint.normalized;
        }

        public void Reset()
        {
            hasWaypoint = false;
            nextRepathTime = 0;
        }

        private static Vector2Int CellOf(Vector2 point) => new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y));
        private static Vector2 CenterOf(Vector2Int cell) => new Vector2(cell.x + .5f, cell.y + .5f);

        private static ContactFilter2D Filter(LayerMask walls)
        {
            var filter = new ContactFilter2D();
            filter.SetLayerMask(walls);
            filter.useTriggers = false;
            return filter;
        }

        private bool IsWalkable(Vector2Int cell, ContactFilter2D filter)
        {
            if (!walkable.TryGetValue(cell, out bool free))
            {
                free = Physics2D.OverlapCircle(CenterOf(cell), CellClearance, filter, OverlapResults) == 0;
                walkable[cell] = free;
            }
            return free;
        }

        private bool FindPath(Vector2 from, Vector2 to, LayerMask walls)
        {
            path.Clear();
            walkable.Clear();
            cameFrom.Clear();
            cost.Clear();
            open.Clear();
            var filter = Filter(walls);
            Vector2Int start = CellOf(from), goal = CellOf(to);
            walkable[start] = true;
            walkable[goal] = true;
            cost[start] = 0;
            Push(Heuristic(start, goal), start);

            for (int expanded = 0; open.Count > 0 && expanded < SearchBudget; expanded++)
            {
                Vector2Int current = Pop();
                if (current == goal)
                {
                    for (Vector2Int cell = goal; cell != start; cell = cameFrom[cell])
                        path.Add(cell);
                    path.Reverse();
                    return true;
                }

                foreach (Vector2Int step in Steps)
                {
                    Vector2Int next = current + step;
                    if (!IsWalkable(next, filter))
                        continue;
                    // Dijagonala ne sme da preseče ugao zida.
                    if (step.x != 0 && step.y != 0 && (!IsWalkable(new Vector2Int(current.x + step.x, current.y), filter) || !IsWalkable(new Vector2Int(current.x, current.y + step.y), filter)))
                        continue;
                    float nextCost = cost[current] + (step.x != 0 && step.y != 0 ? 1.4142f : 1f);
                    if (cost.TryGetValue(next, out float known) && known <= nextCost)
                        continue;
                    cost[next] = nextCost;
                    cameFrom[next] = current;
                    Push(nextCost + Heuristic(next, goal), next);
                }
            }
            return false;
        }

        // Bira najdalju tačku puta do koje telo može da ide pravo, da kretanje ne bi bilo "stepenasto".
        private bool PickWaypoint(Vector2 from, float bodyRadius, LayerMask walls, out Vector2 point)
        {
            point = from;
            if (path.Count == 0)
                return false;
            var filter = Filter(walls);
            point = CenterOf(path[0]);
            for (int i = Mathf.Min(path.Count, LookAhead) - 1; i > 0; i--)
            {
                Vector2 candidate = CenterOf(path[i]);
                Vector2 delta = candidate - from;
                if (Physics2D.CircleCast(from, bodyRadius, delta.normalized, filter, CastResults, delta.magnitude) == 0)
                {
                    point = candidate;
                    break;
                }
            }
            return true;
        }

        private static float Heuristic(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x), dy = Mathf.Abs(a.y - b.y);
            return Mathf.Max(dx, dy) + .4142f * Mathf.Min(dx, dy);
        }

        // Mali binarni hip za otvorene čvorove.
        private void Push(float priority, Vector2Int cell)
        {
            open.Add((priority, cell));
            for (int i = open.Count - 1; i > 0;)
            {
                int parent = (i - 1) / 2;
                if (open[parent].priority <= open[i].priority)
                    break;
                (open[parent], open[i]) = (open[i], open[parent]);
                i = parent;
            }
        }

        private Vector2Int Pop()
        {
            Vector2Int top = open[0].cell;
            int last = open.Count - 1;
            open[0] = open[last];
            open.RemoveAt(last);
            for (int i = 0; ;)
            {
                int left = i * 2 + 1, right = left + 1, smallest = i;
                if (left < open.Count && open[left].priority < open[smallest].priority)
                    smallest = left;
                if (right < open.Count && open[right].priority < open[smallest].priority)
                    smallest = right;
                if (smallest == i)
                    break;
                (open[smallest], open[i]) = (open[i], open[smallest]);
                i = smallest;
            }
            return top;
        }
    }
}
