using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class buildingSelector : MonoBehaviour {
    
    public bool selected;
    public string selector; 
    
    // Start is called before the first frame update
    void Start() {
        selected = false;
        selector = null; 
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(selected);
    }

    public void UpdateSelection(string nameselector, bool select) {
        selected = select;
        selector = nameselector; 
    }
}
