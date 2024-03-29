﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(AnimateBody))]
public class Worker : MonoBehaviour {
    public enum Job { Farmer, Builder, Breeder, Priest, Logger };
    bool isSacrificed = false;
    bool isReadyForSacrifice = false;
    Temple temple;
    public Job job = Job.Farmer;

    public float baseVelocity = 3f;
    public float numDaysBeforeDouble = 3f;
    public float speedup {
        get {
            return (1 + Mathf.Log(1 + (Mathf.Exp(1) - 1) * numNights / numDaysBeforeDouble));
        }
    }
    public float height = 0f;

    public float starvingTime = 20f;
    public float faithSlowDown = 3f;
    int numNights = 0;

    public float plantingTime = 2f;
    public float choppingTime = 4f;
    public float buildingTime = 5f;
    public float hexingTime = 3f;
    public float sacrificingTime = 10f;
    public float breedingTime = 3f;

    private AnimateBody animation;

    Vector2Int target;
    List<Vector2Int> currentPath;
    int currentPathPos;
    public bool isSleeping;

    IEnumerator<int> actions;

    Notifications notif;
    private Faith faith;
    private float speedBonus;

    [HideInInspector]
    public House house;
    

    void Start() {
        currentPath = new List<Vector2Int>();
        target = new Vector2Int(-1, -1);
        actions = Actions().GetEnumerator();
        animation = GetComponent<AnimateBody>();

        notif = FindObjectOfType<Notifications>();
        notif.Post(String.Format("A {0} joined your cult!", job));

        faith = FindObjectOfType<Faith>();
    }

    IEnumerable<int> Actions() {
        if (isSacrificed) while (true) foreach (int i in SacrificedWork()) yield return i;

        switch (job) {
            case Job.Farmer:
                while (true) foreach (int i in FarmerWork()) yield return 0;

            case Job.Logger:
                while (true) foreach (int i in LoggerWork()) yield return 0;

            case Job.Builder:
                while (true) foreach (int i in BuilderWork()) yield return 0;

            case Job.Priest:
                while (true) foreach (int i in PriestWork()) yield return 0;

            case Job.Breeder:
                while (true) foreach (int i in BreederWork()) yield return 0;
        }
    }

    private Vector3 InteractionWorldPos(Building b) {
        var pos = Vector3.down * 500;
        for(var y = b.pos.y; y < b.pos.y + b.size.y; y++)
        for (var x = b.pos.x; x < b.pos.x + b.size.x; x++) {
            var bPos = WorldGrid.instance.RealPos(new Vector2Int(x, y));
            if ((bPos - transform.position).sqrMagnitude < (pos - transform.position).sqrMagnitude)
                pos = bPos;
        }

        return pos;
    }

    IEnumerable<int> FarmerWork() {
        Field field = null;
        foreach (var f in FindBuilding<Field>(f => f.HasCorn())) {
            if (f == null) {
                yield return 0;
            } else {
                field = f;
            }
        }

        var corn = field.Harvest();
        animation.Act(plantingTime / speedup, 1, InteractionWorldPos(field));
        while (animation.IsActing) yield return 0;
        if (field == null) yield break;
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

        warehouse.AddElement(Resource.Food);
        animation.Drop();
        yield return 0;
    }

    IEnumerable<int> LoggerWork() {
        Tree tree = null;
        foreach (var t in FindBuilding<Tree>(t => !t.harvested)) {
            if (t == null) {
                yield return 0;
            } else {
                tree = t;
            }
        }

        tree.harvested = true;
        animation.Act(choppingTime / speedup, 3, InteractionWorldPos(tree));
        while (animation.IsActing) yield return 0;
        if (tree == null) yield break;
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

        warehouse.AddElement(Resource.Wood);
        animation.Drop();
        yield return 0;
    }

    IEnumerable<int> BuilderWork() {
        Warehouse warehouse = null;
        foreach (var w in FindBuilding<Warehouse>(w => w.Has(Resource.Wood))) {
            if (w == null) {
                yield return 0;
            } else {
                warehouse = w;
            }
        }

        warehouse.RemoveElement(Resource.Wood);
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
        building.buildingTime = buildingTime / speedup;
        building.ProvideWood();
        animation.Act(buildingTime / speedup, 5, InteractionWorldPos(building));
        while (animation.IsActing) yield return 0;
        if (building == null) yield break;
        building.isBeingConstructed = false;
        yield return 0;
    }

    IEnumerable<int> PriestWork() {
        Worker worker = null;
        while (worker == null) {
            foreach (int i in EatSleep()) yield return i;

            Worker[] workers = FindObjectsOfType<Worker>();
            if (workers.Length == 1) {
                // The only worker is ourselves!
                yield return 0;
                continue;
            }

            // Sample one random worker
            worker = this;
            while (worker == this) {
                // <= 2 iterations in expectation
                worker = workers[Random.Range(0, workers.Length)];
            }

            // Now try to catch him
            while ((transform.position - worker.transform.position).sqrMagnitude >= 1) {
                bool gotPath = false;
                int steps = 10;
                foreach (int i in MoveTo(WorldGrid.instance.GridPos(worker.transform.position))) {
                    yield return 0;
                    if (worker == null || !worker.gameObject.activeSelf) {
                        worker = null;
                        break;
                    }

                    gotPath = true;
                    if (--steps <= 0) break;
                }
                if (worker == null || abortMoveTo) {
                    // For some reason the target became unwalkable, just choose another
                    worker = null;
                    break;
                }

                if (!gotPath) {
                    // There is no path to the target.
                    if (WorldGrid.instance.GridPos(worker.transform.position) == WorldGrid.instance.GridPos(transform.position)) {
                        // We are on the same cell, in which case we just move in direction of the target to reach its real world pos
                        DirectMoveTo(worker.transform.position);
                        yield return 0;
                        if (worker == null || !worker.gameObject.activeSelf) {
                            worker = null;
                            break;
                        }
                    } else {
                        // The target is unreachable, in which case we should choose another
                        worker = null;
                        break;
                    }
                }
            }
        }

        if (Random.value > .1f) {
            animation.Act(.5f, 1, worker.transform.position);
            while (animation.IsActing) yield return 0;
            worker.speedBonus = 10;
            yield break;
        }

        // Get him!
        worker.isSacrificed = true;
        worker.hexingTimeOfPriest = hexingTime / speedup;
        worker.actions = worker.Actions().GetEnumerator();
        animation.Act(hexingTime / speedup, 3, worker.transform.position);
        while (animation.IsActing) yield return 0;

        // Follow the poor guy to the temple
        // NICE COPY
        while (!worker.isReadyForSacrifice || (transform.position - worker.transform.position).sqrMagnitude >= 1) {
            bool gotPath = false;
            int steps = 10;
            foreach (int i in MoveTo(WorldGrid.instance.GridPos(worker.transform.position))) {
                yield return 0;
                if (worker == null || !worker.gameObject.activeSelf) {
                    worker = null;
                    break;
                }

                gotPath = true;
                if (--steps <= 0) break;
            }
            if (worker == null || abortMoveTo) {
                // For some reason the target became unwalkable, just choose another
                worker = null;
                break;
            }

            if (!gotPath) {
                // There is no path to the target.
                if (WorldGrid.instance.GridPos(worker.transform.position) == WorldGrid.instance.GridPos(transform.position)) {
                    // We are on the same cell, in which case we just move in direction of the target to reach its real world pos
                    DirectMoveTo(worker.transform.position);
                    yield return 0;
                    if (worker == null || !worker.gameObject.activeSelf) {
                        worker = null;
                        break;
                    }
                } else {
                    // The target is unreachable, in which case we should choose another
                    worker = null;
                    break;
                }
            }
        }
        // Make sure the worker reached the temple
        if (worker == null || worker.temple == null) yield break;
        temple = worker.temple;

        // NOW KILL HIM
        temple.StartSacrifice();
        animation.Act(sacrificingTime / speedup, 15, worker.transform.position);
        while (animation.IsActing) yield return 0;
        if (worker) worker.Die("was sacrificed by your Priest");
        if (temple) temple.EndSacrifice();
        yield return 0;
    }

    float hexingTimeOfPriest;
    IEnumerable<int> SacrificedWork() {
        // Wait for the animation of the priest
        float time = Time.time;
        while (Time.time < time + hexingTimeOfPriest) yield return 0;

        // Go to the temple
        Temple temple = null;
        foreach (var t in FindBuilding<Temple>(t => t.CanSacrifice(), false, true)) {
            if (t == null) {
                yield return 0;
            } else {
                temple = t;
            }
        }

        // Wait to die
        isReadyForSacrifice = true;
        this.temple = temple;
        while (true) yield return 0;
    }

    IEnumerable<int> BreederWork() {
        Warehouse warehouse = null;
        foreach (var w in FindBuilding<Warehouse>(w => w.Has(Resource.Food))) {
            if (w == null) {
                yield return 0;
            } else {
                warehouse = w;
            }
        }

        warehouse.RemoveElement(Resource.Food);
        animation.Hold(Resource.Food);

        House h = null;
        foreach (var b in FindBuilding<House>(b => b.CanStoreFood(), true, true)) {
            if (b == null) {
                yield return 0;
            } else {
                h = b;
            }
        }

        animation.Drop();
        h.AddFood();
        animation.Act(breedingTime / speedup, 3, InteractionWorldPos(h));
        while (animation.IsActing) {
            yield return 0;
        }
    }

    IEnumerable<int> EatSleep() {
        if (WorldGrid.instance.night) {
            house = null;
            foreach (var b in FindBuilding<House>(b => b.HasRoom(), false, true)) {
                if (b == null) {
                    yield return 0;
                } else {
                    house = b;
                }
            }

            house.Inhabit();
            isSleeping = true;
            gameObject.SetActive(false);
            WorldGrid.instance.sleepers.Add(this);
            yield return 0;

            // Awake, try to eat inside the house
            bool hasEaten = house.TryEat();
            house.Leave();
            house = null;
            if (!hasEaten) {
                // Go to eat outside
                Warehouse warehouse = null;
                float morning = Time.time;
                foreach (var w in FindBuilding<Warehouse>(w => w.Has(Resource.Food), false)) {
                    if (w == null) {
                        //Debug.Log("Trying to eat " + Time.time + " " + morning + " " + starvingTime);
                        if (Time.time - morning >= starvingTime) {
                            Die("starved to death");
                        }
                        yield return 0;
                    } else {
                        warehouse = w;
                    }
                }

                warehouse.RemoveElement(Resource.Food);
                yield return 0;
            }
        }
    }

    // Yields action to go to a building with the required predicate. Returns null until the last step, which is the found building.
    IEnumerable<B> FindBuilding<B>(Func<B, bool> pred, bool overrideEatSleep = true, bool onlyFront = false) where B : Building {
        while (true) {
            if (overrideEatSleep)
                foreach (int i in EatSleep()) {
                    yield return null;
                }

            WorldGrid.InteractableBuilding<B> target = WorldGrid.instance.NearestBuilding(WorldGrid.instance.GridPos(transform.position), pred, onlyFront);
            if (target.b == null) {
                // No building could be found, wait and restart!
                yield return null;
                continue;
            }

            // We have a building, now go to it.
            foreach (int i in MoveTo(target.pos)) {
                yield return null;
            }
            if (abortMoveTo) {
                // The building became unaccessible, wait and restart!
                yield return null;
                continue;
            }

            // Now that we are near the building, check if the predicate is still true
            if (target.b == null || !pred(target.b)) {
                continue; // Search another building now that we moved
            }

            // We are near a building with a true predicate!
            yield return target.b;
            yield break;
        }
    }

    public bool ComputePath() {
        if (!WorldGrid.instance.IsValid(target) || !WorldGrid.instance.IsWalkable(target)) {
            return false;
        }
        //Debug.Log("Computing path for " + this.ToString() + " to target " + target.ToString());
        Vector2Int origin = WorldGrid.instance.GridPos(transform.position);
        currentPath = WorldGrid.instance.Smooth(WorldGrid.instance.Path(origin, target));
        currentPathPos = 0;
        return true;
    }

    // Actions to reach a target. If there is no path, does nothing (sets currentPath to null).
    // Check that abortMoveTo is false afterwards!!
    public bool abortMoveTo;
    IEnumerable<int> MoveTo(Vector2Int target) {
        this.target = target;
        abortMoveTo = !ComputePath();
        if (currentPath == null) yield break;

        while (!abortMoveTo && currentPathPos < currentPath.Count) {
            // Goes to next checkpoint if we reached our current objective
            Vector3 objective = WorldGrid.instance.RealPos(currentPath[currentPathPos], height);
            Vector3 curPos = transform.position;
            if (Vector3.ProjectOnPlane(objective - curPos, Vector3.up).sqrMagnitude < 1e-12) {
                currentPathPos++;
                continue;
            }

            DirectMoveTo(WorldGrid.instance.RealPos(currentPath[currentPathPos], height));
            yield return 0;
        }

        animation.velocity = Vector3.zero;
        this.target = new Vector2Int(-1, -1);
    }

    // Directly updates the transform in direction of the target
    void DirectMoveTo(Vector3 target) {
        // Update velocity
        Vector2Int pos = WorldGrid.instance.GridPos(transform.position);
        float velocity = baseVelocity * speedup * (WorldGrid.instance.cells[pos.y, pos.x].isRoad ? WorldGrid.roadFactor : 1f);
        velocity *= 1f / Mathf.Lerp(1, faithSlowDown, 1 - faith.value);
        if (speedBonus > 0) velocity *= 1.5f;

        // Move to objective
        Vector3 delta = Vector3.ProjectOnPlane(target - transform.position, Vector3.up);
        delta = delta * Mathf.Min(1, velocity * Time.deltaTime / delta.magnitude);
        transform.position += delta;
        animation.velocity = delta / Time.deltaTime;
    }

    // Update is called once per frame
    void Update() {
        actions.MoveNext();
        if (speedBonus > 0) speedBonus = Mathf.Max(speedBonus - Time.deltaTime, 0);
    }

    public void Die(string message) {
        notif.Post(string.Format("Your {0} {1}", job.ToString(), message));
        foreach (VoxelModel model in GetComponentsInChildren<VoxelModel>()) {
            model.Explode();
        }
        Destroy(gameObject);
    }
}
