﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODOs:
// - recompute paths when a new building is created/destroyed
// - findbuilding can be optimized to only compute distances of interesting buildings, and only return the closest instead of sorting them
// - go to buildings that will soon become harvestable / wait at points of interest

public class Worker : MonoBehaviour {
    public enum Job { Farmer, Builder, Breeder, Priest, Logger };
    public Job job = Job.Farmer;
    public float baseVelocity = 3f;
    private float currentVelocity;
    public float height = 0f;

    List<Vector2Int> currentPath;
    int currentPathPos;

    public Vector2Int target;

    IEnumerable<int> actions;

    // Start is called before the first frame update
    void Start() {
        currentPath = new List<Vector2Int>();
        target = WorldGrid.instance.GridPos(transform.position);
        actions = Actions();
        currentVelocity = baseVelocity;
        //Debug.Log("Starting on " + target.ToString());
    }

    IEnumerable<int> Actions() {
        switch (job) {
            case Job.Farmer:
                while (true) {
                    // Find a suitable field
                    Field field = null;
                    foreach (Field f in FindBuilding<Field>(f => f.hasCorn())) {
                        if (f == null) {
                            yield return 0;
                        } else {
                            field = f;
                        }
                    }

                    // Harvest, TODO animation?
                    Debug.Assert(field.harvest(), "Harvest failed");
                    yield return 0;

                    // TODO: change model to farmer holding corn

                    // Find a suitable warehouse
                    Warehouse warehouse = null;
                    foreach (Warehouse w in FindBuilding<Warehouse>(w => w.acceptCorn())) {
                        if (w == null) {
                            yield return 0;
                        }
                        else {
                            warehouse = w;
                        }
                    }

                    // Store, TODO animation?
                    Debug.Assert(warehouse.storeCorn(), "Storing failed");
                    yield return 0;

                    // TODO: change model to farmer without corn
                }
        }
    }

    // Yields action to go to a building with the required predicate. Returns null until the last step, which is the found building.
    IEnumerable<B> FindBuilding<B>(Func<B, bool> pred) where B : Building {
        while (true) {
            foreach (WorldGrid.DistantB<B> b in WorldGrid.instance.Buildings<B>(WorldGrid.instance.GridPos(transform.position)) {
                if (!pred(b.b)) continue; // Move on to next closest building

                // We have a building, now go to it.
                foreach (int i in MoveTo(b.pos)) {
                    yield return null;
                }

                // Now that we are near the building, check if the predicate is still true
                if (!pred(b.b)) break; // Recompute all buildings now that we moved

                // We are near a building with a true predicate!
                yield return b.b;
                yield break;
            }
        }
    }

    // Actions to reach a target. If there is no path, does nothing (sets currentPath to null).
    IEnumerable<int> MoveTo(Vector2Int target) {
        Vector2Int origin = WorldGrid.instance.GridPos(transform.position);
        currentPath = WorldGrid.instance.Smooth(WorldGrid.instance.Path(origin, target));
        currentPathPos = 0;

        if (currentPath == null) yield break;

        while (currentPathPos < currentPath.Count) {
            // Goes to next checkpoint if we reached our current objective
            Vector3 objective = WorldGrid.instance.RealPos(currentPath[currentPathPos], height);
            Vector3 curPos = transform.position;
            if ((objective - curPos).sqrMagnitude < 1e-12) {
                currentPathPos++;
                continue;
            }

            // Update velocity
            Vector2Int pos = WorldGrid.instance.GridPos(transform.position);
            currentVelocity = baseVelocity * (WorldGrid.instance.cells[pos.y, pos.x].isRoad ? WorldGrid.roadFactor : 1f);

            // Move to objective
            Vector3 delta = WorldGrid.instance.RealPos(currentPath[currentPathPos], height) - transform.position;
            delta = delta * Mathf.Min(1, currentVelocity * Time.deltaTime / delta.magnitude);
            transform.position += delta;
            yield return 0;
        }
    }

    // Update is called once per frame
    void Update() {

    }
}
