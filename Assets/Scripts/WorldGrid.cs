using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using Random = UnityEngine.Random;

[RequireComponent(typeof(VoxelModel))]
public class WorldGrid : MonoBehaviour {

    public static WorldGrid instance;

    public int width;
    public int height;
    public float scale;
    public float timeScale = 1f;

    public int treeCount;
    public Building treePrefab;
    public float minDist;
    
    public List<Building> buildings;
    
    private Vector3 zero; // origin of the grid, i.e. real-world coordinates of the left-bootom corner of the leftest-bottomest cell

    public static float roadFactor = 1.5f;

    public float dayDuration, nightDuration;
    private float cyclePosition;
    public Light sun;
    public bool night;
    public List<Worker> sleepers;

    public struct Cell {
        public bool isWalkable;
        public bool isRoad;
        public bool isBuildable;
    }

    public struct Position : IComparable<Position> {
        public Vector2Int pos;
        public Vector2Int father;
        public float distanceFromOrigin;
        public float estimatedTotalDistance;

        public Position(Vector2Int pos, Vector2Int father, float dOrigin, float dTarget) {
            this.pos = pos;
            this.father = father;
            distanceFromOrigin = dOrigin;
            estimatedTotalDistance = dOrigin + dTarget;
        }

        public int CompareTo(Position other) {
            return this.estimatedTotalDistance.CompareTo(other.estimatedTotalDistance);
        }
    }

    public struct PathInfo {
        public Vector2Int father;
        public float distance;
        public bool visited;

        public PathInfo(Vector2Int father, float distance) {
            this.father = father;
            this.distance = distance;
            visited = true;
        }
    }

    public struct Neighbor {
        public Vector2Int pos;
        public float distance;

        public Neighbor(Vector2Int pos, float distance) {
            this.pos = pos;
            this.distance = distance;
        }
    }

    public Cell[,] cells;

    [ContextMenu("Awake")]
    private void Awake() {
        if (instance != null) Debug.LogError("Multiples instances of Grid");
        instance = this;
        cells = new Cell[height, width];
        zero = Vector3.ProjectOnPlane(transform.position, Vector3.up) - scale / 2 * new Vector3(width, 0, height);
        buildings = new List<Building>();
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                cells[y, x].isBuildable = true;
                cells[y, x].isWalkable = true;
            }
        }

        ResetVisited();
    }

    private void PlaceTrees() {
        for (var i = 0; i < treeCount; i++) {
            var x = Random.Range(0, width);
            var y = Random.Range(0, height);
            var pos = new Vector2Int(x, y);
            if (cells[y, x].isBuildable && Vector2.Distance(pos, new Vector2Int(width / 2, height / 2)) > minDist)
                Instantiate(treePrefab, RealPos(pos), Quaternion.identity).transform.parent = transform;
        }
    }

    private void Start() {
        var model = GetComponent<VoxelModel>();
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                if (model.Voxels[x, 0, y].color.a < 1) {
                    cells[y, x].isBuildable = false;
                    cells[y, x].isWalkable = false;
                }
            }
        }
        PlaceTrees();
    }

    public IEnumerable<Neighbor> Neighbors(Vector2Int pos) {
        for (int y = -1; y <= 1; y++) {
            for (int x = -1; x <= 1; x++) {
                Vector2Int delta = new Vector2Int(x, y);
                Vector2Int next = pos + delta;
                float dist = delta.magnitude;
                if (next.x >= 0 && next.x < width && next.y >= 0 && next.y < height && dist != 0 && cells[next.y, next.x].isWalkable && cells[pos.y, next.x].isWalkable && cells[next.y, pos.x].isWalkable) {
                    yield return new Neighbor(next, dist);
                }
            }
        }
    }

    // Stop as soon as we reached one target
    public PathInfo[,] AStar(Vector2Int origin, List<Vector2Int> targets) {
        PathInfo[,] path = new PathInfo[height, width];
        if (targets.Count == 0) {
            return path;
        }
        
        targets = targets.ToList();
        targets.Sort((tA, tB) => (tA - origin).sqrMagnitude.CompareTo((tB - origin).sqrMagnitude));
        if (targets.Count > 50) {
            targets = targets.Take(50).ToList();
        }
        var roadF = (targets[0] - origin).sqrMagnitude <= 20*20 ? roadFactor : 1;
        
        foreach (Vector2Int target in targets) {
            if (!cells[target.y, target.x].isWalkable) {
                Debug.LogError("Un-walkable target " + target.ToString());
                return null;
            }
        }

        PriorityQueue<Position> toVisit = new PriorityQueue<Position>();

        Action<Vector2Int, Vector2Int, float> enqueue = (Vector2Int pos, Vector2Int father, float dOrigin) => {
            float dTarget = Mathf.Infinity;
            foreach (Vector2Int target in targets) {
                dTarget = Mathf.Min(dTarget, (target - pos).sqrMagnitude);
            }

            dTarget = Mathf.Sqrt(dTarget) / roadF;
            toVisit.Enqueue(new Position(pos, father, dOrigin, dTarget));
        };

        enqueue(origin, origin, 0);

        while (toVisit.Count() > 0) {
            Position c = toVisit.Dequeue();
            if (path[c.pos.y, c.pos.x].visited) {
                continue;
            }

            if (visitedOnce != null) visitedOnce[c.pos.y, c.pos.x] = true;
            path[c.pos.y, c.pos.x] = new PathInfo(c.father, c.distanceFromOrigin);
            bool done = false;
            foreach (Vector2Int target in targets)
                if (c.pos == target) {
                    done = true;
                    break;
                }
            if (done)
                break;
            foreach (Neighbor n in Neighbors(c.pos)) {
                enqueue(n.pos, c.pos, c.distanceFromOrigin + n.distance / (cells[c.pos.y, c.pos.x].isRoad ? roadF : 1));
            }
        }

        return path;
    }

    public int NearestTarget(Vector2Int origin, List<Vector2Int> targets) {
        PathInfo[,] path = AStar(origin, targets);
        for (int i = 0; i < targets.Count; i++) {
            if (path[targets[i].y, targets[i].x].visited)
                return i;
        }
        return -1;
    }

    public List<Vector2Int> Path(Vector2Int origin, Vector2Int target) {
        List<Vector2Int> targets = new List<Vector2Int>();
        targets.Add(target);
        PathInfo[,] path = AStar(origin, targets);

        List<Vector2Int> positions = new List<Vector2Int>();
        Vector2Int cur = target;
        while (cur != origin) {
            positions.Add(cur);
            if (!path[cur.y, cur.x].visited) return null;
            cur = path[cur.y, cur.x].father;
        }
        positions.Reverse();

        return positions;
    }

    public List<Vector2Int> Smooth(List<Vector2Int> path) {
        return path;
    }

    public Vector2Int GridPos(Vector3 pos) {
        Vector3 delta = (pos - zero) / scale; // offset to the bottom-left of the (0,0) cell
        return new Vector2Int((int)delta.x, (int)delta.z);
    }

    public Vector3 RealPos(Vector2Int pos, float height = 0f, bool center = true) {
        return zero + new Vector3(pos.x, height / scale, pos.y) * scale +
               (center ? new Vector3(1, 0, 1) * scale / 2 : Vector3.zero);
    }

    public Vector3 RealVec(Vector2Int vec) {
        return new Vector3(vec.x, height / scale, vec.y) * scale;
    }

    public bool CanPlaceAt(Vector2Int pos, Vector2Int size, bool isWalkable) {
        for (var y = pos.y; y < pos.y + size.y; y++) {
            for (var x = pos.x; x < pos.x + size.x; x++) {
                Vector2Int p = new Vector2Int(x, y);
                if (!IsValid(p)) return false;
                if (!cells[y, x].isBuildable) return false;
                if (!isWalkable)
                    foreach (Worker w in FindObjectsOfType<Worker>()) {
                        if (GridPos(w.transform.position) == p) {
                            return false;
                        }
                }
            }
        }

        return true;
    }

    public bool IsValid(Vector2Int p) {
        return p.x >= 0 && p.x < width && p.y >= 0 && p.y < height;
    }

    public bool IsWalkable(Vector2Int p) {
        return cells[p.y, p.x].isWalkable;
    }

    public void AddBuilding(Building b) {
        //Debug.Log("Adding building of type " + b.ToString() + " at pos " + b.pos.ToString());
        for (int y = b.pos.y; y < b.pos.y + b.size.y; y++) {
            for (int x = b.pos.x; x < b.pos.x + b.size.x; x++) {
                if (b is Road) {
                    cells[y, x].isRoad = true;
                }

                cells[y, x].isBuildable = false;
                cells[y, x].isWalkable = b.Walkable();
            }
        }

        buildings.Add(b);

        foreach (Worker w in FindObjectsOfType<Worker>()) {
            w.abortMoveTo = !w.ComputePath();
        }
    }

    public void RemoveBuilding(Building b) {
        for (int y = b.pos.y; y < b.pos.y + b.size.y; y++) {
            for (int x = b.pos.x; x < b.pos.x + b.size.x; x++) {
                cells[y, x].isRoad = false;
                cells[y, x].isBuildable = true;
                cells[y, x].isWalkable = true;
            }
        }

        buildings.Remove(b);

        foreach (Worker w in FindObjectsOfType<Worker>()) {
            w.abortMoveTo = !w.ComputePath();
        }
    }

    // Returns all buildings of type B
    public IEnumerable<B> Buildings<B>() where B : Building {
        foreach (Building b in buildings) {
            if (b is B) {
                yield return b as B;
            }
        }
    }

    public struct InteractableBuilding<B> where B : Building {
        public B b;
        public Vector2Int pos; // Interaction position

        public InteractableBuilding(B b, Vector2Int pos) {
            this.b = b;
            this.pos = pos;
        }
    }

    // Returns position of nearest building of type B with predicate being true
    public InteractableBuilding<B> NearestBuilding<B>(Vector2Int pos, Func<B, bool> pred, bool onlyFront = false) where B : Building {
        List<Vector2Int> positions = new List<Vector2Int>();
        List<B> buildings = new List<B>();
        foreach (B b in Buildings<B>()) {
            if (pred(b))
                foreach (Vector2Int p in b.InteractionPositions(onlyFront)) {
                    positions.Add(p);
                    buildings.Add(b);
                }
        }
        int i = NearestTarget(pos, positions);
        if (i == -1) {
            return new InteractableBuilding<B>(null, Vector2Int.zero);
        }
        return new InteractableBuilding<B>(buildings[i], positions[i]);
    }

    private void Update() {
        Time.timeScale = timeScale;

        float factor = (night && FindObjectOfType<Worker>() == null ? 10f : 1);

        cyclePosition = Mathf.Repeat(cyclePosition + factor * Time.deltaTime, dayDuration + nightDuration);
        const float transition = 5f;
        var fade = Mathf.Clamp(dayDuration / 2 - Mathf.Abs(cyclePosition - dayDuration / 2), 0, transition) / transition;
        sun.intensity = fade * 5 + 1e-2f;
        var sunCycle = cyclePosition < dayDuration
            ? cyclePosition / dayDuration * 180
            : 180 + (cyclePosition - dayDuration) / nightDuration * 180;
        sun.transform.rotation = Quaternion.Euler(sunCycle, 130, 0);
        if (night && cyclePosition < dayDuration) {
            foreach (Worker w in FindObjectsOfType<Worker>()) {
                w.Die("died of sleep deprivation");
            }
            foreach (Worker w in sleepers) {
                if (w.house != null) {
                    w.isSleeping = false;
                    w.gameObject.SetActive(true);
                } else Destroy(w.gameObject);
            }
            sleepers.Clear();
        }
        night = cyclePosition > dayDuration;
    }

    private bool[,] visitedOnce;
    public bool drawWalkable;
    public bool drawVisited;

    [ContextMenu("Reset Visited")]
    private void ResetVisited() {
        if (visitedOnce == null) visitedOnce = new bool[height, width];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
            visitedOnce[y, x] = false;
    }
    
    private void OnDrawGizmos() {
        if (drawWalkable) {
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++) {
                Vector3 pos = RealPos(new Vector2Int(x, y));
                Gizmos.color = cells[y, x].isWalkable ? Color.green : Color.red;
                Gizmos.DrawCube(pos, Vector3.one);
            }
        }
        
        if (drawVisited) {
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++) {
                Vector3 pos = RealPos(new Vector2Int(x, y));
                Gizmos.color = visitedOnce[y, x] ? Color.green : Color.red;
                Gizmos.DrawCube(pos, Vector3.one);
            }
        }
    }
}
