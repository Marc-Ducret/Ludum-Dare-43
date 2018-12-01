using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour {
    public Vector2Int pos; // bottom left cell
    public Vector2Int size;

    // Start is called before the first frame update
    public void Start() {
        size = Vector2Int.one;
        pos = WorldGrid.instance.GridPos(transform.position);

        WorldGrid.instance.AddBuilding(this);
    }

    // Update is called once per frame
    public void Update() {

    }
}
