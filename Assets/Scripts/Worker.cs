using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Worker : MonoBehaviour {
    public enum Job { Farmer, Builder, Breeder, Priest };
    public Job job = Job.Farmer;
    public float velocity = 3f;

    List<Vector2Int> currentPath;
    int currentPathPos;

    public Vector2Int target;

    // Start is called before the first frame update
    void Start() {
        currentPath = new List<Vector2Int>();
    }

    // Update is called once per frame
    void Update() {
        // Update target if need be

        // Detect if we achieved our goal
        if (currentPathPos < currentPath.Count) {
            Vector2Int target = currentPath[currentPathPos];
            Vector2Int curPos = GridPos(transform.position);
            if (curPos == target) {
                currentPathPos++;
            }
        }

        // Move to target
        if (currentPathPos < currentPath.Count) {
            Vector2Int target = currentPath[currentPathPos];
            Debug.Log("Moving to " + RealPos(target).ToString() + " from " + transform.position.ToString());
            Vector3 delta = RealPos(target) - transform.position;
            delta = delta * Mathf.Min(1, velocity * Time.deltaTime / delta.magnitude);
            transform.position += delta;
        }
    }

    Vector2Int GridPos(Vector3 pos) {
        return new Vector2Int((int)pos.x, (int)pos.z);
    }

    Vector3 RealPos(Vector2Int pos) {
        return new Vector3(pos.x + 0.5f, 0f, pos.y + 0.5f);
    }

    void moveTo(Vector2Int target) {
        Vector2Int origin = GridPos(transform.position);
        Debug.Log("origin: " + origin.ToString() + " target: " + target.ToString());
        currentPath = WorldGrid.instance.Smooth(WorldGrid.instance.Path(origin, target));
        Debug.Log("New path: " + String.Join(";", currentPath.ConvertAll(v => v.ToString()).ToArray()));
        currentPathPos = 0;
    }

    private void OnValidate() {
        if (WorldGrid.instance != null) {
            moveTo(target);
        }
    }
}
