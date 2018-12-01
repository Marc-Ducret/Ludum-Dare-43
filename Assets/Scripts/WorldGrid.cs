using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGrid : MonoBehaviour {

    public static WorldGrid instance;

    public int width;
    public int height;
    
    private MonoBehaviour[] cells;
    
    private void Start() {
        if(instance != null) Debug.LogError("Multiples instances of Grid");
        instance = this;
        cells = new MonoBehaviour[width * height];
    }

    private void Update() {
    }
}
