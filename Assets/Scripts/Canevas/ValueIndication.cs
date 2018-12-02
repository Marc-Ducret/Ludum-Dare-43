using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class ValueIndication : MonoBehaviour {
    public Text txt;
    public int maxValue;
    private string buttonName; 

    // Start is called before the first frame update
    void Start() {
        buttonName = txt.text; 
        txt.text = buttonName + Convert.ToString(maxValue);
    }

    // Update is called once per frame
    void Update() {
    }
}
