using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODOs:
// - recompute paths when a new building is created/destroyed
// - findbuilding can be optimized to only compute distances of interesting buildings, and only return the closest instead of sorting them
// - go to buildings that will soon become harvestable / wait at points of interest

[RequireComponent(typeof(AnimateBody))]
public class Worker : MonoBehaviour {
    public enum Job { Farmer, Builder, Breeder, Priest, Logger };
    public Job job = Job.Farmer;
    public float baseVelocity = 3f;

    public float currentVelocity;
    public float height = 0f;

    private AnimateBody animation;

    Vector2Int target;
    List<Vector2Int> currentPath;
    int currentPathPos;

    IEnumerator<int> actions;

    // Start is called before the first frame update
    void Start() {
        currentPath = new List<Vector2Int>();
        target = new Vector2Int(-1, -1); ;
        actions = Actions().GetEnumerator();
        currentVelocity = baseVelocity;
        animation = GetComponent<AnimateBody>();
    }

    IEnumerable<int> Actions() {
        switch (job) {
            case Job.Farmer:
                while (true) {
                    Field field = null;
                    foreach (var f in FindBuilding<Field>(f => f.HasCorn())) {
                        if (f == null) {
                            yield return 0;
                        }
                        else {
                            field = f;
                        }
                    }

                    var corn = field.Harvest();
                    Debug.Assert(corn >= 0, "Harvest failed");
                    animation.Act(2, 1);
                    while (animation.IsActing) yield return 0;
                    field.Replant(corn);
                    animation.Hold(Resource.Food);

                    Warehouse warehouse = null;
                    foreach (var w in FindBuilding<Warehouse>(w => w.CanStore(Resource.Food))) {
                        if (w == null) {
                            yield return 0;
                        } else {
                            warehouse = w;
                        }
                    }

                    Debug.Assert(warehouse.AddElement(Resource.Food), "Storing failed");
                    animation.Drop();
                    yield return 0;
                }

            case Job.Logger:
                while (true) {
                    Tree tree = null;
                    foreach (var t in FindBuilding<Tree>(t => !t.harvested)) {
                        if (t == null) {
                            yield return 0;
                        }
                        else {
                            tree = t;
                        }
                    }

                    tree.harvested = true;
                    animation.Act(4, 3);
                    while (animation.IsActing) yield return 0;
                    tree.GetComponent<VoxelModel>().Explode();
                    animation.Hold(Resource.Wood);

                    Warehouse warehouse = null;
                    foreach (var w in FindBuilding<Warehouse>(w => w.CanStore(Resource.Wood))) {
                        if (w == null) {
                            yield return 0;
                        } else {
                            warehouse = w;
                        }
                    }

                    Debug.Assert(warehouse.AddElement(Resource.Wood), "Storing failed");
                    animation.Drop();
                    yield return 0;
                }
                
            case Job.Builder:
                while (true) {
                    Warehouse warehouse = null;
                    foreach (var w in FindBuilding<Warehouse>(w => w.Has(Resource.Wood))) {
                        if (w == null) {
                            yield return 0;
                        } else {
                            warehouse = w;
                        }
                    }
                    
                    Debug.Assert(warehouse.RemoveElement(Resource.Wood), "Retrieve failed");
                    animation.Hold(Resource.Wood);
                    
                    Building building = null;
                    foreach (var b in FindBuilding<Building>(b => b.RequireMoreWood())) {
                        if (b == null) {
                            yield return 0;
                        } else {
                            building = b;
                        }
                    }

                    animation.Drop();
                    building.ProvideWood();
                    animation.Act(5, 5);
                    while (animation.IsActing) yield return 0;
                    yield return 0;
                }
        }
    }

    // Yields action to go to a building with the required predicate. Returns null until the last step, which is the found building.
    IEnumerable<B> FindBuilding<B>(Func<B, bool> pred) where B : Building {
        while (true) {
            WorldGrid.InteractableBuilding<B> target = WorldGrid.instance.NearestBuilding(WorldGrid.instance.GridPos(transform.position), pred);
            if (target.b == null) {
                // No building could be found, wait and restart!
                yield return null;
                continue;
            }

            // We have a building, now go to it.
            foreach (int i in MoveTo(target.pos)) {
                yield return null;
            }

            // Now that we are near the building, check if the predicate is still true
            if (!pred(target.b)) {
                continue; // Search another building now that we moved
            }

            // We are near a building with a true predicate!
            yield return target.b;
            yield break;
        }
    }

    public void ComputePath() {
        if (target.x < 0 || target.y < 0) {
            return;
        }
        Vector2Int origin = WorldGrid.instance.GridPos(transform.position);
        currentPath = WorldGrid.instance.Smooth(WorldGrid.instance.Path(origin, target));
        currentPathPos = 0;
    }

    // Actions to reach a target. If there is no path, does nothing (sets currentPath to null).
    IEnumerable<int> MoveTo(Vector2Int target) {
        this.target = target;
        ComputePath();
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
            animation.velocity.x = delta.x / Time.deltaTime;
            animation.velocity.y = delta.z / Time.deltaTime;
            yield return 0;
        }

        animation.velocity = Vector3.zero;
        this.target = new Vector2Int(-1, -1);
    }

    // Update is called once per frame
    void Update() {
        actions.MoveNext();
    }
}
