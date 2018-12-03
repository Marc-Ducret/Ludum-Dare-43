using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Field : Building {
    public float matureTime = 15; // time in seconds
    bool needFirstPlant = true;

    public struct Corn {
        public float plantTime;
        public int maxStage;
        public int stage;
        public VoxelModel model;
    }

    Corn[] corn;

    // Start is called before the first frame update
    new void Start() {
        base.Start();
        VoxelModel[] models = GetComponentsInChildren<VoxelModel>();
        models = models.Skip(1).ToArray();

        corn = new Corn[models.Length];

        for (int i = 0; i < corn.Length; i++) {
            corn[i].model = models[i];
            corn[i].maxStage = models[i].VoxelsList[models[i].VoxelsList.Count - 1].depth;
        }
    }

    public void Replant(int i) {
        corn[i].plantTime = Time.time;
        corn[i].stage = -1; // To trigger a (re)draw, since the actual stage is 0
    }

    // Wether at least one corn is mature
    public bool HasCorn() {
        return IsFinished() && corn.Any(t => t.stage == t.maxStage);
    }

    // Tries to harvest one corn. Automatically replant.
    public int Harvest() {
        for (int i = 0; i < corn.Length; i++) {
            if (corn[i].stage == corn[i].maxStage) {
                corn[i].stage++;
                return i;
            }
        }

        return -1;
    }



    new void Update() {
        base.Update();

        if (needFirstPlant && IsFinished()) {
            needFirstPlant = false;
            for (int i = 0; i < corn.Length; i++) {
                Replant(i);
            }
        }

        // Recompute stages, updating models if need be
        for (int i = 0; i < corn.Length; i++) {
            int stage = (int)(corn[i].maxStage * Mathf.Min(1, (Time.time - corn[i].plantTime) / matureTime));
            if (stage > corn[i].stage) {
                corn[i].stage = stage;
                // Redraw
                corn[i].model.GenerateMesh(stage);
            }
        }
    }
}
