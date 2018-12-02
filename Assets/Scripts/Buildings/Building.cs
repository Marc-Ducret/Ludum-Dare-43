using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour {
    [HideInInspector]
    public Vector2Int pos; // bottom left cell
    public Vector2Int size;
    public bool isWalkable = false;

    public void Start() {
        pos = WorldGrid.instance.GridPos(transform.position);

        WorldGrid.instance.AddBuilding(this);
    }

    public bool Walkable() {
        return isWalkable;
    }

    // Find positions suitable for interacting with the building (typically, nearest walkable positions)
    // Guaranteed to be walkable, existing positions in the grid
    // The default returns positions with a O (if the building is represented by X):
    //  OO
    // OXXO
    // OXXO
    //  OO
    // For walkable buildings, this is any position inside the building
    public List<Vector2Int> InteractionPositions() {
        List<Vector2Int> l = new List<Vector2Int>();

        if (isWalkable) {
            for (int y = pos.y; y < pos.y + size.y; y++) {
                for (int x = pos.x; x < pos.x + size.x; x++) {
                    l.Add(new Vector2Int(x, y));
                }
            }
            return l;
        }

        for (int x = pos.x; x < pos.x + size.x; x++) {
            l.Add(new Vector2Int(x, pos.y - 1));
            l.Add(new Vector2Int(x, pos.y + size.y));
        }
        for (int y = pos.y; y < pos.y + size.y; y++) {
            l.Add(new Vector2Int(pos.x - 1, y));
            l.Add(new Vector2Int(pos.x + size.x, y));
        }
        return l.FindAll(p => WorldGrid.instance.IsValid(p) && WorldGrid.instance.IsWalkable(p));
    }

    private void OnDestroy() {
        WorldGrid.instance.RemoveBuilding(this);
    }
}
