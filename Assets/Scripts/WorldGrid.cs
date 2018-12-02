using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(VoxelModel))]
public class WorldGrid : MonoBehaviour {

    public static WorldGrid instance;

    public int width;
    public int height;
    public float scale;

    public int treeCount;
    public Building treePrefab;
    public float minDist;

    public List<Building> buildings;

    private Vector3 zero; // origin of the grid, i.e. real-world coordinates of the left-bootom corner of the leftest-bottomest cell

    public static float roadFactor = 3f;

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

        public Position(Vector2Int pos, Vector2Int father, float dOrigin, Vector2Int target) {
            this.pos = pos;
            this.father = father;
            distanceFromOrigin = dOrigin;
            Vector2Int delta = target - pos;
            estimatedTotalDistance = dOrigin + (Mathf.Abs(delta.x) + Mathf.Abs(delta.y)) / roadFactor;
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
                var clear = model.Voxels[x, 0, y].color.a > 0;
                cells[y, x].isBuildable = clear;
                cells[y, x].isWalkable = clear;
            }
        }
        PlaceTrees();
    }

    public List<Neighbor> Neighbors(Vector2Int pos) {
        List<Neighbor> neighbors = new List<Neighbor>(8);

        for (int y = -1; y <= 1; y++) {
            for (int x = -1; x <= 1; x++) {
                Vector2Int delta = new Vector2Int(x, y);
                Vector2Int next = pos + delta;
                float dist = delta.magnitude;
                if (next.x >= 0 && next.x < width && next.y >= 0 && next.y < height && dist != 0 && cells[next.y, next.x].isWalkable) {
                    neighbors.Add(new Neighbor(next, dist));
                }
            }
        }

        return neighbors;
    }

    public PathInfo[,] AStar(Vector2Int origin, Vector2Int target) {
        PathInfo[,] path = new PathInfo[height, width];
        PriorityQueue<Position> toVisit = new PriorityQueue<Position>();
        toVisit.Enqueue(new Position(origin, origin, 0, target));

        if (!cells[target.y, target.x].isWalkable) {
            Debug.LogError("Un-walkable target");
            return null;
        }

        while (toVisit.Count() > 0) {
            Position c = toVisit.Dequeue();
            if (path[c.pos.y, c.pos.x].visited) {
                continue;
            }
            path[c.pos.y, c.pos.x] = new PathInfo(c.father, c.distanceFromOrigin);
            if (c.pos == target) {
                break;
            }
            foreach (Neighbor n in Neighbors(c.pos)) {
                Position d = new Position(n.pos, c.pos, c.distanceFromOrigin + n.distance / (cells[c.pos.y, c.pos.x].isRoad && cells[n.pos.y, n.pos.x].isRoad ? 1 : roadFactor), target);
                toVisit.Enqueue(d);
            }
        }

        return path;
    }

    public List<float> ComputeDistances(Vector2Int origin, List<Vector2Int> targets) {
        // TODO: do something smarter than n A*
        List<float> distances = new List<float>();
        foreach (Vector2Int target in targets) {
            PathInfo[,] path = AStar(origin, target);
            distances.Add(path == null ? Mathf.Infinity : path[target.y, target.x].distance);
        }
        return distances;
    }

    public List<Vector2Int> Path(Vector2Int origin, Vector2Int target) {
        PathInfo[,] path = AStar(origin, target);

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

    public bool CanPlaceAt(Vector2Int pos, Vector2Int size) {
        for (var y = pos.y; y < pos.y + size.y; y++) {
            for (var x = pos.x; x < pos.x + size.x; x++) {
                if (x < 0 || y < 0 || x >= width || y >= height || !cells[y, x].isBuildable) return false;
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
    }

    // Returns all buildings of type B
    public IEnumerable<B> Buildings<B>() where B : Building {
        foreach (Building b in buildings) {
            if (b is B) {
                yield return b as B;
            }
        }
    }

    public struct DistantB<B> : IComparable<DistantB<B>> where B : Building {
        public B b;
        public Vector2Int pos; // Interaction position
        public float distance; // Distance from origin

        public DistantB(B b, Vector2Int pos, float distance) {
            this.b = b;
            this.pos = pos;
            this.distance = distance;
        }

        public int CompareTo(DistantB<B> other) {
            return this.distance.CompareTo(other.distance);
        }
    }

    // Returns all buildings of type B, sorted by increasing distance from pos
    public IEnumerable<DistantB<B>> Buildings<B>(Vector2Int pos) where B : Building {
        List<DistantB<B>> bs = new List<DistantB<B>>();
        foreach (B b in Buildings<B>()) {
            List<Vector2Int> positions = b.InteractionPositions();
            List<float> distances = ComputeDistances(pos, positions);

            // Get best position
            int best = -1;
            float bestDistance = Mathf.Infinity;
            for (int i = 0; i < distances.Count; i++) {
                if (distances[i] < bestDistance) {
                    best = i;
                    bestDistance = distances[i];
                }
            }

            // Skip unreachable buildings
            if (best == -1) continue;

            bs.Add(new DistantB<B>(b, positions[best], bestDistance));
        }

        bs.Sort();
        return bs;
    }
}
