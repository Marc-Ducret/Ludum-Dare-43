using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Worker : MonoBehaviour {
    public enum Job { Farmer, Builder, Breeder, Priest, Logger };
    public Job job = Job.Farmer;
    public float velocity = 3f;
    public float height = 0f;

    List<Vector2Int> currentPath;
    int currentPathPos;

    public Vector2Int target;

    // Start is called before the first frame update
    void Start() {
        currentPath = new List<Vector2Int>();
        target = WorldGrid.instance.GridPos(transform.position);
        //Debug.Log("Starting on " + target.ToString());
    }

    // Update is called once per frame
    void Update() {
        if (currentPath == null) return;
        // Detect if we achieved our goal
        if (currentPathPos < currentPath.Count) {
            Vector3 target = WorldGrid.instance.RealPos(currentPath[currentPathPos], height);
            Vector3 curPos = transform.position;
            if ((target - curPos).sqrMagnitude < 1e-12) {
                currentPathPos++;
            }
        }

        // Move to target
        if (currentPathPos < currentPath.Count) {
            Vector2Int target = currentPath[currentPathPos];
            Vector3 delta = WorldGrid.instance.RealPos(target, height) - transform.position;
            delta = delta * Mathf.Min(1, velocity * Time.deltaTime / delta.magnitude);
            transform.position += delta;
        }
    }

    void moveTo(Vector2Int target) {
        Vector2Int origin = WorldGrid.instance.GridPos(transform.position);
        currentPath = WorldGrid.instance.Smooth(WorldGrid.instance.Path(origin, target));
        if(currentPath == null) Debug.LogError("No path found");
        currentPathPos = 0;
    }

    private void OnValidate() {
        if (WorldGrid.instance != null) {
            moveTo(target);
        }
    }
}
