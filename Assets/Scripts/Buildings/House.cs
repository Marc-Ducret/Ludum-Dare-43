﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class House : Building {
    public int population = 4;
    Worker[] inhabitants;

    public int foodRequired = 3;
    int foodStored;

    // Start is called before the first frame update
    new void Start() {
        base.Start();

        inhabitants = new Worker[population];
    }

    public bool HasRoom() {
        foreach (Worker w in inhabitants) {
            if (w == null) return true;
        }
        return false;
    }

    public bool Inhabit(Worker w) {
        for (int i = 0; i < population; i++) {
            if (inhabitants[i] == null) {
                inhabitants[i] = w;
                return true;
            }
        }
        return false;
    }

    public bool AddFood() {
        foodStored++;
        if (foodStored == foodRequired) {
            foodStored = 0;
            return true;
        }
        return false;
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
}