using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class BuildingSelector : MonoBehaviour {
    public bool selected;
    public string selector;
    public Camera camera;
    public LayerMask mask;
    public Transform cube;
    private Transform c;

    // Start is called before the first frame update
    void Start() {
        selected = false;
        selector = null;
        c = Instantiate(cube);
    }

    // Update is called once per frame
    void Update() {
        if (selected) {
            var ray = camera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            Physics.Raycast(ray, out hit, 100, mask);
            c.position = hit.point; 
            
            if (Input.GetMouseButtonDown(0)) {
                c.position = hit.point;
                c = Instantiate(cube);
                UpdateSelection(null, false);
                
            }
        }
    }

    public void UpdateSelection(string nameSelector, bool select) {
        selected = select;
        selector = nameSelector;
    }
}
