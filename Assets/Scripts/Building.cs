using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour {
    [HideInInspector]
    public Vector2Int pos; // bottom left cell
    public Vector2Int size;

    public void Start() {
        pos = WorldGrid.instance.GridPos(transform.position);

        WorldGrid.instance.AddBuilding(this);
    }
}
