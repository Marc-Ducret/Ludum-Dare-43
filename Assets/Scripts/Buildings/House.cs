using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class House : Building {
    public int population = 4;
    int peopleInside = 0;
    public Worker[] workerPrefabs;

    public int foodCost = 5;
    public int foodCapacity = 20;
    int foodStored = 0;

    private Light[] lights;

    new void Start() {
        base.Start();
        lights = GetComponentsInChildren<Light>();
    }

    public bool HasRoom() {
        return IsFinished() && peopleInside < population;
    }

    public void Inhabit() {
        peopleInside++;
    }

    public void Leave() {
        peopleInside--;
        if (peopleInside == 0) {
            while (TryCreateWorker()) ;
        }
    }

    public bool CanStoreFood() {
        return IsFinished() && foodStored < foodCapacity;
    }

    public void AddFood() {
        foodStored++;
    }

    new void Update() {
        base.Update();

        foreach (var l in lights) l.enabled = peopleInside > 0;
    }

    public bool TryCreateWorker() {
        if (foodStored < foodCost) return false;
        List<Vector2Int> pos = InteractionPositions();
        if (pos.Count == 0) return false;

        Worker w = Instantiate(workerPrefabs[Random.Range(0, workerPrefabs.Length)]);
        w.transform.position = WorldGrid.instance.RealPos(pos[Random.Range(0, pos.Count - 1)]);
        Debug.Log("Created " + w.ToString() + " at " + w.transform.position.ToString());
        foodStored -= foodCost;
        return true;
    }

    public bool TryEat() {
        if (foodStored > 0) {
            foodStored--;
            return true;
        }
        return false;
    }
}
