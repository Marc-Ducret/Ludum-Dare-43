using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Temple : Building {
    
    bool hasSacrifice;
    public float faithBonus;
    
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

    new void Update() {
        base.Update();

        if (faithBonus > 0 && IsFinished()) {
            FindObjectOfType<Faith>().value += faithBonus;
            faithBonus = 0;
        }
    }
}
