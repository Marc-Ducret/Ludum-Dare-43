using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGrid : MonoBehaviour {

    public static WorldGrid instance;

    public int width;
    public int height;
    public float scale;
    private Vector3 zero; // origin of the grid, i.e. real-world coordinates of the center of the leftest-bottomest cell

    public static float roadFactor = 3f;

    public struct Cell {
        public bool isObstacle;
        public bool isRoad;
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

    private void Awake() {
        if (instance != null) Debug.LogError("Multiples instances of Grid");
        instance = this;
        cells = new Cell[height, width];
        zero = transform.position - scale/2 * new Vector3(width + 1, 0, height + 1);
        Debug.Log("Center of (0,0) cell: " + zero.ToString());
    }

    private void Update() {
    }

    public List<Neighbor> Neighbors(Vector2Int pos) {
        List<Neighbor> neighbors = new List<Neighbor>(8);

        for (int y = -1; y <= 1; y++) {
            for (int x = -1; x <= 1; x++) {
                Vector2Int delta = new Vector2Int(x, y);
                Vector2Int next = pos + delta;
                float dist = delta.magnitude;
                if (next.x >= 0 && next.x < width && next.y >= 0 && next.y < height && dist != 0 && !cells[next.y, next.x].isObstacle) {
                    neighbors.Add(new Neighbor(next, dist));
                }
            }
        }

        return neighbors;
    }

    public List<Vector2Int> Path(Vector2Int origin, Vector2Int target) {
        PathInfo[,] path = new PathInfo[height, width];
        PriorityQueue<Position> toVisit = new PriorityQueue<Position>();
        toVisit.Enqueue(new Position(origin, origin, 0, target));

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

        List<Vector2Int> positions = new List<Vector2Int>();
        Vector2Int cur = target;
        while (cur != origin) {
            positions.Add(cur);
            cur = path[cur.y, cur.x].father;
        }
        positions.Reverse();

        return positions;
    }

    public List<Vector2Int> Smooth(List<Vector2Int> path) {
        return path;
    }

    public Vector2Int GridPos(Vector3 pos) {
        Vector3 delta = (pos - zero) / scale + new Vector3(0.5f, 0f, 0.5f); // offset to the bottom-left of the (0,0) cell
        return new Vector2Int((int)delta.x, (int)delta.z);
    }

    public Vector3 RealPos(Vector2Int pos, float height = 0f) {
        return zero + new Vector3(pos.x, height / scale, pos.y) * scale;
    }

    public Vector3 RealVec(Vector2Int vec) {
        return new Vector3(vec.x, height / scale, vec.y) * scale;
    }

    public bool CanPlaceAt(Vector2Int pos, Vector2Int size) {
        for (var y = pos.y; y < pos.y + size.y; y++) {
            for (var x = pos.x; x < pos.x + size.x; x++) {
                if (cells[y, x].isObstacle) return false;
            }
        }

        return true;
    }

    public void AddBuilding(Building b) {
        Debug.Log("Adding building " + b + " at " + b.pos.ToString() + " size " + b.size.ToString());
        for (int y = b.pos.y; y < b.pos.y + b.size.y; y++) {
            for (int x = b.pos.x; x < b.pos.x + b.size.x; x++) {
                if (b is Road) {
                    cells[y, x].isRoad = true;
                } else {
                    cells[y, x].isObstacle = true;
                }
            }
        }
    }
}
