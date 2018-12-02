using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Field : Building {
    private const int numCorn = 8;
    float[] plantTimes;
    public float matureTime = 15; // time in seconds

    // Start is called before the first frame update
    new void Start() {
        base.Start();

        plantTimes = Enumerable.Repeat(Time.time, numCorn).ToArray();
    }

    // Wether at least one corn is mature
    public bool hasCorn() {
        return plantTimes.Any(t => t + matureTime <= Time.time);
    }

    // Tries to harvest one corn. Returns true if it succeeded. Automatically replant.
    public bool harvest() {
        for (int i = 0; i < numCorn; i++) {
            if (plantTimes[i] + matureTime <= Time.time) {
                plantTimes[i] = Time.time;
                return true;
            }
        }
        return false;
    }
}
