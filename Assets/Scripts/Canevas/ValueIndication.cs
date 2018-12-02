using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class ValueIndication : MonoBehaviour {
    //public Text txt;
    public float value;
    private float width;
    private RectTransform trans;     

    public void Start() {
        trans = GetComponent<RectTransform>();
        width = trans.rect.width;
    }

    public void Update() {
        trans.offsetMax = new Vector2(trans.offsetMin.x + width * value, trans.offsetMax.y);
    }
}
