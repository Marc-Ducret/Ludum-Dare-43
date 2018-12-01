using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using UnityEngine;
using UnityEngine.UI;

public class SelectedBuilding : MonoBehaviour {
    public bool selected;
    public Text txt;
    public buildingSelector bs;

    // public Image im;
    // private bool b;

    // Start is called before the first frame update
    void Start() {
        txt.text = "No building selected";
        //bool selected = GameObject.FindWithTag("Image").GetComponent("imageTraitement").GetE();
        // b = im.selected;
        //    Debug.Log("Selected Building start" + b);
    }

    // Update is called once per frame
    void Update() {
        //   b = im.enabled;
        //     Debug.Log(b);
        //Debug.Log(GameObject.Find("Image").GetComponent("imageTraitment").selected);
        /* if (!im.) {
             Debug.Log(im.selected);
         }*/
    }
}

