using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Temple : Building {
    bool hasSacrifice = false;

    // Start is called before the first frame update
    new void Start() {
        base.Start();
    }

    public bool CanSacrifice() {
        return IsFinished() && !hasSacrifice;
    }

    public void StartSacrifice() {
        hasSacrifice = true;
    }

    public void EndSacrifice() {
        hasSacrifice = false;
    }

    // Update is called once per frame
    new void Update() {
        base.Update();
    }
}
