using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class House : Building {
    public int population = 4;
    int peopleInside = 0;
    public Worker builder, breeder, priest, farmer, logger;
    Worker[] workerPrefabs;
    public int numBuilders, numBreeders, numPriests, numFarmers, numLoggers;
    float[] targetRepartition;

    public int foodCost = 5;
    public int foodCapacity = 20;
    int foodStored = 0;

    // Start is called before the first frame update
    new void Start() {
        base.Start();

        workerPrefabs = new Worker[5];
        workerPrefabs[(int)Worker.Job.Builder] = builder;
        workerPrefabs[(int)Worker.Job.Breeder] = breeder;
        workerPrefabs[(int)Worker.Job.Priest] = priest;
        workerPrefabs[(int)Worker.Job.Farmer] = farmer;
        workerPrefabs[(int)Worker.Job.Logger] = logger;

        float sum = numBuilders + numBreeders + numPriests + numFarmers + numLoggers;
        targetRepartition = new float[5];
        targetRepartition[(int)Worker.Job.Builder] = numBuilders / sum;
        targetRepartition[(int)Worker.Job.Breeder] = numBreeders / sum;
        targetRepartition[(int)Worker.Job.Priest] = numPriests / sum;
        targetRepartition[(int)Worker.Job.Farmer] = numFarmers / sum;
        targetRepartition[(int)Worker.Job.Logger] = numLoggers / sum;
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

    // Update is called once per frame
    new void Update() {
        base.Update();

        //string s = "";
        //foreach (Worker w in inhabitants) {
        //    s += " " + (w == null ? "null" : w.job.ToString()); 
        //}
        //Debug.Log("Inhabitants " + s);
    }

    public bool TryCreateWorker() {
        if (foodStored < foodCost) return false;
        List<Vector2Int> pos = InteractionPositions();
        if (pos.Count == 0) return false;

        int[] workers = new int[5];
        int numWorkers = 0;
        foreach (Worker wk in FindObjectsOfType<Worker>()) {
            workers[(int)wk.job]++;
            numWorkers++;
        }

        float maxGap = 0;
        int bestJob = 0;
        for (int job = 0; job < 5; job++) {
            float gap = targetRepartition[job] - workers[job] / (float)numWorkers;
            if (gap > maxGap) {
                maxGap = gap;
                bestJob = job;
            }
        }

        Worker w = Instantiate(workerPrefabs[bestJob]);
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
