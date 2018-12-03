using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

[RequireComponent(typeof(Image))]
public class Faith : MonoBehaviour {
    
    public float value;
    public float faithDecay;

    public Gradient gradient;
    
    private float width;
    private RectTransform trans;
    private Image image;
    
    public void Start() {
        trans = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        width = trans.rect.width;
    }

    public void Update() {
        value -= faithDecay * Time.deltaTime;
        value = Mathf.Clamp(value, 0, 1);
        trans.offsetMax = new Vector2(trans.offsetMin.x + width * value, trans.offsetMax.y);
        image.color = gradient.Evaluate(value);
    }
}
